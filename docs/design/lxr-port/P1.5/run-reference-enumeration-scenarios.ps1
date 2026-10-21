# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

[CmdletBinding()]
param(
    [string]$RepositoryRoot,
    [string]$RuntimeRoot,
    [string]$OutputDirectory,
    [ValidateRange(3, 20)]
    [int]$Invocations = 3,
    [ValidateRange(0.1, 30.0)]
    [double]$WarmupSeconds = 0.5,
    [ValidateRange(0.5, 120.0)]
    [double]$DurationSeconds = 2.5
)

$ErrorActionPreference = 'Stop'
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
if (-not $RepositoryRoot) {
    $RepositoryRoot = (Resolve-Path (Join-Path $scriptRoot '..\..\..\..')).Path
}
if (-not $RuntimeRoot) {
    $RuntimeRoot = Join-Path $RepositoryRoot (
        'artifacts\tests\coreclr\windows.x64.Release\Tests\Core_Root')
}
if (-not $OutputDirectory) {
    $OutputDirectory = Join-Path $RepositoryRoot (
        'artifacts\p15-reference-enumeration-scenarios')
}

$dotnet = Join-Path $RepositoryRoot '.dotnet\dotnet.exe'
$workerProject = Join-Path $RepositoryRoot (
    'docs\design\lxr-port\harness\src\Lxr.Harness.Worker\' +
    'Lxr.Harness.Worker.csproj')
$workerDirectory = Join-Path $RepositoryRoot (
    'artifacts\lxr-harness\build\bin\Lxr.Harness.Worker\release')
$worker = Join-Path $workerDirectory 'Lxr.Harness.Worker.dll'
$corerun = Join-Path $RuntimeRoot 'corerun.exe'
$linkedHook = Join-Path $RuntimeRoot 'coreclr.dll'
$standaloneHook = Join-Path $RepositoryRoot (
    'artifacts\bin\coreclr\windows.x64.Release\clrgc.dll')
$sessionId = (
    [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ') + '-' +
    [Guid]::NewGuid().ToString('N'))
$sourceCommit = (& git -C $RepositoryRoot rev-parse HEAD).Trim()
if (($LASTEXITCODE -ne 0) -or
    ($sourceCommit -notmatch '^[0-9a-f]{40}$')) {
    throw 'Unable to identify the harness source commit.'
}
$sourceScopes = @(
    'docs\design\lxr-port\harness',
    'docs\design\lxr-port\P1.5\run-reference-enumeration-scenarios.ps1')
$sourceStatus = @(
    & git -C $RepositoryRoot status --porcelain -- @sourceScopes)
if (($LASTEXITCODE -ne 0) -or ($sourceStatus.Count -ne 0)) {
    throw (
        'Scenario harness sources must match the identified commit: ' +
        ($sourceStatus -join '; '))
}
$invocationRows = [Collections.Generic.List[object]]::new()
$controlRows = [Collections.Generic.List[object]]::new()

foreach ($path in @(
    $dotnet,
    $workerProject,
    $corerun,
    $linkedHook,
    $standaloneHook
)) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Required file not found: $path"
    }
}

& $dotnet build $workerProject -c Release --nologo
if ($LASTEXITCODE -ne 0) {
    throw 'Unable to build the LXR harness worker.'
}
if (-not (Test-Path -LiteralPath $worker -PathType Leaf)) {
    throw "Harness worker was not produced: $worker"
}

if (Test-Path -LiteralPath $OutputDirectory) {
    Remove-Item -LiteralPath $OutputDirectory -Recurse -Force
}
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
New-Item -ItemType Directory -Path (
    Join-Path $OutputDirectory 'reports') -Force | Out-Null
New-Item -ItemType Directory -Path (
    Join-Path $OutputDirectory 'samples') -Force | Out-Null
New-Item -ItemType Directory -Path (
    Join-Path $OutputDirectory 'logs') -Force | Out-Null

