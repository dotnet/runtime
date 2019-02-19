@if not defined _echo @echo off

REM build.cmd will bootstrap the cli and ultimately call "dotnet pack"

@call %~dp0dotnet.cmd pack %~dp0..\src\ILLink.Tasks\ILLink.Tasks.csproj %*
@exit /b %ERRORLEVEL%
