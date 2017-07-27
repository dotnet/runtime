@if "%_echo%" neq "on" echo off
rem
rem This file invokes cmake and generates the build system for windows.

set argC=0
for %%x in (%*) do Set /A argC+=1

if NOT %argC%==4 GOTO :USAGE
if %1=="/?" GOTO :USAGE

echo %1
echo %2
echo %3
echo %4
echo %5

setlocal
set __sourceDir=%1
:: VS 2015 is the minimum supported toolset
set __VSString=14 2015

:: Set the target architecture to a format cmake understands. ANYCPU defaults to x64
set cm_BaseRid=win10-%2
if /i "%2" == "x86"     (set cm_Arch=I386&&set __VSString=%__VSString%)
if /i "%2" == "x64"     (set cm_Arch=AMD64&&set __VSString=%__VSString% Win64)
if /i "%2" == "arm"     (set cm_Arch=ARM&&set __VSString=%__VSString% ARM)

set __ResourcesDir=%3
set __OutputDir=%4

echo "Computed RID for native build is %cm_BaseRid%"

if defined CMakePath goto DoGen

:: Eval the output from probe-win1.ps1
pushd "%__sourceDir%"
for /f "delims=" %%a in ('powershell -NoProfile -ExecutionPolicy ByPass "& %~dp0\probe-win.ps1"') do %%a
popd

:DoGen
echo "%CMakePath%" %__sourceDir% %__SDKVersion% %__CMAKE_SYSTEM% %__CMAKE_CUSTOM_DEFINES% "-DCLI_CMAKE_PKG_RID:STRING=%cm_BaseRid%" "-DCLI_CMAKE_PLATFORM_ARCH_%cm_Arch%=1" "-DCMAKE_INSTALL_PREFIX=%__OutputDir%" "-DCLI_CMAKE_RESOURCE_DIR:STRING=%__ResourcesDir%" -G "Visual Studio %__VSString%"

"%CMakePath%" %__sourceDir% %__SDKVersion% %__CMAKE_SYSTEM% %__CMAKE_CUSTOM_DEFINES% "-DCLI_CMAKE_PKG_RID:STRING=%cm_BaseRid%" "-DCLI_CMAKE_PLATFORM_ARCH_%cm_Arch%=1" "-DCMAKE_INSTALL_PREFIX=%__OutputDir%" "-DCLI_CMAKE_RESOURCE_DIR:STRING=%__ResourcesDir%" -G "Visual Studio %__VSString%"
endlocal
GOTO :DONE

:USAGE
  echo "Usage..."
  echo "gen-buildsys-win.bat <path to top level CMakeLists.txt> <Target Architecture> <Commit Hash> <NativeResourceDir> <OutputDir>"
  EXIT /B 1

:DONE
  EXIT /B 0