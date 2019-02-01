:: Set up VS MSVC environment when running MSVC build mono-sgen.exe with all supplied arguments.
:: Simplify the setup of VS and MSVC toolchain, when running Mono AOT compiler
:: since it need to locate correct compiler and OS libraries as well as clang.exe and link.exe
:: from VS setup for the corresponding architecture.

@echo off

setlocal

set EXECUTE_RESULT=1

:: Make sure we can restore current working directory after setting up environment.
:: Some of the VS scripts can change the current working directory.
set CALLER_WD=%CD%

:: Get path for current running script.
set RUN_MONO_SGEN_MSVC_SCRIPT_PATH=%~dp0

:: If we are running from none Windows shell we will need to restore a clean PATH
:: before setting up VS MSVC build environment. If not there is a risk we will pick up
:: for example cygwin binaries when running toolchain commands not explicitly setup by VS MSVC build environment.
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

:: NOTE, MSVC build mono-sgen.exe AOT compiler currently support 64-bit AMD codegen. Below will only setup
:: amd64 versions of VS MSVC build environment and corresponding ClangC2 compiler.

set VS_2015_TOOLCHAIN_ARCH=amd64
set VS_2015_VCVARS_ARCH=%VS_2015_TOOLCHAIN_ARCH%\vcvars64.bat
set VS_2015_CLANGC2_ARCH=%VS_2015_TOOLCHAIN_ARCH%
set VS_2017_VCVARS_ARCH=vcvars64.bat
set VS_2017_CLANGC2_ARCH=HostX64

:: 32-bit AOT toolchains for MSVC build mono-sgen.exe is currently not supported.
:: set VS_2015_TOOLCHAIN_ARCH=x86
:: set VS_2015_VCVARS_ARCH=vcvars32.bat
:: set VS_2015_CLANGC2_ARCH=%VS_2015_TOOLCHAIN_ARCH%
:: set VS_2017_VCVARS_ARCH=vcvars32.bat
:: set VS_2017_CLANGC2_ARCH=HostX86

set MONO_AS_AOT_COMPILER=0
set VS_CLANGC2_TOOLS_BIN_PATH=

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

:: Visual Studio 2015 == 14.0
if "%VisualStudioVersion%" == "14.0" (
    goto SETUP_VS_2015
)

:: Visual Studio 2017 == 15.0
if "%VisualStudioVersion%" == "15.0" (
    goto SETUP_VS_2017
)

:SETUP_VS_2015

set VS_2015_VCINSTALL_DIR=%ProgramFiles(x86)%\Microsoft Visual Studio 14.0\VC\

:: Try to locate installed VS2015 Clang/C2.
SET VS_2015_CLANGC2_TOOLS_BIN_PATH=%VS_2015_VCINSTALL_DIR%ClangC2\bin\%VS_2015_CLANGC2_ARCH%\
SET VS_2015_CLANGC2_TOOLS_BIN=%VS_2015_CLANGC2_TOOLS_BIN_PATH%clang.exe

if not exist "%VS_2015_CLANGC2_TOOLS_BIN%" (
    goto SETUP_VS_2017
)

:SETUP_VS_2015_BUILD_TOOLS

:: Try to locate VS2015 build tools installation.
set VS_2015_BUILD_TOOLS_INSTALL_DIR=%ProgramFiles(x86)%\Microsoft Visual C++ Build Tools\
set VS_2015_BUILD_TOOLS_CMD=%VS_2015_BUILD_TOOLS_INSTALL_DIR%vcbuildtools.bat

:: Setup VS2015 VC development environment using build tools installation.
call :setup_build_env "%VS_2015_BUILD_TOOLS_CMD%" "%VS_2015_TOOLCHAIN_ARCH%" "%CALLER_WD%" && (
    set "VS_CLANGC2_TOOLS_BIN_PATH=%VS_2015_CLANGC2_TOOLS_BIN_PATH%"
    goto ON_EXECUTE
)

:SETUP_VS_2015_VC

:: Try to locate installed VS2015 VC environment.
set VS_2015_DEV_CMD=%VS_2015_VCINSTALL_DIR%bin\%VS_2015_VCVARS_ARCH%

call :setup_build_env "%VS_2015_DEV_CMD%" "" "%CALLER_WD%" && (
    set "VS_CLANGC2_TOOLS_BIN_PATH=%VS_2015_CLANGC2_TOOLS_BIN_PATH%"
    goto ON_EXECUTE
)

