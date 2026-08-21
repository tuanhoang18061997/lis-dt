using Management.Models;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Reflection;
using Object = Management.Models.Object;

namespace Management.BL
{
    public class ReportBL
    {
        private readonly LABContext _db;
        public ReportBL(LABContext db)
        {
            _db = db;
        }

        public async Task<List<TotalReportService>> GetReportByService(DateTime fromtime, DateTime totime, string location, string user, long? hospitalId)
        {
            try
            {
                var fromTimeString = fromtime.AddDays(1).ToString("dd/MM/yyyy");
                var toTimeString = totime.ToString("dd/MM/yyyy");
                var dateNow = DateTime.Now.ToString("dd/MM/yyyy");
                var lstTotalReportService = new List<TotalReportService>();
                var lstReportService = new List<ReportService>();

                if (location == "XN")
                {
                    if(hospitalId == null)
                    {
                        lstReportService = (from patient in _db.Patients
                                            join result in _db.ResultXNs on patient.Id equals result.PatientId
                                            where patient.InsertTime > fromtime && patient.InsertTime <= totime && patient.ValidXN == true && result.Active == true
                                            select new ReportService
                                            {
                                                SID = result.Patient.Sid,
                                                ServiceID = result.ServiceId.ToString(),
                                                ServiceName = result.Service.Name,
                                                BenhAn = patient.BenhAn, // in => Nội trú, out => Ngoại trú
                                                ObjectCode = patient.Object.Code // 01 => Thu phí, 02 => BHYT, 03 => BHBL, 04 => DV
                                            }).Distinct().ToList();
                    }
                    else
                    {
                        lstReportService = (from patient in _db.Patients
                                            join result in _db.ResultXNs on patient.Id equals result.PatientId
                                            where patient.InsertTime > fromtime && patient.InsertTime <= totime && patient.ValidXN == true && result.Active == true && patient.HospitalId == hospitalId
                                            select new ReportService
                                            {
                                                SID = result.Patient.Sid,
                                                ServiceID = result.ServiceId.ToString(),
                                                ServiceName = result.Service.Name,
                                                BenhAn = patient.BenhAn, // in => Nội trú, out => Ngoại trú
                                                ObjectCode = patient.Object.Code // 01 => Thu phí, 02 => BHYT, 03 => BHBL, 04 => DV
                                            }).Distinct().ToList();
                    }
                    

                    lstReportService = (from item in lstReportService
                                        group item by new { item.ServiceID, item.ServiceName, item.BenhAn, item.ObjectCode } into p
                                        select new ReportService
                                        {
                                            ServiceID = p.Key.ServiceID,
                                            ServiceName = p.Key.ServiceName,
                                            BenhAn = p.Key.BenhAn,
                                            ObjectCode = p.Key.ObjectCode,
                                            Count = p.Count()
                                        }).ToList();
                }
                else if (location == "SA")
                {
                    lstReportService = (from patient in _db.Patients
                                        join result in _db.ResultCDHAs on patient.Id equals result.PatientId
                                        join service in _db.Services on result.ServiceId equals service.Id
                                        where patient.InsertTime > fromtime && patient.InsertTime <= totime && result.Active == true && ((service.Category.Code == "SA" && patient.ValidSA == true) || (service.Category.Code == "SAT" && patient.ValidSAT == true))
                                        select new ReportService
                                        {
                                            SID = result.Patient.Sid,
                                            ServiceID = result.ServiceId.ToString(),
                                            ServiceName = result.Service.Name,
                                            BenhAn = patient.BenhAn, // in => Nội trú, out => Ngoại trú
                                            ObjectCode = patient.Object.Code // 01 => Thu phí, 02 => BHYT, 03 => BHBL, 04 => DV
                                        }).Distinct().ToList();

                    lstReportService = (from item in lstReportService
                                        group item by new { item.ServiceID, item.ServiceName, item.BenhAn, item.ObjectCode } into p
                                        select new ReportService
                                        {
                                            ServiceID = p.Key.ServiceID,
                                            ServiceName = p.Key.ServiceName,
                                            BenhAn = p.Key.BenhAn,
                                            ObjectCode = p.Key.ObjectCode,
                                            Count = p.Count()
                                        }).ToList();
                }
                else if (location == "XQ")
                {
                    lstReportService = (from patient in _db.Patients
                                        join result in _db.ResultCDHAs on patient.Id equals result.PatientId
                                        join service in _db.Services on result.ServiceId equals service.Id
                                        where patient.InsertTime > fromtime && patient.InsertTime <= totime && result.Active == true && service.Category.Code == "XQ" && patient.ValidXQ == true
                                        select new ReportService
                                        {
                                            SID = result.Patient.Sid,
                                            ServiceID = result.ServiceId.ToString(),
                                            ServiceName = result.Service.Name,
                                            BenhAn = patient.BenhAn, // in => Nội trú, out => Ngoại trú
                                            ObjectCode = patient.Object.Code // 01 => Thu phí, 02 => BHYT, 03 => BHBL, 04 => DV
                                        }).Distinct().ToList();

                    lstReportService = (from item in lstReportService
                                        group item by new { item.ServiceID, item.ServiceName, item.BenhAn, item.ObjectCode } into p
                                        select new ReportService
                                        {
                                            ServiceID = p.Key.ServiceID,
                                            ServiceName = p.Key.ServiceName,
                                            BenhAn = p.Key.BenhAn,
                                            ObjectCode = p.Key.ObjectCode,
                                            Count = p.Count()
                                        }).ToList();
                }
                else if (location == "NS")
                {
                    lstReportService = (from patient in _db.Patients
                                        join result in _db.ResultCDHAs on patient.Id equals result.PatientId
                                        join service in _db.Services on result.ServiceId equals service.Id
                                        where patient.InsertTime > fromtime && patient.InsertTime <= totime && result.Active == true && service.Category.Code == "NS" && patient.ValidNS == true
                                        select new ReportService
                                        {
                                            SID = result.Patient.Sid,
                                            ServiceID = result.ServiceId.ToString(),
                                            ServiceName = result.Service.Name,
                                            BenhAn = patient.BenhAn, // in => Nội trú, out => Ngoại trú
                                            ObjectCode = patient.Object.Code // 01 => Thu phí, 02 => BHYT, 03 => BHBL, 04 => DV
                                        }).Distinct().ToList();

                    lstReportService = (from item in lstReportService
                                        group item by new { item.ServiceID, item.ServiceName, item.BenhAn, item.ObjectCode } into p
                                        select new ReportService
                                        {
                                            ServiceID = p.Key.ServiceID,
                                            ServiceName = p.Key.ServiceName,
                                            BenhAn = p.Key.BenhAn,
                                            ObjectCode = p.Key.ObjectCode,
                                            Count = p.Count()
                                        }).ToList();
                }

                if (lstReportService != null)
                {
                    var stt = 0;
                    foreach (var i in lstReportService)
                    {
                        var itemOld = lstTotalReportService.Where(p => p.ServiceID == i.ServiceID).FirstOrDefault();
                        if (itemOld != null)
                        {
                            if (i.ObjectCode == "BHYT" || i.ObjectCode == "BHBL")
                            {
                                itemOld.TotalBHYT += i.Count;
                                if (i.BenhAn == "in")
                                    itemOld.TotalBHYT_NoiTru += i.Count;
                                else
                                    itemOld.TotalBHYT_NgoaiTru += i.Count;
                            }
                            else
                            {
                                itemOld.TotalTP += i.Count;
                                if (i.BenhAn == "in")
                                    itemOld.TotalTP_NoiTru += i.Count;
                                else
                                    itemOld.TotalTP_NgoaiTru += i.Count;
                            }
                        }
                        else
                        {
                            stt++;
                            var itemNew = new TotalReportService();
                            itemNew.FromDate = fromTimeString;
                            itemNew.ToDate = toTimeString;
                            itemNew.DateNow = dateNow;
                            itemNew.User = user;
                            itemNew.STT = stt;
                            itemNew.ServiceID = i.ServiceID;
                            itemNew.ServiceName = i.ServiceName;
                            if (i.ObjectCode == "BHYT" || i.ObjectCode == "BHBL")
                            {
                                itemNew.TotalBHYT = i.Count;
                                if (i.BenhAn == "in")
                                    itemNew.TotalBHYT_NoiTru = i.Count;
                                else
                                    itemNew.TotalBHYT_NgoaiTru = i.Count;
                            }
                            else
                            {
                                itemNew.TotalTP = i.Count;
                                if (i.BenhAn == "in")
                                    itemNew.TotalTP_NoiTru = i.Count;
                                else
                                    itemNew.TotalTP_NgoaiTru = i.Count;
                            }
                            lstTotalReportService.Add(itemNew);
                        }
                    }
                }

                return lstTotalReportService;
            }
            catch
            {
                return null;
            }
        }

