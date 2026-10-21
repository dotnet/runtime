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
    Assert-Check (
        ($actualValues.Count -eq $expectedValues.Count) -and
        (($actualValues -join "`n") -ceq ($expectedValues -join "`n"))
    ) $Message
}

$manifest = Get-Content (Join-Path $scriptRoot 'evidence-manifest.json') -Raw | ConvertFrom-Json
$spec = Get-Content (Join-Path $scriptRoot 'mechanism-spec.json') -Raw | ConvertFrom-Json
$raw = Join-Path $scriptRoot 'raw'
$sourceManifest = Join-Path $scriptRoot 'source-manifest.txt'

Assert-Check ($manifest.schemaVersion -eq 1) 'Evidence schema differs.'
Assert-Check ($spec.schemaVersion -eq 1) 'Mechanism schema differs.'
Assert-Check ($spec.dependencyCommit -eq '17ac93d30d5ebc08aeda0ddf24e562d506d72230') 'Dependency differs.'
Assert-Check ($spec.oracle.core -eq '304ce69d43aae87de501111fceb8cbd33173a03a') 'Oracle differs.'
Assert-Check ($spec.threshold.bytes -eq 32768) 'Threshold differs.'
Assert-Check ($spec.threshold.predicate -eq 'greater-than-or-equal') 'Threshold predicate differs.'
Assert-Check $spec.threshold.oracleCorrection 'Oracle correction is not recorded.'
Assert-Check ($spec.geometry.pageBytes -eq 4096) 'Page geometry differs.'
Assert-Check ($spec.metadata.markNurseryBits -eq 2) 'LOS state width differs.'
Assert-Check ($spec.metadata.pageReuseBits -eq 8) 'Page reuse width differs.'

$header = Get-Content (Join-Path $RepositoryRoot 'src\coreclr\gc\large_object_space.h') -Raw
$implementation = Get-Content (Join-Path $RepositoryRoot 'src\coreclr\gc\large_object_space.cpp') -Raw
$metadata = Get-Content (Join-Path $RepositoryRoot 'src\coreclr\gc\side_metadata.cpp') -Raw
$benchmarkProgram = Get-Content (
    Join-Path $RepositoryRoot 'docs\design\lxr-port\P2.5\large-object-space-benchmark\Program.cs') -Raw
Assert-Check ($header.Contains('MinimumObjectBytes = ImmixBlockGeometry::BlockBytes')) 'Threshold alias missing.'
Assert-Check ($header.Contains('PageBytes = 4 * 1024')) 'Page size missing.'
Assert-Check ($implementation.Contains('return objectBytes >= MinimumObjectBytes;')) 'Inclusive threshold missing.'
Assert-Check ($implementation.Contains('VirtualDecommit')) 'Independent decommit missing.'
Assert-Check ($implementation.Contains('memset(reinterpret_cast<void*>(candidate.page_start), 0, committedBytes)')) 'Zero-before-publication missing.'
Assert-Check ($implementation.Contains('LargeObjectPageReuse')) 'Page reuse update missing.'
Assert-Check ($metadata.Contains('ADD_SPEC(LargeObjectMarkNursery, Local, 1, 1, 3')) 'LOS state plane missing.'
Assert-Check (
    $benchmarkProgram.Contains(
        "private const int Operations = $($spec.benchmark.operationsPerInvoke);")
) 'Benchmark operation count source differs.'

$sourcePaths = @(
    Get-Content $sourceManifest |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
)
Assert-Check ($sourcePaths.Count -gt 0) 'Source manifest is empty.'
Assert-Check (($sourcePaths | Sort-Object -Unique).Count -eq $sourcePaths.Count) 'Source manifest duplicates paths.'
foreach ($relative in $sourcePaths) {
    Assert-Check (Test-Path (Join-Path $RepositoryRoot $relative) -PathType Leaf) "Missing source: $relative"
}

Assert-Check (Test-Path $raw -PathType Container) 'Raw evidence directory is missing.'
foreach ($name in $manifest.rawFiles) {
    Assert-Check (Test-Path (Join-Path $raw $name) -PathType Leaf) "Missing raw evidence: $name"
}

