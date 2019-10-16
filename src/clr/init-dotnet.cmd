@if not defined _echo @echo off
setlocal

echo Installing dotnet using Arcade...
for /f %%i in ('git rev-parse --show-toplevel') do set __RepoRootDirRaw=%%i
set "__RepoRootDir=%__RepoRootDirRaw:/=\%"

echo Repo root folder: %__RepoRootDir%

set PS_DOTNET_INSTALL_SCRIPT=". %__RepoRootDir%\eng\configure-toolset.ps1; . %__RepoRootDir%\eng\common\tools.ps1; InitializeBuildTool"
echo running: powershell -NoProfile -ExecutionPolicy unrestricted -Command %PS_DOTNET_INSTALL_SCRIPT%
powershell -NoProfile -ExecutionPolicy unrestricted -Command %PS_DOTNET_INSTALL_SCRIPT%
if NOT [%ERRORLEVEL%] == [0] (
  echo Failed to install dotnet using Arcade.
  exit /b %ERRORLEVEL%
)

exit /b 0