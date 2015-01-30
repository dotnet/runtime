rem
rem This file invokes cmake and generates the build system for windows.

echo off

set argC=0
for %%x in (%*) do Set /A argC+=1

if NOT %argC%==1 GOTO :USAGE
if %1=="/?" GOTO :USAGE

cmake -DCMAKE_USER_MAKE_RULES_OVERRIDE=%1\src\pal\tools\windows-compiler-override.txt -G "Visual Studio 12 2013 Win64" %1
GOTO :DONE

:USAGE
  echo "Usage..."
  echo "gen-buildsys-win.bat <path to top level CMakeLists.txt>"
  echo "Specify the path to the top level CMake file - <ProjectK>/src/NDP"
  EXIT /B 1

:DONE
  EXIT /B 0






