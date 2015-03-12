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
if not defined __ProductSourceDir set "__ProductSourceDir=%__ProjectFilesDir%..\src"

set "__TestManagedBuildLog=%__LogsDir%\Tests_Managed_%__BuildOS%__%__BuildArch%__%__BuildType%.log"
set "__TestNativeBuildLog=%__LogsDir%\Tests_Native_%__BuildOS%__%__BuildArch%__%__BuildType%.log"
set "__XunitWrapperBuildLog=%__LogsDir%\Tests_XunitWrapper_%__BuildOS%__%__BuildArch%__%__BuildType%.log"
set "__CMakeTestSlnDir=%__TestWorkingDir%\CMake\"

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
echo Checking pre-requisites...
echo.
:: Eval the output from probe-win1.ps1
for /f "delims=" %%a in ('powershell -NoProfile -ExecutionPolicy RemoteSigned "& ""%__ProductSourceDir%\pal\tools\probe-win.ps1"""') do %%a

:: Check presence of VS
if defined VS120COMNTOOLS goto CheckVSExistence
echo Installation of VS 2013 is a pre-requisite to build this repository.
goto :eof

:CheckVSExistence
:: Does VS 2013 really exist?
if exist "%VS120COMNTOOLS%\..\IDE\devenv.exe" goto CheckMSBuild
echo Installation of VS 2013 is a pre-requisite to build this repository.
goto :eof

:CheckMSBuild    
set _msbuildexe="%ProgramFiles(x86)%\MSBuild\12.0\Bin\MSBuild.exe"
if not exist %_msbuildexe% set _msbuildexe="%ProgramFiles%\MSBuild\12.0\Bin\MSBuild.exe"
if not exist %_msbuildexe% set _msbuildexe="%ProgramFiles(x86)%\MSBuild\14.0\Bin\MSBuild.exe"
if not exist %_msbuildexe% set _msbuildexe="%ProgramFiles%\MSBuild\14.0\Bin\MSBuild.exe"
if not exist %_msbuildexe% echo Error: Could not find MSBuild.exe.  Please see https://github.com/dotnet/coreclr/wiki/Developer%%20Guide for build instructions. && exit /b 1

::Building Native part of  Tests
setlocal

echo Commencing build of native test components for %__BuildArch%/%__BuildType%
echo.

:: Set the environment for the native build
call "%VS120COMNTOOLS%..\..\VC\vcvarsall.bat" x86_amd64

if exist "%VSINSTALLDIR%DIA SDK" goto GenVSSolution
echo Error: DIA SDK is missing at "%VSINSTALLDIR%DIA SDK". ^
This is due to bug in VS Intaller. It does not install DIA SDK at "%VSINSTALLDIR%" but rather ^
at VS install location of previous version. Workaround is to copy DIA SDK folder from VS install location ^
of previous version to "%VSINSTALLDIR%" and then resume build.
goto :eof

:GenVSSolution
:: Regenerate the VS solution
if not exist "%__CMakeTestSlnDir%" md "%__CMakeTestSlnDir%"
pushd "%__CMakeTestSlnDir%"
call "%__ProductSourceDir%\pal\tools\gen-buildsys-win.bat" "%__ProjectFilesDir%\"
popd

:BuildComponents
if exist "%__CMakeTestSlnDir%\install.vcxproj" goto BuildTestNativeComponents
echo Failed to generate test native component build project!
goto :eof

REM Build CoreCLR
:BuildTestNativeComponents
%_msbuildexe% "%__CMakeTestSlnDir%\install.vcxproj" %__MSBCleanBuildArgs% /nologo /maxcpucount /nodeReuse:false /p:Configuration=%__BuildType% /p:Platform=%__BuildArch% /fileloggerparameters:Verbosity=diag;LogFile="%__TestNativeBuildLog%"
IF NOT ERRORLEVEL 1 goto PerformManagedTestBuild
echo Native component build failed. Refer !__TestNativeBuildLog! for details.
goto :eof

:PerformManagedTestBuild
REM endlocal to rid us of environment changes from vcvarsall.bat
endlocal
::End Building native tests

::Building Managed Tests
REM setlocal to prepare for vsdevcmd.bat
setlocal
:: Set the environment for the managed build- Vs cmd prompt
call "%VS120COMNTOOLS%\VsDevCmd.bat"
if not defined VSINSTALLDIR echo Error: build.cmd should be run from a Visual Studio Command Prompt.  Please see https://github.com/dotnet/coreclr/wiki/Developer%%20Guide for build instructions. && exit /b 1


:BuildTests

echo Starting the Manged Tests Build
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

goto :eof

:build

%_buildprefix% %_msbuildexe% "%__ProjectFilesDir%build.proj" %__MSBCleanBuildArgs% /nologo /maxcpucount /verbosity:minimal /nodeReuse:false /fileloggerparameters:Verbosity=diag;LogFile="%__TestManagedBuildLog%";Append %* %_buildpostfix%
IF ERRORLEVEL 1 echo Test build failed. Refer !__TestManagedBuildLog! for details && exit /b 1

endlocal



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
