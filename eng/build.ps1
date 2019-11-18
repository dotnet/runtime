[CmdletBinding(PositionalBinding=$false)]
Param(
  [switch][Alias('b')]$build,
  [switch][Alias('t')]$test,
  [switch]$buildtests,
  [string][Alias('c')]$configuration = "Debug",
  [string][Alias('f')]$framework,
  [string]$vs,
  [string]$os,
  [switch]$allconfigurations,
  [switch]$coverage,
  [string]$testscope,
  [string]$arch,
  [string]$subsetCategory,
  [string]$subset,
  [Parameter(ValueFromRemainingArguments=$true)][String[]]$properties
)

function Get-Help() {
  Write-Host "Common settings:"
  Write-Host "  -subset                 Build a subset, print availabe subsets with -subset help"
  Write-Host "  -os                     Build operating system: Windows_NT or Unix"
  Write-Host "  -arch                   Build platform: x86, x64, arm or arm64"
  Write-Host "  -configuration <value>  Build configuration: Debug or Release (short: -c)"
  Write-Host "  -verbosity <value>      MSBuild verbosity: q[uiet], m[inimal], n[ormal], d[etailed], and diag[nostic] (short: -v)"
  Write-Host "  -binaryLog              Output binary log (short: -bl)"
  Write-Host "  -help                   Print help and exit (short: -h)"
  Write-Host ""

  Write-Host "Actions (defaults to -restore -build):"
  Write-Host "  -restore                Restore dependencies (short: -r)"
  Write-Host "  -build                  Build all source projects (short: -b)"
  Write-Host "  -buildtests             Build all test projects"
  Write-Host "  -rebuild                Rebuild all source projects"
  Write-Host "  -test                   Run all unit tests (short: -t)"
  Write-Host "  -pack                   Package build outputs into NuGet packages"
  Write-Host "  -sign                   Sign build outputs"
  Write-Host "  -publish                Publish artifacts (e.g. symbols)"
  Write-Host "  -clean                  Clean the solution"
  Write-Host ""

  Write-Host "Libraries settings:"
  Write-Host "  -vs                     Open the solution with VS for Test Explorer support. Path or solution name (ie -vs Microsoft.CSharp)"
  Write-Host "  -framework              Build framework: netcoreapp or netfx (short: -f)"
  Write-Host "  -coverage               Collect code coverage when testing"
  Write-Host "  -testscope              Scope tests, allowed values: innerloop, outerloop, all"
  Write-Host "  -allconfigurations      Build packages for all build configurations"
  Write-Host ""

  Write-Host "Command-line arguments not listed above are passed thru to msbuild."
  Write-Host "The above arguments can be shortened as much as to be unambiguous (e.g. -con for configuration, -t for test, etc.)."
}

# Exit if script has been dot-sourced
if ($MyInvocation.InvocationName -eq ".") {
  exit 0
}

# VS Test Explorer support for libraries
if ($vs) {
  if (-Not (Test-Path $vs)) {
    $vs = Join-Path "$PSScriptRoot\..\src\libraries" $vs | Join-Path -ChildPath "$vs.sln"
  }

  $archTestHost = if ($arch) { $arch } else { "x64" }

  # This tells .NET Core to use the same dotnet.exe that build scripts use
  $env:DOTNET_ROOT="$PSScriptRoot\..\artifacts\bin\testhost\netcoreapp-Windows_NT-$configuration-$archTestHost";

  # This tells .NET Core not to go looking for .NET Core in other places
  $env:DOTNET_MULTILEVEL_LOOKUP=0;

  # Put our local dotnet.exe on PATH first so Visual Studio knows which one to use
  $env:PATH=($env:DOTNET_ROOT + ";" + $env:PATH);

  # Launch Visual Studio with the locally defined environment variables
  ."$vs"

  exit 0
}

# Check if an action is passed in
$actions = "r","restore","b","build","buildtests","rebuild","t","test","pack","sign","publish","clean"
$actionPassedIn = @(Compare-Object -ReferenceObject @($PSBoundParameters.Keys) -DifferenceObject $actions -ExcludeDifferent -IncludeEqual).Length -ne 0
if ($null -ne $properties -and $actionPassedIn -ne $true) {
  $actionPassedIn = @(Compare-Object -ReferenceObject $properties -DifferenceObject $actions.ForEach({ "-" + $_ }) -ExcludeDifferent -IncludeEqual).Length -ne 0
}

if (!$actionPassedIn -or $subsetCategory -ne "libraries") {
  $arguments = "-restore -build"
}

foreach ($argument in $PSBoundParameters.Keys)
{
  switch($argument)
  {
    "build"             { $arguments += " -build" }
    "buildtests"        { if ($build -eq $true) { $arguments += " /p:BuildTests=true" } else { $arguments += " -build /p:BuildTests=only" } }
    "test"              { $arguments += " -test" }
    "configuration"     { $configuration = (Get-Culture).TextInfo.ToTitleCase($($PSBoundParameters[$argument])); $arguments += " /p:ConfigurationGroup=$configuration -configuration $configuration" }
    "framework"         { $arguments += " /p:TargetGroup=$($PSBoundParameters[$argument].ToLowerInvariant())"}
    "os"                { $arguments += " /p:OSGroup=$($PSBoundParameters[$argument])" }
    "allconfigurations" { $arguments += " /p:BuildAllConfigurations=true" }
    "arch"              { $arguments += " /p:ArchGroup=$($PSBoundParameters[$argument]) /p:TargetArchitecture=$($PSBoundParameters[$argument])" }
    "properties"        { $arguments += " " + $properties }
    default             { $arguments += " /p:$argument=$($PSBoundParameters[$argument])" }
  }
}

Invoke-Expression "& `"$PSScriptRoot/common/build.ps1`" $arguments"
exit $lastExitCode
