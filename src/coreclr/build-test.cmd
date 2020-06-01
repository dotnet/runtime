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
set __TargetOS=Windows_NT

set "__ProjectDir=%~dp0"
:: remove trailing slash
if %__ProjectDir:~-1%==\ set "__ProjectDir=%__ProjectDir:~0,-1%"
set "__RepoRootDir=%__ProjectDir%\..\.."
for %%i in ("%__RepoRootDir%") do SET "__RepoRootDir=%%~fi"

set "__TestDir=%__ProjectDir%\tests"
set "__ProjectFilesDir=%__TestDir%"
set "__SourceDir=%__ProjectDir%\src"
set "__RootBinDir=%__RepoRootDir%\artifacts"
set "__LogsDir=%__RootBinDir%\log"
set "__MsbuildDebugLogsDir=%__LogsDir%\MsbuildDebugLogs"

:: Default __Exclude to issues.targets
set __Exclude=%__TestDir%\issues.targets

REM __UnprocessedBuildArgs are args that we pass to msbuild (e.g. /p:TargetArchitecture=x64)
set "__args= %*"
set processedArgs=
set __UnprocessedBuildArgs=
set __CommonMSBuildArgs=

set __SkipRestorePackages=
set __SkipManaged=
set __SkipTestWrappers=
set __BuildTestWrappersOnly=
set __SkipNative=
set __RuntimeId=
set __TargetsWindows=1
set __DoCrossgen=
set __DoCrossgen2=
set __CompositeBuildMode=
set __CopyNativeTestBinaries=0
set __CopyNativeProjectsAfterCombinedTestBuild=true
set __SkipGenerateLayout=0
set __LocalCoreFXConfig=%__BuildType%
set __SkipFXRestoreArg=
set __GenerateLayoutOnly=0

@REM CMD has a nasty habit of eating "=" on the argument list, so passing:
@REM    -priority=1
@REM appears to CMD parsing as "-priority 1". Handle -priority specially to avoid problems,
@REM and allow the "-priority=1" syntax.
set __Priority=0
set __PriorityArg=

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

