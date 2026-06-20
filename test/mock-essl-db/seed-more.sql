-- Add more production-like test attendance records (run after setup.sql)
USE etimetracklite1;
GO

INSERT INTO dbo.DeviceLogs_6_2026
    (DownloadDate, DeviceId, UserId, LogDate, Direction, AttDirection, C1, C2, C3, C4, C5, C6, C7, WorkCode, UpdateFlag, EmployeeImage, FileName, Longitude, Latitude, IsApproved, CreatedDate)
VALUES
    (GETDATE(), 19, N'00011', DATEADD(SECOND, -9, GETDATE()), N'in',  NULL, N'in',  NULL, NULL, 0, 1, NULL, NULL, 0, 0, NULL, NULL, 0, 0, -1, GETDATE()),
    (GETDATE(), 21, N'00012', DATEADD(SECOND, -9, GETDATE()), N'in',  NULL, N'in',  NULL, NULL, 0, 1, NULL, NULL, 0, 0, NULL, NULL, 0, 0, -1, GETDATE()),
    (DATEADD(MINUTE, 5, GETDATE()), 21, N'00012', DATEADD(MINUTE, 5, DATEADD(SECOND, -9, GETDATE())), N'out', NULL, N'out', NULL, NULL, 1, 1, NULL, NULL, 0, 0, NULL, NULL, 0, 0, -1, DATEADD(MINUTE, 5, GETDATE()));
GO

SELECT TOP 20 * FROM dbo.DeviceLogs_6_2026 ORDER BY DeviceLogId DESC;
GO
