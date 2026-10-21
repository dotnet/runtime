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
$counterHeader = Get-Content -LiteralPath (
    Join-Path $RepositoryRoot 'src\coreclr\gc\lxr_completion_counter.h') -Raw
$counterSource = Get-Content -LiteralPath (
    Join-Path $RepositoryRoot 'src\coreclr\gc\lxr_completion_counter.cpp') -Raw
$schedulerSource = Get-Content -LiteralPath (
    Join-Path $RepositoryRoot 'src\coreclr\gc\lxr_work_scheduler.cpp') -Raw
$packetHeader = Get-Content -LiteralPath (
    Join-Path $RepositoryRoot 'src\coreclr\gc\lxr_work_packet.h') -Raw

Assert-Condition (
    $spec.declaredOracle.binding -ceq '0682434ae725787e38defbc17bd66dc918e4bdb7' -and
    $spec.declaredOracle.core -ceq '304ce69d43aae87de501111fceb8cbd33173a03a') (
    'Declared oracle differs from the binding-pinned HEAD pair.')
Assert-Condition ($spec.stateStorage -ceq 'embedded') (
    'Completion generation storage is not embedded.')
Assert-Condition (-not (
    $counterHeader.Contains('shared_ptr') -or
    $counterSource.Contains('shared_ptr') -or
    $schedulerSource.Contains('shared_ptr'))) (
    'Completion implementation contains shared_ptr.')
Assert-Condition (
    $schedulerSource.Contains('LxrLazyCompletionGenerationState completion;') -and
    $schedulerSource.Contains('std::array<DeferredQueueSet, 2> deferredQueues;')) (
    'Two embedded queue-set completion states are missing.')
Assert-Condition (
    $counterSource.Contains('std::memory_order_seq_cst') -and
    -not $counterSource.Contains('memory_order_relaxed')) (
    'Counter operations are not exclusively sequentially consistent.')
Assert-Condition (
    $counterSource.Contains('m_decrement_zero_transitions.fetch_add(1') -and
    $counterSource.Contains('m_overall_zero_transitions.fetch_add(1')) (
    'Exactly-once zero-transition accounting is missing.')
Assert-Condition (
    $packetHeader.Contains('#ifdef LXR_ENABLE_LAZY_COMPLETION_COUNTER') -and
    $schedulerSource.Contains('guard.unlock();') -and
    $schedulerSource.Contains('packet->CompleteRegistration();')) (
    'Default-off packet gating or callback-outside-lock completion is missing.')

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

$identities = @(Import-Csv (Join-Path $raw 'source-identities.csv'))
Assert-Condition (
    (($identities.path | Sort-Object) -join '|') -ceq (($sourcePaths | Sort-Object) -join '|')) (
    'Source identity path set mismatch.')
foreach ($identity in $identities) {
    $actual = (Get-FileHash -Algorithm SHA256 (
        Join-Path $RepositoryRoot $identity.path)).Hash.ToLowerInvariant()
    Assert-Condition ($actual -ceq $identity.sha256.ToLowerInvariant()) (
        "Source identity mismatch: $($identity.path)")
}

