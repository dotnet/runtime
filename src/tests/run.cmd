@if not defined _echo @echo off
setlocal EnableDelayedExpansion EnableExtensions

:: Define a prefix for most output progress messages that come from this script. That makes
:: it easier to see where these are coming from. Note that there is a trailing space here.
set "__MsgPrefix=RUNTEST: "

:: Set the default arguments
set __BuildArch=x64
set __BuildType=Debug
set __TargetOS=windows

set "__ProjectDir=%~dp0"
set "__RepoRootDir=%~dp0..\.."
:: normalize
for %%i in ("%__RepoRootDir%") do set "__RepoRootDir=%%~fi"
:: remove trailing slash
if %__ProjectDir:~-1%==\ set "__ProjectDir=%__ProjectDir:~0,-1%"
set "__ProjectFilesDir=%__ProjectDir%"
set "__RootBinDir=%__RepoRootDir%\artifacts"
set "__LogsDir=%__RootBinDir%\log"
set "__MsbuildDebugLogsDir=%__LogsDir%\MsbuildDebugLogs"
set __ToolsDir=%__ProjectDir%\..\Tools
set "DotNetCli=%__RepoRootDir%\dotnet.cmd"

set __Sequential=
set __ParallelType=
set __msbuildExtraArgs=
set __LongGCTests=
set __GCSimulatorTests=
set __IlasmRoundTrip=
set __PrintLastResultsOnly=
set RunInUnloadableContext=
set TieringTest=

:Arg_Loop
if "%1" == "" goto ArgsDone

if /i "%1" == "/?"    goto Usage
if /i "%1" == "-?"    goto Usage
if /i "%1" == "/h"    goto Usage
if /i "%1" == "-h"    goto Usage
if /i "%1" == "/help" goto Usage
if /i "%1" == "-help" goto Usage

if /i "%1" == "x64"                                     (set __BuildArch=x64&shift&goto Arg_Loop)
if /i "%1" == "x86"                                     (set __BuildArch=x86&shift&goto Arg_Loop)
if /i "%1" == "arm"                                     (set __BuildArch=arm&shift&goto Arg_Loop)
if /i "%1" == "arm64"                                   (set __BuildArch=arm64&shift&goto Arg_Loop)

if /i "%1" == "debug"                                   (set __BuildType=Debug&shift&goto Arg_Loop)
if /i "%1" == "release"                                 (set __BuildType=Release&shift&goto Arg_Loop)
if /i "%1" == "checked"                                 (set __BuildType=Checked&shift&goto Arg_Loop)

if /i "%1" == "TestEnv"                                 (set __TestEnv=%2&shift&shift&goto Arg_Loop)
if /i "%1" == "sequential"                              (set __Sequential=1&shift&goto Arg_Loop)
if /i "%1" == "parallel"                                (set __ParallelType=%2&shift&shift&goto Arg_Loop)
if /i "%1" == "jitstress"                               (set DOTNET_JitStress=%2&shift&shift&goto Arg_Loop)
if /i "%1" == "jitstressregs"                           (set DOTNET_JitStressRegs=%2&shift&shift&goto Arg_Loop)
if /i "%1" == "jitminopts"                              (set DOTNET_JITMinOpts=1&shift&goto Arg_Loop)
if /i "%1" == "jitforcerelocs"                          (set DOTNET_ForceRelocs=1&shift&goto Arg_Loop)

