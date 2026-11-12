namespace Management.Models.Pacs
{
    public sealed class PacsRecoveryWorklistRequest
    {
        public long PacsWorklistItemId { get; set; }
    }

    /// <summary>
    /// Read-only preview result for pending Worklists that may need Recovery.
    /// PREPROD-01B-02 will populate this model; this file only freezes the contract.
    /// </summary>
    public sealed class PacsRecoveryPreviewResult
    {
        public bool Success { get; set; }
        public string? Status { get; set; }
        public string? Message { get; set; }
        public string? ErrorMessage { get; set; }

        public DateTime? RecoveryFrom { get; set; }
        public DateTime? RecoveryTo { get; set; }
        public int MinAgeMinutes { get; set; }
        public int BatchSize { get; set; }
        public int CandidateCount { get; set; }

        public int RecoverableCount { get; set; }
        public int NotFoundCount { get; set; }
        public int NotStableCount { get; set; }
        public int AlreadyHandledCount { get; set; }
        public int AmbiguousCount { get; set; }
        public int IdentityMismatchCount { get; set; }
        public int ErrorCount { get; set; }

        public List<PacsRecoveryPreviewItem> Items { get; set; } = new();
    }

    public sealed class PacsRecoveryPreviewItem
    {
        public long WorklistId { get; set; }
        public long ResultCDHAId { get; set; }
        public long? PatientId { get; set; }
        public long? ServiceId { get; set; }

        public string? AccessionNumber { get; set; }
        public string? Modality { get; set; }

        public int WorklistStatus { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime? PublishedOn { get; set; }
        public DateTime? LastRecoveryCheckedOn { get; set; }

        public string? CurrentOrthancStudyId { get; set; }
        public string? CurrentStudyInstanceUID { get; set; }

        public bool PacsStudyExists { get; set; }
        public long? PacsStudyId { get; set; }
        public int? PacsStudySeriesCount { get; set; }
        public int? PacsStudyInstanceCount { get; set; }
        public string? PacsStudyStatus { get; set; }

        public int OrthancStudyCount { get; set; }
        public List<string> OrthancStudyIds { get; set; } = new();
        public string? OrthancStudyId { get; set; }
        public string? OrthancAccessionNumber { get; set; }
        public string? OrthancStudyInstanceUID { get; set; }
        public bool? IsStable { get; set; }
        public int OrthancSeriesCount { get; set; }
        public int OrthancInstanceCount { get; set; }

        public bool IsAmbiguous { get; set; }
        public bool CanRecover { get; set; }
        public string? RecoveryStatus { get; set; }
        public string? Message { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public sealed class PacsRecoveryManualResult
    {
        public bool Success { get; set; }
        public string? Status { get; set; }
        public string? Message { get; set; }
        public string? ErrorMessage { get; set; }

        public long PacsWorklistItemId { get; set; }
        public long? ResultCDHAId { get; set; }
        public string? AccessionNumber { get; set; }

        public DateTime? RecoveryFrom { get; set; }
        public DateTime? WorklistCreatedOn { get; set; }
        public int? WorklistStatusBefore { get; set; }
        public int? WorklistStatusAfter { get; set; }
        public bool WorklistCompleted { get; set; }
        public DateTime? CompletedOn { get; set; }

        public int OrthancStudyCount { get; set; }
        public string? OrthancStudyId { get; set; }
        public string? StudyInstanceUID { get; set; }
        public bool? IsStable { get; set; }
        public int OrthancSeriesCount { get; set; }
        public int OrthancInstanceCount { get; set; }

        public long? PacsStudyId { get; set; }
        public int SeriesCount { get; set; }
        public int InstanceCount { get; set; }
        public bool MappedResultCDHA { get; set; }
    }

    public sealed class PacsRecoveryBatchResult
    {
        public bool Success { get; set; }
        public string? Status { get; set; }
        public string? Message { get; set; }
        public string? ErrorMessage { get; set; }

        public DateTime? RecoveryFrom { get; set; }
        public DateTime? RecoveryTo { get; set; }
        public int MinAgeMinutes { get; set; }
        public int BatchSize { get; set; }
        public int CandidateCount { get; set; }

        public int RecoveredCount { get; set; }
        public int NotFoundCount { get; set; }
        public int NotStableCount { get; set; }
        public int AlreadyHandledCount { get; set; }
        public int AmbiguousCount { get; set; }
        public int IdentityMismatchCount { get; set; }
        public int SyncBusyCount { get; set; }
        public int SkippedCount { get; set; }
        public int ErrorCount { get; set; }

        public List<PacsRecoveryManualResult> Items { get; set; } = new();
    }
}
