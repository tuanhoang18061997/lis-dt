using Management.Models;
using Management.Models.Pacs;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Management.BL.Pacs
{
    public class PacsConfigBL
    {
        private readonly LABContext _context;
        private readonly IHttpClientFactory _httpClientFactory;

        // Tránh nhiều request Test Connection ghi đè/chồng log cùng lúc.
        private static readonly SemaphoreSlim _connectionLogLock = new SemaphoreSlim(1, 1);

        public PacsConfigBL(LABContext context, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<List<PacsConfig>> GetAllAsync()
        {
            return await _context.PacsConfigs
                .OrderByDescending(x => x.IsDefault)
                .ThenByDescending(x => x.IsActive)
                .ThenBy(x => x.Name)
                .ToListAsync();
        }

        public async Task<PacsConfig> GetByIdAsync(int id)
        {
            return await _context.PacsConfigs
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<PacsConfig> GetDefaultAsync()
        {
            return await _context.PacsConfigs
                .FirstOrDefaultAsync(x => x.IsDefault == true && x.IsActive == true);
        }

        public async Task<object> SaveAsync(PacsConfig model, int? userId = null)
        {
            if (model == null)
            {
                return new
                {
                    success = false,
                    message = "Dữ liệu cấu hình PACS không hợp lệ."
                };
            }

            if (string.IsNullOrWhiteSpace(model.Code))
            {
                return new
                {
                    success = false,
                    message = "Vui lòng nhập mã PACS server."
                };
            }

            if (string.IsNullOrWhiteSpace(model.Name))
            {
                return new
                {
                    success = false,
                    message = "Vui lòng nhập tên PACS server."
                };
            }

            if (string.IsNullOrWhiteSpace(model.BaseUrl))
            {
                return new
                {
                    success = false,
                    message = "Vui lòng nhập Base URL Orthanc."
                };
            }

            model.Code = model.Code.Trim();
            model.Name = model.Name.Trim();
            model.ServerType = string.IsNullOrWhiteSpace(model.ServerType)
                ? "Orthanc"
                : model.ServerType.Trim();

            model.BaseUrl = NormalizeBaseUrl(model.BaseUrl);

            if (!string.IsNullOrWhiteSpace(model.DicomWebRoot))
            {
                model.DicomWebRoot = NormalizeDicomWebRoot(model.DicomWebRoot);
            }
            else
            {
                model.DicomWebRoot = "/dicom-web/";
            }

            if (model.DicomPort == null || model.DicomPort <= 0)
            {
                model.DicomPort = 4242;
            }

            if (model.SyncIntervalSeconds <= 0)
            {
                model.SyncIntervalSeconds = 60;
            }

            var isDuplicateCode = await _context.PacsConfigs
                .AnyAsync(x => x.Code == model.Code && x.Id != model.Id);

            if (isDuplicateCode)
            {
                return new
                {
                    success = false,
                    message = "Mã PACS server đã tồn tại."
                };
            }

            PacsConfig entity;

            if (model.Id > 0)
            {
                entity = await _context.PacsConfigs.FirstOrDefaultAsync(x => x.Id == model.Id);

                if (entity == null)
                {
                    return new
                    {
                        success = false,
                        message = "Không tìm thấy cấu hình PACS cần cập nhật."
                    };
                }

                entity.UpdatedOn = DateTime.Now;
                entity.UpdatedBy = userId;
            }
            else
            {
                entity = new PacsConfig
                {
                    CreatedOn = DateTime.Now,
                    CreatedBy = userId,
                    LastStatus = "Unknown"
                };

                await _context.PacsConfigs.AddAsync(entity);
            }

            entity.Code = model.Code;
            entity.Name = model.Name;
            entity.ServerType = model.ServerType;

            entity.BaseUrl = model.BaseUrl;
            entity.ApiUsername = model.ApiUsername;
            entity.ApiPassword = model.ApiPassword;

            entity.DicomAETitle = model.DicomAETitle;
            entity.DicomHost = model.DicomHost;
            entity.DicomPort = model.DicomPort;

            entity.DicomWebRoot = model.DicomWebRoot;
            entity.OhifViewerUrl = model.OhifViewerUrl;

            entity.SyncEnabled = model.SyncEnabled;
            entity.SyncIntervalSeconds = model.SyncIntervalSeconds;

            entity.IsDefault = model.IsDefault;
            entity.IsActive = model.IsActive;

            if (entity.IsDefault)
            {
                var otherDefaults = await _context.PacsConfigs
                    .Where(x => x.Id != entity.Id && x.IsDefault)
                    .ToListAsync();

                foreach (var item in otherDefaults)
                {
                    item.IsDefault = false;
                    item.UpdatedOn = DateTime.Now;
                    item.UpdatedBy = userId;
                }
            }

            await _context.SaveChangesAsync();

            return new
            {
                success = true,
                message = "Lưu cấu hình PACS thành công.",
                data = entity
            };
        }

        public async Task<object> DeleteAsync(int id)
        {
            var entity = await _context.PacsConfigs.FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
            {
                return new
                {
                    success = false,
                    message = "Không tìm thấy cấu hình PACS cần xóa."
                };
            }

            _context.PacsConfigs.Remove(entity);
            await _context.SaveChangesAsync();

            return new
            {
                success = true,
                message = "Xóa cấu hình PACS thành công."
            };
        }

        public async Task<object> TestConnectionAsync(int id)
        {
            var requestId = Guid.NewGuid().ToString("N").Substring(0, 8);

            await WriteConnectionLogAsync(
                requestId,
                $"START TestConnection | PacsConfigId={id}"
            );

            try
            {
                var config = await _context.PacsConfigs
                    .FirstOrDefaultAsync(x => x.Id == id);

                if (config == null)
                {
                    await WriteConnectionLogAsync(
                        requestId,
                        $"STOP TestConnection | Không tìm thấy PacsConfigId={id}"
                    );

                    return new
                    {
                        success = false,
                        status = "Offline",
                        message = "Không tìm thấy cấu hình PACS.",
                        requestId
                    };
                }

                return await TestConnectionAsync(config, requestId);
            }
            catch (Exception ex)
            {
                await WriteConnectionLogAsync(
                    requestId,
                    $"FATAL TestConnection | PacsConfigId={id}",
                    ex
                );

                return new
                {
                    success = false,
                    status = "Offline",
                    message = "Test kết nối PACS thất bại. Xem file log để biết chi tiết.",
                    requestId,
                    lastError = ex.Message
                };
            }
        }

        public async Task<object> TestDefaultConnectionAsync()
        {
            var requestId = Guid.NewGuid().ToString("N").Substring(0, 8);

            await WriteConnectionLogAsync(
                requestId,
                "START TestDefaultConnection"
            );

            try
            {
                var config = await GetDefaultAsync();

                if (config == null)
                {
                    await WriteConnectionLogAsync(
                        requestId,
                        "STOP TestDefaultConnection | Chưa có PACS mặc định đang active."
                    );

                    return new
                    {
                        success = false,
                        status = "Disabled",
                        message = "Chưa cấu hình PACS server mặc định.",
                        requestId
                    };
                }

                return await TestConnectionAsync(config, requestId);
            }
            catch (Exception ex)
            {
                await WriteConnectionLogAsync(
                    requestId,
                    "FATAL TestDefaultConnection",
                    ex
                );

                return new
                {
                    success = false,
                    status = "Offline",
                    message = "Không kiểm tra được trạng thái PACS. Xem file log để biết chi tiết.",
                    requestId,
                    lastError = ex.Message
                };
            }
        }

        private async Task<object> TestConnectionAsync(PacsConfig config, string requestId)
        {
            var checkedAt = DateTime.Now;

            await WriteConnectionLogAsync(
                requestId,
                $"CONFIG | Id={config.Id} | Code={config.Code} | Name={config.Name} | " +
                $"BaseUrl={config.BaseUrl} | DicomWebRoot={config.DicomWebRoot} | " +
                $"DicomHost={config.DicomHost} | DicomPort={config.DicomPort} | " +
                $"AuthEnabled={!string.IsNullOrWhiteSpace(config.ApiUsername)}"
            );

            // Không log ApiPassword / Authorization header.
            var apiResult = await CheckOrthancEndpointAsync(config, "/system");
            await LogHttpCheckResultAsync(requestId, "ORTHANC API /system", apiResult);

            var studiesResult = await CheckOrthancEndpointAsync(config, "/studies");
            await LogHttpCheckResultAsync(requestId, "ORTHANC API /studies", studiesResult);

            var dicomWebRoot = string.IsNullOrWhiteSpace(config.DicomWebRoot)
                ? "/dicom-web/"
                : NormalizeDicomWebRoot(config.DicomWebRoot);

            var dicomWebEndpoint = dicomWebRoot.TrimEnd('/') + "/studies";

            var dicomWebResult = await CheckOrthancEndpointAsync(
                config,
                dicomWebEndpoint
            );

            await LogHttpCheckResultAsync(
                requestId,
                $"DICOM WEB {dicomWebEndpoint}",
                dicomWebResult
            );

            var host = string.IsNullOrWhiteSpace(config.DicomHost)
                ? GetHostFromBaseUrl(config.BaseUrl)
                : config.DicomHost.Trim();

            var port = config.DicomPort ?? 4242;

            var dicomPortResult = await CheckTcpPortAsync(host, port, 3000);

            await WriteConnectionLogAsync(
                requestId,
                $"DICOM TCP | Target={dicomPortResult.Target} | " +
                $"Success={dicomPortResult.Success} | ElapsedMs={dicomPortResult.ElapsedMs} | " +
                $"Error={dicomPortResult.Error ?? "(none)"}"
            );

            bool apiReady = apiResult.Success;
            bool studiesReady = studiesResult.Success;
            bool dicomWebReady = dicomWebResult.Success;
            bool dicomPortReady = dicomPortResult.Success;

            var failedServices = new List<string>();
            var errors = new List<string>();

            AddFailure("Orthanc /system", apiResult, failedServices, errors);
            AddFailure("Orthanc /studies", studiesResult, failedServices, errors);
            AddFailure("DICOMweb /studies", dicomWebResult, failedServices, errors);
            AddFailure("DICOM TCP Port", dicomPortResult, failedServices, errors);

            string lastError = errors.Count > 0
                ? string.Join(" | ", errors)
                : null;

            string status;
            string message;
            bool success;

            if (apiReady && studiesReady && dicomWebReady && dicomPortReady)
            {
                success = true;
                status = "Ready";
                message = "PACS Server is ready";
            }
            else if (apiReady || studiesReady || dicomWebReady || dicomPortReady)
            {
                success = false;
                status = "Warning";
                message = failedServices.Count > 0
                    ? "PACS Server warning. Chưa sẵn sàng: " + string.Join(", ", failedServices)
                    : "PACS Server warning. Một số dịch vụ chưa sẵn sàng.";
            }
            else
            {
                success = false;
                status = "Offline";
                message = "PACS Server is offline";
            }

            config.LastStatus = status;
            config.LastApiStatus = apiReady;
            config.LastStudiesStatus = studiesReady;
            config.LastDicomWebStatus = dicomWebReady;
            config.LastDicomPortStatus = dicomPortReady;
            config.LastCheckedAt = checkedAt;
            config.LastError = lastError;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                await WriteConnectionLogAsync(
                    requestId,
                    "DATABASE ERROR | Không lưu được trạng thái Test Connection vào PacsConfig.",
                    ex
                );

                throw;
            }

            await WriteConnectionLogAsync(
                requestId,
                $"SUMMARY | Status={status} | Success={success} | " +
                $"ApiReady={apiReady} | StudiesReady={studiesReady} | " +
                $"DicomWebReady={dicomWebReady} | DicomPortReady={dicomPortReady} | " +
                $"LastError={lastError ?? "(none)"}"
            );

            await WriteConnectionLogAsync(
                requestId,
                "END TestConnection"
            );

            return new
            {
                success,
                status,
                message,
                requestId,
                pacsConfigId = config.Id,
                apiReady,
                studiesReady,
                dicomWebReady,
                dicomPortReady,
                lastCheckedAt = checkedAt.ToString("dd/MM/yyyy HH:mm:ss"),
                lastError,

                // Trả thêm chi tiết để F12 > Network có thể xem ngay.
                details = new
                {
                    api = ToResponseDetail(apiResult),
                    studies = ToResponseDetail(studiesResult),
                    dicomWeb = ToResponseDetail(dicomWebResult),
                    dicomPort = ToResponseDetail(dicomPortResult)
                }
            };
        }

        private async Task<ConnectionCheckResult> CheckOrthancEndpointAsync(
            PacsConfig config,
            string endpoint)
        {
            var watch = Stopwatch.StartNew();

            var baseUrl = NormalizeBaseUrl(config.BaseUrl);
            var target = string.IsNullOrWhiteSpace(baseUrl)
                ? endpoint
                : baseUrl + (endpoint.StartsWith("/") ? endpoint : "/" + endpoint);

            try
            {
                // Reuse the same PACS-only TLS trust handler as OrthancClient.
                using var client = _httpClientFactory.CreateClient("pacs-orthanc");

                client.Timeout = TimeSpan.FromSeconds(5);
                client.BaseAddress = new Uri(baseUrl);

                if (!string.IsNullOrWhiteSpace(config.ApiUsername))
                {
                    var rawAuth = $"{config.ApiUsername}:{config.ApiPassword}";
                    var token = Convert.ToBase64String(
                        Encoding.ASCII.GetBytes(rawAuth)
                    );

                    client.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Basic", token);
                }

                using var response = await client.GetAsync(endpoint);

                var responseBody = response.Content == null
                    ? null
                    : await response.Content.ReadAsStringAsync();

                watch.Stop();

                return new ConnectionCheckResult
                {
                    Success = response.IsSuccessStatusCode,
                    Target = target,
                    StatusCode = (int)response.StatusCode,
                    ReasonPhrase = response.ReasonPhrase,
                    ResponseBody = LimitText(responseBody, 3000),
                    ElapsedMs = watch.ElapsedMilliseconds,
                    Error = response.IsSuccessStatusCode
                        ? null
                        : $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}"
                };
            }
            catch (Exception ex)
            {
                watch.Stop();

                return new ConnectionCheckResult
                {
                    Success = false,
                    Target = target,
                    ElapsedMs = watch.ElapsedMilliseconds,
                    Error = ex.Message,
                    ExceptionDetail = ex.ToString()
                };
            }
        }

        private async Task<ConnectionCheckResult> CheckTcpPortAsync(
            string host,
            int port,
            int timeoutMs)
        {
            var watch = Stopwatch.StartNew();
            var target = $"{host}:{port}";

            try
            {
                using var client = new TcpClient();

                var connectTask = client.ConnectAsync(host, port);
                var timeoutTask = Task.Delay(timeoutMs);

                var completedTask = await Task.WhenAny(connectTask, timeoutTask);

                if (completedTask != connectTask)
                {
                    watch.Stop();

                    return new ConnectionCheckResult
                    {
                        Success = false,
                        Target = target,
                        ElapsedMs = watch.ElapsedMilliseconds,
                        Error = $"Timeout sau {timeoutMs} ms."
                    };
                }

                // Nếu ConnectAsync hoàn thành ở trạng thái faulted,
                // await ở đây sẽ ném đúng exception để log được nguyên nhân.
                await connectTask;

                watch.Stop();

                return new ConnectionCheckResult
                {
                    Success = client.Connected,
                    Target = target,
                    ElapsedMs = watch.ElapsedMilliseconds,
                    Error = client.Connected
                        ? null
                        : "TCP connect hoàn thành nhưng socket chưa Connected."
                };
            }
            catch (Exception ex)
            {
                watch.Stop();

                return new ConnectionCheckResult
                {
                    Success = false,
                    Target = target,
                    ElapsedMs = watch.ElapsedMilliseconds,
                    Error = ex.Message,
                    ExceptionDetail = ex.ToString()
                };
            }
        }

        private void AddFailure(
            string serviceName,
            ConnectionCheckResult result,
            List<string> failedServices,
            List<string> errors)
        {
            if (result.Success)
            {
                return;
            }

            failedServices.Add(serviceName);

            var detail = !string.IsNullOrWhiteSpace(result.Error)
                ? result.Error
                : "Không nhận được trạng thái thành công.";

            errors.Add($"{serviceName}: {detail}");
        }

        private object ToResponseDetail(ConnectionCheckResult result)
        {
            return new
            {
                success = result.Success,
                target = result.Target,
                statusCode = result.StatusCode,
                reasonPhrase = result.ReasonPhrase,
                elapsedMs = result.ElapsedMs,
                responsePreview = result.ResponseBody,
                error = result.Error
            };
        }

        private async Task LogHttpCheckResultAsync(
            string requestId,
            string checkName,
            ConnectionCheckResult result)
        {
            await WriteConnectionLogAsync(
                requestId,
                $"{checkName} | Target={result.Target} | Success={result.Success} | " +
                $"StatusCode={result.StatusCode?.ToString() ?? "(none)"} | " +
                $"Reason={result.ReasonPhrase ?? "(none)"} | " +
                $"ElapsedMs={result.ElapsedMs} | Error={result.Error ?? "(none)"}"
            );

            if (!string.IsNullOrWhiteSpace(result.ResponseBody))
            {
                await WriteConnectionLogAsync(
                    requestId,
                    $"{checkName} RESPONSE | {MakeSingleLine(result.ResponseBody)}"
                );
            }

            if (!string.IsNullOrWhiteSpace(result.ExceptionDetail))
            {
                await WriteConnectionLogAsync(
                    requestId,
                    $"{checkName} EXCEPTION | {MakeSingleLine(result.ExceptionDetail)}"
                );
            }
        }

        private async Task WriteConnectionLogAsync(
            string requestId,
            string message,
            Exception exception = null)
        {
            try
            {
                // AppContext.BaseDirectory trên IIS trỏ về thư mục publish của ứng dụng,
                // ví dụ: C:\inetpub\wwwroot\lis-live\
                // => log sẽ nằm tại: C:\inetpub\wwwroot\lis-live\Logs\PacsConnection
                var rootPath = AppContext.BaseDirectory;

                var logFolder = Path.Combine(
                    rootPath,
                    "Logs",
                    "PacsConnection"
                );

                Directory.CreateDirectory(logFolder);

                var logFile = Path.Combine(
                    logFolder,
                    $"pacs-connection-{DateTime.Now:yyyyMMdd}.log"
                );

                var builder = new StringBuilder();

                builder.Append('[')
                    .Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"))
                    .Append("] [")
                    .Append(requestId)
                    .Append("] ")
                    .AppendLine(message);

                if (exception != null)
                {
                    builder.Append('[')
                        .Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"))
                        .Append("] [")
                        .Append(requestId)
                        .Append("] EXCEPTION DETAIL | ")
                        .AppendLine(MakeSingleLine(exception.ToString()));
                }

                await _connectionLogLock.WaitAsync();

                try
                {
                    await File.AppendAllTextAsync(
                        logFile,
                        builder.ToString(),
                        Encoding.UTF8
                    );
                }
                finally
                {
                    _connectionLogLock.Release();
                }
            }
            catch
            {
                // Tuyệt đối không để lỗi ghi log làm hỏng chức năng Test Connection.
            }
        }

        private string LimitText(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            if (value.Length <= maxLength)
            {
                return value;
            }

            return value.Substring(0, maxLength)
                   + $" ... [TRUNCATED, total={value.Length} chars]";
        }

        private string MakeSingleLine(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            return value
                .Replace("\r", " ")
                .Replace("\n", " ")
                .Trim();
        }

        private sealed class ConnectionCheckResult
        {
            public bool Success { get; set; }
            public string Target { get; set; }
            public int? StatusCode { get; set; }
            public string ReasonPhrase { get; set; }
            public string ResponseBody { get; set; }
            public long ElapsedMs { get; set; }
            public string Error { get; set; }
            public string ExceptionDetail { get; set; }
        }

        private string NormalizeBaseUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return url;
            }

            return url.Trim().TrimEnd('/');
        }

        private string NormalizeDicomWebRoot(string root)
        {
            if (string.IsNullOrWhiteSpace(root))
            {
                return "/dicom-web/";
            }

            root = root.Trim();

            if (!root.StartsWith("/"))
            {
                root = "/" + root;
            }

            if (!root.EndsWith("/"))
            {
                root += "/";
            }

            return root;
        }

        private string GetHostFromBaseUrl(string baseUrl)
        {
            try
            {
                var uri = new Uri(baseUrl);
                return uri.Host;
            }
            catch
            {
                return "localhost";
            }
        }
    }
}
