using Management.Models;
using Management.Models.Pacs;
using Management.Services.Pacs;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Text.Json;

namespace Management.BL.Pacs
{
    /// <summary>
    /// Shared Orthanc Study / Series / Instance sync core.
    ///
    /// PACS-XQ-04-05:
    /// - sync 1 Study + toàn bộ Series/Instances;
    /// - map Study vào LIS bằng canonical AccessionNumber;
    /// - ưu tiên PacsWorklistItems vì Worklist đã khóa ResultCDHAId;
    /// - fallback ResultCDHA chỉ khi không có Worklist;
    /// - update actual StudyInstanceUID + OrthancStudyId vào ResultCDHA;
    /// - chuyển Worklist Published(Status=1) -> Completed(Status=3);
    /// - idempotent khi sync lại cùng Study;
    /// - chưa cập nhật PacsSyncState checkpoint.
    /// </summary>
    public class PacsStudyBL
    {
        /*
         * PACS-XQ-PREPROD-01B-01
         *
         * Global process-local gate for every Study hierarchy mutation.
         *
         * IMPORTANT:
         * - this is NOT the PacsSyncState checkpoint lock;
         * - owning this gate never grants checkpoint ownership;
         * - SyncSingleStudyAsync waits because FAST PATH/manual sync must complete;
         * - TrySyncSingleStudyAsync is non-blocking so Recovery/Reconciliation can
         *   return STUDY_SYNC_BUSY and retry in a later cycle;
         * - all actual Study/Series/Instance writes remain in one canonical core.
         */
        private static readonly SemaphoreSlim
            _studySyncLock = new(1, 1);

        private readonly LABContext _context;
        private readonly PacsConfigBL _pacsConfigBL;
        private readonly OrthancClient _orthancClient;

        public PacsStudyBL(
            LABContext context,
            PacsConfigBL pacsConfigBL,
            OrthancClient orthancClient)
        {
            _context = context;
            _pacsConfigBL = pacsConfigBL;
            _orthancClient = orthancClient;
        }

        /// <summary>
        /// Canonical blocking entry point used by FAST PATH and manual sync.
        /// The caller waits until another Study mutation finishes.
        /// Checkpoint ownership remains outside PacsStudyBL.
        /// </summary>
        public async Task<PacsSingleStudySyncResult> SyncSingleStudyAsync(
            string orthancStudyId,
            CancellationToken cancellationToken = default)
        {
            await _studySyncLock.WaitAsync(
                cancellationToken);

            try
            {
                return await SyncSingleStudyCoreAsync(
                    orthancStudyId,
                    cancellationToken);
            }
            finally
            {
                _studySyncLock.Release();
            }
        }

        /// <summary>
        /// Non-blocking entry point reserved for Recovery/Reconciliation.
        /// If another Study mutation is running, return STUDY_SYNC_BUSY instead
        /// of waiting and let the reliability scheduler retry later.
        /// </summary>
        public async Task<PacsSingleStudySyncResult> TrySyncSingleStudyAsync(
            string orthancStudyId,
            CancellationToken cancellationToken = default)
        {
            var normalizedStudyId =
                NormalizeIdentity(orthancStudyId);

            var lockAcquired =
                await _studySyncLock.WaitAsync(
                    0,
                    cancellationToken);

            if (!lockAcquired)
            {
                return new PacsSingleStudySyncResult
                {
                    Success = false,
                    OrthancStudyId = normalizedStudyId,
                    ErrorCode = "STUDY_SYNC_BUSY",
                    Message =
                        "Đang có tiến trình đồng bộ PACS Study khác chạy. " +
                        "Recovery/Reconciliation sẽ thử lại ở chu kỳ sau."
                };
            }

            try
            {
                return await SyncSingleStudyCoreAsync(
                    orthancStudyId,
                    cancellationToken);
            }
            finally
            {
                _studySyncLock.Release();
            }
        }

