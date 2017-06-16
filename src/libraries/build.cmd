@if not defined _echo @echo off
setlocal

if /I [%1] == [-?] goto Usage

if [%1] == [] goto Build

set _rootDirectory=%CD%\
if EXIST %_rootDirectory%%1 goto :BuildDirectory
set _rootDirectory=%~dp0src\
if EXIST %_rootDirectory%%1 goto :BuildDirectory

:Build
call %~dp0build-native.cmd %*
if NOT [%ERRORLEVEL%]==[0] exit /b 1
call %~dp0build-managed.cmd -BuildPackages=true %*
exit /b %ERRORLEVEL%
goto :EOF

:BuildDirectory
echo %~dp0run.cmd build-directory -directory:%_rootDirectory%%*
call %~dp0run.cmd build-directory -directory:%_rootDirectory%%*
exit /b %ERRORLEVEL%

goto :EOF

:Usage
echo.
echo There are new changes on how we build. Use this script only for generic
echo build instructions that apply for both build native and build managed.
echo Otherwise:
echo.
echo Before                Now
echo build.cmd native      build-native.cmd
echo build.cmd managed     build-managed.cmd
echo.
echo For more information: "https://github.com/dotnet/corefx/blob/master/Documentation/project-docs/developer-guide.md"
echo ----------------------------------------------------------------------------
echo.
echo.
goto :Build
