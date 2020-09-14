@if not defined _echo @echo off
setlocal EnableDelayedExpansion EnableExtensions

:: Define a prefix for most output progress messages that come from this script. That makes
:: it easier to see where these are coming from. Note that there is a trailing space here.
set "__MsgPrefix=RUNTEST: "

:: Set the default arguments
set __BuildArch=x64
set __BuildType=Debug
set __TargetOS=Windows_NT

set "__ProjectDir=%~dp0"
set "__RepoRootDir=%~dp0\..\.."
:: remove trailing slash
if %__ProjectDir:~-1%==\ set "__ProjectDir=%__ProjectDir:~0,-1%"
set "__ProjectFilesDir=%__ProjectDir%"
set "__RootBinDir=%~dp0..\..\..\artifacts"
set "__LogsDir=%__RootBinDir%\log"
set "__MsbuildDebugLogsDir=%__LogsDir%\MsbuildDebugLogs"
set __ToolsDir=%__ProjectDir%\..\Tools
set "DotNetCli=%__ProjectDir%\..\..\..\dotnet.cmd"

set __Sequential=
set __msbuildExtraArgs=
set __LongGCTests=
set __GCSimulatorTests=
set __JitDisasm=
set __IlasmRoundTrip=
set __DoCrossgen=
set __CrossgenAltJit=
set __PrintLastResultsOnly=
set RunInUnloadableContext=

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

if /i "%1" == "vs2017"                                  (set __VSVersion=%1&shift&goto Arg_Loop)
if /i "%1" == "vs2019"                                  (set __VSVersion=%1&shift&goto Arg_Loop)

if /i "%1" == "TestEnv"                                 (set __TestEnv=%2&shift&shift&goto Arg_Loop)
if /i "%1" == "AgainstPackages"                         (echo error: Remove /AgainstPackages switch&&echo /b 1)
if /i "%1" == "sequential"                              (set __Sequential=1&shift&goto Arg_Loop)
if /i "%1" == "crossgen"                                (set __DoCrossgen=1&shift&goto Arg_Loop)
if /i "%1" == "crossgenaltjit"                          (set __DoCrossgen=1&set __CrossgenAltJit=%2&shift&shift&goto Arg_Loop)
if /i "%1" == "longgc"                                  (set __LongGCTests=1&shift&goto Arg_Loop)
if /i "%1" == "gcsimulator"                             (set __GCSimulatorTests=1&shift&goto Arg_Loop)
if /i "%1" == "jitstress"                               (set COMPlus_JitStress=%2&shift&shift&goto Arg_Loop)
if /i "%1" == "jitstressregs"                           (set COMPlus_JitStressRegs=%2&shift&shift&goto Arg_Loop)
if /i "%1" == "jitminopts"                              (set COMPlus_JITMinOpts=1&shift&goto Arg_Loop)
if /i "%1" == "jitforcerelocs"                          (set COMPlus_ForceRelocs=1&shift&goto Arg_Loop)
if /i "%1" == "jitdisasm"                               (set __JitDisasm=1&shift&goto Arg_Loop)
if /i "%1" == "ilasmroundtrip"                          (set __IlasmRoundTrip=1&shift&goto Arg_Loop)

if /i "%1" == "printlastresultsonly"                    (set __PrintLastResultsOnly=1&shift&goto Arg_Loop)
if /i "%1" == "runcrossgentests"                        (set RunCrossGen=true&shift&goto Arg_Loop)
if /i "%1" == "runcrossgen2tests"                       (set RunCrossGen2=true&shift&goto Arg_Loop)
REM This test feature is currently intentionally undocumented
if /i "%1" == "runlargeversionbubblecrossgentests"      (set RunCrossGen=true&set CrossgenLargeVersionBubble=true&shift&goto Arg_Loop)
if /i "%1" == "runlargeversionbubblecrossgen2tests"     (set RunCrossGen2=true&set CrossgenLargeVersionBubble=true&shift&goto Arg_Loop)
if /i "%1" == "link"                                    (set DoLink=true&set ILLINK=%2&shift&shift&goto Arg_Loop)
REM tieredcompilation is on by default now, but setting this environment variable is harmless and I didn't want to break any automation that might be using it just yet
if /i "%1" == "tieredcompilation"                       (set COMPLUS_TieredCompilation=1&shift&goto Arg_Loop)
if /i "%1" == "gcname"                                  (set COMPlus_GCName=%2&shift&shift&goto Arg_Loop)
if /i "%1" == "timeout"                                 (set __TestTimeout=%2&shift&shift&goto Arg_Loop)
if /i "%1" == "altjitarch"                              (set __AltJitArch=%2&shift&shift&goto Arg_Loop)

