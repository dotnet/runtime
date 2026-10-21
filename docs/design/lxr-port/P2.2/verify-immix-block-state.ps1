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
function Assert-Check([bool]$Condition, [string]$Message) {
    $script:checks++
    if (-not $Condition) {
        throw $Message
    }
}

function Assert-ExactSet(
    [object[]]$Actual,
    [object[]]$Expected,
    [string]$Message) {
    $actualValues = @($Actual | ForEach-Object { "$_" } | Sort-Object -Unique)
    $expectedValues = @($Expected | ForEach-Object { "$_" } | Sort-Object -Unique)
    Assert-Check `
        (($actualValues.Count -eq $expectedValues.Count) -and
         (($actualValues -join "`n") -ceq ($expectedValues -join "`n"))) `
        $Message
}

$manifestPath = Join-Path $scriptRoot 'evidence-manifest.json'
$specPath = Join-Path $scriptRoot 'mechanism-spec.json'
$sourceManifestPath = Join-Path $scriptRoot 'source-manifest.txt'
$raw = Join-Path $scriptRoot 'raw'
foreach ($path in @($manifestPath, $specPath, $sourceManifestPath)) {
    Assert-Check (Test-Path -LiteralPath $path -PathType Leaf) "Required verifier input is missing: $path"
}

$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
$spec = Get-Content -LiteralPath $specPath -Raw | ConvertFrom-Json
Assert-Check ($manifest.schemaVersion -eq 1) 'Evidence manifest schema differs.'
Assert-Check ($spec.schemaVersion -eq 1) 'Mechanism spec schema differs.'
Assert-Check ($spec.dependencyCommit -eq '8866eee4907667c93752cc4411bcdd838b98fec5') 'P2.1 dependency identity differs.'
Assert-Check ($spec.oracles.pldi.core -eq 'df8d30a39237a5bf5a8e27ca5a6f46acc6080c94') 'PLDI oracle identity differs.'
Assert-Check ($spec.oracles.head.core -eq '304ce69d43aae87de501111fceb8cbd33173a03a') 'HEAD oracle identity differs.'
Assert-Check $spec.oracleDecision.a05Exception 'A05 exception is not recorded.'
Assert-Check (-not $spec.oracleDecision.hybridStateEncoding) 'Hybrid state encoding is forbidden.'
Assert-Check ($spec.geometry.addressBits -eq 47) 'Address-bit geometry differs.'
Assert-Check ($spec.geometry.blockBytes -eq 32768) 'Block geometry differs.'
Assert-Check ($spec.geometry.lineBytes -eq 256) 'Line geometry differs.'
Assert-Check ($spec.geometry.linesPerBlock -eq 128) 'Line count differs.'
Assert-Check ($spec.phaseEpoch.initialGlobal -eq 1) 'Initial global epoch differs.'
Assert-Check ($spec.phaseEpoch.wrapFrom -eq 254) 'Epoch wrap source differs.'
Assert-Check ($spec.phaseEpoch.wrapTo -eq 1) 'Epoch wrap target differs.'
Assert-Check $spec.phaseEpoch.pauseStartBump 'Pause-start bump is missing.'
Assert-Check $spec.phaseEpoch.releaseBump 'Release bump is missing.'

$headerPath = Join-Path $RepositoryRoot 'src\coreclr\gc\immix_block.h'
$implementationPath = Join-Path $RepositoryRoot 'src\coreclr\gc\immix_block.cpp'
$metadataHeaderPath = Join-Path $RepositoryRoot 'src\coreclr\gc\side_metadata.h'
$metadataImplementationPath = Join-Path $RepositoryRoot 'src\coreclr\gc\side_metadata.cpp'
foreach ($path in @(
    $headerPath,
    $implementationPath,
    $metadataHeaderPath,
    $metadataImplementationPath,
    (Join-Path $RepositoryRoot 'src\coreclr\gc\immixblockstatetest.cpp'),
    (Join-Path $scriptRoot 'immix-block-validation.cpp'),
    (Join-Path $scriptRoot 'immix-block-benchmark-native.cpp'),
    (Join-Path $scriptRoot 'run-immix-block-validation.ps1'),
    (Join-Path $scriptRoot 'run-immix-block-validation.sh'),
    (Join-Path $scriptRoot 'run-immix-block-runtime-smoke.ps1'),
    (Join-Path $scriptRoot 'run-immix-block-runtime-smoke.sh'),
    (Join-Path $scriptRoot 'run-immix-block-combined-exports.sh'),
    (Join-Path $scriptRoot 'run-immix-block-benchmark.ps1'),
    (Join-Path $scriptRoot 'run-immix-block-evidence.ps1'))) {
    Assert-Check (Test-Path -LiteralPath $path -PathType Leaf) "Required product or evidence source is missing: $path"
}

