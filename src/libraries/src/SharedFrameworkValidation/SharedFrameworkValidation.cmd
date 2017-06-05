@if not defined _echo @echo off
setlocal
set BUILDARCH=%1
set REPO_ROOT=%~dp0..\..\
if /I [%BUILDARCH%] == [] set BUILDARCH=x64

call %REPO_ROOT%sync.cmd -- /p:ArchGroup=%BUILDARCH%
if NOT [%ERRORLEVEL%]==[0] exit /b 1
call %REPO_ROOT%build-managed.cmd -Project:%REPO_ROOT%src\SharedFrameworkValidation\SharedFrameworkValidation.proj -- /t:CreateScriptToDownloadSharedFrameworkZip
if NOT [%ERRORLEVEL%]==[0] exit /b 1
powershell -NoProfile -ExecutionPolicy unrestricted -Command "%REPO_ROOT%bin\DownloadSharedFramework.ps1"
call %REPO_ROOT%build-managed.cmd -Project:%REPO_ROOT%src\SharedFrameworkValidation\SharedFrameworkValidation.proj -buildArch=%BUILDARCH%
if NOT [%ERRORLEVEL%]==[0] exit /b 1
call %REPO_ROOT%bin\CloneAndRunTests.cmd
exit /b %ERRORLEVEL%
