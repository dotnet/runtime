@if not defined _echo @echo off
setlocal

:: Clear the 'Platform' env variable for this session, as it's a per-project setting within the build, and
:: misleading value (such as 'MCD' in HP PCs) may lead to build breakage (issue: #69).
set Platform=

:: Don't resolve runtime, shared framework, or SDK from other locations to ensure build determinism
set DOTNET_MULTILEVEL_LOOKUP=0

:: Disable first run since we want to control all package sources
set DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1

set PS_DOTNET_INSTALL_SCRIPT=". %~dp0eng\common\tools.ps1; InitializeDotNetCli($true)"
set "PS_COMMAND=powershell -NoProfile -ExecutionPolicy unrestricted -Command %PS_DOTNET_INSTALL_SCRIPT%"

for /f "delims=" %%l in ('%PS_COMMAND%') do set "__dotnetDir=%%l"

set "dotnetPath=%__dotnetDir%\dotnet.exe"
call "%dotnetPath%" %*
