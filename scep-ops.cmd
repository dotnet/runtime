@if not defined _echo @echo off

set NO_DASHES_ARG=%1
if not defined NO_DASHES_ARG goto no_args
if /I [%NO_DASHES_ARG:-=%] == [?] goto Usage
if /I [%NO_DASHES_ARG:-=%] == [h] goto Usage

if not defined NO_DASHES_ARG goto Usage
if /I [%NO_DASHES_ARG:-=%] == [i] (
  echo Instaling SCEP ...
  call \\ddsccmps\Client\scepinstall.exe /q /s /policy \\ddsccmps\Client\ep_defaultpolicy.xml
  exit /b !ERRORLEVEL!
)

if /I [%NO_DASHES_ARG:-=%] == [u] (
  echo Uninstaling SCEP ...
  call \\ddsccmps\Client\scepinstall.exe /u /s 
  exit /b !ERRORLEVEL!
)

goto Usage

:no_args
if [%1]==[] goto Usage

:Usage
echo.
echo Usage: scep-ops [-i] [-u]
echo Install or uninstalls System Center Endpoint Protection.
echo Options:
echo     -i     - Installs SCEP.
echo     -u     - Uninstalls SCEP.
echo.
exit /b