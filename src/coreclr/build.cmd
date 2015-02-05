@echo off
setlocal

:: Set the default arguments for build
set __BuildArch=x64
set __BuildType=debug

:: Set the various build properties here so that CMake and MSBuild can pick them up
set __ProjectDir=%~dp0
set __ProjectFilesDir=%~dp0
set __SourceDir=%__ProjectDir%\src
set __PackagesDir=%__SourceDir%\.nuget
set __RootBinDir=%__ProjectDir%\binaries
set __LogsDir=%__RootBinDir%\Logs
set __CMakeSlnDir=%__RootBinDir%\CMake
set __MSBCleanBuildArgs=

:Arg_Loop
if "%1" == "" goto ArgsDone
if /i "%1" == "/?" goto Usage
if /i "%1" == "x64"    (set __BuildArch=x64&&shift&goto Arg_Loop)

if /i "%1" == "debug"    (set __BuildType=debug&shift&goto Arg_Loop)
if /i "%1" == "release"   (set __BuildType=release&shift&goto Arg_Loop)

if /i "%1" == "clean"   (set __CleanBuild=1&shift&goto Arg_Loop)

if /i "%1" == "unixmscorlib" (set __UnixMscorlibOnly=1&shift&goto Arg_Loop)

echo Invalid commandline argument: %1
goto Usage

:ArgsDone

echo Commencing CoreCLR Repo build
echo.

:: Set the remaining variables based upon the determined build configuration
set __BinDir=%__RootBinDir%\Product\%__BuildArch%\%__BuildType%
set __PackagesBinDir=%__BinDir%\.nuget
set __ToolsDir=%__RootBinDir%\tools
set __TestWorkingDir=%__RootBinDir%\tests\%__BuildArch%\%__BuildType%
set __IntermediatesDir=%__RootBinDir%\intermediates\%__BuildArch%\%__BuildType%

:: Generate path to be set for CMAKE_INSTALL_PREFIX to contain forward slash
set __CMakeBinDir=%__BinDir%
set "__CMakeBinDir=%__CMakeBinDir:\=/%"

:: Switch to clean build mode if the binaries output folder does not exist
if not exist %__RootBinDir% set __CleanBuild=1

:: Configure environment if we are doing a clean build.
if not defined __CleanBuild goto CheckPrereqs
echo Doing a clean build
echo.

:: MSBuild projects would need a rebuild
set __MSBCleanBuildArgs=/t:rebuild

:: Cleanup the binaries drop folder
if exist %__BinDir% rd /s /q %__BinDir%
md %__BinDir%

:: Cleanup the CMake folder
if exist %__CMakeSlnDir% rd /s /q %__CMakeSlnDir%
md %__CMakeSlnDir%

:: Cleanup the logs folder
if exist %__LogsDir% rd /s /q %__LogsDir%
md %__LogsDir%

::Cleanup intermediates folder
if exist %__IntermediatesDir% rd /s /q %__IntermediatesDir%

:: Check prerequisites
:CheckPrereqs
echo Checking pre-requisites...
echo.
::
:: Check presence of CMake on the path
for %%X in (cmake.exe) do (set FOUNDCMake=%%~$PATH:X)
if defined FOUNDCMake goto CheckVS
echo CMake is a pre-requisite to build this repository but it was not found on the path. Please install CMake from http://www.cmake.org/download/ and ensure it is on your path.
goto :eof

:CheckVS
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
:: Note: We've disabled node reuse because it causes file locking issues.
::       The issue is that we extend the build with our own targets which
::       means that that rebuilding cannot successfully delete the task
::       assembly. 
set _msbuildexe="%ProgramFiles(x86)%\MSBuild\12.0\Bin\MSBuild.exe"
if not exist %_msbuildexe% set _msbuildexe="%ProgramFiles%\MSBuild\12.0\Bin\MSBuild.exe"
if not exist %_msbuildexe% set _msbuildexe="%ProgramFiles(x86)%\MSBuild\14.0\Bin\MSBuild.exe"
if not exist %_msbuildexe% set _msbuildexe="%ProgramFiles%\MSBuild\14.0\Bin\MSBuild.exe"
if not exist %_msbuildexe% echo Error: Could not find MSBuild.exe.  Please see https://github.com/dotnet/coreclr/wiki/Developer%%20Guide for build instructions. && exit /b 1

