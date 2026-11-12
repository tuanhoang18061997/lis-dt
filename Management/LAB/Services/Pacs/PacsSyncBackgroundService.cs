using Management.BL.Pacs;
using Management.Models.Pacs;
using Microsoft.Extensions.Options;

namespace Management.Services.Pacs
{
    public sealed class PacsSyncBackgroundOptions
    {
        /// <summary>
        /// Global kill-switch.
        /// Server-level enable/disable vẫn lấy từ PacsConfigs.SyncEnabled.
        /// </summary>
        public bool Enabled { get; set; } = false;

        public int InitialDelaySeconds { get; set; } = 10;

        /// <summary>
        /// Delay dùng khi chưa có default PACS hoặc server-level sync đang tắt.
        /// </summary>
        public int IdlePollSeconds { get; set; } = 15;

        public int Limit { get; set; } = 100;

        public int MaxPages { get; set; } = 10;

        public int MinimumIntervalSeconds { get; set; } = 10;

        public int MaximumIntervalSeconds { get; set; } = 3600;
    }

    /// <summary>
    /// PACS-XQ-04-08
    ///
    /// Background consumer của checkpoint pipeline đã PASS ở 04-06/04-07.
    ///
    /// Quy tắc:
    /// - tạo IServiceScope mới cho mỗi cycle;
    /// - không giữ LABContext sống xuyên suốt lifetime của HostedService;
    /// - honor global Enabled + PacsConfigs.SyncEnabled;
    /// - interval lấy từ PacsConfigs.SyncIntervalSeconds;
    /// - lỗi một cycle không làm chết application;
    /// - cancellation shutdown được tôn trọng;
    /// - không tự bootstrap/reset checkpoint;
    /// - không gọi manual-range API;
    /// - checkpoint ownership vẫn nằm trong PacsChangeSyncBL.SyncFromCheckpointAsync().
    /// </summary>
    public sealed class PacsSyncBackgroundService
        : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<PacsSyncBackgroundService> _logger;
        private readonly IOptionsMonitor<PacsSyncBackgroundOptions>
            _optionsMonitor;

