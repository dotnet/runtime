@echo off
SETLOCAL enabledelayedexpansion

if not "%~1%" == "dotnetPath" (
  powershell -ExecutionPolicy ByPass -NoProfile -Command "& { . '%~dp0dotnet.ps1'; Get-DotNetPath %* }"
  goto:eof
)

:: Clear the 'Platform' env variable for this session, as it's a per-project setting within the build, and
:: misleading value (such as 'MCD' in HP PCs) may lead to build breakage (issue: #69).
set Platform=

:: Don't resolve runtime, shared framework, or SDK from other locations to ensure build determinism
set DOTNET_MULTILEVEL_LOOKUP=0

:: Disable first run since we want to control all package sources
set DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1

set dotnetPath="%~2\dotnet.exe"
: Remove first two arguments
set params=%*
set params=!params:%1 =!
set params=!params:%2=!

call "%dotnetPath%"!params!
