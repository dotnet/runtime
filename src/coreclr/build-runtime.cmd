@if not defined _echo @echo off
setlocal EnableDelayedExpansion EnableExtensions

:: Define a prefix for most output progress messages that come from this script. That makes
:: it easier to see where these are coming from. Note that there is a trailing space here.
set "__MsgPrefix=BUILD: "

echo %__MsgPrefix%Starting Build at %TIME%

set __ThisScriptFull="%~f0"
set __ThisScriptDir="%~dp0"

call "%__ThisScriptDir%"\setup_vs_tools.cmd
if NOT '%ERRORLEVEL%' == '0' goto ExitWithError

if defined VS160COMNTOOLS (
    set "__VSToolsRoot=%VS160COMNTOOLS%"
    set "__VCToolsRoot=%VS160COMNTOOLS%\..\..\VC\Auxiliary\Build"
    set __VSVersion=vs2019
) else if defined VS150COMNTOOLS (
    set "__VSToolsRoot=%VS150COMNTOOLS%"
    set "__VCToolsRoot=%VS150COMNTOOLS%\..\..\VC\Auxiliary\Build"
    set __VSVersion=vs2017
)

:: Note that the msbuild project files (specifically, dir.proj) will use the following variables, if set:
::      __BuildArch         -- default: x64
::      __BuildType         -- default: Debug
::      __TargetOS           -- default: Windows_NT
::      __ProjectDir        -- default: directory of the dir.props file
::      __RepoRootDir       -- default: directory two levels above the dir.props file
::      __SourceDir         -- default: %__ProjectDir%\src\
::      __RootBinDir        -- default: %__RepoRootDir%\artifacts\
::      __BinDir            -- default: %__RootBinDir%\%__TargetOS%.%__BuildArch.%__BuildType%\
::      __IntermediatesDir
::      __PackagesBinDir    -- default: %__BinDir%\.nuget
::
:: Thus, these variables are not simply internal to this script!

:: Set the default arguments for build
set __BuildArch=x64
set __BuildType=Debug
set __TargetOS=Windows_NT

:: Set the various build properties here so that CMake and MSBuild can pick them up
set "__ProjectDir=%~dp0"
:: remove trailing slash
if %__ProjectDir:~-1%==\ set "__ProjectDir=%__ProjectDir:~0,-1%"
set "__RepoRootDir=%__ProjectDir%\..\.."

set "__ProjectFilesDir=%__ProjectDir%"
set "__SourceDir=%__ProjectDir%\src"
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

REM __PassThroughArgs is a set of things that will be passed through to nested calls to build.cmd
REM when using "all".
set __PassThroughArgs=

REM __UnprocessedBuildArgs are args that we pass to msbuild (e.g. /p:TargetArchitecture=x64)
set "__args= %*"
set processedArgs=
set __UnprocessedBuildArgs=
set __CommonMSBuildArgs=

set __BuildNative=1
set __BuildCrossArchNative=0
set __SkipCrossArchNative=0
set __SkipGenerateVersion=0
set __RestoreOptData=1
set __CrossArch=
set __PgoOptDataPath=
set __CMakeArgs=

@REM CMD has a nasty habit of eating "=" on the argument list, so passing:
@REM    -priority=1
@REM appears to CMD parsing as "-priority 1". Handle -priority specially to avoid problems,
@REM and allow the "-priority=1" syntax.
set __Priority=

:Arg_Loop
if "%1" == "" goto ArgsDone

if /i "%1" == "/?"     goto Usage
if /i "%1" == "-?"     goto Usage
if /i "%1" == "/h"     goto Usage
if /i "%1" == "-h"     goto Usage
if /i "%1" == "/help"  goto Usage
if /i "%1" == "-help"  goto Usage
if /i "%1" == "--help" goto Usage

