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

function Require-Pattern([string]$relativePath, [string]$pattern) {
    $path = Join-Path $RepositoryRoot $relativePath
    Confirm (Test-Path -LiteralPath $path -PathType Leaf) "Missing $relativePath"
    if (Test-Path -LiteralPath $path -PathType Leaf) {
        Confirm ([bool](Select-String -LiteralPath $path -SimpleMatch $pattern -Quiet)) (
            "$relativePath does not contain '$pattern'")
    }
}

function Require-PatternCount([string]$relativePath, [string]$pattern, [int]$expectedCount) {
    $path = Join-Path $RepositoryRoot $relativePath
    Confirm (Test-Path -LiteralPath $path -PathType Leaf) "Missing $relativePath"
    if (Test-Path -LiteralPath $path -PathType Leaf) {
        $actualCount = @(Select-String -LiteralPath $path -SimpleMatch $pattern).Count
        Confirm ($actualCount -eq $expectedCount) (
            "$relativePath contains '$pattern' $actualCount times; expected $expectedCount.")
    }
}

function Forbid-Pattern([string]$relativePath, [string]$pattern) {
    $path = Join-Path $RepositoryRoot $relativePath
    Confirm (Test-Path -LiteralPath $path -PathType Leaf) "Missing $relativePath"
    if (Test-Path -LiteralPath $path -PathType Leaf) {
        Confirm (-not [bool](Select-String -LiteralPath $path -SimpleMatch $pattern -Quiet)) (
            "$relativePath unexpectedly contains '$pattern'")
    }
}

$document = Join-Path $RepositoryRoot 'docs\design\lxr-port\P1.2-x64-slot-log-barrier.md'
$microPath = Join-Path $scriptRoot 'raw\microbenchmark.csv'
$endToEndPath = Join-Path $scriptRoot 'raw\end-to-end-summary.csv'
$fastPath = Join-Path $scriptRoot 'raw\lowalloc-fast-summary.csv'
$churnFastPath = Join-Path $scriptRoot 'raw\churn-fast-summary.csv'
$pairedPath = Join-Path $scriptRoot 'raw\paired-ratio-summary.csv'
$rangeFollowupPath = Join-Path $scriptRoot 'raw\range-followup.csv'
$interpreterFoldingPath = Join-Path $scriptRoot 'raw\interpreter-folding.txt'

foreach ($path in @(
    $document,
    $microPath,
    $endToEndPath,
    $fastPath,
    $churnFastPath,
    $pairedPath,
    $rangeFollowupPath,
    $interpreterFoldingPath
)) {
    Confirm (Test-Path -LiteralPath $path -PathType Leaf) "Missing shipped artifact $path"
}

Require-Pattern 'src\coreclr\gc\gcinterface.h' 'GC_WRITE_BARRIER_COMPLETE_STORE_INTERFACE_MINOR_VERSION 10'
Require-Pattern 'src\coreclr\gc\gcinterface.h' 'GC_WRITE_BARRIER_EPOCH_RESET_INTERFACE_MINOR_VERSION 11'
Require-Pattern 'src\coreclr\gc\gcinterface.h' 'write_barrier_epoch_reset'
Require-Pattern 'src\coreclr\vm\amd64\JitHelpers_FastWriteBarriers.asm' 'JIT_WriteBarrier_SlotLog64'
Require-Pattern 'src\coreclr\vm\amd64\jithelpers_fastwritebarriers.S' 'JIT_WriteBarrier_SlotLog64'
Require-PatternCount 'src\coreclr\vm\amd64\JitHelpers_FastWriteBarriers.asm' 'xor     al, 0A5h' 2
Require-PatternCount 'src\coreclr\vm\amd64\jithelpers_fastwritebarriers.S' 'xor     al, 0xA5' 2
Require-PatternCount 'src\coreclr\vm\writebarriermanager.cpp' '0x34 == *(m_pSlotLogPolarity - 1)' 2
Require-PatternCount 'src\coreclr\vm\writebarriermanager.cpp' '0xa5 == *m_pSlotLogPolarity' 2
Require-Pattern 'src\coreclr\vm\amd64\SlotLogWriteBarrier.asm' 'JIT_WriteBarrier_SlotLog_Slow'
Require-Pattern 'src\coreclr\vm\amd64\slotlogwritebarrier.S' 'JIT_WriteBarrier_SlotLog_Slow'
Require-Pattern 'src\coreclr\jit\assertionprop.cpp' 'JIT_FLAG_WRITE_BARRIER_REQUIRES_OLD_VALUE'
Require-Pattern 'src\coreclr\interpreter\compiler.cpp' 'RequiresOldValueWriteBarrier'
Require-Pattern 'src\coreclr\interpreter\compiler.cpp' 'CORJIT_FLAG_WRITE_BARRIER_REQUIRES_OLD_VALUE'
Forbid-Pattern 'src\coreclr\interpreter\inc\intops.def' 'INTOP_LDC_I4_WRITE_BARRIER_REQUIRES_OLD_VALUE'
Forbid-Pattern 'src\coreclr\vm\interpexec.cpp' 'INTOP_LDC_I4_WRITE_BARRIER_REQUIRES_OLD_VALUE'
Require-Pattern 'src\coreclr\jit\lower.cpp' 'TryDecomposeInitBlockStoreAsIndirs'
Require-Pattern 'src\coreclr\System.Private.CoreLib\src\System\Buffer.CoreCLR.cs' 'ClearWithOldValueWriteBarrier'
Require-Pattern 'src\coreclr\System.Private.CoreLib\src\System\Object.CoreCLR.cs' 'dataOffset'
Require-Pattern 'src\libraries\System.Private.CoreLib\src\System\Runtime\CompilerServices\RuntimeHelpers.cs' 'RequiresOldValueWriteBarrier'
Require-Pattern 'src\coreclr\vm\arraynative.inl' 'ErectWriteBarrierRangePre'
Require-Pattern 'src\coreclr\vm\comutilnative.cpp' 'ErectWriteBarrierPre'
Require-Pattern 'src\coreclr\vm\gchelpers.cpp' 'SoftwareWriteWatchSetDirtyRegion'
Require-Pattern 'src\coreclr\vm\gchelpers.cpp' 'ErectWriteBarrierLayoutRangePre'
Require-Pattern 'src\coreclr\vm\gchelpers.cpp' 'return ErectWriteBarrierRangePre('
Require-Pattern 'src\coreclr\vm\gchelpers.cpp' 'GC_WriteBarrierTest_InvokeEmptyRange'
Require-Pattern 'src\coreclr\gc\standardwritebarriertest.cpp' '(source == nullptr) ? nullptr : source[index]'
Require-Pattern 'src\coreclr\gc\standardwritebarriertest.cpp' 'g_write_barrier_test_dependent_edge_call_count'
Require-Pattern 'src\coreclr\vm\interpexec.cpp' 'liveLocalsIntervals'
Require-Pattern 'src\coreclr\vm\interpexec.cpp' 'map->GetHighestSeries()'

