using Management.Models;
using Management.Models.Pacs;
using Management.Services.Pacs;
using Microsoft.EntityFrameworkCore;

namespace Management.BL.Pacs
{
    public class PacsViewerBL
    {
        private readonly LABContext _context;
        private readonly OrthancClient _orthancClient;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public PacsViewerBL(
            LABContext context,
            OrthancClient orthancClient,
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _orthancClient = orthancClient;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<string> GetOhifViewerUrlByResultCDHAIdAsync(
            long resultCDHAId)
        {
            if (resultCDHAId <= 0)
            {
                return string.Empty;
            }

            var study = await _context.PacsStudies
                .AsNoTracking()
                .Where(x => x.ResultCDHAId == resultCDHAId)
                .OrderByDescending(x => x.Id)
                .FirstOrDefaultAsync();

            if (study == null ||
                string.IsNullOrWhiteSpace(study.StudyInstanceUID))
            {
                return string.Empty;
            }

            var ohifBaseUrl =
                GetOhifBaseUrlByCurrentRequest();

            return
                $"{ohifBaseUrl}/viewer" +
                $"?StudyInstanceUIDs=" +
                $"{Uri.EscapeDataString(study.StudyInstanceUID)}";
        }

        public async Task<PacsSeriesThumbnailResult>
            GetSeriesThumbnailsByResultCDHAIdAsync(
                long resultCDHAId)
        {
            var result =
                new PacsSeriesThumbnailResult
                {
                    ResultCDHAId = resultCDHAId
                };

            if (resultCDHAId <= 0)
            {
                result.Success = false;
                result.Message = "Thiếu ResultCDHAId.";

                return result;
            }

            var study = await _context.PacsStudies
                .AsNoTracking()
                .Where(x => x.ResultCDHAId == resultCDHAId)
                .OrderByDescending(x => x.Id)
                .FirstOrDefaultAsync();

            if (study == null)
            {
                result.Success = false;
                result.Message =
                    "Dịch vụ này chưa có Study PACS hoặc chưa sync Orthanc.";

                return result;
            }

            if (string.IsNullOrWhiteSpace(
                study.StudyInstanceUID))
            {
                result.Success = false;
                result.Message =
                    "Study PACS chưa có StudyInstanceUID.";

                return result;
            }

            var ohifBaseUrl =
                GetOhifBaseUrlByCurrentRequest();

            var viewerUrl =
                $"{ohifBaseUrl}/viewer" +
                $"?StudyInstanceUIDs=" +
                $"{Uri.EscapeDataString(study.StudyInstanceUID)}";

            result.PacsStudyId = study.Id;
            result.AccessionNumber =
                study.AccessionNumber;
            result.StudyInstanceUID =
                study.StudyInstanceUID;
            result.OrthancStudyId =
                study.OrthancStudyId;
            result.ViewerUrl =
                viewerUrl;

            var seriesList =
                await _context.PacsSeries
                    .AsNoTracking()
                    .Where(
                        x => x.PacsStudyId == study.Id)
                    .ToListAsync();

            if (seriesList.Count == 0)
            {
                result.Success = false;
                result.Message =
                    "Study này chưa có Series PACS.";

                return result;
            }

            seriesList = seriesList
                .OrderBy(
                    x => ParseIntSafe(
                        x.SeriesNumber))
                .ThenBy(x => x.Id)
                .ToList();

            var seriesIds =
                seriesList
                    .Select(x => x.Id)
                    .ToList();

            var instances =
                await _context.PacsInstances
                    .AsNoTracking()
                    .Where(
                        x => seriesIds.Contains(
                            x.PacsSeriesId))
                    .Select(x => new
                    {
                        x.Id,
                        x.PacsSeriesId,
                        x.OrthancInstanceId,
                        x.SOPInstanceUID,
                        x.InstanceNumber
                    })
                    .ToListAsync();

            foreach (var series in seriesList)
            {
                var firstInstance =
                    instances
                        .Where(x =>
                            x.PacsSeriesId ==
                                series.Id &&
                            !string.IsNullOrWhiteSpace(
                                x.OrthancInstanceId))
                        .OrderBy(
                            x => ParseIntSafe(
                                x.InstanceNumber))
                        .ThenBy(x => x.Id)
                        .FirstOrDefault();

                result.Items.Add(
                    new PacsSeriesThumbnailItem
                    {
                        PacsSeriesId =
                            series.Id,

                        OrthancSeriesId =
                            series.OrthancSeriesId,

                        SeriesInstanceUID =
                            series.SeriesInstanceUID,

                        SeriesNumber =
                            series.SeriesNumber,

                        SeriesDescription =
                            series.SeriesDescription,

                        Modality =
                            series.Modality,

                        ProtocolName =
                            series.ProtocolName,

                        InstanceCount =
                            series.InstanceCount,

                        PacsInstanceId =
                            firstInstance?.Id,

                        OrthancInstanceId =
                            firstInstance
                                ?.OrthancInstanceId,

                        SOPInstanceUID =
                            firstInstance
                                ?.SOPInstanceUID,

                        InstanceNumber =
                            firstInstance
                                ?.InstanceNumber,

                        ViewerUrl =
                            viewerUrl
                    });
            }

            result.Success =
                result.Items.Any(
                    x =>
                        !string.IsNullOrWhiteSpace(
                            x.OrthancInstanceId));

            result.Message =
                result.Success
                    ? "Lấy danh sách Series PACS thành công."
                    : "Có Series PACS nhưng chưa có Instance để tạo thumbnail.";

            return result;
        }

        public async Task<byte[]?>
            GetOrthancInstancePreviewAsync(
                string orthancInstanceId)
        {
            if (string.IsNullOrWhiteSpace(
                orthancInstanceId))
            {
                return null;
            }

            var config =
                await GetDefaultPacsConfigAsync();

            if (config == null)
            {
                throw new InvalidOperationException(
                    "Chưa có PACS Server mặc định hoặc PACS Server chưa được bật."
                );
            }

            _orthancClient.Configure(config);

            return await _orthancClient
                .GetInstancePreviewBytesAsync(
                    orthancInstanceId);
        }

        private string
            GetOhifBaseUrlByCurrentRequest()
        {
            var request =
                _httpContextAccessor
                    .HttpContext
                    ?.Request;

            var isHttpsRequest =
                request?.IsHttps == true;

            var ohifBaseUrl =
                isHttpsRequest
                    ? _configuration[
                        "Pacs:OhifHttpsBaseUrl"]
                    : _configuration[
                        "Pacs:OhifHttpBaseUrl"];

            if (string.IsNullOrWhiteSpace(
                ohifBaseUrl))
            {
                ohifBaseUrl =
                    _configuration[
                        "Pacs:OhifBaseUrl"];
            }

            /*
             * Không fallback trực tiếp :3000.
             *
             * Viewer phải đi qua Project A / YARP
             * để giữ same-origin và tránh mixed-content.
             */
            if (string.IsNullOrWhiteSpace(
                    ohifBaseUrl) &&
                request != null)
            {
                ohifBaseUrl =
                    $"{request.Scheme}://" +
                    $"{request.Host}" +
                    $"{request.PathBase}/ohif";
            }

            if (string.IsNullOrWhiteSpace(
                ohifBaseUrl))
            {
                return "/ohif";
            }

            return ohifBaseUrl.TrimEnd('/');
        }

        private async Task<PacsConfig?>
            GetDefaultPacsConfigAsync()
        {
            return await _context.PacsConfigs
                .AsNoTracking()
                .Where(x =>
                    x.IsActive &&
                    x.IsDefault)
                .OrderByDescending(x => x.Id)
                .FirstOrDefaultAsync();
        }

        private static int ParseIntSafe(
            string? value)
        {
            if (string.IsNullOrWhiteSpace(
                value))
            {
                return int.MaxValue;
            }

            return int.TryParse(
                value.Trim(),
                out var number)
                    ? number
                    : int.MaxValue;
        }
    }
}