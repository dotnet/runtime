# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

<#
.SYNOPSIS
Archive gate for the P2.3 reference-count evidence.

.DESCRIPTION
Refuses a dirty worktree, index, or untracked file; requires the recorded
source commit to be a distinct direct parent of the evidence commit; requires
every source-manifest path to be unchanged between them; creates two
independent clean `git archive` extracts and runs the verifier once in each;
and then runs 30 perturbations, exactly two for each of the 15 audited
properties.

Correctness perturbations mutate product source in their own extract and
re-run the P2.3 validation harness there, so they must compile and change
observable validator behaviour rather than only text. Evidence perturbations
mutate recorded evidence and must be rejected by the verifier.

Scratch trees use indexed names under a unique short root in
`C:\Users\Public`; only that exactly resolved root is deleted. Gate summaries,
archive-identity logs, perturbation logs, and the caller's output directory
remain under -OutputDirectory.
#>
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
        'artifacts\P2.3\gate\' + [guid]::NewGuid().ToString('N'))
} else {
    $OutputDirectory = $PSCmdlet.GetUnresolvedProviderPathFromPSPath($OutputDirectory)
}
if (Test-Path -LiteralPath $OutputDirectory) {
    throw "OutputDirectory already exists; caller-owned output is not modified: '$OutputDirectory'."
}

$relativeRoot = 'docs\design\lxr-port\P2.3'
$verifierRelative = "$relativeRoot\verify-reference-count.ps1"
$validationRelative = "$relativeRoot\run-reference-count-validation.ps1"
$manifestRelative = "$relativeRoot\evidence-manifest.json"

# --- worktree, index, and untracked state ---------------------------------
$status = @(
    git -C $RepositoryRoot -c core.longpaths=true status --porcelain=v1 --untracked-files=all
)
if ($LASTEXITCODE -ne 0) {
    throw 'Unable to inspect the repository worktree.'
}
if ($status.Count -ne 0) {
    throw "The P2.3 gate requires a clean worktree, index, and untracked set; found $($status.Count) entries."
}

# --- source and evidence topology -----------------------------------------
$raw = Join-Path $RepositoryRoot "$relativeRoot\raw"
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
$parentCommit = (git -C $RepositoryRoot rev-parse 'HEAD^1').Trim()
if ($parentCommit -ne $sourceCommit) {
    throw 'Evidence commit is not a direct child of the source commit.'
}
git -C $RepositoryRoot merge-base --is-ancestor $sourceCommit $headCommit
if ($LASTEXITCODE -ne 0) {
    throw 'Source commit is not an ancestor of the evidence commit.'
}

$sourcePaths = @(
    Get-Content -LiteralPath (
        Join-Path $RepositoryRoot "$relativeRoot\source-manifest.txt") |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
)
if ($sourcePaths.Count -eq 0) {
    throw 'Source manifest is empty.'
}
foreach ($path in $sourcePaths) {
    git -C $RepositoryRoot diff --quiet $sourceCommit $headCommit -- $path
    if ($LASTEXITCODE -ne 0) {
        throw "Source path changed after identity generation: $path"
    }
}

# --- two independent clean extracts ---------------------------------------
New-Item -ItemType Directory -Path $OutputDirectory | Out-Null
$scratchParent = Join-Path $env:SystemDrive 'Users\Public'
do {
    $scratch = Join-Path $scratchParent (
        'p23g-' + [guid]::NewGuid().ToString('N').Substring(0, 8))
} while (Test-Path -LiteralPath $scratch)
New-Item -ItemType Directory -Path $scratch | Out-Null

trap {
    $message = $_.Exception.Message
    if (Test-Path -LiteralPath $scratch) {
        Remove-Item -LiteralPath $scratch -Recurse -Force -ErrorAction SilentlyContinue
    }
    $ErrorActionPreference = 'Continue'
    [Console]::Error.WriteLine($message)
    exit 1
}

