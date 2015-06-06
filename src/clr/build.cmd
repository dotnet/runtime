@echo off
setlocal EnableDelayedExpansion

:: Set the default arguments for build
set __BuildArch=x64
set __BuildType=Debug
set __BuildOS=Windows_NT

:: Default to VS2013
set __VSVersion=VS2013
set __VSProductVersion=120

:: Set the various build properties here so that CMake and MSBuild can pick them up
set "__ProjectDir=%~dp0"
:: remove trailing slash
if %__ProjectDir:~-1%==\ set "__ProjectDir=%__ProjectDir:~0,-1%"
set "__ProjectFilesDir=%__ProjectDir%"
set "__SourceDir=%__ProjectDir%\src"
set "__PackagesDir=%__ProjectDir%\packages"
set "__RootBinDir=%__ProjectDir%\bin"
set "__LogsDir=%__RootBinDir%\Logs"
set __MSBCleanBuildArgs=

:Arg_Loop
if "%1" == "" goto ArgsDone
if /i "%1" == "/?" goto Usage
if /i "%1" == "x64"    (set __BuildArch=x64&&shift&goto Arg_Loop)

if /i "%1" == "debug"    (set __BuildType=Debug&shift&goto Arg_Loop)
if /i "%1" == "release"   (set __BuildType=Release&shift&goto Arg_Loop)

if /i "%1" == "clean"   (set __CleanBuild=1&shift&goto Arg_Loop)

if /i "%1" == "linuxmscorlib" (set __MscorlibOnly=1&set __BuildOS=Linux&shift&goto Arg_Loop)
if /i "%1" == "osxmscorlib" (set __MscorlibOnly=1&set __BuildOS=OSX&shift&goto Arg_Loop)

if /i "%1" == "vs2013" (set __VSVersion=%1&set __VSProductVersion=120&shift&goto Arg_Loop)
if /i "%1" == "vs2015" (set __VSVersion=%1&set __VSProductVersion=140&shift&goto Arg_Loop)

echo Invalid commandline argument: %1
goto Usage

:ArgsDone

echo Commencing CoreCLR Repo build
echo.

:: Set the remaining variables based upon the determined build configuration
set "__BinDir=%__RootBinDir%\Product\%__BuildOS%.%__BuildArch%.%__BuildType%"
set "__IntermediatesDir=%__RootBinDir%\obj\%__BuildOS%.%__BuildArch%.%__BuildType%"
set "__PackagesBinDir=%__BinDir%\.nuget"
set "__ToolsDir=%__RootBinDir%\tools"
set "__TestBinDir=%__RootBinDir%\tests\%__BuildOS%.%__BuildArch%.%__BuildType%"
set "__TestIntermediatesDir=%__RootBinDir%\tests\obj\%__BuildOS%.%__BuildArch%.%__BuildType%"

:: Generate path to be set for CMAKE_INSTALL_PREFIX to contain forward slash
set "__CMakeBinDir=%__BinDir%"
set "__CMakeBinDir=%__CMakeBinDir:\=/%"

:: Configure environment if we are doing a clean build.
if not defined __CleanBuild goto MakeDirs
echo Doing a clean build
echo.

:: MSBuild projects would need a rebuild
set __MSBCleanBuildArgs=/t:rebuild

:: Cleanup the previous output for the selected configuration
if exist "%__BinDir%" rd /s /q "%__BinDir%"
if exist "%__IntermediatesDir%" rd /s /q "%__IntermediatesDir%"

if exist "%__TestBinDir%" rd /s /q "%__TestBinDir%"
if exist "%__TestIntermediatesDir%" rd /s /q "%__TestIntermediatesDir%"

if exist "%__LogsDir%" del /f /q "%__LogsDir%\*_%__BuildOS%__%__BuildArch%__%__BuildType%.*"

:MakeDirs
if not exist "%__BinDir%" md "%__BinDir%"
if not exist "%__IntermediatesDir%" md "%__IntermediatesDir%"
if not exist "%__LogsDir%" md "%__LogsDir%"

:: CMake isn't a requirement when building mscorlib only
if defined __MscorlibOnly goto CheckVS

:CheckPrereqs
:: Check prerequisites
echo Checking pre-requisites...
echo.
:: Eval the output from probe-win1.ps1
for /f "delims=" %%a in ('powershell -NoProfile -ExecutionPolicy RemoteSigned "& ""%__SourceDir%\pal\tools\probe-win.ps1"""') do %%a

goto CheckVS

:CheckVS
:: Check presence of VS
if defined VS%__VSProductVersion%COMNTOOLS goto CheckVSExistence
echo Visual Studio 2013 Community (free) is a pre-requisite to build this repository.
echo See: https://github.com/dotnet/coreclr/blob/master/Documentation/developer-guide.md#prerequisites
exit /b 1

:CheckVSExistence
:: Does VS 2013 or VS 2015 really exist?
if exist "!VS%__VSProductVersion%COMNTOOLS!\..\IDE\devenv.exe" goto CheckMSBuild
echo Visual Studio 2013 Community (free) is a pre-requisite to build this repository.
echo See: https://github.com/dotnet/coreclr/blob/master/Documentation/developer-guide.md#prerequisites
exit /b 1

:CheckMSBuild
:: Note: We've disabled node reuse because it causes file locking issues.
::       The issue is that we extend the build with our own targets which
::       means that that rebuilding cannot successfully delete the task
::       assembly. 
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
if not exist %_msbuildexe% echo Error: Could not find MSBuild.exe.  Please see https://github.com/dotnet/coreclr/blob/master/developer-guide.md for build instructions. && exit /b 1