$variants = @(
    [pscustomobject]@{ Name = 'callback'; Mode = 1 },
    [pscustomobject]@{ Name = 'visitor'; Mode = 2 },
    [pscustomobject]@{ Name = 'cursor'; Mode = 3 }
)
$scenarios = @(
    [pscustomobject]@{
        Name = 'pointer-chasing'
        Parameters = @(
            'nodeCount=65536',
            'hops=64',
            'mutationsPerOperation=16',
            'allocationBytesPerOperation=1024')
    },
    [pscustomobject]@{
        Name = 'long-lived-cache'
        Parameters = @(
            'residentEntries=32768',
            'entrySizeBytes=512',
            'lookupsPerOperation=8')
    }
)
$configurations = @(
    [pscustomobject]@{
        Id = 'wks-linked'
        Name = 'wks'
        Arm = 'wks'
        Server = 'false'
        HeapCount = 1
        Deployment = 'linked'
    },
    [pscustomobject]@{
        Id = 'wks-standalone'
        Name = 'wks'
        Arm = 'wks'
        Server = 'false'
        HeapCount = 1
        Deployment = 'standalone'
    },
    [pscustomobject]@{
        Id = 'srv-linked'
        Name = 'srv'
        Arm = 'srv'
        Server = 'true'
        HeapCount = 8
        Deployment = 'linked'
    }
)

function Get-PropertyArguments([object]$gcMode) {
    $result = [Collections.Generic.List[string]]::new()
    $result.Add('-p')
    $result.Add("System.GC.Server=$($gcMode.Server)")
    $result.Add('-p')
    $result.Add('System.GC.Concurrent=true')
    if ($gcMode.Name -eq 'srv') {
        $result.Add('-p')
        $result.Add("System.GC.HeapCount=$($gcMode.HeapCount)")
    }
    return $result.ToArray()
}

function Get-WorkerArguments(
    [object]$scenario,
    [object]$configuration,
    [int]$seed,
    [string]$report,
    [string]$samples,
    [string]$deployment
) {
    $result = [Collections.Generic.List[string]]::new()
    foreach ($value in @(
        '--scenario', $scenario.Name,
        '--arm', $configuration.Arm,
        '--server-heap-count', $configuration.HeapCount.ToString(),
        '--seed', $seed.ToString(),
        '--workers', '1',
        '--warmup-seconds', $WarmupSeconds.ToString(
            'R',
            [Globalization.CultureInfo]::InvariantCulture),
        '--duration-seconds', $DurationSeconds.ToString(
            'R',
            [Globalization.CultureInfo]::InvariantCulture),
        '--mode', 'throughput',
        '--output', $report,
        '--samples', $samples,
        '--requested', "System.GC.Server=$($configuration.Server)",
        '--requested', 'System.GC.Concurrent=true',
        '--requested', 'DOTNET_ReadyToRun=0',
        '--requested', 'DOTNET_TieredCompilation=0'
    )) {
        $result.Add($value)
    }
    if ($configuration.Name -eq 'srv') {
        $result.Add('--requested')
        $result.Add("System.GC.HeapCount=$($configuration.HeapCount)")
    }
    if ($deployment -eq 'standalone') {
        $result.Add('--requested')
        $result.Add("System.GC.Path=$standaloneHook")
    }
    foreach ($parameter in $scenario.Parameters) {
        $result.Add('--param')
        $result.Add($parameter)
    }
    return $result.ToArray()
}

