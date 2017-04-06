@echo off
setlocal EnableDelayedExpansion

set __ProjectDir=%~dp0
set __DotNet=%__ProjectDir%\Tools\dotnetcli\dotnet.exe
set __MSBuild=%__ProjectDir%\Tools\MSBuild.dll
set __PortableBuildArgs=false

:Arg_Loop
if "%1" == "" goto ArgsDone

if /i "%1" == "-portable"             (set __PortableBuildArgs=true&shift&goto Arg_Loop)

:ArgsDone

:: Initialize the MSBuild Tools
call "%__ProjectDir%\init-tools.cmd"

:: Restore dependencies mainly to obtain runtime.json
pushd "%__ProjectDir%\deps"
"%__DotNet%" restore --configfile "%__ProjectDir%\..\NuGet.Config" --packages "%__ProjectDir%\packages"
popd

:: Clean up existing nupkgs
if exist "%__ProjectDir%\bin" (rmdir /s /q "%__ProjectDir%\bin")

"%__DotNet%" "%__MSBuild%" "%__ProjectDir%\tasks\core-setup.tasks.builds" /verbosity:minimal /flp:logfile=tools.log;v=diag
if not ERRORLEVEL 0 goto :Error

:: Package the assets using Tools
"%__DotNet%" "%__MSBuild%" "%__ProjectDir%\packages.builds" /p:OSGroup=Windows_NT /verbosity:minimal /p:PortableBuild=%__PortableBuildArgs%

if not ERRORLEVEL 0 goto :Error
exit /b 0

:Error
echo An error occurred during packing.
exit /b 1
