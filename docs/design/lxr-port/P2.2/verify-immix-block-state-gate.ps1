# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

[CmdletBinding()]
param(
    [string]$RepositoryRoot,
    [string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'
$scriptRoot = $PSScriptRoot
if (-not $RepositoryRoot) {
    $RepositoryRoot = (Resolve-Path (Join-Path $scriptRoot '..\..\..\..')).ProviderPath
} else {
    $RepositoryRoot = (Resolve-Path -LiteralPath $RepositoryRoot).ProviderPath
}
if (-not $OutputDirectory) {
    $OutputDirectory = Join-Path $RepositoryRoot (
        'artifacts\P2.2\gate\' + [guid]::NewGuid().ToString('N'))
} else {
    $OutputDirectory = $PSCmdlet.GetUnresolvedProviderPathFromPSPath($OutputDirectory)
}
if (Test-Path -LiteralPath $OutputDirectory) {
    throw "OutputDirectory already exists; caller-owned output is not modified: '$OutputDirectory'."
}

$status = @(git -C $RepositoryRoot -c core.longpaths=true status --porcelain=v1)
if ($LASTEXITCODE -ne 0) {
    throw 'Unable to inspect the repository worktree.'
}
if ($status.Count -ne 0) {
    throw 'The P2.2 gate requires a clean worktree.'
}

$raw = Join-Path $RepositoryRoot 'docs\design\lxr-port\P2.2\raw'
$sourceCommit = (Get-Content -LiteralPath (Join-Path $raw 'source-commit.txt') -Raw).Trim()
if ($sourceCommit -notmatch '^[0-9a-f]{40}$') {
    throw 'Source commit identity is malformed.'
}
git -C $RepositoryRoot cat-file -e "$sourceCommit`^{commit}"
if ($LASTEXITCODE -ne 0) {
    throw 'Source commit does not exist.'
}
$headCommit = (git -C $RepositoryRoot rev-parse HEAD).Trim()
if ($sourceCommit -eq $headCommit) {
    throw 'Source and evidence commits must be distinct.'
}
git -C $RepositoryRoot merge-base --is-ancestor $sourceCommit $headCommit
if ($LASTEXITCODE -ne 0) {
    throw 'Source commit is not an ancestor of the evidence commit.'
}

$sourcePaths = @(
    Get-Content -LiteralPath (
        Join-Path $RepositoryRoot 'docs\design\lxr-port\P2.2\source-manifest.txt') |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
)
foreach ($path in $sourcePaths) {
    git -C $RepositoryRoot diff --quiet $sourceCommit HEAD -- $path
    if ($LASTEXITCODE -ne 0) {
        throw "Source path changed after identity generation: $path"
    }
}

New-Item -ItemType Directory -Path $OutputDirectory | Out-Null
$scratch = Join-Path $OutputDirectory 'scratch'
$clean = Join-Path $scratch 'clean'
$tar = Join-Path $scratch 'tree.tar'
New-Item -ItemType Directory -Path $scratch | Out-Null
New-Item -ItemType Directory -Path $clean | Out-Null
git -C $RepositoryRoot archive --format=tar --output=$tar HEAD
if ($LASTEXITCODE -ne 0) {
    throw 'Unable to archive the committed tree.'
}
tar -xf $tar -C $clean
if ($LASTEXITCODE -ne 0) {
    throw 'Unable to extract the committed tree.'
}

$summary = [Collections.Generic.List[object]]::new()

function Invoke-VerifierPass([string]$Name, [string]$Root) {
    $verifier = Join-Path $Root 'docs\design\lxr-port\P2.2\verify-immix-block-state.ps1'
    try {
        $output = @(& $verifier -RepositoryRoot $Root 6>&1 2>&1)
    } catch {
        throw "Verifier unexpectedly failed for '$Name': $($_.Exception.Message)"
    }
    if (($output -join ' ') -notmatch 'PASS: \d+ P2\.2 evidence checks') {
        throw "Verifier success output is missing for '$Name'."
    }
    $summary.Add([pscustomobject][ordered]@{
        id = $Name
        kind = 'verifier-pass'
        expected = 'PASS'
        observed = ($output -join ' ')
        result = 'PASS'
    })
}

function Invoke-VerifierFailure(
    [string]$Name,
    [string]$Root,
    [string]$ExpectedError) {
    $verifier = Join-Path $Root 'docs\design\lxr-port\P2.2\verify-immix-block-state.ps1'
    $failed = $false
    $message = ''
    try {
        & $verifier -RepositoryRoot $Root 2>&1 | Out-Null
    } catch {
        $failed = $true
        $message = $_.Exception.Message
    }
    if (-not $failed -or ($message -notlike "*$ExpectedError*")) {
        throw "Verifier perturbation '$Name' did not fail with '$ExpectedError'; observed '$message'."
    }
    $summary.Add([pscustomobject][ordered]@{
        id = $Name
        kind = 'verifier-failure'
        expected = $ExpectedError
        observed = $message
        result = 'PASS'
    })
}

function Replace-ExactlyOnce(
    [string]$Path,
    [string]$Old,
    [string]$New) {
    $text = Get-Content -LiteralPath $Path -Raw
    $matches = [regex]::Matches($text, [regex]::Escape($Old))
    if ($matches.Count -ne 1) {
        throw "Expected one replacement token in '$Path', found $($matches.Count)."
    }
    [IO.File]::WriteAllText(
        $Path,
        $text.Replace($Old, $New),
        [Text.UTF8Encoding]::new($false))
}

function New-PerturbationTree([string]$Name) {
    $target = Join-Path $scratch $Name
    Copy-Item -LiteralPath $clean -Destination $target -Recurse
    return $target
}

function Invoke-BehaviorFailure(
    [string]$Name,
    [string]$Root,
    [string]$ExpectedPattern) {
    $runner = Join-Path $Root 'docs\design\lxr-port\P2.2\run-immix-block-validation.ps1'
    $output = Join-Path $OutputDirectory "behavior-$Name"
    $failed = $false
    try {
        & $runner -RepositoryRoot $Root -OutputDirectory $output | Out-Null
    } catch {
        $failed = $true
    }
    $log = Join-Path $output 'x64\run.log'
    $logText = if (Test-Path -LiteralPath $log -PathType Leaf) {
        Get-Content -LiteralPath $log -Raw
    } else {
        ''
    }
    if (-not $failed -or ($logText -notmatch $ExpectedPattern)) {
        throw "Behavior perturbation '$Name' did not fail with pattern '$ExpectedPattern'."
    }
    $summary.Add([pscustomobject][ordered]@{
        id = $Name
        kind = 'behavior-failure'
        expected = $ExpectedPattern
        observed = [IO.Path]::GetFileName($log)
        result = 'PASS'
    })
}

Invoke-VerifierPass 'clean-archive-1' $clean
Invoke-VerifierPass 'clean-archive-2' $clean

$tree = New-PerturbationTree 'geometry-block-log'
Replace-ExactlyOnce `
    (Join-Path $tree 'src\coreclr\gc\immix_block.h') `
    'BlockLogBytes = 15' `
    'BlockLogBytes = 14'
Invoke-BehaviorFailure 'geometry-block-log' $tree 'static assertion|static_assert|C2607|C2338'

$tree = New-PerturbationTree 'geometry-line-log'
Replace-ExactlyOnce `
    (Join-Path $tree 'src\coreclr\gc\immix_block.h') `
    'LineLogBytes = 8' `
    'LineLogBytes = 9'
Invoke-BehaviorFailure 'geometry-line-log' $tree 'static assertion|static_assert|C2607|C2338'

$tree = New-PerturbationTree 'transition-unallocated-mark'
Replace-ExactlyOnce `
    (Join-Path $tree 'src\coreclr\gc\immix_block.cpp') `
    @'
    if (result == SideMetadataResult::Success && state == ImmixBlockState::Unallocated)
    {
        *status = ImmixBlockOperationStatus::InvalidTransition;
        Unlock(block);
        return SideMetadataResult::Success;
    }
    if (result == SideMetadataResult::Success)
    {
        result = TransitionLocked(
            block,
            state,
            unavailableLines,
            ImmixBlockState::Marked,
'@ `
    @'
    if (result == SideMetadataResult::Success)
    {
        result = TransitionLocked(
            block,
            state,
            unavailableLines,
            ImmixBlockState::Marked,
'@
Invoke-BehaviorFailure 'transition-unallocated-mark' $tree 'FAIL: matrix mark status'

$tree = New-PerturbationTree 'transition-unallocated-reuse'
Replace-ExactlyOnce `
    (Join-Path $tree 'src\coreclr\gc\immix_block.cpp') `
    @'
    if (result == SideMetadataResult::Success && state == ImmixBlockState::Unallocated)
    {
        *status = ImmixBlockOperationStatus::InvalidTransition;
        Unlock(block);
        return SideMetadataResult::Success;
    }
    if (result == SideMetadataResult::Success)
    {
        result = TransitionLocked(
            block,
            state,
            oldUnavailableLines,
            ImmixBlockState::Reusable,
'@ `
    @'
    if (result == SideMetadataResult::Success)
    {
        result = TransitionLocked(
            block,
            state,
            oldUnavailableLines,
            ImmixBlockState::Reusable,
'@
Invoke-BehaviorFailure 'transition-unallocated-reuse' $tree 'FAIL: matrix reusable status'

$tree = New-PerturbationTree 'atomicity-state-cas'
Replace-ExactlyOnce `
    (Join-Path $tree 'src\coreclr\gc\immix_block.cpp') `
    @'
        desiredRaw,
        expectedRaw,
        &observed,
        &exchanged);
'@ `
    @'
        desiredRaw,
        desiredRaw,
        &observed,
        &exchanged);
'@
Invoke-BehaviorFailure 'atomicity-state-cas' $tree 'FAIL: mark updated'

$tree = New-PerturbationTree 'atomicity-phase-gate'
Replace-ExactlyOnce `
    (Join-Path $tree 'src\coreclr\gc\immix_block.cpp') `
    @'
    return CompareExchangeByte(
        LxrSideMetadataKind::BlockInUse,
        block,
        1,
        0,
        &observed,
        acquired);
'@ `
    @'
    return CompareExchangeByte(
        LxrSideMetadataKind::BlockLog,
        block,
        1,
        0,
        &observed,
        acquired);
'@
Invoke-BehaviorFailure 'atomicity-phase-gate' $tree 'FAIL: second acquire stale'

$tree = New-PerturbationTree 'epoch-pause-start'
Replace-ExactlyOnce `
    (Join-Path $tree 'src\coreclr\gc\immix_block.cpp') `
    @'
    m_global_phase_epoch.store(
        static_cast<uint8_t>(oldEpoch + 1),
        std::memory_order_seq_cst);
'@ `
    @'
    m_global_phase_epoch.store(
        oldEpoch,
        std::memory_order_seq_cst);
'@
Invoke-BehaviorFailure 'epoch-pause-start' $tree 'FAIL: pause epoch even'

$tree = New-PerturbationTree 'epoch-release'
Replace-ExactlyOnce `
    (Join-Path $tree 'src\coreclr\gc\immix_block.cpp') `
    ': static_cast<uint8_t>(oldEpoch + 1);' `
    ': oldEpoch;'
Invoke-BehaviorFailure 'epoch-release' $tree 'FAIL: release epoch odd'

$tree = New-PerturbationTree 'epoch-wrap-limit'
Replace-ExactlyOnce `
    (Join-Path $tree 'src\coreclr\gc\immix_block.h') `
    'LastPhaseEpoch = 254' `
    'LastPhaseEpoch = 6'
Invoke-BehaviorFailure 'epoch-wrap-limit' $tree 'FAIL: epoch reaches 253'

$tree = New-PerturbationTree 'epoch-wrap-reset'
Replace-ExactlyOnce `
    (Join-Path $tree 'src\coreclr\gc\immix_block.cpp') `
    @'
            LxrSideMetadataKind::PhaseEpoch,
            0);
'@ `
    @'
            LxrSideMetadataKind::PhaseEpoch,
            1);
'@
Invoke-BehaviorFailure 'epoch-wrap-reset' $tree 'FAIL: old block epoch is zero'

$tree = New-PerturbationTree 'predicate-nursery-reusing'
Replace-ExactlyOnce `
    (Join-Path $tree 'src\coreclr\gc\immix_block.cpp') `
    '*result = (state == ImmixBlockState::Unallocated) && nurseryOrReusing;' `
    '*result = (state != ImmixBlockState::Unallocated) && nurseryOrReusing;'
Invoke-BehaviorFailure 'predicate-nursery-reusing' $tree 'FAIL: fresh block is nursery'

$tree = New-PerturbationTree 'predicate-gc-reusing'
Replace-ExactlyOnce `
    (Join-Path $tree 'src\coreclr\gc\immix_block.cpp') `
    '*result = blockEpoch == globalEpoch;' `
    '*result = blockEpoch == static_cast<uint8_t>(globalEpoch - 1);'
Invoke-BehaviorFailure 'predicate-gc-reusing' $tree 'FAIL: promoted is GC reusing'

$tree = New-PerturbationTree 'predicate-gc-reusing-unallocated'
Replace-ExactlyOnce `
    (Join-Path $tree 'src\coreclr\gc\immix_block.cpp') `
    @'
    if ((metadataResult != SideMetadataResult::Success) ||
        (state == ImmixBlockState::Unallocated))
    {
        return metadataResult;
    }

'@ `
    @'
    if (metadataResult != SideMetadataResult::Success)
    {
        return metadataResult;
    }

'@
Invoke-BehaviorFailure `
    'predicate-gc-reusing-unallocated' `
    $tree `
    'FAIL: released block is not GC reusing'

$tree = New-PerturbationTree 'eligibility-mutator-reusing'
Replace-ExactlyOnce `
    (Join-Path $tree 'src\coreclr\gc\immix_block.cpp') `
    @'
    bool reusing =
        (state != ImmixBlockState::Unallocated) && nurseryOrReusing;
'@ `
    @'
    bool reusing = false;
'@
Invoke-BehaviorFailure `
    'eligibility-mutator-reusing' `
    $tree `
    'FAIL: same-mutator-phase reuse stale'

$tree = New-PerturbationTree 'eligibility-gc-reusing'
Replace-ExactlyOnce `
    (Join-Path $tree 'src\coreclr\gc\immix_block.cpp') `
    @'
    bool gcReusing =
        (state != ImmixBlockState::Unallocated) &&
        ((globalEpoch & 1) == 0) &&
        (blockEpoch == globalEpoch);
'@ `
    @'
    bool gcReusing = false;
'@
Invoke-BehaviorFailure `
    'eligibility-gc-reusing' `
    $tree `
    'FAIL: same-GC copy reuse stale'

$tree = New-PerturbationTree 'eligibility-clean-copy-nursery'
Replace-ExactlyOnce `
    (Join-Path $tree 'src\coreclr\gc\immix_block.cpp') `
    @'
    bool nursery =
        (state == ImmixBlockState::Unallocated) && nurseryOrReusing;
'@ `
    @'
    bool nursery = false;
'@
Invoke-BehaviorFailure `
    'eligibility-clean-copy-nursery' `
    $tree `
    'FAIL: normal copy nursery is stale'

$tree = New-PerturbationTree 'identity-source-hash'
Add-Content -LiteralPath (
    Join-Path $tree 'src\coreclr\gc\immix_block.cpp') -Value 'identity-perturbation'
Invoke-VerifierFailure `
    'identity-source-hash' `
    $tree `
    'Source identity mismatch: src/coreclr/gc/immix_block.cpp'

$tree = New-PerturbationTree 'identity-source-path'
$path = Join-Path $tree 'docs\design\lxr-port\P2.2\raw\source-identities.csv'
$rows = @(Import-Csv $path)
$rows[1..($rows.Count - 1)] | Export-Csv $path -NoTypeInformation
Invoke-VerifierFailure 'identity-source-path' $tree 'Source identity path set mismatch.'

$tree = New-PerturbationTree 'evidence-missing-row'
$path = Join-Path $tree 'docs\design\lxr-port\P2.2\raw\validation-summary.csv'
$rows = @(Import-Csv $path | Where-Object platform -ne 'linux-x64')
$rows | Export-Csv $path -NoTypeInformation
Invoke-VerifierFailure 'evidence-missing-row' $tree 'Validation platform evidence is incomplete.'

$tree = New-PerturbationTree 'evidence-duplicate-row'
$path = Join-Path $tree 'docs\design\lxr-port\P2.2\raw\runtime-smoke-summary.csv'
$rows = @(Import-Csv $path)
@($rows + $rows[0]) | Export-Csv $path -NoTypeInformation
Invoke-VerifierFailure 'evidence-duplicate-row' $tree 'Runtime smoke evidence is incomplete.'

$tree = New-PerturbationTree 'benchmark-aa'
$path = Join-Path $tree 'docs\design\lxr-port\P2.2\raw\benchmark-controls.csv'
$rows = @(Import-Csv $path)
$rows[0].aa_ratio = '2.0'
$rows | Export-Csv $path -NoTypeInformation
Invoke-VerifierFailure 'benchmark-aa' $tree 'Benchmark A/A ratio is above its bound.'

$tree = New-PerturbationTree 'benchmark-injected-cost'
$path = Join-Path $tree 'docs\design\lxr-port\P2.2\raw\benchmark-controls.csv'
$rows = @(Import-Csv $path)
$rows[0].extra_cas_ratio = '1.0'
$rows | Export-Csv $path -NoTypeInformation
Invoke-VerifierFailure `
    'benchmark-injected-cost' `
    $tree `
    'Benchmark extra-CAS control is below its bound.'

$manifest = Get-Content -LiteralPath (
    Join-Path $clean 'docs\design\lxr-port\P2.2\evidence-manifest.json') -Raw |
    ConvertFrom-Json
$expectedCount = 2 + $manifest.perturbations.Count
if ($summary.Count -ne $expectedCount) {
    throw "Gate result count differs: $($summary.Count)/$expectedCount."
}

$summary | Export-Csv (Join-Path $OutputDirectory 'gate-summary.csv') -NoTypeInformation
Remove-Item -LiteralPath $scratch -Recurse -Force
Write-Host "PASS: $($summary.Count) P2.2 archive gate scenarios"
Write-Host "Output: $OutputDirectory"
exit 0
