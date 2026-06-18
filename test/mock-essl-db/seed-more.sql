-- Add more test attendance records (run after setup.sql)
USE etimetracklite1;
GO

INSERT INTO dbo.DeviceLogs_6_2026 (DeviceId, UserId, LogDate, Direction)
VALUES
    (6, N'EMP004', GETDATE(), N'in'),
    (6, N'EMP005', GETDATE(), N'in'),
    (6, N'EMP005', DATEADD(MINUTE, 5, GETDATE()), N'out');
GO

SELECT TOP 20 * FROM dbo.DeviceLogs_6_2026 ORDER BY DeviceLogId DESC;
GO
