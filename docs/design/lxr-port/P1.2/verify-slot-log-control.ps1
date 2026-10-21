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

$temporaryRoot = Join-Path ([IO.Path]::GetTempPath()) "p12-slot-log-$([Guid]::NewGuid())"
$archive = Join-Path $temporaryRoot 'tip.tar'
$original = Join-Path $temporaryRoot 'original'
$perturbed = Join-Path $temporaryRoot 'perturbed'

function Assert-LiteralCount(
    [string]$Text,
    [string]$Value,
    [int]$ExpectedCount,
    [string]$Description
) {
    $actualCount = [regex]::Matches($Text, [regex]::Escape($Value)).Count
    if ($actualCount -ne $ExpectedCount) {
        throw "$Description matched $actualCount sites; expected $ExpectedCount."
    }
}

try {
    New-Item -ItemType Directory -Path $original -Force | Out-Null
    git -C $RepositoryRoot archive --format=tar --output=$archive HEAD
    if ($LASTEXITCODE -ne 0) {
        throw 'Unable to archive HEAD.'
    }

    tar -xf $archive -C $original
    if ($LASTEXITCODE -ne 0) {
        throw 'Unable to extract HEAD.'
    }

    $verifier = Join-Path $original 'docs\design\lxr-port\P1.2\verify-slot-log.ps1'
    & pwsh -NoProfile -File $verifier -RepositoryRoot $original
    if ($LASTEXITCODE -ne 0) {
        throw 'Verifier rejected the exact committed archive.'
    }

    Copy-Item -LiteralPath $original -Destination $perturbed -Recurse
    $summary = Join-Path $perturbed 'docs\design\lxr-port\P1.2\raw\end-to-end-summary.csv'
    $rows = @(Import-Csv -LiteralPath $summary)
    $rows[0].SlotOverStock = '9.9999'
    $rows | Export-Csv -LiteralPath $summary -NoTypeInformation

    $perturbedVerifier = Join-Path $perturbed 'docs\design\lxr-port\P1.2\verify-slot-log.ps1'
    & pwsh -NoProfile -File $perturbedVerifier -RepositoryRoot $perturbed *> $null
    if ($LASTEXITCODE -eq 0) {
        throw 'Verifier accepted a perturbed published ratio.'
    }

    Copy-Item -LiteralPath (
        Join-Path $original 'docs\design\lxr-port\P1.2\raw\end-to-end-summary.csv') -Destination $summary
    $gasBarrier = Join-Path $perturbed 'src\coreclr\vm\amd64\jithelpers_fastwritebarriers.S'
    $gasBarrierText = Get-Content -LiteralPath $gasBarrier -Raw
    Assert-LiteralCount $gasBarrierText 'xor     al, 0xA5' 2 'Polarity sentinel'
    $gasBarrierText = $gasBarrierText.Replace('xor     al, 0xA5', 'xor     al, 0xF0')
    Assert-LiteralCount $gasBarrierText 'xor     al, 0xA5' 0 'Original polarity sentinel after perturbation'
    Assert-LiteralCount $gasBarrierText 'xor     al, 0xF0' 2 'Perturbed polarity sentinel'
    Set-Content -LiteralPath $gasBarrier -Value $gasBarrierText -NoNewline
    & pwsh -NoProfile -File $perturbedVerifier -RepositoryRoot $perturbed *> $null
    if ($LASTEXITCODE -eq 0) {
        throw 'Verifier accepted a perturbed polarity sentinel.'
    }

    Copy-Item -LiteralPath (
        Join-Path $original 'src\coreclr\vm\amd64\jithelpers_fastwritebarriers.S') -Destination $gasBarrier
    $rangeCallback = Join-Path $perturbed 'src\coreclr\gc\standardwritebarriertest.cpp'
    $rangeCallbackText = Get-Content -LiteralPath $rangeCallback -Raw
    $nullSourceExpression = '(source == nullptr) ? nullptr : source[index]'
    Assert-LiteralCount $rangeCallbackText $nullSourceExpression 1 'Null-source range expression'
    $rangeCallbackText = $rangeCallbackText.Replace($nullSourceExpression, 'source[index]')
    Assert-LiteralCount $rangeCallbackText $nullSourceExpression 0 (
        'Null-source range expression after perturbation')
    Set-Content -LiteralPath $rangeCallback -Value $rangeCallbackText -NoNewline
    & pwsh -NoProfile -File $perturbedVerifier -RepositoryRoot $perturbed *> $null
    if ($LASTEXITCODE -eq 0) {
        throw 'Verifier accepted a null-source range dereference.'
    }

    Copy-Item -LiteralPath (
        Join-Path $original 'src\coreclr\gc\standardwritebarriertest.cpp') -Destination $rangeCallback
    $gcHelpers = Join-Path $perturbed 'src\coreclr\vm\gchelpers.cpp'
    $gcHelpersText = Get-Content -LiteralPath $gcHelpers -Raw
    $dependentEdgeRegex = [regex]::new(
        'if \(ref->Collectible\(\)\)\s*\{\s*Object\* newLoaderAllocator',
        [Text.RegularExpressions.RegexOptions]::CultureInvariant)
    $dependentEdgeMatches = $dependentEdgeRegex.Matches($gcHelpersText).Count
    if ($dependentEdgeMatches -ne 1) {
        throw "Collectible dependent-edge gate matched $dependentEdgeMatches sites; expected 1."
    }
    $gcHelpersText = $dependentEdgeRegex.Replace(
        $gcHelpersText,
        "if (true)`n    {`n        Object* newLoaderAllocator",
        1)
    if ($dependentEdgeRegex.IsMatch($gcHelpersText)) {
        throw 'Collectible dependent-edge gate remained after one-site perturbation.'
    }
    Set-Content -LiteralPath $gcHelpers -Value $gcHelpersText -NoNewline
    & pwsh -NoProfile -File $perturbedVerifier -RepositoryRoot $perturbed *> $null
    if ($LASTEXITCODE -eq 0) {
        throw 'Verifier accepted an unconditional dependent edge.'
    }

    & pwsh -NoProfile -File $verifier -RepositoryRoot $original
    if ($LASTEXITCODE -ne 0) {
        throw 'Verifier did not pass again on the untouched archive.'
    }

    Write-Host (
        'RESULT: PASS (archive pass, ratio/sentinel/null-source/dependent-edge perturbations fail, ' +
        'archive re-pass)')
} finally {
    if (Test-Path -LiteralPath $temporaryRoot) {
        Remove-Item -LiteralPath $temporaryRoot -Recurse -Force
    }
}
