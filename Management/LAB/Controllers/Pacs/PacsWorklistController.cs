using Management.BL.Pacs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Management.Controllers.Pacs
{
    [Authorize]
    public class PacsWorklistController : Controller
    {
        private readonly ILogger<PacsWorklistController>
            _logger;

        private readonly PacsWorklistBL
            _pacsWorklistBL;

        public PacsWorklistController(
            ILogger<PacsWorklistController> logger,
            PacsWorklistBL pacsWorklistBL)
        {
            _logger = logger;
            _pacsWorklistBL = pacsWorklistBL;
        }

        /// <summary>
        /// PACS-XQ-03-04 validation endpoint.
        ///
        /// Ensures ResultCDHA has a canonical AccessionNumber.
        /// Does not create PacsWorklistItems and does not publish MWL.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult>
            EnsureAccessionNumber(
                [FromBody] PacsEnsureAccessionRequest request)
        {
            if (request == null ||
                request.ResultCDHAId <= 0 ||
                string.IsNullOrWhiteSpace(
                    request.CategoryCode))
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "ResultCDHAId và CategoryCode là bắt buộc."
                });
            }

            try
            {
                var result =
                    await _pacsWorklistBL
                        .EnsureAccessionNumberAsync(
                            request.ResultCDHAId,
                            request.CategoryCode);

                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "EnsureAccessionNumber failed. ResultCDHAId={ResultCDHAId}, CategoryCode={CategoryCode}",
                    request.ResultCDHAId,
                    request.CategoryCode);

                return StatusCode(
                    500,
                    new
                    {
                        success = false,
                        message =
                            "Không thể đảm bảo AccessionNumber PACS."
                    });
            }
        }

        /// <summary>
        /// PACS-XQ-03-05 validation endpoint.
        ///
        /// Generates .dump and .wl in staging only, runs dump2dcm
        /// and dcmdump, validates required MWL tags, but does not
        /// publish the file to Orthanc and does not create
        /// PacsWorklistItems.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult>
            GenerateMwlValidationFixture(
                [FromBody] PacsGenerateMwlValidationRequest request)
        {
            if (request == null ||
                request.ResultCDHAId <= 0 ||
                string.IsNullOrWhiteSpace(
                    request.CategoryCode))
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "ResultCDHAId và CategoryCode là bắt buộc."
                });
            }

            try
            {
                var result =
                    await _pacsWorklistBL
                        .GenerateMwlValidationFixtureAsync(
                            request.ResultCDHAId,
                            request.CategoryCode,
                            request.ScheduledAt);

                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "GenerateMwlValidationFixture failed. ResultCDHAId={ResultCDHAId}, CategoryCode={CategoryCode}",
                    request.ResultCDHAId,
                    request.CategoryCode);

                return StatusCode(
                    500,
                    new
                    {
                        success = false,
                        message =
                            "Không thể tạo MWL validation fixture."
                    });
            }
        }


        /// <summary>
        /// PACS-XQ-03-06.
        /// Creates and controlled-publishes exactly one ResultCDHA Worklist.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult>
            CreateForResultCDHA(
                [FromBody] PacsCreateResultWorklistRequest request)
        {
            if (request == null ||
                request.ResultCDHAId <= 0 ||
                string.IsNullOrWhiteSpace(
                    request.CategoryCode))
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "ResultCDHAId và CategoryCode là bắt buộc."
                });
            }

            try
            {
                var result =
                    await _pacsWorklistBL
                        .CreateWorklistForResultCDHAAsync(
                            request.ResultCDHAId,
                            request.CategoryCode,
                            request.ScheduledAt,
                            GetCurrentUserId());

                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "CreateForResultCDHA failed. ResultCDHAId={ResultCDHAId}, CategoryCode={CategoryCode}",
                    request.ResultCDHAId,
                    request.CategoryCode);

                return StatusCode(
                    500,
                    new
                    {
                        success = false,
                        message =
                            "Không thể tạo Result-level PACS Worklist."
                    });
            }
        }



        /// <summary>
        /// PACS-XQ-03-07.
        /// Creates/reuses Worklists for all ResultCDHA rows of one patient
        /// in the requested CategoryCode. Does not hook XQ workflow yet.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult>
            CreateForPatient(
                [FromBody] PacsCreatePatientWorklistRequest request)
        {
            if (request == null ||
                request.PatientId <= 0 ||
                string.IsNullOrWhiteSpace(
                    request.CategoryCode))
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "PatientId và CategoryCode là bắt buộc."
                });
            }

            try
            {
                var result =
                    await _pacsWorklistBL
                        .CreateWorklistForPatientAsync(
                            request.PatientId,
                            request.CategoryCode,
                            request.ScheduledAt,
                            GetCurrentUserId());

                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "CreateForPatient failed. PatientId={PatientId}, CategoryCode={CategoryCode}",
                    request.PatientId,
                    request.CategoryCode);

                return StatusCode(
                    500,
                    new
                    {
                        success = false,
                        message =
                            "Không thể tạo Patient-level PACS Worklist."
                    });
            }
        }

        private long? GetCurrentUserId()
        {
            try
            {
                var userIdText =
                    User?.Claims?
                        .FirstOrDefault(
                            x => x.Type == "UserId")
                        ?.Value;

                if (long.TryParse(
                    userIdText,
                    out var userId))
                {
                    return userId;
                }

                userIdText =
                    HttpContext.Session
                        .GetString("UserId");

                if (long.TryParse(
                    userIdText,
                    out userId))
                {
                    return userId;
                }
            }
            catch
            {
                // CreatedBy/UpdatedBy are optional audit fields.
            }

            return null;
        }

    }

    public class PacsEnsureAccessionRequest
    {
        public long ResultCDHAId { get; set; }

        public string CategoryCode { get; set; } =
            string.Empty;
    }

    public class PacsGenerateMwlValidationRequest
    {
        public long ResultCDHAId { get; set; }

        public string CategoryCode { get; set; } =
            string.Empty;

        public DateTime? ScheduledAt { get; set; }
    }



    public class PacsCreateResultWorklistRequest
    {
        public long ResultCDHAId { get; set; }

        public string CategoryCode { get; set; } =
            string.Empty;

        public DateTime? ScheduledAt { get; set; }
    }



    public class PacsCreatePatientWorklistRequest
    {
        public long PatientId { get; set; }

        public string CategoryCode { get; set; } =
            string.Empty;

        public DateTime? ScheduledAt { get; set; }
    }

}