:: Script will setup environment variables directly in callers environment.

:: If we are running from none Windows shell we will need to restore a clean PATH
:: before setting up VS MSVC build environment. If not there is a risk we will pick up
:: for example cygwin binaries when running toolchain commands not explicitly setup by
:: VS MSVC build environment.
set HKCU_ENV_PATH=
set HKLM_ENV_PATH=
if "%SHELL%" == "/bin/bash" (
    for /f "tokens=2,*" %%a in ('%WINDIR%\System32\reg.exe query "HKCU\Environment" /v "Path" ^| %WINDIR%\System32\find.exe /i "REG_"') do (
        SET HKCU_ENV_PATH=%%b
    )
    for /f "tokens=2,*" %%a in ('%WINDIR%\System32\reg.exe query "HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Environment" /v "Path" ^| %WINDIR%\System32\find.exe /i "REG_"') do (
        SET HKLM_ENV_PATH=%%b
    )
)

:: Restore default path, if we are running from none Windows shell.
if "%SHELL%" == "/bin/bash" (
    call :restore_default_path "%HKCU_ENV_PATH%" "%HKLM_ENV_PATH%"
)

:: There is still a scenario where the default path can include cygwin\bin folder. If that's the case
:: there is still a big risk that build tools will be incorrectly resolved towards cygwin bin folder.
:: Make sure to adjust path and drop all cygwin paths.
set NEW_PATH=
call where /Q "cygpath.exe" && (
    echo Warning, PATH includes cygwin bin folders. This can cause build errors due to incorrectly
    echo located build tools. Build script will drop all cygwin folders from used PATH.
    for %%a in ("%PATH:;=";"%") do (
        if not exist "%%~a\cygpath.exe" (
            call :add_to_new_path "%%~a"
        )
    )
)

if not "%NEW_PATH%" == "" (
    set "PATH=%NEW_PATH%"
)

exit /b 0

:restore_default_path

:: Restore default PATH.
if not "%~2" == "" (
    if not "%~1" == "" (
        set "PATH=%~2;%~1"
    ) else (
        set "PATH=%~2"
    )
)

goto :EOF

:add_to_new_path

if "%NEW_PATH%" == "" (
    set "NEW_PATH=%~1"
) else (
    SET "NEW_PATH=%NEW_PATH%;%~1"
)

goto :EOF