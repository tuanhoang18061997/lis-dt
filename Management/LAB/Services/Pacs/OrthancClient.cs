using Management.Models.Pacs;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Management.Services.Pacs
{
    /// <summary>
    /// Shared REST client giao tiếp với Orthanc.
    ///
    /// Không phụ thuộc module XQ / CT / MRI / MG / SA.
    ///
    /// PacsConfig được truyền vào Configure() để thiết lập:
    /// - Base URL
    /// - Timeout
    /// - Basic Authentication
    ///
    /// Sau Configure(), client được sử dụng cho:
    /// - Study
    /// - Series
    /// - Instance
    /// - Changes
    /// - Recovery lookup
    /// - Preview
    /// </summary>
    public class OrthancClient
    {
        private readonly HttpClient _httpClient;

        /*
         * PACS-XQ-PREPROD-01D-05-HOTFIX-04
         *
         * HttpClient.BaseAddress / Timeout cannot be changed after the
         * first request has started. One OrthancClient can be reused by
         * PacsStudyBL for multiple StableStudy items inside one checkpoint
         * batch, so Configure() must be idempotent for the same PACS config.
         */
        private readonly object _configureLock = new();
        private bool _isConfigured;

        public OrthancClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Cấu hình HttpClient theo PACS server đang được sử dụng.
        ///
        /// Contract:
        /// - Lần đầu: cấu hình BaseAddress / Timeout / Basic Auth.
        /// - Gọi lại với CÙNG config: NO-OP.
        /// - Yêu cầu đổi sang config khác trên cùng OrthancClient:
        ///   fail-closed, không mutate HttpClient.
        ///
        /// HttpClient không cho phép thay đổi BaseAddress / Timeout
        /// sau khi request đầu tiên đã bắt đầu.
        /// </summary>
        public void Configure(PacsConfig config)
        {
            if (config == null)
            {
                throw new Exception(
                    "PACS config không hợp lệ.");
            }

            if (string.IsNullOrWhiteSpace(config.BaseUrl))
            {
                throw new Exception(
                    "BaseUrl PACS chưa được cấu hình.");
            }

            var desiredBaseAddress =
                new Uri(
                    config.BaseUrl
                        .Trim()
                        .TrimEnd('/'));

            var desiredTimeout =
                TimeSpan.FromSeconds(30);

            AuthenticationHeaderValue?
                desiredAuthorization = null;

            if (!string.IsNullOrWhiteSpace(
                config.ApiUsername))
            {
                var rawAuth =
                    $"{config.ApiUsername}:{config.ApiPassword}";

                var token =
                    Convert.ToBase64String(
                        Encoding.ASCII.GetBytes(
                            rawAuth));

                desiredAuthorization =
                    new AuthenticationHeaderValue(
                        "Basic",
                        token);
            }

            lock (_configureLock)
            {
                if (_isConfigured)
                {
                    var currentAuthorization =
                        _httpClient
                            .DefaultRequestHeaders
                            .Authorization;

                    var sameBaseAddress =
                        Equals(
                            _httpClient.BaseAddress,
                            desiredBaseAddress);

                    var sameTimeout =
                        _httpClient.Timeout ==
                        desiredTimeout;

                    var sameAuthorization =
                        SameAuthorization(
                            currentAuthorization,
                            desiredAuthorization);

                    if (sameBaseAddress &&
                        sameTimeout &&
                        sameAuthorization)
                    {
                        // Idempotent call for the same PACS config.
                        // Do not mutate HttpClient after its first request.
                        return;
                    }

                    throw new InvalidOperationException(
                        "OrthancClient đã được cấu hình và đã bắt đầu được sử dụng. " +
                        "Không cho phép đổi PACS BaseUrl/Timeout/Authentication " +
                        "trên cùng HttpClient instance.");
                }

                _httpClient.BaseAddress =
                    desiredBaseAddress;

                _httpClient.Timeout =
                    desiredTimeout;

                _httpClient
                    .DefaultRequestHeaders
                    .Clear();

                if (desiredAuthorization != null)
                {
                    _httpClient
                        .DefaultRequestHeaders
                        .Authorization =
                            desiredAuthorization;
                }

                _isConfigured = true;
            }
        }

        private static bool SameAuthorization(
            AuthenticationHeaderValue? current,
            AuthenticationHeaderValue? desired)
        {
            if (current == null &&
                desired == null)
            {
                return true;
            }

            if (current == null ||
                desired == null)
            {
                return false;
            }

            return
                string.Equals(
                    current.Scheme,
                    desired.Scheme,
                    StringComparison.OrdinalIgnoreCase)
                &&
                string.Equals(
                    current.Parameter,
                    desired.Parameter,
                    StringComparison.Ordinal);
        }

        /// <summary>
        /// Lấy danh sách toàn bộ Study trong Orthanc.
        ///
        /// GET /studies
        /// </summary>
        public async Task<string> GetStudiesJsonAsync()
        {
            var response =
                await _httpClient.GetAsync(
                    "/studies");

            response.EnsureSuccessStatusCode();

            return await response
                .Content
                .ReadAsStringAsync();
        }

        /// <summary>
        /// Đọc Orthanc change feed.
        ///
        /// GET /changes?since={seq}&limit={limit}
        ///
        /// Đây là API chính cho background PACS sync.
        /// </summary>
        public async Task<string> GetChangesJsonAsync(
            long since = 0,
            int limit = 100,
            CancellationToken cancellationToken = default)
        {
            EnsureConfigured();

            if (since < 0)
            {
                since = 0;
            }

            if (limit <= 0)
            {
                limit = 100;
            }

            // Không cho một request đọc quá nhiều event.
            if (limit > 500)
            {
                limit = 500;
            }

            var url =
                $"/changes?since={since}&limit={limit}";

            using var response =
                await _httpClient.GetAsync(
                    url,
                    cancellationToken);

            response.EnsureSuccessStatusCode();

            return await response
                .Content
                .ReadAsStringAsync();
        }

        /// <summary>
        /// Tìm Orthanc StudyId theo AccessionNumber.
        ///
        /// Dùng cho PACS Recovery.
        ///
        /// POST /tools/find
        ///
        /// Hàm này:
        /// - Chỉ query Orthanc
        /// - Không sync Study
        /// - Không update Worklist
        /// - Không thay đổi PacsSyncStates.LastChangeSeq
        /// </summary>
        public async Task<List<string>>
            FindStudyIdsByAccessionNumberAsync(
                string accessionNumber,
                CancellationToken cancellationToken = default)
        {
            EnsureConfigured();

            if (string.IsNullOrWhiteSpace(
                accessionNumber))
            {
                return new List<string>();
            }

            accessionNumber =
                accessionNumber.Trim();

            var requestBody = new
            {
                Level = "Study",

                Query =
                    new Dictionary<string, string>
                    {
                        ["AccessionNumber"] =
                            accessionNumber
                    }
            };

            var requestJson =
                JsonSerializer.Serialize(
                    requestBody);

            using var content =
                new StringContent(
                    requestJson,
                    Encoding.UTF8,
                    "application/json");

            using var response =
                await _httpClient.PostAsync(
                    "/tools/find",
                    content,
                    cancellationToken);

            response.EnsureSuccessStatusCode();

            var responseJson =
                await response
                    .Content
                    .ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(
                responseJson))
            {
                return new List<string>();
            }

            var studyIds =
                JsonSerializer
                    .Deserialize<List<string>>(
                        responseJson)
                ?? new List<string>();

            return studyIds
                .Where(
                    x => !string.IsNullOrWhiteSpace(x))
                .Select(
                    x => x.Trim())
                .Distinct(
                    StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>
        /// Lấy metadata của một Orthanc Study.
        ///
        /// GET /studies/{studyId}
        /// </summary>
        public async Task<string> GetStudyJsonAsync(
            string orthancStudyId,
            CancellationToken cancellationToken = default)
        {
            EnsureConfigured();

            if (string.IsNullOrWhiteSpace(
                orthancStudyId))
            {
                throw new Exception(
                    "OrthancStudyId không hợp lệ.");
            }

            var url =
                "/studies/" +
                Uri.EscapeDataString(
                    orthancStudyId.Trim());

            using var response =
                await _httpClient.GetAsync(
                    url,
                    cancellationToken);

            response.EnsureSuccessStatusCode();

            return await response
                .Content
                .ReadAsStringAsync();
        }

        /// <summary>
        /// Lấy toàn bộ Series metadata thuộc Study
        /// bằng một HTTP request.
        ///
        /// GET /studies/{studyId}/series
        /// </summary>
        public async Task<string>
            GetStudySeriesJsonAsync(
                string orthancStudyId,
                CancellationToken cancellationToken = default)
        {
            EnsureConfigured();

            if (string.IsNullOrWhiteSpace(
                orthancStudyId))
            {
                throw new Exception(
                    "OrthancStudyId không hợp lệ.");
            }

            var url =
                $"/studies/" +
                $"{Uri.EscapeDataString(orthancStudyId.Trim())}" +
                "/series";

            using var response =
                await _httpClient.GetAsync(
                    url,
                    cancellationToken);

            response.EnsureSuccessStatusCode();

            return await response
                .Content
                .ReadAsStringAsync();
        }

        /// <summary>
        /// Lấy metadata của một Series.
        ///
        /// GET /series/{seriesId}
        /// </summary>
        public async Task<string> GetSeriesJsonAsync(
            string orthancSeriesId)
        {
            EnsureConfigured();

            if (string.IsNullOrWhiteSpace(
                orthancSeriesId))
            {
                throw new Exception(
                    "OrthancSeriesId không hợp lệ.");
            }

            var url =
                "/series/" +
                Uri.EscapeDataString(
                    orthancSeriesId.Trim());

            using var response =
                await _httpClient.GetAsync(url);

            response.EnsureSuccessStatusCode();

            return await response
                .Content
                .ReadAsStringAsync();
        }

        /// <summary>
        /// Lấy toàn bộ Instance của một Series
        /// bằng một HTTP request.
        ///
        /// GET /series/{seriesId}/instances
        /// </summary>
        public async Task<string>
            GetSeriesInstancesJsonAsync(
                string orthancSeriesId,
                CancellationToken cancellationToken = default)
        {
            EnsureConfigured();

            if (string.IsNullOrWhiteSpace(
                orthancSeriesId))
            {
                throw new Exception(
                    "OrthancSeriesId không hợp lệ.");
            }

            var url =
                "/series/" +
                Uri.EscapeDataString(
                    orthancSeriesId.Trim()) +
                "/instances";

            using var response =
                await _httpClient.GetAsync(
                    url,
                    cancellationToken);

            response.EnsureSuccessStatusCode();

            return await response
                .Content
                .ReadAsStringAsync();
        }

        /// <summary>
        /// Lấy metadata của một Instance.
        ///
        /// GET /instances/{instanceId}
        /// </summary>
        public async Task<string> GetInstanceJsonAsync(
            string orthancInstanceId)
        {
            EnsureConfigured();

            if (string.IsNullOrWhiteSpace(
                orthancInstanceId))
            {
                throw new Exception(
                    "OrthancInstanceId không hợp lệ.");
            }

            var url =
                "/instances/" +
                Uri.EscapeDataString(
                    orthancInstanceId.Trim());

            using var response =
                await _httpClient.GetAsync(url);

            response.EnsureSuccessStatusCode();

            return await response
                .Content
                .ReadAsStringAsync();
        }

        /// <summary>
        /// Lấy ảnh preview của một DICOM Instance.
        ///
        /// GET /instances/{instanceId}/preview
        ///
        /// Dùng cho thumbnail PACS trên UI.
        /// </summary>
        public async Task<byte[]?>
            GetInstancePreviewBytesAsync(
                string orthancInstanceId)
        {
            if (string.IsNullOrWhiteSpace(
                orthancInstanceId))
            {
                return null;
            }

            var url =
                "/instances/" +
                Uri.EscapeDataString(
                    orthancInstanceId.Trim()) +
                "/preview";

            return await GetBytesAsync(url);
        }

        /// <summary>
        /// Helper lấy binary content từ Orthanc.
        /// </summary>
        private async Task<byte[]> GetBytesAsync(
            string relativeUrl)
        {
            EnsureConfigured();

            using var response =
                await _httpClient.GetAsync(
                    relativeUrl);

            response.EnsureSuccessStatusCode();

            return await response
                .Content
                .ReadAsByteArrayAsync();
        }

        /// <summary>
        /// Guard dùng chung để tránh gọi Orthanc
        /// trước khi Configure(PacsConfig).
        /// </summary>
        private void EnsureConfigured()
        {
            if (_httpClient.BaseAddress == null)
            {
                throw new Exception(
                    "OrthancClient chưa được Configure.");
            }
        }
    }
}