if /i "%1" == "printlastresultsonly"                    (set __PrintLastResultsOnly=1&shift&goto Arg_Loop)
if /i "%1" == "runcrossgen2tests"                       (set RunCrossGen2=1&shift&goto Arg_Loop)
REM This test feature is currently intentionally undocumented
if /i "%1" == "runlargeversionbubblecrossgen2tests"     (set RunCrossGen2=1&set CrossgenLargeVersionBubble=1&shift&goto Arg_Loop)
if /i "%1" == "synthesizepgo"                           (set CrossGen2SynthesizePgo=1&shift&goto Arg_Loop)
if /i "%1" == "link"                                    (set DoLink=true&set ILLINK=%2&shift&shift&goto Arg_Loop)
if /i "%1" == "gcname"                                  (set DOTNET_GCName=%2&shift&shift&goto Arg_Loop)
if /i "%1" == "gcstresslevel"                           (set DOTNET_GCStress=%2&set __TestTimeout=1800000&shift&shift&goto Arg_Loop)
if /i "%1" == "gcsimulator"                             (set __GCSimulatorTests=1&shift&goto Arg_Loop)
if /i "%1" == "longgc"                                  (set __LongGCTests=1&shift&goto Arg_Loop)
if /i "%1" == "ilasmroundtrip"                          (set __IlasmRoundTrip=1&shift&goto Arg_Loop)
if /i "%1" == "timeout"                                 (set __TestTimeout=%2&shift&shift&goto Arg_Loop)
if /i "%1" == "runincontext"                            (set RunInUnloadableContext=1&shift&goto Arg_Loop)
if /i "%1" == "tieringtest"                             (set TieringTest=1&shift&goto Arg_Loop)
if /i "%1" == "runnativeaottests"                       (set RunNativeAot=1&shift&goto Arg_Loop)

if /i not "%1" == "msbuildargs" goto SkipMsbuildArgs
:: All the rest of the args will be collected and passed directly to msbuild.
:CollectMsbuildArgs
shift
if "%1"=="" goto ArgsDone
set __msbuildExtraArgs=%__msbuildExtraArgs% %1
goto CollectMsbuildArgs
:SkipMsbuildArgs

set CORE_ROOT=%1
echo %__MsgPrefix%CORE_ROOT is initially set to: "%CORE_ROOT%"
shift
:ArgsDone

:: Done with argument processing. Check argument values for validity.

if defined __TestEnv (if not exist %__TestEnv% echo %__MsgPrefix%Error: Test Environment script %__TestEnv% not found && exit /b 1)

:: Set the remaining variables based upon the determined configuration
set __MSBuildBuildArch=%__BuildArch%

set "__BinDir=%__RootBinDir%\bin\coreclr\%__TargetOS%.%__BuildArch%.%__BuildType%"
set "__TestWorkingDir=%__RootBinDir%\tests\coreclr\%__TargetOS%.%__BuildArch%.%__BuildType%"

:: Default global test environment variables
:: REVIEW: are these ever expected to be defined on entry to this script? Why? By whom?
:: REVIEW: XunitTestReportDirBase is not used in this script. Who needs to have it set? Used in run.proj _XunitProlog.
if not defined XunitTestBinBase       set  XunitTestBinBase=%__TestWorkingDir%\
if not defined XunitTestReportDirBase set  XunitTestReportDirBase=%XunitTestBinBase%\Reports\

REM Set up arguments to call run.py

set __RuntestPyArgs=-arch %__BuildArch% -build_type %__BuildType%

if defined DoLink (
    set __RuntestPyArgs=%__RuntestPyArgs% --il_link
)

if defined __LongGCTests (
    set __RuntestPyArgs=%__RuntestPyArgs% --long_gc
)

if defined __GCSimulatorTests (
    set __RuntestPyArgs=%__RuntestPyArgs% --gcsimulator
)

if defined __IlasmRoundTrip (
    set __RuntestPyArgs=%__RuntestPyArgs% --ilasmroundtrip
)

if defined __TestEnv (
    set __RuntestPyArgs=%__RuntestPyArgs% -test_env %__TestEnv%
)

if defined __Sequential (
    set __RuntestPyArgs=%__RuntestPyArgs% --sequential
)

if defined __ParallelType (
    set __RuntestPyArgs=%__RuntestPyArgs% -parallel %__ParallelType%
)

if defined RunCrossGen2 (
    set __RuntestPyArgs=%__RuntestPyArgs% --run_crossgen2_tests
)

if defined CrossgenLargeVersionBubble (
    set __RuntestPyArgs=%__RuntestPyArgs% --large_version_bubble
)

if defined CrossGen2SynthesizePgo (
    set __RuntestPyArgs=%__RuntestPyArgs% --synthesize_pgo
)

if defined __PrintLastResultsOnly (
    set __RuntestPyArgs=%__RuntestPyArgs% --analyze_results_only
)

