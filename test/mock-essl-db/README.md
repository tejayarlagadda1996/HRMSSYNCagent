# Mock eSSL SQL Server Database

Dummy SQL Server database that mimics **eSSL eTimeTrackLite** attendance tables for local testing with SSMS.

## Prerequisites

- SQL Server (Express, Developer, or full edition)
- SQL Server Management Studio (SSMS)

## Setup in SSMS

1. Open SSMS and connect to your SQL Server instance (e.g. `localhost` or `localhost\SQLEXPRESS`).
2. Open `setup.sql` from this folder.
3. Execute the script (F5).

This creates:

- Database: `etimetracklite1`
- Table: `DeviceLogs_6_2026` (current month format used by eSSL)
- 5 sample attendance records

## Agent wizard settings

| Field | Typical value |
|-------|----------------|
| SQL Server | `localhost` or `localhost\SQLEXPRESS` |
| Database | `etimetracklite1` |
| Authentication | Windows Authentication (leave username/password blank) |

## Add more test data

Run `seed-more.sql` in SSMS to insert additional punches.

## Verify

```sql
USE etimetracklite1;
SELECT * FROM dbo.DeviceLogs_6_2026 ORDER BY DeviceLogId;
```

## Full local test flow

1. Run `test\mock-essl-db\setup.sql` in SSMS
2. Start mock API: `cd test\mock-hrms-api && npm install && npm start`
3. Install and run `HRMSAgent.exe` (or build with `.\scripts\publish.ps1`)
4. Complete setup wizard with test credentials from `test\mock-hrms-api\README.md`
5. Check mock API console for received attendance batches
