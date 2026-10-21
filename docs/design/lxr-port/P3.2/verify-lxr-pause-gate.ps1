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
if (@(git -C $RepositoryRoot status --porcelain=v1).Count -ne 0) {
    throw 'The P3.2 archive gate requires a clean worktree.'
}

$sourceCommit = (Get-Content -LiteralPath (
    Join-Path $scriptRoot 'raw\source-commit.txt') -Raw).Trim()
git -C $RepositoryRoot cat-file -e "$sourceCommit`^{commit}"
if ($LASTEXITCODE -ne 0) {
    throw 'Source commit does not exist.'
}
if ((git -C $RepositoryRoot rev-parse HEAD^).Trim() -ne $sourceCommit) {
    throw 'Evidence commit is not a direct child of the recorded source commit.'
}
$changedPaths = @(git -C $RepositoryRoot diff --name-only $sourceCommit HEAD)
if ($changedPaths.Count -eq 0 -or
    @($changedPaths | Where-Object {
        $_ -notlike 'docs/design/lxr-port/P3.2/raw/*'
    }).Count -ne 0) {
    throw 'Evidence commit contains non-raw drift.'
}

$scratch = Join-Path $RepositoryRoot (
    'artifacts\P3.2\archive-gate-' + [guid]::NewGuid().ToString('N'))
$clean = Join-Path $scratch 'clean'
$perturbed = Join-Path $scratch 'perturbed'
$tar = Join-Path $scratch 'tree.tar'
New-Item -ItemType Directory -Path $clean -Force | Out-Null
git -C $RepositoryRoot archive --format=tar --output=$tar HEAD
if ($LASTEXITCODE -ne 0) {
    throw 'Unable to archive the evidence commit.'
}
tar -xf $tar -C $clean
Copy-Item -LiteralPath $clean -Destination $perturbed -Recurse

$results = [Collections.Generic.List[object]]::new()
function Add-Result([string]$Name, [string]$Kind, [string]$Detail) {
    $results.Add([pscustomobject][ordered]@{
        name = $Name
        kind = $Kind
        detail = $Detail
        result = 'PASS'
    })
}

function Replace-ExactlyOnce(
    [string]$Path,
    [string]$Old,
    [string]$New) {
    $bytes = [IO.File]::ReadAllBytes($Path)
    $text = [Text.Encoding]::UTF8.GetString($bytes)
    $newline = if ($text.Contains("`r`n")) { "`r`n" } else { "`n" }
    $oldStyled = $Old.Replace("`r`n", "`n").Replace("`r", "`n").Replace(
        "`n",
        $newline)
    $newStyled = $New.Replace("`r`n", "`n").Replace("`r", "`n").Replace(
        "`n",
        $newline)
    $first = $text.IndexOf($oldStyled, [StringComparison]::Ordinal)
    if ($first -lt 0 -or
        $text.IndexOf(
            $oldStyled,
            $first + $oldStyled.Length,
            [StringComparison]::Ordinal) -ge 0) {
        throw "Expected exactly one perturbation match in '$Path'."
    }
    $updated = $text.Substring(0, $first) +
        $newStyled +
        $text.Substring($first + $oldStyled.Length)
    [IO.File]::WriteAllText(
        $Path,
        $updated,
        [Text.UTF8Encoding]::new($false))
}

$headerPath = 'src\coreclr\gc\lxr_pause_schedule.h'
$schedulePath = 'src\coreclr\gc\lxr_pause_schedule.cpp'
$coordinatorPath = 'src\coreclr\gc\lxr_pause_coordinator.cpp'
$schedulerPath = 'src\coreclr\gc\lxr_work_scheduler.cpp'
$identityPath = 'docs\design\lxr-port\P3.2\raw\source-identities.csv'
$validationPath = 'docs\design\lxr-port\P3.2\raw\validation-summary.csv'
$benchmarkRawPath = 'docs\design\lxr-port\P3.2\raw\benchmark-raw.csv'
$controlsPath = 'docs\design\lxr-port\P3.2\raw\benchmark-controls.csv'
$mutablePaths = @(
    $headerPath,
    $schedulePath,
    $coordinatorPath,
    $schedulerPath,
    $identityPath,
    $validationPath,
    $benchmarkRawPath,
    $controlsPath
)

function Restore-MutablePaths {
    foreach ($relative in $mutablePaths) {
        Copy-Item -LiteralPath (Join-Path $clean $relative) -Destination (
            Join-Path $perturbed $relative) -Force
    }
}

