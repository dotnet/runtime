@echo off
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0WasmBatchRunner.ps1" %*
exit /b %ERRORLEVEL%
