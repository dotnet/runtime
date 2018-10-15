@echo off
setlocal

set TEMP_PATH=%PATH%
set BUILD_RESULT=1

set CMAKE_BIN_NAME=cmake.exe
set GIT_BIN_NAME=git.exe

:: Arguments
:: --------------------------------------------
:: %1 Mono source root directory.
:: %2 Mono build root directory.
:: %3 Mono distribution root directory.
:: %4 VS CFLAGS.
:: %5 VS platform (Win32/x64)
:: %6 VS configuration (Debug/Release)
:: %7 VS target
:: %8 MsBuild bin path, if used.

set MONO_DIR=%~1
set MONO_BUILD_DIR=%~2
set MONO_DIST_DIR=%~3
set VS_CFLAGS=%~4
set VS_PLATFORM=%~5
set VS_CONFIGURATION=%~6
set VS_TARGET=%~7
set MSBUILD_BIN_PATH=%~8

:: Setup toolchain.
:: set CMAKE=
:: set GIT=
set MSBUILD=%MSBUILD_BIN_PATH%msbuild.exe

if "%MONO_DIR%" == "" (
    echo Missing mono source directory argument.
    goto ECHO_USAGE
)

if "%MONO_BUILD_DIR%" == "" (
    echo Missing mono build directory argument.
    goto ECHO_USAGE
)

if "%MONO_DIST_DIR%" == "" (
    echo Missing mono install directory argument.
    goto ECHO_USAGE
)

if "%VS_CFLAGS%" == "" (
    echo Missing CFLAGS argument.
    goto ECHO_USAGE
)

if "%VS_PLATFORM%" == "" (
    set VS_PLATFORM=x64
)

if "%VS_CONFIGURATION%" == "" (
    set VS_CONFIGURATION=Release
)

if "%VS_TARGET%" == "" (
    set VS_TARGET=Build
)

if not exist %MONO_DIR% (
    echo Could not find "%MONO_DIR%".
    goto ON_ERROR
)

if not exist %MONO_DIST_DIR% (
    echo Could not find "%MONO_DIST_DIR%".
    goto ON_ERROR
)

set BTLS_ROOT_PATH=%MONO_DIR%\external\boringssl
if not exist %BTLS_ROOT_PATH% (
    echo Could not find "%BTLS_ROOT_PATH%".
    goto ON_ERROR
)

set MONO_BTLS_ROOT_PATH=%MONO_DIR%\mono\btls
if not exist %MONO_BTLS_ROOT_PATH% (
    echo Could not find "%MONO_BTLS_ROOT_PATH%".
    goto ON_ERROR
)

set BTLS_CFLAGS=%VS_CFLAGS%
set BTLS_ARCH=x86_64
if /i "%VS_PLATFORM%" == "win32" (
    set BTLS_ARCH=i386
)

set BTLS_BUILD_DIR=%MONO_BUILD_DIR%

:: Check target.
if /i "%VS_TARGET%" == "build" (
    goto ON_BUILD_BTLS
)

if /i "%VS_TARGET%" == "install" (
    goto ON_INSTALL_BTLS
)

if /i "%VS_TARGET%" == "clean" (
    goto ON_CLEAN_BTLS
)

:ON_BUILD_BTLS

:: If not set by caller, check environment for working git.exe.
if not exist "%GIT%" (
    call where /q %GIT_BIN_NAME% && (
        echo Using %GIT_BIN_NAME% available in PATH:
        where %GIT_BIN_NAME%
        set GIT=%GIT_BIN_NAME%
    ) || (
        echo Failed to located working %GIT_BIN_NAME%, needs to be accessible in PATH or set using GIT environment variable.
        goto ON_ERROR
    )
)

:: Make sure boringssl submodule is up to date.
pushd
cd %MONO_BTLS_ROOT_PATH%
"%GIT%" submodule update --init
if not ERRORLEVEL == 0 (
   "%GIT%" submodule init
    "%GIT%" submodule update
    if not ERRORLEVEL == 0 (
        echo Git boringssl submodules failed to updated. You may experience compilation problems if some submodules are out of date.
    )
)
popd

