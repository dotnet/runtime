# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

param(
    [Parameter(Mandatory=$true)][string]$MsiPath,
    [Parameter(Mandatory=$true)][string]$MsiVersion,
    [Parameter(Mandatory=$true)][string]$NuspecFile,
    [Parameter(Mandatory=$true)][string]$NupkgFile,
    [Parameter(Mandatory=$true)][string]$Architecture,
    [Parameter(Mandatory=$true)][string]$ComponentName,
    [Parameter(Mandatory=$true)][string]$ComponentFriendlyName,
    [Parameter(Mandatory=$true)][string]$ProjectUrl,
    [Parameter(Mandatory=$true)][string]$BinDir
)

$NuGetDir = Join-Path $BinDir  "nuget"
$NuGetExe = Join-Path $NuGetDir "nuget.exe"
$OutputDirectory = [System.IO.Path]::GetDirectoryName($NupkgFile)

if (-not (Test-Path $NuGetDir)) {
    New-Item -ItemType Directory -Force -Path $NuGetDir | Out-Null
}

if (-not (Test-Path $NuGetExe)) {
    # Using 3.5.0 to workaround https://github.com/NuGet/Home/issues/5016
    Write-Output "Downloading nuget.exe to $NuGetExe"
    wget https://dist.nuget.org/win-x86-commandline/v3.5.0/nuget.exe -OutFile $NuGetExe
}

if (Test-Path $NupkgFile) {
    Remove-Item -Force $NupkgFile
}

& $NuGetExe pack $NuspecFile -Version $MsiVersion -OutputDirectory $OutputDirectory -NoDefaultExcludes -NoPackageAnalysis -Properties COMPONENT_MSI=$MsiPath`;ARCH=$Architecture`;COMPONENT_NAME=$ComponentName`;FRIENDLY_NAME=$ComponentFriendlyName`;PROJECT_URL=$ProjectUrl
Exit $LastExitCode
