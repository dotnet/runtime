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

:: Make sure we can restore current working directory after setting up environment.
:: Some of the VS scripts can change the current working directory.
set CALLER_WD=%CD%

:: Get path for current running script.
set RUN_MSBUILD_SCRIPT_PATH=%~dp0

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

:: Visual Studio 2015 == 14.0
if "%VisualStudioVersion%" == "14.0" (
    goto SETUP_VS_2015
)

:: Visual Studio 2017 == 15.0
if "%VisualStudioVersion%" == "15.0" (
    goto SETUP_VS_2017
)

:SETUP_VS_2015

:SETUP_VS_2015_BUILD_TOOLS

:: Try to locate VS2015 build tools installation.
set VS_2015_BUILD_TOOLS_CMD=%ProgramFiles(x86)%\Microsoft Visual C++ Build Tools\vcbuildtools_msbuild.bat

:: Setup VS2015 VC development environment using build tools installation.
call :setup_build_env "%VS_2015_BUILD_TOOLS_CMD%" "%CALLER_WD%" && (
    goto ON_BUILD
)

:SETUP_VS_2015_VC

:: Try to locate installed VS2015 VC environment.
set VS_2015_DEV_CMD=%ProgramFiles(x86)%\Microsoft Visual Studio 14.0\Common7\Tools\VsMSBuildCmd.bat

:: Setup VS2015 VC development environment using VS installation.
call :setup_build_env "%VS_2015_DEV_CMD%" "%CALLER_WD%" && (
    goto ON_BUILD
)

:SETUP_VS_2017

:SETUP_VS_2017_BUILD_TOOLS

:: Try to locate VS2017 build tools installation.
set VS_2017_BUILD_TOOLS_CMD=%ProgramFiles(x86)%\Microsoft Visual Studio\2017\BuildTools\Common7\Tools\VsMSBuildCmd.bat

:: Setup VS2017 VC development environment using build tools installation.
call :setup_build_env "%VS_2017_BUILD_TOOLS_CMD%" "%CALLER_WD%" && (
    goto ON_BUILD
)

:SETUP_VS_2017_VC

:: VS2017 includes vswhere.exe that can be used to locate current VS2017 installation.
set VSWHERE_TOOLS_BIN=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe
set VS_2017_DEV_CMD=

:: Try to locate installed VS2017 VC environment.
if exist "%VSWHERE_TOOLS_BIN%" (
    for /f "tokens=*" %%a in ('"%VSWHERE_TOOLS_BIN%" -latest -property installationPath') do (
        set VS_2017_DEV_CMD=%%a\Common7\Tools\VsMSBuildCmd.bat
    )
)

:: Setup VS2017 VC development environment using VS installation.
call :setup_build_env "%VS_2017_DEV_CMD%" "%CALLER_WD%" && (
    goto ON_BUILD
)

:ON_ENV_ERROR

echo Warning, failed to setup build environment needed by msbuild.exe.
echo Incomplete build environment can cause build error's due to missing compiler, linker and platform libraries.

:ON_BUILD

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

:setup_build_env

:: Check if VS build environment script exists.
if not exist "%~1" (
    goto setup_build_env_error
)

:: Run VS build environment script.
call "%~1" > NUL

:: Restore callers working directory in case it has been changed by VS scripts.
cd /d "%~2"

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

:add_to_new_path

if "%NEW_PATH%" == "" (
    set "NEW_PATH=%~1"
) else (
    SET "NEW_PATH=%NEW_PATH%;%~1"
)

goto :EOF

@echo on
