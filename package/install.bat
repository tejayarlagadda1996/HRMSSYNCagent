@echo off
setlocal

:: HRMS Attendance Sync Agent - Quick Install
:: Right-click this file and choose "Run as administrator"

net session >nul 2>&1
if %errorlevel% neq 0 (
    echo.
    echo ERROR: Please run this script as Administrator.
    echo Right-click install.bat and select "Run as administrator".
    echo.
    pause
    exit /b 1
)

set "SOURCE=%~dp0"
set "TARGET=C:\Program Files\HRMSAgent"

echo.
echo Installing HRMS Attendance Sync Agent...
echo   From: %SOURCE%
echo   To:   %TARGET%
echo.

if not exist "%SOURCE%HRMSSyncManager.exe" (
    echo ERROR: HRMSSyncManager.exe not found in this folder.
    pause
    exit /b 1
)

if not exist "%TARGET%" mkdir "%TARGET%"

xcopy "%SOURCE%*" "%TARGET%\" /E /I /Y /Q >nul

if not exist "C:\ProgramData\HRMSAgent" mkdir "C:\ProgramData\HRMSAgent"
if not exist "C:\ProgramData\HRMSAgent\Logs" mkdir "C:\ProgramData\HRMSAgent\Logs"

set "DESKTOP=%USERPROFILE%\Desktop"
powershell -NoProfile -Command "$s=(New-Object -ComObject WScript.Shell).CreateShortcut('%DESKTOP%\HRMS Attendance Sync Manager.lnk');$s.TargetPath='%TARGET%\HRMSSyncManager.exe';$s.WorkingDirectory='%TARGET%';$s.Save()"

echo.
echo Installation complete.
echo.
echo NEXT STEP: Open "HRMS Attendance Sync Manager" from your Desktop
echo            and complete the 5-step setup wizard.
echo.
pause
start "" "%TARGET%\HRMSSyncManager.exe"

endlocal
