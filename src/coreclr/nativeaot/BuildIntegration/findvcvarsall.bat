@ECHO OFF
SETLOCAL

IF "%~1"=="" (
    ECHO Usage: %~nx0 ^<arch^>
    GOTO :ERROR
)

SET vswherePath=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe
IF NOT EXIST "%vswherePath%" GOTO :ERROR

SET toolsSuffix=x86.x64
IF /I "%~1"=="arm64" SET toolsSuffix=ARM64

FOR /F "tokens=*" %%i IN (
    '"%vswherePath%" -latest -prerelease -products * ^
    -requires Microsoft.VisualStudio.Component.VC.Tools.%toolsSuffix% ^
    -version [16^,18^) ^
    -property installationPath'
) DO SET vsBase=%%i

IF "%vsBase%"=="" GOTO :ERROR

SET procArch=%PROCESSOR_ARCHITEW6432%
IF "%procArch%"=="" SET procArch=%PROCESSOR_ARCHITECTURE%

SET vcEnvironment=%~1
IF /I "%~1"=="x64" (
    SET vcEnvironment=x86_amd64
    IF /I "%procArch%"=="AMD64" SET vcEnvironment=amd64
)
IF /I "%~1"=="arm64" (
    SET vcEnvironment=x86_arm64
    IF /I "%procArch%"=="AMD64" SET vcEnvironment=amd64_arm64
)

CALL "%vsBase%\vc\Auxiliary\Build\vcvarsall.bat" %vcEnvironment% > NUL

FOR /F "delims=" %%W IN ('where link') DO (
    FOR %%A IN ("%%W") DO ECHO %%~dpA#
    GOTO :CAPTURE_LIB_PATHS
)

GOTO :ERROR

:CAPTURE_LIB_PATHS
IF "%LIB%"=="" GOTO :ERROR
ECHO %LIB%

ENDLOCAL

EXIT /B 0

:ERROR
EXIT /B 1
