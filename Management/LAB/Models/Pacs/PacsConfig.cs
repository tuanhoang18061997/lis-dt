using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Management.Models.Pacs
{
    [Table("PacsConfigs")]
    public class PacsConfig
    {
        [Key]
        public long Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Code { get; set; }

        [Required]
        [StringLength(250)]
        public string Name { get; set; }

        [Required]
        [StringLength(50)]
        public string ServerType { get; set; }

        [Required]
        [StringLength(500)]
        public string BaseUrl { get; set; }

        [StringLength(100)]
        public string ApiUsername { get; set; }

        [StringLength(500)]
        public string ApiPassword { get; set; }

        [StringLength(100)]
        public string DicomAETitle { get; set; }

        [StringLength(250)]
        public string DicomHost { get; set; }

        public int? DicomPort { get; set; }

        [StringLength(250)]
        public string DicomWebRoot { get; set; }

        [StringLength(500)]
        public string OhifViewerUrl { get; set; }

        public bool SyncEnabled { get; set; }

        public int SyncIntervalSeconds { get; set; }

        public bool IsDefault { get; set; }

        public bool IsActive { get; set; }

        [StringLength(50)]
        public string LastStatus { get; set; }

        public bool? LastApiStatus { get; set; }

        public bool? LastStudiesStatus { get; set; }

        public bool? LastDicomWebStatus { get; set; }

        public bool? LastDicomPortStatus { get; set; }

        public DateTime? LastCheckedAt { get; set; }

        public string? LastError { get; set; }

        public DateTime CreatedOn { get; set; }

        public long? CreatedBy { get; set; }

        public DateTime? UpdatedOn { get; set; }

        public long? UpdatedBy { get; set; }
    }
}