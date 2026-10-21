# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

[CmdletBinding()]
param(
    [string]$RepositoryRoot,
    [string]$OutputDirectory,
    [ValidateSet('Debug', 'Release')][string]$Configuration = 'Debug',
    [ValidateSet('None', 'EarlyRoot', 'SweepQuery', 'TraceSatb', 'TraceDeletion',
        'PredictorUpdate', 'TriggerOrder', 'Lifetime', 'StorageDiagnostics', 'ObjectInterior', 'PendingRetirement',
        'MatureStale', 'MatureReplay', 'MatureShape', 'MatureScan', 'MatureReaderCompletion', 'MatureDeadScan', 'MatureEarlyRelease', 'MatureCount', 'MaturePredictor', 'ReserveUnits',
        'LogPublication', 'LogDecode', 'LogCapacity', 'LogReuse', 'LogDrop',
        'TraceBatchDrop', 'TraceBatchUnbatched', 'TraceBatchEarly')][string]$Control = 'None'
)

$ErrorActionPreference = 'Stop'
if (-not $RepositoryRoot) {
    $RepositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..\..')).ProviderPath
}
if (-not $OutputDirectory) {
    $OutputDirectory = Join-Path $RepositoryRoot (
        'artifacts\P4\vertical-windows-' + $Configuration + '-' + [guid]::NewGuid().ToString('N'))
}
$OutputDirectory = $PSCmdlet.GetUnresolvedProviderPathFromPSPath($OutputDirectory)
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$gc = Join-Path $RepositoryRoot 'src\coreclr\gc'
$sources = @(
    'side_metadata.cpp', 'immix_block.cpp', 'immix_allocation.cpp', 'reference_count.cpp',
    'large_object_space.cpp', 'lxr_work_packet.cpp', 'lxr_work_scheduler.cpp',
    'lxr_pause_schedule.cpp', 'lxr_pause_coordinator.cpp', 'lxr_completion_counter.cpp',
    'lxr_reference_counting.cpp'
) | ForEach-Object { Join-Path $gc $_ }
$sources += @(
    (Join-Path $RepositoryRoot 'docs\design\lxr-port\P2.5\large-object-space-test-platform.cpp'),
    (Join-Path $RepositoryRoot 'docs\design\lxr-port\P4\lxr-rc-validation.cpp')
)
$inputs = $sources + @(
    'side_metadata.h', 'immix_block.h', 'immix_allocation.h', 'reference_count.h',
    'large_object_space.h', 'lxr_work_packet.h', 'lxr_work_scheduler.h',
    'lxr_pause_schedule.h', 'lxr_pause_coordinator.h', 'lxr_completion_counter.h',
    'lxr_reference_counting.h', 'lxr_virtual_array.h', 'gcref.h', 'gcdesc.h'
    | ForEach-Object { Join-Path $gc $_ }
)
$inputs | ForEach-Object {
    Get-FileHash -LiteralPath $_ -Algorithm SHA256
} | Select-Object Path, Hash | Export-Csv (Join-Path $OutputDirectory 'source-identities.csv') -NoTypeInformation

