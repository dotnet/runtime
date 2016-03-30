@if "%_echo%" neq "on" echo off
setlocal EnableDelayedExpansion

set buildTests=build-tests.log
echo Running build-tests.cmd %* > %buildTests%

set options=/nologo /maxcpucount /v:minimal /clp:Summary /nodeReuse:false /flp:v=detailed;Append;LogFile=%buildTests%
set allargs=%*

if /I [%1] == [/?] goto Usage
if /I [%1] == [/help] goto Usage

REM ensure that msbuild is available
echo Running init-tools.cmd
call %~dp0init-tools.cmd

echo msbuild.exe %~dp0src\tests.builds !options! !allargs! >> %buildTests%
call msbuild.exe %~dp0src\tests.builds !options! !allargs!
if NOT [%ERRORLEVEL%]==[0] (
  echo ERROR: An error occurred while building the tests, see %buildTests% for more details.
  exit /b
)

echo Done Building tests.
exit /b

:Usage
echo.
echo Builds the tests that are in the repository.
echo No option parameters.
exit /b