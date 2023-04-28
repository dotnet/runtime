@echo off
setlocal enabledelayedexpansion

:: SetCommands defined in eng\testing\tests.wasm.targets
[[SetCommands]]
[[SetCommandsEcho]]

set EXECUTION_DIR=%~dp0
if [%3] NEQ [] (
    set SCENARIO=%3
)

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
    if /I [%SCENARIO%]==[WasmTestOnBrowser] (
        set XHARNESS_COMMAND=test-browser
    ) else (
        set XHARNESS_COMMAND=test
    )
)

if /I [%XHARNESS_COMMAND%] == [test] (
    if [%JS_ENGINE%] == [] (
        if /I [%SCENARIO%] == [WasmTestOnNodeJS] (
            set "JS_ENGINE=--engine^=NodeJS"
        ) else (
            set "JS_ENGINE=--engine^=V8"
        )
    )
    if [%MAIN_JS%] == [] (
        set "MAIN_JS=--js-file^=test-main.js"
    )

    if [%JS_ENGINE_ARGS%] == [] (
        set "JS_ENGINE_ARGS=--engine-arg^=--stack-trace-limit^=1000 --engine-arg^=--experimental-wasm-eh"
    )
) else (
    if [%BROWSER_PATH%] == [] if not [%HELIX_CORRELATION_PAYLOAD%] == [] (
        set "BROWSER_PATH=--browser-path^=%HELIX_CORRELATION_PAYLOAD%\chrome-win\chrome.exe"
    )
)

if [%XHARNESS_ARGS%] == [] (
    set "XHARNESS_ARGS=%JS_ENGINE% %JS_ENGINE_ARGS% %BROWSER_PATH% %MAIN_JS%"
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
echo MAIN_JS=%MAIN_JS%
echo JS_ENGINE=%JS_ENGINE%
echo JS_ENGINE_ARGS=%JS_ENGINE_ARGS%
echo XHARNESS_ARGS=%XHARNESS_ARGS%

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
