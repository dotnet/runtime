[CmdletBinding(PositionalBinding=$false)]
Param(
  [switch][Alias('h')]$help,
  [switch][Alias('b')]$build,
  [switch][Alias('t')]$test,
  [switch]$buildtests,
  [string[]][Alias('c')]$configuration = @("Debug"),
  [string][Alias('f')]$framework,
  [string]$vs,
  [string]$os,
  [switch]$allconfigurations,
  [switch]$coverage,
  [string]$testscope,
  [string[]][Alias('a')]$arch = @([System.Runtime.InteropServices.RuntimeInformation]::ProcessArchitecture.ToString().ToLowerInvariant()),
  [string]$subsetCategory,
  [string]$subset,
  [ValidateSet("Debug","Release","Checked")][string]$runtimeConfiguration = "Debug",
  [ValidateSet("Debug","Release")][string]$librariesConfiguration,
  [Parameter(ValueFromRemainingArguments=$true)][String[]]$properties
)

function Get-Help() {
  Write-Host "Common settings:"
  Write-Host "  -subset                   Build a subset, print available subsets with -subset help"
  Write-Host "  -subsetCategory           Build a subsetCategory, print available subsetCategories with -subset help"
  Write-Host "  -vs                       Open the solution with VS for Test Explorer support. Path or solution name (ie -vs Microsoft.CSharp)"
  Write-Host "  -os                       Build operating system: Windows_NT or Unix"
  Write-Host "  -arch                     Build platform: x86, x64, arm or arm64 (short: -a). Pass a comma-separated list to build for multiple architectures."
  Write-Host "  -configuration            Build configuration: Debug, Release or [CoreCLR]Checked (short: -c). Pass a comma-separated list to build for multiple configurations"
  Write-Host "  -runtimeConfiguration     Runtime build configuration: Debug, Release or [CoreCLR]Checked"
  Write-Host "  -librariesConfiguration   Libraries build configuration: Debug or Release"
  Write-Host "  -verbosity                MSBuild verbosity: q[uiet], m[inimal], n[ormal], d[etailed], and diag[nostic] (short: -v)"
  Write-Host "  -binaryLog                Output binary log (short: -bl)"
  Write-Host "  -help                     Print help and exit (short: -h)"
  Write-Host ""

  Write-Host "Actions (defaults to -restore -build):"
  Write-Host "  -restore                Restore dependencies (short: -r)"
  Write-Host "  -build                  Build all source projects (short: -b)"
  Write-Host "  -buildtests             Build all test projects"
  Write-Host "  -rebuild                Rebuild all source projects"
  Write-Host "  -test                   Build and run tests (short: -t)"
  Write-Host "  -pack                   Package build outputs into NuGet packages"
  Write-Host "  -sign                   Sign build outputs"
  Write-Host "  -publish                Publish artifacts (e.g. symbols)"
  Write-Host "  -clean                  Clean the solution"
  Write-Host ""

  Write-Host "Libraries settings:"
  Write-Host "  -framework              Build framework: netcoreapp5.0 or net472 (short: -f)"
  Write-Host "  -coverage               Collect code coverage when testing"
  Write-Host "  -testscope              Scope tests, allowed values: innerloop, outerloop, all"
  Write-Host "  -allconfigurations      Build packages for all build configurations"
  Write-Host ""

  Write-Host "Command-line arguments not listed above are passed thru to msbuild."
  Write-Host "The above arguments can be shortened as much as to be unambiguous (e.g. -con for configuration, -t for test, etc.)."
}

if ($help -or (($null -ne $properties) -and ($properties.Contains('/help') -or $properties.Contains('/?')))) {
  Get-Help
  exit 0
}

$subsetCategory = $subsetCategory.ToLowerInvariant()

