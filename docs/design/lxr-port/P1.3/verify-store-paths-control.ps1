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

$temporaryRoot = Join-Path ([IO.Path]::GetTempPath()) "p13-store-paths-$([Guid]::NewGuid())"
$archive = Join-Path $temporaryRoot 'tip.tar'
$archiveRoot = Join-Path $temporaryRoot 'original'
$perturbedRoot = Join-Path $temporaryRoot 'perturbed'
$perturbationCount = 0

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

function Invoke-Perturbation(
    [string]$RelativePath,
    [string]$OriginalText,
    [string]$ReplacementText,
    [string]$Description
) {
    $sourcePath = Join-Path $archiveRoot $RelativePath
    $path = Join-Path $perturbedRoot $RelativePath
    Copy-Item -LiteralPath $sourcePath -Destination $path -Force
    try {
        $text = Get-Content -LiteralPath $path -Raw
        Assert-LiteralCount $text $OriginalText 1 "$Description original"
        $text = $text.Replace($OriginalText, $ReplacementText)
        Assert-LiteralCount $text $OriginalText 0 "$Description original after perturbation"
        Assert-LiteralCount $text $ReplacementText 1 "$Description replacement"
        Set-Content -LiteralPath $path -Value $text -NoNewline

        $verifier = Join-Path $perturbedRoot 'docs\design\lxr-port\P1.3\verify-store-paths.ps1'
        & pwsh -NoProfile -File $verifier -RepositoryRoot $perturbedRoot *> $null
        if ($LASTEXITCODE -eq 0) {
            throw "Verifier accepted perturbation: $Description"
        }
        $script:perturbationCount++
    } finally {
        Copy-Item -LiteralPath $sourcePath -Destination $path -Force
    }
}

