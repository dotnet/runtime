@if not defined _echo @echo off
setlocal EnableDelayedExpansion EnableExtensions

:: Define a prefix for most output progress messages that come from this script. That makes
:: it easier to see where these are coming from. Note that there is a trailing space here.
set "__MsgPrefix=BUILDTEST: "

echo %__MsgPrefix%Starting Build at %TIME%

set "__ProjectDir=%~dp0"
:: remove trailing slash
if %__ProjectDir:~-1%==\ set "__ProjectDir=%__ProjectDir:~0,-1%"
set "__RepoRootDir=%__ProjectDir%\..\.."
for %%i in ("%__RepoRootDir%") do SET "__RepoRootDir=%%~fi"

set "__TestDir=%__RepoRootDir%\src\tests"

:: Set the default arguments for build
set __BuildArch=x64
set __BuildType=Debug
set __TargetOS=windows

set "__ProjectFilesDir=%__TestDir%"
set "__RootBinDir=%__RepoRootDir%\artifacts"
set "__LogsDir=%__RootBinDir%\log"
set "__MsbuildDebugLogsDir=%__LogsDir%\MsbuildDebugLogs"

:: Default __Exclude to issues.targets
set __Exclude=%__RepoRootDir%\src\tests\issues.targets

REM __UnprocessedBuildArgs are args that we pass to msbuild (e.g. /p:TargetArchitecture=x64)
set "__args= %*"
set processedArgs=
set __UnprocessedBuildArgs=
set __CommonMSBuildArgs=

set __RebuildTests=0
set __BuildTestProject=%%3B
set __BuildTestDir=%%3B
set __BuildTestTree=%%3B

set __BuildLogRootName=TestBuild

set __SkipRestorePackages=0
set __SkipManaged=
set __SkipTestWrappers=
set __BuildTestWrappersOnly=
set __SkipNative=
set __CompositeBuildMode=
set __TestBuildMode=
set __CreatePdb=
set __CreatePerfmap=
set __CopyNativeTestBinaries=0
set __CopyNativeProjectsAfterCombinedTestBuild=true
set __SkipGenerateLayout=0
set __GenerateLayoutOnly=0
set __Ninja=1
set __CMakeArgs=

@REM CMD has a nasty habit of eating "=" on the argument list, so passing:
@REM    -priority=1
@REM appears to CMD parsing as "-priority 1". Handle -priority specially to avoid problems,
@REM and allow the "-priority=1" syntax.
set __Priority=0

set __BuildNeedTargetArg=

:Arg_Loop
if "%1" == "" goto ArgsDone

if /i "%1" == "/?"     goto Usage
if /i "%1" == "-?"     goto Usage
if /i "%1" == "/h"     goto Usage
if /i "%1" == "-h"     goto Usage
if /i "%1" == "/help"  goto Usage
if /i "%1" == "-help"  goto Usage
if /i "%1" == "--help" goto Usage

