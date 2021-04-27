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

call %__RepoRootDir%\eng\native\init-vs-env.cmd
if NOT '%ERRORLEVEL%' == '0' exit /b 1

if defined VCINSTALLDIR (
    set "__VCToolsRoot=%VCINSTALLDIR%Auxiliary\Build"
)

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

set __SkipRestorePackages=
set __SkipManaged=
set __SkipTestWrappers=
set __BuildTestWrappersOnly=
set __SkipNative=
set __TargetsWindows=1
set __DoCrossgen=
set __DoCrossgen2=
set __CompositeBuildMode=
set __CreatePdb=
set __CopyNativeTestBinaries=0
set __CopyNativeProjectsAfterCombinedTestBuild=true
set __SkipGenerateLayout=0
set __GenerateLayoutOnly=0
set __Ninja=1

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

if /i "%1" == "skiprestorepackages"   (set __SkipRestorePackages=1&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "skipmanaged"           (set __SkipManaged=1&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "skipnative"            (set __SkipNative=1&set __CopyNativeProjectsAfterCombinedTestBuild=false&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "skiptestwrappers"      (set __SkipTestWrappers=1&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "skipgeneratelayout"    (set __SkipGenerateLayout=1&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)

if /i "%1" == "copynativeonly"        (set __CopyNativeTestBinaries=1&set __SkipNative=1&set __CopyNativeProjectsAfterCombinedTestBuild=false&set __SkipGenerateLayout=1&set __SkipTestWrappers=1&set __SkipCrossgenFramework=1&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "generatelayoutonly"    (set __SkipManaged=1&set __SkipNative=1&set __CopyNativeProjectsAfterCombinedTestBuild=false&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "buildtestwrappersonly" (set __SkipNative=1&set __SkipManaged=1&set __BuildTestWrappersOnly=1&set __SkipGenerateLayout=1&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)

if /i "%1" == "-msbuild"              (set __Ninja=0&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "buildagainstpackages"  (echo error: Remove /BuildAgainstPackages switch&&exit /b1)
if /i "%1" == "crossgen"              (set __DoCrossgen=1&set __TestBuildMode=crossgen&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "crossgen2"             (set __DoCrossgen2=1&set __TestBuildMode=crossgen2&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "composite"             (set __CompositeBuildMode=1&set __DoCrossgen2=1&set __TestBuildMode=crossgen2&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "pdb"                   (set __CreatePdb=1&shift&goto Arg_Loop)
if /i "%1" == "targetsNonWindows"     (set __TargetsWindows=0&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "Exclude"               (set __Exclude=%2&set processedArgs=!processedArgs! %1 %2&shift&shift&goto Arg_Loop)
if /i "%1" == "-priority"             (set __Priority=%2&shift&set processedArgs=!processedArgs! %1=%2&shift&goto Arg_Loop)
if /i "%1" == "allTargets"            (set "__BuildNeedTargetArg=/p:CLRTestBuildAllTargets=%1"&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
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

set "__OSPlatformConfig=%__TargetOS%.%__BuildArch%.%__BuildType%"
set "__BinDir=%__RootBinDir%\bin\coreclr\%__OSPlatformConfig%"
set "__TestRootDir=%__RootBinDir%\tests\coreclr"
set "__TestBinDir=%__TestRootDir%\%__OSPlatformConfig%"
set "__TestIntermediatesDir=%__TestRootDir%\obj\%__OSPlatformConfig%"

if not defined XunitTestBinBase set XunitTestBinBase=%__TestBinDir%\
set "CORE_ROOT=%XunitTestBinBase%\Tests\Core_Root"

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

REM =========================================================================================
REM ===
REM === Native test build section
REM ===
REM =========================================================================================

if defined __SkipNative goto skipnative

echo %__MsgPrefix%Commencing build of native test components for %__BuildArch%/%__BuildType%

REM Set the environment for the native build

REM Eval the output from set-cmake-path.ps1
for /f "delims=" %%a in ('powershell -NoProfile -ExecutionPolicy ByPass "& ""%__RepoRootDir%\eng\native\set-cmake-path.ps1"""') do %%a
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

set __ExtraCmakeArgs=

if %__Ninja% EQU 1 (
    set __ExtraCmakeArgs="-DCMAKE_SYSTEM_VERSION=10.0 -DCMAKE_BUILD_TYPE=!__BuildType!"
) else (
    set __ExtraCmakeArgs="-DCMAKE_SYSTEM_VERSION=10.0"
)
call "%__RepoRootDir%\eng\native\gen-buildsys.cmd" "%__ProjectFilesDir%" "%__NativeTestIntermediatesDir%" %__VSVersion% %__BuildArch% !__ExtraCmakeArgs!

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

set __BuildLogRootName=Tests_Native
set __BuildLog="%__LogsDir%\!__BuildLogRootName!_%__TargetOS%__%__BuildArch%__%__BuildType%.log"
set __BuildWrn="%__LogsDir%\!__BuildLogRootName!_%__TargetOS%__%__BuildArch%__%__BuildType%.wrn"
set __BuildErr="%__LogsDir%\!__BuildLogRootName!_%__TargetOS%__%__BuildArch%__%__BuildType%.err"
set __MsbuildLog=/flp:Verbosity=normal;LogFile=!__BuildLog!
set __MsbuildWrn=/flp1:WarningsOnly;LogFile=!__BuildWrn!
set __MsbuildErr=/flp2:ErrorsOnly;LogFile=!__BuildErr!
set __Logging=!__MsbuildLog! !__MsbuildWrn! !__MsbuildErr!

set __CmakeBuildToolArgs=

if %__Ninja% EQU 1 (
    set __CmakeBuildToolArgs=
) else (
    REM We pass the /m flag directly to MSBuild so that we can get both MSBuild and CL parallelism, which is fastest for our builds.
    set __CmakeBuildToolArgs=/nologo /m !__Logging!
)

"%CMakePath%" --build %__NativeTestIntermediatesDir% --target install --config %__BuildType% -- !__CmakeBuildToolArgs!

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
  %__RepoRootDir%\src\tests\build.proj -warnAsError:0 /t:BatchRestorePackages /nodeReuse:false^
  /p:RestoreDefaultOptimizationDataPackage=false /p:PortableBuild=true^
  /p:UsePartialNGENOptimization=false /maxcpucount^
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

set __NativeBinariesLayoutTypeArg=

if %__Ninja% == 0 (
    set __NativeBinariesLayoutTypeArg=/p:UseVisualStudioNativeBinariesLayout=true
)

for /l %%G in (1, 1, %__NumberOfTestGroups%) do (

    set __MsbuildLog=/flp:Verbosity=normal;LogFile="%__BuildLog%";Append=!__AppendToLog!
    set __MsbuildWrn=/flp1:WarningsOnly;LogFile="%__BuildWrn%";Append=!__AppendToLog!
    set __MsbuildErr=/flp2:ErrorsOnly;LogFile="%__BuildErr%";Append=!__AppendToLog!
    set __Logging='!__MsbuildLog!' '!__MsbuildWrn!' '!__MsbuildErr!'

    set __TestGroupToBuild=%%G

    if not "%__CopyNativeTestBinaries%" == "1" (
        set __MSBuildBuildArgs=!__RepoRootDir!\src\tests\build.proj
        set __MSBuildBuildArgs=!__MSBuildBuildArgs! -warnAsError:0
        set __MSBuildBuildArgs=!__MSBuildBuildArgs! /nodeReuse:false
        set __MSBuildBuildArgs=!__MSBuildBuildArgs! !__Logging!
        set __MSBuildBuildArgs=!__MSBuildBuildArgs! !TargetsWindowsMsbuildArg!
        set __MSBuildBuildArgs=!__MSBuildBuildArgs! !__msbuildArgs!
        set __MSBuildBuildArgs=!__MSBuildBuildArgs! !__PriorityArg! !__BuildNeedTargetArg!
        set __MSBuildBuildArgs=!__MSBuildBuildArgs! !__UnprocessedBuildArgs!
        set __MSBuildBuildArgs=!__MSBuildBuildArgs! /p:CopyNativeProjectBinaries=!__CopyNativeProjectsAfterCombinedTestBuild!
        set __MSBuildBuildArgs=!__MSBuildBuildArgs! /p:__SkipPackageRestore=true
        set __MSBuildBuildArgs=!__MSBuildBuildArgs! !__NativeBinariesLayoutTypeArg!
        echo Running: msbuild !__MSBuildBuildArgs!
        !__CommonMSBuildCmdPrefix! !__MSBuildBuildArgs!

        if errorlevel 1 (
            echo %__ErrMsgPrefix%%__MsgPrefix%Error: managed test build failed. Refer to the build log files for details:
            echo     %__BuildLog%
            echo     %__BuildWrn%
            echo     %__BuildErr%
            REM This is necessary because of a(n apparent) bug in the FOR /L command.  Under certain circumstances,
            REM such as when this script is invoke with CMD /C "build.cmd", a non-zero exit directly from
            REM within the loop body will not propagate to the caller.  For some reason, goto works around it.
            goto     :Exit_Failure
        )
    ) else (
        set __MSBuildBuildArgs=!__RepoRootDir!\src\tests\build.proj -warnAsError:0 /nodeReuse:false !__Logging! !TargetsWindowsMsbuildArg! !__msbuildArgs!  !__PriorityArg! !__BuildNeedTargetArg! !__NativeBinariesLayoutTypeArg!  !__UnprocessedBuildArgs! "/t:CopyAllNativeProjectReferenceBinaries"
        echo Running: msbuild !__MSBuildBuildArgs!
        !__CommonMSBuildCmdPrefix! !__MSBuildBuildArgs!

        if errorlevel 1 (
            echo %__ErrMsgPrefix%%__MsgPrefix%Error: copying native test binaries failed. Refer to the build log files for details:
            echo     %__BuildLog%
            echo     %__BuildWrn%
            echo     %__BuildErr%
            REM This is necessary because of a(n apparent) bug in the FOR /L command.  Under certain circumstances,
            REM such as when this script is invoke with CMD /C "build.cmd", a non-zero exit directly from
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
echo Running: dotnet msbuild %__RepoRootDir%\src\tests\run.proj /t:CheckTestBuild /nodeReuse:false /p:CLRTestPriorityToBuild=%__Priority% %__msbuildArgs% %__unprocessedBuildArgs%
powershell -NoProfile -ExecutionPolicy ByPass -NoLogo -File "%__RepoRootDir%\eng\common\msbuild.ps1" %__ArcadeScriptArgs%^
    %__RepoRootDir%\src\tests\run.proj /t:CheckTestBuild /nodeReuse:false /p:CLRTestPriorityToBuild=%__Priority% %__msbuildArgs% %__unprocessedBuildArgs%
if errorlevel 1 (
    echo %__ErrMsgPrefix%%__MsgPrefix%Error: Check Test Build failed.
    exit /b 1
)

:SkipManagedBuild

if "%__SkipGenerateLayout%" == "1" goto SkipGenerateLayout

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

set __BuildLogRootName=Tests_Overlay_Managed
set __BuildLog=%__LogsDir%\%__BuildLogRootName%_%__TargetOS%__%__BuildArch%__%__BuildType%.log
set __BuildWrn=%__LogsDir%\%__BuildLogRootName%_%__TargetOS%__%__BuildArch%__%__BuildType%.wrn
set __BuildErr=%__LogsDir%\%__BuildLogRootName%_%__TargetOS%__%__BuildArch%__%__BuildType%.err
set __MsbuildLog=/flp:Verbosity=normal;LogFile="%__BuildLog%"
set __MsbuildWrn=/flp1:WarningsOnly;LogFile="%__BuildWrn%"
set __MsbuildErr=/flp2:ErrorsOnly;LogFile="%__BuildErr%"
set __Logging=!__MsbuildLog! !__MsbuildWrn! !__MsbuildErr!

powershell -NoProfile -ExecutionPolicy ByPass -NoLogo -File "%__RepoRootDir%\eng\common\msbuild.ps1" %__ArcadeScriptArgs%^
  %__RepoRootDir%\src\tests\run.proj /t:CreateTestOverlay /nodeReuse:false^
  /p:RestoreDefaultOptimizationDataPackage=false /p:PortableBuild=true^
  /p:UsePartialNGENOptimization=false /maxcpucount^
  !__Logging! %__CommonMSBuildArgs% %__PriorityArg% %__BuildNeedTargetArg% %__UnprocessedBuildArgs%
if errorlevel 1 (
    echo %__ErrMsgPrefix%%__MsgPrefix%Error: Create Test Overlay failed. Refer to the build log files for details:
    echo     %__BuildLog%
    echo     %__BuildWrn%
    echo     %__BuildErr%
    exit /b 1
)

:SkipGenerateLayout

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
call "%__RepoRootDir%\dotnet.cmd" msbuild %__RepoRootDir%\src\tests\run.proj /nodereuse:false /p:BuildWrappers=true /p:TestBuildMode=%__TestBuildMode% !__Logging! %__msbuildArgs% %TargetsWindowsMsbuildArg% %__UnprocessedBuildArgs% /p:RuntimeFlavor=%RuntimeFlavor%
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
    echo %__MsgPrefix%Running crossgen2 on framework assemblies in CORE_ROOT: %CORE_ROOT%
    call :PrecompileFX
    if ERRORLEVEL 1 (
        echo %__ErrMsgPrefix%%__MsgPrefix%Error: crossgen2 precompilation of framework assemblies failed
        exit /b 1
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
echo skiprestorepackages: skip package restore
echo crossgen: Precompiles the framework managed assemblies
echo copynativeonly: Only copy the native test binaries to the managed output. Do not build the native or managed tests.
echo skipgeneratelayout: Do not generate the Core_Root layout
echo generatelayoutonly: Generate the Core_Root layout without building managed or native test components
echo targetsNonWindows:
echo Exclude- Optional parameter - specify location of default exclusion file ^(defaults to tests\issues.targets if not specified^)
echo     Set to "" to disable default exclusion file.
echo -- ... : all arguments following this tag will be passed directly to msbuild.
echo -priority=^<N^> : specify a set of tests that will be built and run, with priority N.
echo     0: Build only priority 0 cases as essential testcases (default)
echo     1: Build all tests with priority 0 and 1
echo     666: Build all tests with priority 0, 1 ... 666
echo allTargets: Build managed tests for all target platforms.
echo -verbose: enables detailed file logging for the msbuild tasks into the msbuild log file.
exit /b 1

:PrecompileFX

set "__CrossgenOutputDir=%__TestIntermediatesDir%\crossgen.out"

set __CrossgenCmd="%__RepoRootDir%\dotnet.cmd" "%CORE_ROOT%\R2RTest\R2RTest.dll" compile-framework -cr "%CORE_ROOT%" --output-directory "%__CrossgenOutputDir%" --release --nocleanup --target-arch %__BuildArch% -dop %NUMBER_OF_PROCESSORS% -m "%CORE_ROOT%\StandardOptimizationData.mibc"

if defined __CreatePdb (
    set __CrossgenCmd=!__CrossgenCmd! --pdb
)

if defined __CompositeBuildMode (
    set __CrossgenCmd=%__CrossgenCmd% --composite
) else (
    set __CrossgenCmd=%__CrossgenCmd% --crossgen2-parallelism 1
)

set __CrossgenDir=%__BinDir%
if defined __DoCrossgen (
    if /i "%__BuildArch%" == "arm" (
        set __CrossgenDir=!__CrossgenDir!\x86
    )
    if /i "%__BuildArch%" == "arm64" (
        set __CrossgenDir=!__CrossgenDir!\x64
    )
    set __CrossgenCmd=%__CrossgenCmd% --crossgen --nocrossgen2 --crossgen-path "!__CrossgenDir!\crossgen.exe"
) else (
    if /i "%__BuildArch%" == "arm" (
        set __CrossgenDir=!__CrossgenDir!\x64
    )
    if /i "%__BuildArch%" == "arm64" (
        set __CrossgenDir=!__CrossgenDir!\x64
    )
    if /i "%__BuildArch%" == "x86" (
        set __CrossgenDir=!__CrossgenDir!\x64
    )
    set __CrossgenCmd=%__CrossgenCmd% --verify-type-and-field-layout --crossgen2-path "!__CrossgenDir!\crossgen2\crossgen2.dll"
)

echo Running %__CrossgenCmd%
call %__CrossgenCmd%
set /a __exitCode = !errorlevel!

if %__exitCode% neq 0 (
    echo Failed to crossgen the framework
    exit /b 1
)

move /Y "%__CrossgenOutputDir%\*.dll" %CORE_ROOT% > nul

if defined __CreatePdb (
    move /Y "!__CrossgenOutputDir!\*.ni.pdb" !CORE_ROOT! > nul
    copy /Y "!__BinDir!\PDB\System.Private.CoreLib.ni.pdb" !CORE_ROOT! > nul
)

exit /b 0

REM Exit_Failure:
REM This is necessary because of a(n apparent) bug in the FOR /L command.  Under certain circumstances,
REM such as when this script is invoke with CMD /C "build.cmd", a non-zero exit directly from
REM within the loop body will not propagate to the caller.  For some reason, goto works around it.
:Exit_Failure
exit /b 1
