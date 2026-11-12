/* =====================================================================
 PACS-XQ-04-02-02-MIGRATION.sql
 Project A - Shared Study / Series / Instance Sync

 Hardening:
   1. Normalize PACS identity/correlation columns to nvarchar(64)
   2. Add Worklist uniqueness guards already required by Phase 03 contract
   3. Add Study / Series / Instance unique identity indexes
   4. Add lookup indexes used by Phase 04 sync/viewer

 Rerun safe.
 ===================================================================== */

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @CurrentDatabase sysname = DB_NAME();

IF @CurrentDatabase IN (N'master', N'model', N'msdb', N'tempdb')
BEGIN
    THROW 51100, N'Không chạy PACS-XQ-04-02 trên system database.', 1;
END;

IF OBJECT_ID(N'dbo.ResultCDHA', N'U') IS NULL
 OR OBJECT_ID(N'dbo.PacsWorklistItems', N'U') IS NULL
 OR OBJECT_ID(N'dbo.PacsStudies', N'U') IS NULL
 OR OBJECT_ID(N'dbo.PacsSeries', N'U') IS NULL
 OR OBJECT_ID(N'dbo.PacsInstances', N'U') IS NULL
BEGIN
    THROW 51101, N'Thiếu table PACS bắt buộc.', 1;
END;

-- Fail fast on oversized values.
IF EXISTS (SELECT 1 FROM dbo.ResultCDHA WHERE LEN(AccessionNumber) > 64 OR LEN(StudyInstanceUID) > 64 OR LEN(OrthancStudyId) > 64)
 OR EXISTS (SELECT 1 FROM dbo.PacsWorklistItems WHERE LEN(AccessionNumber) > 64 OR LEN(StudyInstanceUID) > 64 OR LEN(OrthancStudyId) > 64)
 OR EXISTS (SELECT 1 FROM dbo.PacsStudies WHERE LEN(AccessionNumber) > 64 OR LEN(StudyInstanceUID) > 64 OR LEN(OrthancStudyId) > 64)
 OR EXISTS (SELECT 1 FROM dbo.PacsSeries WHERE LEN(OrthancSeriesId) > 64 OR LEN(OrthancParentStudyId) > 64 OR LEN(SeriesInstanceUID) > 64)
 OR EXISTS (SELECT 1 FROM dbo.PacsInstances WHERE LEN(OrthancInstanceId) > 64 OR LEN(OrthancParentSeriesId) > 64 OR LEN(SOPInstanceUID) > 64 OR LEN(SopClassUID) > 64 OR LEN(TransferSyntaxUID) > 64)
BEGIN
    THROW 51102, N'Có PACS identity dài hơn 64 ký tự. Dừng migration để review.', 1;
END;

-- Fail fast on duplicate identities before unique indexes.
IF EXISTS
(
    SELECT ResultCDHAId
    FROM dbo.PacsWorklistItems
    GROUP BY ResultCDHAId
    HAVING COUNT(*) > 1
)
BEGIN
    THROW 51103, N'Duplicate PacsWorklistItems.ResultCDHAId.', 1;
END;

IF EXISTS
(
    SELECT AccessionNumber
    FROM dbo.PacsWorklistItems
    WHERE NULLIF(LTRIM(RTRIM(AccessionNumber)), N'') IS NOT NULL
    GROUP BY AccessionNumber
    HAVING COUNT(*) > 1
)
BEGIN
    THROW 51104, N'Duplicate PacsWorklistItems.AccessionNumber.', 1;
END;

IF EXISTS
(
    SELECT StudyInstanceUID
    FROM dbo.PacsStudies
    WHERE NULLIF(LTRIM(RTRIM(StudyInstanceUID)), N'') IS NOT NULL
    GROUP BY StudyInstanceUID
    HAVING COUNT(*) > 1
)
BEGIN
    THROW 51105, N'Duplicate PacsStudies.StudyInstanceUID.', 1;
END;

IF EXISTS
(
    SELECT OrthancStudyId
    FROM dbo.PacsStudies
    WHERE NULLIF(LTRIM(RTRIM(OrthancStudyId)), N'') IS NOT NULL
    GROUP BY OrthancStudyId
    HAVING COUNT(*) > 1
)
BEGIN
    THROW 51106, N'Duplicate PacsStudies.OrthancStudyId.', 1;
END;

IF EXISTS
(
    SELECT OrthancSeriesId
    FROM dbo.PacsSeries
    WHERE NULLIF(LTRIM(RTRIM(OrthancSeriesId)), N'') IS NOT NULL
    GROUP BY OrthancSeriesId
    HAVING COUNT(*) > 1
)
BEGIN
    THROW 51107, N'Duplicate PacsSeries.OrthancSeriesId.', 1;
