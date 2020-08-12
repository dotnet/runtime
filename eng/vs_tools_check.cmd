@if not defined _echo @echo off

REM This script checks and ensures there is a working Visual Studio installation
REM alongside its dev tools. This is a requirement to build the runtime repo.
REM All passed arguments are ignored
REM Script will return 0 if a VS installation is found, and 1 if any problems
REM cause it to fail.

:: Default to highest Visual Studio version available

:: For VS2017 and later, multiple instances can be installed on the same box.
:: SxS and VS1*0COMNTOOLS are no longer set as global environment variables and
:: are instead only set if the user has launched the Visual Studio Developer
:: Command Prompt.

:: Following this logic, we will default to the Visual Studio toolset associated
:: with the active Developer Command Prompt. Otherwise, we will query VSWhere to
:: locate the later version of Visual Studio available on the machine. Finally,
:: we will fail the script if not supported instance can be found.

if defined VisualStudioVersion (
    goto skip_setup
)

set _VSWHERE="%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"
if exist %_VSWHERE% (
    for /f "usebackq tokens=*" %%i in (`%_VSWHERE% -latest -prerelease -property installationPath`) do set _VSCOMNTOOLS=%%i\Common7\Tools
    goto call_vs
)

:call_vs
if not exist "%_VSCOMNTOOLS%" (
    echo %__MsgPrefix%Error: Visual Studio 2019 is required to build this repo. Make sure to install it and try again.
    echo For a full list of requirements, see https://github.com/dotnet/runtime/blob/master/docs/workflow/requirements/windows-requirements.md
    exit /b 1
)

:skip_setup

exit /b 0
