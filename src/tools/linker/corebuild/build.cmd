@if not defined _echo @echo off

REM build.cmd will bootstrap the cli and ultimately call "dotnet build".
REM If no configuration is specified, the default configuration will be
REM set to netcore_Debug (see config.json).

@call run.cmd build "'-Project=..\linker\Mono.Linker.csproj'" %*
@exit /b %ERRORLEVEL%
