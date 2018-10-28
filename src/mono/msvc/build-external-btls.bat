:: --------------------------------------------------
:: Run full BTLS build using msvc toolchain and available cmake generator.
:: Script needs to be run from within a matching build environment, x86|x64.
:: When executed from withing Visual Studio build environment current
:: build environment will be inherited by script.
::
:: %1 Mono source root directory.
:: %2 Mono build root directory.
:: %3 Mono distribution root directory.
:: %4 VS CFLAGS.
:: %5 VS platform (Win32/x64)
:: %6 VS configuration (Debug/Release)
:: %7 VS target
:: %8 MsBuild bin path, if used.
:: --------------------------------------------------

@echo off
setlocal

set TEMP_PATH=%PATH%
set BUILD_RESULT=1

set CL_BIN_NAME=cl.exe
set LINK_BIN_NAME=link.exe
set GIT_BIN_NAME=git.exe
set CMAKE_BIN_NAME=cmake.exe
set NINJA_BIN_NAME=ninja.exe
set PERL_BIN_NAME=perl.exe
set YASM_BIN_NAME=yasm.exe

set MONO_DIR=%~1
set MONO_BUILD_DIR=%~2
set MONO_DIST_DIR=%~3
set VS_CFLAGS=%~4
set VS_PLATFORM=%~5
set VS_CONFIGURATION=%~6
set VS_TARGET=%~7
set MSBUILD_BIN_PATH=%~8

:: Setup toolchain.
:: set GIT=
:: set CMAKE=
:: set NINJA=
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

set BTLS_NO_ASM_SUPPORT=1
set BTLS_BUILD_DIR=%MONO_BUILD_DIR%

:: Check if executed from VS2015/VS2017 build environment.
if "%VisualStudioVersion%" == "14.0" (
    goto ON_ENV_OK
)

if "%VisualStudioVersion%" == "15.0" (
    goto ON_ENV_OK
)

:: Executed outside VS2015/VS2017 build environment, try to locate Visual Studio C/C++ compiler and linker.
call :FIND_PROGRAM "" "%CL_BIN_NAME%" CL_PATH
if "%CL_PATH%" == "" (
    goto ON_ENV_WARNING
)

call :FIND_PROGRAM "" "%LINK_BIN_NAME%" LINK_PATH
if "%LINK_PATH%" == "" (
    goto ON_ENV_WARNING
)

goto ON_ENV_OK

:ON_ENV_WARNING

:: VS 2015.
set VC_VARS_ALL_FILE=%ProgramFiles(x86)%\Microsoft Visual Studio 14.0\VC\vcvarsall.bat
IF EXIST "%VC_VARS_ALL_FILE%" (
    echo For VS2015 builds, make sure to run this from within Visual Studio build or using "VS2015 x86|x64 Native Tools Command Prompt" command prompt.
	echo Setup a "VS2015 x86|x64 Native Tools Command Prompt" command prompt by using "%VC_VARS_ALL_FILE% x86|amd64".
)

:: VS 2017.
set VSWHERE_TOOLS_BIN=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe
if exist "%VSWHERE_TOOLS_BIN%" (
    echo For VS2017 builds, make sure to run this from within Visual Studio build or using "x86|x64 Native Tools Command Prompt for VS2017" command prompt.
	for /f "tokens=*" %%a IN ('"%VSWHERE_TOOLS_BIN%" -latest -property installationPath') do (
		echo Setup a "x86|x64 Native Tools Command Prompt for VS2017" command prompt by using "%%a\VC\Auxiliary\Build\vcvars32.bat|vcvars64.bat".
	)
)

echo Could not detect Visual Studio build environment. You may experience build problems if wrong toolchain is auto detected.

:ON_ENV_OK

:: Setup all cmake related generator, tools and variables.
call :SETUP_CMAKE_ENVIRONMENT
if "%CMAKE%" == "" (
    echo Failed to located working %CMAKE_BIN_NAME%, needs to be accessible in PATH or set using CMAKE environment variable.
    goto ON_ERROR
)

if "%CMAKE_GENERATOR%" == "" (
    echo Failed to setup cmake generator.
    goto ON_ERROR
)

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
call :FIND_PROGRAM "%GIT%" "%GIT_BIN_NAME%" GIT
if "%GIT%" == "" (
    echo Failed to located working %GIT_BIN_NAME%, needs to be accessible in PATH or set using GIT environment variable.
    goto ON_ERROR
)

:: Make sure boringssl submodule is up to date.
pushd
cd %MONO_BTLS_ROOT_PATH%
echo Updating boringssl submodule.
"%GIT%" submodule update --init
if not ERRORLEVEL == 0 (
   "%GIT%" submodule init
    "%GIT%" submodule update
    if not ERRORLEVEL == 0 (
        echo Git boringssl submodules failed to updated. You may experience compilation problems if some submodules are out of date.
    )
)
popd

if not exist "%BTLS_BUILD_DIR%" (
    mkdir "%BTLS_BUILD_DIR%"
)

cd "%BTLS_BUILD_DIR%"

:: Make sure cmake pick up msvc toolchain regardless of selected generator (Visual Studio|Ninja)
set CC=%CL_BIN_NAME%
set CXX=%CL_BIN_NAME%

set BTLS_BUILD_TYPE=
if /i "%CMAKE_GENERATOR%" == "ninja" (
    set BTLS_BUILD_TYPE=-D CMAKE_BUILD_TYPE=%VS_CONFIGURATION%
)

