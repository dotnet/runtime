[CmdletBinding(PositionalBinding=$false)]
Param(
  [switch][Alias('b')]$build,
  [switch] $buildtests,
  [string][Alias('c')]$configuration = "Debug",
  [string][Alias('f')]$framework,
  [string] $os,
  [switch] $allconfigurations,
  [switch] $coverage,
  [switch] $outerloop,
  [string] $arch,
  [switch][Alias('h')]$help,
  [Parameter(ValueFromRemainingArguments=$true)][String[]]$properties
)
function Print-Usage() {
    Write-Host "Default if no actions are passed in: -restore -build"
    Write-Host ""
    Write-Host "CoreFX specific settings:"
    Write-Host "  -buildtests             Build test projects"
    Write-Host "  -framework              The target group assemblies are built for (short: -f)"
    Write-Host "  -os                     The operating system assemblies are built for"
    Write-Host "  -allconfigurations      Build packages for all build configurations"
    Write-Host "  -coverage               Collect code coverage when testing"
    Write-Host "  -outerloop              Include tests which are marked as OuterLoop"
    Write-Host "  -arch                   The architecture group"
    Write-Host ""
}

if ($help -or (($null -ne $properties) -and ($properties.Contains("/help") -or $properties.Contains("/?")))) {
  Print-Usage
  Invoke-Expression "& `"$PSScriptRoot/common/build.ps1`" -help"
  exit 0
}

# Check if an action is passed in
$actions = "r","restore","b","build","rebuild","deploy","deployDeps","test","integrationTest","performanceTest","sign","publish","buildtests"
$actionPassedIn = @(Compare-Object -ReferenceObject @($PSBoundParameters.Keys) -DifferenceObject $actions -ExcludeDifferent -IncludeEqual).Length -ne 0
if ($null -ne $properties -and $actionPassedIn -ne $true) {
  $actionPassedIn = @(Compare-Object -ReferenceObject $properties -DifferenceObject $actions.ForEach({ "-" + $_ }) -ExcludeDifferent -IncludeEqual).Length -ne 0
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
    "buildtests"        { $arguments += " /p:BuildTests=true" }
    "configuration"     { $arguments += " /p:ConfigurationGroup=$($PSBoundParameters[$argument]) -configuration $($PSBoundParameters[$argument])" }
    "framework"         { $arguments += " /p:TargetGroup=$($PSBoundParameters[$argument].ToLowerInvariant())"}
    "os"                { $arguments += " /p:OSGroup=$($PSBoundParameters[$argument])" }
    "allconfigurations" { $arguments += " /p:BuildAllConfigurations=true" }
    "coverage"          { $arguments += " /p:Coverage=true" }
    "outerloop"         { $arguments += " /p:OuterLoop=true" }
    "arch"              { $arguments += " /p:ArchGroup=$($PSBoundParameters[$argument])" }
    "properties"        { $arguments += " " + $properties }
    default             { $arguments += " /p:$argument=$($PSBoundParameters[$argument])" }
  }
}

Invoke-Expression "& `"$PSScriptRoot/common/build.ps1`" $arguments"
exit $lastExitCode
