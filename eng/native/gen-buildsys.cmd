@if not defined _echo @echo off
rem
rem This file invokes cmake and generates the build system for windows.

setlocal

set argC=0
for %%x in (%*) do Set /A argC+=1

if %argC% lss 4 GOTO :USAGE
if %1=="/?" GOTO :USAGE

setlocal
set basePath=%~dp0
:: remove quotes
set "basePath=%basePath:"=%"
:: remove trailing slash
if %basePath:~-1%==\ set "basePath=%basePath:~0,-1%"

set __SourceDir=%1
set __IntermediatesDir=%2
set __VSVersion=%3
set __Arch=%4
set __CmakeGenerator=Visual Studio
set __UseEmcmake=0
if /i "%__Ninja%" == "1" (
    set __CmakeGenerator=Ninja
) else (
    if /i NOT "%__Arch%" == "wasm" (
        if /i "%__VSVersion%" == "vs2019" (set __CmakeGenerator=%__CmakeGenerator% 16 2019)
        if /i "%__VSVersion%" == "vs2017" (set __CmakeGenerator=%__CmakeGenerator% 15 2017)
        
        if /i "%__Arch%" == "x64" (set __ExtraCmakeParams=%__ExtraCmakeParams% -A x64)
        if /i "%__Arch%" == "arm" (set __ExtraCmakeParams=%__ExtraCmakeParams% -A ARM)
        if /i "%__Arch%" == "arm64" (set __ExtraCmakeParams=%__ExtraCmakeParams% -A ARM64)
        if /i "%__Arch%" == "x86" (set __ExtraCmakeParams=%__ExtraCmakeParams% -A Win32)
    ) else (
        set __CmakeGenerator=NMake Makefiles
    )
)

if /i "%__Arch%" == "wasm" (
    if "%EMSDK_PATH%" == "" (
       echo Error: Should set EMSDK_PATH environment variable pointing to emsdk root.
       exit /B 1
    )

    if "%EMSCRIPTEN_ROOT%" == "" (
      set EMSCRIPTEN_ROOT="%EMSDK_PATH/upstream/emscripten%"
    )
    set __ExtraCmakeParams=%__ExtraCmakeParams% "-DEMSCRIPTEN_GENERATE_BITCODE_STATIC_LIBRARIES=1" "-DCMAKE_TOOLCHAIN_FILE=%EMSCRIPTEN%/cmake/Modules/Platform/Emscripten.cmake"
    set __UseEmcmake=1
) else (
    set __ExtraCmakeParams=%__ExtraCmakeParams%  "-DCMAKE_SYSTEM_VERSION=10.0"
)

:loop
if [%5] == [] goto end_loop
set __ExtraCmakeParams=%__ExtraCmakeParams% %5
shift
goto loop
:end_loop

set __ExtraCmakeParams="-DCMAKE_INSTALL_PREFIX=%__CMakeBinDir%" "-DCLR_CMAKE_HOST_ARCH=%__Arch%" %__ExtraCmakeParams%

if /i "%__UseEmcmake%" == "1" (
    emcmake "%CMakePath%" %__ExtraCmakeParams% --no-warn-unused-cli -G "%__CmakeGenerator%" -B %__IntermediatesDir% -S %__SourceDir% 
) else (
    "%CMakePath%" %__ExtraCmakeParams% --no-warn-unused-cli -G "%__CmakeGenerator%" -B %__IntermediatesDir% -S %__SourceDir% 
)
endlocal
exit /B %errorlevel%

:USAGE
  echo "Usage..."
  echo "gen-buildsys.cmd <path to top level CMakeLists.txt> <path to location for intermediate files> <VSVersion> <arch>"
  echo "Specify the path to the top level CMake file - <ProjectK>/src/NDP"
  echo "Specify the VSVersion to be used - VS2017 or VS2019"
  EXIT /B 1
