namespace Management.Services.Pacs
{
    /// <summary>
    /// PACS-XQ-PREPROD-01B Recovery configuration contract.
    ///
    /// StartDate is deliberately kept as configuration text and will be parsed
    /// fail-closed by PacsReliabilityBL in 01B-02. An empty/invalid StartDate
    /// must never cause a historical full-table recovery scan.
    /// </summary>
    public sealed class PacsSyncRecoveryOptions
    {
        /// <summary>
        /// Reserved for automatic background Recovery in PREPROD-01D.
        /// Manual preview/recovery endpoints are not controlled by this switch.
        /// </summary>
        public bool Enabled { get; set; } = false;

        /// <summary>
        /// yyyy-MM-dd. Required by recovery logic; empty/invalid means fail closed.
        /// </summary>
        public string? StartDate { get; set; }

        /// <summary>
        /// Reserved background interval for PREPROD-01D.
        /// </summary>
        public int IntervalSeconds { get; set; } = 60;

        /// <summary>
        /// Maximum number of pending Worklists examined per recovery batch.
        /// </summary>
        public int BatchSize { get; set; } = 50;

        /// <summary>
        /// Newly published Worklists must age for this many minutes before they
        /// become Recovery candidates. This prevents Recovery racing normal C-STORE.
        /// </summary>
        public int MinAgeMinutes { get; set; } = 2;
    }

    /// <summary>
    /// PACS-XQ-PREPROD-01C Reconciliation configuration contract.
    ///
    /// Reconciliation only examines Worklists that are already Completed
    /// (Status=3) and compares the LIS hierarchy with the current Orthanc
    /// hierarchy. StartDate is mandatory and will be parsed fail-closed by
    /// PacsReliabilityBL beginning in PREPROD-01C-02.
    /// </summary>
    public sealed class PacsSyncReconciliationOptions
    {
        /// <summary>
        /// Reserved for automatic background Reconciliation in PREPROD-01D.
        /// Manual preview/reconciliation endpoints are not controlled by this switch.
        /// </summary>
        public bool Enabled { get; set; } = false;

        /// <summary>
        /// yyyy-MM-dd. Required by reconciliation logic; empty/invalid means fail closed.
        /// </summary>
        public string? StartDate { get; set; }

        /// <summary>
        /// Reserved background interval for PREPROD-01D.
        /// </summary>
        public int IntervalMinutes { get; set; } = 10;

        /// <summary>
        /// Maximum number of completed Worklists examined per reconciliation batch.
        /// </summary>
        public int BatchSize { get; set; } = 20;

        /// <summary>
        /// Completed studies must age for this many minutes before they become
        /// Reconciliation candidates. This reduces races with late C-STORE bursts.
        /// </summary>
        public int MinAgeMinutes { get; set; } = 10;
    }

}
