# HRMS Attendance Sync Agent

Synchronizes attendance from **eSSL eTimeTrackLite** (SQL Server) to the HRMS cloud API.

---

## For Installation on Windows (No Code Required)

1. Get the **`HRMSAgent`** folder (or `HRMSAgentSetup.exe`)
2. Follow **[INSTALLATION.md](INSTALLATION.md)**

That is all you need on the target Windows machine.

---

## Package Contents (after build)

```
dist/HRMSAgent/          ← Copy this entire folder to Windows
├── HRMSSyncManager.exe
├── HRMSSyncService.exe
├── install.bat
├── INSTALLATION.md
└── (runtime files)
```

Optional: `dist/HRMSAgentSetup.exe` — Windows installer built from the same folder.

---

## For Developers (build the package once)

On a **Windows** machine with .NET 8 SDK:

```powershell
.\scripts\publish.ps1
```

Output: `dist\HRMSAgent\` — zip this folder and deliver to customers.

Optional installer:

```powershell
iscc installer\HRMSAgentInstaller.iss
```

Output: `dist\HRMSAgentSetup.exe`

---

## Solution Structure (source code)

```
src/
├── HRMSAgent.Core/       Shared library
├── HRMSSyncService/      Windows background service
└── HRMSSyncManager/      WPF setup & dashboard
```