        /// <summary>
        /// The only implementation that mutates PacsStudy/PacsSeries/PacsInstance
        /// and maps the Study into LIS. Callers must enter through one of the two
        /// lock-aware public methods above.
        /// </summary>
        private async Task<PacsSingleStudySyncResult> SyncSingleStudyCoreAsync(
            string orthancStudyId,
            CancellationToken cancellationToken = default)
        {
            var result = new PacsSingleStudySyncResult
            {
                Success = false,
                OrthancStudyId = NormalizeIdentity(orthancStudyId),
                MappingDeferred = false
            };

            if (string.IsNullOrWhiteSpace(result.OrthancStudyId))
            {
                result.ErrorCode = "INVALID_ORTHANC_STUDY_ID";
                result.Message = "OrthancStudyId không hợp lệ.";
                return result;
            }

            if (result.OrthancStudyId.Length > 64)
            {
                result.ErrorCode = "ORTHANC_STUDY_ID_TOO_LONG";
                result.Message = "OrthancStudyId dài quá 64 ký tự.";
                return result;
            }

            var config = await _pacsConfigBL.GetDefaultAsync();

            if (config == null ||
                !config.IsActive ||
                !config.IsDefault)
            {
                result.ErrorCode = "PACS_CONFIG_NOT_FOUND";
                result.Message =
                    "Chưa có PACS Server mặc định đang hoạt động.";
                return result;
            }

            _orthancClient.Configure(config);

            StudySnapshot snapshot;

            try
            {
                snapshot =
                    await ReadOrthancStudySnapshotAsync(
                        result.OrthancStudyId,
                        cancellationToken);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (PacsSyncValidationException ex)
            {
                result.ErrorCode = ex.ErrorCode;
                result.Message = ex.Message;
                return result;
            }
            catch (Exception ex)
            {
                result.ErrorCode = "ORTHANC_READ_FAILED";
                result.Message =
                    "Không đọc được Study/Series/Instance từ Orthanc: " +
                    ex.GetBaseException().Message;
                return result;
            }

            result.StudyInstanceUID =
                snapshot.StudyInstanceUID;

            result.AccessionNumber =
                snapshot.AccessionNumber;

            result.SeriesCount =
                snapshot.Series.Count;

            result.InstanceCount =
                snapshot.Series.Sum(
                    x => x.Instances.Count);

            await using var transaction =
                await _context.Database
                    .BeginTransactionAsync(
                        IsolationLevel.Serializable,
                        cancellationToken);

            try
            {
                var pacsStudy =
                    await GetOrCreateStudyAsync(
                        snapshot,
                        result,
                        cancellationToken);

                await UpsertStudyMetadataAsync(
                    pacsStudy,
                    snapshot,
                    cancellationToken);

                /*
                 * Cần PacsStudy.Id trước khi tạo child rows.
                 * Save lần đầu vẫn nằm trong cùng transaction.
                 */
                await _context.SaveChangesAsync(
                    cancellationToken);

                var existingSeries =
                    await _context.PacsSeries
                        .Where(
                            x =>
                                x.PacsStudyId ==
                                pacsStudy.Id)
                        .ToListAsync(
                            cancellationToken);

                var existingSeriesByOrthancId =
                    existingSeries
                        .Where(
                            x =>
                                !string.IsNullOrWhiteSpace(
                                    x.OrthancSeriesId))
                        .ToDictionary(
                            x => x.OrthancSeriesId,
                            StringComparer.Ordinal);

                var existingSeriesByUid =
                    existingSeries
                        .Where(
                            x =>
                                !string.IsNullOrWhiteSpace(
                                    x.SeriesInstanceUID))
                        .GroupBy(
                            x => x.SeriesInstanceUID!,
                            StringComparer.Ordinal)
                        .ToDictionary(
                            g => g.Key,
                            g => g.Single(),
                            StringComparer.Ordinal);

                var currentSeriesDbIds =
                    new HashSet<long>();

                var currentOrthancSeriesIds =
                    new HashSet<string>(
                        StringComparer.Ordinal);

                foreach (var seriesSnapshot in snapshot.Series)
                {
                    cancellationToken
                        .ThrowIfCancellationRequested();

                    currentOrthancSeriesIds.Add(
                        seriesSnapshot.OrthancSeriesId);

                    existingSeriesByOrthancId
                        .TryGetValue(
                            seriesSnapshot.OrthancSeriesId,
                            out var byOrthancSeriesId);

                    existingSeriesByUid
                        .TryGetValue(
                            seriesSnapshot.SeriesInstanceUID,
                            out var bySeriesUid);

                    if (byOrthancSeriesId != null &&
                        bySeriesUid != null &&
                        byOrthancSeriesId.Id !=
                        bySeriesUid.Id)
                    {
                        throw new PacsSyncValidationException(
                            "SERIES_IDENTITY_CONFLICT",
                            "OrthancSeriesId và SeriesInstanceUID " +
                            "đang trỏ tới hai PacsSeries khác nhau.");
                    }

                    var pacsSeries =
                        byOrthancSeriesId ??
                        bySeriesUid;

                    if (pacsSeries == null)
                    {
                        pacsSeries =
                            new PacsSeries
                            {
                                PacsStudyId =
                                    pacsStudy.Id,

                                OrthancSeriesId =
                                    seriesSnapshot
                                        .OrthancSeriesId,

                                OrthancParentStudyId =
                                    snapshot.OrthancStudyId,

                                SeriesInstanceUID =
                                    seriesSnapshot
                                        .SeriesInstanceUID,

                                CreatedOn = DateTime.Now
                            };

                        await _context.PacsSeries
                            .AddAsync(
                                pacsSeries,
                                cancellationToken);

                        result.CreatedSeriesCount++;
                    }
                    else
                    {
                        ValidateSeriesIdentity(
                            pacsSeries,
                            snapshot,
                            seriesSnapshot);

                        pacsSeries.UpdatedOn =
                            DateTime.Now;

                        result.UpdatedSeriesCount++;
                    }

                    ApplySeriesMetadata(
                        pacsSeries,
                        snapshot,
                        seriesSnapshot);

                    await _context.SaveChangesAsync(
                        cancellationToken);

                    currentSeriesDbIds.Add(
                        pacsSeries.Id);

                    await SyncInstancesAsync(
                        pacsStudy.Id,
                        pacsSeries,
                        seriesSnapshot,
                        result,
                        cancellationToken);
                }

                /*
                 * Xóa Series không còn tồn tại trong Orthanc Study.
                 * Xóa child Instances trước.
                 */
                var staleSeries =
                    existingSeries
                        .Where(
                            x =>
                                !currentOrthancSeriesIds
                                    .Contains(
                                        x.OrthancSeriesId))
                        .ToList();

                if (staleSeries.Count > 0)
                {
                    var staleSeriesIds =
                        staleSeries
                            .Select(x => x.Id)
                            .ToList();

                    var staleInstances =
                        await _context.PacsInstances
                            .Where(
                                x =>
                                    staleSeriesIds.Contains(
                                        x.PacsSeriesId))
                            .ToListAsync(
                                cancellationToken);

                    if (staleInstances.Count > 0)
                    {
                        result.RemovedInstanceCount +=
                            staleInstances.Count;

                        _context.PacsInstances
                            .RemoveRange(
                                staleInstances);
                    }

                    result.RemovedSeriesCount +=
                        staleSeries.Count;

                    _context.PacsSeries
                        .RemoveRange(
                            staleSeries);
                }

                pacsStudy.SeriesCount =
                    snapshot.Series.Count;

                pacsStudy.InstanceCount =
                    snapshot.Series.Sum(
                        x => x.Instances.Count);

                /*
                 * PACS-XQ-04-05:
                 * Chỉ map sau khi Study hierarchy đã sync thành công.
                 * Nếu mapping lỗi/conflict, transaction rollback toàn bộ lượt sync.
                 */
                var mapping =
                    await MapStudyByAccessionAsync(
                        pacsStudy,
                        snapshot,
                        cancellationToken);

                result.MappedResultCDHA =
                    mapping.Mapped;

                result.MappingSource =
                    mapping.MappingSource;

                result.MappingStatus =
                    mapping.MappingStatus;

                result.ResultCDHAId =
                    mapping.ResultCDHAId;

                result.WorklistId =
                    mapping.WorklistId;

                result.WorklistCompleted =
                    mapping.WorklistCompleted;

                result.PreviousWorklistStudyInstanceUID =
                    mapping.PreviousWorklistStudyInstanceUID;

                result.WorklistStudyInstanceUIDUpdated =
                    mapping.WorklistStudyInstanceUIDUpdated;

                pacsStudy.Status =
                    mapping.Mapped
                        ? "MAPPED"
                        : "CONTENT_SYNCED_UNMAPPED";

                pacsStudy.UpdatedOn =
                    DateTime.Now;

                await _context.SaveChangesAsync(
                    cancellationToken);

                await transaction.CommitAsync(
                    cancellationToken);

                result.Success = true;
                result.PacsStudyId =
                    pacsStudy.Id;

                if (mapping.Mapped)
                {
                    result.Message =
                        "Đã sync Study/Series/Instance và map vào LIS theo AccessionNumber. " +
                        (mapping.WorklistCompleted
                            ? "Worklist đã chuyển Completed."
                            : "Không có Worklist tương ứng; đã map trực tiếp ResultCDHA.");
                }
                else
                {
                    result.Message =
                        "Đã sync Study/Series/Instance nhưng chưa tìm thấy ResultCDHA/Worklist " +
                        "khớp AccessionNumber. Study được giữ ở trạng thái CONTENT_SYNCED_UNMAPPED.";
                }

                return result;
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                await transaction.RollbackAsync(
                    CancellationToken.None);
                throw;
            }
            catch (PacsSyncValidationException ex)
            {
                await transaction.RollbackAsync(
                    CancellationToken.None);

                result.ErrorCode =
                    ex.ErrorCode;

                result.Message =
                    ex.Message;

                return result;
            }
            catch (DbUpdateException ex)
            {
                await transaction.RollbackAsync(
                    CancellationToken.None);

                result.ErrorCode =
                    "DB_IDENTITY_CONFLICT";

                result.Message =
                    "Không lưu được PACS hierarchy do DB identity guard: " +
                    ex.GetBaseException().Message;

                return result;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(
                    CancellationToken.None);

                result.ErrorCode =
                    "STUDY_CONTENT_SYNC_FAILED";

                result.Message =
                    ex.GetBaseException().Message;

                return result;
            }
        }

        private async Task<StudySnapshot>
            ReadOrthancStudySnapshotAsync(
                string orthancStudyId,
                CancellationToken cancellationToken)
        {
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

            var patientTags =
                GetObject(
                    studyRoot,
                    "PatientMainDicomTags");

            var studyInstanceUid =
                RequireIdentity(
                    GetString(
                        mainTags,
                        "StudyInstanceUID"),
                    "STUDY_INSTANCE_UID_MISSING",
                    "Orthanc Study không có StudyInstanceUID.");

            var accessionNumber =
                NormalizeIdentity(
                    GetString(
                        mainTags,
                        "AccessionNumber"));

            ValidateMax64(
                studyInstanceUid,
                "STUDY_INSTANCE_UID_TOO_LONG");

            ValidateMax64(
                accessionNumber,
                "ACCESSION_TOO_LONG");

            var expectedSeriesIds =
                GetStringArray(
                    studyRoot,
                    "Series");

            var seriesBulkJson =
                await _orthancClient
                    .GetStudySeriesJsonAsync(
                        orthancStudyId,
                        cancellationToken);

            using var seriesBulkDocument =
                JsonDocument.Parse(
                    seriesBulkJson);

            if (seriesBulkDocument
                    .RootElement
                    .ValueKind !=
                JsonValueKind.Array)
            {
                throw new PacsSyncValidationException(
                    "ORTHANC_SERIES_JSON_INVALID",
                    "GET /studies/{id}/series không trả JSON array.");
            }

            var seriesById =
                new Dictionary<string, SeriesSnapshot>(
                    StringComparer.Ordinal);

            foreach (var item in
                     seriesBulkDocument
                        .RootElement
                        .EnumerateArray())
            {
                var series =
                    ParseSeriesSnapshot(
                        item,
                        orthancStudyId);

                if (seriesById.ContainsKey(
                        series.OrthancSeriesId))
                {
                    throw new PacsSyncValidationException(
                        "ORTHANC_DUPLICATE_SERIES_ID",
                        "Orthanc trả duplicate Series ID: " +
                        series.OrthancSeriesId);
                }

                seriesById.Add(
                    series.OrthancSeriesId,
                    series);
            }

            var finalSeries =
                new List<SeriesSnapshot>();

            foreach (var expectedSeriesId in
                     expectedSeriesIds)
            {
                if (!seriesById.TryGetValue(
                        expectedSeriesId,
                        out var series))
                {
                    /*
                     * Defensive fallback nếu bulk endpoint chưa trả đủ.
                     */
                    var singleSeriesJson =
                        await _orthancClient
                            .GetSeriesJsonAsync(
                                expectedSeriesId);

                    using var singleSeriesDocument =
                        JsonDocument.Parse(
                            singleSeriesJson);

                    series =
                        ParseSeriesSnapshot(
                            singleSeriesDocument
                                .RootElement,
                            orthancStudyId);
                }

                var instancesJson =
                    await _orthancClient
                        .GetSeriesInstancesJsonAsync(
                            series.OrthancSeriesId,
                            cancellationToken);

                using var instancesDocument =
                    JsonDocument.Parse(
                        instancesJson);

                if (instancesDocument
                        .RootElement
                        .ValueKind !=
                    JsonValueKind.Array)
                {
                    throw new PacsSyncValidationException(
                        "ORTHANC_INSTANCE_JSON_INVALID",
                        "GET /series/{id}/instances không trả JSON array.");
                }

                foreach (var instanceElement in
                         instancesDocument
                            .RootElement
                            .EnumerateArray())
                {
                    series.Instances.Add(
                        ParseInstanceSnapshot(
                            instanceElement,
                            series.OrthancSeriesId));
                }

                var duplicateOrthancInstances =
                    series.Instances
                        .GroupBy(
                            x => x.OrthancInstanceId,
                            StringComparer.Ordinal)
                        .FirstOrDefault(
                            g => g.Count() > 1);

                if (duplicateOrthancInstances != null)
                {
                    throw new PacsSyncValidationException(
                        "ORTHANC_DUPLICATE_INSTANCE_ID",
                        "Orthanc trả duplicate Instance ID: " +
                        duplicateOrthancInstances.Key);
                }

                var duplicateSop =
                    series.Instances
                        .GroupBy(
                            x => x.SOPInstanceUID,
                            StringComparer.Ordinal)
                        .FirstOrDefault(
                            g => g.Count() > 1);

                if (duplicateSop != null)
                {
                    throw new PacsSyncValidationException(
                        "ORTHANC_DUPLICATE_SOP_UID",
                        "Orthanc trả duplicate SOPInstanceUID: " +
                        duplicateSop.Key);
                }

                finalSeries.Add(
                    series);
            }

            var duplicateSeriesUid =
                finalSeries
                    .GroupBy(
                        x => x.SeriesInstanceUID,
                        StringComparer.Ordinal)
                    .FirstOrDefault(
                        g => g.Count() > 1);

            if (duplicateSeriesUid != null)
            {
                throw new PacsSyncValidationException(
                    "ORTHANC_DUPLICATE_SERIES_UID",
                    "Orthanc Study có duplicate SeriesInstanceUID: " +
                    duplicateSeriesUid.Key);
            }

            return new StudySnapshot
            {
                OrthancStudyId =
                    orthancStudyId,

                StudyInstanceUID =
                    studyInstanceUid,

                AccessionNumber =
                    accessionNumber,

                PatientId =
                    NormalizeText(
                        GetString(
                            patientTags,
                            "PatientID")),

                PatientName =
                    NormalizeDicomPersonName(
                        GetString(
                            patientTags,
                            "PatientName")),

                PatientBirthDate =
                    NormalizeText(
                        GetString(
                            patientTags,
                            "PatientBirthDate")),

                PatientSex =
                    NormalizeText(
                        GetString(
                            patientTags,
                            "PatientSex")),

                StudyDate =
                    NormalizeText(
                        GetString(
                            mainTags,
                            "StudyDate"))
                    ?? string.Empty,

                StudyTime =
                    NormalizeText(
                        GetString(
                            mainTags,
                            "StudyTime"))
                    ?? string.Empty,

                StudyDescription =
                    NormalizeText(
                        GetString(
                            mainTags,
                            "StudyDescription")),

                RequestedProcedureDescription =
                    NormalizeText(
                        GetString(
                            mainTags,
                            "RequestedProcedureDescription")),

                ReferringPhysicianName =
                    NormalizeDicomPersonName(
                        GetString(
                            mainTags,
                            "ReferringPhysicianName")),

                InstitutionName =
                    NormalizeText(
                        GetString(
                            mainTags,
                            "InstitutionName")),

                Modality =
                    NormalizeText(
                        GetString(
                            mainTags,
                            "ModalitiesInStudy"))
                    ??
                    finalSeries
                        .Select(x => x.Modality)
                        .FirstOrDefault(
                            x =>
                                !string.IsNullOrWhiteSpace(x)),

                RawJson =
                    studyJson,

                Series =
                    finalSeries
            };
        }

        private async Task<PacsStudy>
            GetOrCreateStudyAsync(
                StudySnapshot snapshot,
                PacsSingleStudySyncResult result,
                CancellationToken cancellationToken)
        {
            var byOrthancId =
                await _context.PacsStudies
                    .FirstOrDefaultAsync(
                        x =>
                            x.OrthancStudyId ==
                            snapshot.OrthancStudyId,
                        cancellationToken);

            var byStudyUid =
                await _context.PacsStudies
                    .FirstOrDefaultAsync(
                        x =>
                            x.StudyInstanceUID ==
                            snapshot.StudyInstanceUID,
                        cancellationToken);

            if (byOrthancId != null &&
                byStudyUid != null &&
                byOrthancId.Id !=
                byStudyUid.Id)
            {
                throw new PacsSyncValidationException(
                    "STUDY_IDENTITY_CONFLICT",
                    "OrthancStudyId và StudyInstanceUID " +
                    "đang trỏ tới hai PacsStudy khác nhau.");
            }

            var pacsStudy =
                byOrthancId ??
                byStudyUid;

            if (pacsStudy == null)
            {
                pacsStudy =
                    new PacsStudy
                    {
                        OrthancStudyId =
                            snapshot.OrthancStudyId,

                        StudyInstanceUID =
                            snapshot.StudyInstanceUID,

                        CreatedOn =
                            DateTime.Now
                    };

                await _context.PacsStudies
                    .AddAsync(
                        pacsStudy,
                        cancellationToken);

                result.Created = true;
            }
            else
            {
                if (!string.Equals(
                        NormalizeIdentity(
                            pacsStudy.OrthancStudyId),
                        snapshot.OrthancStudyId,
                        StringComparison.Ordinal))
                {
                    throw new PacsSyncValidationException(
                        "ORTHANC_IDENTITY_CONFLICT",
                        "StudyInstanceUID đã tồn tại nhưng OrthancStudyId không khớp.");
                }

                if (!string.Equals(
                        NormalizeIdentity(
                            pacsStudy.StudyInstanceUID),
                        snapshot.StudyInstanceUID,
                        StringComparison.Ordinal))
                {
                    throw new PacsSyncValidationException(
                        "DICOM_IDENTITY_CONFLICT",
                        "OrthancStudyId đã tồn tại nhưng StudyInstanceUID không khớp.");
                }

                pacsStudy.UpdatedOn =
                    DateTime.Now;

                result.Updated = true;
            }

            return pacsStudy;
        }

        private Task UpsertStudyMetadataAsync(
            PacsStudy pacsStudy,
            StudySnapshot snapshot,
            CancellationToken cancellationToken)
        {
            pacsStudy.OrthancStudyId =
                snapshot.OrthancStudyId;

            pacsStudy.StudyInstanceUID =
                snapshot.StudyInstanceUID;

            pacsStudy.AccessionNumber =
                snapshot.AccessionNumber;

            pacsStudy.PatientId =
                snapshot.PatientId;

            pacsStudy.PatientName =
                snapshot.PatientName;

            pacsStudy.PatientBirthDate =
                snapshot.PatientBirthDate;

            pacsStudy.PatientSex =
                snapshot.PatientSex;

            pacsStudy.StudyDate =
                snapshot.StudyDate;

            pacsStudy.StudyTime =
                snapshot.StudyTime;

            pacsStudy.StudyDescription =
                snapshot.StudyDescription;

            pacsStudy.RequestedProcedureDescription =
                snapshot.RequestedProcedureDescription;

            pacsStudy.ReferringPhysicianName =
                snapshot.ReferringPhysicianName;

            pacsStudy.InstitutionName =
                snapshot.InstitutionName;

            pacsStudy.Modality =
                snapshot.Modality;

            pacsStudy.SeriesCount =
                snapshot.Series.Count;

            pacsStudy.InstanceCount =
                snapshot.Series.Sum(
                    x => x.Instances.Count);

            pacsStudy.Status =
                "CONTENT_SYNCING";

            pacsStudy.RawJson =
                snapshot.RawJson;

            return Task.CompletedTask;
        }

        private async Task<StudyMappingOutcome>
            MapStudyByAccessionAsync(
                PacsStudy pacsStudy,
                StudySnapshot snapshot,
                CancellationToken cancellationToken)
        {
            var outcome =
                new StudyMappingOutcome
                {
                    MappingStatus =
                        "UNMAPPED"
                };

            var accessionNumber =
                NormalizeIdentity(
                    snapshot.AccessionNumber);

            if (string.IsNullOrWhiteSpace(
                    accessionNumber))
            {
                outcome.MappingStatus =
                    "ACCESSION_MISSING";

                return outcome;
            }

            /*
             * 1. Ưu tiên Worklist.
             *
             * Phase 03 đã enforce:
             * - unique AccessionNumber
             * - unique ResultCDHAId
             *
             * Vì vậy Worklist là correlation source mạnh nhất.
             */
            var worklistMatches =
                await _context.PacsWorklistItems
                    .Where(
                        x =>
                            x.AccessionNumber ==
                            accessionNumber)
                    .Take(2)
                    .ToListAsync(
                        cancellationToken);

            if (worklistMatches.Count > 1)
            {
                throw new PacsSyncValidationException(
                    "ACCESSION_AMBIGUOUS_WORKLIST",
                    "Có nhiều PacsWorklistItems cùng AccessionNumber.");
            }

            PacsWorklistItem? worklist =
                worklistMatches
                    .SingleOrDefault();

            ResultCDHA? resultCDHA =
                null;

            if (worklist != null)
            {
                resultCDHA =
                    await _context.ResultCDHAs
                        .FirstOrDefaultAsync(
                            x =>
                                x.Id ==
                                worklist.ResultCDHAId,
                            cancellationToken);

                if (resultCDHA == null)
                {
                    throw new PacsSyncValidationException(
                        "WORKLIST_RESULT_NOT_FOUND",
                        "Worklist có ResultCDHAId nhưng không tìm thấy ResultCDHA.");
                }

                var resultAccession =
                    NormalizeIdentity(
                        resultCDHA.AccessionNumber);

                if (!string.Equals(
                        resultAccession,
                        accessionNumber,
                        StringComparison.Ordinal))
                {
                    throw new PacsSyncValidationException(
                        "WORKLIST_RESULT_ACCESSION_MISMATCH",
                        "Worklist và ResultCDHA không cùng AccessionNumber.");
                }

                /*
                 * ResultCDHA chưa có unique index AccessionNumber,
                 * nên vẫn kiểm tra defensive để không map nhầm.
                 */
                var otherResultCount =
                    await _context.ResultCDHAs
                        .CountAsync(
                            x =>
                                x.AccessionNumber ==
                                    accessionNumber
                                &&
                                x.Id !=
                                    resultCDHA.Id,
                            cancellationToken);

                if (otherResultCount > 0)
                {
                    throw new PacsSyncValidationException(
                        "ACCESSION_AMBIGUOUS_RESULT",
                        "Có nhiều ResultCDHA cùng AccessionNumber.");
                }

                outcome.MappingSource =
                    "WORKLIST";
            }
            else
            {
                /*
                 * 2. Fallback ResultCDHA.
                 *
                 * Chỉ dùng nếu không có Worklist.
                 * Nếu có >1 candidate thì fail, không chọn đại row mới nhất.
                 */
                var resultMatches =
                    await _context.ResultCDHAs
                        .Where(
                            x =>
                                x.AccessionNumber ==
                                accessionNumber)
                        .OrderBy(x => x.Id)
                        .Take(2)
                        .ToListAsync(
                            cancellationToken);

                if (resultMatches.Count == 0)
                {
                    outcome.MappingStatus =
                        "ACCESSION_NOT_FOUND";

                    return outcome;
                }

                if (resultMatches.Count > 1)
                {
                    throw new PacsSyncValidationException(
                        "ACCESSION_AMBIGUOUS_RESULT",
                        "Có nhiều ResultCDHA cùng AccessionNumber.");
                }

                resultCDHA =
                    resultMatches[0];

                worklist =
                    await _context.PacsWorklistItems
                        .FirstOrDefaultAsync(
                            x =>
                                x.ResultCDHAId ==
                                resultCDHA.Id,
                            cancellationToken);

                if (worklist != null &&
                    !string.Equals(
                        NormalizeIdentity(
                            worklist.AccessionNumber),
                        accessionNumber,
                        StringComparison.Ordinal))
                {
                    throw new PacsSyncValidationException(
                        "RESULT_WORKLIST_ACCESSION_MISMATCH",
                        "ResultCDHA fallback có Worklist nhưng AccessionNumber không khớp.");
                }

                outcome.MappingSource =
                    worklist != null
                        ? "RESULTCDHA_WORKLIST"
                        : "RESULTCDHA";
            }

            /*
             * 3. Một ResultCDHA không được map sang hai Study khác nhau.
             */
            var otherMappedStudy =
                await _context.PacsStudies
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        x =>
                            x.ResultCDHAId ==
                                resultCDHA.Id
                            &&
                            x.Id !=
                                pacsStudy.Id,
                        cancellationToken);

            if (otherMappedStudy != null)
            {
                throw new PacsSyncValidationException(
                    "RESULT_ALREADY_MAPPED_TO_OTHER_STUDY",
                    "ResultCDHA đã được map tới một PacsStudy khác.");
            }

            if (pacsStudy.ResultCDHAId.HasValue &&
                pacsStudy.ResultCDHAId.Value !=
                    resultCDHA.Id)
            {
                throw new PacsSyncValidationException(
                    "STUDY_RESULT_MAPPING_CONFLICT",
                    "PacsStudy đã map tới ResultCDHA khác.");
            }

            /*
             * 4. Map PacsStudy.
             */
            pacsStudy.ResultCDHAId =
                resultCDHA.Id;

            pacsStudy.PatientDbId =
                resultCDHA.PatientId;

            pacsStudy.ServiceId =
                resultCDHA.ServiceId;

            /*
             * 5. Actual DICOM Study identity trở thành runtime identity
             * của ResultCDHA sau khi Study đã tới Orthanc.
             */
            resultCDHA.StudyInstanceUID =
                snapshot.StudyInstanceUID;

            resultCDHA.OrthancStudyId =
                snapshot.OrthancStudyId;

            resultCDHA.PacsStatus =
                "SYNCED";

            /*
             * 6. Worklist transition:
             * Published(1) -> Completed(3).
             *
             * StudyInstanceUID trong MWL file cũ có thể là UID deterministic
             * được tạo ở Phase 03. Khi Study thật đã nhận, DB Worklist giữ
             * actual StudyInstanceUID để phục vụ reconciliation/runtime.
             *
             * Không rewrite file MWL đã publish.
             */
            if (worklist != null)
            {
                outcome.PreviousWorklistStudyInstanceUID =
                    NormalizeIdentity(
                        worklist.StudyInstanceUID);

                worklist.PatientId =
                    resultCDHA.PatientId;

                worklist.ServiceId =
                    resultCDHA.ServiceId;

                worklist.OrthancStudyId =
                    snapshot.OrthancStudyId;

                worklist.StudyInstanceUID =
                    snapshot.StudyInstanceUID;

                worklist.Status = 3;

                if (!worklist.CompletedOn.HasValue)
                {
                    worklist.CompletedOn =
                        DateTime.Now;
                }

                worklist.UpdatedOn =
                    DateTime.Now;

                worklist.ErrorMessage =
                    null;

                outcome.WorklistId =
                    worklist.Id;

                outcome.WorklistCompleted =
                    true;

                outcome.WorklistStudyInstanceUIDUpdated =
                    !string.Equals(
                        outcome.PreviousWorklistStudyInstanceUID,
                        snapshot.StudyInstanceUID,
                        StringComparison.Ordinal);
            }

            outcome.Mapped =
                true;

            outcome.ResultCDHAId =
                resultCDHA.Id;

            outcome.MappingStatus =
                "MAPPED";

            return outcome;
        }