:SETUP_VS_2017

:: VS2017 includes vswhere.exe that can be used to locate current VS2017 installation.
set VSWHERE_TOOLS_BIN=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe
set VS_2017_VCINSTALL_DIR=

:: Try to locate installed VS2017 VC environment.
if exist "%VSWHERE_TOOLS_BIN%" (
    for /f "tokens=*" %%a in ('"%VSWHERE_TOOLS_BIN%" -latest -property installationPath') do (
        set VS_2017_VCINSTALL_DIR=%%a\VC\
    )
)

:: Try to locate installed VS2017 Clang/C2.
SET VS_2017_CLANGC2_VERSION_FILE=%VS_2017_VCINSTALL_DIR%Auxiliary/Build/Microsoft.ClangC2Version.default.txt
if not exist "%VS_2017_CLANGC2_VERSION_FILE%" (
	goto ON_ENV_ERROR
)

set /p VS_2017_CLANGC2_VERSION=<"%VS_2017_CLANGC2_VERSION_FILE%"
set VS_2017_CLANGC2_TOOLS_BIN_PATH=%VS_2017_VCINSTALL_DIR%Tools\ClangC2\%VS_2017_CLANGC2_VERSION%\bin\%VS_2017_CLANGC2_ARCH%\
set VS_2017_CLANGC2_TOOLS_BIN=%VS_2017_CLANGC2_TOOLS_BIN_PATH%clang.exe
if not exist "%VS_2017_CLANGC2_TOOLS_BIN%" (
	goto ON_ENV_ERROR
)

:SETUP_VS_2017_BUILD_TOOLS

:: Try to locate VS2017 build tools installation.
set VS_2017_BUILD_TOOLS_INSTALL_DIR=%ProgramFiles(x86)%\Microsoft Visual Studio\2017\BuildTools\
set VS_2017_BUILD_TOOLS_CMD=%VS_2017_BUILD_TOOLS_INSTALL_DIR%VC\Auxiliary\Build\%VS_2017_VCVARS_ARCH%

:: Setup VS2017 VC development environment using build tools installation.
call :setup_build_env "%VS_2017_BUILD_TOOLS_CMD%" "" "%CALLER_WD%" && (
    set "VS_CLANGC2_TOOLS_BIN_PATH=%VS_2017_CLANGC2_TOOLS_BIN_PATH%"
    goto ON_EXECUTE
)

:SETUP_VS_2017_VC

:: Try to locate installed VS2017 VC environment.
set VS_2017_DEV_CMD=%VS_2017_VCINSTALL_DIR%Auxiliary\Build\%VS_2017_VCVARS_ARCH%

:: Setup VS2017 VC development environment using VS installation.
call :setup_build_env "%VS_2017_DEV_CMD%" "" "%CALLER_WD%" && (
    set "VS_CLANGC2_TOOLS_BIN_PATH=%VS_2017_CLANGC2_TOOLS_BIN_PATH%"
    goto ON_EXECUTE
)

:ON_ENV_ERROR

echo Warning, failed to setup build environment needed by MSVC build mono-sgen.exe running as an AOT compiler.
echo Incomplete build environment can cause AOT compiler build due to missing compiler, linker and platform libraries.

:ON_EXECUTE

:: Add mono.sgen.exe (needed for optional LLVM tooling) and ClangC2 folders to PATH
set "PATH=%RUN_MONO_SGEN_MSVC_SCRIPT_PATH%;%VS_CLANGC2_TOOLS_BIN_PATH%;%PATH%"

call "%RUN_MONO_SGEN_MSVC_SCRIPT_PATH%mono-sgen.exe" %* && (
    set EXCEUTE_RESULT=0
) || (
    set EXCEUTE_RESULT=1
    if not %ERRORLEVEL% == 0 (
        set EXCEUTE_RESULT=%ERRORLEVEL%
    )
)

exit /b %EXCEUTE_RESULT%

:setup_build_env

:: Check if VS build environment script exists.
if not exist "%~1" (
    goto setup_build_env_error
)

:: Run VS build environment script.
call "%~1" %~2 > NUL

:: Restore callers working directory in case it has been changed by VS scripts.
cd /d "%~3"

goto setup_build_env_exit

:setup_build_env_error
exit /b 1

:setup_build_env_exit
goto :EOF

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

@echo on
