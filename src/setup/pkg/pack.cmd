@echo off
setlocal EnableDelayedExpansion

set __ProjectDir=%~dp0
set __DotNet=%__ProjectDir%\Tools\dotnetcli\dotnet.exe
set __MSBuild=%__ProjectDir%\Tools\MSBuild.exe

:: Initialize the MSBuild Tools
call "%__ProjectDir%\init-tools.cmd"

:: Restore dependencies mainly to obtain runtime.json
pushd "%__ProjectDir%\deps"
"%__DotNet%" restore --source "https://dotnet.myget.org/F/dotnet-core" --packages "%__ProjectDir%\packages"
popd

:: Clean up existing nupkgs
if exist "%__ProjectDir%\bin" (rmdir /s /q "%__ProjectDir%\bin")

:: Package the assets using Tools

"%__DotNet%" "%__MSBuild%" "%__ProjectDir%\projects\packages.builds" /p:TargetsWindows=true /verbosity:minimal

if not ERRORLEVEL 0 goto :Error
exit /b 0

:Error
echo An error occurred during packing.
exit /b 1
