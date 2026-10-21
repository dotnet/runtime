# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

[CmdletBinding()]
param(
    [string]$RepositoryRoot,
    [string]$InputDirectory,
    [string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
if (-not $RepositoryRoot) {
    $RepositoryRoot = (Resolve-Path (Join-Path $scriptRoot '..\..\..\..')).Path
}
if (-not $InputDirectory) {
    $InputDirectory = Join-Path $RepositoryRoot (
        'artifacts\p15-reference-enumeration-scenarios')
}
if (-not $OutputDirectory) {
    $OutputDirectory = Join-Path $scriptRoot 'raw'
}

$invocationPath = Join-Path $InputDirectory 'scenario-invocations.csv'
$summaryPath = Join-Path $InputDirectory 'scenario-summary.csv'
$controlPath = Join-Path $InputDirectory 'scenario-controls.csv'
$identityPath = Join-Path $InputDirectory 'scenario-identities.csv'
foreach ($path in @(
    $invocationPath,
    $summaryPath,
    $controlPath,
    $identityPath
)) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Required scenario artifact not found: $path"
    }
}

$rows = @(Import-Csv -LiteralPath $invocationPath)
$summaries = @(Import-Csv -LiteralPath $summaryPath)
$controls = @(Import-Csv -LiteralPath $controlPath)
$identities = @(Import-Csv -LiteralPath $identityPath)

if ($rows.Count -ne 90) {
    throw "Scenario invocation data has $($rows.Count) rows; expected 90."
}
if ($summaries.Count -ne 12) {
    throw "Scenario summary has $($summaries.Count) rows; expected 12."
}
if (($controls.Count -ne 1) -or
    ($controls[0].Name -ne 'native-mode-mismatch') -or
    ($controls[0].PerturbationCount -ne '1') -or
    ($controls[0].Result -ne 'PASS')) {
    throw 'Scenario control evidence is incomplete.'
}
if ($identities.Count -ne 3) {
    throw "Scenario identities have $($identities.Count) rows; expected 3."
}

$sessions = @($rows.Session | Sort-Object -Unique)
if ($sessions.Count -ne 1) {
    throw "Scenario rows contain $($sessions.Count) sessions; expected one."
}
$runtimeVersions = @($rows.CoreClrFileVersion | Sort-Object -Unique)
if (($runtimeVersions.Count -ne 1) -or
    ($runtimeVersions[0] -notmatch '@Commit: (?<commit>[0-9a-f]{40})')) {
    throw 'Scenario rows do not identify one exact runtime commit.'
}
$runtimeCommit = $Matches.commit
$processors = @($rows.Processor | Sort-Object -Unique)
$logicalCoreCounts = @($rows.LogicalCores | Sort-Object -Unique)
$operatingSystems = @($rows.OS | Sort-Object -Unique)
if (($processors.Count -ne 1) -or
    ($logicalCoreCounts.Count -ne 1) -or
    ($operatingSystems.Count -ne 1)) {
    throw 'Scenario rows do not identify one machine configuration.'
}

$modeByVariant = @{
    callback = 1
    visitor = 2
    cursor = 3
}
$expectedConfigurations = @(
    'wks|linked',
    'wks|standalone',
    'srv|linked')
