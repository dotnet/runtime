@echo off
powershell -ExecutionPolicy ByPass -NoProfile %~dp0eng\build.ps1 %*
exit /b %ERRORLEVEL%
