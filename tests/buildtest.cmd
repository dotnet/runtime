@if not defined __echo @echo off
setlocal EnableDelayedExpansion

:: Set the default arguments for build
set __BuildArch=x64
set __BuildType=Debug
set __BuildOS=Windows_NT

:: Default to highest Visual Studio version available
set __VSVersion=vs2015

if defined VS120COMNTOOLS set __VSVersion=vs2013
if defined VS140COMNTOOLS set __VSVersion=vs2015

:: Define a prefix for most output progress messages that come from this script. That makes
:: it easier to see where these are coming from. Note that there is a trailing space here.
set __MsgPrefix=BUILDTEST: 

set "__ProjectDir=%~dp0..\"
:: remove trailing slash
if %__ProjectDir:~-1%==\ set "__ProjectDir=%__ProjectDir:~0,-1%"
set "__TestDir=%__ProjectDir%\tests"
set "__ProjectFilesDir=%__TestDir%"
set "__SourceDir=%__ProjectDir%\src"
set "__PackagesDir=%__ProjectDir%\packages"
set "__RootBinDir=%__ProjectDir%\bin"
set "__LogsDir=%__RootBinDir%\Logs"

set __CleanBuild=
set __crossgen=
set __ILAsmRoundtrip=
set __BuildSequential=
set __TestPriority=
set __msbuildCleanBuildArgs=
set __msbuildExtraArgs=
set __verbosity=normal

:Arg_Loop
if "%1" == "" goto ArgsDone

if /i "%1" == "/?"    goto Usage
if /i "%1" == "-?"    goto Usage
if /i "%1" == "/h"    goto Usage
if /i "%1" == "-h"    goto Usage
if /i "%1" == "/help" goto Usage
if /i "%1" == "-help" goto Usage

if /i "%1" == "x64"                 (set __BuildArch=x64&shift&goto Arg_Loop)
if /i "%1" == "x86"                 (set __BuildArch=x86&shift&goto Arg_Loop)
if /i "%1" == "arm"                 (set __BuildArch=arm&shift&goto Arg_Loop)
if /i "%1" == "arm64"               (set __BuildArch=arm64&shift&goto Arg_Loop)

if /i "%1" == "debug"               (set __BuildType=Debug&shift&goto Arg_Loop)
if /i "%1" == "release"             (set __BuildType=Release&shift&goto Arg_Loop)
if /i "%1" == "checked"             (set __BuildType=Checked&shift&goto Arg_Loop)

if /i "%1" == "clean"               (set __CleanBuild=1&shift&goto Arg_Loop)

if /i "%1" == "vs2013"              (set __VSVersion=%1&shift&goto Arg_Loop)
if /i "%1" == "vs2015"              (set __VSVersion=%1&shift&goto Arg_Loop)

if /i "%1" == "crossgen"            (set __crossgen=true&shift&goto Arg_Loop)
if /i "%1" == "ilasmroundtrip"      (set __ILAsmRoundtrip=true&shift&goto Arg_Loop)
if /i "%1" == "sequential"          (set __BuildSequential=1&shift&goto Arg_Loop)
if /i "%1" == "priority"            (set __TestPriority=%2&shift&shift&goto Arg_Loop)

if /i "%1" == "verbose"             (set __verbosity=detailed&shift&goto Arg_Loop)

if /i not "%1" == "msbuildargs" goto SkipMsbuildArgs
:: All the rest of the args will be collected and passed directly to msbuild.
:CollectMsbuildArgs
shift
if "%1"=="" goto ArgsDone
set __msbuildExtraArgs=%__msbuildExtraArgs% %1
goto CollectMsbuildArgs
:SkipMsbuildArgs

echo Invalid command-line argument: %1
goto Usage

:ArgsDone

if %__verbosity%==detailed (
    echo Enabling verbose file logging
)

echo %__MsgPrefix%Commencing CoreCLR repo test build

set "__BinDir=%__RootBinDir%\Product\%__BuildOS%.%__BuildArch%.%__BuildType%"
set "__TestBinDir=%__RootBinDir%\tests\%__BuildOS%.%__BuildArch%.%__BuildType%\"
:: We have different managed and native intermediate dirs because the managed bits will include
:: the configuration information deeper in the intermediates path.
:: These variables are used by the msbuild project files.

if not defined __TestIntermediateDir (
    set "__TestIntermediateDir=tests\obj\%__BuildOS%.%__BuildArch%.%__BuildType%"
)
set "__NativeTestIntermediatesDir=%__RootBinDir%\%__TestIntermediateDirDir%\Native"
set "__ManagedTestIntermediatesDir=%__RootBinDir%\%__TestIntermediateDir%\Managed"

