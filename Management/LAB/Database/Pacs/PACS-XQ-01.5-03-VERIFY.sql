/* =====================================================================
   FILE:
   PACS-XQ-01.5-03-VERIFY.sql

   PURPOSE:
   Kiểm tra database SAU PACS migration.

   EXPECTED:
   - 4 PACS columns trong ResultCDHA
   - 6 PACS tables
   - Schema column đúng
   - 6 Primary Keys
   - Id của 6 PACS tables là IDENTITY
   - Unique index:
       UX_PacsSyncStates_PacsConfigId
   - Default:
       PacsSyncStates.LastChangeSeq = 0
       PacsSyncStates.CreatedOn     = GETDATE()
   - Không có physical FK PACS ngoài baseline
   ===================================================================== */

SET NOCOUNT ON;

DECLARE @CurrentDatabase sysname = DB_NAME();

PRINT N'============================================================';
PRINT N' PACS DATABASE VERIFY';
PRINT N' Database : ' + ISNULL(@CurrentDatabase, N'<NULL>');
PRINT N' Time     : ' + CONVERT(nvarchar(30), SYSDATETIME(), 121);
PRINT N'============================================================';

-----------------------------------------------------------------------
-- SAFETY
-----------------------------------------------------------------------

IF @CurrentDatabase IN (N'master', N'model', N'msdb', N'tempdb')
BEGIN
    RAISERROR(
        N'PACS VERIFY: Không được chạy trên system database.',
        16,
        1
    );

    RETURN;
END;

-----------------------------------------------------------------------
-- A. VERIFY ResultCDHA PACS COLUMNS
-----------------------------------------------------------------------

PRINT N'';
PRINT N'--- A. VERIFY ResultCDHA PACS COLUMNS -----------------------';

DECLARE @ExpectedResultCDHA TABLE
(
    ColumnName sysname NOT NULL,
    DataType nvarchar(128) NOT NULL,
    MaxLength int NULL,
    IsNullable varchar(3) NOT NULL
);

INSERT INTO @ExpectedResultCDHA
(
    ColumnName,
    DataType,
    MaxLength,
    IsNullable
)
VALUES
    (N'AccessionNumber',   N'nvarchar', -1, 'YES'),
    (N'StudyInstanceUID',  N'nvarchar', -1, 'YES'),
    (N'OrthancStudyId',    N'nvarchar', -1, 'YES'),
    (N'PacsStatus',        N'nvarchar', -1, 'YES');

SELECT
    e.ColumnName AS ExpectedColumn,
    e.DataType AS ExpectedType,
    e.MaxLength AS ExpectedMaxLength,
    e.IsNullable AS ExpectedNullable,

    c.COLUMN_NAME AS ActualColumn,
    c.DATA_TYPE AS ActualType,
    c.CHARACTER_MAXIMUM_LENGTH AS ActualMaxLength,
    c.IS_NULLABLE AS ActualNullable,

    CASE
        WHEN c.COLUMN_NAME IS NULL
            THEN N'MISSING'
        WHEN c.DATA_TYPE <> e.DataType
            THEN N'TYPE_MISMATCH'
        WHEN ISNULL(c.CHARACTER_MAXIMUM_LENGTH, -99999)
             <> ISNULL(e.MaxLength, -99999)
            THEN N'LENGTH_MISMATCH'
        WHEN c.IS_NULLABLE <> e.IsNullable
            THEN N'NULLABILITY_MISMATCH'
        ELSE N'PASS'
    END AS VerifyStatus
FROM @ExpectedResultCDHA e
LEFT JOIN INFORMATION_SCHEMA.COLUMNS c
    ON c.TABLE_SCHEMA = N'dbo'
   AND c.TABLE_NAME = N'ResultCDHA'
   AND c.COLUMN_NAME = e.ColumnName
ORDER BY e.ColumnName;

-----------------------------------------------------------------------
-- B. VERIFY ALL PACS TABLES EXIST
-----------------------------------------------------------------------

