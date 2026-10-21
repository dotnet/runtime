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

$script:p24Checks = 0
function Assert-P24([bool]$Condition, [string]$Message) {
    $script:p24Checks++
    if (-not $Condition) {
        throw $Message
    }
}

function Assert-ExactSet(
    [object[]]$Actual,
    [object[]]$Expected,
    [string]$Message) {
    $actualValues = @($Actual | ForEach-Object { [string]$_ } | Sort-Object)
    $expectedValues = @($Expected | ForEach-Object { [string]$_ } | Sort-Object)
    Assert-P24 `
        (($actualValues.Count -eq $expectedValues.Count) -and
         (($actualValues -join "`n") -ceq ($expectedValues -join "`n"))) `
        $Message
}

$p24 = Join-Path $RepositoryRoot 'docs\design\lxr-port\P2.4'
$manifestPath = Join-Path $p24 'evidence-manifest.json'
$mechanismPath = Join-Path $p24 'mechanism-spec.json'
$sourceManifestPath = Join-Path $p24 'source-manifest.txt'
$raw = Join-Path $p24 'raw'
foreach ($path in @($manifestPath, $mechanismPath, $sourceManifestPath)) {
    Assert-P24 (Test-Path -LiteralPath $path -PathType Leaf) "Missing verifier input: $path"
}

$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
$mechanism = Get-Content -LiteralPath $mechanismPath -Raw | ConvertFrom-Json
Assert-P24 ($manifest.schemaVersion -eq 1) 'Evidence schema version differs.'
Assert-P24 ($mechanism.schemaVersion -eq 1) 'Mechanism schema version differs.'
Assert-P24 ($mechanism.dependencyCommit -eq '17ac93d30d5ebc08aeda0ddf24e562d506d72230') 'Dependency commit differs.'
Assert-P24 ($mechanism.oracle.core -eq '304ce69d43aae87de501111fceb8cbd33173a03a') 'Core oracle differs.'
Assert-P24 $mechanism.oracle.rcEnabled 'RC-enabled reachability is missing.'
Assert-P24 ($mechanism.referenceCountBits -join ',' -eq '2,4,8') 'RC width set differs.'
Assert-P24 ($mechanism.objectMark.recycledConcurrentOrFinalMark -eq 1) 'Conservative object mark differs.'
Assert-P24 ($mechanism.objectMark.recycledInactive -eq 'preserve') 'Inactive recycled object-mark rule differs.'
Assert-P24 ($mechanism.lineReuse.wrap -eq '255->0') 'Reuse wrap differs.'

$allocationHeader = Get-Content -LiteralPath (
    Join-Path $RepositoryRoot 'src\coreclr\gc\immix_allocation.h') -Raw
$allocationImplementation = Get-Content -LiteralPath (
    Join-Path $RepositoryRoot 'src\coreclr\gc\immix_allocation.cpp') -Raw
$metadataHeader = Get-Content -LiteralPath (
    Join-Path $RepositoryRoot 'src\coreclr\gc\side_metadata.h') -Raw
$metadataImplementation = Get-Content -LiteralPath (
    Join-Path $RepositoryRoot 'src\coreclr\gc\side_metadata.cpp') -Raw
$coreCmake = Get-Content -LiteralPath (
    Join-Path $RepositoryRoot 'src\coreclr\CMakeLists.txt') -Raw
Assert-P24 (-not $metadataHeader.Contains('LineMark')) 'Ordinary line marks must remain unselected.'
Assert-P24 $allocationHeader.Contains('class ImmixAllocationManager final') 'Allocation manager is missing.'
Assert-P24 $allocationHeader.Contains('class ImmixBumpAllocator final') 'Bump allocator is missing.'
Assert-P24 $allocationImplementation.Contains('(line == 0) || previousDead') 'RC first-line rule differs.'
Assert-P24 $allocationImplementation.Contains('VisitWords(') 'Bulk RC-word scan is missing.'
Assert-P24 $allocationImplementation.Contains('LxrSideMetadataKind::ObjectMark') 'Object-mark publication is missing.'
Assert-P24 $allocationImplementation.Contains('FetchAddWrapping(') 'Wrapping reuse update is missing.'
Assert-P24 $allocationImplementation.Contains('if (!IsCopySource(source))') 'Copy-specific zeroing is missing.'
Assert-P24 $allocationImplementation.Contains('memset(reinterpret_cast<void*>(hole.start), 0, size)') 'Mutator hole zeroing is missing.'
Assert-P24 $coreCmake.Contains('CLR_CMAKE_ENABLE_IMMIX_ALLOCATION_TEST') 'P2.4 CMake option is missing.'
Assert-P24 ($coreCmake -match 'IMMIX_ALLOCATION_TEST\s*\r?\n\s*"[^"]+"\s+OFF') 'P2.4 CMake option is not default-off.'

