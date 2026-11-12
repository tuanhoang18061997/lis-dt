using Management.Models;
using Management.Models.Pacs;
using Management.Services.Pacs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Text.Json;

namespace Management.BL.Pacs
{
    /// <summary>
    /// Reliability safety-net around the canonical PACS pipeline.
    ///
    /// PREPROD reliability currently provides:
    /// - 01B-02: read-only Missing Study Recovery preview;
    /// - 01B-03: manual Recovery for exactly one validated Worklist;
    /// - 01B-04: bounded batch Recovery with anti-starvation scheduling;
    /// - 01C-02: read-only Completed Study Reconciliation preview;
    /// - 01C-03: manual ReconcileCompletedStudy through canonical PacsStudyBL;
    /// - 01C-05: bounded batch Reconciliation with anti-starvation scheduling.
    ///
    /// PacsReliabilityBL never owns PacsSyncState checkpoint. Actual Study /
    /// Series / Instance mutation is delegated to the canonical PacsStudyBL.
    /// </summary>
    public sealed class PacsReliabilityBL
    {
        private const int DefaultBatchSize = 50;
        private const int MaxBatchSize = 200;
        private const int DefaultMinAgeMinutes = 2;
        private const int MaxMinAgeMinutes = 1440;

        private const int DefaultReconciliationBatchSize = 20;
        private const int DefaultReconciliationMinAgeMinutes = 10;

        // Prevent two manual/automatic Recovery batches from competing for
        // the same anti-starvation candidate set. This lock does NOT own
        // PacsSyncState checkpoint and does NOT block normal FAST PATH reads.
        private static readonly SemaphoreSlim
            _recoveryBatchLock = new(1, 1);

        // Prevent two manual/automatic Reconciliation batches from competing
        // for the same completed Worklist candidate set. This lock is
        // completely separate from the Recovery batch lock and from
        // PacsSyncState checkpoint ownership.
        private static readonly SemaphoreSlim
            _reconciliationBatchLock = new(1, 1);

        private readonly LABContext _context;
        private readonly PacsConfigBL _pacsConfigBL;
        private readonly OrthancClient _orthancClient;
        private readonly PacsStudyBL _pacsStudyBL;
        private readonly IOptionsMonitor<PacsSyncRecoveryOptions>
            _recoveryOptions;

        private readonly IOptionsMonitor<PacsSyncReconciliationOptions>
            _reconciliationOptions;

        public PacsReliabilityBL(
            LABContext context,
            PacsConfigBL pacsConfigBL,
            OrthancClient orthancClient,
            PacsStudyBL pacsStudyBL,
            IOptionsMonitor<PacsSyncRecoveryOptions> recoveryOptions,
            IOptionsMonitor<PacsSyncReconciliationOptions> reconciliationOptions)
        {
            _context = context;
            _pacsConfigBL = pacsConfigBL;
            _orthancClient = orthancClient;
            _pacsStudyBL = pacsStudyBL;
            _recoveryOptions = recoveryOptions;
            _reconciliationOptions = reconciliationOptions;
        }

        /// <summary>
        /// PACS-XQ-PREPROD-01C-02
        ///
        /// Read-only preview for Completed Study Reconciliation.
        ///
        /// Candidate:
        /// - Project A Worklist Status=3 only;
        /// - CreatedOn >= configured Reconciliation StartDate;
        /// - CompletedOn must exist and be older than MinAgeMinutes;
        /// - OrthancStudyId must already be persisted by the canonical sync.
        ///
        /// Safety:
        /// - StartDate is mandatory and fail-closed;
        /// - no SaveChangesAsync;
        /// - no SyncSingleStudy/TrySyncSingleStudy call;
        /// - no LastReconciliationCheckedOn mutation;
        /// - no PacsSyncState/checkpoint mutation.
        /// </summary>
        public async Task<PacsReconciliationPreviewResult>
            PreviewReconciliationAsync(
                int? requestedBatchSize = null,
                CancellationToken cancellationToken = default)
        {
            var result =
                new PacsReconciliationPreviewResult();

            var options =
                _reconciliationOptions.CurrentValue
                ?? new PacsSyncReconciliationOptions();

            if (!TryParseReconciliationStartDate(
                    options.StartDate,
                    out var reconciliationFrom))
            {
                result.Success = false;
                result.Status =
                    PacsReconciliationStatuses.ConfigInvalid;
                result.Message =
                    "PacsSyncReconciliation:StartDate bắt buộc có định dạng yyyy-MM-dd. " +
                    "Reconciliation fail-closed để không quét nhầm dữ liệu lịch sử.";

                return result;
            }

            var minAgeMinutes =
                NormalizeReconciliationMinAgeMinutes(
                    options.MinAgeMinutes);

            var batchSize =
                NormalizeReconciliationBatchSize(
                    requestedBatchSize,
                    options.BatchSize);

            var reconciliationTo =
                DateTime.Now.AddMinutes(
                    -minAgeMinutes);

            result.ReconciliationFrom =
                reconciliationFrom;

            result.ReconciliationTo =
                reconciliationTo;

            result.MinAgeMinutes =
                minAgeMinutes;

            result.BatchSize =
                batchSize;

            var config =
                await _pacsConfigBL.GetDefaultAsync();

            if (config == null ||
                !config.IsActive ||
                !config.IsDefault)
            {
                result.Success = false;
                result.Status =
                    PacsReconciliationStatuses.PacsConfigNotFound;
                result.Message =
                    "Chưa có PACS Server mặc định đang hoạt động.";

                return result;
            }

            _orthancClient.Configure(config);

            /*
             * READ ONLY candidate scan.
             *
             * Anti-starvation order is already frozen for the later batch:
             * 1. never checked first;
             * 2. oldest LastReconciliationCheckedOn;
             * 3. oldest CompletedOn;
             * 4. smallest Id.
             *
             * Preview intentionally DOES NOT update
             * LastReconciliationCheckedOn.
             */
            var candidates =
                await _context.PacsWorklistItems
                    .AsNoTracking()
                    .Where(x =>
                        x.Status == 3 &&
                        x.CreatedOn >= reconciliationFrom &&
                        x.CompletedOn.HasValue &&
                        x.CompletedOn.Value <= reconciliationTo &&
                        x.OrthancStudyId != null &&
                        x.OrthancStudyId != "")
                    .OrderBy(x =>
                        x.LastReconciliationCheckedOn.HasValue
                            ? 1
                            : 0)
                    .ThenBy(x =>
                        x.LastReconciliationCheckedOn)
                    .ThenBy(x =>
                        x.CompletedOn)
                    .ThenBy(x =>
                        x.Id)
                    .Take(batchSize)
                    .Select(x => new
                    {
                        x.Id,
                        x.ResultCDHAId,
                        x.AccessionNumber,
                        x.Status,
                        x.CreatedOn,
                        x.CompletedOn,
                        x.LastReconciliationCheckedOn,
                        x.OrthancStudyId,
                        x.StudyInstanceUID
                    })
                    .ToListAsync(
                        cancellationToken);

            result.CandidateCount =
                candidates.Count;

            foreach (var candidate in candidates)
            {
                cancellationToken
                    .ThrowIfCancellationRequested();

                var item =
                    new PacsReconciliationPreviewItem
                    {
                        WorklistId =
                            candidate.Id,

                        ResultCDHAId =
                            candidate.ResultCDHAId,

                        AccessionNumber =
                            NormalizeIdentity(
                                candidate.AccessionNumber),

                        CreatedOn =
                            candidate.CreatedOn,

                        CompletedOn =
                            candidate.CompletedOn,

                        LastReconciliationCheckedOn =
                            candidate.LastReconciliationCheckedOn,

                        WorklistOrthancStudyId =
                            NormalizeIdentity(
                                candidate.OrthancStudyId),

                        WorklistStudyInstanceUID =
                            NormalizeIdentity(
                                candidate.StudyInstanceUID)
                    };

                try
                {
                    var worklistOrthancStudyId =
                        item.WorklistOrthancStudyId;

                    if (string.IsNullOrWhiteSpace(
                            worklistOrthancStudyId))
                    {
                        item.ReconciliationStatus =
                            PacsReconciliationStatuses.IdentityMismatch;
                        item.CanReconcile = false;
                        item.Message =
                            "Completed Worklist không có OrthancStudyId hợp lệ.";

                        result.IdentityMismatchCount++;
                        result.Items.Add(item);
                        continue;
                    }

                    /*
                     * Resolve the LIS PacsStudy by immutable Orthanc identity.
                     * Defensive fallback by StudyInstanceUID is read-only only.
                     */
                    var pacsStudy =
                        await _context.PacsStudies
                            .AsNoTracking()
                            .FirstOrDefaultAsync(
                                x =>
                                    x.OrthancStudyId ==
                                    worklistOrthancStudyId,
                                cancellationToken);

                    if (pacsStudy == null &&
                        !string.IsNullOrWhiteSpace(
                            item.WorklistStudyInstanceUID))
                    {
                        var worklistStudyInstanceUid =
                            item.WorklistStudyInstanceUID;

                        pacsStudy =
                            await _context.PacsStudies
                                .AsNoTracking()
                                .FirstOrDefaultAsync(
                                    x =>
                                        x.StudyInstanceUID ==
                                        worklistStudyInstanceUid,
                                    cancellationToken);
                    }

                    if (pacsStudy == null)
                    {
                        item.ReconciliationStatus =
                            PacsReconciliationStatuses.Error;
                        item.CanReconcile = false;
                        item.Message =
                            "Worklist đã Completed nhưng không resolve được PacsStudy trong LIS.";
                        item.ErrorMessage =
                            "LIS_PACS_STUDY_METADATA_MISSING";

                        result.ErrorCount++;
                        result.Items.Add(item);
                        continue;
                    }

                    item.PacsStudyId =
                        pacsStudy.Id;

                    item.PacsStudyStatus =
                        pacsStudy.Status;

                    item.LisRecordedSeriesCount =
                        pacsStudy.SeriesCount;

                    item.LisRecordedInstanceCount =
                        pacsStudy.InstanceCount;

                    /*
                     * Validate persisted LIS identity before querying Orthanc.
                     * Reconciliation is not allowed to guess across identities.
                     */
                    if (pacsStudy.ResultCDHAId !=
                            candidate.ResultCDHAId
                        ||
                        !string.Equals(
                            NormalizeIdentity(
                                pacsStudy.OrthancStudyId),
                            worklistOrthancStudyId,
                            StringComparison.OrdinalIgnoreCase)
                        ||
                        !IdentityMatchesRequired(
                            item.WorklistStudyInstanceUID,
                            NormalizeIdentity(
                                pacsStudy.StudyInstanceUID))
                        ||
                        !IdentityMatchesRequired(
                            item.AccessionNumber,
                            NormalizeIdentity(
                                pacsStudy.AccessionNumber)))
                    {
                        item.ReconciliationStatus =
                            PacsReconciliationStatuses.IdentityMismatch;
                        item.CanReconcile = false;
                        item.Message =
                            "Identity giữa Completed Worklist và PacsStudy trong LIS không khớp.";

                        result.IdentityMismatchCount++;
                        result.Items.Add(item);
                        continue;
                    }

                    item.LisActualSeriesCount =
                        await _context.PacsSeries
                            .AsNoTracking()
                            .CountAsync(
                                x =>
                                    x.PacsStudyId ==
                                    pacsStudy.Id,
                                cancellationToken);

                    item.LisActualInstanceCount =
                        await _context.PacsInstances
                            .AsNoTracking()
                            .CountAsync(
                                x =>
                                    x.PacsStudyId ==
                                    pacsStudy.Id,
                                cancellationToken);

                    string studyJson;

                    try
                    {
                        studyJson =
                            await _orthancClient
                                .GetStudyJsonAsync(
                                    worklistOrthancStudyId,
                                    cancellationToken);
                    }
                    catch (HttpRequestException ex)
                        when (
                            ex.StatusCode ==
                            HttpStatusCode.NotFound)
                    {
                        item.ReconciliationStatus =
                            PacsReconciliationStatuses.PacsStudyNotFound;
                        item.CanReconcile = false;
                        item.Message =
                            "Completed Worklist tham chiếu Orthanc Study không còn tồn tại.";
                        item.ErrorMessage =
                            ex.GetBaseException().Message;

                        result.PacsStudyNotFoundCount++;
                        result.Items.Add(item);
                        continue;
                    }

                    using var studyDocument =
                        JsonDocument.Parse(
                            studyJson);

                    var studyRoot =
                        studyDocument.RootElement;

                    var mainTags =
                        GetObject(
                            studyRoot,
                            "MainDicomTags");

                    item.OrthancAccessionNumber =
                        NormalizeIdentity(
                            GetString(
                                mainTags,
                                "AccessionNumber"));

                    item.OrthancStudyInstanceUID =
                        NormalizeIdentity(
                            GetString(
                                mainTags,
                                "StudyInstanceUID"));

                    item.IsStable =
                        GetNullableBoolean(
                            studyRoot,
                            "IsStable");

                    if (item.IsStable != true)
                    {
                        item.ReconciliationStatus =
                            PacsReconciliationStatuses.NotStable;
                        item.CanReconcile = false;
                        item.Message =
                            "Study Orthanc hiện chưa Stable. Chưa được Reconcile.";

                        result.NotStableCount++;
                        result.Items.Add(item);
                        continue;
                    }

                    /*
                     * End-to-end identity must agree:
                     * Worklist == PacsStudy == current Orthanc metadata.
                     */
                    if (!IdentityMatchesRequired(
                            item.AccessionNumber,
                            item.OrthancAccessionNumber)
                        ||
                        !IdentityMatchesRequired(
                            item.WorklistStudyInstanceUID,
                            item.OrthancStudyInstanceUID)
                        ||
                        !IdentityMatchesRequired(
                            NormalizeIdentity(
                                pacsStudy.StudyInstanceUID),
                            item.OrthancStudyInstanceUID))
                    {
                        item.ReconciliationStatus =
                            PacsReconciliationStatuses.IdentityMismatch;
                        item.CanReconcile = false;
                        item.Message =
                            "Identity giữa Worklist/PacsStudy và Orthanc không khớp.";

                        result.IdentityMismatchCount++;
                        result.Items.Add(item);
                        continue;
                    }

                    var orthancSeriesIds =
                        GetStringArray(
                            studyRoot,
                            "Series");

                    item.OrthancSeriesCount =
                        orthancSeriesIds.Count;

                    item.OrthancInstanceCount =
                        await ReadOrthancInstanceCountAsync(
                            worklistOrthancStudyId,
                            item.OrthancSeriesCount,
                            cancellationToken);

                    item.SeriesDelta =
                        item.OrthancSeriesCount -
                        item.LisActualSeriesCount;

                    item.InstanceDelta =
                        item.OrthancInstanceCount -
                        item.LisActualInstanceCount;

                    var actualSeriesMatch =
                        item.LisActualSeriesCount ==
                        item.OrthancSeriesCount;

                    var actualInstanceMatch =
                        item.LisActualInstanceCount ==
                        item.OrthancInstanceCount;

                    var recordedCountsMatchActual =
                        item.LisRecordedSeriesCount ==
                            item.LisActualSeriesCount
                        &&
                        item.LisRecordedInstanceCount ==
                            item.LisActualInstanceCount;

                    if (actualSeriesMatch &&
                        actualInstanceMatch &&
                        recordedCountsMatchActual)
                    {
                        item.ReconciliationStatus =
                            PacsReconciliationStatuses.Match;
                        item.CanReconcile = false;
                        item.Message =
                            "LIS và Orthanc đang khớp.";

                        result.MatchCount++;
                        result.Items.Add(item);
                        continue;
                    }

                    /*
                     * Child rows already match Orthanc. Only PacsStudy's
                     * metadata counts are stale.
                     */
                    if (actualSeriesMatch &&
                        actualInstanceMatch)
                    {
                        item.ReconciliationStatus =
                            PacsReconciliationStatuses.MetadataCountMismatch;
                        item.CanReconcile = true;
                        item.Message =
                            "Child Series/Instances trong LIS đã đủ nhưng " +
                            "SeriesCount/InstanceCount trên PacsStudy đang stale.";

                        result.MetadataMismatchCount++;
                        result.Items.Add(item);
                        continue;
                    }

                    /*
                     * Orthanc has equal-or-more data in both dimensions and
                     * at least one dimension is greater: safe candidate for
                     * later canonical re-sync.
                     */
                    if (item.SeriesDelta >= 0 &&
                        item.InstanceDelta >= 0 &&
                        (item.SeriesDelta > 0 ||
                         item.InstanceDelta > 0))
                    {
                        item.ReconciliationStatus =
                            PacsReconciliationStatuses.LisMissingData;
                        item.CanReconcile = true;
                        item.Message =
                            $"LIS thiếu dữ liệu so với Orthanc. " +
                            $"SeriesDelta={item.SeriesDelta}, " +
                            $"InstanceDelta={item.InstanceDelta}.";

                        result.MissingDataCount++;
                        result.Items.Add(item);
                        continue;
                    }

                    /*
                     * Project A safety rule:
                     * canonical Study sync can remove stale hierarchy rows.
                     * Never auto-reconcile when LIS is currently the superset.
                     */
                    if (item.SeriesDelta <= 0 &&
                        item.InstanceDelta <= 0 &&
                        (item.SeriesDelta < 0 ||
                         item.InstanceDelta < 0))
                    {
                        item.ReconciliationStatus =
                            PacsReconciliationStatuses.LisHasExtraData;
                        item.CanReconcile = false;
                        item.Message =
                            $"LIS đang có nhiều dữ liệu hơn Orthanc. " +
                            $"SeriesDelta={item.SeriesDelta}, " +
                            $"InstanceDelta={item.InstanceDelta}. " +
                            "Không được tự Reconcile.";

                        result.ExtraDataCount++;
                        result.Items.Add(item);
                        continue;
                    }

                    item.ReconciliationStatus =
                        PacsReconciliationStatuses.CountMismatchComplex;
                    item.CanReconcile = false;
                    item.Message =
                        $"Count mismatch không đơn giản. " +
                        $"SeriesDelta={item.SeriesDelta}, " +
                        $"InstanceDelta={item.InstanceDelta}. " +
                        "Cần kiểm tra thủ công.";

                    result.ComplexMismatchCount++;
                }
                catch (OperationCanceledException)
                    when (
                        cancellationToken
                            .IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    item.ReconciliationStatus =
                        PacsReconciliationStatuses.Error;
                    item.CanReconcile = false;
                    item.Message =
                        "Lỗi khi Preview Reconciliation.";
                    item.ErrorMessage =
                        ex.GetBaseException().Message;

                    result.ErrorCount++;
                }

                result.Items.Add(item);
            }

            result.Success =
                true;

            result.Status =
                result.CandidateCount == 0
                    ? PacsReconciliationStatuses.NoCandidates
                    : result.ErrorCount == 0
                        ? PacsReconciliationStatuses.Completed
                        : PacsReconciliationStatuses.CompletedWithErrors;

            result.Message =
                $"Reconciliation Preview hoàn tất. " +
                $"Candidates={result.CandidateCount}, " +
                $"Match={result.MatchCount}, " +
                $"MissingData={result.MissingDataCount}, " +
                $"MetadataMismatch={result.MetadataMismatchCount}, " +
                $"ExtraData={result.ExtraDataCount}, " +
                $"ComplexMismatch={result.ComplexMismatchCount}, " +
                $"PacsStudyNotFound={result.PacsStudyNotFoundCount}, " +
                $"NotStable={result.NotStableCount}, " +
                $"IdentityMismatch={result.IdentityMismatchCount}, " +
                $"Errors={result.ErrorCount}.";

            return result;
        }

