[CmdletBinding(PositionalBinding=$false)]
Param(
  [switch][Alias('b')]$build,
  [switch][Alias('t')]$test,
  [switch] $buildtests,
  [string][Alias('c')]$configuration = "Debug",
  [string][Alias('f')]$framework,
  [string] $os,
  [switch] $allconfigurations,
  [switch] $coverage,
  [string] $testscope,
  [string] $arch,
  [switch] $clean,
  [Parameter(ValueFromRemainingArguments=$true)][String[]]$properties
)

function Get-Help() {
  Write-Host "Common settings:"
  Write-Host "  -framework              Build framework: netcoreapp, netfx, uap or uapaot (short: -f)"
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

  Write-Host "Advanced settings:"
  Write-Host "  -coverage               Collect code coverage when testing"
  Write-Host "  -testscope              Scope tests, allowed values: innerloop, outerloop, all"
  Write-Host "  -allconfigurations      Build packages for all build configurations"
  Write-Host "  -os                     Build operating system: Windows_NT or Unix"
  Write-Host "  -arch                   Build platform: x86, x64, arm or arm64"
  Write-Host "  -msbuildEngine <value>  MSBuild engine to use: dotnet or vs"
  Write-Host ""

  Write-Host "Command-line arguments not listed above are passed thru to msbuild."
  Write-Host "The above arguments can be shortened as much as to be unambiguous (e.g. -con for configuration, -t for test, etc.)."
}

# Exit if script has been dot-sourced
if ($MyInvocation.InvocationName -eq ".") {
  exit 0
}

# Check if an action is passed in
$actions = "r","restore","b","build","rebuild","deploy","deployDeps","test","integrationTest","sign","publish","buildtests"
$actionPassedIn = @(Compare-Object -ReferenceObject @($PSBoundParameters.Keys) -DifferenceObject $actions -ExcludeDifferent -IncludeEqual).Length -ne 0
if ($null -ne $properties -and $actionPassedIn -ne $true) {
  $actionPassedIn = @(Compare-Object -ReferenceObject $properties -DifferenceObject $actions.ForEach({ "-" + $_ }) -ExcludeDifferent -IncludeEqual).Length -ne 0
}

if ($clean) {
  $artifactsPath = "$PSScriptRoot\..\artifacts"
  if(Test-Path $artifactsPath) {
    Remove-Item -Recurse -Force $artifactsPath
    Write-Host "Artifacts directory deleted."
  }
  if (!$actionPassedIn) {
    exit 0
  }
}

if (!$actionPassedIn) {
  $arguments = "-restore -build"
}

$possibleDirToBuild = if($properties.Length -gt 0) { $properties[0]; } else { $null }

if ($null -ne $possibleDirToBuild) {
  $dtb = $possibleDirToBuild.TrimEnd('\')
  if (Test-Path $dtb) {
    $properties[0] = "/p:DirectoryToBuild=$(Resolve-Path $dtb)"
  }
  else {
    $dtb = Join-Path "$PSSCriptRoot\..\src" $dtb
    if (Test-Path $dtb) {
      $properties[0] = "/p:DirectoryToBuild=$(Resolve-Path $dtb)"
    }
  }
}

foreach ($argument in $PSBoundParameters.Keys)
{
  switch($argument)
  {
    "build"             { $arguments += " -build" }
    "test"              { $arguments += " -test" }
    "buildtests"        { $arguments += " /p:BuildTests=true" }
    "clean"             { }
    "configuration"     { $configuration = (Get-Culture).TextInfo.ToTitleCase($($PSBoundParameters[$argument])); $arguments += " /p:ConfigurationGroup=$configuration -configuration $configuration" }
    "framework"         { $arguments += " /p:TargetGroup=$($PSBoundParameters[$argument].ToLowerInvariant())"}
    "os"                { $arguments += " /p:OSGroup=$($PSBoundParameters[$argument])" }
    "allconfigurations" { $arguments += " /p:BuildAllConfigurations=true" }
    "coverage"          { $arguments += " /p:Coverage=true" }
    "testscope"         { $arguments += " /p:TestScope=$($PSBoundParameters[$argument])" }
    "arch"              { $arguments += " /p:ArchGroup=$($PSBoundParameters[$argument])" }
    "properties"        { $arguments += " " + $properties }
    default             { $arguments += " /p:$argument=$($PSBoundParameters[$argument])" }
  }
}

Invoke-Expression "& `"$PSScriptRoot/common/build.ps1`" $arguments"
exit $lastExitCode
