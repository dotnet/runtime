@echo off
setlocal EnableDelayedExpansion

rem *.cmd and *.sh files may be considered test entry points. If launched directly, consider it a pass.
if "%~1" neq "--runCustomTest" exit /b 0

set CLRTestExpectedExitCode=100

echo Collect profile without R2R, use profile without R2R
del /f /q profile.mcj 2>nul
call "%CORE_ROOT%\corerun" BasicTestWithMcj.dll
set CLRTestExitCode=!ErrorLevel!
if %CLRTestExitCode% neq %CLRTestExpectedExitCode% exit /b %CLRTestExitCode%
call "%CORE_ROOT%\corerun" BasicTestWithMcj.dll
set CLRTestExitCode=!ErrorLevel!
if %CLRTestExitCode% neq %CLRTestExpectedExitCode% exit /b %CLRTestExitCode%

echo Collect profile with R2R, use profile with R2R
del /f /q profile.mcj 2>nul
call "%CORE_ROOT%\corerun" r2r\BasicTestWithMcj.dll
set CLRTestExitCode=!ErrorLevel!
if %CLRTestExitCode% neq %CLRTestExpectedExitCode% exit /b %CLRTestExitCode%
call "%CORE_ROOT%\corerun" r2r\BasicTestWithMcj.dll
set CLRTestExitCode=!ErrorLevel!
if %CLRTestExitCode% neq %CLRTestExpectedExitCode% exit /b %CLRTestExitCode%

echo Collect profile without R2R, use profile with R2R
del /f /q profile.mcj 2>nul
call "%CORE_ROOT%\corerun" BasicTestWithMcj.dll
set CLRTestExitCode=!ErrorLevel!
if %CLRTestExitCode% neq %CLRTestExpectedExitCode% exit /b %CLRTestExitCode%
call "%CORE_ROOT%\corerun" r2r\BasicTestWithMcj.dll
set CLRTestExitCode=!ErrorLevel!
if %CLRTestExitCode% neq %CLRTestExpectedExitCode% exit /b %CLRTestExitCode%

echo Collect profile with R2R, use profile without R2R
del /f /q profile.mcj 2>nul
call "%CORE_ROOT%\corerun" r2r\BasicTestWithMcj.dll
set CLRTestExitCode=!ErrorLevel!
if %CLRTestExitCode% neq %CLRTestExpectedExitCode% exit /b %CLRTestExitCode%
call "%CORE_ROOT%\corerun" BasicTestWithMcj.dll
set CLRTestExitCode=!ErrorLevel!
if %CLRTestExitCode% neq %CLRTestExpectedExitCode% exit /b %CLRTestExitCode%

echo Collect profile with R2R disabled, use profile with R2R enabled
del /f /q profile.mcj 2>nul
set COMPlus_ReadyToRun=0
call "%CORE_ROOT%\corerun" r2r\BasicTestWithMcj.dll
set CLRTestExitCode=!ErrorLevel!
set COMPlus_ReadyToRun=
if %CLRTestExitCode% neq %CLRTestExpectedExitCode% exit /b %CLRTestExitCode%
call "%CORE_ROOT%\corerun" r2r\BasicTestWithMcj.dll
set CLRTestExitCode=!ErrorLevel!
if %CLRTestExitCode% neq %CLRTestExpectedExitCode% exit /b %CLRTestExitCode%

del /f /q profile.mcj 2>nul
exit /b %CLRTestExpectedExitCode%

endlocal
