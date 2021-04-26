@echo off
setlocal enabledelayedexpansion

set EXECUTION_DIR=%~dp0
set SCENARIO=%3

if [%HELIX_WORKITEM_UPLOAD_ROOT%] == [] (
    set XHARNESS_OUT="%EXECUTION_DIR%xharness-output"
) else (
    set XHARNESS_OUT="%HELIX_WORKITEM_UPLOAD_ROOT%xharness-output"
)

if [%XHARNESS_CLI_PATH%] NEQ [] (
    :: When running in CI, we only have the .NET runtime available
    :: We need to call the XHarness CLI DLL directly via dotnet exec
    set HARNESS_RUNNER=dotnet.exe exec "%XHARNESS_CLI_PATH%"
) else (
    set HARNESS_RUNNER=dotnet.exe xharness
)

if [%SCENARIO%]==[WasmTestOnBrowser] (
    set XHARNESS_COMMAND=test-browser
) else (
    if [%XHARNESS_COMMAND%] == [] (
    set XHARNESS_COMMAND=test
    )
)

:: ========================= BEGIN Test Execution ============================= 
echo ----- start %DATE% %TIME% ===============  To repro directly: ===================================================== 
echo pushd %EXECUTION_DIR%
[[RunCommandsEcho]]
echo popd
echo ===========================================================================================================
pushd %EXECUTION_DIR%
@echo on
[[RunCommands]]
set EXIT_CODE=%ERRORLEVEL%
@echo off
popd
echo ----- end %DATE% %TIME% ----- exit code %EXIT_CODE% ----------------------------------------------------------

echo XHarness artifacts: %XHARNESS_OUT%

exit /b %EXIT_CODE%
