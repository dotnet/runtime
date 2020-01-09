:: Set up VS MSVC environment when running MSVC build mono-sgen.exe with all supplied arguments.
:: Simplify the setup of VS and MSVC toolchain, when running Mono AOT compiler
:: since it need to locate correct compiler and OS libraries as well as clang.exe and link.exe
:: from VS setup for the corresponding architecture.

@echo off
setlocal

set EXECUTE_RESULT=1

:: Get path for current running script.
set RUN_MONO_SGEN_MSVC_SCRIPT_PATH=%~dp0

:: Optimization, check if we need to setup full build environment, only needed when running mono-sgen.exe as AOT compiler.
if "%MONO_AS_AOT_COMPILER%" == "1" (
    goto SETUP_VS_ENV
)

set MONO_AS_AOT_COMPILER=0
:: Look for --aot or --aot=, --aot-path alone should not trigger setup of VS env.
echo.%* | findstr /r /c:".*--aot[^-a-zA-Z0-9].*" > nul && (
    set MONO_AS_AOT_COMPILER=1
)

if %MONO_AS_AOT_COMPILER% == 1 (
    goto SETUP_VS_ENV
)

:: mono-sgen.exe not invoked as a AOT compiler, no need to setup full build environment.
goto ON_EXECUTE

:: Try setting up VS MSVC build environment.
:SETUP_VS_ENV

:: Optimization, check if we have something that looks like a VS MSVC build environment
:: already available.
if /i not "%VCINSTALLDIR%" == "" (
    if /i not "%INCLUDE%" == "" (
        if /i not "%LIB%" == "" (
            goto ON_EXECUTE
        )
    )
)

:: Setup Windows environment.
call %RUN_MONO_SGEN_MSVC_SCRIPT_PATH%setup-windows-env.bat

if "%MONO_VS_MSVCBUILD_ENV_FILE%" == "" (
    set MONO_VS_MSVCBUILD_ENV_FILE=%RUN_MONO_SGEN_MSVC_SCRIPT_PATH%mono-sgen.exe.env
)

:: Check import of VS MSVC build environment using a file instead of running all commands.
:: NOTE, this is an optimization since setting up a development command
:: prompt could take some time.
if /i "%MONO_IMPORT_VS_MSVCBUILD_ENV_FILE%" == "true" (
    if exist "%MONO_VS_MSVCBUILD_ENV_FILE%" (
        for /f "delims=" %%a in (%MONO_VS_MSVCBUILD_ENV_FILE%) do SET %%a
    )
)

if not "%MONO_MSVC_PATH%" == "" (
    set "PATH=%MONO_MSVC_PATH%;%PATH%"
    goto ON_EXECUTE
)

:: Setup VS MSVC build environment.
set TEMP_PATH=%PATH%
call %RUN_MONO_SGEN_MSVC_SCRIPT_PATH%setup-vs-msvcbuild-env.bat
call set MONO_MSVC_PATH=%%PATH:%TEMP_PATH%=%%

:: Check if msvc env should be exported into file for later import.
for /f %%a in ('uuidgen.exe') do set NEW_UUID=%%a
if /i "%MONO_EXPORT_VS_MSVCBUILD_ENV_FILE%" == "true" (
    SET VCINSTALLDIR >> "%TEMP%\%NEW_UUID%.env"
    SET INCLUDE >> "%TEMP%\%NEW_UUID%.env"
    SET LIB >> "%TEMP%\%NEW_UUID%.env"
    SET MONO_MSVC_PATH >> "%TEMP%\%NEW_UUID%.env"
    move /Y "%TEMP%\%NEW_UUID%.env" "%MONO_VS_MSVCBUILD_ENV_FILE%" >nul 2>&1
)

:ON_EXECUTE

:: Add mono.sgen.exe (needed for optional LLVM tooling) to PATH
set "PATH=%RUN_MONO_SGEN_MSVC_SCRIPT_PATH%;%PATH%"

call "%RUN_MONO_SGEN_MSVC_SCRIPT_PATH%mono-sgen.exe" %* && (
    set EXECUTE_RESULT=0
) || (
    set EXECUTE_RESULT=1
    if not %ERRORLEVEL% == 0 (
        set EXECUTE_RESULT=%ERRORLEVEL%
    )
)

exit /b %EXECUTE_RESULT%

@echo on