$sourcePaths = @(
    Get-Content -LiteralPath $sourceManifestPath |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
)
Assert-P24 ($sourcePaths.Count -gt 0) 'Source manifest is empty.'
Assert-P24 (($sourcePaths | Sort-Object -Unique).Count -eq $sourcePaths.Count) 'Source manifest contains duplicate paths.'
foreach ($relative in $sourcePaths) {
    Assert-P24 `
        (Test-Path -LiteralPath (Join-Path $RepositoryRoot $relative) -PathType Leaf) `
        "Source manifest path is missing: $relative"
}

Assert-P24 (Test-Path -LiteralPath $raw -PathType Container) 'Raw evidence directory is missing.'
foreach ($name in $manifest.rawFiles) {
    Assert-P24 `
        (Test-Path -LiteralPath (Join-Path $raw $name) -PathType Leaf) `
        "Required raw evidence is missing: $name"
}

$sourceCommit = (Get-Content -LiteralPath (
    Join-Path $raw 'source-commit.txt') -Raw).Trim()
Assert-P24 ($sourceCommit -match '^[0-9a-f]{40}$') 'Source commit identity is malformed.'
$identities = @(Import-Csv (Join-Path $raw 'source-identities.csv'))
Assert-ExactSet $identities.path $sourcePaths 'Source identity path set mismatch.'
foreach ($identity in $identities) {
    $hash = (Get-FileHash -LiteralPath (
        Join-Path $RepositoryRoot $identity.path) -Algorithm SHA256).Hash
    Assert-P24 ($hash -eq $identity.sha256) "Source identity mismatch: $($identity.path)"
}

$validation = @(Import-Csv (Join-Path $raw 'validation-summary.csv'))
Assert-P24 ($validation.Count -eq $manifest.validationPlatforms.Count) 'Validation platform evidence is incomplete.'
foreach ($expected in $manifest.validationPlatforms) {
    $row = @($validation | Where-Object platform -eq $expected.platform)
    Assert-P24 ($row.Count -eq 1) "Validation row differs for $($expected.platform)."
    Assert-P24 ([int]$row[0].passed -eq [int]$expected.total) "Validation pass count differs for $($expected.platform)."
    Assert-P24 ([int]$row[0].total -eq [int]$expected.total) "Validation total differs for $($expected.platform)."
    Assert-P24 ($row[0].result -eq 'PASS') "Validation result differs for $($expected.platform)."
}

$runtime = @(Import-Csv (Join-Path $raw 'runtime-smoke-summary.csv'))
$expectedRuntimeCount =
    $manifest.runtimePlatforms.Count *
    $manifest.runtimeLinkages.Count *
    $manifest.runtimeGcModes.Count
Assert-P24 ($runtime.Count -eq $expectedRuntimeCount) 'Runtime smoke evidence is incomplete.'
foreach ($platform in $manifest.runtimePlatforms) {
    foreach ($linkage in $manifest.runtimeLinkages) {
        foreach ($gcMode in $manifest.runtimeGcModes) {
            $row = @($runtime | Where-Object {
                ($_.platform -eq $platform) -and
                ($_.linkage -eq $linkage) -and
                ($_.gc_mode -eq $gcMode)
            })
            Assert-P24 ($row.Count -eq 1) "Runtime smoke row differs for $platform/$linkage/$gcMode."
            Assert-P24 (($row[0].result -eq 'PASS') -and ([int]$row[0].exit_code -eq 0)) "Runtime smoke failed for $platform/$linkage/$gcMode."
        }
    }
}

