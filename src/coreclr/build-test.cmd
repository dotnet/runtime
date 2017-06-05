@if not defined _echo @echo off
setlocal EnableDelayedExpansion

:: Set the default arguments for build
set __BuildArch=x64
set __VCBuildArch=x86_amd64
set __BuildType=Debug
set __BuildOS=Windows_NT

:: Default to highest Visual Studio version available
::
:: For VS2015 (and prior), only a single instance is allowed to be installed on a box
:: and VS140COMNTOOLS is set as a global environment variable by the installer. This
:: allows users to locate where the instance of VS2015 is installed.
::
:: For VS2017, multiple instances can be installed on the same box SxS and VS150COMNTOOLS
:: is no longer set as a global environment variable and is instead only set if the user
:: has launched the VS2017 Developer Command Prompt.
::
:: Following this logic, we will default to the VS2017 toolset if VS150COMNTOOLS tools is
:: set, as this indicates the user is running from the VS2017 Developer Command Prompt and
:: is already configured to use that toolset. Otherwise, we will fallback to using the VS2015
:: toolset if it is installed. Finally, we will fail the script if no supported VS instance
:: can be found.
if defined VS150COMNTOOLS (
  set "__VSToolsRoot=%VS150COMNTOOLS%"
  set "__VCToolsRoot=%VS150COMNTOOLS%\..\..\VC\Auxiliary\Build"
  set __VSVersion=vs2017
) else (
  set "__VSToolsRoot=%VS140COMNTOOLS%"
  set "__VCToolsRoot=%VS140COMNTOOLS%\..\..\VC"
  set __VSVersion=vs2015
)

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
set __BuildAgainstPackagesArg=
set __RuntimeId=
set __ZipTests=
set __TargetsWindows=1
set __DoCrossgen=

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
if /i "%1" == "toolset_dir"           (set __ToolsetDir=%2&set __PassThroughArgs=%__PassThroughArgs% %2&set processedArgs=!processedArgs! %1 %2&shift&shift&goto Arg_Loop)
if /i "%1" == "buildagainstpackages"  (set __ZipTests=1&set __BuildAgainstPackagesArg=-BuildTestsAgainstPackages&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "ziptests"              (set __ZipTests=1&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "crossgen"              (set __DoCrossgen=1&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "runtimeid"             (set __RuntimeId=%2&set processedArgs=!processedArgs! %1 %2&shift&shift&goto Arg_Loop)
if /i "%1" == "targetsNonWindows"     (set __TargetsWindows=0&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "Exclude"               (set __Exclude=%2&set processedArgs=!processedArgs! %1 %2&shift&shift&goto Arg_Loop)

if [!processedArgs!]==[] (
  call set __UnprocessedBuildArgs=!__args!
) else (
  call set __UnprocessedBuildArgs=%%__args:*!processedArgs!=%%
)

:ArgsDone

if defined __BuildAgainstPackagesArg (
    if not defined __RuntimeID (
        echo %__MsgPrefix%Error: When building against packages, you must supply a target Runtime ID.
        exit /b 1
    )
)

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

call "%__TestDir%\setup-stress-dependencies.cmd" /arch %__BuildArch% /outputdir %__BinDir%

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
echo %__MsgPrefix%Using environment: "%__VCToolsRoot%\vcvarsall.bat" %__VCBuildArch%
call                                 "%__VCToolsRoot%\vcvarsall.bat" %__VCBuildArch%
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

if not defined __BuildAgainstPackagesArg goto SkipRestoreProduct
REM =========================================================================================
REM ===
REM === Restore product binaries from packages
REM ===
REM =========================================================================================

if not defined XunitTestBinBase       set  XunitTestBinBase=%__TestWorkingDir%
set "CORE_ROOT=%XunitTestBinBase%\Tests\Core_Root"

call "%__ProjectDir%\run.cmd" build -Project=%__ProjectDir%\tests\build.proj -BatchRestorePackages -MsBuildLog=!__msbuildLog! -MsBuildWrn=!__msbuildWrn! -MsBuildErr=!__msbuildErr! %__RunArgs% %__BuildAgainstPackagesArg% %__unprocessedBuildArgs%

set __BuildLogRootName=Tests_GenerateRuntimeLayout

call "%__ProjectDir%\run.cmd" build -Project=%__ProjectDir%\tests\runtest.proj -BinPlaceRef -BinPlaceProduct -CopyCrossgenToProduct -RuntimeId="%__RuntimeId%" -MsBuildLog=!__msbuildLog! -MsBuildWrn=!__msbuildWrn! -MsBuildErr=!__msbuildErr! %__RunArgs% %__BuildAgainstPackagesArg% %__unprocessedBuildArgs%
if errorlevel 1 (
    echo BinPlace of mscorlib.dll failed
    exit /b 1
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

call "%__ProjectDir%\run.cmd" build -Project=%__ProjectDir%\tests\build.proj -MsBuildLog=!__msbuildLog! -MsBuildWrn=!__msbuildWrn! -MsBuildErr=!__msbuildErr! %__RunArgs% %__BuildAgainstPackagesArg% %__unprocessedBuildArgs%
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
set CORE_ROOT_STAGE=%__TestBinDir%\Tests\Core_Root_Stage
if exist "%CORE_ROOT%" rd /s /q "%CORE_ROOT%"
if exist "%CORE_ROOT_STAGE%" rd /s /q "%CORE_ROOT_STAGE%"
md "%CORE_ROOT%"
md "%CORE_ROOT_STAGE%"
xcopy /s "%__BinDir%" "%CORE_ROOT_STAGE%"


if defined __BuildAgainstPackagesArg ( 
  if "%__TargetsWindows%"=="0" (

    if not exist %__PackagesDir%\TestNativeBins (
        echo %__MsgPrefix%Error: Ensure you have run sync.cmd -ab before building a non-Windows test overlay against packages
        exit /b 1
    )

    for /R %__PackagesDir%\TestNativeBins\%__RuntimeId%\%__BuildType% %%f in (*.so) do copy %%f %CORE_ROOT_STAGE%
    for /R %__PackagesDir%\TestNativeBins\%__RuntimeId%\%__BuildType% %%f in (*.dylib) do copy %%f %CORE_ROOT_STAGE%
  )
)

echo %__MsgPrefix%Creating test wrappers...

set RuntimeIdArg=
set TargetsWindowsArg=

if defined __RuntimeId (
    set RuntimeIdArg=-RuntimeID="%__RuntimeId%"
)

if "%__TargetsWindows%"=="1" (
    set TargetsWindowsArg=-TargetsWindows=true
) else if "%__TargetsWindows%"=="0" (
    set TargetsWindowsArg=-TargetsWindows=false
)

set __BuildLogRootName=Tests_XunitWrapper
set __BuildLog=%__LogsDir%\%__BuildLogRootName%_%__BuildOS%__%__BuildArch%__%__BuildType%.log
set __BuildWrn=%__LogsDir%\%__BuildLogRootName%_%__BuildOS%__%__BuildArch%__%__BuildType%.wrn
set __BuildErr=%__LogsDir%\%__BuildLogRootName%_%__BuildOS%__%__BuildArch%__%__BuildType%.err
set __msbuildLog=/flp:Verbosity=normal;LogFile="%__BuildLog%"
set __msbuildWrn=/flp1:WarningsOnly;LogFile="%__BuildWrn%"
set __msbuildErr=/flp2:ErrorsOnly;LogFile="%__BuildErr%"

call %__ProjectDir%\run.cmd build -Project=%__ProjectDir%\tests\runtest.proj -BuildWrappers -MsBuildEventLogging=" " -MsBuildLog=!__msbuildLog! -MsBuildWrn=!__msbuildWrn! -MsBuildErr=!__msbuildErr! %__RunArgs% %__BuildAgainstPackagesArg% %TargetsWindowsArg% %__unprocessedBuildArgs%
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

call %__ProjectDir%\run.cmd build -Project=%__ProjectDir%\tests\runtest.proj -testOverlay -MsBuildLog=!__msbuildLog! -MsBuildWrn=!__msbuildWrn! -MsBuildErr=!__msbuildErr! %__RunArgs% %RuntimeIdArg% %__unprocessedBuildArgs%
if errorlevel 1 (
    echo %__MsgPrefix%Error: build failed. Refer to the build log files for details:
    echo     %__BuildLog%
    echo     %__BuildWrn%
    echo     %__BuildErr%
    exit /b 1
)

xcopy /s /y "%CORE_ROOT_STAGE%" "%CORE_ROOT%"

set __CrossgenArg = ""
if defined __DoCrossgen (
  set __CrossgenArg="-Crossgen"
  if "%__TargetsWindows%" == "1" (
    call :PrecompileFX
  ) else (
    echo "%__MsgPrefix% Crossgen only supported on Windows, for now"
  )
)

rd /s /q "%CORE_ROOT_STAGE%"

if not defined __ZipTests goto SkipPrepForPublish

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

call %__ProjectDir%\run.cmd build -Project=%__ProjectDir%\tests\helixprep.proj  -MsBuildLog=!__msbuildLog! -MsBuildWrn=!__msbuildWrn! -MsBuildErr=!__msbuildErr! %__RunArgs% %__BuildAgainstPackagesArg% %RuntimeIdArg% %TargetsWindowsArg% %__CrossgenArg% %__unprocessedBuildArgs%
if errorlevel 1 (
    echo %__MsgPrefix%Error: build failed. Refer to the build log files for details:
    echo     %__BuildLog%
    echo     %__BuildWrn%
    echo     %__BuildErr%
    exit /b 1
)

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
echo buildagainstpackages: builds tests against restored packages, instead of against a built product.
echo runtimeid ^<ID^>: Builds a test overlay for the specified OS (Only supported when building against packages). Supported IDs are:
echo     alpine.3.4.3-x64: Builds overlay for Alpine 3.4.3
echo     debian.8-x64: Builds overlay for Debian 8
echo     fedora.24-x64: Builds overlay for Fedora 24
echo     linux-x64: Builds overlay for portable linux
echo     opensuse.42.1-x64: Builds overlay for OpenSUSE 42.1
echo     osx.10.12-x64: Builds overlay for OSX 10.12
echo     osx-x64: Builds overlay for portable OSX
echo     rhel.7-x64: Builds overlay for RHEL 7 or CentOS
echo     ubuntu.14.04-x64: Builds overlay for Ubuntu 14.04
echo     ubuntu.16.04-x64: Builds overlay for Ubuntu 16.04
echo     ubuntu.16.10-x64: Builds overlay for Ubuntu 16.10
echo     win-x64: Builds overlay for portable Windows
echo     win7-x64: Builds overlay for Windows 7
echo ziptests: zips CoreCLR tests & Core_Root for a Helix run
echo crossgen: Precompiles the framework managed assemblies
echo Exclude- Optional parameter - specify location of default exclusion file (defaults to tests\issues.targets if not specified)
echo     Set to "" to disable default exclusion file.
echo -- ... : all arguments following this tag will be passed directly to msbuild.
echo -priority=^<N^> : specify a set of test that will be built and run, with priority N.
echo     0: Build only priority 0 cases as essential testcases (default)
echo     1: Build all tests with priority 0 and 1
echo     666: Build all tests with priority 0, 1 ... 666
echo -sequential: force a non-parallel build ^(default is to build in parallel
echo     using all processors^).
echo -verbose: enables detailed file logging for the msbuild tasks into the msbuild log file.
exit /b 1

:NoDIA
echo Error: DIA SDK is missing at "%VSINSTALLDIR%DIA SDK". ^
This is due to a bug in the Visual Studio installer. It does not install DIA SDK at "%VSINSTALLDIR%" but rather ^
at the install location of previous Visual Studio version. The workaround is to copy the DIA SDK folder from the Visual Studio install location ^
of the previous version to "%VSINSTALLDIR%" and then build.
:: DIA SDK not included in Express editions
echo Visual Studio Express does not include the DIA SDK. ^
You need Visual Studio 2015 or 2017 (Community is free).
echo See: https://github.com/dotnet/coreclr/blob/master/Documentation/project-docs/developer-guide.md#prerequisites
exit /b 1


:PrivateToolSet

echo %__MsgPrefix% Setting Up the usage of __ToolsetDir:%__ToolsetDir%

if /i "%__ToolsetDir%" == "" (
    echo %__MsgPrefix%Error: A toolset directory is required for the Arm64 Windows build. Use the toolset_dir argument.
    exit /b 1
)

if not exist "%__ToolsetDir%"\buildenv_arm64.cmd goto :Not_EWDK
call "%__ToolsetDir%"\buildenv_arm64.cmd
exit /b 0

:Not_EWDK
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

:PrecompileFX
for %%F in (%CORE_ROOT%\*.dll) do call :PrecompileAssembly "%%F" %%~nF%%~xF
exit /b 0

REM Compile the managed assemblies in Core_ROOT before running the tests
:PrecompileAssembly

REM Skip mscorlib since it is already precompiled.
if /I "%2" == "mscorlib.dll" exit /b 0
if /I "%2" == "mscorlib.ni.dll" exit /b 0
REM don't precompile anything from CoreCLR
if /I exist %CORE_ROOT_STAGE%\%2 exit /b 0

"%CORE_ROOT_STAGE%\crossgen.exe" /Platform_Assemblies_Paths "%CORE_ROOT%" /in "%1" /out "%CORE_ROOT%/temp.ni.dll" >nul 2>nul
set /a __exitCode = %errorlevel%
if "%__exitCode%" == "-2146230517" (
    echo %2 is not a managed assembly.
    exit /b 0
)

if %__exitCode% neq 0 (
    echo Unable to precompile %2
    exit /b 0
)

:: Delete original .dll & replace it with the Crossgened .dll
del %1
ren "%CORE_ROOT%\temp.ni.dll" %2
    
echo Successfully precompiled %2
exit /b 0
