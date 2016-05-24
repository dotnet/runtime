@if "%_echo%" neq "on" echo off
setlocal EnableDelayedExpansion

set synclog=sync.log
echo Running Sync.cmd %* > %synclog%

set options=/nologo /v:minimal /clp:Summary /flp:v=detailed;Append;LogFile=%synclog%

set "__args= %*"
set processedArgs=
set unprocessedBuildArgs=

set src=false
set packages=false
set azureBlobs=false

if [%1]==[] (
  set src=true
  set packages=true
  goto Begin
)

:Loop
if [%1]==[] goto Begin

if /I [%1] == [/?] goto Usage
if /I [%1] == [/help] goto Usage

if /I [%1] == [/p] (
    set packages=true
    set processedArgs=!processedArgs! %1
    goto Next
)

if /I [%1] == [/s] (
    set src=true
    set processedArgs=!processedArgs! %1
    goto Next
)

if /I [%1] == [/ab] (
    set azureBlobs=true
    set processedArgs=!processedArgs! %1
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

:Begin
echo Running init-tools.cmd
call %~dp0init-tools.cmd

if [%src%] == [true] (
  echo Fetching git database from remote repos ...
  call git fetch --all -p -v >> %synclog% 2>&1
  if NOT [!ERRORLEVEL!]==[0] (
    echo ERROR: An error occurred while fetching remote source code, see %synclog% for more details.
    exit /b 1
  )
)

if [%azureBlobs%] == [true] (
  echo Connecting and downloading packages from Azure BLOB ...
  echo msbuild.exe %~dp0src\syncAzure.proj !options! !unprocessedBuildArgs! >> %synclog%
  call msbuild.exe %~dp0src\syncAzure.proj !options! !unprocessedBuildArgs!
  if NOT [!ERRORLEVEL!]==[0] (
    echo ERROR: An error occurred while downloading packages from Azure BLOB, see %synclog% for more details. There may have been networking problems so please try again in a few minutes.
    exit /b 1
  )
)

set targets=RestoreNETCorePlatforms

if [%packages%] == [true] (
  set options=!options! /t:!targets! /p:RestoreDuringBuild=true
  echo msbuild.exe %~dp0build.proj !options! !unprocessedBuildArgs! >> %synclog%
  call msbuild.exe %~dp0build.proj !options! !unprocessedBuildArgs!
  if NOT [!ERRORLEVEL!]==[0] (
    echo ERROR: An error occurred while syncing packages, see %synclog% for more details. There may have been networking problems so please try again in a few minutes.
    exit /b 1
  )
)

echo Done Syncing.
exit /b 0

goto :EOF

:Usage
echo.
echo Repository syncing script.
echo.
echo Options:
echo     /s     - Fetches source history from all configured remotes
echo              (git fetch --all -p -v)
echo     /p     - Restores all nuget packages for repository
echo     /ab    - Downloads the latests product packages from Azure.
echo              The following properties are required:
echo                 /p:CloudDropAccountName="Account name"
echo                 /p:CloudDropAccessToken="Access token"
echo              To download a specific group of product packages, specify:
echo                 /p:BuildNumberMajor
echo                 /p:BuildNumberMinor
echo.
echo.
echo If no option is specified then sync.cmd /s /p is implied.