namespace Management.Models.Pacs
{
    public class PacsSeriesThumbnailResult
    {
        public bool Success { get; set; }

        public string? Message { get; set; }

        public long? PacsStudyId { get; set; }

        public long? ResultCDHAId { get; set; }

        public string? AccessionNumber { get; set; }

        public string? StudyInstanceUID { get; set; }

        public string? OrthancStudyId { get; set; }

        public string? ViewerUrl { get; set; }

        public List<PacsSeriesThumbnailItem> Items { get; set; }
            = new();
    }

    public class PacsSeriesThumbnailItem
    {
        public long PacsSeriesId { get; set; }

        public string? OrthancSeriesId { get; set; }

        public string? SeriesInstanceUID { get; set; }

        public string? SeriesNumber { get; set; }

        public string? SeriesDescription { get; set; }

        public string? Modality { get; set; }

        public string? ProtocolName { get; set; }

        public int? InstanceCount { get; set; }

        public long? PacsInstanceId { get; set; }

        public string? OrthancInstanceId { get; set; }

        public string? SOPInstanceUID { get; set; }

        public string? InstanceNumber { get; set; }

        public string? ThumbnailUrl { get; set; }

        public string? ViewerUrl { get; set; }
    }
}