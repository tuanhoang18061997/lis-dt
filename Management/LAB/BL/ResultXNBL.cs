using Management.Models;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Runtime.InteropServices;
using Object = Management.Models.Object;

namespace Management.BL
{
    public class ResultXNBL
    {
        private readonly LABContext _db;
        private readonly ServiceTestBL _serviceTestBL;
        public ResultXNBL(LABContext db, ServiceTestBL serviceTestBL)
        {
            _db = db;
            _serviceTestBL = serviceTestBL;
        }

        public async Task<ResultXN> GetResultXN(long id)
        {
            try
            {
                return await _db.ResultXNs.Where(p => p.Active == true && p.Id == id).FirstOrDefaultAsync();
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> DeleteByPatientId(long patientId, long? userInsertOrUpdate)
        {
            try
            {
                var _lstResultXN = await _db.ResultXNs.Where(p => p.Active == true && p.PatientId == patientId).ToListAsync();
                if (_lstResultXN != null)
                {
                    foreach (var _item in _lstResultXN)
                    {
                        _item.Active = false;
                        _item.UserUpdateId = userInsertOrUpdate;
                    }
                    await _db.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteById(long id, long? userInsertOrUpdate)
        {
            try
            {
                var _resultXN = await _db.ResultXNs.Where(p => p.Active == true && p.Id == id).FirstOrDefaultAsync();
                if (_resultXN != null)
                {
                    if (_resultXN.TestCode.IsTestHead)
                    {
                        var _lstChild = await _db.ResultXNs.Where(p => p.Active == true && p.PatientId == _resultXN.PatientId && p.ServiceId == _resultXN.ServiceId).ToListAsync();
                        if (_lstChild != null)
                        {
                            foreach (var _item in _lstChild)
                            {
                                _item.Active = false;
                                _item.UserUpdateId = userInsertOrUpdate;
                            }
                            await _db.SaveChangesAsync();
                        }
                    }
                    else
                    {
                        _resultXN.Active = false;
                        _resultXN.UserUpdateId = userInsertOrUpdate;
                        await _db.SaveChangesAsync();
                    }
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> SaveService(long patientId, long serviceId, long? userInsertIdOrUpdateId, DateTime dateTime, long doctorId)
        {
            try
            {
                var _lstServiceTest = await _serviceTestBL.GetListServiceTestByServiceId(serviceId);
                if (_lstServiceTest == null || _lstServiceTest.Count == 0) return false;

                var _lstResultXN = await _db.ResultXNs.Where(p => p.Active == true && p.PatientId == patientId).OrderBy(p => p.InsertTime).ToListAsync();
                var _resultXN = _lstResultXN.Where(p => p.ServiceId == serviceId).FirstOrDefault();
                if (_resultXN != null) return false;

                var keyResultForHis = "xn-" + Guid.NewGuid().ToString();
                if (_lstResultXN != null && _lstResultXN.Count > 0) keyResultForHis = _lstResultXN[0].KeyResultForHis;
                
                foreach (var item in _lstServiceTest)
                {
                    _resultXN = new ResultXN();
                    _resultXN.PatientId = patientId;
                    _resultXN.TestCodeId = item.TestCodeId;
                    _resultXN.ServiceId = item.ServiceId;
                    _resultXN.UserInsertId = userInsertIdOrUpdateId;
                    _resultXN.ValidPrint = true;
                    _resultXN.Status = 0;
                    _resultXN.StatusResult = StatusForResult.NotResult;
                    _resultXN.Result = item.TestCode.IsTestHead ? "." : string.Empty;
                    _resultXN.InsertTime = dateTime;
                    _resultXN.DoctorId = doctorId;
                    _resultXN.KeyResultForHis = keyResultForHis;
                    _resultXN.Active = true;
                    await _db.ResultXNs.AddAsync(_resultXN);
                }
                await _db.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<ResultXN>> GetListResultXNByPatientId_ForValidPrint(long patientId)
        {
            try
            {
                return await _db.ResultXNs.Where(p => p.Active == true && p.PatientId == patientId && p.ValidPrint == true).OrderBy(p => p.Service.Category.PrintOrder).ThenBy(p => p.Service.PrintOrder).ThenBy(p => p.TestCode.PrintOrder).ToListAsync();
            }
            catch
            {
                return null;
            }
        }

        public async Task<List<ResultXN>> GetListResultXNByPatientId(long patientId)
        {
            try
            {
                return await _db.ResultXNs.Where(p => p.Active == true && p.PatientId == patientId).OrderBy(p => p.Service.Category.PrintOrder).ThenBy(p => p.Service.PrintOrder).ThenBy(p => p.TestCode.PrintOrder).ToListAsync();
            }
            catch
            {
                return null;
            }
        }

        public async Task<List<Search>> GetListSearchByPatientId(long patientId)
        {
            try
            {
                var lstSearch = new List<Search>();

                var lstSearchXN = (from result in _db.ResultXNs
                                   where result.PatientId == patientId && result.Active == true
                                   select new Search
                                   {
                                       KeyResultForHis = result.KeyResultForHis,
                                       ServiceId = result.Service.Id,
                                       ServiceName = result.Service.Name,
                                       Status = result.Patient.WaitXN ? "Wait" : (result.Patient.ProcessXN ? "Process" : "Valid")
                                   }).Distinct().ToList();
                lstSearch.AddRange(lstSearchXN);

                var lstResultCDHA = await _db.ResultCDHAs.Where(p => p.Active == true && p.PatientId == patientId).ToListAsync();
                if (lstResultCDHA != null)
                {
                    foreach (var item in lstResultCDHA)
                    {
                        if (item.Service.Category.Code == "SA")
                        {
                            lstSearch.Add(new Search
                            {
                                KeyResultForHis = item.KeyResultForHis,
                                ServiceId = item.Service.Id,
                                ServiceName = item.Service.Name,
                                Status = item.Patient.WaitSA ? "Wait" : (item.Patient.ProcessSA ? "Process" : "Valid")
                            });
                        }
                        else if (item.Service.Category.Code == "SAT")
                        {
                            lstSearch.Add(new Search
                            {
                                KeyResultForHis = item.KeyResultForHis,
                                ServiceId = item.Service.Id,
                                ServiceName = item.Service.Name,
                                Status = item.Patient.WaitSAT ? "Wait" : (item.Patient.ProcessSAT ? "Process" : "Valid")
                            });
                        }
                        else if (item.Service.Category.Code == "DDT")
                        {
                            lstSearch.Add(new Search
                            {
                                KeyResultForHis = item.KeyResultForHis,
                                ServiceId = item.Service.Id,
                                ServiceName = item.Service.Name,
                                Status = item.Patient.WaitDDT ? "Wait" : (item.Patient.ProcessDDT ? "Process" : "Valid")
                            });
                        }
                        else if (item.Service.Category.Code == "XQ")
                        {
                            lstSearch.Add(new Search
                            {
                                KeyResultForHis = item.KeyResultForHis,
                                ServiceId = item.Service.Id,
                                ServiceName = item.Service.Name,
                                Status = item.Patient.WaitXQ ? "Wait" : (item.Patient.ProcessXQ ? "Process" : "Valid")
                            });
                        }
                        else if (item.Service.Category.Code == "NS")
                        {
                            lstSearch.Add(new Search
                            {
                                KeyResultForHis = item.KeyResultForHis,
                                ServiceId = item.Service.Id,
                                ServiceName = item.Service.Name,
                                Status = item.Patient.WaitNS ? "Wait" : (item.Patient.ProcessNS ? "Process" : "Valid")
                            });
                        }
                    }
                }
                return lstSearch;
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> ExitResultXN_Update_GetSampleTime(long patientId, DateTime getSampleTime)
        {
            try
            {
                var resultXN = await _db.ResultXNs.Where(p => p.Active == true && p.PatientId == patientId).FirstOrDefaultAsync();
                if (resultXN != null)
                {
                    resultXN.Patient.GetSampleTimeXN = getSampleTime;
                    await _db.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> Update(List<ResultXNModel> lstResult, bool isValidPrint, long? userInsertOrUpdate)
        {
            try
            {
                foreach (var item in lstResult)
                {
                    var _resultXN = await _db.ResultXNs.Where(p => p.Active == true && p.Id == item.id).FirstOrDefaultAsync();
                    if (_resultXN == null) continue;
                    _resultXN.Status = item.status;
                    _resultXN.Result = item.result;
                    if (isValidPrint)
                    {
                        _resultXN.ValidPrint = (item.validPrint && !string.IsNullOrEmpty(item.result)) ? true : false;
                    }
                    else
                    {
                        _resultXN.ValidPrint = item.validPrint;
                    }
                    _resultXN.UserUpdateId = userInsertOrUpdate;
                }
                await _db.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> FullResultXN(long patientId)
        {
            var _item = await _db.ResultXNs.Where(p => p.Active == true && p.PatientId == patientId && string.IsNullOrEmpty(p.Result.Trim()) && string.IsNullOrEmpty(p.PosNeg.Trim())).FirstOrDefaultAsync();
            if (_item != null)
                return false;
            else
                return true;
        }

        public async Task<bool> Delete(string ticketItemId)
        {
            try
            {
                var _lstResultXN = await _db.ResultXNs.Where(p => p.Active == true && p.TicketItemId == ticketItemId && p.Patient.WaitXN == true).ToListAsync();
                if (_lstResultXN != null)
                {
                    foreach (var _item in _lstResultXN)
                    {
                        _item.Active = false;
                    }
                    await _db.SaveChangesAsync();
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> UpdateSignStoreId_CKS(
            long resultXNId,
            long signStoreId,
            long userInsertIdOrUpdateId,
            DateTime dateTime)
        {
            try
            {
                if (resultXNId <= 0 || signStoreId <= 0)
                    return false;

                // Tìm record
                var resultXNItem = await _db.ResultXNs
                    .Where(p => p.Active == true && p.Id == resultXNId)
                    .FirstOrDefaultAsync();

                if (resultXNItem == null)
                    return false;

                // Gán giá trị mới
                resultXNItem.SignStoreId = signStoreId;
                resultXNItem.UserUpdateId = userInsertIdOrUpdateId;
                resultXNItem.UpdateTime = dateTime;
                resultXNItem.SignStatus = 1;

                await _db.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
