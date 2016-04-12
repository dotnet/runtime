@ECHO off

SET PLATFORM=%1
SET CONFIG=%2
SET BUILD_DIR=%3
SET ARGUMENTS=%4

SET BUILD_DIR=%BUILD_DIR:"=%

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
	SET "OPTIONS=/s /e /q /y ^>nul"
)

ECHO Packaging mono build %PLATFORM% %CONFIG% into '%PACKAGE_DIR%' ...

IF EXIST %PACKAGE_DIR% rmdir %PACKAGE_DIR% /s /q
mkdir "%PACKAGE_DIR%"
mkdir "%PACKAGE_DIR%\include\mono-2.0"

SET RUN=xcopy ".\include\*.*" "%PACKAGE_DIR%\include\mono-2.0\" %OPTIONS%
%RUN%

SET RUN=xcopy "%BUILD_DIR%\%PLATFORM%\lib\%CONFIG%\*.lib" "%PACKAGE_DIR%\lib\" %OPTIONS%
%RUN%

SET RUN=xcopy "%BUILD_DIR%\%PLATFORM%\lib\%CONFIG%\*.pdb" "%PACKAGE_DIR%\lib\" %OPTIONS%
%RUN%

SET RUN=xcopy "%BUILD_DIR%\%PLATFORM%\bin\%CONFIG%\*.exe" "%PACKAGE_DIR%\bin\" %OPTIONS%
%RUN%

SET RUN=xcopy "%BUILD_DIR%\%PLATFORM%\bin\%CONFIG%\*.dll" "%PACKAGE_DIR%\bin\" %OPTIONS%
%RUN%

SET RUN=xcopy "%BUILD_DIR%\%PLATFORM%\bin\%CONFIG%\*.pdb" "%PACKAGE_DIR%\bin\" %OPTIONS%
%RUN%

SET RUN=xcopy "%BUILD_DIR%\%PLATFORM%\bin\%CONFIG%\*.lib" "%PACKAGE_DIR%\bin\" %OPTIONS%
%RUN%

ECHO Packaging of mono build %PLATFORM% %CONFIG% into '%PACKAGE_DIR%' DONE. 

EXIT /b 0

:ON_ERROR
	ECHO "package.bat [win32|x64] [Debug|Release] [MONO_BUILD_DIR_PREFIX] [ARGUMENTS]"
	EXIT /b 1

@ECHO on
