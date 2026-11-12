using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Management.Services.Pacs
{
    /// <summary>
    /// PACS 07A-08B minimal bridge for the private CA (net6.0).
    /// No changes to Windows Trusted Root, OrthancClient or clinical workflows.
    /// </summary>
    public static class PacsTlsHandlerFactory
    {
        // SHA256 of the public ca.crt, verified on Ubuntu and the LIS server.
        private const string ExpectedCaSha256 =
            "8BE83055D29F39DD60F11362F745524B3D7B77B54EEE243D3700AC424A1FE7EC";

        public static HttpMessageHandler Create(IConfiguration configuration)
        {
            var caPath = configuration["PacsTls:CaCertificatePath"];

            // Preserve existing behavior when PACS custom-CA integration is not configured.
            // HTTPS with an untrusted private CA will still fail normally (no bypass).
            if (string.IsNullOrWhiteSpace(caPath))
                return new HttpClientHandler();

            var bytes = File.ReadAllBytes(caPath);
            var actualSha256 = Convert.ToHexString(SHA256.HashData(bytes));
            if (!string.Equals(actualSha256, ExpectedCaSha256,
                               StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("PACS TLS CA SHA256 mismatch.");
            }

            using var caCertificate = X509Certificate2.CreateFromPem(File.ReadAllText(caPath));
            var caDer = caCertificate.Export(X509ContentType.Cert);

            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback =
                (request, certificate, _, errors) =>
                {
                    // Never accept a missing or hostname/IP-mismatched server cert.
                    // The target IP SAN was already verified at checkpoint 07A-07D.
                    if (certificate == null || request.RequestUri == null ||
                        request.RequestUri.Scheme != Uri.UriSchemeHttps ||
                        !string.Equals(request.RequestUri.Host, "192.168.10.106",
                            StringComparison.OrdinalIgnoreCase) ||
                        (errors & (SslPolicyErrors.RemoteCertificateNotAvailable |
                                   SslPolicyErrors.RemoteCertificateNameMismatch)) != 0)
                    {
                        return false;
                    }

                    using var root = new X509Certificate2(caDer);
                    using var validationChain = new X509Chain();
                    validationChain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
                    validationChain.ChainPolicy.CustomTrustStore.Add(root);
                    validationChain.ChainPolicy.VerificationFlags = X509VerificationFlags.NoFlag;
                    // Mirrors the already PASS 07A-07D diagnostic (private CA has no CRL/OCSP).
                    validationChain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                    validationChain.ChainPolicy.ApplicationPolicy.Add(
                        new Oid("1.3.6.1.5.5.7.3.1")); // TLS serverAuth

                    return validationChain.Build(certificate);
                };
            return handler;
        }
    }
}
