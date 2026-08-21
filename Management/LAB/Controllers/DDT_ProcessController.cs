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
    public class DDT_ProcessController : Controller
    {
        private readonly ILogger<DDT_ProcessController> _logger;
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
        private readonly IWebHostEnvironment _environment;
        public readonly string _DDT = "DDT";
        public readonly DeviceBL _deviceBL;

        public DDT_ProcessController(ILogger<DDT_ProcessController> logger, PatientCDHABL patientCDHABL, ObjectBL objectBL, LocationBL locationBL,
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

            ViewData["lstPatient"] = await _patientCDHABL.Get_ListPatient(from, to, false, true, false, _DDT);
            ViewData["lstUser"] = await _userBL.GetListUser(UserTypeModel.DDT);
            ViewData["lstBenhAnModel"] = await BenhAnModel.GetListBenhAnModel();
            ViewData["lstSampleResult"] = await _sampleBL.GetListSampleByCategory(_DDT);
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
            var countGetSample = await _patientCDHABL.Get_CountPatient(from, to, true, false, false, _DDT);
            var countProcess = await _patientCDHABL.Get_CountPatient(from, to, false, true, false, _DDT);
            var countReturnResult = await _patientCDHABL.Get_CountPatient(from, to, false, false, true, _DDT);
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
            ViewData["lstPatient"] = await _patientCDHABL.Get_ListPatientByPidOrSid(from, to, false, true, false, null, _DDT);

            return PartialView("_DDT_Process_ListPatient");
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Search(string pidorseq, DateTime timeSearchFrom, DateTime timeSearchTo)
        {
            var from = new DateTime(timeSearchFrom.Year, timeSearchFrom.Month, timeSearchFrom.Day, 23, 59, 59).AddDays(-1);
            var to = new DateTime(timeSearchTo.Year, timeSearchTo.Month, timeSearchTo.Day, 23, 59, 59);
            ViewData["lstPatient"] = await _patientCDHABL.Get_ListPatientByPidOrSid(from, to, false, true, false, pidorseq, _DDT);

            return PartialView("_DDT_Process_ListPatient");
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> GetSample(long id)
        {
            var _userLogin = this.GetUserLogin();
            var _getSample = await _patientCDHABL.GetSample_ProcessResult_ReturnResult(id, true, false, false, _userLogin.Value, _DDT);
            return Content(_getSample.ToString());
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetPatientInfo(long id)
        {
            var _userLoginId = this.GetUserLogin();
            ViewData["userLoginId"] = _userLoginId;
            ViewData["lstUser"] = await _userBL.GetListUser(UserTypeModel.DDT);
            ViewData["lstBenhAnModel"] = await BenhAnModel.GetListBenhAnModel();
            ViewData["patientInfo"] = await _patientCDHABL.Get_PatientBySid(id);

            return PartialView("_DDT_Process_PatientInfo");
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetServiceForPatient(long patientId)
        {
            ViewData["lstResultCDHAs"] = await _resultCDHABL.GetListResultCDHAByPatientId(patientId, _DDT);
            return PartialView("_DDT_Process_ListService");
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
        public async Task<IActionResult> Get_Description_Result_Suggest_ForService(long id)
        {
            var _resultCDHA = await _resultCDHABL.GetResultCDHA(id);
            if (_resultCDHA != null)
            {
                var _lstItem = new List<object>() { new { Description = _resultCDHA.Description, Result = _resultCDHA.Result, Suggest = _resultCDHA.Suggest } };
                return Json(_lstItem);
            }
            else
            {
                return Json(null);
            }
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
                await _patientCDHABL.Update(resultCDHA.patientId, resultCDHA.returnResultTime, resultCDHA.userReturnResult, _userLogin.Value, _dateTime, _DDT);
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
                    if (await _patientCDHABL.Update(resultCDHA.patientId, resultCDHA.returnResultTime, resultCDHA.userReturnResult, false, false, true, _userLogin.Value, _DDT))
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
                                var _folder = Path.Combine(_environment.WebRootPath, "pdf", "ddt");
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

        //[HttpPost]
        //[Authorize]
        //public async Task<IActionResult> ValidPrint([FromBody] ResultCDHAModel resultCDHA)
        //{
        //    if (resultCDHA != null)
        //    {
        //        var _dateTime = ToolBL.Get_DateNow();
        //        var _userLogin = this.GetUserLogin();
        //        var _device = this.GetSession_Device();
        //        if (await _resultCDHABL.Update(resultCDHA, _userLogin.Value, _dateTime, _device))
        //        {
        //            if (await _patientCDHABL.Update(resultCDHA.patientId, resultCDHA.returnResultTime, resultCDHA.userReturnResult, false, false, true, _userLogin.Value, _SA))
        //            {
        //                var _result = await _resultCDHABL.GetResultCDHAByPatientId_ForValidPrint(resultCDHA.resultCDHAId);
        //                if (_result != null)
        //                {
        //                    try
        //                    {
        //                        var bytePdf = await CreatePdf(_result);
        //                        var fileBase64 = Convert.ToBase64String(bytePdf);
        //                        return Content(fileBase64);
        //                    }
        //                    catch { }
        //                }
        //            }
        //        }
        //    }
        //    return Content(string.Empty);
        //}

        //public async Task<byte[]> CreatePdf(ResultCDHA model)
        //{
        //    try
        //    {
        //        return Document.Create(container =>
        //        {
        //            container.Page(page =>
        //            {
        //                page.Size(PageSizes.A4.Portrait());
        //                page.MarginTop(10);
        //                page.MarginLeft(30);
        //                page.MarginRight(20);
        //                page.MarginBottom(10);
        //                page.DefaultTextStyle(TextStyle.Default.FontSize(13).FontColor(Colors.Black).FontFamily(Fonts.TimesNewRoman));

        //                page.Content().Column(columns =>
        //                {
        //                    columns.Item().Row(row =>
        //                    {
        //                        row.RelativeItem().ExtendHorizontal().PaddingRight(10).Column(col1 =>
        //                        {
        //                            col1.Spacing(2);
        //                            col1.Item().Row(row1 =>
        //                            {
        //                                row1.RelativeItem().Column(col3 =>
        //                                {
        //                                    var env = HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>();
        //                                    var logoPath = Path.Combine(env.WebRootPath, "images", "logo", "Logo.png");
        //                                    var logoByte = System.IO.File.ReadAllBytes(logoPath);
        //                                    col3.Item().PaddingTop(5).Width(120).Image(logoByte);
        //                                });

        //                                row1.RelativeItem().Column(col3 =>
        //                                {

        //                                });
        //                            });

        //                            col1.Item().PaddingTop(10).AlignCenter().Text($"PHIẾU KẾT QUẢ ĐO VẬN TỐC SÓNG MẠCH").Style(TextStyle.Default.FontSize(18)).Bold();

        //                            col1.Item().PaddingTop(10).Row(row1 =>
        //                            {
        //                                row1.RelativeItem().Column(col3 =>
        //                                {
        //                                    col3.Item().ExtendHorizontal().AlignLeft().Text(text =>
        //                                    {
        //                                        text.Span("Họ tên: ").Bold();
        //                                    });
        //                                });

        //                                row1.ConstantItem(70).Column(col3 =>
        //                                {
        //                                    col3.Item().ExtendHorizontal().AlignLeft().Text(text =>
        //                                    {
        //                                        text.Span("Tuổi: ").Bold();
        //                                    });
        //                                });

        //                                row1.ConstantItem(110).Column(col3 =>
        //                                {
        //                                    col3.Item().ExtendHorizontal().AlignLeft().Text(text =>
        //                                    {
        //                                        text.Span("Giới tính: ").Bold();
        //                                    });
        //                                });
        //                            });

        //                            col1.Item().PaddingTop(3).Row(row1 =>
        //                            {
        //                                row1.RelativeItem().Column(col3 =>
        //                                {
        //                                    col3.Item().ExtendHorizontal().AlignLeft().Text(text =>
        //                                    {
        //                                        text.Span("Địa chỉ: ").Bold();
        //                                    });
        //                                });
        //                            });

        //                            col1.Item().PaddingTop(3).Row(row1 =>
        //                            {
        //                                row1.RelativeItem().Column(col3 =>
        //                                {
        //                                    col3.Item().ExtendHorizontal().AlignLeft().Text(text =>
        //                                    {
        //                                        text.Span("Khoa phòng: ").Bold();
        //                                    });
        //                                });
        //                                row1.RelativeItem().Column(col3 =>
        //                                {
        //                                    col3.Item().ExtendHorizontal().AlignLeft().Text(text =>
        //                                    {
        //                                        text.Span("Bác sĩ chỉ định: ").Bold();
        //                                    });
        //                                });
        //                            });

        //                            col1.Item().PaddingTop(3).Row(row1 =>
        //                            {
        //                                row1.RelativeItem().Column(col3 =>
        //                                {
        //                                    col3.Item().ExtendHorizontal().AlignLeft().Text(text =>
        //                                    {
        //                                        text.Span("Chẩn đoán: ").Bold();
        //                                    });
        //                                });
        //                            });

        //                            //if (model?.Images != null && model?.Images?.Length >= 2)
        //                            //{
        //                            //    var path = Path.Combine("wwwroot", model?.Images[1]?.HinhAnhUrl.TrimStart('/'));
        //                            //    byte[] hinh = System.IO.File.ReadAllBytes(path);
        //                            //    col1.Item().AlignCenter().PaddingTop(2).Width(490).Height(530).Image(hinh).FitUnproportionally();
        //                            //}

        //                            col1.Item().PaddingTop(10).EnsureSpace().Row(row1 =>
        //                            {
        //                                row1.RelativeItem().Text("");
        //                                row1.ConstantItem(150).Text("");
        //                                row1.RelativeItem().Text(text =>
        //                                {
        //                                    var time = DateTime.Now;
        //                                    text.Line("Ngày " + time.ToString("dd") + " tháng " + time.ToString("MM") + " năm " + time.ToString("yyyy")).Italic();
        //                                    text.Line($"Bác sĩ đọc kết quả").Bold();
        //                                    text.Line("(Ký, ghi rõ họ tên)").Italic();
        //                                    text.Span("VITRICHUKYBSTHUCHIEN").FontColor("#FFFFFF").FontSize(10f);
        //                                    text.AlignCenter();
        //                                });
        //                            });
        //                        });
        //                    });
        //                });

        //                page.Footer().AlignCenter().Text(text =>
        //                {
        //                    text.Span("Lưu ý: mang theo kết quả này cho lần khám sau").FontSize(11).Italic();
        //                });
        //            });
        //        }).GeneratePdf();
        //    }
        //    catch
        //    {
        //        return null;
        //    }
        //}

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
                    HttpContext.Response.Cookies.Append(SessionKeyModel._sessionDeviceDDT, value, cookieOptions);
                    _status = true;
                }
            }
            catch { }
            return Content(_status.ToString());
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
            string _value = HttpContext.Request.Cookies[SessionKeyModel._sessionDeviceDDT];
            Device _device = null;
            if (!string.IsNullOrEmpty(_value))
            {
                _device = JsonConvert.DeserializeObject<Device>(_value);
            }
            return _device;
        }
    }
}
