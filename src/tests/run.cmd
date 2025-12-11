@if not defined _echo @echo off
setlocal EnableDelayedExpansion EnableExtensions

:: Define a prefix for most output progress messages that come from this script. That makes
:: it easier to see where these are coming from. Note that there is a trailing space here.
set "__MsgPrefix=RUNTEST: "

set "__RepoRootDir=%~dp0..\.."
:: normalize
for %%i in ("%__RepoRootDir%") do set "__RepoRootDir=%%~fi"

REM Find python and set it to the variable PYTHON
set _C=-c "import sys; sys.stdout.write(sys.executable)"
(py -3 %_C% || py -2 %_C% || python3 %_C% || python2 %_C% || python %_C%) > %TEMP%\pythonlocation.txt 2> NUL
set _C=
set /p PYTHON=<%TEMP%\pythonlocation.txt

if NOT DEFINED PYTHON (
    echo %__MsgPrefix%Error: Could not find a Python installation.
    exit /b 1
)

REM Run the tests using cross platform run.py
REM All argument parsing and processing is now done in run.py
"%PYTHON%" "%__RepoRootDir%\src\tests\run.py" %*

exit /b %ERRORLEVEL%
