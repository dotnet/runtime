# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

[CmdletBinding()]
param(
    [string]$RepositoryRoot,
    [Parameter(Mandatory)]
    [string]$InvalidRuntimeRoot,
    [string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
if (-not $RepositoryRoot) {
    $RepositoryRoot = (Resolve-Path (Join-Path $scriptRoot '..\..\..\..')).Path
}
$RepositoryRoot = (Resolve-Path -LiteralPath $RepositoryRoot).ProviderPath
$InvalidRuntimeRoot = (Resolve-Path -LiteralPath $InvalidRuntimeRoot).ProviderPath
if (-not $OutputDirectory) {
    $OutputDirectory = Join-Path $RepositoryRoot (
        'artifacts\P2.1\core-root-control\' + [guid]::NewGuid().ToString('N'))
} else {
    $OutputDirectory = $PSCmdlet.GetUnresolvedProviderPathFromPSPath($OutputDirectory)
}

New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$attemptOutput = Join-Path $OutputDirectory 'unexpected-evidence'
$log = Join-Path $OutputDirectory 'invalid-coreroot-control.log'
$exitCode = 0
try {
    & (Join-Path $scriptRoot 'run-side-metadata-evidence.ps1') `
        -RepositoryRoot $RepositoryRoot `
        -RuntimeRoot $InvalidRuntimeRoot `
        -OutputDirectory $attemptOutput `
        -LaunchCount 1 `
        -WarmupCount 1 `
        -IterationCount 1
    throw 'Incomplete CoreRoot unexpectedly reached evidence execution.'
} catch {
    $message = $_.Exception.Message
    if (($message -notmatch 'RuntimeRoot must be a complete CoreRoot') -or
        ($message -notmatch 'System.Runtime.dll')) {
        throw
    }
    $exitCode = 1
    $message | Out-File $log -Encoding utf8
}

if (Test-Path -LiteralPath $attemptOutput) {
    $unexpected = @(Get-ChildItem -LiteralPath $attemptOutput -Force)
    if ($unexpected.Count -ne 0) {
        throw 'Incomplete CoreRoot produced evidence before rejection.'
    }
}

[pscustomobject][ordered]@{
    missing_file = 'System.Runtime.dll'
    exit_code = $exitCode
    result = 'PASS'
    log = [IO.Path]::GetFileName($log)
} | Export-Csv (Join-Path $OutputDirectory 'core-root-control-summary.csv') -NoTypeInformation

Write-Host 'PASS: incomplete CoreRoot rejected before evidence execution'
Write-Host "Output: $OutputDirectory"
