@if not defined __echo @echo off
setlocal

set __ThisScriptShort=%0
set __ThisScriptFull=%~f0
set __ThisScriptPath=%~dp0

REM =========================================================================================
REM ===
REM === Parse arguments
REM ===
REM =========================================================================================

set __OutputDir=
set __Arch= 
set __CoreclrBinPath=
set __NugetCacheDir=
set __CliPath=

:Arg_Loop
if "%1" == "" goto ArgsDone

if /i "%1" == "/?"    goto Usage
if /i "%1" == "-?"    goto Usage
if /i "%1" == "/h"    goto Usage
if /i "%1" == "-h"    goto Usage
if /i "%1" == "/help" goto Usage
if /i "%1" == "-help" goto Usage

if /i "%1" == "/outputDir"          (set __OutputDir=%2&shift&shift&goto Arg_Loop)
if /i "%1" == "/coreclrBinDir"      (set __CoreclrBinPath=%2&shift&shift&goto Arg_Loop)
if /i "%1" == "/nugetCacheDir"      (set __NugetCacheDir=%2&shift&shift&goto Arg_Loop)
if /i "%1" == "/cliPath"            (set __CliPath=%2&shift&shift&goto Arg_Loop)


echo Invalid command-line argument: %1
goto Usage

:ArgsDone

if not defined __OutputDir goto Usage
if not defined __Arch goto Usage 

REM Create directories needed
if exist "%__OutputDir%\debuggertests" rmdir /S /Q "%__OutputDir%\debuggertests"
md "%__OutputDir%\debuggertests"
set __InstallDir=%__OutputDir%\debuggertests

REM =========================================================================================
REM ===
REM === download debuggertests package
REM ===
REM =========================================================================================
set DEBUGGERTESTS_URL=https://dotnetbuilddrops.blob.core.windows.net/debugger-container/Windows.DebuggerTests.zip
set LOCAL_ZIP_PATH=%__InstallDir%\debuggertests.zip
if exist "%LOCAL_ZIP_PATH%" del "%LOCAL_ZIP_PATH%"
set DEBUGGERTESTS_INSTALL_LOG="%__ThisScriptPath%debuggerinstall.log"
REM Download the package
echo Download and unzip debuggertests package to %LOCAL_ZIP_PATH%
powershell -NoProfile -ExecutionPolicy unrestricted -Command "$retryCount = 0; $success = $false; do { try { (New-Object Net.WebClient).DownloadFile('%DEBUGGERTESTS_URL%', '%LOCAL_ZIP_PATH%'); $success = $true; } catch { if ($retryCount -ge 6) { throw; } else { $retryCount++; Start-Sleep -Seconds (5 * $retryCount); } } } while ($success -eq $false); Add-Type -Assembly 'System.IO.Compression.FileSystem' -ErrorVariable AddTypeErrors; if ($AddTypeErrors.Count -eq 0) { [System.IO.Compression.ZipFile]::ExtractToDirectory('%LOCAL_ZIP_PATH%', '%__InstallDir%') } else { (New-Object -com shell.application).namespace('%LOCAL_ZIP_PATH%').CopyHere((new-object -com shell.application).namespace('%__InstallDir%').Items(),16) }" >> %DEBUGGERTESTS_INSTALL_LOG%

if errorlevel 1 (
    echo Failed to install debuggertests to %__InstallDir%
    goto Fail
)

REM =========================================================================================
REM ===
REM === Setting up the right config file.
REM ===
REM =========================================================================================
echo Generating config file.

call %__ThisScriptPath%\ConfigFilesGenerators\GenerateConfig.cmd rt %__CoreclrBinPath% nc %__NugetCacheDir% cli %__CliPath%
move Debugger.Tests.Config.txt %__InstallDir%\\Debugger.Tests\dotnet\Debugger.Tests.Config.txt

REM =========================================================================================
REM ===
REM === Scripts generation.
REM ===
REM =========================================================================================
mkdir %__InstallDir%\ScriptGenerator
copy %__ThisScriptPath%\ScriptGenerator\*  %__InstallDir%\ScriptGenerator\
pushd %__InstallDir%\ScriptGenerator
%__CliPath%\dotnet restore
%__CliPath%\dotnet build
popd 

%__CliPath%\dotnet run --project %__InstallDir%\ScriptGenerator %__InstallDir% %__CoreclrBinPath% %__InstallDir%\Dotnet.Tests\dotnet

REM Deleting runtests.cmd to avoid double test-running.
del %__InstallDir%\runtests.cmd

if errorlevel 1 (
    echo Failed to build and run script generation.
    goto Fail
)



exit /b 0

:Fail
exit /b 1

REM =========================================================================================
REM ===
REM === Helper routines
REM ===
REM =========================================================================================

:Usage
echo.
echo install debugger tests
echo.
echo Usage:
echo     %__ThisScriptShort% /coreclrBinDir ^<coreclr bin path^> /outputDir ^<debuggertests install path^> /nugetCacheDir ^<nuget cache dir path^>
echo.
exit /b 1
