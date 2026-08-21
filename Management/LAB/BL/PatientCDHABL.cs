using Management.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Net;
using System.Numerics;
using System.Security.Cryptography;

namespace Management.BL
{
    public class PatientCDHABL
    {
        private readonly LABContext _db;
        public PatientCDHABL(LABContext db)
        {
            _db = db;
        }
        public async Task<List<Patient>> Get_ListPatient(DateTime fromDate, DateTime toDate, bool wait, bool process, bool valid, string categoryCode)
        {
            if (categoryCode == "SA")
            {
                return await _db.Patients.Where(p => p.Active == true && p.InsertTime > fromDate && p.InsertTime <= toDate &&
                                    p.WaitSA == wait && p.ProcessSA == process && p.ValidSA == valid).OrderByDescending(p => p.InsertTime).ToListAsync();
            }
            else if (categoryCode == "SAT")
            {
                return await _db.Patients.Where(p => p.Active == true && p.InsertTime > fromDate && p.InsertTime <= toDate &&
                                    p.WaitSAT == wait && p.ProcessSAT == process && p.ValidSAT == valid).OrderByDescending(p => p.InsertTime).ToListAsync();
            }
            else if (categoryCode == "DDT")
            {
                return await _db.Patients.Where(p => p.Active == true && p.InsertTime > fromDate && p.InsertTime <= toDate &&
                                    p.WaitDDT == wait && p.ProcessDDT == process && p.ValidDDT == valid).OrderByDescending(p => p.InsertTime).ToListAsync();
            }
            else if (categoryCode == "NS")
            {
                return await _db.Patients.Where(p => p.Active == true && p.InsertTime > fromDate && p.InsertTime <= toDate &&
                                     p.WaitNS == wait && p.ProcessNS == process && p.ValidNS == valid).OrderByDescending(p => p.InsertTime).ToListAsync();
            }
            else if (categoryCode == "XQ")
            {
                return await _db.Patients.Where(p => p.Active == true && p.InsertTime > fromDate && p.InsertTime <= toDate &&
                                    p.WaitXQ == wait && p.ProcessXQ == process && p.ValidXQ == valid).OrderByDescending(p => p.InsertTime).ToListAsync();
            }
            return null;
        }

        public async Task<int> Get_CountPatient(DateTime fromDate, DateTime toDate, bool wait, bool process, bool valid, string categoryCode)
        {
            if (categoryCode == "SA")
            {
                if (wait)
                {
                    return await _db.Patients.Where(p => p.Active == true && p.InsertTime > fromDate && p.InsertTime <= toDate &&
                                    p.WaitSA == wait).CountAsync();
                }
                else
                {
                    return await _db.Patients.Where(p => p.Active == true && p.InsertTime > fromDate && p.InsertTime <= toDate &&
                                   p.WaitSA == wait && p.ProcessSA == process && p.ValidSA == valid).CountAsync();
                }
            }
            else if (categoryCode == "SAT")
            {
                if (wait)
                {
                    return await _db.Patients.Where(p => p.Active == true && p.InsertTime > fromDate && p.InsertTime <= toDate &&
                                    p.WaitSAT == wait).CountAsync();
                }
                else
                {
                    return await _db.Patients.Where(p => p.Active == true && p.InsertTime > fromDate && p.InsertTime <= toDate &&
                                   p.WaitSAT == wait && p.ProcessSAT == process && p.ValidSAT == valid).CountAsync();
                }
            }
            else if (categoryCode == "DDT")
            {
                if (wait)
                {
                    return await _db.Patients.Where(p => p.Active == true && p.InsertTime > fromDate && p.InsertTime <= toDate &&
                                    p.WaitDDT == wait).CountAsync();
                }
                else
                {
                    return await _db.Patients.Where(p => p.Active == true && p.InsertTime > fromDate && p.InsertTime <= toDate &&
                                   p.WaitDDT == wait && p.ProcessDDT == process && p.ValidDDT == valid).CountAsync();
                }
            }
            else if (categoryCode == "NS")
            {
                if (wait)
                {
                    return await _db.Patients.Where(p => p.Active == true && p.InsertTime > fromDate && p.InsertTime <= toDate &&
                                    p.WaitNS == wait).CountAsync();
                }
                else
                {
                    return await _db.Patients.Where(p => p.Active == true && p.InsertTime > fromDate && p.InsertTime <= toDate &&
                                   p.WaitNS == wait && p.ProcessNS == process && p.ValidNS == valid).CountAsync();
                }
            }
            else if (categoryCode == "XQ")
            {
                if (wait)
                {
                    return await _db.Patients.Where(p => p.Active == true && p.InsertTime > fromDate && p.InsertTime <= toDate &&
                                    p.WaitXQ == wait).CountAsync();
                }
                else
                {
                    return await _db.Patients.Where(p => p.Active == true && p.InsertTime > fromDate && p.InsertTime <= toDate &&
                                   p.WaitXQ == wait && p.ProcessXQ == process && p.ValidXQ == valid).CountAsync();
                }
            }
            return 0;
        }

