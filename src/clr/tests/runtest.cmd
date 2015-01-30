@echo off
setlocal
set __ProjectFilesDir=%~dp0
:Arg_Loop
if "%1" == "" goto ArgsDone
if /i "%1" == "x64"    (set __BuildArch=x64&set __MSBuildBuildArch=x64&shift&goto Arg_Loop)

if /i "%1" == "debug"    (set __BuildType=debug&shift&goto Arg_Loop)
if /i "%1" == "release"   (set __BuildType=release&shift&goto Arg_Loop)
if /i "%1" == "SkipWrapperGeneration" (set __SkipWrapperGeneration=yes&shift&goto Arg_Loop)

if /i "%1" == "/?"      (goto Usage)

set Core_Root=%1

:ArgsDone
:: Check prerequisites

:: Check presence of VS
if defined VS120COMNTOOLS goto CheckMSbuild
echo Installation of VS 2013 is a pre-requisite to build this repository.
goto :eof

:CheckMSBuild    
set _msbuildexe="%ProgramFiles(x86)%\MSBuild\12.0\Bin\MSBuild.exe"
if not exist %_msbuildexe% set _msbuildexe="%ProgramFiles%\MSBuild\12.0\Bin\MSBuild.exe"
if not exist %_msbuildexe% set _msbuildexe="%ProgramFiles(x86)%\MSBuild\14.0\Bin\MSBuild.exe"
if not exist %_msbuildexe% set _msbuildexe="%ProgramFiles%\MSBuild\14.0\Bin\MSBuild.exe"
if not exist %_msbuildexe% echo Error: Could not find MSBuild.exe.  Please see https://github.com/dotnet/corefx/wiki/Developer%%20Guide for build instructions. && exit /b 1

:: Set the environment for the  build- Vs cmd prompt
call "%VS120COMNTOOLS%\VsDevCmd.bat"

if not defined VSINSTALLDIR echo Error: runtest.cmd should be run from a Visual Studio Command Prompt.  Please see https://github.com/dotnet/coreclr/wiki/Developer%%20Guide for build instructions. && exit /b 1


if not defined __BuildArch set __BuildArch=x64
if not defined __BuildType set __BuildType=debug
if not defined __BinDir    set  __BinDir=%__ProjectFilesDir%..\binaries\Product\%__BuildArch%\%__BuildType%
if not defined __TestWorkingDir set __TestWorkingDir=%__ProjectFilesDir%..\binaries\tests\%__BuildArch%\%__BuildType%
if not defined __LogsDir        set  __LogsDir=%CD%

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


:SkipDefaultCoreRootSetup
set __XunitWrapperBuildLog=%__LogsDir%\Tests_XunitWrapper_%__BuildArch%__%__BuildType%.log
set __TestRunBuildLog=%__LogsDir%\TestRunResults_%__BuildArch%__%__BuildType%.log
set __TestRunHtmlLog=%__LogsDir%\\TestRun_%__BuildArch%__%__BuildType%.html

echo "Core_Root to be used will be %Core_Root%"
echo "Starting The Test Run .. "
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

goto :eof

:PerformXunitWrapperBuild
if  "%__SkipWrapperGeneration%"=="true" goto :preptests

%_buildprefix% %_msbuildexe% "%__ProjectFilesDir%runtest.proj" /p:NoRun=true /nologo /maxcpucount /verbosity:minimal /nodeReuse:false /fileloggerparameters:Verbosity=diag;LogFile="%__XunitWrapperBuildLog%";Append %* %_buildappend%%_buildpostfix%

IF  ERRORLEVEL 1 echo XunitWrapperBuild build failed. Refer %__XunitWrapperBuildLog% for details. && exit /b 1
:preptests
%_buildprefix% if not "%noCore_RootSet%"=="true" goto :runtests %_buildpostfix%
%_buildprefix% set Core_Root=%XunitTestBinBase%\Tests\Core_Root %_buildpostfix%
%_buildprefix% rd /s /q %Core_Root% %_buildpostfix%
%_buildprefix% md %Core_Root% %_buildpostfix%
%_buildprefix% echo d | xcopy /s %__BinDir% %Core_Root% %_buildpostfix%

:runtests
%_buildprefix% %_msbuildexe% "%__ProjectFilesDir%runtest.proj" /p:NoBuild=true /nologo /maxcpucount /verbosity:minimal /nodeReuse:false /fileloggerparameters:Verbosity=diag;LogFile="%__TestRunBuildLog%";Append %* %_buildpostfix%

%_buildprefix% echo "Core_Root that was used is %Core_Root%" %_buildpostfix%
echo "Find details of the run in %__TestRunHtmlLog%

IF  ERRORLEVEL 1 echo Test Run  failed. Refer %__TestRunBuildLog% for details. && exit /b 1

goto :eof

:Usage
echo.
echo Usage:
echo %0 BuildArch BuildType [SkipWrapperGeneration] CORE_ROOT   where:
echo.
echo BuildArch is x64
echo BuildType can be: Debug, Release
echo SkipWrapperGeneration- Optional parameter this will run the same set of tests as the last time it was run
echo CORE_ROOT The path to the runtime  
goto :eof
