@ECHO OFF
setlocal enabledelayedexpansion

SET EXECUTION_DIR=%~dp0
SET ASSEMBLY_NAME=%1
SET TARGET_ARCH=%2
SET TARGET_OS=%3
SET TEST_NAME=%4
SET REPO_ROOT=%5

:Arg_Loop
if "%6" == "" goto ArgsDone
set "__AdditionalArgs=!__AdditionalArgs! %6"&shift&goto Arg_Loop
:ArgsDone

SET "XHARNESS_OUT=%EXECUTION_DIR%xharness-output"

cd %EXECUTION_DIR%

:lock
MKDIR androidtests.lock 2>NUL 
IF "%errorlevel%" NEQ "0" (
    ping -n 6 127.0.0.1 >NUL
    GOTO :lock
)

IF [%XHARNESS_CLI_PATH%] NEQ [] (
    :: When running in CI, we only have the .NET runtime available
    :: We need to call the XHarness CLI DLL directly via dotnet exec
    SET HARNESS_RUNNER=%REPO_ROOT%dotnet.cmd exec "%XHARNESS_CLI_PATH%"
) ELSE (
    SET HARNESS_RUNNER=%REPO_ROOT%dotnet.cmd xharness
)

%HARNESS_RUNNER% android test --instrumentation="net.dot.MonoRunner" --package-name="net.dot.%ASSEMBLY_NAME%" --app="%EXECUTION_DIR%bin\%TEST_NAME%.apk" --output-directory="%XHARNESS_OUT%" --timeout=1800 %__AdditionalArgs%

SET EXIT_CODE=%ERRORLEVEL%

ECHO XHarness artifacts: %XHARNESS_OUT%

RMDIR /Q androidtests.lock 2>NUL
EXIT /B %EXIT_CODE%

:: ========== FUNCTIONS ==========
:NORMALIZEPATH
  SET RETVAL=%~f1
  EXIT /B