function Invoke-Scenario(
    [object]$scenario,
    [object]$configuration,
    [object]$variant,
    [int]$invocation,
    [int]$order,
    [bool]$ExpectSuccess = $true,
    [int]$ExpectedMode = 0
) {
    $seed =
        20260825 +
        ($scenario.Name -eq 'long-lived-cache' ? 1000 : 0) +
        ($configuration.Name -eq 'srv' ? 100 : 0) +
        ($configuration.Deployment -eq 'standalone' ? 10 : 0) +
        $invocation
    $id = (
        "$($scenario.Name)-$($configuration.Id)-" +
        "$($variant.Name)-$invocation")
    $report = Join-Path $OutputDirectory "reports\$id.json"
    $samples = Join-Path $OutputDirectory "samples\$id.samples.gz"
    $log = Join-Path $OutputDirectory "logs\$id.log"
    $hook = if ($configuration.Deployment -eq 'standalone') {
        $standaloneHook
    } else {
        $linkedHook
    }

    $env:CORE_LIBRARIES = $workerDirectory
    $env:DOTNET_ReadyToRun = '0'
    $env:DOTNET_TieredCompilation = '0'
    $env:DOTNET_GCObjectReferenceEnumerationTestMode =
        $variant.Mode.ToString()
    $env:P15_REFERENCE_ENUMERATION_HOOK_LIBRARY = $hook
    $env:P15_REFERENCE_ENUMERATION_EXPECTED_MODE = (
        $ExpectedMode -ne 0 ? $ExpectedMode : $variant.Mode).ToString()
    $env:P15_REFERENCE_ENUMERATION_EXPECTED_MODE_NAME = $variant.Name
    $fixedFullCollectionCount =
        $scenario.Name -eq 'pointer-chasing' ? 2 : 0
    $env:P15_REFERENCE_ENUMERATION_FIXED_FULL_COLLECTION_COUNT =
        $fixedFullCollectionCount.ToString()
    if ($configuration.Deployment -eq 'standalone') {
        $env:DOTNET_GCPath = $standaloneHook
    } else {
        Remove-Item Env:\DOTNET_GCPath -ErrorAction SilentlyContinue
    }

    $arguments = [Collections.Generic.List[string]]::new()
    foreach ($argument in Get-PropertyArguments $configuration) {
        $arguments.Add($argument)
    }
    $arguments.Add($worker)
    foreach ($argument in Get-WorkerArguments (
        $scenario) $configuration $seed $report $samples (
        $configuration.Deployment)) {
        $arguments.Add($argument)
    }

    $stopwatch = [Diagnostics.Stopwatch]::StartNew()
    & $corerun @arguments *> $log
    $exitCode = $LASTEXITCODE
    $stopwatch.Stop()
    $output = Get-Content -LiteralPath $log -Raw
    if (-not (Test-Path -LiteralPath $report -PathType Leaf)) {
        throw "Worker report missing: $id"
    }
    $workerReport = Get-Content -LiteralPath $report -Raw | ConvertFrom-Json
    $markerSeen = $output -match '(?m)^LXR-HARNESS-COMPLETE '

    if ($ExpectSuccess) {
        if (($exitCode -ne 0) -or
            -not $markerSeen -or
            -not $workerReport.valid -or
            -not $workerReport.collectorConfirmed -or
            -not $workerReport.verificationSuccess -or
            ($workerReport.referenceEnumeration.mode -ne $variant.Mode) -or
            ($workerReport.referenceEnumeration.errors -ne 0) -or
            ($workerReport.referenceEnumeration.objectScans -le 0) -or
            ($workerReport.referenceEnumeration.ranges -le 0) -or
            ($workerReport.referenceEnumeration.slots -le 0) -or
            ($workerReport.referenceEnumeration.nonNullSlots -le 0) -or
            ($workerReport.referenceEnumeration.window -ne
             'post-reset-through-telemetry-end') -or
            ($workerReport.gc.expectedInducedCollections -ne
             $fixedFullCollectionCount) -or
            ($workerReport.gc.inducedCollections -ne
             $fixedFullCollectionCount) -or
            ($workerReport.gc.gen0Collections -le 0) -or
            ($scenario.Name -eq 'pointer-chasing' -and
             $workerReport.gc.gen2Collections -lt
             $fixedFullCollectionCount) -or
            ($workerReport.observedGcConfig.ServerGC -ne $configuration.Server) -or
            ($workerReport.observedGcConfig.ConcurrentGC -ne 'true') -or
            ($configuration.Name -eq 'srv' -and
             $workerReport.observedGcConfig.HeapCount -ne '8') -or
            ($configuration.Deployment -eq 'standalone' -and
             $workerReport.observedGcConfig.GCPath -ne $standaloneHook)) {
            throw "Scenario validation failed: $id. See $log and $report."
        }
    }

    return [pscustomobject]@{
        Id = $id
        ExitCode = $exitCode
        WallSeconds = $stopwatch.Elapsed.TotalSeconds
        MarkerSeen = $markerSeen
        Marker = $workerReport.marker
        Report = $workerReport
        ReportPath = $report
        SamplesPath = $samples
        LogPath = $log
        Seed = $seed
        Order = $order
    }
}

