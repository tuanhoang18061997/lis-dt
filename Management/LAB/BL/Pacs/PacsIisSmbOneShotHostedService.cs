using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Management.BL.Pacs;

/// <summary>
/// Gated one-shot SMB probe hosted inside LIS IIS. This class never reads patient data,
/// touches SQL or creates a DICOM .wl file. Keep Worklist production flags disabled.
/// Operator must prepare a restricted gate and a separate, restricted receipt folder.
/// </summary>
public sealed class PacsIisSmbOneShotHostedService : BackgroundService
{
    private const string GatePath = @"F:\LAB\PACS\RuntimeDiag\SMB-ONE-SHOT-ARM.txt";
    private const string ReceiptDirectory = @"F:\LAB\PACS\RuntimeDiag\Results";
    private const string GateText = "RUN-IIS-SMB-ONCE-20261029";
    private const string ExpectedShare = @"\\192.168.10.106\pacs-worklists";

    private readonly IConfiguration _configuration;
    private readonly ILogger<PacsIisSmbOneShotHostedService> _logger;

    public PacsIisSmbOneShotHostedService(
        IConfiguration configuration,
        ILogger<PacsIisSmbOneShotHostedService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!OperatingSystem.IsWindows() || !File.Exists(GatePath)) return;

        // The JSON receipt is written before the gate is consumed. If the IIS identity
        // cannot write results, the probe is not run. The receipt contains no secrets.
        if (!Directory.Exists(ReceiptDirectory))
        {
            _logger.LogWarning("PACS_IIS_SMB_ONESHOT STOP RECEIPT_DIRECTORY_MISSING");
            return;
        }

        var runId = Guid.NewGuid().ToString("N");
        var receipt = Path.Combine(ReceiptDirectory, "SMB-ONESHOT-" + runId + ".json");
        var startedUtc = DateTime.UtcNow;
        var stage = "GATE_VALIDATE";
        var status = "STOP";
        var exceptionType = "NONE";
        var hresult = 0;
        var receiptStarted = false;
        try
        {
            var gate = new FileInfo(GatePath);
            var now = DateTime.UtcNow;
            if (now - gate.LastWriteTimeUtc > TimeSpan.FromMinutes(5) ||
                gate.LastWriteTimeUtc > now.AddMinutes(1) ||
                (await File.ReadAllTextAsync(GatePath, stoppingToken)).Trim() != GateText)
            {
                _logger.LogWarning("PACS_IIS_SMB_ONESHOT STOP GATE_INVALID_OR_EXPIRED");
                return;
            }

            // Create the first receipt before deleting the gate: fail closed if no write ACL.
            await WriteReceiptAsync(receipt, startedUtc, "RUNNING", "GATE_VALIDATED", "NONE", 0, stoppingToken);
            receiptStarted = true;
            stage = "GATE_CONSUME";
            File.Delete(GatePath);
            await WriteReceiptAsync(receipt, startedUtc, "RUNNING", "GATE_CONSUMED", "NONE", 0, stoppingToken);

            stage = "CONFIG_VALIDATE";
            var share = (_configuration["Pacs:WorklistSmbShare"] ?? "").Trim().TrimEnd('\\');
            var username = (_configuration["Pacs:WorklistSmbUsername"] ?? "").Trim();
            var secret = (_configuration["Pacs:WorklistSmbSecretFile"] ?? "").Trim();
            if (!string.Equals(share, ExpectedShare, StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(secret))
                throw new InvalidOperationException("SMB configuration is incomplete or points to an unexpected share.");

            var isolated = new ConfigurationBuilder().AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Pacs:WorklistSmbEnabled"] = "true",
                    ["Pacs:WorklistFolder"] = ExpectedShare,
                    ["Pacs:WorklistSmbShare"] = ExpectedShare,
                    ["Pacs:WorklistSmbUsername"] = username,
                    ["Pacs:WorklistSmbSecretFile"] = secret
                }).Build();

