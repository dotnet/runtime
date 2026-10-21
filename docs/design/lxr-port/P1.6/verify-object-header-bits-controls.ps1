# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

[CmdletBinding()]
param(
    [string]$RepositoryRoot
)

$ErrorActionPreference = 'Stop'
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path

if (-not $RepositoryRoot) {
    $RepositoryRoot = (Resolve-Path (Join-Path $scriptRoot '..\..\..\..')).Path
}

function Invoke-Control(
    [string]$Name,
    [string]$EnvironmentVariable,
    [string]$ExpectedFailure
) {
    $log = Join-Path $RepositoryRoot "artifacts\p1.6-$Name-control.log"
    $saved = [Environment]::GetEnvironmentVariable($EnvironmentVariable)
    try {
        [Environment]::SetEnvironmentVariable($EnvironmentVariable, '1')
        & powershell.exe -NoProfile -ExecutionPolicy Bypass -File `
            (Join-Path $scriptRoot 'run-object-header-bits-runtime.ps1') `
            -RepositoryRoot $RepositoryRoot -GCMode Workstation *> $log
        $exitCode = $LASTEXITCODE
    }
    finally {
        [Environment]::SetEnvironmentVariable($EnvironmentVariable, $saved)
    }

    $output = Get-Content -LiteralPath $log -Raw
    if (($exitCode -eq 0) -or ($output -notmatch $ExpectedFailure)) {
        throw "$Name control did not fail for the expected reason."
    }
}

Invoke-Control `
    'post-compaction-clear' `
    'P16_CLEAR_AFTER_COMPACTION_CONTROL' `
    'state 2 survives compacting GC'
Invoke-Control `
    'stale-reuse' `
    'P16_STALE_REUSE_CONTROL' `
    'recycled Large header starts clear'

Write-Host '2/2 object-header bit controls failed as expected'
exit 0
