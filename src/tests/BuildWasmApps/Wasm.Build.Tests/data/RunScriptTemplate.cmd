@echo off
setlocal enabledelayedexpansion

set EXECUTION_DIR=%~dp0
set SCENARIO=%3

cd %EXECUTION_DIR%

if [%HELIX_WORKITEM_UPLOAD_ROOT%] == [] (
    set XHARNESS_OUT=%EXECUTION_DIR%xharness-output
) else (
    set XHARNESS_OUT=%HELIX_WORKITEM_UPLOAD_ROOT%\xharness-output
)

if [%XHARNESS_CLI_PATH%] NEQ [] (
    :: When running in CI, we only have the .NET runtime available
    :: We need to call the XHarness CLI DLL directly via dotnet exec
    set HARNESS_RUNNER=dotnet.exe exec "%XHARNESS_CLI_PATH%"
) else (
    set HARNESS_RUNNER=dotnet.exe xharness
)

set TEST_LOG_PATH=%XHARNESS_OUT%\logs

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

REM Functions
:SetEnvVars
if [%TEST_USING_WORKLOADS%] == [true] (
    set "PATH=%BASE_DIR%\dotnet-workload;%PATH%"
    set "SDK_FOR_WORKLOAD_TESTING_PATH=%BASE_DIR%\dotnet-workload"
    set "AppRefDir=%BASE_DIR%\microsoft.netcore.app.ref"
) else (
    set "WasmBuildSupportDir=%BASE_DIR%\build"
)
EXIT /b 0
