@if not defined _echo @echo off
setlocal EnableDelayedExpansion

:: Set the default arguments for build
set __BuildArch=x64
set __VCBuildArch=x86_amd64
set __BuildType=Debug
set __BuildOS=Windows_NT
set __VSVersion=vs2015
set __VSToolsRoot=%VS140COMNTOOLS%

:: Define a prefix for most output progress messages that come from this script. That makes
:: it easier to see where these are coming from. Note that there is a trailing space here.
set __MsgPrefix=BUILDTEST: 

set "__ProjectDir=%~dp0"
:: remove trailing slash
if %__ProjectDir:~-1%==\ set "__ProjectDir=%__ProjectDir:~0,-1%"
set "__TestDir=%__ProjectDir%\tests"
set "__ProjectFilesDir=%__TestDir%"
set "__SourceDir=%__ProjectDir%\src"
set "__PackagesDir=%__ProjectDir%\packages"
set "__RootBinDir=%__ProjectDir%\bin"
set "__LogsDir=%__RootBinDir%\Logs"

:: Default __Exclude to issues.targets
set __Exclude=%__TestDir%\issues.targets

REM __unprocessedBuildArgs are args that we pass to msbuild (e.g. /p:__BuildArch=x64)
set "__args= %*"
set processedArgs=
set __unprocessedBuildArgs=
set __RunArgs=
set __BuildAgainstPackages=
set __BuildAgainstPackagesArg=
set __RuntimeId=

:Arg_Loop
if "%1" == "" goto ArgsDone

if /i "%1" == "-?"    goto Usage
if /i "%1" == "-h"    goto Usage
if /i "%1" == "-help" goto Usage

