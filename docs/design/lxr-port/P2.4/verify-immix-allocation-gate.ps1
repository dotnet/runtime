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
        'artifacts\P2.4\gate\' + [guid]::NewGuid().ToString('N'))
} else {
    $OutputDirectory = $PSCmdlet.GetUnresolvedProviderPathFromPSPath($OutputDirectory)
}
if (Test-Path -LiteralPath $OutputDirectory) {
    throw "OutputDirectory already exists: '$OutputDirectory'."
}

$status = @(git -C $RepositoryRoot -c core.longpaths=true status --porcelain=v1)
if (($LASTEXITCODE -ne 0) -or ($status.Count -ne 0)) {
    throw 'The P2.4 archive gate requires a clean worktree.'
}

$manifest = Get-Content -LiteralPath (
    Join-Path $RepositoryRoot 'docs\design\lxr-port\P2.4\evidence-manifest.json') -Raw |
    ConvertFrom-Json
$raw = Join-Path $RepositoryRoot 'docs\design\lxr-port\P2.4\raw'
$sourceCommit = (Get-Content -LiteralPath (
    Join-Path $raw 'source-commit.txt') -Raw).Trim()
$head = (git -C $RepositoryRoot rev-parse HEAD).Trim()
$headParent = (git -C $RepositoryRoot rev-parse HEAD^).Trim()
if ($headParent -ne $sourceCommit) {
    throw 'Evidence commit is not the direct source child.'
}
$sourceParent = (git -C $RepositoryRoot rev-parse "$sourceCommit^").Trim()
if ($sourceParent -ne '17ac93d30d5ebc08aeda0ddf24e562d506d72230') {
    throw 'Source commit is not directly parented by P2.2.'
}
$changed = @(git -C $RepositoryRoot diff --name-only $sourceCommit $head)
if (($changed.Count -eq 0) -or
    (@($changed | Where-Object {
        -not $_.StartsWith('docs/design/lxr-port/P2.4/raw/')
    }).Count -ne 0)) {
    throw 'Evidence commit contains a non-raw change.'
}

