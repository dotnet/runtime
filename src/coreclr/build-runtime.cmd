@if not defined _echo @echo off
setlocal EnableDelayedExpansion EnableExtensions

:: Define a prefix for most output progress messages that come from this script. That makes
:: it easier to see where these are coming from. Note that there is a trailing space here.
set "__MsgPrefix=BUILD: "

echo %__MsgPrefix%Starting Build at %TIME%

set __ThisScriptFull="%~f0"

:: Note that the msbuild project files (specifically, dir.proj) will use the following variables, if set:
::      __BuildArch         -- default: x64
::      __BuildType         -- default: Debug
::      __TargetOS          -- default: windows
::      __ProjectDir        -- default: directory of the dir.props file
::      __RepoRootDir       -- default: directory two levels above the dir.props file
::      __RootBinDir        -- default: %__RepoRootDir%\artifacts\
::      __BinDir            -- default: %__RootBinDir%\%__TargetOS%.%__BuildArch.%__BuildType%\
::      __IntermediatesDir
::      __PackagesBinDir    -- default: %__BinDir%\.nuget
::
:: Thus, these variables are not simply internal to this script!

:: Set the default arguments for build
set __BuildArch=x64
set __BuildType=Debug
set __TargetOS=windows

:: Set the various build properties here so that CMake and MSBuild can pick them up
set "__ProjectDir=%~dp0"
:: remove trailing slash
if %__ProjectDir:~-1%==\ set "__ProjectDir=%__ProjectDir:~0,-1%"
set "__RepoRootDir=%__ProjectDir%\..\.."

set "__ProjectFilesDir=%__ProjectDir%"
set "__RootBinDir=%__RepoRootDir%\artifacts"

set __BuildAll=

set __BuildArchX64=0
set __BuildArchX86=0
set __BuildArchArm=0
set __BuildArchArm64=0

set __BuildTypeDebug=0
set __BuildTypeChecked=0
set __BuildTypeRelease=0

set __PgoInstrument=0
set __PgoOptimize=1
set __EnforcePgo=0
set __IbcTuning=
set __ConsoleLoggingParameters=/clp:ForceNoAlign;Summary

REM __PassThroughArgs is a set of things that will be passed through to nested calls to build.cmd
REM when using "all".
set __PassThroughArgs=

REM __UnprocessedBuildArgs are args that we pass to msbuild (e.g. /p:OfficialBuildId=value)
set "__remainingArgs=%*"
set __UnprocessedBuildArgs=
set __CommonMSBuildArgs=

set __BuildNative=1
set __BuildCrossArchNative=0
set __SkipCrossArchNative=0
set __SkipGenerateVersion=0
set __RestoreOptData=1
set __CrossArch=
set __CrossArch2=
set __CrossOS=0
set __PgoOptDataPath=
set __CMakeArgs=
set __Ninja=1
set __RequestedBuildComponents=

:Arg_Loop
if "%1" == "" goto ArgsDone
set "__remainingArgs=!__remainingArgs:*%1=!"

if /i "%1" == "/?"     goto Usage
if /i "%1" == "-?"     goto Usage
if /i "%1" == "/h"     goto Usage
if /i "%1" == "-h"     goto Usage
if /i "%1" == "/help"  goto Usage
if /i "%1" == "-help"  goto Usage
if /i "%1" == "--help" goto Usage

if /i "%1" == "-all"                 (set __BuildAll=1&shift&goto Arg_Loop)
if /i "%1" == "-x64"                 (set __BuildArchX64=1&shift&goto Arg_Loop)
if /i "%1" == "-x86"                 (set __BuildArchX86=1&shift&goto Arg_Loop)
if /i "%1" == "-arm"                 (set __BuildArchArm=1&shift&goto Arg_Loop)
if /i "%1" == "-arm64"               (set __BuildArchArm64=1&shift&goto Arg_Loop)

if /i "%1" == "-debug"               (set __BuildTypeDebug=1&shift&goto Arg_Loop)
if /i "%1" == "-checked"             (set __BuildTypeChecked=1&shift&goto Arg_Loop)
if /i "%1" == "-release"             (set __BuildTypeRelease=1&shift&goto Arg_Loop)