if /i "%1" == "x64"                   (set __BuildArch=x64&set __VCBuildArch=x86_amd64&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "x86"                   (set __BuildArch=x86&set __VCBuildArch=x86&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "arm"                   (set __BuildArch=arm&set __VCBuildArch=x86_arm&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "arm64"                 (set __BuildArch=arm64&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)

if /i "%1" == "debug"                 (set __BuildType=Debug&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "release"               (set __BuildType=Release&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "checked"               (set __BuildType=Checked&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)

if /i "%1" == "skipmanaged"           (set __SkipManaged=1&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "updateinvalidpackages" (set __UpdateInvalidPackagesArg=1&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "toolset_dir"           (set __ToolsetDir=%2&set __PassThroughArgs=%__PassThroughArgs% %2&set processedArgs=!processedArgs! %1 %2&shift&shift&goto Arg_Loop)
if /i "%1" == "buildagainstpackages"  (set __BuildAgainstPackages=1&set __BuildAgainstPackagesArg=-BuildTestsAgainstPackages&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "runtimeid"             (set __RuntimeId=%2&set processedArgs=!processedArgs! %1 %2&shift&shift&goto Arg_Loop)
if /i "%1" == "Exclude"              (set __Exclude=%2&set processedArgs=!processedArgs! %1 %2&shift&shift&goto Arg_Loop)

if [!processedArgs!]==[] (
  call set __UnprocessedBuildArgs=!__args!
) else (
  call set __UnprocessedBuildArgs=%%__args:*!processedArgs!=%%
)

:ArgsDone

echo %__MsgPrefix%Using environment: "%__VSToolsRoot%\VsDevCmd.bat"
call                                 "%__VSToolsRoot%\VsDevCmd.bat"

set __RunArgs=-BuildOS=%__BuildOS% -BuildType=%__BuildType% -BuildArch=%__BuildArch%

rem arm64 builds currently use private toolset which has not been released yet
REM TODO, remove once the toolset is open.
if /i "%__BuildArch%" == "arm64" call :PrivateToolSet

echo %__MsgPrefix%Commencing CoreCLR repo test build

set "__BinDir=%__RootBinDir%\Product\%__BuildOS%.%__BuildArch%.%__BuildType%"
set "__TestRootDir=%__RootBinDir%\tests"
set "__TestBinDir=%__TestRootDir%\%__BuildOS%.%__BuildArch%.%__BuildType%"
:: We have different managed and native intermediate dirs because the managed bits will include
:: the configuration information deeper in the intermediates path.
:: These variables are used by the msbuild project files.

if not defined __TestIntermediateDir (
    set "__TestIntermediateDir=tests\obj\%__BuildOS%.%__BuildArch%.%__BuildType%"
)
set "__NativeTestIntermediatesDir=%__RootBinDir%\%__TestIntermediateDir%\Native"
set "__ManagedTestIntermediatesDir=%__RootBinDir%\%__TestIntermediateDir%\Managed"

:: Generate path to be set for CMAKE_INSTALL_PREFIX to contain forward slash
set "__CMakeBinDir=%__TestBinDir%"
set "__CMakeBinDir=%__CMakeBinDir:\=/%"

if not exist "%__TestBinDir%"                   md "%__TestBinDir%"
if not exist "%__NativeTestIntermediatesDir%"   md "%__NativeTestIntermediatesDir%"
if not exist "%__ManagedTestIntermediatesDir%"  md "%__ManagedTestIntermediatesDir%"
if not exist "%__LogsDir%"                      md "%__LogsDir%"

echo %__MsgPrefix%Checking prerequisites

:: Eval the output from probe-win1.ps1
for /f "delims=" %%a in ('powershell -NoProfile -ExecutionPolicy ByPass "& ""%__SourceDir%\pal\tools\probe-win.ps1"""') do %%a

REM =========================================================================================
REM ===
REM === Restore Build Tools
REM ===
REM =========================================================================================
call "%__ProjectDir%\init-tools.cmd"

REM =========================================================================================
REM ===
REM === Resolve runtime dependences
REM ===
REM =========================================================================================
call "%__TestDir%\setup-runtime-dependencies.cmd" /arch %__BuildArch% /outputdir %__BinDir%

if defined __UpdateInvalidPackagesArg (
  goto skipnative
)

REM =========================================================================================
REM ===
REM === Native test build section
REM ===
REM =========================================================================================

echo %__MsgPrefix%Commencing build of native test components for %__BuildArch%/%__BuildType%

if defined __ToolsetDir (
 echo %__MsgPrefix%ToolsetDir is defined to be :%__ToolsetDir%
 goto GenVSSolution :: Private ToolSet is Defined
)

:: Set the environment for the native build
echo %__MsgPrefix%Using environment: "%__VSToolsRoot%\..\..\VC\vcvarsall.bat" %__VCBuildArch%
call                                 "%__VSToolsRoot%\..\..\VC\vcvarsall.bat" %__VCBuildArch%
@if defined _echo @echo on

if not defined VSINSTALLDIR (
    echo %__MsgPrefix%Error: VSINSTALLDIR variable not defined.
    exit /b 1
)
if not exist "%VSINSTALLDIR%DIA SDK" goto NoDIA

:GenVSSolution

pushd "%__NativeTestIntermediatesDir%"
call "%__SourceDir%\pal\tools\gen-buildsys-win.bat" ""%__ProjectFilesDir%"" %__VSVersion% %__BuildArch%
@if defined _echo @echo on
popd

if not exist "%__NativeTestIntermediatesDir%\install.vcxproj" (
    echo %__MsgPrefix%Failed to generate test native component build project!
    exit /b 1
)

set __msbuildNativeArgs=-configuration=%__BuildType%

if defined __ToolsetDir (
    set __msbuildNativeArgs=%__msbuildNativeArgs% -UseEnv
) else (
    set __msbuildNativeArgs=%__msbuildNativeArgs% -platform=%__BuildArch%
)

set __BuildLogRootName=Tests_Native
set __BuildLog=%__LogsDir%\%__BuildLogRootName%_%__BuildOS%__%__BuildArch%__%__BuildType%.log
set __BuildWrn=%__LogsDir%\%__BuildLogRootName%_%__BuildOS%__%__BuildArch%__%__BuildType%.wrn
set __BuildErr=%__LogsDir%\%__BuildLogRootName%_%__BuildOS%__%__BuildArch%__%__BuildType%.err
set __msbuildLog=/flp:Verbosity=normal;LogFile="%__BuildLog%"
set __msbuildWrn=/flp1:WarningsOnly;LogFile="%__BuildWrn%"
set __msbuildErr=/flp2:ErrorsOnly;LogFile="%__BuildErr%"

call "%__ProjectDir%\run.cmd" build -Project="%__NativeTestIntermediatesDir%\install.vcxproj" -MsBuildLog=!__msbuildLog! -MsBuildWrn=!__msbuildWrn! -MsBuildErr=!__msbuildErr! %__msbuildNativeArgs% %__RunArgs% %__unprocessedBuildArgs%
if errorlevel 1 (
    echo %__MsgPrefix%Error: build failed. Refer to the build log files for details:
    echo     %__BuildLog%
    echo     %__BuildWrn%
    echo     %__BuildErr%
    exit /b 1
)

:skipnative

set __BuildLogRootName=Restore_Product
set __BuildLog=%__LogsDir%\%__BuildLogRootName%_%__BuildOS%__%__BuildArch%__%__BuildType%.log
set __BuildWrn=%__LogsDir%\%__BuildLogRootName%_%__BuildOS%__%__BuildArch%__%__BuildType%.wrn
set __BuildErr=%__LogsDir%\%__BuildLogRootName%_%__BuildOS%__%__BuildArch%__%__BuildType%.err
set __msbuildLog=/flp:Verbosity=normal;LogFile="%__BuildLog%"
set __msbuildWrn=/flp1:WarningsOnly;LogFile="%__BuildWrn%"
set __msbuildErr=/flp2:ErrorsOnly;LogFile="%__BuildErr%"

set "__TestWorkingDir=%__RootBinDir%\tests\%__BuildOS%.%__BuildArch%.%__BuildType%"

if not defined __BuildAgainstPackages goto SkipRestoreProduct
REM =========================================================================================
REM ===
REM === Restore product binaries from packages
REM ===
REM =========================================================================================

if not defined XunitTestBinBase       set  XunitTestBinBase=%__TestWorkingDir%
set "CORE_ROOT=%XunitTestBinBase%\Tests\Core_Root"
set "CORE_OVERLAY=%XunitTestBinBase%\Tests\Core_Root_%__RuntimeId%"

call "%__ProjectDir%\run.cmd" build -Project=%__ProjectDir%\tests\build.proj  -UpdateDependencies -BatchRestorePackages -MsBuildLog=!__msbuildLog! -MsBuildWrn=!__msbuildWrn! -MsBuildErr=!__msbuildErr! %__RunArgs% %__BuildAgainstPackagesArg% %__unprocessedBuildArgs%

set __BuildLogRootName=Tests_GenerateRuntimeLayout

call "%__ProjectDir%\run.cmd" build -Project=%__ProjectDir%\tests\runtest.proj -BinPlaceRef -BinPlaceProduct -CopyCrossgenToProduct -MsBuildLog=!__msbuildLog! -MsBuildWrn=!__msbuildWrn! -MsBuildErr=!__msbuildErr! %__RunArgs% %__BuildAgainstPackagesArg% %__unprocessedBuildArgs%
if errorlevel 1 (
    echo BinPlace of mscorlib.dll failed
    exit /b 1
)

if defined __RuntimeId (

    if not exist %__PackagesDir%\TestNativeBins (
        echo %__MsgPrefix%Error: Ensure you have run sync.cmd -ab before building a non-Windows test overlay against packages
        exit /b 1
    )

    call "%__ProjectDir%\run.cmd" build -Project=%__ProjectDir%\tests\runtest.proj -CreateNonWindowsTestOverlay -RuntimeId="%__RuntimeId%"  -MsBuildLog=!__msbuildLog! -MsBuildWrn=!__msbuildWrn! -MsBuildErr=!__msbuildErr! %__RunArgs% %__BuildAgainstPackagesArg% %__unprocessedBuildArgs%
    for /R %__PackagesDir%\TestNativeBins\%__RuntimeId% %%f in (*.so) do copy %%f %Core_Overlay%
    for /R %__PackagesDir%\TestNativeBins\%__RuntimeId% %%f in (*.dylib) do copy %%f %Core_Overlay%

    echo %__MsgPrefix% Created the runtime layout for %__RuntimeId% in %CORE_OVERLAY%
)

echo %__MsgPrefix% Restored CoreCLR product from packages

:SkipRestoreProduct

if defined __SkipManaged exit /b 0

set __BuildLogRootName=Tests_Managed
set __BuildLog=%__LogsDir%\%__BuildLogRootName%_%__BuildOS%__%__BuildArch%__%__BuildType%.log
set __BuildWrn=%__LogsDir%\%__BuildLogRootName%_%__BuildOS%__%__BuildArch%__%__BuildType%.wrn
set __BuildErr=%__LogsDir%\%__BuildLogRootName%_%__BuildOS%__%__BuildArch%__%__BuildType%.err
set __msbuildLog=/flp:Verbosity=normal;LogFile="%__BuildLog%"
set __msbuildWrn=/flp1:WarningsOnly;LogFile="%__BuildWrn%"
set __msbuildErr=/flp2:ErrorsOnly;LogFile="%__BuildErr%"

REM =========================================================================================
REM ===
REM === Managed test build section
REM ===
REM =========================================================================================

echo %__MsgPrefix%Starting the Managed Tests Build

if not defined VSINSTALLDIR (
    echo %__MsgPrefix%Error: buildtest.cmd should be run from a Visual Studio Command Prompt.  Please see https://github.com/dotnet/coreclr/blob/master/Documentation/project-docs/developer-guide.md for build instructions.
    exit /b 1
)

if defined __UpdateInvalidPackagesArg (
  set __up=-updateinvalidpackageversions
)

call "%__ProjectDir%\run.cmd" build -Project=%__ProjectDir%\tests\build.proj -MsBuildLog=!__msbuildLog! -MsBuildWrn=!__msbuildWrn! -MsBuildErr=!__msbuildErr! %__up% %__RunArgs% %__unprocessedBuildArgs%
if errorlevel 1 (
    echo %__MsgPrefix%Error: build failed. Refer to the build log files for details:
    echo     %__BuildLog%
    echo     %__BuildWrn%
    echo     %__BuildErr%
    exit /b 1
)

REM Prepare the Test Drop
REM Cleans any NI from the last run
powershell "Get-ChildItem -path %__TestWorkingDir% -Include '*.ni.*' -Recurse -Force | Remove-Item -force"
REM Cleans up any lock folder used for synchronization from last run
powershell "Get-ChildItem -path %__TestWorkingDir% -Include 'lock' -Recurse -Force |  where {$_.Attributes -eq 'Directory'}| Remove-Item -force -Recurse"

set CORE_ROOT=%__TestBinDir%\Tests\Core_Root
if exist "%CORE_ROOT%" rd /s /q "%CORE_ROOT%"
md "%CORE_ROOT%"
xcopy /s "%__BinDir%" "%CORE_ROOT%"

echo %__MsgPrefix%Creating test wrappers...

set RuntimeIdArg=

if defined __RuntimeId (
    set RuntimeIdArg=-RuntimeID="%__RuntimeId%"
)

set __BuildLogRootName=Tests_XunitWrapper
set __BuildLog=%__LogsDir%\%__BuildLogRootName%_%__BuildOS%__%__BuildArch%__%__BuildType%.log
set __BuildWrn=%__LogsDir%\%__BuildLogRootName%_%__BuildOS%__%__BuildArch%__%__BuildType%.wrn
set __BuildErr=%__LogsDir%\%__BuildLogRootName%_%__BuildOS%__%__BuildArch%__%__BuildType%.err
set __msbuildLog=/flp:Verbosity=diag;LogFile="%__BuildLog%"
set __msbuildWrn=/flp1:WarningsOnly;LogFile="%__BuildWrn%"
set __msbuildErr=/flp2:ErrorsOnly;LogFile="%__BuildErr%"

call %__ProjectDir%\run.cmd build -Project=%__ProjectDir%\tests\runtest.proj -BuildWrappers -MsBuildEventLogging=" " -MsBuildLog=!__msbuildLog! -MsBuildWrn=!__msbuildWrn! -MsBuildErr=!__msbuildErr! %__RunArgs% %__BuildAgainstPackagesArg% %__unprocessedBuildArgs% %RuntimeIdArg%
if errorlevel 1 (
    echo Xunit Wrapper build failed
    exit /b 1
)

echo %__MsgPrefix%Creating test overlay...

set __BuildLogRootName=Tests_Overlay_Managed
set __BuildLog=%__LogsDir%\%__BuildLogRootName%_%__BuildOS%__%__BuildArch%__%__BuildType%.log
set __BuildWrn=%__LogsDir%\%__BuildLogRootName%_%__BuildOS%__%__BuildArch%__%__BuildType%.wrn
set __BuildErr=%__LogsDir%\%__BuildLogRootName%_%__BuildOS%__%__BuildArch%__%__BuildType%.err
set __msbuildLog=/flp:Verbosity=normal;LogFile="%__BuildLog%"
set __msbuildWrn=/flp1:WarningsOnly;LogFile="%__BuildWrn%"
set __msbuildErr=/flp2:ErrorsOnly;LogFile="%__BuildErr%"

call %__ProjectDir%\run.cmd build -Project=%__ProjectDir%\tests\runtest.proj -testOverlay -MsBuildLog=!__msbuildLog! -MsBuildWrn=!__msbuildWrn! -MsBuildErr=!__msbuildErr! %__RunArgs% %__unprocessedBuildArgs%
if errorlevel 1 (
    echo %__MsgPrefix%Error: build failed. Refer to the build log files for details:
    echo     %__BuildLog%
    echo     %__BuildWrn%
    echo     %__BuildErr%
    exit /b 1
)

if not defined __BuildAgainstPackages goto SkipPrepForPublish

set __BuildLogRootName=Helix_Prep
set __BuildLog=%__LogsDir%\%__BuildLogRootName%_%__BuildOS%__%__BuildArch%__%__BuildType%.log
set __BuildWrn=%__LogsDir%\%__BuildLogRootName%_%__BuildOS%__%__BuildArch%__%__BuildType%.wrn
set __BuildErr=%__LogsDir%\%__BuildLogRootName%_%__BuildOS%__%__BuildArch%__%__BuildType%.err
set __msbuildLog=/flp:Verbosity=normal;LogFile="%__BuildLog%"
set __msbuildWrn=/flp1:WarningsOnly;LogFile="%__BuildWrn%"
set __msbuildErr=/flp2:ErrorsOnly;LogFile="%__BuildErr%"

REM =========================================================================================
REM ===
REM === Prep test binaries for Helix publishing
REM ===
REM =========================================================================================

call %__ProjectDir%\run.cmd build -Project=%__ProjectDir%\tests\helixprep.proj  -MsBuildLog=!__msbuildLog! -MsBuildWrn=!__msbuildWrn! -MsBuildErr=!__msbuildErr! %__RunArgs% %__BuildAgainstPackagesArg% %__unprocessedBuildArgs% %RuntimeIdArg%

echo %__MsgPrefix% Prepped test binaries for publishing

:SkipPrepForPublish

REM =========================================================================================
REM ===
REM === All builds complete!
REM ===
REM =========================================================================================

echo %__MsgPrefix%Test build successful.
echo %__MsgPrefix%Test binaries are available at !__TestBinDir!
exit /b 0

:Usage
echo.
echo Usage:
echo     %0 [option1] [option2] ...
echo All arguments are optional. Options are case-insensitive. The options are:
echo.
echo. -? -h -help: view this message.
echo Build architecture: -buildArch: only x64 is currently allowed ^(default: x64^).
echo Build type: -buildType: one of Debug, Checked, Release ^(default: Debug^).
echo updateinvalidpackageversions: Runs the target to update package versions.
echo buildagainstpackages: builds tests against restored packages, instead of against a built product.
echo runtimeid ^<ID^>: Builds a test overlay for the specified OS (Only supported when building against packages). Supported IDs are:
echo     alpine.3.4.3-x64: Builds overlay for Alpine 3.4.3
echo     debian.8-x64: Builds overlay for Debian 8
echo     fedora.23-x64: Builds overlay for Fedora 23
echo     fedora.24-x64: Builds overlay for Fedora 23
echo     opensuse.13.2-x64: Builds overlay for OpenSUSE 13.2
echo     opensuse.42.1-x64: Builds overlay for OpenSUSE 42.1
echo     osx.10.10-x64: Builds overlay for OSX 10.10
echo     rhel.7-x64: Builds overlay for RHEL 7 or CentOS
echo     ubuntu.14.04-x64: Builds overlay for Ubuntu 14.04
echo     ubuntu.16.04-x64: Builds overlay for Ubuntu 16.04
echo     ubuntu.16.10-x64: Builds overlay for Ubuntu 16.10
echo Exclude- Optional parameter - specify location of default exclusion file (defaults to tests\issues.targets if not specified)
echo     Set to "" to disable default exclusion file.
echo -- ... : all arguments following this tag will be passed directly to msbuild.
echo -priority=^<N^> : specify a set of test that will be built and run, with priority N.
echo     0: Build only priority 0 cases as essential testcases (default)
echo     1: Build all tests with priority 0 and 1
echo     666: Build all tests with priority 0, 1 ... 666
echo -sequential: force a non-parallel build ^(default is to build in parallel
echo     using all processors^).
echo -ilasmroundtrip: enables ilasm round trip build and run of the tests before executing them.
echo -verbose: enables detailed file logging for the msbuild tasks into the msbuild log file.
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


:PrivateToolSet

echo %__MsgPrefix% Setting Up the usage of __ToolsetDir:%__ToolsetDir%

if /i "%__ToolsetDir%" == "" (
    echo %__MsgPrefix%Error: A toolset directory is required for the Arm64 Windows build. Use the toolset_dir argument.
    exit /b 1
)

set PATH=%__ToolsetDir%\VC_sdk\bin;%PATH%
set LIB=%__ToolsetDir%\VC_sdk\lib\arm64;%__ToolsetDir%\sdpublic\sdk\lib\arm64
set INCLUDE=^
%__ToolsetDir%\VC_sdk\inc;^
%__ToolsetDir%\sdpublic\sdk\inc;^
%__ToolsetDir%\sdpublic\shared\inc;^
%__ToolsetDir%\sdpublic\shared\inc\minwin;^
%__ToolsetDir%\sdpublic\sdk\inc\ucrt;^
%__ToolsetDir%\sdpublic\sdk\inc\minwin;^
%__ToolsetDir%\sdpublic\sdk\inc\mincore;^
%__ToolsetDir%\sdpublic\sdk\inc\abi;^
%__ToolsetDir%\sdpublic\sdk\inc\clientcore;^
%__ToolsetDir%\diasdk\include
exit /b 0