function Invoke-ValidatorFailure(
    [string]$Name,
    [scriptblock]$Mutation) {
    Restore-MutablePaths
    & $Mutation
    $output = Join-Path $scratch "validator-$Name"
    try {
        & (Join-Path $perturbed (
            'docs\design\lxr-port\P3.2\run-lxr-pause-validation.ps1')) `
            -RepositoryRoot $perturbed `
            -OutputDirectory $output *> $null
        throw "Validator perturbation '$Name' unexpectedly passed."
    } catch {
        if ($_.Exception.Message -eq
            "Validator perturbation '$Name' unexpectedly passed.") {
            throw
        }
    }
    $executable = Join-Path $output 'lxr-pause-validation.exe'
    $validationLog = Join-Path $output 'validation.log'
    if (-not (Test-Path -LiteralPath $executable -PathType Leaf) -or
        -not (Test-Path -LiteralPath $validationLog -PathType Leaf) -or
        (Get-Content -LiteralPath $validationLog -Raw) -notmatch '(?m)^FAIL:') {
        throw "Validator perturbation '$Name' failed during setup/build rather than validation."
    }
    Add-Result $Name 'validator-negative-control' 'validator failed'
}

function Invoke-VerifierFailure(
    [string]$Name,
    [scriptblock]$Mutation,
    [string]$ExpectedError) {
    Restore-MutablePaths
    & $Mutation
    try {
        & (Join-Path $perturbed (
            'docs\design\lxr-port\P3.2\verify-lxr-pause.ps1')) `
            -RepositoryRoot $perturbed *> $null
        throw "Verifier perturbation '$Name' unexpectedly passed."
    } catch {
        $message = $_.Exception.Message
        if ($message -eq "Verifier perturbation '$Name' unexpectedly passed.") {
            throw
        }
        if ($message -notlike "*$ExpectedError*") {
            throw "Verifier perturbation '$Name' failed with '$message', not '$ExpectedError'."
        }
        Add-Result $Name 'verifier-negative-control' $message
    }
}

$cleanOutput = @(& (Join-Path $clean (
    'docs\design\lxr-port\P3.2\verify-lxr-pause.ps1')) `
    -RepositoryRoot $clean 6>&1 2>&1)
if (($cleanOutput -join ' ') -notmatch 'PASS: \d+ P3\.2 evidence checks') {
    throw 'Clean archive verifier did not pass.'
}
Add-Result 'clean-archive' 'verifier-pass' ($cleanOutput -join ' ')

$cleanValidationOutput = Join-Path $scratch 'validator-clean'
& (Join-Path $clean (
    'docs\design\lxr-port\P3.2\run-lxr-pause-validation.ps1')) `
    -RepositoryRoot $clean `
    -OutputDirectory $cleanValidationOutput *> $null
if ($LASTEXITCODE -ne 0 -or
    -not (Test-Path -LiteralPath (
        Join-Path $cleanValidationOutput 'validation-summary.csv') -PathType Leaf)) {
    throw 'Clean archive native validator did not pass.'
}
Add-Result 'clean-native-validator' 'validator-pass' 'native validator passed'

Invoke-ValidatorFailure 'pause-order' {
    Replace-ExactlyOnce (Join-Path $perturbed $schedulePath) `
        "        `"RefCount`",`n        `"InitialMark`"," `
        "        `"InitialMark`",`n        `"RefCount`","
}
Invoke-ValidatorFailure 'pause-rename' {
    Replace-ExactlyOnce (Join-Path $perturbed $schedulePath) `
        '        "RefCount",' `
        '        "ReferenceCount",'
}
Invoke-ValidatorFailure 'schedule-full' {
    Replace-ExactlyOnce (Join-Path $perturbed $schedulePath) `
        '{LxrPauseKind::Full, CommonMask | PrepareMask | ClosureMask | DecrementsMask},' `
        '{LxrPauseKind::Full, CommonMask | PrepareMask | DecrementsMask},'
}
Invoke-ValidatorFailure 'schedule-refcount' {
    Replace-ExactlyOnce (Join-Path $perturbed $schedulePath) `
        '{LxrPauseKind::RefCount, CommonMask},' `
        '{LxrPauseKind::RefCount, CommonMask | PrepareMask | ClosureMask | DecrementsMask},'
}
Invoke-ValidatorFailure 'refcount-prepare' {
    Replace-ExactlyOnce (Join-Path $perturbed $schedulePath) `
        '{LxrPauseKind::RefCount, CommonMask},' `
        '{LxrPauseKind::RefCount, CommonMask | PrepareMask},'
}
Invoke-ValidatorFailure 'refcount-closure' {
    Replace-ExactlyOnce (Join-Path $perturbed $schedulePath) `
        '{LxrPauseKind::RefCount, CommonMask},' `
        '{LxrPauseKind::RefCount, CommonMask | ClosureMask},'
}
Invoke-ValidatorFailure 'refcount-decrements' {
    Replace-ExactlyOnce (Join-Path $perturbed $schedulePath) `
        '{LxrPauseKind::RefCount, CommonMask},' `
        '{LxrPauseKind::RefCount, CommonMask | DecrementsMask},'
}
Invoke-ValidatorFailure 'illegal-transition' {
    Replace-ExactlyOnce (Join-Path $perturbed $coordinatorPath) `
        '    if (m_state != LxrPauseCoordinatorState::Idle)' `
        '    if (false)'
}
Invoke-ValidatorFailure 'duplicate-completion' {
    Replace-ExactlyOnce (Join-Path $perturbed $coordinatorPath) @'
    m_current_kind = LxrPauseKind::Count;
    m_state = LxrPauseCoordinatorState::Idle;
'@ @'
    m_current_kind = LxrPauseKind::Count;
    m_state = LxrPauseCoordinatorState::Active;
'@
}
Invoke-ValidatorFailure 'reset-defaults' {
    Replace-ExactlyOnce (Join-Path $perturbed $schedulerPath) @'
        bucket.state = BucketState::Closed;
        bucket.enabled = true;
        bucket.accepting = true;
'@ @'
        bucket.state = BucketState::Closed;
        bucket.enabled = false;
        bucket.accepting = true;
'@
}
Invoke-ValidatorFailure 'drain-accounting' {
    Replace-ExactlyOnce (Join-Path $perturbed $schedulerPath) @'
        return bucket.outstanding == 0 &&
            bucket.executing == 0 &&
            bucket.queue.empty();
'@ @'
        return bucket.queue.empty();
'@
}
Invoke-ValidatorFailure 'resumable-during-pause' {
    Replace-ExactlyOnce (Join-Path $perturbed $schedulerPath) @'
        resumable.accepting = false;
        resumable.enabled = false;
        resumable.state = BucketState::Closed;
'@ @'
        resumable.accepting = false;
        resumable.enabled = true;
        resumable.state = BucketState::Open;
'@
}
Invoke-ValidatorFailure 'resumable-lost' {
    Replace-ExactlyOnce (Join-Path $perturbed $schedulerPath) @'
    if (m_impl->cycleActive || m_impl->cycleCompleted || !m_impl->interruptionRequested)
'@ @'
    if (true)
'@
}

Invoke-VerifierFailure 'source-identity' {
    $rows = @(Import-Csv (Join-Path $perturbed $identityPath))
    $rows[0].sha256 = '0' * 64
    $rows | Export-Csv (Join-Path $perturbed $identityPath) -NoTypeInformation
} 'Source identity mismatch'
Invoke-VerifierFailure 'evidence-cardinality' {
    $rows = @(Import-Csv (Join-Path $perturbed $validationPath))
    $rows | Select-Object -Skip 1 |
        Export-Csv (Join-Path $perturbed $validationPath) -NoTypeInformation
} 'Validation platform evidence is incomplete'
Invoke-VerifierFailure 'benchmark-aa' {
    $rows = @(Import-Csv (Join-Path $perturbed $controlsPath))
    $rows[0].aa_ratio = '2.0'
    $rows | Export-Csv (Join-Path $perturbed $controlsPath) -NoTypeInformation
} 'Benchmark A/A ratio is outside its bounds'
Invoke-VerifierFailure 'benchmark-null' {
    $rows = @(Import-Csv (Join-Path $perturbed $benchmarkRawPath))
    $row = $rows | Where-Object policy -eq 'null' | Select-Object -First 1
    $row.completed_kind_count = '1'
    $rows | Export-Csv (Join-Path $perturbed $benchmarkRawPath) -NoTypeInformation
} 'A null benchmark row reported synthetic suspension work'
Invoke-VerifierFailure 'benchmark-injected-duration' {
    $rows = @(Import-Csv (Join-Path $perturbed $controlsPath))
    $rows[0].injected_median_delta_ms = '0'
    $rows | Export-Csv (Join-Path $perturbed $controlsPath) -NoTypeInformation
} 'Benchmark injected-duration delta is below its bound'

if ($results.Count -ne 20) {
    throw "Archive gate scenario count differs: $($results.Count)/20."
}

New-Item -ItemType Directory -Path (
    Join-Path $RepositoryRoot 'artifacts\P3.2') -Force | Out-Null
$results | Export-Csv (
    Join-Path $RepositoryRoot 'artifacts\P3.2\archive-gate-summary.csv') `
    -NoTypeInformation
Remove-Item -LiteralPath $scratch -Recurse -Force
Write-Host "PASS: $($results.Count) P3.2 archive gate scenarios"
exit 0