$sourceCommit = (Get-Content (Join-Path $raw 'source-commit.txt') -Raw).Trim()
Assert-Check ($sourceCommit -match '^[0-9a-f]{40}$') 'Source commit is malformed.'
$identities = @(Import-Csv (Join-Path $raw 'source-identities.csv'))
Assert-Check ($identities.Count -eq $sourcePaths.Count) 'Source identity count differs.'
$actualIdentityPaths = @($identities.path | Sort-Object -Unique)
$expectedIdentityPaths = @($sourcePaths | Sort-Object -Unique)
Assert-Check (
    ($actualIdentityPaths.Count -eq $expectedIdentityPaths.Count) -and
    (($actualIdentityPaths -join "`n") -ceq ($expectedIdentityPaths -join "`n"))
) 'Source identity path set differs.'
$identityRoot = $RepositoryRoot
$identityScratch = $null
if (Test-Path (Join-Path $RepositoryRoot '.git')) {
    $identityScratch = Join-Path ([IO.Path]::GetTempPath()) (
        'p25-identity-' + [guid]::NewGuid().ToString('N'))
    $identityTree = Join-Path $identityScratch 'tree'
    New-Item -ItemType Directory -Path $identityTree -Force | Out-Null
    $identityArchive = Join-Path $identityScratch 'tree.tar'
    git -C $RepositoryRoot `
        -c core.autocrlf=false `
        -c core.eol=lf `
        archive --format=tar --output=$identityArchive $sourceCommit -- $sourcePaths
    Assert-Check ($LASTEXITCODE -eq 0) 'Unable to create canonical source archive.'
    Add-Type -AssemblyName System.Formats.Tar
    [System.Formats.Tar.TarFile]::ExtractToDirectory(
        $identityArchive,
        $identityTree,
        $false)
    $identityRoot = $identityTree
}
try {
    foreach ($identity in $identities) {
        $hash = (Get-FileHash (Join-Path $identityRoot $identity.path) -Algorithm SHA256).Hash
        Assert-Check ($hash -eq $identity.sha256) "Source identity differs: $($identity.path)"
    }
} finally {
    if (($null -ne $identityScratch) -and (Test-Path $identityScratch)) {
        Remove-Item -LiteralPath $identityScratch -Recurse -Force
    }
}

$validation = @(Import-Csv (Join-Path $raw 'validation-summary.csv'))
Assert-Check ($validation.Count -eq $manifest.validationPlatforms.Count) 'Validation rows differ.'
foreach ($expected in $manifest.validationPlatforms) {
    $row = @($validation | Where-Object platform -eq $expected.platform)
    Assert-Check ($row.Count -eq 1) "Validation platform missing: $($expected.platform)"
    Assert-Check ([int]$row[0].passed -eq [int]$expected.total) "Validation count differs: $($expected.platform)"
    Assert-Check ([int]$row[0].total -eq [int]$expected.total) "Validation total differs: $($expected.platform)"
    Assert-Check ($row[0].result -eq 'PASS') "Validation failed: $($expected.platform)"
}

$runtime = @(Import-Csv (Join-Path $raw 'runtime-smoke-summary.csv'))
Assert-Check ($runtime.Count -eq $manifest.runtimeRows) 'Runtime smoke rows differ.'
Assert-Check (@($runtime | Where-Object result -ne 'PASS').Count -eq 0) 'Runtime smoke contains failure.'
foreach ($linkage in @('linked', 'standalone')) {
    foreach ($gcMode in @('Workstation', 'Server')) {
        Assert-Check (@($runtime | Where-Object {
            ($_.linkage -eq $linkage) -and ($_.gc_mode -eq $gcMode)
        }).Count -eq 1) "Runtime smoke identity missing: $linkage/$gcMode"
    }
}

$benchmark = @(Import-Csv (Join-Path $raw 'benchmark-raw.csv'))
Assert-Check ($benchmark.Count -eq $manifest.benchmarkRows) 'Benchmark rows differ.'
Assert-Check (@($benchmark | Where-Object { -not $_.Mean }).Count -eq 0) 'Benchmark row lacks mean.'
$expectedBenchmarkKeys = foreach ($method in $spec.benchmark.methods) {
    foreach ($workerCount in $spec.benchmark.workerCounts) {
        "$method|$workerCount"
    }
}
$actualBenchmarkKeys = @($benchmark | ForEach-Object { "$($_.Method)|$($_.WorkerCount)" })
Assert-ExactSet $actualBenchmarkKeys $expectedBenchmarkKeys 'Benchmark matrix differs.'
foreach ($row in $benchmark) {
    Assert-Check ([int]$row.LaunchCount -eq [int]$spec.benchmark.launchCount) 'Raw benchmark launch count differs.'
    Assert-Check ([int]$row.WarmupCount -eq [int]$spec.benchmark.warmupCount) 'Raw benchmark warmup count differs.'
    Assert-Check ([int]$row.IterationCount -eq [int]$spec.benchmark.iterationCount) 'Raw benchmark iteration count differs.'
}
$benchmarkIdentity = @(Import-Csv (Join-Path $raw 'benchmark-identity.csv'))
Assert-Check ($benchmarkIdentity.Count -eq 1) 'Benchmark identity differs.'
Assert-Check ([int]$benchmarkIdentity[0].rows -eq $manifest.benchmarkRows) 'Benchmark identity row count differs.'
Assert-Check ([int]$benchmarkIdentity[0].launch_count -eq [int]$spec.benchmark.launchCount) 'Benchmark launch count differs.'
Assert-Check ([int]$benchmarkIdentity[0].warmup_count -eq [int]$spec.benchmark.warmupCount) 'Benchmark warmup count differs.'
Assert-Check ([int]$benchmarkIdentity[0].iteration_count -eq [int]$spec.benchmark.iterationCount) 'Benchmark iteration count differs.'
Assert-Check (
    [int]$benchmarkIdentity[0].operations_per_invoke -eq
        [int]$spec.benchmark.operationsPerInvoke
) 'Benchmark operation count differs.'
Assert-Check ($benchmarkIdentity[0].native_sha256 -match '^[0-9A-F]{64}$') 'Benchmark native identity differs.'
$controls = @(Import-Csv (Join-Path $raw 'benchmark-controls.csv'))
Assert-Check ($controls.Count -eq 3) 'Benchmark controls differ.'
Assert-ExactSet $controls.worker_count $spec.benchmark.workerCounts 'Benchmark control workers differ.'
foreach ($control in $controls) {
    Assert-Check ([double]$control.aa_ratio -ge [double]$spec.benchmark.aaMinimum) 'A/A below bound.'
    Assert-Check ([double]$control.aa_ratio -le [double]$spec.benchmark.aaMaximum) 'A/A above bound.'
    Assert-Check ([double]$control.extra_update_ratio -ge [double]$spec.benchmark.extraUpdateMinimum) 'Extra update control below bound.'
    Assert-Check ([double]$control.injected_cost_ratio -ge [double]$spec.benchmark.injectedCostMinimum) 'Injected cost control below bound.'
    Assert-Check ($control.result -eq 'PASS') 'Benchmark control result differs.'
}

$builds = @(Import-Csv (Join-Path $raw 'build-summary.csv'))
$expectedBuildKeys = @($spec.builds | ForEach-Object { "$($_.name)|$($_.result)" })
$actualBuildKeys = @($builds | ForEach-Object { "$($_.name)|$($_.result)" })
Assert-ExactSet $actualBuildKeys $expectedBuildKeys 'Build evidence set differs.'
foreach ($build in $builds) {
    Assert-Check ($build.status_sha256 -match '^[0-9A-F]{64}$') "Build identity differs: $($build.name)"
}
$defaultOff = @(Import-Csv (Join-Path $raw 'default-off-summary.csv'))
Assert-Check ($defaultOff.Count -eq 2) 'Default-off binary rows differ.'
Assert-Check (@($defaultOff | Where-Object {
    ($_.p25_source_compiled -ne 'False') -or
    ($_.p25_hook_present -ne 'False') -or
    ($_.result -ne 'PASS')
}).Count -eq 0) 'Default-off proof failed.'
$pressure = @(Import-Csv (Join-Path $raw 'large-object-pressure-summary.csv'))
Assert-Check ($pressure.Count -eq 4) 'Large-object pressure rows differ.'
$expectedPressureKeys = @(
    'p25.preedit.large-object-pressure.sdk.a|wks',
    'p25.preedit.large-object-pressure.sdk.a|srv',
    'p25.preedit.large-object-pressure.sdk.b|wks',
    'p25.preedit.large-object-pressure.sdk.b|srv'
)
$actualPressureKeys = @($pressure | ForEach-Object { "$($_.run)|$($_.collector)" })
Assert-ExactSet $actualPressureKeys $expectedPressureKeys 'Large-object pressure identities differ.'
foreach ($row in $pressure) {
    Assert-Check ([int]$row.invocations -eq 5) 'Large-object pressure invocation count differs.'
    Assert-Check ([double]$row.operations_per_second -gt 0) 'Large-object pressure throughput differs.'
    Assert-Check ($row.source_sha256 -match '^[0-9A-F]{64}$') 'Large-object pressure source identity differs.'
    Assert-Check ($row.result -eq 'PASS') 'Large-object pressure result differs.'
}

Write-Host "PASS: $checks P2.5 evidence checks"