if /i "%1" == "-ci"                  (set __ArcadeScriptArgs="-ci"&set __ErrMsgPrefix=##vso[task.logissue type=error]&shift&goto Arg_Loop)

REM TODO these are deprecated remove them eventually
REM don't add more, use the - syntax instead
if /i "%1" == "all"                 (set __BuildAll=1&shift&goto Arg_Loop)
if /i "%1" == "x64"                 (set __BuildArchX64=1&shift&goto Arg_Loop)
if /i "%1" == "x86"                 (set __BuildArchX86=1&shift&goto Arg_Loop)
if /i "%1" == "arm"                 (set __BuildArchArm=1&shift&goto Arg_Loop)
if /i "%1" == "arm64"               (set __BuildArchArm64=1&shift&goto Arg_Loop)

if /i "%1" == "debug"               (set __BuildTypeDebug=1&shift&goto Arg_Loop)
if /i "%1" == "checked"             (set __BuildTypeChecked=1&shift&goto Arg_Loop)
if /i "%1" == "release"             (set __BuildTypeRelease=1&shift&goto Arg_Loop)

REM Explicitly block -Rebuild.
if /i "%1" == "Rebuild" (
    echo "ERROR: 'Rebuild' is not supported.  Please remove it."
    goto Usage
)
if /i "%1" == "-Rebuild" (
    echo "ERROR: 'Rebuild' is not supported.  Please remove it."
    goto Usage
)


REM All arguments after this point will be passed through directly to build.cmd on nested invocations
REM using the "all" argument, and must be added to the __PassThroughArgs variable.
if [!__PassThroughArgs!]==[] (
    set "__PassThroughArgs=%1"
) else (
    set "__PassThroughArgs=%__PassThroughArgs% %1"
)

if /i "%1" == "-alpinedac"           (set __BuildNative=0&set __BuildCrossArchNative=1&set __CrossArch=x64&set __CrossOS=1&set __TargetOS=alpine&shift&goto Arg_Loop)
if /i "%1" == "-linuxdac"            (set __BuildNative=0&set __BuildCrossArchNative=1&set __CrossArch=x64&set __CrossOS=1&set __TargetOS=Linux&shift&goto Arg_Loop)

if /i "%1" == "-cmakeargs"           (set __CMakeArgs=%2 %__CMakeArgs%&set "__remainingArgs=!__remainingArgs:*%2=!"&shift&shift&goto Arg_Loop)
if /i "%1" == "-configureonly"       (set __ConfigureOnly=1&set __BuildNative=1&shift&goto Arg_Loop)
if /i "%1" == "-skipconfigure"       (set __SkipConfigure=1&shift&goto Arg_Loop)
if /i "%1" == "-skipnative"          (set __BuildNative=0&shift&goto Arg_Loop)
if /i "%1" == "-skipcrossarchnative" (set __SkipCrossArchNative=1&shift&goto Arg_Loop)
if /i "%1" == "-skipgenerateversion" (set __SkipGenerateVersion=1&shift&goto Arg_Loop)
if /i "%1" == "-skiprestoreoptdata"  (set __RestoreOptData=0&shift&goto Arg_Loop)
REM -ninja is a no-op option since Ninja is now the default generator on Windows.
if /i "%1" == "-ninja"               (shift&goto Arg_Loop)
if /i "%1" == "-msbuild"             (set __Ninja=0&shift&goto Arg_Loop)
if /i "%1" == "-pgoinstrument"       (set __PgoInstrument=1&shift&goto Arg_Loop)
if /i "%1" == "-enforcepgo"          (set __EnforcePgo=1&shift&goto Arg_Loop)
if /i "%1" == "-nopgooptimize"       (set __PgoOptimize=0&shift&goto Arg_Loop)
if /i "%1" == "-component"           (set __RequestedBuildComponents=%__RequestedBuildComponents%-%2&set "__remainingArgs=!__remainingArgs:*%2=!"&shift&shift&goto Arg_Loop)

REM TODO these are deprecated remove them eventually
REM don't add more, use the - syntax instead
if /i "%1" == "configureonly"       (set __ConfigureOnly=1&set __BuildNative=1&shift&goto Arg_Loop)
if /i "%1" == "skipconfigure"       (set __SkipConfigure=1&shift&goto Arg_Loop)
if /i "%1" == "skipnative"          (set __BuildNative=0&shift&goto Arg_Loop)
if /i "%1" == "skipcrossarchnative" (set __SkipCrossArchNative=1&shift&goto Arg_Loop)
if /i "%1" == "skipgenerateversion" (set __SkipGenerateVersion=1&shift&goto Arg_Loop)
if /i "%1" == "skiprestoreoptdata"  (set __RestoreOptData=0&shift&goto Arg_Loop)
if /i "%1" == "pgoinstrument"       (set __PgoInstrument=1&shift&goto Arg_Loop)
if /i "%1" == "nopgooptimize"       (set __PgoOptimize=0&shift&goto Arg_Loop)
if /i "%1" == "enforcepgo"          (set __EnforcePgo=1&shift&goto Arg_Loop)

REM Preserve the equal sign for MSBuild properties
if "!__remainingArgs:~0,1!" == "="  (set "__UnprocessedBuildArgs=!__UnprocessedBuildArgs! %1=%2"&set "__remainingArgs=!__remainingArgs:*%2=!"&shift&shift&goto Arg_Loop)
set "__UnprocessedBuildArgs=!__UnprocessedBuildArgs! %1"&shift&goto Arg_Loop

:ArgsDone

:: Initialize VS environment
call %__RepoRootDir%\eng\native\init-vs-env.cmd
if NOT '%ERRORLEVEL%' == '0' goto ExitWithError

if defined VCINSTALLDIR (
    set "__VCToolsRoot=%VCINSTALLDIR%Auxiliary\Build"
)

if defined __BuildAll goto BuildAll

set /A __TotalSpecifiedBuildArch=__BuildArchX64 + __BuildArchX86 + __BuildArchArm + __BuildArchArm64
if %__TotalSpecifiedBuildArch% GTR 1 (
    echo Error: more than one build architecture specified, but "all" not specified.
    goto Usage
)

set __ProcessorArch=%PROCESSOR_ARCHITEW6432%
if "%__ProcessorArch%"=="" set __ProcessorArch=%PROCESSOR_ARCHITECTURE%

if %__BuildArchX64%==1      set __BuildArch=x64
if %__BuildArchX86%==1 (
    set __BuildArch=x86
    if /i "%__CrossOS%" NEQ "1" set __CrossArch=x64
)
if %__BuildArchArm%==1 (
    set __BuildArch=arm
    set __CrossArch=x86
    if /i "%__CrossOS%" NEQ "1" set __CrossArch2=x64
)
if %__BuildArchArm64%==1 (
    set __BuildArch=arm64
    if /i not "%__ProcessorArch%"=="ARM64" set __CrossArch=x64
)

set /A __TotalSpecifiedBuildType=__BuildTypeDebug + __BuildTypeChecked + __BuildTypeRelease
if %__TotalSpecifiedBuildType% GTR 1 (
    echo Error: more than one build type specified, but "all" not specified.
    goto Usage
)

if %__BuildTypeDebug%==1    set __BuildType=Debug
if %__BuildTypeChecked%==1  set __BuildType=Checked
if %__BuildTypeRelease%==1  set __BuildType=Release

set __CommonMSBuildArgs=/p:TargetOS=%__TargetOS% /p:Configuration=%__BuildType% /p:TargetArchitecture=%__BuildArch%

if %__EnforcePgo%==1 (
    if %__BuildArchArm%==1 (
        echo NOTICE: enforcepgo does nothing on arm architecture
        set __EnforcePgo=0
    )
    if %__BuildArchArm64%==1 (
        echo NOTICE: enforcepgo does nothing on arm64 architecture
        set __EnforcePgo=0
    )
)

REM Determine if this is a cross-arch build. Only do cross-arch build if we're also building native.

if %__SkipCrossArchNative% EQU 0 (
    if %__BuildNative% EQU 1 (
        if /i "%__BuildArch%"=="arm64" (
            if defined __CrossArch set __BuildCrossArchNative=1
        )
        if /i "%__BuildArch%"=="arm" (
            set __BuildCrossArchNative=1
        )
        if /i "%__BuildArch%"=="x86" (
            set __BuildCrossArchNative=1
        )
    )
)

REM Set the remaining variables based upon the determined build configuration

REM PGO optimization is only applied to release builds (see pgosupport.cmake). Disable PGO by default if not building release.
if NOT "%__BuildType%"=="Release" (
    set __PgoOptimize=0
)

set "__BinDir=%__RootBinDir%\bin\coreclr\%__TargetOS%.%__BuildArch%.%__BuildType%"
set "__IntermediatesDir=%__RootBinDir%\obj\coreclr\%__TargetOS%.%__BuildArch%.%__BuildType%"
set "__LogsDir=%__RootBinDir%\log\!__BuildType!"
set "__MsbuildDebugLogsDir=%__LogsDir%\MsbuildDebugLogs"
set "__ArtifactsIntermediatesDir=%__RepoRootDir%\artifacts\obj\coreclr\"
if "%__Ninja%"=="0" (set "__IntermediatesDir=%__IntermediatesDir%\ide")
set "__PackagesBinDir=%__BinDir%\.nuget"
set "__CrossComponentBinDir=%__BinDir%"
set "__CrossCompIntermediatesDir=%__IntermediatesDir%\crossgen"
set "__CrossComp2IntermediatesDir=%__IntermediatesDir%\crossgen_2"


if NOT "%__CrossArch%" == "" set __CrossComponentBinDir=%__CrossComponentBinDir%\%__CrossArch%
if NOT "%__CrossArch2%" == "" set __CrossComponent2BinDir=%__BinDir%\%__CrossArch2%

REM Generate path to be set for CMAKE_INSTALL_PREFIX to contain forward slash
set "__CMakeBinDir=%__BinDir%"
set "__CMakeBinDir=%__CMakeBinDir:\=/%"

if not exist "%__BinDir%"              md "%__BinDir%"
if not exist "%__IntermediatesDir%"    md "%__IntermediatesDir%"
if not exist "%__LogsDir%"             md "%__LogsDir%"
if not exist "%__MsbuildDebugLogsDir%" md "%__MsbuildDebugLogsDir%"

if not exist "%__RootBinDir%\Directory.Build.props" copy "%__ProjectDir%\EmptyProps.props" "%__RootBinDir%\Directory.Build.props"
if not exist "%__RootBinDir%\Directory.Build.targets" copy "%__ProjectDir%\EmptyProps.props" "%__RootBinDir%\Directory.Build.targets"

REM Set up the directory for MSBuild debug logs.
set MSBUILDDEBUGPATH=%__MsbuildDebugLogsDir%

echo %__MsgPrefix%Commencing CoreCLR product build

REM Set the remaining variables based upon the determined build configuration

echo %__MsgPrefix%Checking prerequisites

set __CMakeNeeded=1
if %__BuildNative%==0 if %__BuildCrossArchNative%==0 set __CMakeNeeded=0
if %__CMakeNeeded%==1 (
    REM Eval the output from set-cmake-path.ps1
    for /f "delims=" %%a in ('powershell -NoProfile -ExecutionPolicy ByPass "& ""%__RepoRootDir%\eng\native\set-cmake-path.ps1"""') do %%a
    echo %__MsgPrefix%Using CMake from !CMakePath!
)

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

REM =========================================================================================
REM ===
REM === Start the build steps
REM ===
REM =========================================================================================

@if defined _echo @echo on

if %__SkipGenerateVersion% EQU 0 (
    echo %__MsgPrefix%Generating native version headers
    set "__BinLog=\"%__LogsDir%\GenerateVersionHeaders_%__TargetOS%__%__BuildArch%__%__BuildType%.binlog\""
    powershell -NoProfile -ExecutionPolicy ByPass -NoLogo -File "%__RepoRootDir%\eng\common\msbuild.ps1" /clp:nosummary %__ArcadeScriptArgs%^
        "%__RepoRootDir%\eng\empty.csproj" /t:GenerateRuntimeVersionFile /restore^
        /p:NativeVersionFile="%__RootBinDir%\obj\coreclr\_version.h"^
        /p:RuntimeVersionFile="%__RootBinDir%\obj\coreclr\runtime_version.h"^
        %__CommonMSBuildArgs% %__UnprocessedBuildArgs% /bl:!__BinLog!
    if not !errorlevel! == 0 (
        set __exitCode=!errorlevel!
        echo %__ErrMsgPrefix%%__MsgPrefix%Error: Failed to generate version headers.
        goto ExitWithCode
    )
)

REM =========================================================================================
REM ===
REM === Restore optimization profile data
REM ===
REM =========================================================================================

set __PgoOptDataPath=
if %__PgoOptimize% EQU 1 (
    set OptDataProjectFilePath=%__ProjectDir%\.nuget\optdata\optdata.csproj
    set __OptDataRestoreArg=
    if %__RestoreOptData% EQU 1 (
        set __OptDataRestoreArg=/restore
    )
    set PgoDataPackagePathOutputFile=%__IntermediatesDir%\optdatapath.txt
    set "__BinLog=\"%__LogsDir%\PgoVersionRead_%__TargetOS%__%__BuildArch%__%__BuildType%.binlog\""

    REM Parse the optdata package versions out of msbuild so that we can pass them on to CMake
    powershell -NoProfile -ExecutionPolicy ByPass -NoLogo -File "%__RepoRootDir%\eng\common\msbuild.ps1" /clp:nosummary %__ArcadeScriptArgs%^
        "!OptDataProjectFilePath!" /t:DumpPgoDataPackagePath^
        /p:PgoDataPackagePathOutputFile="!PgoDataPackagePathOutputFile!" !__OptDataRestoreArg!^
        %__CommonMSBuildArgs% %__UnprocessedBuildArgs% /bl:!__BinLog!

    if not !errorlevel! == 0 (
        set __exitCode=!errorlevel!
        echo %__ErrMsgPrefix%Failed to get PGO data package path.
        goto ExitWithCode
    )
    if not exist "!PgoDataPackagePathOutputFile!" (
        echo %__ErrMsgPrefix%Failed to get PGO data package path.
        goto ExitWithError
    )

    set /p __PgoOptDataPath=<"!PgoDataPackagePathOutputFile!"
)

REM =========================================================================================
REM ===
REM === Locate Python
REM ===
REM =========================================================================================

set __IntermediatesIncDir=%__IntermediatesDir%\src\inc
set __IntermediatesEventingDir=%__ArtifactsIntermediatesDir%\Eventing\%__BuildArch%\%__BuildType%

REM Find python and set it to the variable PYTHON
set _C=-c "import sys; sys.stdout.write(sys.executable)"
(py -3 %_C% || py -2 %_C% || python3 %_C% || python2 %_C% || python %_C%) > %TEMP%\pythonlocation.txt 2> NUL
set _C=
set /p PYTHON=<%TEMP%\pythonlocation.txt

if NOT DEFINED PYTHON (
    echo %__ErrMsgPrefix%%__MsgPrefix%Error: Could not find a Python installation.
    goto ExitWithError
)

set __CMakeTarget=
for /f "delims=" %%a in ("-%__RequestedBuildComponents%-") do (
    set "string=%%a"
    if not "!string:-jit-=!"=="!string!" (
        set __CMakeTarget=!__CMakeTarget! jit
    )
    if not "!string:-alljits-=!"=="!string!" (
        set __CMakeTarget=!__CMakeTarget! alljits
    )
    if not "!string:-runtime-=!"=="!string!" (
        set __CMakeTarget=!__CMakeTarget! runtime
    )
    if not "!string:-paltests-=!"=="!string!" (
        set __CMakeTarget=!__CMakeTarget! paltests_install
    )
    if not "!string:-iltools-=!"=="!string!" (
        set __CMakeTarget=!__CMakeTarget! iltools
    )
)
if [!__CMakeTarget!] == [] (
    set __CMakeTarget=install
)

REM =========================================================================================
REM ===
REM === Build Cross-Architecture Native Components (if applicable)
REM ===
REM =========================================================================================

if %__BuildCrossArchNative% EQU 1 (
    REM Scope environment changes start {
    setlocal

    echo %__MsgPrefix%Commencing build of cross architecture native components for %__TargetOS%.%__BuildArch%.%__BuildType%

    REM Set the environment for the cross-arch native build
    set __VCBuildArch=x86_amd64
    if /i "%__CrossArch%" == "x86" ( set __VCBuildArch=x86 )

    echo %__MsgPrefix%Using environment: "%__VCToolsRoot%\vcvarsall.bat" !__VCBuildArch!
    call                                 "%__VCToolsRoot%\vcvarsall.bat" !__VCBuildArch!
    @if defined _echo @echo on

    if not exist "%__CrossCompIntermediatesDir%" md "%__CrossCompIntermediatesDir%"
    if defined __SkipConfigure goto SkipConfigureCrossBuild

    set __CMakeBinDir=%__CrossComponentBinDir%
    set "__CMakeBinDir=!__CMakeBinDir:\=/!"

    if %__Ninja% EQU 1 (
        set __ExtraCmakeArgs="-DCMAKE_BUILD_TYPE=!__BuildType!"
    )

    set __ExtraCmakeArgs=!__ExtraCmakeArgs! "-DCLR_CROSS_COMPONENTS_BUILD=1" "-DCLR_CMAKE_TARGET_ARCH=%__BuildArch%" "-DCLR_CMAKE_TARGET_OS=%__TargetOS%" "-DCLR_CMAKE_PGO_INSTRUMENT=0" "-DCLR_CMAKE_OPTDATA_PATH=%__PgoOptDataPath%" "-DCLR_CMAKE_PGO_OPTIMIZE=0" %__CMakeArgs%
    call "%__RepoRootDir%\eng\native\gen-buildsys.cmd" "%__ProjectDir%" "%__CrossCompIntermediatesDir%" %__VSVersion% %__CrossArch% !__ExtraCmakeArgs!

    if not !errorlevel! == 0 (
        echo %__ErrMsgPrefix%%__MsgPrefix%Error: failed to generate cross architecture native component build project %__CrossArch%!
        goto ExitWithError
    )
    @if defined _echo @echo on

:SkipConfigureCrossBuild
    if not exist "%__CrossCompIntermediatesDir%\CMakeCache.txt" (
        echo %__ErrMsgPrefix%%__MsgPrefix%Error: unable to find generated cross architecture native component build project %__CrossArch%!
        goto ExitWithError
    )

    if defined __ConfigureOnly goto SkipCrossCompBuild

    set __BuildLogRootName=Cross
    set "__BuildLog="%__LogsDir%\!__BuildLogRootName!_%__TargetOS%__%__BuildArch%__%__BuildType%.log""
    set "__BuildWrn="%__LogsDir%\!__BuildLogRootName!_%__TargetOS%__%__BuildArch%__%__BuildType%.wrn""
    set "__BuildErr="%__LogsDir%\!__BuildLogRootName!_%__TargetOS%__%__BuildArch%__%__BuildType%.err""
    set "__BinLog="%__LogsDir%\!__BuildLogRootName!_%__TargetOS%__%__BuildArch%__%__BuildType%.binlog""
    set "__MsbuildLog=/flp:Verbosity=normal;LogFile=!__BuildLog!"
    set "__MsbuildWrn=/flp1:WarningsOnly;LogFile=!__BuildWrn!"
    set "__MsbuildErr=/flp2:ErrorsOnly;LogFile=!__BuildErr!"
    set "__MsbuildBinLog=/bl:!__BinLog!"
    set "__Logging=!__MsbuildLog! !__MsbuildWrn! !__MsbuildErr! !__MsbuildBinLog! !__ConsoleLoggingParameters!"

    set __CmakeBuildToolArgs=

    if %__Ninja% EQU 1 (
        set __CmakeBuildToolArgs=
    ) else (
        REM We pass the /m flag directly to MSBuild so that we can get both MSBuild and CL parallelism, which is fastest for our builds.
        set __CmakeBuildToolArgs=/nologo /m !__Logging!
    )

    "%CMakePath%" --build %__CrossCompIntermediatesDir% --target crosscomponents --config %__BuildType% -- !__CmakeBuildToolArgs!

    if not !errorlevel! == 0 (
        set __exitCode=!errorlevel!
        echo %__ErrMsgPrefix%%__MsgPrefix%Error: cross-arch components build failed. Refer to the build log files for details.
        echo     !__BuildLog!
        echo     !__BuildWrn!
        echo     !__BuildErr!
        goto ExitWithCode
    )
:SkipCrossCompBuild
    REM } Scope environment changes end
    endlocal

    if NOT "%__CrossArch2%" == "" (
        REM Scope environment changes start {
        setlocal

        echo %__MsgPrefix%Commencing build of cross architecture native components for %__TargetOS%.%__BuildArch%.%__BuildType% hosted on %__CrossArch2%

        if /i "%__CrossArch2%" == "x86" ( set __VCBuildArch=x86 )
        if /i "%__CrossArch2%" == "x64" ( set __VCBuildArch=x86_amd64 )

        echo %__MsgPrefix%Using environment: "%__VCToolsRoot%\vcvarsall.bat" !__VCBuildArch!
        call                                 "%__VCToolsRoot%\vcvarsall.bat" !__VCBuildArch!
        @if defined _echo @echo on

        if not exist "%__CrossComp2IntermediatesDir%" md "%__CrossComp2IntermediatesDir%"
        if defined __SkipConfigure goto SkipConfigureCrossBuild2

        set __CMakeBinDir="%__CrossComponent2BinDir%"
        set "__CMakeBinDir=!__CMakeBinDir:\=/!"

        if %__Ninja% EQU 1 (
            set __ExtraCmakeArgs="-DCMAKE_BUILD_TYPE=!__BuildType!"
        )

        set __ExtraCmakeArgs=!__ExtraCmakeArgs! "-DCLR_CROSS_COMPONENTS_BUILD=1" "-DCLR_CMAKE_TARGET_ARCH=%__BuildArch%" "-DCLR_CMAKE_TARGET_OS=%__TargetOS%" "-DCLR_CMAKE_PGO_INSTRUMENT=0" "-DCLR_CMAKE_OPTDATA_PATH=%__PgoOptDataPath%" "-DCLR_CMAKE_PGO_OPTIMIZE=0" "-DCMAKE_SYSTEM_VERSION=10.0" %__CMakeArgs%
        call "%__RepoRootDir%\eng\native\gen-buildsys.cmd" "%__ProjectDir%" "%__CrossComp2IntermediatesDir%" %__VSVersion% %__CrossArch2% !__ExtraCmakeArgs!

        if not !errorlevel! == 0 (
            echo %__ErrMsgPrefix%%__MsgPrefix%Error: failed to generate cross architecture native component build project %__CrossArch2%!
            goto ExitWithError
        )

        set __VCBuildArch=x86_amd64
        if /i "%__CrossArch%" == "x86" ( set __VCBuildArch=x86 )
        @if defined _echo @echo on

        if not exist "%__CrossComp2IntermediatesDir%\CMakeCache.txt" (
        echo %__ErrMsgPrefix%%__MsgPrefix%Error: unable to find generated cross architecture native component build project %__CrossArch2%!
            goto ExitWithError
        )

:SkipConfigureCrossBuild2
        set __BuildLogRootName=Cross2
        set "__BuildLog=%__LogsDir%\!__BuildLogRootName!_%__TargetOS%__%__BuildArch%__%__BuildType%.log"
        set "__BuildWrn=%__LogsDir%\!__BuildLogRootName!_%__TargetOS%__%__BuildArch%__%__BuildType%.wrn"
        set "__BuildErr=%__LogsDir%\!__BuildLogRootName!_%__TargetOS%__%__BuildArch%__%__BuildType%.err"
        set "__BinLog=%__LogsDir%\!__BuildLogRootName!_%__TargetOS%__%__BuildArch%__%__BuildType%.binlog"
        set "__MsbuildLog=/flp:Verbosity=normal;LogFile=!__BuildLog!"
        set "__MsbuildWrn=/flp1:WarningsOnly;LogFile=!__BuildWrn!"
        set "__MsbuildErr=/flp2:ErrorsOnly;LogFile=!__BuildErr!"
        set "__MsbuildBinLog=/bl:!__BinLog!"
        set "__Logging=!__MsbuildLog! !__MsbuildWrn! !__MsbuildErr! !__MsbuildBinLog! !__ConsoleLoggingParameters!"

        set __CmakeBuildToolArgs=

        if %__Ninja% EQU 1 (
            set __CmakeBuildToolArgs=
        ) else (
            REM We pass the /m flag directly to MSBuild so that we can get both MSBuild and CL parallelism, which is fastest for our builds.
            set __CmakeBuildToolArgs=/nologo /m !__Logging!
        )

        "%CMakePath%" --build %__CrossComp2IntermediatesDir% --target crosscomponents --config %__BuildType% -- !__CmakeBuildToolArgs!

        if not !errorlevel! == 0 (
            set __exitCode=!errorlevel!
            echo %__ErrMsgPrefix%%__MsgPrefix%Error: cross-arch components build failed. Refer to the build log files for details.
            echo     !__BuildLog!
            echo     !__BuildWrn!
            echo     !__BuildErr!
            goto ExitWithCode
        )
:SkipCrossCompBuild2
        REM } Scope environment changes end
        endlocal
    )
)

REM =========================================================================================
REM ===
REM === Build the CLR VM
REM ===
REM =========================================================================================

if %__BuildNative% EQU 1 (
    REM Scope environment changes start {
    setlocal

    echo %__MsgPrefix%Commencing build of native components for %__TargetOS%.%__BuildArch%.%__BuildType%

    REM Set the environment for the native build
    set __VCBuildArch=amd64
    if /i "%__BuildArch%" == "x86" ( set __VCBuildArch=x86 )
    if /i "%__BuildArch%" == "arm" (
        set __VCBuildArch=x86_arm
        set ___CrossBuildDefine="-DCLR_CMAKE_CROSS_ARCH=1" "-DCLR_CMAKE_CROSS_HOST_ARCH=%__CrossArch%"
    )
    if /i "%__BuildArch%" == "arm64" (
        set __VCBuildArch=x86_arm64
        if defined __CrossArch (
            set ___CrossBuildDefine="-DCLR_CMAKE_CROSS_ARCH=1" "-DCLR_CMAKE_CROSS_HOST_ARCH=%__CrossArch%"
        )
    )

    echo %__MsgPrefix%Using environment: "%__VCToolsRoot%\vcvarsall.bat" !__VCBuildArch!
    call                                 "%__VCToolsRoot%\vcvarsall.bat" !__VCBuildArch!
    @if defined _echo @echo on

    if defined __SkipConfigure goto SkipConfigure

    echo %__MsgPrefix%Regenerating the Visual Studio solution

    if %__Ninja% EQU 1 (
        set __ExtraCmakeArgs="-DCMAKE_BUILD_TYPE=!__BuildType!"
    )

    set __ExtraCmakeArgs=!__ExtraCmakeArgs! !___CrossBuildDefine! "-DCLR_CMAKE_PGO_INSTRUMENT=%__PgoInstrument%" "-DCLR_CMAKE_OPTDATA_PATH=%__PgoOptDataPath%" "-DCLR_CMAKE_PGO_OPTIMIZE=%__PgoOptimize%" %__CMakeArgs%
    call "%__RepoRootDir%\eng\native\gen-buildsys.cmd" "%__ProjectDir%" "%__IntermediatesDir%" %__VSVersion% %__BuildArch% !__ExtraCmakeArgs!
    if not !errorlevel! == 0 (
        echo %__ErrMsgPrefix%%__MsgPrefix%Error: failed to generate native component build project!
        goto ExitWithError
    )

    @if defined _echo @echo on

:SkipConfigure
    if not exist "%__IntermediatesDir%\CMakeCache.txt" (
        echo %__ErrMsgPrefix%%__MsgPrefix%Error: unable to find generated native component build project!
        goto ExitWithError
    )

    if defined __ConfigureOnly goto SkipNativeBuild

    set __BuildLogRootName=CoreCLR
    set "__BuildLog="%__LogsDir%\!__BuildLogRootName!_%__TargetOS%__%__BuildArch%__%__BuildType%.log""
    set "__BuildWrn="%__LogsDir%\!__BuildLogRootName!_%__TargetOS%__%__BuildArch%__%__BuildType%.wrn""
    set "__BuildErr="%__LogsDir%\!__BuildLogRootName!_%__TargetOS%__%__BuildArch%__%__BuildType%.err""
    set "__BinLog="%__LogsDir%\!__BuildLogRootName!_%__TargetOS%__%__BuildArch%__%__BuildType%.binlog""
    set "__MsbuildLog=/flp:Verbosity=normal;LogFile=!__BuildLog!"
    set "__MsbuildWrn=/flp1:WarningsOnly;LogFile=!__BuildWrn!"
    set "__MsbuildErr=/flp2:ErrorsOnly;LogFile=!__BuildErr!"
    set "__MsbuildBinLog=/bl:!__BinLog!"
    set "__Logging=!__MsbuildLog! !__MsbuildWrn! !__MsbuildErr! !__MsbuildBinLog! !__ConsoleLoggingParameters!"

    set __CmakeBuildToolArgs=
    if %__Ninja% EQU 1 (
        set __CmakeBuildToolArgs=
    ) else (
        REM We pass the /m flag directly to MSBuild so that we can get both MSBuild and CL parallelism, which is fastest for our builds.
        set __CmakeBuildToolArgs=/nologo /m !__Logging!
    )

    echo running "%CMakePath%" --build %__IntermediatesDir% --target %__CMakeTarget% --config %__BuildType% -- !__CmakeBuildToolArgs!
    "%CMakePath%" --build %__IntermediatesDir% --target %__CMakeTarget% --config %__BuildType% -- !__CmakeBuildToolArgs!

    if not !errorlevel! == 0 (
        set __exitCode=!errorlevel!
        echo %__ErrMsgPrefix%%__MsgPrefix%Error: native component build failed. Refer to the build log files for details.
        echo     !__BuildLog!
        echo     !__BuildWrn!
        echo     !__BuildErr!
        goto ExitWithCode
    )

    if /i "%__BuildArch%" == "arm64" goto SkipCopyUcrt

    if not defined UCRTVersion (
        echo %__ErrMsgPrefix%%__MsgPrefix%Error: Please install Windows 10 SDK.
        goto ExitWithError
    )

    set "__UCRTDir=%UniversalCRTSdkDir%Redist\%UCRTVersion%\ucrt\DLLs\%__BuildArch%\"

    xcopy /Y/I/E/D/F "!__UCRTDir!*.dll" "%__BinDir%\Redist\ucrt\DLLs\%__BuildArch%"
    if not !errorlevel! == 0 (
        set __exitCode=!errorlevel!
        echo %__ErrMsgPrefix%%__MsgPrefix%Error: Failed to copy the Universal CRT to the artifacts directory.
        goto ExitWithCode
    )

:SkipCopyUcrt
    if %__EnforcePgo% EQU 1 (
        set PgoCheckCmd="!PYTHON!" "!__ProjectDir!\scripts\pgocheck.py" "!__BinDir!\coreclr.dll" "!__BinDir!\clrjit.dll"
        echo !PgoCheckCmd!
        !PgoCheckCmd!
        if not !errorlevel! == 0 (
            set __exitCode=!errorlevel!
            echo !__ErrMsgPrefix!!__MsgPrefix!Error: Error running pgocheck.py on coreclr and clrjit.
            goto ExitWithCode
        )
    )

:SkipNativeBuild
    REM } Scope environment changes end
    endlocal
)

REM =========================================================================================
REM ===
REM === All builds complete!
REM ===
REM =========================================================================================

echo %__MsgPrefix%Build succeeded.  Finished at %TIME%
echo %__MsgPrefix%Product binaries are available at !__BinDir!

exit /b 0

REM =========================================================================================
REM ===
REM === Handle the "all" case.
REM ===
REM =========================================================================================

:BuildAll

set __BuildArchList=

set /A __TotalSpecifiedBuildArch=__BuildArchX64 + __BuildArchX86 + __BuildArchArm + __BuildArchArm64
if %__TotalSpecifiedBuildArch% EQU 0 (
    REM Nothing specified means we want to build all architectures.
    set __BuildArchList=x64 x86 arm arm64
)

REM Otherwise, add all the specified architectures to the list.

if %__BuildArchX64%==1      set __BuildArchList=%__BuildArchList% x64
if %__BuildArchX86%==1      set __BuildArchList=%__BuildArchList% x86
if %__BuildArchArm%==1      set __BuildArchList=%__BuildArchList% arm
if %__BuildArchArm64%==1    set __BuildArchList=%__BuildArchList% arm64

set __BuildTypeList=

set /A __TotalSpecifiedBuildType=__BuildTypeDebug + __BuildTypeChecked + __BuildTypeRelease
if %__TotalSpecifiedBuildType% EQU 0 (
    REM Nothing specified means we want to build all build types.
    set __BuildTypeList=Debug Checked Release
)

if %__BuildTypeDebug%==1    set __BuildTypeList=%__BuildTypeList% Debug
if %__BuildTypeChecked%==1  set __BuildTypeList=%__BuildTypeList% Checked
if %__BuildTypeRelease%==1  set __BuildTypeList=%__BuildTypeList% Release

REM Create a temporary file to collect build results. We always build all flavors specified, and
REM report a summary of the results at the end.

set __AllBuildSuccess=true
set __BuildResultFile=%TEMP%\build-all-summary-%RANDOM%.txt
if exist %__BuildResultFile% del /f /q %__BuildResultFile%

for %%i in (%__BuildArchList%) do (
    for %%j in (%__BuildTypeList%) do (
        call :BuildOne %%i %%j
    )
)

if %__AllBuildSuccess%==true (
    echo %__MsgPrefix%All builds succeeded!
    exit /b 0
) else (
    echo %__MsgPrefix%Builds failed:
    type %__BuildResultFile%
    del /f /q %__BuildResultFile%
    goto ExitWithError
)

REM This code is unreachable, but leaving it nonetheless, just in case things change.
exit /b 99

:BuildOne
set __BuildArch=%1
set __BuildType=%2
set __NextCmd=call %__ThisScriptFull% %__BuildArch% %__BuildType% %__PassThroughArgs%
echo %__MsgPrefix%Invoking: %__NextCmd%
%__NextCmd%
if not !errorlevel! == 0 (
    echo %__MsgPrefix%    %__BuildArch% %__BuildType% %__PassThroughArgs% >> %__BuildResultFile%
    set __AllBuildSuccess=false
)
exit /b 0

REM =========================================================================================
REM ===
REM === Helper routines
REM ===
REM =========================================================================================


REM =========================================================================================
REM === These two routines are intended for the exit code to propagate to the parent process
REM === Like MSBuild or Powershell. If we directly exit /b 1 from within a if statement in
REM === any of the routines, the exit code is not propagated.
REM =========================================================================================
:ExitWithError
exit /b 1

:ExitWithCode
exit /b !__exitCode!

:Usage
echo.
echo Build the CoreCLR repo.
echo.
echo Usage:
echo     build-runtime.cmd [option1] [option2]
echo or:
echo     build-runtime.cmd all [option1] [option2]
echo.
echo All arguments are optional. The options are:
echo.
echo.-? -h -help --help: view this message.
echo -all: Builds all configurations and platforms.
echo Build architecture: one of -x64, -x86, -arm, -arm64 ^(default: -x64^).
echo Build type: one of -Debug, -Checked, -Release ^(default: -Debug^).
echo -component ^<name^> : specify this option one or more times to limit components built to those specified.
echo                     Allowed ^<name^>: jit alljits runtime paltests iltools
echo -nopgooptimize: do not use profile guided optimizations.
echo -enforcepgo: verify after the build that PGO was used for key DLLs, and fail the build if not
echo -pgoinstrument: generate instrumented code for profile guided optimization enabled binaries.
echo -cmakeargs: user-settable additional arguments passed to CMake.
echo -configureonly: skip all builds; only run CMake ^(default: CMake and builds are run^)
echo -skipconfigure: skip CMake ^(default: CMake is run^)
echo -skipnative: skip building native components ^(default: native components are built^).
echo -skipcrossarchnative: skip building cross-architecture native components ^(default: components are built^).
echo -skiprestoreoptdata: skip restoring optimization data used by profile-based optimizations.
echo -skipgenerateversion: skip generating the native version headers.
echo.
echo Examples:
echo     build-runtime
echo        -- builds x64 debug, all components
echo     build-runtime -component jit
echo        -- builds x64 debug, just the JIT
echo     build-runtime -component jit -component runtime
echo        -- builds x64 debug, just the JIT and runtime
echo.
echo If "all" is specified, then all build architectures and types are built. If, in addition,
echo one or more build architectures or types is specified, then only those build architectures
echo and types are built.
echo.
echo For example:
echo     build-runtime -all
echo        -- builds all architectures, and all build types per architecture
echo     build-runtime -all -x86
echo        -- builds all build types for x86
echo     build-runtime -all -x64 -x86 -Checked -Release
echo        -- builds x64 and x86 architectures, Checked and Release build types for each
exit /b 1
