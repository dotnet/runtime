@echo off
setlocal EnableDelayedExpansion

set "__ProjectDir=%~dp0..\"
:: remove trailing slash
if %__ProjectDir:~-1%==\ set "__ProjectDir=%__ProjectDir:~0,-1%"
set "__SourceDir=%__ProjectDir%\src"
set "__TestDir=%__ProjectDir%\tests"
set "__ProjectFilesDir=%__TestDir%"
set "__PackagesDir=%__ProjectDir%\packages"
set "__RootBinDir=%__ProjectDir%\bin"
set "__LogsDir=%__RootBinDir%\Logs"

:: Default to highest Visual Studio version available
if not defined __VSVersion (
    set __VSVersion=vs2015

    if defined VS120COMNTOOLS set __VSVersion=vs2013
    if defined VS140COMNTOOLS set __VSVersion=vs2015
)

:Arg_Loop
if "%1" == "" goto ArgsDone
if /i "%1" == "x64"    (set __BuildArch=x64&shift&goto Arg_Loop)

if /i "%1" == "debug"    (set __BuildType=Debug&shift&goto Arg_Loop)
if /i "%1" == "release"   (set __BuildType=Release&shift&goto Arg_Loop)
if /i "%1" == "checked"   (set __BuildType=Checked&shift&goto Arg_Loop)

if /i "%1" == "clean"   (set __CleanBuild=1&shift&goto Arg_Loop)

if /i "%1" == "vs2013"   (set __VSVersion=%1&shift&goto Arg_Loop)
if /i "%1" == "vs2015"   (set __VSVersion=%1&shift&goto Arg_Loop)

if /i "%1" == "crossgen" (set _crossgen=true&shift&goto Arg_Loop)
if /i "%1" == "priority" (set _priorityvalue=%2&shift&shift&goto Arg_Loop)

if /i "%1" == "verbose" (set _verbosity=detailed&shift&goto Arg_Loop)

goto Usage


:ArgsDone

if defined _crossgen echo Building tests with CrossGen enabled.&set _buildParameters=%_buildParameters% /p:CrossGen=true
if defined _priorityvalue echo Building Test Priority %_priorityvalue%&set _buildParameters=%_buildParameters% /p:CLRTestPriorityToBuild=%_priorityvalue%
if defined _verbosity echo Enabling verbose file logging
if not defined _verbosity set _verbosity=normal

if not defined __BuildArch set __BuildArch=x64
if not defined __BuildType set __BuildType=Debug
if not defined __BuildOS set __BuildOS=Windows_NT

set "__TestBinDir=%__RootBinDir%\tests\%__BuildOS%.%__BuildArch%.%__BuildType%"
:: We have different managed and native intermediate dirs because the managed bits will include
:: the configuration information deeper in the intermediates path.
set "__NativeTestIntermediatesDir=%__RootBinDir%\tests\obj\%__BuildOS%.%__BuildArch%.%__BuildType%"
set "__ManagedTestIntermediatesDir=%__RootBinDir%\tests\obj"
set "__TestManagedBuildLog=%__LogsDir%\Tests_Managed_%__BuildOS%__%__BuildArch%__%__BuildType%.log"
set "__TestNativeBuildLog=%__LogsDir%\Tests_Native_%__BuildOS%__%__BuildArch%__%__BuildType%.log"
set "__XunitWrapperBuildLog=%__LogsDir%\Tests_XunitWrapper_%__BuildOS%__%__BuildArch%__%__BuildType%.log"

:: Generate path to be set for CMAKE_INSTALL_PREFIX to contain forward slash
set "__CMakeBinDir=%__TestBinDir%"
set "__CMakeBinDir=%__CMakeBinDir:\=/%"

:: Configure environment if we are doing a clean build.
if not defined __CleanBuild goto MakeDirectories
echo Doing a clean test build
echo.

:: MSBuild projects would need a rebuild
set __MSBCleanBuildArgs=/t:rebuild

:: Cleanup the binaries drop folder for the current configuration
if exist "%__TestBinDir%" rd /s /q "%__TestBinDir%"
if exist "%__NativeTestIntermediatesDir%" rd /s /q "%__NativeTestIntermediatesDir%"

:MakeDirectories
if not exist "%__TestBinDir%" md "%__TestBinDir%"
if not exist "%__NativeTestIntermediatesDir%" md "%__NativeTestIntermediatesDir%"
if not exist "%__ManagedTestIntermediatesDir%" md "%__ManagedTestIntermediatesDir%"
if not exist "%__LogsDir%" md "%__LogsDir%"

:: Note: We've disabled node reuse because it causes file locking issues.
::       The issue is that we extend the build with our own targets which
::       means that that rebuilding cannot successully delete the task
::       assembly. 

:: Check prerequisites
:CheckPrereqs
echo Checking pre-requisites...
echo.
:: Eval the output from probe-win1.ps1
for /f "delims=" %%a in ('powershell -NoProfile -ExecutionPolicy RemoteSigned "& ""%__SourceDir%\pal\tools\probe-win.ps1"""') do %%a

set __VSProductVersion=
if /i "%__VSVersion%" == "vs2013" set __VSProductVersion=120
if /i "%__VSVersion%" == "vs2015" set __VSProductVersion=140

