@if not defined _echo @echo off
setlocal EnableDelayedExpansion

set "__ProjectDir=%~dp0"

set "__args=%*"
set processedArgs=
set unprocessedArgs=
set __MSBuildArgs=

:Arg_Loop
if "%1" == "" goto ArgsDone
if /I [%1] == [/?] goto Usage
if /I [%1] == [/help] goto Usage

REM CMD eats "=" on the argument list.
REM TODO: remove all -Property=Value type arguments here once we get rid of them in buildpipeline.
if /i "%1" == "-BuildArch"       (set processedArgs=!processedArgs! %1=%2&set __MSBuildArgs=!__MSBuildArgs! /p:__BuildArch=%2&shift&shift&goto Arg_Loop)
if /i "%1" == "-BuildType"       (set processedArgs=!processedArgs! %1=%2&set __MSBuildArgs=!__MSBuildArgs! /p:__BuildType=%2&shift&shift&goto Arg_Loop)
if /i "%1" == "-OfficialBuildId" (set processedArgs=!processedArgs! %1=%2&set __MSBuildArgs=!__MSBuildArgs! /p:OfficialBuildId=%2&shift&shift&goto Arg_Loop)
if /i "%1" == "--"               (set processedArgs=!processedArgs! %1&shift)

REM handle any unprocessed arguments, assumed to go only after the processed arguments above
if [!processedArgs!]==[] (
   set unprocessedArgs=%__args%
) else (
   set unprocessedArgs=%__args%
   for %%t in (!processedArgs!) do (
   REM strip out already-processed arguments from unprocessedArgs
   set unprocessedArgs=!unprocessedArgs:*%%t=!
   )
)

:ArgsDone

set logFile=%__ProjectDir%bin\Logs\build-packages.binlog
powershell -NoProfile -ExecutionPolicy ByPass -NoLogo -File "%~dp0eng\common\build.ps1"^
  -r -b -projects %__ProjectDir%src\.nuget\packages.builds^
  -verbosity minimal /bl:%logFile% /nodeReuse:false^
  /p:__BuildOS=Windows_NT /p:ArcadeBuild=true^
  /p:PortableBuild=true /p:FilterToOSGroup=Windows_NT^
  %__MSBuildArgs% %unprocessedArgs%

if NOT [!ERRORLEVEL!]==[0] (
  echo ERROR: An error occurred while building packages. See log for more details:
  echo     %logFile%
  exit /b !ERRORLEVEL!
)

echo Done Building Packages.
exit /b

:Usage
echo.
echo Builds the NuGet packages from the binaries that were built in the Build product binaries step.
echo The following properties are required to define build architecture
echo   -BuildArch=[architecture] -BuildType=[configuration]
echo Architecture can be x64, x86, arm, or arm64
echo Configuration can be Release, Debug, or Checked
exit /b
