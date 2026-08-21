using Management.BL.Services;
using Management.Interfaces;
using Management.Models;
using Management.Models.SignServer;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Management.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ExternalSignController : ControllerBase
    {
        private readonly ISignServerService _signService;
        private readonly IConfiguration _cfg;
        public ExternalSignController(ISignServerService signService) => _signService = signService;

        /// Client từ mọi form post vào đây
        [HttpPost("sign-pdf")]
        [DisableRequestSizeLimit]
        public async Task<IActionResult> SignPdf(
            [FromForm] string signerCCCD,
            [FromForm] IFormFile file,
            [FromForm] SignFormType formType,
            [FromForm] string patientCode,
            [FromForm] string? patientName,
            [FromForm] string? serviceCode,
            [FromForm] string? doctorName,
            [FromForm] DateTime? performedAt,
            [FromForm] string? extra,
            [FromForm] string? targetText // optional override
        )
        {
            if (string.IsNullOrWhiteSpace(signerCCCD)) return BadRequest("signerCCCD is required.");
            if (file == null) return BadRequest("file is required.");
            if (!file.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                return BadRequest("Only PDF files are accepted.");

            var ctx = new SignContext
            {
                FormType = formType,
                PatientCode = patientCode,
                PatientName = patientName,
                ServiceCode = serviceCode,
                DoctorName = doctorName,
                PerformedAt = performedAt,
                Extra = extra
            };

            var raw = await _signService.SignPdfAsync(signerCCCD, file, ctx, targetText, HttpContext.RequestAborted);

            // chỉ trả response.data nếu có
            try
            {
                using var doc = JsonDocument.Parse(raw);
                if (doc.RootElement.TryGetProperty("data", out var data))
                    return Content(data.GetRawText(), "application/json");
                return Content(doc.RootElement.GetRawText(), "application/json");
            }
            catch
            {
                return Ok(raw); // không phải JSON
            }
        }

        [HttpGet("view-signed/{id}")]
        public async Task<IActionResult> ViewPdfSignedServer(string id)
        {
            var directUrl = _signService.GetSignedPdfViewUrl(id);
            return Redirect(directUrl);

            //// Nếu CẦN token → proxy bytes về cho client
            //var bytes = await _signService.FetchSignedPdfAsync(id, HttpContext.RequestAborted);
            //return File(bytes, "application/pdf");
        }

    }
}