END;

IF EXISTS
(
    SELECT SeriesInstanceUID
    FROM dbo.PacsSeries
    WHERE NULLIF(LTRIM(RTRIM(SeriesInstanceUID)), N'') IS NOT NULL
    GROUP BY SeriesInstanceUID
    HAVING COUNT(*) > 1
)
BEGIN
    THROW 51108, N'Duplicate PacsSeries.SeriesInstanceUID.', 1;
END;

IF EXISTS
(
    SELECT OrthancInstanceId
    FROM dbo.PacsInstances
    WHERE NULLIF(LTRIM(RTRIM(OrthancInstanceId)), N'') IS NOT NULL
    GROUP BY OrthancInstanceId
    HAVING COUNT(*) > 1
)
BEGIN
    THROW 51109, N'Duplicate PacsInstances.OrthancInstanceId.', 1;
END;

IF EXISTS
(
    SELECT SOPInstanceUID
    FROM dbo.PacsInstances
    WHERE NULLIF(LTRIM(RTRIM(SOPInstanceUID)), N'') IS NOT NULL
    GROUP BY SOPInstanceUID
    HAVING COUNT(*) > 1
)
BEGIN
    THROW 51110, N'Duplicate PacsInstances.SOPInstanceUID.', 1;
END;

BEGIN TRY
    BEGIN TRANSACTION;

    PRINT N'--- 1. NORMALIZE IDENTITY COLUMN TYPES ----------------------';

    ALTER TABLE dbo.ResultCDHA
        ALTER COLUMN AccessionNumber nvarchar(64) NULL;
    ALTER TABLE dbo.ResultCDHA
        ALTER COLUMN StudyInstanceUID nvarchar(64) NULL;
    ALTER TABLE dbo.ResultCDHA
        ALTER COLUMN OrthancStudyId nvarchar(64) NULL;

    ALTER TABLE dbo.PacsWorklistItems
        ALTER COLUMN AccessionNumber nvarchar(64) NOT NULL;
    ALTER TABLE dbo.PacsWorklistItems
        ALTER COLUMN StudyInstanceUID nvarchar(64) NULL;
    ALTER TABLE dbo.PacsWorklistItems
        ALTER COLUMN OrthancStudyId nvarchar(64) NULL;

    ALTER TABLE dbo.PacsStudies
        ALTER COLUMN AccessionNumber nvarchar(64) NULL;
    ALTER TABLE dbo.PacsStudies
        ALTER COLUMN StudyInstanceUID nvarchar(64) NOT NULL;
    ALTER TABLE dbo.PacsStudies
        ALTER COLUMN OrthancStudyId nvarchar(64) NOT NULL;

    ALTER TABLE dbo.PacsSeries
        ALTER COLUMN OrthancSeriesId nvarchar(64) NOT NULL;
    ALTER TABLE dbo.PacsSeries
        ALTER COLUMN OrthancParentStudyId nvarchar(64) NOT NULL;
    ALTER TABLE dbo.PacsSeries
        ALTER COLUMN SeriesInstanceUID nvarchar(64) NULL;

    ALTER TABLE dbo.PacsInstances
        ALTER COLUMN OrthancInstanceId nvarchar(64) NOT NULL;
    ALTER TABLE dbo.PacsInstances
        ALTER COLUMN OrthancParentSeriesId nvarchar(64) NOT NULL;
    ALTER TABLE dbo.PacsInstances
        ALTER COLUMN SOPInstanceUID nvarchar(64) NOT NULL;
    ALTER TABLE dbo.PacsInstances
        ALTER COLUMN SopClassUID nvarchar(64) NULL;
    ALTER TABLE dbo.PacsInstances
        ALTER COLUMN TransferSyntaxUID nvarchar(64) NULL;

    PRINT N'--- 2. RESULT / WORKLIST LOOKUP + UNIQUE INDEXES ------------';

    IF NOT EXISTS
    (
        SELECT 1 FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.ResultCDHA')
          AND name = N'IX_ResultCDHA_AccessionNumber'
    )
    BEGIN
        CREATE INDEX IX_ResultCDHA_AccessionNumber
            ON dbo.ResultCDHA(AccessionNumber)
            WHERE AccessionNumber IS NOT NULL;
    END;

    IF NOT EXISTS
    (
        SELECT 1 FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.PacsWorklistItems')
          AND name = N'UX_PacsWorklistItems_ResultCDHAId'
    )
    BEGIN
        CREATE UNIQUE INDEX UX_PacsWorklistItems_ResultCDHAId
            ON dbo.PacsWorklistItems(ResultCDHAId);
    END;

    IF NOT EXISTS
    (
        SELECT 1 FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.PacsWorklistItems')
          AND name = N'UX_PacsWorklistItems_AccessionNumber'
    )
    BEGIN
        CREATE UNIQUE INDEX UX_PacsWorklistItems_AccessionNumber
            ON dbo.PacsWorklistItems(AccessionNumber);
    END;

    PRINT N'--- 3. STUDY INDEXES ----------------------------------------';

    IF NOT EXISTS
    (
        SELECT 1 FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.PacsStudies')
          AND name = N'IX_PacsStudies_ResultCDHAId'
    )
    BEGIN
        CREATE INDEX IX_PacsStudies_ResultCDHAId
            ON dbo.PacsStudies(ResultCDHAId)
            WHERE ResultCDHAId IS NOT NULL;
    END;

    IF NOT EXISTS
    (
        SELECT 1 FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.PacsStudies')
          AND name = N'IX_PacsStudies_AccessionNumber'
    )
    BEGIN
        CREATE INDEX IX_PacsStudies_AccessionNumber
            ON dbo.PacsStudies(AccessionNumber)
            WHERE AccessionNumber IS NOT NULL;
    END;

    IF NOT EXISTS
    (
        SELECT 1 FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.PacsStudies')
          AND name = N'UX_PacsStudies_StudyInstanceUID'
    )
    BEGIN
        CREATE UNIQUE INDEX UX_PacsStudies_StudyInstanceUID
            ON dbo.PacsStudies(StudyInstanceUID);
    END;

    IF NOT EXISTS
    (
        SELECT 1 FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.PacsStudies')
          AND name = N'UX_PacsStudies_OrthancStudyId'
    )
    BEGIN
        CREATE UNIQUE INDEX UX_PacsStudies_OrthancStudyId
            ON dbo.PacsStudies(OrthancStudyId);
    END;

    PRINT N'--- 4. SERIES INDEXES ---------------------------------------';

    IF NOT EXISTS
    (
        SELECT 1 FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.PacsSeries')
          AND name = N'IX_PacsSeries_PacsStudyId'
    )
    BEGIN
        CREATE INDEX IX_PacsSeries_PacsStudyId
            ON dbo.PacsSeries(PacsStudyId);
    END;

    IF NOT EXISTS
    (
        SELECT 1 FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.PacsSeries')
          AND name = N'UX_PacsSeries_OrthancSeriesId'
    )
    BEGIN
        CREATE UNIQUE INDEX UX_PacsSeries_OrthancSeriesId
            ON dbo.PacsSeries(OrthancSeriesId);
    END;

    IF NOT EXISTS
    (
        SELECT 1 FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.PacsSeries')
          AND name = N'UX_PacsSeries_SeriesInstanceUID'
    )
    BEGIN
        CREATE UNIQUE INDEX UX_PacsSeries_SeriesInstanceUID
            ON dbo.PacsSeries(SeriesInstanceUID)
            WHERE SeriesInstanceUID IS NOT NULL;
    END;

    PRINT N'--- 5. INSTANCE INDEXES -------------------------------------';

    IF NOT EXISTS
    (
        SELECT 1 FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.PacsInstances')
          AND name = N'IX_PacsInstances_PacsStudyId'
    )
    BEGIN
        CREATE INDEX IX_PacsInstances_PacsStudyId
            ON dbo.PacsInstances(PacsStudyId);
    END;

    IF NOT EXISTS
    (
        SELECT 1 FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.PacsInstances')
          AND name = N'IX_PacsInstances_PacsSeriesId'
    )
    BEGIN
        CREATE INDEX IX_PacsInstances_PacsSeriesId
            ON dbo.PacsInstances(PacsSeriesId);
    END;

    IF NOT EXISTS
    (
        SELECT 1 FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.PacsInstances')
          AND name = N'UX_PacsInstances_OrthancInstanceId'
    )
    BEGIN
        CREATE UNIQUE INDEX UX_PacsInstances_OrthancInstanceId
            ON dbo.PacsInstances(OrthancInstanceId);
    END;

    IF NOT EXISTS
    (
        SELECT 1 FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.PacsInstances')
          AND name = N'UX_PacsInstances_SOPInstanceUID'
    )
    BEGIN
        CREATE UNIQUE INDEX UX_PacsInstances_SOPInstanceUID
            ON dbo.PacsInstances(SOPInstanceUID);
    END;

    COMMIT TRANSACTION;

    PRINT N'============================================================';
    PRINT N' PACS-XQ-04-02 MIGRATION COMPLETED';
    PRINT N'============================================================';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    DECLARE @ErrorMessage nvarchar(4000) = ERROR_MESSAGE();
    THROW 51199, @ErrorMessage, 1;
END CATCH;
