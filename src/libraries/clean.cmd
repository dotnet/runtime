@if "%_echo%" neq "on" echo off
setlocal EnableDelayedExpansion

echo Stop VBCSCompiler.exe execution.
for /f "tokens=2 delims=," %%F in ('tasklist /nh /fi "imagename eq VBCSCompiler.exe" /fo csv') do taskkill /f /PID %%~F

if [%1] == [-all] (
  echo Cleaning entire working directory ...
  call git clean -xdf
  exit /b !ERRORLEVEL!
)

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