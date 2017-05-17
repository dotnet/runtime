# set the base tools directory
$toolsLocalPath = Join-Path $PSScriptRoot "Tools"
$restorePackagesPath = Join-Path $PSScriptRoot "packages"

$initTools = Join-Path $PSScriptRoot "init-tools.cmd"
& $initTools

# execute the tool using the dotnet.exe host
$dotNetExe = Join-Path $toolsLocalPath "dotnetcli\dotnet.exe"
$runExe = Join-Path $toolsLocalPath "run.exe"
& $dotNetExe $runExe $args
exit $LastExitCode