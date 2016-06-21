@if not defined __echo @echo off
setlocal EnableDelayedExpansion

:: Set the default arguments
set __BuildArch=x64
set __BuildType=Debug
set __BuildOS=Windows_NT

:: Default to highest Visual Studio version available
set __VSVersion=vs2015

if defined VS120COMNTOOLS set __VSVersion=vs2013
if defined VS140COMNTOOLS set __VSVersion=vs2015

:: Define a prefix for most output progress messages that come from this script. That makes
:: it easier to see where these are coming from. Note that there is a trailing space here.
set __MsgPrefix=RUNTEST: 

set __ProjectDir=%~dp0
:: remove trailing slash
if %__ProjectDir:~-1%==\ set "__ProjectDir=%__ProjectDir:~0,-1%"
set "__ProjectFilesDir=%__ProjectDir%"
set "__RootBinDir=%__ProjectDir%\..\bin"
set "__LogsDir=%__RootBinDir%\Logs"

:: Default __Exclude to issues.targets
set __Exclude0=%~dp0\issues.targets

set __Sequential=
set __msbuildExtraArgs=
set __LongGCTests=
set __GCSimulatorTests=

:Arg_Loop
if "%1" == "" goto ArgsDone

if /i "%1" == "/?"    goto Usage
if /i "%1" == "-?"    goto Usage
if /i "%1" == "/h"    goto Usage
if /i "%1" == "-h"    goto Usage
if /i "%1" == "/help" goto Usage
if /i "%1" == "-help" goto Usage

if /i "%1" == "x64"                   (set __BuildArch=x64&set __MSBuildBuildArch=x64&shift&goto Arg_Loop)
if /i "%1" == "x86"                   (set __BuildArch=x86&set __MSBuildBuildArch=x86&shift&goto Arg_Loop)

if /i "%1" == "debug"                 (set __BuildType=Debug&shift&goto Arg_Loop)
if /i "%1" == "release"               (set __BuildType=Release&shift&goto Arg_Loop)
if /i "%1" == "checked"               (set __BuildType=Checked&shift&goto Arg_Loop)

if /i "%1" == "vs2013"                (set __VSVersion=%1&shift&goto Arg_Loop)
if /i "%1" == "vs2015"                (set __VSVersion=%1&shift&goto Arg_Loop)

if /i "%1" == "SkipWrapperGeneration" (set __SkipWrapperGeneration=true&shift&goto Arg_Loop)
if /i "%1" == "Exclude"               (set __Exclude=%2&shift&shift&goto Arg_Loop)
if /i "%1" == "Exclude0"              (set __Exclude0=%2&shift&shift&goto Arg_Loop)
if /i "%1" == "TestEnv"               (set __TestEnv=%2&shift&shift&goto Arg_Loop)
if /i "%1" == "sequential"            (set __Sequential=1&shift&goto Arg_Loop)
if /i "%1" == "crossgen"              (set __DoCrossgen=1&shift&goto Arg_Loop)
if /i "%1" == "longgc"                (set __LongGCTests=1&shift&goto Arg_Loop)
if /i "%1" == "gcsimulator"           (set __GCSimulatorTests=1&shift&goto Arg_Loop)
if /i "%1" == "jitstress"             (set COMPlus_JitStress=%2&shift&shift&goto Arg_Loop)
if /i "%1" == "jitstressregs"         (set COMPlus_JitStressRegs=%2&shift&shift&goto Arg_Loop)
if /i "%1" == "jitminopts"            (set COMPlus_JITMinOpts=1&shift&shift&goto Arg_Loop)
if /i "%1" == "jitforcerelocs"        (set COMPlus_ForceRelocs=1&shift&shift&goto Arg_Loop)
if /i "%1" == "GenerateLayoutOnly"    (set __GenerateLayoutOnly=1&set __SkipWrapperGeneration=true&shift&goto Arg_Loop)
if /i "%1" == "PerfTests"             (set __PerfTests=true&set __SkipWrapperGeneration=true&shift&goto Arg_Loop)
if /i "%1" == "runcrossgentests"      (set RunCrossGen=true&shift&goto Arg_Loop)
REM change it to COMPlus_GCStress when we stop using xunit harness
if /i "%1" == "gcstresslevel"         (set __GCSTRESSLEVEL=%2&set __TestTimeout=1800000&shift&shift&goto Arg_Loop) 

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

:: Set the remaining variables based upon the determined configuration
set "__BinDir=%__RootBinDir%\Product\%__BuildOS%.%__BuildArch%.%__BuildType%"
set "__TestWorkingDir=%__RootBinDir%\tests\%__BuildOS%.%__BuildArch%.%__BuildType%"

