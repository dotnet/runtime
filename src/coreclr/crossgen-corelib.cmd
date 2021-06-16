@if not defined _echo @echo off
setlocal EnableDelayedExpansion EnableExtensions

:: Define a prefix for most output progress messages that come from this script. That makes
:: it easier to see where these are coming from. Note that there is a trailing space here.
set "__MsgPrefix=CROSSGEN-CORELIB: "

echo %__MsgPrefix%Starting Build at %TIME%

:: Note that the msbuild project files (specifically, dir.proj) will use the following variables, if set:
::      __BuildArch         -- default: x64
::      __BuildType         -- default: Debug
::      __TargetOS           -- default: windows
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

set "__ProjectDir=%~dp0"
:: remove trailing slash
if %__ProjectDir:~-1%==\ set "__ProjectDir=%__ProjectDir:~0,-1%"
set "__RepoRootDir=%__ProjectDir%\..\.."

set "__RootBinDir=%__RepoRootDir%\artifacts"
set "__LogsDir=%__RootBinDir%\log"

set __BuildArchX64=0
set __BuildArchX86=0
set __BuildArchArm=0
set __BuildArchArm64=0

set __BuildTypeDebug=0
set __BuildTypeChecked=0
set __BuildTypeRelease=0

set __PgoInstrument=0
set __IbcTuning=

REM __PassThroughArgs is a set of things that will be passed through to nested calls to build.cmd
REM when using "all".
set __PassThroughArgs=

set "__args= %*"
set processedArgs=
set __CrossgenAltJit=
set __CrossArch=

:Arg_Loop
if "%1" == "" goto ArgsDone

if /i "%1" == "/?"     goto Usage
if /i "%1" == "-?"     goto Usage
if /i "%1" == "/h"     goto Usage
if /i "%1" == "-h"     goto Usage
if /i "%1" == "/help"  goto Usage
if /i "%1" == "-help"  goto Usage
if /i "%1" == "--help" goto Usage

