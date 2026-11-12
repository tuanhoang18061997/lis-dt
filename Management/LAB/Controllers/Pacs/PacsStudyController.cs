using Management.BL.Pacs;
using Management.Models.Pacs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Management.Controllers.Pacs
{
    /// <summary>
    /// PACS-XQ-04-03:
    /// Narrow manual endpoint chỉ để runtime-test Single Study Sync Core.
    ///
    /// Các API changes/checkpoint/background đầy đủ thuộc 04-06/07/08.
    /// </summary>
    [Authorize]
    public class PacsStudyController : Controller
    {
        private readonly PacsStudyBL _pacsStudyBL;

        public PacsStudyController(
            PacsStudyBL pacsStudyBL)
        {
            _pacsStudyBL = pacsStudyBL;
        }

        [HttpPost]
        public async Task<IActionResult> SyncSingleStudy(
            [FromBody] PacsSingleStudySyncRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request == null ||
                string.IsNullOrWhiteSpace(
                    request.OrthancStudyId))
            {
                return Json(
                    new PacsSingleStudySyncResult
                    {
                        Success = false,
                        ErrorCode =
                            "INVALID_ORTHANC_STUDY_ID",
                        Message =
                            "OrthancStudyId là bắt buộc."
                    });
            }

            var result =
                await _pacsStudyBL
                    .SyncSingleStudyAsync(
                        request.OrthancStudyId,
                        cancellationToken);

            return Json(result);
        }
    }
}
