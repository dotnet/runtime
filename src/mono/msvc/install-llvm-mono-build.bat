:: --------------------------------------------------
:: Install needed LLVM binaries from LLVM install directory
:: into Mono build output directory.
::
:: %1 LLVM install root directory (internal or external LLVM build).
:: %2 Mono distribution root directory.
:: --------------------------------------------------

@echo off
setlocal

set BUILD_RESULT=1

set LLVM_INSTALL_DIR=%~1
shift
set MONO_DIST_DIR=%~1
shift

if "%LLVM_INSTALL_DIR%" == "" (
    echo Missing LLVM install directory argument.
    goto ECHO_USAGE
)

if "%MONO_DIST_DIR%" == "" (
    echo Missing Mono dist directory argument.
    goto ECHO_USAGE
)

if not exist "%LLVM_INSTALL_DIR%\bin\opt.exe" (
    echo Missing LLVM build output, "%LLVM_INSTALL_DIR%\bin\opt.exe"
    goto ON_ERROR
)

if not exist "%LLVM_INSTALL_DIR%\bin\llc.exe" (
    echo Missing LLVM build output, "%LLVM_INSTALL_DIR%\bin\llc.exe"
    goto ON_ERROR
)

if not exist "%LLVM_INSTALL_DIR%\bin\llvm-dis.exe" (
    echo Missing LLVM build output, "%LLVM_INSTALL_DIR%\bin\llvm-dis.exe"
    goto ON_ERROR
)

if not exist "%LLVM_INSTALL_DIR%\bin\llvm-mc.exe" (
    echo Missing LLVM build output, "%LLVM_INSTALL_DIR%\bin\llvm-mc.exe"
    goto ON_ERROR
)

if not exist "%LLVM_INSTALL_DIR%\bin\llvm-as.exe" (
    echo Missing LLVM build output, "%LLVM_INSTALL_DIR%\bin\llvm-as.exe"
    goto ON_ERROR
)

copy /Y "%LLVM_INSTALL_DIR%\bin\opt.exe" "%MONO_DIST_DIR%" >nul 2>&1
copy /Y "%LLVM_INSTALL_DIR%\bin\llc.exe" "%MONO_DIST_DIR%" >nul 2>&1
copy /Y "%LLVM_INSTALL_DIR%\bin\llvm-dis.exe" "%MONO_DIST_DIR%" >nul 2>&1
copy /Y "%LLVM_INSTALL_DIR%\bin\llvm-mc.exe" "%MONO_DIST_DIR%" >nul 2>&1
copy /Y "%LLVM_INSTALL_DIR%\bin\llvm-as.exe" "%MONO_DIST_DIR%" >nul 2>&1

goto ON_SUCCESS

:ON_SUCCESS

set BUILD_RESULT=0
goto ON_EXIT

:ECHO_USAGE:
    ECHO Usage: install-llvm-mono-build.bat [llvm_install_dir] [mono_dist_dir].

:ON_ERROR
    echo Failed to install LLVM binaries into Mono build output directory.
    goto ON_EXIT

:ON_EXIT
    exit /b %BUILD_RESULT%

@echo on
