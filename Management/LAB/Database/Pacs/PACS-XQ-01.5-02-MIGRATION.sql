/* =====================================================================
   FILE:
   PACS-XQ-01.5-02-MIGRATION.sql

   PURPOSE:
   - Tích hợp PACS database schema vào Project A.
   - Shared PACS schema.
   - XQ là consumer đầu tiên, schema KHÔNG phụ thuộc XQ.

   CHANGES:
   1. Add 4 PACS columns to ResultCDHA
   2. Create PacsConfigs
   3. Create PacsStudies
   4. Create PacsSeries
   5. Create PacsInstances
   6. Create PacsWorklistItems
   7. Create PacsSyncStates
   8. Create unique index UX_PacsSyncStates_PacsConfigId

   IMPORTANT:
   - Script is rerun-safe.
   - Không tạo FK ngoài baseline Project B.
   - Không tạo module-specific PACS tables.
   - Không hard-code database name.
   ===================================================================== */

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @CurrentDatabase sysname = DB_NAME();

PRINT N'============================================================';
PRINT N' PACS DATABASE MIGRATION';
PRINT N' Database : ' + ISNULL(@CurrentDatabase, N'<NULL>');
PRINT N' Time     : ' + CONVERT(nvarchar(30), SYSDATETIME(), 121);
PRINT N'============================================================';

-----------------------------------------------------------------------
-- SAFETY GUARD
-----------------------------------------------------------------------

IF @CurrentDatabase IN (N'master', N'model', N'msdb', N'tempdb')
BEGIN
    RAISERROR(
        N'PACS MIGRATION: Không được chạy migration trên system database.',
        16,
        1
    );

    RETURN;
END;

IF OBJECT_ID(N'dbo.ResultCDHA', N'U') IS NULL
BEGIN
    RAISERROR(
        N'PACS MIGRATION: Không tìm thấy dbo.ResultCDHA. Dừng migration.',
        16,
        1
    );

    RETURN;
END;

