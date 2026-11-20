@if not defined _echo @echo off

if "%~1" == "" goto :Usage
if "%~2" == "" goto :Usage

set "__CMakeToolchainArch=%~1"
set "__CMakeToolchainOS=%~2"
set "__CMakeToolchainRepoRoot=%~dp0..\.."
for %%i in ("%__CMakeToolchainRepoRoot%") do set "__CMakeToolchainRepoRoot=%%~fi"

set "CMAKE_CONFIGURE_COMMAND_WRAPPER="
set "CMAKE_INITIAL_CACHE="
set "CMAKE_TOOLCHAIN_FILE="
set "CLR_CMAKE_TARGET_OS="

if /i not "%__CMakeToolchainArch%" == "wasm" goto :ConfigureLauncher
if /i "%__CMakeToolchainOS%" == "browser" goto :ConfigureEmscripten
if /i "%__CMakeToolchainOS%" == "wasi" goto :ConfigureWasi

echo Error: target OS must be browser or wasi when the target architecture is wasm.
exit /b 1

:ConfigureEmscripten
if not "%EMSDK_PATH%" == "" goto :InitializeEmscripten

call "%__CMakeToolchainRepoRoot%\eng\wasm\wasm-tool-cache.cmd" emscripten "%__CMakeToolchainRepoRoot%\src\mono\browser\emscripten-version.txt" "%__CMakeToolchainRepoRoot%"
if "%WASM_TOOL_CACHE_RESULT%" == "" (
    echo Error: You need to set the EMSDK_PATH environment variable pointing to the emscripten SDK root.
    exit /b 1
)
set "EMSDK_PATH=%WASM_TOOL_CACHE_RESULT%"

:InitializeEmscripten
set "EMSDK_QUIET=1"
call "%EMSDK_PATH%\emsdk_env.cmd"
if errorlevel 1 exit /b 1
set "CMAKE_CONFIGURE_COMMAND_WRAPPER=emcmake"
set "CMAKE_INITIAL_CACHE=%__CMakeToolchainRepoRoot%\eng\native\tryrun.browser.cmake"
goto :ConfigureLauncher

:ConfigureWasi
if not "%WASI_SDK_PATH%" == "" goto :SetWasiToolchain

call "%__CMakeToolchainRepoRoot%\eng\wasm\wasm-tool-cache.cmd" wasi-sdk "%__CMakeToolchainRepoRoot%\eng\wasm\wasi-sdk-version.txt" "%__CMakeToolchainRepoRoot%"
if "%WASM_TOOL_CACHE_RESULT%" == "" (
    echo Error: You need to set the WASI_SDK_PATH environment variable pointing to the WASI SDK root.
    exit /b 1
)
set "WASI_SDK_PATH=%WASM_TOOL_CACHE_RESULT%"

:SetWasiToolchain
set "CMAKE_TOOLCHAIN_FILE=%WASI_SDK_PATH%\share\cmake\wasi-sdk-p2.cmake"
set "CLR_CMAKE_TARGET_OS=wasi"

:ConfigureLauncher
if /i not "%USE_SCCACHE%" == "true" goto :Success
set "CMAKE_C_COMPILER_LAUNCHER=sccache"
if /i "%__CMakeToolchainOS%" == "osx" set "CMAKE_C_COMPILER_LAUNCHER=%__CMakeToolchainRepoRoot%\eng\native\sccache-xarch-wrapper.sh"
if /i "%__CMakeToolchainOS%" == "maccatalyst" set "CMAKE_C_COMPILER_LAUNCHER=%__CMakeToolchainRepoRoot%\eng\native\sccache-xarch-wrapper.sh"
set "CMAKE_CXX_COMPILER_LAUNCHER=%CMAKE_C_COMPILER_LAUNCHER%"

:Success
set "__CMakeToolchainArch="
set "__CMakeToolchainOS="
set "__CMakeToolchainRepoRoot="
exit /b 0

:Usage
echo Usage: init-cmake-toolchain.cmd ^<architecture^> ^<target OS^>
exit /b 1
