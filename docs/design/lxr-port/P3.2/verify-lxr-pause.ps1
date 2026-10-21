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

$spec = Get-Content -LiteralPath (Join-Path $scriptRoot 'mechanism-spec.json') -Raw |
    ConvertFrom-Json
$manifest = Get-Content -LiteralPath (Join-Path $scriptRoot 'evidence-manifest.json') -Raw |
    ConvertFrom-Json
$sourceManifestPath = Join-Path $scriptRoot 'source-manifest.txt'
$raw = Join-Path $scriptRoot 'raw'
foreach ($path in @($sourceManifestPath, $raw)) {
    Assert-Condition (Test-Path -LiteralPath $path) "Required evidence input is missing: $path"
}

$pauseHeader = Get-Content -LiteralPath (
    Join-Path $RepositoryRoot 'src\coreclr\gc\lxr_pause_schedule.h') -Raw
$pauseSource = Get-Content -LiteralPath (
    Join-Path $RepositoryRoot 'src\coreclr\gc\lxr_pause_schedule.cpp') -Raw
$enumMatch = [regex]::Match(
    $pauseHeader,
    'enum class LxrPauseKind.*?\{(?<body>.*?)\};',
    [Text.RegularExpressions.RegexOptions]::Singleline)
Assert-Condition $enumMatch.Success 'LxrPauseKind enum was not found.'
$enumNames = @(
    [regex]::Matches(
        $enumMatch.Groups['body'].Value,
        '(?m)^\s*(?<name>[A-Za-z0-9_]+),\s*$') |
        ForEach-Object { $_.Groups['name'].Value } |
        Where-Object { $_ -ne 'Count' }
)
$expectedKinds = @(
    $spec.pauseKinds |
        Sort-Object ordinal |
        ForEach-Object name
)
Assert-Condition (
    ($enumNames -join '|') -ceq ($expectedKinds -join '|')) (
    "Pause enum order differs: '$($enumNames -join ',')'.")
foreach ($kind in $expectedKinds) {
    Assert-Condition (
        $pauseSource.Contains('"' + $kind + '"')) (
        "Pause name table is missing '$kind'.")
}

$allStages = @($spec.stwStages)
foreach ($pause in $spec.pauseKinds) {
    $enabled = @($pause.enabledStages)
    Assert-Condition (
        $enabled.Count -eq ($enabled | Sort-Object -Unique).Count) (
        "Pause '$($pause.name)' has duplicate enabled stages.")
    foreach ($stage in $enabled) {
        Assert-Condition (
            $allStages -ccontains $stage) (
            "Pause '$($pause.name)' enables unknown stage '$stage'.")
    }
}
$refCount = $spec.pauseKinds | Where-Object name -eq 'RefCount'
foreach ($forbidden in @('Prepare', 'Closure', 'STWRCDecsAndSweep')) {
    Assert-Condition (
        @($refCount.enabledStages) -cnotcontains $forbidden) (
        "RefCount unexpectedly enables '$forbidden'.")
}

$sourcePaths = @(
    Get-Content -LiteralPath $sourceManifestPath |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
)
Assert-Condition (
    $sourcePaths.Count -eq ($sourcePaths | Sort-Object -Unique).Count) (
    'Source manifest contains duplicate paths.')
foreach ($path in $sourcePaths) {
    Assert-Condition (
        Test-Path -LiteralPath (Join-Path $RepositoryRoot $path) -PathType Leaf) (
        "Source manifest path is missing: $path")
}

$identities = @(Import-Csv (Join-Path $raw 'source-identities.csv'))
Assert-Condition (
    (($identities.path | Sort-Object) -join '|') -ceq
        (($sourcePaths | Sort-Object) -join '|')) (
    'Source identity path set mismatch.')
foreach ($identity in $identities) {
    $actual = (Get-FileHash -Algorithm SHA256 (
        Join-Path $RepositoryRoot $identity.path)).Hash.ToLowerInvariant()
    Assert-Condition ($actual -ceq $identity.sha256.ToLowerInvariant()) (
        "Source identity mismatch: $($identity.path)")
}

$validation = @(Import-Csv (Join-Path $raw 'validation-summary.csv'))
Assert-Condition (
    (($validation.platform | Sort-Object) -join '|') -ceq
        ((@($manifest.validationPlatforms) | Sort-Object) -join '|')) (
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
    (($runtimeActual | Sort-Object) -join '|') -ceq
        (($runtimeExpected | Sort-Object) -join '|')) (
    'Runtime smoke evidence is incomplete.')