$header = Get-Content -LiteralPath $headerPath -Raw
$implementation = Get-Content -LiteralPath $implementationPath -Raw
$metadataHeader = Get-Content -LiteralPath $metadataHeaderPath -Raw
$metadataImplementation = Get-Content -LiteralPath $metadataImplementationPath -Raw
foreach ($token in @(
    'BlockLogBytes = 15',
    'LineLogBytes = 8',
    'LinesPerBlock = static_cast<size_t>(1) << BlockLogLines',
    'InitialPhaseEpoch = 1',
    'LastPhaseEpoch = 254',
    'std::atomic<uint8_t> m_global_phase_epoch',
    'std::atomic<uint32_t> m_active_operations',
    'std::atomic<bool> m_phase_transition')) {
    Assert-Check ($header.Contains($token)) "Required product contract is missing: $token"
}
foreach ($token in @(
    'BeginBlockOperation',
    'BeginPhaseTransition',
    'ResetAllDataRangesQuiescent',
    'blockEpoch == static_cast<uint8_t>(globalEpoch - 1)',
    'state == ImmixBlockState::Unallocated',
    'state != ImmixBlockState::Unallocated',
    'm_global_phase_epoch.store(newEpoch, std::memory_order_seq_cst)')) {
    Assert-Check ($implementation.Contains($token)) "Required implementation contract is missing: $token"
}
Assert-Check ($metadataHeader.Contains('ResetAllDataRangesQuiescent')) 'P2.1 reset-all API is missing.'
Assert-Check ($metadataHeader.Contains('bool IsSpecEnabled')) 'P2.1 enabled-spec query is missing.'
Assert-Check ($metadataImplementation.Contains('ResetRangeQuiescentCore')) 'P2.1 reset core is missing.'

$sourcePaths = @(
    Get-Content -LiteralPath $sourceManifestPath |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
)
Assert-Check ($sourcePaths.Count -gt 0) 'Source manifest is empty.'
Assert-Check (($sourcePaths | Sort-Object -Unique).Count -eq $sourcePaths.Count) 'Source manifest contains duplicate paths.'
foreach ($relative in $sourcePaths) {
    Assert-Check `
        (Test-Path -LiteralPath (Join-Path $RepositoryRoot $relative) -PathType Leaf) `
        "Source manifest path is missing: $relative"
}

Assert-Check (Test-Path -LiteralPath $raw -PathType Container) 'Raw evidence directory is missing.'
foreach ($name in $manifest.rawFiles) {
    Assert-Check `
        (Test-Path -LiteralPath (Join-Path $raw $name) -PathType Leaf) `
        "Required raw evidence is missing: $name"
}

$sourceCommit = (Get-Content -LiteralPath (Join-Path $raw 'source-commit.txt') -Raw).Trim()
Assert-Check ($sourceCommit -match '^[0-9a-f]{40}$') 'Source commit identity is malformed.'
$identities = @(Import-Csv (Join-Path $raw 'source-identities.csv'))
Assert-ExactSet $identities.path $sourcePaths 'Source identity path set mismatch.'
foreach ($identity in $identities) {
    $path = Join-Path $RepositoryRoot $identity.path
    $hash = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash
    Assert-Check ($hash -eq $identity.sha256) "Source identity mismatch: $($identity.path)"
}

$validation = @(Import-Csv (Join-Path $raw 'validation-summary.csv'))
Assert-Check ($validation.Count -eq $manifest.validationPlatforms.Count) 'Validation platform evidence is incomplete.'
foreach ($expected in $manifest.validationPlatforms) {
    $row = @($validation | Where-Object platform -eq $expected.platform)
    Assert-Check ($row.Count -eq 1) "Validation row differs for $($expected.platform)."
    Assert-Check ([int]$row[0].passed -eq [int]$expected.total) "Validation pass count differs for $($expected.platform)."
    Assert-Check ([int]$row[0].total -eq [int]$expected.total) "Validation total differs for $($expected.platform)."
    Assert-Check ($row[0].result -eq 'PASS') "Validation result differs for $($expected.platform)."
}

$runtime = @(Import-Csv (Join-Path $raw 'runtime-smoke-summary.csv'))
$expectedRuntimeCount =
    $manifest.runtimePlatforms.Count *
    $manifest.runtimeLinkages.Count *
    $manifest.runtimeGcModes.Count
Assert-Check ($runtime.Count -eq $expectedRuntimeCount) 'Runtime smoke evidence is incomplete.'
foreach ($platform in $manifest.runtimePlatforms) {
    foreach ($linkage in $manifest.runtimeLinkages) {
        foreach ($gcMode in $manifest.runtimeGcModes) {
            $row = @($runtime | Where-Object {
                ($_.platform -eq $platform) -and
                ($_.linkage -eq $linkage) -and
                ($_.gc_mode -eq $gcMode)
            })
            Assert-Check ($row.Count -eq 1) "Runtime smoke row differs for $platform/$linkage/$gcMode."
            Assert-Check (($row[0].result -eq 'PASS') -and ([int]$row[0].exit_code -eq 0)) "Runtime smoke failed for $platform/$linkage/$gcMode."
        }
    }
}

