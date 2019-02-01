@echo off
powershell -ExecutionPolicy ByPass -NoProfile -File "%~dp0eng\build.ps1" %*
exit /b %ERRORLEVEL%
