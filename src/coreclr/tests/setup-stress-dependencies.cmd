@if not defined _echo @echo off
setlocal

set __ThisScriptShort=%0
set __ThisScriptFull=%~f0
set __ThisScriptPath=%~dp0

REM =========================================================================================
REM ===
REM === Parse arguments
REM ===
REM =========================================================================================

set __OutputDir=
set __Arch= 

:Arg_Loop
if "%1" == "" goto ArgsDone

if /i "%1" == "/?"    goto Usage
if /i "%1" == "-?"    goto Usage
if /i "%1" == "/h"    goto Usage
if /i "%1" == "-h"    goto Usage
if /i "%1" == "/help" goto Usage
if /i "%1" == "-help" goto Usage

if /i "%1" == "/arch"             (set __Arch=%2&shift&shift&goto Arg_Loop) 
if /i "%1" == "/outputdir"        (set __OutputDir=%2&shift&shift&goto Arg_Loop)

echo Invalid command-line argument: %1
goto Usage

:ArgsDone

if not defined __OutputDir goto Usage
if not defined __Arch goto Usage 

REM Check if the platform is supported
if /i "%__Arch%" == "arm" (
    echo No runtime dependencies for Arm32.
    exit /b 0
)

if /i "%__Arch%" == "arm64" (
    echo No runtime dependencies for Arm64.
    exit /b 0
)

REM =========================================================================================
REM ===
REM === Check if dotnet CLI and necessary directories exist
REM ===
REM =========================================================================================

set __DotNetCmd=%__ThisScriptPath%..\..\..\dotnet.cmd
set __CsprojPath=%__ThisScriptPath%\stress_dependencies\stress_dependencies.csproj

REM Create directories needed
if not exist "%__OutputDir%" md "%__OutputDir%"

REM =========================================================================================
REM ===
REM === Download packages
REM ===
REM =========================================================================================

REM Download the package
echo Downloading CoreDisTools package
set CoreDisToolsPackagePathOutputFile="%__ThisScriptPath%\..\..\..\artifacts\obj\coreclr\Windows_NT.%__Arch%\coredistoolspath.txt"
set DOTNETCMD="%__DotNetCmd%" restore "%__CsprojPath%"
echo %DOTNETCMD%
call %DOTNETCMD%
if errorlevel 1 goto Fail
set DOTNETCMD="%__DotNetCmd%" msbuild "%__CsprojPath%" /t:DumpCoreDisToolsPackagePath /p:CoreDisToolsPackagePathOutputFile="%CoreDisToolsPackagePathOutputFile%" /p:RuntimeIdentifier="win-%__Arch%"
call %DOTNETCMD%
if errorlevel 1 goto Fail

if not exist "%CoreDisToolsPackagePathOutputFile%" (
    echo %__ErrMsgPrefix%Failed to get CoreDisTools package path.
    exit /b 1
)


set /p __PkgDir=<"%CoreDisToolsPackagePathOutputFile%"

REM Get downloaded dll path
echo Locating coredistools.dll
FOR /F "delims=" %%i IN ('dir "%__PkgDir%\coredistools.dll" /b/s ^| findstr /R "win-%__Arch%"') DO set __LibPath=%%i
echo CoreDisTools library path: %__LibPath%
if not exist "%__LibPath%" (
    echo Failed to locate the downloaded library: %__LibPath%
    goto Fail
)

REM Copy library to output directory
echo Copy library: %__LibPath% to %__OutputDir%
copy /y "%__LibPath%" "%__OutputDir%"
if errorlevel 1 (
    echo Failed to copy %__LibPath% to %__OutputDir%
    goto Fail
)

exit /b 0

:Fail
exit /b 1

REM =========================================================================================
REM ===
REM === Helper routines
REM ===
REM =========================================================================================

:Usage
echo.
echo Download coredistools for GC stress testing
echo.
echo Usage:
echo     %__ThisScriptShort% /arch ^<TargetArch^> /outputdir ^<coredistools_lib_install_path^>
echo.
exit /b 1
