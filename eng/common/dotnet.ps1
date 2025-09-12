# This script is used to install the .NET SDK.
# It will also invoke the SDK with any provided arguments.

. $PSScriptRoot\tools.ps1
$dotnetRoot = InitializeDotNetCli -install:$true

$env:DOTNET_CLI_USE_MSBUILD_SERVER=1
$env:MSBUILDUSESERVER=1

# Invoke acquired SDK with args if they are provided

if ($args.count -gt 0) {
  $env:DOTNET_NOLOGO=1
  & "$dotnetRoot\dotnet.exe" $args
}
