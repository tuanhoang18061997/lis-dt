/* =====================================================================
 PACS-XQ-04-02-01-PRECHECK.sql
 Project A - Shared Study / Series / Instance Sync
 Purpose:
   - Validate data before narrowing PACS identity columns to nvarchar(64)
   - Validate duplicate identities before unique indexes are created
   - READ ONLY
 ===================================================================== */

SET NOCOUNT ON;

DECLARE @CurrentDatabase sysname = DB_NAME();

PRINT N'============================================================';
PRINT N' PACS-XQ-04-02 PRECHECK';
PRINT N' Database : ' + ISNULL(@CurrentDatabase, N'<NULL>');
PRINT N'============================================================';

IF @CurrentDatabase IN (N'master', N'model', N'msdb', N'tempdb')
BEGIN
    THROW 51000, N'Không chạy PACS-XQ-04-02 trên system database.', 1;
END;

DECLARE @Required TABLE (TableName sysname);
INSERT INTO @Required(TableName)
VALUES
(N'ResultCDHA'),
(N'PacsWorklistItems'),
(N'PacsStudies'),
(N'PacsSeries'),
(N'PacsInstances'),
(N'PacsSyncStates');

IF EXISTS
(
    SELECT 1
    FROM @Required r
    WHERE OBJECT_ID(N'dbo.' + r.TableName, N'U') IS NULL
)
BEGIN
    SELECT r.TableName AS MissingTable
    FROM @Required r
    WHERE OBJECT_ID(N'dbo.' + r.TableName, N'U') IS NULL;

    THROW 51001, N'Thiếu table bắt buộc cho PACS-XQ-04-02.', 1;
END;

PRINT N'--- A. ROW COUNTS -------------------------------------------';

SELECT N'ResultCDHA' AS TableName, COUNT_BIG(*) AS TotalRows FROM dbo.ResultCDHA
UNION ALL SELECT N'PacsWorklistItems', COUNT_BIG(*) FROM dbo.PacsWorklistItems
UNION ALL SELECT N'PacsStudies', COUNT_BIG(*) FROM dbo.PacsStudies
UNION ALL SELECT N'PacsSeries', COUNT_BIG(*) FROM dbo.PacsSeries
UNION ALL SELECT N'PacsInstances', COUNT_BIG(*) FROM dbo.PacsInstances
UNION ALL SELECT N'PacsSyncStates', COUNT_BIG(*) FROM dbo.PacsSyncStates;

PRINT N'--- B. MAX IDENTITY LENGTHS ---------------------------------';

SELECT
    N'ResultCDHA' AS TableName,
    MAX(LEN(AccessionNumber)) AS MaxAccessionNumber,
    MAX(LEN(StudyInstanceUID)) AS MaxStudyInstanceUID,
    MAX(LEN(OrthancStudyId)) AS MaxOrthancStudyId
FROM dbo.ResultCDHA;

SELECT
    N'PacsWorklistItems' AS TableName,
    MAX(LEN(AccessionNumber)) AS MaxAccessionNumber,
    MAX(LEN(StudyInstanceUID)) AS MaxStudyInstanceUID,
    MAX(LEN(OrthancStudyId)) AS MaxOrthancStudyId
FROM dbo.PacsWorklistItems;

SELECT
    N'PacsStudies' AS TableName,
    MAX(LEN(AccessionNumber)) AS MaxAccessionNumber,
    MAX(LEN(StudyInstanceUID)) AS MaxStudyInstanceUID,
    MAX(LEN(OrthancStudyId)) AS MaxOrthancStudyId
FROM dbo.PacsStudies;

SELECT
    N'PacsSeries' AS TableName,
    MAX(LEN(OrthancSeriesId)) AS MaxOrthancSeriesId,
    MAX(LEN(OrthancParentStudyId)) AS MaxOrthancParentStudyId,
    MAX(LEN(SeriesInstanceUID)) AS MaxSeriesInstanceUID
FROM dbo.PacsSeries;