if /i "%1" == "skipmanaged"           (set __SkipManaged=1&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "skipnative"            (set __SkipNative=1&set __CopyNativeProjectsAfterCombinedTestBuild=false&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "skiptestwrappers"      (set __SkipTestWrappers=1&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "buildtesthostonly"     (set __SkipNative=1&set __SkipManaged=1&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "buildtestwrappersonly" (set __SkipNative=1&set __SkipManaged=1&set __BuildTestWrappersOnly=1&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "buildagainstpackages"  (echo error: Remove /BuildAgainstPackages switch&&exit /b1)
if /i "%1" == "skiprestorepackages"   (set __SkipRestorePackages=1&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "crossgen"              (set __DoCrossgen=1&set __TestBuildMode=crossgen&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "crossgen2"             (set __DoCrossgen2=1&set __TestBuildMode=crossgen2&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "composite"             (set __CompositeBuildMode=1&set __DoCrossgen2=1&set __TestBuildMode=crossgen2&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "runtimeid"             (set __RuntimeId=%2&set processedArgs=!processedArgs! %1 %2&shift&shift&goto Arg_Loop)
if /i "%1" == "targetsNonWindows"     (set __TargetsWindows=0&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "Exclude"               (set __Exclude=%2&set processedArgs=!processedArgs! %1 %2&shift&shift&goto Arg_Loop)
if /i "%1" == "-priority"             (set __Priority=%2&shift&set processedArgs=!processedArgs! %1=%2&shift&goto Arg_Loop)
if /i "%1" == "targetGeneric"         (set "__BuildNeedTargetArg=/p:CLRTestNeedTargetToBuild=%1"&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "targetSpecific"        (set "__BuildNeedTargetArg=/p:CLRTestNeedTargetToBuild=%1"&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "copynativeonly"        (set __CopyNativeTestBinaries=1&set __SkipNative=1&set __CopyNativeProjectsAfterCombinedTestBuild=false&set __SkipCrossgenFramework=1&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "skipgeneratelayout"    (set __SkipGenerateLayout=1&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "generatelayoutonly"    (set __SkipManaged=1&set __SkipNative=1&set __CopyNativeProjectsAfterCombinedTestBuild=false&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "-excludemonofailures"  (set __Mono=1&set processedArgs=!processedArgs!&shift&goto Arg_Loop)
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

set __CommonMSBuildArgs=/p:TargetOS=%__TargetOS% /p:Configuration=%__BuildType% /p:TargetArchitecture=%__BuildArch%
set __msbuildArgs=/p:TargetOS=%__TargetOS% /p:Configuration=%__BuildType% /p:TargetArchitecture=%__BuildArch% /nologo /verbosity:minimal /clp:Summary /maxcpucount

echo %__MsgPrefix%Commencing CoreCLR test build

set "__BinDir=%__RootBinDir%\bin\coreclr\%__TargetOS%.%__BuildArch%.%__BuildType%"
set "__TestRootDir=%__RootBinDir%\tests\coreclr"
set "__TestBinDir=%__TestRootDir%\%__TargetOS%.%__BuildArch%.%__BuildType%"

if not defined XunitTestBinBase set XunitTestBinBase=%__TestBinDir%\
set "CORE_ROOT=%XunitTestBinBase%\Tests\Core_Root"

REM We have different managed and native intermediate dirs because the managed bits will include
REM the configuration information deeper in the intermediates path.
REM These variables are used by the msbuild project files.

if not defined __TestIntermediateDir (
    set "__TestIntermediateDir=tests\coreclr\obj\%__TargetOS%.%__BuildArch%.%__BuildType%"
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

REM =========================================================================================
REM ===
REM === Restore Build Tools
REM ===
REM =========================================================================================

@if defined _echo @echo on

set "__ToolsDir=%__ProjectDir%\Tools"

REM =========================================================================================
REM ===
REM === Resolve runtime dependences
REM ===
REM =========================================================================================

call "%__TestDir%\setup-stress-dependencies.cmd" /arch %__BuildArch% /outputdir %__BinDir%
if errorlevel 1 (
    echo %__ErrMsgPrefix%%__MsgPrefix%Error: setup-stress-dependencies failed.
    goto     :Exit_Failure
)
@if defined _echo @echo on

REM =========================================================================================
REM ===
REM === Native test build section
REM ===
REM =========================================================================================

if defined __SkipNative goto skipnative

echo %__MsgPrefix%Commencing build of native test components for %__BuildArch%/%__BuildType%

REM Set the environment for the native build

REM Eval the output from set-cmake-path.ps1
for /f "delims=" %%a in ('powershell -NoProfile -ExecutionPolicy ByPass "& ""%__SourceDir%\pal\tools\set-cmake-path.ps1"""') do %%a
echo %__MsgPrefix%Using CMake from !CMakePath!

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

set __VCBuildArch=x86_amd64
if /i "%__BuildArch%" == "x86" ( set __VCBuildArch=x86 )
if /i "%__BuildArch%" == "arm" ( set __VCBuildArch=x86_arm )
if /i "%__BuildArch%" == "arm64" ( set __VCBuildArch=x86_arm64 )

echo %__MsgPrefix%Using environment: "%__VCToolsRoot%\vcvarsall.bat" %__VCBuildArch%
call                                 "%__VCToolsRoot%\vcvarsall.bat" %__VCBuildArch%
@if defined _echo @echo on

if not defined VSINSTALLDIR (
    echo %__ErrMsgPrefix%%__MsgPrefix%Error: VSINSTALLDIR variable not defined.
    exit /b 1
)
if not exist "%VSINSTALLDIR%DIA SDK" goto NoDIA

set __ExtraCmakeArgs="-DCMAKE_SYSTEM_VERSION=10.0" "-DCLR_ENG_NATIVE_DIR=%__RepoRootDir%/eng/native"
call "%__SourceDir%\pal\tools\gen-buildsys.cmd" "%__ProjectFilesDir%" "%__NativeTestIntermediatesDir%" %__VSVersion% %__BuildArch% !__ExtraCmakeArgs!

if not !errorlevel! == 0 (
    echo %__ErrMsgPrefix%%__MsgPrefix%Error: failed to generate native component build project!
    exit /b 1
)

@if defined _echo @echo on

if not exist "%__NativeTestIntermediatesDir%\CMakeCache.txt" (
    echo %__ErrMsgPrefix%%__MsgPrefix%Error: unable to find generated native component build project!
    exit /b 1
)

set __BuildLogRootName=Tests_Native
set __BuildLog="%__LogsDir%\!__BuildLogRootName!_%__TargetOS%__%__BuildArch%__%__BuildType%.log"
set __BuildWrn="%__LogsDir%\!__BuildLogRootName!_%__TargetOS%__%__BuildArch%__%__BuildType%.wrn"
set __BuildErr="%__LogsDir%\!__BuildLogRootName!_%__TargetOS%__%__BuildArch%__%__BuildType%.err"
set __MsbuildLog=/flp:Verbosity=normal;LogFile=!__BuildLog!
set __MsbuildWrn=/flp1:WarningsOnly;LogFile=!__BuildWrn!
set __MsbuildErr=/flp2:ErrorsOnly;LogFile=!__BuildErr!
set __Logging=!__MsbuildLog! !__MsbuildWrn! !__MsbuildErr!

REM We pass the /m flag directly to MSBuild so that we can get both MSBuild and CL parallelism, which is fastest for our builds.
"%CMakePath%" --build %__NativeTestIntermediatesDir% --target install --config %__BuildType% -- /nologo /m !__Logging!

if errorlevel 1 (
    echo %__ErrMsgPrefix%%__MsgPrefix%Error: native test build failed.
    exit /b 1
)

:skipnative

REM =========================================================================================
REM ===
REM === Restore product binaries from packages
REM ===
REM =========================================================================================

if "%__SkipRestorePackages%" == "1" goto SkipRestoreProduct

echo %__MsgPrefix%Restoring CoreCLR product from packages

set __BuildLogRootName=Restore_Product
set __BuildLog=%__LogsDir%\%__BuildLogRootName%_%__TargetOS%__%__BuildArch%__%__BuildType%.log
set __BuildWrn=%__LogsDir%\%__BuildLogRootName%_%__TargetOS%__%__BuildArch%__%__BuildType%.wrn
set __BuildErr=%__LogsDir%\%__BuildLogRootName%_%__TargetOS%__%__BuildArch%__%__BuildType%.err
set __MsbuildLog=/flp:Verbosity=normal;LogFile="%__BuildLog%"
set __MsbuildWrn=/flp1:WarningsOnly;LogFile="%__BuildWrn%"
set __MsbuildErr=/flp2:ErrorsOnly;LogFile="%__BuildErr%"
set __Logging='!__MsbuildLog!' '!__MsbuildWrn!' '!__MsbuildErr!'

powershell -NoProfile -ExecutionPolicy ByPass -NoLogo -Command "%__RepoRootDir%\eng\common\msbuild.ps1" %__ArcadeScriptArgs%^
  %__ProjectDir%\tests\build.proj -warnAsError:0 /t:BatchRestorePackages /nodeReuse:false^
  /p:RestoreDefaultOptimizationDataPackage=false /p:PortableBuild=true^
  /p:UsePartialNGENOptimization=false /maxcpucount^
  %__SkipFXRestoreArg%^
  !__Logging! %__CommonMSBuildArgs% %__PriorityArg% %__BuildNeedTargetArg% %__UnprocessedBuildArgs%

if errorlevel 1 (
    echo %__ErrMsgPrefix%%__MsgPrefix%Error: Package restoration failed. Refer to the build log files for details:
    echo     %__BuildLog%
    echo     %__BuildWrn%
    echo     %__BuildErr%
    exit /b 1
)

:SkipRestoreProduct

REM =========================================================================================
REM ===
REM === Managed test build section
REM ===
REM =========================================================================================

if defined __SkipManaged goto SkipManagedBuild

echo %__MsgPrefix%Starting the Managed Tests Build

if not defined VSINSTALLDIR (
    echo %__ErrMsgPrefix%%__MsgPrefix%Error: build-test.cmd should be run from a Visual Studio Command Prompt.  Please see https://github.com/dotnet/runtime/tree/master/docs/workflow for build instructions.
    exit /b 1
)
set __AppendToLog=false
set __BuildLogRootName=Tests_Managed
set __BuildLog=%__LogsDir%\%__BuildLogRootName%_%__TargetOS%__%__BuildArch%__%__BuildType%.log
set __BuildWrn=%__LogsDir%\%__BuildLogRootName%_%__TargetOS%__%__BuildArch%__%__BuildType%.wrn
set __BuildErr=%__LogsDir%\%__BuildLogRootName%_%__TargetOS%__%__BuildArch%__%__BuildType%.err

REM Execute msbuild test build in stages - workaround for excessive data retention in MSBuild ConfigCache
REM See https://github.com/Microsoft/msbuild/issues/2993

set __SkipPackageRestore=false
set __SkipTargetingPackBuild=false
set __NumberOfTestGroups=3

if %__Priority% GTR 0 (set __NumberOfTestGroups=10)
echo %__MsgPrefix%Building tests divided into %__NumberOfTestGroups% test groups

set __CommonMSBuildCmdPrefix=powershell -NoProfile -ExecutionPolicy ByPass -NoLogo -Command "!__RepoRootDir!\eng\common\msbuild.ps1" !__ArcadeScriptArgs!

for /l %%G in (1, 1, %__NumberOfTestGroups%) do (

    set __MsbuildLog=/flp:Verbosity=normal;LogFile="%__BuildLog%";Append=!__AppendToLog!
    set __MsbuildWrn=/flp1:WarningsOnly;LogFile="%__BuildWrn%";Append=!__AppendToLog!
    set __MsbuildErr=/flp2:ErrorsOnly;LogFile="%__BuildErr%";Append=!__AppendToLog!
    set __Logging='!__MsbuildLog!' '!__MsbuildWrn!' '!__MsbuildErr!'

    set __TestGroupToBuild=%%G

    if not "%__CopyNativeTestBinaries%" == "1" (
        set __MSBuildBuildArgs=!__ProjectDir!\tests\build.proj
        set __MSBuildBuildArgs=!__MSBuildBuildArgs! -warnAsError:0
        set __MSBuildBuildArgs=!__MSBuildBuildArgs! /nodeReuse:false
        set __MSBuildBuildArgs=!__MSBuildBuildArgs! !__Logging!
        set __MSBuildBuildArgs=!__MSBuildBuildArgs! !TargetsWindowsMsbuildArg!
        set __MSBuildBuildArgs=!__MSBuildBuildArgs! !__msbuildArgs!
        set __MSBuildBuildArgs=!__MSBuildBuildArgs! !__PriorityArg! !__BuildNeedTargetArg!
        set __MSBuildBuildArgs=!__MSBuildBuildArgs! !__UnprocessedBuildArgs!
        set __MSBuildBuildArgs=!__MSBuildBuildArgs! /p:CopyNativeProjectBinaries=!__CopyNativeProjectsAfterCombinedTestBuild!
        set __MSBuildBuildArgs=!__MSBuildBuildArgs! /p:__SkipPackageRestore=true
        set __MSBuildBuildArgs=!__MSBuildBuildArgs! !__SkipFXRestoreArg!
        echo Running: msbuild !__MSBuildBuildArgs!
        !__CommonMSBuildCmdPrefix! !__MSBuildBuildArgs!

        if errorlevel 1 (
            echo %__ErrMsgPrefix%%__MsgPrefix%Error: managed test build failed. Refer to the build log files for details:
            echo     %__BuildLog%
            echo     %__BuildWrn%
            echo     %__BuildErr%
            REM This is necessary because of a(n apparent) bug in the FOR /L command.  Under certain circumstances,
            REM such as when this script is invoke with CMD /C "build-test.cmd", a non-zero exit directly from
            REM within the loop body will not propagate to the caller.  For some reason, goto works around it.
            goto     :Exit_Failure
        )
    ) else (
        set __MSBuildBuildArgs=!__ProjectDir!\tests\build.proj -warnAsError:0 /nodeReuse:false !__Logging! !TargetsWindowsMsbuildArg! !__msbuildArgs!  !__PriorityArg! !__BuildNeedTargetArg! !__SkipFXRestoreArg! !__UnprocessedBuildArgs! "/t:CopyAllNativeProjectReferenceBinaries"
        echo Running: msbuild !__MSBuildBuildArgs!
        !__CommonMSBuildCmdPrefix! !__MSBuildBuildArgs!

        if errorlevel 1 (
            echo %__ErrMsgPrefix%%__MsgPrefix%Error: copying native test binaries failed. Refer to the build log files for details:
            echo     %__BuildLog%
            echo     %__BuildWrn%
            echo     %__BuildErr%
            REM This is necessary because of a(n apparent) bug in the FOR /L command.  Under certain circumstances,
            REM such as when this script is invoke with CMD /C "build-test.cmd", a non-zero exit directly from
            REM within the loop body will not propagate to the caller.  For some reason, goto works around it.
            goto     :Exit_Failure
        )
    )

    set __SkipPackageRestore=true
    set __SkipTargetingPackBuild=true
    set __AppendToLog=true
)

if "%__CopyNativeTestBinaries%" == "1" goto :SkipManagedBuild

REM Check that we've built about as many tests as we expect. This is primarily intended to prevent accidental changes that cause us to build
REM drastically fewer Pri-1 tests than expected.
echo %__MsgPrefix%Check the managed tests build
echo Running: dotnet msbuild %__ProjectDir%\tests\src\runtest.proj /t:CheckTestBuild /nodeReuse:false /p:CLRTestPriorityToBuild=%__Priority% %__SkipFXRestoreArg% %__msbuildArgs% %__unprocessedBuildArgs%
powershell -NoProfile -ExecutionPolicy ByPass -NoLogo -File "%__RepoRootDir%\eng\common\msbuild.ps1" %__ArcadeScriptArgs%^
    %__ProjectDir%\tests\src\runtest.proj /t:CheckTestBuild /nodeReuse:false /p:CLRTestPriorityToBuild=%__Priority% %__msbuildArgs% %__unprocessedBuildArgs%
if errorlevel 1 (
    echo %__ErrMsgPrefix%%__MsgPrefix%Error: Check Test Build failed.
    exit /b 1
)

:SkipManagedBuild

if "%__SkipGenerateLayout%" == "1" goto TestBuildDone

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
if exist "%CORE_ROOT%" rd /s /q "%CORE_ROOT%"
md "%CORE_ROOT%"

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
set __BuildLog=%__LogsDir%\%__BuildLogRootName%_%__TargetOS%__%__BuildArch%__%__BuildType%.log
set __BuildWrn=%__LogsDir%\%__BuildLogRootName%_%__TargetOS%__%__BuildArch%__%__BuildType%.wrn
set __BuildErr=%__LogsDir%\%__BuildLogRootName%_%__TargetOS%__%__BuildArch%__%__BuildType%.err
set __MsbuildLog=/flp:Verbosity=normal;LogFile="%__BuildLog%"
set __MsbuildWrn=/flp1:WarningsOnly;LogFile="%__BuildWrn%"
set __MsbuildErr=/flp2:ErrorsOnly;LogFile="%__BuildErr%"
set __Logging=!__MsbuildLog! !__MsbuildWrn! !__MsbuildErr!

powershell -NoProfile -ExecutionPolicy ByPass -NoLogo -File "%__RepoRootDir%\eng\common\msbuild.ps1" %__ArcadeScriptArgs%^
  %__ProjectDir%\tests\src\runtest.proj /t:CreateTestOverlay /nodeReuse:false^
  /p:RestoreDefaultOptimizationDataPackage=false /p:PortableBuild=true^
  /p:UsePartialNGENOptimization=false /maxcpucount^
  %__SkipFXRestoreArg%^
  !__Logging! %__CommonMSBuildArgs% %RuntimeIdArg% %__PriorityArg% %__BuildNeedTargetArg% %__UnprocessedBuildArgs%
if errorlevel 1 (
    echo %__ErrMsgPrefix%%__MsgPrefix%Error: Create Test Overlay failed. Refer to the build log files for details:
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

if defined __BuildTestWrappersOnly goto BuildTestWrappers

if defined __SkipManaged goto SkipBuildingWrappers
if defined __SkipTestWrappers goto SkipBuildingWrappers

:BuildTestWrappers
echo %__MsgPrefix%Creating test wrappers

set __BuildLogRootName=Tests_XunitWrapper
set __BuildLog=%__LogsDir%\%__BuildLogRootName%_%__TargetOS%__%__BuildArch%__%__BuildType%.log
set __BuildWrn=%__LogsDir%\%__BuildLogRootName%_%__TargetOS%__%__BuildArch%__%__BuildType%.wrn
set __BuildErr=%__LogsDir%\%__BuildLogRootName%_%__TargetOS%__%__BuildArch%__%__BuildType%.err
set __MsbuildLog=/flp:Verbosity=normal;LogFile="%__BuildLog%"
set __MsbuildWrn=/flp1:WarningsOnly;LogFile="%__BuildWrn%"
set __MsbuildErr=/flp2:ErrorsOnly;LogFile="%__BuildErr%"
set __Logging=!__MsbuildLog! !__MsbuildWrn! !__MsbuildErr!

if %%__Mono%%==1 (
  set RuntimeFlavor="mono"
) else (
  set RuntimeFlavor="coreclr"
)

REM Build wrappers using the local SDK's msbuild. As we move to arcade, the other builds should be moved away from run.exe as well.
call "%__RepoRootDir%\dotnet.cmd" msbuild %__ProjectDir%\tests\src\runtest.proj /nodereuse:false /p:BuildWrappers=true /p:TestBuildMode=%__TestBuildMode% !__Logging! %__msbuildArgs% %TargetsWindowsMsbuildArg% %__SkipFXRestoreArg% %__UnprocessedBuildArgs% /p:RuntimeFlavor=%RuntimeFlavor%
if errorlevel 1 (
    echo %__ErrMsgPrefix%%__MsgPrefix%Error: XUnit wrapper build failed. Refer to the build log files for details:
    echo     %__BuildLog%
    echo     %__BuildWrn%
    echo     %__BuildErr%
    exit /b 1
)

echo { "build_os": "%__TargetOS%", "build_arch": "%__BuildArch%", "build_type": "%__BuildType%" } > "%__TestBinDir%/build_info.json"

:SkipBuildingWrappers

REM =========================================================================================
REM ===
REM === Crossgen assemblies if needed.
REM ===
REM =========================================================================================

if defined __SkipCrossgenFramework goto SkipCrossgen
if defined __BuildTestWrappersOnly goto SkipCrossgen

set __CrossgenArg = ""
if defined __DoCrossgen (
    set __CrossgenArg="/p:Crossgen=true"
    if "%__TargetsWindows%" == "1" (
        echo %__MsgPrefix%Running crossgen on framework assemblies in CORE_ROOT: %CORE_ROOT%
        call :PrecompileFX
        if ERRORLEVEL 1 (
            echo %__ErrMsgPrefix%%__MsgPrefix%Error: crossgen precompilation of framework assemblies failed
            exit /b 1
        )
    ) else (
        echo "%__MsgPrefix%Crossgen only supported on Windows, for now"
    )
)

if defined __DoCrossgen2 (
    set __CrossgenArg="/p:Crossgen2=true"
    if "%__BuildArch%" == "x64" (
        echo %__MsgPrefix%Running crossgen2 on framework assemblies in CORE_ROOT: %CORE_ROOT%
        call :PrecompileFX
        if ERRORLEVEL 1 (
            echo %__ErrMsgPrefix%%__MsgPrefix%Error: crossgen2 precompilation of framework assemblies failed
            exit /b 1
        )
    ) else (
        echo "%__MsgPrefix%Crossgen2 only supported on x64, for now"
    )
)

:SkipCrossgen

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
echo copynativeonly: Only copy the native test binaries to the managed output. Do not build the native or managed tests.
echo skipgeneratelayout: Do not generate the Core_Root layout or the CoreFX testhost.
echo generatelayoutonly: Generate the Core_Root layout without building managed or native test components
echo targetsNonWindows:
echo Exclude- Optional parameter - specify location of default exclusion file ^(defaults to tests\issues.targets if not specified^)
echo     Set to "" to disable default exclusion file.
echo -- ... : all arguments following this tag will be passed directly to msbuild.
echo -priority=^<N^> : specify a set of tests that will be built and run, with priority N.
echo     0: Build only priority 0 cases as essential testcases (default)
echo     1: Build all tests with priority 0 and 1
echo     666: Build all tests with priority 0, 1 ... 666
echo targetGeneric: Only build tests which run on any target platform.
echo targetSpecific: Only build tests which run on a specific target platform.
echo -verbose: enables detailed file logging for the msbuild tasks into the msbuild log file.
exit /b 1

:NoDIA
echo Error: DIA SDK is missing at "%VSINSTALLDIR%DIA SDK". ^
Did you install all the requirements for building on Windows, including the "Desktop Development with C++" workload? ^
Please see https://github.com/dotnet/runtime/blob/master/docs/workflow/requirements/windows-requirements.md ^
Another possibility is that you have a parallel installation of Visual Studio and the DIA SDK is there. In this case it ^
may help to copy its "DIA SDK" folder into "%VSINSTALLDIR%" manually, then try again.
exit /b 1

:PrecompileFX
set __TotalPrecompiled=0
set __FailedToPrecompile=0
set __FailedAssemblies=
set __CompositeOutputDir=%CORE_ROOT%\composite.out
set __CompositeResponseFile=%__CompositeOutputDir%\framework-r2r.dll.rsp

if defined __CompositeBuildMode (
    mkdir !__CompositeOutputDir!
    echo --composite>>!__CompositeResponseFile!
    echo -O>>!__CompositeResponseFile!
    echo --out^:%__CompositeOutputDir%\framework-r2r.dll>>!__CompositeResponseFile!
)

for %%F in ("%CORE_ROOT%\System.*.dll";"%CORE_ROOT%\Microsoft.*.dll";%CORE_ROOT%\netstandard.dll;%CORE_ROOT%\mscorlib.dll) do (
    if not "%%~nxF"=="Microsoft.CodeAnalysis.VisualBasic.dll" (
    if not "%%~nxF"=="Microsoft.CodeAnalysis.CSharp.dll" (
    if not "%%~nxF"=="Microsoft.CodeAnalysis.dll" (
    if not "%%~nxF"=="System.Runtime.WindowsRuntime.dll" (
        if defined __CompositeBuildMode (
            echo %%F>>!__CompositeResponseFile!
        ) else (
            call :PrecompileAssembly "%%F" %%~nxF __TotalPrecompiled __FailedToPrecompile __FailedAssemblies
            echo Processed: !__TotalPrecompiled!, failed !__FailedToPrecompile!
        )
    )))))
)

if defined __CompositeBuildMode (
    echo Composite response line^: %__CompositeResponseFile%
    type "%__CompositeResponseFile%"
)

if defined __CompositeBuildMode (
    set __CompositeCommandLine="%__RepoRootDir%\dotnet.cmd"
    set __CompositeCommandLine=!__CompositeCommandLine! "%CORE_ROOT%\crossgen2\crossgen2.dll"
    set __CompositeCommandLine=!__CompositeCommandLine! "@%__CompositeResponseFile%"
    echo Building composite R2R framework^: !__CompositeCommandLine!
    call !__CompositeCommandLine!
    set __FailedToPrecompile=!ERRORLEVEL!
    copy /Y "!__CompositeOutputDir!\*.*" "!CORE_ROOT!\"
)

if !__FailedToPrecompile! NEQ 0 (
    @echo Failed assemblies:
    FOR %%G IN (!__FailedAssemblies!) do echo   %%G
)

exit /b !__FailedToPrecompile!

REM Compile the managed assemblies in Core_ROOT before running the tests
:PrecompileAssembly

set AssemblyPath=%1
set AssemblyName=%2

set __CrossgenExe="%__BinDir%\crossgen.exe"
if /i "%__BuildArch%" == "arm" ( set __CrossgenExe="%__BinDir%\x86\crossgen.exe" )
if /i "%__BuildArch%" == "arm64" ( set __CrossgenExe="%__BinDir%\x64\crossgen.exe" )
set __CrossgenExe=%__CrossgenExe%

if defined __DoCrossgen2 (
    set __CrossgenExe="%__RepoRootDir%\dotnet.cmd" "%CORE_ROOT%\crossgen2\crossgen2.dll"
)

REM Intentionally avoid using the .dll extension to prevent
REM subsequent compilations from picking it up as a reference
set __CrossgenOutputFile="%CORE_ROOT%\temp.ni._dll"
set __CrossgenCmd=

if defined __DoCrossgen (
    set __CrossgenCmd=!__CrossgenExe! /Platform_Assemblies_Paths "!CORE_ROOT!" /in !AssemblyPath! /out !__CrossgenOutputFile!
    echo !__CrossgenCmd!
    !__CrossgenCmd!
) else (
    set __CrossgenCmd=!__CrossgenExe! -r:"!CORE_ROOT!\System.*.dll" -r:"!CORE_ROOT!\Microsoft.*.dll" -r:"!CORE_ROOT!\mscorlib.dll" -r:"!CORE_ROOT!\netstandard.dll" -O --inputbubble --out:!__CrossgenOutputFile! !AssemblyPath!
    echo !__CrossgenCmd!
    call !__CrossgenCmd!
)

set /a __exitCode = !errorlevel!

set /a "%~3+=1"

if "%__exitCode%" == "-2146230517" (
    echo %AssemblyPath% is not a managed assembly.
    exit /b 0
)

if %__exitCode% neq 0 (
    echo Unable to precompile %AssemblyPath%, exit code is %__exitCode%
    set /a "%~4+=1"
    set "%~5=!%~5!,!AssemblyName!"
    exit /b 0
)

REM Delete original .dll & replace it with the Crossgened .dll
del %AssemblyPath%
ren "%__CrossgenOutputFile%" %AssemblyName%

echo Successfully precompiled %AssemblyPath%
exit /b 0

:Exit_Failure
exit /b 1