        public async Task<List<TotalReportPatient>> GetReportByPatient(DateTime fromTime, DateTime toTime, string location, string user, long? hospitalId)
        {
            try
            {
                var fromTimeString = fromTime.AddDays(1).ToString("dd/MM/yyyy");
                var toTimeString = toTime.ToString("dd/MM/yyyy");
                var dateNow = DateTime.Now.ToString("dd/MM/yyyy");
                var lstTotalReportPatient = new List<TotalReportPatient>();
                var lstReportPatient = new List<ReportPatient>();

                if (location == "XN")
                {
                    if (hospitalId == null)
                    {
                        lstReportPatient = (from patient in _db.Patients
                                            join result in _db.ResultXNs on patient.Id equals result.PatientId
                                            where patient.InsertTime > fromTime && patient.InsertTime <= toTime && patient.ValidXN == true
                                            group patient by new { patient.PatientId, DayInSID = patient.Sid.Substring(0, 6), ObjectCode = patient.Object.Code, patient.BenhAn } into p
                                            select new ReportPatient
                                            {
                                                PatientID = p.Key.PatientId,
                                                DayInSID = p.Key.DayInSID,
                                                BenhAn = p.Key.BenhAn, // in => Nội trú, out => Ngoại trú
                                                ObjectCode = p.Key.ObjectCode, // 01 => Thu phí, 02 => BHYT, 03 => BHBL, 04 => DV
                                            }).ToList();
                    }
                    else
                    {
                        lstReportPatient = (from patient in _db.Patients
                                            join result in _db.ResultXNs on patient.Id equals result.PatientId
                                            where patient.InsertTime > fromTime && patient.InsertTime <= toTime && patient.ValidXN == true && patient.HospitalId == hospitalId
                                            group patient by new { patient.PatientId, DayInSID = patient.Sid.Substring(0, 6), ObjectCode = patient.Object.Code, patient.BenhAn } into p
                                            select new ReportPatient
                                            {
                                                PatientID = p.Key.PatientId,
                                                DayInSID = p.Key.DayInSID,
                                                BenhAn = p.Key.BenhAn, // in => Nội trú, out => Ngoại trú
                                                ObjectCode = p.Key.ObjectCode, // 01 => Thu phí, 02 => BHYT, 03 => BHBL, 04 => DV
                                            }).ToList();
                    }
                }
                else if (location == "SA")
                {
                    lstReportPatient = (from patient in _db.Patients
                                        join result in _db.ResultCDHAs on patient.Id equals result.PatientId
                                        join service in _db.Services on result.ServiceId equals service.Id
                                        where patient.InsertTime > fromTime && patient.InsertTime <= toTime && ((service.Category.Code == "SA" && patient.ValidSA == true) || (service.Category.Code == "SAT" && patient.ValidSAT == true))
                                        group patient by new { patient.PatientId, DayInSID = patient.Sid.Substring(0, 6), ObjectCode = patient.Object.Code, patient.BenhAn } into p
                                        select new ReportPatient
                                        {
                                            PatientID = p.Key.PatientId,
                                            DayInSID = p.Key.DayInSID,
                                            BenhAn = p.Key.BenhAn, // in => Nội trú, out => Ngoại trú
                                            ObjectCode = p.Key.ObjectCode, // 01 => Thu phí, 02 => BHYT, 03 => BHBL, 04 => DV
                                        }).ToList();
                }
                else if (location == "XQ")
                {
                    lstReportPatient = (from patient in _db.Patients
                                        join result in _db.ResultCDHAs on patient.Id equals result.PatientId
                                        join service in _db.Services on result.ServiceId equals service.Id
                                        where patient.InsertTime > fromTime && patient.InsertTime <= toTime && service.Category.Code == "XQ" && patient.ValidXQ == true
                                        group patient by new { patient.PatientId, DayInSID = patient.Sid.Substring(0, 6), ObjectCode = patient.Object.Code, patient.BenhAn } into p
                                        select new ReportPatient
                                        {
                                            PatientID = p.Key.PatientId,
                                            DayInSID = p.Key.DayInSID,
                                            BenhAn = p.Key.BenhAn, // in => Nội trú, out => Ngoại trú
                                            ObjectCode = p.Key.ObjectCode, // 01 => Thu phí, 02 => BHYT, 03 => BHBL, 04 => DV
                                        }).ToList();
                }
                else if (location == "NS")
                {
                    lstReportPatient = (from patient in _db.Patients
                                        join result in _db.ResultCDHAs on patient.Id equals result.PatientId
                                        join service in _db.Services on result.ServiceId equals service.Id
                                        where patient.InsertTime > fromTime && patient.InsertTime <= toTime && service.Category.Code == "NS" && patient.ValidNS == true
                                        group patient by new { patient.PatientId, DayInSID = patient.Sid.Substring(0, 6), ObjectCode = patient.Object.Code, patient.BenhAn } into p
                                        select new ReportPatient
                                        {
                                            PatientID = p.Key.PatientId,
                                            DayInSID = p.Key.DayInSID,
                                            BenhAn = p.Key.BenhAn, // in => Nội trú, out => Ngoại trú
                                            ObjectCode = p.Key.ObjectCode, // 01 => Thu phí, 02 => BHYT, 03 => BHBL, 04 => DV
                                        }).ToList();
                }

                if (lstReportPatient != null)
                {
                    var item = new TotalReportPatient();
                    item.FromDate = fromTimeString;
                    item.ToDate = toTimeString;
                    item.DateNow = dateNow;
                    item.User = user;
                    foreach (var i in lstReportPatient)
                    {
                        if (i.ObjectCode == "BHYT" || i.ObjectCode == "BHBL")
                        {
                            item.TotalBHYT++;
                            if (i.BenhAn == "in")
                                item.TotalBHYT_NoiTru++;
                            else
                                item.TotalBHYT_NgoaiTru++;
                        }
                        else
                        {
                            item.TotalTP++;
                            if (i.BenhAn == "in")
                                item.TotalTP_NoiTru++;
                            else
                                item.TotalTP_NgoaiTru++;
                        }
                    }

                    lstTotalReportPatient.Add(item);
                }

                return lstTotalReportPatient;
            }
            catch
            {
                return null;
            }
        }

