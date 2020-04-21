@echo off
setlocal 
set COMPOSITENAME=
set COMPILEARG=
set TESTINITIALBINPATH=%2
set TESTTARGET_DIR=%3
set TESTBATCHROOT=%1

:Loop
if "%4"=="" goto Continue
set COMPILEARG=%COMPILEARG% %TESTINITIALBINPATH%\%4.dll
set COMPOSITENAME=%COMPOSITENAME%%4
shift
goto Loop
:Continue

echo on
call %TESTBATCHROOT%\..\..\..\..\..\..\.dotnet\dotnet %CORE_ROOT%\crossgen2\crossgen2.dll -r %CORE_ROOT%\* -r %TESTINITIALBINPATH%\*.dll -o %TESTINITIALBINPATH%\%TESTTARGET_DIR%\%COMPOSITENAME%Composite.dll --composite %COMPILEARG%