@if "%_echo%" neq "on" echo off
setlocal EnableDelayedExpansion

:: Note: We've disabled node reuse because it causes file locking issues.
::       The issue is that we extend the build with our own targets which
::       means that that rebuilding cannot successfully delete the task
::       assembly.

:ReadArguments
:: Read in the args to determine whether to run the native build, managed build, or both (default)
set OfficialBuildIdArg=
set "__args= %*"
set processedArgs=
set unprocessedBuildArgs=

:Loop
if [%1]==[] goto Tools

if /I [%1]==[native] (
    set __buildSpec=native
    set processedArgs=!processedArgs! %1
    goto Next
)

if /I [%1] == [managed] (
    set __buildSpec=managed
    set processedArgs=!processedArgs! %1
    goto Next
)

if /I [%1] == [/p:OfficialBuildId] (
    if /I [%2]==[] (
        echo Error: officialbuildid arg should have a value
        exit /b 1
    )
    set processedArgs=!processedArgs! %1=%2
    set OfficialBuildIdArg=/p:OfficialBuildId=%2
    shift /1
    goto Next
)

if [!processedArgs!]==[] (
  call set unprocessedBuildArgs=!__args!
) else (
  call set unprocessedBuildArgs=%%__args:*!processedArgs!=%%
)

:Next
shift /1
goto Loop

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
:: Generate Native versioning assets
set __binDir=%~dp0bin
set __versionLog=%~dp0version.log
msbuild "%~dp0build.proj" /nologo /t:GenerateVersionHeader /p:NativeVersionHeaderFile="%__binDir%\obj\_version.h" /p:GenerateVersionHeader=true %OfficialBuildIdArg% > "%__versionLog%"
IF EXIST "%~dp0src\native\Windows\build-native.cmd" (
    call %~dp0src\native\Windows\build-native.cmd %__args% >nativebuild.log
    IF ERRORLEVEL 1 (
        echo Native component build failed see nativebuild.log for more details.
    ) else (
        echo [%time%] Successfully built Native Libraries.
    )
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
set _binclashLoggerDll=%~dp0Tools\net45\Microsoft.DotNet.Build.Tasks.dll
set _binclashlog=%~dp0binclash.log
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
%_buildprefix% msbuild "%_buildproj%" /nologo /maxcpucount /v:minimal /clp:Summary /nodeReuse:false /flp:v=normal;LogFile="%_buildlog%";Append "/l:BinClashLogger,%_binclashLoggerDll%;LogFile=%_binclashlog%" !unprocessedBuildArgs! %_buildpostfix% %OfficialBuildIdArg%
set BUILDERRORLEVEL=%ERRORLEVEL%
goto :eof

:AfterBuild

echo.
:: Pull the build summary from the log file
findstr /ir /c:".*Warning(s)" /c:".*Error(s)" /c:"Time Elapsed.*" "%_buildlog%"
echo [%time%] Build Exit Code = %BUILDERRORLEVEL%

exit /b %BUILDERRORLEVEL%
