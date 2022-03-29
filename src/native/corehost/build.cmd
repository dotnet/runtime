@if not defined _echo @echo off
setlocal

:SetupArgs
:: Initialize the args that will be passed to cmake
set "__sourceDir=%~dp0"
:: remove trailing slash
if %__sourceDir:~-1%==\ set "__ProjectDir=%__sourceDir:~0,-1%"

set __engNativeDir=%__sourceDir%\..\..\..\eng\native
set __CMakeBinDir=""
set __IntermediatesDir=""
set __BuildArch=x64
set CMAKE_BUILD_TYPE=Debug
set __PortableBuild=0
set __ConfigureOnly=0
set __IncrementalNativeBuild=0
set __Ninja=1

:Arg_Loop
if [%1] == [] goto :InitVSEnv
if /i [%1] == [Release]     ( set CMAKE_BUILD_TYPE=Release&&shift&goto Arg_Loop)
if /i [%1] == [Debug]       ( set CMAKE_BUILD_TYPE=Debug&&shift&goto Arg_Loop)

if /i [%1] == [AnyCPU]      ( set __BuildArch=x64&&shift&goto Arg_Loop)
if /i [%1] == [x86]         ( set __BuildArch=x86&&shift&goto Arg_Loop)
if /i [%1] == [arm]         ( set __BuildArch=arm&&shift&goto Arg_Loop)
if /i [%1] == [x64]         ( set __BuildArch=x64&&shift&goto Arg_Loop)
if /i [%1] == [amd64]       ( set __BuildArch=x64&&shift&goto Arg_Loop)
if /i [%1] == [arm64]       ( set __BuildArch=arm64&&shift&goto Arg_Loop)

if /i [%1] == [portable]    ( set __PortableBuild=1&&shift&goto Arg_Loop)
if /i [%1] == [rid]         ( set __TargetRid=%2&&shift&&shift&goto Arg_Loop)
if /i [%1] == [toolsetDir]  ( set "__ToolsetDir=%2"&&shift&&shift&goto Arg_Loop)
if /i [%1] == [hostver]     (set __HostVersion=%2&&shift&&shift&goto Arg_Loop)
if /i [%1] == [apphostver]  (set __AppHostVersion=%2&&shift&&shift&goto Arg_Loop)
if /i [%1] == [fxrver]      (set __HostFxrVersion=%2&&shift&&shift&goto Arg_Loop)
if /i [%1] == [policyver]   (set __HostPolicyVersion=%2&&shift&&shift&goto Arg_Loop)
if /i [%1] == [commit]      (set __CommitSha=%2&&shift&&shift&goto Arg_Loop)

if /i [%1] == [configureonly] ( set __ConfigureOnly=1&&shift&goto Arg_Loop)
if /i [%1] == [incremental-native-build] ( set __IncrementalNativeBuild=1&&shift&goto Arg_Loop)
if /i [%1] == [rootDir]     ( set __rootDir=%2&&shift&&shift&goto Arg_Loop)
if /i [%1] == [msbuild] (set __Ninja=0)
if /i [%1] == [runtimeflavor]  (set __RuntimeFlavor=%2&&shift&&shift&goto Arg_Loop)
if /i [%1] == [runtimeconfiguration]  (set __RuntimeConfiguration=%2&&shift&&shift&goto Arg_Loop)

shift
goto :Arg_Loop

:InitVSEnv
call "%__engNativeDir%\init-vs-env.cmd" %__BuildArch%
if NOT [%errorlevel%] == [0] goto :Failure

if [%__rootDir%] == [] (
    echo Root directory must be provided via the rootDir parameter.
    exit /b 1
)

set __binDir=%__rootDir%\artifacts\bin
set __objDir=%__rootDir%\artifacts\obj

:: Setup to cmake the native components
echo Configuring corehost native components
echo.

if %__CMakeBinDir% == "" (
    set "__CMakeBinDir=%__binDir%\%__TargetRid%.%CMAKE_BUILD_TYPE%"
)

