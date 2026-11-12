using Management.BL.Pacs;
using Management.Models.Pacs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Management.Controllers.Pacs
{
    public class PacsConfigController : Controller
    {
        private readonly PacsConfigBL _pacsConfigBL;

        public PacsConfigController(
            PacsConfigBL pacsConfigBL)
        {
            _pacsConfigBL = pacsConfigBL;
        }

        /// <summary>
        /// Trang cấu hình PACS.
        ///
        /// View sẽ được port ở step frontend/config sau.
        /// </summary>
        public IActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// Lấy toàn bộ cấu hình PACS.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var data =
                    await _pacsConfigBL.GetAllAsync();

                var result =
                    data.Select(x => new
                    {
                        x.Id,
                        x.Code,
                        x.Name,
                        x.ServerType,

                        x.BaseUrl,

                        x.ApiUsername,
                        x.ApiPassword,

                        x.DicomAETitle,
                        x.DicomHost,
                        x.DicomPort,

                        x.DicomWebRoot,
                        x.OhifViewerUrl,

                        x.SyncEnabled,
                        x.SyncIntervalSeconds,

                        x.IsDefault,
                        x.IsActive,

                        x.LastStatus,
                        x.LastApiStatus,
                        x.LastStudiesStatus,
                        x.LastDicomWebStatus,
                        x.LastDicomPortStatus,

                        LastCheckedAt =
                            x.LastCheckedAt.HasValue
                                ? x.LastCheckedAt.Value
                                    .ToString(
                                        "dd/MM/yyyy HH:mm:ss")
                                : "",

                        x.LastError
                    })
                    .ToList();

                return Json(new
                {
                    success = true,
                    data = result
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        /// <summary>
        /// Lấy một cấu hình PACS theo Id.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var item =
                    await _pacsConfigBL.GetByIdAsync(id);

                if (item == null)
                {
                    return Json(new
                    {
                        success = false,
                        message =
                            "Không tìm thấy cấu hình PACS."
                    });
                }

                return Json(new
                {
                    success = true,

                    data = new
                    {
                        item.Id,
                        item.Code,
                        item.Name,
                        item.ServerType,

                        item.BaseUrl,

                        item.ApiUsername,
                        item.ApiPassword,

                        item.DicomAETitle,
                        item.DicomHost,
                        item.DicomPort,

                        item.DicomWebRoot,
                        item.OhifViewerUrl,

                        item.SyncEnabled,
                        item.SyncIntervalSeconds,

                        item.IsDefault,
                        item.IsActive,

                        item.LastStatus,
                        item.LastApiStatus,
                        item.LastStudiesStatus,
                        item.LastDicomWebStatus,
                        item.LastDicomPortStatus,

                        LastCheckedAt =
                            item.LastCheckedAt.HasValue
                                ? item.LastCheckedAt.Value
                                    .ToString(
                                        "dd/MM/yyyy HH:mm:ss")
                                : "",

                        item.LastError
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        /// <summary>
        /// Thêm mới hoặc cập nhật cấu hình PACS.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Save(
            [FromBody] PacsConfig model)
        {
            try
            {
                var userId =
                    GetCurrentUserId();

                var result =
                    await _pacsConfigBL.SaveAsync(
                        model,
                        userId);

                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        /// <summary>
        /// Xóa một cấu hình PACS.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var result =
                    await _pacsConfigBL.DeleteAsync(id);

                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        /// <summary>
        /// Kiểm tra một cấu hình PACS cụ thể.
        ///
        /// Test:
        /// - Orthanc /system
        /// - Orthanc /studies
        /// - DICOMweb /studies
        /// - DICOM TCP Port
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> TestConnection(
            int id)
        {
            try
            {
                var result =
                    await _pacsConfigBL
                        .TestConnectionAsync(id);

                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    status = "Offline",
                    message =
                        "Test kết nối PACS thất bại.",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Kiểm tra PACS mặc định đang Active.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetStatus()
        {
            try
            {
                var result =
                    await _pacsConfigBL
                        .TestDefaultConnectionAsync();

                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    status = "Offline",
                    message =
                        "Không kiểm tra được trạng thái PACS.",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Project A lưu UserId bằng Claim.
        ///
        /// Ưu tiên Claim.
        /// Fallback Session để giữ compatibility
        /// nếu sau này có flow cũ vẫn lưu Session.
        /// </summary>
        private int? GetCurrentUserId()
        {
            try
            {
                // ---------------------------------------------
                // 1. Project A authentication baseline:
                //    UserId được lưu trong Claim.
                // ---------------------------------------------

                var userIdText =
                    User?.Claims?
                        .FirstOrDefault(
                            x => x.Type == "UserId")
                        ?.Value;

                if (int.TryParse(
                    userIdText,
                    out var userId))
                {
                    return userId;
                }

                // ---------------------------------------------
                // 2. Compatibility fallback
                // ---------------------------------------------

                userIdText =
                    HttpContext.Session
                        .GetString("UserId");

                if (int.TryParse(
                    userIdText,
                    out userId))
                {
                    return userId;
                }

                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}