# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$EvidenceDirectory,
    [Parameter(Mandatory)]
    [string]$SourceCommit,
    [string[]]$SupersededBenchmarkDirectories = @(),
    [string[]]$AttemptLogPaths = @(),
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
if ($SourceCommit -notmatch '^[0-9a-f]{40}$') {
    throw 'SourceCommit must be a full lowercase commit hash.'
}
git -C $RepositoryRoot cat-file -e "$SourceCommit`^{commit}"
if ($LASTEXITCODE -ne 0) {
    throw 'SourceCommit does not exist.'
}
if ((git -C $RepositoryRoot rev-parse HEAD).Trim() -ne $SourceCommit) {
    throw 'Evidence must be collected while HEAD is the immutable source commit.'
}
if (@(git -C $RepositoryRoot status --porcelain=v1).Count -ne 0) {
    throw 'Evidence collection requires a clean source worktree.'
}

$raw = Join-Path $scriptRoot 'raw'
if (Test-Path -LiteralPath $raw) {
    throw "Raw evidence directory already exists: '$raw'."
}
New-Item -ItemType Directory -Path $raw | Out-Null

foreach ($name in @(
    'validation-summary.csv',
    'runtime-smoke-summary.csv',
    'benchmark-raw.csv',
    'benchmark-summary.csv',
    'benchmark-controls.csv',
    'benchmark-invocation.csv',
    'invocation.csv')) {
    Copy-Item -LiteralPath (Join-Path $EvidenceDirectory $name) -Destination (
        Join-Path $raw $name)
}

$sourcePaths = @(
    Get-Content -LiteralPath (Join-Path $scriptRoot 'source-manifest.txt') |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
)
$identities = foreach ($path in $sourcePaths) {
    [pscustomobject][ordered]@{
        path = $path
        sha256 = (Get-FileHash -Algorithm SHA256 (
            Join-Path $RepositoryRoot $path)).Hash.ToLowerInvariant()
    }
}
$identities | Export-Csv (Join-Path $raw 'source-identities.csv') -NoTypeInformation
$SourceCommit | Out-File (Join-Path $raw 'source-commit.txt') -Encoding ascii -NoNewline

$logs = Join-Path $raw 'logs.zip'
$logInputs = Get-ChildItem -LiteralPath $EvidenceDirectory -Recurse -Filter '*.log'
Compress-Archive -LiteralPath $logInputs.FullName -DestinationPath $logs

if ($AttemptLogPaths.Count -gt 0) {
    $attemptScratch = Join-Path $RepositoryRoot (
        'artifacts\P3.1\attempt-log-archive-' + [guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $attemptScratch | Out-Null
    try {
        foreach ($path in $AttemptLogPaths) {
            $resolved = (Resolve-Path -LiteralPath $path).ProviderPath
            Copy-Item -LiteralPath $resolved -Destination (
                Join-Path $attemptScratch ([IO.Path]::GetFileName($resolved)))
        }
        Compress-Archive -LiteralPath (
            Get-ChildItem -LiteralPath $attemptScratch -File).FullName `
            -DestinationPath (Join-Path $raw 'attempt-logs.zip')
    } finally {
        Remove-Item -LiteralPath $attemptScratch -Recurse -Force
    }
}

$superseded = Join-Path $raw 'superseded'
foreach ($directory in $SupersededBenchmarkDirectories) {
    $resolved = (Resolve-Path -LiteralPath $directory).ProviderPath
    $target = Join-Path $superseded ([IO.Path]::GetFileName($resolved))
    New-Item -ItemType Directory -Path $target -Force | Out-Null
    foreach ($name in @(
        'benchmark-raw.csv',
        'benchmark-summary.csv',
        'benchmark-controls.csv',
        'benchmark-invocation.csv')) {
        Copy-Item -LiteralPath (Join-Path $resolved $name) -Destination (
            Join-Path $target $name)
    }
}

Write-Host "PASS: collected P3.1 raw evidence for $SourceCommit"
