$ErrorActionPreference = "Stop"

$Root = Split-Path -Parent $PSScriptRoot
$Dist = Join-Path $Root "dist\HRMSAgent"

Write-Host "Building HRMS Attendance Sync Agent..." -ForegroundColor Cyan

if (Test-Path $Dist) {
    Write-Host "Stopping agent processes that lock the build output..." -ForegroundColor Yellow
    sc.exe stop HRMSSyncService 2>$null | Out-Null
    Start-Sleep -Seconds 2
    Get-Process -Name "HRMSAgent" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 1
    Remove-Item $Dist -Recurse -Force
}
New-Item -ItemType Directory -Path $Dist | Out-Null

dotnet publish (Join-Path $Root "src\HRMSSyncManager\HRMSSyncManager.csproj") `
    -c Release -r win-x64 --self-contained true `
    /p:DebugType=None /p:DebugSymbols=false `
    -o $Dist

Get-ChildItem $Dist -Filter "*.pdb" | Remove-Item -Force

Copy-Item (Join-Path $Root "package\uninstall-agent.ps1") $Dist -Force
Copy-Item (Join-Path $Root "package\OPERATIONS.md") $Dist -Force

$ExePath = Join-Path $Dist "HRMSAgent.exe"
if (-not (Test-Path $ExePath)) {
    throw "Expected output not found: $ExePath"
}

Write-Host ""
Write-Host "Build ready:" -ForegroundColor Green
Write-Host "  $ExePath"
Write-Host ""
Write-Host "Launches with Administrator privileges (UAC prompt)." -ForegroundColor Yellow
Write-Host ""
Write-Host "Optional installer (Inno Setup 6):" -ForegroundColor Cyan
Write-Host "  iscc `"$(Join-Path $Root 'installer\HRMSAgentInstaller.iss')`""
