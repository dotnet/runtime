# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

[CmdletBinding()]
param(
    [string]$RepositoryRoot,
    [string]$OutputDirectory,
    [string]$Revision = 'HEAD'
)

$ErrorActionPreference = 'Stop'
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
if (-not $RepositoryRoot) {
    $RepositoryRoot = (Resolve-Path (Join-Path $scriptRoot '..\..\..\..')).Path
}
$preserveOutput = -not [string]::IsNullOrWhiteSpace($OutputDirectory)
$runName = (
    'p15-reference-enumeration-gate-' +
    [Guid]::NewGuid().ToString('N'))
if ($preserveOutput) {
    $outputParent = [IO.Path]::GetFullPath($OutputDirectory)
    if (Test-Path -LiteralPath $outputParent -PathType Leaf) {
        throw "Explicit output path is a file: $outputParent"
    }
    New-Item -ItemType Directory -Path $outputParent -Force | Out-Null
    $runRoot = Join-Path $outputParent $runName
} else {
    $runRoot = Join-Path ([IO.Path]::GetTempPath()) $runName
}
if (Test-Path -LiteralPath $runRoot) {
    throw "Unique gate output already exists: $runRoot"
}

$createdRunRoot = $false
$verificationSucceeded = $false
try {
    New-Item -ItemType Directory -Path $runRoot | Out-Null
    $createdRunRoot = $true
    Write-Host "GATE_OUTPUT_DIRECTORY: $runRoot"

    $archive = Join-Path $runRoot 'source.tar'
    $cleanRoot = Join-Path $runRoot 'clean'
    $controlRoot = Join-Path $runRoot 'missing-control-row'
    $scenarioRoot = Join-Path $runRoot 'missing-scenario-row'
    $collectionRoot = Join-Path $runRoot 'wrong-induced-count'

    New-Item -ItemType Directory -Path $cleanRoot | Out-Null
    New-Item -ItemType Directory -Path $controlRoot | Out-Null
    New-Item -ItemType Directory -Path $scenarioRoot | Out-Null
    New-Item -ItemType Directory -Path $collectionRoot | Out-Null

git -C $RepositoryRoot archive --format=tar --output=$archive $Revision
if ($LASTEXITCODE -ne 0) {
    throw "Unable to archive revision $Revision."
}
tar -xf $archive -C $cleanRoot
if ($LASTEXITCODE -ne 0) {
    throw 'Unable to extract clean archive.'
}
tar -xf $archive -C $controlRoot
if ($LASTEXITCODE -ne 0) {
    throw 'Unable to extract control archive.'
}
tar -xf $archive -C $scenarioRoot
if ($LASTEXITCODE -ne 0) {
    throw 'Unable to extract scenario control archive.'
}
tar -xf $archive -C $collectionRoot
if ($LASTEXITCODE -ne 0) {
    throw 'Unable to extract induced-count control archive.'
}

$relativeVerifier = 'docs\design\lxr-port\P1.5\verify-reference-enumeration.ps1'
$cleanVerifier = Join-Path $cleanRoot $relativeVerifier
$controlVerifier = Join-Path $controlRoot $relativeVerifier
$scenarioVerifier = Join-Path $scenarioRoot $relativeVerifier
$collectionVerifier = Join-Path $collectionRoot $relativeVerifier

$powerShell = (Get-Process -Id $PID).Path
& $powerShell -NoProfile -File $cleanVerifier -RepositoryRoot $cleanRoot
if ($LASTEXITCODE -ne 0) {
    throw 'Committed verifier failed on the clean archive.'
}

$controlCsv = Join-Path $controlRoot (
    'docs\design\lxr-port\P1.5\raw\control-summary.csv')
$lines = @(Get-Content -LiteralPath $controlCsv)
if ($lines.Count -ne 10) {
    throw "Control CSV has $($lines.Count) lines; expected 10 before perturbation."
}
Set-Content -LiteralPath $controlCsv -Value $lines[0..8]
$perturbedLines = @(Get-Content -LiteralPath $controlCsv)
if ($perturbedLines.Count -ne 9) {
    throw 'Control perturbation did not delete exactly one row.'
}

$controlLog = Join-Path $runRoot 'missing-control-row.log'
& $powerShell -NoProfile -File $controlVerifier `
    -RepositoryRoot $controlRoot *> $controlLog
$controlExitCode = $LASTEXITCODE
$controlOutput = Get-Content -LiteralPath $controlLog -Raw
if (($controlExitCode -eq 0) -or
    ($controlOutput -notmatch 'Control summary has 8 rows; expected 9')) {
    throw 'Deleted-row perturbation did not fail for the expected reason.'
}

$scenarioCsv = Join-Path $scenarioRoot (
    'docs\design\lxr-port\P1.5\raw\scenario-invocations.csv')
$lines = @(Get-Content -LiteralPath $scenarioCsv)
if ($lines.Count -ne 91) {
    throw "Scenario CSV has $($lines.Count) lines; expected 91 before perturbation."
}
Set-Content -LiteralPath $scenarioCsv -Value $lines[0..89]
$perturbedLines = @(Get-Content -LiteralPath $scenarioCsv)
if ($perturbedLines.Count -ne 90) {
    throw 'Scenario perturbation did not delete exactly one row.'
}

$scenarioLog = Join-Path $runRoot 'missing-scenario-row.log'
& $powerShell -NoProfile -File $scenarioVerifier `
    -RepositoryRoot $scenarioRoot *> $scenarioLog
$scenarioExitCode = $LASTEXITCODE
$scenarioOutput = Get-Content -LiteralPath $scenarioLog -Raw
if (($scenarioExitCode -eq 0) -or
    ($scenarioOutput -notmatch (
        'Scenario invocation data has 89 rows; expected 90'))) {
    throw 'Deleted scenario-row perturbation did not fail for the expected reason.'
}

$collectionCsv = Join-Path $collectionRoot (
    'docs\design\lxr-port\P1.5\raw\scenario-invocations.csv')
$beforeRows = @(Import-Csv -LiteralPath $collectionCsv)
$perturbedRows = @(Import-Csv -LiteralPath $collectionCsv)
$targets = @($perturbedRows | Where-Object {
    ($_.Scenario -eq 'pointer-chasing') -and
    ($_.GC -eq 'wks') -and
    ($_.Deployment -eq 'linked') -and
    ($_.Variant -eq 'callback') -and
    ([int]$_.Invocation -eq 0)
})
if ($targets.Count -ne 1) {
    throw "Induced-count perturbation matched $($targets.Count) rows; expected one."
}
$targets[0].Induced = '0'
$perturbedRows | Export-Csv -LiteralPath $collectionCsv -NoTypeInformation
$afterRows = @(Import-Csv -LiteralPath $collectionCsv)
$differenceCount = 0
for ($rowIndex = 0; $rowIndex -lt $beforeRows.Count; $rowIndex++) {
    foreach ($property in $beforeRows[$rowIndex].PSObject.Properties.Name) {
        if ($beforeRows[$rowIndex].$property -ne
            $afterRows[$rowIndex].$property) {
            $differenceCount++
        }
    }
}
if ($differenceCount -ne 1) {
    throw "Induced-count perturbation changed $differenceCount fields; expected one."
}

$collectionLog = Join-Path $runRoot 'wrong-induced-count.log'
& $powerShell -NoProfile -File $collectionVerifier `
    -RepositoryRoot $collectionRoot *> $collectionLog
$collectionExitCode = $LASTEXITCODE
$collectionOutput = Get-Content -LiteralPath $collectionLog -Raw
if (($collectionExitCode -eq 0) -or
    ($collectionOutput -notmatch (
        'Scenario collection evidence is invalid for pointer-chasing/wks\|linked/callback'))) {
    throw 'Induced-count perturbation did not fail for the expected reason.'
}

Write-Host 'PASS: clean archive accepted'
Write-Host 'PASS: exactly one deleted control row rejected'
Write-Host 'PASS: exactly one deleted scenario invocation row rejected'
Write-Host 'PASS: exactly one altered induced count rejected'
$verificationSucceeded = $true
} finally {
    if ($createdRunRoot -and -not $preserveOutput) {
        try {
            if (Test-Path -LiteralPath $runRoot) {
                Remove-Item -LiteralPath $runRoot -Recurse -Force `
                    -ErrorAction Stop
            }
            Write-Host "GATE_OUTPUT_CLEANED: $runRoot"
        } catch {
            Write-Warning (
                "Unable to clean temporary gate output '$runRoot': " +
                $_.Exception.Message)
        }
    } elseif ($createdRunRoot) {
        Write-Host "GATE_OUTPUT_PRESERVED: $runRoot"
    }
}

if ($verificationSucceeded) {
    $global:LASTEXITCODE = 0
}