Assert-Condition (
    @($runtime | Where-Object result -ne 'PASS').Count -eq 0) (
    'A runtime smoke row failed.')

$build = @(Import-Csv (Join-Path $raw 'build-summary.csv'))
Assert-Condition (
    (($build.id | Sort-Object) -join '|') -ceq
        ((@($manifest.buildEvidence) | Sort-Object) -join '|')) (
    'Build evidence is incomplete.')
Assert-Condition (
    @($build | Where-Object result -ne 'PASS').Count -eq 0) (
    'A required build row failed.')

$benchmarkRaw = @(Import-Csv (Join-Path $raw 'benchmark-raw.csv'))
$expectedRawKeys = foreach ($scenario in $manifest.benchmark.scenarios) {
    foreach ($mode in $manifest.benchmark.gcModes) {
        foreach ($policy in $manifest.benchmark.policies) {
            for ($launch = 1; $launch -le [int]$manifest.benchmark.launches; $launch++) {
                foreach ($kind in $spec.pauseKinds.name) {
                    "$scenario|$mode|$policy|$launch|$kind"
                }
            }
        }
    }
}
$actualRawKeys = @($benchmarkRaw | ForEach-Object {
    "$($_.scenario)|$($_.gc_mode)|$($_.policy)|$($_.launch)|$($_.pause_kind)"
})
Assert-Condition (
    $actualRawKeys.Count -eq ($actualRawKeys | Sort-Object -Unique).Count) (
    'Benchmark raw rows contain duplicate composite keys.')
Assert-Condition (
    (($actualRawKeys | Sort-Object) -join '|') -ceq
        (($expectedRawKeys | Sort-Object) -join '|')) (
    'Benchmark raw composite key set differs.')
Assert-Condition (
    @($benchmarkRaw | Where-Object result -ne 'PASS').Count -eq 0) (
    'A benchmark raw row failed.')
$nullRows = @($benchmarkRaw | Where-Object policy -eq 'null')
Assert-Condition (
    @($nullRows | Where-Object {
        [uint64]$_.completed_kind_count -ne 0 -or
        [uint64]$_.synthetic_suspends -ne 0
    }).Count -eq 0) (
    'A null benchmark row reported synthetic suspension work.')
foreach ($row in $benchmarkRaw) {
    $expectedCompleted = switch ($row.policy) {
        'scheduled' { [uint64]$manifest.benchmark.countPerKind }
        'same-code' { [uint64]$manifest.benchmark.countPerKind * 2 }
        'injected' { [uint64]$manifest.benchmark.countPerKind * 2 }
        'null' { 0 }
        default { throw "Unknown benchmark policy '$($row.policy)'." }
    }
    Assert-Condition (
        [int]$row.requested_count -eq [int]$manifest.benchmark.countPerKind -and
        [int]$row.sample_count -eq [int]$manifest.benchmark.countPerKind -and
        [uint64]$row.completed_kind_count -eq $expectedCompleted -and
        [double]$row.heap_factor -eq [double]$manifest.benchmark.heapFactor) (
        "Benchmark row configuration differs: $($row.scenario)/$($row.policy)/$($row.pause_kind).")
}

$benchmarkSummary = @(Import-Csv (Join-Path $raw 'benchmark-summary.csv'))
$expectedSummaryKeys = foreach ($scenario in $manifest.benchmark.scenarios) {
    foreach ($mode in $manifest.benchmark.gcModes) {
        foreach ($policy in $manifest.benchmark.policies) {
            foreach ($kind in $spec.pauseKinds.name) {
                "$scenario|$mode|$policy|$kind"
            }
        }
    }
}
$actualSummaryKeys = @($benchmarkSummary | ForEach-Object {
    "$($_.scenario)|$($_.gc_mode)|$($_.policy)|$($_.pause_kind)"
})
Assert-Condition (
    $actualSummaryKeys.Count -eq ($actualSummaryKeys | Sort-Object -Unique).Count) (
    'Benchmark summary contains duplicate composite keys.')
Assert-Condition (
    (($actualSummaryKeys | Sort-Object) -join '|') -ceq
        (($expectedSummaryKeys | Sort-Object) -join '|')) (
    'Benchmark summary composite key set differs.')