try {
    New-Item -ItemType Directory -Path $archiveRoot -Force | Out-Null
    git -C $RepositoryRoot archive --format=tar --output=$archive HEAD
    if ($LASTEXITCODE -ne 0) {
        throw 'Unable to archive HEAD.'
    }
    tar -xf $archive -C $archiveRoot
    if ($LASTEXITCODE -ne 0) {
        throw 'Unable to extract HEAD.'
    }

    $verifier = Join-Path $archiveRoot 'docs\design\lxr-port\P1.3\verify-store-paths.ps1'
    & pwsh -NoProfile -File $verifier -RepositoryRoot $archiveRoot
    if ($LASTEXITCODE -ne 0) {
        throw 'Verifier rejected the exact committed archive.'
    }
    New-Item -ItemType Directory -Path $perturbedRoot -Force | Out-Null
    tar -xf $archive -C $perturbedRoot
    if ($LASTEXITCODE -ne 0) {
        throw 'Unable to extract the perturbation tree.'
    }

    Invoke-Perturbation `
        'src\coreclr\gc\gcinterface.h' `
        'GC_WRITE_BARRIER_BULK_SCAN_INTERFACE_MINOR_VERSION 12' `
        'GC_WRITE_BARRIER_BULK_SCAN_INTERFACE_MINOR_VERSION 11' `
        'bulk interface version'
    Invoke-Perturbation `
        'src\coreclr\vm\gchelpers.cpp' `
        'workBits = ~workBits;' `
        'workBits = workBits;' `
        'clear-bit polarity'
    Invoke-Perturbation `
        'src\libraries\System.Private.CoreLib\src\System\SpanHelpers.T.cs' `
        'Buffer.BulkFillWithOldValueWriteBarrier(' `
        'Buffer.BulkMoveWithOldValueWriteBarrier(' `
        'Span fill funnel'
    Invoke-Perturbation `
        'src\coreclr\jit\lower.cpp' `
        'CORINFO_HELP_BULK_WRITEBARRIER_CLEAR_WITH_LAYOUT,' `
        'CORINFO_HELP_BROKEN_LAYOUT_CLEAR,' `
        'JIT layout clear helper'
    Invoke-Perturbation `
        'docs\design\lxr-port\P1.3\raw\benchmark-summary.csv' `
        ',1.8734,ratio-of-means,0.1701,,,1.8734,' `
        ',9.9999,ratio-of-means,0.1701,,,1.8734,' `
        'published benchmark ratio'
    Invoke-Perturbation `
        'src\coreclr\jit\lower.cpp' `
        'layout->GetSize() > MaxUnrolledLayoutBytes' `
        'layout->GetSize() < MaxUnrolledLayoutBytes' `
        'large-layout JIT bound'
    Invoke-Perturbation `
        'src\coreclr\jit\lower.cpp' `
        '!layout->IsValueClass() ||' `
        'false ||' `
        'structural value-class helper eligibility contract'
    Invoke-Perturbation `
        'src\coreclr\vm\gchelpers.cpp' `
        '_ASSERTE(!type->IsArray());' `
        '_ASSERTE(type->IsArray());' `
        'shared chunk descriptor contract'
    Invoke-Perturbation `
        'src\coreclr\vm\gchelpers.cpp' `
        'lowestOffsetSeries[-middle]' `
        'lowestOffsetSeries[middle]' `
        'chunk descriptor search direction'
    Invoke-Perturbation `
        'src\coreclr\jit\layout.cpp' `
        'm_gcLayoutOffset + offset' `
        'm_gcLayoutOffset + 0xDEAD' `
        'custom-slice descriptor offset'
    Invoke-Perturbation `
        'docs\design\lxr-port\P1.3\raw\bulk-throughput-invocations.csv' `
        '"2689546.041091"' `
        '"1.000000"' `
        'paired throughput raw row'
    Invoke-Perturbation `
        'docs\design\lxr-port\P1.3\raw\bulk-throughput-invocations.csv' `
        '"2689546.041091","0","0","0","true","true","true","true","true"' `
        '"2689546.041091","0","0","0","false","true","true","true","true"' `
        'observed ServerGC arm'
    Invoke-Perturbation `
        'docs\design\lxr-port\P1.3\raw\bulk-throughput-invocations.csv' `
        '"2689546.041091","0","0","0","true","true","true","true","true"' `
        '"2689546.041091","0","0","0","true","true","false","true","true"' `
        'collector confirmation'
    Invoke-Perturbation `
        'docs\design\lxr-port\P1.3\raw\bulk-throughput-invocations.csv' `
        '"2689546.041091","0","0","0","true","true","true","true","true","0","0","0","launcher environment; not independently confirmed by the worker","87fd9956a9d22e870e2f3173fcdc4cf0019990fe2b63243d7da4bd532646b685"' `
        '"2689546.041091","0","0","0","true","true","true","true","true","0","0","0","launcher environment; not independently confirmed by the worker","87fd9956a9d22e870e2f3173fcdc4cf0019990fe2b63243d7da4bd532646b684"' `
        'runtime binary identity'
    Invoke-Perturbation `
        'docs\design\lxr-port\P1.3\raw\stock-fill-codegen.csv' `
        '751FCE9133704DEC292AA226D333A0297F0E3E94B4C229B26F086B61B771AA3A,true,CRLF' `
        '751FCE9133704DEC292AA226D333A0297F0E3E94B4C229B26F086B61B771AA3B,true,CRLF' `
        'stock Fill identity hash'
    Invoke-Perturbation `
        'docs\design\lxr-port\P1.3\raw\layout-helper-codegen.csv' `
        ',bounded-helper,49,1,PASS,' `
        ',bounded-helper,49000,1,PASS,' `
        'bounded large-layout code size'
    Invoke-Perturbation `
        'docs\design\lxr-port\P1.3\raw\reference-class-codegen.csv' `
        'execution-codegen-not-eligibility' `
        'behavioral-gate-proof' `
        'stack evidence scope'
    Invoke-Perturbation `
        'docs\design\lxr-port\P1.3\raw\reference-class-codegen.csv' `
        'linux-x64-debug,Debug,D0E6886EAB3C637EB34129350C5668D2F4FA3F8C108BF2413AE3A22A9B5E8991,1024,Object.MemberwiseClone' `
        'linux-x64-debug,Debug,D0E6886EAB3C637EB34129350C5668D2F4FA3F8C108BF2413AE3A22A9B5E8991,2048,Object.MemberwiseClone' `
        'MemberwiseClone Debug chunk size'
    Invoke-Perturbation `
        'docs\design\lxr-port\P1.3\raw\reference-class-codegen.csv' `
        'windows-x64-checked,Checked,5462DB2F984C4FAC66843F0A30729DD18DF3A9BBBE175C1A005005CC084F0E1C,1024,Object.MemberwiseClone' `
        'windows-x64-checked,Checked,5462DB2F984C4FAC66843F0A30729DD18DF3A9BBBE175C1A005005CC084F0E1C,2048,Object.MemberwiseClone' `
        'MemberwiseClone Checked chunk size'
    Invoke-Perturbation `
        'docs\design\lxr-port\P1.3-store-path-coverage-and-bulk-barrier.md' `
        'code-size comparison: 106,418 to 49 bytes for' `
        'code-size comparison: 106,418 to 48 bytes for' `
        'published final helper code size'

    & pwsh -NoProfile -File $verifier -RepositoryRoot $archiveRoot
    if ($LASTEXITCODE -ne 0) {
        throw 'Verifier did not pass again on the untouched archive.'
    }

    Write-Host "RESULT: PASS (archive pass, $perturbationCount exact perturbations fail, archive re-pass)"
} finally {
    if (Test-Path -LiteralPath $temporaryRoot) {
        Remove-Item -LiteralPath $temporaryRoot -Recurse -Force
    }
}
