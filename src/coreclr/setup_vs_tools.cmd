@if not defined _echo @echo off

REM This script is responsible for setting up the vs2017 or vs2019 env
REM All passed arguments are ignored
REM Script will return with 0 if pass, 1 if there is a failure to find either
REM vs2017 or vs2019

:: Default to highest Visual Studio version available
::
:: For VS2017 and later, multiple instances can be installed on the same box SxS and VS1*0COMNTOOLS
:: is no longer set as a global environment variable and is instead only set if the user
:: has launched the Visual Studio Developer Command Prompt.
::
:: Following this logic, we will default to the Visual Studio toolset assocated with the active
:: Developer Command Prompt. Otherwise, we will query VSWhere to locate the later version of
:: Visual Studio available on the machine. Finally, we will fail the script if not supported
:: instance can be found.

:: This script is also called by the main build.cmd, located at the root of the
:: runtime repo. For this specific scenario, we want to check if there is a
:: valid VS installation, but not call the Developer Command Prompt. For this
:: purpose, we pass a flag labelled as the number '1' from said script and store
:: it in this variable "__VSOnlyCheck". This is then checked at the end to define
:: whether or not to call the VS Dev Prompt.
::
:: The original CoreCLR callers within this directory remain untouched. In this
:: case, they don't pass any flags and so since "__VSOnlyCheck" will be undefined
:: in those cases, execution will proceed as normally.

set __VSOnlyCheck=%1

if defined VisualStudioVersion (
    if not defined __VSVersion echo %__MsgPrefix%Detected Visual Studio %VisualStudioVersion% developer command ^prompt environment
    goto skip_setup
)

echo %__MsgPrefix%Searching ^for Visual Studio installation
set _VSWHERE="%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"
if exist %_VSWHERE% (
    for /f "usebackq tokens=*" %%i in (`%_VSWHERE% -latest -prerelease -property installationPath`) do set _VSCOMNTOOLS=%%i\Common7\Tools
    goto call_vs
)

:call_vs
if not exist "%_VSCOMNTOOLS%" (
    echo %__MsgPrefix%Error: Visual Studio 2019 required.
    echo        Please see https://github.com/dotnet/runtime/blob/master/docs/workflow/requirements/windows-requirements.md for build instructions.
    exit /b 1
)

:: If called from runtime's build.cmd, we are done here and thus we proceed
:: to the exit.

if "%__VSOnlyCheck%" == "1" goto :skip_setup

echo %__MsgPrefix%"%_VSCOMNTOOLS%\VsDevCmd.bat"
call "%_VSCOMNTOOLS%\VsDevCmd.bat"

:skip_setup

exit /b 0