PRINT N'';
PRINT N'--- B. VERIFY PACS TABLES -----------------------------------';

DECLARE @ExpectedTables TABLE
(
    TableName sysname NOT NULL
);

INSERT INTO @ExpectedTables(TableName)
VALUES
    (N'PacsConfigs'),
    (N'PacsStudies'),
    (N'PacsSeries'),
    (N'PacsInstances'),
    (N'PacsWorklistItems'),
    (N'PacsSyncStates');

SELECT
    e.TableName,
    CASE
        WHEN t.object_id IS NULL
            THEN N'MISSING'
        ELSE N'PASS'
    END AS VerifyStatus
FROM @ExpectedTables e
LEFT JOIN sys.tables t
    ON t.name = e.TableName
LEFT JOIN sys.schemas s
    ON s.schema_id = t.schema_id
   AND s.name = N'dbo'
ORDER BY e.TableName;

-----------------------------------------------------------------------
-- C. EXPECTED PACS COLUMN SCHEMA
-----------------------------------------------------------------------

PRINT N'';
PRINT N'--- C. VERIFY PACS COLUMN SCHEMA -----------------------------';

DECLARE @ExpectedColumns TABLE
(
    TableName sysname NOT NULL,
    ColumnName sysname NOT NULL,
    DataType nvarchar(128) NOT NULL,
    MaxLength int NULL,
    IsNullable varchar(3) NOT NULL
);

-----------------------------------------------------------------------
-- PacsConfigs
-----------------------------------------------------------------------

INSERT INTO @ExpectedColumns VALUES
(N'PacsConfigs', N'Id',                  N'bigint',    NULL, 'NO'),
(N'PacsConfigs', N'Code',                N'nvarchar',  100,  'NO'),
(N'PacsConfigs', N'Name',                N'nvarchar',  250,  'NO'),
(N'PacsConfigs', N'ServerType',          N'nvarchar',  50,   'NO'),
(N'PacsConfigs', N'BaseUrl',             N'nvarchar',  500,  'NO'),
(N'PacsConfigs', N'ApiUsername',         N'nvarchar',  100,  'NO'),
(N'PacsConfigs', N'ApiPassword',         N'nvarchar',  500,  'NO'),
(N'PacsConfigs', N'DicomAETitle',        N'nvarchar',  100,  'NO'),
(N'PacsConfigs', N'DicomHost',           N'nvarchar',  250,  'NO'),
(N'PacsConfigs', N'DicomPort',           N'int',       NULL, 'YES'),
(N'PacsConfigs', N'DicomWebRoot',        N'nvarchar',  250,  'NO'),
(N'PacsConfigs', N'OhifViewerUrl',       N'nvarchar',  500,  'NO'),
(N'PacsConfigs', N'SyncEnabled',         N'bit',       NULL, 'NO'),
(N'PacsConfigs', N'SyncIntervalSeconds', N'int',       NULL, 'NO'),
(N'PacsConfigs', N'IsDefault',           N'bit',       NULL, 'NO'),
(N'PacsConfigs', N'IsActive',            N'bit',       NULL, 'NO'),
(N'PacsConfigs', N'LastStatus',          N'nvarchar',  50,   'NO'),
(N'PacsConfigs', N'LastApiStatus',       N'bit',       NULL, 'YES'),
(N'PacsConfigs', N'LastStudiesStatus',   N'bit',       NULL, 'YES'),
(N'PacsConfigs', N'LastDicomWebStatus',  N'bit',       NULL, 'YES'),
(N'PacsConfigs', N'LastDicomPortStatus', N'bit',       NULL, 'YES'),
(N'PacsConfigs', N'LastCheckedAt',       N'datetime2', NULL, 'YES'),
(N'PacsConfigs', N'LastError',           N'nvarchar',  -1,   'YES'),
(N'PacsConfigs', N'CreatedOn',           N'datetime2', NULL, 'NO'),
(N'PacsConfigs', N'CreatedBy',           N'bigint',    NULL, 'YES'),
(N'PacsConfigs', N'UpdatedOn',           N'datetime2', NULL, 'YES'),
(N'PacsConfigs', N'UpdatedBy',           N'bigint',    NULL, 'YES');

