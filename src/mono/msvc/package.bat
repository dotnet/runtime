@ECHO off

IF "" == "%1" (
	ECHO Error: No platform parameter set.
	GOTO ON_ERROR
)
	
IF "" == "%2" (
	ECHO Error: No configuration parameter set.
	GOTO ON_ERROR
)

IF NOT EXIST .\%1\lib\%2 (
	ECHO Error: No lib directory available for %1 %2. Any build availalbe for platform, configuration pair?
	GOTO ON_ERROR
)

IF NOT EXIST .\%1\bin\%2 (
	ECHO Error: No bin directory available for %1 %2. Any build availalbe for platform, configuration pair?
	GOTO ON_ERROR
)

SET PACKAGE_DIR=.\package\%1\%2

ECHO Packaging mono build %1 %2 into '%PACKAGE_DIR%' ...

IF EXIST %PACKAGE_DIR% rmdir %PACKAGE_DIR% /s /q
mkdir %PACKAGE_DIR%
mkdir %PACKAGE_DIR%\include\mono-2.0
xcopy .\include\*.* %PACKAGE_DIR%\include\mono-2.0\ /s /e /q /y > nul

xcopy .\%1\lib\%2\*.lib %PACKAGE_DIR%\lib\ /s /e /q /y > nul
xcopy .\%1\lib\%2\*.pdb %PACKAGE_DIR%\lib\ /s /e /q /y > nul

xcopy .\%1\bin\%2\*.exe %PACKAGE_DIR%\bin\ /s /e /q /y > nul
xcopy .\%1\bin\%2\*.dll %PACKAGE_DIR%\bin\ /s /e /q /y > nul
xcopy .\%1\bin\%2\*.pdb %PACKAGE_DIR%\bin\ /s /e /q /y > nul
xcopy .\%1\bin\%2\*.lib %PACKAGE_DIR%\bin\ /s /e /q /y > nul

ECHO Packaging of mono build %1 %2 into '%PACKAGE_DIR%' DONE. 

EXIT /b 0

:ON_ERROR
	ECHO "package.bat [win32|x64] [Debug|Release]"
	EXIT /b 1

@ECHO on