:: All set to commence the build

setlocal
if defined __MscorlibOnly goto PerformMScorlibBuild

echo Commencing build of native components for %__BuildOS%.%__BuildArch%.%__BuildType%
echo.

:: Set the environment for the native build
call "!VS%__VSProductVersion%COMNTOOLS!\..\..\VC\vcvarsall.bat" x86_amd64

if exist "%VSINSTALLDIR%DIA SDK" goto GenVSSolution
echo Error: DIA SDK is missing at "%VSINSTALLDIR%DIA SDK". ^
This is due to a bug in the Visual Studio installer. It does not install DIA SDK at "%VSINSTALLDIR%" but rather ^
at VS install location of previous version. Workaround is to copy DIA SDK folder from VS install location ^
of previous version to "%VSINSTALLDIR%" and then resume build.
:: DIA SDK not included in Express editions
echo Visual Studio 2013 Express does not include the DIA SDK. ^
You need Visual Studio 2013 Community (free).
echo See: https://github.com/dotnet/coreclr/blob/master/Documentation/developer-guide.md#prerequisites
exit /b 1

:GenVSSolution
:: Regenerate the VS solution
pushd "%__IntermediatesDir%"
call "%__SourceDir%\pal\tools\gen-buildsys-win.bat" "%__ProjectDir%" %__VSVersion% %__BuildType%
popd

:BuildComponents
if exist "%__IntermediatesDir%\install.vcxproj" goto BuildCoreCLR
echo Failed to generate native component build project!
exit /b 1

REM Build CoreCLR
:BuildCoreCLR
set "__CoreCLRBuildLog=%__LogsDir%\CoreCLR_%__BuildOS%__%__BuildArch%__%__BuildType%.log"
%_msbuildexe% "%__IntermediatesDir%\install.vcxproj" %__MSBCleanBuildArgs% /nologo /maxcpucount /nodeReuse:false /p:Configuration=%__BuildType% /p:Platform=%__BuildArch% /fileloggerparameters:Verbosity=diag;LogFile="%__CoreCLRBuildLog%"
IF NOT ERRORLEVEL 1 goto PerformMScorlibBuild
echo Native component build failed. Refer !__CoreCLRBuildLog! for details.
exit /b 1

:PerformMScorlibBuild
REM endlocal to rid us of environment changes from vcvarsall.bat
endlocal

REM setlocal to prepare for vsdevcmd.bat
setlocal
set __AdditionalMSBuildArgs=

rem Explicitly set Platform causes conflicts in mscorlib project files. Clear it to allow building from VS x64 Native Tools Command Prompt
set Platform=

if defined __MscorlibOnly set __AdditionalMSBuildArgs=/p:BuildNugetPackage=false

:: Set the environment for the managed build
call "!VS%__VSProductVersion%COMNTOOLS!\VsDevCmd.bat"
echo Commencing build of mscorlib for %__BuildOS%.%__BuildArch%.%__BuildType%
echo.
set "__MScorlibBuildLog=%__LogsDir%\MScorlib_%__BuildOS%__%__BuildArch%__%__BuildType%.log"
%_msbuildexe% "%__ProjectFilesDir%\build.proj" %__MSBCleanBuildArgs% /nologo /maxcpucount /verbosity:minimal /nodeReuse:false /fileloggerparameters:Verbosity=diag;LogFile="%__MScorlibBuildLog%" /p:OS=%__BuildOS% %__AdditionalMSBuildArgs%
IF NOT ERRORLEVEL 1 (
  if defined __MscorlibOnly exit /b 0
  goto CrossGenMscorlib
)

echo MScorlib build failed. Refer !__MScorlibBuildLog! for details.
exit /b 1

:CrossGenMscorlib
echo Generating native image of mscorlib for %__BuildOS%.%__BuildArch%.%__BuildType%
echo.
set "__CrossGenMScorlibLog=%__LogsDir%\CrossgenMScorlib_%__BuildOS%__%__BuildArch%__%__BuildType%.log"
%__BinDir%\crossgen.exe %__BinDir%\mscorlib.dll > "%__CrossGenMScorlibLog%" 2>&1
IF NOT ERRORLEVEL 1 (
  goto PerformTestBuild
)

echo CrossGen mscorlib failed. Refer !__CrossGenMScorlibLog! for details.
exit /b 1

:PerformTestBuild
echo.
echo Commencing build of tests for %__BuildOS%.%__BuildArch%.%__BuildType%
echo.
call tests\buildtest.cmd
IF NOT ERRORLEVEL 1 goto SuccessfulBuild
echo Test binaries build failed. Refer !__MScorlibBuildLog! for details.
exit /b 1

:SuccessfulBuild
::Build complete
echo Repo successfully built.
echo.
echo Product binaries are available at !__BinDir!
echo Test binaries are available at !__TestBinDir!
exit /b 0

:Usage
echo.
echo Usage:
echo %0 BuildArch BuildType [clean] [vsversion] where:
echo.
echo BuildArch can be: x64
echo BuildType can be: Debug, Release
echo Clean - optional argument to force a clean build.
echo VSVersion - optional argument to use VS2013 or VS2015 (default VS2013)
echo linuxmscorlib - Build mscorlib for Linux
echo osxmscorlib - Build mscorlib for OS X
exit /b 1
