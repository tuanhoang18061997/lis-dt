using Management.BL;
using Management.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SelectPdf;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using IHostingEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;

namespace Management.Controllers
{
    public class XN_ProcessController : Controller
    {
        private readonly ILogger<XN_ProcessController> _logger;
        private readonly PatientXNBL _patientBL;
        private readonly ObjectBL _objectBL;
        private readonly LocationBL _locationBL;
        private readonly DoctorBL _doctorBL;
        private readonly UserBL _userBL;
        private readonly CategoryBL _categoryBL;
        private readonly ServiceBL _serviceBL;
        private readonly ResultXNBL _resultXNBL;
        private readonly SettingBL _settingBL;
        private readonly GroupBL _groupBL;
        private readonly ToolBL _toolBL;
        private readonly HospitalBL _hospitalBL;
        private readonly IWebHostEnvironment _environment;

        public XN_ProcessController(ILogger<XN_ProcessController> logger, PatientXNBL patientBL, ObjectBL objectBL, LocationBL locationBL,
                            DoctorBL doctorBL, UserBL userBL, CategoryBL categoryBL, ServiceBL serviceBL, ResultXNBL resultXNBL,
                            SettingBL settingBL, GroupBL groupBL, IWebHostEnvironment environment, ToolBL toolBL, HospitalBL hospitalBL)
        {
            _logger = logger;
            _patientBL = patientBL;
            _objectBL = objectBL;
            _locationBL = locationBL;
            _doctorBL = doctorBL;
            _userBL = userBL;
            _categoryBL = categoryBL;
            _serviceBL = serviceBL;
            _resultXNBL = resultXNBL;
            _settingBL = settingBL;
            _groupBL = groupBL;
            _environment = environment;
            _toolBL = toolBL;
            _hospitalBL = hospitalBL;
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Process()
        {
            var dateTimeNow = ToolBL.Get_DateNow();
            var from = new DateTime(dateTimeNow.Year, dateTimeNow.Month, dateTimeNow.Day, 23, 59, 59).AddDays(-1);
            var to = new DateTime(dateTimeNow.Year, dateTimeNow.Month, dateTimeNow.Day, 23, 59, 59);

            ViewData["lstPatient"] = await _patientBL.Get_ListPatient(from, to, false, true, false);
            ViewData["lstUser"] = await _userBL.GetListUser(UserTypeModel.XN);
            ViewData["lstBenhAnModel"] = await BenhAnModel.GetListBenhAnModel();
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
            var countGetSample = await _patientBL.Get_CountPatient(from, to, true, false, false);
            var countProcessNotFullResult = await _patientBL.Get_CountPatientNotFullResult(from, to, false, true, false);
            var countProcessFullResult = await _patientBL.Get_CountPatientFullResult(from, to, false, true, false);
            var countReturnResult = await _patientBL.Get_CountPatient(from, to, false, false, true);
            ViewData["countGetSample"] = countGetSample;
            ViewData["countProcessNotFullResult"] = countProcessNotFullResult;
            ViewData["countProcessFullResult"] = countProcessFullResult;
            ViewData["countReturnResult"] = countReturnResult;

            return Content(countGetSample + ";" + countProcessNotFullResult + ";" + countProcessFullResult + ";" + countReturnResult);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Refresh()
        {
            var dateTimeNow = ToolBL.Get_DateNow();
            var from = new DateTime(dateTimeNow.Year, dateTimeNow.Month, dateTimeNow.Day, 23, 59, 59).AddDays(-1);
            var to = new DateTime(dateTimeNow.Year, dateTimeNow.Month, dateTimeNow.Day, 23, 59, 59);
            ViewData["lstPatient"] = await _patientBL.Get_ListPatientByPidOrSid(from, to, false, true, false, null);

            return PartialView("_XN_Process_ListPatient");
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Search(string pidorseq, DateTime timeSearchFrom, DateTime timeSearchTo)
        {
            var from = new DateTime(timeSearchFrom.Year, timeSearchFrom.Month, timeSearchFrom.Day, 23, 59, 59).AddDays(-1);
            var to = new DateTime(timeSearchTo.Year, timeSearchTo.Month, timeSearchTo.Day, 23, 59, 59);
            ViewData["lstPatient"] = await _patientBL.Get_ListPatientByPidOrSid(from, to, false, true, false, pidorseq);

            return PartialView("_XN_Process_ListPatient");
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> GetSample(long id)
        {
            var _userLogin = this.GetUserLogin();
            var _getSample = await _patientBL.GetSample_ProcessResult_ReturnResult(id, true, false, false, _userLogin.Value);
            return Content(_getSample.ToString());
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetPatientInfo(long id)
        {
            var _userLoginId = this.GetUserLogin();
            ViewData["userLoginId"] = _userLoginId;
            ViewData["lstUser"] = await _userBL.GetListUser(UserTypeModel.XN);
            ViewData["lstBenhAnModel"] = await BenhAnModel.GetListBenhAnModel();
            ViewData["patientInfo"] = await _patientBL.Get_PatientBySid(id);

            return PartialView("_XN_Process_PatientInfo");
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetServiceForPatient(long patientId)
        {
            ViewData["lstResultXNs"] = await _resultXNBL.GetListResultXNByPatientId(patientId);
            return PartialView("_XN_Process_ListService");
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetServiceByCategory(long? categoryId)
        {
            var _lstService = await _serviceBL.GetListServiceByCategory(categoryId);
            var _lstItem = _lstService.Select(p => new { Id = p.Id, Name = p.Name }).ToList();

            return Json(_lstItem);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> SaveResult([FromBody] List<ResultXNModel> lstResult)
        {
            var _saveResult = false;
            var _fullResultXN = false;
            if (lstResult != null && lstResult.Count > 0)
            {
                var _userLogin = this.GetUserLogin();
                _saveResult = await _resultXNBL.Update(lstResult, false, _userLogin.Value);
                _fullResultXN = await _resultXNBL.FullResultXN(lstResult[0].patientId);
                await _patientBL.Update(lstResult[0].patientId, lstResult[0].returnResultTime, lstResult[0].userReturnResult, _userLogin.Value, _fullResultXN);

            }
            return Content(_saveResult.ToString());
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> ValidPrint([FromBody] List<ResultXNModel> lstResult)
        {
            if (lstResult != null && lstResult.Count > 0)
            {
                var _userLogin = this.GetUserLogin();
                if (await _resultXNBL.Update(lstResult, true, _userLogin.Value))
                {
                    var _fullResultXN = await _resultXNBL.FullResultXN(lstResult[0].patientId);
                    if (await _patientBL.Update(lstResult[0].patientId, lstResult[0].returnResultTime, lstResult[0].userReturnResult, false, false, true, _userLogin.Value, _fullResultXN))
                    {
                        var _lstResult = await _resultXNBL.GetListResultXNByPatientId_ForValidPrint(lstResult[0].patientId);
                        if (_lstResult != null)
                        {
                            try
                            {
                                ViewData["ListResultXN"] = _lstResult;
                                var _hospital = await _hospitalBL.GetHospital();
                                var content = await this.RenderViewAsync("Content", _hospital);
                                var header = await this.RenderViewAsync("Header", _hospital);
                                var footer = await this.RenderViewAsync("Footer", _hospital);
                                var _folder = Path.Combine(_environment.WebRootPath, "pdf", "xn");
                                var _file = Path.Combine(_folder, _lstResult[0]?.KeyResultForHis + ".pdf");
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
        public async Task<IActionResult> SaveSignStoreIdForResultXN(long signStoreId, [FromBody] List<long> ids)
        {
            if (signStoreId <= 0 || ids == null || ids.Count == 0)
                return BadRequest("Dữ liệu không hợp lệ");

            var _dateTime = ToolBL.Get_DateNow();
            var _userLogin = this.GetUserLogin();
            if (!_userLogin.HasValue) return Unauthorized();
            var res = false;
            var objFirst = await _resultXNBL.GetResultXN(ids[0]);
            var _keyResult = objFirst != null ? objFirst.KeyResultForHis : null;
            foreach (var id in ids)
            {
                if (id > 0)
                {
                    res = await _resultXNBL.UpdateSignStoreId_CKS(id, signStoreId, _userLogin.Value, _dateTime);
                }
            }
            if (res == true && objFirst != null)
            {
                return Json(new { success = true, keyResult = _keyResult });
            }
            else
            {
                return Json(new { success = false, keyResult = _keyResult });
            }
        }
    }
}