$combined = @(Import-Csv (Join-Path $raw 'combined-export-summary.csv'))
$expectedExports = @(
    'GC_AllocationNotificationTest_Reset',
    'GC_ImmixAllocationTest_Run',
    'GC_ImmixBlockStateTest_Run',
    'GC_WriteBarrierTest_Reset'
)
Assert-ExactSet $combined.symbol $expectedExports 'Combined export set differs.'
foreach ($row in $combined) {
    Assert-P24 ($row.result -eq 'PASS') "Combined export failed: $($row.symbol)"
}

$benchmark = @(Import-Csv (Join-Path $raw 'benchmark-raw.csv'))
Assert-P24 ($benchmark.Count -eq $manifest.benchmark.rows) 'Benchmark row count differs.'
foreach ($method in $manifest.benchmark.methods) {
    foreach ($worker in $manifest.benchmark.workerCounts) {
        $row = @($benchmark | Where-Object {
            ($_.Method -eq $method) -and
            ([int]$_.WorkerCount -eq [int]$worker)
        })
        Assert-P24 ($row.Count -eq 1) "Benchmark row differs for $method/$worker."
        Assert-P24 (-not [string]::IsNullOrWhiteSpace($row[0].Mean)) "Benchmark mean is missing for $method/$worker."
    }
}

$controls = @(Import-Csv (Join-Path $raw 'benchmark-controls.csv'))
Assert-P24 ($controls.Count -eq $manifest.benchmark.controls) 'Benchmark controls are incomplete.'
foreach ($row in $controls) {
    Assert-P24 ([double]$row.fresh_aa_ratio -ge [double]$manifest.benchmark.aaMinimum) 'Fresh A/A ratio is below its bound.'
    Assert-P24 ([double]$row.fresh_aa_ratio -le [double]$manifest.benchmark.aaMaximum) 'Fresh A/A ratio is above its bound.'
    Assert-P24 ([double]$row.recycled_aa_ratio -ge [double]$manifest.benchmark.aaMinimum) 'Recycled A/A ratio is below its bound.'
    Assert-P24 ([double]$row.recycled_aa_ratio -le [double]$manifest.benchmark.aaMaximum) 'Recycled A/A ratio is above its bound.'
    Assert-P24 ([double]$row.extra_scan_ratio -ge [double]$manifest.benchmark.extraScanMinimum) 'Extra-scan control is below its bound.'
    Assert-P24 ([double]$row.owner_delay_ratio -ge [double]$manifest.benchmark.ownerDelayMinimum) 'Owner-delay control is below its bound.'
    Assert-P24 ($row.result -eq 'PASS') 'Benchmark control result differs.'
}

$identity = @(Import-Csv (Join-Path $raw 'benchmark-identity.csv'))
Assert-P24 ($identity.Count -eq 1) 'Benchmark identity row differs.'
Assert-P24 ([int]$identity[0].launch_count -eq [int]$manifest.benchmark.launchCount) 'Benchmark launch count differs.'
Assert-P24 ([int]$identity[0].warmup_count -eq [int]$manifest.benchmark.warmupCount) 'Benchmark warmup count differs.'
Assert-P24 ([int]$identity[0].iteration_count -eq [int]$manifest.benchmark.iterationCount) 'Benchmark iteration count differs.'
Assert-P24 ($identity[0].controls_enforced -eq 'True') 'Benchmark controls were not enforced.'

$defaultAllocation = @(
    Import-Csv (Join-Path $raw 'default-allocation-controls.csv'))
