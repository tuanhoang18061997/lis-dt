using System.Text.Json.Serialization;

namespace Management.Models.Pacs
{
    public class OrthancChangesResponse
    {
        [JsonPropertyName("Changes")]
        public List<OrthancChangeItem> Changes { get; set; }
            = new();

        [JsonPropertyName("Done")]
        public bool Done { get; set; }

        [JsonPropertyName("Last")]
        public long Last { get; set; }
    }

    public class OrthancChangeItem
    {
        [JsonPropertyName("ChangeType")]
        public string? ChangeType { get; set; }

        [JsonPropertyName("ResourceType")]
        public string? ResourceType { get; set; }

        [JsonPropertyName("ID")]
        public string? Id { get; set; }

        [JsonPropertyName("Seq")]
        public long Seq { get; set; }

        [JsonPropertyName("Date")]
        public string? Date { get; set; }

        [JsonPropertyName("Path")]
        public string? Path { get; set; }
    }

    public class PacsStableStudyChange
    {
        public long Seq { get; set; }

        public string OrthancStudyId { get; set; }
            = string.Empty;

        public string? Date { get; set; }

        public string? Path { get; set; }
    }

    public class PacsChangesPreviewResult
    {
        public bool Success { get; set; }

        public string? ErrorCode { get; set; }

        public string? Message { get; set; }

        public long Since { get; set; }

        public int Limit { get; set; }

        public long Last { get; set; }

        public bool Done { get; set; }

        public int TotalChanges { get; set; }

        public int StableStudyCount { get; set; }

        public List<PacsStableStudyChange> StableStudies
        {
            get;
            set;
        } = new();
    }

    public class PacsCheckpointInitializeRequest
    {
        /// <summary>
        /// Bắt buộc true.
        /// Hành động này bỏ qua lịch sử change cũ tới tail hiện tại.
        /// </summary>
        public bool ConfirmSkipHistory { get; set; }

        public int Limit { get; set; } = 500;

        public int MaxPages { get; set; } = 1000;
    }

    public class PacsCheckpointInitializeResult
    {
        public bool Success { get; set; }

        public string? ErrorCode { get; set; }

        public string? Message { get; set; }

        public long? PacsConfigId { get; set; }

        public long CheckpointBefore { get; set; }

        public long CheckpointAfter { get; set; }

        public int PagesRead { get; set; }

        public int TotalChangesSkipped { get; set; }

        public bool Done { get; set; }
    }

    public class PacsSyncCheckpointRequest
    {
        public int Limit { get; set; } = 100;

        public int MaxPages { get; set; } = 10;
    }

    public class PacsSyncCheckpointItem
    {
        public long Seq { get; set; }

        public string OrthancStudyId { get; set; }
            = string.Empty;

        public string? ChangeDate { get; set; }

        public string? Path { get; set; }

        public bool Success { get; set; }

        public string? ErrorCode { get; set; }

        public string? ErrorMessage { get; set; }

        public long? PacsStudyId { get; set; }

        public string? AccessionNumber { get; set; }

        public string? StudyInstanceUID { get; set; }

        public long? ResultCDHAId { get; set; }

        public long? WorklistId { get; set; }

        public bool MappedResultCDHA { get; set; }

        public bool WorklistCompleted { get; set; }

        public int SeriesCount { get; set; }

        public int InstanceCount { get; set; }
    }

    public class PacsSyncCheckpointResult
    {
        public bool Success { get; set; }

        public string? ErrorCode { get; set; }

        public string? Message { get; set; }

        public string? ErrorMessage { get; set; }

        public long? PacsConfigId { get; set; }

        public long CheckpointBefore { get; set; }

        public long CheckpointAfter { get; set; }

        public int Limit { get; set; }

        public int MaxPages { get; set; }

        public int PagesProcessed { get; set; }

        public int TotalChanges { get; set; }

        public int TotalStableEvents { get; set; }

        public int UniqueStudyCount { get; set; }

        public int SuccessStudyCount { get; set; }

        public int FailedStudyCount { get; set; }

        public bool Done { get; set; }

        public bool HasMore { get; set; }

        public List<PacsSyncCheckpointItem> Items
        {
            get;
            set;
        } = new();
    }

    public class PacsCheckpointStatusResult
    {
        public bool Success { get; set; }

        public string? ErrorCode { get; set; }

        public string? Message { get; set; }

        public long? PacsConfigId { get; set; }

        public string? PacsConfigCode { get; set; }

        public string? PacsConfigName { get; set; }

        public bool StateExists { get; set; }

        public long LastChangeSeq { get; set; }

        public DateTime? LastRunAt { get; set; }

        public DateTime? LastSuccessAt { get; set; }

        public string? LastError { get; set; }

        public DateTime? CreatedOn { get; set; }

        public DateTime? UpdatedOn { get; set; }
    }

    public class PacsManualSyncRequest
    {
        public long Since { get; set; }

        public int Limit { get; set; } = 100;

        public int MaxPages { get; set; } = 10;
    }

    public class PacsManualSyncItem
    {
        public long Seq { get; set; }

        public string OrthancStudyId { get; set; }
            = string.Empty;

        public string? ChangeDate { get; set; }

        public string? Path { get; set; }

        public bool Success { get; set; }

        public string? ErrorCode { get; set; }

        public string? ErrorMessage { get; set; }

        public long? PacsStudyId { get; set; }

        public string? AccessionNumber { get; set; }

        public string? StudyInstanceUID { get; set; }

        public long? ResultCDHAId { get; set; }

        public long? WorklistId { get; set; }

        public bool MappedResultCDHA { get; set; }

        public bool WorklistCompleted { get; set; }

        public int SeriesCount { get; set; }

        public int InstanceCount { get; set; }
    }

    public class PacsManualSyncResult
    {
        public bool Success { get; set; }

        public string? ErrorCode { get; set; }

        public string? Message { get; set; }

        public long? PacsConfigId { get; set; }

        public long Since { get; set; }

        public int Limit { get; set; }

        public int MaxPages { get; set; }

        public int PagesProcessed { get; set; }

        public int TotalChanges { get; set; }

        public int TotalStableEvents { get; set; }

        public int UniqueStudyCount { get; set; }

        public int SuccessStudyCount { get; set; }

        public int FailedStudyCount { get; set; }

        public bool Done { get; set; }

        public bool HasMore { get; set; }

        public long SuggestedNextSince { get; set; }

        public bool CanAdvanceCheckpoint { get; set; }

        public long CheckpointBefore { get; set; }

        public long CheckpointAfter { get; set; }

        public bool CheckpointChanged { get; set; }

        public List<PacsManualSyncItem> Items
        {
            get;
            set;
        } = new();
    }

}
