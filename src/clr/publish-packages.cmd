@if "%_echo%" neq "on" echo off
setlocal EnableDelayedExpansion

set packagesLog=publish-packages.log
echo Running publish-packages.cmd %* > %packagesLog%

set options=/nologo /v:minimal /flp:v=detailed;Append;LogFile=%packagesLog%
set allargs=%*

if /I [%1] == [/?] goto Usage
if /I [%1] == [/help] goto Usage

REM ensure that msbuild is available
echo Running init-tools.cmd
call %~dp0init-tools.cmd

echo msbuild.exe %~dp0src\publish.proj !options! !allargs! >> %packagesLog%
call msbuild.exe %~dp0src\publish.proj !options! !allargs!
if NOT [%ERRORLEVEL%]==[0] (
  echo ERROR: An error occurred while publishing packages, see %packagesLog% for more details.
  exit /b 1
)

echo Done publishing packages.
exit /b

:Usage
echo.
echo Publishes the NuGet packages to the specified location.
echo For publishing to Azure the following properties are required.
echo   /p:CloudDropAccountName="account name"
echo   /p:CloudDropAccessToken="access token"
echo   /p:__BuildType="Configuration"
echo   /p:__BuildArch="Architecture"
echo Architecture can be x64, x86, arm, or arm64
echo Configuration can be Release, Debug, or Checked
exit /b