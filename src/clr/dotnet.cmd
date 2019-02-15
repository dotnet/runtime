@if not defined _echo @echo off
setlocal

set "__ProjectDir=%~dp0"

call "%__ProjectDir%"\setup_vs_tools.cmd

REM setup_vs_tools.cmd will correctly echo error message.
if NOT '%ERRORLEVEL%' == '0' exit /b 1

:: Clear the 'Platform' env variable for this session, as it's a per-project setting within the build, and
:: misleading value (such as 'MCD' in HP PCs) may lead to build breakage (issue: #69).
set Platform=
set __ProjectDir=

:: Restore the Tools directory
call %~dp0init-tools.cmd
if NOT [%ERRORLEVEL%]==[0] (
  exit /b 1
)

pushd %~dp0
echo Running: dotnet %*
call "%~dp0\Tools\dotnetcli\dotnet.exe" %*
popd
if NOT [%ERRORLEVEL%]==[0] (
  exit /b 1
)

exit /b 0
