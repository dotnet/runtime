@if not defined _echo @echo off
setlocal

if not defined VisualStudioVersion (
  if defined VS140COMNTOOLS (
	 if not exist "%VS140COMNTOOLS%\..\IDE\devenv.exe"      goto NoVS
	 if not exist "%VS140COMNTOOLS%\..\..\VC\vcvarsall.bat" goto NoVS
	 if not exist "%VS140COMNTOOLS%\VsDevCmd.bat" 			  goto NoVS
    call "%VS140COMNTOOLS%\VsDevCmd.bat"
    goto :Run
  )

  :NoVS
  echo Error: Visual Studio 2015 required.
  echo        https://github.com/dotnet/coreclr/blob/master/Documentation/building/windows-instructions.md for build instructions.
  exit /b 1
)

:Run
:: Clear the 'Platform' env variable for this session, as it's a per-project setting within the build, and
:: misleading value (such as 'MCD' in HP PCs) may lead to build breakage (issue: #69).
set Platform=

:: Restore the Tools directory
call %~dp0init-tools.cmd
if NOT [%ERRORLEVEL%]==[0] (
  exit /b 1
)

set _toolRuntime=%~dp0Tools
set _dotnet=%_toolRuntime%\dotnetcli\dotnet.exe

echo Running: %_dotnet% %_toolRuntime%\run.exe %~dp0config.json %*
call %_dotnet% %_toolRuntime%\run.exe %~dp0config.json %*
if NOT [%ERRORLEVEL%]==[0] (
  exit /b 1
)

exit /b 0
