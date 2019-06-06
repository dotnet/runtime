@if not defined _echo @echo off
setlocal EnableDelayedExpansion EnableExtensions

:: Define a prefix for most output progress messages that come from this script. That makes
:: it easier to see where these are coming from. Note that there is a trailing space here.
set "__MsgPrefix=BUILDTEST: "

echo %__MsgPrefix%Starting Build at %TIME%

set __ThisScriptDir="%~dp0"

call "%__ThisScriptDir%"\setup_vs_tools.cmd
if NOT '%ERRORLEVEL%' == '0' exit /b 1

if defined VS160COMNTOOLS (
    set "__VSToolsRoot=%VS160COMNTOOLS%"
    set "__VCToolsRoot=%VS160COMNTOOLS%\..\..\VC\Auxiliary\Build"
    set __VSVersion=vs2019
) else if defined VS150COMNTOOLS (
    set "__VSToolsRoot=%VS150COMNTOOLS%"
    set "__VCToolsRoot=%VS150COMNTOOLS%\..\..\VC\Auxiliary\Build"
    set __VSVersion=vs2017
)

:: Set the default arguments for build
set __BuildArch=x64
set __BuildType=Debug
set __BuildOS=Windows_NT

set "__ProjectDir=%~dp0"
:: remove trailing slash
if %__ProjectDir:~-1%==\ set "__ProjectDir=%__ProjectDir:~0,-1%"
set "__TestDir=%__ProjectDir%\tests"
set "__ProjectFilesDir=%__TestDir%"
set "__SourceDir=%__ProjectDir%\src"
set "__PackagesDir=%__ProjectDir%\.packages"
set "__RootBinDir=%__ProjectDir%\bin"
set "__LogsDir=%__RootBinDir%\Logs"
set "__MsbuildDebugLogsDir=%__LogsDir%\MsbuildDebugLogs"

:: Default __Exclude to issues.targets
set __Exclude=%__TestDir%\issues.targets

REM __UnprocessedBuildArgs are args that we pass to msbuild (e.g. /p:__BuildArch=x64)
set "__args= %*"
set processedArgs=
set __UnprocessedBuildArgs=
set __CommonMSBuildArgs=

set __SkipRestorePackages=
set __SkipManaged=
set __SkipNative=
set __RuntimeId=
set __TargetsWindows=1
set __DoCrossgen=

@REM CMD has a nasty habit of eating "=" on the argument list, so passing:
@REM    -priority=1
@REM appears to CMD parsing as "-priority 1". Handle -priority specially to avoid problems,
@REM and allow the "-priority=1" syntax.
set __Priority=0
set __PriorityArg=

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