if defined RunInUnloadableContext (
    set __RuntestPyArgs=%__RuntestPyArgs% --run_in_context
)

if defined TieringTest (
    set __RuntestPyArgs=%__RuntestPyArgs% --tiering_test
)

if defined RunNativeAot (
    set __RuntestPyArgs=%__RuntestPyArgs% --run_nativeaot_tests
)

REM Find python and set it to the variable PYTHON
set _C=-c "import sys; sys.stdout.write(sys.executable)"
(py -3 %_C% || py -2 %_C% || python3 %_C% || python2 %_C% || python %_C%) > %TEMP%\pythonlocation.txt 2> NUL
set _C=
set /p PYTHON=<%TEMP%\pythonlocation.txt

if NOT DEFINED PYTHON (
    echo %__MsgPrefix%Error: Could not find a Python installation.
    exit /b 1
)

set NEXTCMD="%PYTHON%" "%__RepoRootDir%\src\tests\run.py" %__RuntestPyArgs%
echo %NEXTCMD%
%NEXTCMD%

exit /b %ERRORLEVEL%

REM =========================================================================================
REM ===
REM === Display a help message describing how to use this script.
REM ===
REM =========================================================================================

:Usage
@REM NOTE: The caret character is used to escape meta-characters known to the CMD shell. This character does
@REM NOTE: not appear in output. Thus, while it might look like in lines below that the "-" are not aligned,
@REM NOTE: they are in the output (and please keep them aligned).
echo.
echo Usage:
echo   %0 [options] [^<CORE_ROOT^>]
echo.
echo where:
echo.
echo./? -? /h -h /help -help   - View this message.
echo ^<build_architecture^>      - Specifies build architecture: x64, x86, arm, or arm64 ^(default: x64^).
echo ^<build_type^>              - Specifies build type: Debug, Release, or Checked ^(default: Debug^).
echo TestEnv ^<test_env_script^> - Run a custom script before every test to set custom test environment settings.
echo sequential                - Run tests sequentially ^(no parallelism^).
echo parallel ^<type^>           - Run tests with given level of parallelism: none, collections, assemblies, all. Default: collections.
echo RunCrossgen2Tests         - Runs ReadytoRun tests compiled with Crossgen2
echo synthesizepgo             - Enabled synthesizing PGO data in CrossGen2
echo jitstress ^<n^>             - Runs the tests with DOTNET_JitStress=n
echo jitstressregs ^<n^>         - Runs the tests with DOTNET_JitStressRegs=n
echo jitminopts                - Runs the tests with DOTNET_JITMinOpts=1
echo jitforcerelocs            - Runs the tests with DOTNET_ForceRelocs=1
echo gcname ^<name^>             - Runs the tests with DOTNET_GCName=name
echo gcstresslevel ^<n^>         - Runs the tests with DOTNET_GCStress=n. n=0 means no GC Stress. Otherwise, n is a bitmask of the following:
echo                               1: GC on all allocations and 'easy' places
echo                               2: GC on transitions to preemptive GC
echo                               4: GC on every allowable JITed instruction
echo                               8: GC on every allowable NGEN instruction
echo                              16: GC only on a unique stack trace
echo                              (Note that the value must be expresed in hex.)
echo gcsimulator               - Run the GC Simulator tests
echo longgc                    - Run the long-running GC tests
echo ilasmroundtrip            - Runs ilasm round trip on the tests
echo link ^<ILlink^>             - Runs the tests after linking via the IL linker ^<ILlink^>.
echo printlastresultsonly      - Print the last test results without running tests.
echo runincontext              - Run each tests in an unloadable AssemblyLoadContext
echo timeout ^<n^>               - Sets the per-test timeout in milliseconds ^(default is 10 minutes = 10 * 60 * 1000 = 600000^).
echo                             Note: some options override this ^(gcstresslevel, longgc, gcsimulator^).
echo msbuildargs ^<args...^>     - Pass all subsequent args directly to msbuild invocations.
echo ^<CORE_ROOT^>               - Path to the runtime to test ^(if specified^).
echo.
echo Note that arguments are not case-sensitive.
echo.
echo Examples:
echo   %0 x86 checked
echo   %0 x64 release
exit /b 1
