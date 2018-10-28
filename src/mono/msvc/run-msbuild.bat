:: Set up build environment and run execute msbuild with all supplied arguments.
@echo off
setlocal

set BUILD_RESULT=1

set VS_2015_DEV_CMD=%ProgramFiles(x86)%\Microsoft Visual Studio 14.0\Common7\Tools\VsMSBuildCmd.bat
set VS_2015_BUILD_TOOLS_CMD=%ProgramFiles(x86)%\Microsoft Visual C++ Build Tools\vcbuildtools_msbuild.bat
set VSWHERE_TOOLS_BIN=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe
set VS_2017_DEV_CMD=
set VS_2017_BUILD_TOOLS_CMD=%ProgramFiles(x86)%\Microsoft Visual Studio\2017\BuildTools\Common7\Tools\VsMSBuildCmd.bat
set VS_PLATFORM_TOOLSET=/p:PlatformToolset=v140

:: Visual Studio 2015 == 14.0
:: Visual Studio 2017 == 15.0
if "%VisualStudioVersion%" == "15.0" (
    goto SETUP_VS_2017
)

:SETUP_VS_2015

if exist "%VS_2015_DEV_CMD%" (
    echo Setting up VS2015 build environment.
    call "%VS_2015_DEV_CMD%" && (
        goto ON_BUILD
    )
)

if exist "%VS_2015_BUILD_TOOLS_CMD%" (
    echo Setting up VS2015 build environment.
    call "%VS_2015_BUILD_TOOLS_CMD%" && (
        goto ON_BUILD
    )
)

:SETUP_VS_2017

if exist "%VSWHERE_TOOLS_BIN%" (
    for /f "tokens=*" %%a in ('"%VSWHERE_TOOLS_BIN%" -latest -property installationPath') do (
        set VS_2017_DEV_CMD=%%a\Common7\Tools\VsMSBuildCmd.bat
    )
)

if exist "%VS_2017_DEV_CMD%" (
    echo Setting up VS2017 build environment.
    call "%VS_2017_DEV_CMD%"
    set VS_PLATFORM_TOOLSET=/p:PlatformToolset=v141
)

if exist "%VS_2017_BUILD_TOOLS_CMD%" (
    echo Setting up VS2017 build environment.
    call "%VS_2017_BUILD_TOOLS_CMD%"
    set VS_PLATFORM_TOOLSET=/p:PlatformToolset=v141
)

:ON_BUILD

call msbuild.exe %VS_PLATFORM_TOOLSET% %* "%~dp0mono.sln" && (
    set BUILD_RESULT=0
)

exit /b %BUILD_RESULT%

@echo on
