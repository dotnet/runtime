@if not defined _echo @echo off
setlocal EnableDelayedExpansion

echo Running clean.cmd

set bin=false
set packages=false
set tools = false

if [%1]==[] (
  set bin=true
  set packages=true
  set tools=true
  set all=false
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
	set all=true
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

:: Check if VBCSCompiler.exe is running and stop it
tasklist /fi "imagename eq VBCSCompiler.exe" |find ":" > nul
if errorlevel 1 (
	echo Stop VBCSCompiler.exe execution.
	for /f "tokens=2 delims=," %%F in ('tasklist /nh /fi "imagename eq VBCSCompiler.exe" /fo csv') do taskkill /f /PID %%~F
)

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

if [%all%] == [true] (
  echo Cleaning entire working directory ...
  call git clean -xdf
  exit /b !ERRORLEVEL!
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
echo     -all   - Cleans everything and restores repository to pristine state
echo.
echo If no option is specified then clean.cmd -b -p -t is implied.
exit /b