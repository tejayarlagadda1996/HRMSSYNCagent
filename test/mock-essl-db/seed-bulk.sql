-- Bulk seed ~2001 attendance logs for sync testing
USE etimetracklite1;
GO

DECLARE @StartId BIGINT = (SELECT ISNULL(MAX(DeviceLogId), 0) FROM dbo.DeviceLogs_6_2026);
DECLARE @TargetCount INT = 2001;
DECLARE @i INT = 1;

WHILE @i <= @TargetCount
BEGIN
    INSERT INTO dbo.DeviceLogs_6_2026 (DeviceId, UserId, LogDate, Direction)
    VALUES (
        6 + (@i % 3),
        CONCAT(N'EMP', RIGHT(CONCAT(N'000', (@i % 250) + 1), 3)),
        DATEADD(MINUTE, -@i, GETDATE()),
        CASE WHEN @i % 2 = 0 THEN N'out' ELSE N'in' END
    );

    SET @i += 1;
END
GO

SELECT COUNT(*) AS TotalRows FROM dbo.DeviceLogs_6_2026;
SELECT TOP 5 DeviceLogId, UserId, LogDate, Direction FROM dbo.DeviceLogs_6_2026 ORDER BY DeviceLogId DESC;
GO
