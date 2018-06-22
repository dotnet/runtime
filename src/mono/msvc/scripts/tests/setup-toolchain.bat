SET SCRIPT_DIR=%~dp0

REM Look for Mono toolchain.
ECHO Searching for Mono toolchain...
IF NOT EXIST "%MONO_CROSS_COMPILER_HOME%\mono-sgen.exe" (
	ECHO Could not find "%MONO_CROSS_COMPILER_HOME%\mono-sgen.exe".
	EXIT /b 1
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
	CALL %SCRIPT_DIR%clang-vs2015-toolchain.bat
) ELSE (
	IF "%VisualStudioVersion%" == "15.0" (
		CALL %SCRIPT_DIR%clang-vs2017-toolchain.bat
	) ELSE (
		ECHO Failed to identify supported Visual Studio toolchain. Environment variable VisualStudioVersion must be set to 14.0 for VS2015 or 15.0 for VS2017. Checking supported toolchains for more error diagnostics...
		CALL %SCRIPT_DIR%clang-vs2015-toolchain.bat
		CALL %SCRIPT_DIR%clang-vs2017-toolchain.bat
		EXIT /b 1
	)
)

IF NOT ERRORLEVEL == 0 (
	EXIT /b %ERRORLEVEL%
)

SET PATH=%MONO_JIT_EXECUTABLE_PATH%;%MONO_AOT_RUNTIME_PATH%;%MONO_AOT_COMPILER_PATH%;%MONO_LLVM_EXECUTABLES%;%PATH%

EXIT /b 0