SELECT
    N'PacsInstances' AS TableName,
    MAX(LEN(OrthancInstanceId)) AS MaxOrthancInstanceId,
    MAX(LEN(OrthancParentSeriesId)) AS MaxOrthancParentSeriesId,
    MAX(LEN(SOPInstanceUID)) AS MaxSOPInstanceUID,
    MAX(LEN(SopClassUID)) AS MaxSopClassUID,
    MAX(LEN(TransferSyntaxUID)) AS MaxTransferSyntaxUID
FROM dbo.PacsInstances;

PRINT N'--- C. VALUES LONGER THAN 64 --------------------------------';

SELECT N'ResultCDHA.AccessionNumber' AS FieldName, COUNT_BIG(*) AS TooLong
FROM dbo.ResultCDHA WHERE LEN(AccessionNumber) > 64
UNION ALL
SELECT N'ResultCDHA.StudyInstanceUID', COUNT_BIG(*)
FROM dbo.ResultCDHA WHERE LEN(StudyInstanceUID) > 64
UNION ALL
SELECT N'ResultCDHA.OrthancStudyId', COUNT_BIG(*)
FROM dbo.ResultCDHA WHERE LEN(OrthancStudyId) > 64
UNION ALL
SELECT N'PacsWorklistItems.AccessionNumber', COUNT_BIG(*)
FROM dbo.PacsWorklistItems WHERE LEN(AccessionNumber) > 64
UNION ALL
SELECT N'PacsWorklistItems.StudyInstanceUID', COUNT_BIG(*)
FROM dbo.PacsWorklistItems WHERE LEN(StudyInstanceUID) > 64
UNION ALL
SELECT N'PacsWorklistItems.OrthancStudyId', COUNT_BIG(*)
FROM dbo.PacsWorklistItems WHERE LEN(OrthancStudyId) > 64
UNION ALL
SELECT N'PacsStudies.AccessionNumber', COUNT_BIG(*)
FROM dbo.PacsStudies WHERE LEN(AccessionNumber) > 64
UNION ALL
SELECT N'PacsStudies.StudyInstanceUID', COUNT_BIG(*)
FROM dbo.PacsStudies WHERE LEN(StudyInstanceUID) > 64
UNION ALL
SELECT N'PacsStudies.OrthancStudyId', COUNT_BIG(*)
FROM dbo.PacsStudies WHERE LEN(OrthancStudyId) > 64
UNION ALL
SELECT N'PacsSeries.OrthancSeriesId', COUNT_BIG(*)
FROM dbo.PacsSeries WHERE LEN(OrthancSeriesId) > 64
UNION ALL
SELECT N'PacsSeries.OrthancParentStudyId', COUNT_BIG(*)
FROM dbo.PacsSeries WHERE LEN(OrthancParentStudyId) > 64
UNION ALL
SELECT N'PacsSeries.SeriesInstanceUID', COUNT_BIG(*)
FROM dbo.PacsSeries WHERE LEN(SeriesInstanceUID) > 64
UNION ALL
SELECT N'PacsInstances.OrthancInstanceId', COUNT_BIG(*)
FROM dbo.PacsInstances WHERE LEN(OrthancInstanceId) > 64
UNION ALL
SELECT N'PacsInstances.OrthancParentSeriesId', COUNT_BIG(*)
FROM dbo.PacsInstances WHERE LEN(OrthancParentSeriesId) > 64
UNION ALL
SELECT N'PacsInstances.SOPInstanceUID', COUNT_BIG(*)
FROM dbo.PacsInstances WHERE LEN(SOPInstanceUID) > 64
UNION ALL
SELECT N'PacsInstances.SopClassUID', COUNT_BIG(*)
FROM dbo.PacsInstances WHERE LEN(SopClassUID) > 64
UNION ALL
SELECT N'PacsInstances.TransferSyntaxUID', COUNT_BIG(*)
FROM dbo.PacsInstances WHERE LEN(TransferSyntaxUID) > 64;

PRINT N'--- D. DUPLICATE WORKLIST IDENTITY --------------------------';

