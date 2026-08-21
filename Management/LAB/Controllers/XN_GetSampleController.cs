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
    public class XN_GetSampleController : Controller
    {
        public readonly ILogger<XN_GetSampleController> _logger;
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
        public readonly HospitalBL _hospitalBL;

        public XN_GetSampleController(ILogger<XN_GetSampleController> logger, PatientXNBL patientBL, ObjectBL objectBL, LocationBL locationBL, HospitalBL hospitalBL,
                            DoctorBL doctorBL, UserBL userBL, CategoryBL categoryBL, ServiceBL serviceBL, ResultXNBL resultXNBL, SettingBL settingBL, GroupBL groupBL)
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
            _hospitalBL = hospitalBL;
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetSample()
        {
            var dateTimeNow = ToolBL.Get_DateNow();
            var from = new DateTime(dateTimeNow.Year, dateTimeNow.Month, dateTimeNow.Day, 23, 59, 59).AddDays(-1);
            var to = new DateTime(dateTimeNow.Year, dateTimeNow.Month, dateTimeNow.Day, 23, 59, 59);

            ViewData["lstPatient"] = await _patientBL.Get_ListPatient(from, to, true, false, false);
            ViewData["lstObject"] = await _objectBL.GetListObject();
            ViewData["lstLocation"] = await _locationBL.GetListLocation();
            ViewData["lstDoctor"] = await _doctorBL.GetList();
            ViewData["lstSexModel"] = await SexModel.GetListSexModel();
            ViewData["lstBenhAnModel"] = await BenhAnModel.GetListBenhAnModel();
            ViewData["lstCategory"] = await _categoryBL.GetListCategoryByGroup(GroupBL.XN);
            ViewData["lstService"] = await _serviceBL.GetListServiceByGroup(GroupBL.XN);
            ViewData["lstHospital"] = await _hospitalBL.GetListHospital();
            await this.Get_Count();
            GetUserLogin();

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
            ViewData["lstPatient"] = await _patientBL.Get_ListPatientByPidOrSid(from, to, true, false, false, null);

            return PartialView("_XN_GetSample_ListPatient");
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Search(string pidorseq, DateTime timeSearchFrom, DateTime timeSearchTo)
        {
            var from = new DateTime(timeSearchFrom.Year, timeSearchFrom.Month, timeSearchFrom.Day, 23, 59, 59).AddDays(-1);
            var to = new DateTime(timeSearchTo.Year, timeSearchTo.Month, timeSearchTo.Day, 23, 59, 59);
            ViewData["lstPatient"] = await _patientBL.Get_ListPatientByPidOrSid(from, to, true, false, false, pidorseq);

            return PartialView("_XN_GetSample_ListPatient");
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
            ViewData["patientInfo"] = await _patientBL.Get_PatientBySid(id);
            ViewData["lstHospital"] = await _hospitalBL.GetListHospital();

            return PartialView("_XN_GetSample_PatientInfo");
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetServiceForPatient(long patientId)
        {
            ViewData["lstResultXNs"] = await _resultXNBL.GetListResultXNByPatientId(patientId);
            ViewData["lstCategory"] = await _categoryBL.GetListCategoryByGroup(GroupBL.XN);
            ViewData["lstService"] = await _serviceBL.GetListServiceByGroup(GroupBL.XN);
            return PartialView("_XN_GetSample_ListService");
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
        public async Task<IActionResult> DeletePatientAndService(long id)
        {
            var _status = string.Empty;
            var _userLogin = this.GetUserLogin();
            if (await _resultXNBL.DeleteByPatientId(id, _userLogin.Value))
            {
                _status = await _patientBL.DeleteById(id) ? "/XN_GetSample/Refresh/" : string.Empty;
            }

            return Content(_status);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetSID(string seq)
        {
            var _sid = string.Empty;
            if(!string.IsNullOrEmpty(seq))
            {
                var _dateTime = ToolBL.Get_DateNow();
                _sid = _dateTime.Day.ToString().PadLeft(2, '0') + _dateTime.Month.ToString().PadLeft(2, '0') + _dateTime.Year.ToString().Substring(2, 2) + "-" + seq;
            }
            return Content(_sid);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> SavePatient(string id, string patientId, string seq, string sid, string patientName, DateTime age, 
                                                               string sex, string obj, string type, string location, string doctor, DateTime getSampleTime, 
                                                               string address, string diagnostic, string service, string hospital)
        {
            var _dateTime = ToolBL.Get_DateNow();
            if (string.IsNullOrEmpty(seq) && string.IsNullOrEmpty(sid))
            {
                seq = await _settingBL.GetSeq();
                sid = _dateTime.Day.ToString().PadLeft(2, '0') + _dateTime.Month.ToString().PadLeft(2, '0') + _dateTime.Year.ToString().Substring(2, 2) + "-" + seq;
            }
            var _userLogin = this.GetUserLogin();            
            long? _id = string.IsNullOrEmpty(id) ? null : long.Parse(id);
            if(string.IsNullOrEmpty(hospital) || hospital == "null")
            {
                var hospitalModel = await _hospitalBL.GetHospital();
                hospital = hospitalModel.Id.ToString();
            }
            var _patient = await _patientBL.SaveOrUpdate(_id, patientId, seq, sid, patientName, age, sex, obj, type, location, doctor, getSampleTime, address, diagnostic, _userLogin.Value, _dateTime, true, false, false, hospital);
            
            return Content(_patient == null ? string.Empty : _patient.Id.ToString());
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AddServiceForPatient(long patientId, long serviceId, long doctorId)
        {
            var _userLogin = this.GetUserLogin();
            var _dateTime = ToolBL.Get_DateNow();
            var _save = await _resultXNBL.SaveService(patientId, serviceId, _userLogin.Value, _dateTime, doctorId);

            return Content(_save.ToString());
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> DeleteServiceForPatient(long id)
        {
            var _userLogin = this.GetUserLogin();
            var _delete = await _resultXNBL.DeleteById(id, _userLogin.Value);
            return Content(_delete.ToString());
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> ProcessResult(long id, DateTime getSampleTime)
        {
            var _exitResultXN = await _resultXNBL.ExitResultXN_Update_GetSampleTime(id, getSampleTime);
            var _getSample = false;
            if (_exitResultXN)
            {
                var _userLogin = this.GetUserLogin();
                _getSample = await _patientBL.GetSample_ProcessResult_ReturnResult(id, false, true, false, _userLogin.Value);
            }          
            return Content(_getSample.ToString());
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
