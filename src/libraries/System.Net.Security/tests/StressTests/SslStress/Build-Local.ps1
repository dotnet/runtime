## This is a helper script for non-containerized local build and test execution.
## It downloads and uses the daily SDK which contains the compatible AspNetCore bits.
## Usage:
## ./build-local.ps1 [StressConfiguration] [LibrariesConfiguration]

# Note that this script does much less than it's counterpart in HttpStress.
# In SslStress it's a thin utility to generate a runscript for running the app with the live-built testhost.
# The main reason to use an equivalent solution in SslStress is consistency with HttpStress.

$Version="7.0"
$RepoRoot="$(git rev-parse --show-toplevel)"
$DailyDotnetRoot= "./.dotnet-daily"

$StressConfiguration = "Release"
if (-not ([string]::IsNullOrEmpty($args[0]))) {
    $StressConfiguration = $args[0]
}

$LibrariesConfiguration = "Release"
if (-not ([string]::IsNullOrEmpty($args[1]))) {
    $LibrariesConfiguration = $args[0]
}

Write-Host "StressConfiguration: $StressConfiguration, LibrariesConfiguration: $LibrariesConfiguration"

$TestHostRoot="$RepoRoot/artifacts/bin/testhost/net$Version-windows-$LibrariesConfiguration-x64"

Write-Host "Building solution."
dotnet build -c $StressConfiguration

$Runscript=".\run-stress-$LibrariesConfiguration-$StressConfiguration.ps1"
if (-not (Test-Path $Runscript)) {
    Write-Host "Generating Runscript."
    Add-Content -Path $Runscript -Value "& '$TestHostRoot/dotnet' exec ./bin/$StressConfiguration/net$Version/SslStress.dll `$args"
}

Write-Host "To run tests type:"
Write-Host "$Runscript [stress test args]"