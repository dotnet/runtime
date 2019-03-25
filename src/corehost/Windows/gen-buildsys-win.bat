@if "%_echo%" neq "on" echo off
rem
rem This file invokes cmake and generates the build system for windows.

set argC=0
for %%x in (%*) do Set /A argC+=1

if NOT %argC%==9 GOTO :USAGE
if %1=="/?" GOTO :USAGE

setlocal
set __sourceDir=%~dp0..
set __VSString=%2
 :: Remove quotes
set __VSString=%__VSString:"=%
set __ExtraCmakeParams=

:: Set the target architecture to a format cmake understands. ANYCPU defaults to x64
set __RIDArch=%3
if /i "%3" == "x64"     (set cm_BaseRid=win7&&set  cm_Arch=AMD64&&set __ExtraCmakeParams=%__ExtraCmakeParams% -A x64)
if /i "%3" == "x86"     (set cm_BaseRid=win7&&set  cm_Arch=I386&&set  __ExtraCmakeParams=%__ExtraCmakeParams% -A Win32)
if /i "%3" == "arm"     (set cm_BaseRid=win8&&set  cm_Arch=ARM&&set   __ExtraCmakeParams=%__ExtraCmakeParams% -A ARM)
if /i "%3" == "arm64"   (set cm_BaseRid=win10&&set cm_Arch=ARM64&&set __ExtraCmakeParams=%__ExtraCmakeParams% -A ARM64)

set __LatestCommit=%4
set __HostVersion=%5
set __AppHostVersion=%6
set __HostFxrVersion=%7
set __HostPolicyVersion=%8

:: Form the base RID to be used if we are doing a portable build
if /i "%9" == "1"       (set cm_BaseRid=win)
set cm_BaseRid=%cm_BaseRid%-%__RIDArch%
echo "Computed RID for native build is %cm_BaseRid%"

if defined CMakePath goto DoGen

:: Eval the output from probe-win1.ps1
pushd "%__sourceDir%"
for /f "delims=" %%a in ('powershell -NoProfile -ExecutionPolicy ByPass "& .\Windows\probe-win.ps1"') do %%a
popd

:DoGen
echo "%CMakePath%" %__sourceDir% %__SDKVersion% "-DCLI_CMAKE_RUNTIME_ID:STRING=%cm_BaseRid%" "-DCLI_CMAKE_HOST_VER:STRING=%__HostVersion%" "-DCLI_CMAKE_COMMON_HOST_VER:STRING=%__AppHostVersion%" "-DCLI_CMAKE_HOST_FXR_VER:STRING=%__HostFxrVersion%" "-DCLI_CMAKE_HOST_POLICY_VER:STRING=%__HostPolicyVersion%" "-DCLI_CMAKE_PKG_RID:STRING=%cm_BaseRid%" "-DCLI_CMAKE_COMMIT_HASH:STRING=%__LatestCommit%" "-DCLI_CMAKE_PLATFORM_ARCH_%cm_Arch%=1" "-DCMAKE_INSTALL_PREFIX=%__CMakeBinDir%" "-DCLI_CMAKE_RESOURCE_DIR:STRING=%__ResourcesDir%" -G "Visual Studio %__VSString%" %__ExtraCmakeParams%
"%CMakePath%" %__sourceDir% %__SDKVersion% "-DCLI_CMAKE_RUNTIME_ID:STRING=%cm_BaseRid%" "-DCLI_CMAKE_HOST_VER:STRING=%__HostVersion%" "-DCLI_CMAKE_COMMON_HOST_VER:STRING=%__AppHostVersion%" "-DCLI_CMAKE_HOST_FXR_VER:STRING=%__HostFxrVersion%" "-DCLI_CMAKE_HOST_POLICY_VER:STRING=%__HostPolicyVersion%" "-DCLI_CMAKE_PKG_RID:STRING=%cm_BaseRid%" "-DCLI_CMAKE_COMMIT_HASH:STRING=%__LatestCommit%" "-DCLI_CMAKE_PLATFORM_ARCH_%cm_Arch%=1" "-DCMAKE_INSTALL_PREFIX=%__CMakeBinDir%" "-DCLI_CMAKE_RESOURCE_DIR:STRING=%__ResourcesDir%" -G "Visual Studio %__VSString%" %__ExtraCmakeParams%
endlocal
GOTO :DONE

:USAGE
  echo "Usage..."
  echo "gen-buildsys-win.bat <path to top level CMakeLists.txt> <VSVersion> <Target Architecture>"
  echo "Specify the path to the top level CMake file"
  echo "Specify the VSVersion to be used - VS2017 or VS2019"
  echo "Specify the Target Architecture - AnyCPU, x86, x64, ARM, or ARM64."
  echo "Specify latest commit hash"
  echo "Specify the host version, apphost version, hostresolver version, hostpolicy version"
  EXIT /B 1

:DONE
  EXIT /B 0