try {
    foreach ($scenario in $scenarios) {
        foreach ($configuration in $configurations) {
            for ($invocation = 0;
                 $invocation -lt $Invocations;
                 $invocation++) {
                $rotation = $invocation % $variants.Count
                for ($order = 0; $order -lt $variants.Count; $order++) {
                    $variant = $variants[
                        ($order + $rotation) % $variants.Count]
                    $outcome = Invoke-Scenario (
                        $scenario) $configuration $variant (
                        $invocation) $order
                        $report = $outcome.Report
                        $reference = $report.referenceEnumeration
                        $invocationRows.Add([pscustomobject][ordered]@{
                            Session = $sessionId
                            Scenario = $scenario.Name
                            GC = $configuration.Name
                            Deployment = $configuration.Deployment
                            Variant = $variant.Name
                            RequestedMode = $variant.Mode
                            ObservedMode = $reference.mode
                            Invocation = $invocation
                            Order = $order
                            Seed = $outcome.Seed
                            OperationsPerSecond = [double](
                                $report.metrics.operationsPerSecond)
                            SteadyOperations = [int64](
                                $report.metrics.steadyOperations)
                            WallSeconds = $outcome.WallSeconds
                            ObjectScans = [int64]$reference.objectScans
                            Ranges = [int64]$reference.ranges
                            Slots = [int64]$reference.slots
                            NonNullSlots = [int64]$reference.nonNullSlots
                            ScanChecksum = [uint64]$reference.checksum
                            ScanErrors = [int]$reference.errors
                            Gen0 = [int]$report.gc.gen0Collections
                            Gen1 = [int]$report.gc.gen1Collections
                            Gen2 = [int]$report.gc.gen2Collections
                            ExpectedInduced = [int](
                                $report.gc.expectedInducedCollections)
                            Induced = [int]$report.gc.inducedCollections
                            RequestedServerGC = $configuration.Server
                            ObservedServerGC =
                                $report.observedGcConfig.ServerGC
                            RequestedConcurrentGC = 'true'
                            ObservedConcurrentGC =
                                $report.observedGcConfig.ConcurrentGC
                            RequestedHeapCount = $configuration.HeapCount
                            ObservedHeapCount =
                                $report.observedGcConfig.HeapCount
                            RequestedGCPath = if (
                                $configuration.Deployment -eq 'standalone') {
                                $standaloneHook
                            } else {
                                ''
                            }
                            ObservedGCPath =
                                $report.observedGcConfig.GCPath
                            CollectorConfirmed =
                                [bool]$report.collectorConfirmed
                            ReportValid = [bool]$report.valid
                            VerificationSuccess =
                                [bool]$report.verificationSuccess
                            CompletionMarker = $outcome.Marker
                            CoreClrSha256 =
                                $report.runtime.coreClrSha256
                            CoreClrFileVersion =
                                $report.runtime.coreClrFileVersion
                            HookSha256 = $reference.hookLibrarySha256
                            HookLibrary = $reference.hookLibraryPath
                            SamplePath = [IO.Path]::GetRelativePath(
                                $OutputDirectory,
                                $outcome.SamplesPath)
                            ReportPath = [IO.Path]::GetRelativePath(
                                $OutputDirectory,
                                $outcome.ReportPath)
                            ProcessCount = [int]$report.machine.processCount
                            Processor = $report.machine.processorName
                            LogicalCores = [int]$report.machine.logicalCores
                            OS = $report.machine.osDescription
                        })
                        Write-Host (
                            "PASS: {0}, {1:N0} ops/s, {2} objects, {3} slots" -f
                            $outcome.Id,
                            $report.metrics.operationsPerSecond,
                            $reference.objectScans,
                            $reference.slots)
                }
            }
        }
    }

    $controlScenario = $scenarios[0]
    $controlConfiguration = $configurations[0]
    $controlVariant = $variants[1]
    $control = Invoke-Scenario (
        $controlScenario) $controlConfiguration $controlVariant 99 0 (
        $false) 3
    $controlReport = $control.Report
    $controlFired =
        ($control.ExitCode -ne 0) -and
        $control.MarkerSeen -and
        -not $controlReport.valid -and
        ($controlReport.invalidReason -eq (
            'reference-enumeration-probe-failed')) -and
        (@($controlReport.referenceEnumerationFailures).Count -eq 1) -and
        ($controlReport.referenceEnumeration.mode -eq 2) -and
        ($controlReport.referenceEnumeration.expectedMode -eq 3) -and
        ($controlReport.gc.expectedInducedCollections -eq 2) -and
        ($controlReport.gc.inducedCollections -eq 2) -and
        ($controlReport.gc.gen2Collections -ge 2)
    if (-not $controlFired) {
        throw 'Reference-enumeration mode-mismatch control did not fire.'
    }
    $controlRows.Add([pscustomobject][ordered]@{
        Name = 'native-mode-mismatch'
        PerturbationCount = 1
        Expected = 'worker invalidates a native mode mismatch'
        Result = 'PASS'
        ExitCode = $control.ExitCode
        Evidence = [IO.Path]::GetRelativePath(
            $OutputDirectory,
            $control.ReportPath)
    })
} finally {
    foreach ($name in @(
        'CORE_LIBRARIES',
        'DOTNET_ReadyToRun',
        'DOTNET_TieredCompilation',
        'DOTNET_GCObjectReferenceEnumerationTestMode',
        'DOTNET_GCPath',
        'P15_REFERENCE_ENUMERATION_HOOK_LIBRARY',
        'P15_REFERENCE_ENUMERATION_EXPECTED_MODE',
        'P15_REFERENCE_ENUMERATION_EXPECTED_MODE_NAME',
        'P15_REFERENCE_ENUMERATION_FIXED_FULL_COLLECTION_COUNT'
    )) {
        Remove-Item "Env:\$name" -ErrorAction SilentlyContinue
    }
}