:: Check presence of VS
if defined VS%__VSProductVersion%COMNTOOLS goto CheckMSBuild
echo Visual Studio 2013+ (Community is free) is a pre-requisite to build this repository.
exit /b 1

:CheckMSBuild
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

::Building Native part of Tests
setlocal

echo Commencing build of native test components for %__BuildArch%/%__BuildType%
echo.

:: Set the environment for the native build
call "!VS%__VSProductVersion%COMNTOOLS!..\..\VC\vcvarsall.bat" x86_amd64

if exist "%VSINSTALLDIR%DIA SDK" goto GenVSSolution
echo Error: DIA SDK is missing at "%VSINSTALLDIR%DIA SDK". ^
This is due to bug in VS Intaller. It does not install DIA SDK at "%VSINSTALLDIR%" but rather ^
at VS install location of previous version. Workaround is to copy DIA SDK folder from VS install location ^
of previous version to "%VSINSTALLDIR%" and then resume build.
exit /b 1

:GenVSSolution
:: Regenerate the VS solution
pushd "%__NativeTestIntermediatesDir%"
call "%__SourceDir%\pal\tools\gen-buildsys-win.bat" "%__ProjectFilesDir%\" %__VSVersion% %__BuildArch%
popd

:BuildComponents
if exist "%__NativeTestIntermediatesDir%\install.vcxproj" goto BuildTestNativeComponents
echo Failed to generate test native component build project!
exit /b 1

REM Build CoreCLR
:BuildTestNativeComponents
%_msbuildexe% "%__NativeTestIntermediatesDir%\install.vcxproj" %__MSBCleanBuildArgs% /nologo /maxcpucount /nodeReuse:false /p:Configuration=%__BuildType% /p:Platform=%__BuildArch% /fileloggerparameters:Verbosity=%_verbosity%;LogFile="%__TestNativeBuildLog%"
IF NOT ERRORLEVEL 1 goto PerformManagedTestBuild
echo Native component build failed. Refer !__TestNativeBuildLog! for details.
exit /b 1

:PerformManagedTestBuild
REM endlocal to rid us of environment changes from vcvarsall.bat
endlocal
::End Building native tests

::Building Managed Tests
REM setlocal to prepare for vsdevcmd.bat
setlocal
:: Set the environment for the managed build- Vs cmd prompt
call "!VS%__VSProductVersion%COMNTOOLS!\VsDevCmd.bat"
if not defined VSINSTALLDIR echo Error: build.cmd should be run from a Visual Studio Command Prompt.  Please see https://github.com/dotnet/coreclr/blob/master/Documentation/project-docs/developer-guide.md for build instructions. && exit /b 1

:BuildTests

echo Starting the Managed Tests Build
echo.
:: Log build command line
set _buildprefix=echo
set _buildpostfix=^> "%__TestManagedBuildLog%"
set _buildappend=^>
call :build %1

:: Build
set _buildprefix=
set _buildpostfix=
set _buildappend=
call :build %1
if ERRORLEVEL 1 exit /b 1

set CORE_ROOT=%__TestBinDir%\Tests\Core_Root
echo.
echo Creating test overlay...

:: Log build command line
set _buildprefix=echo
set _buildpostfix=^> "%__TestManagedBuildLog%"
set _buildappend=^>
call :CreateTestOverlay %1

:: Build
set _buildprefix=
set _buildpostfix=
set _buildappend=
call :CreateTestOverlay %1

exit /b %ERRORLEVEL%

:build

%_buildprefix% %_msbuildexe% "%__ProjectFilesDir%\build.proj" %__MSBCleanBuildArgs% /nologo /maxcpucount /verbosity:minimal /nodeReuse:false %_buildParameters% /fileloggerparameters:Verbosity=%_verbosity%;LogFile="%__TestManagedBuildLog%";Append %* %_buildpostfix%
IF ERRORLEVEL 1 echo Test build failed. Refer to !__TestManagedBuildLog! for details && exit /b 1
exit /b 0

:CreateTestOverlay

%_buildprefix% %_msbuildexe% "%__ProjectFilesDir%\runtest.proj" /t:CreateTestOverlay /nologo /maxcpucount /verbosity:minimal /nodeReuse:false /fileloggerparameters:Verbosity=%_verbosity%;LogFile="%__TestManagedBuildLog%";Append %* %_buildpostfix%
IF ERRORLEVEL 1 echo Failed to create the test overlay. Refer to !__TestManagedBuildLog! for details && exit /b 1
exit /b 0

:Usage
echo.
echo Usage:
echo %0 BuildArch BuildType [clean] [vsversion] [crossgen] [priority N] [verbose] where:
echo.
echo BuildArch can be: x64
echo BuildType can be: Debug, Release, Checked
echo Clean - optional argument to force a clean build.
echo VSVersion - optional argument to use VS2013 or VS2015  (default VS2015)
echo CrossGen - Enables the tests to run crossgen on the test executables before executing them. 
echo Priority (N) where N is a number greater than zero that signifies the set of tests that will be built and consequently run.
echo Verbose - Enables detailed file logging for the msbuild tasks.
exit /b 1
