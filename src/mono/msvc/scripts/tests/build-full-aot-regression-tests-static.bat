@ECHO OFF

SETLOCAL

SET FULL_AOT_MODE=static

CALL %~dp0build-full-aot-regression-tests.bat %*

@ECHO ON
