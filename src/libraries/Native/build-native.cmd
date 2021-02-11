@if not defined _echo @echo off
setlocal

:SetupArgs
:: Initialize the args that will be passed to cmake
set __sourceDir=%~dp0\Windows
set __repoRoot=%~dp0..\..\..
set __engNativeDir=%__repoRoot%\eng\native
set __artifactsDir=%__repoRoot%\artifacts
set __CMakeBinDir=""
set __IntermediatesDir=""
set __BuildArch=x64
set __BuildTarget="build"
set __VCBuildArch=x86_amd64
set __TargetOS=windows
set CMAKE_BUILD_TYPE=Debug
set "__LinkArgs= "
set "__LinkLibraries= "
set __Ninja=0

:Arg_Loop
:: Since the native build requires some configuration information before msbuild is called, we have to do some manual args parsing
if [%1] == [] goto :ToolsVersion
if /i [%1] == [Release]     ( set CMAKE_BUILD_TYPE=Release&&shift&goto Arg_Loop)
if /i [%1] == [Debug]       ( set CMAKE_BUILD_TYPE=Debug&&shift&goto Arg_Loop)

if /i [%1] == [AnyCPU]      ( set __BuildArch=x64&&set __VCBuildArch=x86_amd64&&shift&goto Arg_Loop)
if /i [%1] == [x86]         ( set __BuildArch=x86&&set __VCBuildArch=x86&&shift&goto Arg_Loop)
if /i [%1] == [arm]         ( set __BuildArch=arm&&set __VCBuildArch=x86_arm&&shift&goto Arg_Loop)
if /i [%1] == [x64]         ( set __BuildArch=x64&&set __VCBuildArch=x86_amd64&&shift&goto Arg_Loop)
if /i [%1] == [amd64]       ( set __BuildArch=x64&&set __VCBuildArch=x86_amd64&&shift&goto Arg_Loop)
if /i [%1] == [arm64]       ( set __BuildArch=arm64&&set __VCBuildArch=x86_arm64&&shift&goto Arg_Loop)
if /i [%1] == [wasm]        ( set __BuildArch=wasm&&set __VCBuildArch=x86_amd64&&shift&goto Arg_Loop)

if /i [%1] == [outconfig] ( set __outConfig=%2&&shift&&shift&goto Arg_Loop)

if /i [%1] == [Browser] ( set __TargetOS=Browser&&shift&goto Arg_Loop)

if /i [%1] == [rebuild] ( set __BuildTarget=rebuild&&shift&goto Arg_Loop)

if /i [%1] == [ninja] ( set __Ninja=1&&shift&goto Arg_Loop)

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
goto :SetupDirs

:VS2017
:: Setup vars for VS2017
set __VSVersion=vs2017
set __PlatformToolset=v141
:: Set the environment for the native build
call "%VS150COMNTOOLS%..\..\VC\Auxiliary\Build\vcvarsall.bat" %__VCBuildArch%
goto :SetupDirs

:SetupDirs
:: Setup to cmake the native components
echo Commencing build of native components
echo.

if /i "%__BuildArch%" == "wasm" set __sourceDir=%~dp0\Unix

if [%__outConfig%] == [] set __outConfig=%__TargetOS%-%__BuildArch%-%CMAKE_BUILD_TYPE%

if %__CMakeBinDir% == "" (
    set "__CMakeBinDir=%__artifactsDir%\bin\native\%__outConfig%"
)
if %__IntermediatesDir% == "" (
    set "__IntermediatesDir=%__artifactsDir%\obj\native\%__outConfig%"
)
set "__CMakeBinDir=%__CMakeBinDir:\=/%"
set "__IntermediatesDir=%__IntermediatesDir:\=/%"

:: Check that the intermediate directory exists so we can place our cmake build tree there
if "%__BuildTarget%"=="rebuild" if exist "%__IntermediatesDir%" rd /s /q "%__IntermediatesDir%"
if not exist "%__IntermediatesDir%" md "%__IntermediatesDir%"

:: Write an empty Directory.Build.props/targets to ensure that msbuild doesn't pick up
:: the repo's root Directory.Build.props/targets.
set MSBUILD_EMPTY_PROJECT_CONTENT= ^
 ^^^<Project xmlns=^"http://schemas.microsoft.com/developer/msbuild/2003^"^^^> ^
 ^^^</Project^^^>
echo %MSBUILD_EMPTY_PROJECT_CONTENT% > "%__artifactsDir%\obj\native\Directory.Build.props"
echo %MSBUILD_EMPTY_PROJECT_CONTENT% > "%__artifactsDir%\obj\native\Directory.Build.targets"

if exist "%VSINSTALLDIR%DIA SDK" goto FindCMake
echo Error: DIA SDK is missing at "%VSINSTALLDIR%DIA SDK". ^
Did you install all the requirements for building on Windows, including the "Desktop Development with C++" workload? ^
Please see https://github.com/dotnet/runtime/blob/master/docs/workflow/requirements/windows-requirements.md ^
Another possibility is that you have a parallel installation of Visual Studio and the DIA SDK is there. In this case it ^
may help to copy its "DIA SDK" folder into "%VSINSTALLDIR%" manually, then try again.
exit /b 1

:FindCMake
if defined CMakePath goto GenVSSolution
:: Find CMake

:: Eval the output from set-cmake-path.ps1
for /f "delims=" %%a in ('powershell -NoProfile -ExecutionPolicy ByPass "& ""%__repoRoot%\eng\native\set-cmake-path.ps1"""') do %%a

:GenVSSolution
:: generate version file
powershell -NoProfile -ExecutionPolicy ByPass -NoLogo -File "%__repoRoot%\eng\common\msbuild.ps1" /clp:nosummary %__ArcadeScriptArgs%^
    "%__repoRoot%\eng\empty.csproj" /p:NativeVersionFile="%__artifactsDir%\obj\_version.h"^
    /t:GenerateNativeVersionFile /restore
:: Regenerate the VS solution

:: cmake requires forward slashes in paths
set __cmakeRepoRoot=%__repoRoot:\=/%

pushd "%__IntermediatesDir%"
call "%__repoRoot%\eng\native\gen-buildsys.cmd" "%__sourceDir%" "%__IntermediatesDir%" %__VSVersion% %__BuildArch% "-DCMAKE_REPO_ROOT=%__cmakeRepoRoot%"
if NOT [%errorlevel%] == [0] goto :Failure
popd

:BuildNativeProj
:: Build the project created by Cmake
set __generatorArgs=
if [%__Ninja%] == [1] (
    set __generatorArgs=
) else if [%__BuildArch%] == [wasm] (
    set __generatorArgs=
) else (
    set __generatorArgs=/p:Platform=%__BuildArch% /p:PlatformToolset="%__PlatformToolset%" -noWarn:MSB8065
)

call "%CMakePath%" --build "%__IntermediatesDir%" --target install --config %CMAKE_BUILD_TYPE% -- %__generatorArgs%
IF ERRORLEVEL 1 (
    goto :Failure
)

echo Done building Native components
exit /B 0

:Failure
:: Build failed
echo Failed to generate native component build project!
exit /b 1