        public PacsSyncBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<PacsSyncBackgroundService> logger,
            IOptionsMonitor<PacsSyncBackgroundOptions> optionsMonitor)
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
                "PACS Background START. " +
                "GlobalEnabled={Enabled}, InitialDelay={InitialDelay}s, " +
                "IdlePoll={IdlePoll}s, Limit={Limit}, MaxPages={MaxPages}.",
                startupOptions.Enabled,
                startupOptions.InitialDelaySeconds,
                startupOptions.IdlePollSeconds,
                startupOptions.Limit,
                startupOptions.MaxPages);

            if (startupOptions.InitialDelaySeconds > 0)
            {
                try
                {
                    await Task.Delay(
                        TimeSpan.FromSeconds(
                            startupOptions.InitialDelaySeconds),
                        stoppingToken);
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                var options =
                    NormalizeOptions(
                        _optionsMonitor.CurrentValue);

                var nextDelaySeconds =
                    options.IdlePollSeconds;

                try
                {
                    if (!options.Enabled)
                    {
                        _logger.LogDebug(
                            "PACS Background global switch đang OFF.");

                        await DelayNextCycleAsync(
                            nextDelaySeconds,
                            stoppingToken);

                        continue;
                    }

                    await using var scope =
                        _scopeFactory.CreateAsyncScope();

                    var pacsConfigBL =
                        scope.ServiceProvider
                            .GetRequiredService<PacsConfigBL>();

                    var config =
                        await pacsConfigBL.GetDefaultAsync();

                    if (config == null)
                    {
                        _logger.LogWarning(
                            "PACS Background: chưa có PACS default active.");

                        await DelayNextCycleAsync(
                            nextDelaySeconds,
                            stoppingToken);

                        continue;
                    }

                    nextDelaySeconds =
                        NormalizeServerInterval(
                            config.SyncIntervalSeconds,
                            options);

                    if (!config.SyncEnabled)
                    {
                        _logger.LogDebug(
                            "PACS Background server sync OFF. " +
                            "PacsConfigId={PacsConfigId}, Code={Code}.",
                            config.Id,
                            config.Code);

                        await DelayNextCycleAsync(
                            Math.Min(
                                nextDelaySeconds,
                                options.IdlePollSeconds),
                            stoppingToken);

                        continue;
                    }

                    var changeSyncBL =
                        scope.ServiceProvider
                            .GetRequiredService<PacsChangeSyncBL>();

                    _logger.LogDebug(
                        "PACS Background cycle START. " +
                        "PacsConfigId={PacsConfigId}, Limit={Limit}, " +
                        "MaxPages={MaxPages}.",
                        config.Id,
                        options.Limit,
                        options.MaxPages);

                    var startedAt =
                        DateTime.Now;

                    var result =
                        await changeSyncBL
                            .SyncFromCheckpointAsync(
                                new PacsSyncCheckpointRequest
                                {
                                    Limit =
                                        options.Limit,
                                    MaxPages =
                                        options.MaxPages
                                },
                                stoppingToken);

                    var elapsedMs =
                        (DateTime.Now - startedAt)
                            .TotalMilliseconds;

                    if (result.Success)
                    {
                        if (result.UniqueStudyCount > 0)
                        {
                            _logger.LogInformation(
                                "PACS Background PASS. " +
                                "Checkpoint {Before}->{After}, " +
                                "Studies={Studies}, Success={Success}, " +
                                "Failed={Failed}, Changes={Changes}, " +
                                "ElapsedMs={ElapsedMs:0}.",
                                result.CheckpointBefore,
                                result.CheckpointAfter,
                                result.UniqueStudyCount,
                                result.SuccessStudyCount,
                                result.FailedStudyCount,
                                result.TotalChanges,
                                elapsedMs);
                        }
                        else
                        {
                            _logger.LogDebug(
                                "PACS Background NO-OP. " +
                                "Checkpoint {Before}->{After}, " +
                                "Changes={Changes}, ElapsedMs={ElapsedMs:0}.",
                                result.CheckpointBefore,
                                result.CheckpointAfter,
                                result.TotalChanges,
                                elapsedMs);
                        }
                    }
                    else
                    {
                        _logger.LogError(
                            "PACS Background cycle FAILED. " +
                            "Checkpoint {Before}->{After}, " +
                            "ErrorCode={ErrorCode}, Error={Error}, " +
                            "ElapsedMs={ElapsedMs:0}.",
                            result.CheckpointBefore,
                            result.CheckpointAfter,
                            result.ErrorCode,
                            result.ErrorMessage ??
                                result.Message,
                            elapsedMs);
                    }
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    /*
                     * Không rethrow.
                     * Một lần Orthanc/network/DB lỗi không được giết application.
                     */
                    _logger.LogError(
                        ex,
                        "PACS Background unexpected cycle error. " +
                        "Service sẽ retry ở cycle tiếp theo.");
                }

                try
                {
                    await DelayNextCycleAsync(
                        nextDelaySeconds,
                        stoppingToken);
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }

            _logger.LogInformation(
                "PACS Background STOP.");
        }

        private static PacsSyncBackgroundOptions
            NormalizeOptions(
                PacsSyncBackgroundOptions? source)
        {
            source ??=
                new PacsSyncBackgroundOptions();

            var minInterval =
                Math.Clamp(
                    source.MinimumIntervalSeconds,
                    5,
                    3600);

            var maxInterval =
                Math.Clamp(
                    source.MaximumIntervalSeconds,
                    minInterval,
                    86400);

            return new PacsSyncBackgroundOptions
            {
                Enabled =
                    source.Enabled,

                InitialDelaySeconds =
                    Math.Clamp(
                        source.InitialDelaySeconds,
                        0,
                        300),

                IdlePollSeconds =
                    Math.Clamp(
                        source.IdlePollSeconds,
                        5,
                        300),

                Limit =
                    Math.Clamp(
                        source.Limit,
                        1,
                        500),

                MaxPages =
                    Math.Clamp(
                        source.MaxPages,
                        1,
                        100),

                MinimumIntervalSeconds =
                    minInterval,

                MaximumIntervalSeconds =
                    maxInterval
            };
        }

        private static int NormalizeServerInterval(
            int configuredSeconds,
            PacsSyncBackgroundOptions options)
        {
            var fallback =
                60;

            var value =
                configuredSeconds > 0
                    ? configuredSeconds
                    : fallback;

            return Math.Clamp(
                value,
                options.MinimumIntervalSeconds,
                options.MaximumIntervalSeconds);
        }

        private static Task DelayNextCycleAsync(
            int seconds,
            CancellationToken cancellationToken)
        {
            return Task.Delay(
                TimeSpan.FromSeconds(seconds),
                cancellationToken);
        }
    }
}