$combinedExports = @(Import-Csv (Join-Path $raw 'combined-export-summary.csv'))
$expectedCombinedExports = @(
    'GC_ImmixBlockStateTest_Run',
    'GC_WriteBarrierTest_Reset',
    'GC_AllocationNotificationTest_Reset'
)
Assert-Check ($combinedExports.Count -eq $expectedCombinedExports.Count) 'Combined export evidence is incomplete.'
foreach ($symbol in $expectedCombinedExports) {
    $row = @($combinedExports | Where-Object symbol -eq $symbol)
    Assert-Check ($row.Count -eq 1) "Combined export row differs for $symbol."
    Assert-Check ($row[0].result -eq 'PASS') "Combined export failed for $symbol."
}

$benchmark = @(Import-Csv (Join-Path $raw 'benchmark-raw.csv'))
Assert-Check ($benchmark.Count -eq $manifest.benchmark.rows) 'Benchmark row count differs.'
foreach ($method in $manifest.benchmark.methods) {
    foreach ($workerCount in $manifest.benchmark.workerCounts) {
        $row = @($benchmark | Where-Object {
            ($_.Method -eq $method) -and
            ([int]$_.WorkerCount -eq [int]$workerCount)
        })
        Assert-Check ($row.Count -eq 1) "Benchmark row differs for $method/$workerCount."
        Assert-Check (-not [string]::IsNullOrWhiteSpace($row[0].Mean)) "Benchmark mean is missing for $method/$workerCount."
    }
}

$controls = @(Import-Csv (Join-Path $raw 'benchmark-controls.csv'))
Assert-Check ($controls.Count -eq $manifest.benchmark.controls) 'Benchmark control count differs.'
foreach ($control in $controls) {
    Assert-Check ([double]$control.aa_ratio -ge [double]$manifest.benchmark.aaMinimum) 'Benchmark A/A ratio is below its bound.'
    Assert-Check ([double]$control.aa_ratio -le [double]$manifest.benchmark.aaMaximum) 'Benchmark A/A ratio is above its bound.'
    Assert-Check ([double]$control.extra_cas_ratio -ge [double]$manifest.benchmark.extraCasMinimum) 'Benchmark extra-CAS control is below its bound.'
    Assert-Check ([double]$control.owner_delay_ratio -ge [double]$manifest.benchmark.ownerDelayMinimum) 'Benchmark owner-delay control is below its bound.'
    Assert-Check ($control.result -eq 'PASS') 'Benchmark control did not pass.'
}

$benchmarkIdentity = @(Import-Csv (Join-Path $raw 'benchmark-identity.csv'))
Assert-Check ($benchmarkIdentity.Count -eq 1) 'Benchmark identity row count differs.'
Assert-Check ([int]$benchmarkIdentity[0].launch_count -eq [int]$manifest.benchmark.launchCount) 'Benchmark launch count differs.'
Assert-Check ([int]$benchmarkIdentity[0].warmup_count -eq [int]$manifest.benchmark.warmupCount) 'Benchmark warmup count differs.'
Assert-Check ([int]$benchmarkIdentity[0].iteration_count -eq [int]$manifest.benchmark.iterationCount) 'Benchmark iteration count differs.'
Assert-Check ([int]$benchmarkIdentity[0].rows -eq [int]$manifest.benchmark.rows) 'Benchmark identity row total differs.'
Assert-Check ($benchmarkIdentity[0].native_sha256 -match '^[0-9A-F]{64}$') 'Benchmark native identity is malformed.'

$builds = @(Import-Csv (Join-Path $raw 'build-summary.csv'))
Assert-ExactSet $builds.name $manifest.buildNames 'Build evidence set differs.'
foreach ($build in $builds) {
    $expectedResult = if ($build.name -eq 'long-path-baseline-failure') { 'EXPECTED-FAIL' } else { 'PASS' }
    Assert-Check ($build.result -eq $expectedResult) "Build result differs for $($build.name)."
}

$platforms = @(Import-Csv (Join-Path $raw 'platform-summary.csv'))
Assert-Check (@($platforms | Where-Object platform -eq 'windows-x64').Count -eq 1) 'Windows x64 platform summary is missing.'
Assert-Check (@($platforms | Where-Object platform -eq 'linux-x64').Count -eq 1) 'Linux x64 platform summary is missing.'
Assert-Check (@($platforms | Where-Object platform -eq 'windows-x86').Count -eq 1) 'Windows x86 platform summary is missing.'
Assert-Check (@($platforms | Where-Object platform -eq 'windows-arm64').Count -eq 1) 'Windows ARM64 platform summary is missing.'

$checks++
if ($checks -ne [int]$manifest.verifierExpectedChecks) {
    throw "Verifier check count differs: $checks/$($manifest.verifierExpectedChecks)."
}
Write-Host "PASS: $checks P2.2 evidence checks"
