[CmdletBinding(PositionalBinding=$false)]
Param(
  [switch][Alias('h')]$help,
  [switch][Alias('t')]$test,
  [ValidateSet("Debug","Release","Checked")][string[]][Alias('c')]$configuration = @("Debug"),
  [string][Alias('f')]$framework,
  [string]$vs,
  [string][Alias('v')]$verbosity = "minimal",
  [ValidateSet("Windows_NT","Linux","OSX","Browser")][string]$os,
  [switch]$allconfigurations,
  [switch]$coverage,
  [string]$testscope,
  [switch]$testnobuild,
  [ValidateSet("x86","x64","arm","arm64","wasm")][string[]][Alias('a')]$arch = @([System.Runtime.InteropServices.RuntimeInformation]::ProcessArchitecture.ToString().ToLowerInvariant()),
  [Parameter(Position=0)][string][Alias('s')]$subset,
  [ValidateSet("Debug","Release","Checked")][string][Alias('rc')]$runtimeConfiguration,
  [ValidateSet("Debug","Release")][string][Alias('lc')]$librariesConfiguration,
  [Parameter(ValueFromRemainingArguments=$true)][String[]]$properties
)

function Get-Help() {
  Write-Host "Common settings:"
  Write-Host "  -arch (-a)                     Build platform: x86, x64, arm, arm64, or wasm."
  Write-Host "                                 Pass a comma-separated list to build for multiple architectures."
  Write-Host "                                 [Default: Your machine's architecture.]"
  Write-Host "  -binaryLog (-bl)               Output binary log."
  Write-Host "  -configuration (-c)            Build configuration: Debug, Release or [CoreCLR]Checked."
  Write-Host "                                 Pass a comma-separated list to build for multiple configurations."
  Write-Host "                                 [Default: Debug]"
  Write-Host "  -help (-h)                     Print help and exit."
  Write-Host "  -librariesConfiguration (-lc)  Libraries build configuration: Debug or Release."
  Write-Host "                                 [Default: Debug]"
  Write-Host "  -os                            Build operating system: Windows_NT, Linux, OSX, or Browser."
  Write-Host "                                 [Default: Your machine's OS.]"
  Write-Host "  -runtimeConfiguration (-rc)    Runtime build configuration: Debug, Release or [CoreCLR]Checked."
  Write-Host "                                 [Default: Debug]"
  Write-Host "  -subset (-s)                   Build a subset, print available subsets with -subset help."
  Write-Host "                                 '-subset' can be omitted if the subset is given as the first argument."
  Write-Host "                                 [Default: Builds the entire repo.]"
  Write-Host "  -verbosity (-v)                MSBuild verbosity: q[uiet], m[inimal], n[ormal], d[etailed], and diag[nostic]."
  Write-Host "                                 [Default: Minimal]"
  Write-Host "  -vs                            Open the solution with VS using the locally acquired SDK. Path or solution name."
  Write-Host "                                 (Example: -vs Microsoft.CSharp)"
  Write-Host ""

  Write-Host "Actions (defaults to -restore -build):"
  Write-Host "  -build (-b)             Build all source projects."
  Write-Host "  -clean                  Clean the solution."
  Write-Host "  -pack                   Package build outputs into NuGet packages."
  Write-Host "  -publish                Publish artifacts (e.g. symbols)."
  Write-Host "  -rebuild                Rebuild all source projects."
  Write-Host "  -restore                Restore dependencies."
  Write-Host "  -sign                   Sign build outputs."
  Write-Host "  -test (-t)              Build and run tests."
  Write-Host ""

  Write-Host "Libraries settings:"
  Write-Host "  -allconfigurations      Build packages for all build configurations."
  Write-Host "  -coverage               Collect code coverage when testing."
  Write-Host "  -framework (-f)         Build framework: net5.0 or net472."
  Write-Host "  -testnobuild            Skip building tests when invoking -test."
  Write-Host "  -testscope              Scope tests, allowed values: innerloop, outerloop, all."
  Write-Host ""

  Write-Host "Command-line arguments not listed above are passed through to MSBuild."
  Write-Host "The above arguments can be shortened as much as to be unambiguous."
  Write-Host "(Example: -con for configuration, -t for test, etc.)."
  Write-Host ""

  Write-Host "Here are some quick examples:"
  Write-Host ""
  Write-Host "Build CoreCLR on Windows for x64 on release configuration:"
  Write-Host ".\build.cmd clr -os Windows_NT -arch x64 -configuration Release"
  Write-Host ""
  Write-Host "Cross-compile CoreCLR runtime on Windows for ARM64."
  Write-Host ".\build.cmd -subset clr.runtime -os Windows_NT -arch arm64 -configuration Release"
  Write-Host ""
  Write-Host "Build Debug libraries with a Release runtime."
  Write-Host ".\build.cmd -subset clr+libs -os Windows_NT -arch x64 -runtimeConfiguration Release"
  Write-Host ""
  Write-Host "For more information, check out https://github.com/dotnet/runtime/blob/master/docs/workflow/README.md"
}

