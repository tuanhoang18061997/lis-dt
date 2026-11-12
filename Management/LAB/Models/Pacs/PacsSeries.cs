using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Management.Models.Pacs
{
    [Table("PacsSeries")]
    public class PacsSeries
    {
        [Key]
        public long Id { get; set; }

        public long PacsStudyId { get; set; }

        public string OrthancSeriesId { get; set; }

        public string OrthancParentStudyId { get; set; }

        public string? SeriesInstanceUID { get; set; }

        public string? SeriesNumber { get; set; }

        public string? SeriesDescription { get; set; }

        public string? Modality { get; set; }

        public string? ProtocolName { get; set; }

        public int InstanceCount { get; set; }

        public string OrthancStatus { get; set; }

        public string RawJson { get; set; }

        public DateTime CreatedOn { get; set; }

        public DateTime? UpdatedOn { get; set; }
    }
}