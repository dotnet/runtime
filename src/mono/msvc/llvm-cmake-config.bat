@ECHO OFF

SET TEMP_PATH=%PATH%

REM Update PATH to include local cmake and phython installations.
REM SET PATH="C:\tools\cmake-3.10.2-win32-x86\bin";"C:\tools\python2.2.7.15\tools\";%PATH%

SET TOP=%1
IF "" == "%TOP%" (
	ECHO Error, first script parameter should be LLVM source folder.
	GOTO ON_ERROR
)

IF NOT EXIST "%TOP%" (
	ECHO Error, could not find "%TOP%".
	GOTO ON_ERROR
)

IF NOT EXIST "%~dp0mono.sln" (
	ECHO Error, script bust be located in same directory as mono.sln file.
	GOTO ON_ERROR
)

SET LLVM_SRC_PATH=%TOP%
SET LLVM_BUILD_PATH=%TOP%\llvm-build

REM Update to reflect value used in mono.props, MONO_LLVM_INSTALL_DIR_PREFIX property.
SET LLVM_INSTALL_PATH=%~dp0dist\llvm

SET CROSS_CMAKE_FLAGS=^
-DCMAKE_INSTALL_PREFIX="%LLVM_INSTALL_PATH%" ^
-DCMAKE_BUILD_TYPE=Release ^
-DLLVM_ENABLE_ZLIB=OFF ^
-DLLVM_TARGETS_TO_BUILD="X86" ^
-DCMAKE_CROSSCOMPILING=False ^
-DCMAKE_SYSTEM_NAME=Windows

SET TEMP_WD=%CD%
cd %LLVM_BUILD_PATH%
ECHO cmake.exe -G "Visual Studio 14 2015 Win64" %CROSS_CMAKE_FLAGS% %LLVM_SRC_PATH%
cmake.exe -G "Visual Studio 14 2015 Win64" %CROSS_CMAKE_FLAGS% %LLVM_SRC_PATH%
cd %TEMP_WD%

:ON_ERROR
	SET CONFIG_RESULT=ERRORLEVEL
	GOTO ON_EXIT

:ON_EXIT
	SET PATH=%TEMP_PATH%
	EXIT /b %CONFIG_RESULT%

@ECHO ON