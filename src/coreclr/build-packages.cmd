@if not defined _echo @echo off
setlocal EnableDelayedExpansion

set "__ProjectDir=%~dp0"
set allargs=%*

if /I [%1] == [/?] goto Usage
if /I [%1] == [/help] goto Usage

call %__ProjectDir%/run.cmd build-packages -Project=%__ProjectDir%\src\.nuget\packages.builds -FilterToOSGroup=Windows_NT %allargs%
if NOT [!ERRORLEVEL!]==[0] (
  echo ERROR: An error occurred while building packages, see build-packages.log for more details.
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
