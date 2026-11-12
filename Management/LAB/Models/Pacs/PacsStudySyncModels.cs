namespace Management.Models.Pacs
{
    public class PacsSingleStudySyncRequest
    {
        public string OrthancStudyId { get; set; } = string.Empty;
    }

    public class PacsSingleStudySyncResult
    {
        public bool Success { get; set; }

        public bool Created { get; set; }

        public bool Updated { get; set; }

        public string? ErrorCode { get; set; }

        public string? Message { get; set; }

        public long? PacsStudyId { get; set; }

        public string? OrthancStudyId { get; set; }

        public string? StudyInstanceUID { get; set; }

        public string? AccessionNumber { get; set; }

        public int SeriesCount { get; set; }

        public int InstanceCount { get; set; }

        public int CreatedSeriesCount { get; set; }

        public int UpdatedSeriesCount { get; set; }

        public int RemovedSeriesCount { get; set; }

        public int CreatedInstanceCount { get; set; }

        public int UpdatedInstanceCount { get; set; }

        public int RemovedInstanceCount { get; set; }

        public bool MappingDeferred { get; set; } = false;

        public bool MappedResultCDHA { get; set; }

        public string? MappingStatus { get; set; }

        public string? MappingSource { get; set; }

        public long? ResultCDHAId { get; set; }

        public long? WorklistId { get; set; }

        public bool WorklistCompleted { get; set; }

        public string? PreviousWorklistStudyInstanceUID { get; set; }

        public bool WorklistStudyInstanceUIDUpdated { get; set; }
    }
}