if /i "%1" == "-all"                 (set __BuildAll=1&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "-x64"                 (set __BuildArchX64=1&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "-x86"                 (set __BuildArchX86=1&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "-arm"                 (set __BuildArchArm=1&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "-arm64"               (set __BuildArchArm64=1&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)

if /i "%1" == "-debug"               (set __BuildTypeDebug=1&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "-checked"             (set __BuildTypeChecked=1&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "-release"             (set __BuildTypeRelease=1&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)

if /i "%1" == "-ci"                  (set __ArcadeScriptArgs="-ci"&set __ErrMsgPrefix=##vso[task.logissue type=error]&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)

REM TODO these are deprecated remove them eventually
REM don't add more, use the - syntax instead
if /i "%1" == "all"                 (set __BuildAll=1&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "x64"                 (set __BuildArchX64=1&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "x86"                 (set __BuildArchX86=1&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "arm"                 (set __BuildArchArm=1&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "arm64"               (set __BuildArchArm64=1&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)

if /i "%1" == "debug"               (set __BuildTypeDebug=1&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "checked"             (set __BuildTypeChecked=1&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "release"             (set __BuildTypeRelease=1&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)

if /i "%1" == "-priority"           (set __Priority=%2&shift&set processedArgs=!processedArgs! %1=%2&shift&goto Arg_Loop)

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
    set __PassThroughArgs=%1
) else (
    set __PassThroughArgs=%__PassThroughArgs% %1
)

if /i "%1" == "-alpinedac"           (set __BuildNative=0&set __BuildCrossArchNative=1&set __CrossArch=x64&set __TargetOS=alpine&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "-linuxdac"            (set __BuildNative=0&set __BuildCrossArchNative=1&set __CrossArch=x64&set __TargetOS=Linux&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)

if /i "%1" == "-cmakeargs"           (set __CMakeArgs=%2 %__CMakeArgs%&set processedArgs=!processedArgs! %1 %2&shift&shift&goto Arg_Loop)
if /i "%1" == "-configureonly"       (set __ConfigureOnly=1&set __BuildNative=1&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "-skipconfigure"       (set __SkipConfigure=1&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "-skipnative"          (set __BuildNative=0&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "-skipcrossarchnative" (set __SkipCrossArchNative=1&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "-skipgenerateversion" (set __SkipGenerateVersion=1&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "-skiprestoreoptdata"  (set __RestoreOptData=0&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "-usenmakemakefiles"   (set __NMakeMakefiles=1&set __ConfigureOnly=1&set __BuildNative=1&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "-pgoinstrument"       (set __PgoInstrument=1&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "-enforcepgo"          (set __EnforcePgo=1&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "-nopgooptimize"       (set __PgoOptimize=0&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)

REM TODO these are deprecated remove them eventually
REM don't add more, use the - syntax instead
if /i "%1" == "configureonly"       (set __ConfigureOnly=1&set __BuildNative=1&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "skipconfigure"       (set __SkipConfigure=1&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "skipnative"          (set __BuildNative=0&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "skipcrossarchnative" (set __SkipCrossArchNative=1&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "skipgenerateversion" (set __SkipGenerateVersion=1&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "skiprestoreoptdata"  (set __RestoreOptData=0&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "usenmakemakefiles"   (set __NMakeMakefiles=1&set __ConfigureOnly=1&set __BuildNative=1&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "pgoinstrument"       (set __PgoInstrument=1&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "nopgooptimize"       (set __PgoOptimize=0&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "enforcepgo"          (set __EnforcePgo=1&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
REM TODO remove this once it's no longer used in buildpipeline
if /i "%1" == "--"                  (set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)

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
if defined __Priority (
    if defined __PassThroughArgs (
        set __PassThroughArgs=%__PassThroughArgs% -priority=%__Priority%
    ) else (
        set __PassThroughArgs=-priority=%__Priority%
    )
)

if defined __BuildAll goto BuildAll

set /A __TotalSpecifiedBuildArch=__BuildArchX64 + __BuildArchX86 + __BuildArchArm + __BuildArchArm64
if %__TotalSpecifiedBuildArch% GTR 1 (
    echo Error: more than one build architecture specified, but "all" not specified.
    goto Usage
)

if %__BuildArchX64%==1      set __BuildArch=x64
if %__BuildArchX86%==1      set __BuildArch=x86
if %__BuildArchArm%==1 (
    set __BuildArch=arm
    set __CrossArch=x86
)
if %__BuildArchArm64%==1 (
    set __BuildArch=arm64
    set __CrossArch=x64
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
            set __BuildCrossArchNative=1
        )
        if /i "%__BuildArch%"=="arm" (
            set __BuildCrossArchNative=1
        )
    )
)

REM Set the remaining variables based upon the determined build configuration

if %__PgoOptimize%==0 (
    set __RestoreOptData=0
)

set "__BinDir=%__RootBinDir%\bin\coreclr\%__TargetOS%.%__BuildArch%.%__BuildType%"
set "__IntermediatesDir=%__RootBinDir%\obj\coreclr\%__TargetOS%.%__BuildArch%.%__BuildType%"
set "__LogsDir=%__RootBinDir%\log\!__BuildType!"
set "__MsbuildDebugLogsDir=%__LogsDir%\MsbuildDebugLogs"
set "__ArtifactsIntermediatesDir=%__RepoRootDir%\artifacts\obj\coreclr\"
if "%__NMakeMakefiles%"=="1" (set "__IntermediatesDir=%__RootBinDir%\nmakeobj\%__TargetOS%.%__BuildArch%.%__BuildType%")
set "__PackagesBinDir=%__BinDir%\.nuget"
set "__CrossComponentBinDir=%__BinDir%"
set "__CrossCompIntermediatesDir=%__IntermediatesDir%\crossgen"


if NOT "%__CrossArch%" == "" set __CrossComponentBinDir=%__CrossComponentBinDir%\%__CrossArch%

REM Generate path to be set for CMAKE_INSTALL_PREFIX to contain forward slash
set "__CMakeBinDir=%__BinDir%"
set "__CMakeBinDir=%__CMakeBinDir:\=/%"

if not exist "%__BinDir%"              md "%__BinDir%"
if not exist "%__IntermediatesDir%"    md "%__IntermediatesDir%"
if not exist "%__LogsDir%"             md "%__LogsDir%"
if not exist "%__MsbuildDebugLogsDir%" md "%__MsbuildDebugLogsDir%"

if not exist "%__RootBinDir%\Directory.Build.props" copy %__ProjectDir%\EmptyProps.props %__RootBinDir%\Directory.Build.props
if not exist "%__RootBinDir%\Directory.Build.targets" copy %__ProjectDir%\EmptyProps.props %__RootBinDir%\Directory.Build.targets

REM Set up the directory for MSBuild debug logs.
set MSBUILDDEBUGPATH=%__MsbuildDebugLogsDir%

echo %__MsgPrefix%Commencing CoreCLR product build

REM Set the remaining variables based upon the determined build configuration

echo %__MsgPrefix%Checking prerequisites

set __CMakeNeeded=1
if %__BuildNative%==0 if %__BuildCrossArchNative%==0 set __CMakeNeeded=0
if %__CMakeNeeded%==1 (
    REM Eval the output from set-cmake-path.ps1
    for /f "delims=" %%a in ('powershell -NoProfile -ExecutionPolicy ByPass "& ""%__SourceDir%\pal\tools\set-cmake-path.ps1"""') do %%a
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
    set "__BinLog=%__LogsDir%\GenerateVersionHeaders_%__TargetOS%__%__BuildArch%__%__BuildType%.binlog"
    powershell -NoProfile -ExecutionPolicy ByPass -NoLogo -File "%__RepoRootDir%\eng\common\msbuild.ps1" /clp:nosummary %__ArcadeScriptArgs%^
        %__RepoRootDir%\eng\empty.csproj /t:GenerateRuntimeVersionFile /restore^
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

set OptDataProjectFilePath=%__ProjectDir%\src\.nuget\optdata\optdata.csproj
if %__RestoreOptData% EQU 1 (
    echo %__MsgPrefix%Restoring the OptimizationData Package
    set "__BinLog=%__LogsDir%\OptRestore_%__TargetOS%__%__BuildArch%__%__BuildType%.binlog"
    powershell -NoProfile -ExecutionPolicy ByPass -NoLogo -File "%__RepoRootDir%\eng\common\msbuild.ps1" /clp:nosummary %__ArcadeScriptArgs%^
        %OptDataProjectFilePath% /t:Restore^
        %__CommonMSBuildArgs% %__UnprocessedBuildArgs%^
        /nodereuse:false /bl:!__BinLog!
    if not !errorlevel! == 0 (
        set __exitCode=!errorlevel!
        echo %__ErrMsgPrefix%%__MsgPrefix%Error: Failed to restore the optimization data package.
        goto ExitWithCode
    )
)
set __PgoOptDataPath=
if %__PgoOptimize% EQU 1 (
    set PgoDataPackagePathOutputFile="%__IntermediatesDir%\optdatapath.txt"
    set "__BinLog=%__LogsDir%\PgoVersionRead_%__TargetOS%__%__BuildArch%__%__BuildType%.binlog"

    REM Parse the optdata package versions out of msbuild so that we can pass them on to CMake
    powershell -NoProfile -ExecutionPolicy ByPass -NoLogo -File "%__RepoRootDir%\eng\common\msbuild.ps1" /clp:nosummary %__ArcadeScriptArgs%^
        "%OptDataProjectFilePath%" /t:DumpPgoDataPackagePath %__CommonMSBuildArgs% /p:PgoDataPackagePathOutputFile="!PgoDataPackagePathOutputFile!"^
        /bl:!__BinLog!

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
    echo %__ErrMsgPrefix%%__MsgPrefix%Error: Could not find a python installation
    goto ExitWithError
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
    set __ExtraCmakeArgs="-DCLR_CROSS_COMPONENTS_BUILD=1" "-DCLR_CMAKE_TARGET_ARCH=%__BuildArch%" "-DCLR_CMAKE_TARGET_OS=%__TargetOS%" "-DCLR_CMAKE_PGO_INSTRUMENT=%__PgoInstrument%" "-DCLR_CMAKE_OPTDATA_PATH=%__PgoOptDataPath%" "-DCLR_CMAKE_PGO_OPTIMIZE=%__PgoOptimize%" "-DCMAKE_SYSTEM_VERSION=10.0" "-DCLR_ENG_NATIVE_DIR=%__RepoRootDir%/eng/native" "-DCLR_REPO_ROOT_DIR=%__RepoRootDir%" %__CMakeArgs%
    call "%__SourceDir%\pal\tools\gen-buildsys.cmd" "%__ProjectDir%" "%__CrossCompIntermediatesDir%" %__VSVersion% %__CrossArch% !__ExtraCmakeArgs!

    if not !errorlevel! == 0 (
        echo %__ErrMsgPrefix%%__MsgPrefix%Error: failed to generate cross architecture native component build project!
        goto ExitWithError
    )
    @if defined _echo @echo on

:SkipConfigureCrossBuild
    if not exist "%__CrossCompIntermediatesDir%\CMakeCache.txt" (
        echo %__ErrMsgPrefix%%__MsgPrefix%Error: unable to find generated cross architecture native component build project!
        goto ExitWithError
    )

    if defined __ConfigureOnly goto SkipCrossCompBuild

    set __BuildLogRootName=Cross
    set "__BuildLog=%__LogsDir%\!__BuildLogRootName!_%__TargetOS%__%__BuildArch%__%__BuildType%.log"
    set "__BuildWrn=%__LogsDir%\!__BuildLogRootName!_%__TargetOS%__%__BuildArch%__%__BuildType%.wrn"
    set "__BuildErr=%__LogsDir%\!__BuildLogRootName!_%__TargetOS%__%__BuildArch%__%__BuildType%.err"
    set "__BinLog=%__LogsDir%\!__BuildLogRootName!_%__TargetOS%__%__BuildArch%__%__BuildType%.binlog"
    set "__MsbuildLog=/flp:Verbosity=normal;LogFile=!__BuildLog!"
    set "__MsbuildWrn=/flp1:WarningsOnly;LogFile=!__BuildWrn!"
    set "__MsbuildErr=/flp2:ErrorsOnly;LogFile=!__BuildErr!"
    set "__MsbuildBinLog=/bl:!__BinLog!"
    set "__Logging=!__MsbuildLog! !__MsbuildWrn! !__MsbuildErr! !__MsbuildBinLog!"

    REM We pass the /m flag directly to MSBuild so that we can get both MSBuild and CL parallelism, which is fastest for our builds.
    "%CMakePath%" --build %__CrossCompIntermediatesDir% --target install --config %__BuildType% -- /nologo /m !__Logging!

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
    set __VCBuildArch=x86_amd64
    if /i "%__BuildArch%" == "x86" ( set __VCBuildArch=x86 )
    if /i "%__BuildArch%" == "arm" (
        set __VCBuildArch=x86_arm
        set ___CrossBuildDefine="-DCLR_CMAKE_CROSS_ARCH=1" "-DCLR_CMAKE_CROSS_HOST_ARCH=%__CrossArch%"
    )
    if /i "%__BuildArch%" == "arm64" (
        set __VCBuildArch=x86_arm64
        set ___CrossBuildDefine="-DCLR_CMAKE_CROSS_ARCH=1" "-DCLR_CMAKE_CROSS_HOST_ARCH=%__CrossArch%"
    )

    echo %__MsgPrefix%Using environment: "%__VCToolsRoot%\vcvarsall.bat" !__VCBuildArch!
    call                                 "%__VCToolsRoot%\vcvarsall.bat" !__VCBuildArch!
    @if defined _echo @echo on

    if not defined VSINSTALLDIR (
        echo %__ErrMsgPrefix%%__MsgPrefix%Error: VSINSTALLDIR variable not defined.
        goto ExitWithError
    )
    if not exist "!VSINSTALLDIR!DIA SDK" goto NoDIA

    if defined __SkipConfigure goto SkipConfigure

    echo %__MsgPrefix%Regenerating the Visual Studio solution

    set __ExtraCmakeArgs="-DCMAKE_SYSTEM_VERSION=10.0" !___CrossBuildDefine! "-DCLR_CMAKE_PGO_INSTRUMENT=%__PgoInstrument%" "-DCLR_CMAKE_OPTDATA_PATH=%__PgoOptDataPath%" "-DCLR_CMAKE_PGO_OPTIMIZE=%__PgoOptimize%" "-DCLR_ENG_NATIVE_DIR=%__RepoRootDir%/eng/native" "-DCLR_REPO_ROOT_DIR=%__RepoRootDir%" %__CMakeArgs%
    call "%__SourceDir%\pal\tools\gen-buildsys.cmd" "%__ProjectDir%" "%__IntermediatesDir%" %__VSVersion% %__BuildArch% !__ExtraCmakeArgs!
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
    set "__BuildLog=%__LogsDir%\!__BuildLogRootName!_%__TargetOS%__%__BuildArch%__%__BuildType%.log"
    set "__BuildWrn=%__LogsDir%\!__BuildLogRootName!_%__TargetOS%__%__BuildArch%__%__BuildType%.wrn"
    set "__BuildErr=%__LogsDir%\!__BuildLogRootName!_%__TargetOS%__%__BuildArch%__%__BuildType%.err"
    set "__BinLog=%__LogsDir%\!__BuildLogRootName!_%__TargetOS%__%__BuildArch%__%__BuildType%.binlog"
    set "__MsbuildLog=/flp:Verbosity=normal;LogFile=!__BuildLog!"
    set "__MsbuildWrn=/flp1:WarningsOnly;LogFile=!__BuildWrn!"
    set "__MsbuildErr=/flp2:ErrorsOnly;LogFile=!__BuildErr!"
    set "__MsbuildBinLog=/bl:!__BinLog!"
    set "__Logging=!__MsbuildLog! !__MsbuildWrn! !__MsbuildErr! !__MsbuildBinLog!"

    REM We pass the /m flag directly to MSBuild so that we can get both MSBuild and CL parallelism, which is fastest for our builds.
    "%CMakePath%" --build %__IntermediatesDir% --target install --config %__BuildType% -- /nologo /m !__Logging!

    if not !errorlevel! == 0 (
        set __exitCode=!errorlevel!
        echo %__ErrMsgPrefix%%__MsgPrefix%Error: native component build failed. Refer to the build log files for details.
        echo     !__BuildLog!
        echo     !__BuildWrn!
        echo     !__BuildErr!
        goto ExitWithCode
    )

    if /i "%__BuildArch%" == "arm64" goto SkipCopyUcrt

    set "__UCRTDir=%UniversalCRTSDKDIR%Redist\ucrt\DLLs\%__BuildArch%\"
    if not exist "!__UCRTDir!" set "__UCRTDir=%UniversalCRTSDKDIR%Redist\%UCRTVersion%\ucrt\DLLs\%__BuildArch%\"
    if not exist "!__UCRTDir!" (
        echo %__ErrMsgPrefix%%__MsgPrefix%Error: Please install the Redistributable Universal C Runtime.
        goto ExitWithError
    )

    xcopy /Y/I/E/D/F "!__UCRTDir!*.dll" "%__BinDir%\Redist\ucrt\DLLs\%__BuildArch%"
    if not !errorlevel! == 0 (
        set __exitCode=!errorlevel!
        echo %__ErrMsgPrefix%%__MsgPrefix%Error: Failed to copy the CRT to the output.
        goto ExitWithCode
    )

:SkipCopyUcrt
    if %__EnforcePgo% EQU 1 (
        "%PYTHON%" "%__ProjectDir%\src\scripts\pgocheck.py" "%__BinDir%\coreclr.dll" "%__BinDir%\clrjit.dll"
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
echo -priority=^<N^> : specify a set of test that will be built and run, with priority N.
echo portable : build for portable RID.
echo.
echo If "all" is specified, then all build architectures and types are built. If, in addition,
echo one or more build architectures or types is specified, then only those build architectures
echo and types are built.
echo.
echo For example:
echo     build -all
echo        -- builds all architectures, and all build types per architecture
echo     build -all -x86
echo        -- builds all build types for x86
echo     build -all -x64 -x86 -Checked -Release
echo        -- builds x64 and x86 architectures, Checked and Release build types for each
exit /b 1

:NoDIA
echo Error: DIA SDK is missing at "%VSINSTALLDIR%DIA SDK". ^
Did you install all the requirements for building on Windows, including the "Desktop Development with C++" workload? ^
Please see https://github.com/dotnet/runtime/blob/master/docs/workflow/requirements/windows-requirements.md ^
Another possibility is that you have a parallel installation of Visual Studio and the DIA SDK is there. In this case it ^
may help to copy its "DIA SDK" folder into "%VSINSTALLDIR%" manually, then try again.
exit /b 1