$vs = Join-Path $RepositoryRoot 'eng\native\init-vs-env.cmd'
$quoted = ($sources | ForEach-Object { '"' + $_ + '"' }) -join ' '
$native = Join-Path $RepositoryRoot 'src\native'
$configurationFlags = if ($Configuration -eq 'Debug') { '/Od /MTd' } else { '/O2 /MT /DNDEBUG' }
$controlFlag = switch ($Control) {
    'EarlyRoot' { '/DLXR_RC_EARLY_ROOT_CONTROL' }
    'SweepQuery' { '/DLXR_RC_SWEEP_QUERY_CONTROL' }
    'TraceSatb' { '/DLXR_TRACE_SKIP_SATB_CONTROL' }
    'TraceDeletion' { '/DLXR_TRACE_SKIP_DELETION_CONTROL' }
    'PredictorUpdate' { '/DLXR_POLICY_WRONG_UPDATE_CONTROL' }
    'TriggerOrder' { '/DLXR_POLICY_WRONG_ORDER_CONTROL' }
    'Lifetime' { '/DLXR_PROBE_SKIP_RECLAIM_CONTROL' }
    'StorageDiagnostics' { '/DLXR_STORAGE_SKIP_COPY_CONTROL' }
    'ObjectInterior' { '/DLXR_OBJECT_INTERIOR_CONTROL' }
    'PendingRetirement' { '/DLXR_RETIRE_PENDING_CONTROL' }
    'MatureStale' { '/DLXR_REMSET_STALE_CONTROL' }
    'MatureReplay' { '/DLXR_REMSET_REPLAY_CONTROL' }
    'MatureShape' { '/DLXR_REMSET_SHAPE_CONTROL' }
    'MatureScan' { '/DLXR_MATURE_SCAN_REBASE_CONTROL' }
    'MatureReaderCompletion' { '/DLXR_MATURE_READER_COMPLETION_CONTROL' }
    'MatureDeadScan' { '/DLXR_MATURE_DEAD_SCAN_CONTROL' }
    'MatureEarlyRelease' { '/DLXR_MATURE_EARLY_RELEASE_CONTROL' }
    'MatureCount' { '/DLXR_MATURE_COUNT_CONTROL' }
    'MaturePredictor' { '/DLXR_MATURE_PREDICTOR_CONTROL' }
    'ReserveUnits' { '/DLXR_RESERVE_UNITS_CONTROL' }
    'LogPublication' { '/DLXR_LOG_PUBLICATION_CONTROL' }
    'LogDecode' { '/DLXR_LOG_DECODE_CONTROL' }
    'LogCapacity' { '/DLXR_LOG_CAPACITY_CONTROL' }
    'LogReuse' { '/DLXR_LOG_REUSE_CONTROL' }
    'LogDrop' { '/DLXR_LOG_DROP_CONTROL' }
    'TraceBatchDrop' { '/DLXR_TRACE_BATCH_DROP_CONTROL' }
    'TraceBatchUnbatched' { '/DLXR_TRACE_BATCH_UNBATCHED_CONTROL' }
    'TraceBatchEarly' { '/DLXR_TRACE_BATCH_EARLY_CONTROL' }
    default { '' }
}
$executable = Join-Path $OutputDirectory 'lxr-rc-validation.exe'
$compile = @(
    'cl.exe /nologo /std:c++17 /EHsc /W4 /WX /wd4100 /wd4324 /wd4458',
    $configurationFlags,
    $controlFlag,
    '/DBUILD_AS_STANDALONE /DTARGET_WINDOWS /DHOST_WINDOWS /DWIN32',
    '/DTARGET_AMD64 /DHOST_AMD64 /DTARGET_64BIT /DHOST_64BIT',
    '/DLXR_SIDE_METADATA_TEST_PLATFORM /DLXR_ENABLE_LAZY_COMPLETION_COUNTER',
    "/I`"$gc`" /I`"$gc\env`" /I`"$native`" /I`"$native\inc`"",
    $quoted,
    "/Fe:`"$executable`""
) -join ' '
$command = "call `"$vs`" x64 >nul && cd /d `"$OutputDirectory`" && $compile"
$command | Set-Content (Join-Path $OutputDirectory 'compile-command.txt')
& $env:ComSpec /d /s /c $command *> (Join-Path $OutputDirectory 'build.log')
if ($LASTEXITCODE -ne 0) {
    Get-Content (Join-Path $OutputDirectory 'build.log') -Tail 35
    throw "Integrated P4 compiler failed with exit code $LASTEXITCODE."
}
$selectors = @()
if ($Control -in @('MatureStale', 'MatureReplay')) {
    $selectors = @('--mature-remset')
} elseif ($Control -eq 'MatureShape') {
    $selectors = @('--mature-remset-wrap')
} elseif ($Control -in @('MatureScan', 'MatureReaderCompletion')) {
    $selectors = @('--mature-reader')
} elseif ($Control -eq 'MatureDeadScan') {
    $selectors = @('--mature-completed-scan')
} elseif ($Control -in @('MatureEarlyRelease', 'MatureCount', 'MaturePredictor')) {
    $selectors = @('--mature')
} elseif ($Control -eq 'ReserveUnits') {
    $selectors = @('--p8-policy')
} elseif ($Control -like 'Log*') {
    $selectors = @('--log-storage')
} elseif ($Control -like 'TraceBatch*') {
    $selectors = @('--trace-batching')
}
& $executable @selectors *> (Join-Path $OutputDirectory 'run.log')
$exitCode = $LASTEXITCODE
$output = Get-Content (Join-Path $OutputDirectory 'run.log') -Raw
if ($Control -ne 'None') {
    if ($exitCode -eq 0 -or $output -notmatch 'FAIL:') {
        throw "P4 failure control '$Control' did not detect its injected violation."
    }
    Write-Host "CONTROL $Control detected: exit=$exitCode"
    return
}
Write-Host $output.Trim()
$match = [regex]::Match($output, 'PASS: (?<passed>\d+)/(?<total>\d+) integrated P4 checks')
if ($exitCode -ne 0 -or -not $match.Success -or
    $match.Groups['passed'].Value -ne $match.Groups['total'].Value -or
    [int]$match.Groups['total'].Value -eq 0) {
    throw "Integrated P4 validation failed with exit code $exitCode."
}
[pscustomobject]@{
    platform = 'windows-x64'
    configuration = $Configuration
    passed = [int]$match.Groups['passed'].Value
    total = [int]$match.Groups['total'].Value
    exit_code = $exitCode
    scope = 'native product arenas; not a selectable collector'
} | Export-Csv (Join-Path $OutputDirectory 'validation-summary.csv') -NoTypeInformation
