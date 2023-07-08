## This is a helper script for non-containerized local build and test execution.
## It downloads and uses the daily SDK which contains the compatible AspNetCore bits.
## Usage:
## ./build-local.ps1 [StressConfiguration] [LibrariesConfiguration]

$Version="8.0"
$RepoRoot="$(git rev-parse --show-toplevel)"
$DailyDotnetRoot= "./.dotnet-daily"

$StressConfiguration = "Release"
if (-not ([string]::IsNullOrEmpty($args[0]))) {
    $StressConfiguration = $args[0]
}

$LibrariesConfiguration = "Release"
if (-not ([string]::IsNullOrEmpty($args[1]))) {
    $LibrariesConfiguration = $args[1]
}

$TestHostRoot="$RepoRoot/artifacts/bin/testhost/net$Version-windows-$LibrariesConfiguration-x64"

Write-Host "StressConfiguration: $StressConfiguration, LibrariesConfiguration: $LibrariesConfiguration, testhost: $TestHostRoot"

if (-not (Test-Path -Path $TestHostRoot)) {
    Write-Host "Cannot find testhost in: $TestHostRoot"
    Write-Host "Make sure libraries with the requested configuration are built!"
    Write-Host "Usage:"
    Write-Host "./build-local.sh [StressConfiguration] [LibrariesConfiguration]"
    Write-Host "StressConfiguration and LibrariesConfiguration default to Release!"
    exit 1
}

if (-not (Test-Path -Path $DailyDotnetRoot)) {
    Write-Host "Downloading daily SDK to: $DailyDotnetRoot"
    New-Item -ItemType Directory -Path $DailyDotnetRoot
    Invoke-WebRequest -Uri https://dot.net/v1/dotnet-install.ps1 -OutFile "$DailyDotnetRoot\dotnet-install.ps1"
    & "$DailyDotnetRoot\dotnet-install.ps1" -NoPath -Channel "$Version.1xx" -Quality daily -InstallDir $DailyDotnetRoot
} else {
    Write-Host "Daily SDK found in $DailyDotnetRoot"
}

$env:DOTNET_ROOT=$DailyDotnetRoot
$env:PATH="$DailyDotnetRoot;$env:PATH"
$env:DOTNET_MULTILEVEL_LOOKUP=0

if (-not (Test-Path -Path "$TestHostRoot/shared/Microsoft.AspNetCore.App")) {
    Write-Host "Copying Microsoft.AspNetCore.App bits from daily SDK to testhost: $TestHostRoot"
    Copy-Item -Recurse -Path "$DailyDotnetRoot/shared/Microsoft.AspNetCore.App" -Destination "$TestHostRoot/shared"
} else {
    Write-Host "Microsoft.AspNetCore.App found in testhost: $TestHostRoot"
}

Write-Host "Building solution."
dotnet build -c $StressConfiguration

$Runscript=".\run-stress-$StressConfiguration-$LibrariesConfiguration.ps1"
if (-not (Test-Path $Runscript)) {
    Write-Host "Generating Runscript."
    Add-Content -Path $Runscript -Value "& '$TestHostRoot/dotnet' exec --roll-forward Major ./bin/$StressConfiguration/net$Version/HttpStress.dll `$args"
}

Write-Host "To run tests type:"
Write-Host "$Runscript [stress test args]"
