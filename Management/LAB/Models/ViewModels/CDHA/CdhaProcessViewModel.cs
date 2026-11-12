using Management.BL;

namespace Management.Models.ViewModels.CDHA
{
    /// <summary>
    /// Cấu hình UI dùng chung cho màn hình Process của các module CDHA.
    ///
    /// Lưu ý:
    /// - Không phải Entity Framework entity.
    /// - Không map database.
    /// - Không dùng để SaveChanges().
    /// - Chỉ phục vụ render shared Razor partial.
    /// </summary>
    public class CdhaProcessModuleOptions
    {
        public string ModulePrefix { get; set; } = string.Empty;

        public string ScreenPrefix { get; set; } = "process";

        public string ModuleCode { get; set; } = string.Empty;

        public string ModuleName { get; set; } = string.Empty;

        public string PacsTitle { get; set; } = string.Empty;

        /// <summary>
        /// Một số module hiện tại có DOM id khác nhau cho
        /// select người trả/thực hiện kết quả.
        ///
        /// XQ hiện tại:
        /// xq_process_userReturnResultXQ
        /// </summary>
        public string UserReturnResultElementSuffix { get; set; }
            = "userReturnResult";

        public bool EnableSave { get; set; } = true;

        public bool EnablePacs { get; set; }

        public bool EnableDigitalSign { get; set; }

        public bool EnableSampleTemplate { get; set; }

        public bool EnableCancel { get; set; }

        public bool EnableValidPrint { get; set; } = true;

        public bool EnableReturnToWaiting { get; set; }

        public bool EnableDeviceModal { get; set; }

        public bool EnableManualImages { get; set; }

        public bool EnableCamera { get; set; }

        public bool EnableVideo { get; set; }

        /// <summary>
        /// Ví dụ:
        /// xq_process
        /// ct_process
        /// mr_process
        /// </summary>
        public string DomPrefix
        {
            get
            {
                return
                    $"{ModulePrefix}_{ScreenPrefix}";
            }
        }

        /// <summary>
        /// Sinh DOM id nhưng vẫn giữ đúng contract JS cũ.
        ///
        /// Id("patientId")
        /// => xq_process_patientId
        /// </summary>
        public string Id(string suffix)
        {
            return
                $"{DomPrefix}_{suffix}";
        }

        public string UserReturnResultElementId
        {
            get
            {
                return
                    Id(
                        UserReturnResultElementSuffix
                    );
            }
        }

        /// <summary>
        /// CSS hook riêng của module nếu thực sự cần.
        ///
        /// ModuleClass("process")
        /// => xq-process
        /// </summary>
        public string ModuleClass(string suffix)
        {
            return
                $"{ModulePrefix}-{suffix}";
        }

        public string PacsDefaultSize { get; set; } = "75";

        /// <summary>
        /// Sinh ID thuộc workflow GetSample cũ.
        /// Ví dụ XQ:
        /// GetSampleId("select_device_select")
        /// => xq_getsample_select_device_select
        /// </summary>
        public string GetSampleId(string suffix)
        {
            return $"{ModulePrefix}_getsample_{suffix}";
        }
    }


    /// <summary>
    /// ViewModel chung cho CDHA Process.
    ///
    /// Chỉ là projection của dữ liệu lên UI.
    /// Tuyệt đối không thay thế Patient entity.
    /// </summary>
    public class CdhaProcessViewModel
    {
        public CdhaProcessModuleOptions Options { get; set; }
            = new CdhaProcessModuleOptions();

        public List<Patient> Patients { get; set; }
            = new List<Patient>();

        public List<User> Users { get; set; }
            = new List<User>();

        public List<BenhAnModel> MedicalRecords { get; set; }
            = new List<BenhAnModel>();

        public List<Sample> Samples { get; set; }
            = new List<Sample>();

        public List<Device> Devices { get; set; }
            = new List<Device>();

        public List<ResultCDHA> Results { get; set; }
            = new List<ResultCDHA>();

