@echo off
setlocal enabledelayedexpansion

:: SetCommands defined in eng\testing\tests.wasi.targets
[[SetCommands]]
[[SetCommandsEcho]]

set EXECUTION_DIR=%~dp0
if [%3] NEQ [] (
    set SCENARIO=%3
)

set PATH=%PREPEND_PATH%;%PATH%

if [%HELIX_WORKITEM_UPLOAD_ROOT%] == [] (
    set "XHARNESS_OUT=%EXECUTION_DIR%xharness-output"
) else (
    set "XHARNESS_OUT=%HELIX_WORKITEM_UPLOAD_ROOT%\xharness-output"
)

if [%XHARNESS_CLI_PATH%] NEQ [] (
    :: When running in CI, we only have the .NET runtime available
    :: We need to call the XHarness CLI DLL directly via dotnet exec
    set HARNESS_RUNNER=dotnet.exe exec "%XHARNESS_CLI_PATH%"
) else (
    set HARNESS_RUNNER=dotnet.exe xharness
)

if [%XHARNESS_COMMAND%] == [] (
    set XHARNESS_COMMAND=test
)

if [%XHARNESS_ARGS%] == [] (
    set "XHARNESS_ARGS=%ENGINE_ARGS%"
)

if [%PREPEND_PATH%] NEQ [] (
    set "PATH=%PREPEND_PATH%:%PATH%"
)

if [%XUNIT_RANDOM_ORDER_SEED%] NEQ [] (
    set "WasmXHarnessMonoArgs=%WasmXHarnessMonoArgs% --setenv=XUNIT_RANDOM_ORDER_SEED=%XUNIT_RANDOM_ORDER_SEED%"
)

echo EXECUTION_DIR=%EXECUTION_DIR%
echo SCENARIO=%SCENARIO%
echo XHARNESS_OUT=%XHARNESS_OUT%
echo XHARNESS_CLI_PATH=%XHARNESS_CLI_PATH%
echo HARNESS_RUNNER=%HARNESS_RUNNER%
echo XHARNESS_COMMAND=%XHARNESS_COMMAND%
echo XHARNESS_ARGS=%XHARNESS_ARGS%

:: ========================= BEGIN Test Execution ============================= 
echo ----- start %DATE% %TIME% ===============  To repro directly: ===================================================== 
echo pushd %EXECUTION_DIR%
:: RunCommands defined in eng\testing\tests.wasi.targets
[[RunCommandsEcho]]
echo popd
echo ===========================================================================================================
pushd %EXECUTION_DIR%
@echo on
:: RunCommands defined in eng\testing\tests.wasi.targets
[[RunCommands]]
set EXIT_CODE=%ERRORLEVEL%
@echo off
popd
echo ----- end %DATE% %TIME% ----- exit code %EXIT_CODE% ----------------------------------------------------------

echo XHarness artifacts: %XHARNESS_OUT%

exit /b %EXIT_CODE%