New-Item -ItemType Directory -Path $OutputDirectory | Out-Null
$tempRoot = [IO.Path]::GetPathRoot([IO.Path]::GetTempPath())
$scratch = Join-Path $tempRoot ('p24-' + [guid]::NewGuid().ToString('N'))
$scratch = [IO.Path]::GetFullPath($scratch)
if (-not $scratch.StartsWith($tempRoot, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Scratch path escaped the system temp root.'
}
$clean = Join-Path $scratch 'tree'
$tar = Join-Path $scratch 'tree.tar'
$summary = [Collections.Generic.List[object]]::new()

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

function Invoke-VerifierFailure(
    [string]$Name,
    [string]$ExpectedError,
    [scriptblock]$Perturbation,
    [scriptblock]$Restoration) {
    & $Perturbation
    $failed = $false
    $message = ''
    try {
        & (Join-Path $clean 'docs\design\lxr-port\P2.4\verify-immix-allocation.ps1') `
            -RepositoryRoot $clean | Out-Null
    } catch {
        $failed = $true
        $message = $_.Exception.Message
    } finally {
        & $Restoration
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

function Invoke-BehaviorFailure(
    [string]$Name,
    [string]$Path,
    [string]$Old,
    [string]$New,
    [string]$ExpectedPattern) {
    $original = [IO.File]::ReadAllText($Path)
    $originalHash = (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash
    Replace-ExactlyOnce $Path $Old $New
    $output = Join-Path $OutputDirectory "behavior-$Name"
    $failed = $false
    try {
        & (Join-Path $clean (
            'docs\design\lxr-port\P2.4\run-immix-allocation-validation.ps1')) `
            -RepositoryRoot $clean `
            -OutputDirectory $output | Out-Null
    } catch {
        $failed = $true
    } finally {
        [IO.File]::WriteAllText(
            $Path,
            $original,
            [Text.UTF8Encoding]::new($false))
    }
    $restoredHash = (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash
    if ($restoredHash -ne $originalHash) {
        throw "Behavior perturbation '$Name' did not restore its source."
    }
    $log = Join-Path $output 'x64\run.log'
    $logText = if (Test-Path -LiteralPath $log -PathType Leaf) {
        Get-Content -LiteralPath $log -Raw
    } else {
        ''
    }
    if (-not $failed -or ($logText -notmatch $ExpectedPattern)) {
        throw "Behavior perturbation '$Name' did not fail with '$ExpectedPattern'."
    }
    $summary.Add([pscustomobject][ordered]@{
        id = $Name
        kind = 'behavior-failure'
        expected = $ExpectedPattern
        observed = [IO.Path]::GetFileName($log)
        result = 'PASS'
    })
}

try {
    New-Item -ItemType Directory -Path $clean -Force | Out-Null
    git -C $RepositoryRoot archive --format=tar --output=$tar HEAD
    if ($LASTEXITCODE -ne 0) {
        throw 'Unable to archive the committed tree.'
    }
    tar -xf $tar -C $clean
    if ($LASTEXITCODE -ne 0) {
        throw 'Unable to extract the committed tree.'
    }

    $identityPaths = @(
        Get-Content -LiteralPath (
            Join-Path $clean 'docs\design\lxr-port\P2.4\source-manifest.txt') |
            Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
    )
    $rawRelative = 'docs\design\lxr-port\P2.4\raw'
    $identityPaths += @(
        Get-ChildItem -LiteralPath (Join-Path $clean $rawRelative) -File -Recurse |
            ForEach-Object {
                [IO.Path]::GetRelativePath($clean, $_.FullName).Replace('\', '/')
            }
    )
    foreach ($relative in $identityPaths | Sort-Object -Unique) {
        $blob = (git -C $RepositoryRoot rev-parse "HEAD:$relative").Trim()
        if ($LASTEXITCODE -ne 0) {
            throw "Archive identity path is absent from HEAD: $relative"
        }
        $extracted = Join-Path $clean $relative
        $extractedBlob = (git hash-object $extracted).Trim()
        if ($blob -ne $extractedBlob) {
            throw "Archive identity mismatch: $relative"
        }
    }
    foreach ($script in Get-ChildItem -LiteralPath (
        Join-Path $clean 'docs\design\lxr-port\P2.4') -Filter *.sh -File) {
        $text = [IO.File]::ReadAllText($script.FullName)
        if ($text.Contains("`r")) {
            throw "Committed shell script contains CR: $($script.Name)"
        }
    }

    $verifierOutput = @(
        & (Join-Path $clean (
            'docs\design\lxr-port\P2.4\verify-immix-allocation.ps1')) `
            -RepositoryRoot $clean 6>&1 2>&1
    )
    if (($verifierOutput -join ' ') -notmatch 'PASS: \d+ P2\.4 evidence checks') {
        throw 'Clean archive verifier output is missing.'
    }
    $summary.Add([pscustomobject][ordered]@{
        id = 'clean-archive'
        kind = 'verifier-pass'
        expected = 'PASS'
        observed = ($verifierOutput -join ' ')
        result = 'PASS'
    })

    $allocationPath = Join-Path $clean 'src\coreclr\gc\immix_allocation.cpp'
    Invoke-BehaviorFailure `
        'hole-line-zero' `
        $allocationPath `
        'if (dead && ((line == 0) || previousDead))' `
        'if (dead && previousDead)' `
        'FAIL: all-dead starts at block'
    Invoke-BehaviorFailure `
        'hole-zeroing' `
        $allocationPath `
        '        memset(reinterpret_cast<void*>(hole.start), 0, size);' `
        '        (void)size;' `
        'FAIL: recycled hole zeroed'
    Invoke-BehaviorFailure `
        'object-mark-publication' `
        $allocationPath `
        ("    return m_metadata->ResetRangeQuiescent(" +
         [Environment]::NewLine +
         '        LxrSideMetadataKind::ObjectMark,') `
        ("    return m_metadata->ResetRangeQuiescent(" +
         [Environment]::NewLine +
         '        LxrSideMetadataKind::FieldUnlogged,') `
        'FAIL: fresh object mark cleared'
    Invoke-BehaviorFailure `
        'reuse-wrap-plane' `
        $allocationPath `
        '            LxrSideMetadataKind::LineReuse,' `
        '            LxrSideMetadataKind::ReferenceCount,' `
        'FAIL: recycled first reuse value'

    $identityTarget = Join-Path $clean 'src\coreclr\gc\immix_allocation.h'
    $identityOriginal = [IO.File]::ReadAllText($identityTarget)
    Invoke-VerifierFailure `
        'source-identity' `
        'Source identity mismatch: src/coreclr/gc/immix_allocation.h' `
        { Add-Content -LiteralPath $identityTarget -Value 'identity-perturbation' } `
        {
            [IO.File]::WriteAllText(
                $identityTarget,
                $identityOriginal,
                [Text.UTF8Encoding]::new($false))
        }

    $validationPath = Join-Path $clean (
        'docs\design\lxr-port\P2.4\raw\validation-summary.csv')
    $validationOriginal = [IO.File]::ReadAllText($validationPath)
    Invoke-VerifierFailure `
        'validation-row' `
        'Validation platform evidence is incomplete.' `
        {
            @(Import-Csv $validationPath |
                Where-Object platform -ne 'linux-x64') |
                Export-Csv $validationPath -NoTypeInformation
        } `
        {
            [IO.File]::WriteAllText(
                $validationPath,
                $validationOriginal,
                [Text.UTF8Encoding]::new($false))
        }

    $controlsPath = Join-Path $clean (
        'docs\design\lxr-port\P2.4\raw\benchmark-controls.csv')
    $controlsOriginal = [IO.File]::ReadAllText($controlsPath)
    Invoke-VerifierFailure `
        'benchmark-aa' `
        'Fresh A/A ratio is above its bound.' `
        {
            $rows = @(Import-Csv $controlsPath)
            $rows[0].fresh_aa_ratio = '2.0'
            $rows | Export-Csv $controlsPath -NoTypeInformation
        } `
        {
            [IO.File]::WriteAllText(
                $controlsPath,
                $controlsOriginal,
                [Text.UTF8Encoding]::new($false))
        }

    $defaultAllocationPath = Join-Path $clean (
        'docs\design\lxr-port\P2.4\raw\default-allocation-controls.csv')
    $defaultAllocationOriginal =
        [IO.File]::ReadAllText($defaultAllocationPath)
    Invoke-VerifierFailure `
        'default-allocation-aa' `
        'Default allocation changed ratio is above its bound.' `
        {
            $rows = @(Import-Csv $defaultAllocationPath)
            $rows[0].changed_ratio = '2.0'
            $rows | Export-Csv $defaultAllocationPath -NoTypeInformation
        } `
        {
            [IO.File]::WriteAllText(
                $defaultAllocationPath,
                $defaultAllocationOriginal,
                [Text.UTF8Encoding]::new($false))
        }

    $defaultPath = Join-Path $clean (
        'docs\design\lxr-port\P2.4\raw\default-path-summary.csv')
    $defaultOriginal = [IO.File]::ReadAllText($defaultPath)
    Invoke-VerifierFailure `
        'default-path-row' `
        'Default-path evidence differs.' `
        {
            @(Import-Csv $defaultPath |
                Where-Object property -ne 'normal-gc-call-sites') |
                Export-Csv $defaultPath -NoTypeInformation
        } `
        {
            [IO.File]::WriteAllText(
                $defaultPath,
                $defaultOriginal,
                [Text.UTF8Encoding]::new($false))
        }

    $expectedCount = 1 + $manifest.perturbations.Count
    if ($summary.Count -ne $expectedCount) {
        throw "Gate result count differs: $($summary.Count)/$expectedCount."
    }
    $summary | Export-Csv (
        Join-Path $OutputDirectory 'gate-summary.csv') -NoTypeInformation
} finally {
    if (Test-Path -LiteralPath $scratch) {
        $resolvedScratch = [IO.Path]::GetFullPath(
            (Resolve-Path -LiteralPath $scratch).ProviderPath)
        if (-not $resolvedScratch.StartsWith(
                $tempRoot,
                [StringComparison]::OrdinalIgnoreCase)) {
            throw 'Refusing to clean a scratch path outside the temp root.'
        }
        Remove-Item -LiteralPath $resolvedScratch -Recurse -Force
    }
}

Write-Host "PASS: $($summary.Count) P2.4 archive gate scenarios"
Write-Host "Output: $OutputDirectory"
