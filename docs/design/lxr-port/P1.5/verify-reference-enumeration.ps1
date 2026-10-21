# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

[CmdletBinding()]
param(
    [string]$RepositoryRoot
)

$ErrorActionPreference = 'Stop'
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
if (-not $RepositoryRoot) {
    $RepositoryRoot = (Resolve-Path (Join-Path $scriptRoot '..\..\..\..')).Path
}

$failures = [Collections.Generic.List[string]]::new()
$checks = 0

function Confirm([bool]$condition, [string]$message) {
    $script:checks++
    if (-not $condition) {
        $script:failures.Add($message)
    }
}

function Get-ScenarioBootstrapInterval(
    [double[]]$values,
    [int]$seed
) {
    $resamples = 10000
    $random = [Random]::new($seed)
    $samples = [double[]]::new($resamples)
    for ($sample = 0; $sample -lt $resamples; $sample++) {
        $logSum = 0.0
        for ($index = 0; $index -lt $values.Length; $index++) {
            $logSum += [Math]::Log(
                $values[$random.Next($values.Length)])
        }
        $samples[$sample] = [Math]::Exp($logSum / $values.Length)
    }
    [Array]::Sort($samples)
    return [pscustomobject]@{
        Low = $samples[249]
        High = $samples[9749]
    }
}

function Require-Pattern(
    [string]$relativePath,
    [string]$pattern
) {
    $path = Join-Path $RepositoryRoot $relativePath
    Confirm (Test-Path -LiteralPath $path -PathType Leaf) "Missing $relativePath"
    if (Test-Path -LiteralPath $path -PathType Leaf) {
        Confirm ([bool](Select-String -LiteralPath $path -SimpleMatch $pattern -Quiet)) (
            "$relativePath does not contain '$pattern'")
    }
}

function Require-PatternCount(
    [string]$relativePath,
    [string]$pattern,
    [int]$expected
) {
    $path = Join-Path $RepositoryRoot $relativePath
    Confirm (Test-Path -LiteralPath $path -PathType Leaf) "Missing $relativePath"
    if (Test-Path -LiteralPath $path -PathType Leaf) {
        $actual = @(Select-String -LiteralPath $path -SimpleMatch $pattern).Count
        Confirm ($actual -eq $expected) (
            "$relativePath contains '$pattern' $actual times; expected $expected.")
    }
}

function Forbid-Pattern(
    [string]$relativePath,
    [string]$pattern
) {
    $path = Join-Path $RepositoryRoot $relativePath
    Confirm (Test-Path -LiteralPath $path -PathType Leaf) "Missing $relativePath"
    if (Test-Path -LiteralPath $path -PathType Leaf) {
        Confirm (-not [bool](
            Select-String -LiteralPath $path -SimpleMatch $pattern -Quiet)) (
            "$relativePath unexpectedly contains '$pattern'")
    }
}

$document = Join-Path $RepositoryRoot (
    'docs\design\lxr-port\P1.5-object-reference-enumeration.md')
$rawRoot = Join-Path $scriptRoot 'raw'
$validation = Join-Path $rawRoot 'validation-summary.csv'
$compatibility = Join-Path $rawRoot 'compatibility-summary.csv'
$controls = Join-Path $rawRoot 'control-summary.csv'
$malformed = Join-Path $rawRoot 'malformed-summary.csv'
$enabled = Join-Path $rawRoot 'enabled-summary.csv'
$nativeAotValidation = Join-Path $rawRoot 'nativeaot-validation-summary.csv'
$nativeAotControls = Join-Path $rawRoot 'nativeaot-control-summary.csv'
$nativeAotIdentities = Join-Path $rawRoot 'nativeaot-identities.csv'
$platforms = Join-Path $rawRoot 'platform-summary.csv'
$runtimeIdentities = Join-Path $rawRoot 'runtime-identities.csv'
$benchmarkInvocations = Join-Path $rawRoot 'benchmark-invocations.csv'
$benchmarkSummary = Join-Path $rawRoot 'benchmark-summary.csv'
$benchmarkIdentities = Join-Path $rawRoot 'benchmark-identities.csv'
$scenarioInvocations = Join-Path $rawRoot 'scenario-invocations.csv'
$scenarioSummary = Join-Path $rawRoot 'scenario-summary.csv'
$scenarioScanSummary = Join-Path $rawRoot 'scenario-scan-summary.csv'
$scenarioControls = Join-Path $rawRoot 'scenario-controls.csv'
$scenarioControlDetail = Join-Path $rawRoot 'scenario-control-detail.csv'
$scenarioIdentities = Join-Path $rawRoot 'scenario-identities.csv'
$scenarioSession = Join-Path $rawRoot 'scenario-session.csv'
$scenarioMatrix = Join-Path $rawRoot 'scenario-matrix.csv'

foreach ($path in @(
    $document,
    $validation,
    $compatibility,
    $controls,
    $malformed,
    $enabled,
    $nativeAotValidation,
    $nativeAotControls,
    $nativeAotIdentities,
    $platforms,
    $runtimeIdentities,
    $benchmarkInvocations,
    $benchmarkSummary,
    $benchmarkIdentities,
    $scenarioInvocations,
    $scenarioSummary,
    $scenarioScanSummary,
    $scenarioControls,
    $scenarioControlDetail,
    $scenarioIdentities,
    $scenarioSession,
    $scenarioMatrix
)) {
    Confirm (Test-Path -LiteralPath $path -PathType Leaf) (
        "Missing shipped artifact $path")
}

Require-Pattern 'src\coreclr\gc\gcinterface.h' (
    '#define GC_INTERFACE_MINOR_VERSION 15')
Require-Pattern 'src\coreclr\gc\gcinterface.h' (
    '#define GC_OBJECT_REFERENCE_ENUMERATION_INTERFACE_MINOR_VERSION 14')
Require-Pattern 'src\coreclr\gc\gcinterface.h' (
    '#define GC_ALLOCATION_NOTIFICATION_INTERFACE_MINOR_VERSION 13')
Require-Pattern 'src\coreclr\gc\gcinterface.h' (
    'GetObjectReferenceEnumerationParameters()')
Require-PatternCount 'src\coreclr\gc\gcinterface.h' (
    'GetObjectReferenceEnumerationParameters()') 1
Require-Pattern 'src\coreclr\gc\gcinterface.h' (
    'static ObjectReferenceEnumerationParameters parameters = {};')
Require-Pattern 'src\coreclr\gc\gcref.h' 'class GCReferenceRanges'
Require-Pattern 'src\coreclr\gc\gcref.h' 'class GCReferenceRangeIterator'
Require-Pattern 'src\coreclr\gc\gcref.h' '#ifndef DACCESS_COMPILE'
Require-PatternCount 'src\coreclr\gc\gcref.h' (
    'reinterpret_cast<ArrayBase*>(object)->GetNumComponents()') 1
