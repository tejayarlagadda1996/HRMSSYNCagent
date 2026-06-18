# Mock HRMS API

Dummy Node.js backend for testing the HRMS Attendance Sync Agent locally.

## Setup

```powershell
cd test\mock-hrms-api
npm install
npm start
```

Server runs at **http://localhost:3000**

## Test credentials (use in the agent setup wizard)

| Field | Value |
|-------|-------|
| API Sync URL | `http://localhost:3000/api/attendance/sync` |
| API Key | `test-api-key-123` |
| Company Code | `TESTCO` |

## Endpoints

| Method | Path | Description |
|--------|------|-------------|
| GET | `/health` | Health check |
| POST | `/api/attendance/sync` | Receives attendance batches (Bearer auth) |
| GET | `/api/attendance/received` | View all received batches (Bearer auth) |

## Example request

```powershell
curl -X POST http://localhost:3000/api/attendance/sync `
  -H "Authorization: Bearer test-api-key-123" `
  -H "Content-Type: application/json" `
  -d '{"companyCode":"TESTCO","logs":[{"deviceLogId":1,"employeeCode":"EMP001","deviceId":6,"logDate":"2026-06-15T09:00:00","direction":"in"}]}'
```
