# HRMS Attendance Sync Agent — Installation Guide

> **For installing on Windows.** No code or .NET required on the target PC.

---

## What You Receive

A single folder named **`HRMSAgent`** containing:

| File | Purpose |
|------|---------|
| `HRMSSyncManager.exe` | Setup wizard & monitoring dashboard |
| `HRMSSyncService.exe` | Background sync service |
| `install.bat` | One-click install (run as Administrator) |
| `INSTALLATION.md` | This guide |
| Other files | Required runtime — **do not delete** |

Alternatively you may receive **`HRMSAgentSetup.exe`** — a Windows installer that installs the same files.

---

## Before You Start

Have these ready:

- **Company Code** and **API Key** from HRMS admin
- **API URL** (e.g. `https://api.myhrms.com`)
- **SQL Server instance** (e.g. `localhost\ESSL`)
- **Database name** (usually `etimetracklite1`)
- SQL **username/password** (or Windows Authentication)

---

## Install in 3 Steps

### 1. Copy to Windows

Copy the entire **`HRMSAgent`** folder to the Windows server (USB, zip, or network share).  
Unzip if needed. Keep all files in the same folder.

### 2. Run Installer

**Right-click** `install.bat` → **Run as administrator**

This copies files to `C:\Program Files\HRMSAgent` and creates a desktop shortcut.

*Skip this step if you received `HRMSAgentSetup.exe` — run that as Administrator instead.*

### 3. Complete Setup Wizard

Open **HRMS Attendance Sync Manager** and complete all 5 steps:

1. **Company** — name and code  
2. **API** — URL, key, test connection  
3. **SQL** — detect instance, database, test connection  
4. **Sync** — interval (30s) and batch size (100)  
5. **Finish** — installs and starts the Windows service  

---

## Verify It Works

On the dashboard, confirm:

- Service Status: **Running** (green)
- SQL Status: **Healthy**
- API Status: **Healthy** (after first sync)

Logs: `C:\ProgramData\HRMSAgent\Logs\`

---

## Troubleshooting

| Problem | Fix |
|---------|-----|
| SQL test fails | Check instance name, database, credentials; ensure SQL Server is running |
| API test fails | Verify URL, API key, and firewall |
| Service stopped | Click **Start Service** on dashboard |
| Re-run wizard | Delete `C:\ProgramData\HRMSAgent\config.json` and reopen manager |

---

## Full Guide

See **[package/INSTALLATION.md](package/INSTALLATION.md)** for detailed troubleshooting, uninstall steps, and file locations.