function Assert-ArchiveExtractIdentity(
    [string]$ArchivePath,
    [string]$ExtractRoot,
    [string]$LogPath) {
    $archiveHash = [Security.Cryptography.IncrementalHash]::CreateHash(
        [Security.Cryptography.HashAlgorithmName]::SHA256)
    $extractHash = [Security.Cryptography.IncrementalHash]::CreateHash(
        [Security.Cryptography.HashAlgorithmName]::SHA256)
    $fileCount = 0
    [uint64]$byteCount = 0
    $archive = [IO.Compression.ZipFile]::OpenRead($ArchivePath)
    try {
        foreach ($entry in $archive.Entries) {
            if ([string]::IsNullOrEmpty($entry.Name)) {
                continue
            }

            $relative = $entry.FullName
            $path = Join-Path $ExtractRoot $relative.Replace('/', '\')
            $item = Get-Item -LiteralPath $path -Force -ErrorAction Stop
            if ($item.PSIsContainer -or ($item.Length -ne $entry.Length)) {
                throw "Archive file identity differs: '$relative'."
            }

            $archiveStream = $entry.Open()
            $extractStream = [IO.File]::OpenRead($path)
            try {
                $archiveBuffer = [byte[]]::new(64 * 1024)
                $extractBuffer = [byte[]]::new(64 * 1024)
                while (($archiveRead = $archiveStream.Read(
                            $archiveBuffer,
                            0,
                            $archiveBuffer.Length)) -gt 0) {
                    $extractRead = $extractStream.Read(
                        $extractBuffer,
                        0,
                        $archiveRead)
                    if ($extractRead -ne $archiveRead) {
                        throw "Archive file content is truncated: '$relative'."
                    }
                    $archiveHash.AppendData($archiveBuffer, 0, $archiveRead)
                    $extractHash.AppendData($extractBuffer, 0, $extractRead)
                }
                if ($extractStream.ReadByte() -ne -1) {
                    throw "Archive file content is longer than expected: '$relative'."
                }
            } finally {
                $archiveStream.Dispose()
                $extractStream.Dispose()
            }
            $fileCount++
            $byteCount += [uint64]$item.Length
        }
    } finally {
        $archive.Dispose()
    }

    $expectedHash = [Convert]::ToHexString($archiveHash.GetHashAndReset())
    $actualHash = [Convert]::ToHexString($extractHash.GetHashAndReset())
    $archiveHash.Dispose()
    $extractHash.Dispose()
    $actualFileCount = @(
        Get-ChildItem -LiteralPath $ExtractRoot -Recurse -File -Force
    ).Count
    if ($actualFileCount -ne $fileCount) {
        throw "Archive extract has $actualFileCount files; expected $fileCount."
    }
    if ($actualHash -ne $expectedHash) {
        throw "Archive extracted content hash differs for '$ExtractRoot'."
    }
    foreach ($relative in $sourcePaths) {
        if (-not (Test-Path -LiteralPath (
                    Join-Path $ExtractRoot $relative.Replace('/', '\')) -PathType Leaf)) {
            throw "Archive extract is missing source-manifest path '$relative'."
        }
    }
    [pscustomobject][ordered]@{
        files = $fileCount
        bytes = $byteCount
        sha256 = $actualHash
        result = 'PASS'
    } | Export-Csv $LogPath -NoTypeInformation
}

$cleanRoots = @()
foreach ($index in 1, 2) {
    $archive = Join-Path $scratch "tree-$index.zip"
    $clean = Join-Path $scratch "clean-$index"
    New-Item -ItemType Directory -Path $clean | Out-Null
    git -C $RepositoryRoot archive --format=zip --output=$archive $headCommit
    if ($LASTEXITCODE -ne 0) {
        throw "Unable to archive the committed tree for extract $index."
    }
    [IO.Compression.ZipFile]::ExtractToDirectory($archive, $clean)
    $archiveCheck = Join-Path $OutputDirectory "archive-identity-$index.csv"
    Assert-ArchiveExtractIdentity $archive $clean $archiveCheck
    Remove-Item -LiteralPath $archive -Force
    $cleanRoots += $clean
}
$cleanPrimary = $cleanRoots[0]
$cleanSecondary = $cleanRoots[1]

$manifest = Get-Content -LiteralPath (
    Join-Path $cleanPrimary $manifestRelative) -Raw | ConvertFrom-Json
$summary = [Collections.Generic.List[object]]::new()

# --- helpers ---------------------------------------------------------------
function Invoke-VerifierPass([string]$Name, [string]$Root) {
    $verifier = Join-Path $Root $verifierRelative
    try {
        $output = @(& $verifier -RepositoryRoot $Root 6>&1 2>&1)
    } catch {
        throw "Verifier unexpectedly failed for '$Name': $($_.Exception.Message)"
    }
    if (($output -join ' ') -notmatch 'PASS: \d+ P2\.3 evidence checks') {
        throw "Verifier success output is missing for '$Name'."
    }
    $summary.Add([pscustomobject][ordered]@{
        id = $Name
        property = 'clean-archive'
        kind = 'verifier-pass'
        expected = 'PASS'
        observed = ($output -join ' ')
        result = 'PASS'
    })
}

function Invoke-VerifierFailure(
    [string]$Name,
    [string]$Property,
    [string]$Root,
    [string]$ExpectedError) {
    $verifier = Join-Path $Root $verifierRelative
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
        property = $Property
        kind = 'verifier-failure'
        expected = $ExpectedError
        observed = $message
        result = 'PASS'
    })
}

function Invoke-BehaviorFailure(
    [string]$Name,
    [string]$Property,
    [string]$Root,
    [string]$ExpectedPattern) {
    $runner = Join-Path $Root $validationRelative
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
    if (-not $failed -or ($logText -notmatch [regex]::Escape($ExpectedPattern))) {
        throw "Behavior perturbation '$Name' did not fail with '$ExpectedPattern'."
    }
    $summary.Add([pscustomobject][ordered]@{
        id = $Name
        property = $Property
        kind = 'behavior-failure'
        expected = $ExpectedPattern
        observed = $log
        result = 'PASS'
    })
}

function Replace-ExactlyOnce(
    [string]$Path,
    [string]$Old,
    [string]$New) {
    $bytes = [IO.File]::ReadAllBytes($Path)
    $hasBom =
        ($bytes.Length -ge 3) -and
        ($bytes[0] -eq 0xEF) -and ($bytes[1] -eq 0xBB) -and ($bytes[2] -eq 0xBF)
    $text = [IO.File]::ReadAllText($Path)
    $pattern = (
        ($Old -split "`r?`n" | ForEach-Object { [regex]::Escape($_) }) -join '\r?\n')
    $found = [regex]::Matches($text, $pattern)
    if ($found.Count -ne 1) {
        throw "Expected one replacement token in '$Path', found $($found.Count)."
    }
    $newline = if ($text.Contains("`r`n")) { "`r`n" } else { "`n" }
    $replacement = (($New -split "`r?`n") -join $newline)
    $updated =
        $text.Substring(0, $found[0].Index) +
        $replacement +
        $text.Substring($found[0].Index + $found[0].Length)
    [IO.File]::WriteAllText($Path, $updated, [Text.UTF8Encoding]::new($hasBom))
}

$perturbationIndex = 0
function New-PerturbationTree([string]$Name) {
    $script:perturbationIndex++
    $target = Join-Path $scratch ('p{0:D2}' -f $script:perturbationIndex)
    Copy-Item -LiteralPath $cleanPrimary -Destination $target -Recurse
    return $target
}

# Some perturbations deliberately mutate a file that is itself in the source
# manifest. Without re-stamping its recorded hash the byte-exact identity
# check would fire first and mask the property under test.
function Update-SourceIdentity([string]$Root, [string]$Relative) {
    $path = Join-Path $Root "$relativeRoot\raw\source-identities.csv"
    $rows = @(Import-Csv $path)
    $row = @($rows | Where-Object path -eq $Relative)
    if ($row.Count -ne 1) {
        throw "Source identity row differs for '$Relative'."
    }
    $row[0].sha256 = (Get-FileHash `
        -LiteralPath (Join-Path $Root $Relative.Replace('/', '\')) `
        -Algorithm SHA256).Hash
    $rows | Export-Csv $path -NoTypeInformation
}

function Get-RawPath([string]$Root, [string]$Name) {
    return (Join-Path $Root "$relativeRoot\raw\$Name")
}

function Get-SourcePath([string]$Root, [string]$Relative) {
    return (Join-Path $Root $Relative)
}

# --- two independent clean verifier runs ----------------------------------
Invoke-VerifierPass 'clean-archive-1' $cleanPrimary
Invoke-VerifierPass 'clean-archive-2' $cleanSecondary

# =========================================================================
# Property 1: source topology
# =========================================================================
$tree = New-PerturbationTree 'topology-source-equals-evidence'
$path = Get-RawPath $tree 'topology-summary.csv'
$rows = @(Import-Csv $path)
$rows[0].distinct = 'False'
$rows | Export-Csv $path -NoTypeInformation
Invoke-VerifierFailure `
    'topology-source-equals-evidence' `
    'source-topology' `
    $tree `
    'Source and evidence commits must be distinct.'

$tree = New-PerturbationTree 'topology-not-direct-parent'
$path = Get-RawPath $tree 'topology-summary.csv'
$rows = @(Import-Csv $path)
$rows[0].evidence_parent = '0123456789abcdef0123456789abcdef01234567'
$rows | Export-Csv $path -NoTypeInformation
Invoke-VerifierFailure `
    'topology-not-direct-parent' `
    'source-topology' `
    $tree `
    'Evidence commit is not a direct child of the source commit.'

# =========================================================================
# Property 2: source identity
# =========================================================================
$tree = New-PerturbationTree 'identity-source-content'
Add-Content `
    -LiteralPath (Get-SourcePath $tree 'src\coreclr\gc\reference_count.cpp') `
    -Value '// identity-perturbation'
Invoke-VerifierFailure `
    'identity-source-content' `
    'source-identity' `
    $tree `
    'Source identity mismatch: src/coreclr/gc/reference_count.cpp'

$tree = New-PerturbationTree 'identity-source-path-set'
$path = Get-RawPath $tree 'source-identities.csv'
$rows = @(Import-Csv $path)
$rows[1..($rows.Count - 1)] | Export-Csv $path -NoTypeInformation
Invoke-VerifierFailure `
    'identity-source-path-set' `
    'source-identity' `
    $tree `
    'Source identity path set mismatch.'

# =========================================================================
# Property 3: evidence integrity
# =========================================================================
$tree = New-PerturbationTree 'evidence-missing-validation-row'
$path = Get-RawPath $tree 'validation-summary.csv'
@(Import-Csv $path | Where-Object platform -ne 'linux-x64') |
    Export-Csv $path -NoTypeInformation
Invoke-VerifierFailure `
    'evidence-missing-validation-row' `
    'evidence-integrity' `
    $tree `
    'Validation platform evidence is incomplete.'

$tree = New-PerturbationTree 'evidence-duplicate-runtime-row'
$path = Get-RawPath $tree 'runtime-smoke-summary.csv'
$rows = @(Import-Csv $path)
@($rows + $rows[0]) | Export-Csv $path -NoTypeInformation
Invoke-VerifierFailure `
    'evidence-duplicate-runtime-row' `
    'evidence-integrity' `
    $tree `
    'Runtime smoke evidence is incomplete.'

# =========================================================================
# Property 4: verifier and wrapper completeness
# =========================================================================
$tree = New-PerturbationTree 'verifier-expected-check-count'
Replace-ExactlyOnce `
    (Join-Path $tree $manifestRelative) `
    """verifierExpectedChecks"": $([int]$manifest.verifierExpectedChecks)," `
    """verifierExpectedChecks"": $([int]$manifest.verifierExpectedChecks + 1),"
Update-SourceIdentity $tree 'docs/design/lxr-port/P2.3/evidence-manifest.json'
Invoke-VerifierFailure `
    'verifier-expected-check-count' `
    'verifier-wrapper' `
    $tree `
    'Verifier check count differs'

$tree = New-PerturbationTree 'wrapper-evidence-script-missing'
Remove-Item -LiteralPath (
    Join-Path $tree "$relativeRoot\run-reference-count-evidence.ps1") -Force
Invoke-VerifierFailure `
    'wrapper-evidence-script-missing' `
    'verifier-wrapper' `
    $tree `
    'Required product or evidence source is missing'

# =========================================================================
# Property 5: width and metadata mapping
# =========================================================================
$tree = New-PerturbationTree 'width-active-bits'
Replace-ExactlyOnce `
    (Get-SourcePath $tree 'src\coreclr\gc\reference_count.cpp') `
    '    return static_cast<uint8_t>(1) << m_log_reference_count_bits;' `
    '    return static_cast<uint8_t>(2) << m_log_reference_count_bits;'
Invoke-BehaviorFailure `
    'width-active-bits' `
    'width-mapping' `
    $tree `
    'FAIL: active width'

$tree = New-PerturbationTree 'width-maximum-count'
Replace-ExactlyOnce `
    (Get-SourcePath $tree 'src\coreclr\gc\reference_count.cpp') `
    '        : (static_cast<uintptr_t>(1) << activeBits) - 1;' `
    '        : (static_cast<uintptr_t>(1) << activeBits) - 2;'
Invoke-BehaviorFailure `
    'width-maximum-count' `
    'width-mapping' `
    $tree `
    'FAIL: derived maximum'

# =========================================================================
# Property 6: sticky maximum and zero refusal
# =========================================================================
$tree = New-PerturbationTree 'sticky-decrement-allowed'
Replace-ExactlyOnce `
    (Get-SourcePath $tree 'src\coreclr\gc\side_metadata.cpp') `
    @'
        if (oldValue == 0)
        {
            *status = SideMetadataUpdateStatus::Zero;
            return SideMetadataResult::Success;
        }
        if (oldValue == maximum)
'@ `
    @'
        if (oldValue == 0)
        {
            *status = SideMetadataUpdateStatus::Zero;
            return SideMetadataResult::Success;
        }
        if (oldValue == maximum + 1)
'@
Invoke-BehaviorFailure `
    'sticky-decrement-allowed' `
    'sticky-zero' `
    $tree `
    'FAIL: sticky decrement status'

$tree = New-PerturbationTree 'zero-decrement-allowed'
Replace-ExactlyOnce `
    (Get-SourcePath $tree 'src\coreclr\gc\side_metadata.cpp') `
    @'
        if (oldValue == 0)
        {
            *status = SideMetadataUpdateStatus::Zero;
            return SideMetadataResult::Success;
        }
        if (oldValue == maximum)
'@ `
    @'
        if (oldValue == maximum)
'@
Invoke-BehaviorFailure `
    'zero-decrement-allowed' `
    'sticky-zero' `
    $tree `
    'FAIL: zero decrement status'

# =========================================================================
# Property 7: neighbour atomicity inside one native word
# =========================================================================
$tree = New-PerturbationTree 'neighbor-increment-mask-drop'
Replace-ExactlyOnce `
    (Get-SourcePath $tree 'src\coreclr\gc\side_metadata.cpp') `
    @'
        uintptr_t newWord =
            (oldWord & ~location.mask) | (((oldValue + 1) << location.shift) & location.mask);
'@ `
    @'
        uintptr_t newWord =
            ((oldValue + 1) << location.shift) & location.mask;
'@
Invoke-BehaviorFailure `
    'neighbor-increment-mask-drop' `
    'neighbor-atomicity' `
    $tree `
    'FAIL: shared-word count preserved'

$tree = New-PerturbationTree 'neighbor-store-full-word'
Replace-ExactlyOnce `
    (Get-SourcePath $tree 'src\coreclr\gc\side_metadata.cpp') `
    '        uintptr_t newWord = (oldWord & ~location.mask) | ((newValue << location.shift) & location.mask);' `
    '        uintptr_t newWord = (newValue << location.shift) & location.mask;'
Invoke-BehaviorFailure `
    'neighbor-store-full-word' `
    'neighbor-atomicity' `
    $tree `
    'FAIL: load count succeeds'

# =========================================================================
# Property 8: concurrent compare-exchange convergence
# =========================================================================
$tree = New-PerturbationTree 'concurrency-increment-cas-comparand'
Replace-ExactlyOnce `
    (Get-SourcePath $tree 'src\coreclr\gc\side_metadata.cpp') `
    @'
            (oldWord & ~location.mask) | (((oldValue + 1) << location.shift) & location.mask);
        uintptr_t actual = CompareExchangeWord(location.word, newWord, oldWord);
'@ `
    @'
            (oldWord & ~location.mask) | (((oldValue + 1) << location.shift) & location.mask);
        uintptr_t actual = CompareExchangeWord(location.word, newWord, newWord);
'@
Invoke-BehaviorFailure `
    'concurrency-increment-cas-comparand' `
    'concurrency' `
    $tree `
    'FAIL: same-slot final sticky'

$tree = New-PerturbationTree 'concurrency-decrement-cas-comparand'
Replace-ExactlyOnce `
    (Get-SourcePath $tree 'src\coreclr\gc\side_metadata.cpp') `
    @'
            (oldWord & ~location.mask) | (((oldValue - 1) << location.shift) & location.mask);
        uintptr_t actual = CompareExchangeWord(location.word, newWord, oldWord);
'@ `
    @'
            (oldWord & ~location.mask) | (((oldValue - 1) << location.shift) & location.mask);
        uintptr_t actual = CompareExchangeWord(location.word, newWord, newWord);
'@
Invoke-BehaviorFailure `
    'concurrency-decrement-cas-comparand' `
    'concurrency' `
    $tree `
    'FAIL: decrement result one'

# =========================================================================
# Property 9: exact object starts
# =========================================================================
$tree = New-PerturbationTree 'object-start-alignment'
Replace-ExactlyOnce `
    (Get-SourcePath $tree 'src\coreclr\gc\reference_count.cpp') `
    @'
    if ((m_metadata == nullptr) ||
        ((object & (ObjectAlignment - 1)) != 0) ||
        !m_metadata->IsDataAddressOwned(object) ||
'@ `
    @'
    if ((m_metadata == nullptr) ||
        !m_metadata->IsDataAddressOwned(object) ||
'@
Invoke-BehaviorFailure `
    'object-start-alignment' `
    'object-starts' `
    $tree `
    'FAIL: misaligned byref rejected'

$tree = New-PerturbationTree 'object-start-registration'
Replace-ExactlyOnce `
    (Get-SourcePath $tree 'src\coreclr\gc\reference_count.cpp') `
    @'
    return valid == 1
        ? SideMetadataResult::Success
        : SideMetadataResult::AddressNotOwned;
'@ `
    @'
    return valid <= 1
        ? SideMetadataResult::Success
        : SideMetadataResult::AddressNotOwned;
'@
Invoke-BehaviorFailure `
    'object-start-registration' `
    'object-starts' `
    $tree `
    'FAIL: unregistered exact start rejected'

# =========================================================================
# Property 10: straddle range
# =========================================================================
$tree = New-PerturbationTree 'straddle-includes-first-line'
Replace-ExactlyOnce `
    (Get-SourcePath $tree 'src\coreclr\gc\reference_count.cpp') `
    @'
    *first = (object & ~ImmixBlockGeometry::LineMask) +
        ImmixBlockGeometry::LineBytes;
'@ `
    @'
    *first = object & ~ImmixBlockGeometry::LineMask;
'@
Invoke-BehaviorFailure `
    'straddle-includes-first-line' `
    'straddle-range' `
    $tree `
    'FAIL: mark straddle lines'

$tree = New-PerturbationTree 'straddle-includes-partial-tail'
Replace-ExactlyOnce `
    (Get-SourcePath $tree 'src\coreclr\gc\reference_count.cpp') `
    '    *end = (object + size) & ~ImmixBlockGeometry::LineMask;' `
    @'
    *end = ((object + size) + ImmixBlockGeometry::LineMask) &
        ~ImmixBlockGeometry::LineMask;
'@
Invoke-BehaviorFailure `
    'straddle-includes-partial-tail' `
    'straddle-range' `
    $tree `
    'FAIL: following line is not continuation'

# =========================================================================
# Property 11: the sentinel pair
# =========================================================================
$tree = New-PerturbationTree 'sentinel-missing-straddle-bit'
Replace-ExactlyOnce `
    (Get-SourcePath $tree 'src\coreclr\gc\reference_count.cpp') `
    @'
        SideMetadataResult result = m_metadata->Store(
            LxrSideMetadataKind::ReferenceCountStraddle,
            line,
            1,
            SideMetadataMemoryOrder::Relaxed);
        if (result == SideMetadataResult::Success)
        {
            result = m_metadata->Store(
                LxrSideMetadataKind::ReferenceCount,
                line,
                1,
                SideMetadataMemoryOrder::Relaxed);
        }
'@ `
    @'
        SideMetadataResult result = m_metadata->Store(
            LxrSideMetadataKind::ReferenceCount,
            line,
            1,
            SideMetadataMemoryOrder::Relaxed);
'@
Invoke-BehaviorFailure `
    'sentinel-missing-straddle-bit' `
    'sentinel-pair' `
    $tree `
    'FAIL: expected continuation'

$tree = New-PerturbationTree 'sentinel-missing-reference-count'
Replace-ExactlyOnce `
    (Get-SourcePath $tree 'src\coreclr\gc\reference_count.cpp') `
    @'
            result = m_metadata->Store(
                LxrSideMetadataKind::ReferenceCount,
                line,
                1,
                SideMetadataMemoryOrder::Relaxed);
'@ `
    @'
            result = m_metadata->Store(
                LxrSideMetadataKind::ReferenceCount,
                line,
                0,
                SideMetadataMemoryOrder::Relaxed);
'@
Invoke-BehaviorFailure `
    'sentinel-missing-reference-count' `
    'sentinel-pair' `
    $tree `
    'FAIL: continuation query succeeds'

# =========================================================================
# Property 12: copy, release, and quiescent reset
# =========================================================================
$tree = New-PerturbationTree 'copy-drops-source-count'
Replace-ExactlyOnce `
    (Get-SourcePath $tree 'src\coreclr\gc\reference_count.cpp') `
    @'
        result = m_metadata->Store(
            LxrSideMetadataKind::ReferenceCount,
            destination,
            count,
            SideMetadataMemoryOrder::Relaxed);
'@ `
    @'
        result = m_metadata->Store(
            LxrSideMetadataKind::ReferenceCount,
            destination,
            0,
            SideMetadataMemoryOrder::Relaxed);
'@
Invoke-BehaviorFailure `
    'copy-drops-source-count' `
    'lifecycle-copy-reset' `
    $tree `
    'FAIL: copied count'

$tree = New-PerturbationTree 'reset-retains-validity'
Replace-ExactlyOnce `
    (Get-SourcePath $tree 'src\coreclr\gc\reference_count.cpp') `
    '    result = SetValidObject(object, 0);' `
    '    result = SetValidObject(object, 1);'
Invoke-BehaviorFailure `
    'reset-retains-validity' `
    'lifecycle-copy-reset' `
    $tree `
    'FAIL: reset object no longer registered'

# =========================================================================
# Property 13: reusable extents require prior metadata dismantling
# =========================================================================
$tree = New-PerturbationTree 'continuation-start-registration-allowed'
Replace-ExactlyOnce `
    (Get-SourcePath $tree 'src\coreclr\gc\reference_count.cpp') `
    @'
    return continuation
        ? SideMetadataResult::InconsistentState
        : SideMetadataResult::Success;
'@ `
    @'
    return SideMetadataResult::Success;
'@
Invoke-BehaviorFailure `
    'continuation-start-registration-allowed' `
    'reusable-extent' `
    $tree `
    'FAIL: continuation line start rejected'

$tree = New-PerturbationTree 'reset-leaves-continuation-sentinel'
Replace-ExactlyOnce `
    (Get-SourcePath $tree 'src\coreclr\gc\reference_count.cpp') `
    @'
    result = SetValidObject(object, 0);
    if (result == SideMetadataResult::Success)
    {
        result = ClearStraddleLinesUnchecked(object, size);
    }
'@ `
    @'
    result = SetValidObject(object, 0);
    if (result == SideMetadataResult::Success)
    {
        result = SideMetadataResult::Success;
    }
'@
Invoke-BehaviorFailure `
    'reset-leaves-continuation-sentinel' `
    'reusable-extent' `
    $tree `
    'FAIL: former continuation start reusable'

# =========================================================================
# Property 14: platform matrix and default-off
# =========================================================================
$tree = New-PerturbationTree 'platform-x86-total'
$path = Get-RawPath $tree 'validation-summary.csv'
$rows = @(Import-Csv $path)
foreach ($row in $rows) {
    if ($row.platform -eq 'windows-x86') {
        $row.total = '37'
    }
}
$rows | Export-Csv $path -NoTypeInformation
Invoke-VerifierFailure `
    'platform-x86-total' `
    'platform-default-off' `
    $tree `
    'Validation total differs for windows-x86.'

$tree = New-PerturbationTree 'default-off-control'
$path = Get-RawPath $tree 'default-path-summary.csv'
$rows = @(Import-Csv $path)
$absence = @($rows | Where-Object property -eq 'default-coreclr-export')
if ($absence.Count -eq 0) {
    throw 'Default-path evidence has no export-absence row to perturb.'
}
$absence[0].result = 'FAIL'
$rows | Export-Csv $path -NoTypeInformation
Invoke-VerifierFailure `
    'default-off-control' `
    'platform-default-off' `
    $tree `
    'Default-path control did not pass'

# =========================================================================
# Property 15: benchmark identity and injected-cost sensitivity
# =========================================================================
$tree = New-PerturbationTree 'benchmark-native-identity'
$path = Get-RawPath $tree 'benchmark-identity.csv'
$rows = @(Import-Csv $path)
$rows[0].native_sha256 = 'not-a-sha256'
$rows | Export-Csv $path -NoTypeInformation
Invoke-VerifierFailure `
    'benchmark-native-identity' `
    'benchmark-identity-sensitivity' `
    $tree `
    'Benchmark native identity is malformed.'

$tree = New-PerturbationTree 'benchmark-extra-cas-sensitivity'
$path = Get-RawPath $tree 'benchmark-controls.csv'
$rows = @(Import-Csv $path)
$rows[0].extra_cas_ratio = '1.0'
$rows | Export-Csv $path -NoTypeInformation
Invoke-VerifierFailure `
    'benchmark-extra-cas-sensitivity' `
    'benchmark-identity-sensitivity' `
    $tree `
    'Benchmark extra-CAS control is below its bound.'

# --- completeness ----------------------------------------------------------
$expectedCount = [int]$manifest.gate.verifierPassRuns + $manifest.perturbations.Count
if ($manifest.perturbations.Count -ne [int]$manifest.gate.perturbationCount) {
    throw "Manifest perturbation count differs: $($manifest.perturbations.Count)/$([int]$manifest.gate.perturbationCount)."
}
if ($expectedCount -ne [int]$manifest.gate.summaryRows) {
    throw "Manifest summary row total differs: $expectedCount/$([int]$manifest.gate.summaryRows)."
}
$declaredProperties = @(
    $manifest.perturbations | Group-Object property | Sort-Object Name
)
if ($declaredProperties.Count -ne [int]$manifest.gate.propertyCount) {
    throw "Manifest property count differs: $($declaredProperties.Count)/$([int]$manifest.gate.propertyCount)."
}
foreach ($group in $declaredProperties) {
    if ($group.Count -ne [int]$manifest.gate.perturbationsPerProperty) {
        throw "Property '$($group.Name)' has $($group.Count) perturbations."
    }
}
$executedIds = @($summary | Where-Object kind -ne 'verifier-pass' | ForEach-Object { $_.id })
$declaredIds = @($manifest.perturbations | ForEach-Object { $_.id })
$missing = @($declaredIds | Where-Object { $executedIds -notcontains $_ })
if ($missing.Count -ne 0) {
    throw "Declared perturbations were not executed: $($missing -join ', ')."
}
$unexpected = @($executedIds | Where-Object { $declaredIds -notcontains $_ })
if ($unexpected.Count -ne 0) {
    throw "Undeclared perturbations were executed: $($unexpected -join ', ')."
}
foreach ($declared in $manifest.perturbations) {
    $row = @($summary | Where-Object id -eq $declared.id)
    if ($row.Count -ne 1) {
        throw "Gate summary row differs for '$($declared.id)'."
    }
    if ($row[0].property -ne $declared.property) {
        throw "Gate summary property differs for '$($declared.id)'."
    }
    $expectedKind = if ($declared.kind -eq 'behavior') {
        'behavior-failure'
    } else {
        'verifier-failure'
    }
    if ($row[0].kind -ne $expectedKind) {
        throw "Gate summary kind differs for '$($declared.id)'."
    }
    if ($row[0].expected -ne $declared.expected) {
        throw "Gate expected message differs for '$($declared.id)'."
    }
}
if ($summary.Count -ne $expectedCount) {
    throw "Gate result count differs: $($summary.Count)/$expectedCount."
}

$summary | Export-Csv (Join-Path $OutputDirectory 'gate-summary.csv') -NoTypeInformation
Remove-Item -LiteralPath $scratch -Recurse -Force
Write-Host "PASS: $($summary.Count) P2.3 archive gate scenarios"
Write-Host "Output: $OutputDirectory"
exit 0