SELECT ResultCDHAId, COUNT(*) AS Total
FROM dbo.PacsWorklistItems
GROUP BY ResultCDHAId
HAVING COUNT(*) > 1;

SELECT AccessionNumber, COUNT(*) AS Total
FROM dbo.PacsWorklistItems
WHERE NULLIF(LTRIM(RTRIM(AccessionNumber)), N'') IS NOT NULL
GROUP BY AccessionNumber
HAVING COUNT(*) > 1;

PRINT N'--- E. DUPLICATE STUDY IDENTITY -----------------------------';

SELECT StudyInstanceUID, COUNT(*) AS Total
FROM dbo.PacsStudies
WHERE NULLIF(LTRIM(RTRIM(StudyInstanceUID)), N'') IS NOT NULL
GROUP BY StudyInstanceUID
HAVING COUNT(*) > 1;

SELECT OrthancStudyId, COUNT(*) AS Total
FROM dbo.PacsStudies
WHERE NULLIF(LTRIM(RTRIM(OrthancStudyId)), N'') IS NOT NULL
GROUP BY OrthancStudyId
HAVING COUNT(*) > 1;

PRINT N'--- F. DUPLICATE SERIES IDENTITY ----------------------------';

SELECT OrthancSeriesId, COUNT(*) AS Total
FROM dbo.PacsSeries
WHERE NULLIF(LTRIM(RTRIM(OrthancSeriesId)), N'') IS NOT NULL
GROUP BY OrthancSeriesId
HAVING COUNT(*) > 1;

SELECT SeriesInstanceUID, COUNT(*) AS Total
FROM dbo.PacsSeries
WHERE NULLIF(LTRIM(RTRIM(SeriesInstanceUID)), N'') IS NOT NULL
GROUP BY SeriesInstanceUID
HAVING COUNT(*) > 1;

PRINT N'--- G. DUPLICATE INSTANCE IDENTITY --------------------------';

SELECT OrthancInstanceId, COUNT(*) AS Total
FROM dbo.PacsInstances
WHERE NULLIF(LTRIM(RTRIM(OrthancInstanceId)), N'') IS NOT NULL
GROUP BY OrthancInstanceId
HAVING COUNT(*) > 1;

SELECT SOPInstanceUID, COUNT(*) AS Total
FROM dbo.PacsInstances
WHERE NULLIF(LTRIM(RTRIM(SOPInstanceUID)), N'') IS NOT NULL
GROUP BY SOPInstanceUID
HAVING COUNT(*) > 1;

PRINT N'--- H. CURRENT COLUMN TYPES ---------------------------------';

SELECT
    TABLE_NAME,
    COLUMN_NAME,
    DATA_TYPE,
    CHARACTER_MAXIMUM_LENGTH,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = N'dbo'
  AND
  (
      (TABLE_NAME = N'ResultCDHA'
       AND COLUMN_NAME IN (N'AccessionNumber', N'StudyInstanceUID', N'OrthancStudyId'))
      OR
      (TABLE_NAME = N'PacsWorklistItems'
       AND COLUMN_NAME IN (N'AccessionNumber', N'StudyInstanceUID', N'OrthancStudyId'))
      OR
      (TABLE_NAME = N'PacsStudies'
       AND COLUMN_NAME IN (N'AccessionNumber', N'StudyInstanceUID', N'OrthancStudyId'))
      OR
      (TABLE_NAME = N'PacsSeries'
       AND COLUMN_NAME IN (N'OrthancSeriesId', N'OrthancParentStudyId', N'SeriesInstanceUID'))
      OR
      (TABLE_NAME = N'PacsInstances'
       AND COLUMN_NAME IN
       (
           N'OrthancInstanceId',
           N'OrthancParentSeriesId',
           N'SOPInstanceUID',
           N'SopClassUID',
           N'TransferSyntaxUID'
       ))
  )
ORDER BY TABLE_NAME, COLUMN_NAME;

PRINT N'============================================================';
PRINT N' PRECHECK COMPLETE';
PRINT N' All TooLong values must be 0.';
PRINT N' All duplicate result sets must return 0 rows.';
PRINT N'============================================================';
