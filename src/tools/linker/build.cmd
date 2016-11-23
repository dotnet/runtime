@if not defined _echo @echo off
setlocal

call %~dp0run.cmd build -Project=linker\Mono.Linker.csproj
exit /b %ERRORLEVEL%