if %__IntermediatesDir% == "" (
    set "__IntermediatesDir=%__objDir%\%__TargetRid%.%CMAKE_BUILD_TYPE%\corehost"
)
if %__Ninja% == 0 (
    set "__IntermediatesDir=%__IntermediatesDir%\ide"
)
set "__ResourcesDir=%__objDir%\%__TargetRid%.%CMAKE_BUILD_TYPE%\hostResourceFiles"
set "__CMakeBinDir=%__CMakeBinDir:\=/%"
set "__IntermediatesDir=%__IntermediatesDir:\=/%"


:: Check that the intermediate directory exists so we can place our cmake build tree there
if /i "%__IncrementalNativeBuild%" == "1" goto CreateIntermediates
if exist "%__IntermediatesDir%" rd /s /q "%__IntermediatesDir%"

:CreateIntermediates
if not exist "%__IntermediatesDir%" md "%__IntermediatesDir%"

if /i "%__BuildArch%" == "x64"     (set cm_BaseRid=win7)
if /i "%__BuildArch%" == "x86"     (set cm_BaseRid=win7)
if /i "%__BuildArch%" == "arm"     (set cm_BaseRid=win8)
if /i "%__BuildArch%" == "arm64"   (set cm_BaseRid=win10)
:: Form the base RID to be used if we are doing a portable build
if /i "%__PortableBuild%" == "1"   (set cm_BaseRid=win)
set cm_BaseRid=%cm_BaseRid%-%__BuildArch%
echo "Computed RID for native build is %cm_BaseRid%"

set __ExtraCmakeParams=%__ExtraCmakeParams% "-DCLI_CMAKE_HOST_VER=%__HostVersion%" "-DCLI_CMAKE_COMMON_HOST_VER=%__AppHostVersion%" "-DCLI_CMAKE_HOST_FXR_VER=%__HostFxrVersion%"
set __ExtraCmakeParams=%__ExtraCmakeParams% "-DCLI_CMAKE_HOST_POLICY_VER=%__HostPolicyVersion%" "-DCLI_CMAKE_PKG_RID=%cm_BaseRid%" "-DCLI_CMAKE_COMMIT_HASH=%__CommitSha%"
set __ExtraCmakeParams=%__ExtraCmakeParams% "-DRUNTIME_FLAVOR=%__RuntimeFlavor% "
set __ExtraCmakeParams=%__ExtraCmakeParams% "-DCLI_CMAKE_RESOURCE_DIR=%__ResourcesDir%" "-DCMAKE_BUILD_TYPE=%CMAKE_BUILD_TYPE%"

:: Regenerate the native build files
echo Calling "%__engNativeDir%\gen-buildsys.cmd "%__sourceDir%" "%__IntermediatesDir%" %__VSVersion% %__BuildArch% %__ExtraCmakeParams%"

call "%__engNativeDir%\gen-buildsys.cmd" "%__sourceDir%" "%__IntermediatesDir%" %__VSVersion% %__BuildArch% %__ExtraCmakeParams%
if NOT [%errorlevel%] == [0] goto :Failure
popd

if [%__ConfigureOnly%] == [1] goto :Exit

:BuildNativeProj

echo Commencing build of corehost native components

:: Build the project created by Cmake
set __generatorArgs=
if [%__Ninja%] == [1] (
    set __generatorArgs=
) else if [%__BuildArch%] == [wasm] (
    set __generatorArgs=-j
) else (
    set __generatorArgs=/p:Platform=%__BuildArch% /p:PlatformToolset="%__PlatformToolset%" -noWarn:MSB8065
)

call "%CMakePath%" --build "%__IntermediatesDir%" --target install --config %CMAKE_BUILD_TYPE% -- %__generatorArgs%
IF ERRORLEVEL 1 (
    goto :Failure
)

echo Done building Native components

:Exit
exit /B 0

:Failure
:: Build failed
echo Failed to generate native component build project!
exit /b 1
