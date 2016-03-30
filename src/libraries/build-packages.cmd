@if "%_echo%" neq "on" echo off
setlocal EnableDelayedExpansion

set packagesLog=build-packages.log
echo Running build-packages.cmd %* > %packagesLog%

set options=/nologo /maxcpucount /v:minimal /clp:Summary /nodeReuse:false /flp:v=detailed;Append;LogFile=%packagesLog%
set allargs=%*

if /I [%1] == [/?] goto Usage
if /I [%1] == [/help] goto Usage

REM ensure that msbuild is available
echo Running init-tools.cmd
call %~dp0init-tools.cmd

echo msbuild.exe %~dp0src\packages.builds !options! !allargs! >> %packagesLog%
call msbuild.exe %~dp0src\packages.builds !options! !allargs!
if NOT [%ERRORLEVEL%]==[0] (
  echo ERROR: An error occurred while building packages, see %packagesLog% for more details.
  exit /b
)

echo Done Building Packages.
exit /b

:Usage
echo.
echo Builds the NuGet packages from the binaries that were built in the Build product binaries step.
echo No option parameters.
exit /b