:: Generate path to be set for CMAKE_INSTALL_PREFIX to contain forward slash
set "__CMakeBinDir=%__TestBinDir%"
set "__CMakeBinDir=%__CMakeBinDir:\=/%"

:: Configure environment if we are doing a clean build.
if not defined __CleanBuild goto SkipCleanBuild
echo %__MsgPrefix%Doing a clean test build

:: MSBuild projects would need a rebuild
set __msbuildCleanBuildArgs=/t:rebuild

:: Cleanup the binaries drop folder for the current configuration
if exist "%__TestBinDir%"                   rd /s /q "%__TestBinDir%"
if exist "%__NativeTestIntermediatesDir%"   rd /s /q "%__NativeTestIntermediatesDir%"

:SkipCleanBuild

if not exist "%__TestBinDir%"                   md "%__TestBinDir%"
if not exist "%__NativeTestIntermediatesDir%"   md "%__NativeTestIntermediatesDir%"
if not exist "%__ManagedTestIntermediatesDir%"  md "%__ManagedTestIntermediatesDir%"
if not exist "%__LogsDir%"                      md "%__LogsDir%"

echo %__MsgPrefix%Checking prerequisites

:: Eval the output from probe-win1.ps1
for /f "delims=" %%a in ('powershell -NoProfile -ExecutionPolicy RemoteSigned "& ""%__SourceDir%\pal\tools\probe-win.ps1"""') do %%a

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
set UseRoslynCompiler=true
:CheckMSBuild14
if not exist %_msbuildexe% set _msbuildexe="%ProgramFiles%\MSBuild\14.0\Bin\MSBuild.exe"
if not exist %_msbuildexe% echo Error: Could not find MSBuild.exe.  Please see https://github.com/dotnet/coreclr/blob/master/Documentation/project-docs/developer-guide.md for build instructions. && exit /b 1

:: Note: We've disabled node reuse because it causes file locking issues.
::       The issue is that we extend the build with our own targets which
::       means that that rebuilding cannot successfully delete the task
::       assembly. 
set __msbuildCommonArgs=/nologo /nodeReuse:false %__msbuildExtraArgs%

if not defined __BuildSequential (
    set __msbuildCommonArgs=%__msbuildCommonArgs% /maxcpucount
)
REM =========================================================================================
REM ===
REM === Restore Build Tools
REM ===
REM =========================================================================================
call %__ProjectDir%\init-tools.cmd 


REM =========================================================================================
REM ===
REM === Native test build section
REM ===
REM =========================================================================================

::Building Native part of Tests
setlocal EnableDelayedExpansion

echo %__MsgPrefix%Commencing build of native test components for %__BuildArch%/%__BuildType%

:: Set the environment for the native build
echo %__MsgPrefix%Using environment: "%__VSToolsRoot%\..\..\VC\vcvarsall.bat" %__VCBuildArch%
call                                 "%__VSToolsRoot%\..\..\VC\vcvarsall.bat" x86_amd64
@if defined __echo @echo on

if not defined VSINSTALLDIR (
    echo %__MsgPrefix%Error: VSINSTALLDIR variable not defined.
    exit /b 1
)
if not exist "%VSINSTALLDIR%DIA SDK" goto NoDIA

echo %__MsgPrefix%Regenerating the Visual Studio solution

pushd "%__NativeTestIntermediatesDir%"
call "%__SourceDir%\pal\tools\gen-buildsys-win.bat" "%__ProjectFilesDir%\" %__VSVersion% %__BuildArch%
@if defined __echo @echo on
popd

if not exist "%__NativeTestIntermediatesDir%\install.vcxproj" (
    echo %__MsgPrefix%Failed to generate test native component build project!
    exit /b 1
)

set __BuildLogRootName=Tests_Native
call :msbuild "%__NativeTestIntermediatesDir%\install.vcxproj" %__msbuildCleanBuildArgs% /p:Configuration=%__BuildType% /p:Platform=%__BuildArch%
if errorlevel 1 exit /b 1

REM endlocal to rid us of environment changes from vcvarsall.bat
endlocal

REM =========================================================================================
REM ===
REM === Managed test build section
REM ===
REM =========================================================================================

REM setlocal to prepare for vsdevcmd.bat
setlocal EnableDelayedExpansion

echo %__MsgPrefix%Starting the Managed Tests Build

:: Set the environment for the managed build
echo %__MsgPrefix%Using environment: "%__VSToolsRoot%\VsDevCmd.bat"
call                                 "%__VSToolsRoot%\VsDevCmd.bat"