-----------------------------------------------------------------------
-- PacsStudies
-----------------------------------------------------------------------

INSERT INTO @ExpectedColumns VALUES
(N'PacsStudies', N'Id',                            N'bigint',    NULL, 'NO'),
(N'PacsStudies', N'PatientDbId',                   N'bigint',    NULL, 'YES'),
(N'PacsStudies', N'ResultCDHAId',                  N'bigint',    NULL, 'YES'),
(N'PacsStudies', N'ServiceId',                     N'bigint',    NULL, 'YES'),
(N'PacsStudies', N'PatientId',                     N'nvarchar',  -1,   'YES'),
(N'PacsStudies', N'PatientName',                   N'nvarchar',  -1,   'YES'),
(N'PacsStudies', N'PatientBirthDate',              N'nvarchar',  -1,   'YES'),
(N'PacsStudies', N'PatientSex',                    N'nvarchar',  -1,   'YES'),
(N'PacsStudies', N'MaBenhAn',                      N'nvarchar',  -1,   'YES'),
(N'PacsStudies', N'AccessionNumber',               N'nvarchar',  -1,   'YES'),
(N'PacsStudies', N'StudyInstanceUID',              N'nvarchar',  -1,   'NO'),
(N'PacsStudies', N'OrthancStudyId',                N'nvarchar',  -1,   'NO'),
(N'PacsStudies', N'StudyDate',                     N'nvarchar',  -1,   'NO'),
(N'PacsStudies', N'StudyTime',                     N'nvarchar',  -1,   'NO'),
(N'PacsStudies', N'StudyDescription',              N'nvarchar',  -1,   'YES'),
(N'PacsStudies', N'RequestedProcedureDescription', N'nvarchar',  -1,   'YES'),
(N'PacsStudies', N'Modality',                      N'nvarchar',  -1,   'YES'),
(N'PacsStudies', N'ReferringPhysicianName',        N'nvarchar',  -1,   'YES'),
(N'PacsStudies', N'InstitutionName',               N'nvarchar',  -1,   'YES'),
(N'PacsStudies', N'SeriesCount',                   N'int',       NULL, 'NO'),
(N'PacsStudies', N'InstanceCount',                 N'int',       NULL, 'NO'),
(N'PacsStudies', N'Status',                        N'nvarchar',  -1,   'NO'),
(N'PacsStudies', N'RawJson',                       N'nvarchar',  -1,   'NO'),
(N'PacsStudies', N'CreatedOn',                     N'datetime2', NULL, 'NO'),
(N'PacsStudies', N'UpdatedOn',                     N'datetime2', NULL, 'YES');

-----------------------------------------------------------------------
-- PacsSeries
-----------------------------------------------------------------------

INSERT INTO @ExpectedColumns VALUES
(N'PacsSeries', N'Id',                    N'bigint',    NULL, 'NO'),
(N'PacsSeries', N'PacsStudyId',           N'bigint',    NULL, 'NO'),
(N'PacsSeries', N'OrthancSeriesId',       N'nvarchar',  -1,   'NO'),
(N'PacsSeries', N'OrthancParentStudyId',  N'nvarchar',  -1,   'NO'),
(N'PacsSeries', N'SeriesInstanceUID',     N'nvarchar',  -1,   'YES'),
(N'PacsSeries', N'SeriesNumber',          N'nvarchar',  -1,   'YES'),
(N'PacsSeries', N'SeriesDescription',     N'nvarchar',  -1,   'YES'),
(N'PacsSeries', N'Modality',              N'nvarchar',  -1,   'YES'),
(N'PacsSeries', N'ProtocolName',          N'nvarchar',  -1,   'YES'),
(N'PacsSeries', N'InstanceCount',         N'int',       NULL, 'NO'),
(N'PacsSeries', N'OrthancStatus',         N'nvarchar',  -1,   'NO'),
(N'PacsSeries', N'RawJson',               N'nvarchar',  -1,   'NO'),
(N'PacsSeries', N'CreatedOn',             N'datetime2', NULL, 'NO'),
(N'PacsSeries', N'UpdatedOn',             N'datetime2', NULL, 'YES');

