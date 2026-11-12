using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Management.Models.Pacs
{
    [Table("PacsStudies")]
    public class PacsStudy
    {
        [Key]
        public long Id { get; set; }

        public long? PatientDbId { get; set; }

        public long? ResultCDHAId { get; set; }

        public long? ServiceId { get; set; }

        public string? PatientId { get; set; }

        public string? PatientName { get; set; }

        public string? PatientBirthDate { get; set; }

        public string? PatientSex { get; set; }

        public string? MaBenhAn { get; set; }

        public string? AccessionNumber { get; set; }

        public string StudyInstanceUID { get; set; }

        public string OrthancStudyId { get; set; }

        public string StudyDate { get; set; }

        public string StudyTime { get; set; }

        public string? StudyDescription { get; set; }

        public string? RequestedProcedureDescription { get; set; }

        public string? Modality { get; set; }

        public string? ReferringPhysicianName { get; set; }

        public string? InstitutionName { get; set; }

        public int SeriesCount { get; set; }

        public int InstanceCount { get; set; }

        public string Status { get; set; }

        public string RawJson { get; set; }

        public DateTime CreatedOn { get; set; }

        public DateTime? UpdatedOn { get; set; }
    }
}