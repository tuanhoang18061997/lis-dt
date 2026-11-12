/* =====================================================================
 PACS-XQ-04-02-03-VERIFY.sql
 READ ONLY verification after migration.
 ===================================================================== */

SET NOCOUNT ON;

PRINT N'--- A. COLUMN TYPES -----------------------------------------';

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

PRINT N'--- B. EXPECTED INDEXES -------------------------------------';

;WITH Expected AS
(
    SELECT N'ResultCDHA' AS TableName, N'IX_ResultCDHA_AccessionNumber' AS IndexName, 0 AS MustBeUnique
    UNION ALL SELECT N'PacsWorklistItems', N'UX_PacsWorklistItems_ResultCDHAId', 1
    UNION ALL SELECT N'PacsWorklistItems', N'UX_PacsWorklistItems_AccessionNumber', 1
    UNION ALL SELECT N'PacsStudies', N'IX_PacsStudies_ResultCDHAId', 0
    UNION ALL SELECT N'PacsStudies', N'IX_PacsStudies_AccessionNumber', 0
    UNION ALL SELECT N'PacsStudies', N'UX_PacsStudies_StudyInstanceUID', 1
    UNION ALL SELECT N'PacsStudies', N'UX_PacsStudies_OrthancStudyId', 1
    UNION ALL SELECT N'PacsSeries', N'IX_PacsSeries_PacsStudyId', 0
    UNION ALL SELECT N'PacsSeries', N'UX_PacsSeries_OrthancSeriesId', 1
    UNION ALL SELECT N'PacsSeries', N'UX_PacsSeries_SeriesInstanceUID', 1
    UNION ALL SELECT N'PacsInstances', N'IX_PacsInstances_PacsStudyId', 0
    UNION ALL SELECT N'PacsInstances', N'IX_PacsInstances_PacsSeriesId', 0
    UNION ALL SELECT N'PacsInstances', N'UX_PacsInstances_OrthancInstanceId', 1
    UNION ALL SELECT N'PacsInstances', N'UX_PacsInstances_SOPInstanceUID', 1
)
SELECT
    e.TableName,
    e.IndexName,
    e.MustBeUnique,
    CASE WHEN i.index_id IS NULL THEN 0 ELSE 1 END AS ExistsFlag,
    i.is_unique AS IsUnique,
    i.filter_definition AS FilterDefinition,
    CASE
        WHEN i.index_id IS NULL THEN N'FAIL_MISSING'
        WHEN e.MustBeUnique = 1 AND i.is_unique <> 1 THEN N'FAIL_NOT_UNIQUE'
        ELSE N'PASS'
    END AS Result
FROM Expected e
LEFT JOIN sys.tables t
    ON t.name = e.TableName
   AND SCHEMA_NAME(t.schema_id) = N'dbo'
LEFT JOIN sys.indexes i
    ON i.object_id = t.object_id
   AND i.name = e.IndexName
ORDER BY e.TableName, e.IndexName;

PRINT N'--- C. DUPLICATE RECHECK ------------------------------------';

SELECT N'Worklist.ResultCDHAId' AS IdentityName, COUNT(*) AS DuplicateGroups
FROM
(
    SELECT ResultCDHAId
    FROM dbo.PacsWorklistItems
    GROUP BY ResultCDHAId
    HAVING COUNT(*) > 1
) x

UNION ALL

SELECT N'Worklist.AccessionNumber', COUNT(*)
FROM
(
    SELECT AccessionNumber
    FROM dbo.PacsWorklistItems
    GROUP BY AccessionNumber
    HAVING COUNT(*) > 1
) x

UNION ALL

SELECT N'Study.StudyInstanceUID', COUNT(*)
FROM
(
    SELECT StudyInstanceUID
    FROM dbo.PacsStudies
    GROUP BY StudyInstanceUID
    HAVING COUNT(*) > 1
) x

UNION ALL

SELECT N'Study.OrthancStudyId', COUNT(*)
FROM
(
    SELECT OrthancStudyId
    FROM dbo.PacsStudies
    GROUP BY OrthancStudyId
    HAVING COUNT(*) > 1
) x

UNION ALL

SELECT N'Series.OrthancSeriesId', COUNT(*)
FROM
(
    SELECT OrthancSeriesId
    FROM dbo.PacsSeries
    GROUP BY OrthancSeriesId
    HAVING COUNT(*) > 1
) x

UNION ALL

SELECT N'Series.SeriesInstanceUID', COUNT(*)
FROM
(
    SELECT SeriesInstanceUID
    FROM dbo.PacsSeries
    WHERE SeriesInstanceUID IS NOT NULL
    GROUP BY SeriesInstanceUID
    HAVING COUNT(*) > 1
) x

UNION ALL

SELECT N'Instance.OrthancInstanceId', COUNT(*)
FROM
(
    SELECT OrthancInstanceId
    FROM dbo.PacsInstances
    GROUP BY OrthancInstanceId
    HAVING COUNT(*) > 1
) x

UNION ALL

SELECT N'Instance.SOPInstanceUID', COUNT(*)
FROM
(
    SELECT SOPInstanceUID
    FROM dbo.PacsInstances
    GROUP BY SOPInstanceUID
    HAVING COUNT(*) > 1
) x;

PRINT N'--- D. PHASE 03 WORKLIST REGRESSION -------------------------';

SELECT
    COUNT(*) AS WorklistRows,
    SUM(CASE WHEN Status = 1 THEN 1 ELSE 0 END) AS PublishedRows,
    SUM(CASE WHEN ErrorMessage IS NOT NULL AND LTRIM(RTRIM(ErrorMessage)) <> N'' THEN 1 ELSE 0 END) AS ErrorRows
FROM dbo.PacsWorklistItems;

SELECT
    r.Id AS ResultCDHAId,
    r.AccessionNumber AS ResultAccession,
    w.Id AS WorklistId,
    w.AccessionNumber AS WorklistAccession
FROM dbo.ResultCDHA r
INNER JOIN dbo.PacsWorklistItems w
    ON w.ResultCDHAId = r.Id
WHERE ISNULL(LTRIM(RTRIM(r.AccessionNumber)), N'')
   <> ISNULL(LTRIM(RTRIM(w.AccessionNumber)), N'');

PRINT N'============================================================';
PRINT N' VERIFY COMPLETE';
PRINT N' Expected: all index Result=PASS, all DuplicateGroups=0,';
PRINT N' and ResultCDHA/Worklist mismatch query returns 0 rows.';
PRINT N'============================================================';
