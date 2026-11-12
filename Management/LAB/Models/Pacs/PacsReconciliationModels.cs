namespace Management.Models.Pacs
{
    /// <summary>
    /// Stable status vocabulary for PREPROD-01C Reconciliation.
    /// The actual decision logic is implemented from PREPROD-01C-02 onward.
    /// </summary>
    public static class PacsReconciliationStatuses
    {
        public const string Match = "MATCH";
        public const string MetadataCountMismatch = "METADATA_COUNT_MISMATCH";
        public const string LisMissingData = "LIS_MISSING_DATA";
        public const string LisHasExtraData = "LIS_HAS_EXTRA_DATA";
        public const string CountMismatchComplex = "COUNT_MISMATCH_COMPLEX";
        public const string PacsStudyNotFound = "PACS_STUDY_NOT_FOUND";
        public const string NotStable = "NOT_STABLE";
        public const string IdentityMismatch = "IDENTITY_MISMATCH";
        public const string Error = "ERROR";

        public const string Reconciled = "RECONCILED";
        public const string AlreadyMatched = "ALREADY_MATCHED";
        public const string InvalidWorklistId = "INVALID_WORKLIST_ID";
        public const string WorklistNotFound = "WORKLIST_NOT_FOUND";
        public const string InvalidWorklistStatus = "INVALID_WORKLIST_STATUS";
        public const string OutsideReconciliationWindow = "OUTSIDE_RECONCILIATION_WINDOW";
        public const string MissingOrthancStudyId = "MISSING_ORTHANC_STUDY_ID";
        public const string SyncBusy = "SYNC_BUSY";
        public const string VerifyCountFailed = "VERIFY_COUNT_FAILED";
        public const string VerifyCompletedOnChanged = "VERIFY_COMPLETED_ON_CHANGED";
        public const string ReconcileError = "RECONCILE_ERROR";

        public const string NoCandidates = "NO_CANDIDATES";
        public const string Completed = "COMPLETED";
        public const string CompletedWithErrors = "COMPLETED_WITH_ERRORS";
        public const string BatchBusy = "RECONCILIATION_BATCH_BUSY";
        public const string ConfigInvalid = "RECONCILIATION_CONFIG_INVALID";
        public const string PacsConfigNotFound = "PACS_CONFIG_NOT_FOUND";
        public const string BatchError = "BATCH_ERROR";
    }

    public sealed class PacsReconciliationWorklistRequest
    {
        public long PacsWorklistItemId { get; set; }
    }

    /// <summary>
    /// Read-only preview of completed Worklists that may need Reconciliation.
    ///
    /// Candidate contract:
    /// - Worklist Status = 3;
    /// - CreatedOn >= configured StartDate;
    /// - CompletedOn <= Now - MinAgeMinutes;
    /// - OrthancStudyId is present.
    ///
    /// PREPROD-01C-02 will implement the actual read-only classification.
    /// </summary>
    public sealed class PacsReconciliationPreviewResult
    {
        public bool Success { get; set; }
        public string? Status { get; set; }
        public string? Message { get; set; }
        public string? ErrorMessage { get; set; }

        public DateTime? ReconciliationFrom { get; set; }
        public DateTime? ReconciliationTo { get; set; }
        public int MinAgeMinutes { get; set; }
        public int BatchSize { get; set; }
        public int CandidateCount { get; set; }

        public int MatchCount { get; set; }
        public int MissingDataCount { get; set; }
        public int MetadataMismatchCount { get; set; }
        public int ExtraDataCount { get; set; }
        public int ComplexMismatchCount { get; set; }
        public int PacsStudyNotFoundCount { get; set; }
        public int NotStableCount { get; set; }
        public int IdentityMismatchCount { get; set; }
        public int ErrorCount { get; set; }

        public List<PacsReconciliationPreviewItem> Items { get; set; } = new();
    }

    public sealed class PacsReconciliationPreviewItem
    {
        public long WorklistId { get; set; }
        public long? ResultCDHAId { get; set; }

        public string? AccessionNumber { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime? CompletedOn { get; set; }
        public DateTime? LastReconciliationCheckedOn { get; set; }

        public string? WorklistOrthancStudyId { get; set; }
        public string? WorklistStudyInstanceUID { get; set; }

        public long? PacsStudyId { get; set; }
        public string? PacsStudyStatus { get; set; }

        // PacsStudy metadata counts.
        public int? LisRecordedSeriesCount { get; set; }
        public int? LisRecordedInstanceCount { get; set; }

        // Actual child-row counts in LIS.
        public int LisActualSeriesCount { get; set; }
        public int LisActualInstanceCount { get; set; }

        // Current Orthanc identity and hierarchy.
        public string? OrthancAccessionNumber { get; set; }
        public string? OrthancStudyInstanceUID { get; set; }
        public bool? IsStable { get; set; }
        public int OrthancSeriesCount { get; set; }
        public int OrthancInstanceCount { get; set; }

        // Positive means Orthanc currently has more than LIS.
        public int SeriesDelta { get; set; }
        public int InstanceDelta { get; set; }

        public bool CanReconcile { get; set; }
        public string? ReconciliationStatus { get; set; }
        public string? Message { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Result contract for manual ReconcileCompletedStudy.
    /// PREPROD-01C-03 will populate it.
    /// </summary>
    public sealed class PacsReconciliationManualResult
    {
        public bool Success { get; set; }
        public string? Status { get; set; }
        public string? Message { get; set; }
        public string? ErrorMessage { get; set; }

        public long WorklistId { get; set; }
        public long? ResultCDHAId { get; set; }
        public long? PacsStudyId { get; set; }

        public string? AccessionNumber { get; set; }
        public string? OrthancStudyId { get; set; }
        public string? StudyInstanceUID { get; set; }
        public bool? IsStable { get; set; }

        public int? LisRecordedSeriesCountBefore { get; set; }
        public int? LisRecordedInstanceCountBefore { get; set; }
        public int LisSeriesCountBefore { get; set; }
        public int LisInstanceCountBefore { get; set; }

        public int OrthancSeriesCount { get; set; }
        public int OrthancInstanceCount { get; set; }

        public int LisSeriesCountAfter { get; set; }
        public int LisInstanceCountAfter { get; set; }

        public DateTime? CompletedOnBefore { get; set; }
        public DateTime? CompletedOnAfter { get; set; }
        public bool CompletedOnPreserved { get; set; }
    }

    public sealed class PacsReconciliationBatchItem
    {
        public long WorklistId { get; set; }
        public string? AccessionNumber { get; set; }

        public string? PreviewStatus { get; set; }
        public bool CanReconcile { get; set; }

        // NONE / RECONCILE / SKIP / WARNING / ERROR
        public string? Action { get; set; }
        public string? ActionStatus { get; set; }

        public PacsReconciliationManualResult? ReconcileResult { get; set; }
    }

    public sealed class PacsReconciliationBatchResult
    {
        public bool Success { get; set; }
        public string? Status { get; set; }
        public string? Message { get; set; }
        public string? ErrorMessage { get; set; }

        public DateTime? ReconciliationFrom { get; set; }
        public DateTime? ReconciliationTo { get; set; }
        public int MinAgeMinutes { get; set; }
        public int BatchSize { get; set; }
        public int CandidateCount { get; set; }

        public int MatchCount { get; set; }
        public int ReconciledCount { get; set; }
        public int AlreadyMatchedCount { get; set; }
        public int NotStableCount { get; set; }
        public int ExtraDataCount { get; set; }
        public int ComplexMismatchCount { get; set; }
        public int IdentityMismatchCount { get; set; }
        public int PacsStudyNotFoundCount { get; set; }
        public int SyncBusyCount { get; set; }
        public int SkippedCount { get; set; }
        public int ErrorCount { get; set; }

        public List<PacsReconciliationBatchItem> Items { get; set; } = new();
    }
}