            var name = "__pacs_iis_probe_" + runId;
            var temporary = Path.Combine(ExpectedShare, name + ".tmp");
            var renamed = Path.Combine(ExpectedShare, name + ".checked.tmp");
            var payload = Encoding.ASCII.GetBytes("PACS-ONESHOT-" + runId);
            try
            {
                stage = "SMB_CONNECT";
                await WriteReceiptAsync(receipt, startedUtc, "RUNNING", stage, "NONE", 0, stoppingToken);
                await using (var session = await PacsSmbConnectionScope.OpenAsync(isolated, stoppingToken))
                {
                    stage = "SMB_WRITE";
                    await WriteReceiptAsync(receipt, startedUtc, "RUNNING", stage, "NONE", 0, stoppingToken);
                    await using (var stream = new FileStream(temporary, FileMode.CreateNew,
                        FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous))
                    {
                        await stream.WriteAsync(payload, stoppingToken);
                        await stream.FlushAsync(stoppingToken);
                    }
                    stage = "SMB_RENAME_READBACK";
                    await WriteReceiptAsync(receipt, startedUtc, "RUNNING", stage, "NONE", 0, stoppingToken);
                    File.Move(temporary, renamed);
                    var observed = await File.ReadAllBytesAsync(renamed, stoppingToken);
                    try
                    {
                        if (!CryptographicOperations.FixedTimeEquals(payload, observed))
                            throw new InvalidDataException("Probe readback failed.");
                    }
                    finally { CryptographicOperations.ZeroMemory(observed); }
                    stage = "SMB_CLEANUP";
                    File.Delete(renamed);
                    await WriteReceiptAsync(receipt, startedUtc, "RUNNING", stage, "NONE", 0, stoppingToken);
                    stage = "SMB_DISCONNECT";
                }
                status = "PASS";
                stage = "COMPLETE";
            }
            finally
            {
                CryptographicOperations.ZeroMemory(payload);
                foreach (var path in new[] { temporary, renamed })
                {
                    try { if (File.Exists(path)) File.Delete(path); }
                    catch { _logger.LogWarning("PACS_IIS_SMB_ONESHOT CLEANUP_INCOMPLETE"); }
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            status = "STOP";
            exceptionType = "OperationCanceledException";
        }
        catch (Exception error)
        {
            status = "STOP";
            exceptionType = error.GetType().Name;
            hresult = error.HResult;
            // No exception message, stack trace, password, token or patient data is logged.
            _logger.LogWarning("PACS_IIS_SMB_ONESHOT STOP Stage={Stage} Type={Type} HResult={HResult}",
                stage, exceptionType, hresult);
        }
        finally
        {
            if (receiptStarted)
            {
                try
                {
                    await WriteReceiptAsync(receipt, startedUtc, status, stage,
                        exceptionType, hresult, CancellationToken.None);
                }
                catch
                {
                    _logger.LogError("PACS_IIS_SMB_ONESHOT RESULT_WRITE_FAILED");
                }
            }
        }
    }

    private static async Task WriteReceiptAsync(
        string receipt, DateTime startedUtc, string status, string stage,
        string exceptionType, int hresult, CancellationToken token)
    {
        var value = new
        {
            Checkpoint = "PACS-IIS-SMB-ONESHOT",
            Status = status,
            Stage = stage,
            ExceptionType = exceptionType,
            HResult = hresult,
            StartedUtc = startedUtc.ToString("O"),
            UpdatedUtc = DateTime.UtcNow.ToString("O")
        };
        var json = JsonSerializer.Serialize(value);
        var temp = receipt + ".writing";
        try
        {
            // No directory creation. Restricted directory must be prepared by operator.
            await File.WriteAllTextAsync(temp, json, new UTF8Encoding(false), token);
            File.Move(temp, receipt, true);
        }
        finally
        {
            try { if (File.Exists(temp)) File.Delete(temp); } catch { }
        }
    }
}
