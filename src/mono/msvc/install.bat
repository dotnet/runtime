@ECHO off

SET PLATFORM=%1
SET CONFIG=%2
SET BUILD_DIR=%3
SET INSTALL_DIR=%4
SET ARGUMENTS=%5

SET XCOPY_COMMAND=%windir%\system32\xcopy

SET BUILD_DIR=%BUILD_DIR:"=%
SET BUILD_DIR=%BUILD_DIR:/=\%
SET INSTALL_DIR=%INSTALL_DIR:"=%
SET INSTALL_DIR=%INSTALL_DIR:/=\%

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
	ECHO Install directory '%INSTALL_DIR%', directory doesn't eixst, creating....
	mkdir "%INSTALL_DIR%"
	ECHO Install directory '%INSTALL_DIR%' created.
)

SET PACKAGE_DIR=%BUILD_DIR%\package\%PLATFORM%\%CONFIG%

IF NOT EXIST %PACKAGE_DIR% (
	ECHO Error: '%PACKAGE_DIR%' directory unavailable.
	GOTO ON_ERROR
)

SET OPTIONS=/s /e /y

IF "-v" == "%ARGUMENTS%" (
	SET OPTIONS=/f /s /e /y
)

IF "-q" == "%ARGUMENTS%" (
	SET "OPTIONS=/s /e /q /y"
)

ECHO Installing mono build %PLATFORM% %CONFIG% from %PACKAGE_DIR% into %INSTALL_DIR% ...

SET RUN=%XCOPY_COMMAND% "%PACKAGE_DIR%\*.*" "%INSTALL_DIR%" %OPTIONS%
call :runCommand "%RUN%" %ARGUMENTS%

ECHO Installing of mono build %PLATFORM% %CONFIG% from %PACKAGE_DIR% into %INSTALL_DIR% DONE.

EXIT /b 0

:ON_ERROR
	ECHO "install.bat [win32|x64] [Debug|Release] [MONO_BUILD_DIR_PREFIX] [MONO_INSTALLATION_DIR_PREFIX] [ARGUMENTS]"
	EXIT /b 1

@ECHO on

:runCommand

	IF "-q" == "%~2" (
		%~1 >nul 2>&1
	) ELSE (
		%~1
	)

goto :EOF
	