        public async Task<List<Patient>> Get_ListPatientByPidOrSid(DateTime fromDate, DateTime toDate, bool wait, bool process, bool valid, string pidorseq, string categoryCode)
        {
            if (string.IsNullOrEmpty(pidorseq))
            {
                if (categoryCode == "SA")
                {
                    return await _db.Patients.Where(p => p.Active == true && p.InsertTime > fromDate && p.InsertTime <= toDate &&
                                        p.WaitSA == wait && p.ProcessSA == process && p.ValidSA == valid).OrderByDescending(p => p.InsertTime).ToListAsync();
                }
                else if (categoryCode == "SAT")
                {
                    return await _db.Patients.Where(p => p.Active == true && p.InsertTime > fromDate && p.InsertTime <= toDate &&
                                        p.WaitSAT == wait && p.ProcessSAT == process && p.ValidSAT == valid).OrderByDescending(p => p.InsertTime).ToListAsync();
                }
                else if (categoryCode == "DDT")
                {
                    return await _db.Patients.Where(p => p.Active == true && p.InsertTime > fromDate && p.InsertTime <= toDate &&
                                        p.WaitDDT == wait && p.ProcessDDT == process && p.ValidDDT == valid).OrderByDescending(p => p.InsertTime).ToListAsync();
                }
                else if (categoryCode == "NS")
                {
                    return await _db.Patients.Where(p => p.Active == true && p.InsertTime > fromDate && p.InsertTime <= toDate &&
                                        p.WaitNS == wait && p.ProcessNS == process && p.ValidNS == valid).OrderByDescending(p => p.InsertTime).ToListAsync();

                }
                else if (categoryCode == "XQ")
                {
                    return await _db.Patients.Where(p => p.Active == true && p.InsertTime > fromDate && p.InsertTime <= toDate &&
                                      p.WaitXQ == wait && p.ProcessXQ == process && p.ValidXQ == valid).OrderByDescending(p => p.InsertTime).ToListAsync();
                }
            }
            else
            {
                if (categoryCode == "SA")
                {
                    return await _db.Patients.Where(p => p.Active == true && p.InsertTime > fromDate && p.InsertTime <= toDate &&
                                       p.WaitSA == wait && p.ProcessSA == process && p.ValidSA == valid && (p.PatientId == pidorseq || p.Seq == pidorseq)).OrderByDescending(p => p.InsertTime).ToListAsync();
                }
                else if (categoryCode == "SAT")
                {
                    return await _db.Patients.Where(p => p.Active == true && p.InsertTime > fromDate && p.InsertTime <= toDate &&
                                       p.WaitSAT == wait && p.ProcessSAT == process && p.ValidSAT == valid && (p.PatientId == pidorseq || p.Seq == pidorseq)).OrderByDescending(p => p.InsertTime).ToListAsync();
                }
                else if (categoryCode == "DDT")
                {
                    return await _db.Patients.Where(p => p.Active == true && p.InsertTime > fromDate && p.InsertTime <= toDate &&
                                       p.WaitDDT == wait && p.ProcessDDT == process && p.ValidDDT == valid && (p.PatientId == pidorseq || p.Seq == pidorseq)).OrderByDescending(p => p.InsertTime).ToListAsync();
                }
                else if (categoryCode == "NS")
                {
                    return await _db.Patients.Where(p => p.Active == true && p.InsertTime > fromDate && p.InsertTime <= toDate &&
                                      p.WaitNS == wait && p.ProcessNS == process && p.ValidNS == valid && (p.PatientId == pidorseq || p.Seq == pidorseq)).OrderByDescending(p => p.InsertTime).ToListAsync();
                }
                else if (categoryCode == "XQ")
                {
                    return await _db.Patients.Where(p => p.Active == true && p.InsertTime > fromDate && p.InsertTime <= toDate &&
                                      p.WaitXQ == wait && p.ProcessXQ == process && p.ValidXQ == valid && (p.PatientId == pidorseq || p.Seq == pidorseq)).OrderByDescending(p => p.InsertTime).ToListAsync();
                }
            }
            return null;
        }