BEGIN TRY

    BEGIN TRANSACTION;

    /* ================================================================
       STEP 1
       RESULTCDHA - PACS COLUMNS
       ================================================================ */

    PRINT N'';
    PRINT N'--- STEP 1: ResultCDHA PACS columns -------------------------';

    IF COL_LENGTH(N'dbo.ResultCDHA', N'AccessionNumber') IS NULL
    BEGIN
        ALTER TABLE dbo.ResultCDHA
        ADD AccessionNumber nvarchar(max) NULL;

        PRINT N'ADDED dbo.ResultCDHA.AccessionNumber';
    END
    ELSE
    BEGIN
        PRINT N'SKIP dbo.ResultCDHA.AccessionNumber - already exists';
    END;

    IF COL_LENGTH(N'dbo.ResultCDHA', N'StudyInstanceUID') IS NULL
    BEGIN
        ALTER TABLE dbo.ResultCDHA
        ADD StudyInstanceUID nvarchar(max) NULL;

        PRINT N'ADDED dbo.ResultCDHA.StudyInstanceUID';
    END
    ELSE
    BEGIN
        PRINT N'SKIP dbo.ResultCDHA.StudyInstanceUID - already exists';
    END;

    IF COL_LENGTH(N'dbo.ResultCDHA', N'OrthancStudyId') IS NULL
    BEGIN
        ALTER TABLE dbo.ResultCDHA
        ADD OrthancStudyId nvarchar(max) NULL;

        PRINT N'ADDED dbo.ResultCDHA.OrthancStudyId';
    END
    ELSE
    BEGIN
        PRINT N'SKIP dbo.ResultCDHA.OrthancStudyId - already exists';
    END;

    IF COL_LENGTH(N'dbo.ResultCDHA', N'PacsStatus') IS NULL
    BEGIN
        ALTER TABLE dbo.ResultCDHA
        ADD PacsStatus nvarchar(max) NULL;

        PRINT N'ADDED dbo.ResultCDHA.PacsStatus';
    END
    ELSE
    BEGIN
        PRINT N'SKIP dbo.ResultCDHA.PacsStatus - already exists';
    END;


    /* ================================================================
       STEP 2
       PACS CONFIGS
       ================================================================ */

    PRINT N'';
    PRINT N'--- STEP 2: PacsConfigs -------------------------------------';

    IF OBJECT_ID(N'dbo.PacsConfigs', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.PacsConfigs
        (
            Id bigint IDENTITY(1,1) NOT NULL,

            Code nvarchar(100) NOT NULL,
            Name nvarchar(250) NOT NULL,
            ServerType nvarchar(50) NOT NULL,

            BaseUrl nvarchar(500) NOT NULL,

            ApiUsername nvarchar(100) NOT NULL,
            ApiPassword nvarchar(500) NOT NULL,

            DicomAETitle nvarchar(100) NOT NULL,
            DicomHost nvarchar(250) NOT NULL,
            DicomPort int NULL,

            DicomWebRoot nvarchar(250) NOT NULL,
            OhifViewerUrl nvarchar(500) NOT NULL,

            SyncEnabled bit NOT NULL,
            SyncIntervalSeconds int NOT NULL,

            IsDefault bit NOT NULL,
            IsActive bit NOT NULL,

            LastStatus nvarchar(50) NOT NULL,

            LastApiStatus bit NULL,
            LastStudiesStatus bit NULL,
            LastDicomWebStatus bit NULL,
            LastDicomPortStatus bit NULL,

            LastCheckedAt datetime2 NULL,
            LastError nvarchar(max) NULL,

            CreatedOn datetime2 NOT NULL,
            CreatedBy bigint NULL,

            UpdatedOn datetime2 NULL,
            UpdatedBy bigint NULL,

            CONSTRAINT PK_PacsConfigs
                PRIMARY KEY CLUSTERED (Id)
        );

        PRINT N'CREATED dbo.PacsConfigs';
    END
    ELSE
    BEGIN
        PRINT N'SKIP dbo.PacsConfigs - already exists';
    END;


    /* ================================================================
       STEP 3
       PACS STUDIES
       ================================================================ */

    PRINT N'';
    PRINT N'--- STEP 3: PacsStudies -------------------------------------';

    IF OBJECT_ID(N'dbo.PacsStudies', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.PacsStudies
        (
            Id bigint IDENTITY(1,1) NOT NULL,

            PatientDbId bigint NULL,
            ResultCDHAId bigint NULL,
            ServiceId bigint NULL,

            PatientId nvarchar(max) NULL,
            PatientName nvarchar(max) NULL,
            PatientBirthDate nvarchar(max) NULL,
            PatientSex nvarchar(max) NULL,

            MaBenhAn nvarchar(max) NULL,

            AccessionNumber nvarchar(max) NULL,

            StudyInstanceUID nvarchar(max) NOT NULL,
            OrthancStudyId nvarchar(max) NOT NULL,

            StudyDate nvarchar(max) NOT NULL,
            StudyTime nvarchar(max) NOT NULL,

            StudyDescription nvarchar(max) NULL,
            RequestedProcedureDescription nvarchar(max) NULL,

            Modality nvarchar(max) NULL,
            ReferringPhysicianName nvarchar(max) NULL,
            InstitutionName nvarchar(max) NULL,

            SeriesCount int NOT NULL,
            InstanceCount int NOT NULL,

            Status nvarchar(max) NOT NULL,

            RawJson nvarchar(max) NOT NULL,

            CreatedOn datetime2 NOT NULL,
            UpdatedOn datetime2 NULL,

            CONSTRAINT PK_PacsStudies
                PRIMARY KEY CLUSTERED (Id)
        );

        PRINT N'CREATED dbo.PacsStudies';
    END
    ELSE
    BEGIN
        PRINT N'SKIP dbo.PacsStudies - already exists';
    END;


    /* ================================================================
       STEP 4
       PACS SERIES
       ================================================================ */

    PRINT N'';
    PRINT N'--- STEP 4: PacsSeries --------------------------------------';

    IF OBJECT_ID(N'dbo.PacsSeries', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.PacsSeries
        (
            Id bigint IDENTITY(1,1) NOT NULL,

            PacsStudyId bigint NOT NULL,

            OrthancSeriesId nvarchar(max) NOT NULL,
            OrthancParentStudyId nvarchar(max) NOT NULL,

            SeriesInstanceUID nvarchar(max) NULL,
            SeriesNumber nvarchar(max) NULL,
            SeriesDescription nvarchar(max) NULL,

            Modality nvarchar(max) NULL,
            ProtocolName nvarchar(max) NULL,

            InstanceCount int NOT NULL,

            OrthancStatus nvarchar(max) NOT NULL,

            RawJson nvarchar(max) NOT NULL,

            CreatedOn datetime2 NOT NULL,
            UpdatedOn datetime2 NULL,

            CONSTRAINT PK_PacsSeries
                PRIMARY KEY CLUSTERED (Id)
        );

        PRINT N'CREATED dbo.PacsSeries';
    END
    ELSE
    BEGIN
        PRINT N'SKIP dbo.PacsSeries - already exists';
    END;


    /* ================================================================
       STEP 5
       PACS INSTANCES
       ================================================================ */

    PRINT N'';
    PRINT N'--- STEP 5: PacsInstances -----------------------------------';

    IF OBJECT_ID(N'dbo.PacsInstances', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.PacsInstances
        (
            Id bigint IDENTITY(1,1) NOT NULL,

            PacsStudyId bigint NOT NULL,
            PacsSeriesId bigint NOT NULL,

            OrthancInstanceId nvarchar(max) NOT NULL,
            OrthancParentSeriesId nvarchar(max) NOT NULL,

            SOPInstanceUID nvarchar(max) NOT NULL,

            InstanceNumber nvarchar(max) NULL,
            ImageIndex nvarchar(max) NULL,

            SopClassUID nvarchar(max) NULL,
            TransferSyntaxUID nvarchar(max) NULL,

            OrthancStatus nvarchar(max) NULL,

            RawJson nvarchar(max) NOT NULL,

            CreatedOn datetime2 NOT NULL,
            UpdatedOn datetime2 NULL,

            CONSTRAINT PK_PacsInstances
                PRIMARY KEY CLUSTERED (Id)
        );

        PRINT N'CREATED dbo.PacsInstances';
    END
    ELSE
    BEGIN
        PRINT N'SKIP dbo.PacsInstances - already exists';
    END;


    /* ================================================================
       STEP 6
       PACS WORKLIST ITEMS
       ================================================================ */

    PRINT N'';
    PRINT N'--- STEP 6: PacsWorklistItems -------------------------------';

    IF OBJECT_ID(N'dbo.PacsWorklistItems', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.PacsWorklistItems
        (
            Id bigint IDENTITY(1,1) NOT NULL,

            ResultCDHAId bigint NOT NULL,
            PatientId bigint NULL,
            ServiceId bigint NULL,

            AccessionNumber nvarchar(max) NOT NULL,

            PatientCode nvarchar(max) NOT NULL,
            PatientName nvarchar(max) NOT NULL,
            PatientBirthDate datetime2 NULL,
            PatientSex nvarchar(max) NULL,

            MaBenhAn nvarchar(max) NULL,

            Seq nvarchar(max) NOT NULL,
            Sid nvarchar(max) NOT NULL,

            ServiceCode nvarchar(max) NOT NULL,
            ServiceName nvarchar(max) NOT NULL,
            CategoryCode nvarchar(max) NOT NULL,

            Modality nvarchar(max) NOT NULL,

            RequestedProcedureId nvarchar(max) NOT NULL,
            RequestedProcedureDescription nvarchar(max) NOT NULL,

            ScheduledStationAETitle nvarchar(max) NOT NULL,
            ScheduledProcedureStepId nvarchar(max) NOT NULL,

            ScheduledProcedureStepStartDate datetime2 NULL,
            ScheduledProcedureStepStartTime time NULL,

            ScheduledProcedureStepDescription nvarchar(max) NOT NULL,

            ReferringPhysicianName nvarchar(max) NULL,
            ScheduledPerformingPhysicianName nvarchar(max) NULL,

            StudyDescription nvarchar(max) NULL,
            Diagnosis nvarchar(max) NULL,

            DumpFileName nvarchar(max) NULL,
            DumpFilePath nvarchar(max) NULL,

            WorklistFileName nvarchar(max) NULL,
            WorklistFilePath nvarchar(max) NULL,

            OrthancWorklistId nvarchar(max) NULL,

            OrthancStudyId nvarchar(max) NULL,
            StudyInstanceUID nvarchar(max) NULL,

            Status int NOT NULL,
            ErrorMessage nvarchar(max) NULL,

            CreatedOn datetime2 NOT NULL,
            CreatedBy bigint NULL,

            UpdatedOn datetime2 NULL,
            UpdatedBy bigint NULL,

            PublishedOn datetime2 NULL,
            CompletedOn datetime2 NULL,
            CanceledOn datetime2 NULL,

            LastRecoveryCheckedOn datetime2 NULL,
            LastReconciliationCheckedOn datetime2 NULL,

            CONSTRAINT PK_PacsWorklistItems
                PRIMARY KEY CLUSTERED (Id)
        );

        PRINT N'CREATED dbo.PacsWorklistItems';
    END
    ELSE
    BEGIN
        PRINT N'SKIP dbo.PacsWorklistItems - already exists';
    END;


    /* ================================================================
       STEP 7
       PACS SYNC STATES
       ================================================================ */

    PRINT N'';
    PRINT N'--- STEP 7: PacsSyncStates ----------------------------------';

    IF OBJECT_ID(N'dbo.PacsSyncStates', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.PacsSyncStates
        (
            Id bigint IDENTITY(1,1) NOT NULL,

            PacsConfigId bigint NOT NULL,

            LastChangeSeq bigint NOT NULL
                CONSTRAINT DF_PacsSyncStates_LastChangeSeq
                DEFAULT (0),

            LastRunAt datetime2 NULL,
            LastSuccessAt datetime2 NULL,

            LastError nvarchar(max) NULL,

            CreatedOn datetime2 NOT NULL
                CONSTRAINT DF_PacsSyncStates_CreatedOn
                DEFAULT (GETDATE()),

            UpdatedOn datetime2 NULL,

            CONSTRAINT PK_PacsSyncStates
                PRIMARY KEY CLUSTERED (Id)
        );

        PRINT N'CREATED dbo.PacsSyncStates';
    END
    ELSE
    BEGIN
        PRINT N'SKIP dbo.PacsSyncStates - already exists';
    END;


    /* ================================================================
       STEP 8
       PACS SYNC UNIQUE INDEX
       ================================================================ */

    PRINT N'';
    PRINT N'--- STEP 8: PacsSyncStates unique index ---------------------';

    IF OBJECT_ID(N'dbo.PacsSyncStates', N'U') IS NOT NULL
       AND NOT EXISTS
       (
           SELECT 1
           FROM sys.indexes
           WHERE object_id = OBJECT_ID(N'dbo.PacsSyncStates')
             AND name = N'UX_PacsSyncStates_PacsConfigId'
       )
    BEGIN
        CREATE UNIQUE INDEX UX_PacsSyncStates_PacsConfigId
            ON dbo.PacsSyncStates(PacsConfigId);

        PRINT N'CREATED UX_PacsSyncStates_PacsConfigId';
    END
    ELSE
    BEGIN
        PRINT N'SKIP UX_PacsSyncStates_PacsConfigId - already exists';
    END;


    /* ================================================================
       COMMIT
       ================================================================ */

    COMMIT TRANSACTION;

    PRINT N'';
    PRINT N'============================================================';
    PRINT N' PACS DATABASE MIGRATION COMPLETED SUCCESSFULLY';
    PRINT N' Database : ' + DB_NAME();
    PRINT N'============================================================';

END TRY
BEGIN CATCH

    IF @@TRANCOUNT > 0
    BEGIN
        ROLLBACK TRANSACTION;
    END;

    DECLARE @ErrorNumber int;
    DECLARE @ErrorSeverity int;
    DECLARE @ErrorState int;
    DECLARE @ErrorLine int;
    DECLARE @ErrorProcedure nvarchar(128);
    DECLARE @ErrorMessage nvarchar(4000);

    SELECT
        @ErrorNumber = ERROR_NUMBER(),
        @ErrorSeverity = ERROR_SEVERITY(),
        @ErrorState = ERROR_STATE(),
        @ErrorLine = ERROR_LINE(),
        @ErrorProcedure = ERROR_PROCEDURE(),
        @ErrorMessage = ERROR_MESSAGE();

    PRINT N'';
    PRINT N'============================================================';
    PRINT N' PACS DATABASE MIGRATION FAILED';
    PRINT N'============================================================';

    SELECT
        @ErrorNumber AS ErrorNumber,
        @ErrorSeverity AS ErrorSeverity,
        @ErrorState AS ErrorState,
        @ErrorProcedure AS ErrorProcedure,
        @ErrorLine AS ErrorLine,
        @ErrorMessage AS ErrorMessage;

    RAISERROR(
        N'PACS DATABASE MIGRATION FAILED: %s',
        16,
        1,
        @ErrorMessage
    );

END CATCH;