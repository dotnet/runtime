@if "%_echo%" neq "on" echo off
setlocal
setlocal enableextensions
setlocal enabledelayedexpansion

if defined VisualStudioVersion goto :Run

set _VSWHERE="%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"
if exist %_VSWHERE% (
  for /f "usebackq tokens=*" %%i in (`%_VSWHERE% -latest -prerelease -property installationPath`) do set _VSCOMNTOOLS=%%i\Common7\Tools
)
if not exist "%_VSCOMNTOOLS%" set _VSCOMNTOOLS=%VS140COMNTOOLS%
if not exist "%_VSCOMNTOOLS%" (
    echo Error: Visual Studio 2015 or 2017 required.
    echo        Please see https://github.com/dotnet/core-setup/blob/master/Documentation/building/windows-instructions.md for build instructions.
    exit /b 1
)

set VSCMD_START_DIR="%~dp0"
call "%_VSCOMNTOOLS%\VsDevCmd.bat"

:Run

:: Makes test explorer work in Visual Studio by setting up environment variables for tests
:: See EnvironmentVariables around https://github.com/dotnet/core-setup/blob/master/src/test/dir.proj#L93
:: Found the env vars by running build -MsBuildLogging=/bl, then look at the Exec task in RunTest target

set CMD_START_DIR=%~dp0
set NUGET_PACKAGES=%CMD_START_DIR%packages\
set DOTNET_SDK_PATH=%CMD_START_DIR%Tools\dotnetcli\

set DOTNET_MULTILEVEL_LOOKUP=0

set TEST_TARGETRID=win-x64
set BUILDRID=win-x64
set BUILD_ARCHITECTURE=x64
set BUILD_CONFIGURATION=Debug

set TEST_ARTIFACTS=%CMD_START_DIR%bin\tests\%TEST_TARGETRID%.%BUILD_CONFIGURATION%\
set MNA_TFM=netcoreapp3.0
set MNA_SEARCH=%CMD_START_DIR%bin\obj\%TEST_TARGETRID%.%BUILD_CONFIGURATION%\sharedFrameworkPublish\shared\Microsoft.NETCore.App\*

:: We expect one hostfxr version directory in the MNA_SEARCH path
for /d  %%i in (%MNA_SEARCH%) do (
  set MNA_PATH=%%i
  )

set MNA_VERSION=%MNA_PATH:*sharedFrameworkPublish\shared\Microsoft.NETCore.App\=%

devenv %CMD_START_DIR%\Microsoft.DotNet.CoreSetup.sln
