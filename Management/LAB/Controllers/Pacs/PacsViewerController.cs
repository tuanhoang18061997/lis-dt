using Management.BL.Pacs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Management.Controllers.Pacs
{
    [Authorize]
    public class PacsViewerController : Controller
    {
        private readonly ILogger<PacsViewerController>
            _logger;

        private readonly PacsViewerBL
            _pacsViewerBL;

        public PacsViewerController(
            ILogger<PacsViewerController> logger,
            PacsViewerBL pacsViewerBL)
        {
            _logger = logger;
            _pacsViewerBL = pacsViewerBL;
        }

        [HttpGet]
        public async Task<IActionResult>
            GetOhifViewerUrl(
                long resultCDHAId)
        {
            if (resultCDHAId <= 0)
            {
                return Json(new
                {
                    success = false,
                    message =
                        "Thiếu ResultCDHAId."
                });
            }

            try
            {
                var url =
                    await _pacsViewerBL
                        .GetOhifViewerUrlByResultCDHAIdAsync(
                            resultCDHAId);

                if (string.IsNullOrWhiteSpace(
                    url))
                {
                    return Json(new
                    {
                        success = false,
                        message =
                            "Dịch vụ này chưa có ảnh PACS hoặc chưa sync StudyInstanceUID."
                    });
                }

                return Json(new
                {
                    success = true,
                    viewerUrl = url
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "GetOhifViewerUrl failed. ResultCDHAId: {ResultCDHAId}",
                    resultCDHAId);

                return StatusCode(
                    500,
                    new
                    {
                        success = false,
                        message =
                            "Không thể lấy link OHIF Viewer. Vui lòng kiểm tra PACS."
                    });
            }
        }

        [HttpGet]
        public async Task<IActionResult>
            GetPacsSeriesThumbnails(
                long resultCDHAId)
        {
            if (resultCDHAId <= 0)
            {
                return Json(new
                {
                    success = false,
                    message =
                        "Thiếu ResultCDHAId."
                });
            }

            try
            {
                var result =
                    await _pacsViewerBL
                        .GetSeriesThumbnailsByResultCDHAIdAsync(
                            resultCDHAId);

                if (result.Items != null)
                {
                    foreach (var item in
                        result.Items)
                    {
                        if (!string.IsNullOrWhiteSpace(
                            item.OrthancInstanceId))
                        {
                            /*
                             * Dùng relative URL để luôn
                             * same-origin với Project A.
                             */
                            item.ThumbnailUrl =
                                Url.Action(
                                    action:
                                        nameof(
                                            GetPacsThumbnail),
                                    controller:
                                        "PacsViewer",
                                    values:
                                        new
                                        {
                                            orthancInstanceId =
                                                item.OrthancInstanceId
                                        }
                                );
                        }
                    }
                }

                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "GetPacsSeriesThumbnails failed. ResultCDHAId: {ResultCDHAId}",
                    resultCDHAId);

                return StatusCode(
                    500,
                    new
                    {
                        success = false,
                        message =
                            "Không thể lấy danh sách Series PACS."
                    });
            }
        }

        [HttpGet]
        public async Task<IActionResult>
            GetPacsThumbnail(
                string orthancInstanceId)
        {
            if (string.IsNullOrWhiteSpace(
                orthancInstanceId))
            {
                return NotFound();
            }

            try
            {
                var bytes =
                    await _pacsViewerBL
                        .GetOrthancInstancePreviewAsync(
                            orthancInstanceId);

                if (bytes == null ||
                    bytes.Length == 0)
                {
                    return NotFound();
                }

                return File(
                    bytes,
                    "image/jpeg"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "GetPacsThumbnail failed. OrthancInstanceId: {OrthancInstanceId}",
                    orthancInstanceId);

                return NotFound();
            }
        }
    }
}