$expectedRows =
    $scenarios.Count *
    $configurations.Count *
    $variants.Count *
    $Invocations
if ($invocationRows.Count -ne $expectedRows) {
    throw "Produced $($invocationRows.Count) rows; expected $expectedRows."
}
$invocationPath = Join-Path $OutputDirectory 'scenario-invocations.csv'
$invocationRows |
    Export-Csv -LiteralPath $invocationPath -NoTypeInformation

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

$summaryRows = [Collections.Generic.List[object]]::new()
foreach ($scenario in $scenarios.Name) {
    foreach ($configuration in $configurations) {
        $gc = $configuration.Name
        $deployment = $configuration.Deployment
            $group = @($invocationRows | Where-Object {
                ($_.Scenario -eq $scenario) -and
                ($_.GC -eq $gc) -and
                ($_.Deployment -eq $deployment)
            })
            foreach ($variant in @('visitor', 'cursor')) {
                $ratios = [Collections.Generic.List[double]]::new()
                for ($invocation = 0;
                     $invocation -lt $Invocations;
                     $invocation++) {
                    $baseline = @($group | Where-Object {
                        ($_.Variant -eq 'callback') -and
                        ([int]$_.Invocation -eq $invocation)
                    })
                    $candidate = @($group | Where-Object {
                        ($_.Variant -eq $variant) -and
                        ([int]$_.Invocation -eq $invocation)
                    })
                    if (($baseline.Count -ne 1) -or
                        ($candidate.Count -ne 1)) {
                        throw "Pair cardinality failed: $scenario/$gc/$deployment/$variant/$invocation"
                    }
                    $ratios.Add(
                        [double]$candidate[0].OperationsPerSecond /
                        [double]$baseline[0].OperationsPerSecond)
                }
                $ratioArray = $ratios.ToArray()
                $point = Get-GeometricMean $ratioArray
                $bootstrapSeed = 20260825 + $summaryRows.Count
                $bootstrap = Get-BootstrapInterval (
                    $ratioArray) $bootstrapSeed
                $summaryRows.Add([pscustomobject][ordered]@{
                    Session = $sessionId
                    Scenario = $scenario
                    GC = $gc
                    Deployment = $deployment
                    Baseline = 'callback'
                    Variant = $variant
                    Invocations = $Invocations
                    RatioStatistic = 'paired-geometric-mean'
                    Ratio = $point
                    RatioCiLow = $bootstrap.Low
                    RatioCiHigh = $bootstrap.High
                    CiMethod = 'paired-bootstrap-percentile-95'
                    BootstrapResamples = 10000
                    BootstrapSeed = $bootstrapSeed
                })
            }
    }
}

$summaryRows |
    Export-Csv -LiteralPath (
        Join-Path $OutputDirectory 'scenario-summary.csv') -NoTypeInformation
$controlRows |
    Export-Csv -LiteralPath (
        Join-Path $OutputDirectory 'scenario-controls.csv') -NoTypeInformation

$runtimeVersions = @(
    $invocationRows.CoreClrFileVersion |
    Sort-Object -Unique)
if (($runtimeVersions.Count -ne 1) -or
    ($runtimeVersions[0] -notmatch '@Commit: (?<commit>[0-9a-f]{40})')) {
    throw 'Unable to identify one exact scenario runtime commit.'
}
$runtimeCommit = $Matches.commit
$identityRows = foreach ($path in @(
    $linkedHook,
    $standaloneHook,
    $worker
)) {
    [pscustomobject][ordered]@{
        Name = [IO.Path]::GetFileName($path)
        Path = $path
        Sha256 = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash
        Length = (Get-Item -LiteralPath $path).Length
        SourceCommit = $path -eq $worker ? $sourceCommit : $runtimeCommit
    }
}
$identityRows |
    Export-Csv -LiteralPath (
        Join-Path $OutputDirectory 'scenario-identities.csv') -NoTypeInformation

Write-Host (
    "PASS: {0} invocations, {1} paired summaries, {2} controls" -f
    $invocationRows.Count,
    $summaryRows.Count,
    $controlRows.Count)
$global:LASTEXITCODE = 0
