@echo off
setlocal enabledelayedexpansion

set EXECUTION_DIR=%~dp0

:argparser_start
  if "%~1" == "" goto argparser_end
  set "argparser_currentarg=%~1"
  shift

  set "argparser_help_specified_inloop="
  if /i "%argparser_currentarg%"=="-h" ( set "argparser_help_specified_inloop=1" )
  if /i "%argparser_currentarg%"=="--help" ( set "argparser_help_specified_inloop=1" )
  if defined argparser_help_specified_inloop (
    goto usage
  )

  set "argparser_runtime_path_specified_inloop="
  if /i "%argparser_currentarg%"=="-r" ( set "argparser_runtime_path_specified_inloop=1" )
  if /i "%argparser_currentarg%"=="--runtime-path" ( set "argparser_runtime_path_specified_inloop=1" )
  if defined argparser_runtime_path_specified_inloop (
    if "%~1" == "" ( goto argparser_invalid )
    set "RUNTIME_PATH=%~1"
    goto argparser_break
  )

  if /i "%argparser_currentarg%"=="--rsp-file" (
    if "%~1" == "" ( goto argparser_invalid )
    set "RSP_FILE=@%~1"
    goto argparser_break
  )

:argparser_invalid
  echo Invalid argument or value: %argparser_currentarg%
  call :usage
  exit /b -1

:argparser_break
  shift
  goto argparser_start

:argparser_end

:: Don't use a globally installed SDK.
set DOTNET_MULTILEVEL_LOOKUP=0

:: ========================= BEGIN Test Execution ============================= 
echo ----- start %DATE% %TIME% ===============  To repro directly: ===================================================== 
echo pushd %EXECUTION_DIR%
[[RunCommandsEcho]]
echo popd
echo ===========================================================================================================
pushd %EXECUTION_DIR%
@echo on
[[RunCommands]]
@echo off
popd
echo ----- end %DATE% %TIME% ----- exit code %ERRORLEVEL% ----------------------------------------------------------
exit /b %ERRORLEVEL%
:: ========================= END Test Execution =================================

:usage
echo Usage: RunTests.cmd {-r^|--runtime-path} ^<runtime-path^> [{--rsp-file} ^<rsp-file^>]
echo.
echo Parameters:
echo --runtime-path           (Mandatory) Testhost containing the test runtime used during test execution (short: -r)"
echo --rsp-file               RSP file to pass in additional arguments
echo --help                   Print help and exit (short: -h)
