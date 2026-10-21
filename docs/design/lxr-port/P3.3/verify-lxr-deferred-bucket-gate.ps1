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
        'artifacts\P3.3\gate\' + [guid]::NewGuid().ToString('N'))
} else {
    $OutputDirectory = $PSCmdlet.GetUnresolvedProviderPathFromPSPath($OutputDirectory)
}
if (Test-Path -LiteralPath $OutputDirectory) {
    throw "OutputDirectory already exists: '$OutputDirectory'."
}
if (@(git -C $RepositoryRoot status --porcelain=v1 -uall).Count -ne 0) {
    throw 'The P3.3 archive gate requires a clean worktree.'
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
        $_ -notlike 'docs/design/lxr-port/P3.3/raw/*'
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
if ($LASTEXITCODE -ne 0) {
    throw 'Unable to extract the evidence commit.'
}
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

function Restore-All {
    foreach ($path in $script:allPerturbationPaths) {
        Copy-Item -LiteralPath (Join-Path $clean $path) -Destination (
            Join-Path $perturbed $path) -Force
    }
}

function Invoke-ValidatorFailure([string]$Name, [scriptblock]$Perturb) {
    Restore-All
    & $Perturb
    Replace-ExactlyOnce (Join-Path $perturbed $script:validatorPath) `
        "    constexpr uint32_t TimeoutMilliseconds = 30'000;" `
        '    constexpr uint32_t TimeoutMilliseconds = 500;'
    $output = Join-Path $OutputDirectory "validator-$Name"
    $failed = $false
    try {
        & (Join-Path $perturbed (
            'docs\design\lxr-port\P3.3\run-lxr-deferred-bucket-validation.ps1')) `
            -RepositoryRoot $perturbed `
            -OutputDirectory $output | Out-Null
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
            'docs\design\lxr-port\P3.3\verify-lxr-deferred-bucket.ps1')) `
            -RepositoryRoot $perturbed | Out-Null
    } catch {
        $message = $_.Exception.Message
    }
    if ($message -notlike "*$ExpectedError*") {
        throw "Verifier perturbation '$Name' did not fail with '$ExpectedError'; observed '$message'."
    }
    Add-Result $Name 'verifier-negative-control' $message
}

$cleanVerifier = Join-Path $clean (
    'docs\design\lxr-port\P3.3\verify-lxr-deferred-bucket.ps1')
$cleanOutput = @(& $cleanVerifier -RepositoryRoot $clean 6>&1 2>&1)
if (($cleanOutput -join ' ') -notmatch 'PASS: \d+ P3\.3 evidence checks') {
    throw 'Clean archive verifier did not pass.'
}
$cleanValidationOutput = Join-Path $OutputDirectory 'clean-validation'
& (Join-Path $clean (
    'docs\design\lxr-port\P3.3\run-lxr-deferred-bucket-validation.ps1')) `
    -RepositoryRoot $clean `
    -OutputDirectory $cleanValidationOutput | Out-Null
Add-Result 'clean-archive' 'verifier-and-validator-pass' ($cleanOutput -join ' ')

$schedulerPath = 'src\coreclr\gc\lxr_work_scheduler.cpp'
$script:validatorPath = 'docs\design\lxr-port\P3.3\lxr-deferred-bucket-validation.cpp'
$identityPath = 'docs\design\lxr-port\P3.3\raw\source-identities.csv'
$validationPath = 'docs\design\lxr-port\P3.3\raw\validation-summary.csv'
$benchmarkRawPath = 'docs\design\lxr-port\P3.3\raw\benchmark-raw.csv'
$controlsPath = 'docs\design\lxr-port\P3.3\raw\benchmark-controls.csv'
$script:allPerturbationPaths = @(
    $schedulerPath,
    $script:validatorPath,
    $identityPath,
    $validationPath,
    $benchmarkRawPath,
    $controlsPath
)

Invoke-ValidatorFailure 'queue-direction' {
    Replace-ExactlyOnce (Join-Path $perturbed $schedulerPath) `
        '          activeDeferredIndex(enableDeferredConcurrentWork ? 0 : LxrNoDeferredQueue),' `
        '          activeDeferredIndex(enableDeferredConcurrentWork ? 1 : LxrNoDeferredQueue),'
}
Invoke-ValidatorFailure 'visible-before-flip' {
    Replace-ExactlyOnce (Join-Path $perturbed $schedulerPath) `
        '        DeferredQueueSet& active = deferredQueues[activeDeferredIndex];' `
        '        DeferredQueueSet& active = deferredQueues[producerDeferredIndex];'
}
Invoke-ValidatorFailure 'missing-after-flip' {
    Replace-ExactlyOnce (Join-Path $perturbed $script:validatorPath) `
        '            (state.activeIndex != LxrNoDeferredQueue) == true,' `
        '            state.activeIndex == LxrNoDeferredQueue,'
}
Invoke-ValidatorFailure 'duplicate-flip' {
    Replace-ExactlyOnce (Join-Path $perturbed $script:validatorPath) `
        @'
            scheduler.PublishDeferredConcurrentWork(generation) ==
                LxrWorkSchedulerResult::InvalidState,
            "duplicate publication rejected");
