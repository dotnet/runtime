@ECHO OFF

SETLOCAL ENABLEDELAYEDEXPANSION

SET TEMP_PATH=%PATH%
SET MONO_RESULT=1

SET BUILD_TARGET=%1
SET CLEAN=%2

IF NOT "" == "%BUILD_TARGET%" (
	SET BUILD_TARGET=%BUILD_TARGET:"=%
)

CALL setup-env.bat
IF NOT ERRORLEVEL == 0 (
	ECHO Failed to setup mono paths.
	GOTO ON_ERROR
)

CALL setup-toolchain.bat
IF NOT ERRORLEVEL == 0 (
	ECHO Failed to setup toolchain.
	GOTO ON_ERROR
)

IF NOT EXIST "%MONO_WINAOT_BCL_PATH%" (
	ECHO Could not find "%MONO_WINAOT_BCL_PATH%".
	GOTO ON_ERROR
)

SET MONO_RUNTIME_TEST_PATH=%MONO_MINI_HOME%
SET MONO_TEST_PATH=%MONO_TEST_BUILD_DIR%

SET FULLAOT_DIR=%MONO_WINAOT_BUILD_DIR%
SET MONO_PATH=%FULLAOT_DIR%

REM Debug output options.

REM SET MONO_LOG_LEVEL=debug
REM SET MONO_LOG_MASK=asm,aot

SET MONO_LOG_LEVEL=
SET MONO_LOG_MASK=

SET FULLAOT_BCL_LIBS=^
mscorlib.dll ^
System.Core.dll ^
System.dll ^
System.Numerics.dll ^
System.Numerics.Vectors.dll ^
System.Xml.dll ^
System.Security.dll ^
Mono.Simd.dll ^
Mono.Security.dll ^
I18N.dll ^
I18N.West.dll ^
MemoryIntrinsics.dll

SET FULLAOT_TEST_LIBS=^
TestDriver.dll ^
generics-variant-types.dll

REM basic-simd.exe not in full AOT profile on Windows.
SET FULLAOT_RUNTIME_TESTS=^
basic.exe ^
basic-float.exe ^
basic-long.exe ^
basic-calls.exe ^
objects.exe ^
arrays.exe ^
basic-math.exe ^
exceptions.exe ^
iltests.exe ^
devirtualization.exe ^
generics.exe ^
basic-vectors.exe ^
gshared.exe ^
aot-tests.exe ^
ratests.exe ^
unaligned.exe ^
builtin-types.exe

SET FULLAOT_LIBS=

mkdir %FULLAOT_DIR% >nul 2>&1

IF /i "bcl" == "%BUILD_TARGET%" (

	SET FULLAOT_LIBS=%FULLAOT_LIBS% %FULLAOT_BCL_LIBS%

)

IF /i "tests" == "%BUILD_TARGET%" (

	SET FULLAOT_LIBS=%FULLAOT_LIBS% %FULLAOT_TEST_LIBS% %FULLAOT_RUNTIME_TESTS%

)

IF /i "all" == "%BUILD_TARGET%" (

	SET FULLAOT_LIBS=%FULLAOT_LIBS% %FULLAOT_BCL_LIBS% %FULLAOT_TEST_LIBS% %FULLAOT_RUNTIME_TESTS%

)

IF "" == "%FULLAOT_LIBS%" (

	IF NOT "" == "%BUILD_TARGET%" (

		SET FULLAOT_LIBS=%BUILD_TARGET%
	)
)

