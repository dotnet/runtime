@echo off
setlocal

:: Call setup_vs_tools with the '1' flag to tell it to only check for the
:: VS installation, and not launch the Dev Prompt. More details are in that
:: script source file.

call "%~dp0src\coreclr\setup_vs_tools.cmd" 1

set _args=%*
if "%~1"=="-?" set _args=-help

powershell -ExecutionPolicy ByPass -NoProfile -Command "& '%~dp0eng\build.ps1'" %_args%
exit /b %ERRORLEVEL%
