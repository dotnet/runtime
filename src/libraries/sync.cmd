@echo off
setlocal

set synclog=sync.log
echo Running Sync.cmd %* > %synclog%

if [%1]==[] (
  set src=true
  set packages=true
  goto Begin
)

set src=false
set packages=false

:Loop
if [%1]==[] goto Begin

if /I [%1]==[/?] goto Usage

if /I [%1] == [/p] (
    set packages=true
    goto Next
)

if /I [%1] == [/s] (
    set src=true
    goto Next
)

echo Unrecognized argument '%1'
goto Usage

:Next
shift /1
goto Loop

:Begin

echo Running init-tools.cmd
call %~dp0init-tools.cmd

if [%src%] == [true] (
  echo Fetching git database from remote repos ...
  call git fetch --all -p -v >> %synclog% 2>&1
  if NOT [%ERRORLEVEL%]==[0] (
    echo ERROR: An error occurred while fetching remote source code, see %synclog% for more details.
    exit /b
  )
)

if [%packages%] == [true] (
  echo Restoring all packages ...
  echo msbuild.exe %~dp0build.proj /t:BatchRestorePackages /nologo /v:minimal /p:RestoreDuringBuild=true /flp:v=detailed;Append;LogFile=%synclog% >> %synclog%
  call msbuild.exe %~dp0build.proj /t:BatchRestorePackages /nologo /v:minimal /p:RestoreDuringBuild=true /flp:v=detailed;Append;LogFile=%synclog%
  if NOT [%ERRORLEVEL%]==[0] (
    echo ERROR: An error occurred while syncing packages, see %synclog% for more details. There may have been networking problems so please try again in a few minutes.
    exit /b
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
echo.
echo If no option is specified then sync.cmd /s /p is implied.