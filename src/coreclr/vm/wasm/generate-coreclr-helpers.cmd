@echo off
setlocal enabledelayedexpansion

:: Default configuration
set "configuration=Release"
set "browser_scan_path_override="
set "wasi_scan_path_override="
set "target_os="

:: Get the repo root (script is in src/coreclr/vm/wasm).
:: This must be computed before argument parsing, because SHIFT also shifts %0.
set "script_dir=%~dp0"
for %%I in ("%~dp0..\..\..\..") do set "repo_root=%%~fI"

set "usage=Usage: %~nx0 [options]"
set "usage=!usage!^

^

Options:^

  -c, --configuration ^<Checked^|Debug^|Release^>  Build configuration (default: Release)^

  -s, --scan-path ^<path^>                       Override the default browser scan path^

  -w, --wasi-scan-path ^<path^>                  Override the default wasi scan path^

  -t, --target-os ^<browser^|wasi^>               Regenerate only this flavor (default: both)^

  -h, --help                                   Show this help message"

:parse_args
if "%~1"=="" goto :done_args
if /i "%~1"=="-c" goto :set_configuration
if /i "%~1"=="--configuration" goto :set_configuration
if /i "%~1"=="-s" goto :set_scan_path
if /i "%~1"=="--scan-path" goto :set_scan_path
if /i "%~1"=="-w" goto :set_wasi_scan_path
if /i "%~1"=="--wasi-scan-path" goto :set_wasi_scan_path
if /i "%~1"=="-t" goto :set_target_os
if /i "%~1"=="--target-os" goto :set_target_os
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
set browser_scan_path_override=%~2
shift
shift
goto :parse_args

:set_wasi_scan_path
set wasi_scan_path_override=%~2
shift
shift
goto :parse_args

:set_target_os
set target_os=%~2
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

:: Validate target OS to prevent injection
if not "%target_os%"=="" if /i not "%target_os%"=="browser" if /i not "%target_os%"=="wasi" (
    echo Error: Invalid target OS "%target_os%". Must be browser or wasi.
    exit /b 1
)

set "target_os_prop="
if not "%target_os%"=="" set "target_os_prop=-p:CallHelperTargetOS=%target_os%"

echo Configuration: %configuration%
echo Repo root: %repo_root%

cd /d "%repo_root%"

:: The scan paths, the crossgen2 lookup and the P/Invoke module list all live in the project next
:: to this script, so they are not restated here and in the .sh.
set "generator_proj=%script_dir%generate-coreclr-helpers.proj"

:: Each override goes in as one quoted argument so that a path containing spaces survives. A
:: trailing backslash would escape the closing quote, so it is dropped here; the project puts the
:: separator back.
set "browser_prop="
if not "%browser_scan_path_override%"=="" (
    if "%browser_scan_path_override:~-1%."=="\." set "browser_scan_path_override=%browser_scan_path_override:~0,-1%"
    set browser_prop="-p:BrowserScanPath=!browser_scan_path_override!"
)

set "wasi_prop="
if not "%wasi_scan_path_override%"=="" (
    if "%wasi_scan_path_override:~-1%."=="\." set "wasi_scan_path_override=%wasi_scan_path_override:~0,-1%"
    set wasi_prop="-p:WasiScanPath=!wasi_scan_path_override!"
)

call .\dotnet.cmd build "%generator_proj%" -t:GenerateCallHelpers "-p:Configuration=%configuration%" %browser_prop% %wasi_prop% %target_os_prop%
if errorlevel 1 exit /b 1

echo Done!
exit /b 0
