#!/usr/bin/env pwsh
$ErrorActionPreference = "Stop"

$Root = Split-Path -Parent $PSScriptRoot
$Dist = Join-Path $Root "dist/HRMSAgent"

Write-Host "Building HRMS Attendance Sync Agent package..." -ForegroundColor Cyan

if (Test-Path $Dist) { Remove-Item $Dist -Recurse -Force }
New-Item -ItemType Directory -Path $Dist | Out-Null

dotnet publish (Join-Path $Root "src/HRMSSyncManager/HRMSSyncManager.csproj") `
    -c Release -r win-x64 --self-contained true `
    -o $Dist

$ServiceBuild = Join-Path $Dist "_service_build"
dotnet publish (Join-Path $Root "src/HRMSSyncService/HRMSSyncService.csproj") `
    -c Release -r win-x64 --self-contained true `
    -o $ServiceBuild

Copy-Item (Join-Path $ServiceBuild "HRMSSyncService.exe") `
    (Join-Path $Dist "HRMSSyncService.exe") -Force

$ServiceDir = Join-Path $Dist "service"
New-Item -ItemType Directory -Path $ServiceDir | Out-Null
Copy-Item (Join-Path $ServiceBuild "*") $ServiceDir -Recurse -Force
Remove-Item $ServiceBuild -Recurse -Force

$Package = Join-Path $Root "package"
Copy-Item (Join-Path $Package "INSTALLATION.md") $Dist -Force
Copy-Item (Join-Path $Package "README.txt") $Dist -Force
Copy-Item (Join-Path $Package "install.bat") $Dist -Force

Write-Host ""
Write-Host "Package ready: $Dist" -ForegroundColor Green
Write-Host ""
Write-Host "Copy dist/HRMSAgent to Windows and follow INSTALLATION.md"
