@ECHO off

SET SOURCE_ROOT=%1
SET TARGET_ROOT=%2
SET ARGUMENTS=%3

SET XCOPY_COMMAND=%windir%\system32\xcopy

SET TARGET_ROOT=%TARGET_ROOT:"=%
SET TARGET_ROOT=%TARGET_ROOT:/=\%
SET SOURCE_ROOT=%SOURCE_ROOT:"=%
SET SOURCE_ROOT=%SOURCE_ROOT:/=\%

IF "" == "%SOURCE_ROOT%" (
	ECHO Error: No source root parameter set.
	GOTO ON_ERROR
)
	
IF "" == "%TARGET_ROOT%" (
	ECHO Error: No target root parameter set.
	GOTO ON_ERROR
)

IF NOT EXIST %SOURCE_ROOT% (
	ECHO Error: source directory '%SOURCE_ROOT%', directory doesn't eixst.
	GOTO ON_ERROR
)

IF NOT EXIST %TARGET_ROOT% (
	ECHO Target directory '%TARGET_ROOT%', directory doesn't eixst, creating....
	mkdir %TARGET_ROOT%
	ECHO Target directory '%TARGET_ROOT%' created.
)

IF "\" == "%SOURCE_ROOT:~-1%" (
	SET SOURCE_ROOT=%SOURCE_ROOT:~0,-1%
)

IF "\" == "%TARGET_ROOT:~-1%" (
	SET TARGET_ROOT=%TARGET_ROOT:~0,-1%
)

SET OPTIONS=/y

IF "-v" == "%ARGUMENTS%" (
	SET OPTIONS=/f /y
)

IF "-q" == "%ARGUMENTS%" (
	SET "OPTIONS=/q /y"
)

ECHO Copying mono include files from %SOURCE_ROOT% to %TARGET_ROOT% ...

SET RUN=%XCOPY_COMMAND% "%SOURCE_ROOT%\cil\opcode.def" "%TARGET_ROOT%\cil\" %OPTIONS%
call :runCommand "%RUN%" %ARGUMENTS%

SET RUN=%XCOPY_COMMAND% "%SOURCE_ROOT%\mini\jit.h" "%TARGET_ROOT%\jit\" %OPTIONS%
call :runCommand "%RUN%" %ARGUMENTS%

SET RUN=%XCOPY_COMMAND% "%SOURCE_ROOT%\metadata\*.h" "%TARGET_ROOT%\metadata\" %OPTIONS%
call :runCommand "%RUN%" %ARGUMENTS%

SET RUN=%XCOPY_COMMAND% "%SOURCE_ROOT%\utils\mono-counters.h" "%TARGET_ROOT%\utils\" %OPTIONS%
call :runCommand "%RUN%" %ARGUMENTS%

SET RUN=%XCOPY_COMMAND% "%SOURCE_ROOT%\utils\mono-dl-fallback.h" "%TARGET_ROOT%\utils\" %OPTIONS%
call :runCommand "%RUN%" %ARGUMENTS%

SET RUN=%XCOPY_COMMAND% "%SOURCE_ROOT%\utils\mono-error.h" "%TARGET_ROOT%\utils\" %OPTIONS%
call :runCommand "%RUN%" %ARGUMENTS%

SET RUN=%XCOPY_COMMAND% "%SOURCE_ROOT%\utils\mono-forward.h" "%TARGET_ROOT%\utils\" %OPTIONS%
call :runCommand "%RUN%" %ARGUMENTS%

SET RUN=%XCOPY_COMMAND% "%SOURCE_ROOT%\utils\mono-logger.h" "%TARGET_ROOT%\utils\" %OPTIONS%
call :runCommand "%RUN%" %ARGUMENTS%

SET RUN=%XCOPY_COMMAND% "%SOURCE_ROOT%\utils\mono-publib.h" "%TARGET_ROOT%\utils\" %OPTIONS%
call :runCommand "%RUN%" %ARGUMENTS%

ECHO Copying mono include files from %SOURCE_ROOT% to %TARGET_ROOT% DONE.

EXIT /b 0

:ON_ERROR
	ECHO "libmono.bat [SOURCE_ROOT] [TARGET_ROOT] [ARGUMENTS]"
	EXIT /b 1

@ECHO on

:runCommand

	IF "-q" == "%~2" (
		%~1 >nul 2>&1
	) ELSE (
		%~1
	)

goto :EOF