if not defined VSINSTALLDIR (
    echo %__MsgPrefix%Error: buildtest.cmd should be run from a Visual Studio Command Prompt.  Please see https://github.com/dotnet/coreclr/blob/master/Documentation/project-docs/developer-guide.md for build instructions.
    exit /b 1
)

set __msbuildManagedBuildArgs=%__msbuildCleanBuildArgs%

if defined __crossgen (
    echo Building tests with CrossGen enabled.
    set __msbuildManagedBuildArgs=%__msbuildManagedBuildArgs% /p:CrossGen=true
)

if defined __ILAsmRoundtrip (
    echo Building tests with IlasmRoundTrip enabled.
    set __msbuildManagedBuildArgs=%__msbuildManagedBuildArgs% /p:IlasmRoundTrip=true
)

if defined __TestPriority (
    echo Building Test Priority %__TestPriority%
    set __msbuildManagedBuildArgs=%__msbuildManagedBuildArgs% /p:CLRTestPriorityToBuild=%__TestPriority%
)

set __BuildLogRootName=Tests_Managed
call :msbuild "%__ProjectFilesDir%\build.proj" %__msbuildManagedBuildArgs%
if errorlevel 1 exit /b 1

set CORE_ROOT=%__TestBinDir%\Tests\Core_Root

echo %__MsgPrefix%Creating test overlay...

set __BuildLogRootName=Tests_Overlay_Managed
call :msbuild "%__ProjectFilesDir%\runtest.proj" /t:CreateTestOverlay
if errorlevel 1 exit /b 1

REM =========================================================================================
REM ===
REM === All builds complete!
REM ===
REM =========================================================================================

echo %__MsgPrefix%Test build successful.
echo %__MsgPrefix%Test binaries are available at !__TestBinDir!
exit /b 0

REM =========================================================================================
REM ===
REM === Helper routines
REM ===
REM =========================================================================================

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
/fileloggerparameters:Verbosity=%__verbosity%;LogFile="%__BuildLog%";Append ^
/fileloggerparameters1:WarningsOnly;LogFile="%__BuildWrn%" ^
/fileloggerparameters2:ErrorsOnly;LogFile="%__BuildErr%" ^
/consoleloggerparameters:Summary ^
/verbosity:minimal

set __msbuildArgs=%* %__msbuildCommonArgs% %__msbuildLogArgs%

@REM The next line will overwrite the existing log file, if any.
echo Invoking: %_msbuildexe% %__msbuildArgs% > "%__BuildLog%"

%_msbuildexe% %__msbuildArgs%
if errorlevel 1 (
    echo %__MsgPrefix%Error: build failed. Refer to the build log files for details:
    echo     %__BuildLog%
    echo     %__BuildWrn%
    echo     %__BuildErr%
    exit /b 1
)

exit /b 0

:Usage
echo.
echo Usage:
echo     %0 [option1] [option2] ...
echo All arguments are optional. Options are case-insensitive. The options are:
echo.
echo./? -? /h -h /help -help: view this message.
echo Build architecture: only x64 is currently allowed ^(default: x64^).
echo Build type: one of Debug, Checked, Release ^(default: Debug^).
echo Visual Studio version: one of VS2013 or VS2015 to force using a particular
echo     Visual Studio version ^(default: VS2015^).
echo clean: force a clean build ^(default is to perform an incremental build^).
echo CrossGen: enables the tests to run crossgen on the test executables before executing them. 
echo msbuildargs ... : all arguments following this tag will be passed directly to msbuild.
echo priority ^<N^> : specify a set of test that will be built and run, with priority N.
echo sequential: force a non-parallel build ^(default is to build in parallel
echo     using all processors^).
echo IlasmRoundTrip: enables ilasm round trip build and run of the tests before executing them.
echo verbose: enables detailed file logging for the msbuild tasks into the msbuild log file.
exit /b 1

:NoVS
echo Visual Studio 2013+ ^(Community is free^) is a prerequisite to build this repository.
echo See: https://github.com/dotnet/coreclr/blob/master/Documentation/project-docs/developer-guide.md#prerequisites
exit /b 1

:NoDIA
echo Error: DIA SDK is missing at "%VSINSTALLDIR%DIA SDK". ^
This is due to a bug in the Visual Studio installer. It does not install DIA SDK at "%VSINSTALLDIR%" but rather ^
at the install location of previous Visual Studio version. The workaround is to copy the DIA SDK folder from the Visual Studio install location ^
of the previous version to "%VSINSTALLDIR%" and then build.
:: DIA SDK not included in Express editions
echo Visual Studio 2013 Express does not include the DIA SDK. ^
You need Visual Studio 2013+ (Community is free).
echo See: https://github.com/dotnet/coreclr/blob/master/Documentation/project-docs/developer-guide.md#prerequisites
exit /b 1
