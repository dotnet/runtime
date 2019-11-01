@if not defined _echo @echo off
setlocal

echo Installing dotnet using Arcade...
set PS_DOTNET_INSTALL_SCRIPT=". %~dp0eng\configure-toolset.ps1; . %~dp0eng\common\tools.ps1; InitializeBuildTool"
echo running: powershell -NoProfile -ExecutionPolicy unrestricted -Command %PS_DOTNET_INSTALL_SCRIPT%
powershell -NoProfile -ExecutionPolicy unrestricted -Command %PS_DOTNET_INSTALL_SCRIPT%
if NOT [%ERRORLEVEL%] == [0] (
  echo Failed to install dotnet using Arcade.
  exit /b %ERRORLEVEL%
)

exit /b 0