:: If not set by caller, check environment for working cmake.exe.
if not exist "%CMAKE%" (
    call where /q %CMAKE_BIN_NAME% && (
        echo Using %CMAKE_BIN_NAME% available in PATH:
        where %CMAKE_BIN_NAME%
        set CMAKE=%CMAKE_BIN_NAME%
    ) || (
        echo Failed to located working %CMAKE_BIN_NAME%, needs to be accessible in PATH or set using CMAKE environment variable.
        goto ON_ERROR
    )
)


:: TODO, check installed yasm, needed to do btls build with assembler support.
:: TODO, check installed ninja, needed to do btls build with assembler support.

:: Detect VS version to use right cmake generator.
set CMAKE_GENERATOR=Visual Studio 14 2015
if "%VisualStudioVersion%" == "15.0" (
    set CMAKE_GENERATOR=Visual Studio 15 2017
)

if /i "%VS_PLATFORM%" == "x64" (
    set CMAKE_GENERATOR=%CMAKE_GENERATOR% Win64
)

if not exist "%BTLS_BUILD_DIR%" (
    mkdir "%BTLS_BUILD_DIR%"
)

cd "%BTLS_BUILD_DIR%"

: Run cmake.
"%CMAKE%" ^
-D BTLS_ROOT:PATH="%BTLS_ROOT_PATH%" ^
-D SRC_DIR:PATH="%MONO_BTLS_ROOT_PATH%" ^
-D BTLS_CFLAGS="%BTLS_CFLAGS%" ^
-D OPENSSL_NO_ASM=1 ^
-D BTLS_ARCH="%BTLS_ARCH%" ^
-D BUILD_SHARED_LIBS=1 ^
-D CMAKE_BUILD_TYPE=%VS_CONFIGURATION% ^
-G "%CMAKE_GENERATOR%" ^
"%MONO_BTLS_ROOT_PATH%"

if not ERRORLEVEL == 0 (
    goto ON_ERROR
)

: Build BTLS.
call "%MSBUILD%" mono-btls.sln /p:Configuration=%VS_CONFIGURATION% /p:Platform=%VS_PLATFORM% /t:%VS_TARGET% || (
    goto ON_ERROR
)

:ON_INSTALL_BTLS

if not exist "%VS_CONFIGURATION%\libmono-btls-shared.dll" (
    echo Missing btls build output, "%VS_CONFIGURATION%\libmono-btls-shared.dll"
    goto ON_ERROR
)

: Copy files into distribution directory.
copy "%VS_CONFIGURATION%\libmono-btls-shared.dll" "%MONO_DIST_DIR%"

if exist "%VS_CONFIGURATION%\libmono-btls-shared.pdb" (
    copy "%VS_CONFIGURATION%\libmono-btls-shared.pdb" "%MONO_DIST_DIR%"
)

goto ON_SUCCESS

:ON_CLEAN_BTLS

if exist "%BTLS_BUILD_DIR%\mono-btls.sln" (
    "%MSBUILD%" "%BTLS_BUILD_DIR%\mono-btls.sln" /p:Configuration=%VS_CONFIGURATION% /p:Platform=%VS_PLATFORM% /t:Clean
)

goto ON_SUCCESS

:ON_SUCCESS

set BUILD_RESULT=0
goto ON_EXIT

:ECHO_USAGE:
    ECHO Usage: build-btls.bat [mono_src_dir] [mono_build_dir] [mono_dist_dir] [vs_cflags] [vs_plaform] [vs_configuration].

:ON_ERROR
	echo Failed to build BTLS.
	goto ON_EXIT

:ON_EXIT
	set PATH=%TEMP_PATH%
	exit /b %BUILD_RESULT%

@echo on