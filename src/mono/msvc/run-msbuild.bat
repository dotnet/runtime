:: Set up build environment and run execute msbuild with all supplied arguments.

:: Arguments:
:: -------------------------------------------------------
:: %1 Visual Studio target, build|clean, default build
:: %2 Host CPU architecture, x86_64|i686, default x86_64
:: %3 Visual Studio configuration, debug|release, default release
:: %4 Additional arguments passed to msbuild, needs to be quoted if multiple.
:: %5 Project to build.
:: -------------------------------------------------------

@echo off
setlocal

set BUILD_RESULT=1

:: Get path for current running script.
set RUN_MSBUILD_SCRIPT_PATH=%~dp0

:: Configure all known build arguments.
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

set VS_TARGET_GC=sgen
if /i "%~1" == "boehm" (
    set VS_TARGET_GC="boehm"
)
shift

set VS_ADDITIONAL_ARGUMENTS=
if not "%~1" == "" (
    set VS_ADDITIONAL_ARGUMENTS=%~1
)
shift

set VS_BUILD_PROJ=mono.sln
if /i not "%~1" == "" (
    set VS_BUILD_PROJ=%~1
)

if not exist %VS_BUILD_PROJ% (
    set VS_BUILD_PROJ=%RUN_MSBUILD_SCRIPT_PATH%%VS_BUILD_PROJ%
)

:: Setup Windows environment.
call %RUN_MSBUILD_SCRIPT_PATH%setup-windows-env.bat

:: Setup VS msbuild environment.
call %RUN_MSBUILD_SCRIPT_PATH%setup-vs-msbuild-env.bat

if "%VS_ADDITIONAL_ARGUMENTS%" == "" (
    set "VS_ADDITIONAL_ARGUMENTS=/p:PlatformToolset=%VS_DEFAULT_PLATFORM_TOOL_SET%"
)

set VS_BUILD_ARGS=/p:Configuration=%VS_CONFIGURATION% /p:Platform=%VS_PLATFORM% /p:MONO_TARGET_GC=%VS_TARGET_GC% %VS_ADDITIONAL_ARGUMENTS% /t:%VS_TARGET% /m
call msbuild.exe %VS_BUILD_ARGS% "%VS_BUILD_PROJ%" && (
    set BUILD_RESULT=0
) || (
    set BUILD_RESULT=1
    if not %ERRORLEVEL% == 0 (
        set BUILD_RESULT=%ERRORLEVEL%
    )
)

exit /b %BUILD_RESULT%

@echo on