$prior = @(Import-Csv (Join-Path $raw 'prior-raw-identities.csv'))
$actualPriorPaths = @(
    Get-ChildItem -LiteralPath (Join-Path $RepositoryRoot 'docs\design\lxr-port\P3.1\raw') -Recurse -File
    Get-ChildItem -LiteralPath (Join-Path $RepositoryRoot 'docs\design\lxr-port\P3.3\raw') -Recurse -File
) | ForEach-Object { [IO.Path]::GetRelativePath($RepositoryRoot, $_.FullName).Replace('\', '/') }
Assert-Condition (
    (($prior.path | Sort-Object) -join '|') -ceq (($actualPriorPaths | Sort-Object) -join '|')) (
    'Prior raw evidence path set mismatch.')
foreach ($identity in $prior) {
    $actual = (Get-FileHash -Algorithm SHA256 (
        Join-Path $RepositoryRoot $identity.path)).Hash.ToLowerInvariant()
    Assert-Condition ($actual -ceq $identity.sha256.ToLowerInvariant()) (
        "Prior raw evidence changed: $($identity.path)")
}

$validation = @(Import-Csv (Join-Path $raw 'validation-summary.csv'))
Assert-Condition (
    (($validation.platform | Sort-Object) -join '|') -ceq (
        (@($manifest.validationPlatforms) | Sort-Object) -join '|')) (
    'Validation platform evidence is incomplete.')
Assert-Condition (@($validation | Where-Object {
    $_.result -ne 'PASS' -or $_.passed -ne $_.total -or [int]$_.total -le 0
}).Count -eq 0) 'A validation row failed.'

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
    (($runtimeActual | Sort-Object) -join '|') -ceq (($runtimeExpected | Sort-Object) -join '|') -and
    @($runtime | Where-Object result -ne 'PASS').Count -eq 0) (
    'Runtime smoke evidence is incomplete or failed.')

$builds = @(Import-Csv (Join-Path $raw 'build-summary.csv'))
Assert-Condition (
    (($builds.id | Sort-Object) -join '|') -ceq (
        (@($manifest.buildRows) | Sort-Object) -join '|') -and
    @($builds | Where-Object result -ne 'PASS').Count -eq 0) (
    'Build evidence is incomplete or failed.')
$cross = @(Import-Csv (Join-Path $raw 'cross-target-summary.csv'))
Assert-Condition (
    (($cross.platform | Sort-Object) -join '|') -ceq (
        (@($manifest.crossTargets) | Sort-Object) -join '|') -and
    @($cross | Where-Object result -ne 'PASS').Count -eq 0) (
    'Cross-target evidence is incomplete or failed.')
$regression = @(Import-Csv (Join-Path $raw 'regression-summary.csv'))
Assert-Condition (
    $regression.Count -eq 4 -and
    @($regression | Where-Object result -ne 'PASS').Count -eq 0) (
    'P3.1/P3.3 regression evidence is incomplete or failed.')
$review = @(Import-Csv (Join-Path $raw 'review-summary.csv'))
Assert-Condition (
    $review.Count -eq 1 -and
    $review[0].result -eq 'PASS' -and
    [int]$review[0].errors -eq 0 -and
    [int]$review[0].warnings -eq 0) (
    'Code/performance review evidence is incomplete.')

$benchmarkRaw = @(Import-Csv (Join-Path $raw 'benchmark-raw.csv'))
$policyNames = @($manifest.benchmark.policies.psobject.Properties.Name)
$expectedRows =
    $manifest.benchmark.workers.Count *
    $manifest.benchmark.backlogs.Count *
    $policyNames.Count *
    [int]$manifest.benchmark.launches
Assert-Condition ($benchmarkRaw.Count -eq $expectedRows) (
    "Benchmark raw row count differs: $($benchmarkRaw.Count)/$expectedRows.")
Assert-Condition (@($benchmarkRaw | Where-Object result -ne 'PASS').Count -eq 0) (
    'A benchmark row failed.')
foreach ($policy in $manifest.benchmark.policies.psobject.Properties) {
    $rows = @($benchmarkRaw | Where-Object policy -eq $policy.Name)
    Assert-Condition (
        $rows.Count -eq (
            $manifest.benchmark.workers.Count *
            $manifest.benchmark.backlogs.Count *
            [int]$manifest.benchmark.launches) -and
        @($rows | Where-Object {
            [int]$_.callback_passes -ne [int]$policy.Value
        }).Count -eq 0) (
        "Benchmark policy differs: $($policy.Name)")
}
Assert-Condition (@($benchmarkRaw | Where-Object {
    [int]$_.observed_min -ne [int]$_.backlog -or
    [int]$_.observed_max -ne [int]$_.backlog -or
    [uint64]$_.callbacks -eq 0 -or
    [uint64]$_.callbacks -ne [uint64]$_.executed -or
    [uint64]$_.ownership_violations -ne 0
}).Count -eq 0) 'Benchmark backlog/callback/ownership evidence differs.'

$summary = @(Import-Csv (Join-Path $raw 'benchmark-summary.csv'))
Assert-Condition (
    $summary.Count -eq (
        $manifest.benchmark.workers.Count *
        $manifest.benchmark.backlogs.Count *
        $policyNames.Count) -and
    @($summary | Where-Object result -ne 'PASS').Count -eq 0) (
    'Benchmark summary evidence differs.')
$controls = @(Import-Csv (Join-Path $raw 'benchmark-controls.csv'))
Assert-Condition ($controls.Count -eq $manifest.benchmark.workers.Count) (
    'Benchmark control cardinality differs.')
foreach ($control in $controls) {
    Assert-Condition (
        [double]$control.aa_min -ge [double]$manifest.benchmark.aaLower -and
        [double]$control.aa_max -le [double]$manifest.benchmark.aaUpper) (
        'Benchmark A/A ratio is outside its bounds.')
    Assert-Condition (
        [double]$control.injected_to_base_depth32 -ge
            [double]$manifest.benchmark.injectedMinimum) (
        'Benchmark injected backlog ratio is below its bound.')
    Assert-Condition (
        [double]$control.injected_depth32_to_depth0 -ge
            [double]$manifest.benchmark.backlogSensitivityMinimum) (
        'Benchmark backlog-depth sensitivity is below its bound.')
}
$invocation = @(Import-Csv (Join-Path $raw 'benchmark-invocation.csv'))
Assert-Condition (
    $invocation.Count -eq 1 -and
    [int]$invocation[0].launches -eq [int]$manifest.benchmark.launches -and
    [int]$invocation[0].packet_count -eq [int]$manifest.benchmark.packetCount -and
    [int]$invocation[0].reference_capacity -eq
        [int]$manifest.benchmark.referenceCapacity -and
    [int]$invocation[0].warmup_cycles -eq
        [int]$manifest.benchmark.warmupCycles -and
    [int]$invocation[0].measured_cycles -eq
        [int]$manifest.benchmark.measuredCycles -and
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

foreach ($script in Get-ChildItem -LiteralPath $scriptRoot -Filter '*.sh') {
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

Write-Host "PASS: $checks P3.4 evidence checks"
