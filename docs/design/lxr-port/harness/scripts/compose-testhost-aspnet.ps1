<#
.SYNOPSIS
    Adds an ASP.NET Core shared framework to the locally built testhost so the aspnet-request-load
    scenario can run against a locally built runtime.

.DESCRIPTION
    This repository builds Microsoft.NETCore.App but not Microsoft.AspNetCore.App, so a stock
    testhost cannot host Kestrel. The flagship scenario carries the acceptance signal, and P0.2
    established that application-observed latency is that signal, so leaving it unable to reach a
    locally built runtime would have left the LXR arm with no acceptance evidence at all.

    The composition copies the bootstrapped SDK's Microsoft.AspNetCore.App into the testhost's
    shared framework directory. The two are built from the same source tree at compatible versions,
    and ASP.NET rolls forward across the preview-to-release boundary, so the framework resolves.
    What matters is that Microsoft.NETCore.App underneath it stays the locally built one: this
    script does not touch it, and the harness proves at run time which coreclr.dll was loaded by
    recording its SHA-256 in every result record.

.PARAMETER RepoRoot
    Repository root. Defaults to the root this script ships in.

.PARAMETER Configuration
    Runtime build configuration. Defaults to Release.

.PARAMETER Force
    Overwrite an existing composed framework instead of leaving it alone.

.EXAMPLE
    ./compose-testhost-aspnet.ps1
    Composes using the defaults, then prints the coreclr.dll hash to compare against a run record.
#>
[CmdletBinding()]
param(
    [string] $RepoRoot,
    [string] $Configuration = 'Release',
    [switch] $Force
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if (-not $RepoRoot) {
    # Derived from the script's own location so it always composes the tree it ships in, never a
    # different checkout that happens to be the working directory.
    # scripts -> harness -> lxr-port -> design -> docs -> repository root.
    $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..\..\..')).Path
}

$dotnetRoot = Join-Path $RepoRoot '.dotnet'
if (-not (Test-Path $dotnetRoot)) {
    throw "No bootstrapped SDK at $dotnetRoot. Run the repository's build once so it restores one."
}

$testhostParent = Join-Path $RepoRoot 'artifacts\bin\testhost'
if (-not (Test-Path $testhostParent)) {
    throw "No testhost at $testhostParent. Build it with: build.cmd clr+libs -rc $Configuration -lc $Configuration"
}

$testhost = Get-ChildItem $testhostParent -Directory |
    Where-Object { $_.Name -like "*-$Configuration-*" } |
    Sort-Object Name |
    Select-Object -Last 1

if (-not $testhost) {
    throw "No $Configuration testhost under $testhostParent."
}

$sourceAspNet = Get-ChildItem (Join-Path $dotnetRoot 'shared\Microsoft.AspNetCore.App') -Directory |
    Sort-Object Name |
    Select-Object -Last 1

if (-not $sourceAspNet) {
    throw "The bootstrapped SDK at $dotnetRoot has no Microsoft.AspNetCore.App to copy."
}

$targetShared = Join-Path $testhost.FullName 'shared\Microsoft.AspNetCore.App'
$target = Join-Path $targetShared $sourceAspNet.Name

if ((Test-Path $target) -and -not $Force) {
    Write-Host "Already composed: $target (pass -Force to overwrite)."
}
else {
    if (Test-Path $target) {
        Remove-Item $target -Recurse -Force
    }

    New-Item -ItemType Directory -Path $targetShared -Force | Out-Null
    Copy-Item $sourceAspNet.FullName $target -Recurse -Force
    Write-Host "Composed $($sourceAspNet.Name) into $targetShared"
}

# The whole point of the exercise is that Microsoft.NETCore.App underneath is still the locally
# built one. Print its hash so it can be compared against coreclrSha256 in a run record - an
# assertion the harness itself makes, but which is worth being able to check by hand.
$coreclr = Join-Path $testhost.FullName "shared\Microsoft.NETCore.App\*\coreclr.dll"
$resolved = Get-Item $coreclr -ErrorAction SilentlyContinue | Select-Object -First 1
if ($resolved) {
    $hash = (Get-FileHash $resolved.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
    Write-Host ""
    Write-Host "testhost:      $($testhost.FullName)"
    Write-Host "coreclr.dll:   $($resolved.FullName)"
    Write-Host "coreclrSha256: $hash"
    Write-Host ""
    Write-Host "A run record from --host testhost must carry exactly this coreclrSha256, or it did not"
    Write-Host "run on the locally built runtime."
}
else {
    Write-Warning "No coreclr.dll under the testhost's Microsoft.NETCore.App - the composition is incomplete."
}
