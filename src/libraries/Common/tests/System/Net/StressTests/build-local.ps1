## This is a helper script for non-containerized local build and test execution.
## It downloads and uses the daily SDK which contains the compatible AspNetCore bits.
## Usage:
## ./build-local.ps1 [TestProjectDir] [StressConfiguration] [LibrariesConfiguration]

$RepoRoot="$(git rev-parse --show-toplevel)"
[xml]$xml = Get-Content (Join-Path $RepoRoot "eng\Versions.props")
$Version="$($xml.Project.PropertyGroup.MajorVersion[0]).$($xml.Project.PropertyGroup.MinorVersion[0])"

function PrintUsageAndExit {
    Write-Host "Usage:"
    Write-Host "./build-local.ps1 [TestProjectDir] [StressConfiguration] [LibrariesConfiguration]"
    Write-Host "StressConfiguration and LibrariesConfiguration default to Release!"
    exit 1
}

if (-not ([string]::IsNullOrEmpty($args[0])) -and (Test-Path -Path $args[0])) {
    $TestProjectDir = $args[0]
} else {
    Write-Host "Valid TestProjectDir is required!"
    PrintUsageAndExit
}

$ProjectName = (Get-Item $TestProjectDir).Name

$DailyDotnetRoot= Join-Path $TestProjectDir ".dotnet-daily"

$StressConfiguration = "Release"
if (-not ([string]::IsNullOrEmpty($args[1]))) {
    $StressConfiguration = $args[1]
}

$LibrariesConfiguration = "Release"
if (-not ([string]::IsNullOrEmpty($args[2]))) {
    $LibrariesConfiguration = $args[2]
}

$TestHostRoot="$RepoRoot/artifacts/bin/testhost/net$Version-windows-$LibrariesConfiguration-x64"

Write-Host "StressConfiguration: $StressConfiguration, LibrariesConfiguration: $LibrariesConfiguration, testhost: $TestHostRoot"

if (-not (Test-Path -Path $TestHostRoot)) {
    Write-Host "Cannot find testhost in: $TestHostRoot"
    Write-Host "Make sure libraries with the requested configuration are built!"
    PrintUsageAndExit
    exit 1
}

if (-not (Test-Path -Path $DailyDotnetRoot)) {
    Write-Host "Downloading daily SDK to: $DailyDotnetRoot"
    New-Item -ItemType Directory -Path $DailyDotnetRoot
    Invoke-WebRequest -Uri https://builds.dotnet.microsoft.com/dotnet/scripts/v1/dotnet-install.ps1 -OutFile "$DailyDotnetRoot\dotnet-install.ps1"
    & "$DailyDotnetRoot\dotnet-install.ps1" -NoPath -Channel $Version -Quality daily -InstallDir $DailyDotnetRoot
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
dotnet build -c $StressConfiguration -f "net$Version"

$Runscript=".\run-stress-$StressConfiguration-$LibrariesConfiguration.ps1"
if (-not (Test-Path $Runscript)) {
    Write-Host "Generating Runscript."
    Add-Content -Path $Runscript -Value "& '$TestHostRoot/dotnet' exec --roll-forward Major ./bin/$StressConfiguration/net$Version/$ProjectName.dll `$args"
}

Write-Host "To run tests type:"
Write-Host "$Runscript [stress test args]"
