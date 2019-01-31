@if not defined _echo @echo off
setlocal EnableDelayedExpansion


set "__args=%*"
set processedArgs=
set unprocessedArgs=
set __MSBuildArgs=

:ArgLoop

if "%1" == "" goto ArgsDone
if /I [%1] == [-?] goto Usage
if /I [%1] == [-help] goto Usage

REM This for loop splits the remaining arguments, preserving "=".
REM x gets the next argument, and y gets all remaining arguments after the first.
FOR /f "tokens=1*" %%x IN ("%*") DO (
    set param=%%x
    if /i "!param:~0,14!" == "-AzureAccount=" (set v=!param:~14!&set __MSBuildArgs=!__MSBuildArgs! /p:CloudDropAccountName=!v!)
    if /i "!param:~0,12!" == "-AzureToken="   (set v=!param:~12!&set __MSBuildArgs=!__MSBuildArgs! /p:CloudDropAccessToken=!v!)
    if /i "!param:~0,11!" == "-BuildArch="    (set v=!param:~11!&set __MSBuildArgs=!__MSBuildArgs! /p:__BuildArch=!v!)
    if /i "!param:~0,11!" == "-BuildType="    (set v=!param:~11!&set __MSBuildArgs=!__MSBuildArgs! /p:__BuildType=!v!)
    if /i "!param:~0,11!" == "-Container="    (set v=!param:~11!&set __MSBuildArgs=!__MSBuildArgs! /p:ContainerName=!v!)
    if /i "!param!" == "-PublishPackages"     (set __MSBuildArgs=!__MSBuildArgs! /p:__PublishPackages=true)
    if /i "!param!" == "-PublishSymbols"      (set __MSBuildArgs=!__MSBuildArgs! /p:__PublishSymbols=true)
    REM all other arguments get passed through to msbuild unchanged.
    if /i not "!param:~0,1!" == "-"           (set __MSBuildArgs=!__MSBuildArgs! !param!)

    REM The innermost recursive invocation of :ArgLoop will execute
    REM msbuild, and all other invocations simply exit.
    call :ArgLoop %%y
    exit /b
)

:ArgsDone

call %~dp0msbuild.cmd /nologo /verbosity:minimal /clp:Summary /nodeReuse:false /p:__BuildOS=Windows_NT^
  .\src\publish.proj^
  /flp:v=detailed;LogFile=publish-packages.log /clp:v=detailed %__MSBuildArgs%
@exit /b %ERRORLEVEL%

:Usage
echo.
echo Publishes the NuGet packages to the specified location.
echo   -?     - Prints Usage
echo   -help  - Prints Usage
echo For publishing to Azure the following properties are required.
echo   -AzureAccount="account name"
echo   -AzureToken="access token"
echo   -BuildType="Configuration"
echo   -BuildArch="Architecture"
echo For publishing to Azure, one of the following properties is required.
echo   -PublishPackages        Pass this switch to publish product packages 
echo   -PublishSymbols         Pass this switch to publish symbol packages
echo To specify the name of the container to publish into, use the following property:
echo   -Container="container name"
echo Architecture can be x64, x86, arm, or arm64
echo Configuration can be Release, Debug, or Checked
exit /b