@if "%_echo%" neq "on" echo off
setlocal

if not defined VisualStudioVersion (
  if defined VS140COMNTOOLS (
    call "%VS140COMNTOOLS%\VsDevCmd.bat"
    goto :Run
  )
  echo Error: Visual Studio 2015 required.
  echo        Please see https://github.com/dotnet/corefx/blob/master/Documentation/building/windows-instructions.md for build instructions.
  exit /b 1
)

:Run
:: We do not want to run the first-time experience.
set DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
powershell -NoProfile -ExecutionPolicy unrestricted -Command "%~dp0run.ps1 -- %*"
exit /b %ERRORLEVEL%