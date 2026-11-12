using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Management.BL.Pacs;

/// <summary>
/// Temporary, explicitly gated development/UAT test for IIS -> encrypted SMB -> Orthanc MWL.
/// No SQL, no patient data, no appsettings changes, no clinical worklist activation.
/// C-FIND must be verified SEPARATELY from Ubuntu: this service only proves
/// synthetic publish, remote readback and safe cleanup from IIS.
/// </summary>
public sealed class PacsIisMwlSyntheticHostedService : BackgroundService
{
    private const string GatePath = @"F:\LAB\PACS\RuntimeDiag\MWL-E2E-ARM.txt";
    private const string FixturePath = @"F:\LAB\PACS\RuntimeDiag\Fixtures\TEST03C00001.wl";
    private const string ReceiptDirectory = @"F:\LAB\PACS\RuntimeDiag\Results";
    private const string GateText = "RUN-IIS-MWL-SYNTHETIC-ONCE";
    private const string ExpectedShare = @"\\192.168.10.106\pacs-worklists";
    private const string ExpectedAccession = "TEST03C00001";
    private const string FixtureHash = "A838825A23CF4E62971714CAE000466ED9A867C84729EBB019D7AD4C5DC18A8E";
    private const int FixtureLength = 424;
    private static readonly TimeSpan VisibilityWindow = TimeSpan.FromSeconds(150);

    private readonly IConfiguration _configuration;
    private readonly ILogger<PacsIisMwlSyntheticHostedService> _logger;

    public PacsIisMwlSyntheticHostedService(
        IConfiguration configuration, ILogger<PacsIisMwlSyntheticHostedService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!OperatingSystem.IsWindows() || !File.Exists(GatePath)) return;
        // A receipt directory must already exist, with a restricted IIS APPPOOL\LIS ACL.
        if (!Directory.Exists(ReceiptDirectory))
        {
            _logger.LogWarning("PACS_B1_MWL STOP RECEIPT_DIRECTORY_MISSING");
            return;
        }

        var runId = Guid.NewGuid().ToString("N");
        var receiptPath = Path.Combine(ReceiptDirectory, "MWL-E2E-" + runId + ".json");
        var startedUtc = DateTime.UtcNow;
        var stage = "GATE_VALIDATE";
        var status = "STOP";
        var exceptionType = "NONE";
        var hresult = 0;
        var receiptCreated = false;
        byte[] fixture = Array.Empty<byte>();

        try
        {
            var gate = new FileInfo(GatePath);
            var now = DateTime.UtcNow;
            if (gate.Attributes.HasFlag(FileAttributes.ReparsePoint) ||
                now - gate.LastWriteTimeUtc > TimeSpan.FromMinutes(5) ||
                gate.LastWriteTimeUtc > now.AddMinutes(1) ||
                (await File.ReadAllTextAsync(GatePath, stoppingToken)).Trim() != GateText)
            {
                _logger.LogWarning("PACS_B1_MWL STOP GATE_INVALID_OR_EXPIRED");
                return;
            }

            // Fail closed when the IIS identity cannot create the first receipt.
            await WriteReceiptAsync(receiptPath, startedUtc, "RUNNING", "GATE_VALIDATED", "NONE", 0, stoppingToken);
            receiptCreated = true;
            stage = "GATE_CONSUME";
            File.Delete(GatePath); // Consumed before any remote operation, so a recycle cannot replay it.
            await WriteReceiptAsync(receiptPath, startedUtc, "RUNNING", "GATE_CONSUMED", "NONE", 0, stoppingToken);

            stage = "CONFIG_VALIDATE";
            foreach (var key in new[] { "WorklistFolder", "WorklistStagingFolder", "WorklistQuarantineFolder" })
                if (!string.IsNullOrWhiteSpace(_configuration["Pacs:" + key]))
                    throw new InvalidOperationException("Patient Worklist configuration must remain disabled.");
            if (bool.TryParse(_configuration["Pacs:WorklistSmbEnabled"], out var liveEnabled) && liveEnabled)
                throw new InvalidOperationException("Patient Worklist SMB flag must remain disabled.");
            if (!string.IsNullOrWhiteSpace(_configuration["Pacs:WorklistSmbEnabled"]) &&
                !bool.TryParse(_configuration["Pacs:WorklistSmbEnabled"], out _))
                throw new InvalidOperationException("Patient Worklist SMB flag is invalid.");

            var share = (_configuration["Pacs:WorklistSmbShare"] ?? "").Trim().TrimEnd('\\');
            var username = (_configuration["Pacs:WorklistSmbUsername"] ?? "").Trim();
            var secret = (_configuration["Pacs:WorklistSmbSecretFile"] ?? "").Trim();
            if (!string.Equals(share, ExpectedShare, StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(secret))
                throw new InvalidOperationException("SMB configuration incomplete or unexpected.");

            stage = "FIXTURE_VALIDATE";
            if (!File.Exists(FixturePath) || new FileInfo(FixturePath).Attributes.HasFlag(FileAttributes.ReparsePoint))
                throw new FileNotFoundException("Verified synthetic fixture is absent.");
            if (new FileInfo(FixturePath).Length != FixtureLength)
                throw new InvalidDataException("Synthetic fixture length mismatch.");
            fixture = await File.ReadAllBytesAsync(FixturePath, stoppingToken);
            if (!string.Equals(Convert.ToHexString(SHA256.HashData(fixture)), FixtureHash, StringComparison.Ordinal))
                throw new InvalidDataException("Synthetic fixture hash mismatch.");

            var isolated = new ConfigurationBuilder().AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Pacs:WorklistSmbEnabled"] = "true", // only for this scoped synthetic operation
                    ["Pacs:WorklistFolder"] = ExpectedShare,
                    ["Pacs:WorklistSmbShare"] = ExpectedShare,
                    ["Pacs:WorklistSmbUsername"] = username,
                    ["Pacs:WorklistSmbSecretFile"] = secret
                }).Build();

