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
:: normalize
for %%i in ("%__RepoRootDir%") do set "__RepoRootDir=%%~fi"

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
set __commandName=%~nx0
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
set __Priority=0

set __BuildNeedTargetArg=

:Arg_Loop
if "%1" == "" goto ArgsDone

@REM All arguments following this tag will be passed directly to msbuild (as unprocessed arguments)
if /i "%1" == "--"                       (set processedArgs=!processedArgs! %1&shift&goto ArgsDone)

@REM The following arguments do not support '/', '-', or '--' prefixes
if /i "%1" == "x64"                      (set __BuildArch=x64&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "x86"                      (set __BuildArch=x86&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "arm64"                    (set __BuildArch=arm64&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)

if /i "%1" == "debug"                    (set __BuildType=Debug&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "release"                  (set __BuildType=Release&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "checked"                  (set __BuildType=Checked&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)

if /i "%1" == "ci"                       (set __ArcadeScriptArgs="-ci"&set __ErrMsgPrefix=##vso[task.logissue type=error]&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)

@REM For the arguments below, we support '/', '-', and '--' prefixes.
@REM But we also recognize them without prefixes at all. To achieve that,
@REM we remove the '/' and '-' characters from the string for comparison.

set arg=%~1
set arg=%arg:/=%
set arg=%arg:-=%

if /i "%arg%" == "?"     goto Usage
if /i "%arg%" == "h"     goto Usage
if /i "%arg%" == "help"  goto Usage

@REM Specify this argument to test the argument parsing logic of this script without executing the build
if /i "%arg%" == "TestArgParsing"        (set __TestArgParsing=1&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)

@REM The following arguments are switches that do not consume any subsequent arguments
if /i "%arg%" == "Rebuild"               (set __RebuildTests=1&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%arg%" == "SkipRestorePackages"   (set __SkipRestorePackages=1&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%arg%" == "SkipManaged"           (set __SkipManaged=1&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%arg%" == "SkipNative"            (set __SkipNative=1&set __CopyNativeProjectsAfterCombinedTestBuild=false&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%arg%" == "SkipTestWrappers"      (set __SkipTestWrappers=1&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%arg%" == "SkipGenerateLayout"    (set __SkipGenerateLayout=1&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)

if /i "%arg%" == "CopyNativeOnly"        (set __CopyNativeTestBinaries=1&set __SkipNative=1&set __CopyNativeProjectsAfterCombinedTestBuild=false&set __SkipGenerateLayout=1&set __SkipTestWrappers=1&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%arg%" == "GenerateLayoutOnly"    (set __GenerateLayoutOnly=1&set __SkipManaged=1&set __SkipNative=1&set __CopyNativeProjectsAfterCombinedTestBuild=false&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%arg%" == "BuildTestWrappersOnly" (set __SkipNative=1&set __SkipManaged=1&set __BuildTestWrappersOnly=1&set __SkipGenerateLayout=1&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%arg%" == "MSBuild"               (set __Ninja=0&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%arg%" == "crossgen2"             (set __TestBuildMode=crossgen2&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%arg%" == "composite"             (set __CompositeBuildMode=1&set __TestBuildMode=crossgen2&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%arg%" == "pdb"                   (set __CreatePdb=1&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%arg%" == "NativeAOT"             (set __TestBuildMode=nativeaot&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%arg%" == "Perfmap"               (set __CreatePerfmap=1&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%arg%" == "AllTargets"            (set "__BuildNeedTargetArg=/p:CLRTestBuildAllTargets=allTargets"&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%arg%" == "ExcludeMonoFailures"   (set __Mono=1&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%arg%" == "Mono"                  (set __Mono=1&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)

@REM The following arguments also consume one subsequent argument
if /i "%arg%" == "test"                  (set __BuildTestProject=!__BuildTestProject!%2%%3B&set processedArgs=!processedArgs! %1 %2&shift&shift&goto Arg_Loop)
if /i "%arg%" == "dir"                   (set __BuildTestDir=!__BuildTestDir!%2%%3B&set processedArgs=!processedArgs! %1 %2&shift&shift&goto Arg_Loop)
if /i "%arg%" == "tree"                  (set __BuildTestTree=!__BuildTestTree!%2%%3B&set processedArgs=!processedArgs! %1 %2&shift&shift&goto Arg_Loop)
if /i "%arg%" == "log"                   (set __BuildLogRootName=%2&set processedArgs=!processedArgs! %1 %2&shift&shift&goto Arg_Loop)
if /i "%arg%" == "exclude"               (set __Exclude=%2&set processedArgs=!processedArgs! %1 %2&shift&shift&goto Arg_Loop)
if /i "%arg%" == "priority"              (set __Priority=%2&set processedArgs=!processedArgs! %1 %2&shift&shift&goto Arg_Loop)

@REM The following arguments also consume two subsequent arguments
if /i "%arg%" == "CMakeArgs"             (set __CMakeArgs="%2=%3" %__CMakeArgs%&set "processedArgs=!processedArgs! %1 %2 %3"&shift&shift&shift&goto Arg_Loop)

@REM Obsolete arguments that now produce errors
if /i "%arg%" == "BuildAgainstPackages"  (echo error: Remove /BuildAgainstPackages switch&&exit /b 1)

@REM If we encounter an unrecognized argument, then all remaining arguments are passed directly to MSBuild
@REM This allows '/p:LibrariesConfiguration=Release' and other arguments to be passed through without having
@REM '/p:LibrariesConfiguration' and 'Release' get handled as separate arguments.

:ArgsDone

if [!processedArgs!]==[] (
    set __UnprocessedBuildArgs=%__args%
) else (
    set __UnprocessedBuildArgs=%__args%
    for %%t in (!processedArgs!) do (
        set __UnprocessedBuildArgs=!__UnprocessedBuildArgs:*%%t=!
    )
)

if defined __TestArgParsing (
    echo.
    echo.args: "%__args%"
    echo.
    echo.PROCESSED ARGS: "%processedArgs%"
    echo.
    echo.UNPROCESSED ARGS: "%__UnprocessedBuildArgs%"
    echo.
    echo.__BuildArch=%__BuildArch%
    echo.__BuildType=%__BuildType%
    echo.__Exclude=%__Exclude%
    echo.__RebuildTests=%__RebuildTests%
    echo.__BuildTestProject=%__BuildTestProject%
    echo.__BuildTestDir=%__BuildTestDir%
    echo.__BuildTestTree=%__BuildTestTree%
    echo.__BuildLogRootName=%__BuildLogRootName%
    echo.__SkipRestorePackages=%__SkipRestorePackages%
    echo.__SkipManaged=%__SkipManaged%
    echo.__SkipTestWrappers=%__SkipTestWrappers%
    echo.__BuildTestWrappersOnly=%__BuildTestWrappersOnly%
    echo.__SkipNative=%__SkipNative%
    echo.__CompositeBuildMode=%__CompositeBuildMode%
    echo.__TestBuildMode=%__TestBuildMode%
    echo.__CreatePdb=%__CreatePdb%
    echo.__CreatePerfmap=%__CreatePerfmap%
    echo.__CopyNativeTestBinaries=%__CopyNativeTestBinaries%
    echo.__CopyNativeProjectsAfterCombinedTestBuild=%__CopyNativeProjectsAfterCombinedTestBuild%
    echo.__SkipGenerateLayout=%__SkipGenerateLayout%
    echo.__GenerateLayoutOnly=%__GenerateLayoutOnly%
    echo.__Ninja=%__Ninja%
    echo.__CMakeArgs=%__CMakeArgs%
    echo.__Priority=%__Priority%
    echo.
)

@if defined _echo @echo on

echo %__MsgPrefix%Commencing CoreCLR test build

set "__OSPlatformConfig=%__TargetOS%.%__BuildArch%.%__BuildType%"
set "__BinDir=%__RootBinDir%\bin\coreclr\%__OSPlatformConfig%"
set "__TestRootDir=%__RootBinDir%\tests\coreclr"
set "__TestBinDir=%__TestRootDir%\%__OSPlatformConfig%"
set "__TestIntermediatesDir=%__TestRootDir%\obj\%__OSPlatformConfig%"

if "%__RebuildTests%" == "1" (
    echo %__MsgPrefix%Removing test build dir^: !__TestBinDir!
    rmdir /s /q !__TestBinDir!
    echo %__MsgPrefix%Removing test intermediate dir^: !__TestIntermediatesDir!
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

set __msbuildArgs=%__CommonMSBuildArgs% /nologo /verbosity:minimal /clp:Summary /maxcpucount %__BuildNeedTargetArg% %__UnprocessedBuildArgs%

echo %__MsgPrefix%Common MSBuild args: %__msbuildArgs%

if defined __TestArgParsing (
    EXIT /b 0
)

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
call "%__RepoRootDir%\eng\native\gen-buildsys.cmd" "%__ProjectFilesDir%" "%__NativeTestIntermediatesDir%" %__VSVersion% %__BuildArch% %__TargetOS% !__ExtraCmakeArgs! !__CMakeArgs!

if not !errorlevel! == 0 (
    echo %__ErrMsgPrefix%%__MsgPrefix%Error: failed to generate native component build project!
    exit /b 1
)

@if defined _echo @echo on

if not exist "%__NativeTestIntermediatesDir%\CMakeCache.txt" (
    echo %__ErrMsgPrefix%%__MsgPrefix%Error: unable to find generated native component build project!
    exit /b 1
)

echo %__MsgPrefix%Environment setup

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

echo %__MsgPrefix%%BuildCommand%
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
echo     %__commandName% [option1] [option2] ...
echo All arguments are optional and case-insensitive, and the '-' prefix is optional. The options are:
echo.
echo.-? -h --help: View this message.
echo.
echo Build architecture: one of "x64", "x86", "arm64" ^(default: x64^).
echo Build type: one of "Debug", "Checked", "Release" ^(default: Debug^).
echo.
echo -Rebuild: Clean up all test artifacts prior to building tests.
echo -SkipRestorePackages: Skip package restore.
echo -SkipManaged: Skip the managed tests build.
echo -SkipNative: Skip the native tests build.
echo -SkipTestWrappers: Skip generating test wrappers.
echo -SkipGenerateLayout: Skip generating the Core_Root layout.
echo.
echo -CopyNativeOnly: Only copy the native test binaries to the managed output. Do not build the native or managed tests.
echo -GenerateLayoutOnly: Only generate the Core_Root layout without building managed or native test components.
echo -BuildTestWrappersOnly: Only generate test wrappers without building managed or native test components or generating layouts.
echo -MSBuild: Use MSBuild instead of Ninja.
echo -Crossgen2: Precompiles the framework managed assemblies in coreroot using the Crossgen2 compiler.
echo -Composite: Use Crossgen2 composite mode (all framework gets compiled into a single native R2R library).
echo -PDB: Create PDB files when precompiling the framework managed assemblies.
echo -NativeAOT: Builds the tests for Native AOT compilation.
echo -Perfmap: Emit perfmap symbol files when compiling the framework assemblies using Crossgen2.
echo -AllTargets: Build managed tests for all target platforms (including test projects in which CLRTestTargetUnsupported resolves to true).
echo -ExcludeMonoFailures, Mono: Build the tests for the Mono runtime honoring mono-specific issues.
echo.
echo -Exclude ^<xxx^>: Specify location of default exclusion file ^(defaults to tests\issues.targets if not specified^).
echo     Set to "" to disable default exclusion file.
echo -Priority ^<N^> : specify a set of tests that will be built and run, with priority N.
echo     0: Build only priority 0 cases as essential testcases (default).
echo     1: Build all tests with priority 0 and 1.
echo     666: Build all tests with priority 0, 1 ... 666.
echo -Test ^<xxx^>: Only build the specified test project ^(relative or absolute project path under src\tests^).
echo -Dir ^<xxx^>: Build all test projects in the given directory ^(relative or absolute directory under src\tests^).
echo -Tree ^<xxx^>: Build all test projects in the given subtree ^(relative or absolute directory under src\tests^).
echo -Log ^<xxx^>: Base file name to use for log files (used in lab pipelines that build tests in multiple steps to retain logs for each step).
echo.
echo -CMakeArgs ^<arg^>=^<value^>: Specify argument values to pass directly to CMake.
echo     Can be used multiple times to provide multiple CMake arguments.
echo.
echo -- : All arguments following this tag will be passed directly to MSBuild.
echo.     Any unrecognized arguments will also be passed directly to MSBuild.
exit /b 1

REM Exit_Failure:
REM This is necessary because of a(n apparent) bug in the FOR /L command.  Under certain circumstances,
REM such as when this script is invoke with CMD /C "build.cmd", a non-zero exit directly from
REM within the loop body will not propagate to the caller.  For some reason, goto works around it.
:Exit_Failure
exit /b 1
