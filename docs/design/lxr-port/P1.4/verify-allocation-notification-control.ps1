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

$temporaryRoot = Join-Path ([IO.Path]::GetTempPath()) (
    "p14-allocation-notification-$([Guid]::NewGuid())")
$archive = Join-Path $temporaryRoot 'tip.tar'
$archiveRoot = Join-Path $temporaryRoot 'original'
$perturbedRoot = Join-Path $temporaryRoot 'perturbed'
$perturbationCount = 0
$defaultAllocationNotificationImplementation = @'
    virtual AllocationNotificationParameters* GetAllocationNotificationParameters()
    {
        static AllocationNotificationParameters parameters = {};
        return &parameters;
    }
'@
$pureVirtualAllocationNotificationImplementation = @'
    virtual AllocationNotificationParameters* GetAllocationNotificationParameters() PURE_VIRTUAL;
'@

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

        $verifier = Join-Path $perturbedRoot (
            'docs\design\lxr-port\P1.4\verify-allocation-notification.ps1')
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

    $verifier = Join-Path $archiveRoot (
        'docs\design\lxr-port\P1.4\verify-allocation-notification.ps1')
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
        'GC_ALLOCATION_NOTIFICATION_INTERFACE_MINOR_VERSION 13' `
        'GC_ALLOCATION_NOTIFICATION_INTERFACE_MINOR_VERSION 12' `
        'allocation notification interface version'
    Invoke-Perturbation `
        'src\coreclr\runtime\amd64\AllocFastNotification.asm' `
        'call        RhpAllocationComplete' `
        'call        RhpNewObject' `
        'Windows fast-success notification'
    Invoke-Perturbation `
        'src\coreclr\runtime\amd64\AllocFastNotification.S' `
        'call        C_FUNC(RhpAllocationComplete)' `
        'call        C_FUNC(RhpNewObject)' `
        'System V fast-success notification'
    Invoke-Perturbation `
        'src\coreclr\vm\runtimehandles.cpp' `
        'RhpAllocationComplete(obj, size);' `
        'UNREFERENCED_PARAMETER(obj);' `
        'interpreter and runtime helper notification'
    Invoke-Perturbation `
        'src\coreclr\vm\gcheaputilities.cpp' `
        'if (g_pConfig->ReadyToRun())' `
        'if (false)' `
        'ReadyToRun fail-closed gate'
    Invoke-Perturbation `
        'src\coreclr\gc\gcinterface.h' `
        $defaultAllocationNotificationImplementation `
        $pureVirtualAllocationNotificationImplementation `
        'old-source-compatible inherited descriptor'
    Invoke-Perturbation `
        'src\coreclr\gc\interface.cpp' `
        'if (allocationNotification == nullptr)' `
        'if (false)' `
        'built-in null descriptor rejection'
    Invoke-Perturbation `
        'src\coreclr\gc\allocationnotificationtest.cpp' `
        'Interlocked::Increment(&g_countOnly);' `
        'UNREFERENCED_PARAMETER(g_countOnly);' `
        'observable counting callback'
    Invoke-Perturbation `
        'docs\design\lxr-port\P1.4\run-allocation-benchmark.ps1' `
        '$observations.Count -ne $expectedObservations' `
        '$observations.Count -gt $expectedObservations' `
        'counting callback observation cardinality'
    Invoke-Perturbation `
        'docs\design\lxr-port\P1.4\run-allocation-benchmark.ps1' `
        'if ($expectedBenchmarks -le 0)' `
        'if ($expectedBenchmarks -lt 0)' `
        'nonzero benchmark cardinality'
    Invoke-Perturbation `
        'src\tests\profiler\native\gcallocateprofiler\gcallocateprofiler.cpp' `
        '(actualAllocationFlags & expectedAllocationFlag) == 0' `
        '(actualAllocationFlags & 0u) == 0' `
        'profiler placement flag assertion'

    & pwsh -NoProfile -File $verifier -RepositoryRoot $archiveRoot
    if ($LASTEXITCODE -ne 0) {
        throw 'Verifier did not pass again on the untouched archive.'
    }

    Write-Host (
        "RESULT: PASS (archive pass, $perturbationCount exact perturbations fail, archive re-pass)")
} finally {
    if (Test-Path -LiteralPath $temporaryRoot) {
        Remove-Item -LiteralPath $temporaryRoot -Recurse -Force
    }
}
