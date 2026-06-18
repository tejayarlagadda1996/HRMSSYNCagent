# HRMS Attendance Sync Agent — Installation Guide

> Install using the single Windows installer. No `.bat` files or manual folder copying required.

---

## What You Receive

**`HRMSAgentSetup.exe`** — a single Windows installer that installs:

| Location | Contents |
|----------|----------|
| `C:\Program Files\HRMSAgent\` | `HRMSAgent.exe` only |
| `C:\ProgramData\HRMSAgent\` | `config.json`, sync state, logs (created during setup) |

`HRMSAgent.exe` handles both the setup wizard / dashboard UI and the background sync service.

---

## Before You Start

Have these ready:

- **Company Code** and **API Key** from HRMS admin
- **API URL** (e.g. `https://api.myhrms.com`)
- **SQL Server instance** (e.g. `localhost\ESSL`)
- **Database name** (usually `etimetracklite1`)
- SQL **username/password** (or Windows Authentication)

---

## Install in 2 Steps

### 1. Run the Installer

Right-click **`HRMSAgentSetup.exe`** → **Run as administrator**

Follow the wizard. This installs `HRMSAgent.exe` and creates the data folder at `C:\ProgramData\HRMSAgent`.

### 2. Complete Setup Wizard

Open **HRMS Attendance Sync Agent** and complete all 5 steps:

1. **Company** — name and code
2. **API** — URL, key, test connection
3. **SQL** — detect instance, database, test connection
4. **Sync** — interval (30s) and batch size (100)
5. **Finish** — saves config and starts the background sync service

Configuration is saved to `C:\ProgramData\HRMSAgent\config.json`.

---

## Verify It Works

On the dashboard, confirm:

- Service Status: **Running** (green)
- SQL Status: **Healthy**
- API Status: **Healthy** (after first sync)

Logs: `C:\ProgramData\HRMSAgent\Logs\`

---

## Local Testing

See the `test/` folder:

- `test/mock-hrms-api/` — dummy Node.js HRMS API
- `test/mock-essl-db/` — dummy SQL Server database for SSMS

---

## Troubleshooting

| Problem | Fix |
|---------|-----|
| SQL test fails | Check instance name, database, credentials; ensure SQL Server is running |
| API test fails | Verify URL, API key, and firewall |
| Service stopped | Click **Start Service** on dashboard |
| Re-run wizard | Delete `C:\ProgramData\HRMSAgent\config.json` and reopen the app |

---

## Uninstall

Use **Add or Remove Programs** → HRMS Attendance Sync Agent.

This removes the application and stops the background service. Your config and logs in `C:\ProgramData\HRMSAgent` are kept unless you delete them manually.