        private async Task SyncInstancesAsync(
            long pacsStudyId,
            PacsSeries pacsSeries,
            SeriesSnapshot snapshot,
            PacsSingleStudySyncResult result,
            CancellationToken cancellationToken)
        {
            var existingInstances =
                await _context.PacsInstances
                    .Where(
                        x =>
                            x.PacsSeriesId ==
                            pacsSeries.Id)
                    .ToListAsync(
                        cancellationToken);

            var byOrthancId =
                existingInstances
                    .Where(
                        x =>
                            !string.IsNullOrWhiteSpace(
                                x.OrthancInstanceId))
                    .ToDictionary(
                        x => x.OrthancInstanceId,
                        StringComparer.Ordinal);

            var bySopUid =
                existingInstances
                    .Where(
                        x =>
                            !string.IsNullOrWhiteSpace(
                                x.SOPInstanceUID))
                    .GroupBy(
                        x => x.SOPInstanceUID,
                        StringComparer.Ordinal)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Single(),
                        StringComparer.Ordinal);

            var currentOrthancIds =
                new HashSet<string>(
                    StringComparer.Ordinal);

            foreach (var item in snapshot.Instances)
            {
                cancellationToken
                    .ThrowIfCancellationRequested();

                currentOrthancIds.Add(
                    item.OrthancInstanceId);

                byOrthancId.TryGetValue(
                    item.OrthancInstanceId,
                    out var byId);

                bySopUid.TryGetValue(
                    item.SOPInstanceUID,
                    out var byUid);

                if (byId != null &&
                    byUid != null &&
                    byId.Id != byUid.Id)
                {
                    throw new PacsSyncValidationException(
                        "INSTANCE_IDENTITY_CONFLICT",
                        "OrthancInstanceId và SOPInstanceUID " +
                        "đang trỏ tới hai PacsInstance khác nhau.");
                }

                var entity =
                    byId ??
                    byUid;

                if (entity == null)
                {
                    entity =
                        new PacsInstance
                        {
                            PacsStudyId =
                                pacsStudyId,

                            PacsSeriesId =
                                pacsSeries.Id,

                            OrthancInstanceId =
                                item.OrthancInstanceId,

                            OrthancParentSeriesId =
                                pacsSeries.OrthancSeriesId,

                            SOPInstanceUID =
                                item.SOPInstanceUID,

                            CreatedOn =
                                DateTime.Now
                        };

                    await _context.PacsInstances
                        .AddAsync(
                            entity,
                            cancellationToken);

                    result.CreatedInstanceCount++;
                }
                else
                {
                    ValidateInstanceIdentity(
                        entity,
                        pacsStudyId,
                        pacsSeries,
                        item);

                    entity.UpdatedOn =
                        DateTime.Now;

                    result.UpdatedInstanceCount++;
                }

                entity.PacsStudyId =
                    pacsStudyId;

                entity.PacsSeriesId =
                    pacsSeries.Id;

                entity.OrthancInstanceId =
                    item.OrthancInstanceId;

                entity.OrthancParentSeriesId =
                    pacsSeries.OrthancSeriesId;

                entity.SOPInstanceUID =
                    item.SOPInstanceUID;

                entity.InstanceNumber =
                    item.InstanceNumber;

                entity.ImageIndex =
                    item.ImageIndex;

                entity.SopClassUID =
                    item.SopClassUID;

                entity.TransferSyntaxUID =
                    item.TransferSyntaxUID;

                entity.OrthancStatus =
                    item.OrthancStatus;

                entity.RawJson =
                    item.RawJson;
            }

