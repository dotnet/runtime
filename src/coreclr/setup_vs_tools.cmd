@if not defined _echo @echo off

REM This script is responsible for setting up either the vs2015 or vs2017 env
REM All passed arguments are ignored
REM Script will return with 0 if pass, 1 if there is a failure to find either
REM vs2015 or vs2017

:: Default to highest Visual Studio version available
::
:: For VS2015 (and prior), only a single instance is allowed to be installed on a box
:: and VS140COMNTOOLS is set as a global environment variable by the installer. This
:: allows users to locate where the instance of VS2015 is installed.
::
:: For VS2017, multiple instances can be installed on the same box SxS and VS150COMNTOOLS
:: is no longer set as a global environment variable and is instead only set if the user
:: has launched the VS2017 Developer Command Prompt.
::
:: Following this logic, we will default to the VS2017 toolset if VS150COMNTOOLS tools is
:: set, as this indicates the user is running from the VS2017 Developer Command Prompt and
:: is already configured to use that toolset. Otherwise, we will fallback to using the VS2015
:: toolset if it is installed. Finally, we will fail the script if no supported VS instance
:: can be found.

if defined VisualStudioVersion (
    if not defined __VSVersion echo %__MsgPrefix%Detected Visual Studio %VisualStudioVersion% developer command ^prompt environment
    goto skip_setup
)

echo %__MsgPrefix%Searching ^for Visual Studio 2017 or 2015 installation
set _VSWHERE="%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"
if exist %_VSWHERE% (
    for /f "usebackq tokens=*" %%i in (`%_VSWHERE% -latest -prerelease -property installationPath`) do set _VSCOMNTOOLS=%%i\Common7\Tools
    goto call_vs
)
if not exist "%_VSCOMNTOOLS%" set _VSCOMNTOOLS=%VS140COMNTOOLS%
echo VS2017 not found, using VS2015
:call_vs
if not exist "%_VSCOMNTOOLS%" (
    echo %__MsgPrefix%Error: Visual Studio 2015 or 2017 required.
    echo        Please see https://github.com/dotnet/coreclr/blob/master/Documentation/building/windows-instructions.md for build instructions.
    exit /b 1
)
echo %__MsgPrefix%"%_VSCOMNTOOLS%\VsDevCmd.bat"
call "%_VSCOMNTOOLS%\VsDevCmd.bat"

:skip_setup

exit /b 0