        public async Task<List<ReportProcess>> GetReportByProcess(DateTime fromTime, DateTime toTime, string location, string user, long? hospitalId)
        {
            try
            {
                var fromTimeString = fromTime.AddDays(1).ToString("dd/MM/yyyy");
                var toTimeString = toTime.ToString("dd/MM/yyyy");
                var dateNow = DateTime.Now.ToString("dd/MM/yyyy");
                var lstReportProcess = new List<ReportProcess>();
                var lstResult = new List<ReportProcess>();

                if (location == "XN")
                {
                    if (hospitalId == null)
                    {
                        lstResult = (from result in _db.ResultXNs
                                     join patient in _db.Patients on result.PatientId equals patient.Id
                                     join us in _db.Users on patient.UserReturnResultXN equals us.Id
                                     join service in _db.Services on result.ServiceId equals service.Id
                                     where result.InsertTime > fromTime && result.InsertTime <= toTime && patient.ValidXN == true && result.Active == true
                                     group result by new { ServiceId = result.ServiceId, ServiceName = result.Service.Name, UserId = us.Id, Sid = result.Patient.Sid, UserName = us.Name } into p
                                     select new ReportProcess
                                     {
                                         DoctorID = p.Key.UserId.ToString(),
                                         DoctorName = p.Key.UserName,
                                         ServiceID = p.Key.ServiceId.ToString(),
                                         ServiceName = p.Key.ServiceName,
                                         FromDate = fromTimeString,
                                         ToDate = toTimeString,
                                         DateNow = dateNow,
                                         User = user
                                     }).ToList();
                    }
                    else
                    {
                        lstResult = (from result in _db.ResultXNs
                                     join patient in _db.Patients on result.PatientId equals patient.Id
                                     join us in _db.Users on patient.UserReturnResultXN equals us.Id
                                     join service in _db.Services on result.ServiceId equals service.Id
                                     where result.InsertTime > fromTime && result.InsertTime <= toTime && patient.ValidXN == true && result.Active == true && patient.HospitalId == hospitalId
                                     group result by new { ServiceId = result.ServiceId, ServiceName = result.Service.Name, UserId = us.Id, Sid = result.Patient.Sid, UserName = us.Name } into p
                                     select new ReportProcess
                                     {
                                         DoctorID = p.Key.UserId.ToString(),
                                         DoctorName = p.Key.UserName,
                                         ServiceID = p.Key.ServiceId.ToString(),
                                         ServiceName = p.Key.ServiceName,
                                         FromDate = fromTimeString,
                                         ToDate = toTimeString,
                                         DateNow = dateNow,
                                         User = user
                                     }).ToList();
                    }

                    lstResult = (from item in lstResult
                                 group item by new { item.DoctorID, item.DoctorName, item.ServiceID, item.ServiceName, item.FromDate, item.ToDate, item.User } into p
                                 select new ReportProcess
                                 {
                                     DoctorID = p.Key.DoctorID,
                                     DoctorName = p.Key.DoctorName,
                                     ServiceID = p.Key.ServiceID,
                                     ServiceName = p.Key.ServiceName,
                                     FromDate = fromTimeString,
                                     ToDate = toTimeString,
                                     DateNow = dateNow,
                                     User = user,
                                     Count = p.Count()
                                 }).ToList();

                }
                else if (location == "SA")
                {
                    lstResult = (from resultImage in _db.ResultCDHAs
                                 join patient in _db.Patients on resultImage.PatientId equals patient.Id
                                 join us in _db.Users on patient.UserReturnResultSA equals us.Id
                                 join service in _db.Services on resultImage.ServiceId equals service.Id
                                 where resultImage.InsertTime > fromTime && resultImage.InsertTime <= toTime && resultImage.Active == true && service.Category.Code == "SA" && patient.ValidSA == true
                                 group resultImage by new { ServiceId = resultImage.ServiceId, ServiceName = resultImage.Service.Name, UserId = us.Id, UserName = us.Name } into p
                                 select new ReportProcess
                                 {
                                     DoctorID = p.Key.UserId.ToString(),
                                     DoctorName = p.Key.UserName,
                                     ServiceID = p.Key.ServiceId.ToString(),
                                     ServiceName = p.Key.ServiceName,
                                     FromDate = fromTimeString,
                                     ToDate = toTimeString,
                                     DateNow = dateNow,
                                     User = user,
                                     Count = p.Count()
                                 }).ToList();

                    var lstResultSAT = (from resultImage in _db.ResultCDHAs
                                        join patient in _db.Patients on resultImage.PatientId equals patient.Id
                                        join us in _db.Users on patient.UserReturnResultSAT equals us.Id
                                        join service in _db.Services on resultImage.ServiceId equals service.Id
                                        where resultImage.InsertTime > fromTime && resultImage.InsertTime <= toTime && service.Category.Code == "SAT" && patient.ValidSAT == true
                                        group resultImage by new { ServiceId = resultImage.ServiceId, ServiceName = resultImage.Service.Name, UserId = us.Id, UserName = us.Name } into p
                                        select new ReportProcess
                                        {
                                            DoctorID = p.Key.UserId.ToString(),
                                            DoctorName = p.Key.UserName,
                                            ServiceID = p.Key.ServiceId.ToString(),
                                            ServiceName = p.Key.ServiceName,
                                            FromDate = fromTimeString,
                                            ToDate = toTimeString,
                                            DateNow = dateNow,
                                            User = user,
                                            Count = p.Count()
                                        }).ToList();

                    lstResult.AddRange(lstResultSAT);
                }
                else if (location == "XQ")
                {
                    lstResult = (from resultImage in _db.ResultCDHAs
                                 join patient in _db.Patients on resultImage.PatientId equals patient.Id
                                 join us in _db.Users on patient.UserReturnResultXQ equals us.Id
                                 join service in _db.Services on resultImage.ServiceId equals service.Id
                                 where resultImage.InsertTime > fromTime && resultImage.InsertTime <= toTime && resultImage.Active == true && service.Category.Code == "XQ" && patient.ValidXQ == true
                                 group resultImage by new { ServiceId = resultImage.ServiceId, ServiceName = resultImage.Service.Name, UserId = us.Id, UserName = us.Name } into p
                                 select new ReportProcess
                                 {
                                     DoctorID = p.Key.UserId.ToString(),
                                     DoctorName = p.Key.UserName,
                                     ServiceID = p.Key.ServiceId.ToString(),
                                     ServiceName = p.Key.ServiceName,
                                     FromDate = fromTimeString,
                                     ToDate = toTimeString,
                                     DateNow = dateNow,
                                     User = user,
                                     Count = p.Count()
                                 }).ToList();
                }
                else if (location == "NS")
                {
                    lstResult = (from resultImage in _db.ResultCDHAs
                                 join patient in _db.Patients on resultImage.PatientId equals patient.Id
                                 join us in _db.Users on patient.UserReturnResultNS equals us.Id
                                 join service in _db.Services on resultImage.ServiceId equals service.Id
                                 where resultImage.InsertTime > fromTime && resultImage.InsertTime <= toTime && resultImage.Active == true && service.Category.Code == "NS" && patient.ValidNS == true
                                 group resultImage by new { ServiceId = resultImage.ServiceId, ServiceName = resultImage.Service.Name, UserId = us.Id, UserName = us.Name } into p
                                 select new ReportProcess
                                 {
                                     DoctorID = p.Key.UserId.ToString(),
                                     DoctorName = p.Key.UserName,
                                     ServiceID = p.Key.ServiceId.ToString(),
                                     ServiceName = p.Key.ServiceName,
                                     FromDate = fromTimeString,
                                     ToDate = toTimeString,
                                     DateNow = dateNow,
                                     User = user,
                                     Count = p.Count()
                                 }).ToList();
                }

                return lstResult;
            }
            catch
            {
                return null;
            }
        }

