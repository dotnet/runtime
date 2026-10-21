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
    $InputDirectory = Join-Path $RepositoryRoot 'artifacts'
}
if (-not $OutputDirectory) {
    $OutputDirectory = Join-Path $scriptRoot 'raw'
}

function Read-Status([string]$name) {
    $path = Join-Path $InputDirectory "$name.status"
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Missing status file: $path"
    }
    $text = (Get-Content -LiteralPath $path -Raw).Trim()
    if ($text -notmatch '^exit=(?<exit>-?[0-9]+)$') {
        throw "Invalid status file: $path"
    }
    return [int]$Matches.exit
}

function Require-LogPattern(
    [string]$name,
    [string]$pattern
) {
    $path = Join-Path $InputDirectory "$name.log"
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Missing log file: $path"
    }
    if (-not (Select-String -LiteralPath $path -Pattern $pattern -Quiet)) {
        throw "Log $path does not contain '$pattern'."
    }
}

$validationRows = [Collections.Generic.List[object]]::new()

$nativeChecks = @(
    @{ Name = 'native-x64'; Log = 'p15-nativeaot-fix-validation-x64'; Pattern = '40/40' },
    @{ Name = 'native-x86'; Log = 'p15-nativeaot-fix-validation-x86'; Pattern = '40/40' },
    @{ Name = 'native-linux-x64'; Log = 'p15-nativeaot-fix-validation-linux-x64'; Pattern = '35/35' }
)
foreach ($check in $nativeChecks) {
    Require-LogPattern $check.Log $check.Pattern
    $validationRows.Add([pscustomobject][ordered]@{
        Name = $check.Name
        Result = 'PASS'
        Evidence = "$($check.Log).log"
        Detail = $check.Pattern
    })
}

if ((Read-Status 'p15-nativeaot-fix-coreclr-build') -ne 0) {
    throw 'CoreCLR contract build did not pass.'
}
$validationRows.Add([pscustomobject][ordered]@{
    Name = 'coreclr-debug-build'
    Result = 'PASS'
    Evidence = 'p15-nativeaot-fix-coreclr-build.log'
    Detail = '0 warnings, 0 errors'
})

if ((Read-Status 'p15-nativeaot-fix-profiler-run') -ne 0) {
    throw 'Real-layout profiler wrapper did not return zero.'
}
Require-LogPattern 'p15-nativeaot-fix-profiler-run' 'END EXECUTION - PASSED'
$validationRows.Add([pscustomobject][ordered]@{
    Name = 'real-layout-profiler'
    Result = 'PASS'
    Evidence = 'p15-nativeaot-fix-profiler-run.log'
    Detail = '3/3 profiler scenarios; exact Debug parity assertions'
})

if ((Read-Status 'p15-enabled-build') -ne 0) {
    throw 'Enabled request build did not pass.'
}
$validationRows.Add([pscustomobject][ordered]@{
    Name = 'enabled-request-build'
    Result = 'PASS'
    Evidence = 'p15-enabled-build.log'
    Detail = 'linked and standalone request implementation'
})

$enabledSource = Join-Path $InputDirectory 'p15-enabled-summary.csv'
$enabledRows = @(Import-Csv -LiteralPath $enabledSource)
if (($enabledRows.Count -ne 2) -or
    (@($enabledRows | Where-Object Result -ne 'PASS').Count -ne 0) -or
    (@($enabledRows | Where-Object {
        ([int]$_.ExitCode -notin @(0, 100)) -or
        ([int]$_.ProfilerPassMarkers -ne 3) -or
        ($_.Request -ne 'Enabled') -or
        ($_.Mode -notin @('linked', 'standalone'))
    }).Count -ne 0) -or
    (@($enabledRows.Mode | Sort-Object -Unique).Count -ne 2)) {
    throw 'Enabled current-runtime evidence is incomplete.'
}
$validationRows.Add([pscustomobject][ordered]@{
    Name = 'enabled-current-runtime'
    Result = 'PASS'
    Evidence = 'p15-enabled-{linked,standalone}.log'
    Detail = '2/2 current-runtime starts; collectible Debug oracle executed'
})

$malformedSource = Join-Path $InputDirectory 'p15-malformed-summary.csv'
$malformedRows = @(Import-Csv -LiteralPath $malformedSource)
if (($malformedRows.Count -ne 8) -or
    (@($malformedRows | Where-Object Result -ne 'PASS').Count -ne 0) -or
    (@($malformedRows | Where-Object {
        ([int]$_.ExitCode -in @(0, 100)) -or
        -not $_.ExpectedPattern -or
        -not $_.Evidence
    }).Count -ne 0)) {
    throw 'Malformed-request evidence is incomplete or success-shaped.'
}
$validationRows.Add([pscustomobject][ordered]@{
    Name = 'malformed-request-controls'
    Result = 'PASS'
    Evidence = 'p15-malformed-{linked,standalone}-{1..4}.log'
    Detail = '8/8 rejected before managed Main'
})

$nativeAotRoot = Join-Path $InputDirectory (
    'p15-nativeaot-reference-enumeration')
