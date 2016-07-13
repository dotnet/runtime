@if "%_echo%" neq "on" echo off
setlocal EnableDelayedExpansion

set "__ProjectDir=%~dp0"
set packagesLog=build-packages.log
set binclashLoggerDll=%~dp0Tools\net45\Microsoft.DotNet.Build.Tasks.dll
set binclashlog=%~dp0binclash.log
echo Running build-packages.cmd %* > %packagesLog%

set options=/nologo /maxcpucount /v:minimal /clp:Summary /nodeReuse:false /flp:v=detailed;Append;LogFile=%packagesLog% /l:BinClashLogger,%binclashLoggerDll%;LogFile=%binclashlog% /p:FilterToOSGroup=Windows_NT
set allargs=%*

if /I [%1] == [/?] goto Usage
if /I [%1] == [/help] goto Usage

REM ensure that msbuild is available
echo Running init-tools.cmd
call %~dp0init-tools.cmd

set __msbuildArgs="%__ProjectDir%\src\.nuget\Microsoft.NETCore.Runtime.CoreClr\Microsoft.NETCore.Runtime.CoreCLR.builds" !allargs!
echo msbuild.exe %__msbuildArgs% !options! >> %packagesLog%
call msbuild.exe %__msbuildArgs% !options!
if NOT [!ERRORLEVEL!]==[0] (
  echo ERROR: An error occurred while building packages, see %packagesLog% for more details.
  exit /b 1
)

set __msbuildArgs="%__ProjectDir%\src\.nuget\Microsoft.NETCore.Jit\Microsoft.NETCore.Jit.builds" !allargs!
echo msbuild.exe %__msbuildArgs% !options! >> %packagesLog%
call msbuild.exe %__msbuildArgs% !options!
if NOT [!ERRORLEVEL!]==[0] (
  echo ERROR: An error occurred while building packages, see %packagesLog% for more details.
  exit /b 1
)

rem Build the ILAsm package
set __msbuildArgs="%__ProjectDir%\src\.nuget\Microsoft.NETCore.ILAsm\Microsoft.NETCore.ILAsm.builds" !allargs!
echo msbuild.exe %__msbuildArgs% !options! >> %packagesLog%
call msbuild.exe %__msbuildArgs% !options!
if NOT [!ERRORLEVEL!]==[0] (
  echo ERROR: An error occurred while building packages, see %packagesLog% for more details.
  exit /b 1
)

rem Build the ILDAsm package
set __msbuildArgs="%__ProjectDir%\src\.nuget\Microsoft.NETCore.ILDAsm\Microsoft.NETCore.ILDAsm.builds" !allargs!
echo msbuild.exe %__msbuildArgs% !options! >> %packagesLog%
call msbuild.exe %__msbuildArgs% !options!
if NOT [!ERRORLEVEL!]==[0] (
  echo ERROR: An error occurred while building packages, see %packagesLog% for more details.
  exit /b 1
)

rem Build the TargetingPack package
set __msbuildArgs="%__ProjectDir%\src\.nuget\Microsoft.TargetingPack.Private.CoreCLR\Microsoft.TargetingPack.Private.CoreCLR.pkgproj" !allargs! 
echo msbuild.exe %__msbuildArgs% !options! >> %packagesLog%
call msbuild.exe %__msbuildArgs% !options!
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
echo   /p:__BuildArch=[architecture] /p:__BuildType=[configuration]
echo Architecture can be x64, x86, arm, or arm64
echo Configuration can be Release, Debug, or Checked
exit /b