            var stale =
                existingInstances
                    .Where(
                        x =>
                            !currentOrthancIds
                                .Contains(
                                    x.OrthancInstanceId))
                    .ToList();

            if (stale.Count > 0)
            {
                result.RemovedInstanceCount +=
                    stale.Count;

                _context.PacsInstances
                    .RemoveRange(
                        stale);
            }

            pacsSeries.InstanceCount =
                snapshot.Instances.Count;

            pacsSeries.UpdatedOn =
                DateTime.Now;

            await _context.SaveChangesAsync(
                cancellationToken);
        }

        private static SeriesSnapshot ParseSeriesSnapshot(
            JsonElement root,
            string expectedParentStudyId)
        {
            var orthancSeriesId =
                RequireIdentity(
                    GetString(
                        root,
                        "ID"),
                    "SERIES_ORTHANC_ID_MISSING",
                    "Orthanc Series không có ID.");

            ValidateMax64(
                orthancSeriesId,
                "SERIES_ORTHANC_ID_TOO_LONG");

            var parentStudyId =
                NormalizeIdentity(
                    GetString(
                        root,
                        "ParentStudy"))
                ??
                expectedParentStudyId;

            ValidateMax64(
                parentStudyId,
                "SERIES_PARENT_ID_TOO_LONG");

            if (!string.Equals(
                    parentStudyId,
                    expectedParentStudyId,
                    StringComparison.Ordinal))
            {
                throw new PacsSyncValidationException(
                    "SERIES_PARENT_MISMATCH",
                    "Orthanc Series không thuộc Study đang sync.");
            }

            var mainTags =
                GetObject(
                    root,
                    "MainDicomTags");

            var seriesInstanceUid =
                RequireIdentity(
                    GetString(
                        mainTags,
                        "SeriesInstanceUID"),
                    "SERIES_INSTANCE_UID_MISSING",
                    "Orthanc Series không có SeriesInstanceUID.");

            ValidateMax64(
                seriesInstanceUid,
                "SERIES_INSTANCE_UID_TOO_LONG");

            return new SeriesSnapshot
            {
                OrthancSeriesId =
                    orthancSeriesId,

                OrthancParentStudyId =
                    parentStudyId,

                SeriesInstanceUID =
                    seriesInstanceUid,

                SeriesNumber =
                    NormalizeText(
                        GetString(
                            mainTags,
                            "SeriesNumber")),

                SeriesDescription =
                    NormalizeText(
                        GetString(
                            mainTags,
                            "SeriesDescription")),

                Modality =
                    NormalizeText(
                        GetString(
                            mainTags,
                            "Modality")),

                ProtocolName =
                    NormalizeText(
                        GetString(
                            mainTags,
                            "ProtocolName")),

                OrthancStatus =
                    NormalizeText(
                        GetString(
                            root,
                            "Status"))
                    ?? "Synced",

                RawJson =
                    root.GetRawText()
            };
        }

