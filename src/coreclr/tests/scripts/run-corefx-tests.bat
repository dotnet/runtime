@echo off
setlocal ENABLEDELAYEDEXPANSION
goto start

:usage
echo Usage: run-corefx-tests.bat ^<runtime path^> ^<tests dir^> ^<test exclusion file^>
echo.
echo Runs the corefx tests on a Windows ARM device, by searching for all relevant corefx
echo RunTests.cmd files in the ^<tests dir^> tree, and running each one in turn. This
echo script is typically run on a Windows ARM machine after the run-corefx-test.py script
echo is run on a Windows x64 machine with the `-no_run_tests` argument, to build the
echo corefx tree, including tests, and then copying the built runtime layout and tests
echo to the ARM machine.
echo.
echo Arguments:
echo ^<runtime path^> -- Path to corefx-built runtime "layout", e.g. _\fx\bin\testhost\netcoreapp-Windows_NT-Release-arm
echo ^<tests dir^> -- Path to corefx test tree, e.g., _\fx\bin\tests
echo ^<test exclusion file^> -- Path to test exclusion file, e.g., C:\coreclr\tests\arm\corefx_test_exclusions.txt
echo.
echo The ^<test exclusion file^> is a file with a list of assemblies for which the
echo tests should not be run. This allows excluding failing tests by excluding the
echo entire assembly in which they live. This obviously does not provide fine-grained
echo control, but is easy to implement. This file should be a list of assembly names,
echo without filename extension, one per line, e.g.:
echo.
echo     System.Console.Tests
echo     System.Data.SqlClient.Tests
echo     System.Diagnostics.Process.Tests
echo.
echo This script only works for Windows ARM, but perhaps should be extended to work
echo for Windows ARM64 as well.
goto :eof

:start
if "%3"=="" goto usage
if not "%4"=="" goto usage

set _runtime_path=%1
set _tests_dir=%2
set _exclusion_file=%3

echo Running CoreFX tests
echo Using runtime: %_runtime_path%
echo Using tests: %_tests_dir%
echo Using test exclusion file: %_exclusion_file%

set _pass=0
set _fail=0
set _skipped=0
set _total=0

pushd %_tests_dir%
for /F %%i in ('dir /s /b /A:D netcoreapp-Windows_NT-Release-arm') do (
    if exist %%i\RunTests.cmd call :one %%i
)
popd
echo COREFX TEST PASS: %_pass%, FAIL: %_fail%, SKIPPED: %_skipped%, TOTAL: %_total%
if %_fail% GTR 0 (
    exit /b 1
)
exit /b 0

:one
set /A _total=_total + 1

REM Extract out the test name from the path.
REM The path looks like: e:\gh\corefx\bin\tests\System.Management.Tests\netcoreapp-Windows_NT-Release-arm
REM From this, we want System.Management.Tests to compare against the exclusion file, which should be a list
REM of test names to skip.

set _t1=%1
set _t2=%_t1:\netcoreapp-Windows_NT-Release-arm=%
for /F %%j in ("%_t2%") do set _t3=%%~nxj
findstr /i %_t3% %_exclusion_file% >nul
if %errorlevel% EQU 0 (
    echo COREFX TEST %_t3% EXCLUDED
    set /A _skipped=_skipped + 1
) else (
    call :run %1\RunTests.cmd %_runtime_path%
)
goto :eof

:run
echo Running: %*
call %*
if %errorlevel% EQU 0 (
    set /A _pass=_pass + 1
    echo COREFX TEST PASSED
) else (
    set /A _fail=_fail + 1
    echo COREFX TEST FAILED
)
goto :eof
