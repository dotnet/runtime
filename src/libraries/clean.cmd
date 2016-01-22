@echo off
setlocal

set cleanlog=%~dp0clean.log
echo Running Clean.cmd %* > %cleanlog%

if [%1] == [] (
  set clean_targets=Clean;
  goto Begin
)

set clean_targets=
set clean_src=
set clean_tools=
set clean_all=

:Loop
if [%1] == [] goto Begin

if /I [%1] == [/?] goto Usage

if /I [%1] == [/b] (
  set clean_targets=Clean;%clean_targets%
  goto Next
)

if /I [%1] == [/p] (
  set clean_targets=CleanPackages;%clean_targets%
  goto Next
)

if /I [%1] == [/c] (
  set clean_targets=CleanPackagesCache;%clean_targets%
  goto Next
)

if /I [%1] == [/s] (
  set clean_src=true
  goto Next
)

if /I [%1] == [/t] (
  set clean_tools=true
  goto Next
)

if /I [%1] == [/all] (
  set clean_src=
  set clean_tools=
  set clean_targets=Clean;CleanPackages;CleanPackagesCache;
  set clean_all=true
  goto Begin
)

echo Unrecognized argument '%1'
goto Usage

:Next
shift /1
goto Loop

:Begin

call %~dp0init-tools.cmd

if /I [%clean_src%] == [true] (
  echo Cleaning src directory ...
  call git clean %~dp0src -xdf >> %cleanlog%
)

if NOT "%clean_targets%" == "" (
  echo Running msbuild clean targets "%clean_targets:~0,-1%" ...
  echo msbuild.exe %~dp0build.proj /t:%clean_targets:~0,-1% /nologo /v:minimal /flp:v=detailed;Append;LogFile=%cleanlog% >> %cleanlog%
  call msbuild.exe %~dp0build.proj /t:%clean_targets:~0,-1% /nologo /v:minimal /flp:v=detailed;Append;LogFile=%cleanlog%
  if NOT [%ERRORLEVEL%]==[0] (
    echo ERROR: An error occurred while cleaning, see %cleanlog% for more details.
    exit /b
  )
)

if /I [%clean_tools%] == [true] (
  echo Cleaning tools directory ...
  rmdir /s /q %~dp0tools >> %cleanlog%dir
)

if /I [%clean_all%] == [true] (
  echo Cleaning entire working directory ...
  call git clean %~dp0 -xdf >> %cleanlog%
)

echo Done Cleaning.
exit /b 0

:Usage
echo.
echo Repository cleaning script.
echo.
echo Options:
echo     /b     - Deletes the binary output directory.
echo     /p     - Deletes the repo-local nuget package directory.
echo     /c     - Deletes the user-local nuget package cache.
echo     /t     - Deletes the tools directory.
echo     /s     - Deletes the untracked files under src directory (git clean src -xdf).
echo     /all   - Combines all of the above.
echo.
echo If no option is specified then clean.cmd /b is implied.