-----------------------------------------------------------------------
-- PacsInstances
-----------------------------------------------------------------------

INSERT INTO @ExpectedColumns VALUES
(N'PacsInstances', N'Id',                     N'bigint',    NULL, 'NO'),
(N'PacsInstances', N'PacsStudyId',            N'bigint',    NULL, 'NO'),
(N'PacsInstances', N'PacsSeriesId',           N'bigint',    NULL, 'NO'),
(N'PacsInstances', N'OrthancInstanceId',      N'nvarchar',  -1,   'NO'),
(N'PacsInstances', N'OrthancParentSeriesId',  N'nvarchar',  -1,   'NO'),
(N'PacsInstances', N'SOPInstanceUID',         N'nvarchar',  -1,   'NO'),
(N'PacsInstances', N'InstanceNumber',         N'nvarchar',  -1,   'YES'),
(N'PacsInstances', N'ImageIndex',             N'nvarchar',  -1,   'YES'),
(N'PacsInstances', N'SopClassUID',            N'nvarchar',  -1,   'YES'),
(N'PacsInstances', N'TransferSyntaxUID',      N'nvarchar',  -1,   'YES'),
(N'PacsInstances', N'OrthancStatus',          N'nvarchar',  -1,   'YES'),
(N'PacsInstances', N'RawJson',                N'nvarchar',  -1,   'NO'),
(N'PacsInstances', N'CreatedOn',              N'datetime2', NULL, 'NO'),
(N'PacsInstances', N'UpdatedOn',              N'datetime2', NULL, 'YES');

-----------------------------------------------------------------------
-- PacsWorklistItems
-----------------------------------------------------------------------