Assert-Condition (
    @($benchmarkSummary | Where-Object result -ne 'PASS').Count -eq 0) (
    'A benchmark summary row failed.')

$controls = @(Import-Csv (Join-Path $raw 'benchmark-controls.csv'))
$expectedControlKeys = foreach ($scenario in $manifest.benchmark.scenarios) {
    foreach ($mode in $manifest.benchmark.gcModes) {
        foreach ($kind in $spec.pauseKinds.name) {
            "$scenario|$mode|$kind"
        }
    }
}
$actualControlKeys = @($controls | ForEach-Object {
    "$($_.scenario)|$($_.gc_mode)|$($_.pause_kind)"
})
Assert-Condition (
    $actualControlKeys.Count -eq ($actualControlKeys | Sort-Object -Unique).Count) (
    'Benchmark controls contain duplicate composite keys.')
Assert-Condition (
    (($actualControlKeys | Sort-Object) -join '|') -ceq
        (($expectedControlKeys | Sort-Object) -join '|')) (
    'Benchmark control composite key set differs.')
foreach ($control in $controls) {
    Assert-Condition (
        [double]$control.aa_ratio -ge [double]$manifest.benchmark.aaLower -and
        [double]$control.aa_ratio -le [double]$manifest.benchmark.aaUpper) (
        'Benchmark A/A ratio is outside its bounds.')
    Assert-Condition (
        $control.null_clean -eq 'True') (
        'Benchmark null control reported work.')
    Assert-Condition (
        [double]$control.injected_median_delta_ms -ge
            [double]$manifest.benchmark.injectedMedianDeltaMinimumMilliseconds) (
        'Benchmark injected-duration delta is below its bound.')
    Assert-Condition ($control.result -eq 'PASS') (
        'A benchmark control row failed.')
}

$invocation = @(Import-Csv (Join-Path $raw 'benchmark-invocation.csv'))
Assert-Condition ($invocation.Count -eq 1) (
    'Benchmark invocation cardinality differs.')
$invocationRow = $invocation[0]
Assert-Condition (
    [int]$invocationRow.launch_count -eq [int]$manifest.benchmark.launches -and
    [double]$invocationRow.heap_factor -eq [double]$manifest.benchmark.heapFactor -and
    [int]$invocationRow.count_per_kind -eq [int]$manifest.benchmark.countPerKind -and
    [int]$invocationRow.injected_microseconds -eq
        [int]$manifest.benchmark.injectedMicroseconds -and
    [double]$invocationRow.warmup_seconds -eq
        [double]$manifest.benchmark.warmupSeconds -and
    [double]$invocationRow.duration_seconds -eq
        [double]$manifest.benchmark.durationSeconds -and
    $invocationRow.result -eq 'PASS') (
    'Benchmark invocation configuration differs.')
Assert-Condition (
    ((@($invocationRow.scenarios -split ';') | Sort-Object) -join '|') -ceq
        ((@($manifest.benchmark.scenarios) | Sort-Object) -join '|') -and
    ((@($invocationRow.gc_modes -split ';') | Sort-Object) -join '|') -ceq
        ((@($manifest.benchmark.gcModes) | Sort-Object) -join '|') -and
    ((@($invocationRow.policies -split ';') | Sort-Object) -join '|') -ceq
        ((@($manifest.benchmark.policies) | Sort-Object) -join '|')) (
    'Benchmark invocation identity sets differ.')

$sourceCommit = (Get-Content -LiteralPath (
    Join-Path $raw 'source-commit.txt') -Raw).Trim()
Assert-Condition ($sourceCommit -match '^[0-9a-f]{40}$') (
    'Source commit identity is malformed.')
git -C $RepositoryRoot rev-parse --is-inside-work-tree *> $null
if ($LASTEXITCODE -eq 0) {
    git -C $RepositoryRoot cat-file -e "$sourceCommit`^{commit}"
    Assert-Condition ($LASTEXITCODE -eq 0) 'Source commit does not exist.'
}

$mapping = @(Import-Csv (Join-Path $raw 'mapping-summary.csv'))
Assert-Condition (
    $mapping.Count -eq 1 -and
    $mapping[0].short_root -eq 'P:\' -and
    $mapping[0].removed -eq 'True') (
    'Temporary short-root mapping cleanup is not proven.')

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

Write-Host "PASS: $checks P3.2 evidence checks"