        /// <summary>
        /// PACS-XQ-PREPROD-01C-03
        ///
        /// Manual Reconciliation for exactly one Completed Worklist.
        ///
        /// Mutation is allowed ONLY when the fresh targeted inspection is:
        /// - LIS_MISSING_DATA, or
        /// - METADATA_COUNT_MISMATCH.
        ///
        /// All Study/Series/Instance writes are delegated to
        /// PacsStudyBL.TrySyncSingleStudyAsync(). This method never owns or
        /// updates PacsSyncState checkpoint and never rewrites CompletedOn.
        /// </summary>
        public async Task<PacsReconciliationManualResult>
            ReconcileCompletedStudyAsync(
                long pacsWorklistItemId,
                CancellationToken cancellationToken = default,
                bool orthancAlreadyConfigured = false)
        {
            var result =
                new PacsReconciliationManualResult
                {
                    WorklistId =
                        pacsWorklistItemId
                };

            if (pacsWorklistItemId <= 0)
            {
                result.Success = false;
                result.Status =
                    PacsReconciliationStatuses.InvalidWorklistId;
                result.Message =
                    "PacsWorklistItemId không hợp lệ.";

                return result;
            }

            var options =
                _reconciliationOptions.CurrentValue
                ?? new PacsSyncReconciliationOptions();

            if (!TryParseReconciliationStartDate(
                    options.StartDate,
                    out var reconciliationFrom))
            {
                result.Success = false;
                result.Status =
                    PacsReconciliationStatuses.ConfigInvalid;
                result.Message =
                    "PacsSyncReconciliation:StartDate bắt buộc có định dạng yyyy-MM-dd. " +
                    "Reconciliation fail-closed.";

                return result;
            }

            var minAgeMinutes =
                NormalizeReconciliationMinAgeMinutes(
                    options.MinAgeMinutes);

            var reconciliationTo =
                DateTime.Now.AddMinutes(
                    -minAgeMinutes);

            var worklist =
                await _context.PacsWorklistItems
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        x =>
                            x.Id ==
                            pacsWorklistItemId,
                        cancellationToken);

            if (worklist == null)
            {
                result.Success = false;
                result.Status =
                    PacsReconciliationStatuses.WorklistNotFound;
                result.Message =
                    "Không tìm thấy PacsWorklistItem.";

                return result;
            }

            result.ResultCDHAId =
                worklist.ResultCDHAId;

            result.AccessionNumber =
                NormalizeIdentity(
                    worklist.AccessionNumber);

            result.OrthancStudyId =
                NormalizeIdentity(
                    worklist.OrthancStudyId);

            result.StudyInstanceUID =
                NormalizeIdentity(
                    worklist.StudyInstanceUID);

            result.CompletedOnBefore =
                worklist.CompletedOn;

            if (worklist.Status != 3)
            {
                result.Success = false;
                result.Status =
                    PacsReconciliationStatuses.InvalidWorklistStatus;
                result.Message =
                    $"Worklist phải Status=3 để Reconcile. Actual={worklist.Status}.";

                return result;
            }

            if (worklist.CreatedOn <
                    reconciliationFrom
                ||
                !worklist.CompletedOn.HasValue
                ||
                worklist.CompletedOn.Value >
                    reconciliationTo)
            {
                result.Success = false;
                result.Status =
                    PacsReconciliationStatuses.OutsideReconciliationWindow;
                result.Message =
                    "Worklist nằm ngoài Reconciliation window hoặc chưa đủ MinAgeMinutes.";

                return result;
            }

            if (string.IsNullOrWhiteSpace(
                    result.OrthancStudyId))
            {
                result.Success = false;
                result.Status =
                    PacsReconciliationStatuses.MissingOrthancStudyId;
                result.Message =
                    "Completed Worklist không có OrthancStudyId.";

                return result;
            }

            var config =
                await _pacsConfigBL.GetDefaultAsync();

            if (config == null ||
                !config.IsActive ||
                !config.IsDefault)
            {
                result.Success = false;
                result.Status =
                    PacsReconciliationStatuses.PacsConfigNotFound;
                result.Message =
                    "Chưa có PACS Server mặc định đang hoạt động.";

                return result;
            }

            /*
             * Direct/manual endpoint owns initial OrthancClient configuration.
             *
             * PREPROD-01C-05 batch already configured this exact
             * PacsReliabilityBL OrthancClient before its first inspection.
             * HttpClient BaseAddress/Timeout cannot be assigned again after
             * the first request has started, so nested batch Reconciliation
             * must reuse the already-configured client.
             */
            if (!orthancAlreadyConfigured)
            {
                _orthancClient.Configure(config);
            }

            /*
             * Fresh targeted inspection immediately before mutation.
             * This does NOT depend on Preview batch ordering.
             */
            var inspection =
                await InspectCompletedWorklistAsync(
                    worklist,
                    cancellationToken);

            CopyInspectionToManualBefore(
                result,
                inspection);

            if (string.Equals(
                    inspection.ReconciliationStatus,
                    PacsReconciliationStatuses.Match,
                    StringComparison.OrdinalIgnoreCase))
            {
                result.Success = true;
                result.Status =
                    PacsReconciliationStatuses.AlreadyMatched;
                result.Message =
                    "LIS và Orthanc hiện đã khớp. Không cần Reconcile.";
                result.CompletedOnAfter =
                    result.CompletedOnBefore;
                result.CompletedOnPreserved = true;
                result.LisSeriesCountAfter =
                    result.LisSeriesCountBefore;
                result.LisInstanceCountAfter =
                    result.LisInstanceCountBefore;

                return result;
            }

            var safeToReconcile =
                inspection.CanReconcile
                &&
                (
                    string.Equals(
                        inspection.ReconciliationStatus,
                        PacsReconciliationStatuses.LisMissingData,
                        StringComparison.OrdinalIgnoreCase)
                    ||
                    string.Equals(
                        inspection.ReconciliationStatus,
                        PacsReconciliationStatuses.MetadataCountMismatch,
                        StringComparison.OrdinalIgnoreCase)
                );

            if (!safeToReconcile)
            {
                result.Success = false;
                result.Status =
                    inspection.ReconciliationStatus
                    ?? PacsReconciliationStatuses.ReconcileError;
                result.Message =
                    inspection.Message
                    ?? "Candidate không được phép tự Reconcile.";
                result.ErrorMessage =
                    inspection.ErrorMessage;

                return result;
            }

            /*
             * Revalidate the immutable Worklist identity immediately before
             * entering the canonical Study mutation gate.
             */
            var worklistBeforeSync =
                await _context.PacsWorklistItems
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        x =>
                            x.Id ==
                            pacsWorklistItemId,
                        cancellationToken);

            if (worklistBeforeSync == null
                ||
                worklistBeforeSync.Status != 3
                ||
                worklistBeforeSync.CompletedOn !=
                    result.CompletedOnBefore
                ||
                !string.Equals(
                    NormalizeIdentity(
                        worklistBeforeSync.OrthancStudyId),
                    result.OrthancStudyId,
                    StringComparison.OrdinalIgnoreCase)
                ||
                !IdentityMatchesRequired(
                    NormalizeIdentity(
                        worklistBeforeSync.StudyInstanceUID),
                    result.StudyInstanceUID))
            {
                result.Success = false;
                result.Status =
                    PacsReconciliationStatuses.IdentityMismatch;
                result.Message =
                    "Worklist thay đổi trong lúc chuẩn bị Reconcile. " +
                    "Không được tiếp tục mutation.";

                return result;
            }

