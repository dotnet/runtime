@ECHO OFF

SETLOCAL

SET USE_LLVM=yes
SET FULL_AOT_MODE=asmonly

CALL %~dp0build-full-aot-regression-tests.bat %*

@ECHO ON
