@ECHO OFF

SETLOCAL
SETLOCAL ENABLEDELAYEDEXPANSION

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

SET META_DATA_FILES=^
appdomain.h ^
assembly.h ^
attrdefs.h ^
blob.h ^
class.h ^
debug-helpers.h ^
debug-mono-symfile.h ^
environment.h ^
exception.h ^
image.h ^
loader.h ^
metadata.h ^
mono-config.h ^
mono-debug.h ^
mono-gc.h ^
mono-private-unstable.h ^
object.h ^
object-forward.h ^
opcodes.h ^
profiler.h ^
profiler-events.h ^
reflection.h ^
row-indexes.h ^
sgen-bridge.h ^
threads.h ^
tokentype.h ^
verify.h

SET UTILS_FILES=^
mono-counters.h ^
mono-dl-fallback.h ^
mono-error.h ^
mono-forward.h ^
mono-jemalloc.h ^
mono-logger.h ^
mono-private-unstable.h ^
mono-publib.h

SET JIT_FILES=^
jit.h ^
mono-private-unstable.h

ECHO Copying mono include files from %SOURCE_ROOT% to %TARGET_ROOT% ...

SET RUN=%XCOPY_COMMAND% "%SOURCE_ROOT%\cil\opcode.def" "%TARGET_ROOT%\cil\" %OPTIONS%
call :runCommand "%RUN%" %ARGUMENTS%

FOR %%a IN (%JIT_FILES%) DO (
	SET RUN=%XCOPY_COMMAND% "%SOURCE_ROOT%\mini\%%a" "%TARGET_ROOT%\jit\" %OPTIONS%
	call :runCommand "!RUN!" %ARGUMENTS%
)

FOR %%a IN (%META_DATA_FILES%) DO (
	SET RUN=%XCOPY_COMMAND% "%SOURCE_ROOT%\metadata\%%a" "%TARGET_ROOT%\metadata\" %OPTIONS%
	call :runCommand "!RUN!" %ARGUMENTS%
)

FOR %%a IN (%UTILS_FILES%) DO (
	SET RUN=%XCOPY_COMMAND% "%SOURCE_ROOT%\utils\%%a" "%TARGET_ROOT%\utils\" %OPTIONS%
	call :runCommand "!RUN!" %ARGUMENTS%
)

ECHO Copying mono include files from %SOURCE_ROOT% to %TARGET_ROOT% DONE.

EXIT /b 0

:ON_ERROR
	ECHO "libmono.bat [SOURCE_ROOT] [TARGET_ROOT] [ARGUMENTS]"
	EXIT /b 1

:runCommand

	IF "-q" == "%~2" (
		%~1 >nul 2>&1
	) ELSE (
		%~1
	)

GOTO :EOF


@ECHO ON
