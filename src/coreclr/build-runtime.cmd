@if not defined _echo @echo off
setlocal EnableDelayedExpansion EnableExtensions

:: Define a prefix for most output progress messages that come from this script. That makes
:: it easier to see where these are coming from. Note that there is a trailing space here.
set "__MsgPrefix=BUILD: "

echo %__MsgPrefix%Starting Build at %TIME%

set __ThisScriptFull="%~f0"

:: Note that the msbuild project files (specifically, dir.proj) will use the following variables, if set:
::      __TargetArch        -- default: x64
::      __BuildType         -- default: Debug
::      __TargetOS          -- default: windows
::      __ProjectDir        -- default: directory of the dir.props file
::      __RepoRootDir       -- default: directory two levels above the dir.props file
::      __RootBinDir        -- default: %__RepoRootDir%\artifacts\
::      __BinDir            -- default: %__RootBinDir%\obj\coreclr\%__TargetOS%.%__TargetArch.%__BuildType%\
::      __IntermediatesDir
::      __PackagesBinDir    -- default: %__BinDir%\.nuget
::
:: Thus, these variables are not simply internal to this script!

:: Set the default arguments for build
set __TargetArch=x64
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

set __TargetArchX64=0
set __TargetArchX86=0
set __TargetArchArm=0
set __TargetArchArm64=0

set __BuildTypeDebug=0
set __BuildTypeChecked=0
set __BuildTypeRelease=0

set __PgoInstrument=0
set __PgoOptimize=0
set __EnforcePgo=0
set __ConsoleLoggingParameters=/clp:ForceNoAlign;Summary

REM __PassThroughArgs is a set of things that will be passed through to nested calls to build.cmd
REM when using "all".
set __PassThroughArgs=

REM __UnprocessedBuildArgs are args that we pass to msbuild (e.g. /p:OfficialBuildId=value)
set "__remainingArgs=%*"
set __UnprocessedBuildArgs=

set __BuildNative=1
set __RestoreOptData=1
set __HostArch=
set __HostArch2=
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
if /i "%1" == "-x64"                 (set __TargetArchX64=1&shift&goto Arg_Loop)
if /i "%1" == "-x86"                 (set __TargetArchX86=1&shift&goto Arg_Loop)
if /i "%1" == "-arm"                 (set __TargetArchArm=1&shift&goto Arg_Loop)
if /i "%1" == "-arm64"               (set __TargetArchArm64=1&shift&goto Arg_Loop)

if /i "%1" == "-debug"               (set __BuildTypeDebug=1&shift&goto Arg_Loop)
if /i "%1" == "-checked"             (set __BuildTypeChecked=1&shift&goto Arg_Loop)
if /i "%1" == "-release"             (set __BuildTypeRelease=1&shift&goto Arg_Loop)

