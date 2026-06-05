@echo off
setlocal enabledelayedexpansion

set "BATCH_DIR=%CD%"
set /a SUITE_COUNT=0
set /a FAIL_COUNT=0
set "FOUND_ZIP="
set "PYTHON=%HELIX_PYTHONPATH%"
if "%PYTHON%"=="" set "PYTHON=python"

if "%HELIX_WORKITEM_UPLOAD_ROOT%"=="" (
    set "ORIGINAL_UPLOAD_ROOT=%CD%\test-results"
) else (
    set "ORIGINAL_UPLOAD_ROOT=%HELIX_WORKITEM_UPLOAD_ROOT%"
)

echo === DesktopBatchRunner ===
echo BATCH_DIR=%BATCH_DIR%
echo ORIGINAL_UPLOAD_ROOT=%ORIGINAL_UPLOAD_ROOT%

for %%z in ("%BATCH_DIR%\*.zip") do (
    if exist "%%~fz" (
        set "FOUND_ZIP=1"
        set "suiteName=%%~nz"
        set "suiteDir=%BATCH_DIR%\!suiteName!"
        set "suiteExitCode=0"

        echo.
        echo ========================= BEGIN !suiteName! =============================

        mkdir "!suiteDir!" >nul 2>nul
        "%PYTHON%" -c "import zipfile,sys; zipfile.ZipFile(sys.argv[1]).extractall(sys.argv[2])" "%%~fz" "!suiteDir!"
        if !errorlevel! neq 0 (
            echo ERROR: Failed to extract %%~fz
            set "suiteExitCode=1"
        ) else (
            set "HELIX_WORKITEM_UPLOAD_ROOT=!ORIGINAL_UPLOAD_ROOT!\!suiteName!"
            mkdir "!HELIX_WORKITEM_UPLOAD_ROOT!" >nul 2>nul

            pushd "!suiteDir!"
            call RunTests.cmd %*
            set "suiteExitCode=!errorlevel!"
            popd
        )

        rmdir /s /q "!suiteDir!" 2>nul

        set /a SUITE_COUNT+=1

        if !suiteExitCode! neq 0 (
            set /a FAIL_COUNT+=1
            echo ----- FAIL !suiteName! - exit code !suiteExitCode! -----
        ) else (
            echo ----- PASS !suiteName! -----
        )

        echo ========================= END !suiteName! ===============================
    )
)

set "HELIX_WORKITEM_UPLOAD_ROOT=%ORIGINAL_UPLOAD_ROOT%"

if not defined FOUND_ZIP (
    echo No .zip files found in %BATCH_DIR%
    exit /b 1
)

echo.
echo === Batch Summary ===
set /a PASS_COUNT=SUITE_COUNT-FAIL_COUNT
echo Total: %SUITE_COUNT% ^| Passed: %PASS_COUNT% ^| Failed: %FAIL_COUNT%

if %FAIL_COUNT% neq 0 (
    exit /b 1
)

exit /b 0
