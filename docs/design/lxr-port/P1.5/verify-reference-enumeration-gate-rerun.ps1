# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

[CmdletBinding()]
param(
    [string]$RepositoryRoot,
    [string]$Revision = 'HEAD'
)

$ErrorActionPreference = 'Stop'
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
if (-not $RepositoryRoot) {
    $RepositoryRoot = (Resolve-Path (
        Join-Path $scriptRoot '..\..\..\..')).Path
}

$gate = Join-Path $scriptRoot 'verify-reference-enumeration-gate.ps1'
$powerShell = (Get-Process -Id $PID).Path
$outputDirectories = [Collections.Generic.List[string]]::new()
$verdicts = @(
    'PASS: clean archive accepted',
    'PASS: exactly one deleted control row rejected',
    'PASS: exactly one deleted scenario invocation row rejected',
    'PASS: exactly one altered induced count rejected')

foreach ($run in 1..2) {
    $output = @(
        & $powerShell -NoProfile -File $gate `
            -RepositoryRoot $RepositoryRoot `
            -Revision $Revision 2>&1)
    $exitCode = $LASTEXITCODE
    $text = $output -join [Environment]::NewLine
    if ($exitCode -ne 0) {
        throw "Default gate run $run failed with exit code $exitCode.$([Environment]::NewLine)$text"
    }

    foreach ($verdict in $verdicts) {
        $count = [regex]::Matches(
            $text,
            [regex]::Escape($verdict)).Count
        if ($count -ne 1) {
            throw "Default gate run $run emitted '$verdict' $count times; expected once."
        }
    }

    $directoryLines = @($output | Where-Object {
        "$_".StartsWith(
            'GATE_OUTPUT_DIRECTORY: ',
            [StringComparison]::Ordinal)
    })
    $cleanupLines = @($output | Where-Object {
        "$_".StartsWith(
            'GATE_OUTPUT_CLEANED: ',
            [StringComparison]::Ordinal)
    })
    if (($directoryLines.Count -ne 1) -or
        ($cleanupLines.Count -ne 1)) {
        throw "Default gate run $run did not report one created and cleaned directory."
    }

    $directory = "$($directoryLines[0])".Substring(
        'GATE_OUTPUT_DIRECTORY: '.Length)
    $cleanedDirectory = "$($cleanupLines[0])".Substring(
        'GATE_OUTPUT_CLEANED: '.Length)
    if (($directory -ne $cleanedDirectory) -or
        (Test-Path -LiteralPath $directory)) {
        throw "Default gate run $run did not clean its unique output: $directory"
    }

    $outputDirectories.Add($directory)
    Write-Host (
        "PASS: default gate run $run accepted the archive and rejected " +
        'all three perturbations')
}

if ((@($outputDirectories | Sort-Object -Unique).Count -ne 2) -or
    ($outputDirectories[0] -eq $outputDirectories[1])) {
    throw 'Two default gate runs did not allocate two unique output directories.'
}

Write-Host 'PASS: two default gate runs used two unique temporary directories'
$global:LASTEXITCODE = 0
