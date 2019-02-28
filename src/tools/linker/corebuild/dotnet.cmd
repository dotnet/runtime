@if not defined _echo @echo off
setlocal

if not defined VisualStudioVersion (
  if defined VS150COMNTOOLS (
    call "%VS150COMNTOOLS%\VsDevCmd.bat"
    goto :Run
  )
  if defined VS140COMNTOOLS (
    call "%VS140COMNTOOLS%\VsDevCmd.bat"
    goto :Run
  )
  echo Error: Visual Studio 2015 or 2017 required.
  exit /b 1
)

:Run
powershell -NoProfile -ExecutionPolicy unrestricted -Command "%~dp0..\eng\dotnet.ps1 -- %*"
exit /b %ERRORLEVEL%
