@if not defined _echo @echo off
setlocal
set BUILDARCH=%1
if /I [%BUILDARCH%] == [] set BUILDARCH=x64

call %~dp0sync.cmd -- /p:ArchGroup=%BUILDARCH%
if NOT [%ERRORLEVEL%]==[0] exit /b 1
call %~dp0build-managed.cmd -Project:%~dp0src\SharedFrameworkValidation\SharedFrameworkValidation.proj -- /t:CreateScriptToDownloadSharedFrameworkZip
if NOT [%ERRORLEVEL%]==[0] exit /b 1
powershell -NoProfile -ExecutionPolicy unrestricted -Command "%~dp0bin\DownloadSharedFramework.ps1"
call %~dp0build-managed.cmd -Project:%~dp0src\SharedFrameworkValidation\SharedFrameworkValidation.proj -buildArch=%BUILDARCH%
if NOT [%ERRORLEVEL%]==[0] exit /b 1
call %~dp0bin\CloneAndRunTests.cmd
exit /b %ERRORLEVEL%
