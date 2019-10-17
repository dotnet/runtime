SET SCRIPT_DIR=%~dp0

REM Look for Mono toolchain.
ECHO Searching for Mono toolchain...
IF NOT EXIST "%MONO_CROSS_COMPILER_HOME%\mono-sgen.exe" (
	ECHO Could not find "%MONO_CROSS_COMPILER_HOME%\mono-sgen.exe".
	GOTO ON_ERROR
)

ECHO Found "%MONO_CROSS_COMPILER_HOME%\mono-sgen.exe".

SET MONO_AOT_COMPILER_PATH=%MONO_CROSS_COMPILER_HOME%
SET MONO_AOT_COMPILER_EXECUTABLE=%MONO_AOT_COMPILER_PATH%\mono-sgen.exe
SET MONO_AOT_RUNTIME_PATH=%MONO_AOT_COMPILER_PATH%
SET MONO_AOT_RUNTIME_EXECUTABLE=%MONO_AOT_COMPILER_EXECUTABLE%
SET MONO_JIT_EXECUTABLE_PATH=%MONO_AOT_COMPILER_PATH%
SET MONO_JIT_EXECUTABLE=%MONO_AOT_COMPILER_EXECUTABLE%
SET MONO_LLVM_EXECUTABLES=%MONO_DIST_DIR%\llvm\bin

REM Setup toolchain.
IF "%VisualStudioVersion%" == "14.0" (
	CALL %SCRIPT_DIR%clang-vs2015-toolchain.bat || (
		GOTO ON_ERROR
	)
	GOTO SETUP_PATH
)

IF "%VisualStudioVersion%" == "15.0" (
	CALL %SCRIPT_DIR%clang-vs2017-toolchain.bat  || (
		GOTO ON_ERROR
	)
	GOTO SETUP_PATH
)

IF "%VisualStudioVersion%" == "16.0" (
	CALL %SCRIPT_DIR%clang-vs2019-toolchain.bat  || (
		GOTO ON_ERROR
	)
	GOTO SETUP_PATH
)

ECHO Failed to identify supported Visual Studio toolchain. Environment variable VisualStudioVersion must be set to 14.0 for VS2015, 15.0 for VS2017 or 16.0 for VS2019. Checking supported toolchains for more error diagnostics...
GOTO ON_ERROR

:SETUP_PATH

SET PATH=%MONO_JIT_EXECUTABLE_PATH%;%MONO_AOT_RUNTIME_PATH%;%MONO_AOT_COMPILER_PATH%;%MONO_LLVM_EXECUTABLES%;%PATH%

GOTO ON_EXIT

:ON_ERROR
	EXIT /b 1

:ON_EXIT
	EXIT /b 0
