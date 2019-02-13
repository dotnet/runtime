@if not defined _echo @echo off
setlocal

:SetupArgs
:: Initialize the args that will be passed to cmake
set __nativeWindowsDir=%~dp0\Windows
set __artifactsDir=%~dp0..\..\artifacts
set __rootDir=%~dp0..\..
set __CMakeBinDir=""
set __IntermediatesDir=""
set __BuildArch=x64
set __appContainer=""
set __VCBuildArch=x86_amd64
set __BuildOS=Windows_NT
set CMAKE_BUILD_TYPE=Debug
set "__LinkArgs= "
set "__LinkLibraries= "

:Arg_Loop
:: Since the native build requires some configuration information before msbuild is called, we have to do some manual args parsing
if [%1] == [] goto :ToolsVersion
if /i [%1] == [Release]     ( set CMAKE_BUILD_TYPE=Release&&shift&goto Arg_Loop)
if /i [%1] == [Debug]       ( set CMAKE_BUILD_TYPE=Debug&&shift&goto Arg_Loop)

if /i [%1] == [AnyCPU]      ( set __BuildArch=x64&&set __VCBuildArch=x86_amd64&&shift&goto Arg_Loop)
if /i [%1] == [x86]         ( set __BuildArch=x86&&set __VCBuildArch=x86&&shift&goto Arg_Loop)
if /i [%1] == [arm]         ( set __BuildArch=arm&&set __VCBuildArch=x86_arm&&set __SDKVersion="-DCMAKE_SYSTEM_VERSION=10.0"&&shift&goto Arg_Loop)
if /i [%1] == [x64]         ( set __BuildArch=x64&&set __VCBuildArch=x86_amd64&&shift&goto Arg_Loop)
if /i [%1] == [amd64]       ( set __BuildArch=x64&&set __VCBuildArch=x86_amd64&&shift&goto Arg_Loop)
if /i [%1] == [arm64]       ( set __BuildArch=arm64&&set __VCBuildArch=x86_arm64&&set __SDKVersion="-DCMAKE_SYSTEM_VERSION=10.0"&&shift&goto Arg_Loop)
if /i [%1] == [wasm]        ( set __BuildArch=wasm&&set __VCBuildArch=x86_amd64&&shift&goto Arg_Loop)

if /i [%1] == [outconfig] ( set __outConfig=%2&&shift&&shift&goto Arg_Loop)

if /i [%1] == [WebAssembly] ( set __BuildOS=WebAssembly&&shift&goto Arg_Loop)

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

call "%_VSCOMNTOOLS%\VsDevCmd.bat"

:RunVCVars
if "%VisualStudioVersion%"=="16.0" (
    goto :VS2019
) else if "%VisualStudioVersion%"=="15.0" (
    goto :VS2017
)

:MissingVersion
:: Can't find VS 2017, 2019
echo Error: Visual Studio 2017 or 2019 required
echo        Please see https://github.com/dotnet/corefx/tree/master/Documentation for build instructions.
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


if [%__outConfig%] == [] set __outConfig=%__BuildOS%-%__BuildArch%-%CMAKE_BUILD_TYPE%

if %__CMakeBinDir% == "" (
    set "__CMakeBinDir=%__artifactsDir%\bin\native\%__outConfig%"
)
if %__IntermediatesDir% == "" (
    set "__IntermediatesDir=%__artifactsDir%\obj\native\%__outConfig%"
)
set "__CMakeBinDir=%__CMakeBinDir:\=/%"
set "__IntermediatesDir=%__IntermediatesDir:\=/%"

:: Check that the intermediate directory exists so we can place our cmake build tree there
if exist "%__IntermediatesDir%" rd /s /q "%__IntermediatesDir%"
if not exist "%__IntermediatesDir%" md "%__IntermediatesDir%"

:: Write an empty Directory.Build.props/targets to ensure that msbuild doesn't pick up
:: the repo's root Directory.Build.props/targets.
set MSBUILD_EMPTY_PROJECT_CONTENT= ^
 ^^^<Project xmlns=^"http://schemas.microsoft.com/developer/msbuild/2003^"^^^> ^
 ^^^</Project^^^>
echo %MSBUILD_EMPTY_PROJECT_CONTENT% > "%__artifactsDir%\obj\native\Directory.Build.props"
echo %MSBUILD_EMPTY_PROJECT_CONTENT% > "%__artifactsDir%\obj\native\Directory.Build.targets"

if exist "%VSINSTALLDIR%DIA SDK" goto GenVSSolution
echo Error: DIA SDK is missing at "%VSINSTALLDIR%DIA SDK". ^
Make sure you selected the correct dependencies when installing Visual Studio.
:: DIA SDK not included in Express editions
echo Visual Studio Express does not include the DIA SDK. ^
You need Visual Studio 2017 or 2019 (Community is free).
echo See: https://github.com/dotnet/corefx/blob/master/Documentation/building/windows-instructions.md#required-software
exit /b 1

:GenVSSolution
:: Regenerate the VS solution

pushd "%__IntermediatesDir%"
call "%__nativeWindowsDir%\gen-buildsys-win.bat" "%__nativeWindowsDir%" %__VSVersion% %__BuildArch%
popd

:CheckForProj
:: Check that the project created by Cmake exists
if exist "%__IntermediatesDir%\install.vcxproj" goto BuildNativeProj
if exist "%__IntermediatesDir%\Makefile" goto BuildNativeEmscripten
goto :Failure

:BuildNativeProj
:: Build the project created by Cmake
set __msbuildArgs=/p:Platform=%__BuildArch% /p:PlatformToolset="%__PlatformToolset%"

call msbuild "%__IntermediatesDir%\install.vcxproj" /t:rebuild /p:Configuration=%CMAKE_BUILD_TYPE% %__msbuildArgs%
IF ERRORLEVEL 1 (
    goto :Failure
)

echo Done building Native components

:BuildNativeAOT
set "__CMakeBinDir=%__artifactsDir%\bin\native\%__outConfig%-aot"
set "__IntermediatesDir=%__IntermediatesDir%-aot"
set "__CMakeBinDir=%__CMakeBinDir:\=/%"
set "__IntermediatesDir=%__IntermediatesDir:\=/%"
if exist "%__IntermediatesDir%" rd /s /q "%__IntermediatesDir%"
if not exist "%__IntermediatesDir%" md "%__IntermediatesDir%"
set "__LinkArgs=%__LinkArgs% /APPCONTAINER"
set "__appContainer=true"

pushd "%__IntermediatesDir%"
call "%__nativeWindowsDir%\gen-buildsys-win.bat" "%__nativeWindowsDir%" %__VSVersion% %__BuildArch%
popd

if not exist "%__IntermediatesDir%\install.vcxproj" goto :Failure

call msbuild "%__IntermediatesDir%\install.vcxproj" /t:rebuild /p:Configuration=%CMAKE_BUILD_TYPE% %__msbuildArgs%
IF ERRORLEVEL 1 (
    goto :Failure
)
echo Done building Native AOT components
exit /B 0

:BuildNativeEmscripten
pushd "%__IntermediatesDir%"
nmake install
popd
IF ERRORLEVEL 1 (
    goto :Failure
)

:: Copy results to native_aot since packaging expects a copy there too
mkdir "%__artifactsDir%\bin\native\%__outConfig%-aot"
copy "%__artifactsDir%\bin\native\%__outConfig%\*" "%__artifactsDir%\bin\native\%__outConfig%-aot"

exit /B 0

:Failure
:: Build failed
echo Failed to generate native component build project!
exit /b 1
