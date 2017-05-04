@if "%_echo%" neq "on" echo off
rem
rem This file invokes cmake and generates the build system for windows.

set argC=0
for %%x in (%*) do Set /A argC+=1

if NOT %argC%==5 GOTO :USAGE
if %1=="/?" GOTO :USAGE

setlocal
set __sourceDir=%~dp0..
:: VS 2015 is the minimum supported toolset
set __VSString=14 2015

:: Set the target architecture to a format cmake understands. ANYCPU defaults to x64
if /i "%3" == "x86"     (set cm_BaseRid=win7-x86&&set cm_Arch=I386&&set __VSString=%__VSString%)
if /i "%3" == "x64"     (set cm_BaseRid=win7-x64&&set cm_Arch=AMD64&&set __VSString=%__VSString% Win64)
if /i "%3" == "arm"     (set cm_BaseRid=win8-arm&&set cm_Arch=ARM&&set __VSString=%__VSString% ARM)
if /i "%3" == "arm64"   (set cm_BaseRid=win10-arm64&&set cm_Arch=ARM64&&set __VSString=%__VSString% Win64)

set __HostVersion=%4
set __LatestCommit=%5

if defined CMakePath goto DoGen

:: Eval the output from probe-win1.ps1
pushd "%__sourceDir%"
for /f "delims=" %%a in ('powershell -NoProfile -ExecutionPolicy ByPass "& .\Windows\probe-win.ps1"') do %%a
popd

:DoGen
echo "%CMakePath%" %__sourceDir% %__SDKVersion% "-DCLI_CMAKE_RUNTIME_ID:STRING=%cm_BaseRid%" "-DCLI_CMAKE_HOST_VER:STRING=%__HostVersion%" "-DCLI_CMAKE_APPHOST_VER:STRING=%__HostVersion%" "-DCLI_CMAKE_HOST_FXR_VER:STRING=%__HostVersion%" "-DCLI_CMAKE_HOST_POLICY_VER:STRING=%__HostVersion%" "-DCLI_CMAKE_PKG_RID:STRING=%cm_BaseRid%" "-DCLI_CMAKE_COMMIT_HASH:STRING=%__LatestCommit%" "-DCLI_CMAKE_PLATFORM_ARCH_%cm_Arch%=1" "-DCMAKE_INSTALL_PREFIX=%__CMakeBinDir%" "-DCLI_CMAKE_RESOURCE_DIR:STRING=%__ResourcesDir%" -G "Visual Studio %__VSString%"
"%CMakePath%" %__sourceDir% %__SDKVersion% "-DCLI_CMAKE_RUNTIME_ID:STRING=%cm_BaseRid%" "-DCLI_CMAKE_HOST_VER:STRING=%__HostVersion%" "-DCLI_CMAKE_APPHOST_VER:STRING=%__HostVersion%" "-DCLI_CMAKE_HOST_FXR_VER:STRING=%__HostVersion%" "-DCLI_CMAKE_HOST_POLICY_VER:STRING=%__HostVersion%" "-DCLI_CMAKE_PKG_RID:STRING=%cm_BaseRid%" "-DCLI_CMAKE_COMMIT_HASH:STRING=%__LatestCommit%" "-DCLI_CMAKE_PLATFORM_ARCH_%cm_Arch%=1" "-DCMAKE_INSTALL_PREFIX=%__CMakeBinDir%" "-DCLI_CMAKE_RESOURCE_DIR:STRING=%__ResourcesDir%" -G "Visual Studio %__VSString%"
endlocal
GOTO :DONE

:USAGE
  echo "Usage..."
  echo "gen-buildsys-win.bat <path to top level CMakeLists.txt> <VSVersion> <Target Architecture>"
  echo "Specify the path to the top level CMake file - <ProjectK>/src/NDP"
  echo "Specify the VSVersion to be used - VS2013 or VS2015"
  echo "Specify the Target Architecture - x86, AnyCPU, ARM, or x64."
  echo "Specify the host version"
  echo "Specify latest commit hash"
  EXIT /B 1

:DONE
  EXIT /B 0