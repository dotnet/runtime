REM Look for Clang VS2019 toolchain in VS installation folders.
ECHO Searching for Clang in VS2019 toolchain...

IF "%VCINSTALLDIR%" == "" (
	ECHO VCINSTALLDIR environment variable not set.
	GOTO ON_ENV_ERROR
)

IF /i NOT "%VSCMD_ARG_TGT_ARCH%" == "x64" (
	ECHO VSCMD_ARG_TGT_ARCH environment variable not set to x64.
	GOTO ON_ENV_ERROR
)

IF NOT "%VisualStudioVersion%" == "16.0" (
	ECHO VisualStudioVersion environment variable not set to 16.0.
	GOTO ON_ENV_ERROR
)

SET CLANG_TOOLS_BIN_PATH=%VCINSTALLDIR%Tools\llvm\bin\
SET CLANG_TOOLS_BIN=%CLANG_TOOLS_BIN_PATH%clang.exe
IF NOT EXIST "%CLANG_TOOLS_BIN%" (
	ECHO Could not find "%CLANG_TOOLS_BIN%".
	GOTO ON_ERROR
)

ECHO Found "%CLANG_TOOLS_BIN%".

ECHO Searching for Linker in VS2019 toolchain...

SET LINK_TOOLS_BIN_PATH=%VCToolsInstallDir%bin\HostX64\x64\
SET LINK_TOOLS_BIN=%LINK_TOOLS_BIN_PATH%link.exe
IF NOT EXIST "%LINK_TOOLS_BIN%" (
	ECHO Could not find "%LINK_TOOLS_BIN%".
	GOTO ON_ERROR
)

ECHO Found "%LINK_TOOLS_BIN%".

SET COMPILER_TOOLS_BIN="%LINK_TOOLS_BIN_PATH%";"%CLANGC2_TOOLS_BIN_PATH%"
SET PATH=%COMPILER_TOOLS_BIN%;%PATH%

SET MONO_RESULT=0
GOTO ON_EXIT

:ON_ENV_ERROR

SET VSWHERE_TOOLS_BIN=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe

ECHO Make sure to run this from a "x64 Native Tools Command Prompt for VS2019" command prompt.
IF EXIST "%VSWHERE_TOOLS_BIN%" (
	FOR /F "tokens=*" %%a IN ('"%VSWHERE_TOOLS_BIN%" -version [16.0^,17.0] -prerelease -property installationPath') DO (
		ECHO Setup a "x64 Native Tools Command Prompt for VS2019 command prompt by using "%%a\VC\Auxiliary\Build\vcvars64.bat".
	)
)

:ON_ERROR

SET MONO_RESULT=1

:ON_EXIT

EXIT /b %MONO_RESULT%