'@ `
        @'
            scheduler.PublishDeferredConcurrentWork(generation) ==
                LxrWorkSchedulerResult::Success,
            "duplicate publication rejected");
'@
}
Invoke-ValidatorFailure 'generation-binding' {
    Replace-ExactlyOnce (Join-Path $perturbed $schedulerPath) `
        '        deferredQueues[producerDeferredIndex].generation = generation + 1;' `
        '        deferredQueues[producerDeferredIndex].generation = generation;'
}
Invoke-ValidatorFailure 'empty-flip' {
    Replace-ExactlyOnce (Join-Path $perturbed $schedulerPath) `
        '        m_impl->deferredEmptyFlips++;' `
        '        m_impl->deferredEmptyFlips += 0;'
}
Invoke-ValidatorFailure 'lazy-off' {
    Replace-ExactlyOnce (Join-Path $perturbed $script:validatorPath) `
        '            scheduler.Submit(eager.get()) == LxrWorkSchedulerResult::Success,' `
        @'
            scheduler.SubmitDeferredConcurrent(
                eager.get(),
                LxrDeferredWorkPriority::Prioritized) ==
                LxrWorkSchedulerResult::Success,
'@
}
Invoke-ValidatorFailure 'producer-seal-race' {
    Replace-ExactlyOnce (Join-Path $perturbed $schedulerPath) `
        '        producerDeferredIndex = nextProducerIndex;' `
        '        producerDeferredIndex = sealedDeferredIndex;'
}
Invoke-ValidatorFailure 'worker-active-only' {
    Replace-ExactlyOnce (Join-Path $perturbed $schedulerPath) `
        '    m_impl->activeDeferredIndex = publishedIndex;' `
        '    m_impl->activeDeferredIndex = m_impl->producerDeferredIndex;'
}
Invoke-ValidatorFailure 'repeat-cycle-loss' {
    Replace-ExactlyOnce (Join-Path $perturbed $script:validatorPath) `
        '        state->executions.fetch_add(1, std::memory_order_relaxed);' `
        '        state->executions.fetch_add(0, std::memory_order_relaxed);'
}
Invoke-ValidatorFailure 'repeat-cycle-duplicate' {
    Replace-ExactlyOnce (Join-Path $perturbed $script:validatorPath) `
        '        state->executions.fetch_add(1, std::memory_order_relaxed);' `
        '        state->executions.fetch_add(2, std::memory_order_relaxed);'
}
Invoke-ValidatorFailure 'reset-shutdown' {
    Replace-ExactlyOnce (Join-Path $perturbed $schedulerPath) `
        '            for (size_t index = 0; index < deferredQueues.size(); index++)' `
        '            for (size_t index = 0; index < 1; index++)'
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
    $rows[0].aa_ratio = '2.0'
    $rows | Export-Csv (Join-Path $perturbed $controlsPath) -NoTypeInformation
} 'Benchmark A/A ratio is outside its bounds.'
Invoke-VerifierFailure 'benchmark-null' {
    $rows = @(Import-Csv (Join-Path $perturbed $benchmarkRawPath))
    $row = $rows | Where-Object policy -eq 'lazy-null' | Select-Object -First 1
    $row.callback_passes = '1'
    $rows | Export-Csv (Join-Path $perturbed $benchmarkRawPath) -NoTypeInformation
} 'Benchmark null/injected policy differs: lazy-null'
Invoke-VerifierFailure 'benchmark-injected-cost' {
    $rows = @(Import-Csv (Join-Path $perturbed $controlsPath))
    $rows[0].eager_injected_ratio = '1.0'
    $rows | Export-Csv (Join-Path $perturbed $controlsPath) -NoTypeInformation
} 'Benchmark injected-cost ratio is below its bound.'

$manifest = Get-Content -LiteralPath (
    Join-Path $clean 'docs\design\lxr-port\P3.3\evidence-manifest.json') -Raw |
    ConvertFrom-Json
$expected = 1 + $manifest.perturbations.Count
if ($results.Count -ne $expected) {
    throw "Gate result count differs: $($results.Count)/$expected."
}
$results | Export-Csv (Join-Path $OutputDirectory 'gate-summary.csv') -NoTypeInformation

Remove-Item -LiteralPath $scratch -Recurse -Force
Write-Host "PASS: $($results.Count) P3.3 archive gate scenarios"
exit 0
