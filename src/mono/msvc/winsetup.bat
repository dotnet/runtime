@ECHO off
SetLocal

SET CONFIG_H="%~dp0..\config.h"
SET CYG_CONFIG_H="%~dp0..\cygconfig.h"
SET WIN_CONFIG_H="%~dp0..\winconfig.h"
SET CONFIGURE_AC="%~dp0..\configure.ac"
SET VERSION_H="%~dp0..\mono\mini\version.h"

ECHO Setting up Mono configuration headers...

:: generate unique temp file path
uuidgen 2>nul || goto no_uuidgen
for /f %%a in ('uuidgen') do set monotemp=%%a
goto :got_temp

:no_uuidgen
:: Random isn't very random or unique. %time% and %date% is not random but fairly unique.
set monotemp=%~n0%random%%time%%date%

:got_temp
:: Remove special characters.
set monotemp=%monotemp:-=%
set monotemp=%monotemp:\=%
set monotemp=%monotemp:/=%
set monotemp=%monotemp::=%
set monotemp=%monotemp: =%
set monotemp=%monotemp:.=%
set monotemp=%temp%\monotemp%monotemp%
mkdir "%monotemp%\.." 2>nul
set monotemp="%monotemp%"
echo %monotemp%

REM Backup existing config.h into cygconfig.h if its not already replaced.
findstr /i /r /c:"#include *\"cygconfig.h\"" %config_h% >nul || copy /y %config_h% %cyg_config_h%

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

:: Remove every define VERSION from config.h and add what we want.
findstr /v /b /i /c:"#define PACKAGE_VERSION " /c:"#define VERSION " /c:"#define MONO_CORLIB_VERSION " %win_config_h% > %monotemp%
echo #define PACKAGE_VERSION "%MONO_VERSION%" >> %monotemp%
echo #define VERSION "%MONO_VERSION%" >> %monotemp%
echo #define MONO_CORLIB_VERSION "%MONO_CORLIB_VERSION%" >> %monotemp%

:: If the file is different, replace it.
fc %monotemp% %config_h% >nul || move /y %monotemp% %config_h%
del %monotemp% 2>nul

echo #define FULL_VERSION "Visual Studio built mono" > %monotemp%
fc %monotemp% %version_h% || move /y %monotemp% %version_h%
del %monotemp% 2>nul

:: Log environment variables that start "mono".
set MONO

ECHO Successfully setup Mono configuration headers.
EXIT /b 0
