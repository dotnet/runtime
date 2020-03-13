@echo off
setlocal

set BUILD_RESULT=1

:: Get path for current running script.
set RUN_WINSETUP_SCRIPT_PATH=%~dp0

:: Setup VS msbuild environment.
call %RUN_WINSETUP_SCRIPT_PATH%setup-vs-msbuild-env.bat

call "msbuild.exe" /t:RunWinConfigSetup %RUN_WINSETUP_SCRIPT_PATH%mono.winconfig.targets && (
    set BUILD_RESULT=0
) || (
    set BUILD_RESULT=1
    if not %ERRORLEVEL% == 0 (
        set BUILD_RESULT=%ERRORLEVEL%
    )
)

exit /b %BUILD_RESULT%

@echo on
