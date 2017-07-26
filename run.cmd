@if "%_echo%" neq "on" echo off
setlocal

if defined VisualStudioVersion goto :Run

set _VSWHERE="%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"
if exist %_VSWHERE% (
  for /f "usebackq tokens=*" %%i in (`%_VSWHERE% -latest -property installationPath`) do set _VSCOMNTOOLS=%%i\Common7\Tools
)
if not exist "%_VSCOMNTOOLS%" set _VSCOMNTOOLS=%VS140COMNTOOLS%
if not exist "%_VSCOMNTOOLS%" (
  echo Error: Visual Studio 2015 or 2017 required.
  echo        Please see https://github.com/dotnet/corefx/blob/master/Documentation/project-docs/developer-guide.md for build instructions.
  exit /b 1
)

call "%_VSCOMNTOOLS%\VsDevCmd.bat"

:Run
:: We do not want to run the first-time experience.
set DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
powershell -NoProfile -ExecutionPolicy unrestricted -Command "%~dp0run.ps1 -- %*"
exit /b %ERRORLEVEL%