INSERT INTO @ExpectedColumns VALUES
(N'PacsWorklistItems', N'Id',                                  N'bigint',    NULL, 'NO'),
(N'PacsWorklistItems', N'ResultCDHAId',                        N'bigint',    NULL, 'NO'),
(N'PacsWorklistItems', N'PatientId',                           N'bigint',    NULL, 'YES'),
(N'PacsWorklistItems', N'ServiceId',                           N'bigint',    NULL, 'YES'),
(N'PacsWorklistItems', N'AccessionNumber',                     N'nvarchar',  -1,   'NO'),
(N'PacsWorklistItems', N'PatientCode',                         N'nvarchar',  -1,   'NO'),
(N'PacsWorklistItems', N'PatientName',                         N'nvarchar',  -1,   'NO'),
(N'PacsWorklistItems', N'PatientBirthDate',                    N'datetime2', NULL, 'YES'),
(N'PacsWorklistItems', N'PatientSex',                          N'nvarchar',  -1,   'YES'),
(N'PacsWorklistItems', N'MaBenhAn',                            N'nvarchar',  -1,   'YES'),
(N'PacsWorklistItems', N'Seq',                                 N'nvarchar',  -1,   'NO'),
(N'PacsWorklistItems', N'Sid',                                 N'nvarchar',  -1,   'NO'),
(N'PacsWorklistItems', N'ServiceCode',                         N'nvarchar',  -1,   'NO'),
(N'PacsWorklistItems', N'ServiceName',                         N'nvarchar',  -1,   'NO'),
(N'PacsWorklistItems', N'CategoryCode',                        N'nvarchar',  -1,   'NO'),
(N'PacsWorklistItems', N'Modality',                            N'nvarchar',  -1,   'NO'),
(N'PacsWorklistItems', N'RequestedProcedureId',                N'nvarchar',  -1,   'NO'),
(N'PacsWorklistItems', N'RequestedProcedureDescription',       N'nvarchar',  -1,   'NO'),
(N'PacsWorklistItems', N'ScheduledStationAETitle',             N'nvarchar',  -1,   'NO'),
(N'PacsWorklistItems', N'ScheduledProcedureStepId',            N'nvarchar',  -1,   'NO'),
(N'PacsWorklistItems', N'ScheduledProcedureStepStartDate',     N'datetime2', NULL, 'YES'),
(N'PacsWorklistItems', N'ScheduledProcedureStepStartTime',     N'time',      NULL, 'YES'),
(N'PacsWorklistItems', N'ScheduledProcedureStepDescription',   N'nvarchar',  -1,   'NO'),
(N'PacsWorklistItems', N'ReferringPhysicianName',              N'nvarchar',  -1,   'YES'),
(N'PacsWorklistItems', N'ScheduledPerformingPhysicianName',    N'nvarchar',  -1,   'YES'),
(N'PacsWorklistItems', N'StudyDescription',                    N'nvarchar',  -1,   'YES'),
(N'PacsWorklistItems', N'Diagnosis',                           N'nvarchar',  -1,   'YES'),
(N'PacsWorklistItems', N'DumpFileName',                        N'nvarchar',  -1,   'YES'),
(N'PacsWorklistItems', N'DumpFilePath',                        N'nvarchar',  -1,   'YES'),
(N'PacsWorklistItems', N'WorklistFileName',                    N'nvarchar',  -1,   'YES'),
(N'PacsWorklistItems', N'WorklistFilePath',                    N'nvarchar',  -1,   'YES'),
(N'PacsWorklistItems', N'OrthancWorklistId',                   N'nvarchar',  -1,   'YES'),
(N'PacsWorklistItems', N'OrthancStudyId',                      N'nvarchar',  -1,   'YES'),
(N'PacsWorklistItems', N'StudyInstanceUID',                    N'nvarchar',  -1,   'YES'),
(N'PacsWorklistItems', N'Status',                              N'int',       NULL, 'NO'),
(N'PacsWorklistItems', N'ErrorMessage',                        N'nvarchar',  -1,   'YES'),
(N'PacsWorklistItems', N'CreatedOn',                           N'datetime2', NULL, 'NO'),
(N'PacsWorklistItems', N'CreatedBy',                           N'bigint',    NULL, 'YES'),
(N'PacsWorklistItems', N'UpdatedOn',                           N'datetime2', NULL, 'YES'),
(N'PacsWorklistItems', N'UpdatedBy',                           N'bigint',    NULL, 'YES'),
(N'PacsWorklistItems', N'PublishedOn',                         N'datetime2', NULL, 'YES'),
(N'PacsWorklistItems', N'CompletedOn',                         N'datetime2', NULL, 'YES'),
(N'PacsWorklistItems', N'CanceledOn',                          N'datetime2', NULL, 'YES'),
(N'PacsWorklistItems', N'LastRecoveryCheckedOn',               N'datetime2', NULL, 'YES'),
(N'PacsWorklistItems', N'LastReconciliationCheckedOn',         N'datetime2', NULL, 'YES');

-----------------------------------------------------------------------
-- PacsSyncStates
-----------------------------------------------------------------------

INSERT INTO @ExpectedColumns VALUES
(N'PacsSyncStates', N'Id',            N'bigint',    NULL, 'NO'),
(N'PacsSyncStates', N'PacsConfigId',  N'bigint',    NULL, 'NO'),
(N'PacsSyncStates', N'LastChangeSeq', N'bigint',    NULL, 'NO'),
(N'PacsSyncStates', N'LastRunAt',     N'datetime2', NULL, 'YES'),
(N'PacsSyncStates', N'LastSuccessAt', N'datetime2', NULL, 'YES'),
(N'PacsSyncStates', N'LastError',     N'nvarchar',  -1,   'YES'),
(N'PacsSyncStates', N'CreatedOn',     N'datetime2', NULL, 'NO'),
(N'PacsSyncStates', N'UpdatedOn',     N'datetime2', NULL, 'YES');

