:: Set up VS msbuild environment depending on installed VS versions.
:: Script will setup environment variables directly in callers environment.

:: Make sure we can restore current working directory after setting up environment.
:: Some of the VS scripts can change the current working directory.
set CALLER_WD=%CD%

:: Get path for current running script.
set RUN_SETUP_VS_MSBUILD_ENV_SCRIPT_PATH=%~dp0

:: VS2017/VS2019 includes vswhere.exe that can be used to locate current VS installation.
set VSWHERE_TOOLS_BIN=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe

:: Visual Studio 2015 == 14.0
if "%VisualStudioVersion%" == "14.0" (
    goto SETUP_VS_2015
)

:: Visual Studio 2017 == 15.0
if "%VisualStudioVersion%" == "15.0" (
    goto SETUP_VS_2017
)

:: Visual Studio 2019 == 16.0
if "%VisualStudioVersion%" == "16.0" (
    goto SETUP_VS_2019
)

:SETUP_VS_2019

:SETUP_VS_2019_BUILD_TOOLS

:: Try to locate VS2019 build tools installation.
set VS_2019_BUILD_TOOLS_CMD=%ProgramFiles(x86)%\Microsoft Visual Studio\2019\BuildTools\Common7\Tools\VsMSBuildCmd.bat

:: Setup VS2019 VC development environment using build tools installation.
call :setup_build_env "%VS_2019_BUILD_TOOLS_CMD%" "%CALLER_WD%" && (
    set VS_DEFAULT_PLATFORM_TOOL_SET=v142
    goto ON_EXIT
)

:SETUP_VS_2019_VC

set VS_2019_DEV_CMD=

:: Try to locate installed VS2019 VC environment.
if exist "%VSWHERE_TOOLS_BIN%" (
    for /f "tokens=*" %%a in ('"%VSWHERE_TOOLS_BIN%" -version [16.0^,17.0] -prerelease -property installationPath') do (
        set VS_2019_DEV_CMD=%%a\Common7\Tools\VsMSBuildCmd.bat
    )
)

:: Setup VS2019 VC development environment using VS installation.
call :setup_build_env "%VS_2019_DEV_CMD%" "%CALLER_WD%" && (
    set VS_DEFAULT_PLATFORM_TOOL_SET=v142
    goto ON_EXIT
)

:SETUP_VS_2017

:SETUP_VS_2017_BUILD_TOOLS

:: Try to locate VS2017 build tools installation.
set VS_2017_BUILD_TOOLS_CMD=%ProgramFiles(x86)%\Microsoft Visual Studio\2017\BuildTools\Common7\Tools\VsMSBuildCmd.bat

:: Setup VS2017 VC development environment using build tools installation.
call :setup_build_env "%VS_2017_BUILD_TOOLS_CMD%" "%CALLER_WD%" && (
    set VS_DEFAULT_PLATFORM_TOOL_SET=v141
    goto ON_EXIT
)

:SETUP_VS_2017_VC

set VS_2017_DEV_CMD=

:: Try to locate installed VS2017 VC environment.
if exist "%VSWHERE_TOOLS_BIN%" (
    for /f "tokens=*" %%a in ('"%VSWHERE_TOOLS_BIN%" -version [15.0^,16.0] -property installationPath') do (
        set VS_2017_DEV_CMD=%%a\Common7\Tools\VsMSBuildCmd.bat
    )
)

:: Setup VS2017 VC development environment using VS installation.
call :setup_build_env "%VS_2017_DEV_CMD%" "%CALLER_WD%" && (
    set VS_DEFAULT_PLATFORM_TOOL_SET=v141
    goto ON_EXIT
)

:SETUP_VS_2015

:SETUP_VS_2015_BUILD_TOOLS

:: Try to locate VS2015 build tools installation.
set VS_2015_BUILD_TOOLS_CMD=%ProgramFiles(x86)%\Microsoft Visual C++ Build Tools\vcbuildtools_msbuild.bat

:: Setup VS2015 VC development environment using build tools installation.
call :setup_build_env "%VS_2015_BUILD_TOOLS_CMD%" "%CALLER_WD%" && (
    set VS_DEFAULT_PLATFORM_TOOL_SET=v140
    goto ON_EXIT
)

:SETUP_VS_2015_VC

:: Try to locate installed VS2015 VC environment.
set VS_2015_DEV_CMD=%ProgramFiles(x86)%\Microsoft Visual Studio 14.0\Common7\Tools\VsMSBuildCmd.bat

:: Setup VS2015 VC development environment using VS installation.
call :setup_build_env "%VS_2015_DEV_CMD%" "%CALLER_WD%" && (
    set VS_DEFAULT_PLATFORM_TOOL_SET=v140
    goto ON_EXIT
)

:ON_ENV_ERROR

echo Warning, failed to setup VS build environment needed by VS tooling.
echo Incomplete build environment can cause build error's due to missing compiler,
echo linker and platform libraries.

exit /b 1

:ON_EXIT

exit /b 0

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