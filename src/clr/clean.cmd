@if not defined __echo @echo off
setlocal EnableDelayedExpansion

echo Running clean.cmd

if /I [%1] == [/?] goto Usage
if /I [%1] == [/help] goto Usage

:: Set __ProjectDir to be the directory of this script
set "__ProjectDir=%~dp0"
:: remove trailing slash
if %__ProjectDir:~-1%==\ set "__ProjectDir=%__ProjectDir:~0,-1%"
set "__RootBinDir=%__ProjectDir%\bin"

if exist "%__RootBinDir%"           rd /s /q "%__RootBinDir%"
if exist "%__ProjectDir%\Tools"     rd /s /q "%__ProjectDir%\Tools"

exit /b 0

:Usage
echo.
echo Repository cleaning script.
echo No option parameters.
exit /b