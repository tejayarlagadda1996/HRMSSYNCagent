-- Mock eSSL eTimeTrackLite database for HRMS Agent testing
-- Schema aligned with production DeviceLogs_* tables (EFICENS-27\ESSL).
-- Run in SQL Server Management Studio (SSMS).

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
IF OBJECT_ID(N'dbo.DeviceLogs_6_2026', N'U') IS NOT NULL
    DROP TABLE dbo.DeviceLogs_6_2026;
GO

CREATE TABLE dbo.DeviceLogs_6_2026
(
    DeviceLogId BIGINT IDENTITY(1, 1) NOT NULL PRIMARY KEY,
    DownloadDate DATETIME NOT NULL,
    DeviceId INT NOT NULL,
    UserId NVARCHAR(50) NOT NULL,
    LogDate DATETIME NOT NULL,
    Direction NVARCHAR(10) NOT NULL,
    AttDirection NVARCHAR(10) NULL,
    C1 NVARCHAR(10) NULL,
    C2 NVARCHAR(50) NULL,
    C3 NVARCHAR(50) NULL,
    C4 INT NULL,
    C5 INT NULL,
    C6 NVARCHAR(50) NULL,
    C7 NVARCHAR(50) NULL,
    WorkCode INT NOT NULL CONSTRAINT DF_DeviceLogs_6_2026_WorkCode DEFAULT (0),
    UpdateFlag INT NOT NULL CONSTRAINT DF_DeviceLogs_6_2026_UpdateFlag DEFAULT (0),
    EmployeeImage VARBINARY(MAX) NULL,
    FileName NVARCHAR(255) NULL,
    Longitude FLOAT NOT NULL CONSTRAINT DF_DeviceLogs_6_2026_Longitude DEFAULT (0),
    Latitude FLOAT NOT NULL CONSTRAINT DF_DeviceLogs_6_2026_Latitude DEFAULT (0),
    IsApproved INT NOT NULL CONSTRAINT DF_DeviceLogs_6_2026_IsApproved DEFAULT (-1),
    CreatedDate DATETIME NOT NULL
);
GO

-- Production-like sample rows (mirrors EFICENS-27\ESSL patterns)
INSERT INTO dbo.DeviceLogs_6_2026
    (DownloadDate, DeviceId, UserId, LogDate, Direction, AttDirection, C1, C2, C3, C4, C5, C6, C7, WorkCode, UpdateFlag, EmployeeImage, FileName, Longitude, Latitude, IsApproved, CreatedDate)
VALUES
    ('2026-06-06 09:11:17', 19, N'00011', '2026-06-06 09:11:08', N'in',  NULL, N'in',  NULL, NULL, 0, 1, NULL, NULL, 0, 0, NULL, NULL, 0, 0, -1, '2026-06-06 09:11:17'),
    ('2026-06-06 18:02:04', 19, N'00011', '2026-06-06 18:01:55', N'out', NULL, N'out', NULL, NULL, 1, 1, NULL, NULL, 0, 0, NULL, NULL, 0, 0, -1, '2026-06-06 18:02:04'),
    ('2026-06-08 09:05:12', 21, N'00012', '2026-06-08 09:05:03', N'in',  NULL, N'in',  NULL, NULL, 0, 1, NULL, NULL, 0, 0, NULL, NULL, 0, 0, -1, '2026-06-08 09:05:12'),
    ('2026-06-08 18:10:41', 21, N'00012', '2026-06-08 18:10:32', N'out', NULL, N'out', NULL, NULL, 1, 1, NULL, NULL, 0, 0, NULL, NULL, 0, 0, -1, '2026-06-08 18:10:41'),
    ('2026-06-17 09:00:55', 19, N'00011', '2026-06-17 09:00:46', N'in',  NULL, N'in',  NULL, NULL, 0, 1, NULL, NULL, 0, 0, NULL, NULL, 0, 0, -1, '2026-06-17 09:00:55');
GO

PRINT 'Mock database ready: etimetracklite1';
PRINT 'Table: DeviceLogs_6_2026 (production-aligned schema)';
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
