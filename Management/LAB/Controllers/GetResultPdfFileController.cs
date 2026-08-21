using Management.BL;
using Management.Models;
using Microsoft.AspNetCore.Mvc;

namespace Management.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GetResultPdfFileController : ControllerBase
    {
        private readonly IWebHostEnvironment _environment;
        private readonly APIBL _apiBL;
        private readonly ServiceBL _serviceBL;
        private readonly ResultXNBL _resultXNBL;
        private readonly ResultCDHABL _resultCDHABL;
        private readonly PatientXNBL _patientXNBL;
        private readonly PatientCDHABL _patientCDHABL;

        public GetResultPdfFileController(IWebHostEnvironment environment, APIBL apiBL, ServiceBL serviceBL, ResultXNBL resultXNBL, ResultCDHABL resultCDHABL, PatientXNBL patientXNBL, PatientCDHABL patientCDHABL)
        {
            _environment = environment;
            _apiBL = apiBL;
            _serviceBL = serviceBL;
            _resultXNBL = resultXNBL;
            _resultCDHABL = resultCDHABL;
            _patientXNBL = patientXNBL;
            _patientCDHABL = patientCDHABL;
        }

        [HttpGet]
        public async Task<IActionResult> GetResultPdfFile(string sid)
        {
            var categoryCode = sid.Split('-')[0];
            var _folder = Path.Combine(_environment.WebRootPath, "pdf", categoryCode);
            var _fileName = sid + ".pdf";
            var _filePath = Path.Combine(_folder, _fileName);

            if (System.IO.File.Exists(_filePath))
            {
                var fileByte = System.IO.File.ReadAllBytes(_filePath);
                return File(fileByte, "application/pdf");
            }
            return BadRequest();
        }

        //[HttpPost]
        //public async Task<IActionResult> AddService([FromBody] IEnumerable<PatientInfo_Add> lstPatientInfo)
        //{
        //    try
        //    {
        //        if (lstPatientInfo != null && lstPatientInfo.Count() > 0)
        //        {
        //            var listUpdateStatusTicketItemId = new List<StatusTicketItemId>();
        //            var lstSIDForTicketItem = new List<SIDForTicketItem>();
        //            var dateTimeServer = DateTime.Now;
        //            var groupPatientInfo = lstPatientInfo.GroupBy(p => p.PatientId);
        //            if (groupPatientInfo != null)
        //            {
        //                foreach (var group in groupPatientInfo)
        //                {
        //                    try
        //                    {
        //                        var lstGroup = group.ToList();
        //                        if (lstGroup != null && lstGroup.Count > 0)
        //                        {
        //                            Patient patient = null;
        //                            DateTime? age = null;
        //                            var sex = string.Empty;
        //                            var objectName = "Thu phí";
        //                            try
        //                            {
        //                                age = DateTime.Parse(lstGroup[0].Age);
        //                            }
        //                            catch { }

        //                            if (lstGroup[0].Sex.Trim() == "male")
        //                            {
        //                                sex = "Nam";
        //                            }
        //                            else
        //                            {
        //                                sex = "Nữ";
        //                            }

        //                            if (!string.IsNullOrEmpty(lstGroup[0].ObjectName.Trim()))
        //                            {
        //                                objectName = lstGroup[0].ObjectName.Trim();
        //                            }

        //                            try
        //                            {
        //                                patient = await _apiBL.AddPatient(null, null, lstGroup[0].PatientId, lstGroup[0].TicketId, lstGroup[0].PatientName, sex, lstGroup[0].Address, age, lstGroup[0].Diagnostic, objectName, lstGroup[0].LocationName, lstGroup[0].DoctorName, dateTimeServer, lstGroup[0].AssignDate, lstGroup[0].Type);
        //                            }
        //                            catch (Exception ex)
        //                            {
        //                                foreach (var item in lstGroup)
        //                                {
        //                                    if (item != null)
        //                                    {
        //                                        listUpdateStatusTicketItemId.Add(new StatusTicketItemId { ticket_item_id = item.TicketItemId, type = item.Type, status = StatusForHIS.open });
        //                                    }
        //                                }
        //                                continue;
        //                            }
        //                            if (patient == null) continue;
        //                            var keyResultForHis_XN = "xn-" + Guid.NewGuid().ToString();
        //                            var keyResultForHis_CDHA = string.Empty;
        //                            foreach (var item in lstGroup)
        //                            {
        //                                if (item != null)
        //                                {
        //                                    try
        //                                    {
        //                                        var service = await _apiBL.GetService(item.ServiceId);
        //                                        if (service != null)
        //                                        {
        //                                            if (service.Category.Group.Code == "XN")
        //                                            {
        //                                                await _apiBL.AddResultXN(patient.Id, item.TicketItemId, dateTimeServer, service, item.DoctorName, keyResultForHis_XN);
        //                                                lstSIDForTicketItem.Add(new SIDForTicketItem { ticketItems = item.TicketItemId, sid = keyResultForHis_XN });
        //                                            }
        //                                            else if (service.Category.Group.Code == "CDHA")
        //                                            {
        //                                                keyResultForHis_CDHA = service.Category.Code.ToLower() + "-" + Guid.NewGuid().ToString();
        //                                                await _apiBL.AddResultCDHA(patient.Id, item.TicketItemId, dateTimeServer, service, item.DoctorName, keyResultForHis_CDHA);
        //                                                lstSIDForTicketItem.Add(new SIDForTicketItem { ticketItems = item.TicketItemId, sid = keyResultForHis_CDHA });
        //                                            }
        //                                            listUpdateStatusTicketItemId.Add(new StatusTicketItemId { ticket_item_id = item.TicketItemId, type = item.Type, status = StatusForHIS.waitting });
        //                                        }
        //                                    }
        //                                    catch
        //                                    {
        //                                        listUpdateStatusTicketItemId.Add(new StatusTicketItemId { ticket_item_id = item.TicketItemId, type = item.Type, status = StatusForHIS.open });
        //                                    }
        //                                }
        //                            }
        //                        }
        //                    }
        //                    catch { }
        //                }
        //            }

        //            if (listUpdateStatusTicketItemId != null && listUpdateStatusTicketItemId.Count > 0)
        //            {
        //                await _apiBL.UpdateStatusTicketItemId(listUpdateStatusTicketItemId);
        //            }
        //            if (lstSIDForTicketItem != null && lstSIDForTicketItem.Count > 0)
        //            {
        //                await _apiBL.UpdateSIDForTicketItemId(lstSIDForTicketItem);
        //            }
        //        }
        //        return Content("True");
        //    }
        //    catch
        //    {
        //        return Content("False");
        //    }
        //}

        //[HttpPost]
        //public async Task<IActionResult> DeleteService([FromBody] IEnumerable<PatientInfo_Del> lstPatientInfo)
        //{
        //    try
        //    {
        //        if (lstPatientInfo != null && lstPatientInfo.Count() > 0)
        //        {
        //            foreach (var item in lstPatientInfo)
        //            {
        //                var service = await _serviceBL.Get(item.ServiceId);
        //                if (service == null) continue;
        //                var sid = string.Empty;
        //                if (service.Category.Group.Code == "XN")
        //                {
        //                    await _resultXNBL.Delete(item.TicketItemId);
        //                }
        //                else if (service.Category.Group.Code == "CDHA")
        //                {
        //                    await _resultCDHABL.Delete(item.TicketItemId, service.Category.Code);
        //                }
        //            }
        //        }
        //        return Content("True");
        //    }
        //    catch
        //    {
        //        return Content("False");
        //    }
        //}
    }
}
