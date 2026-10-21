# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$WindowsValidation,
    [Parameter(Mandatory)]
    [string]$LinuxValidation,
    [Parameter(Mandatory)]
    [string]$Benchmark,
    [Parameter(Mandatory)]
    [string]$RuntimeSmoke,
    [string]$RepositoryRoot,
    [string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
if (-not $OutputDirectory) {
    $OutputDirectory = Join-Path $scriptRoot 'raw'
}
if (-not $RepositoryRoot) {
    $RepositoryRoot = (Resolve-Path (Join-Path $scriptRoot '..\..\..\..')).Path
}
$RepositoryRoot = (Resolve-Path -LiteralPath $RepositoryRoot).ProviderPath
foreach ($name in @('WindowsValidation', 'LinuxValidation', 'Benchmark', 'RuntimeSmoke')) {
    Set-Variable -Name $name -Value (
        Resolve-Path -LiteralPath (Get-Variable -Name $name -ValueOnly)
    ).ProviderPath
}
if ($OutputDirectory) {
    $OutputDirectory = $PSCmdlet.GetUnresolvedProviderPathFromPSPath($OutputDirectory)
}
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$logs = Join-Path $OutputDirectory 'logs'
New-Item -ItemType Directory -Path $logs -Force | Out-Null

$windowsRows = @(
    Import-Csv (Join-Path $WindowsValidation 'validation-summary.csv')
)
$linuxRows = @(
    Import-Csv (Join-Path $LinuxValidation 'validation-summary.csv')
)
$validation = [Collections.Generic.List[object]]::new()
foreach ($row in $windowsRows) {
    $logName = "validation-$($row.platform).log"
    $architecture = $row.platform -replace '^windows-', ''
    $sourceLog = Join-Path $WindowsValidation "$architecture\run.log"
    Copy-Item -LiteralPath $sourceLog -Destination (Join-Path $logs $logName) -Force
    $validation.Add([pscustomobject][ordered]@{
        platform = $row.platform
        passed = $row.passed
        total = $row.total
        result = $row.result
        log = "logs\$logName"
    })
}
foreach ($row in $linuxRows) {
    $logName = "validation-$($row.platform).log"
    Copy-Item -LiteralPath (Join-Path $LinuxValidation 'run.log') `
        -Destination (Join-Path $logs $logName) -Force
    $validation.Add([pscustomobject][ordered]@{
        platform = $row.platform
        passed = $row.passed
        total = $row.total
        result = $row.result
        log = "logs\$logName"
    })
}
$validation |
    Export-Csv (Join-Path $OutputDirectory 'validation-summary.csv') -NoTypeInformation
Copy-Item -LiteralPath (Join-Path $WindowsValidation 'attempts.csv') `
    -Destination (Join-Path $OutputDirectory 'windows-validation-attempts.csv') -Force
Copy-Item -LiteralPath (Join-Path $LinuxValidation 'validation-summary.csv') `
    -Destination (Join-Path $OutputDirectory 'linux-validation-summary.csv') -Force
Copy-Item -LiteralPath (Join-Path $LinuxValidation 'command.txt') `
    -Destination (Join-Path $OutputDirectory 'linux-validation-command.txt') -Force

foreach ($file in @(
    'benchmark-raw.csv',
    'benchmark-controls.csv',
    'benchmark-identity.csv'
)) {
    Copy-Item -LiteralPath (Join-Path $Benchmark $file) `
        -Destination (Join-Path $OutputDirectory $file) -Force
}
Copy-Item -LiteralPath (Join-Path $RuntimeSmoke 'runtime-smoke-summary.csv') `
    -Destination (Join-Path $OutputDirectory 'runtime-smoke-summary.csv') -Force

function Get-CanonicalIdentity([string]$Path) {
    $text = [IO.File]::ReadAllText($Path).Replace("`r`n", "`n").Replace("`r", "`n")
    $bytes = [Text.UTF8Encoding]::new($false).GetBytes($text)
    $algorithm = [Security.Cryptography.SHA256]::Create()
    try {
        $hash = [BitConverter]::ToString($algorithm.ComputeHash($bytes)).Replace('-', '')
    } finally {
        $algorithm.Dispose()
    }
    return [pscustomobject]@{
        Hash = $hash
        Length = $bytes.Length
    }
}

$sourceCommit = (Get-Content (Join-Path $OutputDirectory 'source-commit.txt') -Raw).Trim()
$identityRows = foreach ($name in Get-Content (Join-Path $scriptRoot 'source-manifest.txt')) {
    if ($name) {
        $path = Join-Path $RepositoryRoot $name
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
            throw "Identity source is missing: $name"
        }
        $identity = Get-CanonicalIdentity $path
        [pscustomobject][ordered]@{
            name = $name
            implementation_commit = $sourceCommit
            canonical_sha256 = $identity.Hash
            canonical_length = $identity.Length
        }
    }
}
$identityRows |
    Export-Csv (Join-Path $OutputDirectory 'source-identities.csv') -NoTypeInformation

Write-Host "Collected P2.1 summaries in $OutputDirectory"
