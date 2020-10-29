:: This script is intended for our ICU tests
:: the reason why we need to do this is because we build the tests
:: in one version of Windows and then send the tests to helix to
:: multiple Windows SKUs, including SKUs that doesn't contain ICU,
:: since the globalization mode is calculated very early in the process
:: we can't enable running ICU at all in this SKUs and then skip the tests
:: at runtime, because xunit will start and then blow up when trying to load ICU.
:: So with this script, we check if icu.dll is not present on the system, then we
:: fallback to run on NLS mode.

@echo off
setlocal ENABLEEXTENSIONS
setlocal ENABLEDELAYEDEXPANSION

if EXIST %WINDIR%\system32\icu.dll (
    echo ICU exists in system, running tests with ICU.
    goto done
)

echo icu was not found, so running the tests using NLS.

for %%a in (*.Tests.runtimeconfig.json) do (
    echo Updating nls switch in %%a to true...
    for /f "delims=^ tokens=1" %%i in (%%a) do (
        :: Check if current line is the nls switch
        set str=%%i
        echo.!str! | findstr /C:"System.Globalization.UseNls" 1>nul
        if "!errorlevel!" == "0" (
            echo       "System.Globalization.UseNls": true>>%%a.new
        ) else (
            echo %%i>>%%a.new
        )
    )
    move /y %%a %%a.bak > nul
    ren %%a.new %%a
    del %%a.bak
)

:done
exit /b 0
