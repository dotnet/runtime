@ECHO off

SET PLATFORM=%1
SET CONFIG=%2
SET BUILD_DIR=%3
SET ARGUMENTS=%4

SET XCOPY_COMMAND=%windir%\system32\xcopy

SET BUILD_DIR=%BUILD_DIR:"=%
SET BUILD_DIR=%BUILD_DIR:/=\%

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

IF NOT EXIST %BUILD_DIR% (
	ECHO Error: '%BUILD_DIR%', directory doesn't eixst.
	GOTO ON_ERROR
)

IF "\" == "%BUILD_DIR:~-1%" (
	SET BUILD_DIR=%BUILD_DIR:~0,-1%
)

IF NOT EXIST %BUILD_DIR%\%PLATFORM%\lib\%CONFIG% (
	ECHO Error: No lib directory available for %PLATFORM% %CONFIG% at '%BUILD_DIR%'. Any build availalbe for platform, configuration pair?
	GOTO ON_ERROR
)

IF NOT EXIST %BUILD_DIR%\%PLATFORM%\bin\%CONFIG% (
	ECHO Error: No bin directory available for %PLATFORM% %CONFIG% at '%BUILD_DIR%'. Any build availalbe for platform, configuration pair?
	GOTO ON_ERROR
)

SET PACKAGE_DIR=%BUILD_DIR%\package\%PLATFORM%\%CONFIG%

SET OPTIONS=/s /e /y

IF "-v" == "%ARGUMENTS%" (
	SET OPTIONS=/f /s /e /y
)

IF "-q" == "%ARGUMENTS%" (
	SET "OPTIONS=/s /e /q /y"
)

ECHO Packaging mono build %PLATFORM% %CONFIG% into '%PACKAGE_DIR%' ...

IF EXIST %PACKAGE_DIR% rmdir %PACKAGE_DIR% /s /q
mkdir %PACKAGE_DIR%
mkdir %PACKAGE_DIR%\include\mono-2.0

SET RUN=%XCOPY_COMMAND% ".\include\*.*" "%PACKAGE_DIR%\include\mono-2.0\" %OPTIONS%
call :runCommand "%RUN%" %ARGUMENTS%

SET RUN=%XCOPY_COMMAND% "%BUILD_DIR%\%PLATFORM%\lib\%CONFIG%\*.lib" "%PACKAGE_DIR%\lib\" %OPTIONS%
call :runCommand "%RUN%" %ARGUMENTS%

SET RUN=%XCOPY_COMMAND% "%BUILD_DIR%\%PLATFORM%\lib\%CONFIG%\*.pdb" "%PACKAGE_DIR%\lib\" %OPTIONS%
call :runCommand "%RUN%" %ARGUMENTS%

SET RUN=%XCOPY_COMMAND% "%BUILD_DIR%\%PLATFORM%\bin\%CONFIG%\*.exe" "%PACKAGE_DIR%\bin\" %OPTIONS%
call :runCommand "%RUN%" %ARGUMENTS%

SET RUN=%XCOPY_COMMAND% "%BUILD_DIR%\%PLATFORM%\bin\%CONFIG%\*.dll" "%PACKAGE_DIR%\bin\" %OPTIONS%
call :runCommand "%RUN%" %ARGUMENTS%

SET RUN=%XCOPY_COMMAND% "%BUILD_DIR%\%PLATFORM%\bin\%CONFIG%\*.pdb" "%PACKAGE_DIR%\bin\" %OPTIONS%
call :runCommand "%RUN%" %ARGUMENTS%

SET RUN=%XCOPY_COMMAND% "%BUILD_DIR%\%PLATFORM%\bin\%CONFIG%\*.lib" "%PACKAGE_DIR%\bin\" %OPTIONS%
call :runCommand "%RUN%" %ARGUMENTS%

ECHO Packaging of mono build %PLATFORM% %CONFIG% into '%PACKAGE_DIR%' DONE. 

EXIT /b 0

:ON_ERROR
	ECHO "package.bat [win32|x64] [Debug|Release] [MONO_BUILD_DIR_PREFIX] [ARGUMENTS]"
	EXIT /b 1

@ECHO on

:runCommand

	IF "-q" == "%~2" (
		%~1 >nul 2>&1
	) ELSE (
		%~1
	)

goto :EOF