if /i "%1" == "skipmanaged"           (set __SkipManaged=1&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "skipnative"            (set __SkipNative=1&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "buildtesthostonly"     (set __SkipNative=1&set __SkipManaged=1&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "buildagainstpackages"  (echo error: Remove /BuildAgainstPackages switch&&exit /b1)
if /i "%1" == "skiprestorepackages"   (set __SkipRestorePackages=1&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "crossgen"              (set __DoCrossgen=1&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "runtimeid"             (set __RuntimeId=%2&set processedArgs=!processedArgs! %1 %2&shift&shift&goto Arg_Loop)
if /i "%1" == "targetsNonWindows"     (set __TargetsWindows=0&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "Exclude"               (set __Exclude=%2&set processedArgs=!processedArgs! %1 %2&shift&shift&goto Arg_Loop)
if /i "%1" == "-priority"             (set __Priority=%2&shift&set processedArgs=!processedArgs! %1=%2&shift&goto Arg_Loop)
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

@REM Special handling for -priority=N argument.
if %__Priority% GTR 0 (
    set "__PriorityArg=/p:CLRTestPriorityToBuild=%__Priority%"
)

set TargetsWindowsArg=
set TargetsWindowsMsbuildArg=
if "%__TargetsWindows%"=="1" (
    set TargetsWindowsArg=-TargetsWindows=true
    set TargetsWindowsMsbuildArg=/p:TargetsWindows=true
) else if "%__TargetsWindows%"=="0" (
    set TargetsWindowsArg=-TargetsWindows=false
    set TargetsWindowsMsbuildArg=/p:TargetsWindows=false
)

@if defined _echo @echo on

set __CommonMSBuildArgs=/p:__BuildOS=%__BuildOS% /p:__BuildType=%__BuildType% /p:__BuildArch=%__BuildArch%
REM As we move from buildtools to arcade, __RunArgs should be replaced with __msbuildArgs
set __msbuildArgs=/p:__BuildOS=%__BuildOS% /p:__BuildType=%__BuildType% /p:__BuildArch=%__BuildArch% /nologo /verbosity:minimal /clp:Summary /maxcpucount

echo %__MsgPrefix%Commencing CoreCLR test build

set "__BinDir=%__RootBinDir%\Product\%__BuildOS%.%__BuildArch%.%__BuildType%"
set "__TestRootDir=%__RootBinDir%\tests"
set "__TestBinDir=%__TestRootDir%\%__BuildOS%.%__BuildArch%.%__BuildType%"

REM We have different managed and native intermediate dirs because the managed bits will include
REM the configuration information deeper in the intermediates path.
REM These variables are used by the msbuild project files.

if not defined __TestIntermediateDir (
    set "__TestIntermediateDir=tests\obj\%__BuildOS%.%__BuildArch%.%__BuildType%"
)
set "__NativeTestIntermediatesDir=%__RootBinDir%\%__TestIntermediateDir%\Native"
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

echo %__MsgPrefix%Checking prerequisites

REM Eval the output from set-cmake-path.ps1
for /f "delims=" %%a in ('powershell -NoProfile -ExecutionPolicy ByPass "& ""%__SourceDir%\pal\tools\set-cmake-path.ps1"""') do %%a

REM =========================================================================================
REM ===
REM === Restore Build Tools
REM ===
REM =========================================================================================

call "%__ProjectDir%\init-tools.cmd"
if NOT [%ERRORLEVEL%]==[0] (
    exit /b %ERRORLEVEL%
)
@if defined _echo @echo on

set "__ToolsDir=%__ProjectDir%\Tools"

REM =========================================================================================
REM ===
REM === Resolve runtime dependences
REM ===
REM =========================================================================================

call "%__TestDir%\setup-stress-dependencies.cmd" /arch %__BuildArch% /outputdir %__BinDir%
@if defined _echo @echo on

REM =========================================================================================
REM ===
REM === Native test build section
REM ===
REM =========================================================================================

if defined __SkipNative goto skipnative

echo %__MsgPrefix%Commencing build of native test components for %__BuildArch%/%__BuildType%

REM Set the environment for the native build
set __VCBuildArch=x86_amd64
if /i "%__BuildArch%" == "x86" ( set __VCBuildArch=x86 )
if /i "%__BuildArch%" == "arm" ( set __VCBuildArch=x86_arm )
if /i "%__BuildArch%" == "arm64" ( set __VCBuildArch=x86_arm64 )

echo %__MsgPrefix%Using environment: "%__VCToolsRoot%\vcvarsall.bat" %__VCBuildArch%
call                                 "%__VCToolsRoot%\vcvarsall.bat" %__VCBuildArch%
@if defined _echo @echo on

if not defined VSINSTALLDIR (
    echo %__MsgPrefix%Error: VSINSTALLDIR variable not defined.
    exit /b 1
)
if not exist "%VSINSTALLDIR%DIA SDK" goto NoDIA

pushd "%__NativeTestIntermediatesDir%"
set __ExtraCmakeArgs="-DCMAKE_SYSTEM_VERSION=10.0"
call "%__SourceDir%\pal\tools\gen-buildsys-win.bat" ""%__ProjectFilesDir%"" %__VSVersion% %__BuildArch% !__ExtraCmakeArgs!
@if defined _echo @echo on
popd

if not exist "%__NativeTestIntermediatesDir%\install.vcxproj" (
    echo %__MsgPrefix%Failed to generate test native component build project!
    exit /b 1
)

set __BuildLogRootName=Tests_Native
set __BuildLog=%__LogsDir%\%__BuildLogRootName%_%__BuildOS%__%__BuildArch%__%__BuildType%.log
set __BuildWrn=%__LogsDir%\%__BuildLogRootName%_%__BuildOS%__%__BuildArch%__%__BuildType%.wrn
set __BuildErr=%__LogsDir%\%__BuildLogRootName%_%__BuildOS%__%__BuildArch%__%__BuildType%.err
set __MsbuildLog=/flp:Verbosity=normal;LogFile="%__BuildLog%"
set __MsbuildWrn=/flp1:WarningsOnly;LogFile="%__BuildWrn%"
set __MsbuildErr=/flp2:ErrorsOnly;LogFile="%__BuildErr%"
set __Logging=!__MsbuildLog! !__MsbuildWrn! !__MsbuildErr!

call "%__ProjectDir%\cmake_msbuild.cmd" /nologo /verbosity:minimal /clp:Summary /nodeReuse:false^
  /l:BinClashLogger,Tools/net46/Microsoft.DotNet.Build.Tasks.dll;LogFile=binclash.log^
  /p:RestoreDefaultOptimizationDataPackage=false /p:PortableBuild=true^
  /p:UsePartialNGENOptimization=false /maxcpucount^
  "%__NativeTestIntermediatesDir%\install.vcxproj"^
  !__Logging! /p:Configuration=%__BuildType% /p:Platform=%__BuildArch% %__CommonMSBuildArgs% %__PriorityArg% %__UnprocessedBuildArgs%
if errorlevel 1 (
    echo %__MsgPrefix%Error: build failed. Refer to the build log files for details:
    echo     %__BuildLog%
    echo     %__BuildWrn%
    echo     %__BuildErr%
    exit /b 1
)

:skipnative

REM =========================================================================================
REM ===
REM === Restore product binaries from packages
REM ===
REM =========================================================================================

if "%__SkipRestorePackages%" == 1 goto SkipRestoreProduct

echo %__MsgPrefix%Restoring CoreCLR product from packages

if not defined XunitTestBinBase set XunitTestBinBase=%__TestBinDir%
set "CORE_ROOT=%XunitTestBinBase%\Tests\Core_Root"

set __BuildLogRootName=Restore_Product
set __BuildLog=%__LogsDir%\%__BuildLogRootName%_%__BuildOS%__%__BuildArch%__%__BuildType%.log
set __BuildWrn=%__LogsDir%\%__BuildLogRootName%_%__BuildOS%__%__BuildArch%__%__BuildType%.wrn
set __BuildErr=%__LogsDir%\%__BuildLogRootName%_%__BuildOS%__%__BuildArch%__%__BuildType%.err
set __MsbuildLog=/flp:Verbosity=normal;LogFile="%__BuildLog%"
set __MsbuildWrn=/flp1:WarningsOnly;LogFile="%__BuildWrn%"
set __MsbuildErr=/flp2:ErrorsOnly;LogFile="%__BuildErr%"
set __Logging=!__MsbuildLog! !__MsbuildWrn! !__MsbuildErr!

call "%__ProjectDir%\dotnet.cmd" msbuild /nologo /verbosity:minimal /clp:Summary /nodeReuse:false^
  /l:BinClashLogger,Tools/Microsoft.DotNet.Build.Tasks.dll;LogFile=binclash.log^
  /p:RestoreDefaultOptimizationDataPackage=false /p:PortableBuild=true^
  /p:UsePartialNGENOptimization=false /maxcpucount^
  %__ProjectDir%\tests\build.proj /t:BatchRestorePackages^
  !__Logging! %__CommonMSBuildArgs% %__PriorityArg% %__UnprocessedBuildArgs%

:SkipRestoreProduct

REM =========================================================================================
REM ===
REM === Managed test build section
REM ===
REM =========================================================================================

if defined __SkipManaged goto SkipManagedBuild

echo %__MsgPrefix%Starting the Managed Tests Build

if not defined VSINSTALLDIR (
    echo %__MsgPrefix%Error: build-test.cmd should be run from a Visual Studio Command Prompt.  Please see https://github.com/dotnet/coreclr/blob/master/Documentation/project-docs/developer-guide.md for build instructions.
    exit /b 1
)
set __AppendToLog=false
set __BuildLogRootName=Tests_Managed
set __BuildLog=%__LogsDir%\%__BuildLogRootName%_%__BuildOS%__%__BuildArch%__%__BuildType%.log
set __BuildWrn=%__LogsDir%\%__BuildLogRootName%_%__BuildOS%__%__BuildArch%__%__BuildType%.wrn
set __BuildErr=%__LogsDir%\%__BuildLogRootName%_%__BuildOS%__%__BuildArch%__%__BuildType%.err

REM Execute msbuild test build in stages - workaround for excessive data retention in MSBuild ConfigCache
REM See https://github.com/Microsoft/msbuild/issues/2993

set __SkipPackageRestore=false
set __SkipTargetingPackBuild=false
set __NumberOfTestGroups=3

if %__Priority% GTR 0 (set __NumberOfTestGroups=10)
echo %__MsgPrefix%Building tests divided into %__NumberOfTestGroups% test groups

for /l %%G in (1, 1, %__NumberOfTestGroups%) do (

    set __MsbuildLog=/flp:Verbosity=normal;LogFile="%__BuildLog%";Append=!__AppendToLog!
    set __MsbuildWrn=/flp1:WarningsOnly;LogFile="%__BuildWrn%";Append=!__AppendToLog!
    set __MsbuildErr=/flp2:ErrorsOnly;LogFile="%__BuildErr%";Append=!__AppendToLog!

    set __TestGroupToBuild=%%G
    echo Running: msbuild %__ProjectDir%\tests\build.proj !__MsbuildLog! !__MsbuildWrn! !__MsbuildErr! %TargetsWindowsMsbuildArg% %__msbuildArgs% !__PriorityArg! %__UnprocessedBuildArgs%

    call "%__ProjectDir%\dotnet.cmd" msbuild %__ProjectDir%\tests\build.proj !__MsbuildLog! !__MsbuildWrn! !__MsbuildErr! %TargetsWindowsMsbuildArg% %__msbuildArgs% !__PriorityArg! %__UnprocessedBuildArgs%

    if errorlevel 1 (
        echo %__MsgPrefix%Error: build failed. Refer to the build log files for details:
        echo     %__BuildLog%
        echo     %__BuildWrn%
        echo     %__BuildErr%
        REM This is necessary because of a(n apparent) bug in the FOR /L command.  Under certain circumstances,
        REM such as when this script is invoke with CMD /C "build-test.cmd", a non-zero exit directly from
        REM within the loop body will not propagate to the caller.  For some reason, goto works around it.
        goto     :Exit_Failure
    )

    set __SkipPackageRestore=true
    set __SkipTargetingPackBuild=true
    set __AppendToLog=true
)

REM Check that we've built about as many tests as we expect. This is primarily intended to prevent accidental changes that cause us to build
REM drastically fewer Pri-1 tests than expected.
echo %__MsgPrefix%Check the managed tests build
echo Running: dotnet msbuild %__ProjectDir%\tests\runtest.proj /t:CheckTestBuild /p:CLRTestPriorityToBuild=%__Priority% %__msbuildArgs% %__unprocessedBuildArgs%
call "%__ProjectDir%\dotnet.cmd" msbuild %__ProjectDir%\tests\runtest.proj /t:CheckTestBuild /p:CLRTestPriorityToBuild=%__Priority% %__msbuildArgs% %__unprocessedBuildArgs%
if errorlevel 1 (
    echo %__MsgPrefix%Error: build failed.
    exit /b 1
)

:SkipManagedBuild

REM =========================================================================================
REM ===
REM === Prepare the test drop
REM ===
REM =========================================================================================

echo %__MsgPrefix%Removing 'ni' files and 'lock' folders from %__TestBinDir%
REM Remove any NI from previous runs.
powershell -NoProfile "Get-ChildItem -path %__TestBinDir% -Include '*.ni.*' -Recurse -Force | Remove-Item -force"
REM Remove any lock folder used for synchronization from previous runs.
powershell -NoProfile "Get-ChildItem -path %__TestBinDir% -Include 'lock' -Recurse -Force |  where {$_.Attributes -eq 'Directory'}| Remove-Item -force -Recurse"

set CORE_ROOT=%__TestBinDir%\Tests\Core_Root
set CORE_ROOT_STAGE=%__TestBinDir%\Tests\Core_Root_Stage
if exist "%CORE_ROOT%" rd /s /q "%CORE_ROOT%"
if exist "%CORE_ROOT_STAGE%" rd /s /q "%CORE_ROOT_STAGE%"
md "%CORE_ROOT%"
md "%CORE_ROOT_STAGE%"
xcopy /s "%__BinDir%" "%CORE_ROOT_STAGE%"

REM =========================================================================================
REM ===
REM === Create the test overlay
REM ===
REM =========================================================================================

echo %__MsgPrefix%Creating test overlay

set RuntimeIdArg=
if defined __RuntimeId (
    set RuntimeIdArg=/p:RuntimeId="%__RuntimeId%"
)

set __BuildLogRootName=Tests_Overlay_Managed
set __BuildLog=%__LogsDir%\%__BuildLogRootName%_%__BuildOS%__%__BuildArch%__%__BuildType%.log
set __BuildWrn=%__LogsDir%\%__BuildLogRootName%_%__BuildOS%__%__BuildArch%__%__BuildType%.wrn
set __BuildErr=%__LogsDir%\%__BuildLogRootName%_%__BuildOS%__%__BuildArch%__%__BuildType%.err
set __MsbuildLog=/flp:Verbosity=normal;LogFile="%__BuildLog%"
set __MsbuildWrn=/flp1:WarningsOnly;LogFile="%__BuildWrn%"
set __MsbuildErr=/flp2:ErrorsOnly;LogFile="%__BuildErr%"
set __Logging=!__MsbuildLog! !__MsbuildWrn! !__MsbuildErr!

call %__ProjectDir%\dotnet.cmd msbuild /nologo /verbosity:minimal /clp:Summary /nodeReuse:false^
  /l:BinClashLogger,Tools/Microsoft.DotNet.Build.Tasks.dll;LogFile=binclash.log^
  /p:RestoreDefaultOptimizationDataPackage=false /p:PortableBuild=true^
  /p:UsePartialNGENOptimization=false /maxcpucount^
  %__ProjectDir%\tests\runtest.proj /t:CreateTestOverlay^
  !__Logging! %__CommonMSBuildArgs% %RuntimeIdArg% %__PriorityArg% %__UnprocessedBuildArgs%
if errorlevel 1 (
    echo %__MsgPrefix%Error: build failed. Refer to the build log files for details:
    echo     %__BuildLog%
    echo     %__BuildWrn%
    echo     %__BuildErr%
    exit /b 1
)

xcopy /s /y "%CORE_ROOT_STAGE%" "%CORE_ROOT%"

REM =========================================================================================
REM ===
REM === Create the test host necessary for running CoreFX tests.
REM === The test host includes a dotnet executable, system libraries and CoreCLR assemblies found in CORE_ROOT.
REM ===
REM =========================================================================================

echo %__MsgPrefix%Building CoreFX test host

set __BuildLogRootName=Tests_CoreFX_Testhost
set __BuildLog=%__LogsDir%\%__BuildLogRootName%_%__BuildOS%__%__BuildArch%__%__BuildType%.log
set __BuildWrn=%__LogsDir%\%__BuildLogRootName%_%__BuildOS%__%__BuildArch%__%__BuildType%.wrn
set __BuildErr=%__LogsDir%\%__BuildLogRootName%_%__BuildOS%__%__BuildArch%__%__BuildType%.err
set __MsbuildLog=/flp:Verbosity=normal;LogFile="%__BuildLog%"
set __MsbuildWrn=/flp1:WarningsOnly;LogFile="%__BuildWrn%"
set __MsbuildErr=/flp2:ErrorsOnly;LogFile="%__BuildErr%"
set __Logging=!__MsbuildLog! !__MsbuildWrn! !__MsbuildErr!

call %__ProjectDir%\dotnet.cmd msbuild /nologo /verbosity:minimal /clp:Summary /nodeReuse:false^
  /l:BinClashLogger,Tools/Microsoft.DotNet.Build.Tasks.dll;LogFile=binclash.log^
  /p:RestoreDefaultOptimizationDataPackage=false /p:PortableBuild=true^
  /p:UsePartialNGENOptimization=false /maxcpucount^
  %__ProjectDir%\tests\runtest.proj /t:CreateTestHost^
  !__Logging! %__CommonMSBuildArgs% %RuntimeIdArg% %__PriorityArg% %__UnprocessedBuildArgs%
if errorlevel 1 (
    echo %__MsgPrefix%Error: build failed. Refer to the build log files for details:
    echo     %__BuildLog%
    echo     %__BuildWrn%
    echo     %__BuildErr%
    exit /b 1
)

REM =========================================================================================
REM ===
REM === Create test wrappers.
REM ===
REM =========================================================================================

if defined __SkipManaged goto SkipBuildingWrappers

echo %__MsgPrefix%Creating test wrappers

set __BuildLogRootName=Tests_XunitWrapper
set __BuildLog=%__LogsDir%\%__BuildLogRootName%_%__BuildOS%__%__BuildArch%__%__BuildType%.log
set __BuildWrn=%__LogsDir%\%__BuildLogRootName%_%__BuildOS%__%__BuildArch%__%__BuildType%.wrn
set __BuildErr=%__LogsDir%\%__BuildLogRootName%_%__BuildOS%__%__BuildArch%__%__BuildType%.err
set __MsbuildLog=/flp:Verbosity=normal;LogFile="%__BuildLog%"
set __MsbuildWrn=/flp1:WarningsOnly;LogFile="%__BuildWrn%"
set __MsbuildErr=/flp2:ErrorsOnly;LogFile="%__BuildErr%"
set __Logging=!__MsbuildLog! !__MsbuildWrn! !__MsbuildErr!

REM Build wrappers using the local SDK's msbuild. As we move to arcade, the other builds should be moved away from run.exe as well.
call "%__ProjectDir%\dotnet.cmd" msbuild %__ProjectDir%\tests\runtest.proj /p:RestoreAdditionalProjectSources=https://dotnet.myget.org/F/dotnet-core/  /p:BuildWrappers=true !__Logging! %__msbuildArgs% %TargetsWindowsMsbuildArg% %__UnprocessedBuildArgs%
if errorlevel 1 (
    echo %__MsgPrefix%Error: Xunit wrapper build failed. Refer to the build log files for details:
    echo     %__BuildLog%
    echo     %__BuildWrn%
    echo     %__BuildErr%
    exit /b 1
)

echo { "build_os": "%__BuildOS%", "build_arch": "%__BuildArch%", "build_type": "%__BuildType%" } > "%__TestBinDir%/build_info.json"

:SkipBuildingWrappers

REM =========================================================================================
REM ===
REM === Crossgen assemblies if needed.
REM ===
REM =========================================================================================

set __CrossgenArg = ""
if defined __DoCrossgen (
    set __CrossgenArg="/p:Crossgen=true"
    if "%__TargetsWindows%" == "1" (
        echo %__MsgPrefix%Running crossgen on framework assemblies
        call :PrecompileFX
    ) else (
        echo "%__MsgPrefix%Crossgen only supported on Windows, for now"
    )
)

rd /s /q "%CORE_ROOT_STAGE%"

REM =========================================================================================
REM ===
REM === All builds complete!
REM ===
REM =========================================================================================

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
echo skipmanaged: skip the managed tests build
echo skipnative: skip the native tests build
echo buildtesthostonly: build the CoreFX testhost only
echo skiprestorepackages: skip package restore
echo runtimeid ^<ID^>: Builds a test overlay for the specified OS ^(Only supported when building against packages^). Supported IDs are:
echo     alpine.3.4.3-x64: Builds overlay for Alpine 3.4.3
echo     debian.8-x64: Builds overlay for Debian 8
echo     fedora.24-x64: Builds overlay for Fedora 24
echo     linux-x64: Builds overlay for portable linux
echo     opensuse.42.1-x64: Builds overlay for OpenSUSE 42.1
echo     osx.10.12-x64: Builds overlay for OSX 10.12
echo     osx-x64: Builds overlay for portable OSX
echo     rhel.7-x64: Builds overlay for RHEL 7 or CentOS
echo     ubuntu.14.04-x64: Builds overlay for Ubuntu 14.04
echo     ubuntu.16.04-x64: Builds overlay for Ubuntu 16.04
echo     ubuntu.16.10-x64: Builds overlay for Ubuntu 16.10
echo     win-x64: Builds overlay for portable Windows
echo     win7-x64: Builds overlay for Windows 7
echo crossgen: Precompiles the framework managed assemblies
echo targetsNonWindows:
echo Exclude- Optional parameter - specify location of default exclusion file ^(defaults to tests\issues.targets if not specified^)
echo     Set to "" to disable default exclusion file.
echo -- ... : all arguments following this tag will be passed directly to msbuild.
echo -priority=^<N^> : specify a set of tests that will be built and run, with priority N.
echo     0: Build only priority 0 cases as essential testcases (default)
echo     1: Build all tests with priority 0 and 1
echo     666: Build all tests with priority 0, 1 ... 666
echo -verbose: enables detailed file logging for the msbuild tasks into the msbuild log file.
exit /b 1

:NoDIA
echo Error: DIA SDK is missing at "%VSINSTALLDIR%DIA SDK". ^
This is due to a bug in the Visual Studio installer. It does not install DIA SDK at "%VSINSTALLDIR%" but rather ^
at the install location of previous Visual Studio version. The workaround is to copy the DIA SDK folder from the Visual Studio install location ^
of the previous version to "%VSINSTALLDIR%" and then build.
REM DIA SDK not included in Express editions
echo Visual Studio Express does not include the DIA SDK. ^
You need Visual Studio 2017 or 2019 (Community is free).
echo See: https://github.com/dotnet/coreclr/blob/master/Documentation/project-docs/developer-guide.md#prerequisites
exit /b 1

:PrecompileFX
for %%F in (%CORE_ROOT%\*.dll) do call :PrecompileAssembly "%%F" %%~nF%%~xF
exit /b 0

REM Compile the managed assemblies in Core_ROOT before running the tests
:PrecompileAssembly

REM Skip mscorlib since it is already precompiled.
if /I "%2" == "mscorlib.dll" exit /b 0
if /I "%2" == "mscorlib.ni.dll" exit /b 0
REM don't precompile anything from CoreCLR
if /I exist %CORE_ROOT_STAGE%\%2 exit /b 0

REM Don't precompile xunit.* files
echo "%2" | findstr /b "xunit." >nul && (
  exit /b 0
)

set __CrossgenExe="%CORE_ROOT_STAGE%\crossgen.exe"
if /i "%__BuildArch%" == "arm" ( set __CrossgenExe="%CORE_ROOT_STAGE%\x86\crossgen.exe" )
if /i "%__BuildArch%" == "arm64" ( set __CrossgenExe="%CORE_ROOT_STAGE%\x64\crossgen.exe" )

"%__CrossgenExe%" /Platform_Assemblies_Paths "%CORE_ROOT%" /in "%1" /out "%CORE_ROOT%/temp.ni.dll" >nul 2>nul
set /a __exitCode = %errorlevel%
if "%__exitCode%" == "-2146230517" (
    echo %2 is not a managed assembly.
    exit /b 0
)

if %__exitCode% neq 0 (
    echo Unable to precompile %2, Exit Code is %__exitCode%
    exit /b 0
)

REM Delete original .dll & replace it with the Crossgened .dll
del %1
ren "%CORE_ROOT%\temp.ni.dll" %2
    
echo Successfully precompiled %2
exit /b 0

:Exit_Failure
exit /b 1
