:: Set up VS MSVC environment when running MSVC build mono-sgen.exe with all supplied arguments.
:: Simplify the setup of VS and MSVC toolchain, when running Mono AOT compiler
:: since it need to locate correct compiler and OS libraries as well as clang.exe and link.exe
:: from VS setup for the corresponding architecture.

@echo off
setlocal

set EXECUTE_RESULT=1

:: Get path for current running script.
set RUN_MONO_SGEN_MSVC_SCRIPT_PATH=%~dp0

set MONO_AS_AOT_COMPILER=0

:: Optimization, check if we need to setup full build environment, only needed when running mono-sgen.exe as AOT compiler.
echo.%* | findstr /c:"--aot=" > nul && (
    set MONO_AS_AOT_COMPILER=1
)

if %MONO_AS_AOT_COMPILER% == 1 (
    goto SETUP_VS_ENV
)

:: mono-sgen.exe not invoked as a AOT compiler, no need to setup full build environment.
goto ON_EXECUTE

:: Try setting up VS MSVC build environment.
:SETUP_VS_ENV

:: Optimization, check if we have something that looks like a MSVC build environment already available.
if /i not "%VCINSTALLDIR%" == "" (
    if /i not "%INCLUDE%" == "" (
        if /i not "%LIB%" == "" (
            goto ON_EXECUTE
        )
    )
)

:: Setup Windows environment.
call %RUN_MONO_SGEN_MSVC_SCRIPT_PATH%setup-windows-env.bat

:: Setup VS MSVC build environment.
call %RUN_MONO_SGEN_MSVC_SCRIPT_PATH%setup-vs-msvcbuild-env.bat

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