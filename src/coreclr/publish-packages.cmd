@if not defined _echo @echo off
setlocal EnableDelayedExpansion

if /I [%1] == [-?] goto Usage
if /I [%1] == [-help] goto Usage

echo %~dp0run.cmd publish-packages %*
call %~dp0run.cmd publish-packages %*
@exit /b %ERRORLEVEL%

:Usage
echo.
echo Publishes the NuGet packages to the specified location.
echo   -?     - Prints Usage
echo   -help  - Prints Usage
echo For publishing to Azure the following properties are required.
echo   -AzureAccount="account name"
echo   -AzureToken="access token"
echo   -BuildType="Configuration"
echo   -BuildArch="Architecture"
echo For publishing to Azure, one of the following properties is required.
echo   -PublishPackages        Pass this switch to publish product packages 
echo   -PublishSymbols         Pass this switch to publish symbol packages
echo To specify the name of the container to publish into, use the following property:
echo   -Container="container name"
echo Architecture can be x64, x86, arm, or arm64
echo Configuration can be Release, Debug, or Checked
exit /b