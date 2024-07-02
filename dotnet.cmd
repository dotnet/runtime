@echo off

powershell -ExecutionPolicy ByPass -NoProfile -Command "& { . '%~dp0eng\common\tools.ps1'; InitializeDotNetCli $true $true }"

if NOT [%ERRORLEVEL%] == [0] (
  echo Failed to install or invoke dotnet... 1>&2
  exit /b %ERRORLEVEL%
)

set /p dotnetPath=<%~dp0artifacts\toolset\sdk.txt

:: Clear the 'Platform' env variable for this session, as it's a per-project setting within the build, and
:: misleading value (such as 'MCD' in HP PCs) may lead to build breakage (issue: #69).
set Platform=

:: Don't resolve runtime, shared framework, or SDK from other locations to ensure build determinism
set DOTNET_MULTILEVEL_LOOKUP=0

:: Install at .dotent/${RID}
set DOTNET_USE_ARCH_IN_INSTALL_PATH=1

call "%dotnetPath%\dotnet.exe" %*
