# HRMS Attendance Sync Agent — Installation Guide

Use this guide to install the agent on a **Windows PC** that has eSSL eTimeTrackLite (SQL Server).  
No programming or .NET installation is required on the target machine.

---

## What You Need

Before starting, collect:

| Item | Example |
|------|---------|
| Company Name | Eficens Technologies |
| Company Code | EFICENS |
| HRMS API Sync URL | `https://api.myhrms.com/api/attendance/sync` |
| API Key | Provided by HRMS admin |
| SQL Server instance | `localhost\ESSL` |
| Database name | `etimetracklite1` |
| SQL username / password | Or use Windows Authentication |

**System requirements**

- Windows 10, Windows 11, or Windows Server 2016+
- 64-bit (x64)
- Administrator access for installation
- Internet access to the HRMS API

---

## Step 1 — Copy the Folder to Windows

You will receive a single folder named **`HRMSAgent`**. It contains everything needed:

```
HRMSAgent/
├── HRMSSyncManager.exe     ← Configuration & monitoring app
├── HRMSSyncService.exe     ← Background sync service
├── install.bat             ← Optional quick installer
├── INSTALLATION.md         ← This guide
└── (support files)         ← DLLs and runtime — do not delete
```

1. Copy the entire **`HRMSAgent`** folder to the Windows server (USB drive, network share, or zip file).
2. Unzip if needed. Keep all files together in the same folder.

> **Important:** Do not move or delete the DLL files. The application needs them to run.

---

## Step 2 — Install on Windows

Choose **one** method below.

### Method A — Quick Install (Recommended)

1. Open the **`HRMSAgent`** folder on the Windows PC.
2. **Right-click** `install.bat` → **Run as administrator**.
3. Click through the prompts. Files are copied to `C:\Program Files\HRMSAgent`.
4. A desktop shortcut **HRMS Attendance Sync Manager** is created.
5. The manager app opens automatically.

### Method B — Run Directly (No Copy)

1. Open the **`HRMSAgent`** folder.
2. Double-click **`HRMSSyncManager.exe`**.
3. If Windows SmartScreen appears, click **More info** → **Run anyway**.
4. For production use, prefer Method A so files are in a fixed location.

### Method C — Setup Installer (.exe)

If you received **`HRMSAgentSetup.exe`** instead of a folder:

1. Right-click **`HRMSAgentSetup.exe`** → **Run as administrator**.
2. Follow the installer wizard.
3. Launch **HRMS Attendance Sync Manager** from the Start Menu when finished.

---

## Step 3 — First-Time Setup Wizard

When **HRMSSyncManager** opens for the first time, a 5-step wizard appears.

### Step 1 — Company Information

- Enter **Company Name**
- Enter **Company Code** (must match HRMS)
- Click **Next**

### Step 2 — Cloud API

- Enter **API Sync URL** — full URL including endpoint (e.g. `https://api.myhrms.com/api/attendance/sync`)
- Enter **API Key**
- Click **Test API Connection** — should show success
- Click **Next**

### Step 3 — SQL Server

- Click **Detect SQL Instances** to find installed SQL Server
- Select or type the instance (e.g. `localhost\ESSL`)
- Enter **Database Name** (usually `etimetracklite1`)
- Enter SQL **Username** and **Password** (leave username empty for Windows Auth)
- Click **Test SQL Connection** — should show success
- Click **Next**

### Step 4 — Sync Settings

| Setting | Default | Description |
|---------|---------|-------------|
| Sync Interval | 30 seconds | How often to check for new attendance |
| Batch Size | 100 | Records sent per sync cycle |

- Click **Next**

### Step 5 — Finish

- Review the summary
- Click **Finish & Install**
- The wizard will:
  - Save configuration
  - Install the **HRMSSyncService** Windows service
  - Start the service automatically

---

## Step 4 — Verify Installation

After setup, the **Dashboard** opens automatically.

Check that:

| Indicator | Expected |
|-----------|----------|
| Service Status | **Running** (green) |
| SQL Status | **Healthy** (green) |
| API Status | **Healthy** (green) after first sync |
| Pending Records | **0** (after successful sync) |

### Open logs

Click **Open Logs Folder** on the dashboard, or go to:

```
C:\ProgramData\HRMSAgent\Logs\
```

Look for messages like:

```
Sync successful. Checkpoint updated to DeviceLogId ...
```

### Check service manually (optional)

Open **Command Prompt as Administrator** and run:

```cmd
sc query HRMSSyncService
```

Status should show **RUNNING**.

---

## Daily Use

- Open **HRMS Attendance Sync Manager** from Desktop or Start Menu to view status.
- The background service runs automatically — no need to keep the manager open.
- Attendance syncs every 30 seconds (or your configured interval).
- The service starts automatically when Windows restarts.

---

## Troubleshooting

### “Test SQL Connection” fails

- Confirm SQL Server is running (check Windows Services for SQL Server).
- Verify the instance name in SQL Server Management Studio.
- Check username/password or try Windows Authentication (leave username empty).
- Ensure the database name is correct (`etimetracklite1`).

### “Test API Connection” fails

- Verify API URL and API Key with your HRMS administrator.
- Check internet/firewall allows HTTPS to the API server.
- Ensure `companyCode` is registered in HRMS.

### Service shows Stopped

1. Open the manager dashboard.
2. Click **Start Service**.
3. If it fails, open the logs folder and check the latest log file.

### Service not installed after setup

1. Run **HRMSSyncManager.exe** as Administrator.
2. Complete the setup wizard again, or use dashboard buttons:
   - **Stop Service** → **Start Service**

### Re-run setup wizard

Delete the config file (as Administrator):

```
C:\ProgramData\HRMSAgent\config.json
```

Then open **HRMSSyncManager.exe** — the wizard will appear again.

---

## Uninstall

1. Open **Settings** → **Apps** → find **HRMS Attendance Sync Agent** → **Uninstall**  
   *(if installed via HRMSAgentSetup.exe)*

   **OR** run as Administrator:

   ```cmd
   sc stop HRMSSyncService
   sc delete HRMSSyncService
   ```

2. Delete the install folder:
   - `C:\Program Files\HRMSAgent` (if used Method A)
   - Or the folder you copied (if used Method B)

3. Optionally delete data (removes config, logs, sync history):

   ```
   C:\ProgramData\HRMSAgent\
   ```

---

## Support Checklist

If you need help, provide:

1. Screenshot of the manager dashboard
2. Latest log file from `C:\ProgramData\HRMSAgent\Logs\`
3. SQL Server instance name and eSSL version
4. Company code (do **not** share API key in plain text)

---

## File Locations Reference

| Purpose | Location |
|---------|----------|
| Application | `C:\Program Files\HRMSAgent\` |
| Configuration | `C:\ProgramData\HRMSAgent\config.json` |
| Sync checkpoint | `C:\ProgramData\HRMSAgent\syncstate.json` |
| Logs | `C:\ProgramData\HRMSAgent\Logs\` |
| Windows Service name | `HRMSSyncService` |
