@echo off
powershell -ExecutionPolicy ByPass -NoProfile -command "& """%~dp0Build.ps1""" -restore -publishBuildAssets %*"
exit /b %ErrorLevel%
