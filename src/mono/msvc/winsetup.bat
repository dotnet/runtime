@echo off
setlocal

set BUILD_RESULT=1

:: Make sure we can restore current working directory after setting up environment.
:: Some of the VS scripts can change the current working directory.
set CALLER_WD=%CD%

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

call "msbuild.exe" /t:RunWinConfigSetup mono.winconfig.targets && (
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

@echo on