if ($help -or (($null -ne $properties) -and ($properties.Contains('/help') -or $properties.Contains('/?')))) {
  Get-Help
  exit 0
}

if ($subset -eq 'help') {
  $argumentsForHelp = "-restore -build /p:subset=help /clp:nosummary"
  Invoke-Expression "& `"$PSScriptRoot/common/build.ps1`" $argumentsForHelp"
  exit 0
}

if ($vs) {
  . $PSScriptRoot\common\tools.ps1

  if (-Not (Test-Path $vs)) {
    $solution = $vs
    # Search for the solution in libraries
    $vs = Split-Path $PSScriptRoot -Parent | Join-Path -ChildPath "src\libraries" | Join-Path -ChildPath $vs | Join-Path -ChildPath "$vs.sln"
    if (-Not (Test-Path $vs)) {
      $vs = $solution
      # Search for the solution in installer
      if (-Not ($vs.endswith(".sln"))) {
        $vs = "$vs.sln"
      }
      $vs = Split-Path $PSScriptRoot -Parent | Join-Path -ChildPath "src\installer" | Join-Path -ChildPath $vs
      if (-Not (Test-Path $vs)) {
        Write-Error "Passed in solution cannot be resolved."
        exit 1
      }
    }
  }

  # This tells .NET Core to use the bootstrapped runtime
  $env:DOTNET_ROOT=InitializeDotNetCli -install:$false

  # This tells MSBuild to load the SDK from the directory of the bootstrapped SDK
  $env:DOTNET_MSBUILD_SDK_RESOLVER_CLI_DIR=$env:DOTNET_ROOT

  # This tells .NET Core not to go looking for .NET Core in other places
  $env:DOTNET_MULTILEVEL_LOOKUP=0;

  # Put our local dotnet.exe on PATH first so Visual Studio knows which one to use
  $env:PATH=($env:DOTNET_ROOT + ";" + $env:PATH);

  if ($runtimeConfiguration)
  {
    # Respect the RuntimeConfiguration variable for building inside VS with different runtime configurations
    $env:RUNTIMECONFIGURATION=$runtimeConfiguration
  }

  # Launch Visual Studio with the locally defined environment variables
  ."$vs"

  exit 0
}

# Check if an action is passed in
$actions = "b","build","r","restore","rebuild","sign","testnobuild","publish","clean"
$actionPassedIn = @(Compare-Object -ReferenceObject @($PSBoundParameters.Keys) -DifferenceObject $actions -ExcludeDifferent -IncludeEqual).Length -ne 0
if ($null -ne $properties -and $actionPassedIn -ne $true) {
  $actionPassedIn = @(Compare-Object -ReferenceObject $properties -DifferenceObject $actions.ForEach({ "-" + $_ }) -ExcludeDifferent -IncludeEqual).Length -ne 0
}

if (!$actionPassedIn) {
  $arguments = "-restore -build"
}

foreach ($argument in $PSBoundParameters.Keys)
{
  switch($argument)
  {
    "runtimeConfiguration"   { $arguments += " /p:RuntimeConfiguration=$((Get-Culture).TextInfo.ToTitleCase($($PSBoundParameters[$argument])))" }
    "librariesConfiguration" { $arguments += " /p:LibrariesConfiguration=$((Get-Culture).TextInfo.ToTitleCase($($PSBoundParameters[$argument])))" }
    "framework"              { $arguments += " /p:BuildTargetFramework=$($PSBoundParameters[$argument].ToLowerInvariant())" }
    "os"                     { $arguments += " /p:TargetOS=$($PSBoundParameters[$argument])" }
    "allconfigurations"      { $arguments += " /p:BuildAllConfigurations=true" }
    "properties"             { $arguments += " " + $properties }
    "verbosity"              { $arguments += " -$argument " + $($PSBoundParameters[$argument]) }
    # configuration and arch can be specified multiple times, so they should be no-ops here
    "configuration"          {}
    "arch"                   {}
    default                  { $arguments += " /p:$argument=$($PSBoundParameters[$argument])" }
  }
}

$failedBuilds = @()

foreach ($config in $configuration) {
  $argumentsWithConfig = $arguments + " -configuration $((Get-Culture).TextInfo.ToTitleCase($config))";
  foreach ($singleArch in $arch) {
    $argumentsWithArch =  "/p:TargetArchitecture=$singleArch " + $argumentsWithConfig
    $env:__DistroRid="win-$singleArch"
    Invoke-Expression "& `"$PSScriptRoot/common/build.ps1`" $argumentsWithArch"
    if ($lastExitCode -ne 0) {
        $failedBuilds += "Configuration: $config, Architecture: $singleArch"
    }
  }
}

if ($failedBuilds.Count -ne 0) {
    Write-Host "Some builds failed:"
    foreach ($failedBuild in $failedBuilds) {
        Write-Host "`t$failedBuild"
    }
    exit 1
}

exit 0