$gcHelpersPath = Join-Path $RepositoryRoot 'src\coreclr\vm\gchelpers.cpp'
if (Test-Path -LiteralPath $gcHelpersPath) {
    $gcHelpersText = Get-Content -LiteralPath $gcHelpersPath -Raw
    Confirm ($gcHelpersText -match (
        '(?s)void ErectWriteBarrierForMT\(MethodTable \*\*dst, MethodTable \*ref\).*?' +
        'if \(ref->Collectible\(\)\)\s*\{\s*' +
        'Object\* newLoaderAllocator.*?' +
        'ErectWriteBarrierDependentEdgePre\(dst, nullptr, newLoaderAllocator\);\s*\}\s*' +
        '\*dst = ref;')) (
        'ErectWriteBarrierForMT does not gate its dependent edge on a collectible MethodTable.')
}

if (Test-Path -LiteralPath $microPath) {
    $micro = @(Import-Csv -LiteralPath $microPath)
    $arms = @($micro.Arm | Sort-Object -Unique)
    $methods = @($micro.Method | Sort-Object -Unique)
    Confirm ($arms.Count -gt 1) 'Microbenchmark does not contain multiple arms.'
    Confirm ($methods.Count -gt 1) 'Microbenchmark does not contain both field and array methods.'
    foreach ($arm in $arms) {
        Confirm ((@($micro | Where-Object Arm -eq $arm).Count) -eq $methods.Count) (
            "Microbenchmark arm '$arm' does not contain every method.")
    }
}

$publishedRatios = @{}
if (Test-Path -LiteralPath $endToEndPath) {
    $endToEnd = @(Import-Csv -LiteralPath $endToEndPath)
    $scenarios = @($endToEnd.Scenario | Sort-Object -Unique)
    $gcs = @($endToEnd.GC | Sort-Object -Unique)
    foreach ($scenario in $scenarios) {
        Confirm ((@($endToEnd | Where-Object Scenario -eq $scenario).Count) -eq $gcs.Count) (
            "Scenario '$scenario' does not contain every GC mode.")
    }

    foreach ($row in $endToEnd) {
        $ratio = [double]$row.SlotOpsPerSecond / [double]$row.StockOpsPerSecond
        $allocationRatio =
            [double]$row.SlotAllocatedMbPerSecond / [double]$row.StockAllocatedMbPerSecond
        Confirm ([Math]::Abs($ratio - [double]$row.SlotOverStock) -lt 0.0001) (
            "Throughput ratio does not rederive for $($row.Scenario)/$($row.GC).")
        Confirm ([Math]::Abs($allocationRatio - [double]$row.AllocationRateRatio) -lt 0.0001) (
            "Allocation ratio does not rederive for $($row.Scenario)/$($row.GC).")
        $publishedRatios["$($row.Scenario)|$($row.GC)"] = [double]$row.SlotOverStock
    }
}

