; HRMS Attendance Sync Agent Installer
; Build after running scripts\publish.ps1
; Requires Inno Setup 6.x

#define MyAppName "HRMS Attendance Sync Agent"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Eficens"
#define MyAppExeName "HRMSSyncManager.exe"

[Setup]
AppId={{8F4E2A1B-9C3D-4E5F-A6B7-C8D9E0F1A2B3}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\HRMSAgent
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir=..\dist
OutputBaseFilename=HRMSAgentSetup
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\{#MyAppExeName}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Dirs]
Name: "{commonappdata}\HRMSAgent"
Name: "{commonappdata}\HRMSAgent\Logs"

[Files]
Source: "..\dist\HRMSAgent\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Installation Guide"; Filename: "{app}\INSTALLATION.md"
Name: "{group}\Open Logs Folder"; Filename: "{commonappdata}\HRMSAgent\Logs"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional icons:"

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch HRMS Sync Manager"; Flags: nowait postinstall skipifsilent

[UninstallRun]
Filename: "sc.exe"; Parameters: "stop HRMSSyncService"; Flags: runhidden; RunOnceId: "StopService"
Filename: "sc.exe"; Parameters: "delete HRMSSyncService"; Flags: runhidden; RunOnceId: "DeleteService"

[Messages]
WelcomeLabel2=This will install the HRMS Attendance Sync Agent.%n%nAfter installation, complete the 5-step setup wizard to connect SQL Server and the HRMS API.
