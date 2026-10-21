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
    Join-Path $RepositoryRoot 'src\coreclr\gc\lxr_work_scheduler.h') -Raw
$source = Get-Content -LiteralPath (
    Join-Path $RepositoryRoot 'src\coreclr\gc\lxr_work_scheduler.cpp') -Raw
$packetHeader = Get-Content -LiteralPath (
    Join-Path $RepositoryRoot 'src\coreclr\gc\lxr_work_packet.h') -Raw

Assert-Condition (
    $spec.declaredOracle.binding -ceq '0682434ae725787e38defbc17bd66dc918e4bdb7' -and
    $spec.declaredOracle.core -ceq '304ce69d43aae87de501111fceb8cbd33173a03a') (
    'Declared oracle differs from the binding-pinned HEAD pair.')
Assert-Condition ([int]$spec.queueSetCount -eq 2) 'Deferred queue-set count differs.'
Assert-Condition (
    $source.Contains('std::array<DeferredQueueSet, 2> deferredQueues;')) (
    'Scheduler does not contain exactly two deferred queue sets.')
foreach ($surface in @(
    'SubmitDeferredConcurrent',
    'PublishDeferredConcurrentWork',
    'GetDeferredWorkState')) {
    Assert-Condition ($header.Contains($surface)) "Scheduler surface is missing '$surface'."
}
Assert-Condition (
    $source.Contains('activeDeferredIndex = publishedIndex;') -and
    $source.Contains('sealedDeferredIndex = producerDeferredIndex;') -and
    $source.Contains('producerDeferredIndex = nextProducerIndex;')) (
    'Deferred queue direction or publication differs.')
Assert-Condition (
    $source.Contains('m_impl->resumeRecordedGeneration != expectedGeneration') -and
    $source.Contains('m_impl->sealedGeneration != expectedGeneration')) (
    'Publication is not bound to resume and generation.')
Assert-Condition (
    $source.Contains('active.prioritized') -and $source.Contains('active.normal')) (
    'Prioritized/normal active polling differs.')
Assert-Condition (
    $packetHeader.Contains('LxrRCProcessDecs = LxrWorkBucketStage::STWRCDecsAndSweep')) (
    'Eager decrement placement differs.')

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
        $row.result -eq 'PASS' -and
        $row.passed -eq $row.total -and
        [int]$row.total -gt 0) (
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

$builds = @(Import-Csv (Join-Path $raw 'build-summary.csv'))
Assert-Condition (
    (($builds.id | Sort-Object) -join '|') -ceq (
        (@($manifest.buildRows) | Sort-Object) -join '|')) (
    'Build evidence is incomplete.')
Assert-Condition (@($builds | Where-Object result -ne 'PASS').Count -eq 0) (
    'A required build row failed.')

$crossTargets = @(Import-Csv (Join-Path $raw 'cross-target-summary.csv'))
Assert-Condition (
    (($crossTargets.platform | Sort-Object) -join '|') -ceq (
        (@($manifest.crossTargets) | Sort-Object) -join '|')) (
    'Cross-target evidence is incomplete.')
Assert-Condition (@($crossTargets | Where-Object result -ne 'PASS').Count -eq 0) (
    'A cross-target compile row failed.')

$review = @(Import-Csv (Join-Path $raw 'review-summary.csv'))
Assert-Condition (
    $review.Count -eq 1 -and
    $review[0].result -eq 'PASS' -and
    [int]$review[0].errors -eq 0 -and
    [int]$review[0].warnings -eq 0) (
    'Code-review evidence is incomplete or reports findings.')

$benchmarkRaw = @(Import-Csv (Join-Path $raw 'benchmark-raw.csv'))
$policyNames = @($manifest.benchmark.policies.psobject.Properties.Name)
$expectedBenchmarkRows =
    $manifest.benchmark.workers.Count *
    $policyNames.Count *
    [int]$manifest.benchmark.launches
Assert-Condition ($benchmarkRaw.Count -eq $expectedBenchmarkRows) (
    "Benchmark raw row count differs: $($benchmarkRaw.Count)/$expectedBenchmarkRows.")
Assert-Condition (@($benchmarkRaw | Where-Object result -ne 'PASS').Count -eq 0) (
    'A benchmark raw row failed.')
foreach ($policy in $manifest.benchmark.policies.psobject.Properties) {
    $rows = @($benchmarkRaw | Where-Object policy -eq $policy.Name)
    Assert-Condition (
        $rows.Count -eq (
            $manifest.benchmark.workers.Count * [int]$manifest.benchmark.launches)) (
        "Benchmark policy cardinality differs: $($policy.Name)")
    Assert-Condition (
        @($rows | Where-Object {
            [int]$_.callback_passes -ne [int]$policy.Value
        }).Count -eq 0) (
        "Benchmark null/injected policy differs: $($policy.Name)")
}
Assert-Condition (
    @($benchmarkRaw | Where-Object {
        [uint64]$_.callbacks -eq 0 -or
        [uint64]$_.callbacks -ne [uint64]$_.executed -or
        [uint64]$_.ownership_violations -ne 0
    }).Count -eq 0) (
    'Benchmark callback/execution ownership evidence differs.')

$benchmarkSummary = @(Import-Csv (Join-Path $raw 'benchmark-summary.csv'))
Assert-Condition (
    $benchmarkSummary.Count -eq (
        $manifest.benchmark.workers.Count * $policyNames.Count)) (
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
        [double]$control.eager_injected_ratio -ge
            [double]$manifest.benchmark.eagerInjectedMinimum) (
        'Benchmark injected-cost ratio is below its bound.')
    Assert-Condition (
        [double]$control.injected_separation_ratio -ge
            [double]$manifest.benchmark.injectedSeparationMinimum) (
        'Benchmark injected-cost separation is below its bound.')
    Assert-Condition ($control.result -eq 'PASS') (
        'A benchmark control row failed.')
}

$invocation = @(Import-Csv (Join-Path $raw 'benchmark-invocation.csv'))
Assert-Condition (
    $invocation.Count -eq 1 -and
    [int]$invocation[0].launch_count -eq [int]$manifest.benchmark.launches -and
    [int]$invocation[0].packet_count -eq [int]$manifest.benchmark.packetCount -and
    [int]$invocation[0].reference_capacity -eq
        [int]$manifest.benchmark.referenceCapacity -and
    [int]$invocation[0].warmup_cycles -eq [int]$manifest.benchmark.warmupCycles -and
    [int]$invocation[0].measured_cycles -eq [int]$manifest.benchmark.measuredCycles -and
    $invocation[0].result -eq 'PASS') (
    'Benchmark invocation differs from the structured policy.')

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

Write-Host "PASS: $checks P3.3 evidence checks"
