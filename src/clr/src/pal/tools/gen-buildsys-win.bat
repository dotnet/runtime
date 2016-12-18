@if not defined __echo @echo off
rem
rem This file invokes cmake and generates the build system for windows.

set argC=0
for %%x in (%*) do Set /A argC+=1

if %argC% lss 3 GOTO :USAGE
if %1=="/?" GOTO :USAGE

setlocal
set basePath=%~dp0
:: remove quotes
set "basePath=%basePath:"=%"
:: remove trailing slash
if %basePath:~-1%==\ set "basePath=%basePath:~0,-1%"

set __SourceDir=%1
set __VSVersion=%2
set __Arch=%3
set __CmakeGenerator=Visual Studio
if /i "%__VSVersion%" == "vs2015" (set __CmakeGenerator=%__CmakeGenerator% 14 2015)
if /i "%__Arch%" == "x64" (set __CmakeGenerator=%__CmakeGenerator% Win64)
if /i "%__Arch%" == "arm64" (set __CmakeGenerator=%__CmakeGenerator% Win64)
if /i "%__Arch%" == "arm" (set __CmakeGenerator=%__CmakeGenerator% ARM)

if /i "%__NMakeMakefiles%" == "1" (set __CmakeGenerator=NMake Makefiles)

:loop
if [%4] == [] goto end_loop
set __ExtraCmakeParams=%__ExtraCmakeParams% %4
shift
goto loop
:end_loop

if defined CMakePath goto DoGen

:: Eval the output from probe-win1.ps1
for /f "delims=" %%a in ('powershell -NoProfile -ExecutionPolicy ByPass "& "%basePath%\probe-win.ps1""') do %%a

:DoGen
"%CMakePath%" "-DCMAKE_USER_MAKE_RULES_OVERRIDE=%basePath%\windows-compiler-override.txt" "-DCMAKE_INSTALL_PREFIX:PATH=$ENV{__CMakeBinDir}" "-DCLR_CMAKE_HOST_ARCH=%__Arch%" %__ExtraCmakeParams% -G "%__CmakeGenerator%" %__SourceDir%
endlocal
GOTO :DONE

:USAGE
  echo "Usage..."
  echo "gen-buildsys-win.bat <path to top level CMakeLists.txt> <VSVersion>"
  echo "Specify the path to the top level CMake file - <ProjectK>/src/NDP"
  echo "Specify the VSVersion to be used - VS2015"
  EXIT /B 1

:DONE
  EXIT /B 0
