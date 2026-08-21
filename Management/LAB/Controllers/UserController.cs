using Castle.Core.Internal;
using Management.BL;
using Management.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Globalization;
using System.Net;

namespace Management.Controllers
{
    public class UserController : Controller
    {
        public readonly ILogger<UserController> _logger;
        public readonly HospitalBL _hospitalBL;
        public readonly UserBL _userBL;
        public readonly PatientXNBL _patientBL;

        public UserController(ILogger<UserController> logger, HospitalBL hospitalBL, UserBL userBL, PatientXNBL patientBL)
        {
            _logger = logger;
            _hospitalBL = hospitalBL;
            _userBL = userBL;
            _patientBL = patientBL;
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            ViewData["lstUser"] = await _userBL.GetListUser(true);
            ViewData["lstType"] = await _userBL.GetListType();
            return View();
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Search(string value)
        {
            ViewData["lstUser"] = await _userBL.GetListUserByValue(value, true);
            ViewData["lstType"] = await _userBL.GetListType();
            return PartialView("_User_List");
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetInfo(string code)
        {
            ViewData["User"] = await _userBL.GetUser(code, true);
            ViewData["lstType"] = await _userBL.GetListType();
            return PartialView("_User_Info");
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetList()
        {
            ViewData["lstUser"] = await _userBL.GetListUser(true);
            return PartialView("_User_List");
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetFunction(string code)
        {
            ViewData["User"] = await _userBL.GetUser(code, true);
            ViewData["lstFunction"] = await _userBL.GetListFunction();
            return PartialView("_User_Function");
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Save([FromBody] UserModel user)
        {
            var _save = false;
            if (user != null)
            {
                _save = await _userBL.Save(user);
            }
            return Content(_save.ToString());
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Delete(string code)
        {
            var _delete = await _userBL.Delete(code);
            return Content(_delete.ToString());
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Save_UserFunction([FromBody] UserFunctionModel userFunction)
        {
            var _save = false;
            if (userFunction != null)
            {
                _save = await _userBL.Save_UserFunction(userFunction);
            }
            return Content(_save.ToString());
        }
    }
}