@echo off
setlocal enabledelayedexpansion

:: Default configuration
set "configuration=Debug"
set "browser_scan_path_override="
set "wasi_scan_path_override="

:: Get the repo root (script is in src/coreclr/vm/wasm).
:: This must be computed before argument parsing, because SHIFT also shifts %0.
for %%I in ("%~dp0..\..\..\..") do set "repo_root=%%~fI"

set "usage=Usage: %~nx0 [options]"
set "usage=!usage!^

^

Options:^

  -c, --configuration ^<Checked^|Debug^|Release^>  Build configuration (default: Debug)^

  -s, --scan-path ^<path^>                       Override the default browser scan path^

  -w, --wasi-scan-path ^<path^>                  Override the default wasi scan path^

  -h, --help                                   Show this help message"

:parse_args
if "%~1"=="" goto :done_args
if /i "%~1"=="-c" goto :set_configuration
if /i "%~1"=="--configuration" goto :set_configuration
if /i "%~1"=="-s" goto :set_scan_path
if /i "%~1"=="--scan-path" goto :set_scan_path
if /i "%~1"=="-w" goto :set_wasi_scan_path
if /i "%~1"=="--wasi-scan-path" goto :set_wasi_scan_path
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

:show_help
echo !usage!
exit /b 0

:done_args

:: Validate configuration to prevent injection
if /i not "%configuration%"=="Debug" if /i not "%configuration%"=="Release" if /i not "%configuration%"=="Checked" (
    echo Error: Invalid configuration "%configuration%". Must be Debug, Release, or Checked.
    exit /b 1
)

echo Configuration: %configuration%
echo Repo root: %repo_root%

cd /d "%repo_root%"

:: The generator lives in crossgen2 and uses its type system to compute the wasm ABI. Generation
:: does not load the JIT, so the host-targeting crossgen2 answers wasm questions correctly. Its
:: configuration has to match the one the scanned assemblies came from.
set crossgen2=%repo_root%\artifacts\bin\coreclr\windows.x64.%configuration%\crossgen2\crossgen2.dll

:: Modules the runtime links statically; a P/Invoke into any of them resolves to a direct call.
:: Read from the list the runtime tests' corerun relink imports, so the two cannot be edited apart.
set "pinvoke_modules_file=%repo_root%\eng\wasm\WasmPInvokeModules.props"
if not exist "%pinvoke_modules_file%" (
    echo Error: P/Invoke module list not found at: %pinvoke_modules_file%
    exit /b 1
)

set "pinvoke_module_args="
for /f tokens^=2^ delims^=^" %%M in ('findstr /c:"WasmCoreClrFrameworkPInvokeModule Include=" "%pinvoke_modules_file%"') do (
    set "pinvoke_module_args=!pinvoke_module_args! --directpinvoke %%M"
)

if not defined pinvoke_module_args (
    echo Error: no WasmCoreClrFrameworkPInvokeModule entries found in: %pinvoke_modules_file%
    exit /b 1
)

:: Resolve scan paths (allow overrides).
if not "%browser_scan_path_override%"=="" (
    set browser_scan_path=%browser_scan_path_override%
) else (
    set browser_scan_path=%repo_root%\artifacts\bin\testhost\net11.0-browser-%configuration%-wasm\shared\Microsoft.NETCore.App\11.0.0\
)

if not "%wasi_scan_path_override%"=="" (
    set wasi_scan_path=%wasi_scan_path_override%
) else (
    set wasi_scan_path=%repo_root%\artifacts\bin\testhost\net11.0-wasi-%configuration%-wasm\shared\Microsoft.NETCore.App\11.0.0\
)

call :run_generator browser "%browser_scan_path%" "%repo_root%\src\coreclr\vm\wasm\browser\"
if errorlevel 1 exit /b 1

call :run_generator wasi "%wasi_scan_path%" "%repo_root%\src\coreclr\vm\wasm\wasi\"
if errorlevel 1 exit /b 1

echo Done!
exit /b 0

:: run_generator <target_os> <scan_path> <output_dir>
:run_generator
set "target_os=%~1"
set "scan_path=%~2"
set "output_dir=%~3"

if not exist "%scan_path%" (
    echo Error: Scan path for %target_os% does not exist: %scan_path%
    echo Please build the runtime first using: .\build.cmd clr+libs -os %target_os% -c %configuration%
    exit /b 1
)

if not exist "%crossgen2%" (
    echo Error: crossgen2 was not found at: %crossgen2%
    echo Please build the clr subset first using: .\build.cmd clr -c %configuration%
    exit /b 1
)

echo [%target_os%] Scan path: %scan_path%
echo [%target_os%] Output path: %output_dir%
echo Running generator for %target_os%...
call .\dotnet.cmd "%crossgen2%" --targetos %target_os% --targetarch wasm --generate-portable-callhelpers "%output_dir%" %pinvoke_module_args% "%scan_path%*.dll"

if errorlevel 1 (
    echo Generator failed for %target_os%!
    exit /b 1
)
exit /b 0
