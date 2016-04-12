@ECHO off

SET PLATFORM=%1
SET CONFIG=%2
SET BUILD_DIR=%3
SET INSTALL_DIR=%4

IF "" == "%PLATFORM%" (
	ECHO Error: No platform parameter set.
	GOTO ON_ERROR

	)
IF "" == "%CONFIG%" (
	ECHO Error: No configuration parameter set.
	GOTO ON_ERROR
)

IF "" == "%BUILD_DIR%" (
	ECHO Error: No MONO_BUILD_DIR_PREFIX parameter set.
	GOTO ON_ERROR
)

IF "" == "%INSTALL_DIR%" (
	ECHO Error: No MONO_INSTALLATION_DIR_PREFIX parameter set.
	GOTO ON_ERROR
)

IF "\" == "%BUILD_DIR:~-1%" (
	SET BUILD_DIR=%BUILD_DIR:~0,-1%
)

IF "\" == "%INSTALL_DIR:~-1%" (
	SET INSTALL_DIR=%INSTALL_DIR:~0,-1%
)

IF NOT EXIST %BUILD_DIR% (
	ECHO Error: '%BUILD_DIR%', directory doesn't eixst.
	GOTO ON_ERROR
)

IF NOT EXIST %INSTALL_DIR% (
	ECHO Error: '%INSTALL_DIR%', directory doesn't eixst.
	GOTO ON_ERROR
)

SET PACKAGE_DIR=%BUILD_DIR%\package\%PLATFORM%\%CONFIG%

IF NOT EXIST %PACKAGE_DIR% (
	ECHO Error: '%PACKAGE_DIR%' directory unavailable.
	GOTO ON_ERROR
)

ECHO Installing mono build %PLATFORM% %CONFIG% from %BUILD_DIR% into %INSTALL_DIR% ...

xcopy %PACKAGE_DIR%\*.* %INSTALL_DIR% /s /e /q /y > nul

ECHO Installing of mono build %PLATFORM% %CONFIG% from %BUILD_DIR% into %INSTALL_DIR% DONE. 

EXIT /b 0

:ON_ERROR
	ECHO "install.bat [win32|x64] [Debug|Release] [MONO_BUILD_DIR_PREFIX] [MONO_INSTALLATION_DIR_PREFIX]"
	EXIT /b 1

@ECHO on
	