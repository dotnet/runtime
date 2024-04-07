@if not defined _echo @echo off
rem
rem This file invokes cmake and generates the build system for windows.

setlocal

set argC=0
for %%x in (%*) do Set /A argC+=1

if %argC% lss 4 GOTO :USAGE
if %1=="/?" GOTO :USAGE

setlocal enabledelayedexpansion
set "__repoRoot=%~dp0..\.."
:: normalize
for %%i in ("%__repoRoot%") do set "__repoRoot=%%~fi"

set __SourceDir=%1
set __IntermediatesDir=%2
set __VSVersion=%3
set __Arch=%4
set __Os=%5
set __CmakeGenerator=Visual Studio
set __UseEmcmake=0
if /i "%__Ninja%" == "1" (
    set __CmakeGenerator=Ninja
) else (
    if /i NOT "%__Arch%" == "wasm" (
        if /i "%__VSVersion%" == "vs2022" (set __CmakeGenerator=%__CmakeGenerator% 17 2022)

        if /i "%__Arch%" == "x64" (set __ExtraCmakeParams=%__ExtraCmakeParams% -A x64)
        if /i "%__Arch%" == "arm" (set __ExtraCmakeParams=%__ExtraCmakeParams% -A ARM)
        if /i "%__Arch%" == "arm64" (set __ExtraCmakeParams=%__ExtraCmakeParams% -A ARM64)
        if /i "%__Arch%" == "x86" (set __ExtraCmakeParams=%__ExtraCmakeParams% -A Win32)
    ) else (
        set __CmakeGenerator=NMake Makefiles
    )
)

if /i "%__Arch%" == "wasm" (

    if "%__Os%" == "" (
        echo Error: Please add target OS parameter
        exit /B 1
    )
    if /i "%__Os%" == "browser" (
        if "%EMSDK_PATH%" == "" (
            if not exist "%__repoRoot%\src\mono\browser\emsdk" (
                echo Error: Should set EMSDK_PATH environment variable pointing to emsdk root.
                exit /B 1
            )

            set "EMSDK_PATH=%__repoRoot%\src\mono\browser\emsdk"
        )
        :: replace backslash with forward slash and append last slash
        set "EMSDK_PATH=!EMSDK_PATH:\=/!"
        if not "!EMSDK_PATH:~-1!" == "/" set "EMSDK_PATH=!EMSDK_PATH!/"

        set __ExtraCmakeParams=%__ExtraCmakeParams% "-DCMAKE_TOOLCHAIN_FILE=!EMSDK_PATH!/upstream/emscripten/cmake/Modules/Platform/Emscripten.cmake"
        set __UseEmcmake=1
    )
    if /i "%__Os%" == "wasi" (
        if "%WASI_SDK_PATH%" == "" (
            if not exist "%__repoRoot%\src\mono\wasi\wasi-sdk" (
                echo Error: Should set WASI_SDK_PATH environment variable pointing to emsdk root.
                exit /B 1
            )

            set "WASI_SDK_PATH=%__repoRoot%\src\mono\wasi\wasi-sdk"
        )
        :: replace backslash with forward slash and append last slash
        set "WASI_SDK_PATH=!WASI_SDK_PATH:\=/!"
        if not "!WASI_SDK_PATH:~-1!" == "/" set "WASI_SDK_PATH=!WASI_SDK_PATH!/"
        set __CmakeGenerator=Ninja
        set __ExtraCmakeParams=%__ExtraCmakeParams% -DCLR_CMAKE_TARGET_OS=wasi -DCLR_CMAKE_TARGET_ARCH=wasm "-DWASI_SDK_PREFIX=!WASI_SDK_PATH!" "-DCMAKE_TOOLCHAIN_FILE=!WASI_SDK_PATH!/share/cmake/wasi-sdk.cmake" "-DCMAKE_SYSROOT=!WASI_SDK_PATH!/share/wasi-sysroot"
    )
) else (
    set __ExtraCmakeParams=%__ExtraCmakeParams%  "-DCMAKE_SYSTEM_VERSION=10.0"
)

:loop
if [%6] == [] goto end_loop
set __ExtraCmakeParams=%__ExtraCmakeParams% %6
shift
goto loop
:end_loop

set __ExtraCmakeParams="-DCMAKE_INSTALL_PREFIX=%__CMakeBinDir%" "-DCLR_CMAKE_HOST_ARCH=%__Arch%" %__ExtraCmakeParams%

set __CmdLineOptionsUpToDateFile=%__IntermediatesDir%\cmake_cmd_line.txt
set __CMakeCmdLineCache=
if not "%__ConfigureOnly%" == "1" (
    REM MSBuild can't reload from a CMake reconfigure during build correctly, so only do this
    REM command-line up to date check for non-VS generators.
    if "%__CmakeGenerator:Visual Studio=%" == "%__CmakeGenerator%" (
        if exist "%__CmdLineOptionsUpToDateFile%" (
            set /p __CMakeCmdLineCache=<"%__CmdLineOptionsUpToDateFile%"
            REM Strip the extra space from the end of the cached command line
            if "!__ExtraCmakeParams!" == "!__CMakeCmdLineCache:~0,-1!" (
                echo The CMake command line is the same as the last run. Skipping running CMake.
                exit /B 0
            ) else (
                echo The CMake command line differs from the last run. Running CMake again.
            )
        )
    )
)

if /i "%__UseEmcmake%" == "1" (
    call "!EMSDK_PATH!/emsdk_env.bat" > nul 2>&1 && emcmake "%CMakePath%" %__ExtraCmakeParams% --no-warn-unused-cli -G "%__CmakeGenerator%" -B %__IntermediatesDir% -S %__SourceDir%
) else (
    "%CMakePath%" %__ExtraCmakeParams% --no-warn-unused-cli -G "%__CmakeGenerator%" -B %__IntermediatesDir% -S %__SourceDir%
)

if "%errorlevel%" == "0" (
    echo %__ExtraCmakeParams% > %__CmdLineOptionsUpToDateFile%
)

endlocal
exit /B %errorlevel%

:USAGE
  echo "Usage..."
  echo "gen-buildsys.cmd <path to top level CMakeLists.txt> <path to location for intermediate files> <VSVersion> <arch> <os>"
  echo "Specify the path to the top level CMake file - <ProjectK>/src/NDP"
  echo "Specify the VSVersion to be used - VS2017 or VS2019"
  EXIT /B 1
