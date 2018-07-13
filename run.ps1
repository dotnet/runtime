# set the base tools directory
$toolsLocalPath = Join-Path $PSScriptRoot "Tools"
$restorePackagesPath = Join-Path $PSScriptRoot "packages"

# We do not want to run the first-time experience.
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = 1

# Don't resolve runtime, shared framework, or SDK from other locations to ensure build determinism
$env:DOTNET_MULTILEVEL_LOOKUP=0

$initTools = Join-Path $PSScriptRoot "init-tools.cmd"
& $initTools

# execute the tool using the dotnet.exe host
$dotNetExe = Join-Path $toolsLocalPath "dotnetcli\dotnet.exe"
$runExe = Join-Path $toolsLocalPath "run.exe"
$runConfig = Join-Path $PSScriptRoot "config.json"
& $dotNetExe $runExe $runConfig $args
exit $LastExitCode