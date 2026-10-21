# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$EvidenceDirectory,
    [Parameter(Mandatory)]
    [string]$SourceCommit,
    [string]$RepositoryRoot
)

$ErrorActionPreference = 'Stop'
$scriptRoot = $PSScriptRoot
if (-not $RepositoryRoot) {
    $RepositoryRoot = (Resolve-Path (Join-Path $scriptRoot '..\..\..\..')).ProviderPath
} else {
    $RepositoryRoot = (Resolve-Path -LiteralPath $RepositoryRoot).ProviderPath
}
$EvidenceDirectory = (Resolve-Path -LiteralPath $EvidenceDirectory).ProviderPath
git -C $RepositoryRoot cat-file -e "$SourceCommit`^{commit}"
if ($LASTEXITCODE -ne 0) {
    throw 'Source commit does not exist.'
}
if (@(git -C $RepositoryRoot status --porcelain=v1 -uall).Count -ne 0) {
    throw 'Evidence collection requires a clean worktree.'
}

$raw = Join-Path $scriptRoot 'raw'
if (Test-Path -LiteralPath $raw) {
    throw "Raw evidence already exists: '$raw'."
}
New-Item -ItemType Directory -Path $raw | Out-Null
foreach ($name in @(
    'validation-summary.csv',
    'runtime-smoke-summary.csv',
    'benchmark-raw.csv',
    'benchmark-summary.csv',
    'benchmark-controls.csv',
    'benchmark-invocation.csv',
    'invocation.csv',
    'build-summary.csv',
    'cross-target-summary.csv',
    'regression-summary.csv',
    'review-summary.csv',
    'attempt-summary.csv',
    'baseline-summary.csv')) {
    $source = Join-Path $EvidenceDirectory $name
    if (-not (Test-Path -LiteralPath $source -PathType Leaf)) {
        throw "Evidence input is missing: '$source'."
    }
    Copy-Item -LiteralPath $source -Destination (Join-Path $raw $name)
}

$sourcePaths = @(
    Get-Content -LiteralPath (Join-Path $scriptRoot 'source-manifest.txt') |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
)
$sourceIdentities = foreach ($path in $sourcePaths) {
    $full = Join-Path $RepositoryRoot $path
    if (-not (Test-Path -LiteralPath $full -PathType Leaf)) {
        throw "Source manifest path is missing: '$path'."
    }
    [pscustomobject][ordered]@{
        path = $path
        sha256 = (Get-FileHash -LiteralPath $full -Algorithm SHA256).Hash.ToLowerInvariant()
    }
}
$sourceIdentities | Export-Csv (Join-Path $raw 'source-identities.csv') -NoTypeInformation

$priorPaths = @(
    Get-ChildItem -LiteralPath (Join-Path $RepositoryRoot 'docs\design\lxr-port\P3.1\raw') -Recurse -File
    Get-ChildItem -LiteralPath (Join-Path $RepositoryRoot 'docs\design\lxr-port\P3.3\raw') -Recurse -File
)
$priorIdentities = foreach ($file in $priorPaths) {
    [pscustomobject][ordered]@{
        path = [IO.Path]::GetRelativePath($RepositoryRoot, $file.FullName).Replace('\', '/')
        sha256 = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
    }
}
$priorIdentities | Sort-Object path |
    Export-Csv (Join-Path $raw 'prior-raw-identities.csv') -NoTypeInformation

$SourceCommit | Out-File (Join-Path $raw 'source-commit.txt') -Encoding ascii -NoNewline
Compress-Archive -Path (Join-Path $EvidenceDirectory '*') -DestinationPath (
    Join-Path $raw 'logs.zip')
Write-Host "PASS: collected P3.4 evidence for $SourceCommit"
