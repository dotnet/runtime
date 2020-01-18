REM Look for Clang VS2015 toolchain in VS installation folders.
ECHO Searching for Clang in VS2015 toolchain...

IF "%VCINSTALLDIR%" == "" (
	ECHO VCINSTALLDIR environment variable not set.
	GOTO ON_ENV_ERROR
)

IF /i NOT "%Platform%" == "X64" (
	ECHO Platform environment variable not set to X64.
	GOTO ON_ENV_ERROR
)

IF NOT "%VisualStudioVersion%" == "14.0" (
	ECHO VisualStudioVersion environment variable not set to 14.0.
	GOTO ON_ENV_ERROR
)

SET CLANGC2_TOOLS_BIN_PATH=%VCINSTALLDIR%\ClangC2\bin\amd64\
SET CLANGC2_TOOLS_BIN=%CLANGC2_TOOLS_BIN_PATH%clang.exe
IF NOT EXIST "%CLANGC2_TOOLS_BIN%" (
	ECHO Could not find "%CLANGC2_TOOLS_BIN%"
	GOTO ON_ERROR
)

ECHO Found "%CLANGC2_TOOLS_BIN%"

ECHO Searching for Linker in VS2015 toolchain...

SET LINK_TOOLS_BIN_PATH=%VCINSTALLDIR%bin\amd64\
SET LINK_TOOLS_BIN=%LINK_TOOLS_BIN_PATH%link.exe
IF NOT EXIST "%LINK_TOOLS_BIN%" (
	ECHO Could not find "%LINK_TOOLS_BIN%"
	GOTO ON_ERROR
)

ECHO Found "%LINK_TOOLS_BIN%"

SET COMPILER_TOOLS_BIN="%LINK_TOOLS_BIN_PATH%";"%CLANGC2_TOOLS_BIN_PATH%"
SET PATH=%COMPILER_TOOLS_BIN%;%PATH%

SET MONO_RESULT=0
GOTO ON_EXIT

:ON_ENV_ERROR

SET VC_VARS_ALL_FILE=%ProgramFiles(x86)%\Microsoft Visual Studio 14.0\VC\vcvarsall.bat

ECHO Make sure to run this from a "VS2015 x64 Native Tools Command Prompt" command prompt.
IF EXIST "%VC_VARS_ALL_FILE%" (
	ECHO Setup a "VS2015 x64 Native Tools Command Prompt" command prompt by using "%VC_VARS_ALL_FILE%" amd64.
)

:ON_ERROR

SET MONO_RESULT=1

:ON_EXIT

EXIT /b %MONO_RESULT%