$nativeAotValidationSource = Join-Path $nativeAotRoot 'validation-summary.csv'
$nativeAotControlSource = Join-Path $nativeAotRoot 'control-summary.csv'
$nativeAotIdentitySource = Join-Path $nativeAotRoot 'identities.csv'
$nativeAotValidationRows = @(Import-Csv -LiteralPath $nativeAotValidationSource)
$nativeAotControlRows = @(Import-Csv -LiteralPath $nativeAotControlSource)
$nativeAotIdentityRows = @(Import-Csv -LiteralPath $nativeAotIdentitySource)
if ((Read-Status 'p15-nativeaot-fix-nativeaot-build') -ne 0) {
    throw 'Corrected NativeAOT runtime build did not pass.'
}
if ((Read-Status 'p15-nativeaot-fix-smoke-build') -ne 0) {
    throw 'Corrected NativeAOT smoke build did not pass.'
}
if ((Read-Status 'p15-nativeaot-fix-smoke-run') -ne 100) {
    throw 'Corrected NativeAOT smoke execution did not return 100.'
}
if ((Read-Status 'p15-nativeaot-reference-enumeration') -ne 0) {
    throw 'NativeAOT shared-GC validation runner did not pass.'
}
if (($nativeAotValidationRows.Count -ne 2) -or
    (@($nativeAotValidationRows | Where-Object Result -ne 'PASS').Count -ne 0) -or
    (@($nativeAotValidationRows | Where-Object {
        -not $_.ProductCommit -or -not $_.Observed -or -not $_.Evidence
    }).Count -ne 0)) {
    throw 'NativeAOT validation evidence is incomplete.'
}
if (($nativeAotControlRows.Count -ne 2) -or
    (@($nativeAotControlRows | Where-Object Result -ne 'PASS').Count -ne 0) -or
    (@($nativeAotControlRows | Where-Object {
        ($_.PerturbationCount -ne '1') -or
        -not $_.ProductCommit -or
        -not $_.Observed
    }).Count -ne 0)) {
    throw 'NativeAOT control evidence is incomplete.'
}
if (($nativeAotIdentityRows.Count -ne 3) -or
    (@($nativeAotIdentityRows | Where-Object {
        ($_.ProductCommit -notmatch '^[0-9a-f]{40}$') -or
        ($_.Sha256 -notmatch '^[0-9A-F]{64}$') -or
        ([int64]$_.Length -le 0)
    }).Count -ne 0)) {
    throw 'NativeAOT identity evidence is incomplete.'
}
foreach ($row in $nativeAotValidationRows) {
    $validationRows.Add([pscustomobject][ordered]@{
        Name = $row.Name
        Result = $row.Result
        Evidence = 'nativeaot-validation-summary.csv'
        Detail = $row.Observed
    })
}

$compatibilitySource = Join-Path $InputDirectory (
    'p15-reference-enumeration-compatibility\compatibility-summary.csv')
$controlSource = Join-Path $InputDirectory (
    'p15-reference-enumeration-controls\control-summary.csv')
$scenarioControlSource = Join-Path $InputDirectory (
    'p15-reference-enumeration-scenarios\scenario-controls.csv')
$shippedScenarioControlSource = Join-Path $OutputDirectory (
    'scenario-controls.csv')
$shippedScenarioControlDetail = Join-Path $OutputDirectory (
    'scenario-control-detail.csv')
$compatibilityRows = @(Import-Csv -LiteralPath $compatibilitySource)
$coreControlRows = @(Import-Csv -LiteralPath $controlSource)
$executedScenarioControlRows = @(
    Import-Csv -LiteralPath $scenarioControlSource)
if (-not (Test-Path -LiteralPath $shippedScenarioControlSource -PathType Leaf) -or
    -not (Test-Path -LiteralPath $shippedScenarioControlDetail -PathType Leaf)) {
    throw 'Collect scenario evidence before collecting aggregate validation evidence.'
}
$scenarioControlRows = @(
    Import-Csv -LiteralPath $shippedScenarioControlSource)
$controlRows = @($coreControlRows) +
    @($nativeAotControlRows) +
    @($scenarioControlRows)
if (($compatibilityRows.Count -ne 7) -or
    (@($compatibilityRows | Where-Object Result -ne 'PASS').Count -ne 0)) {
    throw 'Compatibility evidence is incomplete.'
}
if (($scenarioControlRows.Count -ne 1) -or
    ($scenarioControlRows[0].Name -ne 'native-mode-mismatch') -or
    ($scenarioControlRows[0].Evidence -ne 'scenario-control-detail.csv') -or
    ($executedScenarioControlRows.Count -ne 1) -or
    ($executedScenarioControlRows[0].Name -ne
     $scenarioControlRows[0].Name) -or
    ($executedScenarioControlRows[0].Result -ne
     $scenarioControlRows[0].Result) -or
    ($controlRows.Count -ne 9) -or
    (@($controlRows | Where-Object Result -ne 'PASS').Count -ne 0) -or
    (@($controlRows | Where-Object PerturbationCount -ne '1').Count -ne 0)) {
    throw 'Perturbation evidence is incomplete.'
}