            var publishing = Path.Combine(ExpectedShare, ExpectedAccession + ".publishing");
            var final = Path.Combine(ExpectedShare, ExpectedAccession + ".wl");
            stage = "SMB_CONNECT";
            await WriteReceiptAsync(receiptPath, startedUtc, "RUNNING", stage, "NONE", 0, stoppingToken);
            await using (var scope = await PacsSmbConnectionScope.OpenAsync(isolated, stoppingToken))
            {
                // Exactly one frozen synthetic filename. Never overwrite, rename or delete pre-existing files.
                stage = "REMOTE_COLLISION_CHECK";
                if (File.Exists(publishing) || File.Exists(final))
                    throw new IOException("Synthetic file collision; refusing to touch existing remote file.");

                var createdPublishing = false;
                var createdFinal = false;
                try
                {
                    stage = "SMB_WRITE_PUBLISHING";
                    await WriteReceiptAsync(receiptPath, startedUtc, "RUNNING", stage, "NONE", 0, stoppingToken);
                    await using (var stream = new FileStream(publishing, FileMode.CreateNew, FileAccess.Write,
                        FileShare.None, 4096, FileOptions.Asynchronous))
                    {
                        createdPublishing = true;
                        await stream.WriteAsync(fixture, stoppingToken);
                        await stream.FlushAsync(stoppingToken);
                    }

                    stage = "REMOTE_SHA256_READBACK";
                    await ConfirmHashAsync(publishing, stoppingToken);
                    stage = "ATOMIC_PUBLISH";
                    File.Move(publishing, final); // same SMB share/directory, no overwrite
                    createdPublishing = false;
                    createdFinal = true;
                    await ConfirmHashAsync(final, stoppingToken);

                    // This receipt means 'published', NOT 'Orthanc C-FIND PASS'.
                    stage = "VISIBLE_FOR_ORTHANC_CFIND";
                    await WriteReceiptAsync(receiptPath, startedUtc, "RUNNING", stage, "NONE", 0, stoppingToken);
                    await Task.Delay(VisibilityWindow, stoppingToken);
                }
                finally
                {
                    // Any process crash/forced kill can bypass finally. Operator must verify
                    // and clean up TEST03C00001.wl by SHA256 before any retry.
                    var priorStage = stage;
                    try
                    {
                        if (createdFinal) await DeleteOwnedAndVerifiedAsync(final);
                        if (createdPublishing) await DeleteOwnedAndVerifiedAsync(publishing);
                        if (createdFinal && File.Exists(final)) throw new IOException("Synthetic final file cleanup incomplete.");
                        if (createdPublishing && File.Exists(publishing)) throw new IOException("Synthetic partial file cleanup incomplete.");
                    }
                    catch
                    {
                        stage = "REMOTE_CLEANUP";
                        throw;
                    }
                    stage = priorStage;
                }
            }
            status = "PASS";
            stage = "PUBLISH_AND_CLEANUP_COMPLETE";
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            status = "STOP";
            exceptionType = "OperationCanceledException";
        }
        catch (Exception ex)
        {
            status = "STOP";
            exceptionType = ex.GetType().Name;
            hresult = ex.HResult;
            _logger.LogWarning("PACS_B1_MWL STOP Stage={Stage} Type={Type} HResult={HResult}",
                stage, exceptionType, hresult);
        }
        finally
        {
            if (fixture.Length > 0) CryptographicOperations.ZeroMemory(fixture);
            if (receiptCreated)
            {
                try
                {
                    await WriteReceiptAsync(receiptPath, startedUtc, status, stage,
                        exceptionType, hresult, CancellationToken.None);
                }
                catch { _logger.LogError("PACS_B1_MWL RESULT_WRITE_FAILED"); }
            }
        }
    }

    private static async Task ConfirmHashAsync(string path, CancellationToken ct)
    {
        if (new FileInfo(path).Attributes.HasFlag(FileAttributes.ReparsePoint))
            throw new IOException("Unexpected reparse point in synthetic file path.");
        var readback = await File.ReadAllBytesAsync(path, ct);
        try
        {
            if (readback.Length != FixtureLength ||
                !string.Equals(Convert.ToHexString(SHA256.HashData(readback)), FixtureHash, StringComparison.Ordinal))
                throw new InvalidDataException("Remote synthetic file SHA256 mismatch.");
        }
        finally { CryptographicOperations.ZeroMemory(readback); }
    }

    private static async Task DeleteOwnedAndVerifiedAsync(string path)
    {
        if (!File.Exists(path)) return;
        await ConfirmHashAsync(path, CancellationToken.None); // preserve suspicious files for manual review
        File.Delete(path);
    }

    private static async Task WriteReceiptAsync(string path, DateTime startedUtc, string status,
        string stage, string exceptionType, int hresult, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(new
        {
            Checkpoint = "PACS-B1-IIS-MWL-SYNTHETIC-PUBLISH",
            Status = status,
            Stage = stage,
            ExceptionType = exceptionType,
            HResult = hresult,
            Accession = ExpectedAccession, // synthetic identifier only
            FixtureSHA256 = FixtureHash,
            StartedUtc = startedUtc.ToString("O"),
            UpdatedUtc = DateTime.UtcNow.ToString("O")
        });
        var temp = path + ".writing";
        try
        {
            await File.WriteAllTextAsync(temp, json, new UTF8Encoding(false), ct);
            File.Move(temp, path, true);
        }
        finally { try { if (File.Exists(temp)) File.Delete(temp); } catch { } }
    }
}
