using Management.BL;
using Management.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Management.Controllers
{
    public class XN_ReturnResultController : Controller
    {
        public readonly ILogger<XN_ReturnResultController> _logger;
        public readonly PatientXNBL _patientBL;
        public readonly ObjectBL _objectBL;
        public readonly LocationBL _locationBL;
        public readonly DoctorBL _doctorBL;
        public readonly UserBL _userBL;
        public readonly CategoryBL _categoryBL;
        public readonly ServiceBL _serviceBL;
        public readonly ResultXNBL _resultXNBL;
        public readonly SettingBL _settingBL;
        public readonly GroupBL _groupBL;
        private readonly ToolBL _toolBL;
        private readonly HospitalBL _hospitalBL;
        private readonly IWebHostEnvironment _environment;

        public XN_ReturnResultController(ILogger<XN_ReturnResultController> logger, PatientXNBL patientBL, ObjectBL objectBL, LocationBL locationBL,
                            DoctorBL doctorBL, UserBL userBL, CategoryBL categoryBL, ServiceBL serviceBL, ResultXNBL resultXNBL, SettingBL settingBL, 
                            GroupBL groupBL, IWebHostEnvironment environment, ToolBL toolBL, HospitalBL hospitalBL)
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
        public async Task<IActionResult> ReturnResult()
        {
            var dateTimeNow = ToolBL.Get_DateNow();
            var from = new DateTime(dateTimeNow.Year, dateTimeNow.Month, dateTimeNow.Day, 23, 59, 59).AddDays(-1);
            var to = new DateTime(dateTimeNow.Year, dateTimeNow.Month, dateTimeNow.Day, 23, 59, 59);

            ViewData["lstPatient"] = await _patientBL.Get_ListPatient(from, to, false, false, true);
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
            ViewData["lstPatient"] = await _patientBL.Get_ListPatientByPidOrSid(from, to, false, false, true, null);

            return PartialView("_XN_ReturnResult_ListPatient");
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Search(string pidorseq, DateTime timeSearchFrom, DateTime timeSearchTo)
        {
            var from = new DateTime(timeSearchFrom.Year, timeSearchFrom.Month, timeSearchFrom.Day, 23, 59, 59).AddDays(-1);
            var to = new DateTime(timeSearchTo.Year, timeSearchTo.Month, timeSearchTo.Day, 23, 59, 59);
            ViewData["lstPatient"] = await _patientBL.Get_ListPatientByPidOrSid(from, to, false, false, true, pidorseq);

            return PartialView("_XN_ReturnResult_ListPatient");
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Invalid(long id)
        {
            var _userLogin = this.GetUserLogin();
            var dateTimeNow = ToolBL.Get_DateNow();
            var from = new DateTime(dateTimeNow.Year, dateTimeNow.Month, dateTimeNow.Day, 23, 59, 59).AddDays(-1);
            var to = new DateTime(dateTimeNow.Year, dateTimeNow.Month, dateTimeNow.Day, 23, 59, 59);
            var _process = await _patientBL.GetSample_ProcessResult_ReturnResult(id, false, true, false, _userLogin.Value);
            return Content(_process.ToString());
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetPatientInfo(long id)
        {
            ViewData["lstBenhAnModel"] = await BenhAnModel.GetListBenhAnModel();
            ViewData["patientInfo"] = await _patientBL.Get_PatientBySid(id);

            return PartialView("_XN_ReturnResult_PatientInfo");
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetServiceForPatient(long patientId)
        {
            ViewData["lstResultXNs"] = await _resultXNBL.GetListResultXNByPatientId(patientId);
            return PartialView("_XN_ReturnResult_ListService");
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Print(long patientId, string sid)
        {
            var _lstResult = await _resultXNBL.GetListResultXNByPatientId_ForValidPrint(patientId);
            var fileBase64 = string.Empty;
            if (_lstResult != null)
            {
                var _folder = Path.Combine(_environment.WebRootPath, "pdf", "xn");
                var _file = Path.Combine(_folder, _lstResult[0]?.KeyResultForHis + ".pdf");
                try
                {
                    if (System.IO.File.Exists(_file))
                    {
                        fileBase64 = Convert.ToBase64String(System.IO.File.ReadAllBytes(_file));
                    }
                    else
                    {
                        ViewData["ListResultXN"] = _lstResult;
                        var _hospital = await _hospitalBL.GetHospital();
                        var content = await this.RenderViewAsync("Content", _hospital);
                        var header = await this.RenderViewAsync("Header", _hospital);
                        var footer = await this.RenderViewAsync("Footer", _hospital);
                        fileBase64 = await _toolBL.ExportPdf_Result(_folder, _file, header, content, footer);
                    }
                }
                catch { }
            }
            return Content(fileBase64);
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
        public async Task<IActionResult> GetSignStoreId(long patientId)
        {
            if (patientId <= 0)
                return Json(new { signStoreId = (string?)null });
            long? id = null;
            // Tận dụng BL hiện có để lấy record kết quả rồi đọc SignStoreId
            var result = await _resultXNBL.GetListResultXNByPatientId(patientId);
            if (result.Count > 0)
            {
                id = result[0].SignStoreId;          // tên property đúng với cột DB của bạn
            }

            // Trả JSON để JS dùng trực tiếp
            return Json(new { signStoreId = id });
        }
    }
}