:: Run cmake.
"%CMAKE%" ^
-D BTLS_ROOT:PATH="%BTLS_ROOT_PATH%" ^
-D SRC_DIR:PATH="%MONO_BTLS_ROOT_PATH%" ^
-D BTLS_CFLAGS="%BTLS_CFLAGS%" ^
-D OPENSSL_NO_ASM=%BTLS_NO_ASM_SUPPORT% ^
-D BTLS_ARCH="%BTLS_ARCH%" ^
-D BUILD_SHARED_LIBS=1 ^
%BTLS_BUILD_TYPE% ^
-G "%CMAKE_GENERATOR%" ^
"%MONO_BTLS_ROOT_PATH%"

if not ERRORLEVEL == 0 (
    goto ON_ERROR
)

if /i "%CMAKE_GENERATOR%" == "ninja" (
    :: Build BTLS using ninja build system.
    call "%NINJA%" || (
        goto ON_ERROR
    )
) else (
    :: Build BTLS using msbuild build system.
    call "%MSBUILD%" mono-btls.sln /p:Configuration=%VS_CONFIGURATION% /p:Platform=%VS_PLATFORM% /t:%VS_TARGET% || (
        goto ON_ERROR
    )
)

:ON_INSTALL_BTLS

if not exist "%BTLS_BUILD_OUTPUT_DIR%\libmono-btls-shared.dll" (
    echo Missing btls build output, "%BTLS_BUILD_OUTPUT_DIR%\libmono-btls-shared.dll"
    goto ON_ERROR
)

:: Make sure build output folder exists.
if not exist %MONO_DIST_DIR% (
    echo Could not find "%MONO_DIST_DIR%", creating folder for build output.
    mkdir "%MONO_DIST_DIR%"
)

:: Copy files into distribution directory.
copy "%BTLS_BUILD_OUTPUT_DIR%\libmono-btls-shared.dll" "%MONO_DIST_DIR%"

if exist "%BTLS_BUILD_OUTPUT_DIR%\libmono-btls-shared.pdb" (
    copy "%BTLS_BUILD_OUTPUT_DIR%\libmono-btls-shared.pdb" "%MONO_DIST_DIR%"
)

goto ON_SUCCESS

:ON_CLEAN_BTLS

if exist "%BTLS_BUILD_DIR%\build.ninja" (
    pushd
    cd "%BTLS_BUILD_DIR%"
    call "%NINJA%" clean
    popd
)

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

:: ##############################################################################################################################
:: Functions

:: --------------------------------------------------
:: Finds a program using environment.
::
:: %1 Existing program to check for.
:: %2 Name of binary to locate.
:: %3 Output, variable to set if found requested program.
:: --------------------------------------------------
:FIND_PROGRAM

:: If not set by caller, check environment for program.
if exist "%~1" (
    goto :EOF
)

call where /q "%~2" && (
    for /f "delims=" %%a in ('where "%~2"') do (
        set "%~3=%%a"
    )
) || (
    set "%~3="
)

goto :EOF

:: --------------------------------------------------
:: Setup up cmake build environment, including generator, build tools and variables.
:: --------------------------------------------------
:SETUP_CMAKE_ENVIRONMENT

:: If not set by caller, check environment for working cmake.exe.
call :FIND_PROGRAM "%CMAKE%" "%CMAKE_BIN_NAME%" CMAKE
if "%CMAKE%" == "" (
    goto _SETUP_CMAKE_ENVIRONMENT_EXIT
)

echo Found CMake: %CMAKE%

:: Check for optional cmake generate and build tools for full BTLS assembler supported build. NOTE, currently BTLS assembler build
:: can't be done using Visual Studio and must use ninja build generator + yasm and perl.
call :FIND_PROGRAM "%NINJA%" "%NINJA_BIN_NAME%" NINJA
call :FIND_PROGRAM "%YASM%" "%YASM_BIN_NAME%" YASM
call :FIND_PROGRAM "%PERL%" "%PERL_BIN_NAME%" PERL

if not "%NINJA%" == "" if not "%YASM%" == "" if not "%PERL%" == "" (
    goto _SETUP_CMAKE_ENVIRONMENT_NINJA_GENERATOR
)

:_SETUP_CMAKE_ENVIRONMENT_VS_GENERATOR

echo Using Visual Studio build generator, disabling full assembler build.

:: Detect VS version to use right cmake generator.
set CMAKE_GENERATOR=Visual Studio 14 2015
if "%VisualStudioVersion%" == "15.0" (
    set CMAKE_GENERATOR=Visual Studio 15 2017
)

if /i "%VS_PLATFORM%" == "x64" (
    set CMAKE_GENERATOR=%CMAKE_GENERATOR% Win64
)

set BTLS_BUILD_OUTPUT_DIR=%BTLS_BUILD_DIR%\%VS_CONFIGURATION%

goto _SETUP_CMAKE_ENVIRONMENT_EXIT

:_SETUP_CMAKE_ENVIRONMENT_NINJA_GENERATOR

echo Found Ninja: %NINJA%
echo Using Ninja build generator, enabling full assembler build.

set CMAKE_GENERATOR=Ninja
set BTLS_BUILD_OUTPUT_DIR=%BTLS_BUILD_DIR%
set BTLS_NO_ASM_SUPPORT=0

:_SETUP_CMAKE_ENVIRONMENT_EXIT

goto :EOF

@echo on
