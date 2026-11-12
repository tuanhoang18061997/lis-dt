using Management.Interfaces;
using Management.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace Management.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GetResultPdfFileController : ControllerBase
    {
        private const long MaxSignedPdfBytes = 50L * 1024 * 1024;
        private static readonly TimeSpan SignedPdfTimeout = TimeSpan.FromSeconds(10);

        private readonly IWebHostEnvironment _environment;
        private readonly LABContext _db;
        private readonly ISignServerService _signService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<GetResultPdfFileController> _logger;

        public GetResultPdfFileController(
            IWebHostEnvironment environment,
            LABContext db,
            ISignServerService signService,
            IHttpClientFactory httpClientFactory,
            ILogger<GetResultPdfFileController> logger)
        {
            _environment = environment;
            _db = db;
            _signService = signService;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        // Giữ nguyên endpoint GET /api/GetResultPdfFile?sid=...
        [HttpGet]
        public async Task<IActionResult> GetResultPdfFile([FromQuery] string? sid)
        {
            if (string.IsNullOrWhiteSpace(sid))
                return BadRequest();

            sid = sid.Trim();
            var separatorIndex = sid.IndexOf('-');

            // Ngăn truy cập file ngoài thư mục wwwroot/pdf (path traversal).
            if (separatorIndex <= 0 || separatorIndex == sid.Length - 1 ||
                !Regex.IsMatch(sid, @"^[A-Za-z0-9_-]{1,128}$", RegexOptions.CultureInvariant))
                return BadRequest();

            // 1. Ưu tiên nội dung PDF ký số còn hiệu lực.
            var signedPdf = await TryReadSignedPdfAsync(sid, HttpContext.RequestAborted);
            if (signedPdf != null)
                return File(signedPdf, "application/pdf");

            // 2. Fallback: giữ nguyên quy tắc tên file và mã lỗi cũ.
            var categoryCode = sid.Substring(0, separatorIndex);
            var folder = Path.Combine(_environment.WebRootPath, "pdf", categoryCode);
            var filePath = Path.Combine(folder, sid + ".pdf");

            if (System.IO.File.Exists(filePath))
            {
                var bytes = await System.IO.File.ReadAllBytesAsync(filePath, HttpContext.RequestAborted);
                return File(bytes, "application/pdf");
            }

            return BadRequest();
        }

        private async Task<byte[]?> TryReadSignedPdfAsync(string sid, CancellationToken requestCancellationToken)
        {
            try
            {
                // KeyResultForHis là key của PDF, không phải Patient.Sid.
                // XN có thể có nhiều dòng ResultXN dùng chung một key.
                long? signStoreId;
                if (sid.StartsWith("xn-", StringComparison.OrdinalIgnoreCase))
                {
                    signStoreId = await _db.ResultXNs
                        .AsNoTracking()
                        .Where(x => x.Active && x.KeyResultForHis == sid &&
                                    x.SignStatus == 1 && x.SignStoreId.HasValue &&
                                    x.SignStoreId.Value > 0)
                        .OrderByDescending(x => x.UpdateTime)
                        .ThenByDescending(x => x.Id)
                        .Select(x => x.SignStoreId)
                        .FirstOrDefaultAsync(requestCancellationToken);
                }
                else
                {
                    signStoreId = await _db.ResultCDHAs
                        .AsNoTracking()
                        .Where(x => x.Active && x.KeyResultForHis == sid &&
                                    x.SignStatus == 1 && x.SignStoreId.HasValue &&
                                    x.SignStoreId.Value > 0)
                        .OrderByDescending(x => x.UpdateTime)
                        .ThenByDescending(x => x.Id)
                        .Select(x => x.SignStoreId)
                        .FirstOrDefaultAsync(requestCancellationToken);
                }

                if (!signStoreId.HasValue)
                    return null;

                // Hàm hiện có chỉ trả URL. LIS tải bytes về để HIS vẫn nhận HTTP 200/PDF.
                var signedPdfUrl = _signService.GetSignedPdfViewUrl(signStoreId.Value.ToString());
                if (!Uri.TryCreate(signedPdfUrl, UriKind.Absolute, out var uri) ||
                    (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                {
                    _logger.LogWarning("Signed PDF URL invalid. SignStoreId={SignStoreId}", signStoreId.Value);
                    return null;
                }

                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(requestCancellationToken);
                timeoutCts.CancelAfter(SignedPdfTimeout);
                var ct = timeoutCts.Token;

                using var client = _httpClientFactory.CreateClient("mysign");
                using var response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, ct);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Signed PDF unavailable. SignStoreId={SignStoreId}, Status={Status}",
                        signStoreId.Value, (int)response.StatusCode);
                    return null;
                }

                if (response.Content.Headers.ContentLength > MaxSignedPdfBytes)
                {
                    _logger.LogWarning("Signed PDF too large. SignStoreId={SignStoreId}", signStoreId.Value);
                    return null;
                }

                await using var source = await response.Content.ReadAsStreamAsync(ct);
                using var buffer = new MemoryStream();
                var chunk = new byte[64 * 1024];
                int count;
                while ((count = await source.ReadAsync(chunk.AsMemory(0, chunk.Length), ct)) > 0)
                {
                    if (buffer.Length + count > MaxSignedPdfBytes)
                    {
                        _logger.LogWarning("Signed PDF stream exceeded limit. SignStoreId={SignStoreId}", signStoreId.Value);
                        return null;
                    }

                    await buffer.WriteAsync(chunk.AsMemory(0, count), ct);
                }

                var pdf = buffer.ToArray();
                // Tránh trả HTML/JSON (ví dụ trang đăng nhập) dưới MIME application/pdf.
                if (pdf.Length < 5 || pdf[0] != (byte)'%' || pdf[1] != (byte)'P' ||
                    pdf[2] != (byte)'D' || pdf[3] != (byte)'F' || pdf[4] != (byte)'-')
                {
                    _logger.LogWarning("Signed PDF response was not a PDF. SignStoreId={SignStoreId}", signStoreId.Value);
                    return null;
                }

                return pdf;
            }
            catch (Exception ex) when (!requestCancellationToken.IsCancellationRequested)
            {
                // DB/SignServer lỗi không được làm mất quyền xem PDF gốc.
                _logger.LogWarning(ex, "Unable to obtain signed PDF; using legacy PDF fallback");
                return null;
            }
        }
    }
}
