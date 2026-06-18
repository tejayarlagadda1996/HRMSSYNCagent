# HRMS Attendance Sync Agent

Synchronizes attendance from **eSSL eTimeTrackLite** (SQL Server) to the HRMS cloud API.

---

## For Installation on Windows

1. Run **`HRMSAgentSetup.exe`** as Administrator
2. Follow **[INSTALLATION.md](INSTALLATION.md)**

A single **`HRMSAgent.exe`** handles both the setup UI and the background sync service.

---

## For Developers

### Build

On Windows with .NET 8 SDK:

```powershell
.\scripts\publish.ps1
```

Output: `dist\publish\HRMSAgent.exe`

### Create installer (requires Inno Setup 6)

```powershell
iscc installer\HRMSAgentInstaller.iss
```

Output: `dist\HRMSAgentSetup.exe`

### Local testing

| Component | Location |
|-----------|----------|
| Mock HRMS API (Node.js) | `test/mock-hrms-api/` |
| Mock eSSL database (SSMS) | `test/mock-essl-db/` |

```powershell
# Terminal 1 — mock API
cd test\mock-hrms-api
npm install
npm start

# Terminal 2 — run agent (dev)
dotnet run --project src\HRMSSyncManager\HRMSSyncManager.csproj
```

Then run `test\mock-essl-db\setup.sql` in SSMS and use test credentials from `test\mock-hrms-api\README.md`.

---

## Solution Structure

```
src/
├── HRMSAgent.Core/       Shared library (sync, SQL, API, config)
└── HRMSSyncManager/      WPF app → publishes as HRMSAgent.exe
```

Background sync runs as a Windows Service using the same executable: `HRMSAgent.exe --service`.

Config and runtime data: `C:\ProgramData\HRMSAgent\`
