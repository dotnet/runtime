:: Set up build environment and run execute msbuild with all supplied arguments.

:: Arguments:
:: -------------------------------------------------------
:: %1 Visual Studio target, build|clean, default build
:: %2 Host CPU architecture, x86_64|i686, default x86_64
:: %3 Visual Studio configuration, debug|release, default release
:: %4 Additional arguments passed to msbuild, needs to be quoted if multiple.
:: -------------------------------------------------------

@echo off
setlocal

set BUILD_RESULT=1

:: Get path for current running script.
set RUN_MSBUILD_SCRIPT_PATH=%~dp0

:: Configure all known build arguments.
set VS_BUILD_ARGS=""
set VS_TARGET=build
if /i "%~1" == "clean" (
    set VS_TARGET="clean"
)
shift

set VS_PLATFORM=x64
if /i "%~1" == "i686" (
    set VS_PLATFORM="Win32"
)
if /i "%~1" == "win32" (
    set VS_PLATFORM="Win32"
)
shift

set VS_CONFIGURATION=Release
if /i "%~1" == "debug" (
    set VS_CONFIGURATION="Debug"
)
shift

set "VS_ADDITIONAL_ARGUMENTS=/p:PlatformToolset=v140 /p:MONO_TARGET_GC=sgen"
if /i not "%~1" == "" (
    set VS_ADDITIONAL_ARGUMENTS=%~1
)

:: Setup Windows environment.
call %RUN_MSBUILD_SCRIPT_PATH%setup-windows-env.bat

:: Setup VS msbuild environment.
call %RUN_MSBUILD_SCRIPT_PATH%setup-vs-msbuild-env.bat

set VS_BUILD_ARGS=/p:Configuration=%VS_CONFIGURATION% /p:Platform=%VS_PLATFORM% %VS_ADDITIONAL_ARGUMENTS% /t:%VS_TARGET%
call msbuild.exe %VS_BUILD_ARGS% "%RUN_MSBUILD_SCRIPT_PATH%mono.sln" && (
    set BUILD_RESULT=0
) || (
    set BUILD_RESULT=1
    if not %ERRORLEVEL% == 0 (
        set BUILD_RESULT=%ERRORLEVEL%
    )
)

exit /b %BUILD_RESULT%

@echo on