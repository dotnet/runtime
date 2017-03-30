@if not defined _echo @echo off
setlocal 
:: To run tests outside of MSBuild.exe
:: %1 is the path to the bin\<OSConfig> folder
:: %2 is the path to the tools\testdotnetcli folder

set LOCATION=%1
set RUNTIME_PATH=%2

if "%LOCATION%" == "" set LOCATION=%~dp0\bin\Windows_NT.AnyCPU.Debug
if "%RUNTIME_PATH%" == "" set RUNTIME_PATH=%~dp0\bin\runtime\netcoreapp-Windows_NT-Debug-x64

pushd %LOCATION%

FOR /D %%F IN (*.Tests) DO (
	IF EXIST %%F\netcoreapp (
		pushd %%F\netcoreapp
                @echo Looking in %cd%...
		IF EXIST RunTests.cmd (
                        @echo ... found tests
			CALL RunTests.cmd %RUNTIME_PATH%
		)
		popd
	)
)

popd