Require-PatternCount 'src\coreclr\gc\gcref.h' (
    'GetGCReferenceObjectLayout(') 3
Forbid-Pattern 'src\coreclr\gc\gcref.h' 'static_cast<ArrayBase*>(object)'
Forbid-Pattern 'src\coreclr\gc\gcref.h' 'object->GetSize()'
Require-Pattern (
    'docs\design\lxr-port\P1.5\' +
    'nativeaot-reference-enumeration-validation.cpp') (
    'MethodTable* methodTable = GetMethodTable();')
Require-Pattern (
    'docs\design\lxr-port\P1.5\' +
    'run-nativeaot-reference-enumeration-validation.ps1') (
    'Exact pre-fix header did not reproduce the NativeAOT cast failure.')
Require-Pattern 'src\coreclr\vm\gcheaputilities.cpp' (
    'ConfigureObjectReferenceEnumeration')
Require-Pattern 'src\coreclr\nativeaot\Runtime\gcheaputilities.cpp' (
    'ConfigureObjectReferenceEnumeration')
Require-Pattern 'src\coreclr\gc\interface.cpp' (
    'ValidateObjectReferenceRanges')
Require-Pattern (
    'src\tests\profiler\native\gcheapenumerationprofiler\' +
    'gcheapenumerationprofiler.cpp') 'EnumerateObjectReferences'
Require-Pattern 'src\coreclr\gc\gcinternal.h' (
    'ObjectReferenceEnumerationTestScanIfEnabled(reinterpret_cast<Object*>(o))')
Require-Pattern 'src\coreclr\gc\objectreferenceenumerationtest.h' (
    'if (VolatileLoad(&g_object_reference_enumeration_test_mode) != 0)')
Require-Pattern (
    'docs\design\lxr-port\harness\src\Lxr.Harness.Core\' +
    'ReferenceEnumerationProbe.cs') '_stop();'
Require-Pattern (
    'docs\design\lxr-port\P1.5\' +
    'run-reference-enumeration-scenarios.ps1') "Id = 'srv-linked'"
Forbid-Pattern (
    'docs\design\lxr-port\P1.5\' +
    'run-reference-enumeration-scenarios.ps1') "Id = 'srv-standalone'"
Require-Pattern (
    'docs\design\lxr-port\harness\src\Lxr.Harness.Core\' +
    'ReferenceEnumerationProbe.cs') (
    'P15_REFERENCE_ENUMERATION_FIXED_FULL_COLLECTION_COUNT')
Require-Pattern (
    'docs\design\lxr-port\harness\src\Lxr.Harness.Core\' +
    'WorkerEntryPoint.cs') 'class FixedFullCollectionSchedule'
Require-Pattern (
    'docs\design\lxr-port\P1.5\' +
    'run-reference-enumeration-scenarios.ps1') (
    '$scenario.Name -eq ''pointer-chasing'' ? 2 : 0')
Require-Pattern (
    'docs\design\lxr-port\P1.5\' +
    'run-reference-enumeration-scenarios.ps1') 'nodeCount=65536'
Require-Pattern (
    'docs\design\lxr-port\P1.5\' +
    'run-reference-enumeration-scenarios.ps1') (
    'status --porcelain -- @sourceScopes')
Require-Pattern (
    'docs\design\lxr-port\P1.5\' +
    'verify-reference-enumeration-gate.ps1') (
    "[Guid]::NewGuid().ToString('N')")
Require-Pattern (
    'docs\design\lxr-port\P1.5\' +
    'verify-reference-enumeration-gate.ps1') (
    'GATE_OUTPUT_CLEANED: $runRoot')
Require-Pattern (
    'docs\design\lxr-port\P1.5\' +
    'verify-reference-enumeration-gate.ps1') (
    'GATE_OUTPUT_PRESERVED: $runRoot')
Require-Pattern (
    'docs\design\lxr-port\P1.5\' +
    'verify-reference-enumeration-gate.ps1') '} finally {'
Forbid-Pattern (
    'docs\design\lxr-port\P1.5\' +
    'verify-reference-enumeration-gate.ps1') (
    'artifacts\p15-reference-enumeration-gate')
Forbid-Pattern (
    'docs\design\lxr-port\P1.5\' +
    'verify-reference-enumeration-gate.ps1') (
    'Remove-Item -LiteralPath $OutputDirectory')
Require-Pattern (
    'docs\design\lxr-port\P1.5\' +
    'verify-reference-enumeration-gate-rerun.ps1') (
    'foreach ($run in 1..2)')
Require-Pattern (
    'docs\design\lxr-port\P1.5\' +
    'verify-reference-enumeration-gate-rerun.ps1') (
    'two default gate runs used two unique temporary directories')
Forbid-Pattern (
    'docs\design\lxr-port\P1.5\' +
    'verify-reference-enumeration-gate-rerun.ps1') '-OutputDirectory'

$header = Get-Content -LiteralPath (
    Join-Path $RepositoryRoot 'src\coreclr\gc\gcinterface.h') -Raw
Confirm (
    $header -notmatch (
        'GetObjectReferenceEnumerationParameters\(\)\s+PURE_VIRTUAL')) (
    'The 5.14 IGCHeap method is pure virtual.')
$referenceHeader = Get-Content -LiteralPath (
    Join-Path $RepositoryRoot 'src\coreclr\gc\gcref.h') -Raw
$containsMatches = [regex]::Matches(
    $referenceHeader,
    [regex]::Escape('if (!methodTable->ContainsGCPointers())'))
$layoutMatches = [regex]::Matches(
    $referenceHeader,
    [regex]::Escape('GetGCReferenceObjectLayout(object, methodTable);'))
Confirm (
    ($containsMatches.Count -eq 2) -and
    ($layoutMatches.Count -eq 2)) (
    'Reference visitor/iterator early-return markers are incomplete.')
if (($containsMatches.Count -eq 2) -and ($layoutMatches.Count -eq 2)) {
    for ($index = 0; $index -lt 2; $index++) {
        Confirm (
            $layoutMatches[$index].Index -gt $containsMatches[$index].Index) (
            "Reference layout call $index precedes its no-reference early return.")
    }
}

if (Test-Path -LiteralPath $validation) {
    $rows = @(Import-Csv -LiteralPath $validation)
    Confirm ($rows.Count -eq 10) (
        "Validation summary has $($rows.Count) rows; expected 10.")
    Confirm (@($rows | Where-Object Result -ne 'PASS').Count -eq 0) (
        'A validation row is not PASS.')
    Confirm (@($rows | Where-Object Name -eq 'native-x64').Detail -eq '40/40') (
        'Windows x64 native check cardinality changed.')
    Confirm (@($rows | Where-Object Name -eq 'native-x86').Detail -eq '40/40') (
        'Windows x86 native check cardinality changed.')
    Confirm (@($rows | Where-Object Name -eq 'native-linux-x64').Detail -eq '35/35') (
        'Linux x64 native check cardinality changed.')
}

