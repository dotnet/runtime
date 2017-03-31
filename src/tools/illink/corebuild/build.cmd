@if not defined _echo @echo off

REM build.cmd will bootstrap the cli and ultimately call "dotnet build"

@call %~dp0dotnet.cmd build %dp0linker.sln %*
@exit /b %ERRORLEVEL%
