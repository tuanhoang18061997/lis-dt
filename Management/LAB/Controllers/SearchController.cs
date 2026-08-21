using Management.BL;
using Management.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net;
using System.Numerics;
using System.Security.Cryptography;
using Object = Management.Models.Object;

namespace Management.Controllers
{
    public class SearchController : Controller
    {
        public readonly ILogger<SearchController> _logger;
        public readonly PatientXNBL _patientXNBL;
        public readonly PatientCDHABL _patientCDHABL;
        public readonly ObjectBL _objectBL;
        public readonly LocationBL _locationBL;
        public readonly DoctorBL _doctorBL;
        public readonly UserBL _userBL;
        public readonly ServiceBL _serviceBL;
        public readonly ResultCDHABL _resultCDHABL;
        public readonly ResultXNBL _resultXNBL;
        private readonly ToolBL _toolBL;
        private readonly HospitalBL _hospitalBL;
        private readonly IWebHostEnvironment _environment;

        public SearchController(ILogger<SearchController> logger, PatientCDHABL patientCDHABL, ObjectBL objectBL, LocationBL locationBL, HospitalBL hospitalBL, IWebHostEnvironment webHostEnvironment,
                            DoctorBL doctorBL, UserBL userBL, ServiceBL serviceBL, ResultCDHABL resultCDHABL, PatientXNBL patientXNBL, ResultXNBL resultXNBL, ToolBL toolBL)
        {
            _logger = logger;
            _patientCDHABL = patientCDHABL;
            _objectBL = objectBL;
            _locationBL = locationBL;
            _doctorBL = doctorBL;
            _userBL = userBL;
            _serviceBL = serviceBL;
            _resultCDHABL = resultCDHABL;
            _patientXNBL = patientXNBL;
            _resultXNBL = resultXNBL;
            _toolBL = toolBL;
            _hospitalBL = hospitalBL;
            _environment = webHostEnvironment;
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Index()
        {
            var dateTimeNow = ToolBL.Get_DateNow();
            var from = new DateTime(dateTimeNow.Year, dateTimeNow.Month, dateTimeNow.Day, 23, 59, 59).AddDays(-1);
            var to = new DateTime(dateTimeNow.Year, dateTimeNow.Month, dateTimeNow.Day, 23, 59, 59);

            ViewData["lstPatient"] = await _patientXNBL.Get_ListPatient(from, to);
            ViewData["lstObject"] = await _objectBL.GetListObject();
            ViewData["lstLocation"] = await _locationBL.GetListLocation();
            ViewData["lstDoctor"] = await _doctorBL.GetList();
            ViewData["lstSexModel"] = await SexModel.GetListSexModel();
            ViewData["lstBenhAnModel"] = await BenhAnModel.GetListBenhAnModel();
            return View();
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Refresh()
        {
            var dateTimeNow = ToolBL.Get_DateNow();
            var from = new DateTime(dateTimeNow.Year, dateTimeNow.Month, dateTimeNow.Day, 23, 59, 59).AddDays(-1);
            var to = new DateTime(dateTimeNow.Year, dateTimeNow.Month, dateTimeNow.Day, 23, 59, 59);
            ViewData["lstPatient"] = await _patientXNBL.Get_ListPatient(from, to);

            return PartialView("_Search_ListPatient");
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Search(string pidorseq, DateTime timeSearchFrom, DateTime timeSearchTo)
        {
            var from = new DateTime(timeSearchFrom.Year, timeSearchFrom.Month, timeSearchFrom.Day, 23, 59, 59).AddDays(-1);
            var to = new DateTime(timeSearchTo.Year, timeSearchTo.Month, timeSearchTo.Day, 23, 59, 59);
            ViewData["lstPatient"] = await _patientXNBL.Get_ListPatientByPidOrSid(from, to, pidorseq);

            return PartialView("_Search_ListPatient");
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetPatientInfo(long id)
        {
            ViewData["lstObject"] = await _objectBL.GetListObject();
            ViewData["lstLocation"] = await _locationBL.GetListLocation();
            ViewData["lstDoctor"] = await _doctorBL.GetList();
            ViewData["lstSexModel"] = await SexModel.GetListSexModel();
            ViewData["lstBenhAnModel"] = await BenhAnModel.GetListBenhAnModel();
            ViewData["patientInfo"] = await _patientXNBL.Get_PatientBySid(id);

            return PartialView("_Search_PatientInfo");
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetServiceForPatient(long patientId)
        {
            ViewData["lstSearch"] = await _resultXNBL.GetListSearchByPatientId(patientId);
            return PartialView("_Search_ListService");
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Print(string keyresultforhis)
        {
            var fileBase64 = string.Empty;
            try
            {                
                var categoryCode = keyresultforhis.Split("-")[0];
                if (!string.IsNullOrEmpty(categoryCode))
                {
                    var _file = Path.Combine(_environment.WebRootPath, "pdf", categoryCode, keyresultforhis + ".pdf");
                    if (System.IO.File.Exists(_file))
                    {
                        fileBase64 = Convert.ToBase64String(System.IO.File.ReadAllBytes(_file));
                    }
                }
            }
            catch { }          
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
    }
}