if (Test-Path -LiteralPath $compatibility) {
    $rows = @(Import-Csv -LiteralPath $compatibility)
    Confirm ($rows.Count -eq 7) (
        "Compatibility summary has $($rows.Count) rows; expected 7.")
    Confirm (@($rows | Where-Object Result -ne 'PASS').Count -eq 0) (
        'A compatibility row is not PASS.')
    foreach ($name in @(
        'old-binary-current-runtime',
        'new-disabled-gc-old-runtime',
        'new-enabled-gc-old-runtime',
        'old-source-current-header-current-runtime',
        'pure-virtual-control-build'
    )) {
        Confirm (@($rows | Where-Object Name -eq $name).Count -eq 1) (
            "Compatibility summary omits $name.")
    }
}

if (Test-Path -LiteralPath $controls) {
    $rows = @(Import-Csv -LiteralPath $controls)
    Confirm ($rows.Count -eq 9) (
        "Control summary has $($rows.Count) rows; expected 9.")
    Confirm (@($rows | Where-Object Result -ne 'PASS').Count -eq 0) (
        'A perturbation control is not PASS.')
    Confirm (@($rows | Where-Object PerturbationCount -ne '1').Count -eq 0) (
        'A perturbation does not have exact cardinality one.')
    foreach ($name in @(
        'nativeaot-static-cast',
        'nativeaot-unmasked-object-size',
        'native-mode-mismatch'
    )) {
        Confirm (@($rows | Where-Object Name -eq $name).Count -eq 1) (
            "Control summary omits $name.")
    }
}

if (Test-Path -LiteralPath $nativeAotValidation) {
    $rows = @(Import-Csv -LiteralPath $nativeAotValidation)
    Confirm ($rows.Count -eq 2) (
        "NativeAOT validation summary has $($rows.Count) rows; expected 2.")
    $final = @($rows | Where-Object Name -eq 'final-nativeaot-shared-gc')
    $preFix = @($rows | Where-Object Name -eq 'pre-fix-nativeaot-compile')
    Confirm (
        ($final.Count -eq 1) -and
        ($final[0].Result -eq 'PASS') -and
        ($final[0].Observed -eq (
            '21/21 NativeAOT reference enumeration checks passed')) -and
        ($final[0].ProductCommit -match '^[0-9a-f]{40}$')) (
        'Final NativeAOT compile/execution evidence is invalid.')
    Confirm (
        ($preFix.Count -eq 1) -and
        ($preFix[0].Result -eq 'PASS') -and
        ($preFix[0].Observed -match 'inaccessible') -and
        ($preFix[0].ProductCommit -eq (
            '04c9b4c959193b8adb29924abd4c1da2336c1014'))) (
        'Pre-fix NativeAOT compile evidence is invalid.')
}

if (Test-Path -LiteralPath $nativeAotControls) {
    $rows = @(Import-Csv -LiteralPath $nativeAotControls)
    Confirm ($rows.Count -eq 2) (
        "NativeAOT control summary has $($rows.Count) rows; expected 2.")
    foreach ($name in @(
        'nativeaot-static-cast',
        'nativeaot-unmasked-object-size'
    )) {
        $match = @($rows | Where-Object Name -eq $name)
        Confirm (
            ($match.Count -eq 1) -and
            ($match[0].ProductCommit -match '^[0-9a-f]{40}$') -and
            ($match[0].PerturbationCount -eq '1') -and
            ($match[0].Result -eq 'PASS') -and
            [bool]$match[0].Observed) (
            "NativeAOT control evidence is invalid for $name.")
    }
}

if (Test-Path -LiteralPath $enabled) {
    $rows = @(Import-Csv -LiteralPath $enabled)
    Confirm ($rows.Count -eq 2) (
        "Enabled summary has $($rows.Count) rows; expected 2.")
    foreach ($mode in @('linked', 'standalone')) {
        $match = @($rows | Where-Object {
            ($_.Mode -eq $mode) -and
            ($_.Request -eq 'Enabled')
        })
        Confirm ($match.Count -eq 1) (
            "Enabled summary omits exactly one $mode row.")
        if ($match.Count -eq 1) {
            Confirm (
                ([int]$match[0].ExitCode -in @(0, 100)) -and
                ([int]$match[0].ProfilerPassMarkers -eq 3) -and
                ($match[0].Result -eq 'PASS')) (
                "Enabled $mode row is not a successful three-scenario run.")
        }
    }
}

if (Test-Path -LiteralPath $malformed) {
    $rows = @(Import-Csv -LiteralPath $malformed)
    Confirm ($rows.Count -eq 8) (
        "Malformed summary has $($rows.Count) rows; expected 8.")
    Confirm (@($rows | Where-Object Result -ne 'PASS').Count -eq 0) (
        'A malformed request control is not PASS.')
    Confirm (@($rows | Where-Object {
        ([int]$_.ExitCode -in @(0, 100)) -or
        -not $_.ExpectedPattern -or
        -not $_.Evidence
    }).Count -eq 0) 'Malformed evidence contains a success-shaped run.'

    $expectedPatterns = @{
        1 = 'descriptor was not initialized to NotProcessed'
        2 = 'resolver must be initialized to null'
        3 = 'descriptor is null'
        4 = 'request is invalid'
    }
    foreach ($mode in @('linked', 'standalone')) {
        foreach ($value in 1..4) {
            $match = @($rows | Where-Object {
                ($_.Mode -eq $mode) -and
                ([int]$_.Malformed -eq $value)
            })
            Confirm ($match.Count -eq 1) (
                "Malformed summary omits exactly one $mode/$value row.")
            if ($match.Count -eq 1) {
                Confirm (
                    $match[0].ExpectedPattern -eq $expectedPatterns[$value]) (
                    "Malformed $mode/$value has the wrong expected pattern.")
            }
        }
    }
}

if (Test-Path -LiteralPath $platforms) {
    $rows = @(Import-Csv -LiteralPath $platforms)
    Confirm ($rows.Count -eq 7) (
        "Platform summary has $($rows.Count) rows; expected 7.")
    $levels = @($rows.Level | Sort-Object -Unique)
    foreach ($level in @('execution', 'cross-build', 'build', 'audit')) {
        Confirm ($levels -contains $level) "Platform summary omits $level."
    }
    Confirm (@($rows | Where-Object {
        $_.Result -notin @('PASS', 'AUDIT')
    }).Count -eq 0) 'A platform result is invalid.'
}