REM change it to COMPlus_GCStress when we stop using xunit harness
if /i "%1" == "gcstresslevel"                           (set COMPlus_GCStress=%2&set __TestTimeout=1800000&shift&shift&goto Arg_Loop)

if /i "%1" == "runincontext"                            (set RunInUnloadableContext=1&shift&goto Arg_Loop)

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
:: REVIEW: XunitTestReportDirBase is not used in this script. Who needs to have it set?
if not defined XunitTestBinBase       set  XunitTestBinBase=%__TestWorkingDir%\
if not defined XunitTestReportDirBase set  XunitTestReportDirBase=%XunitTestBinBase%\Reports\

REM We are not running in the official build scenario, call runtest.py

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

if defined __JitDisasm (
    set __RuntestPyArgs=%__RuntestPyArgs% --jitdisasm
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

if defined RunCrossGen (
    set __RuntestPyArgs=%__RuntestPyArgs% --run_crossgen_tests
)

if defined RunCrossGen2 (
    set __RuntestPyArgs=%__RuntestPyArgs% --run_crossgen2_tests
)

if defined __DoCrossgen (
    set __RuntestPyArgs=%__RuntestPyArgs% --precompile_core_root
)

if defined CrossgenLargeVersionBubble (
    set __RuntestPyArgs=%__RuntestPyArgs% --large_version_bubble
)

if defined __PrintLastResultsOnly (
    set __RuntestPyArgs=%__RuntestPyArgs% --analyze_results_only
)

if defined __AltJitArch (
    set __RuntestPyArgs=%__RuntestPyArgs% -altjit_arch %__AltJitArch%
)

if defined RunInUnloadableContext (
    set __RuntestPyArgs=%__RuntestPyArgs% --run_in_context
)

set NEXTCMD=python "%__RepoRootDir%\src\coreclr\tests\runtest.py" %__RuntestPyArgs%
echo !NEXTCMD!
!NEXTCMD!

exit /b %ERRORLEVEL%

:SetupMSBuildAndCallRuntestProj

:: Note: We've disabled node reuse because it causes file locking issues.
::       The issue is that we extend the build with our own targets which
::       means that that rebuilding cannot successfully delete the task
::       assembly.
set __msbuildCommonArgs=/nologo /nodeReuse:false %__msbuildExtraArgs% /p:Platform=%__MSBuildBuildArch%

if not defined __Sequential (
    set __msbuildCommonArgs=%__msbuildCommonArgs% /maxcpucount
) else (
    set __msbuildCommonArgs=%__msbuildCommonArgs% /p:ParallelRun=false
)

if defined DoLink (
    set __msbuildCommonArgs=%__msbuildCommonArgs% /p:RunTestsViaIllink=true
)

if not exist "%__LogsDir%"                      md "%__LogsDir%"
if not exist "%__MsbuildDebugLogsDir%"          md "%__MsbuildDebugLogsDir%"

REM Set up the directory for MSBuild debug logs.
set MSBUILDDEBUGPATH=%__MsbuildDebugLogsDir%

REM These log files are created automatically by the test run process. Q: what do they depend on being set?
set __TestRunHtmlLog=%__LogsDir%\TestRun_%__TargetOS%__%__BuildArch%__%__BuildType%.html
set __TestRunXmlLog=%__LogsDir%\TestRun_%__TargetOS%__%__BuildArch%__%__BuildType%.xml

REM Prepare the Test Drop

echo %__MsgPrefix%Removing 'ni' files and 'lock' folders from %__TestWorkingDir%
REM Cleans any NI from the last run
powershell -NoProfile "Get-ChildItem -path %__TestWorkingDir% -Include '*.ni.*' -Recurse -Force | Remove-Item -force"
REM Cleans up any lock folder used for synchronization from last run
powershell -NoProfile "Get-ChildItem -path %__TestWorkingDir% -Include 'lock' -Recurse -Force |  where {$_.Attributes -eq 'Directory'}| Remove-Item -force -Recurse"

if defined CORE_ROOT goto SkipCoreRootSetup

set "CORE_ROOT=%XunitTestBinBase%\Tests\Core_Root"
echo %__MsgPrefix%Using Default CORE_ROOT as %CORE_ROOT%
echo %__MsgPrefix%Copying Built binaries from %__BinDir% to %CORE_ROOT%
if exist "%CORE_ROOT%" rd /s /q "%CORE_ROOT%"
md "%CORE_ROOT%"
xcopy "%__BinDir%" "%CORE_ROOT%"

:SkipCoreRootSetup

if not exist %CORE_ROOT%\coreclr.dll (
    echo %__MsgPrefix%Error: Ensure you have done a successful build of the Product and %CORE_ROOT% contains runtime binaries.
    exit /b 1
)

REM =========================================================================================
REM ===
REM === Run normal (non-perf) tests
REM ===
REM =========================================================================================

call :SetTestEnvironment

call :ResolveDependencies
if errorlevel 1 exit /b 1

if defined __DoCrossgen (
    echo %__MsgPrefix%Running crossgen on framework assemblies
    call :PrecompileFX
)

REM Delete the unecessary mscorlib.ni file.
if exist %CORE_ROOT%\mscorlib.ni.dll del %CORE_ROOT%\mscorlib.ni.dll

::Check if the test Binaries are built
if not exist %XunitTestBinBase% (
    echo %__MsgPrefix%Error: Ensure the Test Binaries are built and are present at %XunitTestBinBase%.
    echo %__MsgPrefix%Run "buildtest.cmd %__BuildArch% %__BuildType%" to build the tests first.
    exit /b 1
)

echo %__MsgPrefix%CORE_ROOT that will be used is: %CORE_ROOT%
echo %__MsgPrefix%Starting test run at %TIME%

set __BuildLogRootName=TestRunResults
call :msbuild "%__ProjectFilesDir%\src\runtest.proj" /p:Runtests=true /clp:showcommandline
set __errorlevel=%errorlevel%


if %__errorlevel% GEQ 1 (
    echo %__MsgPrefix%Test Run failed. Refer to the following:
    echo     Html report: %__TestRunHtmlLog%
    exit /b 1
)

goto TestsDone

REM =========================================================================================
REM ===
REM === All tests complete!
REM ===
REM =========================================================================================

:TestsDone

echo %__MsgPrefix%Test run successful. Finished at %TIME%. Refer to the log files for details:
echo     %__TestRunHtmlLog%
echo     %__TestRunXmlLog%
exit /b 0

REM =========================================================================================
REM ===
REM === Compile the managed assemblies in Core_ROOT before running the tests
REM ===
REM =========================================================================================

:PrecompileAssembly

if defined __JitDisasm goto :jitdisasm

REM Skip mscorlib since it is already precompiled.
if /I "%3" == "mscorlib.dll" exit /b 0
if /I "%3" == "mscorlib.ni.dll" exit /b 0

"%1\crossgen.exe" /nologo /Platform_Assemblies_Paths "%CORE_ROOT%" "%2" >nul 2>nul
set /a __exitCode = %errorlevel%
if "%__exitCode%" == "-2146230517" (
    echo %2 is not a managed assembly.
    exit /b 0
)

if %__exitCode% neq 0 (
    echo Unable to precompile %2
    exit /b 0
)

echo %__MsgPrefix%Successfully precompiled %2
exit /b 0

:jitdisasm

if /I "%3" == "mscorlib.ni.dll" exit /b 0

echo "%1\corerun" "%1\jit-dasm.dll" --crossgen %1\crossgen.exe --platform %CORE_ROOT% --output %__TestWorkingDir%\dasm "%2"
"%1\corerun" "%1\jit-dasm.dll" --crossgen %1\crossgen.exe --platform %CORE_ROOT% --output %__TestWorkingDir%\dasm "%2"
set /a __exitCode = %errorlevel%

if "%__exitCode%" == "-2146230517" (
    echo %2 is not a managed assembly.
    exit /b 0
)

if %__exitCode% neq 0 (
    echo Unable to precompile %2
    exit /b 0
)

echo %__MsgPrefix%Successfully precompiled and generated dasm for %2
exit /b 0

:PrecompileFX
setlocal

if defined __CrossgenAltJit (
    REM Set altjit flags for the crossgen run. Note that this entire crossgen section is within a setlocal/endlocal scope,
    REM so we don't need to save or unset these afterwards.
    echo %__MsgPrefix%Setting altjit environment variables for %__CrossgenAltJit%.
    set COMPlus_AltJit=*
    set COMPlus_AltJitNgen=*
    set COMPlus_AltJitName=%__CrossgenAltJit%
    set COMPlus_AltJitAssertOnNYI=1
    set COMPlus_NoGuiOnAssert=1
    set COMPlus_ContinueOnAssert=0
)

for %%F in (%CORE_ROOT%\*.dll) do call :PrecompileAssembly "%CORE_ROOT%" "%%F" %%~nF%%~xF
endlocal
exit /b 0

REM =========================================================================================
REM ===
REM === Subroutine to invoke msbuild.
REM ===
REM === All arguments are passed to msbuild. The first argument should be the .proj file to invoke.
REM ===
REM === On entry, environment variable __BuildLogRootName must be set to a file name prefix for the generated log files.
REM === All the "standard" environment variables that aren't expected to change per invocation must also be set,
REM === like __msbuildCommonArgs.
REM ===
REM === The build log files will be overwritten, not appended to.
REM ===
REM =========================================================================================

:msbuild

echo %__MsgPrefix%Invoking msbuild

set "__BuildLog=%__LogsDir%\%__BuildLogRootName%_%__TargetOS%__%__BuildArch%__%__BuildType%.log"
set "__BuildWrn=%__LogsDir%\%__BuildLogRootName%_%__TargetOS%__%__BuildArch%__%__BuildType%.wrn"
set "__BuildErr=%__LogsDir%\%__BuildLogRootName%_%__TargetOS%__%__BuildArch%__%__BuildType%.err"

set __msbuildLogArgs=^
/fileloggerparameters:Verbosity=normal;LogFile="%__BuildLog%";Append ^
/fileloggerparameters1:WarningsOnly;LogFile="%__BuildWrn%" ^
/fileloggerparameters2:ErrorsOnly;LogFile="%__BuildErr%" ^
/consoleloggerparameters:Summary ^
/verbosity:minimal

set __msbuildArgs=%* %__msbuildCommonArgs% %__msbuildLogArgs%

@REM The next line will overwrite the existing log file, if any.
echo %__MsgPrefix%"%DotNetCli%" msbuild %__msbuildArgs%
echo Invoking: "%DotNetCli%" msbuild %__msbuildArgs% > "%__BuildLog%"

call "%DotNetCli%" msbuild %__msbuildArgs%
if errorlevel 1 (
    echo %__MsgPrefix%Error: msbuild failed. Refer to the log files for details:
    echo     %__BuildLog%
    echo     %__BuildWrn%
    echo     %__BuildErr%
    exit /b 1
)

exit /b 0

REM =========================================================================================
REM ===
REM === Set various environment variables, based on arguments to this script, before invoking the tests.
REM ===
REM =========================================================================================

:SetTestEnvironment

:: Long GC tests take about 10 minutes per test on average, so
:: they often bump up against the default 10 minute timeout.
:: 20 minutes is more than enough time for a test to complete successfully.
if defined __LongGCTests (
    echo %__MsgPrefix%Running Long GC tests, extending timeout to 20 minutes
    set __TestTimeout=1200000
    set RunningLongGCTests=1
)

:: GCSimulator tests can take up to an hour to complete. They are run twice a week in the
:: CI, so it's fine if they take a long time.
if defined __GCSimulatorTests (
    echo %__MsgPrefix%Running GCSimulator tests, extending timeout to one hour
    set __TestTimeout=3600000
    set RunningGCSimulatorTests=1
)

if defined __JitDisasm (
    if defined __DoCrossgen (
        echo %__MsgPrefix%Running jit disasm on framework and test assemblies
    )
    if not defined __DoCrossgen (
       echo %__MsgPrefix%Running jit disasm on test assemblies only
    )
    set RunningJitDisasm=1
)

if defined __IlasmRoundTrip (
    echo %__MsgPrefix%Running Ilasm round trip
    set RunningIlasmRoundTrip=1
)

exit /b 0

REM =========================================================================================
REM ===
REM === Generate the "layout" directory in CORE_ROOT; download dependencies.
REM ===
REM =========================================================================================

:ResolveDependencies

set __BuildLogRootName=Tests_GenerateRuntimeLayout
call :msbuild "%__ProjectFilesDir%\src\runtest.proj" /p:GenerateRuntimeLayout=true
if errorlevel 1 (
    echo %__MsgPrefix%Test Dependency Resolution Failed
    exit /b 1
)
echo %__MsgPrefix%Created the runtime layout with all dependencies in %CORE_ROOT%

exit /b 0

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
echo VSVersion ^<vs_version^>    - VS2017 or VS2019 ^(default: VS2019^).
echo TestEnv ^<test_env_script^> - Run a custom script before every test to set custom test environment settings.
echo sequential                - Run tests sequentially (no parallelism).
echo crossgen                  - Precompile ^(crossgen^) the managed assemblies in CORE_ROOT before running the tests.
echo crossgenaltjit ^<altjit^>   - Precompile ^(crossgen^) the managed assemblies in CORE_ROOT before running the tests, using the given altjit.
echo link ^<ILlink^>             - Runs the tests after linking via the IL linker ^<ILlink^>.
echo RunCrossgenTests          - Runs ReadytoRun tests
echo jitstress ^<n^>             - Runs the tests with COMPlus_JitStress=n
echo jitstressregs ^<n^>         - Runs the tests with COMPlus_JitStressRegs=n
echo jitminopts                - Runs the tests with COMPlus_JITMinOpts=1
echo jitforcerelocs            - Runs the tests with COMPlus_ForceRelocs=1
echo jitdisasm                 - Runs jit-dasm on the tests
echo ilasmroundtrip            - Runs ilasm round trip on the tests
echo longgc                    - Run the long-running GC tests
echo gcsimulator               - Run the GC Simulator tests
echo gcstresslevel ^<n^>         - Runs the tests with COMPlus_GCStress=n. n=0 means no GC Stress. Otherwise, n is a bitmask of the following:
echo                               1: GC on all allocations and 'easy' places
echo                               2: GC on transitions to preemptive GC
echo                               4: GC on every allowable JITed instruction
echo                               8: GC on every allowable NGEN instruction
echo                              16: GC only on a unique stack trace
echo tieredcompilation         - Run the tests with COMPlus_TieredCompilation=1
echo gcname ^<name^>             - Runs the tests with COMPlus_GCName=name
echo timeout ^<n^>               - Sets the per-test timeout in milliseconds ^(default is 10 minutes = 10 * 60 * 1000 = 600000^).
echo                             Note: some options override this ^(gcstresslevel, longgc, gcsimulator^).
echo printlastresultsonly      - Print the last test results without running tests.
echo runincontext              - Run each tests in an unloadable AssemblyLoadContext
echo msbuildargs ^<args...^>     - Pass all subsequent args directly to msbuild invocations.
echo ^<CORE_ROOT^>               - Path to the runtime to test ^(if specified^).
echo.
echo Note that arguments are not case-sensitive.
echo.
echo Examples:
echo   %0 x86 checked
echo   %0 x64 release
exit /b 1
