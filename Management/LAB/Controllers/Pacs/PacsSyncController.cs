using Management.BL.Pacs;
using Management.Models.Pacs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Management.Controllers.Pacs
{
    /// <summary>
    /// PACS operational endpoints.
    ///
    /// Checkpoint ownership remains in PacsChangeSyncBL.
    /// PREPROD reliability preview/recovery/reconciliation is delegated to
    /// PacsReliabilityBL and must not mutate PacsSyncState checkpoint unless
    /// explicitly owned by a future checkpoint operation.
    /// </summary>
    [Authorize]
    public class PacsSyncController : Controller
    {
        private readonly PacsChangeSyncBL
            _pacsChangeSyncBL;

        private readonly PacsReliabilityBL
            _pacsReliabilityBL;

        public PacsSyncController(
            PacsChangeSyncBL pacsChangeSyncBL,
            PacsReliabilityBL pacsReliabilityBL)
        {
            _pacsChangeSyncBL =
                pacsChangeSyncBL;

            _pacsReliabilityBL =
                pacsReliabilityBL;
        }

        [HttpGet]
        public async Task<IActionResult>
            PreviewChanges(
                long since = 0,
                int limit = 100,
                CancellationToken cancellationToken = default)
        {
            var result =
                await _pacsChangeSyncBL
                    .PreviewChangesAsync(
                        since,
                        limit,
                        cancellationToken);

            return Json(result);
        }

        [HttpGet]
        public async Task<IActionResult>
            GetCheckpointStatus(
                CancellationToken cancellationToken = default)
        {
            var result =
                await _pacsChangeSyncBL
                    .GetCheckpointStatusAsync(
                        cancellationToken);

            return Json(result);
        }

        [HttpPost]
        public async Task<IActionResult>
            SyncManualRange(
                [FromBody]
                PacsManualSyncRequest request,
                CancellationToken cancellationToken = default)
        {
            var result =
                await _pacsChangeSyncBL
                    .SyncManualRangeAsync(
                        request,
                        cancellationToken);

            return Json(result);
        }

        [HttpPost]
        public async Task<IActionResult>
            InitializeCheckpointToTail(
                [FromBody]
                PacsCheckpointInitializeRequest request,
                CancellationToken cancellationToken = default)
        {
            var result =
                await _pacsChangeSyncBL
                    .InitializeCheckpointToTailAsync(
                        request,
                        cancellationToken);

            return Json(result);
        }

        [HttpPost]
        public async Task<IActionResult>
            SyncFromCheckpoint(
                [FromBody]
                PacsSyncCheckpointRequest request,
                CancellationToken cancellationToken = default)
        {
            var result =
                await _pacsChangeSyncBL
                    .SyncFromCheckpointAsync(
                        request,
                        cancellationToken);

            return Json(result);
        }

        /// <summary>
        /// PACS-XQ-PREPROD-01C-02
        ///
        /// Read-only Completed Study Reconciliation preview.
        /// - only completed Worklists (Status=3);
        /// - mandatory StartDate / fail-closed;
        /// - compares PacsStudy metadata + actual child counts + Orthanc;
        /// - never calls canonical Study sync;
        /// - never updates LastReconciliationCheckedOn;
        /// - never updates PacsSyncState checkpoint.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult>
            PreviewReconciliation(
                int? batchSize = null,
                CancellationToken cancellationToken = default)
        {
            var result =
                await _pacsReliabilityBL
                    .PreviewReconciliationAsync(
                        batchSize,
                        cancellationToken);

            return Json(result);
        }

        /// <summary>
        /// PACS-XQ-PREPROD-01C-03
        ///
        /// Manual Reconciliation for one Completed Study.
        /// Only safe mismatch statuses may reach the canonical Study mutation.
        /// CompletedOn and PacsSyncState checkpoint must remain unchanged.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult>
            ReconcileCompletedStudy(
                [FromBody]
                PacsReconciliationWorklistRequest request,
                CancellationToken cancellationToken = default)
        {
            request ??=
                new PacsReconciliationWorklistRequest();

            var result =
                await _pacsReliabilityBL
                    .ReconcileCompletedStudyAsync(
                        request.PacsWorklistItemId,
                        cancellationToken);

            return Json(result);
        }

        /// <summary>
        /// PACS-XQ-PREPROD-01C-05
        ///
        /// Manual batch Reconciliation for completed Worklists.
        /// This endpoint does not check PacsSyncReconciliation:Enabled;
        /// that switch is reserved for automatic background integration in
        /// PREPROD-01D.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult>
            ReconcilePending(
                int? batchSize = null,
                CancellationToken cancellationToken = default)
        {
            var result =
                await _pacsReliabilityBL
                    .ReconcilePendingAsync(
                        batchSize,
                        cancellationToken);

            return Json(result);
        }

        /// <summary>
        /// PACS-XQ-PREPROD-01B-02
        ///
        /// Read-only Recovery preview.
        /// - only Published Worklists (Status=1);
        /// - Recovery StartDate is mandatory and fail-closed;
        /// - respects MinAgeMinutes + anti-starvation ordering;
        /// - reads Orthanc by AccessionNumber;
        /// - never calls Study sync;
        /// - never updates Worklist/PacsStudy/PacsSyncState.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult>
            PreviewRecovery(
                int? batchSize = null,
                CancellationToken cancellationToken = default)
        {
            var result =
                await _pacsReliabilityBL
                    .PreviewRecoveryAsync(
                        batchSize,
                        cancellationToken);

            return Json(result);
        }

        /// <summary>
        /// PACS-XQ-PREPROD-01B-03
        ///
        /// Manual Missing Study Recovery for one Worklist.
        /// The method revalidates the candidate immediately before mutation and
        /// delegates the only hierarchy write to PacsStudyBL.TrySyncSingleStudyAsync().
        /// It does not own or update PacsSyncState checkpoint.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult>
            RecoverWorklist(
                [FromBody]
                PacsRecoveryWorklistRequest request,
                CancellationToken cancellationToken = default)
        {
            request ??=
                new PacsRecoveryWorklistRequest();

            var result =
                await _pacsReliabilityBL
                    .RecoverWorklistAsync(
                        request.PacsWorklistItemId,
                        cancellationToken);

            return Json(result);
        }

        /// <summary>
        /// PACS-XQ-PREPROD-01B-04
        ///
        /// Manual batch Recovery for pending Worklists. This endpoint does not
        /// check PacsSyncRecovery:Enabled; that switch is reserved for the
        /// automatic Background lane in PREPROD-01D.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult>
            RecoverPending(
                int? batchSize = null,
                CancellationToken cancellationToken = default)
        {
            var result =
                await _pacsReliabilityBL
                    .RecoverPendingAsync(
                        batchSize,
                        cancellationToken);

            return Json(result);
        }
    }
}