if /i "%1" == "-ci"                  (set __ArcadeScriptArgs="-ci"&set __ErrMsgPrefix=##vso[task.logissue type=error]&shift&goto Arg_Loop)

REM TODO these are deprecated remove them eventually
REM don't add more, use the - syntax instead
if /i "%1" == "all"                 (set __BuildAll=1&shift&goto Arg_Loop)
if /i "%1" == "x64"                 (set __TargetArchX64=1&shift&goto Arg_Loop)
if /i "%1" == "x86"                 (set __TargetArchX86=1&shift&goto Arg_Loop)
if /i "%1" == "arm"                 (set __TargetArchArm=1&shift&goto Arg_Loop)
if /i "%1" == "arm64"               (set __TargetArchArm64=1&shift&goto Arg_Loop)

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

if /i "%1" == "-hostarch"            (set __HostArch=%2&shift&shift&goto Arg_Loop)
if /i "%1" == "-os"                  (set __TargetOS=%2&shift&shift&goto Arg_Loop)

if /i "%1" == "-cmakeargs"           (set __CMakeArgs=%2 %__CMakeArgs%&set __remainingArgs="!__remainingArgs:*%2=!"&shift&shift&goto Arg_Loop)
if /i "%1" == "-configureonly"       (set __ConfigureOnly=1&set __BuildNative=1&shift&goto Arg_Loop)
if /i "%1" == "-skipconfigure"       (set __SkipConfigure=1&shift&goto Arg_Loop)
if /i "%1" == "-skipnative"          (set __BuildNative=0&shift&goto Arg_Loop)
REM -ninja is a no-op option since Ninja is now the default generator on Windows.
if /i "%1" == "-ninja"               (shift&goto Arg_Loop)
if /i "%1" == "-msbuild"             (set __Ninja=0&shift&goto Arg_Loop)
if /i "%1" == "-pgoinstrument"       (set __PgoInstrument=1&shift&goto Arg_Loop)
if /i "%1" == "-enforcepgo"          (set __EnforcePgo=1&shift&goto Arg_Loop)
if /i "%1" == "-pgodatapath"         (set __PgoOptDataPath=%2&set __PgoOptimize=1&shift&shift&goto Arg_Loop)
if /i "%1" == "-component"           (set __RequestedBuildComponents=%__RequestedBuildComponents%-%2&set "__remainingArgs=!__remainingArgs:*%2=!"&shift&shift&goto Arg_Loop)

REM TODO these are deprecated remove them eventually
REM don't add more, use the - syntax instead
if /i "%1" == "configureonly"       (set __ConfigureOnly=1&set __BuildNative=1&shift&goto Arg_Loop)
if /i "%1" == "skipconfigure"       (set __SkipConfigure=1&shift&goto Arg_Loop)
if /i "%1" == "skipnative"          (set __BuildNative=0&shift&goto Arg_Loop)
if /i "%1" == "pgoinstrument"       (set __PgoInstrument=1&shift&goto Arg_Loop)
if /i "%1" == "enforcepgo"          (set __EnforcePgo=1&shift&goto Arg_Loop)

set "__UnprocessedBuildArgs=!__UnprocessedBuildArgs! %1"&shift&goto Arg_Loop

:ArgsDone

:: Initialize VS environment
call %__RepoRootDir%\eng\native\init-vs-env.cmd
if NOT '%ERRORLEVEL%' == '0' goto ExitWithError

if defined VCINSTALLDIR (
    set "__VCToolsRoot=%VCINSTALLDIR%Auxiliary\Build"
)

if defined __BuildAll goto BuildAll

set /A __TotalSpecifiedTargetArch=__TargetArchX64 + __TargetArchX86 + __TargetArchArm + __TargetArchArm64
if %__TotalSpecifiedTargetArch% GTR 1 (
    echo Error: more than one build architecture specified, but "all" not specified.
    goto Usage
)

set __ProcessorArch=%PROCESSOR_ARCHITEW6432%
if "%__ProcessorArch%"=="" set __ProcessorArch=%PROCESSOR_ARCHITECTURE%

if %__TargetArchX64%==1   set __TargetArch=x64
if %__TargetArchX86%==1   set __TargetArch=x86
if %__TargetArchArm%==1   set __TargetArch=arm
if %__TargetArchArm64%==1 set __TargetArch=arm64
if "%__HostArch%" == "" set __HostArch=%__TargetArch%

set /A __TotalSpecifiedBuildType=__BuildTypeDebug + __BuildTypeChecked + __BuildTypeRelease
if %__TotalSpecifiedBuildType% GTR 1 (
    echo Error: more than one build type specified, but "all" not specified.
    goto Usage
)

if %__BuildTypeDebug%==1    set __BuildType=Debug
if %__BuildTypeChecked%==1  set __BuildType=Checked
if %__BuildTypeRelease%==1  set __BuildType=Release

if %__EnforcePgo%==1 (
    if %__TargetArchArm%==1 (
        echo NOTICE: enforcepgo does nothing on arm architecture
        set __EnforcePgo=0
    )
    if %__TargetArchArm64%==1 (
        echo NOTICE: enforcepgo does nothing on arm64 architecture
        set __EnforcePgo=0
    )
)

REM Set the remaining variables based upon the determined build configuration

REM PGO optimization is only applied to release builds (see pgosupport.cmake). Disable PGO by default if not building release.
if NOT "%__BuildType%"=="Release" (
    set __PgoOptimize=0
)

set "__BinDir=%__RootBinDir%\bin\coreclr\%__TargetOS%.%__TargetArch%.%__BuildType%"
set "__IntermediatesDir=%__RootBinDir%\obj\coreclr\%__TargetOS%.%__TargetArch%.%__BuildType%"
set "__LogsDir=%__RootBinDir%\log\!__BuildType!"
set "__MsbuildDebugLogsDir=%__LogsDir%\MsbuildDebugLogs"
set "__ArtifactsIntermediatesDir=%__RepoRootDir%\artifacts\obj\coreclr\"
if "%__Ninja%"=="0" (set "__IntermediatesDir=%__IntermediatesDir%\ide")
set "__PackagesBinDir=%__BinDir%\.nuget"


if NOT "%__HostArch%" == "%__TargetArch%" set __BinDir=%__BinDir%\%__HostArch%
if NOT "%__HostArch%" == "%__TargetArch%" set __IntermediatesDir=%__IntermediatesDir%\%__HostArch%

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

if %__BuildNative%==0 goto SkipLocateCMake

REM Eval the output from set-cmake-path.ps1
for /f "delims=" %%a in ('powershell -NoProfile -ExecutionPolicy ByPass "& ""%__RepoRootDir%\eng\native\set-cmake-path.ps1"""') do %%a
echo %__MsgPrefix%Using CMake from !CMakePath!

:SkipLocateCMake

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

call "%__RepoRootDir%\eng\native\version\copy_version_files.cmd"

REM =========================================================================================
REM ===
REM === Locate Python
REM ===
REM =========================================================================================

set __IntermediatesIncDir=%__IntermediatesDir%\src\inc
set __IntermediatesEventingDir=%__ArtifactsIntermediatesDir%\Eventing\%__TargetArch%\%__BuildType%

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
    if not "!string:-hosts-=!"=="!string!" (
        set __CMakeTarget=!__CMakeTarget! hosts
    )
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
    if not "!string:-nativeaot-=!"=="!string!" (
        set __CMakeTarget=!__CMakeTarget! nativeaot
    )
    if not "!string:-spmi-=!"=="!string!" (
        set __CMakeTarget=!__CMakeTarget! spmi
    )
    if not "!string:-crosscomponents-=!"=="!string!" (
        set __CMakeTarget=!__CMakeTarget! crosscomponents
    )
)
if "!__CMakeTarget!" == "" (
    set __CMakeTarget=install
)

