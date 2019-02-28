@if not defined _echo @echo off

REM restore.sh will bootstrap the cli and ultimately call "dotnet
REM restore". Dependencies of the linker will get restored as well.

@call powershell %~dp0..\eng\common\msbuild.ps1 /t:Restore %~dp0..\illink.sln %*
@exit /b %ERRORLEVEL%
