<#
.SYNOPSIS
Builds and publishes the cross-platform and combined pacakges for RyuJit. Cross-platform binaries
are sourced from Azure Blob Storage.
#>

[CmdletBinding()]
Param(
    # The feed to publish to.
    [string]$feed,

    # The API key to use during publishing.
    [string]$apiKey,

    # The Azure account to use.
    [string]$storageAccount,

    # The Azure account key to use.
    [string]$storageKey,

    # The Azure container to use.
    [string]$storageContainer,

    # The path to NuGet. Defaults to "nuget.exe".
    [string]$nugetPath = "nuget.exe",

    # The output directory for the cross-platform binaries.
    [string]$binariesDir,

    # The package output directory.
    [string]$packageOutputDir,

    # The directory that contains the .nuspec files that will be used to create the
    # cross-platform and combined packages.
    [string]$nuspecDir,

    # The build number, if any.
    [string]$buildNumber = $null
)

function Get-Latest-Blob-Name
{
    Param([array]$blobs, [string]$expectedSuffix)

    $chosenBlob = $null
    $chosenDate = $null
    foreach ($blob in $blobs)
    {
        if ($blob.name -notlike "*$expectedSuffix")
        {
            continue
        }

        $date = [datetime]($blob.properties."last-modified")
        if (!$chosenBlob -or ($chosenDate -and $date -ge $chosenDate))
        {
            $chosenBlob = $blob.name
            $chosenDate = $date
        }
    }

    return $chosenBlob
}

# Set the CWD
[System.Environment]::CurrentDirectory = Get-Location

# Get the list of blobs in storage
$json = ""
$blobs = @()
try
{
    $tmpFileName = [System.IO.Path]::GetTempFileName()
    azure storage blob list -a $storageAccount -k $storageKey $storageContainer --json > $tmpFileName
    $json = [System.IO.File]::ReadAllText($tmpFileName)
    $blobs = ConvertFrom-Json $json
}
catch
{
    Write-Error "Could not fetch blobs from Azure."
    Write-Error "Response: $json"
    exit 1
}

# Find, fetch, and extract the latest Ubuntu and OSX blobs
[System.Reflection.Assembly]::LoadWithPartialName("System.IO.Compression.FileSystem") | Out-Null

$ubuntuBlob = Get-Latest-Blob-Name $blobs "Ubuntu.14.04_CoreClr_x64_Release_Enu.zip"
$osxBlob = Get-Latest-Blob-Name $blobs "OSX_CoreClr_x64_Release_Enu.zip"

if (!$ubuntuBlob)
{
    Write-Error "Could not locate an Ubuntu drop in Azure."
    exit 1
}

if (!$osxBlob)
{
    Write-Error "Could not locate an OS X drop in Azure."
    exit 1
}

azure storage blob download -m -q -a $storageAccount -k $storageKey $storageContainer $ubuntuBlob
if ($LastExitCode -ne 0)
{
    Write-Error "Failed to fetch Ubuntu drop $ubuntuBlob from Azure."
    exit 1
}

azure storage blob download -m -q -a $storageAccount -k $storageKey $storageContainer $osxBlob
if ($LastExitCode -ne 0)
{
    Write-Error "Failed to fetch OS X drop $osxBlob from Azure."
    exit 1
}

$ubuntuDirectory = [System.IO.Path]::GetFileNameWithoutExtension($ubuntuBlob)
try
{
    [System.IO.Compression.ZipFile]::ExtractToDirectory($ubuntuBlob, $ubuntuDirectory)
}
catch
{
    Write-Error "Failed to extract Ubuntu drop to $ubuntuDirectory`: $_.Exception.Message"
    exit 1
}

$osxDirectory = [System.IO.Path]::GetFileNameWithoutExtension($osxBlob)
try
{
    [System.IO.Compression.ZipFile]::ExtractToDirectory($osxBlob, $osxDirectory)
}
catch
{
    Write-Error "Failed to extract OS X drop to $osxDirectory`: $_.Exception.Message"
    exit 1
}

# Gather the bits from the Ubuntu and OSX blobs into the bin directory
Copy-Item -Path @("$ubuntuDirectory\libryujit.so", "$osxDirectory\libryujit.dylib") -Destination $binariesDir
if ($LastExitCode -ne 0)
{
    Write-Error "Failed to copy cross-platform bits to $binariesDir."
    exit 1
}

# Gather the .nuspecs and their dependencies into the package output directory
$files = @(
    "$nuspecDir\Microsoft.DotNet.RyuJit.nuspec",
    "$nuspecDir\runtime.json",
    "$nuspecDir\toolchain.osx.10.10-x64.Microsoft.DotNet.RyuJit.nuspec",
    "$nuspecDir\toolchain.ubuntu.14.04-x64.Microsoft.DotNet.RyuJit.nuspec"
)
Copy-Item -Path $files -Destination $packageOutputDir
if ($LastExitCode -ne 0)
{
    Write-Error "Failed to copy nuspecs to $packageOutputDir."
    exit 1
}

# Create the packages.
$packages = @(
    "toolchain.osx.10.10-x64.Microsoft.DotNet.RyuJit",
    "toolchain.ubuntu.14.04-x64.Microsoft.DotNet.RyuJit",
    "Microsoft.DotNet.RyuJit"
)

$packageVersion = "1.0.6-prerelease"
if ($buildNumber)
{
    $packageVersion = $packageVersion + "-" + $buildNumber
}

# Note: nuget appears to exit with code 0 in every case, so there's no way to detect failure here
#       other than looking at the output.
foreach ($package in $packages) {
    Invoke-Expression "$nugetPath pack $packageOutputDir\$package.nuspec -NoPackageAnalysis -NoDefaultExcludes -OutputDirectory $packageOutputDir -Version $packageVersion"
    Invoke-Expression "$nugetPath push -NonInteractive $packageOutputDir\$package.nupkg -s $feed $apiKey"
}

Invoke-Expression "$nugetPath push -NonInteractive $packageOutputDir\toolchain.win7-x64.Microsoft.DotNet.RyuJit.nupkg -s $feed $apiKey"
