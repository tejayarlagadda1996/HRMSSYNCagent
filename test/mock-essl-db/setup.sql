-- Mock eSSL eTimeTrackLite database for HRMS Agent testing
-- Run in SQL Server Management Studio (SSMS) connected to your local instance.

USE master;
GO

IF DB_ID(N'etimetracklite1') IS NULL
BEGIN
    CREATE DATABASE etimetracklite1;
END
GO

USE etimetracklite1;
GO

-- eSSL uses monthly DeviceLogs tables: DeviceLogs_{month}_{year}
IF OBJECT_ID(N'dbo.DeviceLogs_6_2026', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DeviceLogs_6_2026
    (
        DeviceLogId BIGINT IDENTITY(1, 1) NOT NULL PRIMARY KEY,
        DeviceId INT NOT NULL,
        UserId NVARCHAR(50) NOT NULL,
        LogDate DATETIME NOT NULL,
        Direction NVARCHAR(10) NOT NULL
    );
END
GO

-- Sample attendance punches for testing
IF NOT EXISTS (SELECT 1 FROM dbo.DeviceLogs_6_2026)
BEGIN
    INSERT INTO dbo.DeviceLogs_6_2026 (DeviceId, UserId, LogDate, Direction)
    VALUES
        (6, N'EMP001', DATEADD(HOUR, -4, GETDATE()), N'in'),
        (6, N'EMP001', DATEADD(HOUR, -1, GETDATE()), N'out'),
        (6, N'EMP002', DATEADD(HOUR, -3, GETDATE()), N'in'),
        (6, N'EMP003', DATEADD(MINUTE, -90, GETDATE()), N'in'),
        (6, N'EMP003', DATEADD(MINUTE, -30, GETDATE()), N'out');
END
GO

PRINT 'Mock database ready: etimetracklite1';
PRINT 'Table: DeviceLogs_6_2026';
GO

-- Allow the HRMS background Windows service (runs as Local System) to read attendance data
USE master;
GO
IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'NT AUTHORITY\SYSTEM')
    CREATE LOGIN [NT AUTHORITY\SYSTEM] FROM WINDOWS;
GO
USE etimetracklite1;
GO
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'NT AUTHORITY\SYSTEM')
    CREATE USER [NT AUTHORITY\SYSTEM] FOR LOGIN [NT AUTHORITY\SYSTEM];
GO
ALTER ROLE db_datareader ADD MEMBER [NT AUTHORITY\SYSTEM];
GO