if (Test-Path -LiteralPath $fastPath) {
    foreach ($row in @(Import-Csv -LiteralPath $fastPath)) {
        $ratio = [double]$row.SlotFastOpsPerSecond / [double]$row.StockOpsPerSecond
        Confirm ([Math]::Abs($ratio - [double]$row.SlotFastOverStock) -lt 0.0001) (
            "Fast-path ratio does not rederive for $($row.GC).")
        Confirm ([int]$row.Collections -eq 0) "Fast-path run collected for $($row.GC)."
        $publishedRatios["low-allocation-compute-fast|$($row.GC)"] =
            [double]$row.SlotFastOverStock
    }
}

if (Test-Path -LiteralPath $churnFastPath) {
    foreach ($row in @(Import-Csv -LiteralPath $churnFastPath)) {
        $ratio = [double]$row.SlotFastOpsPerSecond / [double]$row.StockOpsPerSecond
        $allocationRatio =
            [double]$row.SlotFastAllocatedMbPerSecond / [double]$row.StockAllocatedMbPerSecond
        Confirm ([Math]::Abs($ratio - [double]$row.SlotFastOverStock) -lt 0.0001) (
            "Claimed-bit churn ratio does not rederive for $($row.GC).")
        Confirm ([Math]::Abs($allocationRatio - [double]$row.AllocationRateRatio) -lt 0.0001) (
            "Claimed-bit churn allocation ratio does not rederive for $($row.GC).")
        Confirm (
            ([double]$row.PairedMin -le [double]$row.SlotFastOverStock) -and
            ([double]$row.SlotFastOverStock -le [double]$row.PairedMax)
        ) "Claimed-bit churn paired bounds do not contain the ratio for $($row.GC)."
        Confirm ([int]$row.SlotGen2 -gt 0) "Claimed-bit churn did not exercise a full GC for $($row.GC)."
    }
}

if (Test-Path -LiteralPath $pairedPath) {
    foreach ($row in @(Import-Csv -LiteralPath $pairedPath)) {
        $key = "$($row.Scenario)|$($row.GC)"
        Confirm ($publishedRatios.ContainsKey($key)) "Paired ratio '$key' has no published source."
        if ($publishedRatios.ContainsKey($key)) {
            Confirm ([Math]::Abs($publishedRatios[$key] - [double]$row.MeanRatio) -lt 0.001) (
                "Paired ratio does not agree with the published mean for '$key'.")
        }
        Confirm (
            ([double]$row.Min -le [double]$row.MeanRatio) -and
            ([double]$row.MeanRatio -le [double]$row.Max)
        ) "Paired ratio bounds do not contain the mean for '$key'."
    }
}

if (Test-Path -LiteralPath $rangeFollowupPath) {
    foreach ($row in @(Import-Csv -LiteralPath $rangeFollowupPath)) {
        $ratio = [double]$row.CorrectedMeanNs / [double]$row.PreFixMeanNs
        Confirm ([Math]::Abs($ratio - [double]$row.CorrectedOverPreFix) -lt 0.0001) (
            "Range follow-up ratio does not rederive for $($row.Method).")
    }
}

if (Test-Path -LiteralPath $interpreterFoldingPath) {
    $interpreterFolding = Get-Content -LiteralPath $interpreterFoldingPath -Raw
    Confirm ($interpreterFolding -match '(?s)CardTable unoptimized IR:.*IL_002d: ldc\.i4.*?, 0') (
        'Interpreter evidence omits the CardTable constant-zero fold.')
    Confirm ($interpreterFolding -match '(?s)SlotLog unoptimized IR:.*IL_002d: ldc\.i4.*?, 1') (
        'Interpreter evidence omits the slot-log constant-one fold.')
    Confirm ($interpreterFolding -match 'There is no execution-time write-barrier-mode opcode') (
        'Interpreter evidence does not state the execution-time opcode result.')
}

if (Test-Path -LiteralPath $document) {
    $text = Get-Content -LiteralPath $document -Raw
    Confirm ($text -notmatch 'C:\\github\\runtimelab') 'Document references the abandoned runtimelab tree.'
    Confirm ($text -match 'abbdd1d') 'Document omits the PLDI binding revision.'
    Confirm ($text -match 'df8d30a3') 'Document omits the PLDI core revision.'
    Confirm ($text -match '0682434') 'Document omits the HEAD binding revision.'
    Confirm ($text -match '304ce69d') 'Document omits the HEAD core revision.'
}

if ($failures.Count -ne 0) {
    foreach ($failure in $failures) {
        Write-Error $failure
    }
    Write-Host "RESULT: FAIL ($($failures.Count) failures across $checks checks)"
    exit 1
}

Write-Host "RESULT: PASS ($checks checks)"
exit 0