        private static InstanceSnapshot ParseInstanceSnapshot(
            JsonElement root,
            string expectedParentSeriesId)
        {
            var orthancInstanceId =
                RequireIdentity(
                    GetString(
                        root,
                        "ID"),
                    "INSTANCE_ORTHANC_ID_MISSING",
                    "Orthanc Instance không có ID.");

            ValidateMax64(
                orthancInstanceId,
                "INSTANCE_ORTHANC_ID_TOO_LONG");

            var parentSeriesId =
                NormalizeIdentity(
                    GetString(
                        root,
                        "ParentSeries"))
                ??
                expectedParentSeriesId;

            ValidateMax64(
                parentSeriesId,
                "INSTANCE_PARENT_ID_TOO_LONG");

            if (!string.Equals(
                    parentSeriesId,
                    expectedParentSeriesId,
                    StringComparison.Ordinal))
            {
                throw new PacsSyncValidationException(
                    "INSTANCE_PARENT_MISMATCH",
                    "Orthanc Instance không thuộc Series đang sync.");
            }

            var mainTags =
                GetObject(
                    root,
                    "MainDicomTags");

            var sopInstanceUid =
                RequireIdentity(
                    GetString(
                        mainTags,
                        "SOPInstanceUID"),
                    "SOP_INSTANCE_UID_MISSING",
                    "Orthanc Instance không có SOPInstanceUID.");

            ValidateMax64(
                sopInstanceUid,
                "SOP_INSTANCE_UID_TOO_LONG");

            var metadata =
                GetObject(
                    root,
                    "Metadata");

            var sopClassUid =
                NormalizeIdentity(
                    GetString(
                        mainTags,
                        "SOPClassUID"))
                ??
                NormalizeIdentity(
                    GetString(
                        root,
                        "SopClassUid"));

            var transferSyntaxUid =
                NormalizeIdentity(
                    GetString(
                        metadata,
                        "TransferSyntax"))
                ??
                NormalizeIdentity(
                    GetString(
                        metadata,
                        "TransferSyntaxUID"))
                ??
                NormalizeIdentity(
                    GetString(
                        root,
                        "TransferSyntax"));

            ValidateMax64(
                sopClassUid,
                "SOP_CLASS_UID_TOO_LONG");

            ValidateMax64(
                transferSyntaxUid,
                "TRANSFER_SYNTAX_UID_TOO_LONG");

            return new InstanceSnapshot
            {
                OrthancInstanceId =
                    orthancInstanceId,

                OrthancParentSeriesId =
                    parentSeriesId,

                SOPInstanceUID =
                    sopInstanceUid,

                InstanceNumber =
                    NormalizeText(
                        GetString(
                            mainTags,
                            "InstanceNumber")),

                ImageIndex =
                    NormalizeText(
                        GetString(
                            root,
                            "IndexInSeries")),

                SopClassUID =
                    sopClassUid,

                TransferSyntaxUID =
                    transferSyntaxUid,

                OrthancStatus =
                    NormalizeText(
                        GetString(
                            root,
                            "Status"))
                    ?? "Synced",

                RawJson =
                    root.GetRawText()
            };
        }

