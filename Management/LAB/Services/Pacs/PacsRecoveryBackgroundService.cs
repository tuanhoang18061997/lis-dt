using System.Globalization;
using Management.BL.Pacs;
using Microsoft.Extensions.Options;

namespace Management.Services.Pacs
{
    /// <summary>
    /// PACS-XQ-PREPROD-01D-02
    /// Automatic Missing Study Recovery scheduler.
    /// </summary>
    public sealed class PacsRecoveryBackgroundService
        : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<PacsRecoveryBackgroundService> _logger;
        private readonly IOptionsMonitor<PacsSyncRecoveryOptions>
            _optionsMonitor;

        public PacsRecoveryBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<PacsRecoveryBackgroundService> logger,
            IOptionsMonitor<PacsSyncRecoveryOptions> optionsMonitor)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _optionsMonitor = optionsMonitor;
        }

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            var startupOptions =
                NormalizeOptions(
                    _optionsMonitor.CurrentValue);

            _logger.LogInformation(
                "PACS Recovery Background START. " +
                "Enabled={Enabled}, StartDate={StartDate}, " +
                "IntervalSeconds={IntervalSeconds}, BatchSize={BatchSize}, " +
                "MinAgeMinutes={MinAgeMinutes}.",
                startupOptions.Enabled,
                startupOptions.StartDate,
                startupOptions.IntervalSeconds,
                startupOptions.BatchSize,
                startupOptions.MinAgeMinutes);

            while (!stoppingToken.IsCancellationRequested)
            {
                var options =
                    NormalizeOptions(
                        _optionsMonitor.CurrentValue);

                try
                {
                    if (!options.Enabled)
                    {
                        _logger.LogDebug(
                            "PACS Recovery Background switch đang OFF.");
                    }
                    else if (!TryParseStartDate(
                                 options.StartDate,
                                 out _))
                    {
                        _logger.LogWarning(
                            "PACS Recovery Background fail-closed: " +
                            "StartDate không hợp lệ. " +
                            "Expected yyyy-MM-dd. Actual={StartDate}.",
                            options.StartDate);
                    }
                    else
                    {
                        await RunRecoveryCycleAsync(
                            options,
                            stoppingToken);
                    }
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "PACS Recovery Background unexpected cycle error. " +
                        "Service sẽ retry ở cycle tiếp theo.");
                }

                try
                {
                    await Task.Delay(
                        TimeSpan.FromSeconds(
                            options.IntervalSeconds),
                        stoppingToken);
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }

            _logger.LogInformation(
                "PACS Recovery Background STOP.");
        }

        private async Task RunRecoveryCycleAsync(
            PacsSyncRecoveryOptions options,
            CancellationToken cancellationToken)
        {
            await using var scope =
                _scopeFactory.CreateAsyncScope();

            var pacsConfigBL =
                scope.ServiceProvider
                    .GetRequiredService<PacsConfigBL>();

            var config =
                await pacsConfigBL.GetDefaultAsync();

            if (config == null ||
                !config.IsActive ||
                !config.IsDefault)
            {
                _logger.LogWarning(
                    "PACS Recovery Background idle: " +
                    "chưa có PACS default active.");

                return;
            }

            if (!config.SyncEnabled)
            {
                _logger.LogDebug(
                    "PACS Recovery Background server sync OFF. " +
                    "PacsConfigId={PacsConfigId}, Code={Code}.",
                    config.Id,
                    config.Code);

                return;
            }

            var reliabilityBL =
                scope.ServiceProvider
                    .GetRequiredService<PacsReliabilityBL>();

            var startedAt =
                DateTime.Now;

            var result =
                await reliabilityBL
                    .RecoverPendingAsync(
                        options.BatchSize,
                        cancellationToken);

            var elapsedMs =
                (DateTime.Now - startedAt)
                    .TotalMilliseconds;

            if (result.Success)
            {
                if (result.CandidateCount > 0)
                {
                    _logger.LogInformation(
                        "PACS Recovery Background PASS. " +
                        "Candidates={Candidates}, Recovered={Recovered}, " +
                        "NotFound={NotFound}, NotStable={NotStable}, " +
                        "AlreadyHandled={AlreadyHandled}, Ambiguous={Ambiguous}, " +
                        "IdentityMismatch={IdentityMismatch}, SyncBusy={SyncBusy}, " +
                        "Skipped={Skipped}, Errors={Errors}, ElapsedMs={ElapsedMs:0}.",
                        result.CandidateCount,
                        result.RecoveredCount,
                        result.NotFoundCount,
                        result.NotStableCount,
                        result.AlreadyHandledCount,
                        result.AmbiguousCount,
                        result.IdentityMismatchCount,
                        result.SyncBusyCount,
                        result.SkippedCount,
                        result.ErrorCount,
                        elapsedMs);
                }
                else
                {
                    _logger.LogDebug(
                        "PACS Recovery Background NO-OP. " +
                        "Candidates=0, ElapsedMs={ElapsedMs:0}.",
                        elapsedMs);
                }

                return;
            }

            _logger.LogWarning(
                "PACS Recovery Background cycle not successful. " +
                "Status={Status}, Candidates={Candidates}, Errors={Errors}, " +
                "Message={Message}, Error={Error}, ElapsedMs={ElapsedMs:0}.",
                result.Status,
                result.CandidateCount,
                result.ErrorCount,
                result.Message,
                result.ErrorMessage,
                elapsedMs);
        }

        private static bool TryParseStartDate(
            string? value,
            out DateTime startDate)
        {
            return DateTime.TryParseExact(
                value?.Trim(),
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out startDate);
        }

        private static PacsSyncRecoveryOptions
            NormalizeOptions(
                PacsSyncRecoveryOptions? source)
        {
            source ??=
                new PacsSyncRecoveryOptions();

            return new PacsSyncRecoveryOptions
            {
                Enabled =
                    source.Enabled,

                StartDate =
                    source.StartDate?.Trim(),

                IntervalSeconds =
                    Math.Clamp(
                        source.IntervalSeconds,
                        10,
                        3600),

                BatchSize =
                    Math.Clamp(
                        source.BatchSize,
                        1,
                        500),

                MinAgeMinutes =
                    Math.Clamp(
                        source.MinAgeMinutes,
                        0,
                        1440)
            };
        }
    }
}
