-- Bulk seed ~2001 production-like attendance logs for sync testing
USE etimetracklite1;
GO

DECLARE @TargetCount INT = 2001;
DECLARE @i INT = 1;

WHILE @i <= @TargetCount
BEGIN
    DECLARE @LogDate DATETIME = DATEADD(MINUTE, -@i, GETDATE());
    DECLARE @Direction NVARCHAR(10) = CASE WHEN @i % 2 = 0 THEN N'out' ELSE N'in' END;
    DECLARE @DeviceId INT = CASE WHEN @i % 2 = 0 THEN 21 ELSE 19 END;
    DECLARE @UserId NVARCHAR(50) = CASE WHEN @i % 2 = 0 THEN N'00012' ELSE N'00011' END;
    DECLARE @C4 INT = CASE WHEN @Direction = N'out' THEN 1 ELSE 0 END;

    INSERT INTO dbo.DeviceLogs_6_2026
        (DownloadDate, DeviceId, UserId, LogDate, Direction, AttDirection, C1, C2, C3, C4, C5, C6, C7, WorkCode, UpdateFlag, EmployeeImage, FileName, Longitude, Latitude, IsApproved, CreatedDate)
    VALUES
        (DATEADD(SECOND, 9, @LogDate), @DeviceId, @UserId, @LogDate, @Direction, NULL, @Direction, NULL, NULL, @C4, 1, NULL, NULL, 0, 0, NULL, NULL, 0, 0, -1, DATEADD(SECOND, 9, @LogDate));

    SET @i += 1;
END
GO

SELECT COUNT(*) AS TotalRows FROM dbo.DeviceLogs_6_2026;
SELECT TOP 5 DeviceLogId, UserId, DeviceId, LogDate, Direction FROM dbo.DeviceLogs_6_2026 ORDER BY DeviceLogId DESC;
GO