foreach ($path in @(
    $runtimeIdentities,
    $benchmarkIdentities,
    $nativeAotIdentities,
    $scenarioIdentities
)) {
    if (Test-Path -LiteralPath $path) {
        $rows = @(Import-Csv -LiteralPath $path)
        Confirm ($rows.Count -gt 0) "$path is empty."
        foreach ($row in $rows) {
            Confirm ($row.Sha256 -match '^[0-9A-F]{64}$') (
                "Invalid SHA-256 for $($row.Name).")
            Confirm ([int64]$row.Length -gt 0) (
                "Invalid length for $($row.Name).")
            if ($path -eq $nativeAotIdentities) {
                Confirm ($row.ProductCommit -match '^[0-9a-f]{40}$') (
                    "Invalid product commit for $($row.Name).")
            }
            if ($path -eq $scenarioIdentities) {
                Confirm ($row.SourceCommit -match '^[0-9a-f]{40}$') (
                    "Invalid source commit for $($row.Name).")
                Confirm (-not [IO.Path]::IsPathRooted($row.Path)) (
                    "Scenario identity path is not repository-relative: $($row.Name).")
            }
        }
    }
}

if ((Test-Path -LiteralPath $benchmarkInvocations) -and
    (Test-Path -LiteralPath $benchmarkSummary)) {
    $rawRows = @(Import-Csv -LiteralPath $benchmarkInvocations)
    $summaryRows = @(Import-Csv -LiteralPath $benchmarkSummary)
    Confirm ($rawRows.Count -eq 216) (
        "Benchmark raw data has $($rawRows.Count) rows; expected 216.")
    Confirm ($summaryRows.Count -eq 9) (
        "Benchmark summary has $($summaryRows.Count) rows; expected 9.")
    foreach ($row in $summaryRows) {
        if ($row.MedianSource -eq 'BenchmarkDotNet') {
            Confirm ([double]$row.MedianNanoseconds -gt 0) (
                "Benchmark median is invalid for $($row.Method)/$($row.Scenario).")
        } else {
            Confirm (
                ($row.MedianSource -eq 'not-reported') -and
                -not $row.MedianNanoseconds) (
                "Missing median is not represented explicitly for $($row.Method)/$($row.Scenario).")
        }
    }

    foreach ($group in @($rawRows | Group-Object Method, Scenario)) {
        Confirm ($group.Count -eq 24) (
            "Benchmark group $($group.Name) has $($group.Count) rows; expected 24.")
        Confirm (@($group.Group | Where-Object {
            ([double]$_.NanosecondsPerOperation -le 0) -or
            ([int64]$_.Operations -le 0)
        }).Count -eq 0) "Benchmark group $($group.Name) has invalid samples."
    }

    $documentText = Get-Content -LiteralPath $document -Raw
    foreach ($scenario in 0..2) {
        $visitor = @($summaryRows | Where-Object {
            ($_.Method -eq 'ReferenceRangeVisitor') -and
            ([int]$_.Scenario -eq $scenario)
        })
        Confirm ($visitor.Count -eq 1) (
            "Missing visitor summary for scenario $scenario.")
        if ($visitor.Count -eq 1) {
            $ratio = ([double]$visitor[0].Ratio).ToString(
                'F2',
                [Globalization.CultureInfo]::InvariantCulture)
            Confirm ($documentText.Contains("$ratio" + 'x')) (
                "Document does not contain derived visitor ratio $ratio for scenario $scenario.")
        }
    }
}

