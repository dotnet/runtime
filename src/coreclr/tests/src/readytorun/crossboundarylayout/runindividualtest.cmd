rem @echo off
setlocal 
rd /s /q %2\%3
md %2\%3
copy %2\* %2\%3 > nul
call %1\buildcrossgen2image.cmd %1 %2 %3 %4 %5 %6 %7
@echo on
%CORE_ROOT%\corerun %2\%3\crossboundarytest.dll
@echo off
if %ERRORLEVEL% NEQ 100 (
  echo FAILED
  goto done
)
echo PASSED
:done