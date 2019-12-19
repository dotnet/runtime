@echo off

powershell -ExecutionPolicy ByPass -NoProfile -File "%~dp0eng\build.ps1" %*
goto end

:end
exit /b %ERRORLEVEL%
