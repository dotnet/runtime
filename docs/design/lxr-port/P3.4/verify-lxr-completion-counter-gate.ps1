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
        'artifacts\P3.4\gate\' + [guid]::NewGuid().ToString('N'))
} else {
    $OutputDirectory = $PSCmdlet.GetUnresolvedProviderPathFromPSPath($OutputDirectory)
}
if (Test-Path -LiteralPath $OutputDirectory) {
    throw "OutputDirectory already exists: '$OutputDirectory'."
}
if (@(git -C $RepositoryRoot status --porcelain=v1 -uall).Count -ne 0) {
    throw 'The P3.4 archive gate requires a clean worktree.'
}

$sourceCommit = (Get-Content -LiteralPath (
    Join-Path $scriptRoot 'raw\source-commit.txt') -Raw).Trim()
git -C $RepositoryRoot cat-file -e "$sourceCommit`^{commit}"
if ($LASTEXITCODE -ne 0) {
    throw 'Source commit does not exist.'
}
$parentCommit = (git -C $RepositoryRoot rev-parse HEAD^).Trim()
if ($parentCommit -ne $sourceCommit) {
    throw 'Evidence commit is not a direct child of the recorded source commit.'
}
$changedPaths = @(git -C $RepositoryRoot diff --name-only $sourceCommit HEAD)
if ($changedPaths.Count -eq 0 -or
    @($changedPaths | Where-Object {
        $_ -notlike 'docs/design/lxr-port/P3.4/raw/*'
    }).Count -ne 0) {
    throw 'Evidence child contains non-raw changes or no changes.'
}

New-Item -ItemType Directory -Path $OutputDirectory | Out-Null
$scratch = Join-Path $OutputDirectory 'scratch'
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
function Add-Result([string]$Name, [string]$Kind, [string]$Observed) {
    $script:results.Add([pscustomobject][ordered]@{
        id = $Name
        kind = $Kind
        observed = $Observed
        result = 'PASS'
    })
}

function Replace-ExactlyOnce(
    [string]$Path,
    [string]$Old,
    [string]$New) {
    $text = Get-Content -LiteralPath $Path -Raw
    $newLine = if ($text.Contains("`r`n")) { "`r`n" } else { "`n" }
    $oldForFile = $Old.Replace("`r`n", "`n").Replace("`n", $newLine)
    $newForFile = $New.Replace("`r`n", "`n").Replace("`n", $newLine)
    $count = [regex]::Matches($text, [regex]::Escape($oldForFile)).Count
    if ($count -ne 1) {
        throw "Expected one replacement in '$Path', found $count."
    }
    [IO.File]::WriteAllText(
        $Path,
        $text.Replace($oldForFile, $newForFile),
        [Text.UTF8Encoding]::new($false))
}

$counterPath = 'src\coreclr\gc\lxr_completion_counter.cpp'
$schedulerPath = 'src\coreclr\gc\lxr_work_scheduler.cpp'
$validatorPath = 'docs\design\lxr-port\P3.4\lxr-completion-counter-validation.cpp'
$identityPath = 'docs\design\lxr-port\P3.4\raw\source-identities.csv'
$validationPath = 'docs\design\lxr-port\P3.4\raw\validation-summary.csv'
$benchmarkPath = 'docs\design\lxr-port\P3.4\raw\benchmark-raw.csv'
$controlsPath = 'docs\design\lxr-port\P3.4\raw\benchmark-controls.csv'
$script:allPerturbationPaths = @(
    $counterPath,
    $schedulerPath,
    $validatorPath,
    $identityPath,
    $validationPath,
    $benchmarkPath,
    $controlsPath
)

function Restore-All {
    foreach ($path in $script:allPerturbationPaths) {
        Copy-Item -LiteralPath (Join-Path $clean $path) -Destination (
            Join-Path $perturbed $path) -Force
    }
}

function Invoke-ValidatorFailure([string]$Name, [scriptblock]$Perturb) {
    Restore-All
    & $Perturb
    Replace-ExactlyOnce (Join-Path $perturbed $validatorPath) `
        "    constexpr uint32_t TimeoutMilliseconds = 30'000;" `
        '    constexpr uint32_t TimeoutMilliseconds = 500;'
    $output = Join-Path $OutputDirectory "validator-$Name"
    $failed = $false
    try {
        & (Join-Path $perturbed (
            'docs\design\lxr-port\P3.4\run-lxr-completion-counter-validation.ps1')) `
            -RepositoryRoot $perturbed -OutputDirectory $output | Out-Null
    } catch {
        $failed = $true
    }
    if (-not $failed) {
        throw "Validator perturbation '$Name' unexpectedly passed."
    }
    Add-Result $Name 'validator-negative-control' 'validator failed'
}

function Invoke-VerifierFailure(
    [string]$Name,
    [scriptblock]$Perturb,
    [string]$ExpectedError) {
    Restore-All
    & $Perturb
    $message = ''
    try {
        & (Join-Path $perturbed (
            'docs\design\lxr-port\P3.4\verify-lxr-completion-counter.ps1')) `
            -RepositoryRoot $perturbed | Out-Null
    } catch {
        $message = $_.Exception.Message
    }
    if ($message -notlike "*$ExpectedError*") {
        throw "Verifier perturbation '$Name' did not fail with '$ExpectedError'; observed '$message'."
    }
    Add-Result $Name 'verifier-negative-control' $message
}

