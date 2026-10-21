# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

[CmdletBinding()]
param(
    [string]$RepositoryRoot
)

$ErrorActionPreference = 'Stop'
$scriptRoot = $PSScriptRoot
if (-not $RepositoryRoot) {
    $RepositoryRoot = (Resolve-Path (Join-Path $scriptRoot '..\..\..\..')).ProviderPath
} else {
    $RepositoryRoot = (Resolve-Path -LiteralPath $RepositoryRoot).ProviderPath
}

$checks = 0
function Assert-Condition([bool]$Condition, [string]$Message) {
    $script:checks++
    if (-not $Condition) {
        throw $Message
    }
}

$specPath = Join-Path $scriptRoot 'mechanism-spec.json'
$manifestPath = Join-Path $scriptRoot 'evidence-manifest.json'
$sourceManifestPath = Join-Path $scriptRoot 'source-manifest.txt'
$raw = Join-Path $scriptRoot 'raw'
foreach ($path in @($specPath, $manifestPath, $sourceManifestPath, $raw)) {
    Assert-Condition (Test-Path -LiteralPath $path) "Required evidence input is missing: $path"
}

$spec = Get-Content -LiteralPath $specPath -Raw | ConvertFrom-Json
$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
$header = Get-Content -LiteralPath (
    Join-Path $RepositoryRoot 'src\coreclr\gc\lxr_work_packet.h') -Raw
$packetSource = Get-Content -LiteralPath (
    Join-Path $RepositoryRoot 'src\coreclr\gc\lxr_work_packet.cpp') -Raw

$enumMatch = [regex]::Match(
    $header,
    'enum class LxrWorkBucketStage.*?\{(?<body>.*?)\};',
    [Text.RegularExpressions.RegexOptions]::Singleline)
Assert-Condition $enumMatch.Success 'LxrWorkBucketStage enum was not found.'
$enumNames = @(
    [regex]::Matches($enumMatch.Groups['body'].Value, '(?m)^\s*(?<name>[A-Za-z0-9_]+),\s*$') |
        ForEach-Object { $_.Groups['name'].Value } |
        Where-Object { $_ -ne 'Count' }
)
$expectedStages = @($spec.stages | Sort-Object ordinal | ForEach-Object name)
Assert-Condition (
    ($enumNames -join '|') -ceq ($expectedStages -join '|')) (
    "Stage enum order differs: '$($enumNames -join ',')'.")
foreach ($stage in $spec.stages) {
    Assert-Condition (
        $packetSource.Contains('"' + $stage.name + '"')) (
        "Stage name table is missing '$($stage.name)'.")
}
Assert-Condition (
    $header.Contains('LxrRCProcessIncs = LxrWorkBucketStage::Initial')) (
    'RC increment placement differs.')
Assert-Condition (
    $header.Contains('LxrRCProcessDecs = LxrWorkBucketStage::STWRCDecsAndSweep')) (
    'RC decrement placement differs.')
Assert-Condition (
    $header.Contains('LxrFirstStwStage = LxrWorkBucketStage::FinishConcurrentWork')) (
    'First STW placement differs.')

$sourcePaths = @(
    Get-Content -LiteralPath $sourceManifestPath |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
)
Assert-Condition ($sourcePaths.Count -eq ($sourcePaths | Sort-Object -Unique).Count) (
    'Source manifest contains duplicate paths.')
foreach ($path in $sourcePaths) {
    Assert-Condition (
        Test-Path -LiteralPath (Join-Path $RepositoryRoot $path) -PathType Leaf) (
        "Source manifest path is missing: $path")
}

$identityPath = Join-Path $raw 'source-identities.csv'
Assert-Condition (Test-Path -LiteralPath $identityPath -PathType Leaf) (
    'Source identity evidence is missing.')
$identities = @(Import-Csv $identityPath)
Assert-Condition (
    (($identities.path | Sort-Object) -join '|') -ceq (($sourcePaths | Sort-Object) -join '|')) (
    'Source identity path set mismatch.')
foreach ($identity in $identities) {
    $actual = (Get-FileHash -Algorithm SHA256 (
        Join-Path $RepositoryRoot $identity.path)).Hash.ToLowerInvariant()
    Assert-Condition ($actual -ceq $identity.sha256.ToLowerInvariant()) (
        "Source identity mismatch: $($identity.path)")
}

