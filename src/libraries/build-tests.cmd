@if "%_echo%" neq "on" echo off
setlocal

set buildTests=build-tests.log
echo Running build-tests.cmd %* > %buildTests%

if /I [%1] == [/?] goto Usage
if /I [%1] == [/help] goto Usage

echo msbuild.exe %~dp0src\tests.builds /nologo /maxcpucount /v:minimal /clp:Summary /nodeReuse:false /flp:v=detailed;Append;LogFile=%buildTests% %*>> %buildTests%
call msbuild.exe %~dp0src\tests.builds /nologo /maxcpucount /v:minimal /clp:Summary /nodeReuse:false /flp:v=detailed;Append;LogFile=%buildTests% %*
if NOT [%ERRORLEVEL%]==[0] (
  echo ERROR: An error occurred while building the tests, see %buildTests% for more details.
  exit /b
)

echo Done Building tests.
exit /b

:Usage
echo.
echo Builds the tests that are in the repository.
echo No option parameters.
exit /b