            var syncResult =
                await _pacsStudyBL
                    .TrySyncSingleStudyAsync(
                        result.OrthancStudyId!,
                        cancellationToken);

            if (!syncResult.Success)
            {
                if (string.Equals(
                        syncResult.ErrorCode,
                        "STUDY_SYNC_BUSY",
                        StringComparison.OrdinalIgnoreCase))
                {
                    result.Success = false;
                    result.Status =
                        PacsReconciliationStatuses.SyncBusy;
                    result.Message =
                        syncResult.Message
                        ?? "Đang có tiến trình PACS Study sync khác chạy.";

                    return result;
                }

                result.Success = false;
                result.Status =
                    PacsReconciliationStatuses.ReconcileError;
                result.Message =
                    "Canonical Study sync thất bại khi Reconcile.";
                result.ErrorMessage =
                    string.IsNullOrWhiteSpace(
                        syncResult.ErrorCode)
                        ? syncResult.Message
                        : $"{syncResult.ErrorCode}: {syncResult.Message}";

                return result;
            }

            /*
             * Verify by a fresh read after the canonical mutation.
             * A successful Reconciliation must converge to MATCH.
             */
            var worklistAfter =
                await _context.PacsWorklistItems
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        x =>
                            x.Id ==
                            pacsWorklistItemId,
                        cancellationToken);

            if (worklistAfter == null)
            {
                result.Success = false;
                result.Status =
                    PacsReconciliationStatuses.VerifyCountFailed;
                result.Message =
                    "Reconcile đã chạy nhưng không reload được Worklist để verify.";

                return result;
            }

            result.CompletedOnAfter =
                worklistAfter.CompletedOn;

            result.CompletedOnPreserved =
                result.CompletedOnBefore ==
                result.CompletedOnAfter;

            if (!result.CompletedOnPreserved)
            {
                result.Success = false;
                result.Status =
                    PacsReconciliationStatuses.VerifyCompletedOnChanged;
                result.Message =
                    "Reconcile đã chạy nhưng CompletedOn bị thay đổi.";

                return result;
            }

            if (worklistAfter.Status != 3)
            {
                result.Success = false;
                result.Status =
                    PacsReconciliationStatuses.VerifyCountFailed;
                result.Message =
                    $"Reconcile đã chạy nhưng Worklist Status không còn =3. Actual={worklistAfter.Status}.";

                return result;
            }

            var inspectionAfter =
                await InspectCompletedWorklistAsync(
                    worklistAfter,
                    cancellationToken);

            result.PacsStudyId =
                inspectionAfter.PacsStudyId
                ?? result.PacsStudyId;

            result.LisSeriesCountAfter =
                inspectionAfter.LisActualSeriesCount;

            result.LisInstanceCountAfter =
                inspectionAfter.LisActualInstanceCount;

            if (!string.Equals(
                    inspectionAfter.ReconciliationStatus,
                    PacsReconciliationStatuses.Match,
                    StringComparison.OrdinalIgnoreCase))
            {
                result.Success = false;
                result.Status =
                    PacsReconciliationStatuses.VerifyCountFailed;
                result.Message =
                    "Reconcile đã chạy nhưng LIS/Orthanc chưa hội tụ về MATCH.";
                result.ErrorMessage =
                    $"AfterStatus={inspectionAfter.ReconciliationStatus}; " +
                    $"LIS={inspectionAfter.LisActualSeriesCount}/" +
                    $"{inspectionAfter.LisActualInstanceCount}; " +
                    $"Orthanc={inspectionAfter.OrthancSeriesCount}/" +
                    $"{inspectionAfter.OrthancInstanceCount}.";

                return result;
            }

            result.Success = true;
            result.Status =
                PacsReconciliationStatuses.Reconciled;
            result.Message =
                $"Reconcile thành công. " +
                $"Series {result.LisSeriesCountBefore} → {result.LisSeriesCountAfter}, " +
                $"Instances {result.LisInstanceCountBefore} → {result.LisInstanceCountAfter}. " +
                "CompletedOn được giữ nguyên.";

            return result;
        }

        /// <summary>
        /// Targeted read-only inspection used by manual Reconciliation.
        ///
        /// It intentionally mirrors the PREPROD-01C-02 decision matrix but
        /// targets one known Worklist instead of relying on Preview batch order.
        /// No SaveChanges, no canonical Study sync and no checkpoint access.
        /// </summary>
        private async Task<PacsReconciliationPreviewItem>
            InspectCompletedWorklistAsync(
                PacsWorklistItem candidate,
                CancellationToken cancellationToken)
        {
            var item =
                new PacsReconciliationPreviewItem
                {
                    WorklistId =
                        candidate.Id,

                    ResultCDHAId =
                        candidate.ResultCDHAId,

                    AccessionNumber =
                        NormalizeIdentity(
                            candidate.AccessionNumber),

                    CreatedOn =
                        candidate.CreatedOn,

                    CompletedOn =
                        candidate.CompletedOn,

                    LastReconciliationCheckedOn =
                        candidate.LastReconciliationCheckedOn,

                    WorklistOrthancStudyId =
                        NormalizeIdentity(
                            candidate.OrthancStudyId),

                    WorklistStudyInstanceUID =
                        NormalizeIdentity(
                            candidate.StudyInstanceUID)
                };

            try
            {
                var worklistOrthancStudyId =
                    item.WorklistOrthancStudyId;

                if (string.IsNullOrWhiteSpace(
                        worklistOrthancStudyId))
                {
                    item.ReconciliationStatus =
                        PacsReconciliationStatuses.IdentityMismatch;
                    item.CanReconcile = false;
                    item.Message =
                        "Completed Worklist không có OrthancStudyId hợp lệ.";

                    return item;
                }

                var pacsStudy =
                    await _context.PacsStudies
                        .AsNoTracking()
                        .FirstOrDefaultAsync(
                            x =>
                                x.OrthancStudyId ==
                                worklistOrthancStudyId,
                            cancellationToken);

                if (pacsStudy == null &&
                    !string.IsNullOrWhiteSpace(
                        item.WorklistStudyInstanceUID))
                {
                    var worklistStudyInstanceUid =
                        item.WorklistStudyInstanceUID;

                    pacsStudy =
                        await _context.PacsStudies
                            .AsNoTracking()
                            .FirstOrDefaultAsync(
                                x =>
                                    x.StudyInstanceUID ==
                                    worklistStudyInstanceUid,
                                cancellationToken);
                }

                if (pacsStudy == null)
                {
                    item.ReconciliationStatus =
                        PacsReconciliationStatuses.Error;
                    item.CanReconcile = false;
                    item.Message =
                        "Worklist đã Completed nhưng không resolve được PacsStudy trong LIS.";
                    item.ErrorMessage =
                        "LIS_PACS_STUDY_METADATA_MISSING";

                    return item;
                }

                item.PacsStudyId =
                    pacsStudy.Id;

                item.PacsStudyStatus =
                    pacsStudy.Status;

                item.LisRecordedSeriesCount =
                    pacsStudy.SeriesCount;

                item.LisRecordedInstanceCount =
                    pacsStudy.InstanceCount;

                if (pacsStudy.ResultCDHAId !=
                        candidate.ResultCDHAId
                    ||
                    !string.Equals(
                        NormalizeIdentity(
                            pacsStudy.OrthancStudyId),
                        worklistOrthancStudyId,
                        StringComparison.OrdinalIgnoreCase)
                    ||
                    !IdentityMatchesRequired(
                        item.WorklistStudyInstanceUID,
                        NormalizeIdentity(
                            pacsStudy.StudyInstanceUID))
                    ||
                    !IdentityMatchesRequired(
                        item.AccessionNumber,
                        NormalizeIdentity(
                            pacsStudy.AccessionNumber)))
                {
                    item.ReconciliationStatus =
                        PacsReconciliationStatuses.IdentityMismatch;
                    item.CanReconcile = false;
                    item.Message =
                        "Identity giữa Completed Worklist và PacsStudy trong LIS không khớp.";

                    return item;
                }

                item.LisActualSeriesCount =
                    await _context.PacsSeries
                        .AsNoTracking()
                        .CountAsync(
                            x =>
                                x.PacsStudyId ==
                                pacsStudy.Id,
                            cancellationToken);

                item.LisActualInstanceCount =
                    await _context.PacsInstances
                        .AsNoTracking()
                        .CountAsync(
                            x =>
                                x.PacsStudyId ==
                                pacsStudy.Id,
                            cancellationToken);

                string studyJson;

                try
                {
                    studyJson =
                        await _orthancClient
                            .GetStudyJsonAsync(
                                worklistOrthancStudyId,
                                cancellationToken);
                }
                catch (HttpRequestException ex)
                    when (
                        ex.StatusCode ==
                        HttpStatusCode.NotFound)
                {
                    item.ReconciliationStatus =
                        PacsReconciliationStatuses.PacsStudyNotFound;
                    item.CanReconcile = false;
                    item.Message =
                        "Completed Worklist tham chiếu Orthanc Study không còn tồn tại.";
                    item.ErrorMessage =
                        ex.GetBaseException().Message;

                    return item;
                }

                using var studyDocument =
                    JsonDocument.Parse(
                        studyJson);

                var studyRoot =
                    studyDocument.RootElement;

                var mainTags =
                    GetObject(
                        studyRoot,
                        "MainDicomTags");

                item.OrthancAccessionNumber =
                    NormalizeIdentity(
                        GetString(
                            mainTags,
                            "AccessionNumber"));

                item.OrthancStudyInstanceUID =
                    NormalizeIdentity(
                        GetString(
                            mainTags,
                            "StudyInstanceUID"));

                item.IsStable =
                    GetNullableBoolean(
                        studyRoot,
                        "IsStable");

                if (item.IsStable != true)
                {
                    item.ReconciliationStatus =
                        PacsReconciliationStatuses.NotStable;
                    item.CanReconcile = false;
                    item.Message =
                        "Study Orthanc hiện chưa Stable. Chưa được Reconcile.";

                    return item;
                }

                if (!IdentityMatchesRequired(
                        item.AccessionNumber,
                        item.OrthancAccessionNumber)
                    ||
                    !IdentityMatchesRequired(
                        item.WorklistStudyInstanceUID,
                        item.OrthancStudyInstanceUID)
                    ||
                    !IdentityMatchesRequired(
                        NormalizeIdentity(
                            pacsStudy.StudyInstanceUID),
                        item.OrthancStudyInstanceUID))
                {
                    item.ReconciliationStatus =
                        PacsReconciliationStatuses.IdentityMismatch;
                    item.CanReconcile = false;
                    item.Message =
                        "Identity giữa Worklist/PacsStudy và Orthanc không khớp.";

                    return item;
                }

                var orthancSeriesIds =
                    GetStringArray(
                        studyRoot,
                        "Series");

                item.OrthancSeriesCount =
                    orthancSeriesIds.Count;

                item.OrthancInstanceCount =
                    await ReadOrthancInstanceCountAsync(
                        worklistOrthancStudyId,
                        item.OrthancSeriesCount,
                        cancellationToken);

                item.SeriesDelta =
                    item.OrthancSeriesCount -
                    item.LisActualSeriesCount;

                item.InstanceDelta =
                    item.OrthancInstanceCount -
                    item.LisActualInstanceCount;

                var actualSeriesMatch =
                    item.LisActualSeriesCount ==
                    item.OrthancSeriesCount;

                var actualInstanceMatch =
                    item.LisActualInstanceCount ==
                    item.OrthancInstanceCount;

                var recordedCountsMatchActual =
                    item.LisRecordedSeriesCount ==
                        item.LisActualSeriesCount
                    &&
                    item.LisRecordedInstanceCount ==
                        item.LisActualInstanceCount;

                if (actualSeriesMatch &&
                    actualInstanceMatch &&
                    recordedCountsMatchActual)
                {
                    item.ReconciliationStatus =
                        PacsReconciliationStatuses.Match;
                    item.CanReconcile = false;
                    item.Message =
                        "LIS và Orthanc đang khớp.";

                    return item;
                }

                if (actualSeriesMatch &&
                    actualInstanceMatch)
                {
                    item.ReconciliationStatus =
                        PacsReconciliationStatuses.MetadataCountMismatch;
                    item.CanReconcile = true;
                    item.Message =
                        "Child Series/Instances trong LIS đã đủ nhưng " +
                        "SeriesCount/InstanceCount trên PacsStudy đang stale.";

                    return item;
                }

                if (item.SeriesDelta >= 0 &&
                    item.InstanceDelta >= 0 &&
                    (item.SeriesDelta > 0 ||
                     item.InstanceDelta > 0))
                {
                    item.ReconciliationStatus =
                        PacsReconciliationStatuses.LisMissingData;
                    item.CanReconcile = true;
                    item.Message =
                        $"LIS thiếu dữ liệu so với Orthanc. " +
                        $"SeriesDelta={item.SeriesDelta}, " +
                        $"InstanceDelta={item.InstanceDelta}.";

                    return item;
                }

                if (item.SeriesDelta <= 0 &&
                    item.InstanceDelta <= 0 &&
                    (item.SeriesDelta < 0 ||
                     item.InstanceDelta < 0))
                {
                    item.ReconciliationStatus =
                        PacsReconciliationStatuses.LisHasExtraData;
                    item.CanReconcile = false;
                    item.Message =
                        $"LIS đang có nhiều dữ liệu hơn Orthanc. " +
                        $"SeriesDelta={item.SeriesDelta}, " +
                        $"InstanceDelta={item.InstanceDelta}. " +
                        "Không được tự Reconcile.";

                    return item;
                }

                item.ReconciliationStatus =
                    PacsReconciliationStatuses.CountMismatchComplex;
                item.CanReconcile = false;
                item.Message =
                    $"Count mismatch không đơn giản. " +
                    $"SeriesDelta={item.SeriesDelta}, " +
                    $"InstanceDelta={item.InstanceDelta}. " +
                    "Cần kiểm tra thủ công.";

                return item;
            }
            catch (OperationCanceledException)
                when (
                    cancellationToken
                        .IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                item.ReconciliationStatus =
                    PacsReconciliationStatuses.Error;
                item.CanReconcile = false;
                item.Message =
                    "Lỗi khi inspect Reconciliation.";
                item.ErrorMessage =
                    ex.GetBaseException().Message;

                return item;
            }
        }

        private static void CopyInspectionToManualBefore(
            PacsReconciliationManualResult result,
            PacsReconciliationPreviewItem inspection)
        {
            result.PacsStudyId =
                inspection.PacsStudyId;

            result.OrthancStudyId =
                inspection.WorklistOrthancStudyId;

            result.StudyInstanceUID =
                inspection.WorklistStudyInstanceUID;

            result.IsStable =
                inspection.IsStable;

            result.LisRecordedSeriesCountBefore =
                inspection.LisRecordedSeriesCount;

            result.LisRecordedInstanceCountBefore =
                inspection.LisRecordedInstanceCount;

            result.LisSeriesCountBefore =
                inspection.LisActualSeriesCount;

            result.LisInstanceCountBefore =
                inspection.LisActualInstanceCount;

            result.OrthancSeriesCount =
                inspection.OrthancSeriesCount;

            result.OrthancInstanceCount =
                inspection.OrthancInstanceCount;
        }

        /// <summary>
        /// PACS-XQ-PREPROD-01C-05
        ///
        /// Manual batch Reconciliation for completed Worklists.
        ///
        /// Candidate ordering implements anti-starvation:
        /// 1. never checked first;
        /// 2. oldest LastReconciliationCheckedOn;
        /// 3. oldest CompletedOn;
        /// 4. smallest Worklist Id.
        ///
        /// Each item is freshly inspected. Only:
        /// - LIS_MISSING_DATA
        /// - METADATA_COUNT_MISMATCH
        /// may enter ReconcileCompletedStudyAsync(), which performs its own
        /// second inspection and identity revalidation immediately before the
        /// canonical PacsStudyBL mutation gate.
        ///
        /// Reconciliation never owns PacsSyncState checkpoint.
        /// </summary>
        public async Task<PacsReconciliationBatchResult>
            ReconcilePendingAsync(
                int? requestedBatchSize = null,
                CancellationToken cancellationToken = default)
        {
            var result =
                new PacsReconciliationBatchResult();

            var batchLockAcquired =
                false;

            try
            {
                batchLockAcquired =
                    await _reconciliationBatchLock
                        .WaitAsync(
                            0,
                            cancellationToken);

                if (!batchLockAcquired)
                {
                    result.Success = false;
                    result.Status =
                        PacsReconciliationStatuses.BatchBusy;
                    result.Message =
                        "Đang có một Reconciliation Batch khác chạy.";

                    return result;
                }

                var options =
                    _reconciliationOptions.CurrentValue
                    ?? new PacsSyncReconciliationOptions();

                if (!TryParseReconciliationStartDate(
                        options.StartDate,
                        out var reconciliationFrom))
                {
                    result.Success = false;
                    result.Status =
                        PacsReconciliationStatuses.ConfigInvalid;
                    result.Message =
                        "PacsSyncReconciliation:StartDate bắt buộc có định dạng yyyy-MM-dd. " +
                        "Reconciliation fail-closed.";

                    return result;
                }

                var minAgeMinutes =
                    NormalizeReconciliationMinAgeMinutes(
                        options.MinAgeMinutes);

                var batchSize =
                    NormalizeReconciliationBatchSize(
                        requestedBatchSize,
                        options.BatchSize);

                var reconciliationTo =
                    DateTime.Now.AddMinutes(
                        -minAgeMinutes);

                result.ReconciliationFrom =
                    reconciliationFrom;

                result.ReconciliationTo =
                    reconciliationTo;

                result.MinAgeMinutes =
                    minAgeMinutes;

                result.BatchSize =
                    batchSize;

                var config =
                    await _pacsConfigBL.GetDefaultAsync();

                if (config == null ||
                    !config.IsActive ||
                    !config.IsDefault)
                {
                    result.Success = false;
                    result.Status =
                        PacsReconciliationStatuses.PacsConfigNotFound;
                    result.Message =
                        "Chưa có PACS Server mặc định đang hoạt động.";

                    return result;
                }

                _orthancClient.Configure(config);

                /*
                 * Load IDs only. Every item is reloaded and freshly inspected
                 * immediately before any action.
                 *
                 * Preview and manual Reconciliation use the same candidate
                 * contract. Batch adds only scheduling state.
                 */
                var candidateIds =
                    await _context.PacsWorklistItems
                        .AsNoTracking()
                        .Where(x =>
                            x.Status == 3 &&
                            x.CreatedOn >= reconciliationFrom &&
                            x.CompletedOn.HasValue &&
                            x.CompletedOn.Value <= reconciliationTo &&
                            x.OrthancStudyId != null &&
                            x.OrthancStudyId != "")
                        .OrderBy(x =>
                            x.LastReconciliationCheckedOn.HasValue
                                ? 1
                                : 0)
                        .ThenBy(x =>
                            x.LastReconciliationCheckedOn)
                        .ThenBy(x =>
                            x.CompletedOn)
                        .ThenBy(x =>
                            x.Id)
                        .Take(batchSize)
                        .Select(x =>
                            x.Id)
                        .ToListAsync(
                            cancellationToken);

                result.CandidateCount =
                    candidateIds.Count;

                foreach (var worklistId in
                         candidateIds)
                {
                    cancellationToken
                        .ThrowIfCancellationRequested();

                    var batchItem =
                        new PacsReconciliationBatchItem
                        {
                            WorklistId =
                                worklistId
                        };

                    var markChecked =
                        true;

                    try
                    {
                        var worklist =
                            await _context.PacsWorklistItems
                                .AsNoTracking()
                                .FirstOrDefaultAsync(
                                    x =>
                                        x.Id ==
                                        worklistId,
                                    cancellationToken);

                        if (worklist == null)
                        {
                            batchItem.Action =
                                "SKIP";
                            batchItem.ActionStatus =
                                PacsReconciliationStatuses.WorklistNotFound;

                            result.SkippedCount++;

                            await MarkReconciliationCheckedAsync(
                                worklistId,
                                cancellationToken);

                            result.Items.Add(
                                batchItem);

                            continue;
                        }

                        batchItem.AccessionNumber =
                            NormalizeIdentity(
                                worklist.AccessionNumber);

                        /*
                         * Revalidate candidate window. Another process may have
                         * changed the Worklist after the initial ID scan.
                         */
                        if (worklist.Status != 3)
                        {
                            batchItem.Action =
                                "SKIP";
                            batchItem.ActionStatus =
                                PacsReconciliationStatuses.InvalidWorklistStatus;

                            result.SkippedCount++;

                            result.Items.Add(
                                batchItem);

                            continue;
                        }

                        if (worklist.CreatedOn <
                                reconciliationFrom
                            ||
                            !worklist.CompletedOn.HasValue
                            ||
                            worklist.CompletedOn.Value >
                                reconciliationTo)
                        {
                            batchItem.Action =
                                "SKIP";
                            batchItem.ActionStatus =
                                PacsReconciliationStatuses.OutsideReconciliationWindow;

                            result.SkippedCount++;

                            await MarkReconciliationCheckedAsync(
                                worklistId,
                                cancellationToken);

                            result.Items.Add(
                                batchItem);

                            continue;
                        }

                        var inspection =
                            await InspectCompletedWorklistAsync(
                                worklist,
                                cancellationToken);

                        batchItem.PreviewStatus =
                            inspection.ReconciliationStatus;

                        batchItem.CanReconcile =
                            inspection.CanReconcile;

                        switch (inspection.ReconciliationStatus)
                        {
                            case PacsReconciliationStatuses.Match:
                                batchItem.Action =
                                    "NONE";
                                batchItem.ActionStatus =
                                    PacsReconciliationStatuses.Match;

                                result.MatchCount++;
                                break;

                            case PacsReconciliationStatuses.LisMissingData:
                            case PacsReconciliationStatuses.MetadataCountMismatch:
                                if (!inspection.CanReconcile)
                                {
                                    batchItem.Action =
                                        "ERROR";
                                    batchItem.ActionStatus =
                                        PacsReconciliationStatuses.Error;

                                    result.ErrorCount++;
                                    break;
                                }

                                batchItem.Action =
                                    "RECONCILE";

                                /*
                                 * Do NOT call PacsStudyBL directly here.
                                 * Manual Reconciliation is the canonical
                                 * reliability mutation gate and performs:
                                 * - a second fresh inspection;
                                 * - immutable identity revalidation;
                                 * - CompletedOn preservation verification;
                                 * - post-sync MATCH verification.
                                 *
                                 * The batch has already configured this
                                 * PacsReliabilityBL OrthancClient and has
                                 * already sent Orthanc requests. Reuse that
                                 * configuration; do not reassign HttpClient
                                 * BaseAddress/Timeout after requests started.
                                 */
                                var reconcileResult =
                                    await ReconcileCompletedStudyAsync(
                                        worklistId,
                                        cancellationToken,
                                        orthancAlreadyConfigured: true);

                                batchItem.ReconcileResult =
                                    reconcileResult;

                                batchItem.ActionStatus =
                                    reconcileResult.Status;

                                if (reconcileResult.Success &&
                                    string.Equals(
                                        reconcileResult.Status,
                                        PacsReconciliationStatuses.Reconciled,
                                        StringComparison.OrdinalIgnoreCase))
                                {
                                    result.ReconciledCount++;
                                    break;
                                }

                                if (reconcileResult.Success &&
                                    string.Equals(
                                        reconcileResult.Status,
                                        PacsReconciliationStatuses.AlreadyMatched,
                                        StringComparison.OrdinalIgnoreCase))
                                {
                                    /*
                                     * Valid race: another canonical sync may
                                     * have repaired the Study between batch
                                     * inspection and manual mutation.
                                     */
                                    result.AlreadyMatchedCount++;
                                    break;
                                }

                                switch (reconcileResult.Status)
                                {
                                    case PacsReconciliationStatuses.SyncBusy:
                                        result.SyncBusyCount++;
                                        markChecked = false;
                                        break;

                                    case PacsReconciliationStatuses.NotStable:
                                        result.NotStableCount++;
                                        break;

                                    case PacsReconciliationStatuses.LisHasExtraData:
                                        result.ExtraDataCount++;
                                        break;

                                    case PacsReconciliationStatuses.CountMismatchComplex:
                                        result.ComplexMismatchCount++;
                                        break;

                                    case PacsReconciliationStatuses.IdentityMismatch:
                                        result.IdentityMismatchCount++;
                                        break;

                                    case PacsReconciliationStatuses.PacsStudyNotFound:
                                        result.PacsStudyNotFoundCount++;
                                        break;

                                    case PacsReconciliationStatuses.InvalidWorklistStatus:
                                    case PacsReconciliationStatuses.OutsideReconciliationWindow:
                                    case PacsReconciliationStatuses.WorklistNotFound:
                                    case PacsReconciliationStatuses.MissingOrthancStudyId:
                                        result.SkippedCount++;
                                        break;

                                    default:
                                        result.ErrorCount++;
                                        break;
                                }

                                break;

                            case PacsReconciliationStatuses.NotStable:
                                batchItem.Action =
                                    "WARNING";
                                batchItem.ActionStatus =
                                    PacsReconciliationStatuses.NotStable;

                                result.NotStableCount++;
                                break;

                            case PacsReconciliationStatuses.LisHasExtraData:
                                batchItem.Action =
                                    "WARNING";
                                batchItem.ActionStatus =
                                    PacsReconciliationStatuses.LisHasExtraData;

                                result.ExtraDataCount++;
                                break;

                            case PacsReconciliationStatuses.CountMismatchComplex:
                                batchItem.Action =
                                    "WARNING";
                                batchItem.ActionStatus =
                                    PacsReconciliationStatuses.CountMismatchComplex;

                                result.ComplexMismatchCount++;
                                break;

                            case PacsReconciliationStatuses.IdentityMismatch:
                                batchItem.Action =
                                    "WARNING";
                                batchItem.ActionStatus =
                                    PacsReconciliationStatuses.IdentityMismatch;

                                result.IdentityMismatchCount++;
                                break;

                            case PacsReconciliationStatuses.PacsStudyNotFound:
                                batchItem.Action =
                                    "WARNING";
                                batchItem.ActionStatus =
                                    PacsReconciliationStatuses.PacsStudyNotFound;

                                result.PacsStudyNotFoundCount++;
                                break;

                            case PacsReconciliationStatuses.Error:
                                batchItem.Action =
                                    "ERROR";
                                batchItem.ActionStatus =
                                    PacsReconciliationStatuses.Error;

                                result.ErrorCount++;
                                break;

                            default:
                                batchItem.Action =
                                    "SKIP";
                                batchItem.ActionStatus =
                                    inspection.ReconciliationStatus
                                    ?? PacsReconciliationStatuses.Error;

                                result.SkippedCount++;
                                break;
                        }
                    }
                    catch (OperationCanceledException)
                        when (
                            cancellationToken
                                .IsCancellationRequested)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        batchItem.Action =
                            "ERROR";
                        batchItem.ActionStatus =
                            PacsReconciliationStatuses.Error;

                        batchItem.ReconcileResult =
                            new PacsReconciliationManualResult
                            {
                                Success = false,
                                Status =
                                    PacsReconciliationStatuses.ReconcileError,
                                WorklistId =
                                    worklistId,
                                Message =
                                    "Reconciliation Batch item phát sinh lỗi.",
                                ErrorMessage =
                                    ex.GetBaseException().Message
                            };

                        result.ErrorCount++;
                    }

                    /*
                     * Unlike Recovery, a successful Reconciliation keeps
                     * Worklist Status=3. Therefore successful MATCH/Reconcile
                     * items MUST also receive LastReconciliationCheckedOn so
                     * they rotate to the back of the anti-starvation queue.
                     *
                     * SYNC_BUSY is the only normal outcome that is not marked:
                     * the item has not obtained the canonical mutation gate and
                     * should be retried promptly.
                     */
                    if (markChecked)
                    {
                        try
                        {
                            await MarkReconciliationCheckedAsync(
                                worklistId,
                                cancellationToken);
                        }
                        catch (OperationCanceledException)
                            when (
                                cancellationToken
                                    .IsCancellationRequested)
                        {
                            throw;
                        }
                        catch (Exception ex)
                        {
                            batchItem.Action =
                                "ERROR";
                            batchItem.ActionStatus =
                                "RECONCILIATION_SCHEDULER_UPDATE_ERROR";

                            if (batchItem.ReconcileResult == null)
                            {
                                batchItem.ReconcileResult =
                                    new PacsReconciliationManualResult
                                    {
                                        Success = false,
                                        Status =
                                            "RECONCILIATION_SCHEDULER_UPDATE_ERROR",
                                        WorklistId =
                                            worklistId,
                                        AccessionNumber =
                                            batchItem.AccessionNumber,
                                        Message =
                                            "Không cập nhật được LastReconciliationCheckedOn.",
                                        ErrorMessage =
                                            ex.GetBaseException().Message
                                    };
                            }
                            else
                            {
                                batchItem.ReconcileResult.ErrorMessage =
                                    string.IsNullOrWhiteSpace(
                                        batchItem.ReconcileResult.ErrorMessage)
                                        ? ex.GetBaseException().Message
                                        : batchItem.ReconcileResult.ErrorMessage +
                                          " | Scheduler: " +
                                          ex.GetBaseException().Message;
                            }

                            result.ErrorCount++;
                        }
                    }

                    result.Items.Add(
                        batchItem);
                }

                result.Success =
                    result.ErrorCount == 0;

                result.Status =
                    result.CandidateCount == 0
                        ? PacsReconciliationStatuses.NoCandidates
                        : result.Success
                            ? PacsReconciliationStatuses.Completed
                            : PacsReconciliationStatuses.CompletedWithErrors;

                result.Message =
                    $"Reconciliation Batch hoàn tất. " +
                    $"Candidates={result.CandidateCount}, " +
                    $"Match={result.MatchCount}, " +
                    $"Reconciled={result.ReconciledCount}, " +
                    $"AlreadyMatched={result.AlreadyMatchedCount}, " +
                    $"NotStable={result.NotStableCount}, " +
                    $"ExtraData={result.ExtraDataCount}, " +
                    $"ComplexMismatch={result.ComplexMismatchCount}, " +
                    $"IdentityMismatch={result.IdentityMismatchCount}, " +
                    $"PacsStudyNotFound={result.PacsStudyNotFoundCount}, " +
                    $"SyncBusy={result.SyncBusyCount}, " +
                    $"Skipped={result.SkippedCount}, " +
                    $"Errors={result.ErrorCount}.";

                return result;
            }
            catch (OperationCanceledException)
                when (
                    cancellationToken
                        .IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Status =
                    PacsReconciliationStatuses.BatchError;
                result.Message =
                    "Reconciliation Batch thất bại.";
                result.ErrorMessage =
                    ex.GetBaseException().Message;

                return result;
            }
            finally
            {
                if (batchLockAcquired)
                {
                    _reconciliationBatchLock.Release();
                }
            }
        }

        /// <summary>
        /// Persist Reconciliation anti-starvation scheduling state.
        ///
        /// Completed Worklists remain Status=3 after successful Reconciliation,
        /// so LastReconciliationCheckedOn is required for both MATCH and
        /// successfully Reconciled items. This method never touches
        /// PacsSyncStates.
        /// </summary>
        private async Task MarkReconciliationCheckedAsync(
            long pacsWorklistItemId,
            CancellationToken cancellationToken)
        {
            var worklist =
                await _context.PacsWorklistItems
                    .FirstOrDefaultAsync(
                        x =>
                            x.Id ==
                            pacsWorklistItemId,
                        cancellationToken);

            if (worklist == null ||
                worklist.Status != 3)
            {
                return;
            }

            worklist.LastReconciliationCheckedOn =
                DateTime.Now;

            await _context.SaveChangesAsync(
                cancellationToken);
        }

        /// <summary>
        /// Preview pending Worklists that may need Missing Study Recovery.
        ///
        /// IMPORTANT:
        /// - Project A Worklist lifecycle is 0=draft, 1=published,
        ///   3=completed, 9=publish failed. Therefore Recovery candidates
        ///   are Status=1 only. Project B's Status=2 rule is not copied.
        /// - StartDate must be configured as yyyy-MM-dd. Invalid/empty config
        ///   fails closed before any candidate scan.
        /// - This method performs no DB mutation.
        /// </summary>
        public async Task<PacsRecoveryPreviewResult>
            PreviewRecoveryAsync(
                int? requestedBatchSize = null,
                CancellationToken cancellationToken = default)
        {
            var result =
                new PacsRecoveryPreviewResult();

            var options =
                _recoveryOptions.CurrentValue
                ?? new PacsSyncRecoveryOptions();

            if (!TryParseRecoveryStartDate(
                    options.StartDate,
                    out var recoveryFrom))
            {
                result.Success = false;
                result.Status =
                    "RECOVERY_CONFIG_INVALID";
                result.Message =
                    "PacsSyncRecovery:StartDate bắt buộc có định dạng yyyy-MM-dd. " +
                    "Recovery fail-closed để không quét nhầm dữ liệu lịch sử.";

                return result;
            }

            var minAgeMinutes =
                NormalizeMinAgeMinutes(
                    options.MinAgeMinutes);

            var batchSize =
                NormalizeBatchSize(
                    requestedBatchSize,
                    options.BatchSize);

            var recoveryTo =
                DateTime.Now.AddMinutes(
                    -minAgeMinutes);

            result.RecoveryFrom =
                recoveryFrom;

            result.RecoveryTo =
                recoveryTo;

            result.MinAgeMinutes =
                minAgeMinutes;

            result.BatchSize =
                batchSize;

            var config =
                await _pacsConfigBL.GetDefaultAsync();

            if (config == null ||
                !config.IsActive ||
                !config.IsDefault)
            {
                result.Success = false;
                result.Status =
                    "PACS_CONFIG_NOT_FOUND";
                result.Message =
                    "Chưa có PACS Server mặc định đang hoạt động.";

                return result;
            }

            _orthancClient.Configure(config);

            /*
             * READ ONLY candidate scan.
             *
             * Anti-starvation order is deliberately identical to the future
             * automatic Recovery batch:
             * 1. never checked first;
             * 2. oldest LastRecoveryCheckedOn;
             * 3. oldest CreatedOn;
             * 4. smallest Id for deterministic ordering.
             */
            var candidates =
                await _context.PacsWorklistItems
                    .AsNoTracking()
                    .Where(x =>
                        x.Status == 1 &&
                        x.CreatedOn >= recoveryFrom &&
                        x.CreatedOn <= recoveryTo &&
                        x.AccessionNumber != null &&
                        x.AccessionNumber != "")
                    .OrderBy(x =>
                        x.LastRecoveryCheckedOn.HasValue
                            ? 1
                            : 0)
                    .ThenBy(x =>
                        x.LastRecoveryCheckedOn)
                    .ThenBy(x =>
                        x.CreatedOn)
                    .ThenBy(x =>
                        x.Id)
                    .Take(batchSize)
                    .Select(x => new
                    {
                        x.Id,
                        x.ResultCDHAId,
                        x.PatientId,
                        x.ServiceId,
                        x.AccessionNumber,
                        x.Modality,
                        x.Status,
                        x.CreatedOn,
                        x.PublishedOn,
                        x.LastRecoveryCheckedOn,
                        x.OrthancStudyId,
                        x.StudyInstanceUID
                    })
                    .ToListAsync(
                        cancellationToken);

            result.CandidateCount =
                candidates.Count;

            foreach (var candidate in candidates)
            {
                cancellationToken
                    .ThrowIfCancellationRequested();

                var accessionNumber =
                    candidate.AccessionNumber
                        ?.Trim();

                var item =
                    new PacsRecoveryPreviewItem
                    {
                        WorklistId =
                            candidate.Id,
                        ResultCDHAId =
                            candidate.ResultCDHAId,
                        PatientId =
                            candidate.PatientId,
                        ServiceId =
                            candidate.ServiceId,
                        AccessionNumber =
                            accessionNumber,
                        Modality =
                            candidate.Modality,
                        WorklistStatus =
                            candidate.Status,
                        CreatedOn =
                            candidate.CreatedOn,
                        PublishedOn =
                            candidate.PublishedOn,
                        LastRecoveryCheckedOn =
                            candidate.LastRecoveryCheckedOn,
                        CurrentOrthancStudyId =
                            candidate.OrthancStudyId,
                        CurrentStudyInstanceUID =
                            candidate.StudyInstanceUID
                    };

                try
                {
                    if (string.IsNullOrWhiteSpace(
                            accessionNumber))
                    {
                        item.RecoveryStatus =
                            "MISSING_ACCESSION";
                        item.CanRecover = false;
                        item.Message =
                            "Worklist không có AccessionNumber hợp lệ.";

                        result.IdentityMismatchCount++;
                        result.Items.Add(item);
                        continue;
                    }

                    /*
                     * If a PacsStudy is already mapped by this Accession, this is
                     * no longer a Missing Study problem. Do not auto-repair a
                     * Worklist status in Preview/Recovery by guessing.
                     */
                    var existingStudyByAccession =
                        await _context.PacsStudies
                            .AsNoTracking()
                            .Where(x =>
                                x.AccessionNumber != null &&
                                x.AccessionNumber.Trim() ==
                                    accessionNumber)
                            .OrderByDescending(x => x.Id)
                            .Select(x => new
                            {
                                x.Id,
                                x.ResultCDHAId,
                                x.AccessionNumber,
                                x.OrthancStudyId,
                                x.StudyInstanceUID,
                                x.SeriesCount,
                                x.InstanceCount,
                                x.Status
                            })
                            .FirstOrDefaultAsync(
                                cancellationToken);

                    if (existingStudyByAccession != null)
                    {
                        ApplyExistingStudy(
                            item,
                            existingStudyByAccession.Id,
                            existingStudyByAccession.SeriesCount,
                            existingStudyByAccession.InstanceCount,
                            existingStudyByAccession.Status);

                        if (existingStudyByAccession.ResultCDHAId ==
                            candidate.ResultCDHAId)
                        {
                            item.RecoveryStatus =
                                "PACS_STUDY_EXISTS";
                            item.CanRecover = false;
                            item.Message =
                                "PacsStudy đã tồn tại cho đúng ResultCDHA. " +
                                "Đây không còn là Missing Study Recovery.";

                            result.AlreadyHandledCount++;
                        }
                        else
                        {
                            item.RecoveryStatus =
                                "IDENTITY_MISMATCH";
                            item.CanRecover = false;
                            item.Message =
                                "AccessionNumber đã thuộc PacsStudy của ResultCDHA khác. " +
                                "Không được tự Recovery.";

                            result.IdentityMismatchCount++;
                        }

                        result.Items.Add(item);
                        continue;
                    }

                    var studyIds =
                        await _orthancClient
                            .FindStudyIdsByAccessionNumberAsync(
                                accessionNumber,
                                cancellationToken)
                        ?? new List<string>();

                    item.OrthancStudyIds =
                        studyIds;

                    item.OrthancStudyCount =
                        studyIds.Count;

                    if (studyIds.Count == 0)
                    {
                        item.RecoveryStatus =
                            "ORTHANC_NOT_FOUND";
                        item.CanRecover = false;
                        item.Message =
                            "Chưa tìm thấy Study trong Orthanc.";

                        result.NotFoundCount++;
                        result.Items.Add(item);
                        continue;
                    }

                    if (studyIds.Count > 1)
                    {
                        item.IsAmbiguous = true;
                        item.RecoveryStatus =
                            "AMBIGUOUS";
                        item.CanRecover = false;
                        item.Message =
                            $"Có {studyIds.Count} Study Orthanc cùng AccessionNumber. " +
                            "Không được chọn tùy ý một Study để Recovery.";

                        result.AmbiguousCount++;
                        result.Items.Add(item);
                        continue;
                    }

                    var orthancStudyId =
                        studyIds[0];

                    item.OrthancStudyId =
                        orthancStudyId;

                    var studyJson =
                        await _orthancClient
                            .GetStudyJsonAsync(
                                orthancStudyId,
                                cancellationToken);

                    using var studyDocument =
                        JsonDocument.Parse(
                            studyJson);

                    var studyRoot =
                        studyDocument.RootElement;

                    var mainTags =
                        GetObject(
                            studyRoot,
                            "MainDicomTags");

                    var orthancAccessionNumber =
                        NormalizeIdentity(
                            GetString(
                                mainTags,
                                "AccessionNumber"));

                    var orthancStudyInstanceUid =
                        NormalizeIdentity(
                            GetString(
                                mainTags,
                                "StudyInstanceUID"));

                    item.OrthancAccessionNumber =
                        orthancAccessionNumber;

                    item.OrthancStudyInstanceUID =
                        orthancStudyInstanceUid;

                    var seriesIds =
                        GetStringArray(
                            studyRoot,
                            "Series");

                    item.OrthancSeriesCount =
                        seriesIds.Count;

                    item.IsStable =
                        GetNullableBoolean(
                            studyRoot,
                            "IsStable");

                    /*
                     * /tools/find already queried by AccessionNumber, but never
                     * trust only the search result. Re-read Study identity and
                     * fail closed on any mismatch.
                     */
                    if (!string.Equals(
                            accessionNumber,
                            orthancAccessionNumber,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        item.RecoveryStatus =
                            "IDENTITY_MISMATCH";
                        item.CanRecover = false;
                        item.Message =
                            $"AccessionNumber Worklist={accessionNumber}, " +
                            $"Orthanc={orthancAccessionNumber ?? "<empty>"}.";

                        result.IdentityMismatchCount++;
                        result.Items.Add(item);
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(
                            orthancStudyInstanceUid))
                    {
                        item.RecoveryStatus =
                            "IDENTITY_MISMATCH";
                        item.CanRecover = false;
                        item.Message =
                            "Orthanc Study không có StudyInstanceUID hợp lệ.";

                        result.IdentityMismatchCount++;
                        result.Items.Add(item);
                        continue;
                    }

                    if (item.IsStable != true)
                    {
                        item.RecoveryStatus =
                            "NOT_STABLE";
                        item.CanRecover = false;
                        item.Message =
                            item.IsStable == false
                                ? "Study đã có trong Orthanc nhưng chưa Stable."
                                : "Orthanc không trả trạng thái IsStable hợp lệ.";

                        result.NotStableCount++;
                        result.Items.Add(item);
                        continue;
                    }

                    /*
                     * Defensive identity lookup: a Study may already exist with
                     * a malformed/missing Accession but the same Orthanc ID/UID.
                     */
                    var existingStudyByIdentity =
                        await _context.PacsStudies
                            .AsNoTracking()
                            .Where(x =>
                                x.OrthancStudyId ==
                                    orthancStudyId ||
                                x.StudyInstanceUID ==
                                    orthancStudyInstanceUid)
                            .OrderByDescending(x => x.Id)
                            .Select(x => new
                            {
                                x.Id,
                                x.ResultCDHAId,
                                x.AccessionNumber,
                                x.SeriesCount,
                                x.InstanceCount,
                                x.Status
                            })
                            .FirstOrDefaultAsync(
                                cancellationToken);

                    if (existingStudyByIdentity != null)
                    {
                        ApplyExistingStudy(
                            item,
                            existingStudyByIdentity.Id,
                            existingStudyByIdentity.SeriesCount,
                            existingStudyByIdentity.InstanceCount,
                            existingStudyByIdentity.Status);

                        var sameResult =
                            existingStudyByIdentity.ResultCDHAId ==
                            candidate.ResultCDHAId;

                        var sameAccession =
                            string.Equals(
                                NormalizeIdentity(
                                    existingStudyByIdentity.AccessionNumber),
                                accessionNumber,
                                StringComparison.OrdinalIgnoreCase);

                        if (sameResult && sameAccession)
                        {
                            item.RecoveryStatus =
                                "PACS_STUDY_EXISTS";
                            item.CanRecover = false;
                            item.Message =
                                "Study đã tồn tại trong PacsStudies theo Orthanc identity. " +
                                "Không chạy Missing Study Recovery.";

                            result.AlreadyHandledCount++;
                        }
                        else
                        {
                            item.RecoveryStatus =
                                "IDENTITY_MISMATCH";
                            item.CanRecover = false;
                            item.Message =
                                "OrthancStudyId/StudyInstanceUID đã thuộc PacsStudy có identity khác. " +
                                "Không được tự Recovery.";

                            result.IdentityMismatchCount++;
                        }

                        result.Items.Add(item);
                        continue;
                    }

                    /*
                     * Counts are informational for Preview. Fetch them only after
                     * identity + stability validation has passed.
                     */
                    item.OrthancInstanceCount =
                        await ReadOrthancInstanceCountAsync(
                            orthancStudyId,
                            seriesIds.Count,
                            cancellationToken);

                    item.RecoveryStatus =
                        "RECOVERABLE";
                    item.CanRecover = true;
                    item.Message =
                        "Study đã Stable trong Orthanc, identity hợp lệ và chưa có PacsStudy trong LIS.";

                    result.RecoverableCount++;
                }
                catch (OperationCanceledException)
                    when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    item.RecoveryStatus =
                        "ERROR";
                    item.CanRecover = false;
                    item.ErrorMessage =
                        ex.GetBaseException().Message;
                    item.Message =
                        "Lỗi khi Preview Recovery.";

                    result.ErrorCount++;
                }

                result.Items.Add(item);
            }

            result.Success = true;
            result.Status =
                result.ErrorCount > 0
                    ? "COMPLETED_WITH_ERRORS"
                    : candidates.Count == 0
                        ? "NO_CANDIDATES"
                        : "PREVIEW_COMPLETED";

            result.Message =
                $"Recovery Preview hoàn tất. " +
                $"From={recoveryFrom:yyyy-MM-dd HH:mm:ss}, " +
                $"To={recoveryTo:yyyy-MM-dd HH:mm:ss}, " +
                $"Candidates={result.CandidateCount}, " +
                $"Recoverable={result.RecoverableCount}, " +
                $"NotFound={result.NotFoundCount}, " +
                $"NotStable={result.NotStableCount}, " +
                $"AlreadyHandled={result.AlreadyHandledCount}, " +
                $"Ambiguous={result.AmbiguousCount}, " +
                $"IdentityMismatch={result.IdentityMismatchCount}, " +
                $"Errors={result.ErrorCount}.";

            return result;
        }

        /// <summary>
        /// PACS-XQ-PREPROD-01B-03
        ///
        /// Manual Missing Study Recovery for exactly one published Worklist.
        ///
        /// Rules:
        /// - Project A only: Worklist Status must be 1 (Published);
        /// - StartDate is mandatory and fail-closed;
        /// - the Worklist must also satisfy MinAgeMinutes;
        /// - find exactly one Orthanc Study by AccessionNumber;
        /// - revalidate Orthanc AccessionNumber, StudyInstanceUID and IsStable;
        /// - refuse if PacsStudy already exists by Accession/Orthanc ID/Study UID;
        /// - actual mutation is delegated only to
        ///   PacsStudyBL.TrySyncSingleStudyAsync();
        /// - this method never reads/writes PacsSyncState as an owner and never
        ///   changes LastChangeSeq;
        /// - after sync, re-read DB and verify Worklist, ResultCDHA and hierarchy.
        /// </summary>
        public async Task<PacsRecoveryManualResult>
            RecoverWorklistAsync(
                long pacsWorklistItemId,
                CancellationToken cancellationToken = default,
                bool orthancAlreadyConfigured = false)
        {
            var result =
                new PacsRecoveryManualResult
                {
                    PacsWorklistItemId =
                        pacsWorklistItemId
                };

            if (pacsWorklistItemId <= 0)
            {
                result.Success = false;
                result.Status =
                    "INVALID_WORKLIST_ID";
                result.Message =
                    "PacsWorklistItemId không hợp lệ.";

                return result;
            }

            var options =
                _recoveryOptions.CurrentValue
                ?? new PacsSyncRecoveryOptions();

            if (!TryParseRecoveryStartDate(
                    options.StartDate,
                    out var recoveryFrom))
            {
                result.Success = false;
                result.Status =
                    "RECOVERY_CONFIG_INVALID";
                result.Message =
                    "PacsSyncRecovery:StartDate bắt buộc có định dạng yyyy-MM-dd. " +
                    "Recovery fail-closed để không quét nhầm dữ liệu lịch sử.";

                return result;
            }

            var minAgeMinutes =
                NormalizeMinAgeMinutes(
                    options.MinAgeMinutes);

            var recoveryTo =
                DateTime.Now.AddMinutes(
                    -minAgeMinutes);

            result.RecoveryFrom =
                recoveryFrom;

            var worklist =
                await _context.PacsWorklistItems
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        x =>
                            x.Id ==
                            pacsWorklistItemId,
                        cancellationToken);

            if (worklist == null)
            {
                result.Success = false;
                result.Status =
                    "WORKLIST_NOT_FOUND";
                result.Message =
                    "Không tìm thấy PacsWorklistItem.";

                return result;
            }

            result.ResultCDHAId =
                worklist.ResultCDHAId;

            result.AccessionNumber =
                NormalizeIdentity(
                    worklist.AccessionNumber);

            result.WorklistCreatedOn =
                worklist.CreatedOn;

            result.WorklistStatusBefore =
                worklist.Status;

            if (worklist.CreatedOn <
                recoveryFrom)
            {
                result.Success = false;
                result.Status =
                    "OUTSIDE_RECOVERY_WINDOW";
                result.Message =
                    $"Worklist được tạo trước mốc Recovery " +
                    $"{recoveryFrom:yyyy-MM-dd HH:mm:ss}.";

                return result;
            }

            if (worklist.CreatedOn >
                recoveryTo)
            {
                result.Success = false;
                result.Status =
                    "WORKLIST_TOO_NEW";
                result.Message =
                    $"Worklist chưa đủ MinAgeMinutes={minAgeMinutes} để Recovery.";

                return result;
            }

            if (worklist.Status != 1)
            {
                result.Success = false;
                result.Status =
                    worklist.Status == 3
                        ? "ALREADY_COMPLETED"
                        : "INVALID_WORKLIST_STATUS";
                result.Message =
                    $"Worklist hiện có Status={worklist.Status}. " +
                    "Missing Study Recovery của Project A chỉ xử lý Status=1.";

                return result;
            }

            if (string.IsNullOrWhiteSpace(
                    result.AccessionNumber))
            {
                result.Success = false;
                result.Status =
                    "MISSING_ACCESSION";
                result.Message =
                    "Worklist không có AccessionNumber hợp lệ.";

                return result;
            }

            var accessionNumber =
                result.AccessionNumber;

            /*
             * Missing Study Recovery must not be used as a generic repair tool.
             * If a PacsStudy already exists for this accession, stop here.
             */
            var existingByAccession =
                await _context.PacsStudies
                    .AsNoTracking()
                    .Where(x =>
                        x.AccessionNumber != null &&
                        x.AccessionNumber.Trim() ==
                            accessionNumber)
                    .OrderBy(x => x.Id)
                    .Take(2)
                    .Select(x => new
                    {
                        x.Id,
                        x.ResultCDHAId,
                        x.OrthancStudyId,
                        x.StudyInstanceUID
                    })
                    .ToListAsync(
                        cancellationToken);

            if (existingByAccession.Count > 1)
            {
                result.Success = false;
                result.Status =
                    "LIS_STUDY_AMBIGUOUS";
                result.Message =
                    "Có nhiều PacsStudy cùng AccessionNumber trong LIS. " +
                    "Không được tự Recovery.";

                return result;
            }

            if (existingByAccession.Count == 1)
            {
                var existing =
                    existingByAccession[0];

                result.PacsStudyId =
                    existing.Id;

                result.Success = false;
                result.Status =
                    existing.ResultCDHAId ==
                    worklist.ResultCDHAId
                        ? "PACS_STUDY_EXISTS"
                        : "IDENTITY_MISMATCH";
                result.Message =
                    existing.ResultCDHAId ==
                    worklist.ResultCDHAId
                        ? "PacsStudy đã tồn tại cho đúng ResultCDHA. " +
                          "Đây không còn là Missing Study Recovery."
                        : "AccessionNumber đã thuộc PacsStudy của ResultCDHA khác. " +
                          "Không được tự Recovery.";

                return result;
            }

            if (!orthancAlreadyConfigured)
            {
                var config =
                    await _pacsConfigBL.GetDefaultAsync();

                if (config == null ||
                    !config.IsActive ||
                    !config.IsDefault)
                {
                    result.Success = false;
                    result.Status =
                        "PACS_CONFIG_NOT_FOUND";
                    result.Message =
                        "Chưa có PACS Server mặc định đang hoạt động.";

                    return result;
                }

                _orthancClient.Configure(config);
            }

            try
            {
                var studyIds =
                    await _orthancClient
                        .FindStudyIdsByAccessionNumberAsync(
                            accessionNumber,
                            cancellationToken)
                    ?? new List<string>();

                result.OrthancStudyCount =
                    studyIds.Count;

                if (studyIds.Count == 0)
                {
                    result.Success = false;
                    result.Status =
                        "ORTHANC_NOT_FOUND";
                    result.Message =
                        "Không tìm thấy Study trong Orthanc.";

                    return result;
                }

                if (studyIds.Count > 1)
                {
                    result.Success = false;
                    result.Status =
                        "AMBIGUOUS";
                    result.Message =
                        $"Có {studyIds.Count} Study Orthanc cùng AccessionNumber. " +
                        "Không được chọn tùy ý một Study để Recovery.";

                    return result;
                }

                var orthancStudyId =
                    studyIds[0];

                result.OrthancStudyId =
                    orthancStudyId;

                var studyJson =
                    await _orthancClient
                        .GetStudyJsonAsync(
                            orthancStudyId,
                            cancellationToken);

                using var document =
                    JsonDocument.Parse(
                        studyJson);

                var root =
                    document.RootElement;

                var mainTags =
                    GetObject(
                        root,
                        "MainDicomTags");

                var orthancAccessionNumber =
                    NormalizeIdentity(
                        GetString(
                            mainTags,
                            "AccessionNumber"));

                var studyInstanceUid =
                    NormalizeIdentity(
                        GetString(
                            mainTags,
                            "StudyInstanceUID"));

                result.StudyInstanceUID =
                    studyInstanceUid;

                var seriesIds =
                    GetStringArray(
                        root,
                        "Series");

                result.OrthancSeriesCount =
                    seriesIds.Count;

                result.IsStable =
                    GetNullableBoolean(
                        root,
                        "IsStable");

                if (!string.Equals(
                        accessionNumber,
                        orthancAccessionNumber,
                        StringComparison.OrdinalIgnoreCase))
                {
                    result.Success = false;
                    result.Status =
                        "ACCESSION_MISMATCH";
                    result.Message =
                        $"AccessionNumber Worklist={accessionNumber}, " +
                        $"Orthanc={orthancAccessionNumber ?? "<empty>"}.";

                    return result;
                }

                if (string.IsNullOrWhiteSpace(
                        studyInstanceUid))
                {
                    result.Success = false;
                    result.Status =
                        "STUDY_UID_MISSING";
                    result.Message =
                        "Orthanc Study không có StudyInstanceUID hợp lệ.";

                    return result;
                }

                if (result.IsStable != true)
                {
                    result.Success = false;
                    result.Status =
                        "NOT_STABLE";
                    result.Message =
                        result.IsStable == false
                            ? "Study hiện chưa Stable. Không Recovery."
                            : "Orthanc không trả trạng thái IsStable hợp lệ. Không Recovery.";

                    return result;
                }

                result.OrthancInstanceCount =
                    await ReadOrthancInstanceCountAsync(
                        orthancStudyId,
                        seriesIds.Count,
                        cancellationToken);

                /*
                 * Defensive identity guard immediately before mutation.
                 */
                var existingByIdentity =
                    await _context.PacsStudies
                        .AsNoTracking()
                        .Where(x =>
                            x.OrthancStudyId ==
                                orthancStudyId ||
                            x.StudyInstanceUID ==
                                studyInstanceUid)
                        .OrderBy(x => x.Id)
                        .Take(2)
                        .Select(x => new
                        {
                            x.Id,
                            x.ResultCDHAId,
                            x.AccessionNumber
                        })
                        .ToListAsync(
                            cancellationToken);

                if (existingByIdentity.Count > 0)
                {
                    result.PacsStudyId =
                        existingByIdentity[0].Id;

                    result.Success = false;
                    result.Status =
                        existingByIdentity.Count > 1
                            ? "LIS_STUDY_AMBIGUOUS"
                            : "PACS_STUDY_EXISTS";
                    result.Message =
                        existingByIdentity.Count > 1
                            ? "OrthancStudyId/StudyInstanceUID đang trỏ tới nhiều PacsStudy. " +
                              "Không được tự Recovery."
                            : "Study đã tồn tại trong PacsStudies theo Orthanc identity. " +
                              "Không chạy Missing Study Recovery.";

                    return result;
                }

                /*
                 * This is the ONLY mutation call in Manual Recovery.
                 * It is non-blocking: FAST PATH/manual study sync wins the gate,
                 * Recovery simply returns SYNC_BUSY and can be retried later.
                 */
                var syncResult =
                    await _pacsStudyBL
                        .TrySyncSingleStudyAsync(
                            orthancStudyId,
                            cancellationToken);

                if (!syncResult.Success)
                {
                    result.Success = false;

                    if (string.Equals(
                            syncResult.ErrorCode,
                            "STUDY_SYNC_BUSY",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        result.Status =
                            "SYNC_BUSY";
                        result.Message =
                            syncResult.Message;

                        return result;
                    }

                    result.Status =
                        "STUDY_SYNC_FAILED";
                    result.ErrorMessage =
                        $"{syncResult.ErrorCode}: {syncResult.Message}";
                    result.Message =
                        "Canonical Study sync thất bại. Recovery không được đánh dấu thành công.";

                    return result;
                }

                result.PacsStudyId =
                    syncResult.PacsStudyId;
                result.SeriesCount =
                    syncResult.SeriesCount;
                result.InstanceCount =
                    syncResult.InstanceCount;
                result.MappedResultCDHA =
                    syncResult.MappedResultCDHA;

                if (!syncResult.PacsStudyId.HasValue)
                {
                    result.Success = false;
                    result.Status =
                        "VERIFY_PACS_STUDY_FAILED";
                    result.Message =
                        "Sync báo thành công nhưng không trả PacsStudyId.";

                    return result;
                }

                /*
                 * Post-sync verification. All reads are AsNoTracking so the
                 * verification reflects committed DB state, not tracked values.
                 */
                var updatedWorklist =
                    await _context.PacsWorklistItems
                        .AsNoTracking()
                        .FirstOrDefaultAsync(
                            x =>
                                x.Id ==
                                pacsWorklistItemId,
                            cancellationToken);

                if (updatedWorklist == null)
                {
                    result.Success = false;
                    result.Status =
                        "VERIFY_WORKLIST_NOT_FOUND";
                    result.Message =
                        "Sync đã chạy nhưng không đọc lại được Worklist.";

                    return result;
                }

                result.WorklistStatusAfter =
                    updatedWorklist.Status;
                result.WorklistCompleted =
                    updatedWorklist.Status == 3 &&
                    updatedWorklist.CompletedOn.HasValue;
                result.CompletedOn =
                    updatedWorklist.CompletedOn;

                if (!result.WorklistCompleted)
                {
                    result.Success = false;
                    result.Status =
                        "VERIFY_WORKLIST_FAILED";
                    result.Message =
                        "Study đã sync nhưng Worklist chưa ở Status=3 với CompletedOn hợp lệ.";

                    return result;
                }

                if (!string.Equals(
                        NormalizeIdentity(
                            updatedWorklist.AccessionNumber),
                        accessionNumber,
                        StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(
                        NormalizeIdentity(
                            updatedWorklist.OrthancStudyId),
                        orthancStudyId,
                        StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(
                        NormalizeIdentity(
                            updatedWorklist.StudyInstanceUID),
                        studyInstanceUid,
                        StringComparison.Ordinal))
                {
                    result.Success = false;
                    result.Status =
                        "VERIFY_WORKLIST_IDENTITY_FAILED";
                    result.Message =
                        "Worklist đã Completed nhưng PACS identity sau Recovery không khớp.";

                    return result;
                }

                var pacsStudy =
                    await _context.PacsStudies
                        .AsNoTracking()
                        .FirstOrDefaultAsync(
                            x =>
                                x.Id ==
                                syncResult.PacsStudyId.Value,
                            cancellationToken);

                if (pacsStudy == null ||
                    pacsStudy.ResultCDHAId !=
                        worklist.ResultCDHAId ||
                    !string.Equals(
                        NormalizeIdentity(
                            pacsStudy.AccessionNumber),
                        accessionNumber,
                        StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(
                        pacsStudy.OrthancStudyId,
                        orthancStudyId,
                        StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(
                        pacsStudy.StudyInstanceUID,
                        studyInstanceUid,
                        StringComparison.Ordinal) ||
                    !string.Equals(
                        pacsStudy.Status,
                        "MAPPED",
                        StringComparison.OrdinalIgnoreCase))
                {
                    result.Success = false;
                    result.Status =
                        "VERIFY_PACS_STUDY_FAILED";
                    result.Message =
                        "PacsStudy sau Recovery không khớp ResultCDHA/Orthanc identity hoặc chưa MAPPED.";

                    return result;
                }

                var resultCdha =
                    await _context.ResultCDHAs
                        .AsNoTracking()
                        .FirstOrDefaultAsync(
                            x =>
                                x.Id ==
                                worklist.ResultCDHAId,
                            cancellationToken);

                if (resultCdha == null ||
                    !string.Equals(
                        NormalizeIdentity(
                            resultCdha.AccessionNumber),
                        accessionNumber,
                        StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(
                        NormalizeIdentity(
                            resultCdha.OrthancStudyId),
                        orthancStudyId,
                        StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(
                        NormalizeIdentity(
                            resultCdha.StudyInstanceUID),
                        studyInstanceUid,
                        StringComparison.Ordinal) ||
                    !string.Equals(
                        resultCdha.PacsStatus,
                        "SYNCED",
                        StringComparison.OrdinalIgnoreCase))
                {
                    result.Success = false;
                    result.Status =
                        "VERIFY_RESULT_CDHA_FAILED";
                    result.Message =
                        "ResultCDHA sau Recovery không có PACS identity/SYNCED status hợp lệ.";

                    return result;
                }

                var actualSeriesCount =
                    await _context.PacsSeries
                        .AsNoTracking()
                        .CountAsync(
                            x =>
                                x.PacsStudyId ==
                                pacsStudy.Id,
                            cancellationToken);

                var actualInstanceCount =
                    await _context.PacsInstances
                        .AsNoTracking()
                        .CountAsync(
                            x =>
                                x.PacsStudyId ==
                                pacsStudy.Id,
                            cancellationToken);

                if (actualSeriesCount !=
                        syncResult.SeriesCount ||
                    actualInstanceCount !=
                        syncResult.InstanceCount ||
                    pacsStudy.SeriesCount !=
                        actualSeriesCount ||
                    pacsStudy.InstanceCount !=
                        actualInstanceCount)
                {
                    result.Success = false;
                    result.Status =
                        "VERIFY_HIERARCHY_FAILED";
                    result.Message =
                        $"Hierarchy count sau Recovery không khớp. " +
                        $"Sync={syncResult.SeriesCount}/{syncResult.InstanceCount}, " +
                        $"DB={actualSeriesCount}/{actualInstanceCount}, " +
                        $"PacsStudy={pacsStudy.SeriesCount}/{pacsStudy.InstanceCount}.";

                    return result;
                }

                result.SeriesCount =
                    actualSeriesCount;
                result.InstanceCount =
                    actualInstanceCount;
                result.MappedResultCDHA =
                    true;

                result.Success = true;
                result.Status =
                    "RECOVERED";
                result.Message =
                    $"Recovery thành công. " +
                    $"Accession={accessionNumber}, " +
                    $"PacsStudyId={pacsStudy.Id}, " +
                    $"Series={actualSeriesCount}, " +
                    $"Instances={actualInstanceCount}.";

                return result;
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Status =
                    "RECOVERY_ERROR";
                result.ErrorMessage =
                    ex.GetBaseException().Message;
                result.Message =
                    "Manual Recovery thất bại.";

                return result;
            }
        }

        /// <summary>
        /// PACS-XQ-PREPROD-01B-04
        ///
        /// Batch Missing Study Recovery with anti-starvation scheduling.
        ///
        /// IMPORTANT:
        /// - Project A candidates are Worklist Status=1 only;
        /// - StartDate is mandatory and fail-closed;
        /// - no PacsSyncRecovery:Enabled check here. Enabled is reserved for
        ///   automatic Background integration in PREPROD-01D;
        /// - candidate order: never checked -> oldest checked -> oldest created -> Id;
        /// - every item is fully revalidated by RecoverWorklistAsync();
        /// - one item failure never aborts the remaining batch;
        /// - SYNC_BUSY is NOT marked checked so it can retry soon;
        /// - this method never reads/writes PacsSyncState checkpoint as owner.
        /// </summary>
        public async Task<PacsRecoveryBatchResult>
            RecoverPendingAsync(
                int? requestedBatchSize = null,
                CancellationToken cancellationToken = default)
        {
            var result =
                new PacsRecoveryBatchResult();

            var batchLockAcquired = false;

            try
            {
                batchLockAcquired =
                    await _recoveryBatchLock.WaitAsync(
                        0,
                        cancellationToken);

                if (!batchLockAcquired)
                {
                    result.Success = false;
                    result.Status =
                        "RECOVERY_BATCH_BUSY";
                    result.Message =
                        "Đang có một Recovery Batch khác chạy.";

                    return result;
                }

                var options =
                    _recoveryOptions.CurrentValue
                    ?? new PacsSyncRecoveryOptions();

                if (!TryParseRecoveryStartDate(
                        options.StartDate,
                        out var recoveryFrom))
                {
                    result.Success = false;
                    result.Status =
                        "RECOVERY_CONFIG_INVALID";
                    result.Message =
                        "PacsSyncRecovery:StartDate bắt buộc có định dạng yyyy-MM-dd. " +
                        "Recovery fail-closed để không quét nhầm dữ liệu lịch sử.";

                    return result;
                }

                var minAgeMinutes =
                    NormalizeMinAgeMinutes(
                        options.MinAgeMinutes);

                var batchSize =
                    NormalizeBatchSize(
                        requestedBatchSize,
                        options.BatchSize);

                var recoveryTo =
                    DateTime.Now.AddMinutes(
                        -minAgeMinutes);

                result.RecoveryFrom =
                    recoveryFrom;
                result.RecoveryTo =
                    recoveryTo;
                result.MinAgeMinutes =
                    minAgeMinutes;
                result.BatchSize =
                    batchSize;

                // Configure Orthanc once for the whole batch. Individual item
                // recovery receives orthancAlreadyConfigured=true.
                var config =
                    await _pacsConfigBL.GetDefaultAsync();

                if (config == null ||
                    !config.IsActive ||
                    !config.IsDefault)
                {
                    result.Success = false;
                    result.Status =
                        "PACS_CONFIG_NOT_FOUND";
                    result.Message =
                        "Chưa có PACS Server mặc định đang hoạt động.";

                    return result;
                }

                _orthancClient.Configure(config);

                // Load IDs only. Each item is reloaded and revalidated by the
                // manual Recovery path immediately before any mutation.
                var candidateIds =
                    await _context.PacsWorklistItems
                        .AsNoTracking()
                        .Where(x =>
                            x.Status == 1 &&
                            x.CreatedOn >= recoveryFrom &&
                            x.CreatedOn <= recoveryTo &&
                            x.AccessionNumber != null &&
                            x.AccessionNumber != "")
                        .OrderBy(x =>
                            x.LastRecoveryCheckedOn.HasValue
                                ? 1
                                : 0)
                        .ThenBy(x =>
                            x.LastRecoveryCheckedOn)
                        .ThenBy(x =>
                            x.CreatedOn)
                        .ThenBy(x =>
                            x.Id)
                        .Take(batchSize)
                        .Select(x => x.Id)
                        .ToListAsync(
                            cancellationToken);

                result.CandidateCount =
                    candidateIds.Count;

                foreach (var worklistId in
                         candidateIds)
                {
                    cancellationToken
                        .ThrowIfCancellationRequested();

                    PacsRecoveryManualResult item;

                    try
                    {
                        item =
                            await RecoverWorklistAsync(
                                worklistId,
                                cancellationToken,
                                orthancAlreadyConfigured: true);
                    }
                    catch (OperationCanceledException)
                        when (cancellationToken.IsCancellationRequested)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        item =
                            new PacsRecoveryManualResult
                            {
                                PacsWorklistItemId =
                                    worklistId,
                                Success = false,
                                Status =
                                    "RECOVERY_ERROR",
                                Message =
                                    "Recovery Worklist phát sinh lỗi.",
                                ErrorMessage =
                                    ex.GetBaseException().Message
                            };
                    }

                    // SYNC_BUSY means FAST PATH or another study sync currently
                    // owns the mutation gate. Do not push this item to the back
                    // of the anti-starvation queue before it was truly checked.
                    if (!string.Equals(
                            item.Status,
                            "SYNC_BUSY",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            await MarkRecoveryCheckedAsync(
                                worklistId,
                                cancellationToken);
                        }
                        catch (OperationCanceledException)
                            when (cancellationToken.IsCancellationRequested)
                        {
                            throw;
                        }
                        catch (Exception ex)
                        {
                            item.Success = false;
                            item.Status =
                                "RECOVERY_SCHEDULER_UPDATE_ERROR";
                            item.ErrorMessage =
                                ex.GetBaseException().Message;
                            item.Message =
                                "Recovery đã kiểm tra Worklist nhưng không cập nhật được LastRecoveryCheckedOn.";
                        }
                    }

                    result.Items.Add(item);

                    if (item.Success &&
                        string.Equals(
                            item.Status,
                            "RECOVERED",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        result.RecoveredCount++;
                        continue;
                    }

                    switch (item.Status)
                    {
                        case "ORTHANC_NOT_FOUND":
                            result.NotFoundCount++;
                            break;

                        case "NOT_STABLE":
                            result.NotStableCount++;
                            break;

                        case "ALREADY_COMPLETED":
                        case "PACS_STUDY_EXISTS":
                            result.AlreadyHandledCount++;
                            break;

                        case "AMBIGUOUS":
                            result.AmbiguousCount++;
                            break;

                        case "IDENTITY_MISMATCH":
                        case "LIS_STUDY_AMBIGUOUS":
                        case "ACCESSION_MISMATCH":
                        case "STUDY_UID_MISSING":
                            result.IdentityMismatchCount++;
                            break;

                        case "SYNC_BUSY":
                            result.SyncBusyCount++;
                            break;

                        case "OUTSIDE_RECOVERY_WINDOW":
                        case "WORKLIST_TOO_NEW":
                        case "INVALID_WORKLIST_STATUS":
                        case "MISSING_ACCESSION":
                        case "WORKLIST_NOT_FOUND":
                            result.SkippedCount++;
                            break;

                        default:
                            result.ErrorCount++;
                            break;
                    }
                }

                result.Success =
                    result.ErrorCount == 0;

                result.Status =
                    result.Success
                        ? candidateIds.Count == 0
                            ? "NO_CANDIDATES"
                            : "COMPLETED"
                        : "COMPLETED_WITH_ERRORS";

                result.Message =
                    $"Recovery Batch hoàn tất. " +
                    $"Candidates={result.CandidateCount}, " +
                    $"Recovered={result.RecoveredCount}, " +
                    $"NotFound={result.NotFoundCount}, " +
                    $"NotStable={result.NotStableCount}, " +
                    $"AlreadyHandled={result.AlreadyHandledCount}, " +
                    $"Ambiguous={result.AmbiguousCount}, " +
                    $"IdentityMismatch={result.IdentityMismatchCount}, " +
                    $"SyncBusy={result.SyncBusyCount}, " +
                    $"Skipped={result.SkippedCount}, " +
                    $"Errors={result.ErrorCount}.";

                return result;
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Status =
                    "BATCH_ERROR";
                result.ErrorMessage =
                    ex.GetBaseException().Message;
                result.Message =
                    "Recovery Batch thất bại.";

                return result;
            }
            finally
            {
                if (batchLockAcquired)
                {
                    _recoveryBatchLock.Release();
                }
            }
        }

        /// <summary>
        /// Store anti-starvation scheduler state.
        ///
        /// If an item was recovered successfully it is already Status=3 and no
        /// longer participates in future Recovery scans, so no timestamp write
        /// is needed. Pending Status=1 items receive LastRecoveryCheckedOn.
        /// </summary>
        private async Task MarkRecoveryCheckedAsync(
            long pacsWorklistItemId,
            CancellationToken cancellationToken)
        {
            var worklist =
                await _context.PacsWorklistItems
                    .FirstOrDefaultAsync(
                        x =>
                            x.Id ==
                            pacsWorklistItemId,
                        cancellationToken);

            if (worklist == null ||
                worklist.Status != 1)
            {
                return;
            }

            worklist.LastRecoveryCheckedOn =
                DateTime.Now;

            await _context.SaveChangesAsync(
                cancellationToken);
        }

        private static void ApplyExistingStudy(
            PacsRecoveryPreviewItem item,
            long pacsStudyId,
            int seriesCount,
            int instanceCount,
            string? status)
        {
            item.PacsStudyExists = true;
            item.PacsStudyId =
                pacsStudyId;
            item.PacsStudySeriesCount =
                seriesCount;
            item.PacsStudyInstanceCount =
                instanceCount;
            item.PacsStudyStatus =
                status;
        }

        private async Task<int>
            ReadOrthancInstanceCountAsync(
                string orthancStudyId,
                int expectedSeriesCount,
                CancellationToken cancellationToken)
        {
            var seriesJson =
                await _orthancClient
                    .GetStudySeriesJsonAsync(
                        orthancStudyId,
                        cancellationToken);

            using var document =
                JsonDocument.Parse(
                    seriesJson);

            if (document.RootElement.ValueKind !=
                JsonValueKind.Array)
            {
                throw new Exception(
                    "GET /studies/{id}/series không trả JSON array.");
            }

            var seriesElements =
                document.RootElement
                    .EnumerateArray()
                    .ToList();

            if (seriesElements.Count !=
                expectedSeriesCount)
            {
                throw new Exception(
                    $"Orthanc Study Series count không ổn định khi Preview. " +
                    $"Study={orthancStudyId}, " +
                    $"Expected={expectedSeriesCount}, " +
                    $"Bulk={seriesElements.Count}.");
            }

            var instanceCount = 0;

            foreach (var seriesElement in
                     seriesElements)
            {
                if (seriesElement.ValueKind !=
                    JsonValueKind.Object)
                {
                    throw new Exception(
                        "Orthanc Study Series chứa phần tử không phải JSON object.");
                }

                if (!seriesElement.TryGetProperty(
                        "Instances",
                        out var instancesElement) ||
                    instancesElement.ValueKind !=
                        JsonValueKind.Array)
                {
                    throw new Exception(
                        "Orthanc Series không có mảng Instances hợp lệ.");
                }

                instanceCount +=
                    instancesElement
                        .GetArrayLength();
            }

            return instanceCount;
        }

        private static bool TryParseReconciliationStartDate(
            string? rawValue,
            out DateTime reconciliationFrom)
        {
            reconciliationFrom = default;

            if (string.IsNullOrWhiteSpace(
                    rawValue))
            {
                return false;
            }

            return DateTime.TryParseExact(
                rawValue.Trim(),
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out reconciliationFrom);
        }

        private static int NormalizeReconciliationBatchSize(
            int? requestedBatchSize,
            int configuredBatchSize)
        {
            var value =
                requestedBatchSize.HasValue &&
                requestedBatchSize.Value > 0
                    ? requestedBatchSize.Value
                    : configuredBatchSize > 0
                        ? configuredBatchSize
                        : DefaultReconciliationBatchSize;

            return Math.Clamp(
                value,
                1,
                MaxBatchSize);
        }

        private static int NormalizeReconciliationMinAgeMinutes(
            int configuredValue)
        {
            var value =
                configuredValue > 0
                    ? configuredValue
                    : DefaultReconciliationMinAgeMinutes;

            return Math.Clamp(
                value,
                1,
                MaxMinAgeMinutes);
        }

        private static bool IdentityMatchesRequired(
            string? left,
            string? right)
        {
            var normalizedLeft =
                NormalizeIdentity(left);

            var normalizedRight =
                NormalizeIdentity(right);

            if (normalizedLeft == null ||
                normalizedRight == null)
            {
                return false;
            }

            return string.Equals(
                normalizedLeft,
                normalizedRight,
                StringComparison.OrdinalIgnoreCase);
        }

        private static bool TryParseRecoveryStartDate(
            string? rawValue,
            out DateTime recoveryFrom)
        {
            recoveryFrom = default;

            if (string.IsNullOrWhiteSpace(
                    rawValue))
            {
                return false;
            }

            return DateTime.TryParseExact(
                rawValue.Trim(),
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out recoveryFrom);
        }

        private static int NormalizeBatchSize(
            int? requestedBatchSize,
            int configuredBatchSize)
        {
            var value =
                requestedBatchSize.HasValue &&
                requestedBatchSize.Value > 0
                    ? requestedBatchSize.Value
                    : configuredBatchSize > 0
                        ? configuredBatchSize
                        : DefaultBatchSize;

            return Math.Clamp(
                value,
                1,
                MaxBatchSize);
        }

        private static int NormalizeMinAgeMinutes(
            int configuredValue)
        {
            var value =
                configuredValue > 0
                    ? configuredValue
                    : DefaultMinAgeMinutes;

            return Math.Clamp(
                value,
                1,
                MaxMinAgeMinutes);
        }

        private static JsonElement? GetObject(
            JsonElement root,
            string propertyName)
        {
            if (root.ValueKind ==
                    JsonValueKind.Object &&
                root.TryGetProperty(
                    propertyName,
                    out var value) &&
                value.ValueKind ==
                    JsonValueKind.Object)
            {
                return value;
            }

            return null;
        }

        private static string? GetString(
            JsonElement? source,
            string propertyName)
        {
            if (!source.HasValue ||
                source.Value.ValueKind !=
                    JsonValueKind.Object ||
                !source.Value.TryGetProperty(
                    propertyName,
                    out var value))
            {
                return null;
            }

            return value.ValueKind ==
                    JsonValueKind.String
                ? value.GetString()
                : value.ToString();
        }

        private static List<string> GetStringArray(
            JsonElement source,
            string propertyName)
        {
            if (source.ValueKind !=
                    JsonValueKind.Object ||
                !source.TryGetProperty(
                    propertyName,
                    out var value) ||
                value.ValueKind !=
                    JsonValueKind.Array)
            {
                return new List<string>();
            }

            return value
                .EnumerateArray()
                .Where(x =>
                    x.ValueKind ==
                        JsonValueKind.String)
                .Select(x =>
                    x.GetString())
                .Where(x =>
                    !string.IsNullOrWhiteSpace(x))
                .Select(x =>
                    x!.Trim())
                .Distinct(
                    StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static bool? GetNullableBoolean(
            JsonElement source,
            string propertyName)
        {
            if (source.ValueKind !=
                    JsonValueKind.Object ||
                !source.TryGetProperty(
                    propertyName,
                    out var value))
            {
                return null;
            }

            if (value.ValueKind ==
                JsonValueKind.True)
            {
                return true;
            }

            if (value.ValueKind ==
                JsonValueKind.False)
            {
                return false;
            }

            return null;
        }

        private static string? NormalizeIdentity(
            string? value)
        {
            return string.IsNullOrWhiteSpace(
                    value)
                ? null
                : value.Trim();
        }
    }
}
