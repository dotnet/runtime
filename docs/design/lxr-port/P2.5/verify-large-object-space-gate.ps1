# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

[CmdletBinding()]
param(
    [string]$RepositoryRoot,
    [string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'
$scriptRoot = $PSScriptRoot
if (-not $RepositoryRoot) {
    $RepositoryRoot = (Resolve-Path (Join-Path $scriptRoot '..\..\..\..')).ProviderPath
} else {
    $RepositoryRoot = (Resolve-Path -LiteralPath $RepositoryRoot).ProviderPath
}
if (-not $OutputDirectory) {
    $OutputDirectory = Join-Path $env:SystemDrive "p25-$([guid]::NewGuid().ToString('N').Substring(0,8))"
}
if (Test-Path $OutputDirectory) {
    throw "Gate output already exists: $OutputDirectory"
}

$status = @(git -C $RepositoryRoot -c core.longpaths=true status --porcelain=v1)
if ($status.Count -ne 0) {
    throw 'The P2.5 gate requires a clean worktree.'
}

$raw = Join-Path $RepositoryRoot 'docs\design\lxr-port\P2.5\raw'
$sourceCommit = (Get-Content (Join-Path $raw 'source-commit.txt') -Raw).Trim()
$head = (git -C $RepositoryRoot rev-parse HEAD).Trim()
$parent = (git -C $RepositoryRoot rev-parse HEAD^).Trim()
if ($parent -ne $sourceCommit) {
    throw 'Evidence HEAD is not the direct child of the recorded source commit.'
}
$evidenceChanges = @(git -C $RepositoryRoot diff --name-status --no-renames $sourceCommit $head)
$invalidEvidenceChanges = [Collections.Generic.List[string]]::new()
foreach ($change in $evidenceChanges) {
    $parts = $change -split "`t"
    if (($parts.Count -ne 2) -or
        ($parts[0] -ne 'A') -or
        ($parts[1] -notlike 'docs/design/lxr-port/P2.5/raw/*')) {
        $invalidEvidenceChanges.Add($change)
    }
}
if ($invalidEvidenceChanges.Count -ne 0) {
    throw "Evidence commit contains invalid changes: $($invalidEvidenceChanges -join ', ')"
}

New-Item -ItemType Directory -Path $OutputDirectory | Out-Null
$archive = Join-Path $OutputDirectory 'tree.tar'
$clean = Join-Path $OutputDirectory 'clean'
New-Item -ItemType Directory -Path $clean | Out-Null
git -C $RepositoryRoot `
    -c core.autocrlf=false `
    -c core.eol=lf `
    archive --format=tar --output=$archive HEAD
if ($LASTEXITCODE -ne 0) {
    throw 'Unable to create committed archive.'
}
Add-Type -AssemblyName System.Formats.Tar
[System.Formats.Tar.TarFile]::ExtractToDirectory($archive, $clean, $false)

$summary = [Collections.Generic.List[object]]::new()
try {
    & (Join-Path $clean 'docs\design\lxr-port\P2.5\verify-large-object-space.ps1') `
        -RepositoryRoot $clean
    $summary.Add([pscustomobject]@{ id = 'clean-archive'; result = 'PASS' })

    $perturbed = Join-Path $OutputDirectory 'threshold-perturbed'
    Copy-Item $clean $perturbed -Recurse
    $implementation = Join-Path $perturbed 'src\coreclr\gc\large_object_space.cpp'
    $text = Get-Content $implementation -Raw
    $old = 'return objectBytes >= MinimumObjectBytes;'
    if ([regex]::Matches($text, [regex]::Escape($old)).Count -ne 1) {
        throw 'Threshold perturbation token is not unique.'
    }
    [IO.File]::WriteAllText(
        $implementation,
        $text.Replace($old, 'return objectBytes > MinimumObjectBytes;'),
        [Text.UTF8Encoding]::new($false))
    $validationOutput = Join-Path $OutputDirectory 'threshold-validation'
    $failed = $false
    try {
        & (Join-Path $perturbed 'docs\design\lxr-port\P2.5\run-large-object-space-validation.ps1') `
            -RepositoryRoot $perturbed `
            -OutputDirectory $validationOutput | Out-Null
    } catch {
        $failed = $true
    }
    $log = Join-Path $validationOutput 'x64\run.log'
    if (-not $failed -or
        -not (Test-Path $log) -or
        -not ((Get-Content $log -Raw).Contains('FAIL: threshold exact'))) {
        throw 'Inclusive-threshold perturbation did not fail for the intended property.'
    }
    $summary.Add([pscustomobject]@{ id = 'threshold-inclusive'; result = 'PASS' })

    $promotionTree = Join-Path $OutputDirectory 'promotion-perturbed'
    Copy-Item $clean $promotionTree -Recurse
    $promotionImplementation = Join-Path $promotionTree 'src\coreclr\gc\large_object_space.cpp'
    $promotionText = Get-Content $promotionImplementation -Raw
    $promotionPattern = 'span->object,\r?\n\s*fieldMetadataBytes,'
    if ([regex]::Matches($promotionText, $promotionPattern).Count -ne 1) {
        throw 'Promotion-rounding perturbation token is not unique.'
    }
    [IO.File]::WriteAllText(
        $promotionImplementation,
        [regex]::Replace(
            $promotionText,
            $promotionPattern,
            "span->object,`n        span->object_bytes,",
            1),
        [Text.UTF8Encoding]::new($false))
    $promotionOutput = Join-Path $OutputDirectory 'promotion-validation'
    $failed = $false
    try {
        & (Join-Path $promotionTree 'docs\design\lxr-port\P2.5\run-large-object-space-validation.ps1') `
            -RepositoryRoot $promotionTree `
            -OutputDirectory $promotionOutput | Out-Null
    } catch {
        $failed = $true
    }
    $promotionLog = Join-Path $promotionOutput 'x64\run.log'
    if (-not $failed -or
        -not (Test-Path $promotionLog) -or
        -not ((Get-Content $promotionLog -Raw).Contains('FAIL: threshold plus one promote'))) {
        throw 'Promotion-rounding perturbation did not fail for the intended property.'
    }
    $summary.Add([pscustomobject]@{ id = 'promotion-rounding'; result = 'PASS' })

    $identityTree = Join-Path $OutputDirectory 'identity-perturbed'
    Copy-Item $clean $identityTree -Recurse
    Add-Content (Join-Path $identityTree 'src\coreclr\gc\large_object_space.h') "`n"
    $failed = $false
    try {
        & (Join-Path $identityTree 'docs\design\lxr-port\P2.5\verify-large-object-space.ps1') `
            -RepositoryRoot $identityTree | Out-Null
    } catch {
        $failed = $_.Exception.Message -like '*Source identity differs*'
    }
    if (-not $failed) {
        throw 'Source-identity perturbation did not fail.'
    }
    $summary.Add([pscustomobject]@{ id = 'source-identity'; result = 'PASS' })

    $evidenceTree = Join-Path $OutputDirectory 'evidence-perturbed'
    Copy-Item $clean $evidenceTree -Recurse
    $validationCsv = Join-Path $evidenceTree 'docs\design\lxr-port\P2.5\raw\validation-summary.csv'
    $rows = @(Import-Csv $validationCsv)
    $rows | Select-Object -Skip 1 | Export-Csv $validationCsv -NoTypeInformation
    $failed = $false
    try {
        & (Join-Path $evidenceTree 'docs\design\lxr-port\P2.5\verify-large-object-space.ps1') `
            -RepositoryRoot $evidenceTree | Out-Null
    } catch {
        $failed = $_.Exception.Message -like '*Validation rows differ*'
    }
    if (-not $failed) {
        throw 'Evidence-cardinality perturbation did not fail.'
    }
    $summary.Add([pscustomobject]@{ id = 'evidence-cardinality'; result = 'PASS' })

    $benchmarkTree = Join-Path $OutputDirectory 'benchmark-perturbed'
    Copy-Item $clean $benchmarkTree -Recurse
    $controlsCsv = Join-Path $benchmarkTree 'docs\design\lxr-port\P2.5\raw\benchmark-controls.csv'
    $controls = @(Import-Csv $controlsCsv)
    $controls[0].aa_ratio = '2'
    $controls | Export-Csv $controlsCsv -NoTypeInformation
    $failed = $false
    try {
        & (Join-Path $benchmarkTree 'docs\design\lxr-port\P2.5\verify-large-object-space.ps1') `
            -RepositoryRoot $benchmarkTree | Out-Null
    } catch {
        $failed = $_.Exception.Message -like '*A/A above bound*'
    }
    if (-not $failed) {
        throw 'Benchmark-control perturbation did not fail.'
    }
    $summary.Add([pscustomobject]@{ id = 'benchmark-control'; result = 'PASS' })

    $buildTree = Join-Path $OutputDirectory 'build-perturbed'
    Copy-Item $clean $buildTree -Recurse
    $buildCsv = Join-Path $buildTree 'docs\design\lxr-port\P2.5\raw\build-summary.csv'
    $buildRows = @(Import-Csv $buildCsv)
    $buildRows | Select-Object -Skip 1 | Export-Csv $buildCsv -NoTypeInformation
    $failed = $false
    try {
        & (Join-Path $buildTree 'docs\design\lxr-port\P2.5\verify-large-object-space.ps1') `
            -RepositoryRoot $buildTree | Out-Null
    } catch {
        $failed = $_.Exception.Message -like '*Build evidence set differs*'
    }
    if (-not $failed) {
        throw 'Build-cardinality perturbation did not fail.'
    }
    $summary.Add([pscustomobject]@{ id = 'build-cardinality'; result = 'PASS' })

    $manifest = Get-Content (Join-Path $clean 'docs\design\lxr-port\P2.5\evidence-manifest.json') -Raw |
        ConvertFrom-Json
    if ($summary.Count -ne (1 + $manifest.perturbations.Count)) {
        throw "Gate control count differs: $($summary.Count)/$(1 + $manifest.perturbations.Count)."
    }

} finally {
    if ((Test-Path $OutputDirectory) -and
        ([IO.Path]::GetFullPath($OutputDirectory) -like "$($env:SystemDrive)\p25-*")) {
        Remove-Item -LiteralPath $OutputDirectory -Recurse -Force
    }
}

Write-Host "PASS: $($summary.Count) P2.5 archive gate controls"
