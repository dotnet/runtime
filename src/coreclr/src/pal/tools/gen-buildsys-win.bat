@if not defined __echo @echo off
rem
rem This file invokes cmake and generates the build system for windows.

set argC=0
for %%x in (%*) do Set /A argC+=1

if NOT %argC%==3 if NOT %argC%==4 GOTO :USAGE
if %1=="/?" GOTO :USAGE

setlocal
set basePath=%~dp0
:: remove quotes
set "basePath=%basePath:"=%"
:: remove trailing slash
if %basePath:~-1%==\ set "basePath=%basePath:~0,-1%"

set __VSString=12 2013
set __UseVS=1
if /i "%2" == "vs2015" (set __VSString=14 2015)
if /i "%3" == "x64" (set __VSString=%__VSString% Win64)
if /i "%3" == "arm64" (set UseVS=0)

set __BuildJit32=%4

if defined CMakePath goto DoGen

:: Eval the output from probe-win1.ps1
for /f "delims=" %%a in ('powershell -NoProfile -ExecutionPolicy RemoteSigned "& "%basePath%\probe-win.ps1""') do %%a

:DoGen
if "%UseVS%" == "0" (
    "%CMakePath%" "-DCMAKE_USER_MAKE_RULES_OVERRIDE=%basePath%\windows-compiler-override.txt" "-DCMAKE_INSTALL_PREFIX:PATH=$ENV{__CMakeBinDir}" "-DCLR_CMAKE_HOST_ARCH=%3" -G "Visual Studio %__VSString% Win64" %1
) else (
    "%CMakePath%" "-DCMAKE_USER_MAKE_RULES_OVERRIDE=%basePath%\windows-compiler-override.txt" "-DCMAKE_INSTALL_PREFIX:PATH=$ENV{__CMakeBinDir}" "-DCLR_CMAKE_HOST_ARCH=%3" %__BuildJit32% -G "Visual Studio %__VSString%" %1
)
endlocal
GOTO :DONE

:USAGE
  echo "Usage..."
  echo "gen-buildsys-win.bat <path to top level CMakeLists.txt> <VSVersion>"
  echo "Specify the path to the top level CMake file - <ProjectK>/src/NDP"
  echo "Specify the VSVersion to be used - VS2013 or VS2015"
  EXIT /B 1

:DONE
  EXIT /B 0