-----------------------------------------------------------------------
-- COMPARE EXPECTED VS ACTUAL
-----------------------------------------------------------------------

SELECT
    e.TableName,
    e.ColumnName,

    e.DataType AS ExpectedType,
    e.MaxLength AS ExpectedMaxLength,
    e.IsNullable AS ExpectedNullable,

    c.DATA_TYPE AS ActualType,
    c.CHARACTER_MAXIMUM_LENGTH AS ActualMaxLength,
    c.IS_NULLABLE AS ActualNullable,

    CASE
        WHEN c.COLUMN_NAME IS NULL
            THEN N'MISSING'

        WHEN c.DATA_TYPE <> e.DataType
            THEN N'TYPE_MISMATCH'

        WHEN e.DataType IN (N'nvarchar', N'varchar', N'char', N'nchar')
             AND ISNULL(c.CHARACTER_MAXIMUM_LENGTH, -99999)
                 <> ISNULL(e.MaxLength, -99999)
            THEN N'LENGTH_MISMATCH'

        WHEN c.IS_NULLABLE <> e.IsNullable
            THEN N'NULLABILITY_MISMATCH'

        ELSE N'PASS'
    END AS VerifyStatus

FROM @ExpectedColumns e

LEFT JOIN INFORMATION_SCHEMA.COLUMNS c
    ON c.TABLE_SCHEMA = N'dbo'
   AND c.TABLE_NAME = e.TableName
   AND c.COLUMN_NAME = e.ColumnName

ORDER BY
    e.TableName,
    e.ColumnName;

-----------------------------------------------------------------------
-- D. VERIFY PRIMARY KEYS
-----------------------------------------------------------------------

PRINT N'';
PRINT N'--- D. VERIFY PRIMARY KEYS ----------------------------------';

SELECT
    t.name AS TableName,
    kc.name AS PrimaryKeyName,
    CASE
        WHEN kc.object_id IS NULL
            THEN N'MISSING'
        ELSE N'PASS'
    END AS VerifyStatus
FROM @ExpectedTables e
LEFT JOIN sys.tables t
    ON t.name = e.TableName
   AND SCHEMA_NAME(t.schema_id) = N'dbo'
LEFT JOIN sys.key_constraints kc
    ON kc.parent_object_id = t.object_id
   AND kc.type = N'PK'
ORDER BY e.TableName;

-----------------------------------------------------------------------
-- E. VERIFY IDENTITY COLUMN
-----------------------------------------------------------------------

PRINT N'';
PRINT N'--- E. VERIFY IDENTITY --------------------------------------';

SELECT
    e.TableName,
    c.name AS ColumnName,
    c.is_identity AS IsIdentity,
    CASE
        WHEN c.name IS NULL
            THEN N'MISSING'
        WHEN c.is_identity = 1
            THEN N'PASS'
        ELSE N'NOT_IDENTITY'
    END AS VerifyStatus
FROM @ExpectedTables e
LEFT JOIN sys.tables t
    ON t.name = e.TableName
   AND SCHEMA_NAME(t.schema_id) = N'dbo'
LEFT JOIN sys.columns c
    ON c.object_id = t.object_id
   AND c.name = N'Id'
ORDER BY e.TableName;

-----------------------------------------------------------------------
-- F. VERIFY PacsSyncStates UNIQUE INDEX
-----------------------------------------------------------------------

PRINT N'';
PRINT N'--- F. VERIFY SYNC UNIQUE INDEX -----------------------------';

SELECT
    t.name AS TableName,
    i.name AS IndexName,
    i.is_unique AS IsUnique,
    i.is_disabled AS IsDisabled,
    CASE
        WHEN i.index_id IS NULL
            THEN N'MISSING'
        WHEN i.is_unique <> 1
            THEN N'NOT_UNIQUE'
        WHEN i.is_disabled = 1
            THEN N'DISABLED'
        ELSE N'PASS'
    END AS VerifyStatus