:: Default global test environment variables
:: REVIEW: are these ever expected to be defined on entry to this script? Why? By whom?
:: REVIEW: XunitTestReportDirBase is not used in this script. Who needs to have it set?
if not defined XunitTestBinBase       set  XunitTestBinBase=%__TestWorkingDir%
if not defined XunitTestReportDirBase set  XunitTestReportDirBase=%XunitTestBinBase%\Reports\

if not exist %__LogsDir% md %__LogsDir%

set __VSProductVersion=
if /i "%__VSVersion%" == "vs2013" set __VSProductVersion=120
if /i "%__VSVersion%" == "vs2015" set __VSProductVersion=140

:: Check presence of VS
if not defined VS%__VSProductVersion%COMNTOOLS goto NoVS

set __VSToolsRoot=!VS%__VSProductVersion%COMNTOOLS!
if %__VSToolsRoot:~-1%==\ set "__VSToolsRoot=%__VSToolsRoot:~0,-1%"

:: Does VS really exist?
if not exist "%__VSToolsRoot%\..\IDE\devenv.exe"      goto NoVS
if not exist "%__VSToolsRoot%\..\..\VC\vcvarsall.bat" goto NoVS
if not exist "%__VSToolsRoot%\VsDevCmd.bat"           goto NoVS

if /i "%__VSVersion%" =="vs2015" goto MSBuild14
set _msbuildexe="%ProgramFiles(x86)%\MSBuild\12.0\Bin\MSBuild.exe"
if not exist %_msbuildexe% set _msbuildexe="%ProgramFiles%\MSBuild\12.0\Bin\MSBuild.exe"
if not exist %_msbuildexe% set _msbuildexe="%ProgramFiles(x86)%\MSBuild\14.0\Bin\MSBuild.exe"
goto :CheckMSBuild14
:MSBuild14
set _msbuildexe="%ProgramFiles(x86)%\MSBuild\14.0\Bin\MSBuild.exe"
:CheckMSBuild14
if not exist %_msbuildexe% set _msbuildexe="%ProgramFiles%\MSBuild\14.0\Bin\MSBuild.exe"
if not exist %_msbuildexe% echo Error: Could not find MSBuild.exe.  Please see https://github.com/dotnet/coreclr/blob/master/Documentation/project-docs/developer-guide.md for build instructions. && exit /b 1

:: Set the environment for the  build- VS cmd prompt
echo %__MsgPrefix%Using environment: "%__VSToolsRoot%\VsDevCmd.bat"
call                                 "%__VSToolsRoot%\VsDevCmd.bat"

if not defined VSINSTALLDIR (
    echo %__MsgPrefix%Error: runtest.cmd should be run from a Visual Studio Command Prompt.  Please see https://github.com/dotnet/coreclr/blob/master/Documentation/project-docs/developer-guide.md for build instructions.
    exit /b 1
)

:: Note: We've disabled node reuse because it causes file locking issues.
::       The issue is that we extend the build with our own targets which
::       means that that rebuilding cannot successfully delete the task
::       assembly. 
set __msbuildCommonArgs=/nologo /nodeReuse:false %__msbuildExtraArgs%

if not defined __Sequential (
    set __msbuildCommonArgs=%__msbuildCommonArgs% /maxcpucount
) else (
    set __msbuildCommonArgs=%__msbuildCommonArgs% /p:ParallelRun=false
)

REM Prepare the Test Drop
REM Cleans any NI from the last run
powershell "Get-ChildItem -path %__TestWorkingDir% -Include '*.ni.*' -Recurse -Force | Remove-Item -force"
REM Cleans up any lock folder used for synchronization from last run
powershell "Get-ChildItem -path %__TestWorkingDir% -Include 'lock' -Recurse -Force |  where {$_.Attributes -eq 'Directory'}| Remove-Item -force -Recurse"

if defined CORE_ROOT goto SkipCoreRootSetup

set "CORE_ROOT=%XunitTestBinBase%\Tests\Core_Root"
echo %__MsgPrefix%Using Default CORE_ROOT as %CORE_ROOT%
echo %__MsgPrefix%Copying Built binaries from %__BinDir% to %CORE_ROOT%
if exist "%CORE_ROOT%" rd /s /q "%CORE_ROOT%"
md "%CORE_ROOT%"
xcopy /s "%__BinDir%" "%CORE_ROOT%"

:SkipCoreRootSetup


if defined __Exclude (if not exist %__Exclude% echo %__MsgPrefix%Error: Exclusion .targets file not found && exit /b 1)
if defined __TestEnv (if not exist %__TestEnv% echo %__MsgPrefix%Error: Test Environment script not found && exit /b 1)

