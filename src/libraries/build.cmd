@echo off
setlocal

:: Note: We've disabled node reuse because it causes file locking issues.
::       The issue is that we extend the build with our own targets which
::       means that that rebuilding cannot successfully delete the task
::       assembly. 

:: Check prerequisites
if not defined VS120COMNTOOLS (
    if not defined VS140COMNTOOLS (
        echo Error: build.cmd should be run from a Visual Studio 2013 or 2015 Command Prompt.  
        echo        Please see https://github.com/dotnet/corefx/wiki/Developer-Guide for build instructions.
        exit /b 1
    )
)

:: Log build command line
set _buildproj=%~dp0build.proj
set _buildlog=%~dp0msbuild.log
set _buildprefix=echo
set _buildpostfix=^> "%_buildlog%"
call :build %*

:: Build
set _buildprefix=
set _buildpostfix=
call :build %*

goto :AfterBuild

:build
%_buildprefix% msbuild "%_buildproj%" /nologo /maxcpucount /verbosity:minimal /nodeReuse:false /fileloggerparameters:Verbosity=diag;LogFile="%_buildlog%";Append %* %_buildpostfix%
set BUILDERRORLEVEL=%ERRORLEVEL%
goto :eof

:AfterBuild

echo.
:: Pull the build summary from the log file
findstr /ir /c:".*Warning(s)" /c:".*Error(s)" /c:"Time Elapsed.*" "%_buildlog%"
echo Build Exit Code = %BUILDERRORLEVEL%

exit /b %BUILDERRORLEVEL%