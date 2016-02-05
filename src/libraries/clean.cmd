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
set clean_successful=true

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

echo Running init-tools.cmd
call %~dp0init-tools.cmd

if /I [%clean_src%] == [true] (
  echo Cleaning src directory ...
  echo. >> %cleanlog% && echo git clean -xdf %~dp0src >> %cleanlog%
  call git clean -xdf %~dp0src >> %cleanlog%
  call :CheckErrorLevel
)

if NOT "%clean_targets%" == "" (
  echo Running msbuild clean targets "%clean_targets:~0,-1%" ...
  echo. >> %cleanlog% && echo msbuild.exe %~dp0build.proj /t:%clean_targets:~0,-1% /nologo /v:minimal /flp:v=detailed;Append;LogFile=%cleanlog% >> %cleanlog%
  call msbuild.exe %~dp0build.proj /t:%clean_targets:~0,-1% /nologo /v:minimal /flp:v=detailed;Append;LogFile=%cleanlog%
  call :CheckErrorLevel
)

if /I [%clean_tools%] == [true] (
  echo Cleaning tools directory ...
  echo. >> %cleanlog% && echo rmdir /s /q %~dp0tools >> %cleanlog%
  rmdir /s /q %~dp0tools >> %cleanlog%
  REM Don't call CheckErrorLevel because if the Tools directory didn't exist when this script was
  REM invoked, then it sometimes exits with error level 3 despite successfully deleting the directory.
)

if /I [%clean_all%] == [true] (
  echo Cleaning entire working directory ...
  echo. >> %cleanlog% && echo git clean -xdf -e clean.log %~dp0 >> %cleanlog%
  call git clean -xdf -e clean.log %~dp0 >> %cleanlog%
  call :CheckErrorLevel
)

if /I [%clean_successful%] == [true] (
  echo Clean completed successfully.
  echo. >> %cleanlog% && echo Clean completed successfully. >> %cleanlog%
  exit /b 0
) else (
  echo An error occured while cleaning; see %cleanlog% for more details.
  echo. >> %cleanlog% && echo Clean completed with errors. >> %cleanlog%
  exit /b 1
)

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

exit /b 1

:CheckErrorLevel
if NOT [%ERRORLEVEL%]==[0] (
  echo Command exited with ERRORLEVEL %ERRORLEVEL% >> %cleanlog%
  set clean_successful=false
)
exit /b