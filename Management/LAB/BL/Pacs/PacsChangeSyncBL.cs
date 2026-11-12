using Management.Models;
using Management.Models.Pacs;
using Management.Services.Pacs;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Management.BL.Pacs
{
    /// <summary>
    /// PACS-XQ-04-06
    ///
    /// Ownership:
    /// - Orthanc /changes
    /// - StableStudy detection
    /// - PacsSyncState checkpoint
    ///
    /// Study/Series/Instance sync vẫn thuộc PacsStudyBL.
    ///
    /// Checkpoint rule:
    /// - chỉ advance một page sau khi toàn bộ StableStudy của page sync thành công;
    /// - lỗi Study => giữ checkpoint ở page thành công gần nhất;
    /// - retry an toàn vì PacsStudyBL idempotent.
    /// </summary>
    public class PacsChangeSyncBL
    {
        private readonly LABContext _context;
        private readonly PacsConfigBL _pacsConfigBL;
        private readonly OrthancClient _orthancClient;
        private readonly PacsStudyBL _pacsStudyBL;

        private static readonly SemaphoreSlim
            _checkpointLock = new(1, 1);

        public PacsChangeSyncBL(
            LABContext context,
            PacsConfigBL pacsConfigBL,
            OrthancClient orthancClient,
            PacsStudyBL pacsStudyBL)
        {
            _context = context;
            _pacsConfigBL = pacsConfigBL;
            _orthancClient = orthancClient;
            _pacsStudyBL = pacsStudyBL;
        }

        public async Task<PacsChangesPreviewResult>
            PreviewChangesAsync(
                long since = 0,
                int limit = 100,
                CancellationToken cancellationToken = default)
        {
            var result =
                new PacsChangesPreviewResult
                {
                    Since = since,
                    Limit = NormalizeLimit(limit)
                };

            if (since < 0)
            {
                result.Success = false;
                result.ErrorCode = "INVALID_SINCE";
                result.Message =
                    "Since không được nhỏ hơn 0.";
                return result;
            }

            var config =
                await _pacsConfigBL.GetDefaultAsync();

            if (config == null ||
                !config.IsActive ||
                !config.IsDefault)
            {
                result.Success = false;
                result.ErrorCode = "PACS_CONFIG_NOT_FOUND";
                result.Message =
                    "Chưa có PACS Server mặc định đang hoạt động.";
                return result;
            }

            try
            {
                _orthancClient.Configure(config);

                var response =
                    await ReadChangesAsync(
                        since,
                        result.Limit,
                        cancellationToken);

                var allChanges =
                    response.Changes ??
                    new List<OrthancChangeItem>();

                var stableStudies =
                    GetStableStudies(allChanges);

                result.Success = true;
                result.Last = response.Last;
                result.Done = response.Done;
                result.TotalChanges =
                    allChanges.Count;
                result.StableStudyCount =
                    stableStudies.Count;
                result.StableStudies =
                    stableStudies;

                result.Message =
                    $"Đọc Changes thành công. " +
                    $"Total={result.TotalChanges}, " +
                    $"StableStudy={result.StableStudyCount}, " +
                    $"Last={result.Last}, Done={result.Done}.";

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
                result.ErrorCode = "CHANGES_READ_FAILED";
                result.Message =
                    ex.GetBaseException().Message;
                return result;
            }
        }

        /// <summary>
        /// Controlled bootstrap.
        ///
        /// Dùng khi muốn bỏ qua toàn bộ lịch sử change cũ
        /// và bắt đầu theo dõi từ tail hiện tại.
        ///
        /// Không được gọi mù quáng ở production nếu còn Study lịch sử
        /// cần import.
        /// </summary>
        public async Task<PacsCheckpointInitializeResult>
            InitializeCheckpointToTailAsync(
                PacsCheckpointInitializeRequest request,
                CancellationToken cancellationToken = default)
        {
            request ??=
                new PacsCheckpointInitializeRequest();

            var result =
                new PacsCheckpointInitializeResult();

            if (!request.ConfirmSkipHistory)
            {
                result.Success = false;
                result.ErrorCode =
                    "CONFIRM_SKIP_HISTORY_REQUIRED";
                result.Message =
                    "Phải xác nhận ConfirmSkipHistory=true.";
                return result;
            }

            var limit =
                NormalizeLimit(request.Limit);

            var maxPages =
                NormalizeMaxPages(
                    request.MaxPages);

            await _checkpointLock.WaitAsync(
                cancellationToken);

            try
            {
                var config =
                    await _pacsConfigBL.GetDefaultAsync();

                if (config == null ||
                    !config.IsActive ||
                    !config.IsDefault)
                {
                    result.Success = false;
                    result.ErrorCode =
                        "PACS_CONFIG_NOT_FOUND";
                    result.Message =
                        "Chưa có PACS Server mặc định đang hoạt động.";
                    return result;
                }

                result.PacsConfigId =
                    config.Id;

                var syncState =
                    await GetOrCreateSyncStateAsync(
                        config.Id,
                        cancellationToken);

                result.CheckpointBefore =
                    syncState.LastChangeSeq;

                result.CheckpointAfter =
                    syncState.LastChangeSeq;

                syncState.LastRunAt =
                    DateTime.Now;

                syncState.UpdatedOn =
                    DateTime.Now;

                await _context.SaveChangesAsync(
                    cancellationToken);

                _orthancClient.Configure(config);

                var currentSince =
                    syncState.LastChangeSeq;

                for (var page = 1;
                     page <= maxPages;
                     page++)
                {
                    cancellationToken
                        .ThrowIfCancellationRequested();

                    var pageStart =
                        currentSince;

                    var response =
                        await ReadChangesAsync(
                            currentSince,
                            limit,
                            cancellationToken);

                    var changes =
                        response.Changes ??
                        new List<OrthancChangeItem>();

                    result.PagesRead++;
                    result.TotalChangesSkipped +=
                        changes.Count;

                    if (response.Last <
                        currentSince)
                    {
                        throw new Exception(
                            "Orthanc /changes trả Last nhỏ hơn checkpoint hiện tại.");
                    }

                    currentSince =
                        response.Last;

                    result.Done =
                        response.Done;

                    if (response.Done)
                    {
                        break;
                    }

                    if (currentSince <= pageStart)
                    {
                        throw new Exception(
                            "Orthanc /changes không tiến sequence nhưng Done=false.");
                    }
                }

                if (!result.Done)
                {
                    result.Success = false;
                    result.ErrorCode =
                        "CHECKPOINT_TAIL_NOT_REACHED";
                    result.Message =
                        $"Chưa tới tail sau {maxPages} page. " +
                        "Tăng MaxPages rồi chạy lại.";

                    syncState.LastError =
                        result.Message;

                    syncState.UpdatedOn =
                        DateTime.Now;

                    await _context.SaveChangesAsync(
                        cancellationToken);

                    return result;
                }

                syncState.LastChangeSeq =
                    currentSince;

                syncState.LastSuccessAt =
                    DateTime.Now;

                syncState.LastError =
                    null;

                syncState.UpdatedOn =
                    DateTime.Now;

                await _context.SaveChangesAsync(
                    cancellationToken);

                result.CheckpointAfter =
                    syncState.LastChangeSeq;

                result.Success = true;
                result.Message =
                    $"Checkpoint đã được bootstrap tới Orthanc tail " +
                    $"{result.CheckpointAfter}. " +
                    $"Đã bỏ qua {result.TotalChangesSkipped} change lịch sử.";

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
                result.ErrorCode ??=
                    "CHECKPOINT_INITIALIZE_FAILED";
                result.Message ??=
                    ex.GetBaseException().Message;

                return result;
            }
            finally
            {
                _checkpointLock.Release();
            }
        }

        public async Task<PacsSyncCheckpointResult>
            SyncFromCheckpointAsync(
                PacsSyncCheckpointRequest request,
                CancellationToken cancellationToken = default)
        {
            request ??=
                new PacsSyncCheckpointRequest();

            var limit =
                NormalizeLimit(request.Limit);

            var maxPages =
                NormalizeMaxPages(
                    request.MaxPages);

            var result =
                new PacsSyncCheckpointResult
                {
                    Limit = limit,
                    MaxPages = maxPages
                };

            await _checkpointLock.WaitAsync(
                cancellationToken);

            try
            {
                var config =
                    await _pacsConfigBL.GetDefaultAsync();

                if (config == null ||
                    !config.IsActive ||
                    !config.IsDefault)
                {
                    result.Success = false;
                    result.ErrorCode =
                        "PACS_CONFIG_NOT_FOUND";
                    result.Message =
                        "Chưa có PACS Server mặc định đang hoạt động.";
                    return result;
                }

                result.PacsConfigId =
                    config.Id;

                var syncState =
                    await GetOrCreateSyncStateAsync(
                        config.Id,
                        cancellationToken);

                result.CheckpointBefore =
                    syncState.LastChangeSeq;

                result.CheckpointAfter =
                    syncState.LastChangeSeq;

                syncState.LastRunAt =
                    DateTime.Now;

                syncState.LastError =
                    null;

                syncState.UpdatedOn =
                    DateTime.Now;

                await _context.SaveChangesAsync(
                    cancellationToken);

                _orthancClient.Configure(config);

                var currentSince =
                    syncState.LastChangeSeq;

                var processedStudyIds =
                    new HashSet<string>(
                        StringComparer.OrdinalIgnoreCase);

                for (var pageNumber = 1;
                     pageNumber <= maxPages;
                     pageNumber++)
                {
                    cancellationToken
                        .ThrowIfCancellationRequested();

                    var pageStartSeq =
                        currentSince;

                    var response =
                        await ReadChangesAsync(
                            currentSince,
                            limit,
                            cancellationToken);

                    var allChanges =
                        response.Changes ??
                        new List<OrthancChangeItem>();

                    result.PagesProcessed++;
                    result.TotalChanges +=
                        allChanges.Count;

                    result.Done =
                        response.Done;

                    var stableEvents =
                        GetStableStudies(
                            allChanges);

                    result.TotalStableEvents +=
                        stableEvents.Count;

                    /*
                     * Một Study có thể phát StableStudy nhiều lần.
                     * Trong một page lấy event có Seq mới nhất.
                     */
                    var uniqueStudiesInPage =
                        stableEvents
                            .GroupBy(
                                x => x.OrthancStudyId,
                                StringComparer.OrdinalIgnoreCase)
                            .Select(
                                g =>
                                    g.OrderByDescending(
                                        x => x.Seq)
                                     .First())
                            .OrderBy(x => x.Seq)
                            .ToList();

                    foreach (var change in
                             uniqueStudiesInPage)
                    {
                        cancellationToken
                            .ThrowIfCancellationRequested();

                        if (!processedStudyIds.Add(
                                change.OrthancStudyId))
                        {
                            continue;
                        }

                        result.UniqueStudyCount++;

                        var item =
                            new PacsSyncCheckpointItem
                            {
                                Seq = change.Seq,
                                OrthancStudyId =
                                    change.OrthancStudyId,
                                ChangeDate =
                                    change.Date,
                                Path =
                                    change.Path
                            };

                        var syncResult =
                            await _pacsStudyBL
                                .SyncSingleStudyAsync(
                                    change.OrthancStudyId,
                                    cancellationToken);

                        item.Success =
                            syncResult.Success;

                        item.ErrorCode =
                            syncResult.ErrorCode;

                        item.ErrorMessage =
                            syncResult.Success
                                ? null
                                : syncResult.Message;

                        item.PacsStudyId =
                            syncResult.PacsStudyId;

                        item.AccessionNumber =
                            syncResult.AccessionNumber;

                        item.StudyInstanceUID =
                            syncResult.StudyInstanceUID;

                        item.ResultCDHAId =
                            syncResult.ResultCDHAId;

                        item.WorklistId =
                            syncResult.WorklistId;

                        item.MappedResultCDHA =
                            syncResult.MappedResultCDHA;

                        item.WorklistCompleted =
                            syncResult.WorklistCompleted;

                        item.SeriesCount =
                            syncResult.SeriesCount;

                        item.InstanceCount =
                            syncResult.InstanceCount;

                        result.Items.Add(item);

                        if (!syncResult.Success)
                        {
                            result.FailedStudyCount++;

                            syncState.LastError =
                                $"Seq {change.Seq}, " +
                                $"Study {change.OrthancStudyId}: " +
                                $"{syncResult.ErrorCode} - " +
                                $"{syncResult.Message}";

                            syncState.UpdatedOn =
                                DateTime.Now;

                            await _context.SaveChangesAsync(
                                cancellationToken);

                            /*
                             * QUAN TRỌNG:
                             * Page lỗi chưa được commit checkpoint.
                             * currentSince vẫn là checkpoint của page trước.
                             */
                            result.Success = false;
                            result.ErrorCode =
                                "STUDY_SYNC_FAILED";

                            result.ErrorMessage =
                                syncState.LastError;

                            result.CheckpointAfter =
                                syncState.LastChangeSeq;

                            result.HasMore = true;

                            result.Message =
                                $"Sync lỗi tại StableStudy Seq={change.Seq}. " +
                                $"Checkpoint giữ ở {syncState.LastChangeSeq}.";

                            return result;
                        }

                        result.SuccessStudyCount++;
                    }

                    /*
                     * Chỉ tới đây khi toàn bộ StableStudy trong page PASS.
                     * Các event không phải StableStudy cũng được xem là đã tiêu thụ.
                     */
                    if (response.Last <
                        currentSince)
                    {
                        throw new Exception(
                            "Orthanc /changes trả Last nhỏ hơn checkpoint hiện tại.");
                    }

                    currentSince =
                        response.Last;

                    syncState.LastChangeSeq =
                        currentSince;

                    syncState.LastSuccessAt =
                        DateTime.Now;

                    syncState.LastError =
                        null;

                    syncState.UpdatedOn =
                        DateTime.Now;

                    await _context.SaveChangesAsync(
                        cancellationToken);

                    result.CheckpointAfter =
                        syncState.LastChangeSeq;

                    if (response.Done)
                    {
                        result.Done = true;
                        result.HasMore = false;
                        break;
                    }

                    if (currentSince <= pageStartSeq)
                    {
                        throw new Exception(
                            "Orthanc /changes không tiến sequence nhưng Done=false.");
                    }

                    if (pageNumber == maxPages)
                    {
                        result.HasMore = true;
                    }
                }

                result.Success = true;

                result.Message =
                    result.HasMore
                        ? $"Đã sync {result.PagesProcessed} page thành công. " +
                          $"Checkpoint={result.CheckpointAfter}. " +
                          "Orthanc vẫn còn change phía sau."
                        : $"Sync checkpoint thành công. " +
                          $"Checkpoint={result.CheckpointAfter}, " +
                          $"Studies={result.SuccessStudyCount}.";

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
                result.ErrorCode ??=
                    "CHECKPOINT_SYNC_FAILED";
                result.ErrorMessage =
                    ex.GetBaseException().Message;

                result.Message =
                    "Sync PACS theo checkpoint thất bại.";

                /*
                 * Không chủ động advance checkpoint trong catch.
                 * Các page đã commit trước lỗi vẫn được giữ.
                 */
                try
                {
                    if (result.PacsConfigId.HasValue)
                    {
                        var state =
                            await _context.PacsSyncStates
                                .FirstOrDefaultAsync(
                                    x =>
                                        x.PacsConfigId ==
                                        result.PacsConfigId.Value,
                                    CancellationToken.None);

                        if (state != null)
                        {
                            state.LastError =
                                result.ErrorMessage;

                            state.UpdatedOn =
                                DateTime.Now;

                            await _context.SaveChangesAsync(
                                CancellationToken.None);

                            result.CheckpointAfter =
                                state.LastChangeSeq;
                        }
                    }
                }
                catch
                {
                    // Không che lỗi gốc bằng lỗi ghi LastError.
                }

                return result;
            }
            finally
            {
                _checkpointLock.Release();
            }
        }

        public async Task<PacsCheckpointStatusResult>
            GetCheckpointStatusAsync(
                CancellationToken cancellationToken = default)
        {
            var result =
                new PacsCheckpointStatusResult();

            try
            {
                var config =
                    await _pacsConfigBL.GetDefaultAsync();

                if (config == null ||
                    !config.IsActive ||
                    !config.IsDefault)
                {
                    result.Success = false;
                    result.ErrorCode =
                        "PACS_CONFIG_NOT_FOUND";
                    result.Message =
                        "Chưa có PACS Server mặc định đang hoạt động.";
                    return result;
                }

                result.PacsConfigId =
                    config.Id;

                result.PacsConfigCode =
                    config.Code;

                result.PacsConfigName =
                    config.Name;

                var state =
                    await _context.PacsSyncStates
                        .AsNoTracking()
                        .FirstOrDefaultAsync(
                            x =>
                                x.PacsConfigId ==
                                config.Id,
                            cancellationToken);

                if (state == null)
                {
                    result.Success = true;
                    result.StateExists = false;
                    result.LastChangeSeq = 0;
                    result.Message =
                        "Chưa có PacsSyncState cho PACS mặc định.";
                    return result;
                }

                result.Success = true;
                result.StateExists = true;
                result.LastChangeSeq =
                    state.LastChangeSeq;
                result.LastRunAt =
                    state.LastRunAt;
                result.LastSuccessAt =
                    state.LastSuccessAt;
                result.LastError =
                    state.LastError;
                result.CreatedOn =
                    state.CreatedOn;
                result.UpdatedOn =
                    state.UpdatedOn;

                result.Message =
                    $"Checkpoint hiện tại = {state.LastChangeSeq}.";

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
                result.ErrorCode =
                    "CHECKPOINT_STATUS_FAILED";
                result.Message =
                    ex.GetBaseException().Message;
                return result;
            }
        }

        /// <summary>
        /// Manual range sync.
        ///
        /// Cố tình KHÔNG update:
        /// - LastChangeSeq
        /// - LastRunAt
        /// - LastSuccessAt
        /// - LastError
        ///
        /// Vì đây là công cụ vận hành/retry thủ công.
        /// Người vận hành có thể đọc SuggestedNextSince/CanAdvanceCheckpoint
        /// nhưng checkpoint thật chỉ do SyncFromCheckpointAsync quản lý.
        /// </summary>
        public async Task<PacsManualSyncResult>
            SyncManualRangeAsync(
                PacsManualSyncRequest request,
                CancellationToken cancellationToken = default)
        {
            request ??=
                new PacsManualSyncRequest();

            var result =
                new PacsManualSyncResult
                {
                    Since =
                        request.Since,
                    Limit =
                        NormalizeLimit(
                            request.Limit),
                    MaxPages =
                        NormalizeMaxPages(
                            request.MaxPages),
                    SuggestedNextSince =
                        request.Since
                };

            if (request.Since < 0)
            {
                result.Success = false;
                result.ErrorCode =
                    "INVALID_SINCE";
                result.Message =
                    "Since không được nhỏ hơn 0.";
                return result;
            }

            /*
             * Dùng cùng lock với checkpoint sync.
             * Manual sync không đổi checkpoint nhưng không nên chạy
             * chồng với SyncFromCheckpoint vì cả hai đều upsert cùng Study.
             */
            await _checkpointLock.WaitAsync(
                cancellationToken);

            try
            {
                var config =
                    await _pacsConfigBL.GetDefaultAsync();

                if (config == null ||
                    !config.IsActive ||
                    !config.IsDefault)
                {
                    result.Success = false;
                    result.ErrorCode =
                        "PACS_CONFIG_NOT_FOUND";
                    result.Message =
                        "Chưa có PACS Server mặc định đang hoạt động.";
                    return result;
                }

                result.PacsConfigId =
                    config.Id;

                var stateBefore =
                    await _context.PacsSyncStates
                        .AsNoTracking()
                        .FirstOrDefaultAsync(
                            x =>
                                x.PacsConfigId ==
                                config.Id,
                            cancellationToken);

                result.CheckpointBefore =
                    stateBefore?.LastChangeSeq ?? 0;

                /*
                 * Manual range không gọi GetOrCreateSyncStateAsync().
                 * Nếu state chưa tồn tại thì manual sync vẫn được phép,
                 * nhưng không tạo row checkpoint.
                 */
                _orthancClient.Configure(config);

                var currentSince =
                    request.Since;

                var processedStudyIds =
                    new HashSet<string>(
                        StringComparer.OrdinalIgnoreCase);

                for (var pageNumber = 1;
                     pageNumber <= result.MaxPages;
                     pageNumber++)
                {
                    cancellationToken
                        .ThrowIfCancellationRequested();

                    var pageStartSeq =
                        currentSince;

                    var response =
                        await ReadChangesAsync(
                            currentSince,
                            result.Limit,
                            cancellationToken);

                    var allChanges =
                        response.Changes ??
                        new List<OrthancChangeItem>();

                    result.PagesProcessed++;
                    result.TotalChanges +=
                        allChanges.Count;
                    result.Done =
                        response.Done;

                    var stableEvents =
                        GetStableStudies(
                            allChanges);

                    result.TotalStableEvents +=
                        stableEvents.Count;

                    var uniqueStudiesInPage =
                        stableEvents
                            .GroupBy(
                                x => x.OrthancStudyId,
                                StringComparer.OrdinalIgnoreCase)
                            .Select(
                                g =>
                                    g.OrderByDescending(
                                        x => x.Seq)
                                     .First())
                            .OrderBy(x => x.Seq)
                            .ToList();

                    foreach (var change in
                             uniqueStudiesInPage)
                    {
                        cancellationToken
                            .ThrowIfCancellationRequested();

                        if (!processedStudyIds.Add(
                                change.OrthancStudyId))
                        {
                            continue;
                        }

                        result.UniqueStudyCount++;

                        var item =
                            new PacsManualSyncItem
                            {
                                Seq =
                                    change.Seq,
                                OrthancStudyId =
                                    change.OrthancStudyId,
                                ChangeDate =
                                    change.Date,
                                Path =
                                    change.Path
                            };

                        try
                        {
                            var syncResult =
                                await _pacsStudyBL
                                    .SyncSingleStudyAsync(
                                        change.OrthancStudyId,
                                        cancellationToken);

                            item.Success =
                                syncResult.Success;
                            item.ErrorCode =
                                syncResult.ErrorCode;
                            item.ErrorMessage =
                                syncResult.Success
                                    ? null
                                    : syncResult.Message;
                            item.PacsStudyId =
                                syncResult.PacsStudyId;
                            item.AccessionNumber =
                                syncResult.AccessionNumber;
                            item.StudyInstanceUID =
                                syncResult.StudyInstanceUID;
                            item.ResultCDHAId =
                                syncResult.ResultCDHAId;
                            item.WorklistId =
                                syncResult.WorklistId;
                            item.MappedResultCDHA =
                                syncResult.MappedResultCDHA;
                            item.WorklistCompleted =
                                syncResult.WorklistCompleted;
                            item.SeriesCount =
                                syncResult.SeriesCount;
                            item.InstanceCount =
                                syncResult.InstanceCount;

                            if (syncResult.Success)
                            {
                                result.SuccessStudyCount++;
                            }
                            else
                            {
                                result.FailedStudyCount++;
                            }
                        }
                        catch (OperationCanceledException)
                            when (cancellationToken.IsCancellationRequested)
                        {
                            throw;
                        }
                        catch (Exception ex)
                        {
                            item.Success = false;
                            item.ErrorCode =
                                "MANUAL_STUDY_SYNC_EXCEPTION";
                            item.ErrorMessage =
                                ex.GetBaseException().Message;
                            result.FailedStudyCount++;
                        }

                        result.Items.Add(item);
                    }

                    if (response.Last <
                        currentSince)
                    {
                        throw new Exception(
                            "Orthanc /changes trả Last nhỏ hơn Since hiện tại.");
                    }

                    currentSince =
                        response.Last;

                    result.SuggestedNextSince =
                        currentSince;

                    if (response.Done)
                    {
                        result.Done = true;
                        result.HasMore = false;
                        break;
                    }

                    if (currentSince <=
                        pageStartSeq)
                    {
                        throw new Exception(
                            "Orthanc /changes không tiến sequence nhưng Done=false.");
                    }

                    if (pageNumber ==
                        result.MaxPages)
                    {
                        result.HasMore = true;
                    }
                }

                /*
                 * Reload checkpoint sau manual sync để chứng minh
                 * manual operation không mutate PacsSyncState.
                 */
                var stateAfter =
                    await _context.PacsSyncStates
                        .AsNoTracking()
                        .FirstOrDefaultAsync(
                            x =>
                                x.PacsConfigId ==
                                config.Id,
                            cancellationToken);

                result.CheckpointAfter =
                    stateAfter?.LastChangeSeq ?? 0;

                result.CheckpointChanged =
                    result.CheckpointBefore !=
                    result.CheckpointAfter;

                result.CanAdvanceCheckpoint =
                    result.FailedStudyCount == 0;

                result.Success =
                    result.FailedStudyCount == 0
                    &&
                    !result.CheckpointChanged;

                if (result.CheckpointChanged)
                {
                    result.ErrorCode =
                        "MANUAL_SYNC_MUTATED_CHECKPOINT";
                    result.Message =
                        "Manual sync phát hiện checkpoint đã thay đổi. " +
                        "Đây là invariant violation.";
                    return result;
                }

                if (result.Success)
                {
                    result.Message =
                        $"Manual sync thành công. " +
                        $"Studies={result.SuccessStudyCount}, " +
                        $"SuggestedNextSince={result.SuggestedNextSince}. " +
                        $"Checkpoint vẫn giữ {result.CheckpointAfter}.";
                }
                else
                {
                    result.ErrorCode ??=
                        "MANUAL_SYNC_PARTIAL_FAILURE";
                    result.Message =
                        $"Manual sync hoàn tất nhưng có lỗi. " +
                        $"Success={result.SuccessStudyCount}, " +
                        $"Failed={result.FailedStudyCount}. " +
                        $"Checkpoint vẫn giữ {result.CheckpointAfter}.";
                }

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
                result.ErrorCode ??=
                    "MANUAL_SYNC_FAILED";
                result.Message =
                    ex.GetBaseException().Message;

                /*
                 * Best effort snapshot.
                 * Không ghi bất cứ field nào vào PacsSyncState.
                 */
                try
                {
                    if (result.PacsConfigId.HasValue)
                    {
                        var state =
                            await _context.PacsSyncStates
                                .AsNoTracking()
                                .FirstOrDefaultAsync(
                                    x =>
                                        x.PacsConfigId ==
                                        result.PacsConfigId.Value,
                                    CancellationToken.None);

                        result.CheckpointAfter =
                            state?.LastChangeSeq ?? 0;

                        result.CheckpointChanged =
                            result.CheckpointBefore !=
                            result.CheckpointAfter;
                    }
                }
                catch
                {
                    // Không che lỗi gốc.
                }

                return result;
            }
            finally
            {
                _checkpointLock.Release();
            }
        }

        private async Task<OrthancChangesResponse>
            ReadChangesAsync(
                long since,
                int limit,
                CancellationToken cancellationToken)
        {
            var json =
                await _orthancClient
                    .GetChangesJsonAsync(
                        since,
                        limit,
                        cancellationToken);

            if (string.IsNullOrWhiteSpace(json))
            {
                throw new Exception(
                    "Orthanc không trả dữ liệu /changes.");
            }

            var response =
                JsonSerializer.Deserialize<
                    OrthancChangesResponse>(
                    json,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive =
                            true
                    });

            if (response == null)
            {
                throw new Exception(
                    "Không deserialize được Orthanc /changes.");
            }

            return response;
        }

        private static List<PacsStableStudyChange>
            GetStableStudies(
                IEnumerable<OrthancChangeItem> changes)
        {
            return changes
                .Where(
                    x =>
                        string.Equals(
                            x.ChangeType,
                            "StableStudy",
                            StringComparison.OrdinalIgnoreCase)
                        &&
                        string.Equals(
                            x.ResourceType,
                            "Study",
                            StringComparison.OrdinalIgnoreCase)
                        &&
                        !string.IsNullOrWhiteSpace(
                            x.Id))
                .OrderBy(x => x.Seq)
                .Select(
                    x =>
                        new PacsStableStudyChange
                        {
                            Seq = x.Seq,
                            OrthancStudyId =
                                x.Id!.Trim(),
                            Date = x.Date,
                            Path = x.Path
                        })
                .ToList();
        }

        private async Task<PacsSyncState>
            GetOrCreateSyncStateAsync(
                long pacsConfigId,
                CancellationToken cancellationToken)
        {
            if (pacsConfigId <= 0)
            {
                throw new Exception(
                    "PacsConfigId không hợp lệ.");
            }

            var state =
                await _context.PacsSyncStates
                    .FirstOrDefaultAsync(
                        x =>
                            x.PacsConfigId ==
                            pacsConfigId,
                        cancellationToken);

            if (state != null)
            {
                return state;
            }

            state =
                new PacsSyncState
                {
                    PacsConfigId =
                        pacsConfigId,
                    LastChangeSeq =
                        0,
                    CreatedOn =
                        DateTime.Now
                };

            _context.PacsSyncStates.Add(
                state);

            try
            {
                await _context.SaveChangesAsync(
                    cancellationToken);

                return state;
            }
            catch (DbUpdateException)
            {
                /*
                 * 04-02 đã có unique index PacsConfigId.
                 * Nếu hai request cùng tạo state, reload row thắng.
                 */
                _context.Entry(state)
                    .State =
                    EntityState.Detached;

                state =
                    await _context.PacsSyncStates
                        .FirstOrDefaultAsync(
                            x =>
                                x.PacsConfigId ==
                                pacsConfigId,
                            cancellationToken);

                if (state == null)
                {
                    throw;
                }

                return state;
            }
        }

        private static int NormalizeLimit(
            int limit)
        {
            if (limit <= 0)
            {
                return 100;
            }

            return Math.Min(
                limit,
                500);
        }

        private static int NormalizeMaxPages(
            int maxPages)
        {
            if (maxPages <= 0)
            {
                return 10;
            }

            return Math.Min(
                maxPages,
                1000);
        }
    }
}
