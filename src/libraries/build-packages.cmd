@if "%_echo%" neq "on" echo off
setlocal

set packagesLog=build-packages.log
echo Running build-packages.cmd %* > %packagesLog%

if /I [%1] == [/?] goto Usage
if /I [%1] == [/help] goto Usage

REM ensure that msbuild is available
echo Running init-tools.cmd
call %~dp0init-tools.cmd

echo msbuild.exe %~dp0src\packages.builds %* /nologo /v:minimal /flp:v=detailed;Append;LogFile=%packagesLog% >> %packagesLog%
call msbuild.exe %~dp0src\packages.builds %* /nologo /v:minimal /flp:v=detailed;Append;LogFile=%packagesLog%
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