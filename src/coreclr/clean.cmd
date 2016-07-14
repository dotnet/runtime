@if not defined __echo @echo off
setlocal EnableDelayedExpansion

echo Running clean.cmd

set bin=false
set packages=false
set tools = false

if [%1]==[] (
  set bin=true
  set packages=true
  set tools=true
  goto Begin
)

:Loop
if [%1]==[] goto Begin

if /I [%1] == [-?] goto Usage
if /I [%1] == [-help] goto Usage

if /I [%1] == [-p] (
    set packages=true
    set thisArgs=!thisArgs!%1
    goto Next
)

if /I [%1] == [-b] (
    set bin=true
    set thisArgs=!thisArgs!%1
    goto Next
)

if /I [%1] == [-t] (
    set tools=true
    set thisArgs=!thisArgs!%1
    goto Next
)

if /I [%1] == [-all] (
    set tools=true
    set bin=true
    set packages=true
    goto Begin
)

:Next
shift /1
goto Loop

:Begin
:: Set __ProjectDir to be the directory of this script
set "__ProjectDir=%~dp0"
:: remove trailing slash
if %__ProjectDir:~-1%==\ set "__ProjectDir=%__ProjectDir:~0,-1%"
set "__RootBinDir=%__ProjectDir%\bin"

if [%bin%] == [true] (
	if exist "%__RootBinDir%" (
		echo Deleting bin directory
		rd /s /q "%__RootBinDir%"
		if NOT [!ERRORLEVEL!]==[0] (
  			echo ERROR: An error occurred while deleting the bin directory - error code is !ERRORLEVEL!
  			exit /b 1
  		)
	)
)

if [%tools%] == [true] (
	if exist "%__ProjectDir%\Tools" (
		echo Deleting tools directory
		rd /s /q "%__ProjectDir%\Tools"
		if NOT [!ERRORLEVEL!]==[0] (
  			echo ERROR: An error occurred while deleting the Tools directory - error code is !ERRORLEVEL!
  			exit /b 1
  		)
  	)
)

if [%packages%] == [true] (
	if exist "%__ProjectDir%\packages" (
		echo Deleting packages directory
		rd /s /q "%__ProjectDir%\packages"
		if NOT [!ERRORLEVEL!]==[0] (
  			echo ERROR: An error occurred while deleting the packages directory - error code is !ERRORLEVEL!
  			exit /b 1
  		)
  	)
)

echo Clean was successful
exit /b 0

:Usage
echo.
echo Repository cleaning script.
echo Options:
echo     -b     - Cleans the bin directory
echo     -p     - Cleans the packages directory
echo     -t     - Cleans the tools directory
echo     -all   - Cleans everything
echo.
echo If no option is specified then clean.cmd -b -p -t is implied.
exit /b