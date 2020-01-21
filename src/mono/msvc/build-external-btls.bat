:: --------------------------------------------------
:: Run full BTLS build using msvc toolchain and available cmake generator.
:: Script needs to be run from within a matching build environment, x86|x64.
:: When executed from withing Visual Studio build environment current
:: build environment will be inherited by script.
::
:: %1 Mono BTLS source root directory.
:: %2 BTLS source root directory.
:: %3 BTLS build root directory.
:: %4 Mono distribution root directory.
:: %5 VS CFLAGS.
:: %6 VS platform (Win32/x64)
:: %7 VS configuration (Debug/Release)
:: %8 VS target
:: %9 VS PlatformToolSet, if used.
:: %10 Win SDK, if used.
:: %11 MsBuild bin path, if used.
:: %12 Force MSBuild (true/false), if used.
:: --------------------------------------------------

@echo off
setlocal

set BUILD_RESULT=1

set CL_BIN_NAME=cl.exe
set LINK_BIN_NAME=link.exe
set GIT_BIN_NAME=git.exe
set CMAKE_BIN_NAME=cmake.exe
set NINJA_BIN_NAME=ninja.exe
set PERL_BIN_NAME=perl.exe
set YASM_BIN_NAME=yasm.exe

set MONO_BTLS_DIR=%~1
shift
set BTLS_DIR=%~1
shift
set BTLS_BUILD_DIR=%~1
shift
set MONO_DIST_DIR=%~1
shift
set VS_CFLAGS=%~1
shift
set VS_PLATFORM=%~1
shift
set VS_CONFIGURATION=%~1
shift
set VS_TARGET=%~1
shift
set VS_PLATFORM_TOOL_SET=%~1
shift
set VS_WIN_SDK_VERSION=%~1
shift
set MSBUILD_BIN_PATH=%~1
shift
set FORCE_MSBUILD=%~1

:: Setup toolchain.
:: set GIT=
:: set CMAKE=
:: set NINJA=
set MSBUILD=%MSBUILD_BIN_PATH%msbuild.exe

if "%MONO_BTLS_DIR%" == "" (
    echo Missing mono BTLS source directory argument.
    goto ECHO_USAGE
)

if "%BTLS_DIR%" == "" (
    echo Missing BTLS source directory argument.
    goto ECHO_USAGE
)

