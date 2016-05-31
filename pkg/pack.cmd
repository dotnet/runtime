@echo off
setlocal EnableDelayedExpansion

set __ProjectDir=%~dp0
set __ThisScriptShort=%0
set __ThisScriptFull="%~f0"

::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
:: Adding environment variables to workaround the "Argument Escape" problem with passing arguments to
:: .cmd calls from dotnet-cli-build scripts.
::
set __BuildArch=%__WorkaroundCliCoreHostBuildArch%
set __DotNetHostBinDir=%__WorkaroundCliCoreHostBinDir%
set __HostVer=%__WorkaroundCliCoreHostVer%
set __FxrVer=%__WorkaroundCliCoreHostFxrVer%
set __PolicyVer=%__WorkaroundCliCoreHostPolicyVer%
set __BuildMajor=%__WorkaroundCliCoreHostBuildMajor%
set __BuildMinor=%__WorkaroundCliCoreHostBuildMinor%
set __VersionTag=%__WorkaroundCliCoreHostVersionTag%
::
:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::

:Arg_Loop
if "%1" == "" goto ArgsDone

if /i "%1" == "/?"    goto Usage
if /i "%1" == "-?"    goto Usage
if /i "%1" == "/h"    goto Usage
if /i "%1" == "-h"    goto Usage
if /i "%1" == "/help" goto Usage
if /i "%1" == "-help" goto Usage

if /i "%1" == "x64"                 (set __BuildArch=%1&shift&goto Arg_Loop)
if /i "%1" == "x86"                 (set __BuildArch=%1&shift&goto Arg_Loop)
if /i "%1" == "arm"                 (set __BuildArch=%1&shift&goto Arg_Loop)
if /i "%1" == "arm64"               (set __BuildArch=%1&shift&goto Arg_Loop)
if /i "%1" == "/hostbindir"         (set __DotNetHostBinDir=%2&shift&shift&goto Arg_Loop)
if /i "%1" == "/hostver"            (set __HostVer=%2&shift&shift&goto Arg_Loop)
if /i "%1" == "/fxrver"             (set __FxrVer=%2&shift&shift&goto Arg_Loop)
if /i "%1" == "/policyver"          (set __PolicyVer=%2&shift&shift&goto Arg_Loop)
if /i "%1" == "/buildmajor"         (set __BuildMajor=%2&shift&shift&goto Arg_Loop)
if /i "%1" == "/buildminor"         (set __BuildMinor=%2&shift&shift&goto Arg_Loop)
if /i "%1" == "/vertag"             (set __VersionTag=%2&shift&shift&goto Arg_Loop)

echo Invalid command line argument: %1
goto Usage

:ArgsDone

if [%__BuildArch%]==[] (goto Usage)
if [%__DotNetHostBinDir%]==[] (goto Usage)

:: Initialize the MSBuild Tools
call "%__ProjectDir%\init-tools.cmd"

:: Restore dependencies mainly to obtain runtime.json
pushd "%__ProjectDir%\deps"
"%__ProjectDir%\Tools\dotnetcli\dotnet.exe" restore --source "https://dotnet.myget.org/F/dotnet-core" --packages "%__ProjectDir%\packages"
popd

:: Clean up existing nupkgs
if exist "%__ProjectDir%\bin" (rmdir /s /q "%__ProjectDir%\bin")

:: Package the assets using Tools

copy /y "%__DotNetHostBinDir%\corehost.exe" "%__DotNetHostBinDir%\dotnet.exe"

"%__ProjectDir%\Tools\corerun" "%__ProjectDir%\Tools\MSBuild.exe" "%__ProjectDir%\projects\packages.builds" /p:Platform=%__BuildArch% /p:DotNetHostBinDir=%__DotNetHostBinDir% /p:TargetsWindows=true /p:HostVersion=%__HostVer% /p:HostResolverVersion=%__FxrVer% /p:HostPolicyVersion=%__PolicyVer% /p:BuildNumberMajor=%__BuildMajor% /p:BuildNumberMinor=%__BuildMinor% /p:PreReleaseLabel=%__VersionTag% /p:CLIBuildVersion=%__BuildMajor% /verbosity:minimal

if not ERRORLEVEL 0 goto :Error

exit /b 0

:Usage
echo.
echo Package the dotnet host artifacts
echo.
echo Usage:
echo     %__ThisScriptShort% [x64/x86/arm]  /hostbindir path-to-binaries /hostver /fxrver /policyver /build /vertag
echo.
echo./? -? /h -h /help -help: view this message.

:Error
echo An error occurred during packing.
exit /b 1
