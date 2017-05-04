@if not defined _echo @echo off
setlocal

call %~dp0sync.cmd
if NOT [%ERRORLEVEL%]==[0] exit /b 1
call %~dp0build-managed.cmd -Project:%~dp0src\SharedFrameworkValidation\SharedFrameworkValidation.proj -- /t:ReBuild
if NOT [%ERRORLEVEL%]==[0] exit /b 1
call %~dp0build-tests.cmd -SkipTests -- /p:RefPath=%~dp0bin\ref\SharedFrameworkValidation /p:RunningSharedFrameworkValidation=true /p:RuntimePath=%~dp0bin\runtime\SharedFrameworkValidation\
exit /b %ERRORLEVEL%
