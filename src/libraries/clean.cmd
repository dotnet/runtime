@if not defined _echo @echo off
setlocal EnableDelayedExpansion

set NO_DASHES_ARG=%1
if /I [%NO_DASHES_ARG:-=%] == [?] goto Usage
if /I [%NO_DASHES_ARG:-=%] == [h] goto Usage

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
echo Repository cleaning script.
echo.
echo Options:
echo     -b     - Deletes the binary output directory.
echo     -p     - Deletes the repo-local nuget package directory.
echo     -c     - Deletes the user-local nuget package cache.
echo     -all   - Combines all of the above.
echo.
echo If no option is specified then clean.cmd -b is implied.

exit /b 1
