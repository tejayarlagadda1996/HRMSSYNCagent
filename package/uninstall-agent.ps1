# HRMS Attendance Sync Agent - Uninstall
# Standalone script: copy this file to any PC and run as Administrator.
# Removes the Windows service, all HRMSAgent processes, config, and logs.

#Requires -Version 5.1

$ServiceName = "HRMSSyncService"
$ProcessName = "HRMSAgent"
$ProgramDataPath = "C:\ProgramData\HRMSAgent"
$DefaultInstallPath = "C:\Program Files\HRMSAgent"

function Test-IsAdmin {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($identity)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

if (-not (Test-IsAdmin)) {
    Write-Host "Administrator rights required. Restarting elevated..." -ForegroundColor Yellow
    Start-Process powershell.exe -Verb RunAs -ArgumentList @(
        "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", "`"$PSCommandPath`""
    ) -Wait
    exit 0
}

Write-Host ""
Write-Host "HRMS Attendance Sync Agent - Uninstall" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "[1/4] Stopping Windows service ($ServiceName)..." -ForegroundColor Cyan
sc.exe stop $ServiceName 2>$null | Out-Null
Start-Sleep -Seconds 2
sc.exe delete $ServiceName 2>$null | Out-Null
Start-Sleep -Seconds 1

Write-Host "[2/4] Stopping $ProcessName processes..." -ForegroundColor Cyan
Get-Process -Name $ProcessName -ErrorAction SilentlyContinue | ForEach-Object {
    try {
        Stop-Process -Id $_.Id -Force -ErrorAction Stop
        Write-Host "  Stopped PID $($_.Id)"
    }
    catch {
        Write-Host "  Could not stop PID $($_.Id): $($_.Exception.Message)" -ForegroundColor Yellow
    }
}
Start-Sleep -Seconds 1

Write-Host "[3/4] Removing agent data ($ProgramDataPath)..." -ForegroundColor Cyan
if (Test-Path $ProgramDataPath) {
    Remove-Item $ProgramDataPath -Recurse -Force -ErrorAction SilentlyContinue
    if (Test-Path $ProgramDataPath) {
        Write-Host "  Some files could not be removed. Close HRMSAgent and run this script again." -ForegroundColor Yellow
    }
    else {
        Write-Host "  Removed."
    }
}
else {
    Write-Host "  Not found (already removed)."
}

Write-Host "[4/4] Checking install folder..." -ForegroundColor Cyan
if (Test-Path $DefaultInstallPath) {
    Write-Host "  Found: $DefaultInstallPath"
    Write-Host '  To remove application files: Settings -> Apps -> uninstall, or delete the folder manually.'
}
else {
    Write-Host "  Default install path not found."
}

Write-Host ""
Write-Host "Uninstall complete." -ForegroundColor Green
Write-Host "  - Service removed"
Write-Host "  - Background processes stopped"
Write-Host "  - Config, sync state, and logs removed from $ProgramDataPath"
Write-Host ""
Write-Host "HRMSAgent.exe may still exist if installed via the setup installer."
Write-Host 'Remove it from Settings -> Apps -> HRMS Attendance Sync Agent.'
Write-Host ""
