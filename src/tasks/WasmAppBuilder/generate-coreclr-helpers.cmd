@echo off
setlocal enabledelayedexpansion

:: Default configuration
set "configuration=Debug"
set "scan_path_override="

set "usage=Usage: %~nx0 [options]"
set "usage=!usage!^

^

Options:^

  -c, --configuration ^<Checked^|Debug^|Release^>  Build configuration (default: Debug)^

  -s, --scan-path ^<path^>                       Override the default scan path^

  -h, --help                                   Show this help message"

:parse_args
if "%~1"=="" goto :done_args
if /i "%~1"=="-c" goto :set_configuration
if /i "%~1"=="--configuration" goto :set_configuration
if /i "%~1"=="-s" goto :set_scan_path
if /i "%~1"=="--scan-path" goto :set_scan_path
if /i "%~1"=="-h" goto :show_help
if /i "%~1"=="--help" goto :show_help

echo Unknown option: %~1
echo !usage!
exit /b 1

:set_configuration
set configuration=%~2
shift
shift
goto :parse_args

:set_scan_path
set scan_path_override=%~2
shift
shift
goto :parse_args

:show_help
echo !usage!
exit /b 0

:done_args

:: Validate configuration to prevent injection
if /i not "%configuration%"=="Debug" if /i not "%configuration%"=="Release" if /i not "%configuration%"=="Checked" (
    echo Error: Invalid configuration "%configuration%". Must be Debug, Release, or Checked.
    exit /b 1
)

:: Get the repo root (script is in src/tasks/WasmAppBuilder)
set script_dir=%~dp0
pushd "%script_dir%..\..\..\"
set repo_root=%CD%
popd

echo Configuration: %configuration%
echo Repo root: %repo_root%

if not "%scan_path_override%"=="" (
    set scan_path=%scan_path_override%
) else (
    set scan_path=%repo_root%\artifacts\bin\testhost\net11.0-browser-%configuration%-wasm\shared\Microsoft.NETCore.App\11.0.0\
)

if not exist "%scan_path%" (
    echo Error: Scan path does not exist: %scan_path%
    echo Please build the runtime first using: .\build.cmd clr+libs -os browser -c %configuration%
    exit /b 1
)

cd /d "%repo_root%"
echo Scan path: %scan_path%

:: Run the generator - invoke directly without building a command string
echo Running generator...
echo .\dotnet.cmd build /t:RunGenerator /p:RuntimeFlavor=CoreCLR /p:GeneratorOutputPath=%repo_root%\src\coreclr\vm\wasm\ /p:AssembliesScanPath=%scan_path% src\tasks\WasmAppBuilder\WasmAppBuilder.csproj
.\dotnet.cmd build /t:RunGenerator /p:RuntimeFlavor=CoreCLR /p:GeneratorOutputPath=%repo_root%\src\coreclr\vm\wasm\ /p:AssembliesScanPath=%scan_path% src\tasks\WasmAppBuilder\WasmAppBuilder.csproj

if errorlevel 1 (
    echo Generator failed!
    exit /b 1
)

echo Done!