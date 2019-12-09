function Get-DotNetPath() {
  Set-Content -Path (Join-Path $ToolsetDir 'sdk.txt') -Value $dotnetPath -Force
}

. $PSScriptRoot\eng\common\tools.ps1
$dotnetPath=InitializeDotNetCli($true)

# Invoke dotnet.exe if script hasn't been dot-sourced
if ($MyInvocation.InvocationName -ne ".") {
  # Clear the 'Platform' env variable for this session, as it's a per-project setting within the build, and
  # misleading value (such as 'MCD' in HP PCs) may lead to build breakage (issue: #69).
  $env:Platform=

  # Don't resolve runtime, shared framework, or SDK from other locations to ensure build determinism
  $env:DOTNET_MULTILEVEL_LOOKUP=0

  # Disable first run since we want to control all package sources
  $env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1

  Invoke-Expression "& $dotnetPath\dotnet.exe $args"

  exit $lastExitCode
}
