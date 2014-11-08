@echo off

:: Note: We've disabled node reuse because it causes file locking issues.
::       The issue is that we extend the build with our own targets which
::       means that that rebuilding cannot successully delete the task
::       assembly.

:: Log build command line
set buildprefix=echo
set buildpostfix=^> "%~dp0msbuild.log"
call :build %*

:: Build
set buildprefix=
set buildpostfix=
call :build %*

goto :eof

:build
%buildprefix% "%ProgramFiles(x86)%\MSBuild\12.0\Bin\MSBuild.exe" "%~dp0build.proj" /nologo /maxcpucount /verbosity:minimal /nodeReuse:false /fileloggerparameters:Verbosity=diag;LogFile="%~dp0msbuild.log";Append %* %buildpostfix%
goto :eof
