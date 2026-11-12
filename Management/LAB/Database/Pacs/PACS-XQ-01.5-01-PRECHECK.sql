/* =====================================================================
   FILE:
   PACS-XQ-01.5-01-PRECHECK.sql

   PURPOSE:
   - Kiểm tra trạng thái database TRƯỚC khi tích hợp PACS.
   - KHÔNG tạo table.
   - KHÔNG ALTER table.
   - KHÔNG INSERT/UPDATE/DELETE dữ liệu.

   PACS architecture:
   - Shared PACS subsystem.
   - XQ chỉ là consumer đầu tiên.
   - Có thể reuse cho CT / MRI / MG / SA / ... sau này.

   EXPECTED BEFORE FIRST MIGRATION:
   - ResultCDHA PACS columns : chưa tồn tại
   - PACS tables             : chưa tồn tại
   - PacsSyncStates index    : chưa tồn tại
   ===================================================================== */

SET NOCOUNT ON;

DECLARE @CurrentDatabase sysname = DB_NAME();

PRINT N'============================================================';
PRINT N' PACS DATABASE PRECHECK';
PRINT N' Database : ' + ISNULL(@CurrentDatabase, N'<NULL>');
PRINT N' Time     : ' + CONVERT(nvarchar(30), SYSDATETIME(), 121);
PRINT N'============================================================';

-----------------------------------------------------------------------
-- SAFETY GUARD
-----------------------------------------------------------------------

IF @CurrentDatabase IN (N'master', N'model', N'msdb', N'tempdb')
BEGIN
    RAISERROR(
        N'PACS PRECHECK: Không được chạy script trên system database.',
        16,
        1
    );

    RETURN;
END;

-----------------------------------------------------------------------
-- A. DATABASE INFORMATION
-----------------------------------------------------------------------

SELECT
    DB_NAME() AS CurrentDatabase,
    SUSER_SNAME() AS LoginName,
    @@SERVERNAME AS SqlServerName,
    SYSDATETIME() AS CheckedAt;

-----------------------------------------------------------------------
-- B. KIỂM TRA TABLE ResultCDHA
-----------------------------------------------------------------------

PRINT N'';
PRINT N'--- B. CHECK ResultCDHA -------------------------------------';

IF OBJECT_ID(N'dbo.ResultCDHA', N'U') IS NULL
BEGIN
    SELECT
        N'ERROR' AS Status,
        N'dbo.ResultCDHA' AS ObjectName,
        N'Không tìm thấy table dbo.ResultCDHA.' AS Message;
END
ELSE
BEGIN
    SELECT
        N'OK' AS Status,
        N'dbo.ResultCDHA' AS ObjectName,
        N'Table tồn tại.' AS Message;
END;

-----------------------------------------------------------------------
-- C. KIỂM TRA 4 COLUMN PACS TRONG ResultCDHA
-----------------------------------------------------------------------

PRINT N'';
PRINT N'--- C. CHECK ResultCDHA PACS COLUMNS ------------------------';

;WITH ExpectedColumns AS
(
    SELECT N'AccessionNumber' AS ColumnName
    UNION ALL SELECT N'StudyInstanceUID'
    UNION ALL SELECT N'OrthancStudyId'
    UNION ALL SELECT N'PacsStatus'
)
SELECT
    e.ColumnName AS ExpectedColumn,
    c.COLUMN_NAME AS ActualColumn,
    c.DATA_TYPE AS DataType,
    c.CHARACTER_MAXIMUM_LENGTH AS MaxLength,
    c.IS_NULLABLE AS IsNullable,
    CASE
        WHEN c.COLUMN_NAME IS NULL
            THEN N'NOT_EXISTS'
        ELSE N'EXISTS'
    END AS Status
FROM ExpectedColumns e
LEFT JOIN INFORMATION_SCHEMA.COLUMNS c
    ON c.TABLE_SCHEMA = N'dbo'
   AND c.TABLE_NAME = N'ResultCDHA'
   AND c.COLUMN_NAME = e.ColumnName
ORDER BY e.ColumnName;

-----------------------------------------------------------------------
-- D. KIỂM TRA 6 TABLE PACS
-----------------------------------------------------------------------

PRINT N'';
PRINT N'--- D. CHECK PACS TABLES ------------------------------------';

;WITH ExpectedTables AS
(
    SELECT N'PacsConfigs' AS TableName
    UNION ALL SELECT N'PacsStudies'
    UNION ALL SELECT N'PacsSeries'
    UNION ALL SELECT N'PacsInstances'
    UNION ALL SELECT N'PacsWorklistItems'
    UNION ALL SELECT N'PacsSyncStates'
)
SELECT
    e.TableName,
    CASE
        WHEN t.object_id IS NULL
            THEN N'NOT_EXISTS'
        ELSE N'EXISTS'
    END AS Status
FROM ExpectedTables e
LEFT JOIN sys.tables t
    ON t.name = e.TableName
   AND SCHEMA_NAME(t.schema_id) = N'dbo'
ORDER BY e.TableName;

-----------------------------------------------------------------------
-- E. HIỂN THỊ COLUMN CỦA CÁC TABLE PACS NẾU ĐÃ TỒN TẠI
-----------------------------------------------------------------------

PRINT N'';
PRINT N'--- E. EXISTING PACS TABLE SCHEMA ----------------------------';

