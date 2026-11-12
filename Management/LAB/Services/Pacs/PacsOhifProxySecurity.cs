using Microsoft.Extensions.Configuration;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Management.Services.Pacs
{
    /// <summary>
    /// DEV PREVIEW ONLY. Dedicated outbound TLS and server-to-server Basic Auth.
    /// Retains the existing OrthancClient CA Handler unchanged.
    /// </summary>
    public static class PacsOhifProxySecurity
    {
        private const string ExpectedUpstream = "https://192.168.10.106/";
        private const string CaSha256 =
            "8BE83055D29F39DD60F11362F745524B3D7B77B54EEE243D3700AC424A1FE7EC";

        public static void ConfigureYarpTls(IConfiguration configuration,
                                             SocketsHttpHandler handler)
        {
            var configured = configuration[
                "ReverseProxy:Clusters:ohif-cluster:Destinations:ohif-destination:Address"];
            if (!string.Equals(configured, ExpectedUpstream, StringComparison.Ordinal))
                throw new InvalidOperationException("OHIF HTTPS destination must be pinned.");

            var caDer = LoadPinnedPublicCa(configuration);
            handler.SslOptions.RemoteCertificateValidationCallback =
                (sender, certificate, chain, errors) =>
                    VerifyPinnedCertificate(caDer, certificate, errors);
        }

        private static byte[] LoadPinnedPublicCa(IConfiguration configuration)
        {
            var path = configuration["PacsTls:CaCertificatePath"];
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                throw new InvalidOperationException("PACS public CA file is unavailable.");
            var bytes = File.ReadAllBytes(path);
            var hash = Convert.ToHexString(SHA256.HashData(bytes));
            if (!string.Equals(hash, CaSha256, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("PACS public CA SHA256 mismatch.");
            using var ca = X509Certificate2.CreateFromPem(File.ReadAllText(path));
            return ca.Export(X509ContentType.Cert);
        }

        private static bool VerifyPinnedCertificate(byte[] caDer,
                       X509Certificate? certificate, SslPolicyErrors errors)
        {
            if (certificate == null ||
                (errors & (SslPolicyErrors.RemoteCertificateNotAvailable |
                           SslPolicyErrors.RemoteCertificateNameMismatch)) != 0)
                return false;
            try
            {
                using var cert = new X509Certificate2(certificate);
                using var root = new X509Certificate2(caDer);
                using var chain = new X509Chain();
                chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
                chain.ChainPolicy.CustomTrustStore.Add(root);
                chain.ChainPolicy.VerificationFlags = X509VerificationFlags.NoFlag;
                // Mirrors existing approved handler; do not bypass hostname or chain.
                chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                chain.ChainPolicy.ApplicationPolicy.Add(
                    new Oid("1.3.6.1.5.5.7.3.1"));
                return chain.Build(cert);
            }
            catch (System.Security.Cryptography.CryptographicException)
            {
                return false;
            }
        }

        public static AuthenticationHeaderValue BuildBasicAuthHeader(IConfiguration config)
        {
            // Source credentials only from approved server-side configuration provider.
            // Do not place passwords in appsettings files, browser, JS or logs.
            var username = config["PacsViewerProxy:Username"];
            var password = config["PacsViewerProxy:Password"];
            if (string.IsNullOrWhiteSpace(username) || username.Contains(':') ||
                string.IsNullOrEmpty(password))
                throw new InvalidOperationException("PACS viewer proxy secret not configured.");
            var raw = Convert.ToBase64String(
                Encoding.UTF8.GetBytes(username + ":" + password));
            return new AuthenticationHeaderValue("Basic", raw);
        }
    }
}
