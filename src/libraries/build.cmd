@echo off
setlocal

:: Note: We've disabled node reuse because it causes file locking issues.
::       The issue is that we extend the build with our own targets which
::       means that that rebuilding cannot successfully delete the task
::       assembly.

:ReadArguments
:: Read in the args to determine whether to run the native build, managed build, or both (default)
set "__args=%*"
if /i [%1] == [native] (set __buildSpec=native&&set "__args=%__args:~6%"&&shift&&goto Tools)
if /i [%1] == [managed] (set __buildSpec=managed&&set "__args=%__args:~7%"&&shift&&goto Tools)

:Tools
:: Setup VS
if not defined VisualStudioVersion (
    if defined VS140COMNTOOLS (
        call "%VS140COMNTOOLS%\VsDevCmd.bat"
        goto :Build
    )

    echo Error: build.cmd requires Visual Studio 2015.
    echo        Please see https://github.com/dotnet/corefx/blob/master/Documentation/project-docs/developer-guide.md for build instructions.
    exit /b 1
)

:Build
:: Restore the Tools directory
call %~dp0init-tools.cmd

:: Call the builds
if "%__buildSpec%"=="managed"  goto :BuildManaged

:BuildNative
:: Run the Native Windows build
echo [%time%] Building Native Libraries...
call %~dp0src\native\Windows\build-native.cmd %__args% >nativebuild.log
IF ERRORLEVEL 1 (
    echo Native component build failed see nativebuild.log for more details.
) else (
    echo [%time%] Successfully built Native Libraries.
)

:: If we only wanted to build the native components, exit
if "%__buildSpec%"=="native" goto :eof 

:BuildManaged
:: Clear the 'Platform' env variable for this session,
:: as it's a per-project setting within the build, and
:: misleading value (such as 'MCD' in HP PCs) may lead
:: to build breakage (issue: #69).
set Platform=

:: Log build command line
set _buildproj=%~dp0build.proj
set _buildlog=%~dp0msbuild.log
set _buildprefix=echo
set _buildpostfix=^> "%_buildlog%"
call :build %__args%

:: Build
set _buildprefix=
set _buildpostfix=
echo [%time%] Building Managed Libraries...
call :build %__args%

goto :AfterBuild

:build
%_buildprefix% msbuild "%_buildproj%" /nologo /maxcpucount /v:minimal /clp:Summary /nodeReuse:false /flp:v=normal;LogFile="%_buildlog%";Append %__args% %_buildpostfix%
set BUILDERRORLEVEL=%ERRORLEVEL%
goto :eof

:AfterBuild

echo.
:: Pull the build summary from the log file
findstr /ir /c:".*Warning(s)" /c:".*Error(s)" /c:"Time Elapsed.*" "%_buildlog%"
echo [%time%] Build Exit Code = %BUILDERRORLEVEL%

exit /b %BUILDERRORLEVEL%