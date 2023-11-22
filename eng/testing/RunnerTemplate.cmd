@echo off
setlocal ENABLEEXTENSIONS ENABLEDELAYEDEXPANSION

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

:: Assume failure
set HAS_TEST_RESULTS=0

:: Support for SuperPMI collection
REM SuperPMI collection
if not defined spmi_enable_collection goto :skip_spmi_enable_collection
echo SuperPMI collection enabled
REM spmi_collect_dir and spmi_core_root need to be set before this script is run, if SuperPMI collection is enabled.
if not defined spmi_collect_dir echo ERROR: spmi_collect_dir not defined&exit /b 1
if not defined spmi_core_root echo ERROR: spmi_core_root not defined&exit /b 1
if not exist %spmi_collect_dir% mkdir %spmi_collect_dir%
set SuperPMIShimLogPath=%spmi_collect_dir%
set SuperPMIShimPath=%spmi_core_root%\clrjit.dll
if not exist %SuperPMIShimPath% echo ERROR: %SuperPMIShimPath% not found&exit /b 1
set DOTNET_EnableExtraSuperPmiQueries=1
set DOTNET_JitPath=%spmi_core_root%\superpmi-shim-collector.dll
if not exist %DOTNET_JitPath% echo ERROR: %DOTNET_JitPath% not found&exit /b 1
echo SuperPMIShimLogPath=%SuperPMIShimLogPath%
echo SuperPMIShimPath=%SuperPMIShimPath%
echo DOTNET_EnableExtraSuperPmiQueries=%DOTNET_EnableExtraSuperPmiQueries%
echo DOTNET_JitPath=%DOTNET_JitPath%
:skip_spmi_enable_collection

:: ========================= BEGIN Test Execution =============================
echo ----- start %DATE% %TIME% ===============  To repro directly: =====================================================
echo pushd %EXECUTION_DIR%
[[RunCommandsEcho]]
echo popd
echo ===========================================================================================================
pushd %EXECUTION_DIR%
@echo on
[[RunCommands]]
@set _exit_code=%ERRORLEVEL%
@echo off
if exist testResults.xml (
  set HAS_TEST_RESULTS=1
)
popd
echo ----- end %DATE% %TIME% ----- exit code %_exit_code% ----------------------------------------------------------
:: The helix work item should not exit with non-zero if tests ran and produced results
:: The special console runner for runtime returns 1 when tests fail
if %_exit_code%==1 (
  if %HAS_TEST_RESULTS%==1 (
    if not "%HELIX_WORKITEM_PAYLOAD%"=="" (
      exit /b 0
    )
  )
)
exit /b %_exit_code%
:: ========================= END Test Execution =================================

:usage
echo Usage: RunTests.cmd {-r^|--runtime-path} ^<runtime-path^> [{--rsp-file} ^<rsp-file^>]
echo.
echo Parameters:
echo --runtime-path           (Mandatory) Testhost containing the test runtime used during test execution (short: -r)"
echo --rsp-file               RSP file to pass in additional arguments
echo --help                   Print help and exit (short: -h)