$cleanOutput = @(& (Join-Path $clean (
    'docs\design\lxr-port\P3.4\verify-lxr-completion-counter.ps1')) `
    -RepositoryRoot $clean 6>&1 2>&1)
if (($cleanOutput -join ' ') -notmatch 'PASS: \d+ P3\.4 evidence checks') {
    throw 'Clean archive verifier did not pass.'
}
& (Join-Path $clean (
    'docs\design\lxr-port\P3.4\run-lxr-completion-counter-validation.ps1')) `
    -RepositoryRoot $clean `
    -OutputDirectory (Join-Path $OutputDirectory 'clean-validation') | Out-Null
Add-Result 'clean-archive' 'verifier-and-validator-pass' ($cleanOutput -join ' ')

Invoke-ValidatorFailure 'overall-balance' {
    Replace-ExactlyOnce (Join-Path $perturbed $counterPath) `
        '    m_overall_outstanding.store(1, std::memory_order_seq_cst);' `
        '    m_overall_outstanding.store(0, std::memory_order_seq_cst);'
}
Invoke-ValidatorFailure 'decrement-balance' {
    Replace-ExactlyOnce (Join-Path $perturbed $counterPath) `
        '    m_decrement_outstanding.store(1, std::memory_order_seq_cst);' `
        '    m_decrement_outstanding.store(0, std::memory_order_seq_cst);'
}
Invoke-ValidatorFailure 'underflow' {
    Replace-ExactlyOnce (Join-Path $perturbed $validatorPath) `
        '            seed.Complete() == LxrCompletionResult::AlreadyCompleted &&' `
        '            seed.Complete() == LxrCompletionResult::Success &&'
}
Invoke-ValidatorFailure 'overflow' {
    Replace-ExactlyOnce (Join-Path $perturbed $validatorPath) `
        '                &overflow) == LxrCompletionResult::Overflow,' `
        '                &overflow) == LxrCompletionResult::Success,'
}
Invoke-ValidatorFailure 'decrement-zero-once' {
    Replace-ExactlyOnce (Join-Path $perturbed $counterPath) `
        '            m_decrement_zero_transitions.fetch_add(1, std::memory_order_seq_cst);' `
        '            m_decrement_zero_transitions.fetch_add(2, std::memory_order_seq_cst);'
}
Invoke-ValidatorFailure 'overall-zero-once' {
    Replace-ExactlyOnce (Join-Path $perturbed $counterPath) `
        '    m_overall_zero_transitions.fetch_add(1, std::memory_order_seq_cst);' `
        '    m_overall_zero_transitions.fetch_add(2, std::memory_order_seq_cst);'
}
Invoke-ValidatorFailure 'zero-order' {
    Replace-ExactlyOnce (Join-Path $perturbed $validatorPath) `
        '            context.overallOrder.load(std::memory_order_seq_cst) == 2,' `
        '            context.overallOrder.load(std::memory_order_seq_cst) == 1,'
}
Invoke-ValidatorFailure 'release-before-drain' {
    Replace-ExactlyOnce (Join-Path $perturbed $counterPath) `
        '        registration.m_kind != LxrCompletionWorkKind::MatureSweep)' `
        '        false)'
    Replace-ExactlyOnce (Join-Path $perturbed $counterPath) `
        @'
    if (m_generation.load(std::memory_order_seq_cst) != generation ||
        m_decrement_outstanding.load(std::memory_order_seq_cst) != 0)
'@ `
        @'
    if (m_generation.load(std::memory_order_seq_cst) != generation)
'@
    Replace-ExactlyOnce (Join-Path $perturbed $counterPath) `
        @'
    return lifecycle == LxrCompletionLifecycle::Draining ||
        lifecycle == LxrCompletionLifecycle::DecisionRunning;
'@ `
        '    return true;'
}
Invoke-ValidatorFailure 'promotion-order' {
    Replace-ExactlyOnce (Join-Path $perturbed $validatorPath) `
        '            context.promotionOrder.load(std::memory_order_seq_cst) == 1 &&' `
        '            context.promotionOrder.load(std::memory_order_seq_cst) == 2 &&'
}
Invoke-ValidatorFailure 'child-before-parent' {
    Replace-ExactlyOnce (Join-Path $perturbed $validatorPath) `
        '                blocked.decrementOutstanding == 1 &&' `
        '                blocked.decrementOutstanding == 0 &&'
}
Invoke-ValidatorFailure 'callback-outside-lock' {
    Replace-ExactlyOnce (Join-Path $perturbed $validatorPath) `
        '            context.reentrantQueries.load(std::memory_order_seq_cst) == 2,' `
        '            context.reentrantQueries.load(std::memory_order_seq_cst) == 0,'
}
Invoke-ValidatorFailure 'stale-generation' {
    Replace-ExactlyOnce (Join-Path $perturbed $validatorPath) `
        '                LxrWorkSchedulerResult::StaleGeneration,' `
        '                LxrWorkSchedulerResult::Success,'
}
Invoke-ValidatorFailure 'premature-state-reuse' {
    Replace-ExactlyOnce (Join-Path $perturbed $counterPath) `
        @'
    if (m_lifecycle.load(std::memory_order_seq_cst) !=
            LxrCompletionLifecycle::Drained ||
'@ `
        @'
    if (m_lifecycle.load(std::memory_order_seq_cst) ==
            LxrCompletionLifecycle::Drained ||
'@
}
Invoke-ValidatorFailure 'deferred-publication' {
    Replace-ExactlyOnce (Join-Path $perturbed $schedulerPath) `
        '    m_impl->activeDeferredIndex = publishedIndex;' `
        '    m_impl->activeDeferredIndex = m_impl->producerDeferredIndex;'
}
Invoke-ValidatorFailure 'shutdown-ownership' {
    Replace-ExactlyOnce (Join-Path $perturbed $validatorPath) `
        '            queued.GetState() == LxrWorkPacketState::CallerOwned &&' `
        '            queued.GetState() == LxrWorkPacketState::Completed &&'
}
Invoke-ValidatorFailure 'callback-failure' {
    Replace-ExactlyOnce (Join-Path $perturbed $validatorPath) `
        '                failed.overallCallbacks == 0,' `
        '                failed.overallCallbacks == 1,'
}

Invoke-VerifierFailure 'source-identity' {
    $rows = @(Import-Csv (Join-Path $perturbed $identityPath))
    $rows[1..($rows.Count - 1)] |
        Export-Csv (Join-Path $perturbed $identityPath) -NoTypeInformation
} 'Source identity path set mismatch.'
Invoke-VerifierFailure 'evidence-cardinality' {
    @(Import-Csv (Join-Path $perturbed $validationPath) |
        Where-Object platform -ne 'linux-x64') |
        Export-Csv (Join-Path $perturbed $validationPath) -NoTypeInformation
} 'Validation platform evidence is incomplete.'
Invoke-VerifierFailure 'benchmark-aa' {
    $rows = @(Import-Csv (Join-Path $perturbed $controlsPath))
    $rows[0].aa_max = '9.0'
    $rows | Export-Csv (Join-Path $perturbed $controlsPath) -NoTypeInformation
} 'Benchmark A/A ratio is outside its bounds.'
Invoke-VerifierFailure 'benchmark-null' {
    $rows = @(Import-Csv (Join-Path $perturbed $benchmarkPath))
    $row = $rows | Where-Object policy -eq 'null' | Select-Object -First 1
    $row.callback_passes = '1'
    $rows | Export-Csv (Join-Path $perturbed $benchmarkPath) -NoTypeInformation
} 'Benchmark policy differs: null'
Invoke-VerifierFailure 'benchmark-injected-backlog' {
    $rows = @(Import-Csv (Join-Path $perturbed $controlsPath))
    $rows[0].injected_to_base_depth32 = '1.0'
    $rows | Export-Csv (Join-Path $perturbed $controlsPath) -NoTypeInformation
} 'Benchmark injected backlog ratio is below its bound.'
Invoke-VerifierFailure 'benchmark-depth' {
    $rows = @(Import-Csv (Join-Path $perturbed $benchmarkPath))
    $rows[0].observed_max = '99'
    $rows | Export-Csv (Join-Path $perturbed $benchmarkPath) -NoTypeInformation
} 'Benchmark backlog/callback/ownership evidence differs.'

$manifest = Get-Content -LiteralPath (
    Join-Path $clean 'docs\design\lxr-port\P3.4\evidence-manifest.json') -Raw |
    ConvertFrom-Json
$expected = 1 + $manifest.perturbations.Count
if ($results.Count -ne $expected) {
    throw "Gate result count differs: $($results.Count)/$expected."
}
$results | Export-Csv (Join-Path $OutputDirectory 'gate-summary.csv') -NoTypeInformation
Remove-Item -LiteralPath $scratch -Recurse -Force
Write-Host "PASS: $($results.Count) P3.4 archive gate scenarios"
exit 0
