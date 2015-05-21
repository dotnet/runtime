@echo off
setlocal EnableDelayedExpansion
set __ProjectFilesDir=%~dp0

:: Default to VS2013
set __VSVersion=VS2013
set __VSProductVersion=120

:: Default __Exclude to issues.targets
set __Exclude=%~dp0\issues.targets

:Arg_Loop
if "%1" == "" goto ArgsDone
if /i "%1" == "x64"    (set __BuildArch=x64&set __MSBuildBuildArch=x64&shift&goto Arg_Loop)

if /i "%1" == "debug"    (set __BuildType=debug&shift&goto Arg_Loop)
if /i "%1" == "release"   (set __BuildType=release&shift&goto Arg_Loop)
if /i "%1" == "SkipWrapperGeneration" (set __SkipWrapperGeneration=true&shift&goto Arg_Loop)
if /i "%1" == "Exclude" (set __Exclude=%2&shift&shift&goto Arg_Loop)
if /i "%1" == "TestEnv" (set __TestEnv=%2&shift&shift&goto Arg_Loop)

if /i "%1" == "vs2013"   (set __VSVersion=%1&set __VSProductVersion=120&shift&goto Arg_Loop)
if /i "%1" == "vs2015"   (set __VSVersion=%1&set __VSProductVersion=140&shift&goto Arg_Loop)

if /i "%1" == "/?"      (goto Usage)

set Core_Root=%1
shift 
:ArgsDone
:: Check prerequisites

:: Check presence of VS
if defined VS%__VSProductVersion%COMNTOOLS goto CheckMSbuild
echo Installation of VS 2013 is a pre-requisite to build this repository.
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
if not exist %_msbuildexe% echo Error: Could not find MSBuild.exe.  Please see https://github.com/dotnet/corefx/wiki/Developer%%20Guide for build instructions. && exit /b 1

:: Set the environment for the  build- Vs cmd prompt
call "!VS%__VSProductVersion%COMNTOOLS!\VsDevCmd.bat"

if not defined VSINSTALLDIR echo Error: runtest.cmd should be run from a Visual Studio Command Prompt.  Please see https://github.com/dotnet/coreclr/wiki/Developer%%20Guide for build instructions. && exit /b 1


if not defined __BuildArch set __BuildArch=x64
if not defined __BuildType set __BuildType=debug
if not defined __BuildOS set __BuildOS=Windows_NT
if not defined __BinDir    set  __BinDir=%__ProjectFilesDir%..\bin\Product\%__BuildOS%.%__BuildArch%.%__BuildType%
if not defined __TestWorkingDir set __TestWorkingDir=%__ProjectFilesDir%..\bin\tests\%__BuildOS%.%__BuildArch%.%__BuildType%
if not defined __LogsDir        set  __LogsDir=%__ProjectFilesDir%..\bin\Logs

:: Default global test environmet variables
if not defined XunitTestBinBase       set  XunitTestBinBase=%__TestWorkingDir%\
if not defined XunitTestReportDirBase set  XunitTestReportDirBase=%XunitTestBinBase%\Reports\
if defined Core_Root goto  :CheckTestEnv 

set noCore_RootSet=true
set Core_Root=%__BinDir%

:CheckTestEnv 
::Check if the test Binaries are built
if not exist %XunitTestBinBase% echo Error: Ensure the Test Binaries are built and are present at %XunitTestBinBase%, Run - buildtest.cmd %__BuildArch% %__BuildType% to build the tests first. && exit /b 1
if "%Core_Root%" == ""             echo Error: Ensure you have done a successful build of the Product and Run - runtest BuildArch BuildType {path to product binaries}. && exit /b 1
if not exist %Core_Root%\coreclr.dll echo Error: Ensure you have done a successful build of the Product and %Core_Root% contains runtime binaries. && exit /b 1
if not "%__Exclude%"==""           (if not exist %__Exclude% echo Error: Exclusion .targets file not found && exit /b 1) 
if not "%__TestEnv%"==""           (if not exist %__TestEnv% echo Error: Test Environment script not found && exit /b 1) 
if not exist %__LogsDir%           md  %__LogsDir%

