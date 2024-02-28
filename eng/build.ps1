[CmdletBinding(PositionalBinding=$false)]
Param(
  [switch][Alias('h')]$help,
  [switch][Alias('t')]$test,
  [ValidateSet("Debug","Release","Checked")][string[]][Alias('c')]$configuration = @("Debug"),
  [string][Alias('f')]$framework,
  [string]$vs,
  [string][Alias('v')]$verbosity = "minimal",
  [ValidateSet("windows","linux","osx","android","browser","wasi")][string]$os,
  [switch]$allconfigurations,
  [switch]$coverage,
  [string]$testscope,
  [switch]$testnobuild,
  [ValidateSet("x86","x64","arm","arm64","wasm")][string[]][Alias('a')]$arch = @([System.Runtime.InteropServices.RuntimeInformation]::ProcessArchitecture.ToString().ToLowerInvariant()),
  [string][Alias('s')]$subset,
  [ValidateSet("Debug","Release","Checked")][string][Alias('rc')]$runtimeConfiguration,
  [ValidateSet("Debug","Release")][string][Alias('lc')]$librariesConfiguration,
  [ValidateSet("CoreCLR","Mono")][string][Alias('rf')]$runtimeFlavor,
  [ValidateSet("Debug","Release","Checked")][string][Alias('hc')]$hostConfiguration,
  [switch]$usemonoruntime = $false,
  [switch]$ninja,
  [switch]$msbuild,
  [string]$cmakeargs,
  [switch]$pgoinstrument,
  [string[]]$fsanitize,
  [Parameter(ValueFromRemainingArguments=$true)][String[]]$properties
)

