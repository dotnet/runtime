@if not defined _echo @echo off
setlocal

:SetupArgs
:: Initialize the args that will be passed to cmake
set "__sourceRootDir=%~dp0"
:: remove trailing slash
if %__sourceRootDir:~-1%==\ set "__sourceRootDir=%__sourceRootDir:~0,-1%"
set "__repoRoot=%__sourceRootDir%\..\..\.."
:: normalize
for %%i in ("%__repoRoot%") do set "__repoRoot=%%~fi"
set "__engNativeDir=%__repoRoot%\eng\native"
set "__artifactsDir=%__repoRoot%\artifacts"
set __CMakeBinDir=""
set __IntermediatesDir=""
set __BuildArch=x64
set __BuildTarget="build"
set __TargetOS=windows
set CMAKE_BUILD_TYPE=Debug
set __Ninja=1
set __icuDir=""
set __usePThreads=0

:Arg_Loop
:: Since the native build requires some configuration information before msbuild is called, we have to do some manual args parsing
if [%1] == [] goto :InitVSEnv
if /i [%1] == [Release]     ( set CMAKE_BUILD_TYPE=Release&&shift&goto Arg_Loop)
if /i [%1] == [Debug]       ( set CMAKE_BUILD_TYPE=Debug&&shift&goto Arg_Loop)

if /i [%1] == [AnyCPU]      ( set __BuildArch=x64&&shift&goto Arg_Loop)
if /i [%1] == [x86]         ( set __BuildArch=x86&&shift&goto Arg_Loop)
if /i [%1] == [arm]         ( set __BuildArch=arm&&shift&goto Arg_Loop)
if /i [%1] == [x64]         ( set __BuildArch=x64&&shift&goto Arg_Loop)
if /i [%1] == [amd64]       ( set __BuildArch=x64&&shift&goto Arg_Loop)
if /i [%1] == [arm64]       ( set __BuildArch=arm64&&shift&goto Arg_Loop)
if /i [%1] == [wasm]        ( set __BuildArch=wasm&&shift&goto Arg_Loop)

if /i [%1] == [outconfig] ( set __outConfig=%2&&shift&&shift&goto Arg_Loop)

if /i [%1] == [browser] ( set __TargetOS=browser&&shift&goto Arg_Loop)
if /i [%1] == [wasi] ( set __TargetOS=wasi&&shift&goto Arg_Loop)

if /i [%1] == [rebuild] ( set __BuildTarget=rebuild&&shift&goto Arg_Loop)

if /i [%1] == [msbuild] ( set __Ninja=0&&shift&goto Arg_Loop)

if /i [%1] == [icudir] ( set __icuDir=%2&&shift&&shift&goto Arg_Loop)
if /i [%1] == [usepthreads] ( set __usePThreads=1&&shift&goto Arg_Loop)

shift
goto :Arg_Loop

:InitVSEnv
call "%__engNativeDir%\init-vs-env.cmd" %__BuildArch%
if NOT [%errorlevel%] == [0] goto :Failure

:: Setup to cmake the native components
echo Commencing build of native components
echo.

call "%__engNativeDir%\version\copy_version_files.cmd"

:: cmake requires forward slashes in paths
set __cmakeRepoRoot=%__repoRoot:\=/%
set __ExtraCmakeParams="-DCMAKE_REPO_ROOT=%__cmakeRepoRoot%"
set __ExtraCmakeParams=%__ExtraCmakeParams% "-DCMAKE_BUILD_TYPE=%CMAKE_BUILD_TYPE%"

if NOT %__icuDir% == "" (
    set __ExtraCmakeParams=%__ExtraCmakeParams% "-DCMAKE_ICU_DIR=%__icuDir%"
)
set __ExtraCmakeParams=%__ExtraCmakeParams% "-DCMAKE_USE_PTHREADS=%__usePThreads%"


if [%__outConfig%] == [] set __outConfig=%__TargetOS%-%__BuildArch%-%CMAKE_BUILD_TYPE%

if %__CMakeBinDir% == "" (
    set "__CMakeBinDir=%__artifactsDir%\bin\native\%__outConfig%"
)
if %__IntermediatesDir% == "" (
    set "__IntermediatesDir=%__artifactsDir%\obj\native\%__outConfig%"
)
if %__Ninja% == 0 (
    set "__IntermediatesDir=%__IntermediatesDir%\ide"
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

:: Regenerate the VS solution

pushd "%__IntermediatesDir%"
call "%__repoRoot%\eng\native\gen-buildsys.cmd" "%__sourceRootDir%" "%__IntermediatesDir%" %__VSVersion% %__BuildArch% %__TargetOS% %__ExtraCmakeParams%
if NOT [%errorlevel%] == [0] goto :Failure
popd

:BuildNativeProj
:: Build the project created by Cmake
set __generatorArgs=
if [%__Ninja%] == [1] (
    set __generatorArgs=
) else if [%__TargetOS%] == [browser] (
    set __generatorArgs=
) else if [%__TargetOS%] == [wasi] (
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
