@ECHO OFF
setlocal enabledelayedexpansion

FOR /F "tokens=*" %%i IN ('type TestEnv.txt') DO SET %%i

SET EXECUTION_DIR=%~dp0
SET RUNTIME_PATH=%2

IF [%HELIX_WORKITEM_UPLOAD_ROOT%] == [] (
    SET "XHARNESS_OUT=%EXECUTION_DIR%xharness-output"
) ELSE (
    SET "XHARNESS_OUT=%HELIX_WORKITEM_UPLOAD_ROOT%\xharness-output"
)

FOR /F %%i IN ("%ASSEMBLY_NAME%") DO @SET TEST_SCRIPT=%%~ni.sh

IF /I [%1] NEQ [--runtime-path] (
    ECHO You must specify the runtime path with --runtime-path C:\path\to\runtime
    EXIT /b 1
)

SET ADDITIONAL_ARGS=%3 %4 %5 %6 %7 %8 %9

CALL :NORMALIZEPATH "%RUNTIME_PATH%"
SET RUNTIME_PATH=%RETVAL%

CD %EXECUTION_DIR%

:lock
MKDIR androidtests.lock 2>NUL 
IF "%errorlevel%" NEQ "0" (
    ping -n 6 127.0.0.1 >NUL
    GOTO :lock
)

IF [%XHARNESS_CLI_PATH%] NEQ [] (
    :: When running in CI, we only have the .NET runtime available
    :: We need to call the XHarness CLI DLL directly via dotnet exec
    SET HARNESS_RUNNER=dotnet.exe exec "%XHARNESS_CLI_PATH%"
) ELSE (
    SET HARNESS_RUNNER=dotnet.exe xharness
)

%HARNESS_RUNNER% android-headless test --test-path=%EXECUTION_DIR% --runtime-folder=%RUNTIME_PATH% --test-assembly=%ASSEMBLY_NAME% --device-arch=%TEST_ARCH% --test-script=%TEST_SCRIPT% --output-directory=%XHARNESS_OUT% --timeout=1800 -v %ADDITIONAL_ARGS%

SET EXIT_CODE=%ERRORLEVEL%

ECHO XHarness artifacts: %XHARNESS_OUT%

RMDIR /Q androidtests.lock 2>NUL
EXIT /B %EXIT_CODE%

:: ========== FUNCTIONS ==========
:NORMALIZEPATH
  SET RETVAL=%~f1
  EXIT /B