FROM sys.tables t
LEFT JOIN sys.indexes i
    ON i.object_id = t.object_id
   AND i.name = N'UX_PacsSyncStates_PacsConfigId'
WHERE t.name = N'PacsSyncStates'
  AND SCHEMA_NAME(t.schema_id) = N'dbo';

-----------------------------------------------------------------------
-- G. VERIFY PacsSyncStates DEFAULTS
-----------------------------------------------------------------------

PRINT N'';
PRINT N'--- G. VERIFY SYNC DEFAULT CONSTRAINTS ----------------------';

SELECT
    c.name AS ColumnName,
    dc.name AS ConstraintName,
    dc.definition AS DefaultDefinition
FROM sys.tables t
INNER JOIN sys.columns c
    ON c.object_id = t.object_id
LEFT JOIN sys.default_constraints dc
    ON dc.parent_object_id = c.object_id
   AND dc.parent_column_id = c.column_id
WHERE t.name = N'PacsSyncStates'
  AND SCHEMA_NAME(t.schema_id) = N'dbo'
  AND c.name IN
  (
      N'LastChangeSeq',
      N'CreatedOn'
  )
ORDER BY c.name;

-----------------------------------------------------------------------
-- H. VERIFY NO PACS FOREIGN KEYS
-- Baseline hiện tại dùng logical relationship, không physical FK.
-----------------------------------------------------------------------

PRINT N'';
PRINT N'--- H. VERIFY PACS FOREIGN KEYS -----------------------------';

SELECT
    fk.name AS ForeignKeyName,
    OBJECT_SCHEMA_NAME(fk.parent_object_id) AS SchemaName,
    OBJECT_NAME(fk.parent_object_id) AS ParentTable,
    OBJECT_NAME(fk.referenced_object_id) AS ReferencedTable
FROM sys.foreign_keys fk
WHERE OBJECT_NAME(fk.parent_object_id) IN
(
    N'PacsConfigs',
    N'PacsStudies',
    N'PacsSeries',
    N'PacsInstances',
    N'PacsWorklistItems',
    N'PacsSyncStates'
)
ORDER BY ParentTable, ForeignKeyName;

-----------------------------------------------------------------------
-- I. CURRENT ROW COUNTS
-----------------------------------------------------------------------

PRINT N'';
PRINT N'--- I. PACS ROW COUNTS --------------------------------------';

IF OBJECT_ID(N'dbo.PacsConfigs', N'U') IS NOT NULL
BEGIN
    SELECT
        N'PacsConfigs' AS TableName,
        COUNT_BIG(*) AS [RowCount]
    FROM dbo.PacsConfigs;
END;

IF OBJECT_ID(N'dbo.PacsStudies', N'U') IS NOT NULL
BEGIN
    SELECT
        N'PacsStudies' AS TableName,
        COUNT_BIG(*) AS [RowCount]
    FROM dbo.PacsStudies;
END;

IF OBJECT_ID(N'dbo.PacsSeries', N'U') IS NOT NULL
BEGIN
    SELECT
        N'PacsSeries' AS TableName,
        COUNT_BIG(*) AS [RowCount]
    FROM dbo.PacsSeries;
END;

IF OBJECT_ID(N'dbo.PacsInstances', N'U') IS NOT NULL
BEGIN
    SELECT
        N'PacsInstances' AS TableName,
        COUNT_BIG(*) AS [RowCount]
    FROM dbo.PacsInstances;
END;

IF OBJECT_ID(N'dbo.PacsWorklistItems', N'U') IS NOT NULL
BEGIN
    SELECT
        N'PacsWorklistItems' AS TableName,
        COUNT_BIG(*) AS [RowCount]
    FROM dbo.PacsWorklistItems;
END;

IF OBJECT_ID(N'dbo.PacsSyncStates', N'U') IS NOT NULL
BEGIN
    SELECT
        N'PacsSyncStates' AS TableName,
        COUNT_BIG(*) AS [RowCount]
    FROM dbo.PacsSyncStates;
