using System.Globalization;
using Management.BL.Pacs;
using Microsoft.Extensions.Options;

namespace Management.Services.Pacs
{
    /// <summary>
    /// PACS-XQ-PREPROD-01D-02
    /// Automatic Completed Study Reconciliation scheduler.
    /// </summary>
    public sealed class PacsReconciliationBackgroundService
        : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<PacsReconciliationBackgroundService> _logger;
        private readonly IOptionsMonitor<PacsSyncReconciliationOptions>
            _optionsMonitor;

        public PacsReconciliationBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<PacsReconciliationBackgroundService> logger,
            IOptionsMonitor<PacsSyncReconciliationOptions> optionsMonitor)
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
                "PACS Reconciliation Background START. " +
                "Enabled={Enabled}, StartDate={StartDate}, " +
                "IntervalMinutes={IntervalMinutes}, BatchSize={BatchSize}, " +
                "MinAgeMinutes={MinAgeMinutes}.",
                startupOptions.Enabled,
                startupOptions.StartDate,
                startupOptions.IntervalMinutes,
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
                            "PACS Reconciliation Background switch đang OFF.");
                    }
                    else if (!TryParseStartDate(
                                 options.StartDate,
                                 out _))
                    {
                        _logger.LogWarning(
                            "PACS Reconciliation Background fail-closed: " +
                            "StartDate không hợp lệ. " +
                            "Expected yyyy-MM-dd. Actual={StartDate}.",
                            options.StartDate);
                    }
                    else
                    {
                        await RunReconciliationCycleAsync(
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
                        "PACS Reconciliation Background unexpected cycle error. " +
                        "Service sẽ retry ở cycle tiếp theo.");
                }

                try
                {
                    await Task.Delay(
                        TimeSpan.FromMinutes(
                            options.IntervalMinutes),
                        stoppingToken);
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }

            _logger.LogInformation(
                "PACS Reconciliation Background STOP.");
        }

        private async Task RunReconciliationCycleAsync(
            PacsSyncReconciliationOptions options,
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
                    "PACS Reconciliation Background idle: " +
                    "chưa có PACS default active.");

                return;
            }

            if (!config.SyncEnabled)
            {
                _logger.LogDebug(
                    "PACS Reconciliation Background server sync OFF. " +
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
                    .ReconcilePendingAsync(
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
                        "PACS Reconciliation Background PASS. " +
                        "Candidates={Candidates}, Match={Match}, " +
                        "Reconciled={Reconciled}, AlreadyMatched={AlreadyMatched}, " +
                        "NotStable={NotStable}, ExtraData={ExtraData}, " +
                        "ComplexMismatch={ComplexMismatch}, " +
                        "IdentityMismatch={IdentityMismatch}, " +
                        "PacsStudyNotFound={PacsStudyNotFound}, " +
                        "SyncBusy={SyncBusy}, Skipped={Skipped}, Errors={Errors}, " +
                        "ElapsedMs={ElapsedMs:0}.",
                        result.CandidateCount,
                        result.MatchCount,
                        result.ReconciledCount,
                        result.AlreadyMatchedCount,
                        result.NotStableCount,
                        result.ExtraDataCount,
                        result.ComplexMismatchCount,
                        result.IdentityMismatchCount,
                        result.PacsStudyNotFoundCount,
                        result.SyncBusyCount,
                        result.SkippedCount,
                        result.ErrorCount,
                        elapsedMs);
                }
                else
                {
                    _logger.LogDebug(
                        "PACS Reconciliation Background NO-OP. " +
                        "Candidates=0, ElapsedMs={ElapsedMs:0}.",
                        elapsedMs);
                }

                return;
            }

            _logger.LogWarning(
                "PACS Reconciliation Background cycle not successful. " +
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

        private static PacsSyncReconciliationOptions
            NormalizeOptions(
                PacsSyncReconciliationOptions? source)
        {
            source ??=
                new PacsSyncReconciliationOptions();

            return new PacsSyncReconciliationOptions
            {
                Enabled =
                    source.Enabled,

                StartDate =
                    source.StartDate?.Trim(),

                IntervalMinutes =
                    Math.Clamp(
                        source.IntervalMinutes,
                        1,
                        1440),

                BatchSize =
                    Math.Clamp(
                        source.BatchSize,
                        1,
                        500),

                MinAgeMinutes =
                    Math.Clamp(
                        source.MinAgeMinutes,
                        0,
                        10080)
            };
        }
    }
}