        private static void ApplySeriesMetadata(
            PacsSeries entity,
            StudySnapshot study,
            SeriesSnapshot snapshot)
        {
            entity.PacsStudyId =
                entity.PacsStudyId;

            entity.OrthancSeriesId =
                snapshot.OrthancSeriesId;

            entity.OrthancParentStudyId =
                study.OrthancStudyId;

            entity.SeriesInstanceUID =
                snapshot.SeriesInstanceUID;

            entity.SeriesNumber =
                snapshot.SeriesNumber;

            entity.SeriesDescription =
                snapshot.SeriesDescription;

            entity.Modality =
                snapshot.Modality;

            entity.ProtocolName =
                snapshot.ProtocolName;

            entity.InstanceCount =
                snapshot.Instances.Count;

            entity.OrthancStatus =
                snapshot.OrthancStatus;

            entity.RawJson =
                snapshot.RawJson;
        }

        private static void ValidateSeriesIdentity(
            PacsSeries entity,
            StudySnapshot study,
            SeriesSnapshot snapshot)
        {
            if (entity.PacsStudyId <= 0)
            {
                throw new PacsSyncValidationException(
                    "SERIES_STUDY_ID_INVALID",
                    "PacsSeries đang có PacsStudyId không hợp lệ.");
            }

            if (!string.Equals(
                    NormalizeIdentity(
                        entity.OrthancSeriesId),
                    snapshot.OrthancSeriesId,
                    StringComparison.Ordinal))
            {
                throw new PacsSyncValidationException(
                    "ORTHANC_SERIES_IDENTITY_CONFLICT",
                    "SeriesInstanceUID đã tồn tại nhưng OrthancSeriesId không khớp.");
            }

            if (!string.Equals(
                    NormalizeIdentity(
                        entity.SeriesInstanceUID),
                    snapshot.SeriesInstanceUID,
                    StringComparison.Ordinal))
            {
                throw new PacsSyncValidationException(
                    "DICOM_SERIES_IDENTITY_CONFLICT",
                    "OrthancSeriesId đã tồn tại nhưng SeriesInstanceUID không khớp.");
            }

            if (!string.Equals(
                    NormalizeIdentity(
                        entity.OrthancParentStudyId),
                    study.OrthancStudyId,
                    StringComparison.Ordinal))
            {
                throw new PacsSyncValidationException(
                    "SERIES_PARENT_DB_MISMATCH",
                    "PacsSeries đang trỏ tới Orthanc Study khác.");
            }
        }