$validation = @(Import-Csv (Join-Path $raw 'validation-summary.csv'))
$expectedPlatforms = @($manifest.validationPlatforms | Sort-Object)
Assert-Condition (
    (($validation.platform | Sort-Object) -join '|') -ceq ($expectedPlatforms -join '|')) (
    'Validation platform evidence is incomplete.')
foreach ($row in $validation) {
    Assert-Condition (
        $row.result -eq 'PASS' -and $row.passed -eq $row.total -and [int]$row.total -gt 0) (
        "Validation row failed: $($row.platform)")
}

$runtime = @(Import-Csv (Join-Path $raw 'runtime-smoke-summary.csv'))
$runtimeExpected = foreach ($platform in $manifest.runtimeSmoke.platforms) {
    foreach ($linkage in $manifest.runtimeSmoke.linkages) {
        foreach ($mode in $manifest.runtimeSmoke.gcModes) {
            "$platform|$linkage|$mode"
        }
    }
}
$runtimeActual = @($runtime | ForEach-Object {
    "$($_.platform)|$($_.linkage)|$($_.gc_mode)"
})
Assert-Condition (
    (($runtimeActual | Sort-Object) -join '|') -ceq (($runtimeExpected | Sort-Object) -join '|')) (
    'Runtime smoke evidence is incomplete.')
Assert-Condition (@($runtime | Where-Object result -ne 'PASS').Count -eq 0) (
    'A runtime smoke row failed.')

$benchmarkRaw = @(Import-Csv (Join-Path $raw 'benchmark-raw.csv'))
$expectedBenchmarkRows =
    $manifest.benchmark.workers.Count *
    $manifest.benchmark.policies.Count *
    [int]$manifest.benchmark.launches
Assert-Condition ($benchmarkRaw.Count -eq $expectedBenchmarkRows) (
    "Benchmark raw row count differs: $($benchmarkRaw.Count)/$expectedBenchmarkRows.")
Assert-Condition (@($benchmarkRaw | Where-Object result -ne 'PASS').Count -eq 0) (
    'A benchmark raw row failed.')

$benchmarkSummary = @(Import-Csv (Join-Path $raw 'benchmark-summary.csv'))
Assert-Condition (
    $benchmarkSummary.Count -eq (
        $manifest.benchmark.workers.Count * $manifest.benchmark.policies.Count)) (
    'Benchmark summary cardinality differs.')
Assert-Condition (@($benchmarkSummary | Where-Object result -ne 'PASS').Count -eq 0) (
    'A benchmark summary row failed.')

$controls = @(Import-Csv (Join-Path $raw 'benchmark-controls.csv'))
Assert-Condition ($controls.Count -eq $manifest.benchmark.workers.Count) (
    'Benchmark control cardinality differs.')
foreach ($control in $controls) {
    Assert-Condition (
        [double]$control.aa_ratio -ge [double]$manifest.benchmark.aaLower -and
        [double]$control.aa_ratio -le [double]$manifest.benchmark.aaUpper) (
        'Benchmark A/A ratio is outside its bounds.')
    Assert-Condition (
        [double]$control.injected_cost_ratio -ge
            [double]$manifest.benchmark.injectedCostMinimum) (
        'Benchmark injected-cost ratio is below its bound.')
    Assert-Condition ($control.result -eq 'PASS') (
        'A benchmark control row failed.')
}

$sourceCommit = (Get-Content -LiteralPath (
    Join-Path $raw 'source-commit.txt') -Raw).Trim()
Assert-Condition ($sourceCommit -match '^[0-9a-f]{40}$') (
    'Source commit identity is malformed.')
if (Test-Path -LiteralPath (Join-Path $RepositoryRoot '.git')) {
    git -C $RepositoryRoot cat-file -e "$sourceCommit`^{commit}"
    Assert-Condition ($LASTEXITCODE -eq 0) 'Source commit does not exist.'
}

$shellScripts = Get-ChildItem -LiteralPath $scriptRoot -Filter '*.sh'
foreach ($script in $shellScripts) {
    $bytes = [IO.File]::ReadAllBytes($script.FullName)
    $hasCrLf = $false
    for ($index = 1; $index -lt $bytes.Length; $index++) {
        if ($bytes[$index - 1] -eq 13 -and $bytes[$index] -eq 10) {
            $hasCrLf = $true
            break
        }
    }
    Assert-Condition (-not $hasCrLf) "Shell script is not LF-only: $($script.Name)"
}

Write-Host "PASS: $checks P3.1 evidence checks"
