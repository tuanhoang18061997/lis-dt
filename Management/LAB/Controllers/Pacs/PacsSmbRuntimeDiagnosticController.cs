using Management.BL.Pacs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Text.Json;

namespace Management.Controllers.Pacs
{
    // PACS-PROD-05B-03C-04D-02A
    // TEMPORARY diagnostic endpoint. Remove from source after its test is complete.
    // Disabled unless the short-lived, NTFS-protected guard file is created by an operator.
    [ApiController]
    [Authorize]
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("internal/pacs/04d02")]
    public sealed class PacsSmbRuntimeDiagnosticController : ControllerBase
    {
        private const string GateFile = @"F:\LAB\PACS\RuntimeDiag\04D-02-enable.json";
        private const string SmbShare = @"\\192.168.10.106\pacs-worklists";
        private const string CredentialFile = @"F:\LAB\PACS\Secrets\pacs-worklist-smb.dpapi";
        private static readonly SemaphoreSlim TestGate = new(1, 1);
        private readonly ILogger<PacsSmbRuntimeDiagnosticController> _logger;

        public PacsSmbRuntimeDiagnosticController(ILogger<PacsSmbRuntimeDiagnosticController> logger)
        {
            _logger = logger;
        }

        private sealed class DiagnosticGate
        {
            public string? Checkpoint { get; set; }
            public string? TokenSha256 { get; set; }
            public DateTimeOffset ExpiresUtc { get; set; }
        }

        [HttpPost("verify")]
        public async Task<IActionResult> Verify(CancellationToken cancellationToken)
        {
            // This endpoint must not be used for patient information or clinical workflows.
            if (!OperatingSystem.IsWindows() || !System.IO.File.Exists(GateFile))
                return NotFound();

            // This is defense-in-depth, not a replacement for [Authorize] + token.
            // Do not trust forwarded client IP headers for this check.
            var remoteIp = HttpContext.Connection.RemoteIpAddress;
            if (remoteIp == null || !IPAddress.IsLoopback(remoteIp))
                return NotFound();

            var supplied = Request.Headers["X-Pacs-Diagnostic-Token"].ToString();
            if (supplied.Length < 40 || supplied.Length > 200)
                return NotFound();

            try
            {
                var gateFileInfo = new FileInfo(GateFile);
                if ((gateFileInfo.Attributes & FileAttributes.ReparsePoint) != 0 ||
                    gateFileInfo.Length < 70 || gateFileInfo.Length > 4096)
                    return NotFound();

                DiagnosticGate? gate;
                await using (var stream = new FileStream(GateFile, FileMode.Open, FileAccess.Read,
                    FileShare.Read, 4096, FileOptions.Asynchronous))
                {
                    gate = await JsonSerializer.DeserializeAsync<DiagnosticGate>(stream,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                        cancellationToken);
                }

                var now = DateTimeOffset.UtcNow;
                if (gate == null ||
                    gate.Checkpoint != "PACS-PROD-05B-03C-04D-02" ||
                    gate.ExpiresUtc <= now || gate.ExpiresUtc > now.AddMinutes(15) ||
                    string.IsNullOrWhiteSpace(gate.TokenSha256) ||
                    gate.TokenSha256.Length != 64)
                    return NotFound();

                byte[] expected;
                try { expected = Convert.FromHexString(gate.TokenSha256); }
                catch (FormatException) { return NotFound(); }

                var actual = SHA256.HashData(Encoding.UTF8.GetBytes(supplied));
                if (!CryptographicOperations.FixedTimeEquals(actual, expected))
                    return NotFound();

                // Prevent concurrent diagnostics within this IIS worker process.
                if (!await TestGate.WaitAsync(0, cancellationToken))
                    return StatusCode(409, new { checkpoint = "04D-02", result = "BUSY" });

                try
                {
                    var identity = WindowsIdentity.GetCurrent().Name;
                    if (!identity.Equals(@"IIS APPPOOL\LIS", StringComparison.OrdinalIgnoreCase))
                    {
                        return StatusCode(503, new {
                            checkpoint = "04D-02", result = "STOP",
                            identityMatch = false,
                            dpapiSmbLogin = "NOT_ATTEMPTED"
                        });
                    }

                    // Never modify real appsettings or enable production Worklist publishing.
                    var values = new Dictionary<string, string?>
                    {
                        ["Pacs:WorklistFolder"] = SmbShare,
                        ["Pacs:WorklistSmbEnabled"] = "true",
                        ["Pacs:WorklistSmbShare"] = SmbShare,
                        ["Pacs:WorklistSmbUsername"] = "lis_pacs_mwl",
                        ["Pacs:WorklistSmbSecretFile"] = CredentialFile
                    };
                    IConfiguration testConfig = new ConfigurationBuilder()
                        .AddInMemoryCollection(values)
                        .Build();

                    await using (var scope = await PacsSmbConnectionScope.OpenAsync(testConfig, cancellationToken))
                    {
                        // Forces an actual SMB directory query. Do not return names or content.
                        if (!Directory.Exists(SmbShare))
                            throw new IOException("SMB_SHARE_NOT_ACCESSIBLE");
                        _ = Directory.EnumerateFileSystemEntries(SmbShare).Take(1).ToArray();
                    }

                    return Ok(new {
                        checkpoint = "PACS-PROD-05B-03C-04D-02",
                        result = "PASS",
                        identityMatch = true,
                        dpapiSmbLogin = "PASS",
                        smbReadOnly = "PASS",
                        smbDisconnected = "PASS",
                        remoteFileWrites = 0,
                        worklistPublishing = "NOT_ENABLED_BY_TEST"
                    });
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    return StatusCode(503, new { checkpoint = "04D-02", result = "CANCELLED" });
                }
                catch (Exception ex)
                {
                    // Log only error class and HRESULT. Never log the token or password.
                    _logger.LogWarning("PACS 04D-02 diagnostic stopped: {ErrorType} HResult={HResult}",
                        ex.GetType().Name, ex.HResult);
                    return StatusCode(503, new {
                        checkpoint = "04D-02", result = "STOP",
                        failureType = ex.GetType().Name,
                        errorHResult = ex.HResult,
                        remoteFileWrites = 0
                    });
                }
                finally
                {
                    TestGate.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("PACS 04D-02 gate failure: {ErrorType}", ex.GetType().Name);
                return NotFound();
            }
        }
    }
}
