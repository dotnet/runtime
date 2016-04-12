@ECHO off

IF "" == "%1" (
	ECHO Error: No platform parameter set.
	GOTO ON_ERROR

	)
IF "" == "%2" (
	ECHO Error: No configuration parameter set.
	GOTO ON_ERROR
)

IF "" == "%3" (
	ECHO Error: No MONO_INSTALLATION_PREFIX parameter set.
	GOTO ON_ERROR
)

IF NOT EXIST %3 (
	ECHO Error: '%3', directory doesn't eixst.
	GOTO ON_ERROR
)

SET PACKAGE_DIR=.\package\%1\%2

IF NOT EXIST %PACKAGE_DIR% (
	ECHO Error: '%PACKAGE_DIR%' directory unavailable.
	GOTO ON_ERROR
)

ECHO Installing mono build %1 %2 into %3 ...

xcopy %PACKAGE_DIR%\*.* %3 /s /e /q /y > nul

ECHO Installing of mono build %1 %2 into %3 DONE. 

EXIT /b 0

:ON_ERROR
	ECHO "install.bat [win32|x64] [Debug|Release] [MONO_INSTALLATION_PREFIX]"
	EXIT /b 1

@ECHO on
	