        private static void ValidateInstanceIdentity(
            PacsInstance entity,
            long pacsStudyId,
            PacsSeries pacsSeries,
            InstanceSnapshot snapshot)
        {
            if (entity.PacsStudyId != pacsStudyId ||
                entity.PacsSeriesId !=
                pacsSeries.Id)
            {
                throw new PacsSyncValidationException(
                    "INSTANCE_PARENT_DB_MISMATCH",
                    "PacsInstance đang thuộc Study/Series khác.");
            }

            if (!string.Equals(
                    NormalizeIdentity(
                        entity.OrthancInstanceId),
                    snapshot.OrthancInstanceId,
                    StringComparison.Ordinal))
            {
                throw new PacsSyncValidationException(
                    "ORTHANC_INSTANCE_IDENTITY_CONFLICT",
                    "SOPInstanceUID đã tồn tại nhưng OrthancInstanceId không khớp.");
            }

            if (!string.Equals(
                    NormalizeIdentity(
                        entity.SOPInstanceUID),
                    snapshot.SOPInstanceUID,
                    StringComparison.Ordinal))
            {
                throw new PacsSyncValidationException(
                    "DICOM_INSTANCE_IDENTITY_CONFLICT",
                    "OrthancInstanceId đã tồn tại nhưng SOPInstanceUID không khớp.");
            }
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
            JsonElement root,
            string propertyName)
        {
            if (root.ValueKind !=
                    JsonValueKind.Object ||
                !root.TryGetProperty(
                    propertyName,
                    out var value))
            {
                return null;
            }

            return JsonElementToString(
                value);
        }

