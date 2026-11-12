using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Management.Models.Pacs
{
    [Table("PacsWorklistItems")]
    public class PacsWorklistItem
    {
        [Key]
        public long Id { get; set; }

        public long ResultCDHAId { get; set; }

        public long? PatientId { get; set; }

        public long? ServiceId { get; set; }

        public string AccessionNumber { get; set; }

        public string PatientCode { get; set; }

        public string PatientName { get; set; }

        public DateTime? PatientBirthDate { get; set; }

        public string? PatientSex { get; set; }

        public string? MaBenhAn { get; set; }

        public string Seq { get; set; }

        public string Sid { get; set; }

        public string ServiceCode { get; set; }

        public string ServiceName { get; set; }

        public string CategoryCode { get; set; }

        public string Modality { get; set; }

        public string RequestedProcedureId { get; set; }

        public string RequestedProcedureDescription { get; set; }

        public string ScheduledStationAETitle { get; set; }

        public string ScheduledProcedureStepId { get; set; }

        public DateTime? ScheduledProcedureStepStartDate { get; set; }

        public TimeSpan? ScheduledProcedureStepStartTime { get; set; }

        public string ScheduledProcedureStepDescription { get; set; }

        public string? ReferringPhysicianName { get; set; }

        public string? ScheduledPerformingPhysicianName { get; set; }

        public string? StudyDescription { get; set; }

        public string? Diagnosis { get; set; }

        public string? DumpFileName { get; set; }

        public string? DumpFilePath { get; set; }

        public string? WorklistFileName { get; set; }

        public string? WorklistFilePath { get; set; }

        public string? OrthancWorklistId { get; set; }

        public string? OrthancStudyId { get; set; }

        public string? StudyInstanceUID { get; set; }

        public int Status { get; set; }

        public string? ErrorMessage { get; set; }

        public DateTime CreatedOn { get; set; }

        public long? CreatedBy { get; set; }

        public DateTime? UpdatedOn { get; set; }

        public long? UpdatedBy { get; set; }

        public DateTime? PublishedOn { get; set; }

        public DateTime? CompletedOn { get; set; }

        public DateTime? CanceledOn { get; set; }

        public DateTime? LastRecoveryCheckedOn { get; set; }

        public DateTime? LastReconciliationCheckedOn { get; set; }
    }
}