REM =========================================================================================
REM ===
REM === Build Native assets including CLR runtime
REM ===
REM =========================================================================================

if %__BuildNative% EQU 1 (
    REM Scope environment changes start {
    setlocal

    echo %__MsgPrefix%Commencing build of native components for %__TargetOS%.%__TargetArch%.%__BuildType%

    REM Set the environment for the native build
    set __VCTargetArch=amd64
    if /i "%__HostArch%" == "x86" ( set __VCTargetArch=x86 )
    if /i "%__HostArch%" == "arm" (
        set __VCTargetArch=x86_arm
    )
    if /i "%__HostArch%" == "arm64" (
        set __VCTargetArch=x86_arm64
    )

    echo %__MsgPrefix%Using environment: "%__VCToolsRoot%\vcvarsall.bat" !__VCTargetArch!
    call                                 "%__VCToolsRoot%\vcvarsall.bat" !__VCTargetArch!
    @if defined _echo @echo on

    if defined __SkipConfigure goto SkipConfigure

    echo %__MsgPrefix%Regenerating the Visual Studio solution

    if %__Ninja% EQU 1 (
        set __ExtraCmakeArgs="-DCMAKE_BUILD_TYPE=!__BuildType!"
    )

    set __ExtraCmakeArgs=!__ExtraCmakeArgs! "-DCLR_CMAKE_TARGET_ARCH=%__TargetArch%" "-DCLR_CMAKE_TARGET_OS=%__TargetOS%" "-DCLR_CMAKE_PGO_INSTRUMENT=%__PgoInstrument%" "-DCLR_CMAKE_OPTDATA_PATH=%__PgoOptDataPath%" "-DCLR_CMAKE_PGO_OPTIMIZE=%__PgoOptimize%" %__CMakeArgs%
    echo Calling "%__RepoRootDir%\eng\native\gen-buildsys.cmd" "%__ProjectDir%" "%__IntermediatesDir%" %__VSVersion% %__HostArch% !__ExtraCmakeArgs!
    call "%__RepoRootDir%\eng\native\gen-buildsys.cmd" "%__ProjectDir%" "%__IntermediatesDir%" %__VSVersion% %__HostArch% !__ExtraCmakeArgs!
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
    set "__BuildLog="%__LogsDir%\!__BuildLogRootName!_%__TargetOS%__%__TargetArch%__%__BuildType%__%__HostArch%.log""
    set "__BuildWrn="%__LogsDir%\!__BuildLogRootName!_%__TargetOS%__%__TargetArch%__%__BuildType%__%__HostArch%.wrn""
    set "__BuildErr="%__LogsDir%\!__BuildLogRootName!_%__TargetOS%__%__TargetArch%__%__BuildType%__%__HostArch%.err""
    set "__BinLog="%__LogsDir%\!__BuildLogRootName!_%__TargetOS%__%__TargetArch%__%__BuildType%__%__HostArch%.binlog""
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

set __TargetArchList=

set /A __TotalSpecifiedTargetArch=__TargetArchX64 + __TargetArchX86 + __TargetArchArm + __TargetArchArm64
if %__TotalSpecifiedTargetArch% EQU 0 (
    REM Nothing specified means we want to build all architectures.
    set __TargetArchList=x64 x86 arm arm64
)

REM Otherwise, add all the specified architectures to the list.

if %__TargetArchX64%==1      set __TargetArchList=%__TargetArchList% x64
if %__TargetArchX86%==1      set __TargetArchList=%__TargetArchList% x86
if %__TargetArchArm%==1      set __TargetArchList=%__TargetArchList% arm
if %__TargetArchArm64%==1    set __TargetArchList=%__TargetArchList% arm64

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

for %%i in (%__TargetArchList%) do (
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
set __TargetArch=%1
set __BuildType=%2
set __NextCmd=call %__ThisScriptFull% %__TargetArch% %__BuildType% %__PassThroughArgs%
echo %__MsgPrefix%Invoking: %__NextCmd%
%__NextCmd%
if not !errorlevel! == 0 (
    echo %__MsgPrefix%    %__TargetArch% %__BuildType% %__PassThroughArgs% >> %__BuildResultFile%
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
echo                     Allowed ^<name^>: hosts jit alljits runtime paltests iltools nativeaot spmi
echo -enforcepgo: verify after the build that PGO was used for key DLLs, and fail the build if not
echo -pgoinstrument: generate instrumented code for profile guided optimization enabled binaries.
echo -cmakeargs: user-settable additional arguments passed to CMake.
echo -configureonly: skip all builds; only run CMake ^(default: CMake and builds are run^)
echo -skipconfigure: skip CMake ^(default: CMake is run^)
echo -skipnative: skip building native components ^(default: native components are built^).
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
