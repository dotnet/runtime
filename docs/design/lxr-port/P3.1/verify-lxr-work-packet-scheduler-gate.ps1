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
        'artifacts\P3.1\gate\' + [guid]::NewGuid().ToString('N'))
} else {
    $OutputDirectory = $PSCmdlet.GetUnresolvedProviderPathFromPSPath($OutputDirectory)
}
if (Test-Path -LiteralPath $OutputDirectory) {
    throw "OutputDirectory already exists: '$OutputDirectory'."
}
if (@(git -C $RepositoryRoot status --porcelain=v1).Count -ne 0) {
    throw 'The P3.1 archive gate requires a clean worktree.'
}

$sourceCommit = (Get-Content -LiteralPath (
    Join-Path $scriptRoot 'raw\source-commit.txt') -Raw).Trim()
git -C $RepositoryRoot cat-file -e "$sourceCommit`^{commit}"
if ($LASTEXITCODE -ne 0) {
    throw 'Source commit does not exist.'
}
$headCommit = (git -C $RepositoryRoot rev-parse HEAD).Trim()
$parentCommit = (git -C $RepositoryRoot rev-parse HEAD^).Trim()
if ($parentCommit -ne $sourceCommit) {
    throw 'Evidence commit is not a direct child of the recorded source commit.'
}
$changedPaths = @(git -C $RepositoryRoot diff --name-only $sourceCommit HEAD)
if ($changedPaths.Count -eq 0 -or
    @($changedPaths | Where-Object {
        $_ -notlike 'docs/design/lxr-port/P3.1/raw/*'
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

function Restore-Path([string]$RelativePath) {
    $target = Join-Path $perturbed $RelativePath
    $source = Join-Path $clean $RelativePath
    Copy-Item -LiteralPath $source -Destination $target -Force
}

function Invoke-ValidatorFailure(
    [string]$Name,
    [string[]]$Restore,
    [scriptblock]$Perturb) {
    foreach ($path in $script:allPerturbationPaths) {
        Restore-Path $path
    }
    foreach ($path in $Restore) {
        Restore-Path $path
    }
    & $Perturb
    $output = Join-Path $OutputDirectory "validator-$Name"
    $failed = $false
    try {
        & (Join-Path $perturbed (
            'docs\design\lxr-port\P3.1\run-lxr-work-packet-validation.ps1')) `
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
    [string[]]$Restore,
    [scriptblock]$Perturb,
    [string]$ExpectedError) {
    foreach ($path in $script:allPerturbationPaths) {
        Restore-Path $path
    }
    foreach ($path in $Restore) {
        Restore-Path $path
    }
    & $Perturb
    $message = ''
    try {
        & (Join-Path $perturbed (
            'docs\design\lxr-port\P3.1\verify-lxr-work-packet-scheduler.ps1')) `
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
    'docs\design\lxr-port\P3.1\verify-lxr-work-packet-scheduler.ps1')
$cleanOutput = @(& $cleanVerifier -RepositoryRoot $clean 6>&1 2>&1)
if (($cleanOutput -join ' ') -notmatch 'PASS: \d+ P3\.1 evidence checks') {
    throw 'Clean archive verifier did not pass.'
}
Add-Result 'clean-archive' 'verifier-pass' ($cleanOutput -join ' ')

$headerPath = 'src\coreclr\gc\lxr_work_packet.h'
$packetPath = 'src\coreclr\gc\lxr_work_packet.cpp'
$schedulerPath = 'src\coreclr\gc\lxr_work_scheduler.cpp'
$validatorPath = 'docs\design\lxr-port\P3.1\lxr-work-packet-validation.cpp'
$identityRelative = 'docs\design\lxr-port\P3.1\raw\source-identities.csv'
$validationRelative = 'docs\design\lxr-port\P3.1\raw\validation-summary.csv'
$controlsRelative = 'docs\design\lxr-port\P3.1\raw\benchmark-controls.csv'
$script:allPerturbationPaths = @(
    $headerPath,
    $packetPath,
    $schedulerPath,
    $validatorPath,
    $identityRelative,
    $validationRelative,
    $controlsRelative
)

Invoke-ValidatorFailure 'stage-order' @($headerPath) {
    Replace-ExactlyOnce (Join-Path $perturbed $headerPath) `
        "    Concurrent,`n    ConcurrentResumable," `
        "    ConcurrentResumable,`n    Concurrent,"
}
Invoke-ValidatorFailure 'stage-rename' @($packetPath) {
    Replace-ExactlyOnce (Join-Path $perturbed $packetPath) `
        '        "Prepare",' `
        '        "Preparation",'
}
Invoke-ValidatorFailure 'premature-open' @($schedulerPath) {
    Replace-ExactlyOnce (Join-Path $perturbed $schedulerPath) `
        '        while (cycleActive && AllWorkersParkedLocked())' `
        '        while (cycleActive)'
    Replace-ExactlyOnce (Join-Path $perturbed $schedulerPath) `
        '            if (!IsBucketDrainedLocked(currentStwStage))' `
        '            if (false)'
}
Invoke-ValidatorFailure 'drain-executing' @($schedulerPath) {
    Replace-ExactlyOnce (Join-Path $perturbed $schedulerPath) `
        '        while (cycleActive && AllWorkersParkedLocked())' `
        '        while (cycleActive)'
    Replace-ExactlyOnce (Join-Path $perturbed $schedulerPath) `
        @'
        return bucket.outstanding == 0 &&
            bucket.executing == 0 &&
            bucket.queue.empty();
'@ `
        @'
        return bucket.queue.empty();
'@
}
Invoke-ValidatorFailure 'packet-drop' @($validatorPath) {
    Replace-ExactlyOnce (Join-Path $perturbed $validatorPath) `
        'state->executions[packet->GetId()].fetch_add(1, std::memory_order_relaxed);' `
        'state->executions[packet->GetId()].fetch_add(0, std::memory_order_relaxed);'
}
Invoke-ValidatorFailure 'packet-duplicate' @($validatorPath) {
    Replace-ExactlyOnce (Join-Path $perturbed $validatorPath) `
        'state->executions[packet->GetId()].fetch_add(1, std::memory_order_relaxed);' `
        'state->executions[packet->GetId()].fetch_add(2, std::memory_order_relaxed);'
}
Invoke-ValidatorFailure 'stealing-disabled' @($schedulerPath) {
    Replace-ExactlyOnce (Join-Path $perturbed $schedulerPath) `
        @'
        if (preferredWorker != LxrWorkScheduler::NoPreferredWorker &&
            preferredWorker != workerIndex)
'@ `
        @'
        if (false)
'@
}
Invoke-ValidatorFailure 'ownership-duplicate-submit' @($validatorPath) {
    Replace-ExactlyOnce (Join-Path $perturbed $validatorPath) `
        '                LxrWorkSchedulerResult::PacketAlreadyOwned,' `
        '                LxrWorkSchedulerResult::Success,'
}
Invoke-ValidatorFailure 'increment-placement' @($headerPath) {
    Replace-ExactlyOnce (Join-Path $perturbed $headerPath) `
        'constexpr LxrWorkBucketStage LxrRCProcessIncs = LxrWorkBucketStage::Initial;' `
        'constexpr LxrWorkBucketStage LxrRCProcessIncs = LxrWorkBucketStage::Prepare;'
}
Invoke-ValidatorFailure 'decrement-placement' @($headerPath) {
    Replace-ExactlyOnce (Join-Path $perturbed $headerPath) `
        'constexpr LxrWorkBucketStage LxrRCProcessDecs = LxrWorkBucketStage::STWRCDecsAndSweep;' `
        'constexpr LxrWorkBucketStage LxrRCProcessDecs = LxrWorkBucketStage::Release;'
}

Invoke-VerifierFailure 'source-identity' @($identityRelative) {
    $identity = Join-Path $perturbed $identityRelative
    $rows = @(Import-Csv $identity)
    $rows[1..($rows.Count - 1)] | Export-Csv $identity -NoTypeInformation
} 'Source identity path set mismatch.'

Invoke-VerifierFailure 'evidence-cardinality' @($validationRelative) {
    $validation = Join-Path $perturbed $validationRelative
    @(Import-Csv $validation | Where-Object platform -ne 'linux-x64') |
        Export-Csv $validation -NoTypeInformation
} 'Validation platform evidence is incomplete.'

Invoke-VerifierFailure 'benchmark-aa' @($controlsRelative) {
    $controls = Join-Path $perturbed $controlsRelative
    $rows = @(Import-Csv $controls)
    $rows[0].aa_ratio = '2.0'
    $rows | Export-Csv $controls -NoTypeInformation
} 'Benchmark A/A ratio is outside its bounds.'
Invoke-VerifierFailure 'benchmark-injected-cost' @($controlsRelative) {
    $controls = Join-Path $perturbed $controlsRelative
    $rows = @(Import-Csv $controls)
    $rows[0].injected_cost_ratio = '1.0'
    $rows | Export-Csv $controls -NoTypeInformation
} 'Benchmark injected-cost ratio is below its bound.'

$manifest = Get-Content -LiteralPath (
    Join-Path $clean 'docs\design\lxr-port\P3.1\evidence-manifest.json') -Raw |
    ConvertFrom-Json
$expected = 1 + $manifest.perturbations.Count
if ($results.Count -ne $expected) {
    throw "Gate result count differs: $($results.Count)/$expected."
}
$results | Export-Csv (Join-Path $OutputDirectory 'gate-summary.csv') -NoTypeInformation

Remove-Item -LiteralPath $scratch -Recurse -Force
Write-Host "PASS: $($results.Count) P3.1 archive gate scenarios"
exit 0