SELECT
    c.TABLE_NAME,
    c.ORDINAL_POSITION,
    c.COLUMN_NAME,
    c.DATA_TYPE,
    c.CHARACTER_MAXIMUM_LENGTH,
    c.IS_NULLABLE,
    c.COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS c
WHERE c.TABLE_SCHEMA = N'dbo'
  AND c.TABLE_NAME IN
  (
      N'PacsConfigs',
      N'PacsStudies',
      N'PacsSeries',
      N'PacsInstances',
      N'PacsWorklistItems',
      N'PacsSyncStates'
  )
ORDER BY
    c.TABLE_NAME,
    c.ORDINAL_POSITION;

-----------------------------------------------------------------------
-- F. KIỂM TRA UNIQUE INDEX PacsSyncStates
-----------------------------------------------------------------------

PRINT N'';
PRINT N'--- F. CHECK PACS SYNC UNIQUE INDEX --------------------------';

SELECT
    t.name AS TableName,
    i.name AS IndexName,
    i.is_unique AS IsUnique,
    i.is_primary_key AS IsPrimaryKey,
    i.is_disabled AS IsDisabled
FROM sys.indexes i
INNER JOIN sys.tables t
    ON t.object_id = i.object_id
INNER JOIN sys.schemas s
    ON s.schema_id = t.schema_id
WHERE s.name = N'dbo'
  AND t.name = N'PacsSyncStates'
  AND i.name = N'UX_PacsSyncStates_PacsConfigId';

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes i
    INNER JOIN sys.tables t
        ON t.object_id = i.object_id
    INNER JOIN sys.schemas s
        ON s.schema_id = t.schema_id
    WHERE s.name = N'dbo'
      AND t.name = N'PacsSyncStates'
      AND i.name = N'UX_PacsSyncStates_PacsConfigId'
)
BEGIN
    SELECT
        N'NOT_EXISTS' AS Status,
        N'UX_PacsSyncStates_PacsConfigId' AS IndexName;
END;

-----------------------------------------------------------------------
-- G. KIỂM TRA DEFAULT CONSTRAINT PacsSyncStates NẾU TABLE TỒN TẠI
-----------------------------------------------------------------------

PRINT N'';
PRINT N'--- G. CHECK PACS SYNC DEFAULTS ------------------------------';

SELECT
    t.name AS TableName,
    c.name AS ColumnName,
    dc.name AS ConstraintName,
    dc.definition AS DefaultDefinition
FROM sys.default_constraints dc
INNER JOIN sys.columns c
    ON c.object_id = dc.parent_object_id
   AND c.column_id = dc.parent_column_id
INNER JOIN sys.tables t
    ON t.object_id = dc.parent_object_id
INNER JOIN sys.schemas s
    ON s.schema_id = t.schema_id
WHERE s.name = N'dbo'
  AND t.name = N'PacsSyncStates'
  AND c.name IN
  (
      N'LastChangeSeq',
      N'CreatedOn'
  )
ORDER BY c.name;

-----------------------------------------------------------------------
-- H. SUMMARY
-----------------------------------------------------------------------

PRINT N'';
PRINT N'--- H. PRECHECK SUMMARY -------------------------------------';

DECLARE @ResultCDHAPacsColumnCount int;

SELECT
    @ResultCDHAPacsColumnCount = COUNT(*)
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = N'dbo'
  AND TABLE_NAME = N'ResultCDHA'
  AND COLUMN_NAME IN
  (
      N'AccessionNumber',
      N'StudyInstanceUID',
      N'OrthancStudyId',
      N'PacsStatus'
  );

DECLARE @PacsTableCount int;

SELECT
    @PacsTableCount = COUNT(*)
FROM sys.tables t
INNER JOIN sys.schemas s
    ON s.schema_id = t.schema_id
WHERE s.name = N'dbo'
  AND t.name IN
  (
      N'PacsConfigs',
      N'PacsStudies',
      N'PacsSeries',
      N'PacsInstances',
      N'PacsWorklistItems',
      N'PacsSyncStates'
  );

DECLARE @SyncIndexCount int;

SELECT
    @SyncIndexCount = COUNT(*)
FROM sys.indexes i
INNER JOIN sys.tables t
    ON t.object_id = i.object_id
INNER JOIN sys.schemas s
    ON s.schema_id = t.schema_id
WHERE s.name = N'dbo'
  AND t.name = N'PacsSyncStates'
  AND i.name = N'UX_PacsSyncStates_PacsConfigId'
  AND i.is_unique = 1;

SELECT
    @CurrentDatabase AS CurrentDatabase,
    @ResultCDHAPacsColumnCount AS ResultCDHAPacsColumnCount,
    @PacsTableCount AS PacsTableCount,
    @SyncIndexCount AS SyncUniqueIndexCount,
    CASE
        WHEN @ResultCDHAPacsColumnCount = 0
         AND @PacsTableCount = 0
         AND @SyncIndexCount = 0
            THEN N'CLEAN_BASELINE'
        WHEN @ResultCDHAPacsColumnCount = 4
         AND @PacsTableCount = 6
         AND @SyncIndexCount = 1
            THEN N'PACS_ALREADY_PRESENT'
        ELSE N'PARTIAL_STATE_REVIEW_REQUIRED'
    END AS DatabaseState;

PRINT N'';
PRINT N'============================================================';
PRINT N' PACS PRECHECK FINISHED';
PRINT N'============================================================';