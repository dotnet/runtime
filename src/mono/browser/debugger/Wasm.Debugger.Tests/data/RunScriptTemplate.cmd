@echo off
setlocal enabledelayedexpansion

set EXECUTION_DIR=%~dp0

cd %EXECUTION_DIR%

if [%HELIX_WORKITEM_UPLOAD_ROOT%] == [] (
    set XHARNESS_OUT=%EXECUTION_DIR%xharness-output
) else (
    set XHARNESS_OUT=%HELIX_WORKITEM_UPLOAD_ROOT%\xharness-output
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

echo artifacts: %XHARNESS_OUT%

exit /b %EXIT_CODE%
