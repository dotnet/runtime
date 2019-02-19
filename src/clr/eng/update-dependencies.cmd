@if not defined _echo @echo off
setlocal

:: Clear the 'Platform' env variable for this session, as it's a per-project setting within the build, and
:: misleading value (such as 'MCD' in HP PCs) may lead to build breakage (issue: #69).
set Platform=

:: Restore the Tools directory
call "%~dp0..\init-tools.cmd"
if NOT [%ERRORLEVEL%]==[0] (
  exit /b 1
)

set "DotNetCli=%~dp0..\Tools\dotnetcli\dotnet.exe"
set "DependenciesBuildProj=%~dp0..\tests\build.proj"

echo Running: "%DotNetCli%" msbuild "%DependenciesBuildProj%" %*
call "%DotNetCli%" msbuild "%DependenciesBuildProj%" %*
if NOT [%ERRORLEVEL%]==[0] (
  exit /b 1
)

exit /b 0
