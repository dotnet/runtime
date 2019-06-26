@ECHO OFF

SETLOCAL

SET FULL_AOT_MODE=asmonly

CALL %~dp0build-full-aot-regression-tests.bat %*

@ECHO ON