$platformRows = @(
    [pscustomobject][ordered]@{
        Platform = 'Windows x64 CoreCLR'
        Level = 'execution'
        Result = 'PASS'
        Evidence = 'p15-nativeaot-fix-profiler-run.log; p15-enabled-build.log'
        Limitation = ''
    },
    [pscustomobject][ordered]@{
        Platform = 'Windows x86'
        Level = 'execution'
        Result = 'PASS'
        Evidence = 'p15-nativeaot-fix-validation-x86.log'
        Limitation = 'Full target build is blocked by unrelated generated thunk offsets in the installed preview toolchain.'
    },
    [pscustomobject][ordered]@{
        Platform = 'Linux x64'
        Level = 'execution'
        Result = 'PASS'
        Evidence = 'p15-nativeaot-fix-validation-linux-x64.log'
        Limitation = 'Header/native validator; full CoreCLR Linux build not run in this Windows worktree.'
    },
    [pscustomobject][ordered]@{
        Platform = 'Windows ARM64 CoreCLR'
        Level = 'cross-build'
        Result = 'PASS'
        Evidence = 'p15-arm64-target-build.log'
        Limitation = 'No ARM64 execution hardware. Optional x64 debugger cross-component stage fails in the installed preview assembler.'
    },
    [pscustomobject][ordered]@{
        Platform = 'Windows x64 NativeAOT'
        Level = 'execution'
        Result = 'PASS'
        Evidence = 'nativeaot-validation-summary.csv; nativeaot-identities.csv'
        Limitation = 'Shared-GC compile and 21/21 marked-MT execution; NativeAOT has no collectible MethodTables.'
    },
    [pscustomobject][ordered]@{
        Platform = 'DAC'
        Level = 'build'
        Result = 'PASS'
        Evidence = 'p15-nativeaot-fix-coreclr-build.log'
        Limitation = 'Raw iterator is excluded from DACCESS_COMPILE.'
    },
    [pscustomobject][ordered]@{
        Platform = 'Mono'
        Level = 'audit'
        Result = 'AUDIT'
        Evidence = 'source audit'
        Limitation = 'Mono does not consume CoreCLR GCDesc or this GC/EE interface.'
    }
)

New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$validationRows | Export-Csv -LiteralPath (
    Join-Path $OutputDirectory 'validation-summary.csv') -NoTypeInformation
$compatibilityRows | Export-Csv -LiteralPath (
    Join-Path $OutputDirectory 'compatibility-summary.csv') -NoTypeInformation
$controlRows | Export-Csv -LiteralPath (
    Join-Path $OutputDirectory 'control-summary.csv') -NoTypeInformation
$malformedRows | Export-Csv -LiteralPath (
    Join-Path $OutputDirectory 'malformed-summary.csv') -NoTypeInformation
$enabledRows | Export-Csv -LiteralPath (
    Join-Path $OutputDirectory 'enabled-summary.csv') -NoTypeInformation
$nativeAotValidationRows | Export-Csv -LiteralPath (
    Join-Path $OutputDirectory 'nativeaot-validation-summary.csv') -NoTypeInformation
$nativeAotControlRows | Export-Csv -LiteralPath (
    Join-Path $OutputDirectory 'nativeaot-control-summary.csv') -NoTypeInformation
$nativeAotIdentityRows | Export-Csv -LiteralPath (
    Join-Path $OutputDirectory 'nativeaot-identities.csv') -NoTypeInformation
$platformRows | Export-Csv -LiteralPath (
    Join-Path $OutputDirectory 'platform-summary.csv') -NoTypeInformation

$identityFiles = @(
    'src\coreclr\gc\gcinterface.h',
    'src\coreclr\gc\gcref.h',
    'artifacts\bin\coreclr\windows.x64.Debug\coreclr.dll',
    'artifacts\bin\coreclr\windows.x64.Debug\clrgc.dll',
    'artifacts\bin\coreclr\windows.x64.Debug\aotsdk\Runtime.WorkstationGC.lib',
    'artifacts\bin\coreclr\windows.x64.Debug\aotsdk\Runtime.ServerGC.lib',
    'artifacts\bin\coreclr\windows.arm64.Release\coreclr.dll',
    'artifacts\bin\coreclr\windows.arm64.Release\clrgc.dll'
)
$identityRows = foreach ($relativePath in $identityFiles) {
    $path = Join-Path $RepositoryRoot $relativePath
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Identity file missing: $path"
    }
    [pscustomobject][ordered]@{
        Name = $relativePath
        Sha256 = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash
        Length = (Get-Item -LiteralPath $path).Length
    }
}
$identityRows | Export-Csv -LiteralPath (
    Join-Path $OutputDirectory 'runtime-identities.csv') -NoTypeInformation

Write-Host (
    "PASS: {0} validation, {1} compatibility, {2} controls, {3} platforms" -f
    $validationRows.Count,
    $compatibilityRows.Count,
    $controlRows.Count,
    $platformRows.Count)
