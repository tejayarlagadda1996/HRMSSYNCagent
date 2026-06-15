$ErrorActionPreference = "Stop"

$Root = Split-Path -Parent $PSScriptRoot
$Dist = Join-Path $Root "dist\HRMSAgent"

Write-Host "Building HRMS Attendance Sync Agent package..." -ForegroundColor Cyan

if (Test-Path $Dist) { Remove-Item $Dist -Recurse -Force }
New-Item -ItemType Directory -Path $Dist | Out-Null

# Manager UI (main folder)
dotnet publish (Join-Path $Root "src\HRMSSyncManager\HRMSSyncManager.csproj") `
    -c Release -r win-x64 --self-contained true `
    -o $Dist

# Service executable alongside manager (wizard looks here first)
dotnet publish (Join-Path $Root "src\HRMSSyncService\HRMSSyncService.csproj") `
    -c Release -r win-x64 --self-contained true `
    -o (Join-Path $Dist "_service_build")

Copy-Item (Join-Path $Dist "_service_build\HRMSSyncService.exe") `
    (Join-Path $Dist "HRMSSyncService.exe") -Force

# Also keep full service folder as fallback
$ServiceDir = Join-Path $Dist "service"
New-Item -ItemType Directory -Path $ServiceDir | Out-Null
Copy-Item (Join-Path $Dist "_service_build\*") $ServiceDir -Recurse -Force
Remove-Item (Join-Path $Dist "_service_build") -Recurse -Force

# Package docs and install helper
$Package = Join-Path $Root "package"
Copy-Item (Join-Path $Package "INSTALLATION.md") $Dist -Force
Copy-Item (Join-Path $Package "README.txt") $Dist -Force
Copy-Item (Join-Path $Package "install.bat") $Dist -Force

Write-Host ""
Write-Host "Package ready:" -ForegroundColor Green
Write-Host "  $Dist"
Write-Host ""
Write-Host "Deliver this folder to Windows:" -ForegroundColor Yellow
Write-Host "  1. Copy dist\HRMSAgent to USB / network / zip"
Write-Host "  2. On Windows: right-click install.bat -> Run as administrator"
Write-Host "  3. Follow INSTALLATION.md"
Write-Host ""
Write-Host "Optional - build setup installer (requires Inno Setup):" -ForegroundColor Cyan
Write-Host "  iscc `"$(Join-Path $Root 'installer\HRMSAgentInstaller.iss')`""