if ((Test-Path -LiteralPath $scenarioInvocations) -and
    (Test-Path -LiteralPath $scenarioSummary) -and
    (Test-Path -LiteralPath $scenarioScanSummary) -and
    (Test-Path -LiteralPath $scenarioControls) -and
    (Test-Path -LiteralPath $scenarioControlDetail) -and
    (Test-Path -LiteralPath $scenarioIdentities) -and
    (Test-Path -LiteralPath $scenarioSession) -and
    (Test-Path -LiteralPath $scenarioMatrix)) {
    $scenarioRows = @(Import-Csv -LiteralPath $scenarioInvocations)
    $scenarioSummaryRows = @(Import-Csv -LiteralPath $scenarioSummary)
    $scenarioScanRows = @(Import-Csv -LiteralPath $scenarioScanSummary)
    $scenarioControlRows = @(Import-Csv -LiteralPath $scenarioControls)
    $scenarioControlDetailRows = @(
        Import-Csv -LiteralPath $scenarioControlDetail)
    $scenarioIdentityRows = @(Import-Csv -LiteralPath $scenarioIdentities)
    $scenarioSessionRows = @(Import-Csv -LiteralPath $scenarioSession)
    $scenarioMatrixRows = @(Import-Csv -LiteralPath $scenarioMatrix)
    $expectedRuntimeCommit = (
        'cd45d1cb3f2a6bf3a183840eb59b58e0bc97e068')
    $expectedHarnessCommit = (
        'ea5c3a02eac5cd2c53af6082e5dd9fbbf74bb331')
    $expectedScenarios = @('pointer-chasing', 'long-lived-cache')
    $expectedConfigurations = @{
        'wks|linked' = @{
            Server = 'false'
            HeapCount = 1
            Hook = 'coreclr.dll'
        }
        'wks|standalone' = @{
            Server = 'false'
            HeapCount = 1
            Hook = 'clrgc.dll'
        }
        'srv|linked' = @{
            Server = 'true'
            HeapCount = 8
            Hook = 'coreclr.dll'
        }
    }
    $expectedModes = @{
        callback = 1
        visitor = 2
        cursor = 3
    }
    $documentText = Get-Content -LiteralPath $document -Raw

    Confirm ($scenarioRows.Count -eq 90) (
        "Scenario invocation data has $($scenarioRows.Count) rows; expected 90.")
    Confirm ($scenarioSummaryRows.Count -eq 12) (
        "Scenario summary has $($scenarioSummaryRows.Count) rows; expected 12.")
    Confirm ($scenarioScanRows.Count -eq 18) (
        "Scenario scan summary has $($scenarioScanRows.Count) rows; expected 18.")
    Confirm ($scenarioControlRows.Count -eq 1) (
        "Scenario controls have $($scenarioControlRows.Count) rows; expected 1.")
    Confirm ($scenarioControlDetailRows.Count -eq 1) (
        "Scenario control details have $($scenarioControlDetailRows.Count) rows; expected 1.")
    Confirm ($scenarioIdentityRows.Count -eq 3) (
        "Scenario identities have $($scenarioIdentityRows.Count) rows; expected 3.")
    Confirm ($scenarioSessionRows.Count -eq 1) (
        "Scenario session data has $($scenarioSessionRows.Count) rows; expected 1.")
    Confirm ($scenarioMatrixRows.Count -eq 4) (
        "Scenario matrix has $($scenarioMatrixRows.Count) rows; expected 4.")

    $scenarioSessions = @($scenarioRows.Session | Sort-Object -Unique)
    $runtimeVersions = @(
        $scenarioRows.CoreClrFileVersion |
        Sort-Object -Unique)
    $scenarioProcessors = @(
        $scenarioRows.Processor |
        Sort-Object -Unique)
    $scenarioLogicalCoreCounts = @(
        $scenarioRows.LogicalCores |
        Sort-Object -Unique)
    $scenarioOperatingSystems = @(
        $scenarioRows.OS |
        Sort-Object -Unique)
    Confirm ($scenarioSessions.Count -eq 1) (
        "Scenario invocation data has $($scenarioSessions.Count) sessions; expected one.")
    Confirm ($runtimeVersions.Count -eq 1) (
        "Scenario invocation data has $($runtimeVersions.Count) runtime versions; expected one.")
    Confirm (
        ($scenarioProcessors.Count -eq 1) -and
        ($scenarioLogicalCoreCounts.Count -eq 1) -and
        ($scenarioOperatingSystems.Count -eq 1)) (
        'Scenario invocation data does not identify one machine configuration.')
    if ($runtimeVersions.Count -eq 1) {
        $commitMatch = [regex]::Match(
            $runtimeVersions[0],
            '@Commit: (?<commit>[0-9a-f]{40})')
        Confirm $commitMatch.Success (
            'Scenario runtime version does not identify an exact commit.')
        if ($commitMatch.Success) {
            Confirm (
                $commitMatch.Groups['commit'].Value -eq $expectedRuntimeCommit) (
                'Scenario runtime version does not identify the reviewed product commit.')
        }
    }

    $identityByName = @{}
    foreach ($identity in $scenarioIdentityRows) {
        $identityByName[$identity.Name] = $identity
        Confirm (-not [IO.Path]::IsPathRooted($identity.Path)) (
            "Scenario identity path is not portable: $($identity.Name).")
        $expectedSourceCommit = if (
            $identity.Name -eq 'Lxr.Harness.Worker.dll') {
            $expectedHarnessCommit
        } else {
            $expectedRuntimeCommit
        }
        Confirm ($identity.SourceCommit -eq $expectedSourceCommit) (
            "Scenario identity has the wrong source commit: $($identity.Name).")
    }
    foreach ($name in @(
        'coreclr.dll',
        'clrgc.dll',
        'Lxr.Harness.Worker.dll'
    )) {
        Confirm $identityByName.ContainsKey($name) (
            "Scenario identities omit $name.")
    }

    foreach ($row in $scenarioRows) {
        $configuration = "$($row.GC)|$($row.Deployment)"
        $expectedInducedCollections =
            $row.Scenario -eq 'pointer-chasing' ? 2 : 0
        Confirm ($expectedScenarios -contains $row.Scenario) (
            "Unexpected scenario in $($row.Id).")
        Confirm $expectedConfigurations.ContainsKey($configuration) (
            "Unexpected scenario configuration $configuration.")
        Confirm $expectedModes.ContainsKey($row.Variant) (
            "Unexpected scenario variant $($row.Variant).")
        if ($expectedModes.ContainsKey($row.Variant)) {
            Confirm (
                ([int]$row.RequestedMode -eq $expectedModes[$row.Variant]) -and
                ([int]$row.ObservedMode -eq $expectedModes[$row.Variant])) (
                "Scenario native mode was not confirmed for $($row.Scenario)/$configuration/$($row.Variant).")
        }
        if ($expectedConfigurations.ContainsKey($configuration)) {
            $expectedConfiguration = $expectedConfigurations[$configuration]
            Confirm (
                ($row.RequestedServerGC -eq $expectedConfiguration.Server) -and
                ($row.ObservedServerGC -eq $expectedConfiguration.Server) -and
                ($row.RequestedConcurrentGC -eq 'true') -and
                ($row.ObservedConcurrentGC -eq 'true') -and
                ([int]$row.RequestedHeapCount -eq
                 $expectedConfiguration.HeapCount) -and
                ([int]$row.ObservedHeapCount -eq
                 $expectedConfiguration.HeapCount)) (
                "Scenario GC configuration was not confirmed for $($row.Scenario)/$configuration.")
            if ($row.GC -eq 'srv') {
                Confirm (
                    ($row.RequestedDynamicAdaptationMode -eq '0') -and
                    ($row.ObservedDynamicAdaptationMode -eq '0')) (
                    "Scenario DATAS mode was not pinned for $($row.Scenario)/$configuration.")
            } else {
                Confirm (
                    -not $row.RequestedDynamicAdaptationMode -and
                    ($row.ObservedDynamicAdaptationMode -match '^[01]$')) (
                    "Workstation scenario has invalid DATAS readback for $($row.Scenario)/$configuration.")
            }
            Confirm ($row.HookLibrary.EndsWith(
                $expectedConfiguration.Hook,
                [StringComparison]::OrdinalIgnoreCase)) (
                "Scenario hook library is wrong for $configuration.")
        }
        if ($row.Deployment -eq 'standalone') {
            Confirm (
                [bool]$row.RequestedGCPath -and
                ($row.RequestedGCPath -eq $row.ObservedGCPath)) (
                "Standalone GC path was not confirmed for $($row.Scenario).")
        } else {
            Confirm (
                -not $row.RequestedGCPath -and
                -not $row.ObservedGCPath) (
                "Linked scenario unexpectedly requested a standalone GC path.")
        }
        foreach ($pathValue in @(
            $row.RequestedGCPath,
            $row.ObservedGCPath,
            $row.HookLibrary
        )) {
            Confirm (
                -not $pathValue -or
                -not [IO.Path]::IsPathRooted($pathValue)) (
                "Scenario evidence contains a non-portable path: $pathValue")
        }
        Confirm (
            ([int]$row.ScanErrors -eq 0) -and
            ([int64]$row.ObjectScans -gt 0) -and
            ([int64]$row.Ranges -gt 0) -and
            ([int64]$row.Slots -gt 0) -and
            ([int64]$row.NonNullSlots -gt 0) -and
            ([int64]$row.NonNullSlots -le [int64]$row.Slots) -and
            ([uint64]$row.ScanChecksum -ne 0)) (
            "Scenario scan data is invalid for $($row.Scenario)/$configuration/$($row.Variant).")
        Confirm (
            ([double]$row.OperationsPerSecond -gt 0) -and
            ([int64]$row.SteadyOperations -gt 0) -and
            ([double]$row.WarmupSeconds -eq 0.5) -and
            ([double]$row.SteadyStateSeconds -eq 3.0) -and
            ($row.ScanWindow -eq
             'post-reset-through-telemetry-end')) (
            "Scenario timing data is invalid for $($row.Scenario)/$configuration/$($row.Variant).")
        Confirm (
            ([int]$row.Gen0 -gt 0) -and
            ([int]$row.ExpectedInduced -eq
             $expectedInducedCollections) -and
            ([int]$row.Induced -eq $expectedInducedCollections) -and
            (($row.Scenario -ne 'pointer-chasing') -or
             (([int]$row.Gen2 -ge $expectedInducedCollections) -and
              ([int64]$row.ObjectScans -ge 131072) -and
              ([int64]$row.NonNullSlots -ge 262144)))) (
            "Scenario collection evidence is invalid for $($row.Scenario)/$configuration/$($row.Variant).")
        Confirm (
            ($row.CollectorConfirmed -eq 'True') -and
            ($row.ReportValid -eq 'True') -and
            ($row.VerificationSuccess -eq 'True') -and
            $row.CompletionMarker.StartsWith("$($row.Scenario):")) (
            "Scenario completion was not confirmed for $($row.Scenario)/$configuration/$($row.Variant).")
        Confirm (
            ($row.CoreClrSha256 -match '^[0-9a-f]{64}$') -and
            ($row.HookSha256 -match '^[0-9a-f]{64}$')) (
            "Scenario hashes are invalid for $($row.Scenario)/$configuration/$($row.Variant).")
        if ($identityByName.ContainsKey('coreclr.dll')) {
            Confirm (
                $row.CoreClrSha256 -eq
                $identityByName['coreclr.dll'].Sha256.ToLowerInvariant()) (
                'Scenario CoreCLR hash does not match its identity row.')
        }
        if ($expectedConfigurations.ContainsKey($configuration)) {
            $hookName = $expectedConfigurations[$configuration].Hook
            if ($identityByName.ContainsKey($hookName)) {
                Confirm (
                    $row.HookSha256 -eq
                    $identityByName[$hookName].Sha256.ToLowerInvariant()) (
                    "Scenario hook hash does not match $hookName.")
            }
        }
    }

    foreach ($scenario in $expectedScenarios) {
        foreach ($configuration in $expectedConfigurations.Keys) {
            $parts = $configuration.Split('|')
            foreach ($variant in $expectedModes.Keys) {
                $group = @($scenarioRows | Where-Object {
                    ($_.Scenario -eq $scenario) -and
                    ($_.GC -eq $parts[0]) -and
                    ($_.Deployment -eq $parts[1]) -and
                    ($_.Variant -eq $variant)
                })
                Confirm ($group.Count -eq 5) (
                    "$scenario/$configuration/$variant has $($group.Count) rows; expected 5.")
                Confirm (
                    (@($group.Invocation | Sort-Object -Unique).Count -eq 5) -and
                    (@($group.Order | Sort-Object -Unique).Count -eq 3)) (
                    "$scenario/$configuration/$variant does not cover five invocations and rotated order.")
                $scanMatch = @($scenarioScanRows | Where-Object {
                    ($_.Scenario -eq $scenario) -and
                    ($_.GC -eq $parts[0]) -and
                    ($_.Deployment -eq $parts[1]) -and
                    ($_.Variant -eq $variant)
                })
                Confirm ($scanMatch.Count -eq 1) (
                    "$scenario/$configuration/$variant does not have one scan summary row.")
                if ($variant -ne 'callback') {
                    $summaryMatch = @($scenarioSummaryRows | Where-Object {
                        ($_.Scenario -eq $scenario) -and
                        ($_.GC -eq $parts[0]) -and
                        ($_.Deployment -eq $parts[1]) -and
                        ($_.Variant -eq $variant)
                    })
                    Confirm ($summaryMatch.Count -eq 1) (
                        "$scenario/$configuration/$variant does not have one paired summary row.")
                }
            }
            foreach ($invocation in 0..4) {
                $pair = @($scenarioRows | Where-Object {
                    ($_.Scenario -eq $scenario) -and
                    ($_.GC -eq $parts[0]) -and
                    ($_.Deployment -eq $parts[1]) -and
                    ([int]$_.Invocation -eq $invocation)
                })
                Confirm (
                    ($pair.Count -eq 3) -and
                    (@($pair.Seed | Sort-Object -Unique).Count -eq 1) -and
                    (@($pair.Order | Sort-Object -Unique).Count -eq 3) -and
                    (@($pair.Variant | Sort-Object -Unique).Count -eq 3)) (
                    "$scenario/$configuration invocation $invocation is not a complete interleaved pair.")
            }

            $configurationRows = @($scenarioRows | Where-Object {
                ($_.Scenario -eq $scenario) -and
                ($_.GC -eq $parts[0]) -and
                ($_.Deployment -eq $parts[1])
            })
            $configurationDisplay = switch ($configuration) {
                'wks|linked' { 'Workstation linked' }
                'wks|standalone' { 'Workstation standalone' }
                'srv|linked' { 'Server linked' }
            }
            $countRanges = [Collections.Generic.List[string]]::new()
            foreach ($property in @('ObjectScans', 'Ranges', 'Slots')) {
                $values = @(
                    $configurationRows.$property |
                    ForEach-Object { [int64]$_ })
                $minimum = ($values | Measure-Object -Minimum).Minimum
                $maximum = ($values | Measure-Object -Maximum).Maximum
                $minimumText = $minimum.ToString(
                    'N0',
                    [Globalization.CultureInfo]::InvariantCulture)
                $maximumText = $maximum.ToString(
                    'N0',
                    [Globalization.CultureInfo]::InvariantCulture)
                $countRanges.Add(
                    $minimum -eq $maximum ?
                        $minimumText :
                        "$minimumText-$maximumText")
            }
            $scanLine = '| {0} | {1} | {2} | {3} | {4} |' -f
                $scenario,
                $configurationDisplay,
                $countRanges[0],
                $countRanges[1],
                $countRanges[2]
            Confirm $documentText.Contains($scanLine) (
                "Document does not contain derived scenario scan row: $scanLine")
        }
    }

    $summarySeeds = @(
        $scenarioSummaryRows.BootstrapSeed |
        ForEach-Object { [int]$_ } |
        Sort-Object -Unique)
    Confirm (
        ($summarySeeds.Count -eq 12) -and
        ($summarySeeds[0] -eq 20260825) -and
        ($summarySeeds[11] -eq 20260836)) (
        'Scenario paired summaries do not identify 12 exact bootstrap seeds.')
    foreach ($row in $scenarioSummaryRows) {
        $configuration = "$($row.GC)|$($row.Deployment)"
        Confirm (
            ($row.Session -eq $scenarioSessions[0]) -and
            ($expectedScenarios -contains $row.Scenario) -and
            $expectedConfigurations.ContainsKey($configuration) -and
            ($row.Baseline -eq 'callback') -and
            ($row.Variant -in @('visitor', 'cursor')) -and
            ([int]$row.Invocations -eq 5) -and
            ($row.RatioStatistic -eq 'paired-geometric-mean') -and
            ([double]$row.Ratio -gt 0) -and
            ([double]$row.RatioCiLow -gt 0) -and
            ([double]$row.RatioCiLow -le [double]$row.Ratio) -and
            ([double]$row.RatioCiHigh -ge [double]$row.Ratio) -and
            ($row.CiMethod -eq 'paired-bootstrap-percentile-95') -and
            ([int]$row.BootstrapResamples -eq 10000) -and
            ([int]$row.BootstrapSeed -ge 20260825) -and
            ([int]$row.BootstrapSeed -le 20260836)) (
            "Scenario paired summary is invalid for $($row.Scenario)/$configuration/$($row.Variant).")

        $logRatioSum = 0.0
        $ratioCount = 0
        $pairedRatios = [Collections.Generic.List[double]]::new()
        foreach ($invocation in 0..4) {
            $callback = @($scenarioRows | Where-Object {
                ($_.Scenario -eq $row.Scenario) -and
                ($_.GC -eq $row.GC) -and
                ($_.Deployment -eq $row.Deployment) -and
                ($_.Variant -eq 'callback') -and
                ([int]$_.Invocation -eq $invocation)
            })
            $variant = @($scenarioRows | Where-Object {
                ($_.Scenario -eq $row.Scenario) -and
                ($_.GC -eq $row.GC) -and
                ($_.Deployment -eq $row.Deployment) -and
                ($_.Variant -eq $row.Variant) -and
                ([int]$_.Invocation -eq $invocation)
            })
            if (($callback.Count -eq 1) -and ($variant.Count -eq 1)) {
                $pairedRatio =
                    [double]$variant[0].OperationsPerSecond /
                    [double]$callback[0].OperationsPerSecond
                $pairedRatios.Add($pairedRatio)
                $logRatioSum += [Math]::Log($pairedRatio)
                $ratioCount++
            }
        }
        Confirm ($ratioCount -eq 5) (
            "Scenario paired ratio is missing inputs for $($row.Scenario)/$configuration/$($row.Variant).")
        if ($ratioCount -eq 5) {
            $derivedRatio = [Math]::Exp($logRatioSum / $ratioCount)
            Confirm (
                [Math]::Abs($derivedRatio - [double]$row.Ratio) -lt 1e-12) (
                "Scenario paired ratio was not derived from invocation data for $($row.Scenario)/$configuration/$($row.Variant).")
            $bootstrap = Get-ScenarioBootstrapInterval (
                $pairedRatios.ToArray()) ([int]$row.BootstrapSeed)
            Confirm (
                ([Math]::Abs(
                    $bootstrap.Low - [double]$row.RatioCiLow) -lt 1e-12) -and
                ([Math]::Abs(
                    $bootstrap.High - [double]$row.RatioCiHigh) -lt 1e-12)) (
                "Scenario bootstrap interval was not derived from invocation data for $($row.Scenario)/$configuration/$($row.Variant).")
        }

        $configurationDisplay = switch ($configuration) {
            'wks|linked' { 'Workstation linked' }
            'wks|standalone' { 'Workstation standalone' }
            'srv|linked' { 'Server linked' }
        }
        $summaryLine = '| {0} | {1} | {2} | {3} | [{4}, {5}] |' -f
            $row.Scenario,
            $configurationDisplay,
            $row.Variant,
            ([double]$row.Ratio).ToString(
                'F5',
                [Globalization.CultureInfo]::InvariantCulture),
            ([double]$row.RatioCiLow).ToString(
                'F5',
                [Globalization.CultureInfo]::InvariantCulture),
            ([double]$row.RatioCiHigh).ToString(
                'F5',
                [Globalization.CultureInfo]::InvariantCulture)
        Confirm $documentText.Contains($summaryLine) (
            "Document does not contain derived scenario row: $summaryLine")
    }

    foreach ($row in $scenarioScanRows) {
        $group = @($scenarioRows | Where-Object {
            ($_.Scenario -eq $row.Scenario) -and
            ($_.GC -eq $row.GC) -and
            ($_.Deployment -eq $row.Deployment) -and
            ($_.Variant -eq $row.Variant)
        })
        Confirm ($group.Count -eq 5) (
            "Scenario scan summary has no five-row group for $($row.Scenario)/$($row.GC)/$($row.Deployment)/$($row.Variant).")
        if ($group.Count -eq 5) {
            $objectScans = @(
                $group.ObjectScans | ForEach-Object { [int64]$_ })
            $ranges = @(
                $group.Ranges | ForEach-Object { [int64]$_ })
            $slots = @(
                $group.Slots | ForEach-Object { [int64]$_ })
            $nonNullSlots = @(
                $group.NonNullSlots | ForEach-Object { [int64]$_ })
            $gen0 = @(
                $group.Gen0 | ForEach-Object { [int]$_ })
            $gen1 = @(
                $group.Gen1 | ForEach-Object { [int]$_ })
            $gen2 = @(
                $group.Gen2 | ForEach-Object { [int]$_ })
            $induced = @(
                $group.Induced | ForEach-Object { [int]$_ })
            $expectedInduced = @(
                $group.ExpectedInduced | ForEach-Object { [int]$_ })
            Confirm (
                ([int]$row.Invocations -eq 5) -and
                ([int64]$row.ObjectScans -eq
                 ($objectScans | Measure-Object -Sum).Sum) -and
                ([int64]$row.Ranges -eq
                 ($ranges | Measure-Object -Sum).Sum) -and
                ([int64]$row.Slots -eq
                 ($slots | Measure-Object -Sum).Sum) -and
                ([int64]$row.NonNullSlots -eq
                 ($nonNullSlots | Measure-Object -Sum).Sum) -and
                ([int]$row.Gen0Collections -eq
                 ($gen0 | Measure-Object -Sum).Sum) -and
                ([int]$row.Gen1Collections -eq
                 ($gen1 | Measure-Object -Sum).Sum) -and
                ([int]$row.Gen2Collections -eq
                 ($gen2 | Measure-Object -Sum).Sum) -and
                ([int]$row.InducedCollections -eq
                 ($induced | Measure-Object -Sum).Sum) -and
                ([int]$row.ExpectedInducedCollections -eq
                 ($expectedInduced | Measure-Object -Sum).Sum) -and
                ([int64]$row.MinObjectScans -eq
                 ($objectScans | Measure-Object -Minimum).Minimum) -and
                ([int64]$row.MaxObjectScans -eq
                 ($objectScans | Measure-Object -Maximum).Maximum) -and
                ([int64]$row.MinSlots -eq
                 ($slots | Measure-Object -Minimum).Minimum) -and
                ([int64]$row.MaxSlots -eq
                 ($slots | Measure-Object -Maximum).Maximum)) (
                "Scenario scan summary was not derived from invocation data for $($row.Scenario)/$($row.GC)/$($row.Deployment)/$($row.Variant).")
        }
    }

    if ($scenarioControlRows.Count -eq 1) {
        $row = $scenarioControlRows[0]
        Confirm (
            ($row.Name -eq 'native-mode-mismatch') -and
            ([int]$row.PerturbationCount -eq 1) -and
            ($row.Expected -eq 'worker invalidates a native mode mismatch') -and
            ($row.Result -eq 'PASS') -and
            ([int]$row.ExitCode -eq 1) -and
            ($row.Evidence -eq 'scenario-control-detail.csv')) (
            'Native mode-mismatch control evidence is invalid.')
    }

    if ($scenarioControlDetailRows.Count -eq 1) {
        $row = $scenarioControlDetailRows[0]
        Confirm (
            ($row.Session -eq $scenarioSessions[0]) -and
            ($row.Name -eq 'native-mode-mismatch') -and
            ($row.Scenario -eq 'pointer-chasing') -and
            ([int]$row.ExitCode -eq 1) -and
            ($row.CollectorConfirmed -eq 'True') -and
            ($row.ReportValid -eq 'False') -and
            ($row.InvalidReason -eq 'reference-enumeration-probe-failed') -and
            ($row.VerificationSuccess -eq 'True') -and
            ($row.Failure -eq
             'reference-enumeration mode expected 3, observed 2') -and
            ([int]$row.ExpectedMode -eq 3) -and
            ([int]$row.ObservedMode -eq 2) -and
            ([int]$row.ScanErrors -eq 0) -and
            ([int64]$row.ObjectScans -gt 0) -and
            ([int64]$row.Ranges -gt 0) -and
            ([int64]$row.Slots -gt 0) -and
            ([int64]$row.NonNullSlots -gt 0) -and
            ([uint64]$row.ScanChecksum -ne 0) -and
            ($row.ScanWindow -eq
             'post-reset-through-telemetry-end') -and
            -not [IO.Path]::IsPathRooted($row.HookLibrary) -and
            ($row.HookSha256 -eq
             $identityByName['coreclr.dll'].Sha256.ToLowerInvariant()) -and
            ($row.CoreClrFileVersion -match
             [regex]::Escape($expectedRuntimeCommit)) -and
            ($row.RuntimeCommit -eq $expectedRuntimeCommit) -and
            ([int]$row.ExpectedInducedCollections -eq 2) -and
            ([int]$row.InducedCollections -eq 2) -and
            ([int]$row.Gen2Collections -ge 2) -and
            ([int64]$row.ObjectScans -ge 131072) -and
            ([int64]$row.NonNullSlots -ge 262144) -and
            $row.CompletionMarker.StartsWith('pointer-chasing:')) (
            'Native mode-mismatch control detail is invalid.')
    }

    if ($scenarioSessionRows.Count -eq 1) {
        $row = $scenarioSessionRows[0]
        Confirm (
            ($row.Session -eq $scenarioSessions[0]) -and
            ($row.RuntimeCommit -eq $expectedRuntimeCommit) -and
            ($row.HarnessSourceCommit -eq $expectedHarnessCommit) -and
            ([int]$row.Invocations -eq 5) -and
            ([int]$row.InvocationRows -eq 90) -and
            ([int]$row.SummaryRows -eq 12) -and
            ([int]$row.ControlRows -eq 1) -and
            ([double]$row.WarmupSeconds -eq 0.5) -and
            ([double]$row.SteadyStateSeconds -eq 3.0) -and
            ($row.Configurations -eq
             'wks|linked,wks|standalone,srv|linked') -and
            ($row.BuildConfiguration -eq 'Release-noPGO') -and
            ([int]$row.RequestedReadyToRun -eq 0) -and
            ([int]$row.RequestedTieredCompilation -eq 0) -and
            ($row.ScanWindow -eq
             'post-reset-through-telemetry-end') -and
            ([int]$row.PointerChasingFixedFullCollections -eq 2) -and
            ([int]$row.LongLivedCacheFixedFullCollections -eq 0) -and
            ($row.RatioStatistic -eq 'paired-geometric-mean') -and
            ($row.CiMethod -eq 'paired-bootstrap-percentile-95') -and
            ([int]$row.BootstrapResamples -eq 10000) -and
            ($row.Processor -eq $scenarioProcessors[0]) -and
            ([int]$row.LogicalCores -eq
             [int]$scenarioLogicalCoreCounts[0]) -and
            ($row.OS -eq $scenarioOperatingSystems[0])) (
            'Scenario session evidence is invalid.')
    }

    if ($scenarioMatrixRows.Count -eq 4) {
        foreach ($configuration in $expectedConfigurations.Keys) {
            $parts = $configuration.Split('|')
            $match = @($scenarioMatrixRows | Where-Object {
                ($_.GC -eq $parts[0]) -and
                ($_.Deployment -eq $parts[1])
            })
            Confirm (
                ($match.Count -eq 1) -and
                ($match[0].Status -eq 'MEASURED') -and
                ([int]$match[0].HeapCount -eq
                 $expectedConfigurations[$configuration].HeapCount) -and
                [bool]$match[0].Reason) (
                "Scenario matrix does not mark $configuration as measured.")
        }
        $excluded = @($scenarioMatrixRows | Where-Object {
            ($_.GC -eq 'srv') -and
            ($_.Deployment -eq 'standalone')
        })
        Confirm (
            ($excluded.Count -eq 1) -and
            ($excluded[0].Status -eq 'EXCLUDED') -and
            ([int]$excluded[0].HeapCount -eq 8) -and
            ([int]$excluded[0].RequestedDynamicAdaptationMode -eq 0) -and
            ([int]$excluded[0].ObservedDynamicAdaptationMode -eq 1) -and
            ($excluded[0].Reason -match 'DynamicAdaptationMode=1') -and
            ($excluded[0].Reason -match 'requires 0')) (
            'Scenario matrix does not record the Server standalone exclusion.')
    }
}

if ($failures.Count -ne 0) {
    foreach ($failure in $failures) {
        Write-Error $failure
    }
    throw "$($failures.Count) of $checks P1.5 verification checks failed."
}

Write-Host "PASS: $checks P1.5 verification checks"