FOR %%a IN (%FULLAOT_LIBS%) DO (

	del "%FULLAOT_DIR%\%%a.dll" >nul 2>&1
	ECHO Deleting "%FULLAOT_DIR%\%%a.dll".
	del "%FULLAOT_DIR%\%%a.dll.lib" >nul 2>&1
	ECHO Deleting "%FULLAOT_DIR%\%%a.dll.lib".
	del "%FULLAOT_DIR%\%%a.dll.exp" >nul 2>&1
	ECHO Deleting "%FULLAOT_DIR%\%%a.dll.exp".
	del "%FULLAOT_DIR%\%%a.dll.pdb" >nul 2>&1
	ECHO Deleting "%FULLAOT_DIR%\%%a.dll.pdb".
	del "%FULLAOT_DIR%\%%a.obj" >nul 2>&1
	ECHO Deleting "%FULLAOT_DIR%\%%a.obj".

	IF EXIST %MONO_WINAOT_BCL_PATH%\%%a (
		ECHO Copying %MONO_WINAOT_BCL_PATH%\%%a %FULLAOT_DIR%\%%a.
		copy %MONO_WINAOT_BCL_PATH%\%%a %FULLAOT_DIR%\%%a >nul 2>&1
	) ELSE (
		SET FOUND_TEST_TARGET_PATH=
		CALL :INNER_COPY_LOOP FOUND_TEST_TARGET_PATH %%a
		ECHO Copying !FOUND_TEST_TARGET_PATH! %FULLAOT_DIR%\%%a.
		copy !FOUND_TEST_TARGET_PATH! %FULLAOT_DIR%\%%a >nul 2>&1
	)
)

GOTO BUILD

:INNER_COPY_LOOP
FOR /d %%d IN (%MONO_TEST_PATH%\*) DO (
	IF EXIST %%d\%2 (
		SET %1=%%d\%2
		GOTO RETURN_INNER_COPY_LOOP
	)
)

IF EXIST %MONO_RUNTIME_TEST_PATH%\%2 (
	SET %1=%MONO_RUNTIME_TEST_PATH%\%2
)

:RETURN_INNER_COPY_LOOP
GOTO :EOF

:BUILD

SET FULLAOT_TEMP_DIR=%FULLAOT_DIR%\%%a-build
REM SET USE_LLVM=llvm

SET MONO_FULL_AOT_COMPILE_ARGS_TEMPLATE=--aot=full,temp-path=%FULLAOT_TEMP_DIR%,print-skipped,%USE_LLVM%,outfile=%FULLAOT_DIR%\%%a.dll
REM SET MONO_FULL_AOT_COMPILE_ARGS_TEMPLATE=--aot=full,temp-path=%FULLAOT_TEMP_DIR%,print-skipped,static,%USE_LLVM%,outfile=%FULLAOT_DIR%\%%a.obj,llvm-outfile=%FULLAOT_DIR%\%%a-llvm.obj
REM SET MONO_FULL_AOT_COMPILE_ARGS_TEMPLATE=--aot=full,temp-path=%FULLAOT_TEMP_DIR%,print-skipped,asmonly,%USE_LLVM%,outfile=%FULLAOT_DIR%\%%a.s,llvm-outfile=%FULLAOT_DIR%\%%a-llvm.s

IF "" == "%CLEAN%" (

	FOR %%a IN (%FULLAOT_LIBS%) DO (

		mkdir %FULLAOT_TEMP_DIR% >nul 2>&1

		ECHO %MONO_AOT_COMPILER_EXECUTABLE% %MONO_FULL_AOT_COMPILE_ARGS_TEMPLATE% %FULLAOT_DIR%\%%a.
		%MONO_AOT_COMPILER_EXECUTABLE% %MONO_FULL_AOT_COMPILE_ARGS_TEMPLATE% %FULLAOT_DIR%\%%a

		rmdir /S /Q %FULLAOT_TEMP_DIR%

		IF NOT ERRORLEVEL == 0 (
			ECHO "Failed Full AOT compile of %FULLAOT_DIR%\%%a".
			GOTO ON_ERROR
		)
	)
)

SET MONO_RESULT=0
GOTO ON_EXIT

:ON_ERROR
	ECHO Usage: build-full-aot-regression-tests.bat [bcl|tests|all|assembly path] [clean].
	SET MONO_RESULT=ERRORLEVEL
	GOTO ON_EXIT

:ON_EXIT
	SET PATH=%TEMP_PATH%
	EXIT /b %MONO_RESULT%

@ECHO ON