END;

-----------------------------------------------------------------------
-- J. FINAL SUMMARY
-----------------------------------------------------------------------

PRINT N'';
PRINT N'--- J. FINAL VERIFY SUMMARY ---------------------------------';

DECLARE @MissingResultColumns int;

SELECT
    @MissingResultColumns = COUNT(*)
FROM @ExpectedResultCDHA e
LEFT JOIN INFORMATION_SCHEMA.COLUMNS c
    ON c.TABLE_SCHEMA = N'dbo'
   AND c.TABLE_NAME = N'ResultCDHA'
   AND c.COLUMN_NAME = e.ColumnName
WHERE c.COLUMN_NAME IS NULL;

DECLARE @MissingTables int;

SELECT
    @MissingTables = COUNT(*)
FROM @ExpectedTables e
LEFT JOIN sys.tables t
    ON t.name = e.TableName
   AND SCHEMA_NAME(t.schema_id) = N'dbo'
WHERE t.object_id IS NULL;

DECLARE @MissingPacsColumns int;

SELECT
    @MissingPacsColumns = COUNT(*)
FROM @ExpectedColumns e
LEFT JOIN INFORMATION_SCHEMA.COLUMNS c
    ON c.TABLE_SCHEMA = N'dbo'
   AND c.TABLE_NAME = e.TableName
   AND c.COLUMN_NAME = e.ColumnName
WHERE c.COLUMN_NAME IS NULL;

DECLARE @SyncUniqueIndexExists bit = 0;

IF EXISTS
(
    SELECT 1
    FROM sys.indexes i
    WHERE i.object_id = OBJECT_ID(N'dbo.PacsSyncStates')
      AND i.name = N'UX_PacsSyncStates_PacsConfigId'
      AND i.is_unique = 1
      AND i.is_disabled = 0
)
BEGIN
    SET @SyncUniqueIndexExists = 1;
END;

DECLARE @LastChangeSeqDefaultExists bit = 0;

IF EXISTS
(
    SELECT 1
    FROM sys.default_constraints dc
    INNER JOIN sys.columns c
        ON c.object_id = dc.parent_object_id
       AND c.column_id = dc.parent_column_id
    WHERE dc.parent_object_id = OBJECT_ID(N'dbo.PacsSyncStates')
      AND c.name = N'LastChangeSeq'
)
BEGIN
    SET @LastChangeSeqDefaultExists = 1;
END;

DECLARE @CreatedOnDefaultExists bit = 0;

IF EXISTS
(
    SELECT 1
    FROM sys.default_constraints dc
    INNER JOIN sys.columns c
        ON c.object_id = dc.parent_object_id
       AND c.column_id = dc.parent_column_id
    WHERE dc.parent_object_id = OBJECT_ID(N'dbo.PacsSyncStates')
      AND c.name = N'CreatedOn'
)
BEGIN
    SET @CreatedOnDefaultExists = 1;
END;

SELECT
    @CurrentDatabase AS CurrentDatabase,

    @MissingResultColumns AS MissingResultCDHAColumns,
    @MissingTables AS MissingPacsTables,
    @MissingPacsColumns AS MissingPacsColumns,

    @SyncUniqueIndexExists AS SyncUniqueIndexExists,
    @LastChangeSeqDefaultExists AS LastChangeSeqDefaultExists,
    @CreatedOnDefaultExists AS CreatedOnDefaultExists,

    CASE
        WHEN @MissingResultColumns = 0
         AND @MissingTables = 0
         AND @MissingPacsColumns = 0
         AND @SyncUniqueIndexExists = 1
         AND @LastChangeSeqDefaultExists = 1
         AND @CreatedOnDefaultExists = 1
            THEN N'PASS'
        ELSE N'FAIL'
    END AS OverallStatus;

PRINT N'';
PRINT N'============================================================';
PRINT N' PACS DATABASE VERIFY FINISHED';
PRINT N'============================================================';