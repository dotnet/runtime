@ECHO OFF

SETLOCAL

SET FULL_AOT_MODE=dynamic

CALL %~dp0build-full-aot-regression-tests.bat %*

@ECHO ON