# VS Test Explorer support for libraries
if ($vs) {
  . $PSScriptRoot\common\tools.ps1

  # Microsoft.DotNet.CoreSetup.sln is special - hosting tests are currently meant to run on the
  # bootstrapped .NET Core, not on the live-built runtime.
  if (([System.IO.Path]::GetFileName($vs) -ieq "Microsoft.DotNet.CoreSetup.sln") -or ($vs -ieq "Microsoft.DotNet.CoreSetup")) {
    if (-Not (Test-Path $vs)) {
      if (-Not ( $vs.endswith(".sln"))) {
          $vs = "$vs.sln"
      }
      $vs = Join-Path "$PSScriptRoot\..\src\installer" $vs
    }

    # This tells .NET Core to use the bootstrapped runtime to run the tests
    $env:DOTNET_ROOT=InitializeDotNetCli -install:$false
  }
  else {
    if (-Not (Test-Path $vs)) {
      $vs = Join-Path "$PSScriptRoot\..\src\libraries" $vs | Join-Path -ChildPath "$vs.sln"
    }

    $archTestHost = if ($arch) { $arch } else { "x64" }

    # This tells .NET Core to use the same dotnet.exe that build scripts use
    $env:DOTNET_ROOT="$PSScriptRoot\..\artifacts\bin\testhost\netcoreapp5.0-Windows_NT-$configuration-$archTestHost";
    $env:DEVPATH="$PSScriptRoot\..\artifacts\bin\testhost\net472-Windows_NT-$configuration-$archTestHost";
  }

  # This tells MSBuild to load the SDK from the directory of the bootstrapped SDK
  $env:DOTNET_MSBUILD_SDK_RESOLVER_CLI_DIR=InitializeDotNetCli -install:$false

  # This tells .NET Core not to go looking for .NET Core in other places
  $env:DOTNET_MULTILEVEL_LOOKUP=0;

  # Put our local dotnet.exe on PATH first so Visual Studio knows which one to use
  $env:PATH=($env:DOTNET_ROOT + ";" + $env:PATH);

  # Respect the RuntimeConfiguration variable for building inside VS with different runtime configurations
  $env:RUNTIMECONFIGURATION=$runtimeConfiguration

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

if (!$actionPassedIn) {
  $arguments = "-restore -build"
}

$possibleDirToBuild = if($properties.Length -gt 0) { $properties[0]; } else { $null }

if ($null -ne $possibleDirToBuild -and $subsetCategory -eq "libraries") {
  $dtb = $possibleDirToBuild.TrimEnd('\')
  if (Test-Path $dtb) {
    $properties[0] = "/p:DirectoryToBuild=$(Resolve-Path $dtb)"
  }
  else {
    $dtb = Join-Path "$PSSCriptRoot\..\src\libraries" $dtb
    if (Test-Path $dtb) {
      $properties[0] = "/p:DirectoryToBuild=$(Resolve-Path $dtb)"
    }
  }
}

foreach ($argument in $PSBoundParameters.Keys)
{
  switch($argument)
  {
    "build"                { $arguments += " -build" }
    "buildtests"           { if ($build -eq $true) { $arguments += " /p:BuildTests=true" } else { $arguments += " -build /p:BuildTests=only" } }
    "test"                 { $arguments += " -test" }
    "runtimeConfiguration" { $arguments += " /p:RuntimeConfiguration=$((Get-Culture).TextInfo.ToTitleCase($($PSBoundParameters[$argument])))" }
    "framework"            { $arguments += " /p:BuildTargetFramework=$($PSBoundParameters[$argument].ToLowerInvariant())" }
    "os"                   { $arguments += " /p:TargetOS=$($PSBoundParameters[$argument])" }
    "allconfigurations"    { $arguments += " /p:BuildAllConfigurations=true" }
    "properties"           { $arguments += " " + $properties }
    # configuration and arch can be specified multiple times, so they should be no-ops here
    "configuration"        {}
    "arch"                 {}
    default                { $arguments += " /p:$argument=$($PSBoundParameters[$argument])" }
  }
}

$failedBuilds = @()

foreach ($config in $configuration) {
  $argumentsWithConfig = $arguments + " -configuration $((Get-Culture).TextInfo.ToTitleCase($config))";
  foreach ($singleArch in $arch) {
    $argumentsWithArch =  "/p:ArchGroup=$singleArch /p:TargetArchitecture=$singleArch " + $argumentsWithConfig
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
