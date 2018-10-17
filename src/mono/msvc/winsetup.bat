@ECHO off
SetLocal

SET CONFIG_H="%~dp0..\config.h"
SET CYG_CONFIG_H="%~dp0..\cygconfig.h"
SET WIN_CONFIG_H="%~dp0..\winconfig.h"
SET CONFIGURE_AC="%~dp0..\configure.ac"
SET VERSION_H="%~dp0..\mono\mini\version.h"

ECHO Setting up Mono configuration headers...

:: generate unique temp file path
uuidgen > nul 2>&1 || goto no_uuidgen
for /f %%a in ('uuidgen') do set CONFIG_H_TEMP=%%a
goto :got_temp

:no_uuidgen
:: Random isn't very random or unique. %time% and %date% is not random but fairly unique.
set CONFIG_H_TEMP=%~n0%random%%time%%date%

:got_temp
:: Remove special characters.
set CONFIG_H_TEMP=%CONFIG_H_TEMP:-=%
set CONFIG_H_TEMP=%CONFIG_H_TEMP:\=%
set CONFIG_H_TEMP=%CONFIG_H_TEMP:/=%
set CONFIG_H_TEMP=%CONFIG_H_TEMP::=%
set CONFIG_H_TEMP=%CONFIG_H_TEMP: =%
set CONFIG_H_TEMP=%CONFIG_H_TEMP:.=%
set CONFIG_H_TEMP=%temp%\CONFIG_H_TEMP%CONFIG_H_TEMP%
mkdir "%CONFIG_H_TEMP%\.." 2>nul
set CONFIG_H_TEMP="%CONFIG_H_TEMP%"

REM Backup existing config.h into cygconfig.h if its not already replaced.
findstr /i /r /c:"#include *\"cygconfig.h\"" %CONFIG_H% >nul || copy /y %CONFIG_h% %CYG_CONFIG_H%

:: Extract MONO_VERSION from configure.ac.
for /f "delims=[] tokens=2" %%a in ('findstr /b /c:"AC_INIT(mono, [" %CONFIGURE_AC%') do (
	set MONO_VERSION=%%a
)

:: Split MONO_VERSION into three parts.
for /f "delims=. tokens=1-3" %%a in ('echo %MONO_VERSION%') do (
	set MONO_VERSION_MAJOR=%%a
	set MONO_VERSION_MINOR=%%b
	set MONO_VERSION_PATCH=%%c
)
:: configure.ac hardcodes this.
set MONO_VERSION_PATCH=00

:: Extract MONO_CORLIB_VERSION from configure.ac.
for /f "tokens=*" %%a in ('findstr /b /c:MONO_CORLIB_VERSION= %CONFIGURE_AC%') do set %%a

:: Pad out version pieces to 2 characters with zeros on left.
if "%MONO_VERSION_MAJOR:~1%" == "" set MONO_VERSION_MAJOR=0%MONO_VERSION_MAJOR%
if "%MONO_VERSION_MINOR:~1%" == "" set MONO_VERSION_MINOR=0%MONO_VERSION_MINOR%

:: Remove every define VERSION from winconfig.h and add what we want.
findstr /v /b /i /c:"#define PACKAGE_VERSION " /c:"#define VERSION " /c:"#define MONO_CORLIB_VERSION " %WIN_CONFIG_H% > %CONFIG_H_TEMP%

: Setup dynamic section of config.h
echo #ifdef _MSC_VER >> %CONFIG_H_TEMP%
echo #define PACKAGE_VERSION "%MONO_VERSION%" >> %CONFIG_H_TEMP%
echo #define VERSION "%MONO_VERSION%" >> %CONFIG_H_TEMP%
echo #define MONO_CORLIB_VERSION "%MONO_CORLIB_VERSION%" >> %CONFIG_H_TEMP%

:: Add dynamic configuration parameters set in original config.h affecting msvc build.
for /f "tokens=*" %%a in ('findstr /i /r /c:".*#define.*HAVE_BTLS.*1" %CYG_CONFIG_H%') do (
	echo #define HAVE_BTLS 1 >> %CONFIG_H_TEMP%
)

for /f "tokens=*" %%a in ('findstr /i /r /c:".*#define.*LLVM.*" %CYG_CONFIG_H%') do (
	echo %%a >> %CONFIG_H_TEMP%
)

echo #endif >> %CONFIG_H_TEMP%

:: If the file is different, replace it.
fc %CONFIG_H_TEMP% %CONFIG_H% >nul 2>&1 || move /y %CONFIG_H_TEMP% %CONFIG_H%
del %CONFIG_H_TEMP% 2>nul

echo #define FULL_VERSION "Visual Studio built mono" > %CONFIG_H_TEMP%
fc %CONFIG_H_TEMP% %VERSION_H% >nul 2>&1 || move /y %CONFIG_H_TEMP% %VERSION_H%
del %CONFIG_H_TEMP% 2>nul

:: Log environment variables that start "mono".
set MONO

ECHO Successfully setup Mono configuration headers.
EXIT /b 0
