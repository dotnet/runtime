@echo off
setlocal enabledelayedexpansion

:: SetCommands defined in eng\testing\tests.wasm.targets
[[SetCommands]]
[[SetCommandsEcho]]

set EXECUTION_DIR=%~dp0

if [%HELIX_WORKITEM_UPLOAD_ROOT%] == [] (
    set "XHARNESS_OUT=%EXECUTION_DIR%xharness-output"
) else (
    set "XHARNESS_OUT=%HELIX_WORKITEM_UPLOAD_ROOT%\xharness-output"
)

if [%PREPEND_PATH%] NEQ [] (
    set "PATH=%PREPEND_PATH%;%PATH%"
)

echo EXECUTION_DIR=%EXECUTION_DIR%
echo SCENARIO=%SCENARIO%
echo XHARNESS_OUT=%XHARNESS_OUT%
echo XHARNESS_CLI_PATH=%XHARNESS_CLI_PATH%

set TEST_LOG_PATH=%XHARNESS_OUT%\logs

:: ========================= BEGIN Test Execution ============================= 
echo ----- start %DATE% %TIME% ===============  To repro directly: ===================================================== 
echo pushd %EXECUTION_DIR%
:: RunCommands defined in eng\testing\tests.wasm.targets
[[RunCommandsEcho]]
echo popd
echo ===========================================================================================================
pushd %EXECUTION_DIR%
@echo on
:: RunCommands defined in eng\testing\tests.wasm.targets
[[RunCommands]]
set EXIT_CODE=%ERRORLEVEL%
@echo off
popd
echo ----- end %DATE% %TIME% ----- exit code %EXIT_CODE% ----------------------------------------------------------

echo XHarness artifacts: %XHARNESS_OUT%

exit /b %EXIT_CODE%

REM Functions
:SetEnvVars
if [%TEST_USING_WORKLOADS%] == [true] (
    set SDK_HAS_WORKLOAD_INSTALLED=true
) else (
    set SDK_HAS_WORKLOAD_INSTALLED=false
)
if [%TEST_USING_WEBCIL%] == [false] (
   set USE_WEBCIL_FOR_TESTS=false
) else (
   set USE_WEBCIL_FOR_TESTS=true
)

if [%HELIX_CORRELATION_PAYLOAD%] NEQ [] (
    robocopy /mt /np /nfl /NDL /nc /e %BASE_DIR%\%SDK_DIR_NAME% %EXECUTION_DIR%\%SDK_DIR_NAME%
    set _SDK_DIR=%EXECUTION_DIR%\%SDK_DIR_NAME%
) else (
    set _SDK_DIR=%BASE_DIR%\%SDK_DIR_NAME%
)

set "SDK_FOR_WORKLOAD_TESTING_PATH=%_SDK_DIR%"
EXIT /b 0
