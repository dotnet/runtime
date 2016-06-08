@echo off
setlocal EnableDelayedExpansion

set __ProjectDir=%~dp0

:: Initialize the MSBuild Tools
call "%__ProjectDir%\init-tools.cmd"

:: Restore dependencies mainly to obtain runtime.json
pushd "%__ProjectDir%\deps"
"%__ProjectDir%\Tools\dotnetcli\dotnet.exe" restore --source "https://dotnet.myget.org/F/dotnet-core" --packages "%__ProjectDir%\packages"
popd

:: Clean up existing nupkgs
if exist "%__ProjectDir%\bin" (rmdir /s /q "%__ProjectDir%\bin")

:: Package the assets using Tools

"%__ProjectDir%\Tools\corerun" "%__ProjectDir%\Tools\MSBuild.exe" "%__ProjectDir%\projects\packages.builds" /p:TargetsWindows=true /verbosity:minimal

if not ERRORLEVEL 0 goto :Error
exit /b 0

:Error
echo An error occurred during packing.
exit /b 1