REM These log files are created automatically by the test run process. Q: what do they depend on being set?
set __TestRunHtmlLog=%__LogsDir%\TestRun_%__BuildOS%__%__BuildArch%__%__BuildType%.html
set __TestRunXmlLog=%__LogsDir%\TestRun_%__BuildOS%__%__BuildArch%__%__BuildType%.xml


if "%__PerfTests%"=="true" goto RunPerfTests
if "%__SkipWrapperGeneration%"=="true" goto SkipWrapperGeneration

set __BuildLogRootName=Tests_XunitWrapper
call :msbuild "%__ProjectFilesDir%\runtest.proj" /p:BuildWrappers=true
if errorlevel 1 exit /b 1

:SkipWrapperGeneration

call :ResolveDependecies

if not defined __DoCrossgen goto :SkipPrecompileFX
call :PrecompileFX

:SkipPrecompileFX

if  defined __GenerateLayoutOnly (
    exit /b 1
)

if not exist %CORE_ROOT%\coreclr.dll (
    echo %__MsgPrefix%Error: Ensure you have done a successful build of the Product and %CORE_ROOT% contains runtime binaries.
    exit /b 1
)

::Check if the test Binaries are built
if not exist %XunitTestBinBase% (
    echo %__MsgPrefix%Error: Ensure the Test Binaries are built and are present at %XunitTestBinBase%.
    echo %__MsgPrefix%Run "buildtest.cmd %__BuildArch% %__BuildType%" to build the tests first.
    exit /b 1
)
echo %__MsgPrefix%CORE_ROOT that will be used is: %CORE_ROOT%
echo %__MsgPrefix%Starting the test run ...

set __BuildLogRootName=TestRunResults
call :msbuild "%__ProjectFilesDir%\runtest.proj" /p:Runtests=true /clp:showcommandline

if errorlevel 1 (
    echo Test Run failed. Refer to the following:
    echo     Html report: %__TestRunHtmlLog%
    exit /b 1
)

if not defined __PerfTests goto :SkipRunPerfTests

:RunPerfTests 
echo %__MsgPrefix%CORE_ROOT that will be used is: %CORE_ROOT%  
echo %__MsgPrefix%Starting the test run ...  

set __BuildLogRootName=PerfTestRunResults  
echo Running perf tests  
call :msbuild "%__ProjectFilesDir%\runtest.proj" /t:RunPerfTests /clp:showcommandline  

if errorlevel 1 (  
   echo Test Run failed. Refer to the following:  
   echo     Html report: %__TestRunHtmlLog%  
)  

:SkipRunPerfTests

REM =========================================================================================
REM ===
REM === All tests complete!
REM ===
REM =========================================================================================

echo %__MsgPrefix%Test run successful. Refer to the log files for details:
echo     %__TestRunHtmlLog%
echo     %__TestRunXmlLog%
exit /b 0

REM =========================================================================================
REM ===
REM === Helper routines
REM ===
REM =========================================================================================

REM Compile the managed assemblies in Core_ROOT before running the tests
:PrecompileAssembly

REM Skip mscorlib since it is already precompiled.
if /I "%3" == "mscorlib.dll" exit /b 0
if /I "%3" == "mscorlib.ni.dll" exit /b 0

"%1\crossgen.exe" /Platform_Assemblies_Paths "%CORE_ROOT%" "%2" >nul 2>nul
set /a __exitCode = %errorlevel%
if "%__exitCode%" == "-2146230517" (
    echo %2 is not a managed assembly.
    exit /b 0
)

if %__exitCode% neq 0 (
    echo Unable to precompile %2
    exit /b 0
)
    
echo Successfully precompiled %2
exit /b 0

:PrecompileFX
for %%F in (%CORE_ROOT%\*.dll) do call :PrecompileAssembly "%CORE_ROOT%" "%%F" %%~nF%%~xF
exit /b 0

:msbuild
@REM Subroutine to invoke msbuild. All arguments are passed to msbuild. The first argument should be the
@REM .proj file to invoke.
@REM
@REM On entry, __BuildLogRootName must be set to a file name prefix for the generated log file.
@REM All the "standard" environment variables that aren't expected to change per invocation must also be set,
@REM like __msbuildCommonArgs.
@REM
@REM The build log files will be overwritten, not appended to.

echo %__MsgPrefix%Invoking msbuild

