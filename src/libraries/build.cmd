@echo off

if "%~1"=="-h" goto help
if "%~1"=="-help" goto help
if "%~1"=="-?" goto help
if "%~1"=="/?" goto help

powershell -ExecutionPolicy ByPass -NoProfile -File "%~dp0eng\build.ps1" %*
goto end

:help
powershell -ExecutionPolicy ByPass -NoProfile -Command "& { . %~dp0eng\build.ps1; Get-Help }"

:end
exit /b %ERRORLEVEL%
