@if not defined _echo @echo off
setlocal EnableDelayedExpansion

set NO_DASHES_ARG=%1
if not defined NO_DASHES_ARG goto no_help
if /I [%NO_DASHES_ARG:-=%] == [?] goto Usage
if /I [%NO_DASHES_ARG:-=%] == [h] goto Usage

:no_help
:: Check if VBCSCompiler.exe is running
tasklist /fi "imagename eq VBCSCompiler.exe" |find ":" > nul
:: Compiler is running if errorlevel == 1
if errorlevel 1 (
	echo Stop VBCSCompiler.exe execution.
	for /f "tokens=2 delims=," %%F in ('tasklist /nh /fi "imagename eq VBCSCompiler.exe" /fo csv') do taskkill /f /PID %%~F
)

:: Strip all dashes off the argument and use invariant
:: compare to match as many versions of "all" that we can
:: All other argument validation happens inside Run.exe
if not defined NO_DASHES_ARG goto no_args
if /I [%NO_DASHES_ARG:-=%] == [all] (
  echo Cleaning entire working directory ...
  call git clean -xdf
  exit /b !ERRORLEVEL!
)

:no_args
if [%1]==[] set __args=-b
call %~dp0run.cmd clean %__args% %*
exit /b %ERRORLEVEL%

:Usage
echo.
echo Usage: clean [-b] [-p] [-c] [-all]
echo Repository cleaning script.
echo Options:
echo     -b     - Delete the binary output directory.
echo     -p     - Delete the repo-local NuGet package directory.
echo     -c     - Deletes the user-local NuGet package cache.
echo     -all   - Cleans repository and restores it to pristine state.
echo.
echo ^If no option is specified then "clean -b" is implied.
exit /b