set "__BuildLog=%__LogsDir%\%__BuildLogRootName%_%__BuildOS%__%__BuildArch%__%__BuildType%.log"
set "__BuildWrn=%__LogsDir%\%__BuildLogRootName%_%__BuildOS%__%__BuildArch%__%__BuildType%.wrn"
set "__BuildErr=%__LogsDir%\%__BuildLogRootName%_%__BuildOS%__%__BuildArch%__%__BuildType%.err"

set __msbuildLogArgs=^
/fileloggerparameters:Verbosity=normal;LogFile="%__BuildLog%";Append ^
/fileloggerparameters1:WarningsOnly;LogFile="%__BuildWrn%" ^
/fileloggerparameters2:ErrorsOnly;LogFile="%__BuildErr%" ^
/consoleloggerparameters:Summary ^
/verbosity:minimal

set __msbuildArgs=%* %__msbuildCommonArgs% %__msbuildLogArgs%

@REM The next line will overwrite the existing log file, if any.
echo Invoking: %_msbuildexe% %__msbuildArgs% > "%__BuildLog%"

%_msbuildexe% %__msbuildArgs%
if errorlevel 1 (
    echo %__MsgPrefix%Error: msbuild failed. Refer to the log files for details:
    echo     %__BuildLog%
    echo     %__BuildWrn%
    echo     %__BuildErr%
    exit /b 1
)

exit /b 0

:ResolveDependecies:

if "%CORE_ROOT%" == "" (
    echo %__MsgPrefix%Error: Ensure you have done a successful build of the Product and Run - runtest BuildArch BuildType {path to product binaries}.
    exit /b 1
)

:: Long GC tests take about 10 minutes per test on average, so
:: they often bump up against the default 10 minute timeout.
:: 20 minutes is more than enough time for a test to complete successfully.
if defined __LongGCTests (
    echo Running Long GC tests, extending timeout to 20 minutes
    set __TestTimeout=1200000
    set RunningLongGCTests=1
)

:: GCSimulator tests can take up to an hour to complete. They are run twice a week in the
:: CI, so it's fine if they take a long time.
if defined __GCSimulatorTests (
    echo Running GCSimulator tests, extending timeout to one hour
    set __TestTimeout=3600000
    set RunningGCSimulatorTests=1
)

set __BuildLogRootName=Tests_GenerateRuntimeLayout
call :msbuild "%__ProjectFilesDir%\runtest.proj" /p:GenerateRuntimeLayout=true 
if errorlevel 1 (
    echo Test Dependency Resolution Failed
    exit /b 1
)
echo %__MsgPrefix% Created the runtime layout with all dependencies in %CORE_ROOT%
exit /b 0



:Usage
echo.
echo Usage:
echo   %0 BuildArch BuildType [SkipWrapperGeneration] [Exclude EXCLUSION_TARGETS] [TestEnv TEST_ENV_SCRIPT] [VSVersion] CORE_ROOT
echo where:
echo.
echo./? -? /h -h /help -help: view this message.
echo BuildArch- Optional parameter - x64 or x86 ^(default: x64^).
echo BuildType- Optional parameter - Debug, Release, or Checked ^(default: Debug^).
echo SkipWrapperGeneration- Optional parameter - this will run the same set of tests as the last time it was run
echo Exclude0- Optional parameter - specify location of default exclusion file (defaults to issues.targets if not specified)
echo                                Set to "" to disable default exclusion file.
echo Exclude-  Optional parameter - this will exclude individual tests from running, specified by ExcludeList ItemGroup in an .targets file.
echo TestEnv- Optional parameter - this will run a custom script to set custom test environment settings.
echo VSVersion- Optional parameter - VS2013 or VS2015 ^(default: VS2015^)
echo GenerateLayoutOnly - If specified will not run the tests and will only create the Runtime Dependency Layout
echo RunCrossgenTests   - Runs ReadytoRun tests
echo jitstress n        - Runs the tests with COMPlus_JitStress=n
echo jitstressregs n    - Runs the tests with COMPlus_JitStressRegs=n
echo jitminopts         - Runs the tests with COMPlus_JITMinOpts=1
echo jitforcerelocs     - Runs the tests with COMPlus_ForceRelocs=1
echo gcstresslevel n    - Runs the tests with COMPlus_GCStress=n
echo     0: None                                1: GC on all allocs and 'easy' places
echo     2: GC on transitions to preemptive GC  4: GC on every allowable JITed instr
echo     8: GC on every allowable NGEN instr   16: GC only on a unique stack trace
echo CORE_ROOT The path to the runtime  
exit /b 1

:NoVS
echo Visual Studio 2013+ ^(Community is free^) is a prerequisite to build this repository.
echo See: https://github.com/dotnet/coreclr/blob/master/Documentation/project-docs/developer-guide.md#prerequisites
exit /b 1
