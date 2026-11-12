using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Management.Models.Pacs
{
    [Table("PacsInstances")]
    public class PacsInstance
    {
        [Key]
        public long Id { get; set; }

        public long PacsStudyId { get; set; }

        public long PacsSeriesId { get; set; }

        public string OrthancInstanceId { get; set; }

        public string OrthancParentSeriesId { get; set; }

        public string SOPInstanceUID { get; set; }

        public string? InstanceNumber { get; set; }

        public string? ImageIndex { get; set; }

        public string? SopClassUID { get; set; }

        public string? TransferSyntaxUID { get; set; }

        public string? OrthancStatus { get; set; }

        public string RawJson { get; set; }

        public DateTime CreatedOn { get; set; }

        public DateTime? UpdatedOn { get; set; }
    }
}