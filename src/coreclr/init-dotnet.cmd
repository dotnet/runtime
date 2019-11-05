@if not defined _echo @echo off
setlocal

set "__ProjectDir=%~dp0"
set "__RepoRootDir=%__ProjectDir%..\..\"

rem Remove after repo consolidation
if not exist "%__RepoRootDir%\.dotnet-runtime-placeholder" ( set "__RepoRootDir=%__ProjectDir%" )

echo Installing dotnet using Arcade...
set PS_DOTNET_INSTALL_SCRIPT=". %__RepoRootDir%eng\configure-toolset.ps1; . %__RepoRootDir%eng\common\tools.ps1; InitializeBuildTool"
echo running: powershell -NoProfile -ExecutionPolicy unrestricted -Command %PS_DOTNET_INSTALL_SCRIPT%
powershell -NoProfile -ExecutionPolicy unrestricted -Command %PS_DOTNET_INSTALL_SCRIPT%
if NOT [%ERRORLEVEL%] == [0] (
  echo Failed to install dotnet using Arcade.
  exit /b %ERRORLEVEL%
)

exit /b 0