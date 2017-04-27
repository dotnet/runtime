@if not defined _echo @echo off
setlocal

:: Default to highest Visual Studio version available
::
:: For VS2015 (and prior), only a single instance is allowed to be installed on a box
:: and VS140COMNTOOLS is set as a global environment variable by the installer. This
:: allows users to locate where the instance of VS2015 is installed.
::
:: For VS2017, multiple instances can be installed on the same box SxS and VS150COMNTOOLS
:: is no longer set as a global environment variable and is instead only set if the user
:: has launched the VS2017 Developer Command Prompt.
::
:: Following this logic, we will default to the VS2017 toolset if VS150COMNTOOLS tools is
:: set, as this indicates the user is running from the VS2017 Developer Command Prompt and
:: is already configured to use that toolset. Otherwise, we will fallback to using the VS2015
:: toolset if it is installed. Finally, we will fail the script if no supported VS instance
:: can be found.
if not defined VisualStudioVersion (
  if defined VS150COMNTOOLS (
	 if not exist "%VS150COMNTOOLS%\..\IDE\devenv.exe"      goto NoVS
	 if not exist "%VS150COMNTOOLS%\..\..\VC\Auxiliary\Build\vcvarsall.bat" goto NoVS
	 if not exist "%VS150COMNTOOLS%\VsDevCmd.bat" 			  goto NoVS
    call "%VS150COMNTOOLS%\VsDevCmd.bat"
    goto :Run
  ) else if defined VS140COMNTOOLS (
	 if not exist "%VS140COMNTOOLS%\..\IDE\devenv.exe"      goto NoVS
	 if not exist "%VS140COMNTOOLS%\..\..\VC\vcvarsall.bat" goto NoVS
	 if not exist "%VS140COMNTOOLS%\VsDevCmd.bat" 			  goto NoVS
    call "%VS140COMNTOOLS%\VsDevCmd.bat"
    goto :Run
  )

  :NoVS
  echo Error: Visual Studio 2015 or 2017 required.
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

:: Always copy over the Tools-Override
xcopy %~dp0Tools-Override\* %~dp0Tools /y >nul

set _toolRuntime=%~dp0Tools
set _dotnet=%_toolRuntime%\dotnetcli\dotnet.exe
set _json=%~dp0config.json

:: run.exe depends on running in the root directory, notably because the config.json specifies
:: a relative path to the binclash logger

pushd %~dp0
echo Running: %_dotnet% %_toolRuntime%\run.exe %~dp0config.json %*
call %_dotnet% %_toolRuntime%\run.exe "%_json%" %*
popd
if NOT [%ERRORLEVEL%]==[0] (
  exit /b 1
)

exit /b 0
