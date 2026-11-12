/* ============================================================
   PACS-XQ-03-04 – ACCESSION PRECHECK
   Read-only
   ============================================================ */

SET NOCOUNT ON;

PRINT '=== Existing PACS accession overview ===';

SELECT
    COUNT(*) AS TotalResultCDHA,
    SUM(CASE WHEN NULLIF(LTRIM(RTRIM(AccessionNumber)), '') IS NULL
             THEN 1 ELSE 0 END) AS EmptyAccession,
    SUM(CASE WHEN NULLIF(LTRIM(RTRIM(AccessionNumber)), '') IS NOT NULL
             THEN 1 ELSE 0 END) AS HasAccession
FROM dbo.ResultCDHA;

PRINT '=== Duplicate existing accessions ===';

SELECT
    LTRIM(RTRIM(AccessionNumber)) AS AccessionNumber,
    COUNT(*) AS DuplicateCount
FROM dbo.ResultCDHA
WHERE NULLIF(LTRIM(RTRIM(AccessionNumber)), '') IS NOT NULL
GROUP BY LTRIM(RTRIM(AccessionNumber))
HAVING COUNT(*) > 1
ORDER BY DuplicateCount DESC, AccessionNumber;

PRINT '=== Recent XQ candidates with empty accession ===';

SELECT TOP 20
    r.Id AS ResultCDHAId,
    r.PatientId,
    r.ServiceId,
    c.Code AS CategoryCode,
    r.AccessionNumber,
    r.Active,
    r.InsertTime
FROM dbo.ResultCDHA r
LEFT JOIN dbo.Service s
    ON s.Id = r.ServiceId
LEFT JOIN dbo.Category c
    ON c.Id = s.CategoryId
WHERE c.Code = 'XQ'
  AND NULLIF(LTRIM(RTRIM(r.AccessionNumber)), '') IS NULL
  AND NOT EXISTS
  (
      SELECT 1
      FROM dbo.PacsWorklistItems p
      WHERE p.ResultCDHAId = r.Id
  )
ORDER BY r.Id DESC;
