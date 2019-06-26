@ECHO OFF

SETLOCAL
SETLOCAL ENABLEDELAYEDEXPANSION

SET TEMP_PATH=%PATH%
SET MONO_RESULT=1

SET BUILD_TARGET=%1
SET CLEAN=%2

IF NOT "" == "%BUILD_TARGET%" (
	SET BUILD_TARGET=%BUILD_TARGET:"=%
)

CALL %~dp0setup-env.bat
IF NOT ERRORLEVEL == 0 (
	ECHO Failed to setup mono paths.
	GOTO ON_ERROR
)

CALL %~dp0setup-toolchain.bat
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

REM When building Full AOT using net_4x BCL Mono SIMD tests are available.
SET MONO_ENABLE_SIMD_TESTS=0
IF /i %MONO_BCL_PATH% == %MONO_WINAOT_BCL_PATH% (
	SET MONO_ENABLE_SIMD_TESTS=1
)

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
Mono.Security.dll ^
I18N.dll ^
I18N.West.dll ^
MemoryIntrinsics.dll

IF %MONO_ENABLE_SIMD_TESTS% == 1 (
	SET FULLAOT_BCL_LIBS=^
	%FULLAOT_BCL_LIBS% ^
	System.Configuration.dll ^
	Mono.Simd.dll
)

SET FULLAOT_TEST_LIBS=^
TestDriver.dll ^
generics-variant-types.dll

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

IF %MONO_ENABLE_SIMD_TESTS% == 1 (
	SET FULLAOT_RUNTIME_TESTS=^
	%FULLAOT_RUNTIME_TESTS% ^
	basic-simd.exe
)

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
	IF EXIST %MONO_WINAOT_BCL_PATH%\%%a (
		IF NOT "" == "%CLEAN%" (
			CALL :DELETE_FULLAOT_LIB %FULLAOT_DIR% %%a
		) ELSE (
			fc.exe /B %MONO_WINAOT_BCL_PATH%\%%a %FULLAOT_DIR%\%%a >nul 2>&1 && (
				ECHO %FULLAOT_DIR%\%%a already up to date.
			) || (
				CALL :DELETE_FULLAOT_LIB %FULLAOT_DIR% %%a
				ECHO Copying %MONO_WINAOT_BCL_PATH%\%%a %FULLAOT_DIR%\%%a.
				copy %MONO_WINAOT_BCL_PATH%\%%a %FULLAOT_DIR%\%%a >nul 2>&1
			)
		)
	) ELSE (
		IF NOT "" == "%CLEAN%" (
			CALL :DELETE_FULLAOT_LIB %FULLAOT_DIR% %%a
		) ELSE (
			SET FOUND_TEST_TARGET_PATH=
			CALL :INNER_COPY_LOOP FOUND_TEST_TARGET_PATH %%a
			fc.exe /B !FOUND_TEST_TARGET_PATH! %FULLAOT_DIR%\%%a >nul 2>&1 && (
				ECHO %FULLAOT_DIR%\%%a already up to date.
			) || (
				CALL :DELETE_FULLAOT_LIB %FULLAOT_DIR% %%a
				ECHO Copying !FOUND_TEST_TARGET_PATH! %FULLAOT_DIR%\%%a.
				copy !FOUND_TEST_TARGET_PATH! %FULLAOT_DIR%\%%a >nul 2>&1
			)
		)
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

:DELETE_FULLAOT_LIB

ECHO Deleting "%1\%2.dll".
del "%1\%2.dll" >nul 2>&1
ECHO Deleting "%1\%2.dll.lib".
del "%1\%2.dll.lib" >nul 2>&1
ECHO Deleting "%1\%2.dll.exp".
del "%1\%2.dll.exp" >nul 2>&1
ECHO Deleting "%1\%2.dll.pdb".
del "%1\%2.dll.pdb" >nul 2>&1
ECHO Deleting "%1\%2.obj".
del "%1\%2.obj" >nul 2>&1
ECHO Deleting "%1\%2.s".
del "%1\%2.s" >nul 2>&1
ECHO Deleting "%1\%2-llvm.s".
del "%1\%2.s.bc" >nul 2>&1
ECHO Deleting "%1\%2-llvm.s.bc".
del "%1\%2.s.opt.bc" >nul 2>&1
ECHO Deleting "%1\%2-llvm.s.opt.bc".
del "%1\%2-llvm.s" >nul 2>&1
ECHO Deleting "%1\%2-llvm.obj".
del "%1\%2-llvm.obj" >nul 2>&1

GOTO :EOF

:BUILD

SET FULLAOT_TEMP_DIR=%FULLAOT_DIR%\%%a-build

IF "" == "%FULL_AOT_MODE%" (
	SET FULL_AOT_MODE=dynamic
)

IF /i "yes" == "%USE_LLVM%" (
	SET LLVM_ARG=llvm
)

IF /i "static" == "%FULL_AOT_MODE%" (
	SET MONO_FULL_AOT_COMPILE_ARGS_TEMPLATE=--aot=full,temp-path=%FULLAOT_TEMP_DIR%,print-skipped,static,%LLVM_ARG%,outfile=%FULLAOT_DIR%\%%a.obj,llvm-outfile=%FULLAOT_DIR%\%%a-llvm.obj
)

IF /i "asmonly"  == "%FULL_AOT_MODE%" (
	SET MONO_FULL_AOT_COMPILE_ARGS_TEMPLATE=--aot=full,temp-path=%FULLAOT_TEMP_DIR%,print-skipped,asmonly,%LLVM_ARG%,outfile=%FULLAOT_DIR%\%%a.s,llvm-outfile=%FULLAOT_DIR%\%%a-llvm.s
)

IF /i "dynamic"  == "%FULL_AOT_MODE%" (
	SET MONO_FULL_AOT_COMPILE_ARGS_TEMPLATE=--aot=full,temp-path=%FULLAOT_TEMP_DIR%,print-skipped,%LLVM_ARG%,outfile=%FULLAOT_DIR%\%%a.dll
)

IF "" == "%CLEAN%" (

	FOR %%a IN (%FULLAOT_LIBS%) DO (

		IF NOT EXIST %FULLAOT_DIR%\%%a.obj (

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