:: All set to commence the build

setlocal
if defined __UnixMscorlibOnly goto PerformMScorlibBuild

echo Commencing build of native components for %__BuildArch%/%__BuildType%
echo.

:: Set the environment for the native build
call "%VS120COMNTOOLS%\..\..\VC\vcvarsall.bat" x86_amd64

if exist "%VSINSTALLDIR%DIA SDK" goto GenVSSolution
echo Error: DIA SDK is missing at "%VSINSTALLDIR%DIA SDK". ^
This is due to bug in VS Intaller. It does not install DIA SDK at "%VSINSTALLDIR%" but rather ^
at VS install location of previous version. Workaround is to copy DIA SDK folder from VS install location ^
of previous version to "%VSINSTALLDIR%" and then resume build.
goto :eof

:GenVSSolution
:: Regenerate the VS solution
pushd %__CMakeSlnDir%
call %__SourceDir%\pal\tools\gen-buildsys-win.bat %__ProjectDir%
popd

:BuildComponents
if exist %__CMakeSlnDir%\install.vcxproj goto BuildCoreCLR
echo Failed to generate native component build project!
goto :eof

REM Build CoreCLR
:BuildCoreCLR
set __CoreCLRBuildLog=%__LogsDir%\CoreCLR_%__BuildArch%__%__BuildType%.log
%_msbuildexe% %__CMakeSlnDir%\install.vcxproj %__MSBCleanBuildArgs% /nologo /maxcpucount /nodeReuse:false /p:Configuration=%__BuildType% /p:Platform=%__BuildArch% /fileloggerparameters:Verbosity=diag;LogFile="%__CoreCLRBuildLog%"
IF NOT ERRORLEVEL 1 goto PerformMScorlibBuild
echo Native component build failed. Refer %__CoreCLRBuildLog% for details.
goto :eof

:PerformMScorlibBuild
REM endlocal to rid us of environment changes from vcvarsall.bat
endlocal

REM setlocal to prepare for vsdevcmd.bat
setlocal
set __AdditionalMSBuildArgs=

if defined __UnixMscorlibOnly set __AdditionalMSBuildArgs=/p:OS=Unix /p:BuildNugetPackage=false

:: Set the environment for the managed build
call "%VS120COMNTOOLS%\VsDevCmd.bat"
echo Commencing build of mscorlib for %__BuildArch%/%__BuildType%
echo.
set __MScorlibBuildLog=%__LogsDir%\MScorlib_%__BuildArch%__%__BuildType%.log
%_msbuildexe% "%__ProjectFilesDir%build.proj" %__MSBCleanBuildArgs% /nologo /maxcpucount /verbosity:minimal /nodeReuse:false /fileloggerparameters:Verbosity=diag;LogFile="%__MScorlibBuildLog%" %__AdditionalMSBuildArgs%
IF NOT ERRORLEVEL 1 (
  if defined __UnixMscorlibOnly goto :eof
  goto PerformTestBuild
)

echo MScorlib build failed. Refer %__MScorlibBuildLog% for details.
goto :eof

:PerformTestBuild
echo.
echo Commencing build of tests for %__BuildArch%/%__BuildType%
echo.
call tests\buildtest.cmd
IF NOT ERRORLEVEL 1 goto SuccessfulBuild
echo Test binaries build failed. Refer %__MScorlibBuildLog% for details.
goto :eof

:SuccessfulBuild
::Build complete
echo Repo successfully built.
echo.
echo Product binaries are available at %__BinDir%
echo Test binaries are available at %__TestWorkingDir%
goto :eof

:Usage
echo.
echo Usage:
echo %0 BuildArch BuildType [clean] where:
echo.
echo BuildArch can be: x64
echo BuildType can be: Debug, Release
echo Clean - optional argument to force a clean build.
goto :eof
