# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

<#
.SYNOPSIS
Negative control: an incomplete Core_Root must be rejected before any P2.3
evidence is produced.

.DESCRIPTION
Invokes run-reference-count-runtime-smoke.ps1 - the first evidence harness
run-reference-count-evidence.ps1 points at a Windows runtime root - against a
root that is missing at least one required Core_Root file. The control passes
only when that harness throws the documented completeness error and leaves no
evidence behind.

The control deliberately does not invoke run-reference-count-evidence.ps1:
the wrapper invokes this control, so calling back into it would recurse.

.PARAMETER InvalidRuntimeRoot
A directory that looks like a Core_Root but is missing System.Runtime.dll (or
another required file). It is never modified.
#>
[CmdletBinding()]
param(
    [string]$RepositoryRoot,
    [Parameter(Mandatory)]
    [string]$InvalidRuntimeRoot,
    [string]$OutputDirectory,
    [string]$MissingFile = 'System.Runtime.dll'
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
        'artifacts\P2.3\core-root-control\' + [guid]::NewGuid().ToString('N'))
} else {
    $OutputDirectory = $PSCmdlet.GetUnresolvedProviderPathFromPSPath($OutputDirectory)
}
if (Test-Path -LiteralPath $OutputDirectory) {
    throw "OutputDirectory already exists; caller-owned output is not modified: '$OutputDirectory'."
}
if (Test-Path -LiteralPath (Join-Path $InvalidRuntimeRoot $MissingFile)) {
    throw "InvalidRuntimeRoot must not contain '$MissingFile'; the control would be vacuous."
}

New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$attemptOutput = Join-Path $OutputDirectory 'unexpected-evidence'
$log = Join-Path $OutputDirectory 'invalid-coreroot-control.log'
$exitCode = 0
try {
    & (Join-Path $scriptRoot 'run-reference-count-runtime-smoke.ps1') `
        -RepositoryRoot $RepositoryRoot `
        -RuntimeRoot $InvalidRuntimeRoot `
        -OutputDirectory $attemptOutput
    throw 'Incomplete Core_Root unexpectedly reached evidence execution.'
} catch {
    $message = $_.Exception.Message
    if (($message -notmatch 'RuntimeRoot must be a complete CoreRoot') -or
        ($message -notmatch [regex]::Escape($MissingFile))) {
        throw
    }
    $exitCode = 1
    $message | Out-File $log -Encoding utf8
}

if (Test-Path -LiteralPath $attemptOutput) {
    $unexpected = @(Get-ChildItem -LiteralPath $attemptOutput -Force -Recurse)
    if ($unexpected.Count -ne 0) {
        throw 'Incomplete Core_Root produced evidence before rejection.'
    }
}

[pscustomobject][ordered]@{
    missing_file = $MissingFile
    invalid_runtime_root = $InvalidRuntimeRoot
    exit_code = $exitCode
    result = 'PASS'
    log = [IO.Path]::GetFileName($log)
} | Export-Csv (Join-Path $OutputDirectory 'core-root-control-summary.csv') -NoTypeInformation

Write-Host 'PASS: incomplete Core_Root rejected before evidence execution'
Write-Host "Output: $OutputDirectory"