        public async Task<Patient> Get_PatientBySid(long id)
        {
            return await _db.Patients.Where(p => p.Active == true && p.Id == id).FirstOrDefaultAsync();
        }

        public async Task<bool> DeleteById(long id, string group)
        {
            try
            {
                var _resultXN = await _db.ResultXNs.Where(p => p.PatientId == id && p.Active == true).FirstOrDefaultAsync();
                var _resultCDHA = await _db.ResultCDHAs.Where(p => p.PatientId == id && p.Active == true).FirstOrDefaultAsync();
                if (_resultXN == null && _resultCDHA == null)
                {
                    var _patient = await _db.Patients.Where(p => p.Id == id).FirstOrDefaultAsync();
                    if (_patient != null)
                    {
                        _patient.Active = false;
                        await _db.SaveChangesAsync();
                        return true;
                    }
                }
                else
                {
                    var _patient = await _db.Patients.Where(p => p.Id == id).FirstOrDefaultAsync();
                    if (_patient != null)
                    {
                        if (group == "SA")
                        {
                            _patient.WaitSA = false;
                            _patient.ProcessSA = false;
                            _patient.ValidSA = false;
                        }
                        else if (group == "SAT")
                        {
                            _patient.WaitSAT = false;
                            _patient.ProcessSAT = false;
                            _patient.ValidSAT = false;
                        }
                        else if (group == "DDT")
                        {
                            _patient.WaitDDT = false;
                            _patient.ProcessDDT = false;
                            _patient.ValidDDT = false;
                        }
                        else if (group == "NS")
                        {
                            _patient.WaitNS = false;
                            _patient.ProcessNS = false;
                            _patient.ValidNS = false;
                        }
                        else if (group == "XQ")
                        {
                            _patient.WaitXQ = false;
                            _patient.ProcessXQ = false;
                            _patient.ValidXQ = false;
                        }
                        await _db.SaveChangesAsync();
                        return true;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<bool> GetSample_ProcessResult_ReturnResult(long id, bool wait, bool process, bool valid, long? userInsertOrUpdate, string group)
        {
            try
            {
                var _patient = await _db.Patients.Where(p => p.Active == true && p.Id == id).FirstOrDefaultAsync();
                if (_patient != null)
                {
                    if (group == "SA")
                    {
                        _patient.WaitSA = wait;
                        _patient.ProcessSA = process;
                        _patient.ValidSA = valid;
                    }
                    else if (group == "SAT")
                    {
                        _patient.WaitSAT = wait;
                        _patient.ProcessSAT = process;
                        _patient.ValidSAT = valid;
                    }
                    else if (group == "DDT")
                    {
                        _patient.WaitDDT = wait;
                        _patient.ProcessDDT = process;
                        _patient.ValidDDT = valid;
                    }
                    else if (group == "NS")
                    {
                        _patient.WaitNS = wait;
                        _patient.ProcessNS = process;
                        _patient.ValidNS = valid;
                    }
                    else if (group == "XQ")
                    {
                        _patient.WaitXQ = wait;
                        _patient.ProcessXQ = process;
                        _patient.ValidXQ = valid;
                    }
                    _patient.UserUpdateId = userInsertOrUpdate;
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

        public async Task<Patient> SaveOrUpdate(long? id, string patientId, string seq, string sid, string patientName, DateTime age,
                                                string sex, string obj, string benhan, string location, string doctor,
                                                DateTime getSampleTime, string address, string diagnostic,
                                                long? userInsertIdOrUpdateId, DateTime dateTime, bool wait, bool process, bool valid, string group)
        {
            try
            {
                if (id == null)
                {
                    var _patient = new Patient();
                    _patient.PatientId = patientId;
                    _patient.Seq = seq;
                    _patient.Sid = sid;
                    _patient.PatientName = patientName;
                    _patient.Age = age;
                    _patient.Sex = sex;
                    _patient.ObjectId = long.Parse(obj);
                    _patient.BenhAn = benhan;
                    _patient.LocationId = long.Parse(location);
                    _patient.DoctorId = long.Parse(doctor);
                    _patient.Address = address;
                    _patient.Diagnostic = diagnostic;
                    _patient.UserInsertId = userInsertIdOrUpdateId;
                    _patient.InsertTime = dateTime;
                    if (group == "SA")
                    {
                        _patient.WaitSA = wait;
                        _patient.ProcessSA = process;
                        _patient.ValidSA = valid;
                        _patient.GetSampleTimeSA = getSampleTime;
                    }
                    else if (group == "SAT")
                    {
                        _patient.WaitSAT = wait;
                        _patient.ProcessSAT = process;
                        _patient.ValidSAT = valid;
                        _patient.GetSampleTimeSAT = getSampleTime;
                    }
                    else if (group == "DDT")
                    {
                        _patient.WaitDDT = wait;
                        _patient.ProcessDDT = process;
                        _patient.ValidDDT = valid;
                        _patient.GetSampleTimeDDT = getSampleTime;
                    }
                    else if (group == "NS")
                    {
                        _patient.WaitNS = wait;
                        _patient.ProcessNS = process;
                        _patient.ValidNS = valid;
                        _patient.GetSampleTimeNS = getSampleTime;
                    }
                    else if (group == "XQ")
                    {
                        _patient.WaitXQ = wait;
                        _patient.ProcessXQ = process;
                        _patient.ValidXQ = valid;
                        _patient.GetSampleTimeXQ = getSampleTime;
                    }
                    _patient.Active = true;
                    await _db.Patients.AddAsync(_patient);
                    await _db.SaveChangesAsync();
                    return _patient;
                }
                else
                {
                    var _patient = await _db.Patients.Where(p => p.Active == true && p.Id == id).FirstOrDefaultAsync();
                    if (_patient != null)
                    {
                        _patient.PatientId = patientId;
                        _patient.Seq = seq;
                        _patient.Sid = sid;
                        _patient.PatientName = patientName;
                        _patient.Age = age;
                        _patient.Sex = sex;
                        _patient.ObjectId = long.Parse(obj);
                        _patient.BenhAn = benhan;
                        _patient.LocationId = long.Parse(location);
                        _patient.DoctorId = long.Parse(doctor);
                        _patient.Address = address;
                        _patient.Diagnostic = diagnostic;
                        _patient.UserUpdateId = userInsertIdOrUpdateId;
                        _patient.UpdateTime = dateTime;
                        if (group == "SA")
                        {
                            _patient.WaitSA = wait;
                            _patient.ProcessSA = process;
                            _patient.ValidSA = valid;
                            _patient.GetSampleTimeSA = getSampleTime;
                        }
                        else if (group == "SAT")
                        {
                            _patient.WaitSAT = wait;
                            _patient.ProcessSAT = process;
                            _patient.ValidSAT = valid;
                            _patient.GetSampleTimeSAT = getSampleTime;
                        }
                        else if (group == "DDT")
                        {
                            _patient.WaitDDT = wait;
                            _patient.ProcessDDT = process;
                            _patient.ValidDDT = valid;
                            _patient.GetSampleTimeDDT = getSampleTime;
                        }
                        else if (group == "NS")
                        {
                            _patient.WaitNS = wait;
                            _patient.ProcessNS = process;
                            _patient.ValidNS = valid;
                            _patient.GetSampleTimeNS = getSampleTime;
                        }
                        else if (group == "XQ")
                        {
                            _patient.WaitXQ = wait;
                            _patient.ProcessXQ = process;
                            _patient.ValidXQ = valid;
                            _patient.GetSampleTimeXQ = getSampleTime;
                        }
                        await _db.SaveChangesAsync();
                        return _patient;
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public async Task Update(long id, DateTime returnResultTime, long userReturnResult, long? userInserOrUpdate, DateTime dateTime, string group)
        {
            try
            {
                var _patient = await _db.Patients.Where(p => p.Active == true && p.Id == id).FirstOrDefaultAsync();
                if (_patient != null)
                {
                    if (group == "SA")
                    {
                        _patient.ReturnResultTimeSA = returnResultTime;
                        _patient.UserReturnResultSA = userReturnResult;
                        _patient.UserUpdateId = userInserOrUpdate;
                    }
                    else if (group == "SAT")
                    {
                        _patient.ReturnResultTimeSAT = returnResultTime;
                        _patient.UserReturnResultSAT = userReturnResult;
                        _patient.UserUpdateId = userInserOrUpdate;
                    }
                    else if (group == "DDT")
                    {
                        _patient.ReturnResultTimeDDT = returnResultTime;
                        _patient.UserReturnResultDDT = userReturnResult;
                        _patient.UserUpdateId = userInserOrUpdate;
                    }
                    else if (group == "NS")
                    {
                        _patient.ReturnResultTimeNS = returnResultTime;
                        _patient.UserReturnResultNS = userReturnResult;
                        _patient.UserUpdateId = userInserOrUpdate;
                    }
                    else if (group == "XQ")
                    {
                        _patient.ReturnResultTimeXQ = returnResultTime;
                        _patient.UserReturnResultXQ = userReturnResult;
                        _patient.UserUpdateId = userInserOrUpdate;
                    }
                    _patient.UpdateTime = dateTime;
                    await _db.SaveChangesAsync();
                }
            }
            catch { }
        }

        public async Task<bool> Update(long id, DateTime returnResultTime, long userReturnResult, bool wait, bool process, bool valid, long? userInsertOrUpdate, string group)
        {
            try
            {
                var _patient = await _db.Patients.Where(p => p.Active == true && p.Id == id).FirstOrDefaultAsync();
                if (_patient != null)
                {
                    if (group == "SA")
                    {
                        _patient.ReturnResultTimeSA = returnResultTime;
                        _patient.UserReturnResultSA = userReturnResult;
                        _patient.WaitSA = wait;
                        _patient.ProcessSA = process;
                        _patient.ValidSA = valid;
                    }
                    else if (group == "SAT")
                    {
                        _patient.ReturnResultTimeSAT = returnResultTime;
                        _patient.UserReturnResultSAT = userReturnResult;
                        _patient.WaitSAT = wait;
                        _patient.ProcessSAT = process;
                        _patient.ValidSAT = valid;
                    }
                    else if (group == "DDT")
                    {
                        _patient.ReturnResultTimeDDT = returnResultTime;
                        _patient.UserReturnResultDDT = userReturnResult;
                        _patient.WaitDDT = wait;
                        _patient.ProcessDDT = process;
                        _patient.ValidDDT = valid;
                    }
                    else if (group == "NS")
                    {
                        _patient.ReturnResultTimeNS = returnResultTime;
                        _patient.UserReturnResultNS = userReturnResult;
                        _patient.WaitNS = wait;
                        _patient.ProcessNS = process;
                        _patient.ValidNS = valid;
                    }
                    else if (group == "XQ")
                    {
                        _patient.ReturnResultTimeXQ = returnResultTime;
                        _patient.UserReturnResultXQ = userReturnResult;
                        _patient.WaitXQ = wait;
                        _patient.ProcessXQ = process;
                        _patient.ValidXQ = valid;
                    }
                    _patient.UserUpdateId = userInsertOrUpdate;
                    await _db.SaveChangesAsync();
                    return true;
                }
            }
            catch(Exception ex)
            {
                return false;
            }
            return false;
        }
    }
}
