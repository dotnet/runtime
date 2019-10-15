@if not defined _echo @echo off
setlocal

set "DotNetCli=%~dp0..\dotnet.cmd"
set "DependenciesBuildProj=%~dp0..\tests\build.proj"

echo Running: "%DotNetCli%" msbuild "%DependenciesBuildProj%" %*
call "%DotNetCli%" msbuild "%DependenciesBuildProj%" %*
if NOT [%ERRORLEVEL%]==[0] (
  exit /b 1
)

exit /b 0
