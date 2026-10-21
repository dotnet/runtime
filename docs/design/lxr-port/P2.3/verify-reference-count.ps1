# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

<#
.SYNOPSIS
Audits the committed P2.3 reference-count evidence tree.

.DESCRIPTION
The verifier is standalone: it defaults the repository root from its own
script location, accepts an explicit override, and audits only the tree it
was extracted or checked out into. It re-hashes every source-manifest path,
requires the complete manifest, requires every expected platform, build,
runtime, export, benchmark, and control row, and requires the recorded
source/evidence topology. Git ancestry is checked directly when a .git
directory is present and through raw/topology-summary.csv otherwise, because
a `git archive` extract intentionally has no .git directory.

The number of executed checks must equal evidence-manifest.json's
verifierExpectedChecks, so a silently skipped section fails the run.
#>
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

function Assert-Truthy([object]$Value, [string]$Message) {
    Assert-Check (("$Value" -eq 'True') -or ("$Value" -eq 'true')) $Message
}

function Assert-Falsy([object]$Value, [string]$Message) {
    Assert-Check (("$Value" -eq 'False') -or ("$Value" -eq 'false')) $Message
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

# --- manifest and spec identity -------------------------------------------
Assert-Check ($manifest.schemaVersion -eq 1) 'Evidence manifest schema differs.'
Assert-Check ($spec.schemaVersion -eq 1) 'Mechanism spec schema differs.'
Assert-Check ($spec.phase -eq 'P2.3') 'Mechanism spec phase differs.'
Assert-Check ($spec.dependencyCommits.p21 -eq '8866eee4907667c93752cc4411bcdd838b98fec5') 'P2.1 dependency identity differs.'
Assert-Check ($spec.dependencyCommits.p22 -eq '17ac93d30d5ebc08aeda0ddf24e562d506d72230') 'P2.2 dependency identity differs.'
Assert-Check ($spec.oracles.pldi.binding -eq 'abbdd1dbbe7277e4b0dfd1d37ba613c53f5ca50d') 'PLDI binding oracle identity differs.'
Assert-Check ($spec.oracles.pldi.core -eq 'df8d30a39237a5bf5a8e27ca5a6f46acc6080c94') 'PLDI core oracle identity differs.'
Assert-Check ($spec.oracles.head.binding -eq '0682434ae725787e38defbc17bd66dc918e4bdb7') 'HEAD binding oracle identity differs.'
Assert-Check ($spec.oracles.head.core -eq '304ce69d43aae87de501111fceb8cbd33173a03a') 'HEAD core oracle identity differs.'
Assert-Check ($spec.oracles.pldi.role -eq 'specifies') 'PLDI oracle role differs.'
Assert-Check ($spec.oracles.head.role -eq 'validates') 'HEAD oracle role differs.'
Assert-Falsy $spec.oracles.pldi.hasLxrPlanDirectory 'PLDI must be recorded as having no src/plan/lxr directory.'
Assert-Falsy $spec.oracles.namedTagOrBranchIsBasis 'A named tag or branch must not be an oracle basis.'

# --- width, sticky, and update instrument ---------------------------------
Assert-ExactSet $spec.referenceCount.activeBits @(2, 4, 8) 'Active reference-count widths differ.'
Assert-ExactSet $spec.referenceCount.logSelectors @(1, 2, 3) 'Reference-count log selectors differ.'
Assert-Check ($spec.referenceCount.defaultActiveBits -eq 2) 'Default reference-count width differs.'
Assert-Truthy $spec.referenceCount.allActiveWidthsImplemented 'All active widths must be implemented now.'
Assert-Check ($spec.referenceCount.maximumFormula -eq '(1 << bits) - 1') 'Sticky maximum formula differs.'
Assert-Check ([int]$spec.referenceCount.maximumByActiveBits.'2' -eq 3) '2-bit maximum differs.'
Assert-Check ([int]$spec.referenceCount.maximumByActiveBits.'4' -eq 15) '4-bit maximum differs.'
Assert-Check ([int]$spec.referenceCount.maximumByActiveBits.'8' -eq 255) '8-bit maximum differs.'
Assert-Truthy $spec.referenceCount.stickyMaximumRefusesIncrement 'Sticky maximum must refuse increment.'
Assert-Truthy $spec.referenceCount.stickyMaximumRefusesDecrement 'Sticky maximum must refuse decrement.'
Assert-Truthy $spec.referenceCount.stickyMaximumRefusesCompareExchangeDemotion 'CompareExchange must not demote sticky maximum.'
Assert-Truthy $spec.referenceCount.zeroRefusesDecrement 'Zero must refuse decrement.'
Assert-Truthy $spec.referenceCount.operationsReturnPreviousCount 'Operations must return the previous count.'
Assert-ExactSet $spec.referenceCount.updateStatuses @('Updated', 'Saturated', 'Zero') 'Update status set differs.'
Assert-Check ($spec.referenceCount.updateMemoryOrder -eq 'relaxed') 'Reference-count update memory order differs.'
Assert-Check ($spec.referenceCount.updateInstrument -eq 'masked-native-word-compare-exchange') 'Reference-count update instrument differs.'
Assert-Truthy $spec.referenceCount.preservesNeighborFields 'Updates must preserve neighbouring fields.'

# --- exact object starts ---------------------------------------------------
Assert-Check ($spec.objectStarts.alignmentBytes -eq 8) 'Object-start alignment differs.'
Assert-Truthy $spec.objectStarts.requiresOwnedDataRange 'Object starts must be in an owned data range.'
Assert-Truthy $spec.objectStarts.requiresValidObjectRegistration 'Object starts must be registered.'
Assert-Check ($spec.objectStarts.validObjectPlaneOwner -eq 'P2.1') 'ValidObject plane owner differs.'
Assert-Truthy $spec.objectStarts.interiorRejected 'Interior addresses must be rejected.'
Assert-Truthy $spec.objectStarts.byrefRejected 'Byrefs must be rejected.'
Assert-Falsy $spec.objectStarts.normalizesInteriorOrByref 'Interior and byref addresses must not be normalized.'
Assert-Truthy $spec.objectStarts.testConsumerRegistersSyntheticStarts 'Synthetic registration must be recorded.'
Assert-Check ($spec.objectStarts.syntheticRegistrationPendingPhase -eq 'P2.4') 'Synthetic registration owner phase differs.'
Assert-Check ($spec.objectStarts.minimumObjectBytes -eq 24) 'CoreCLR minimum object size differs.'
Assert-Truthy $spec.objectStarts.minimumChangesValidDensityAndAliasing 'The 24-byte minimum must be recorded as a density/aliasing change.'
Assert-Falsy $spec.objectStarts.minimumChangesLatticeOrSemanticsOrOverhead 'The 24-byte minimum must not change the lattice, semantics, or overhead.'

# --- straddle range --------------------------------------------------------
Assert-Check ($spec.straddle.lineBytes -eq 256) 'Line geometry differs.'
Assert-Check ($spec.straddle.blockBytes -eq 32768) 'Block geometry differs.'
Assert-Check ($spec.straddle.markedOnlyWhenSizeGreaterThanBytes -eq 256) 'Straddle threshold differs.'
Assert-Check ($spec.straddle.firstMarkedLine -eq 'align_down(start, 256) + 256') 'First marked straddle line differs.'
Assert-Check ($spec.straddle.endExclusive -eq 'align_down(end, 256)') 'Straddle exclusive end differs.'
Assert-Truthy $spec.straddle.rangeIsHalfOpen 'The straddle range must be half-open.'
Assert-Truthy $spec.straddle.firstLineExcluded 'The object first line must be excluded.'
Assert-Truthy $spec.straddle.partialTailExcluded 'A partial tail line must be excluded.'
Assert-Truthy $spec.straddle.exactBoundaryTailLineIncluded 'An exact-boundary full tail line must be included.'
Assert-Truthy $spec.straddle.crossesBlockRejected 'An object crossing a block must be rejected.'
Assert-Check ($spec.straddle.partialTailSafetyOwner -eq 'P2.4 oracle skip-first-dead-line') 'Partial-tail safety owner differs.'
Assert-Falsy $spec.straddle.implementsAllocator 'P2.3 must not implement an allocator.'
Assert-Falsy $spec.straddle.implementsHoleDiscovery 'P2.3 must not implement hole discovery.'
Assert-Falsy $spec.straddle.implementsSweep 'P2.3 must not implement sweep.'

# --- sentinel pair ---------------------------------------------------------
Assert-Truthy $spec.sentinel.requiresPair 'The continuation sentinel must be a pair.'
Assert-Check ($spec.sentinel.referenceCountValueAtLineStart -eq 1) 'Sentinel reference-count value differs.'
Assert-Check ($spec.sentinel.straddleValueAtLine -eq 1) 'Sentinel straddle value differs.'
Assert-Check ($spec.sentinel.validObjectValueAtLineStart -eq 0) 'Sentinel ValidObject value differs.'
Assert-Falsy $spec.sentinel.objectHeaderAloneIsSentinel 'An object header alone must not be the sentinel.'
Assert-Falsy $spec.sentinel.lineStateAloneIsSentinel 'Line state alone must not be the sentinel.'
Assert-Check ($spec.sentinel.brokenPairResult -eq 'InconsistentState') 'Broken sentinel-pair result differs.'
Assert-Check ((($spec.sentinel.clearOrder) -join ',') -ceq 'ReferenceCount,ReferenceCountStraddle') 'Sentinel clear order differs.'
Assert-Truthy $spec.sentinel.markRequiresEmptyLines 'Marking must require empty continuation lines.'

# --- lifecycle -------------------------------------------------------------
Assert-Truthy $spec.lifecycle.copyTransfersSourceObjectCountOnly 'Copy must transfer only the source object count.'
Assert-Truthy $spec.lifecycle.copyRederivesDestinationSentinels 'Copy must re-derive destination sentinels.'
Assert-Falsy $spec.lifecycle.copyCopiesSourceSentinels 'Copy must not copy source sentinels.'
Assert-Truthy $spec.lifecycle.copyRejectsOverlap 'Copy must reject overlapping ranges.'
Assert-Truthy $spec.lifecycle.releaseRetainsMetadata 'Release must retain reference-count metadata.'
Assert-Truthy $spec.lifecycle.reuseRetainsMetadata 'Reuse must retain reference-count metadata.'
Assert-Truthy $spec.lifecycle.epochWrapRetainsMetadata 'Epoch wrap must retain reference-count metadata.'
Assert-Truthy $spec.lifecycle.quiescentResetClearsValidObject 'Quiescent reset must clear validity.'
Assert-Truthy $spec.lifecycle.quiescentResetClearsReferenceCount 'Quiescent reset must clear reference counts.'
Assert-Truthy $spec.lifecycle.quiescentResetClearsStraddle 'Quiescent reset must clear straddle metadata.'
Assert-Truthy $spec.lifecycle.resetObjectQuiescent.exactStartRequired 'ResetObjectQuiescent must require an exact start.'
Assert-Truthy $spec.lifecycle.resetObjectQuiescent.registeredStartRequired 'ResetObjectQuiescent must require a registered start.'
Assert-Check ([int]$spec.lifecycle.resetObjectQuiescent.minimumObjectBytes -eq 24) 'ResetObjectQuiescent minimum size differs.'
Assert-Truthy $spec.lifecycle.resetObjectQuiescent.singleBlockRequired 'ResetObjectQuiescent must require a single-block extent.'
Assert-Truthy $spec.lifecycle.resetObjectQuiescent.quiescent 'ResetObjectQuiescent must be quiescent.'
Assert-Falsy $spec.lifecycle.resetObjectQuiescent.lineAlignedStartRequired 'ResetObjectQuiescent must allow non-line-aligned starts.'
Assert-Truthy $spec.lifecycle.resetObjectQuiescent.clearsOwnReferenceCount 'ResetObjectQuiescent must clear the object count.'
Assert-Truthy $spec.lifecycle.resetObjectQuiescent.derivesContinuationRangeFromObject 'ResetObjectQuiescent must derive continuation lines.'
Assert-Truthy $spec.lifecycle.resetObjectQuiescent.clearsContinuationSentinels 'ResetObjectQuiescent must clear continuation sentinels.'
Assert-Truthy $spec.lifecycle.resetObjectQuiescent.clearsValidObject 'ResetObjectQuiescent must revoke registration.'
Assert-Truthy $spec.lifecycle.reusableExtent.priorRegistrationCountAndSentinelsMustBeDismantled 'Reusable extents must dismantle prior metadata.'
Assert-Falsy $spec.lifecycle.reusableExtent.ownershipHandoffAloneAllowsOverlap 'Ownership handoff must not license overlap.'
Assert-Truthy $spec.lifecycle.reusableExtent.initializeRejectsMarkedContinuationLine 'Initialization must reject a marked continuation line.'
Assert-Truthy $spec.lifecycle.reusableExtent.copyRejectsMarkedContinuationLine 'Copy must reject a marked continuation line.'

# --- mandatory invariant consumer -----------------------------------------
Assert-Truthy $spec.invariantConsumer.mandatoryFromFirstCommit 'The invariant consumer must be mandatory from the first commit.'
Assert-Check ($spec.invariantConsumer.pldi.commit -eq 'df8d30a39237a5bf5a8e27ca5a6f46acc6080c94') 'PLDI invariant-consumer commit differs.'
Assert-Check ($spec.invariantConsumer.pldi.path -eq 'src/plan/barriers.rs') 'PLDI invariant-consumer path differs.'
Assert-Check ($spec.invariantConsumer.pldi.lines -eq '315-319') 'PLDI invariant-consumer line range differs.'
Assert-Check ([int]$spec.invariantConsumer.pldi.conditionLine -eq 316) 'PLDI invariant-consumer condition line differs.'
Assert-Check ($spec.invariantConsumer.pldi.function -eq 'FieldLoggingBarrier::slow') 'PLDI invariant-consumer function differs.'
Assert-Check ($spec.invariantConsumer.pldi.configuration -eq 'cfg sanity/debug') 'PLDI invariant-consumer configuration differs.'
Assert-Check ($spec.invariantConsumer.head.commit -eq '304ce69d43aae87de501111fceb8cbd33173a03a') 'HEAD invariant-consumer commit differs.'
Assert-Check ($spec.invariantConsumer.head.path -eq 'src/util/sanity/sanity_checker.rs') 'HEAD invariant-consumer path differs.'
Assert-Check ($spec.invariantConsumer.head.lines -eq '311-316') 'HEAD invariant-consumer line range differs.'
Assert-Check ([int]$spec.invariantConsumer.head.conditionLine -eq 312) 'HEAD invariant-consumer condition line differs.'
Assert-Check ($spec.invariantConsumer.head.configuration -eq 'non-default sanity') 'HEAD invariant-consumer configuration differs.'
Assert-Falsy $spec.invariantConsumer.head.barrierCheckPresent 'HEAD must be recorded as having no barrier check.'
Assert-Falsy $spec.invariantConsumer.head.validatedByDefaultConfiguration 'HEAD default configuration must not be claimed to validate the invariant.'

# --- scope and default-off -------------------------------------------------
Assert-Truthy $spec.scope.defaultOff 'P2.3 must be default-off.'
Assert-Check ($spec.scope.cmakeOption -eq 'CLR_CMAKE_ENABLE_REFERENCE_COUNT_TEST') 'CMake option name differs.'
Assert-Check ($spec.scope.testOptionArchitecture -eq 'AMD64') 'Test-option architecture gate differs.'
Assert-Check ($spec.scope.export -eq 'GC_ReferenceCountTest_Run') 'Test export name differs.'
Assert-Truthy $spec.scope.x86FailsClosed 'Windows x86 must fail closed.'
Assert-Truthy $spec.scope.arm64BuildOnly 'ARM64 must be build-only.'
Assert-Check ($spec.scope.linuxExecution -eq 'real WSL Unix execution, not a shim') 'Linux execution description differs.'
foreach ($excluded in @(
    'publicApi',
    'normalWksSvrPath',
    'runtimeEeJitContract',
    'collectorSelection',
    'mono',
    'nativeAotExecution',
    'wasm',
    'p24Allocation',
    'p3Scheduler',
    'p4PacketsOrSweep',
    'p5Satb',
    'p6Evacuation')) {
    Assert-Falsy $spec.scope.$excluded "P2.3 scope must exclude: $excluded"
}

Assert-Check ([int]$spec.benchmark.invocationCount -eq 1) 'Benchmark invocation count contract differs.'
Assert-Check ([int]$spec.benchmark.unrollFactor -eq 1) 'Benchmark unroll factor contract differs.'

# --- paper qualifications --------------------------------------------------
Assert-Check ($spec.paper.arxiv -eq '2210.17175') 'Paper identity differs.'
Assert-Check ($spec.paper.version -eq 'extended') 'Paper version differs.'
Assert-Check ($spec.paper.date -eq '2022-11-01') 'Paper date differs.'
Assert-Truthy $spec.paper.local 'The paper must be the local copy.'
Assert-Check ($spec.paper.table7Scope -eq 'lxr-only p50/p95 pause percentiles') 'Table 7 qualification differs.'
Assert-Check ($spec.paper.table1 -eq 'longer LXR GC pauses than G1 but better application latency') 'Table 1 qualification differs.'
Assert-ExactSet $spec.paper.table5HeapMultiples @('1.3x', '2x', '6x') 'Table 5 heap multiples differ.'
Assert-Check ((($spec.paper.table5ThroughputRatiosVersusG1) -join ',') -eq '0.97,0.96,1.01') 'Table 5 throughput ratios differ.'
Assert-Check ([double]$spec.paper.barrierGeomeanPercent -eq 1.6) 'Barrier geomean differs.'
Assert-Check ($spec.paper.barrierBaseline -eq 'no-barrier full-heap Immix') 'Barrier baseline differs.'
Assert-Check ([double]$spec.paper.barrierNoiseBandPercent -eq 3) 'Barrier noise band differs.'
Assert-ExactSet $spec.paper.disabledFeatures @('class unloading', 'compressed pointers', 'weak references') 'Paper disabled-feature set differs.'
Assert-Falsy $spec.paper.coreclrAcceptanceClaim 'No CoreCLR acceptance claim may be derived from the paper.'

# --- product and evidence sources exist -----------------------------------
$headerPath = Join-Path $RepositoryRoot 'src\coreclr\gc\reference_count.h'
$implementationPath = Join-Path $RepositoryRoot 'src\coreclr\gc\reference_count.cpp'
$testPath = Join-Path $RepositoryRoot 'src\coreclr\gc\referencecounttest.cpp'
$exportPath = Join-Path $RepositoryRoot (
    'src\coreclr\dlls\mscoree\mscorwks_referencecounttest_unixexports.src')
$metadataHeaderPath = Join-Path $RepositoryRoot 'src\coreclr\gc\side_metadata.h'
$metadataImplementationPath = Join-Path $RepositoryRoot 'src\coreclr\gc\side_metadata.cpp'
$blockHeaderPath = Join-Path $RepositoryRoot 'src\coreclr\gc\immix_block.h'
foreach ($path in @(
    $headerPath,
    $implementationPath,
    $testPath,
    $exportPath,
    $metadataHeaderPath,
    $metadataImplementationPath,
    $blockHeaderPath,
    (Join-Path $RepositoryRoot 'src\coreclr\gc\immix_block.cpp'),
    (Join-Path $scriptRoot 'reference-count-validation.cpp'),
    (Join-Path $scriptRoot 'reference-count-benchmark-native.cpp'),
    (Join-Path $scriptRoot 'run-reference-count-validation.ps1'),
    (Join-Path $scriptRoot 'run-reference-count-validation.sh'),
    (Join-Path $scriptRoot 'run-reference-count-runtime-smoke.ps1'),
    (Join-Path $scriptRoot 'run-reference-count-runtime-smoke.sh'),
    (Join-Path $scriptRoot 'run-reference-count-combined-exports.sh'),
    (Join-Path $scriptRoot 'run-reference-count-benchmark.ps1'),
    (Join-Path $scriptRoot 'run-reference-count-evidence.ps1'),
    (Join-Path $scriptRoot 'run-incomplete-coreroot-control.ps1'),
    (Join-Path $scriptRoot 'verify-reference-count-cross-cwd.ps1'),
    (Join-Path $scriptRoot 'verify-reference-count-gate.ps1'))) {
    Assert-Check (Test-Path -LiteralPath $path -PathType Leaf) "Required product or evidence source is missing: $path"
}

# --- product contract tokens ----------------------------------------------
$header = Get-Content -LiteralPath $headerPath -Raw
$implementation = Get-Content -LiteralPath $implementationPath -Raw
$test = Get-Content -LiteralPath $testPath -Raw
$export = Get-Content -LiteralPath $exportPath -Raw
$metadataHeader = Get-Content -LiteralPath $metadataHeaderPath -Raw
$metadataImplementation = Get-Content -LiteralPath $metadataImplementationPath -Raw
$blockHeader = Get-Content -LiteralPath $blockHeaderPath -Raw
foreach ($token in @(
    'static constexpr size_t ObjectAlignment = sizeof(uintptr_t);',
    'static constexpr size_t MinimumObjectSize = 3 * sizeof(uintptr_t);',
    'static_assert(ReferenceCountTable::MinimumObjectSize == 24);',
    'SideMetadataResult MarkStraddleLinesQuiescent(uintptr_t object, size_t size);',
    'SideMetadataResult ClearStraddleLinesQuiescent(uintptr_t object, size_t size);',
    'SideMetadataResult ResetObjectQuiescent(uintptr_t object, size_t size);',
    'SideMetadataResult IsContinuationLine(uintptr_t line, bool* continuation) const;',
    'SideMetadataResult IsSticky(uintptr_t object, bool* sticky) const;')) {
    Assert-Check ($header.Contains($token)) "Required product contract is missing: $token"
}
foreach ($token in @(
    '(referenceCount->log_bits_per_value < 1)',
    '(referenceCount->log_bits_per_value > 3)',
    'return static_cast<uint8_t>(1) << m_log_reference_count_bits;',
    ': (static_cast<uintptr_t>(1) << activeBits) - 1;',
    'IncrementSaturating',
    'DecrementNonZeroNonSaturated',
    'SideMetadataMemoryOrder::Relaxed',
    'if (size <= ImmixBlockGeometry::LineBytes)',
    '*first = (object & ~ImmixBlockGeometry::LineMask) +',
    '*end = (object + size) & ~ImmixBlockGeometry::LineMask;',
    'LxrSideMetadataKind::ReferenceCountStraddle',
    'SideMetadataResult::InconsistentState',
    'ValidateUnregisteredObjectRange',
    'return continuation',
    'result = SetValidObject(object, 0);',
    'ImmixBlockGeometry::IsBlockAligned(block)')) {
    Assert-Check ($implementation.Contains($token)) "Required implementation contract is missing: $token"
}
foreach ($token in @(
    'GC_ReferenceCountTest_Run',
    'ValidateOldReference',
    'old reference must not have a zero reference count')) {
    Assert-Check ($test.Contains($token)) "Required invariant-consumer contract is missing: $token"
}
Assert-Check ($export.Contains('GC_ReferenceCountTest_Run')) 'Unix export list is missing GC_ReferenceCountTest_Run.'
Assert-Check ($metadataHeader.Contains('IncrementSaturating')) 'P2.1 saturating increment is missing.'
Assert-Check ($metadataHeader.Contains('DecrementNonZeroNonSaturated')) 'P2.1 guarded decrement is missing.'
Assert-Check ($metadataHeader.Contains('ReferenceCountStraddle')) 'P2.1 straddle plane is missing.'
Assert-Check ($metadataHeader.Contains('InconsistentState')) 'P2.1 inconsistent-state result is missing.'
Assert-Check ($metadataImplementation.Contains('CompareExchangeWord')) 'P2.1 masked compare-exchange is missing.'
Assert-Check ($blockHeader.Contains('LineBytes')) 'P2.2 line geometry is missing.'

# --- benchmark harness contract -------------------------------------------
# The recorded benchmark rows only mean what the phase document claims if the
# harness still pins one invocation and one unrolled operation per iteration
# and the native batch still refuses a status that contradicts its label.
$benchmarkHarness = Get-Content -LiteralPath (
    Join-Path $scriptRoot 'run-reference-count-benchmark.ps1') -Raw
$benchmarkNative = Get-Content -LiteralPath (
    Join-Path $scriptRoot 'reference-count-benchmark-native.cpp') -Raw
foreach ($token in @("'--invocationCount', '1'", "'--unrollFactor', '1'")) {
    Assert-Check ($benchmarkHarness.Contains($token)) "Benchmark harness no longer pins: $token"
}
foreach ($token in @(
    'status != SideMetadataUpdateStatus::Updated',
    'status != SideMetadataUpdateStatus::Saturated')) {
    Assert-Check ($benchmarkNative.Contains($token)) "Benchmark native status check is missing: $token"
}

# --- source manifest -------------------------------------------------------
$sourcePaths = @(
    Get-Content -LiteralPath $sourceManifestPath |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
)
Assert-Check ($sourcePaths.Count -eq [int]$manifest.sourceManifestPaths) 'Source manifest path count differs.'
Assert-Check (($sourcePaths | Sort-Object -Unique).Count -eq $sourcePaths.Count) 'Source manifest contains duplicate paths.'
foreach ($relative in $sourcePaths) {
    Assert-Check `
        (Test-Path -LiteralPath (Join-Path $RepositoryRoot $relative) -PathType Leaf) `
        "Source manifest path is missing: $relative"
}
foreach ($required in $manifest.requiredSourceManifestPaths) {
    Assert-Check ($sourcePaths -ccontains $required) "Source manifest is incomplete: $required"
}

# --- raw evidence presence -------------------------------------------------
Assert-Check (Test-Path -LiteralPath $raw -PathType Container) 'Raw evidence directory is missing.'
foreach ($name in $manifest.rawFiles) {
    Assert-Check `
        (Test-Path -LiteralPath (Join-Path $raw $name) -PathType Leaf) `
        "Required raw evidence is missing: $name"
}

# --- raw evidence schemas --------------------------------------------------
# Every raw CSV must carry the columns the manifest declares. A recorded
# schema that nothing enforces would let a renamed or dropped column reach the
# archive without failing the audit.
$schemaNames = @($manifest.rawSchemas.PSObject.Properties.Name)
Assert-ExactSet `
    $schemaNames `
    @($manifest.rawFiles | Where-Object { $_ -like '*.csv' }) `
    'Declared raw schemas do not cover every raw CSV.'
foreach ($name in $schemaNames) {
    $columns = @(
        (Import-Csv (Join-Path $raw $name) | Select-Object -First 1).PSObject.Properties.Name
    )
    $declared = @($manifest.rawSchemas.$name)
    $absent = @($declared | Where-Object { $columns -notcontains $_ })
    Assert-Check ($absent.Count -eq 0) "Raw evidence schema differs for ${name}: $($absent -join ', ')"
}

# --- source and evidence identity -----------------------------------------
$sourceCommit = (Get-Content -LiteralPath (Join-Path $raw 'source-commit.txt') -Raw).Trim()
Assert-Check ($sourceCommit -match '^[0-9a-f]{40}$') 'Source commit identity is malformed.'

$topology = @(Import-Csv (Join-Path $raw 'topology-summary.csv'))
Assert-Check ($topology.Count -eq 1) 'Topology evidence row count differs.'
Assert-Check ($topology[0].source_commit -eq $sourceCommit) 'Recorded topology source commit differs.'
Assert-Check ($topology[0].evidence_commit -match '^[0-9a-f]{40}$') 'Recorded evidence commit identity is malformed.'
Assert-Truthy $topology[0].distinct 'Source and evidence commits must be distinct.'
Assert-Check ($topology[0].evidence_parent -eq $sourceCommit) 'Evidence commit is not a direct child of the source commit.'
Assert-Check ($topology[0].result -eq 'PASS') 'Recorded topology check did not pass.'

$hasGit = Test-Path -LiteralPath (Join-Path $RepositoryRoot '.git')
$gitSourceExists = $false
$gitEvidenceExists = $false
$gitHeadCommit = ''
$gitParentCommit = ''
$gitIsAncestor = $false
$gitSourcePathsUnchanged = $false
if ($hasGit) {
    try {
        & git -C $RepositoryRoot cat-file -e "$sourceCommit^{commit}" 2>$null
        $gitSourceExists = $LASTEXITCODE -eq 0
        & git -C $RepositoryRoot cat-file -e "$($topology[0].evidence_commit)^{commit}" 2>$null
        $gitEvidenceExists = $LASTEXITCODE -eq 0
        $gitHeadCommit = ((& git -C $RepositoryRoot rev-parse HEAD 2>$null) | Out-String).Trim()
        $gitParentCommit = ((& git -C $RepositoryRoot rev-parse 'HEAD^1' 2>$null) | Out-String).Trim()
        & git -C $RepositoryRoot merge-base --is-ancestor $sourceCommit HEAD 2>$null
        $gitIsAncestor = $LASTEXITCODE -eq 0
        & git -C $RepositoryRoot diff --quiet $sourceCommit HEAD -- $sourcePaths
        $gitSourcePathsUnchanged = $LASTEXITCODE -eq 0
    } catch {
        throw "Unable to inspect git topology in '$RepositoryRoot': $($_.Exception.Message)"
    }
}
Assert-Check ((-not $hasGit) -or $gitSourceExists) 'Source commit does not exist in this repository.'
Assert-Check ((-not $hasGit) -or ($gitHeadCommit -ne $sourceCommit)) 'Source and evidence commits must be distinct.'
Assert-Check ((-not $hasGit) -or ($gitParentCommit -eq $sourceCommit)) 'Evidence commit is not a direct child of the source commit.'
Assert-Check ((-not $hasGit) -or $gitIsAncestor) 'Source commit is not an ancestor of the evidence commit.'
# topology-summary.csv is itself part of the evidence commit, so it can never
# contain that commit's own hash: stamping the hash rewrites the commit. HEAD
# identity is therefore pinned by the four checks above - HEAD is the source
# commit's distinct direct child and descendant - and the recorded value is
# refused whenever it names some *other* commit that exists in this repository,
# which is the substitution this check exists to catch.
Assert-Check `
    ((-not $hasGit) -or
     (-not $gitEvidenceExists) -or
     ($topology[0].evidence_commit -eq $gitHeadCommit)) `
    'Recorded evidence commit differs from HEAD.'
Assert-Check ((-not $hasGit) -or $gitSourcePathsUnchanged) 'A source-manifest path changed in the evidence commit.'

$identities = @(Import-Csv (Join-Path $raw 'source-identities.csv'))
Assert-ExactSet $identities.path $sourcePaths 'Source identity path set mismatch.'
foreach ($identity in $identities) {
    $path = Join-Path $RepositoryRoot $identity.path
    $hash = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash
    Assert-Check ($hash -eq $identity.sha256) "Source identity mismatch: $($identity.path)"
}

# --- validation ------------------------------------------------------------
$validation = @(Import-Csv (Join-Path $raw 'validation-summary.csv'))
Assert-Check ($validation.Count -eq $manifest.validationPlatforms.Count) 'Validation platform evidence is incomplete.'
foreach ($expected in $manifest.validationPlatforms) {
    $row = @($validation | Where-Object platform -eq $expected.platform)
    Assert-Check ($row.Count -eq 1) "Validation row differs for $($expected.platform)."
    Assert-Check ([int]$row[0].passed -eq [int]$expected.total) "Validation pass count differs for $($expected.platform)."
    Assert-Check ([int]$row[0].total -eq [int]$expected.total) "Validation total differs for $($expected.platform)."
    Assert-Check ([int]$row[0].checks -gt 0) "Validation check count is missing for $($expected.platform)."
    Assert-Check ($row[0].result -eq 'PASS') "Validation result differs for $($expected.platform)."
}

# --- runtime smoke ---------------------------------------------------------
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

$runtimeIdentities = @(Import-Csv (Join-Path $raw 'runtime-identities.csv'))
Assert-ExactSet $runtimeIdentities.role $manifest.runtimeIdentityRoles 'Runtime identity role set differs.'
Assert-Check ($runtimeIdentities.Count -eq $manifest.runtimeIdentityRoles.Count) 'Runtime identity row count differs.'
foreach ($identity in $runtimeIdentities) {
    Assert-Check (-not [string]::IsNullOrWhiteSpace($identity.path)) "Runtime identity path is missing for $($identity.role)."
    Assert-Check ($identity.sha256 -match '^[0-9A-F]{64}$') "Runtime identity hash is malformed for $($identity.role)."
}

# --- combined exports and the default-off control -------------------------
$combinedExports = @(Import-Csv (Join-Path $raw 'combined-export-summary.csv'))
Assert-Check ($combinedExports.Count -eq $manifest.combinedExports.Count) 'Combined export evidence is incomplete.'
foreach ($symbol in $manifest.combinedExports) {
    $row = @($combinedExports | Where-Object symbol -eq $symbol)
    Assert-Check ($row.Count -eq 1) "Combined export row differs for $symbol."
    Assert-Check ($row[0].result -eq 'PASS') "Combined export failed for $symbol."
}

$defaultPath = @(Import-Csv (Join-Path $raw 'default-path-summary.csv'))
Assert-Check ($defaultPath.Count -eq [int]$manifest.defaultPathRows) 'Default-path evidence is incomplete.'
foreach ($property in $manifest.defaultPathProperties) {
    $row = @($defaultPath | Where-Object property -eq $property)
    Assert-Check ($row.Count -eq 1) "Default-path row is missing for $property."
    Assert-Check ($row[0].result -eq 'PASS') "Default-path control did not pass: $property"
}

# --- benchmark rows, controls, and identities -----------------------------
$widthRows = @(Import-Csv (Join-Path $raw 'benchmark-width-raw.csv'))
Assert-Check ($widthRows.Count -eq [int]$manifest.benchmark.widthRows) 'Benchmark width row count differs.'
foreach ($method in $manifest.benchmark.widthMethods) {
    foreach ($logBits in $manifest.benchmark.logReferenceCountBits) {
        $row = @($widthRows | Where-Object {
            ($_.Method -eq $method) -and
            ([int]$_.LogReferenceCountBits -eq [int]$logBits)
        })
        Assert-Check ($row.Count -eq 1) "Benchmark width row differs for $method/$logBits."
        Assert-Check (-not [string]::IsNullOrWhiteSpace($row[0].Mean)) "Benchmark width mean is missing for $method/$logBits."
    }
}

$contentionRows = @(Import-Csv (Join-Path $raw 'benchmark-contention-raw.csv'))
Assert-Check ($contentionRows.Count -eq [int]$manifest.benchmark.contentionRows) 'Benchmark contention row count differs.'
foreach ($method in $manifest.benchmark.contentionMethods) {
    foreach ($workerCount in $manifest.benchmark.workerCounts) {
        $row = @($contentionRows | Where-Object {
            ($_.Method -eq $method) -and
            ([int]$_.WorkerCount -eq [int]$workerCount)
        })
        Assert-Check ($row.Count -eq 1) "Benchmark contention row differs for $method/$workerCount."
        Assert-Check (-not [string]::IsNullOrWhiteSpace($row[0].Mean)) "Benchmark contention mean is missing for $method/$workerCount."
    }
}

$controls = @(Import-Csv (Join-Path $raw 'benchmark-controls.csv'))
Assert-Check ($controls.Count -eq [int]$manifest.benchmark.controls) 'Benchmark control count differs.'
foreach ($workerCount in $manifest.benchmark.workerCounts) {
    $row = @($controls | Where-Object { [int]$_.worker_count -eq [int]$workerCount })
    Assert-Check ($row.Count -eq 1) "Benchmark control row differs for $workerCount workers."
    Assert-Check ([double]$row[0].aa_ratio -ge [double]$manifest.benchmark.aaMinimum) 'Benchmark A/A ratio is below its bound.'
    Assert-Check ([double]$row[0].aa_ratio -le [double]$manifest.benchmark.aaMaximum) 'Benchmark A/A ratio is above its bound.'
    Assert-Check ([double]$row[0].extra_cas_ratio -ge [double]$manifest.benchmark.extraCasMinimum) 'Benchmark extra-CAS control is below its bound.'
    Assert-Check ([double]$row[0].delay_ratio -ge [double]$manifest.benchmark.delayMinimum) 'Benchmark injected-delay control is below its bound.'
    Assert-Check ($row[0].result -eq 'PASS') "Benchmark control did not pass for $workerCount workers."
}

$benchmarkIdentity = @(Import-Csv (Join-Path $raw 'benchmark-identity.csv'))
Assert-Check ($benchmarkIdentity.Count -eq 1) 'Benchmark identity row count differs.'
Assert-Falsy $benchmarkIdentity[0].quick_mode 'Benchmark evidence must not come from a quick run.'
Assert-Check ([int]$benchmarkIdentity[0].launch_count -eq [int]$manifest.benchmark.launchCount) 'Benchmark launch count differs.'
Assert-Check ([int]$benchmarkIdentity[0].warmup_count -eq [int]$manifest.benchmark.warmupCount) 'Benchmark warmup count differs.'
Assert-Check ([int]$benchmarkIdentity[0].iteration_count -eq [int]$manifest.benchmark.iterationCount) 'Benchmark iteration count differs.'
Assert-Check ([int]$benchmarkIdentity[0].invocation_count -eq [int]$manifest.benchmark.invocationCount) 'Benchmark invocation count differs.'
Assert-Check ([int]$benchmarkIdentity[0].unroll_factor -eq [int]$manifest.benchmark.unrollFactor) 'Benchmark unroll factor differs.'
Assert-Check ([int]$benchmarkIdentity[0].width_methods -eq $manifest.benchmark.widthMethods.Count) 'Benchmark width method count differs.'
Assert-Check ([int]$benchmarkIdentity[0].contention_methods -eq $manifest.benchmark.contentionMethods.Count) 'Benchmark contention method count differs.'
Assert-Check ([int]$benchmarkIdentity[0].rows -eq [int]$manifest.benchmark.rows) 'Benchmark identity row total differs.'
Assert-Check ([int]$benchmarkIdentity[0].expected_rows -eq [int]$manifest.benchmark.rows) 'Benchmark identity expected-row total differs.'
Assert-Check ($benchmarkIdentity[0].native_sha256 -match '^[0-9A-F]{64}$') 'Benchmark native identity is malformed.'
Assert-Check ($benchmarkIdentity[0].dotnet_sha256 -match '^[0-9A-F]{64}$') 'Benchmark SDK identity is malformed.'
Assert-Check (-not [string]::IsNullOrWhiteSpace($benchmarkIdentity[0].dotnet_version)) 'Benchmark SDK version is missing.'
Assert-Check (-not [string]::IsNullOrWhiteSpace($benchmarkIdentity[0].benchmark_runtime)) 'Benchmark runtime identity is missing.'
Assert-Check (-not [string]::IsNullOrWhiteSpace($benchmarkIdentity[0].launcher_framework)) 'Benchmark launcher framework identity is missing.'
Assert-Check (-not [string]::IsNullOrWhiteSpace($benchmarkIdentity[0].processor)) 'Benchmark processor identity is missing.'
Assert-Check (-not [string]::IsNullOrWhiteSpace($benchmarkIdentity[0].os)) 'Benchmark OS identity is missing.'
Assert-Check ($benchmarkIdentity[0].result -eq 'PASS') 'Benchmark identity result differs.'

# --- builds, platforms, and the portability controls ----------------------
$builds = @(Import-Csv (Join-Path $raw 'build-summary.csv'))
Assert-ExactSet $builds.name $manifest.buildNames 'Build evidence set differs.'
foreach ($build in $builds) {
    $expectedResult = if ($manifest.expectedFailBuilds -ccontains $build.name) {
        'EXPECTED-FAIL'
    } else {
        'PASS'
    }
    Assert-Check ($build.source_commit -eq $sourceCommit) "Build source identity differs for $($build.name)."
    Assert-Check ($build.result -eq $expectedResult) "Build result differs for $($build.name)."
}

$platforms = @(Import-Csv (Join-Path $raw 'platform-summary.csv'))
Assert-ExactSet $platforms.platform $manifest.platforms 'Platform evidence set differs.'
foreach ($platform in $platforms) {
    Assert-Check ($platform.build -eq 'PASS') "Platform build result differs for $($platform.platform)."
}
foreach ($expected in $manifest.validationPlatforms) {
    $platform = @($platforms | Where-Object platform -eq $expected.platform)
    $expectedValidation = "$($expected.total)/$($expected.total)"
    Assert-Check `
        (($platform.Count -eq 1) -and
         $platform[0].validation.StartsWith($expectedValidation)) `
        "Platform validation rollup differs for $($expected.platform)."
}

$crossCwd = @(Import-Csv (Join-Path $raw 'cross-cwd-summary.csv'))
Assert-Check ($crossCwd.Count -eq 1) 'Cross-CWD control row count differs.'
Assert-Check ([int]$crossCwd[0].benchmark_rows -eq [int]$manifest.benchmark.rows) 'Cross-CWD benchmark row total differs.'
Assert-Check ([int]$crossCwd[0].controls -eq [int]$manifest.benchmark.controls) 'Cross-CWD control total differs.'
Assert-Check ($crossCwd[0].result -eq 'PASS') 'Cross-CWD control did not pass.'

$coreRootControl = @(Import-Csv (Join-Path $raw 'core-root-control-summary.csv'))
Assert-Check ($coreRootControl.Count -eq 1) 'Incomplete Core_Root control row count differs.'
Assert-Check ([int]$coreRootControl[0].exit_code -ne 0) 'Incomplete Core_Root control must record a rejection.'
Assert-Check ($coreRootControl[0].result -eq 'PASS') 'Incomplete Core_Root control did not pass.'

$attempts = @(Import-Csv (Join-Path $raw 'attempt-summary.csv'))
Assert-Check ($attempts.Count -gt 0) 'Attempt evidence is missing.'
Assert-ExactSet $attempts.id $manifest.requiredAttemptIds 'Attempt evidence set differs.'

$invocation = @(Import-Csv (Join-Path $raw 'invocation.csv'))
Assert-Check ($invocation.Count -eq 1) 'Evidence invocation row count differs.'
Assert-Check ([int]$invocation[0].launch_count -eq [int]$manifest.benchmark.launchCount) 'Evidence invocation launch count differs.'
Assert-Check ([int]$invocation[0].warmup_count -eq [int]$manifest.benchmark.warmupCount) 'Evidence invocation warmup count differs.'
Assert-Check ([int]$invocation[0].iteration_count -eq [int]$manifest.benchmark.iterationCount) 'Evidence invocation iteration count differs.'
Assert-Check ([int]$invocation[0].validation_rows -eq $manifest.validationPlatforms.Count) 'Evidence invocation validation row count differs.'
Assert-Check (
    [int]$invocation[0].runtime_rows -eq
    ($manifest.runtimePlatforms.Count *
     $manifest.runtimeLinkages.Count *
     $manifest.runtimeGcModes.Count)) 'Evidence invocation runtime row count differs.'
Assert-Check ($invocation[0].result -eq 'PASS') 'Evidence invocation result differs.'

$checks++
if ($checks -ne [int]$manifest.verifierExpectedChecks) {
    throw "Verifier check count differs: $checks/$($manifest.verifierExpectedChecks)."
}
Write-Host "PASS: $checks P2.3 evidence checks"