if "%BTLS_BUILD_DIR%" == "" (
    echo Missing BTLS build directory argument.
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

if "%VS_PLATFORM_TOOL_SET%" == "" (
    set VS_PLATFORM_TOOL_SET=v142
)

if "%VS_WIN_SDK_VERSION%" == "" (
    set VS_WIN_SDK_VERSION=10.0
)

if "%FORCE_MSBUILD%" == "" (
    set FORCE_MSBUILD=false
)

set BTLS_CFLAGS=%VS_CFLAGS%
set BTLS_ARCH=x86_64
if /i "%VS_PLATFORM%" == "win32" (
    set BTLS_ARCH=i386
)

set BTLS_NO_ASM_SUPPORT=1

:: VS2017/VS2019 includes vswhere.exe that can be used to locate current VS installation.
set VSWHERE_TOOLS_BIN=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe
set VS_COMMON_EXTENSION_TOOLS_PATHS=

:: Check if executed from VS2015/VS2017/VS2019 build environment.
if "%VisualStudioVersion%" == "14.0" (
    if /i not "%VS_PLATFORM_TOOL_SET%" == "v140" (
        echo VisualStudioVersion/PlatformToolchain missmatch, forcing msbuild.
        set FORCE_MSBUILD=true
    )
    goto ON_ENV_OK
)

if "%VisualStudioVersion%" == "15.0" (
    if /i not "%VS_PLATFORM_TOOL_SET%" == "v141" (
        echo VisualStudioVersion/PlatformToolchain missmatch, forcing msbuild.
        set FORCE_MSBUILD=true
    )
    goto ON_ENV_OK
)

if "%VisualStudioVersion%" == "16.0" (
    if /i not "%VS_PLATFORM_TOOL_SET%" == "v142" (
        echo VisualStudioVersion/PlatformToolchain missmatch, forcing msbuild.
        set FORCE_MSBUILD=true
    )
    goto ON_ENV_OK
)

:: Executed outside VS2015/VS2017/VS2019 build environment, try to locate Visual Studio C/C++ compiler and linker.
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

:: VS 2019.
if exist "%VSWHERE_TOOLS_BIN%" (
    echo For VS2019 builds, make sure to run this from within Visual Studio build or using "x86|x64 Native Tools Command Prompt for VS2019" command prompt.
    for /f "tokens=*" %%a IN ('"%VSWHERE_TOOLS_BIN%" -version [16.0^,17.0] -prerelease -property installationPath') do (
        echo Setup a "x86|x64 Native Tools Command Prompt for VS2019" command prompt by using "%%a\VC\Auxiliary\Build\vcvars32.bat|vcvars64.bat".
        goto ON_ENV_WARNING_DONE
    )
)

:: VS 2017.
if exist "%VSWHERE_TOOLS_BIN%" (
    echo For VS2017 builds, make sure to run this from within Visual Studio build or using "x86|x64 Native Tools Command Prompt for VS2017" command prompt.
    for /f "tokens=*" %%a IN ('"%VSWHERE_TOOLS_BIN%" -version [15.0^,16.0] -property installationPath') do (
        echo Setup a "x86|x64 Native Tools Command Prompt for VS2017" command prompt by using "%%a\VC\Auxiliary\Build\vcvars32.bat|vcvars64.bat".
        goto ON_ENV_WARNING_DONE
    )
)

:: VS 2015.
set VC_VARS_ALL_FILE=%ProgramFiles(x86)%\Microsoft Visual Studio 14.0\VC\vcvarsall.bat
IF EXIST "%VC_VARS_ALL_FILE%" (
    echo For VS2015 builds, make sure to run this from within Visual Studio build or using "VS2015 x86|x64 Native Tools Command Prompt" command prompt.
    echo Setup a "VS2015 x86|x64 Native Tools Command Prompt" command prompt by using "%VC_VARS_ALL_FILE% x86|amd64".
    goto ON_ENV_WARNING_DONE
)

:ON_ENV_WARNING_DONE

echo Could not detect Visual Studio build environment. You may experience build problems if wrong toolchain is auto detected.

:ON_ENV_OK

:: Append paths to VS installed common extension tools at end of PATH (Vs2017/VS2019).
call :FIND_VS_COMMON_EXTENSION_TOOLS_PATHS VS_COMMON_EXTENSION_TOOLS_PATHS
if not "%VS_COMMON_EXTENSION_TOOLS_PATHS%" == "" (
    set "PATH=%PATH%;%VS_COMMON_EXTENSION_TOOLS_PATHS%"
)

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
echo Updating submodule "%BTLS_DIR%"
"%GIT%" submodule update --init -- "%BTLS_DIR%"
if not ERRORLEVEL == 0 (
    "%GIT%" submodule init -- "%BTLS_DIR%"
    "%GIT%" submodule update -- "%BTLS_DIR%"
    if not ERRORLEVEL == 0 (
        echo Git boringssl submodules failed to updated. You may experience compilation problems if some submodules are out of date.
    )
)

if not exist "%BTLS_DIR%" (
    echo Could not find "%BTLS_DIR%".
    goto ON_ERROR
)

if not exist "%BTLS_BUILD_DIR%" (
    mkdir "%BTLS_BUILD_DIR%"
)

cd "%BTLS_BUILD_DIR%"

:: Make sure cmake pick up msvc toolchain regardless of selected generator (Visual Studio|Ninja)
set CC=%CL_BIN_NAME%
set CXX=%CL_BIN_NAME%

set BTLS_BUILD_TYPE=
set CMAKE_GENERATOR_TOOLSET=
if /i "%CMAKE_GENERATOR%" == "ninja" (
    set BTLS_BUILD_TYPE=-D CMAKE_BUILD_TYPE=%VS_CONFIGURATION%
) else (
    set CMAKE_GENERATOR_TOOLSET=%VS_PLATFORM_TOOL_SET%
)

if not "%CMAKE_GENERATOR_ARCH%" == "" (
    set CMAKE_GENERATOR_ARCH=-A %CMAKE_GENERATOR_ARCH%
)

:: Run cmake.
"%CMAKE%" ^
-D BTLS_ROOT:PATH="%BTLS_DIR%" ^
-D SRC_DIR:PATH="%MONO_BTLS_DIR%" ^
-D BTLS_CFLAGS="%BTLS_CFLAGS%" ^
-D OPENSSL_NO_ASM=%BTLS_NO_ASM_SUPPORT% ^
-D BTLS_ARCH="%BTLS_ARCH%" ^
-D BUILD_SHARED_LIBS=1 ^
-D CMAKE_SYSTEM_VERSION=%VS_WIN_SDK_VERSION% ^
%CMAKE_GENERATOR_TOOLSET% ^
%BTLS_BUILD_TYPE% ^
-G "%CMAKE_GENERATOR%" ^
%CMAKE_GENERATOR_ARCH% ^
"%MONO_BTLS_DIR%"

if not ERRORLEVEL == 0 (
    goto ON_ERROR
)

if /i "%CMAKE_GENERATOR%" == "ninja" (
    :: Build BTLS using ninja build system.
    call "%NINJA%" -j4 || (
        goto ON_ERROR
    )
) else (
    :: Build BTLS using msbuild build system.
    call "%MSBUILD%" mono-btls.sln /p:Configuration=%VS_CONFIGURATION% /p:Platform=%VS_PLATFORM% /t:%VS_TARGET% /v:m /nologo /m || (
        goto ON_ERROR
    )
)

:ON_INSTALL_BTLS

if not exist "%BTLS_BUILD_OUTPUT_DIR%\libmono-btls-shared.dll" (
    echo Missing btls build output, "%BTLS_BUILD_OUTPUT_DIR%\libmono-btls-shared.dll"
    goto ON_ERROR
)

:: Make sure build output folder exists.
if not exist "%MONO_DIST_DIR%" (
    echo Could not find "%MONO_DIST_DIR%", creating folder for build output.
    mkdir "%MONO_DIST_DIR%"
)

:: Copy files into distribution directory.
copy /Y "%BTLS_BUILD_OUTPUT_DIR%\libmono-btls-shared.dll" "%MONO_DIST_DIR%" >nul 2>&1

if exist "%BTLS_BUILD_OUTPUT_DIR%\libmono-btls-shared.pdb" (
    copy /Y "%BTLS_BUILD_OUTPUT_DIR%\libmono-btls-shared.pdb" "%MONO_DIST_DIR%" >nul 2>&1
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
    "%MSBUILD%" "%BTLS_BUILD_DIR%\mono-btls.sln" /p:Configuration=%VS_CONFIGURATION% /p:Platform=%VS_PLATFORM% /t:Clean /v:m /nologo
)

goto ON_SUCCESS

:ON_SUCCESS

set BUILD_RESULT=0
goto ON_EXIT

:ECHO_USAGE:
    ECHO Usage: build-btls.bat [mono_btls_src_dir] [btls_src_dir] [btls_build_dir] [mono_dist_dir] [vs_cflags] [vs_plaform] [vs_configuration].

:ON_ERROR
    echo Failed to build BTLS.
    goto ON_EXIT

:ON_EXIT
    exit /b %BUILD_RESULT%

:: ##############################################################################################################################
:: Functions

:: --------------------------------------------------
:: Locates PATHS to installed common extension tools.
:: %1 Output, variable including paths.
:: --------------------------------------------------
:FIND_VS_COMMON_EXTENSION_TOOLS_PATHS

set VS_COMMON_EXTENSION_PATH=
if exist "%VSWHERE_TOOLS_BIN%" (
    for /f "tokens=*" %%a in ('"%VSWHERE_TOOLS_BIN%" -version [16.0^,17.0] -property installationPath') do (
        set VS_COMMON_EXTENSION_PATH=%%a\Common7\IDE\CommonExtensions\Microsoft
    )
)

if exist "%VS_COMMON_EXTENSION_PATH%" (
    set "%~1=%VS_COMMON_EXTENSION_PATH%\TeamFoundation\Team Explorer\Git\cmd;%VS_COMMON_EXTENSION_PATH%\CMake\CMake\bin;%VS_COMMON_EXTENSION_PATH%\CMake\Ninja"
)

goto :EOF

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

if /i "%VS_TARGET%" == "build" (
    echo Found CMake: "%CMAKE%"
)

if /i "%FORCE_MSBUILD%" == "true" (
    goto _SETUP_CMAKE_ENVIRONMENT_VS_GENERATOR
)

:: Check for optional cmake generate and build tools for full BTLS assembler supported build. NOTE, currently BTLS assembler build
:: can't be done using Visual Studio and must use ninja build generator + yasm and perl.
call :FIND_PROGRAM "%NINJA%" "%NINJA_BIN_NAME%" NINJA
call :FIND_PROGRAM "%YASM%" "%YASM_BIN_NAME%" YASM
call :FIND_PROGRAM "%PERL%" "%PERL_BIN_NAME%" PERL

if not "%NINJA%" == "" if not "%YASM%" == "" if not "%PERL%" == "" (
    goto _SETUP_CMAKE_ENVIRONMENT_NINJA_GENERATOR
)

:_SETUP_CMAKE_ENVIRONMENT_VS_GENERATOR

if /i "%VS_TARGET%" == "build" (
    echo Using Visual Studio build generator, disabling full assembler build.
)

set CMAKE_GENERATOR_ARCH=

:: Detect VS platform tool set to use matching cmake generator.
if /i "%VS_PLATFORM_TOOL_SET%" == "v140" (
    if /i "%VS_PLATFORM%" == "x64" (
        set CMAKE_GENERATOR=Visual Studio 14 2015 Win64
    ) else (
        set CMAKE_GENERATOR=Visual Studio 14 2015
    )
)

if /i "%VS_PLATFORM_TOOL_SET%" == "v141" (
    if /i "%VS_PLATFORM%" == "x64" (
        set CMAKE_GENERATOR=Visual Studio 15 2017 Win64
    ) else (
        set CMAKE_GENERATOR=Visual Studio 15 2017
    )
)

if /i "%VS_PLATFORM_TOOL_SET%" == "v142" (
    set CMAKE_GENERATOR=Visual Studio 16 2019
    if /i "%VS_PLATFORM%" == "x64" (
        set CMAKE_GENERATOR_ARCH=x64
    ) else (
        set CMAKE_GENERATOR_ARCH=Win32
    )
)

set BTLS_BUILD_OUTPUT_DIR=%BTLS_BUILD_DIR%\%VS_CONFIGURATION%

goto _SETUP_CMAKE_ENVIRONMENT_EXIT

:_SETUP_CMAKE_ENVIRONMENT_NINJA_GENERATOR

if /i "%VS_TARGET%" == "build" (
    echo Found Ninja: "%NINJA%"
    echo Using Ninja build generator, enabling full assembler build.
)

set CMAKE_GENERATOR_ARCH=
set CMAKE_GENERATOR=Ninja
set BTLS_BUILD_OUTPUT_DIR=%BTLS_BUILD_DIR%
set BTLS_NO_ASM_SUPPORT=0

:_SETUP_CMAKE_ENVIRONMENT_EXIT

goto :EOF

@echo on
