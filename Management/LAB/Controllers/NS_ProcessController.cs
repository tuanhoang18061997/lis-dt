using Management.BL;
using Management.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SelectPdf;
using System;
using System.IO;
using System.Security.Cryptography;
using IHostingEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;

namespace Management.Controllers
{
    public class NS_ProcessController : Controller
    {
        private readonly ILogger<NS_ProcessController> _logger;
        private readonly PatientCDHABL _patientCDHABL;
        private readonly ObjectBL _objectBL;
        private readonly LocationBL _locationBL;
        private readonly DoctorBL _doctorBL;
        private readonly UserBL _userBL;
        private readonly CategoryBL _categoryBL;
        private readonly ServiceBL _serviceBL;
        public readonly ResultCDHABL _resultCDHABL;
        private readonly SettingBL _settingBL;
        private readonly GroupBL _groupBL;
        private readonly ToolBL _toolBL;
        private readonly HospitalBL _hospitalBL;
        private readonly SampleBL _sampleBL;
        public readonly DeviceBL _deviceBL;
        private readonly IWebHostEnvironment _environment;
        public readonly string _NS = "NS";

        public NS_ProcessController(ILogger<NS_ProcessController> logger, PatientCDHABL patientCDHABL, ObjectBL objectBL, LocationBL locationBL,
                            DoctorBL doctorBL, UserBL userBL, CategoryBL categoryBL, ServiceBL serviceBL, ResultCDHABL resultCDHABL,
                            SettingBL settingBL, GroupBL groupBL, IWebHostEnvironment environment, ToolBL toolBL, HospitalBL hospitalBL, SampleBL sampleBL, DeviceBL deviceBL)
        {
            _logger = logger;
            _patientCDHABL = patientCDHABL;
            _objectBL = objectBL;
            _locationBL = locationBL;
            _doctorBL = doctorBL;
            _userBL = userBL;
            _categoryBL = categoryBL;
            _serviceBL = serviceBL;
            _resultCDHABL = resultCDHABL;
            _settingBL = settingBL;
            _groupBL = groupBL;
            _environment = environment;
            _toolBL = toolBL;
            _hospitalBL = hospitalBL;
            _sampleBL = sampleBL;
            _deviceBL = deviceBL;
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Process()
        {
            var dateTimeNow = ToolBL.Get_DateNow();
            var from = new DateTime(dateTimeNow.Year, dateTimeNow.Month, dateTimeNow.Day, 23, 59, 59).AddDays(-1);
            var to = new DateTime(dateTimeNow.Year, dateTimeNow.Month, dateTimeNow.Day, 23, 59, 59);

            ViewData["lstPatient"] = await _patientCDHABL.Get_ListPatient(from, to, false, true, false, _NS);
            ViewData["lstUser"] = await _userBL.GetListUser(UserTypeModel.NS);
            ViewData["lstBenhAnModel"] = await BenhAnModel.GetListBenhAnModel();
            ViewData["lstSampleResult"] = await _sampleBL.GetListSampleByCategory(_NS);
            ViewData["lstDevice"] = await _deviceBL.GetList();
            await this.Get_Count();

            return View();
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Get_Count()
        {
            var dateTimeNow = ToolBL.Get_DateNow();
            var from = new DateTime(dateTimeNow.Year, dateTimeNow.Month, dateTimeNow.Day, 23, 59, 59).AddDays(-1);
            var to = new DateTime(dateTimeNow.Year, dateTimeNow.Month, dateTimeNow.Day, 23, 59, 59);
            var countGetSample = await _patientCDHABL.Get_CountPatient(from, to, true, false, false, _NS);
            var countProcess = await _patientCDHABL.Get_CountPatient(from, to, false, true, false, _NS);
            var countReturnResult = await _patientCDHABL.Get_CountPatient(from, to, false, false, true, _NS);
            ViewData["countGetSample"] = countGetSample;
            ViewData["countProcess"] = countProcess;
            ViewData["countReturnResult"] = countReturnResult;

            return Content(countGetSample + ";" + countProcess + ";" + countReturnResult);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Refresh()
        {
            var dateTimeNow = ToolBL.Get_DateNow();
            var from = new DateTime(dateTimeNow.Year, dateTimeNow.Month, dateTimeNow.Day, 23, 59, 59).AddDays(-1);
            var to = new DateTime(dateTimeNow.Year, dateTimeNow.Month, dateTimeNow.Day, 23, 59, 59);
            ViewData["lstPatient"] = await _patientCDHABL.Get_ListPatientByPidOrSid(from, to, false, true, false, null, _NS);

            return PartialView("_NS_Process_ListPatient");
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Search(string pidorseq, DateTime timeSearchFrom, DateTime timeSearchTo)
        {
            var from = new DateTime(timeSearchFrom.Year, timeSearchFrom.Month, timeSearchFrom.Day, 23, 59, 59).AddDays(-1);
            var to = new DateTime(timeSearchTo.Year, timeSearchTo.Month, timeSearchTo.Day, 23, 59, 59);
            ViewData["lstPatient"] = await _patientCDHABL.Get_ListPatientByPidOrSid(from, to, false, true, false, pidorseq, _NS);

            return PartialView("_NS_Process_ListPatient");
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> GetSample(long id)
        {
            var _userLogin = this.GetUserLogin();
            var _getSample = await _patientCDHABL.GetSample_ProcessResult_ReturnResult(id, true, false, false, _userLogin.Value, _NS);
            return Content(_getSample.ToString());
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetPatientInfo(long id)
        {
            var _userLoginId = this.GetUserLogin();
            ViewData["userLoginId"] = _userLoginId;
            ViewData["lstUser"] = await _userBL.GetListUser(UserTypeModel.NS);
            ViewData["lstBenhAnModel"] = await BenhAnModel.GetListBenhAnModel();
            ViewData["patientInfo"] = await _patientCDHABL.Get_PatientBySid(id);

            return PartialView("_NS_Process_PatientInfo");
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetServiceForPatient(long patientId)
        {
            ViewData["lstResultCDHAs"] = await _resultCDHABL.GetListResultCDHAByPatientId(patientId, _NS);
            return PartialView("_NS_Process_ListService");
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetImageForService(long id)
        {
            var _resultCDHA = await _resultCDHABL.GetResultCDHA(id);
            if (_resultCDHA != null)
            {
                var _lstItem = new List<object>();
                if (_resultCDHA.ImageCDHAs != null && _resultCDHA.ImageCDHAs.Count > 0)
                {
                    foreach (var item in _resultCDHA.ImageCDHAs)
                    {
                        _lstItem.Add(new { Id = item.Id, Name = item.ImagePath, Description = item.ResultCDHA.Description, Result = item.ResultCDHA.Result, Suggest = item.ResultCDHA.Suggest });
                    }
                }
                else
                {
                    _lstItem.Add(new { Description = _resultCDHA.Description, Result = _resultCDHA.Result, Suggest = _resultCDHA.Suggest });

                }
                return Json(_lstItem);
            }
            else
            {
                return Json(null);
            }
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetSampleForService(long id)
        {
            var _sample = await _sampleBL.GetSample(id);
            var _item = new { Description = _sample.Description, Result = _sample.Result, Suggest = _sample.Suggest };
            return Json(_item);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Check_SelectDevice()
        {
            var _device = GetSession_Device();
            var _selectDevice = _device == null ? false : true;
            return Content(_selectDevice.ToString());
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> SelectDevice(long deviceId)
        {
            var _status = false;
            try
            {
                var _device = await _deviceBL.Get(deviceId);
                if (_device != null)
                {
                    var device = new Device() { Id = _device.Id, Code = _device.Code, Name = _device.Name, CodeBHYT = _device.CodeBHYT };
                    CookieOptions cookieOptions = new CookieOptions() { Expires = new DateTimeOffset(DateTime.Now.AddDays(1)) };
                    string value = JsonConvert.SerializeObject(device, Formatting.Indented,
                                   new JsonSerializerSettings() { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });
                    HttpContext.Response.Cookies.Append(SessionKeyModel._sessionDeviceNS, value, cookieOptions);
                    _status = true;
                }
            }
            catch { }
            return Content(_status.ToString());
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> SaveImageCDHA([FromBody] ImageModel imageObj)
        {
            var guid = Guid.NewGuid();
            var _folder = Path.Combine(_environment.WebRootPath, "images", "result", "sa");
            var _imagePath1 = Path.Combine(_folder, imageObj.resultCDHAId + "-" + guid + ".png");
            var _image = _toolBL.Base64ToImage(imageObj.imageString);
            var _saveImage = _toolBL.SaveImage(_folder, _imagePath1, _image);

            if (_saveImage)
            {
                var _imagePath = "/images/result/sa/" + imageObj.resultCDHAId + "-" + guid + ".png";
                _saveImage = await _resultCDHABL.SaveImage(long.Parse(imageObj.resultCDHAId), _imagePath, _imagePath1);
            }
            return Content(_saveImage.ToString());
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> DeleteImageCDHA(long imageCDHAId)
        {
            var _imagePath1_resultCDHAId = await _resultCDHABL.DeleteImage(imageCDHAId);
            var _imagePath1 = string.Empty;
            var _resultCDHAId = string.Empty;
            if (!string.IsNullOrEmpty(_imagePath1_resultCDHAId))
            {
                _imagePath1 = _imagePath1_resultCDHAId.Split(';')[0];
                var _delete = _toolBL.DeleteImage(_imagePath1);
                if (_delete) _resultCDHAId = _imagePath1_resultCDHAId.Split(';')[1];
            }
            return Content(_resultCDHAId);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> SaveResult([FromBody] ResultCDHAModel resultCDHA)
        {
            var _saveResult = false;
            if (resultCDHA != null)
            {
                var _dateTime = ToolBL.Get_DateNow();
                var _userLogin = this.GetUserLogin();
                var _device = this.GetSession_Device();
                await _patientCDHABL.Update(resultCDHA.patientId, resultCDHA.returnResultTime, resultCDHA.userReturnResult, _userLogin.Value, _dateTime, _NS);
                _saveResult = await _resultCDHABL.Update(resultCDHA, _userLogin.Value, _dateTime, _device);
            }
            return Content(_saveResult.ToString());
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> ValidPrint([FromBody] ResultCDHAModel resultCDHA)
        {
            if (resultCDHA != null)
            {
                var _dateTime = ToolBL.Get_DateNow();
                var _userLogin = this.GetUserLogin();
                var _device = this.GetSession_Device();
                if (await _resultCDHABL.Update(resultCDHA, _userLogin.Value, _dateTime, _device))
                {
                    if (await _patientCDHABL.Update(resultCDHA.patientId, resultCDHA.returnResultTime, resultCDHA.userReturnResult, false, false, true, _userLogin.Value, _NS))
                    {
                        var _result = await _resultCDHABL.GetResultCDHAByPatientId_ForValidPrint(resultCDHA.resultCDHAId);
                        if (_result != null)
                        {
                            try
                            {
                                ViewData["ResultCDHA"] = _result;
                                var _hospital = await _hospitalBL.GetHospital();
                                var content = await this.RenderViewAsync("Content", _hospital);
                                var header = await this.RenderViewAsync("Header", _hospital);
                                var footer = await this.RenderViewAsync("Footer", _hospital);
                                var _folder = Path.Combine(_environment.WebRootPath, "pdf", "ns");
                                var _file = Path.Combine(_folder, _result?.KeyResultForHis + ".pdf");
                                var fileBase64 = await _toolBL.ExportPdf_Result(_folder, _file, header, content, footer);
                                return Content(fileBase64);
                            }
                            catch { }
                        }
                    }
                }
            }
            return Content(string.Empty);
        }

        public long? GetUserLogin()
        {
            long? userId = null;
            try
            {
                userId = long.Parse(HttpContext?.User?.Claims?.FirstOrDefault(p => p.Type == "UserId")?.Value);
            }
            catch { }
            return userId;
        }

        public Device GetSession_Device()
        {
            string _value = HttpContext.Request.Cookies[SessionKeyModel._sessionDeviceNS];
            Device _device = null;
            if (!string.IsNullOrEmpty(_value))
            {
                _device = JsonConvert.DeserializeObject<Device>(_value);
            }
            return _device;
        }


        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetUserInfo(long userId)
        {
            var u = await _userBL.GetUser(userId);
            if (u == null) return Json(null);

            // Trả về các field bạn cần dùng ở UI/PDF
            return Json(new
            {
                u.Name,
                u.Active,
                u.MaBHYT,
                u.Cccd
            });
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> SaveSignStoreIdForResultCDHA(long resultCDHAId, long signStoreId)
        {
            var _dateTime = ToolBL.Get_DateNow();
            var _userLogin = this.GetUserLogin();

            if (resultCDHAId <= 0 || signStoreId < 0)
                return BadRequest("resultCDHAId hoặc signStoreId không hợp lệ.");

            try
            {
                // Gọi BL để lưu (giả định chữ ký: UpdateSignStoreId_CKS(long id, string signStoreId))
                var ok = await _resultCDHABL.UpdateSignStoreId_CKS(resultCDHAId, signStoreId, _userLogin.Value, _dateTime);
                if (ok == true)
                {
                    var _resultCDHA = await _resultCDHABL.GetResultCDHA(resultCDHAId);
                    var _keyResult = _resultCDHA.KeyResultForHis;
                    return Json(new { success = true, keyResult = _keyResult });
                }
                else
                {
                    return Json(new { success = false, message = "Lỗi lưu signStoreId." });

                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SaveSignStoreIdForResultCDHA failed");
                return StatusCode(500, "Lỗi lưu signStoreId.");
            }
        }
    }
}