if /i "%1" == "x64"                   (set __BuildArch=x64&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "x86"                   (set __BuildArch=x86&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "arm"                   (set __BuildArch=arm&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "arm64"                 (set __BuildArch=arm64&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)

if /i "%1" == "debug"                 (set __BuildType=Debug&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "release"               (set __BuildType=Release&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "checked"               (set __BuildType=Checked&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)

if /i "%1" == "ci"                    (set __ArcadeScriptArgs="-ci"&set __ErrMsgPrefix=##vso[task.logissue type=error]&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)

if /i "%1" == "skiprestorepackages"   (set __SkipRestorePackages=1&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "skipmanaged"           (set __SkipManaged=1&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "skipnative"            (set __SkipNative=1&set __CopyNativeProjectsAfterCombinedTestBuild=false&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "skiptestwrappers"      (set __SkipTestWrappers=1&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "skipgeneratelayout"    (set __SkipGenerateLayout=1&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)

if /i "%1" == "rebuild"               (set __RebuildTests=1&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)

if /i "%1" == "test"                  (set __BuildTestProject=!__BuildTestProject!%2%%3B&set processedArgs=!processedArgs! %1 %2&shift&shift&goto Arg_Loop)
if /i "%1" == "dir"                   (set __BuildTestDir=!__BuildTestDir!%2%%3B&set processedArgs=!processedArgs! %1 %2&shift&shift&goto Arg_Loop)
if /i "%1" == "tree"                  (set __BuildTestTree=!__BuildTestTree!%2%%3B&set processedArgs=!processedArgs! %1 %2&shift&shift&goto Arg_Loop)

if /i "%1" == "log"                   (set __BuildLogRootName=%2&set processedArgs=!processedArgs! %1 %2&shift&shift&goto Arg_Loop)

if /i "%1" == "copynativeonly"        (set __CopyNativeTestBinaries=1&set __SkipNative=1&set __CopyNativeProjectsAfterCombinedTestBuild=false&set __SkipGenerateLayout=1&set __SkipTestWrappers=1&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "generatelayoutonly"    (set __SkipManaged=1&set __SkipNative=1&set __CopyNativeProjectsAfterCombinedTestBuild=false&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "buildtestwrappersonly" (set __SkipNative=1&set __SkipManaged=1&set __BuildTestWrappersOnly=1&set __SkipGenerateLayout=1&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "-cmakeargs"            (set __CMakeArgs="%2=%3" %__CMakeArgs%&set "processedArgs=!processedArgs! %1 %2=%3"&shift&shift&goto Arg_Loop)
if /i "%1" == "-msbuild"              (set __Ninja=0&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "buildagainstpackages"  (echo error: Remove /BuildAgainstPackages switch&&exit /b1)
if /i "%1" == "crossgen2"             (set __TestBuildMode=crossgen2&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "composite"             (set __CompositeBuildMode=1&set __TestBuildMode=crossgen2&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "pdb"                   (set __CreatePdb=1&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "nativeaot"             (set __TestBuildMode=nativeaot&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "perfmap"               (set __CreatePerfmap=1&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "Exclude"               (set __Exclude=%2&set processedArgs=!processedArgs! %1 %2&shift&shift&goto Arg_Loop)
if /i "%1" == "-priority"             (set __Priority=%2&shift&set processedArgs=!processedArgs! %1=%2&shift&goto Arg_Loop)
if /i "%1" == "allTargets"            (set "__BuildNeedTargetArg=/p:CLRTestBuildAllTargets=%1"&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "-excludemonofailures"  (set __Mono=1&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "-mono"                 (set __Mono=1&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "mono"                  (set __Mono=1&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "--"                    (set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)

if [!processedArgs!]==[] (
    set __UnprocessedBuildArgs=%__args%
) else (
    set __UnprocessedBuildArgs=%__args%
    for %%t in (!processedArgs!) do (
        set __UnprocessedBuildArgs=!__UnprocessedBuildArgs:*%%t=!
    )
)

:ArgsDone

@if defined _echo @echo on

echo %__MsgPrefix%Commencing CoreCLR test build

set "__OSPlatformConfig=%__TargetOS%.%__BuildArch%.%__BuildType%"
set "__BinDir=%__RootBinDir%\bin\coreclr\%__OSPlatformConfig%"
set "__TestRootDir=%__RootBinDir%\tests\coreclr"
set "__TestBinDir=%__TestRootDir%\%__OSPlatformConfig%"
set "__TestIntermediatesDir=%__TestRootDir%\obj\%__OSPlatformConfig%"

if "%__RebuildTests%" == "1" (
    echo Removing test build dir^: !__TestBinDir!
    rmdir /s /q !__TestBinDir!
    echo Removing test intermediate dir^: !__TestIntermediatesDir!
    rmdir /s /q !__TestIntermediatesDir!
)

REM We have different managed and native intermediate dirs because the managed bits will include
REM the configuration information deeper in the intermediates path.
REM These variables are used by the msbuild project files.

if not defined __TestIntermediateDir (
    set "__TestIntermediateDir=tests\coreclr\obj\%__TargetOS%.%__BuildArch%.%__BuildType%"
)
set "__NativeTestIntermediatesDir=%__RootBinDir%\%__TestIntermediateDir%\Native"
if "%__Ninja%"=="0" (set "__NativeTestIntermediatesDir=%__NativeTestIntermediatesDir%\ide")
set "__ManagedTestIntermediatesDir=%__RootBinDir%\%__TestIntermediateDir%\Managed"

REM Generate path to be set for CMAKE_INSTALL_PREFIX to contain forward slash
set "__CMakeBinDir=%__TestBinDir%"
set "__CMakeBinDir=%__CMakeBinDir:\=/%"

if not exist "%__TestBinDir%"                   md "%__TestBinDir%"
if not exist "%__NativeTestIntermediatesDir%"   md "%__NativeTestIntermediatesDir%"
if not exist "%__ManagedTestIntermediatesDir%"  md "%__ManagedTestIntermediatesDir%"
if not exist "%__LogsDir%"                      md "%__LogsDir%"
if not exist "%__MsbuildDebugLogsDir%"          md "%__MsbuildDebugLogsDir%"

REM Set up the directory for MSBuild debug logs.
set MSBUILDDEBUGPATH=%__MsbuildDebugLogsDir%

set __CommonMSBuildArgs="/p:TargetOS=%__TargetOS%"
set __CommonMSBuildArgs=%__CommonMSBuildArgs% "/p:Configuration=%__BuildType%"
set __CommonMSBuildArgs=%__CommonMSBuildArgs% "/p:TargetArchitecture=%__BuildArch%"

if "%__Mono%"=="1" (
  set __CommonMSBuildArgs=!__CommonMSBuildArgs! "/p:RuntimeFlavor=mono"
) else (
  set __CommonMSBuildArgs=!__CommonMSBuildArgs! "/p:RuntimeFlavor=coreclr"
)

if %__Ninja% == 0 (
    set __CommonMSBuildArgs=%__CommonMSBuildArgs% /p:UseVisualStudioNativeBinariesLayout=true
)

set __msbuildArgs=%__CommonMSBuildArgs% /nologo /verbosity:minimal /clp:Summary /maxcpucount %__UnprocessedBuildArgs%

echo Common MSBuild args: %__msbuildArgs%

call %__RepoRootDir%\eng\native\init-vs-env.cmd %__BuildArch%
if NOT '%ERRORLEVEL%' == '0' exit /b 1

REM =========================================================================================
REM ===
REM === Native test build section
REM ===
REM =========================================================================================

if "%__SkipNative%" == "1" goto skipnative
if "%__BuildTestWrappersOnly%" == "1" goto skipnative
if "%__GenerateLayoutOnly%" == "1" goto skipnative
if "%__CopyNativeTestBinaries%" == "1" goto skipnative

echo %__MsgPrefix%Commencing build of native test components for %__BuildArch%/%__BuildType%

REM Set the environment for the native build

REM NumberOfCores is an WMI property providing number of physical cores on machine
REM processor(s). It is used to set optimal level of CL parallelism during native build step
if not defined NumberOfCores (
    REM Determine number of physical processor cores available on machine
    set TotalNumberOfCores=0
    for /f "tokens=*" %%I in (
        'wmic cpu get NumberOfCores /value ^| find "=" 2^>NUL'
    ) do set %%I & set /a TotalNumberOfCores=TotalNumberOfCores+NumberOfCores
    set NumberOfCores=!TotalNumberOfCores!
)
echo %__MsgPrefix%Number of processor cores %NumberOfCores%

@if defined _echo @echo on

set __ExtraCmakeArgs=

if %__Ninja% EQU 1 (
    set __ExtraCmakeArgs="-DCMAKE_SYSTEM_VERSION=10.0 -DCMAKE_BUILD_TYPE=!__BuildType!"
) else (
    set __ExtraCmakeArgs="-DCMAKE_SYSTEM_VERSION=10.0"
)
call "%__RepoRootDir%\eng\native\gen-buildsys.cmd" "%__ProjectFilesDir%" "%__NativeTestIntermediatesDir%" %__VSVersion% %__BuildArch% !__ExtraCmakeArgs! !__CMakeArgs!

if not !errorlevel! == 0 (
    echo %__ErrMsgPrefix%%__MsgPrefix%Error: failed to generate native component build project!
    exit /b 1
)

@if defined _echo @echo on

if not exist "%__NativeTestIntermediatesDir%\CMakeCache.txt" (
    echo %__ErrMsgPrefix%%__MsgPrefix%Error: unable to find generated native component build project!
    exit /b 1
)

echo Environment setup

set __CmakeBuildToolArgs=

if %__Ninja% EQU 1 (
    set __CmakeBuildToolArgs=
) else (
    REM We pass the /m flag directly to MSBuild so that we can get both MSBuild and CL parallelism, which is fastest for our builds.
    set __CmakeBuildToolArgs=/nologo /m
)

"%CMakePath%" --build %__NativeTestIntermediatesDir% --target install --config %__BuildType% -- !__CmakeBuildToolArgs!

if errorlevel 1 (
    echo %__ErrMsgPrefix%%__MsgPrefix%Error: native test build failed.
    exit /b 1
)

:skipnative

REM =========================================================================================
REM ===
REM === Restore packages, build managed tests, generate layout and test wrappers, Crossgen framework
REM ===
REM =========================================================================================

set __BuildLog="%__LogsDir%\!__BuildLogRootName!_%__TargetOS%__%__BuildArch%__%__BuildType%.log"
set __BuildWrn="%__LogsDir%\!__BuildLogRootName!_%__TargetOS%__%__BuildArch%__%__BuildType%.wrn"
set __BuildErr="%__LogsDir%\!__BuildLogRootName!_%__TargetOS%__%__BuildArch%__%__BuildType%.err"
set __BuildBinLog="%__LogsDir%\!__BuildLogRootName!_%__TargetOS%__%__BuildArch%__%__BuildType%.binlog"
set __MsbuildLog=/flp:Verbosity=normal;LogFile=!__BuildLog!
set __MsbuildWrn=/flp1:WarningsOnly;LogFile=!__BuildWrn!
set __MsbuildErr=/flp2:ErrorsOnly;LogFile=!__BuildErr!
set __MsbuildBinLog=/bl:!__BuildBinLog!
set __Logging='!__MsbuildLog!' '!__MsbuildWrn!' '!__MsbuildErr!' '!__MsbuildBinLog!'

set BuildCommand=powershell -NoProfile -ExecutionPolicy ByPass -NoLogo -Command "%__RepoRootDir%\eng\common\msbuild.ps1" %__ArcadeScriptArgs%^
  %__RepoRootDir%\src\tests\build.proj -warnAsError:0 /t:TestBuild /nodeReuse:false^
  /p:RestoreDefaultOptimizationDataPackage=false /p:PortableBuild=true^
  /p:UsePartialNGENOptimization=false /maxcpucount %__Logging%^
  %__msbuildArgs%

echo %BuildCommand%
%BuildCommand%

if errorlevel 1 (
    echo %__ErrMsgPrefix%%__MsgPrefix%Error: Test build failed. Refer to the build log files for details:
    echo     %__BuildLog%
    echo     %__BuildWrn%
    echo     %__BuildErr%
    exit /b 1
)

echo { "build_os": "%__TargetOS%", "build_arch": "%__BuildArch%", "build_type": "%__BuildType%" } > "%__TestBinDir%/build_info.json"

REM =========================================================================================
REM ===
REM === All builds complete!
REM ===
REM =========================================================================================
:TestBuildDone

echo %__MsgPrefix%Test build succeeded.  Finished at %TIME%
echo %__MsgPrefix%Test binaries are available at !__TestBinDir!
exit /b 0

:Usage
echo.
echo Build the CoreCLR tests.
echo.
echo Usage:
echo     %0 [option1] [option2] ...
echo All arguments are optional. Options are case-insensitive. The options are:
echo.
echo.-? -h -help --help: view this message.
echo Build architecture: one of x64, x86, arm, arm64 ^(default: x64^).
echo Build type: one of Debug, Checked, Release ^(default: Debug^).
echo skipgeneratelayout: Do not generate the Core_Root layout
echo skipmanaged: skip the managed tests build
echo skipnative: skip the native tests build
echo skiprestorepackages: skip package restore
echo skiptestwrappers: skip generating test wrappers
echo buildtestwrappersonly: generate test wrappers without building managed or native test components or generating layouts
echo copynativeonly: Only copy the native test binaries to the managed output. Do not build the native or managed tests.
echo crossgen2: Precompiles the framework managed assemblies
echo composite: Precompiles the framework managed assemblies in composite build mode
echo pdb: create PDB files when precompiling the framework managed assemblies
echo generatelayoutonly: Generate the Core_Root layout without building managed or native test components
echo Exclude- Optional parameter - specify location of default exclusion file ^(defaults to tests\issues.targets if not specified^)
echo     Set to "" to disable default exclusion file.
echo -- ... : all arguments following this tag will be passed directly to msbuild.
echo -priority=^<N^> : specify a set of tests that will be built and run, with priority N.
echo     0: Build only priority 0 cases as essential testcases (default)
echo     1: Build all tests with priority 0 and 1
echo     666: Build all tests with priority 0, 1 ... 666
echo test ^<xxx^>: Only build test project ^<xxx^> ^(relative or absolute project path under src\tests^)
echo dir ^<xxx^>: Build all test projects in the folder ^<xxx^> ^(relative or absolute folder under src\tests^)
echo tree ^<xxx^>: Build all test projects in the subtree ^<xxx^> ^(relative or absolute folder under src\tests^)
echo rebuild: Clean up all test artifacts prior to building tests
echo allTargets: Build managed tests for all target platforms (including test projects in which CLRTestTargetUnsupported resolves to true)
echo -verbose: enables detailed file logging for the msbuild tasks into the msbuild log file.
echo log: base file name to use for log files (used in lab pipelines that build tests in multiple steps to retain logs for each step)
exit /b 1

REM Exit_Failure:
REM This is necessary because of a(n apparent) bug in the FOR /L command.  Under certain circumstances,
REM such as when this script is invoke with CMD /C "build.cmd", a non-zero exit directly from
REM within the loop body will not propagate to the caller.  For some reason, goto works around it.
:Exit_Failure
exit /b 1
