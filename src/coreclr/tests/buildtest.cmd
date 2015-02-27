@echo off
setlocal EnableDelayedExpansion

set "__ProjectFilesDir=%~dp0"
:Arg_Loop
if "%1" == "" goto ArgsDone
if /i "%1" == "x64"    (set __BuildArch=x64&shift&goto Arg_Loop)

if /i "%1" == "debug"    (set __BuildType=debug&shift&goto Arg_Loop)
if /i "%1" == "release"   (set __BuildType=release&shift&goto Arg_Loop)

if /i "%1" == "clean"   (set __CleanBuild=1&shift&goto Arg_Loop)

goto Usage


:ArgsDone


if not defined __BuildArch set __BuildArch=x64
if not defined __BuildType set __BuildType=debug
if not defined __BuildOS set __BuildOS=Windows_NT
if not defined __TestWorkingDir set "__TestWorkingDir=%__ProjectFilesDir%\..\binaries\tests\%__BuildOS%.%__BuildArch%.%__BuildType%"

if not defined __LogsDir  set  "__LogsDir=%__ProjectFilesDir%..\binaries\Logs"

set "__TestBuildLog=%__LogsDir%\Tests_%__BuildOS%__%__BuildArch%__%__BuildType%.log"
set "__XunitWrapperBuildLog=%__LogsDir%\Tests_XunitWrapper_%__BuildOS%__%__BuildArch%__%__BuildType%.log"

:: Switch to clean build mode if the binaries output folder does not exist
if not exist "%__TestWorkingDir%" set __CleanBuild=1
if not exist "%__LogsDir%" md "%__LogsDir%"

:: Configure environment if we are doing a clean build.
if not defined __CleanBuild goto CheckPrereqs
echo Doing a clean test build
echo.

:: MSBuild projects would need a rebuild
set __MSBCleanBuildArgs=/t:rebuild

:: Cleanup the binaries drop folder
if exist "%__TestWorkingDir%" rd /s /q "%__TestWorkingDir%"


:: Note: We've disabled node reuse because it causes file locking issues.
::       The issue is that we extend the build with our own targets which
::       means that that rebuilding cannot successully delete the task
::       assembly. 

:: Check prerequisites
:CheckPrereqs
:: Check presence of VS
if defined VS120COMNTOOLS goto CheckMSbuild
echo Installation of VS 2013 is a pre-requisite to build this repository.
goto :eof

:CheckMSBuild    
set _msbuildexe="%ProgramFiles(x86)%\MSBuild\12.0\Bin\MSBuild.exe"
if not exist %_msbuildexe% set _msbuildexe="%ProgramFiles%\MSBuild\12.0\Bin\MSBuild.exe"
if not exist %_msbuildexe% set _msbuildexe="%ProgramFiles(x86)%\MSBuild\14.0\Bin\MSBuild.exe"
if not exist %_msbuildexe% set _msbuildexe="%ProgramFiles%\MSBuild\14.0\Bin\MSBuild.exe"
if not exist %_msbuildexe% echo Error: Could not find MSBuild.exe.  Please see https://github.com/dotnet/coreclr/wiki/Developer%%20Guide for build instructions. && exit /b 1

:: Set the environment for the  build- Vs cmd prompt
call "%VS120COMNTOOLS%\VsDevCmd.bat"
if not defined VSINSTALLDIR echo Error: build.cmd should be run from a Visual Studio Command Prompt.  Please see https://github.com/dotnet/coreclr/wiki/Developer%%20Guide for build instructions. && exit /b 1


:BuildTests

echo Starting the Test Build
echo.
:: Log build command line
set _buildprefix=echo
set _buildpostfix=^> "%__TestBuildLog%"
set _buildappend=^>
call :build %1

:: Build
set _buildprefix=
set _buildpostfix=
set _buildappend=
call :build %1

goto :eof

:build

%_buildprefix% %_msbuildexe% "%__ProjectFilesDir%build.proj" %__MSBCleanBuildArgs% /nologo /maxcpucount /verbosity:minimal /nodeReuse:false /fileloggerparameters:Verbosity=diag;LogFile="%__TestBuildLog%";Append %* %_buildpostfix%
IF ERRORLEVEL 1 echo Test build failed. Refer !__TestBuildLog! for details && exit /b 1




goto :eof
:Usage
echo.
echo Usage:
echo %0 BuildArch BuildType [clean]  where:
echo.
echo BuildArch can be: x64
echo BuildType can be: Debug, Release
echo Clean - optional argument to force a clean build.
goto :eof
