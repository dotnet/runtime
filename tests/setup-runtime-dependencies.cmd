@if not defined __echo @echo off
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

REM =========================================================================================
REM ===
REM === Check if dotnet CLI and necessary directories exist
REM ===
REM =========================================================================================

set __DotNetToolDir=%__ThisScriptPath%..\Tools
set __DotNetCmd=%__DotNetToolDir%\dotnetcli\dotnet.exe 
set __PackageDir=%__ThisScriptPath%..\Packages
set __TmpDir=%Temp%\coreclr_gcstress_%RANDOM%

REM Check if dotnet cli exists
if not exist "%__DotNetToolDir%" (
    echo Directory containing dotnet CLI does not exist: %__DotNetToolDir%
    goto Fail
)
if not exist "%__DotNetCmd%" (
    echo dotnet.exe does not exist: %__DotNetCmd%
    goto Fail
)

REM Create directories needed
if not exist "%__PackageDir%" md "%__PackageDir%"
if not exist "%__OutputDir%" md "%__OutputDir%"

REM Check and create a temp directory
if exist "%__TmpDir%" (
    rmdir /S /Q %__TmpDir%
)
mkdir %__TmpDir%

REM Project.json path
set __JsonFilePath=%__TmpDir%\project.json

REM =========================================================================================
REM ===
REM === Download packages
REM ===
REM =========================================================================================

REM Write dependency information to project.json
echo { ^
    "dependencies": { ^
    "runtime.win7-%__Arch%.Microsoft.NETCore.CoreDisTools": "1.0.1-prerelease-*" ^
    }, ^
    "frameworks": { "dnxcore50": { } } ^
    } > "%__JsonFilePath%"

echo JSON file: %__JsonFilePath%
type "%__JsonFilePath%"

REM Download the package
echo Downloading CoreDisTools package
set DOTNETCMD="%__DotNetCmd%" restore "%__JsonFilePath%" --source https://dotnet.myget.org/F/dotnet-core/ --packages "%__PackageDir%"
echo %DOTNETCMD%
call %DOTNETCMD%
if errorlevel 1 goto Fail

REM Get downloaded dll path
echo Locating coredistools.dll
FOR /F "delims=" %%i IN ('dir %__PackageDir%\coredistools.dll /b/s ^| findstr /R "runtime.win[0-9]*-%__Arch%"') DO set __LibPath=%%i
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

REM Delete temporary files
if exist "%__TmpDir%" (
    rmdir /S /Q "%__TmpDir%"
)

exit /b 0

:Fail
if exist "%__TmpDir%" (
    rmdir /S /Q "%__TmpDir%"
)
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
