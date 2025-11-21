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
    -property installationPath'
) DO SET vsBase=%%i

IF "%vsBase%"=="" GOTO :ERROR

IF /I "%PROCESSOR_ARCHITECTURE%" == "ARM64" (
    IF /I "%~1" == "x64"   ( set vcEnvironment=arm64_amd64 )
    IF /I "%~1" == "x86"   ( set vcEnvironment=arm64_x86 )
    IF /I "%~1" == "arm64" ( set vcEnvironment=arm64 )
) ELSE (
    IF /I "%~1" == "x64"   ( set vcEnvironment=amd64 )
    IF /I "%~1" == "x86"   ( set vcEnvironment=amd64_x86 )
    IF /I "%~1" == "arm64" ( set vcEnvironment=amd64_arm64 )
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
