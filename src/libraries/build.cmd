@echo off
setlocal

:: Note: We've disabled node reuse because it causes file locking issues.
::       The issue is that we extend the build with our own targets which
::       means that that rebuilding cannot successfully delete the task
::       assembly. 

if not defined VisualStudioVersion (
    if defined VS140COMNTOOLS (
        call "%VS140COMNTOOLS%\VsDevCmd.bat"
        goto :EnvSet
    )

    if defined VS120COMNTOOLS (
        call "%VS120COMNTOOLS%\VsDevCmd.bat"
        goto :EnvSet
    )

    echo Error: build.cmd requires Visual Studio 2013 or 2015.  
    echo        Please see https://github.com/dotnet/corefx/blob/master/Documentation/project-docs/developer-guide.md for build instructions.
    exit /b 1
)

:EnvSet

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
%_buildprefix% msbuild "%_buildproj%" /nologo /maxcpucount /verbosity:minimal /nodeReuse:false /fileloggerparameters:Verbosity=normal;LogFile="%_buildlog%";Append %* %_buildpostfix%
set BUILDERRORLEVEL=%ERRORLEVEL%
goto :eof

:AfterBuild

echo.
:: Pull the build summary from the log file
findstr /ir /c:".*Warning(s)" /c:".*Error(s)" /c:"Time Elapsed.*" "%_buildlog%"
echo Build Exit Code = %BUILDERRORLEVEL%

exit /b %BUILDERRORLEVEL%