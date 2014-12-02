@echo off
setlocal

:: Note: We've disabled node reuse because it causes file locking issues.
::       The issue is that we extend the build with our own targets which
::       means that that rebuilding cannot successully delete the task
::       assembly. 

:: Check prerequisites
set _msbuildexe="%ProgramFiles(x86)%\MSBuild\12.0\Bin\MSBuild.exe"
if not exist %_msbuildexe% set _msbuildexe="%ProgramFiles%\MSBuild\12.0\Bin\MSBuild.exe"
if not exist %_msbuildexe% set _msbuildexe="%ProgramFiles(x86)%\MSBuild\14.0\Bin\MSBuild.exe"
if not exist %_msbuildexe% set _msbuildexe="%ProgramFiles%\MSBuild\14.0\Bin\MSBuild.exe"
if not exist %_msbuildexe% echo Error: Could not find MSBuild.exe.  Please see https://github.com/dotnet/corefx/wiki/Developer%%20Guide for build instructions. && exit /b 1

if not defined VSINSTALLDIR echo Error: build.cmd should be run from a Visual Studio Command Prompt.  Please see https://github.com/dotnet/corefx/wiki/Developer%%20Guide for build instructions. && exit /b 1

:: Log build command line
set _buildprefix=echo
set _buildpostfix=^> "%~dp0msbuild.log"
call :build %*

:: Build
set _buildprefix=
set _buildpostfix=
call :build %*

goto :eof

:build
%_buildprefix% %_msbuildexe% "%~dp0build.proj" /nologo /maxcpucount /verbosity:minimal /nodeReuse:false /fileloggerparameters:Verbosity=diag;LogFile="%~dp0msbuild.log";Append %* %_buildpostfix%
goto :eof