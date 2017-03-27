@if not defined _echo @echo off

REM build.cmd will bootstrap the cli and ultimately call "dotnet build"..

@call dotnet.cmd build ..\linker\Mono.Linker.csproj -c netcore_Debug %*
@exit /b %ERRORLEVEL%
