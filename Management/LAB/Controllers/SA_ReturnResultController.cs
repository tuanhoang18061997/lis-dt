using Management.BL;
using Management.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Management.Controllers
{
    public class SA_ReturnResultController : Controller
    {
        public readonly ILogger<SA_ReturnResultController> _logger;
        public readonly PatientCDHABL _patientCDHABL;
        public readonly ObjectBL _objectBL;
        public readonly LocationBL _locationBL;
        public readonly DoctorBL _doctorBL;
        public readonly UserBL _userBL;
        public readonly CategoryBL _categoryBL;
        public readonly ServiceBL _serviceBL;
        public readonly ResultCDHABL _resultCDHABL;
        public readonly SettingBL _settingBL;
        public readonly GroupBL _groupBL;
        private readonly ToolBL _toolBL;
        private readonly HospitalBL _hospitalBL;
        private readonly IWebHostEnvironment _environment;
        public readonly string _SA = "SA";

        public SA_ReturnResultController(ILogger<SA_ReturnResultController> logger, PatientCDHABL patientBL, ObjectBL objectBL, LocationBL locationBL,
                            DoctorBL doctorBL, UserBL userBL, CategoryBL categoryBL, ServiceBL serviceBL, ResultCDHABL resultCDHABL, SettingBL settingBL, 
                            GroupBL groupBL, IWebHostEnvironment environment, ToolBL toolBL, HospitalBL hospitalBL)
        {
            _logger = logger;
            _patientCDHABL = patientBL;
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
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> ReturnResult()
        {
            var dateTimeNow = ToolBL.Get_DateNow();
            var from = new DateTime(dateTimeNow.Year, dateTimeNow.Month, dateTimeNow.Day, 23, 59, 59).AddDays(-1);
            var to = new DateTime(dateTimeNow.Year, dateTimeNow.Month, dateTimeNow.Day, 23, 59, 59);

            ViewData["lstPatient"] = await _patientCDHABL.Get_ListPatient(from, to, false, false, true, _SA);
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
            var countGetSample = await _patientCDHABL.Get_CountPatient(from, to, true, false, false, _SA);
            var countProcess = await _patientCDHABL.Get_CountPatient(from, to, false, true, false, _SA);
            var countReturnResult = await _patientCDHABL.Get_CountPatient(from, to, false, false, true, _SA);
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
            ViewData["lstPatient"] = await _patientCDHABL.Get_ListPatientByPidOrSid(from, to, false, false, true, null, _SA);

            return PartialView("_SA_ReturnResult_ListPatient");
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Search(string pidorseq, DateTime timeSearchFrom, DateTime timeSearchTo)
        {
            var from = new DateTime(timeSearchFrom.Year, timeSearchFrom.Month, timeSearchFrom.Day, 23, 59, 59).AddDays(-1);
            var to = new DateTime(timeSearchTo.Year, timeSearchTo.Month, timeSearchTo.Day, 23, 59, 59);
            ViewData["lstPatient"] = await _patientCDHABL.Get_ListPatientByPidOrSid(from, to, false, false, true, pidorseq, _SA);

            return PartialView("_SA_ReturnResult_ListPatient");
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetPatientInfo(long id)
        {
            ViewData["lstBenhAnModel"] = await BenhAnModel.GetListBenhAnModel();
            ViewData["patientInfo"] = await _patientCDHABL.Get_PatientBySid(id);

            return PartialView("_SA_ReturnResult_PatientInfo");
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetServiceForPatient(long patientId)
        {
            ViewData["lstResultCDHAs"] = await _resultCDHABL.GetListResultCDHAByPatientId(patientId, _SA);
            return PartialView("_SA_ReturnResult_ListService");
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

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Invalid(long id)
        {
            var _userLogin = this.GetUserLogin();
            var dateTimeNow = ToolBL.Get_DateNow();
            var from = new DateTime(dateTimeNow.Year, dateTimeNow.Month, dateTimeNow.Day, 23, 59, 59).AddDays(-1);
            var to = new DateTime(dateTimeNow.Year, dateTimeNow.Month, dateTimeNow.Day, 23, 59, 59);
            var _process = await _patientCDHABL.GetSample_ProcessResult_ReturnResult(id, false, true, false, _userLogin.Value, _SA);
            return Content(_process.ToString());
        }


        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Print(long resultCDHAId)
        {
            var _result = await _resultCDHABL.GetResultCDHAByPatientId_ForValidPrint(resultCDHAId);
            var fileBase64 = string.Empty;
            if (_result != null)
            {
                var _folder = Path.Combine(_environment.WebRootPath, "pdf", "sa");
                var _file = Path.Combine(_folder, _result?.KeyResultForHis + ".pdf");
                try
                {
                    if (System.IO.File.Exists(_file))
                    {
                        fileBase64 = Convert.ToBase64String(System.IO.File.ReadAllBytes(_file));
                    }
                    else
                    {
                        ViewData["ResultCDHA"] = _result;
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

        //[HttpGet]
        //public async Task<IActionResult> Print()
        //{
        //    var _result = await _resultCDHABL.GetResultCDHAByPatientId_ForValidPrint(45);
        //    ViewData["ResultCDHA"] = _result;
        //    var _hospital = await _hospitalBL.GetHospital();
        //    return View(_hospital);
        //}


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
        public async Task<IActionResult> GetSignStoreId(long resultId)
        {
            if (resultId <= 0)
                return Json(new { signStoreId = (string?)null });

            // Tận dụng BL hiện có để lấy record kết quả rồi đọc SignStoreId
            var result = await _resultCDHABL.GetResultCDHA(resultId);
            var id = result?.SignStoreId;          // tên property đúng với cột DB của bạn

            // Trả JSON để JS dùng trực tiếp
            return Json(new { signStoreId = id });
        }
    }
}