        public async Task<List<ReportCovid>> GetReportByTestCovid(DateTime fromTime, DateTime toTime, string user, long? hospitalId)
        {
            try
            {
                if (hospitalId == null)
                {
                    var fromTimeString = fromTime.AddDays(1).ToString("dd/MM/yyyy");
                    var toTimeString = toTime.ToString("dd/MM/yyyy");
                    var dateNow = DateTime.Now.ToString("dd/MM/yyyy");
                    var lstReportCovid = (from patient in _db.Patients
                                          join result in _db.ResultXNs on patient.Id equals result.PatientId
                                          where patient.InsertTime > fromTime && patient.InsertTime <= toTime && patient.ValidXN == true &&
                                                result.Service.ProcessType == true
                                          select new ReportCovid
                                          {
                                              SID = patient.Sid,
                                              PatientID = patient.PatientId,
                                              PatientName = patient.PatientName,
                                              Sex = patient.Sex,
                                              Age = patient.Age,
                                              Address = patient.Address,
                                              ServiceID = result.ServiceId.ToString(),
                                              ServiceName = result.Service.Name,
                                              TestCode = result.TestCode.Code,
                                              Testname = result.TestCode.Name,
                                              Result = result.Result,
                                              InsertTime = patient.InsertTime ?? DateTime.Now,
                                              GetSampleTime = patient.GetSampleTimeXN ?? DateTime.Now,
                                              ReturnResultTime = patient.ReturnResultTimeXN ?? DateTime.Now,
                                              User = user,
                                              FromDate = fromTimeString,
                                              ToDate = toTimeString,
                                              DateNow = dateNow,
                                          }).ToList();


                    return lstReportCovid;
                }
                else
                {
                    var fromTimeString = fromTime.AddDays(1).ToString("dd/MM/yyyy");
                    var toTimeString = toTime.ToString("dd/MM/yyyy");
                    var dateNow = DateTime.Now.ToString("dd/MM/yyyy");
                    var lstReportCovid = (from patient in _db.Patients
                                          join result in _db.ResultXNs on patient.Id equals result.PatientId
                                          where patient.InsertTime > fromTime && patient.InsertTime <= toTime && patient.ValidXN == true &&
                                                result.Service.ProcessType == true && patient.HospitalId == hospitalId
                                          select new ReportCovid
                                          {
                                              SID = patient.Sid,
                                              PatientID = patient.PatientId,
                                              PatientName = patient.PatientName,
                                              Sex = patient.Sex,
                                              Age = patient.Age,
                                              Address = patient.Address,
                                              ServiceID = result.ServiceId.ToString(),
                                              ServiceName = result.Service.Name,
                                              TestCode = result.TestCode.Code,
                                              Testname = result.TestCode.Name,
                                              Result = result.Result,
                                              InsertTime = patient.InsertTime ?? DateTime.Now,
                                              GetSampleTime = patient.GetSampleTimeXN ?? DateTime.Now,
                                              ReturnResultTime = patient.ReturnResultTimeXN ?? DateTime.Now,
                                              User = user,
                                              FromDate = fromTimeString,
                                              ToDate = toTimeString,
                                              DateNow = dateNow,
                                          }).ToList();


                    return lstReportCovid;
                }
            }
            catch (Exception ex)
            {
                return null;
            }
        }
    }
}