function Get-Help() {
  Write-Host "Common settings:"
  Write-Host "  -arch (-a)                     Target platform: x86, x64, arm, arm64, or wasm."
  Write-Host "                                 Pass a comma-separated list to build for multiple architectures."
  Write-Host ("                                 [Default: {0} (Depends on your console's architecture.)]" -f [System.Runtime.InteropServices.RuntimeInformation]::ProcessArchitecture.ToString().ToLowerInvariant())
  Write-Host "  -binaryLog (-bl)               Output binary log."
  Write-Host "  -configuration (-c)            Build configuration: Debug, Release or Checked."
  Write-Host "                                 Checked is exclusive to the CLR subset. It is the same as Debug, except code is"
  Write-Host "                                 compiled with optimizations enabled."
  Write-Host "                                 Pass a comma-separated list to build for multiple configurations."
  Write-Host "                                 [Default: Debug]"
  Write-Host "  -help (-h)                     Print help and exit."
  Write-Host "  -hostConfiguration (-hc)       Host build configuration: Debug, Release or Checked."
  Write-Host "                                 [Default: Debug]"
  Write-Host "  -librariesConfiguration (-lc)  Libraries build configuration: Debug or Release."
  Write-Host "                                 [Default: Debug]"
  Write-Host "  -os                            Target operating system: windows, linux, osx, android, wasi or browser."
  Write-Host "                                 [Default: Your machine's OS.]"
  Write-Host "  -runtimeConfiguration (-rc)    Runtime build configuration: Debug, Release or Checked."
  Write-Host "                                 Checked is exclusive to the CLR runtime. It is the same as Debug, except code is"
  Write-Host "                                 compiled with optimizations enabled."
  Write-Host "                                 [Default: Debug]"
  Write-Host "  -runtimeFlavor (-rf)           Runtime flavor: CoreCLR or Mono."
  Write-Host "                                 [Default: CoreCLR]"
  Write-Host "  -subset (-s)                   Build a subset, print available subsets with -subset help."
  Write-Host "                                 '-subset' can be omitted if the subset is given as the first argument."
  Write-Host "                                 [Default: Builds the entire repo.]"
  Write-Host "  -usemonoruntime                Product a .NET runtime with Mono as the underlying runtime."
  Write-Host "  -verbosity (-v)                MSBuild verbosity: q[uiet], m[inimal], n[ormal], d[etailed], and diag[nostic]."
  Write-Host "                                 [Default: Minimal]"
  Write-Host "  -vs                            Open the solution with Visual Studio using the locally acquired SDK."
  Write-Host "                                 Path or any project or solution name is accepted."
  Write-Host "                                 (Example: -vs Microsoft.CSharp or -vs CoreCLR.sln)"
  Write-Host ""

  Write-Host "Actions (defaults to -restore -build):"
  Write-Host "  -build (-b)             Build all source projects."
  Write-Host "                          This assumes -restore has been run already."
  Write-Host "  -clean                  Clean the solution."
  Write-Host "  -pack                   Package build outputs into NuGet packages."
  Write-Host "  -publish                Publish artifacts (e.g. symbols)."
  Write-Host "                          This assumes -build has been run already."
  Write-Host "  -rebuild                Rebuild all source projects."
  Write-Host "  -restore                Restore dependencies."
  Write-Host "  -sign                   Sign build outputs."
  Write-Host "  -test (-t)              Incrementally builds and runs tests."
  Write-Host "                          Use in conjunction with -testnobuild to only run tests."
  Write-Host ""

  Write-Host "Libraries settings:"
  Write-Host "  -allconfigurations      Build packages for all build configurations."
  Write-Host "  -coverage               Collect code coverage when testing."
  Write-Host "  -framework (-f)         Build framework: net9.0 or net48."
  Write-Host "                          [Default: net9.0]"
  Write-Host "  -testnobuild            Skip building tests when invoking -test."
  Write-Host "  -testscope              Scope tests, allowed values: innerloop, outerloop, all."
  Write-Host ""

  Write-Host "Native build settings:"
  Write-Host "  -cmakeargs                User-settable additional arguments passed to CMake."
  Write-Host "  -ninja                    Use Ninja to drive the native build. (default)"
  Write-Host "  -msbuild                  Use MSBuild to drive the native build. This is a no-op for Mono."
  Write-Host "  -pgoinstrument            Build the CLR with PGO instrumentation."
  Write-Host "  -fsanitize (address)      Build the native components with the specified sanitizers."
  Write-Host "                            Sanitizers can be specified with a comma-separated list."
  Write-Host ""

  Write-Host "Command-line arguments not listed above are passed through to MSBuild."
  Write-Host "The above arguments can be shortened as much as to be unambiguous."
  Write-Host "(Example: -con for configuration, -t for test, etc.)."
  Write-Host ""

  Write-Host "Here are some quick examples. These assume you are on a Windows x64 machine:"
  Write-Host ""
  Write-Host "* Build CoreCLR for Windows x64 on Release configuration:"
  Write-Host ".\build.cmd clr -c release"
  Write-Host ""
  Write-Host "* Cross-compile CoreCLR runtime for Windows ARM64 on Release configuration."
  Write-Host ".\build.cmd clr.runtime -arch arm64 -c release"
  Write-Host ""
  Write-Host "* Build Debug libraries with a Release runtime for Windows x64."
  Write-Host ".\build.cmd clr+libs -rc release"
  Write-Host ""
  Write-Host "* Build Release libraries and their tests with a Checked runtime for Windows x64, and run the tests."
  Write-Host ".\build.cmd clr+libs+libs.tests -rc checked -lc release -test"
  Write-Host ""
  Write-Host "* Build Mono runtime for Windows x64 on Release configuration."
  Write-Host ".\build.cmd mono -c release"
  Write-Host ""
  Write-Host "* Build Release coreclr corelib, crossgen corelib and update Debug libraries testhost to run test on an updated corelib."
  Write-Host ".\build.cmd clr.corelib+clr.nativecorelib+libs.pretest -rc release"
  Write-Host ""
  Write-Host "* Build Debug mono corelib and update Release libraries testhost to run test on an updated corelib."
  Write-Host ".\build.cmd mono.corelib+libs.pretest -rc debug -c release"
  Write-Host ""
  Write-Host ""
  Write-Host "For more information, check out https://github.com/dotnet/runtime/blob/main/docs/workflow/README.md"
}

if ($help) {
  Get-Help
  exit 0
}

# check the first argument if subset is not explicitly passed in
if (-not $PSBoundParameters.ContainsKey("subset") -and $properties.Length -gt 0 -and $properties[0] -match '^[a-zA-Z\.\+]+$') {
  $subset = $properties[0]
  $PSBoundParameters.Add("subset", $subset)
  $properties = $properties | Select-Object -Skip 1
}