        /// <summary>
        /// Patient entity thật.
        /// Không clone thành entity generic.
        /// </summary>
        public Patient? PatientInfo { get; set; }

        public long? UserLoginId { get; set; }


        // =====================================================
        // UI PROJECTION ONLY
        // =====================================================

        /// <summary>
        /// Với XQ:
        /// Patient.GetSampleTimeXQ
        /// </summary>
        public DateTime? DisplayGetSampleTime { get; set; }

        /// <summary>
        /// Với XQ:
        /// Patient.ReturnResultTimeXQ
        /// </summary>
        public DateTime? DisplayReturnResultTime { get; set; }

        /// <summary>
        /// Với XQ:
        /// Patient.UserReturnResultXQ
        ///
        /// Nếu DB chưa chọn thì fallback user đang login,
        /// giống behavior UI hiện tại.
        /// </summary>
        public long? SelectedUserReturnResultId { get; set; }

        public string MedicalRecordName { get; set; }
            = string.Empty;


        // =====================================================
        // XQ PROJECTION
        // =====================================================

        /// <summary>
        /// Tạo configuration chuẩn cho XQ.
        ///
        /// Sau này CT / MR / MG sẽ có factory tương tự,
        /// nhưng shared Razor không cần biết module nào.
        /// </summary>
        public static CdhaProcessModuleOptions CreateXqOptions()
        {
            return new CdhaProcessModuleOptions
            {
                ModulePrefix =
                    "xq",

                ScreenPrefix =
                    "process",

                ModuleCode =
                    "XQ",

                ModuleName =
                    "X Quang",

                PacsTitle =
                    "XQ PACS Viewer",

                UserReturnResultElementSuffix =
                    "userReturnResultXQ",

                PacsDefaultSize =
                    "75",

                EnableSave =
                    true,

                EnableValidPrint =
                    true,

                EnablePacs =
                    true,

                EnableDigitalSign =
                    true,

                EnableSampleTemplate =
                    true,

                // SHARED-10-06: XQ không có Process_CancelResultt/backend cancel contract.
                // Không render dead button "Bỏ qua" để tránh JS runtime error.
                EnableCancel =
                    false,

                EnableReturnToWaiting =
                    true,

                EnableDeviceModal =
                    true,

                EnableManualImages =
                    false,

                EnableCamera =
                    false,

                EnableVideo =
                    false
            };
        }


        /// <summary>
        /// Projection Patient XQ -> Shared UI.
        ///
        /// Không save database.
        /// Không thay property Patient.
        /// </summary>
        public static CdhaProcessViewModel CreateXqPatientInfo(
            Patient? patient,
            List<User>? users,
            List<BenhAnModel>? medicalRecords,
            long? userLoginId)
        {
            var vm =
                new CdhaProcessViewModel
                {
                    Options =
                        CreateXqOptions(),

                    PatientInfo =
                        patient,

                    Users =
                        users ??
                        new List<User>(),

                    MedicalRecords =
                        medicalRecords ??
                        new List<BenhAnModel>(),

                    UserLoginId =
                        userLoginId
                };


            if (patient == null)
            {
                return vm;
            }


            // =================================================
            // EXPLICIT XQ MAPPING
            //
            // Đây là nơi duy nhất biết XQ dùng field nào
            // trong Patient.
            // =================================================

            vm.DisplayGetSampleTime =
                patient.GetSampleTimeXQ;

            vm.DisplayReturnResultTime =
                patient.ReturnResultTimeXQ
                ?? DateTime.Now;

            vm.SelectedUserReturnResultId =
                patient.UserReturnResultXQ
                ?? userLoginId;


            // =================================================
            // Bệnh án
            // =================================================

            if (
                !string.IsNullOrWhiteSpace(
                    patient.BenhAn
                )
            )
            {
                var medicalRecord =
                    vm.MedicalRecords
                        .FirstOrDefault(
                            x =>
                                x.Id ==
                                patient.BenhAn
                        );

                vm.MedicalRecordName =
                    medicalRecord?.Name
                    ?? string.Empty;
            }


            return vm;
        }
    }
}
