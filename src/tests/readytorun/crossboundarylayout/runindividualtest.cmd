setlocal 
rd /s /q %2\%3 2> nul
md %2\%3
copy %2\* %2\%3 > nul
call %1\buildcrossgen2image.cmd %*
@echo on
%CORE_ROOT%\corerun %2\%3\crossboundarytest.dll
@echo off
if %ERRORLEVEL% NEQ 100 (
  echo FAILED
  goto done
)
echo PASSED
:done