@if not defined _echo @echo off
setlocal

:SetupArgs
:: Initialize the args that will be passed to cmake
set __nativeWindowsDir=%~dp0Windows
set __binDir=%~dp0..\..\Bin
set __rootDir=%~dp0..\..
set __CMakeBinDir=""
set __IntermediatesDir=""
set __BuildArch=x64
set __appContainer=""
set __VCBuildArch=x86_amd64
set CMAKE_BUILD_TYPE=Debug
set "__LinkArgs= "
set "__LinkLibraries= "
set __PortableBuild=0

:Arg_Loop
if [%1] == [] goto :ToolsVersion
if /i [%1] == [Release]     ( set CMAKE_BUILD_TYPE=Release&&shift&goto Arg_Loop)
if /i [%1] == [Debug]       ( set CMAKE_BUILD_TYPE=Debug&&shift&goto Arg_Loop)

if /i [%1] == [AnyCPU]      ( set __BuildArch=x64&&set __VCBuildArch=x86_amd64&&shift&goto Arg_Loop)
if /i [%1] == [x86]         ( set __BuildArch=x86&&set __VCBuildArch=x86&&shift&goto Arg_Loop)
if /i [%1] == [arm]         ( set __BuildArch=arm&&set __VCBuildArch=x86_arm&&set __SDKVersion="-DCMAKE_SYSTEM_VERSION=10.0"&&shift&goto Arg_Loop)
if /i [%1] == [x64]         ( set __BuildArch=x64&&set __VCBuildArch=x86_amd64&&shift&goto Arg_Loop)
if /i [%1] == [amd64]       ( set __BuildArch=x64&&set __VCBuildArch=x86_amd64&&shift&goto Arg_Loop)
if /i [%1] == [arm64]       ( set __BuildArch=arm64&&set __VCBuildArch=arm64&&shift&goto Arg_Loop)

if /i [%1] == [portable]    ( set __PortableBuild=1&&shift&goto Arg_Loop)
if /i [%1] == [rid]         ( set __TargetRid=%2&&shift&&shift&goto Arg_Loop)
if /i [%1] == [toolsetDir]  ( set "__ToolsetDir=%2"&&shift&&shift&goto Arg_Loop)
if /i [%1] == [hostver]     (set __HostVersion=%2&&shift&&shift&goto Arg_Loop)
if /i [%1] == [apphostver]  (set __AppHostVersion=%2&&shift&&shift&goto Arg_Loop)
if /i [%1] == [fxrver]      (set __HostResolverVersion=%2&&shift&&shift&goto Arg_Loop)
if /i [%1] == [policyver]   (set __HostPolicyVersion=%2&&shift&&shift&goto Arg_Loop)
if /i [%1] == [commit]      (set __CommitSha=%2&&shift&&shift&goto Arg_Loop)

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
echo Commencing build of corehost
echo.

if %__CMakeBinDir% == "" (
    set "__CMakeBinDir=%__binDir%\%__TargetRid%.%CMAKE_BUILD_TYPE%\corehost"
)
if %__IntermediatesDir% == "" (
    set "__IntermediatesDir=%__binDir%\obj\%__TargetRid%.%CMAKE_BUILD_TYPE%\corehost"
)
set "__ResourcesDir=%__binDir%\obj\%__TargetRid%.%CMAKE_BUILD_TYPE%\hostResourceFiles%
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

echo Calling "%__nativeWindowsDir%\gen-buildsys-win.bat %~dp0 %__VSVersion% %__BuildArch% %__CommitSha% %__HostVersion% %__AppHostVersion% %__HostResolverVersion% %__HostPolicyVersion%"
pushd "%__IntermediatesDir%"
call "%__nativeWindowsDir%\gen-buildsys-win.bat" %~dp0 %__VSVersion% %__BuildArch% %__CommitSha% %__HostVersion% %__AppHostVersion% %__HostResolverVersion% %__HostPolicyVersion% %__PortableBuild%
popd

:CheckForProj
:: Check that the project created by Cmake exists
if exist "%__IntermediatesDir%\ALL_BUILD.vcxproj" goto BuildNativeProj
goto :Failure

:BuildNativeProj
:: Build the project created by Cmake
if "%__BuildArch%" == "arm64" (
    set __msbuildArgs=/p:UseEnv=true
) else (
    set __msbuildArgs=/p:Platform=%__BuildArch% /p:PlatformToolset="%__PlatformToolset%"
)

cd %__rootDir%

echo %__rootDir%\run.cmd build-native -- "%__IntermediatesDir%\ALL_BUILD.vcxproj" /t:rebuild /p:Configuration=%CMAKE_BUILD_TYPE% %__msbuildArgs%
call %__rootDir%\run.cmd build-native -- "%__IntermediatesDir%\ALL_BUILD.vcxproj" /t:rebuild /p:Configuration=%CMAKE_BUILD_TYPE% %__msbuildArgs%
IF ERRORLEVEL 1 (
    goto :Failure
)
echo Done building Native components
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