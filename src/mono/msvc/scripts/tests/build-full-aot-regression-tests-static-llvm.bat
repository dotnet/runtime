@ECHO OFF

SETLOCAL

SET USE_LLVM=yes
SET FULL_AOT_MODE=static

CALL %~dp0build-full-aot-regression-tests.bat %*

@ECHO ON
