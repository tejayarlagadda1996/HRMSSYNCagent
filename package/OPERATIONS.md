# HRMS Attendance Sync Agent — Operations Guide

This document describes how the agent works end-to-end on a customer PC. Share **`uninstall-agent.ps1`** with field teams — it does not require the source code.

---

## What gets installed

| Item | Location | Purpose |
|------|----------|---------|
| Application | `C:\Program Files\HRMSAgent\HRMSAgent.exe` | Setup wizard + dashboard UI |
| Windows Service | `HRMSSyncService` | Background sync (`HRMSAgent.exe --service`) |
| Configuration | `C:\ProgramData\HRMSAgent\config.json` | Company, API, SQL settings |
| Sync checkpoint | `C:\ProgramData\HRMSAgent\syncstate.json` | Last synced record ID |
| Failed batches | `C:\ProgramData\HRMSAgent\pending.json` | Retries after API errors |
| Logs | `C:\ProgramData\HRMSAgent\Logs\` | Daily service logs |

---

## Complete operation flow

```
┌─────────────────┐     reads      ┌──────────────────────┐
│  eSSL SQL DB    │ ◄──────────────│  HRMSSyncService     │
│  etimetracklite1│                │  (background)        │
│  DeviceLogs_*   │                └──────────┬───────────┘
└─────────────────┘                           │
                                                │ POST (Bearer token)
                                                ▼
                                     ┌──────────────────────┐
                                     │  HRMS Cloud API      │
                                     │  (your full sync URL)│
                                     └──────────────────────┘

┌─────────────────┐
│  HRMSAgent.exe  │  ← UI only: setup, dashboard, pause/start service
│  (close window) │     Closing UI does NOT stop background sync
└─────────────────┘
```

### 1. First-time setup (wizard)

1. Run **`HRMSAgent.exe`** as Administrator (UAC prompt).
2. **Step 1** — Company name and company code (must match HRMS).
3. **Step 2** — **Full API sync URL** (including endpoint path) and API key.
   - Example: `https://api.myhrms.com/api/attendance/sync`
   - Local test: `http://localhost:3000/api/attendance/sync`
4. **Step 3** — SQL Server instance, database (`etimetracklite1`), Windows or SQL auth.
5. **Step 4** — Sync interval (seconds) and batch size.
6. **Step 5** — Finish: saves config, installs service, starts sync.

### 2. Background sync (automatic)

Every **sync interval** (default 30s), the service:

1. Reloads `config.json` (changes apply without restart).
2. Connects to SQL Server and reads `DeviceLogs_{month}_{year}`.
3. Fetches rows where `DeviceLogId > last checkpoint`.
4. Sends up to **batch size** records (default 100) to the configured **API sync URL**.
5. Updates checkpoint on success; stores failures in `pending.json` for retry.

### 3. Dashboard (after setup)

- **Service status** refreshes every 5 seconds.
- **Pause Service** — stops background sync (service still installed).
- **Start Service** — resumes sync.
- **Restart Service** — stop then start.
- **Install Service** — if service was removed but config remains.

Closing the dashboard window only exits the UI. The service keeps running.

### 4. API request format

The agent POSTs to the **exact URL** configured in the wizard:

```
POST {ApiUrl from config}
Authorization: Bearer {ApiKey}
Content-Type: application/json

{
  "companyCode": "123456",
  "logs": [
    {
      "deviceLogId": 1,
      "employeeCode": "EMP001",
      "deviceId": 6,
      "logDate": "2026-06-15T09:00:00",
      "direction": "in"
    }
  ]
}
```

Expected response: `{ "success": true, "acceptedCount": N }`

---

## Uninstall / full removal

### Option A — Standalone script (no source code needed)

1. Copy **`uninstall-agent.ps1`** to the customer PC.
2. Right-click → **Run with PowerShell** (or run in Admin PowerShell):

```powershell
powershell -ExecutionPolicy Bypass -File .\uninstall-agent.ps1
```

This will:

1. Stop and delete `HRMSSyncService`
2. Kill all `HRMSAgent.exe` processes
3. Delete `C:\ProgramData\HRMSAgent` (config, logs, sync state)

### Option B — Windows Settings (if installed via installer)

**Settings → Apps → HRMS Attendance Sync Agent → Uninstall**

The installer also stops the service, kills processes, and removes ProgramData.

### Option C — Manual commands (Administrator)

```powershell
sc.exe stop HRMSSyncService
sc.exe delete HRMSSyncService
taskkill /F /IM HRMSAgent.exe
Remove-Item "C:\ProgramData\HRMSAgent" -Recurse -Force
```

---

## Troubleshooting

| Issue | Action |
|-------|--------|
| Service won't install | Run `HRMSAgent.exe` as Administrator |
| SQL login failed for service | Grant `NT AUTHORITY\SYSTEM` read access on eSSL database |
| API unhealthy | Verify full sync URL includes path; test with **Test API Connection** |
| Files locked on uninstall | Run `uninstall-agent.ps1` as Administrator |
| Re-sync all data | Delete `syncstate.json` in ProgramData (service will resend from start) |
| Change API URL | Edit in dashboard; service picks up on next cycle |

---

## Files to distribute to customers

| File | Purpose |
|------|---------|
| `HRMSAgentSetup.exe` or `HRMSAgent.exe` | Install / run agent |
| `uninstall-agent.ps1` | Remove agent without source code |
| `OPERATIONS.md` | This guide |