if /i "%1" == "-x64"                 (set __BuildArchX64=1&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "-x86"                 (set __BuildArchX86=1&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "-arm"                 (set __BuildArchArm=1&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "-arm64"               (set __BuildArchArm64=1&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)

if /i "%1" == "-debug"               (set __BuildTypeDebug=1&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "-checked"             (set __BuildTypeChecked=1&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "-release"             (set __BuildTypeRelease=1&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)

if /i "%1" == "-ci"                  (set __ErrMsgPrefix=##vso[task.logissue type=error]&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)

REM All arguments after this point will be passed through directly to build.cmd on nested invocations
REM using the "all" argument, and must be added to the __PassThroughArgs variable.
if [!__PassThroughArgs!]==[] (
    set __PassThroughArgs=%1
) else (
    set __PassThroughArgs=%__PassThroughArgs% %1
)
if /i "%1" == "-ibcinstrument"       (set __IbcTuning=/Tuning&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "-pgoinstrument"       (set __PgoInstrument=1&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "-crossgenaltjit"      (set __CrossgenAltJit=%2&set processedArgs=!processedArgs! %1 %2&shift&shift&goto Arg_Loop)
if /i "%1" == "--"                  (set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)

:ArgsDone

set /A __TotalSpecifiedBuildArch=__BuildArchX64 + __BuildArchX86 + __BuildArchArm + __BuildArchArm64
if %__TotalSpecifiedBuildArch% GTR 1 (
    echo Error: more than one build architecture specified.
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
    echo Error: more than one build type specified.
    goto Usage
)

if %__BuildTypeDebug%==1    set __BuildType=Debug
if %__BuildTypeChecked%==1  set __BuildType=Checked
if %__BuildTypeRelease%==1  set __BuildType=Release

set "__BinDir=%__RootBinDir%\bin\coreclr\%__TargetOS%.%__BuildArch%.%__BuildType%"
set "__IntermediatesDir=%__RootBinDir%\obj\coreclr\%__TargetOS%.%__BuildArch%.%__BuildType%"
set "__CrossComponentBinDir=%__BinDir%"


if NOT "%__CrossArch%" == "" set __CrossComponentBinDir=%__CrossComponentBinDir%\%__CrossArch%
set "__CrossGenCoreLibLog=%__LogsDir%\CrossgenCoreLib_%__TargetOS%__%__BuildArch%__%__BuildType%.log"
set "__CrossgenExe=%__CrossComponentBinDir%\crossgen.exe"

if not exist "%__BinDir%"              md "%__BinDir%"
if not exist "%__IntermediatesDir%"    md "%__IntermediatesDir%"
if not exist "%__LogsDir%"             md "%__LogsDir%"

REM Need VC native tools environment for the host arch to find Microsoft.DiaSymReader.Native in the Visual Studio install.
call %__RepoRootDir%\eng\native\init-vs-env.cmd %__BuildArch%
if NOT '%ERRORLEVEL%' == '0' goto ExitWithError

@if defined _echo @echo on

if defined VCINSTALLDIR (
    set "__VCToolsRoot=%VCINSTALLDIR%Auxiliary\Build"
)

echo %__MsgPrefix%Generating native image of System.Private.CoreLib for %__TargetOS%.%__BuildArch%.%__BuildType%. Logging to "%__CrossGenCoreLibLog%".
if exist "%__CrossGenCoreLibLog%" del "%__CrossGenCoreLibLog%"

REM Need VS native tools environment for the **target** arch when running instrumented binaries
if %__PgoInstrument% EQU 1 (
    set __VCExecArch=%__BuildArch%
    if /i [%__BuildArch%] == [x64] set __VCExecArch=amd64
    echo %__MsgPrefix%Using environment: "%__VCToolsRoot%\vcvarsall.bat" !__VCExecArch!
    call                                 "%__VCToolsRoot%\vcvarsall.bat" !__VCExecArch!
    @if defined _echo @echo on
    if NOT !errorlevel! == 0 (
        echo %__ErrMsgPrefix%%__MsgPrefix%Error: Failed to load native tools environment for !__VCExecArch!
        goto ExitWithError
    )

    REM HACK: Workaround for [dotnet/runtime#8929](https://github.com/dotnet/runtime/issues/8929)
    set __PgoRtPath=
    for /f "tokens=*" %%f in ('where pgort*.dll') do (
        if not defined __PgoRtPath set "__PgoRtPath=%%~f"
    )
    echo %__MsgPrefix%Copying "!__PgoRtPath!" into "%__BinDir%"
    copy /y "!__PgoRtPath!" "%__BinDir%" || (
        echo %__ErrMsgPrefix%%__MsgPrefix%Error: copy failed
        goto ExitWithError
    )
    REM End HACK
)

if defined __CrossgenAltJit (
    REM Set altjit flags for the crossgen run.
    echo %__MsgPrefix%Setting altjit environment variables for %__CrossgenAltJit%.
    echo %__MsgPrefix%Setting altjit environment variables for %__CrossgenAltJit%. >> "%__CrossGenCoreLibLog%"
    set COMPlus_AltJit=*
    set COMPlus_AltJitNgen=*
    set COMPlus_AltJitName=%__CrossgenAltJit%
    set COMPlus_AltJitAssertOnNYI=1
    set COMPlus_NoGuiOnAssert=1
    set COMPlus_ContinueOnAssert=0
)

REM Need diasymreader.dll on your path for /CreatePdb
set PATH=%PATH%;%WinDir%\Microsoft.Net\Framework64\V4.0.30319;%WinDir%\Microsoft.Net\Framework\V4.0.30319

    for /f "tokens=*" %%f in ('where Microsoft.DiaSymReader.Native.amd64.dll') do (
        echo "%%~f"
    )

set NEXTCMD="%__CrossgenExe%" /nologo %__IbcTuning% /Platform_Assemblies_Paths "%__BinDir%\IL" /out "%__BinDir%\System.Private.CoreLib.dll" "%__BinDir%\IL\System.Private.CoreLib.dll"
echo %__MsgPrefix%!NEXTCMD!
echo %__MsgPrefix%!NEXTCMD! >> "%__CrossGenCoreLibLog%"
!NEXTCMD! >> "%__CrossGenCoreLibLog%" 2>&1
if NOT !errorlevel! == 0 (
    echo %__ErrMsgPrefix%%__MsgPrefix%Error: CrossGen System.Private.CoreLib build failed. Refer to %__CrossGenCoreLibLog%
    REM Put it in the same log, helpful for CI
    type %__CrossGenCoreLibLog%
    goto ExitWithError
)

set NEXTCMD="%__CrossgenExe%" /nologo /Platform_Assemblies_Paths "%__BinDir%" /CreatePdb "%__BinDir%\PDB" "%__BinDir%\System.Private.CoreLib.dll"
echo %__MsgPrefix%!NEXTCMD!
echo %__MsgPrefix%!NEXTCMD! >> "%__CrossGenCoreLibLog%"
!NEXTCMD! >> "%__CrossGenCoreLibLog%" 2>&1
if NOT !errorlevel! == 0 (
    echo %__ErrMsgPrefix%%__MsgPrefix%Error: CrossGen /CreatePdb System.Private.CoreLib build failed. Refer to %__CrossGenCoreLibLog%
    REM Put it in the same log, helpful for CI
    type %__CrossGenCoreLibLog%
    goto ExitWithError
)

REM =========================================================================================
REM ===
REM === All builds complete!
REM ===
REM =========================================================================================

echo %__MsgPrefix%Crossgenning of System.Private.CoreLib succeeded.  Finished at %TIME%
echo %__MsgPrefix%Product binaries are available at !__BinDir!
exit /b 0


REM =========================================================================================
REM === These two routines are intended for the exit code to propagate to the parent process
REM === Like MSBuild or Powershell. If we directly exit /b 1 from within a if statement in
REM === any of the routines, the exit code is not propagated.
REM =========================================================================================
:ExitWithError
exit /b 1

:ExitWithCode
exit /b !__exitCode!
