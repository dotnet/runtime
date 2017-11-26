@echo off
setlocal EnableDelayedExpansion

:: Set the default arguments for script generation.
set __RuntimeRoot=`$(TestRoot)\Runtimes\Coreclr1
set __NugetCacheDir=`$(WorkingDir)\packages
set __CliPath=
set __ConfigFileName=Debugger.Tests.Config.txt
set __TemplateFileName=%~dp0\ConfigTemplate.txt

:Arg_Loop
if "%1" == "" goto ArgsDone
if /i "%1" == "/?" goto Usage

if /i "%1" == "rt"      (set "__RuntimeRoot=%2"&shift&shift&goto Arg_Loop)
if /i "%1" == "nc"      (set "__NugetCacheDir=%2"&shift&shift&goto Arg_Loop)
if /i "%1" == "cli"     (set "__CliPath=%2"&shift&shift&goto Arg_Loop)

echo Invalid commandline argument: %1
goto Usage

:ArgsDone

if not exist %__TemplateFileName% (
    echo Template file %__TemplateFileName% doesn't exist.
    exit /b 1
)

:: Delete previous config file.
if exist %__ConfigFileName% (
    echo Deleting current config file.
    del %__ConfigFileName%
)

:: powershell "Get-Content %__TemplateFileName% -replace (""##Insert_Runtime_Root##"", ""%__RuntimeRoot%"") | Output-File %__ConfigFileName% "
powershell -NoProfile "(Get-Content \"%__TemplateFileName%\")`"^
    "-replace \"##Insert_Runtime_Root##\", \"%__RuntimeRoot%\" `"^
    "|ForEach-Object{$_ -replace \"##Insert_Nuget_Cache_Root##\", \"%__NugetCacheDir%\"} `"^
    "|ForEach-Object{$_ -replace \"##Cli_Path##\", \"%__CliPath%\"} `"^
    "| Out-File \"%__ConfigFileName%\""

exit /b 0

:Usage
echo.
echo Usage:
echo %0 [rt ^<runtime_path^>] [nc ^<nuget_cache_path^>] [cli ^<cli_path^>] where:
echo.
echo ^<runtime_path^>: path to the runtime that you want to use for testing.
echo ^<nuget_cache_path^>: path to the nuget cache.
echo ^<cli_path^>: path to the cli tool.
exit /b 1
endlocal
