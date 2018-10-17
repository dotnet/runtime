[CmdletBinding(PositionalBinding=$false)]
Param(
  [string] $ArchGroup,
  [switch] $Release,
  [Alias("framework")][string] $TargetGroup,
  [Alias("os")][string] $OSGroup,
  [Alias("allconfigurations")][switch] $BuildAllConfigurations,
  [string] $RuntimeOS,
  [switch] $OuterLoop,
  [switch] $SkipTests,
  [Alias("sync")][switch] $Restore,
  [switch] $Clean,
  [Alias("includetests")][switch] $BuildTests,
  [switch] $Test,
  [switch] $Coverage,
  [switch] $InitTools,
  [Parameter(ValueFromRemainingArguments=$true)][String[]]$ExtraArgs
)

$defaultargs = "-restore -build -warnaserror:0"

$possibleDirToBuild = if($ExtraArgs.Length -gt 0) { $ExtraArgs[0]; } else { $null }

if ($possibleDirToBuild -ne $null) {
  $dtb = $possibleDirToBuild
  if (Test-Path $dtb) {
    $ExtraArgs[0] = "/p:DirectoryToBuild=$(Resolve-Path $dtb)"
  }
  else {
    $dtb = Join-Path "$PSSCriptRoot\..\src" $possibleDirToBuild
    if (Test-Path $dtb) {
      $ExtraArgs[0] = "/p:DirectoryToBuild=$(Resolve-Path $dtb)"
    }
  }
}

foreach ($argument in $PSBoundParameters.Keys)
{
  switch($argument)
  {
    "Debug"      { $arguments += " /p:ConfigurationGroup=Debug -configuration Debug" }
    "Release"    { $arguments += " /p:ConfigurationGroup=Release -configuration Release" }
    "ExtraArgs"  { $arguments += " " + $ExtraArgs }
    "Restore"    { $defaultargs = "-restore" }
    "Test"       { $defaultargs = "-test -warnaserror:0" }
    "InitTools"  { $defaultargs = "-restore -warnaserror:0" }
    default      { $arguments += " /p:$argument=$($PSBoundParameters[$argument])" }
  }
}

$arguments = "$defaultargs $arguments"

Invoke-Expression "& `"$PSScriptRoot/common/build.ps1`" $arguments"
exit $lastExitCode