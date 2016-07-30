@if "%_echo%" neq "on" echo off
setlocal EnableDelayedExpansion

set "__ProjectDir=%~dp0"
set allargs=%*

if /I [%1] == [/?] goto Usage
if /I [%1] == [/help] goto Usage

call %__ProjectDir%/run.cmd build-packages -Project=%__ProjectDir%/src/.nuget/Microsoft.NETCore.Runtime.CoreClr/Microsoft.NETCore.Runtime.CoreCLR.builds -FilterToOSGroup=Windows_NT %allargs%
if NOT [!ERRORLEVEL!]==[0] (
  echo ERROR: An error occurred while building CoreCLR Runtime package, see build-packages.log for more details.
  exit /b 1
)

call %__ProjectDir%/run.cmd build-packages -Project=%__ProjectDir%/src/.nuget/Microsoft.NETCore.Jit/Microsoft.NETCore.Jit.builds -FilterToOSGroup=Windows_NT %allargs%
if NOT [!ERRORLEVEL!]==[0] (
  echo ERROR: An error occurred while building Jit package, see build-packages.log for more details.
  exit /b 1
)

call %__ProjectDir%/run.cmd build-packages -Project=%__ProjectDir%/src/.nuget/Microsoft.NETCore.ILAsm/Microsoft.NETCore.ILAsm.builds -FilterToOSGroup=Windows_NT %allargs%
if NOT [!ERRORLEVEL!]==[0] (
  echo ERROR: An error occurred while building ILAsm package, see build-packages.log for more details.
  exit /b 1
)

call %__ProjectDir%/run.cmd build-packages -Project=%__ProjectDir%/src/.nuget/Microsoft.NETCore.ILDAsm/Microsoft.NETCore.ILDAsm.builds -FilterToOSGroup=Windows_NT %allargs%
if NOT [!ERRORLEVEL!]==[0] (
  echo ERROR: An error occurred while building ILDAsm package, see build-packages.log for more details.
  exit /b 1
)

call %__ProjectDir%/run.cmd build-packages -Project=%__ProjectDir%/src/.nuget/Microsoft.TargetingPack.Private.CoreCLR/Microsoft.TargetingPack.Private.CoreCLR.pkgproj -FilterToOSGroup=Windows_NT %allargs%
if NOT [!ERRORLEVEL!]==[0] (
  echo ERROR: An error occurred while building CoreCLR TargetingPack package, see build-packages.log for more details.
  exit /b 1
)

call %__ProjectDir%/run.cmd build-packages -Project=%__ProjectDir%\src\.nuget\Microsoft.NETCore.TestHost\Microsoft.NETCore.TestHost.builds -FilterToOSGroup=Windows_NT %allargs%
if NOT [!ERRORLEVEL!]==[0] (
  echo ERROR: An error occurred while building packages, see %packagesLog% for more details.
  exit /b 1
)

echo Done Building Packages.
exit /b

:Usage
echo.
echo Builds the NuGet packages from the binaries that were built in the Build product binaries step.
echo The following properties are required to define build architecture
echo   -BuildArch=[architecture] -BuildType=[configuration]
echo Architecture can be x64, x86, arm, or arm64
echo Configuration can be Release, Debug, or Checked
exit /b
