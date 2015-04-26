@echo off
rem
rem This file invokes cmake and generates the build system for windows.

set argC=0
for %%x in (%*) do Set /A argC+=1

if NOT %argC%==2 GOTO :USAGE
if %1=="/?" GOTO :USAGE

setlocal
set basePath=%~dp0
:: remove quotes
set "basePath=%basePath:"=%"
:: remove trailing slash
if %basePath:~-1%==\ set "basePath=%basePath:~0,-1%"

set __VSString=12 2013
if /i "%2" == "vs2015" (set __VSString=14 2015)

if defined CMakePath goto DoGen

:: Eval the output from probe-win1.ps1
for /f "delims=" %%a in ('powershell -NoProfile -ExecutionPolicy RemoteSigned "& .\probe-win.ps1"') do %%a

:DoGen
"%CMakePath%" "-DCMAKE_USER_MAKE_RULES_OVERRIDE=%basePath%\windows-compiler-override.txt" -G "Visual Studio %__VSString% Win64" %1
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