:SkipDefaultCoreRootSetup
set __XunitWrapperBuildLog=%__LogsDir%\Tests_XunitWrapper_%__BuildOS%__%__BuildArch%__%__BuildType%.log
set __TestRunBuildLog=%__LogsDir%\TestRunResults_%__BuildOS%__%__BuildArch%__%__BuildType%.log
set __TestRunHtmlLog=%__LogsDir%\TestRun_%__BuildOS%__%__BuildArch%__%__BuildType%.html
set __TestRunXmlLog=%__LogsDir%\TestRun_%__BuildOS%__%__BuildArch%__%__BuildType%.xml

echo "Core_Root that will be used is : %Core_Root%"
echo "Starting The Test Run .. "
if  "%__SkipWrapperGeneration%"=="true" goto :preptests

:: Log build command line
set _buildprefix=echo
set _buildpostfix=^> "%__TestRunBuildLog%"
set _buildappend=^>
call :PerformXunitWrapperBuild 

:: Build
set _buildprefix=
set _buildpostfix=
set _buildappend=
call :PerformXunitWrapperBuild 
IF ERRORLEVEL 1 (
    echo XunitWrapperBuild build failed. Refer %__XunitWrapperBuildLog% for details.
    exit /b 1
)

:: Log build command line
set _buildprefix=echo
set _buildpostfix=^> "%__TestRunBuildLog%"
set _buildappend=^>
call :runtests

:: Build
set _buildprefix=
set _buildpostfix=
set _buildappend=
if not "%noCore_RootSet%"=="true" goto :runtests 
set Core_Root=%XunitTestBinBase%\Tests\Core_Root
echo "Using Default Core_Root as %Core_Root% " 
echo "Copying Built binaries from  %__BinDir% to %Core_Root%"
if exist %Core_Root% rd /s /q %Core_Root%
md %Core_Root%
xcopy /s %__BinDir% %Core_Root%
call :runtests 
if ERRORLEVEL 1 (
    echo Test Run failed. Refer to the following"
    echo Msbuild log: %__TestRunBuildLog%
    echo Html report: %__TestRunHtmlLog%
    exit /b 1
)

:runtests
%_buildprefix% %_msbuildexe% "%__ProjectFilesDir%runtest.proj" /p:NoBuild=true /nologo /maxcpucount /verbosity:minimal /nodeReuse:false /fileloggerparameters:Verbosity=diag;LogFile="%__TestRunBuildLog%";Append %1 %_buildpostfix% /clp:showcommandline
exit /b %ERRORLEVEL%

:PerformXunitWrapperBuild

%_buildprefix% %_msbuildexe% "%__ProjectFilesDir%runtest.proj" /p:NoRun=true /nologo /maxcpucount /verbosity:minimal /nodeReuse:false /fileloggerparameters:Verbosity=diag;LogFile="%__XunitWrapperBuildLog%";Append %1  %_buildappend%%_buildpostfix%
exit /b %ERRORLEVEL%

:Usage
echo.
echo Usage:
echo %0 BuildArch BuildType [SkipWrapperGeneration] [Exclude EXCLUSION_TARGETS] [TestEnv TEST_ENV_SCRIPT] [vsversion] CORE_ROOT   where:
echo.
echo BuildArch is x64
echo BuildType can be: Debug, Release
echo SkipWrapperGeneration- Optional parameter - this will run the same set of tests as the last time it was run
echo Exclude- Optional parameter - this will exclude individual tests from running, specified by ExcludeList ItemGroup in an .targets file.
echo TestEnv- Optional parameter - this will run a custom script to set custom test envirommnent settings.
echo VSVersion- optional argument to use VS2013 or VS2015  (default VS2013)
echo CORE_ROOT The path to the runtime  
exit /b 1