        private static string? GetString(
            JsonElement? root,
            string propertyName)
        {
            if (!root.HasValue)
            {
                return null;
            }

            return GetString(
                root.Value,
                propertyName);
        }

        private static string? JsonElementToString(
            JsonElement value)
        {
            switch (value.ValueKind)
            {
                case JsonValueKind.String:
                    return value.GetString();

                case JsonValueKind.Number:
                case JsonValueKind.True:
                case JsonValueKind.False:
                    return value.ToString();

                case JsonValueKind.Null:
                case JsonValueKind.Undefined:
                    return null;

                default:
                    return value.ToString();
            }
        }

        private static List<string> GetStringArray(
            JsonElement root,
            string propertyName)
        {
            if (root.ValueKind !=
                    JsonValueKind.Object ||
                !root.TryGetProperty(
                    propertyName,
                    out var array) ||
                array.ValueKind !=
                    JsonValueKind.Array)
            {
                return new List<string>();
            }

            return array
                .EnumerateArray()
                .Select(JsonElementToString)
                .Where(
                    x =>
                        !string.IsNullOrWhiteSpace(x))
                .Select(
                    x =>
                        x!.Trim())
                .Distinct(
                    StringComparer.Ordinal)
                .ToList();
        }

        private static string RequireIdentity(
            string? value,
            string errorCode,
            string message)
        {
            var normalized =
                NormalizeIdentity(value);

            if (string.IsNullOrWhiteSpace(
                    normalized))
            {
                throw new PacsSyncValidationException(
                    errorCode,
                    message);
            }

            return normalized;
        }

        private static void ValidateMax64(
            string? value,
            string errorCode)
        {
            if (!string.IsNullOrWhiteSpace(value) &&
                value.Length > 64)
            {
                throw new PacsSyncValidationException(
                    errorCode,
                    "PACS/DICOM identity dài quá 64 ký tự.");
            }
        }

        private static string? NormalizeIdentity(
            string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }

        private static string? NormalizeText(
            string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }

        private static string? NormalizeDicomPersonName(
            string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return string.Join(
                " ",
                value
                    .Trim()
                    .Split(
                        '^',
                        StringSplitOptions
                            .RemoveEmptyEntries |
                        StringSplitOptions
                            .TrimEntries));
        }

        private sealed class StudySnapshot
        {
            public string OrthancStudyId { get; set; } =
                string.Empty;

            public string StudyInstanceUID { get; set; } =
                string.Empty;

            public string? AccessionNumber { get; set; }

            public string? PatientId { get; set; }

            public string? PatientName { get; set; }

            public string? PatientBirthDate { get; set; }

            public string? PatientSex { get; set; }

            public string StudyDate { get; set; } =
                string.Empty;

            public string StudyTime { get; set; } =
                string.Empty;

            public string? StudyDescription { get; set; }

            public string? RequestedProcedureDescription
            {
                get;
                set;
            }

            public string? ReferringPhysicianName
            {
                get;
                set;
            }

            public string? InstitutionName { get; set; }

            public string? Modality { get; set; }

            public string RawJson { get; set; } =
                string.Empty;

            public List<SeriesSnapshot> Series
            {
                get;
                set;
            } = new();
        }

        private sealed class SeriesSnapshot
        {
            public string OrthancSeriesId { get; set; } =
                string.Empty;

            public string OrthancParentStudyId
            {
                get;
                set;
            } = string.Empty;

            public string SeriesInstanceUID { get; set; } =
                string.Empty;

            public string? SeriesNumber { get; set; }

            public string? SeriesDescription { get; set; }

            public string? Modality { get; set; }

            public string? ProtocolName { get; set; }

            public string OrthancStatus { get; set; } =
                "Synced";

            public string RawJson { get; set; } =
                string.Empty;

            public List<InstanceSnapshot> Instances
            {
                get;
                set;
            } = new();
        }

        private sealed class InstanceSnapshot
        {
            public string OrthancInstanceId { get; set; } =
                string.Empty;

            public string OrthancParentSeriesId
            {
                get;
                set;
            } = string.Empty;

            public string SOPInstanceUID { get; set; } =
                string.Empty;

            public string? InstanceNumber { get; set; }

            public string? ImageIndex { get; set; }

            public string? SopClassUID { get; set; }

            public string? TransferSyntaxUID { get; set; }

            public string? OrthancStatus { get; set; }

            public string RawJson { get; set; } =
                string.Empty;
        }

        private sealed class StudyMappingOutcome
        {
            public bool Mapped { get; set; }

            public string MappingStatus { get; set; } =
                "UNMAPPED";

            public string? MappingSource { get; set; }

            public long? ResultCDHAId { get; set; }

            public long? WorklistId { get; set; }

            public bool WorklistCompleted { get; set; }

            public string? PreviousWorklistStudyInstanceUID
            {
                get;
                set;
            }

            public bool WorklistStudyInstanceUIDUpdated
            {
                get;
                set;
            }
        }

        private sealed class PacsSyncValidationException
            : Exception
        {
            public string ErrorCode { get; }

            public PacsSyncValidationException(
                string errorCode,
                string message)
                : base(message)
            {
                ErrorCode = errorCode;
            }
        }
    }
}