Assert-ExactSet `
    $defaultAllocation.gc_mode `
    $manifest.defaultAllocation.gcModes `
    'Default allocation GC modes differ.'
foreach ($row in $defaultAllocation) {
    Assert-P24 ([int]$row.launch_count -eq [int]$manifest.defaultAllocation.launchCount) 'Default allocation launch count differs.'
    Assert-P24 ([int]$row.warmup_count -eq [int]$manifest.defaultAllocation.warmupCount) 'Default allocation warmup count differs.'
    Assert-P24 ([int]$row.iteration_count -eq [int]$manifest.defaultAllocation.iterationCount) 'Default allocation iteration count differs.'
    Assert-P24 ([double]$row.aa_ratio -ge [double]$manifest.defaultAllocation.ratioMinimum) 'Default allocation A/A ratio is below its bound.'
    Assert-P24 ([double]$row.aa_ratio -le [double]$manifest.defaultAllocation.ratioMaximum) 'Default allocation A/A ratio is above its bound.'
    Assert-P24 ([double]$row.changed_ratio -ge [double]$manifest.defaultAllocation.ratioMinimum) 'Default allocation changed ratio is below its bound.'
    Assert-P24 ([double]$row.changed_ratio -le [double]$manifest.defaultAllocation.ratioMaximum) 'Default allocation changed ratio is above its bound.'
    Assert-P24 ($row.baseline_corerun_sha256 -eq $row.control_corerun_sha256) 'Default allocation corerun control identity differs.'
    Assert-P24 ($row.baseline_coreclr_sha256 -eq $row.control_coreclr_sha256) 'Default allocation CoreCLR control identity differs.'
    Assert-P24 ($row.baseline_corelib_sha256 -eq $row.control_corelib_sha256) 'Default allocation CoreLib control identity differs.'
    Assert-P24 (-not [string]::IsNullOrWhiteSpace($row.changed_corerun_sha256)) 'Changed corerun identity is missing.'
    Assert-P24 (-not [string]::IsNullOrWhiteSpace($row.changed_coreclr_sha256)) 'Changed CoreCLR identity is missing.'
    Assert-P24 (-not [string]::IsNullOrWhiteSpace($row.changed_corelib_sha256)) 'Changed CoreLib identity is missing.'
    Assert-P24 ($row.controls_enforced -eq 'True') 'Default allocation controls were not enforced.'
    Assert-P24 ($row.result -eq 'PASS') 'Default allocation control result differs.'
}

$defaultPath = @(Import-Csv (Join-Path $raw 'default-path-summary.csv'))
Assert-ExactSet $defaultPath.property $manifest.defaultPathProperties 'Default-path evidence differs.'
foreach ($row in $defaultPath) {
    Assert-P24 ($row.result -eq 'PASS') "Default-path check failed: $($row.property)"
}

$reviews = @(Import-Csv (Join-Path $raw 'review-summary.csv'))
Assert-P24 ($reviews.Count -ge [int]$manifest.reviewModels) 'Review evidence is incomplete.'
Assert-P24 (($reviews.model | Sort-Object -Unique).Count -ge [int]$manifest.reviewModels) 'Review model diversity is incomplete.'
foreach ($row in $reviews) {
    Assert-P24 ($row.final_result -eq 'NO_ERRORS_OR_WARNINGS') "Review did not pass: $($row.reviewer)"
    Assert-P24 ($row.source_commit -eq $sourceCommit) "Review source differs: $($row.reviewer)"
}

$platform = @(Import-Csv (Join-Path $raw 'platform-summary.csv'))
foreach ($name in @('windows-x64', 'windows-x86', 'windows-arm64', 'linux-x64')) {
    $row = @($platform | Where-Object platform -eq $name)
    Assert-P24 ($row.Count -eq 1) "Platform summary differs for $name."
    Assert-P24 ($row[0].result -eq 'PASS') "Platform result failed for $name."
}

if (Test-Path -LiteralPath (Join-Path $RepositoryRoot '.git')) {
    git -C $RepositoryRoot cat-file -e "$sourceCommit`^{commit}"
    Assert-P24 ($LASTEXITCODE -eq 0) 'Source commit does not exist.'
    $sourceParent = (git -C $RepositoryRoot rev-parse "$sourceCommit^").Trim()
    Assert-P24 ($sourceParent -eq $mechanism.dependencyCommit) 'Source parent differs from P2.2.'
    $head = (git -C $RepositoryRoot rev-parse HEAD).Trim()
    $headParent = (git -C $RepositoryRoot rev-parse HEAD^).Trim()
    Assert-P24 ($headParent -eq $sourceCommit) 'Evidence commit is not the direct source child.'
    $changed = @(git -C $RepositoryRoot diff --name-only $sourceCommit $head)
    Assert-P24 ($changed.Count -gt 0) 'Evidence commit has no changes.'
    foreach ($path in $changed) {
        Assert-P24 $path.StartsWith('docs/design/lxr-port/P2.4/raw/') "Non-raw path changed after source freeze: $path"
    }
}

Write-Host "PASS: $script:p24Checks P2.4 evidence checks"