if ($subset -eq 'help') {
  Invoke-Expression "& `"$PSScriptRoot/common/build.ps1`" -restore -build /p:subset=help /clp:nosummary"
  exit 0
}

# Lower-case the passed in OS string.
if ($os) {
  $os = $os.ToLowerInvariant()
}

if ($os -eq "browser") {
  # override default arch for Browser, we only support wasm
  $arch = "wasm"

  if ($msbuild -eq $True) {
    Write-Error "Using the -msbuild option isn't supported when building for Browser on Windows, we need need ninja for Emscripten."
    exit 1
  }
}

if ($os -eq "wasi") {
  # override default arch for wasi, we only support wasm
  $arch = "wasm"

  if ($msbuild -eq $True) {
    Write-Error "Using the -msbuild option isn't supported when building for WASI on Windows, we need ninja for WASI-SDK."
    exit 1
  }
}

if ($vs) {
  $archToOpen = $arch[0]
  $configToOpen = $configuration[0]
  $repoRoot = Split-Path $PSScriptRoot -Parent
  if ($runtimeConfiguration) {
    $configToOpen = $runtimeConfiguration
  }

  if ($vs -ieq "coreclr.sln") {
    # If someone passes in coreclr.sln (case-insensitive),
    # launch the generated CMake solution.
    $vs = Split-Path $PSScriptRoot -Parent | Join-Path -ChildPath "artifacts\obj\coreclr" | Join-Path -ChildPath "windows.$archToOpen.$((Get-Culture).TextInfo.ToTitleCase($configToOpen))" | Join-Path -ChildPath "ide" | Join-Path -ChildPath "CoreCLR.sln"
    if (-Not (Test-Path $vs)) {
      Invoke-Expression "& `"$repoRoot/src/coreclr/build-runtime.cmd`" -configureonly -$archToOpen -$configToOpen -msbuild"
      if ($lastExitCode -ne 0) {
        Write-Error "Failed to generate the CoreCLR solution file."
        exit 1
      }
      if (-Not (Test-Path $vs)) {
        Write-Error "Unable to find the CoreCLR solution file at $vs."
      }
    }
  }
  elseif ($vs -ieq "corehost.sln") {
    $vs = Split-Path $PSScriptRoot -Parent | Join-Path -ChildPath "artifacts\obj\" | Join-Path -ChildPath "win-$archToOpen.$((Get-Culture).TextInfo.ToTitleCase($configToOpen))" | Join-Path -ChildPath "corehost" | Join-Path -ChildPath "ide" | Join-Path -ChildPath "corehost.sln"
    if (-Not (Test-Path $vs)) {
      Invoke-Expression "& `"$repoRoot/eng/common/msbuild.ps1`" $repoRoot/src/native/corehost/corehost.proj /clp:nosummary /restore /p:Ninja=false /p:Configuration=$configToOpen /p:TargetArchitecture=$archToOpen /p:ConfigureOnly=true"
      if ($lastExitCode -ne 0) {
        Write-Error "Failed to generate the CoreHost solution file."
        exit 1
      }
      if (-Not (Test-Path $vs)) {
        Write-Error "Unable to find the CoreHost solution file at $vs."
      }
    }
  }
  elseif (-Not (Test-Path $vs)) {
    $solution = $vs

    if ($runtimeFlavor -eq "Mono") {
      # Search for the solution in mono
      $vs = Split-Path $PSScriptRoot -Parent | Join-Path -ChildPath "src\mono" | Join-Path -ChildPath $vs | Join-Path -ChildPath "$vs.sln"
    } else {
      # Search for the solution in coreclr
      $vs = Split-Path $PSScriptRoot -Parent | Join-Path -ChildPath "src\coreclr" | Join-Path -ChildPath $vs | Join-Path -ChildPath "$vs.sln"
    }

    if (-Not (Test-Path $vs)) {
      $vs = $solution

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
  }

  . $PSScriptRoot\common\tools.ps1

  # This tells .NET Core to use the bootstrapped runtime
  $env:DOTNET_ROOT=InitializeDotNetCli -install:$true -createSdkLocationFile:$true

  # This tells MSBuild to load the SDK from the directory of the bootstrapped SDK
  $env:DOTNET_MSBUILD_SDK_RESOLVER_CLI_DIR=$env:DOTNET_ROOT

  # This tells .NET Core not to go looking for .NET Core in other places
  $env:DOTNET_MULTILEVEL_LOOKUP=0;

  # Put our local dotnet.exe on PATH first so Visual Studio knows which one to use
  $env:PATH=($env:DOTNET_ROOT + ";" + $env:PATH);

  # Disable .NET runtime signature validation errors which errors for local builds
  $env:VSDebugger_ValidateDotnetDebugLibSignatures=0;

  # Respect the RuntimeConfiguration variable for building inside VS with different runtime configurations
  if ($runtimeConfiguration)
  {
    $env:RUNTIMECONFIGURATION=$runtimeConfiguration
  }

  # Respect the RuntimeFlavor variable for building inside VS with a different CoreLib and runtime
  if ($runtimeFlavor)
  {
    $env:RUNTIMEFLAVOR=$runtimeFlavor
  }

  # Respect the TargetOS variable for building non AnyOS libraries
  if ($os) {
    $env:TARGETOS=$os
  }

  # Respect the TargetArchitecture variable for building non AnyCPU libraries
  if ($arch) {
    $env:TARGETARCHITECTURE=$arch
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
    "runtimeFlavor"          { $arguments += " /p:RuntimeFlavor=$($PSBoundParameters[$argument].ToLowerInvariant())" }
    "usemonoruntime"         { $arguments += " /p:PrimaryRuntimeFlavor=Mono" }
    "librariesConfiguration" { $arguments += " /p:LibrariesConfiguration=$((Get-Culture).TextInfo.ToTitleCase($($PSBoundParameters[$argument])))" }
    "hostConfiguration"      { $arguments += " /p:HostConfiguration=$((Get-Culture).TextInfo.ToTitleCase($($PSBoundParameters[$argument])))" }
    "framework"              { $arguments += " /p:BuildTargetFramework=$($PSBoundParameters[$argument].ToLowerInvariant())" }
    "os"                     { $arguments += " /p:TargetOS=$($PSBoundParameters[$argument])" }
    "allconfigurations"      { $arguments += " /p:BuildAllConfigurations=true" }
    "properties"             { $arguments += " " + $properties }
    "verbosity"              { $arguments += " -$argument " + $($PSBoundParameters[$argument]) }
    "cmakeargs"              { $arguments += " /p:CMakeArgs=`"$($PSBoundParameters[$argument])`"" }
    # The -ninja switch is a no-op since Ninja is the default generator on Windows.
    "ninja"                  { }
    "msbuild"                { $arguments += " /p:Ninja=false" }
    "pgoinstrument"          { $arguments += " /p:PgoInstrument=$($PSBoundParameters[$argument])"}
    # configuration and arch can be specified multiple times, so they should be no-ops here
    "configuration"          {}
    "arch"                   {}
    "fsanitize"              { $arguments += " /p:EnableNativeSanitizers=$($PSBoundParameters[$argument])"}
    default                  { $arguments += " /p:$argument=$($PSBoundParameters[$argument])" }
  }
}

if ($env:TreatWarningsAsErrors -eq 'false') {
  $arguments += " -warnAsError 0"
}

# disable terminal logger for now: https://github.com/dotnet/runtime/issues/97211
$arguments += " -tl:false"

# Disable targeting pack caching as we reference a partially constructed targeting pack and update it later.
# The later changes are ignored when using the cache.
$env:DOTNETSDK_ALLOW_TARGETING_PACK_CACHING=0

$failedBuilds = @()

foreach ($config in $configuration) {
  $argumentsWithConfig = $arguments + " -configuration $((Get-Culture).TextInfo.ToTitleCase($config))";
  foreach ($singleArch in $arch) {
    $argumentsWithArch =  "/p:TargetArchitecture=$singleArch " + $argumentsWithConfig
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

if ($ninja) {
    Write-Host "The -ninja option has no effect on Windows builds since the Ninja generator is the default generator."
}

exit 0
