@if not defined _echo @echo off
setlocal

:SetupArgs
:: Initialize the args that will be passed to cmake
set __nativeWindowsDir=%~dp0\Windows
set __binDir=%~dp0..\..\bin
set __rootDir=%~dp0..\..
set __CMakeBinDir=""
set __IntermediatesDir=""
set __BuildArch=x64
set __appContainer=""
set __VCBuildArch=x86_amd64
set CMAKE_BUILD_TYPE=Debug
set "__LinkArgs= "
set "__LinkLibraries= "

call %__rootDir%/run.cmd build-managed -GenerateVersion -project=%__rootDir%/build.proj

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
if /i [%1] == [arm64]       ( set __BuildArch=arm64&&set __VCBuildArch=arm64&&shift&goto Arg_Loop)

if /i [%1] == [toolsetDir]  ( set "__ToolsetDir=%2"&&shift&&shift&goto Arg_Loop)

shift
goto :Arg_Loop

:ToolsVersion
:: Determine the tools version to pass to cmake/msbuild
if not defined VisualStudioVersion (
    if defined VS140COMNTOOLS (
        goto :VS2015
    ) 
    goto :MissingVersion
) 
if "%VisualStudioVersion%"=="14.0" (
    goto :VS2015
) 

:MissingVersion
:: Can't find VS 2013+
echo Error: Visual Studio 2015 required  
echo        Please see https://github.com/dotnet/corefx/blob/master/Documentation/project-docs/developer-guide.md for build instructions.
exit /b 1

:VS2015
:: Setup vars for VS2015
set __VSVersion=vs2015
set __PlatformToolset=v140
if NOT "%__BuildArch%" == "arm64" ( 
    :: Set the environment for the native build
    call "%VS140COMNTOOLS%\..\..\VC\vcvarsall.bat" %__VCBuildArch%
)
goto :SetupDirs

:SetupDirs
:: Setup to cmake the native components
echo Commencing build of native components
echo.

if %__CMakeBinDir% == "" (
    set "__CMakeBinDir=%__binDir%\Windows_NT.%__BuildArch%.%CMAKE_BUILD_TYPE%\Native"
)
if %__IntermediatesDir% == "" (
    set "__IntermediatesDir=%__binDir%\obj\Windows_NT.%__BuildArch%.%CMAKE_BUILD_TYPE%\Native"
)
set "__CMakeBinDir=%__CMakeBinDir:\=/%"
set "__IntermediatesDir=%__IntermediatesDir:\=/%"

:: Check that the intermediate directory exists so we can place our cmake build tree there
if exist "%__IntermediatesDir%" rd /s /q "%__IntermediatesDir%"
if not exist "%__IntermediatesDir%" md "%__IntermediatesDir%"

if exist "%VSINSTALLDIR%DIA SDK" goto GenVSSolution
echo Error: DIA SDK is missing at "%VSINSTALLDIR%DIA SDK". ^
This is due to a bug in the Visual Studio installer. It does not install DIA SDK at "%VSINSTALLDIR%" but rather ^
at VS install location of previous version. Workaround is to copy DIA SDK folder from VS install location ^
of previous version to "%VSINSTALLDIR%" and then resume build.
:: DIA SDK not included in Express editions
echo Visual Studio 2013 Express does not include the DIA SDK. ^
You need Visual Studio 2013+ (Community is free).
echo See: https://github.com/dotnet/coreclr/blob/master/Documentation/project-docs/developer-guide.md#prerequisites
exit /b 1

:GenVSSolution
:: Regenerate the VS solution

if /i "%__BuildArch%" == "arm64" ( 
    REM arm64 builds currently use private toolset which has not been released yet
    REM TODO, remove once the toolset is open.
    call :PrivateToolSet
)

pushd "%__IntermediatesDir%"
call "%__nativeWindowsDir%\gen-buildsys-win.bat" %__nativeWindowsDir% %__VSVersion% %__BuildArch%
popd

:CheckForProj
:: Check that the project created by Cmake exists
if exist "%__IntermediatesDir%\install.vcxproj" goto BuildNativeProj
goto :Failure

:BuildNativeProj
:: Build the project created by Cmake
if "%__BuildArch%" == "arm64" (
    set __msbuildArgs=/p:UseEnv=true
) else (
    set __msbuildArgs=/p:Platform=%__BuildArch% /p:PlatformToolset="%__PlatformToolset%"
)

call %__rootDir%/run.cmd build-managed -project="%__IntermediatesDir%\install.vcxproj" -- /t:rebuild /p:Configuration=%CMAKE_BUILD_TYPE% %__msbuildArgs%
IF ERRORLEVEL 1 (
    goto :Failure
)
echo Done building Native components

:BuildNativeAOT
set "__CMakeBinDir=%__binDir%\Windows_NT.%__BuildArch%.%CMAKE_BUILD_TYPE%\Native_aot"
set "__IntermediatesDir=%__binDir%\obj\Windows_NT.%__BuildArch%.%CMAKE_BUILD_TYPE%\Native_aot"
set "__CMakeBinDir=%__CMakeBinDir:\=/%"
set "__IntermediatesDir=%__IntermediatesDir:\=/%"
if exist "%__IntermediatesDir%" rd /s /q "%__IntermediatesDir%"
if not exist "%__IntermediatesDir%" md "%__IntermediatesDir%"
set "__LinkArgs=%__LinkArgs% /APPCONTAINER"
set "__appContainer=true"

pushd "%__IntermediatesDir%"
call "%__nativeWindowsDir%\gen-buildsys-win.bat" %__nativeWindowsDir% %__VSVersion% %__BuildArch%
popd

if not exist "%__IntermediatesDir%\install.vcxproj" goto :Failure

call %__rootDir%/run.cmd build-managed -project="%__IntermediatesDir%\install.vcxproj" -- /t:rebuild /p:Configuration=%CMAKE_BUILD_TYPE% %__msbuildArgs%
IF ERRORLEVEL 1 (
    goto :Failure
)
echo Done building Native AOT components
exit /B 0

:Failure
:: Build failed
echo Failed to generate native component build project!
exit /b 1

:PrivateToolSet
echo %__MsgPrefix% Setting Up the usage of __ToolsetDir:%__ToolsetDir%

if /i "%__ToolsetDir%" == "" (
    echo %__MsgPrefix%Error: A toolset directory is required for the Arm64 Windows build. Use the toolsetDir argument.
    exit /b 1
)

set PATH=%__ToolsetDir%\VC_sdk\bin;%PATH%
set LIB=%__ToolsetDir%\VC_sdk\lib\arm64;%__ToolsetDir%\sdpublic\sdk\lib\arm64
set INCLUDE=^
%__ToolsetDir%\VC_sdk\inc;^
%__ToolsetDir%\sdpublic\sdk\inc;^
%__ToolsetDir%\sdpublic\shared\inc;^
%__ToolsetDir%\sdpublic\shared\inc\minwin;^
%__ToolsetDir%\sdpublic\sdk\inc\ucrt;^
%__ToolsetDir%\sdpublic\sdk\inc\minwin;^
%__ToolsetDir%\sdpublic\sdk\inc\mincore;^
%__ToolsetDir%\sdpublic\sdk\inc\abi;^
%__ToolsetDir%\sdpublic\sdk\inc\clientcore;^
%__ToolsetDir%\diasdk\include
exit /b 0