$repositoryPrefix = (
    [IO.Path]::GetFullPath($RepositoryRoot).TrimEnd('\') + '\')

function ConvertTo-RepositoryRelativePath([string]$path) {
    if (-not $path) {
        return ''
    }
    if (-not [IO.Path]::IsPathRooted($path)) {
        return $path
    }
    if ($path.StartsWith(
        $repositoryPrefix,
        [StringComparison]::OrdinalIgnoreCase)) {
        return $path.Substring($repositoryPrefix.Length)
    }
    if ($path -match '^[A-Za-z]:\\(?<relative>artifacts\\.+)$') {
        return $Matches.relative
    }
    throw "Cannot make scenario path repository-relative: $path"
}

function Get-GeometricMean([double[]]$values) {
    return [Math]::Exp(
        ($values |
            ForEach-Object { [Math]::Log($_) } |
            Measure-Object -Average).Average)
}

function Get-BootstrapInterval(
    [double[]]$values,
    [int]$seed
) {
    $resamples = 10000
    $random = [Random]::new($seed)
    $samples = [double[]]::new($resamples)
    for ($sample = 0; $sample -lt $resamples; $sample++) {
        $draw = [double[]]::new($values.Length)
        for ($index = 0; $index -lt $values.Length; $index++) {
            $draw[$index] = $values[$random.Next($values.Length)]
        }
        $samples[$sample] = Get-GeometricMean $draw
    }
    [Array]::Sort($samples)
    return [pscustomobject]@{
        Low = $samples[249]
        High = $samples[9749]
    }
}

foreach ($row in $rows) {
    $configuration = "$($row.GC)|$($row.Deployment)"
    $expectedInducedCollections =
        $row.Scenario -eq 'pointer-chasing' ? 2 : 0
    if (($row.Scenario -notin @('pointer-chasing', 'long-lived-cache')) -or
        ($configuration -notin $expectedConfigurations) -or
        ($row.Variant -notin @('callback', 'visitor', 'cursor')) -or
        ([int]$row.RequestedMode -ne $modeByVariant[$row.Variant]) -or
        ([int]$row.ObservedMode -ne $modeByVariant[$row.Variant]) -or
        ([int]$row.ScanErrors -ne 0) -or
        ([int64]$row.ObjectScans -le 0) -or
        ([int64]$row.Ranges -le 0) -or
        ([int64]$row.Slots -le 0) -or
        ([int64]$row.NonNullSlots -le 0) -or
        ([uint64]$row.ScanChecksum -eq 0) -or
        ([double]$row.OperationsPerSecond -le 0) -or
        ([int64]$row.SteadyOperations -le 0) -or
        ([int]$row.Gen0 -le 0) -or
        ([int]$row.ExpectedInduced -ne $expectedInducedCollections) -or
        ([int]$row.Induced -ne $expectedInducedCollections) -or
        (($row.Scenario -eq 'pointer-chasing') -and
         (([int]$row.Gen2 -lt $expectedInducedCollections) -or
          ([int64]$row.ObjectScans -lt 131072) -or
          ([int64]$row.NonNullSlots -lt 262144))) -or
        ($row.RequestedServerGC -ne $row.ObservedServerGC) -or
        ($row.RequestedConcurrentGC -ne $row.ObservedConcurrentGC) -or
        ($row.CollectorConfirmed -ne 'True') -or
        ($row.ReportValid -ne 'True') -or
        ($row.VerificationSuccess -ne 'True') -or
        -not $row.CompletionMarker -or
        ($row.CoreClrSha256 -notmatch '^[0-9a-f]{64}$') -or
        ($row.HookSha256 -notmatch '^[0-9a-f]{64}$') -or
        -not $row.SamplePath -or
        -not $row.ReportPath) {
        throw "Invalid scenario row: $($row.Id)"
    }
    if (($row.GC -eq 'srv') -and
        (([int]$row.RequestedHeapCount -ne 8) -or
         ([int]$row.ObservedHeapCount -ne 8))) {
        throw "Server heap count was not pinned: $($row.Id)"
    }
    if (($row.Deployment -eq 'standalone') -and
        ($row.RequestedGCPath -ne $row.ObservedGCPath)) {
        throw "Standalone GC path was not confirmed: $($row.Id)"
    }

    $reportPath = Join-Path $InputDirectory $row.ReportPath
    $samplePath = Join-Path $InputDirectory $row.SamplePath
    if (-not (Test-Path -LiteralPath $reportPath -PathType Leaf) -or
        -not (Test-Path -LiteralPath $samplePath -PathType Leaf)) {
        throw "Scenario raw artifacts are missing: $($row.Id)"
    }
    $report = Get-Content -LiteralPath $reportPath -Raw | ConvertFrom-Json
    if ($report.referenceEnumeration.window -ne
        'post-reset-through-telemetry-end') {
        throw "Scenario scan window is invalid: $($row.Id)"
    }
    $row | Add-Member -NotePropertyName WarmupSeconds -NotePropertyValue (
        [double]$report.warmupSeconds)
    $row | Add-Member -NotePropertyName SteadyStateSeconds -NotePropertyValue (
        [double]$report.steadyStateSeconds)
    $row | Add-Member -NotePropertyName (
        'RequestedDynamicAdaptationMode') -NotePropertyValue (
        $row.GC -eq 'srv' ? '0' : '')
    $row | Add-Member -NotePropertyName (
        'ObservedDynamicAdaptationMode') -NotePropertyValue (
        [string]$report.observedGcConfig.GCDynamicAdaptationMode)
    $row | Add-Member -NotePropertyName ScanWindow -NotePropertyValue (
        [string]$report.referenceEnumeration.window)
    if (($row.GC -eq 'srv') -and
        ($row.ObservedDynamicAdaptationMode -ne '0')) {
        throw "Server DATAS mode was not pinned: $($row.Id)"
    }

    foreach ($property in @(
        'RequestedGCPath',
        'ObservedGCPath',
        'HookLibrary'
    )) {
        if ($row.$property) {
            $row.$property = ConvertTo-RepositoryRelativePath $row.$property
        }
    }
}

$controlReportPath = Join-Path $InputDirectory $controls[0].Evidence
if (-not (Test-Path -LiteralPath $controlReportPath -PathType Leaf)) {
    throw "Scenario control report is missing: $controlReportPath"
}
$controlReport = Get-Content -LiteralPath $controlReportPath -Raw |
    ConvertFrom-Json
if (($controls[0].ExitCode -ne '1') -or
    ($controlReport.scenario -ne 'pointer-chasing') -or
    ($controlReport.collectorConfirmed -ne $true) -or
    ($controlReport.valid -ne $false) -or
    ($controlReport.invalidReason -ne
     'reference-enumeration-probe-failed') -or
    ($controlReport.verificationSuccess -ne $true) -or
    (@($controlReport.referenceEnumerationFailures).Count -ne 1) -or
    ($controlReport.referenceEnumerationFailures[0] -ne
     'reference-enumeration mode expected 3, observed 2') -or
    ([int]$controlReport.referenceEnumeration.expectedMode -ne 3) -or
    ([int]$controlReport.referenceEnumeration.mode -ne 2) -or
    ([int]$controlReport.referenceEnumeration.errors -ne 0) -or
    ([int64]$controlReport.referenceEnumeration.objectScans -le 0) -or
    ([int64]$controlReport.referenceEnumeration.ranges -le 0) -or
    ([int64]$controlReport.referenceEnumeration.slots -le 0) -or
    ([int64]$controlReport.referenceEnumeration.nonNullSlots -le 0) -or
    ([uint64]$controlReport.referenceEnumeration.checksum -eq 0) -or
    ($controlReport.referenceEnumeration.window -ne
     'post-reset-through-telemetry-end') -or
    ($controlReport.runtime.coreClrFileVersion -notmatch
     [regex]::Escape($runtimeCommit)) -or
    ($controlReport.runtime.coreClrSha256 -ne $rows[0].CoreClrSha256) -or
    ([int]$controlReport.gc.expectedInducedCollections -ne 2) -or
    ([int]$controlReport.gc.inducedCollections -ne 2) -or
    ([int]$controlReport.gc.gen2Collections -lt 2) -or
    ([int64]$controlReport.referenceEnumeration.objectScans -lt 131072) -or
    ([int64]$controlReport.referenceEnumeration.nonNullSlots -lt 262144)) {
    throw 'Scenario mode-mismatch control report is invalid.'
}
$controlDetailRows = @(
    [pscustomobject][ordered]@{
        Session = $sessions[0]
        Name = 'native-mode-mismatch'
        Scenario = $controlReport.scenario
        ExitCode = [int]$controls[0].ExitCode
        CollectorConfirmed = $controlReport.collectorConfirmed
        ReportValid = $controlReport.valid
        InvalidReason = $controlReport.invalidReason
        VerificationSuccess = $controlReport.verificationSuccess
        Failure = $controlReport.referenceEnumerationFailures[0]
        ExpectedMode = [int]$controlReport.referenceEnumeration.expectedMode
        ObservedMode = [int]$controlReport.referenceEnumeration.mode
        ScanErrors = [int]$controlReport.referenceEnumeration.errors
        ObjectScans = [int64]$controlReport.referenceEnumeration.objectScans
        Ranges = [int64]$controlReport.referenceEnumeration.ranges
        Slots = [int64]$controlReport.referenceEnumeration.slots
        NonNullSlots = [int64]$controlReport.referenceEnumeration.nonNullSlots
        ScanChecksum = [uint64]$controlReport.referenceEnumeration.checksum
        ScanWindow = $controlReport.referenceEnumeration.window
        HookLibrary = ConvertTo-RepositoryRelativePath (
            $controlReport.referenceEnumeration.hookLibraryPath)
        HookSha256 = $controlReport.referenceEnumeration.hookLibrarySha256
        CoreClrFileVersion = $controlReport.runtime.coreClrFileVersion
        RuntimeCommit = $runtimeCommit
        CompletionMarker = $controlReport.marker
        ExpectedInducedCollections = [int](
            $controlReport.gc.expectedInducedCollections)
        InducedCollections = [int]$controlReport.gc.inducedCollections
        Gen2Collections = [int]$controlReport.gc.gen2Collections
    }
)
$controls[0].Evidence = 'scenario-control-detail.csv'

foreach ($scenario in @('pointer-chasing', 'long-lived-cache')) {
    foreach ($configuration in $expectedConfigurations) {
        $parts = $configuration.Split('|')
        foreach ($variant in @('callback', 'visitor', 'cursor')) {
            $group = @($rows | Where-Object {
                ($_.Scenario -eq $scenario) -and
                ($_.GC -eq $parts[0]) -and
                ($_.Deployment -eq $parts[1]) -and
                ($_.Variant -eq $variant)
            })
            if ($group.Count -ne 5) {
                throw "$scenario/$configuration/$variant has $($group.Count) rows; expected 5."
            }
            foreach ($invocation in 0..4) {
                $pair = @($group | Where-Object {
                    [int]$_.Invocation -eq $invocation
                })
                if ($pair.Count -ne 1) {
                    throw "$scenario/$configuration/$variant invocation $invocation is not unique."
                }
            }
        }
        foreach ($invocation in 0..4) {
            $pair = @($rows | Where-Object {
                ($_.Scenario -eq $scenario) -and
                ($_.GC -eq $parts[0]) -and
                ($_.Deployment -eq $parts[1]) -and
                ([int]$_.Invocation -eq $invocation)
            })
            if ((@($pair.Seed | Sort-Object -Unique).Count -ne 1) -or
                (@($pair.Order | Sort-Object -Unique).Count -ne 3) -or
                (@($pair.Variant | Sort-Object -Unique).Count -ne 3)) {
                throw "$scenario/$configuration invocation $invocation is not a complete interleaved pair."
            }
        }
    }
}

for ($summaryIndex = 0;
     $summaryIndex -lt $summaries.Count;
     $summaryIndex++) {
    $summary = $summaries[$summaryIndex]
    if (($summary.Session -ne $sessions[0]) -or
        ($summary.Scenario -notin @('pointer-chasing', 'long-lived-cache')) -or
        ("$($summary.GC)|$($summary.Deployment)" -notin $expectedConfigurations) -or
        ($summary.Baseline -ne 'callback') -or
        ($summary.Variant -notin @('visitor', 'cursor')) -or
        ([int]$summary.Invocations -ne 5) -or
        ($summary.RatioStatistic -ne 'paired-geometric-mean') -or
        ([double]$summary.Ratio -le 0) -or
        ([double]$summary.RatioCiLow -le 0) -or
        ([double]$summary.RatioCiLow -gt [double]$summary.Ratio) -or
        ([double]$summary.RatioCiHigh -lt [double]$summary.Ratio) -or
        ($summary.CiMethod -ne 'paired-bootstrap-percentile-95') -or
        ([int]$summary.BootstrapResamples -ne 10000)) {
        throw "Invalid scenario summary row: $($summary.Scenario)/$($summary.GC)/$($summary.Deployment)/$($summary.Variant)"
    }

    $ratios = [Collections.Generic.List[double]]::new()
    foreach ($invocation in 0..4) {
        $baseline = @($rows | Where-Object {
            ($_.Scenario -eq $summary.Scenario) -and
            ($_.GC -eq $summary.GC) -and
            ($_.Deployment -eq $summary.Deployment) -and
            ($_.Variant -eq 'callback') -and
            ([int]$_.Invocation -eq $invocation)
        })
        $candidate = @($rows | Where-Object {
            ($_.Scenario -eq $summary.Scenario) -and
            ($_.GC -eq $summary.GC) -and
            ($_.Deployment -eq $summary.Deployment) -and
            ($_.Variant -eq $summary.Variant) -and
            ([int]$_.Invocation -eq $invocation)
        })
        if (($baseline.Count -ne 1) -or ($candidate.Count -ne 1)) {
            throw "Scenario summary pair is incomplete: $($summary.Scenario)/$($summary.GC)/$($summary.Deployment)/$($summary.Variant)/$invocation"
        }
        $ratios.Add(
            [double]$candidate[0].OperationsPerSecond /
            [double]$baseline[0].OperationsPerSecond)
    }
    $ratioArray = $ratios.ToArray()
    $bootstrapSeed = 20260825 + $summaryIndex
    $point = Get-GeometricMean $ratioArray
    $bootstrap = Get-BootstrapInterval $ratioArray $bootstrapSeed
    if (([Math]::Abs($point - [double]$summary.Ratio) -gt 1e-12) -or
        ([Math]::Abs(
            $bootstrap.Low - [double]$summary.RatioCiLow) -gt 1e-12) -or
        ([Math]::Abs(
            $bootstrap.High - [double]$summary.RatioCiHigh) -gt 1e-12)) {
        throw "Scenario summary was not derived from invocation data: $($summary.Scenario)/$($summary.GC)/$($summary.Deployment)/$($summary.Variant)"
    }
    if ($summary.PSObject.Properties.Name -contains 'BootstrapSeed') {
        if ([int]$summary.BootstrapSeed -ne $bootstrapSeed) {
            throw "Scenario summary has the wrong bootstrap seed: $($summary.Scenario)/$($summary.GC)/$($summary.Deployment)/$($summary.Variant)"
        }
    } else {
        $summary | Add-Member -NotePropertyName BootstrapSeed -NotePropertyValue (
            $bootstrapSeed)
    }
}

$scanRows = [Collections.Generic.List[object]]::new()
foreach ($group in @($rows | Group-Object Scenario, GC, Deployment, Variant)) {
    $values = $group.Group
    $scanRows.Add([pscustomobject][ordered]@{
        Session = $sessions[0]
        Scenario = $values[0].Scenario
        GC = $values[0].GC
        Deployment = $values[0].Deployment
        Variant = $values[0].Variant
        Invocations = $values.Count
        ObjectScans = (
            $values.ObjectScans |
            ForEach-Object { [int64]$_ } |
            Measure-Object -Sum).Sum
        Ranges = (
            $values.Ranges |
            ForEach-Object { [int64]$_ } |
            Measure-Object -Sum).Sum
        Slots = (
            $values.Slots |
            ForEach-Object { [int64]$_ } |
            Measure-Object -Sum).Sum
        NonNullSlots = (
            $values.NonNullSlots |
            ForEach-Object { [int64]$_ } |
            Measure-Object -Sum).Sum
        Gen0Collections = (
            $values.Gen0 |
            ForEach-Object { [int]$_ } |
            Measure-Object -Sum).Sum
        Gen1Collections = (
            $values.Gen1 |
            ForEach-Object { [int]$_ } |
            Measure-Object -Sum).Sum
        Gen2Collections = (
            $values.Gen2 |
            ForEach-Object { [int]$_ } |
            Measure-Object -Sum).Sum
        InducedCollections = (
            $values.Induced |
            ForEach-Object { [int]$_ } |
            Measure-Object -Sum).Sum
        ExpectedInducedCollections = (
            $values.ExpectedInduced |
            ForEach-Object { [int]$_ } |
            Measure-Object -Sum).Sum
        MinObjectScans = (
            $values.ObjectScans |
            ForEach-Object { [int64]$_ } |
            Measure-Object -Minimum).Minimum
        MaxObjectScans = (
            $values.ObjectScans |
            ForEach-Object { [int64]$_ } |
            Measure-Object -Maximum).Maximum
        MinSlots = (
            $values.Slots |
            ForEach-Object { [int64]$_ } |
            Measure-Object -Minimum).Minimum
        MaxSlots = (
            $values.Slots |
            ForEach-Object { [int64]$_ } |
            Measure-Object -Maximum).Maximum
    })
}
if ($scanRows.Count -ne 18) {
    throw "Scan summary has $($scanRows.Count) rows; expected 18."
}

$identityByName = @{}
foreach ($identity in $identities) {
    if (($identity.Sha256 -notmatch '^[0-9A-F]{64}$') -or
        ([int64]$identity.Length -le 0) -or
        ($identity.SourceCommit -notmatch '^[0-9a-f]{40}$')) {
        throw "Invalid scenario identity: $($identity.Name)"
    }
    $identity.Path = ConvertTo-RepositoryRelativePath $identity.Path
    $identityByName[$identity.Name] = $identity
}
if (($identityByName['coreclr.dll'].Sha256.ToLowerInvariant() -ne
     $rows[0].CoreClrSha256.ToLowerInvariant()) -or
    -not $identityByName.ContainsKey('clrgc.dll') -or
    -not $identityByName.ContainsKey('Lxr.Harness.Worker.dll')) {
    throw 'Scenario identity hashes do not match invocation data.'
}
if (($identityByName['coreclr.dll'].SourceCommit -ne $runtimeCommit) -or
    ($identityByName['clrgc.dll'].SourceCommit -ne $runtimeCommit)) {
    throw 'Scenario runtime identities do not match the runtime commit.'
}
$harnessSourceCommit =
    $identityByName['Lxr.Harness.Worker.dll'].SourceCommit

$sessionRows = @(
    [pscustomobject][ordered]@{
        Session = $sessions[0]
        RuntimeCommit = $runtimeCommit
        HarnessSourceCommit = $harnessSourceCommit
        Invocations = 5
        InvocationRows = 90
        SummaryRows = 12
        ControlRows = 1
        WarmupSeconds = [double]$rows[0].WarmupSeconds
        SteadyStateSeconds = [double]$rows[0].SteadyStateSeconds
        Configurations = ($expectedConfigurations -join ',')
        BuildConfiguration = 'Release-noPGO'
        RequestedReadyToRun = 0
        RequestedTieredCompilation = 0
        ScanWindow = 'post-reset-through-telemetry-end'
        PointerChasingFixedFullCollections = 2
        LongLivedCacheFixedFullCollections = 0
        RatioStatistic = 'paired-geometric-mean'
        CiMethod = 'paired-bootstrap-percentile-95'
        BootstrapResamples = 10000
        Processor = $processors[0]
        LogicalCores = [int]$logicalCoreCounts[0]
        OS = $operatingSystems[0]
    }
)
$matrixRows = @(
    [pscustomobject][ordered]@{
        GC = 'wks'
        Deployment = 'linked'
        Status = 'MEASURED'
        HeapCount = 1
        RequestedDynamicAdaptationMode = ''
        ObservedDynamicAdaptationMode = ''
        Reason = 'Built-in Workstation GC identity is observable.'
    },
    [pscustomobject][ordered]@{
        GC = 'wks'
        Deployment = 'standalone'
        Status = 'MEASURED'
        HeapCount = 1
        RequestedDynamicAdaptationMode = ''
        ObservedDynamicAdaptationMode = ''
        Reason = 'Standalone Workstation GC path and identity are observable.'
    },
    [pscustomobject][ordered]@{
        GC = 'srv'
        Deployment = 'linked'
        Status = 'MEASURED'
        HeapCount = 8
        RequestedDynamicAdaptationMode = 0
        ObservedDynamicAdaptationMode = 0
        Reason = 'Built-in Server GC accepts and reports eight pinned heaps.'
    },
    [pscustomobject][ordered]@{
        GC = 'srv'
        Deployment = 'standalone'
        Status = 'EXCLUDED'
        HeapCount = 8
        RequestedDynamicAdaptationMode = 0
        ObservedDynamicAdaptationMode = 1
        Reason = (
            'Pilot readback reported DynamicAdaptationMode=1; ' +
            'the pinned eight-heap identity requires 0.')
    }
)

New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$shippedRows = @(
    $rows |
    Select-Object -Property * -ExcludeProperty SamplePath, ReportPath)
$shippedRows | Export-Csv -LiteralPath (
    Join-Path $OutputDirectory 'scenario-invocations.csv') -NoTypeInformation
$summaries | Export-Csv -LiteralPath (
    Join-Path $OutputDirectory 'scenario-summary.csv') -NoTypeInformation
$scanRows | Sort-Object Scenario, GC, Deployment, Variant |
    Export-Csv -LiteralPath (
        Join-Path $OutputDirectory 'scenario-scan-summary.csv') -NoTypeInformation
$controls | Export-Csv -LiteralPath (
    Join-Path $OutputDirectory 'scenario-controls.csv') -NoTypeInformation
$controlDetailRows | Export-Csv -LiteralPath (
    Join-Path $OutputDirectory 'scenario-control-detail.csv') -NoTypeInformation
$identities | Export-Csv -LiteralPath (
    Join-Path $OutputDirectory 'scenario-identities.csv') -NoTypeInformation
$sessionRows | Export-Csv -LiteralPath (
    Join-Path $OutputDirectory 'scenario-session.csv') -NoTypeInformation
$matrixRows | Export-Csv -LiteralPath (
    Join-Path $OutputDirectory 'scenario-matrix.csv') -NoTypeInformation

Write-Host (
    "PASS: {0} invocations, {1} ratios, {2} scan groups, commit {3}" -f
    $rows.Count,
    $summaries.Count,
    $scanRows.Count,
    $runtimeCommit)
