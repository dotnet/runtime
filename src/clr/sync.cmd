@if not defined _echo @echo off
setlocal EnableDelayedExpansion

set "__args=%*"
set processedArgs=
set unprocessedArgs=
set __MSBuildArgs=

REM If no processed arguments are specified, -p is implied.
if "%1" == ""   (set __MSBuildArgs=.\build.proj /p:RestoreDuringBuild=true /t:Sync&goto ArgsDone)
if "%1" == "--" (set __MSBuildArgs=.\build.proj /p:RestoreDuringBuild=true /t:Sync&goto ArgsDone)

:ArgLoop

if "%1" == "" goto ArgsDone
if /I [%1] == [-?] goto Usage
if /I [%1] == [-help] goto Usage

REM This for loop splits the remaining arguments, preserving "=".
REM x gets the next argument, and y gets all remaining arguments after the first.
FOR /f "tokens=1*" %%x IN ("%*") DO (
    set param=%%x
    if /i "!param!" == "-p"                     (set __MSBuildArgs=!__MSBuildArgs! .\build.proj /p:RestoreDuringBuild=true /t:Sync)
    if /i "!param!" == "-ab"                    (set __MSBuildArgs=!__MSBuildArgs! .\src\syncAzure.proj)
    if /i "!param:~0,14!" == "-AzureAccount="   (set v=!param:~14!&set __MSBuildArgs=!__MSBuildArgs! /p:CloudDropAccountName=!v!)
    if /i "!param:~0,12!" == "-AzureToken="     (set v=!param:~12!&set __MSBuildArgs=!__MSBuildArgs! /p:CloudDropAccessToken=!v!)
    if /i "!param:~0,12!" == "-BuildMajor="     (set v=!param:~12!&set __MSBuildArgs=!__MSBuildArgs! /p:BuildNumberMajor=!v!)
    if /i "!param:~0,12!" == "-BuildMinor="     (set v=!param:~12!&set __MSBuildArgs=!__MSBuildArgs! /p:BuildNumberMinor=!v!)
    if /i "!param:~0,11!" == "-Container="      (set v=!param:~11!&set __MSBuildArgs=!__MSBuildArgs! /p:ContainerName=!v!)
    if /i "!param:~0,16!" == "-BlobNamePrefix=" (set v=!param:~16!&set __MSBuildArgs=!__MSBuildArgs! /p:__BlobNamePrefix=!v!)
    if /i "!param:~0,11!" == "-RuntimeId="      (set v=!param:~11!&set __MSBuildArgs=!__MSBuildArgs! /p:RuntimeId=!v!)
    REM all other arguments get passed through to msbuild unchanged.
    if /i not "!param:~0,1!" == "-"             (set __MSBuildArgs=!__MSBuildArgs! !param!)

    REM The innermost recursive invocation of :ArgLoop will execute
    REM msbuild, and all other invocations simply exit.
    call :ArgLoop %%y
    exit /b
)

:ArgsDone

@call %~dp0dotnet.cmd msbuild /nologo /verbosity:minimal /clp:Summary /nodeReuse:false /flp:v=detailed;LogFile=sync.log %__MSBuildArgs%
@exit /b %ERRORLEVEL%

:Usage
echo.
echo Repository syncing script.
echo.
echo Options:
echo     -?     - Prints Usage
echo     -help  - Prints Usage
echo     -p     - Restores all nuget packages for repository
echo     -ab    - Downloads the latests product packages from Azure.
echo              The following properties are required:
echo                 -AzureAccount="Account name"
echo                 -AzureToken="Access token"
echo              To download a specific group of product packages, specify:
echo                 -BuildMajor
echo                 -BuildMinor
echo              To download from a specific container, specify:
echo                 -Container="container name"
echo              To download blobs starting with a specific prefix, specify:
echo                 -BlobNamePrefix="Blob name prefix"
echo              To specify which RID you are downloading binaries for (optional):
echo                 -RuntimeId="RID" (Needs to match what's in the container)
echo.
echo.
echo.
echo If no option is specified then sync.cmd -p is implied.
