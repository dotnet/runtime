@if not defined _echo @echo off
setlocal EnableDelayedExpansion

:kill-process
set __ProcName=%1

:: Check if __ProcName is running
tasklist /fi "imagename eq %__ProcName%" |find ":" > nul
:: __ProcName is running if errorlevel == 1
if errorlevel 1 (
	echo Stop %__ProcName% execution.
	for /f "tokens=2 delims=," %%F in ('tasklist /nh /fi "imagename eq %__ProcName%" /fo csv') do taskkill /f /PID %%~F
)

exit /b 0
