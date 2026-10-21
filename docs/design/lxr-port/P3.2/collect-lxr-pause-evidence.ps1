# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string[]]$ValidationDirectories,
    [Parameter(Mandatory)]
    [string[]]$RuntimeSmokeDirectories,
    [Parameter(Mandatory)]
    [string]$BenchmarkDirectory,
    [Parameter(Mandatory)]
    [string]$BuildSummaryPath,
    [Parameter(Mandatory)]
    [string]$AttemptSummaryPath,
    [Parameter(Mandatory)]
    [string]$MappingSummaryPath,
    [Parameter(Mandatory)]
    [string]$SourceCommit,
    [string[]]$SupersededBenchmarkDirectories = @(),
    [string[]]$AdditionalLogPaths = @(),
    [string]$RepositoryRoot
)

$ErrorActionPreference = 'Stop'
$scriptRoot = $PSScriptRoot
if (-not $RepositoryRoot) {
    $RepositoryRoot = (Resolve-Path (Join-Path $scriptRoot '..\..\..\..')).ProviderPath
} else {
    $RepositoryRoot = (Resolve-Path -LiteralPath $RepositoryRoot).ProviderPath
}
$BenchmarkDirectory = (Resolve-Path -LiteralPath $BenchmarkDirectory).ProviderPath
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

@($ValidationDirectories | ForEach-Object {
    Import-Csv (Join-Path (Resolve-Path -LiteralPath $_).ProviderPath 'validation-summary.csv')
}) | Export-Csv (Join-Path $raw 'validation-summary.csv') -NoTypeInformation

@($RuntimeSmokeDirectories | ForEach-Object {
    Import-Csv (Join-Path (Resolve-Path -LiteralPath $_).ProviderPath 'runtime-smoke-summary.csv')
}) | Export-Csv (Join-Path $raw 'runtime-smoke-summary.csv') -NoTypeInformation

foreach ($name in @(
    'benchmark-raw.csv',
    'benchmark-summary.csv',
    'benchmark-controls.csv',
    'benchmark-invocation.csv')) {
    Copy-Item -LiteralPath (Join-Path $BenchmarkDirectory $name) -Destination (
        Join-Path $raw $name)
}
Copy-Item -LiteralPath (Resolve-Path -LiteralPath $BuildSummaryPath) -Destination (
    Join-Path $raw 'build-summary.csv')
Copy-Item -LiteralPath (Resolve-Path -LiteralPath $AttemptSummaryPath) -Destination (
    Join-Path $raw 'attempt-summary.csv')
Copy-Item -LiteralPath (Resolve-Path -LiteralPath $MappingSummaryPath) -Destination (
    Join-Path $raw 'mapping-summary.csv')

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

$logScratch = Join-Path $RepositoryRoot (
    'artifacts\P3.2\evidence-logs-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $logScratch | Out-Null
try {
    $logIndex = 0
    $logRoots = @(
        $ValidationDirectories +
        $RuntimeSmokeDirectories +
        @($BenchmarkDirectory)
    )
    foreach ($root in $logRoots) {
        foreach ($log in Get-ChildItem -LiteralPath (
            (Resolve-Path -LiteralPath $root).ProviderPath) -Recurse -Filter '*.log') {
            $logIndex++
            Copy-Item -LiteralPath $log.FullName -Destination (
                Join-Path $logScratch (
                    '{0:D4}-{1}' -f $logIndex, $log.Name))
        }
    }
    foreach ($path in $AdditionalLogPaths) {
        $logIndex++
        $resolved = (Resolve-Path -LiteralPath $path).ProviderPath
        Copy-Item -LiteralPath $resolved -Destination (
            Join-Path $logScratch (
                '{0:D4}-{1}' -f $logIndex, [IO.Path]::GetFileName($resolved)))
    }
    Compress-Archive -LiteralPath (
        Get-ChildItem -LiteralPath $logScratch -File).FullName `
        -DestinationPath (Join-Path $raw 'logs.zip')
} finally {
    Remove-Item -LiteralPath $logScratch -Recurse -Force
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
        $path = Join-Path $resolved $name
        if (Test-Path -LiteralPath $path -PathType Leaf) {
            Copy-Item -LiteralPath $path -Destination (Join-Path $target $name)
        }
    }
}

Write-Host "PASS: collected P3.2 raw evidence for $SourceCommit"
