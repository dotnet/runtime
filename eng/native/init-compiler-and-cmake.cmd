@if not defined _echo @echo off
rem
rem This file locates VS C++ compilers and cmake for windows.

rem Make sure the current directory stays intact
set VSCMD_START_DIR="%CD%"

:SetupArgs
:: Initialize the args that will be passed to cmake
set __VCBuildArch=x86_amd64

:Arg_Loop
:: Since the native build requires some configuration information before msbuild is called, we have to do some manual args parsing
if [%1] == [] goto :ToolsVersion
if /i [%1] == [x86]         ( set __VCBuildArch=x86&&shift&goto Arg_Loop)
if /i [%1] == [arm]         ( set __VCBuildArch=x86_arm&&shift&goto Arg_Loop)
if /i [%1] == [x64]         ( set __VCBuildArch=x86_amd64&&shift&goto Arg_Loop)
if /i [%1] == [arm64]       ( set __VCBuildArch=x86_arm64&&shift&goto Arg_Loop)

shift
goto :Arg_Loop

:ToolsVersion
:: Default to highest Visual Studio version available
::
:: For VS2017 and later, multiple instances can be installed on the same box SxS and VSxxxCOMNTOOLS is only set if the user
:: has launched the VS2017 or VS2019 Developer Command Prompt.
::
:: Following this logic, we will default to the VS2017 or VS2019 toolset if VS150COMNTOOLS or VS160COMMONTOOLS tools is
:: set, as this indicates the user is running from the VS2017 or VS2019 Developer Command Prompt and
:: is already configured to use that toolset. Otherwise, we will fail the script if no supported VS instance can be found.

if defined VisualStudioVersion goto :RunVCVars

set _VSWHERE="%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"
if exist %_VSWHERE% (
  for /f "usebackq tokens=*" %%i in (`%_VSWHERE% -latest -prerelease -property installationPath`) do set _VSCOMNTOOLS=%%i\Common7\Tools
)
if not exist "%_VSCOMNTOOLS%" goto :MissingVersion

call "%_VSCOMNTOOLS%\VsDevCmd.bat" -no_logo

:RunVCVars
if "%VisualStudioVersion%"=="16.0" (
    goto :VS2019
) else if "%VisualStudioVersion%"=="15.0" (
    goto :VS2017
)

:MissingVersion
:: Can't find appropriate VS install
echo Error: Visual Studio 2019 required
echo        Please see https://github.com/dotnet/runtime/tree/master/docs/workflow/building/libraries for build instructions.
exit /b 1

:VS2019
:: Setup vars for VS2019
set __VSVersion=vs2019
set __PlatformToolset=v142
:: Set the environment for the native build
call "%VS160COMNTOOLS%..\..\VC\Auxiliary\Build\vcvarsall.bat" %__VCBuildArch%
goto FindDIASDK

:VS2017
:: Setup vars for VS2017
set __VSVersion=vs2017
set __PlatformToolset=v141
:: Set the environment for the native build
call "%VS150COMNTOOLS%..\..\VC\Auxiliary\Build\vcvarsall.bat" %__VCBuildArch%
goto FindDIASDK

:FindDIASDK
if exist "%VSINSTALLDIR%DIA SDK" goto CheckRepoRoot
echo Error: DIA SDK is missing at "%VSINSTALLDIR%DIA SDK". ^
Did you install all the requirements for building on Windows, including the "Desktop Development with C++" workload? ^
Please see https://github.com/dotnet/runtime/blob/master/docs/workflow/requirements/windows-requirements.md ^
Another possibility is that you have a parallel installation of Visual Studio and the DIA SDK is there. In this case it ^
may help to copy its "DIA SDK" folder into "%VSINSTALLDIR%" manually, then try again.
exit /b 1

:CheckRepoRoot

if exist "%__repoRoot%" goto :FindCMake
:: Can't find repo root
echo Error: variable __repoRoot not set.
exit /b 1

:FindCMake
:: Find CMake

:: Eval the output from set-cmake-path.ps1
for /f "delims=" %%a in ('powershell -NoProfile -ExecutionPolicy ByPass "cd "%__repoRoot:"=%"; & eng\native\set-cmake-path.ps1"') do %%a
