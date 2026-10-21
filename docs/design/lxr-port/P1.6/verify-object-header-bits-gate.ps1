# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

[CmdletBinding()]
param(
    [string]$RepositoryRoot,
    [string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path

if (-not $RepositoryRoot) {
    $RepositoryRoot = (Resolve-Path (Join-Path $scriptRoot '..\..\..\..')).Path
}

$parent = if ($OutputDirectory) {
    $OutputDirectory
} else {
    [IO.Path]::GetTempPath()
}
New-Item -ItemType Directory -Path $parent -Force | Out-Null
$runRoot = Join-Path $parent ("p16-object-header-bits-gate-" + [guid]::NewGuid())
$archive = Join-Path $runRoot 'source.tar'
$clean = Join-Path $runRoot 'clean'
$summary = [Collections.Generic.List[object]]::new()
New-Item -ItemType Directory -Path $clean -Force | Out-Null

function Invoke-Verifier(
    [string]$Name,
    [string]$Root,
    [bool]$ExpectSuccess,
    [string]$ExpectedFailure
) {
    $log = Join-Path $runRoot "$Name.log"
    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File `
        (Join-Path $Root 'docs\design\lxr-port\P1.6\verify-object-header-bits.ps1') `
        -RepositoryRoot $Root *> $log
    $exitCode = $LASTEXITCODE
    $text = Get-Content -LiteralPath $log -Raw

    if ($ExpectSuccess) {
        if (($exitCode -ne 0) -or
            ($text -notmatch 'PASS: P1.6 object-header bit evidence')) {
            throw "Clean archive verification failed. See $log."
        }
    } elseif (($exitCode -eq 0) -or
        ($text -notmatch [regex]::Escape($ExpectedFailure))) {
        throw "Perturbation '$Name' did not fail for '$ExpectedFailure'. See $log."
    }

    $summary.Add([pscustomobject][ordered]@{
        name = $Name
        expected = $(if ($ExpectSuccess) { 'pass' } else { 'fail' })
        exit_code = $exitCode
        result = 'PASS'
        evidence = [IO.Path]::GetFileName($log)
    })
}

function New-PerturbationTree([string]$Destination) {
    $paths = @(
        'docs\design\lxr-port\P1.4\verify-allocation-notification.ps1',
        'docs\design\lxr-port\P1.5\reference-enumeration-validation.cpp',
        'docs\design\lxr-port\P1.5\verify-reference-enumeration.ps1',
        'docs\design\lxr-port\P1.6',
        'docs\design\lxr-port\P1.6-gc-reserved-object-header-bits.md',
        'docs\design\lxr-port\README.md',
        'src\coreclr\CMakeLists.txt',
        'src\coreclr\dlls\mscoree\CMakeLists.txt',
        'src\coreclr\dlls\mscoree\coreclr\CMakeLists.txt',
        'src\coreclr\dlls\mscoree\mscorwks_objectheaderbitstest_unixexports.src',
        'src\coreclr\gc\CMakeLists.txt',
        'src\coreclr\gc\env\gcenv.object.h',
        'src\coreclr\gc\gcconfig.h',
        'src\coreclr\gc\gcimpl.h',
        'src\coreclr\gc\gcinterface.h',
        'src\coreclr\gc\interface.cpp',
        'src\coreclr\nativeaot\Runtime\ObjectLayout.cpp',
        'src\coreclr\nativeaot\Runtime\ObjectLayout.h',
        'src\coreclr\nativeaot\Runtime\clrgc.enabled.cpp',
        'src\coreclr\nativeaot\Runtime\gcheaputilities.cpp',
        'src\coreclr\nativeaot\Runtime\gcheaputilities.h',
        'src\coreclr\vm\CMakeLists.txt',
        'src\coreclr\vm\gcheaputilities.cpp',
        'src\coreclr\vm\gcheaputilities.h',
        'src\coreclr\vm\object.h',
        'src\coreclr\vm\objectheaderbitstest.cpp',
        'src\coreclr\vm\syncblk.cpp',
        'src\coreclr\vm\syncblk.h',
        'src\coreclr\vm\wks\CMakeLists.txt'
    )

    foreach ($path in $paths) {
        $source = Join-Path $clean $path
        $target = Join-Path $Destination $path
        $targetParent = Split-Path -Parent $target
        New-Item -ItemType Directory -Path $targetParent -Force | Out-Null
        if (Test-Path -LiteralPath $source -PathType Container) {
            Copy-Item -LiteralPath $source -Destination $targetParent -Recurse
        } else {
            Copy-Item -LiteralPath $source -Destination $target
        }
    }
}

git -C $RepositoryRoot diff --quiet HEAD
if ($LASTEXITCODE -ne 0) {
    throw 'The archive gate requires a clean committed worktree.'
}
git -C $RepositoryRoot diff --cached --quiet HEAD
if ($LASTEXITCODE -ne 0) {
    throw 'The archive gate requires an empty index.'
}

git -C $RepositoryRoot archive --format=tar --output=$archive HEAD
if ($LASTEXITCODE -ne 0) {
    throw 'Unable to create exact HEAD archive.'
}
tar -xf $archive -C $clean
if ($LASTEXITCODE -ne 0) {
    throw 'Unable to extract exact HEAD archive.'
}

Invoke-Verifier 'clean' $clean $true ''

$missingRuntimeRow = Join-Path $runRoot 'missing-runtime-row'
New-PerturbationTree $missingRuntimeRow
$runtimePath = Join-Path $missingRuntimeRow (
    'docs\design\lxr-port\P1.6\raw\runtime-summary.csv')
$runtimeRows = @(Import-Csv $runtimePath)
$runtimeRows[0..($runtimeRows.Count - 2)] |
    Export-Csv $runtimePath -NoTypeInformation
Invoke-Verifier 'missing-runtime-row' $missingRuntimeRow $false (
    'Runtime scenario evidence is incomplete')

$changedCodegen = Join-Path $runRoot 'changed-codegen'
New-PerturbationTree $changedCodegen
$codegenPath = Join-Path $changedCodegen (
    'docs\design\lxr-port\P1.6\raw\hot-function-codegen.csv')
$codegenRows = @(Import-Csv $codegenPath)
$codegenRows[0].normalized_sha256 = '0' * 64
$codegenRows | Export-Csv $codegenPath -NoTypeInformation
Invoke-Verifier 'changed-codegen' $changedCodegen $false (
    'Hot-function codegen changed')

$x86FalsePositive = Join-Path $runRoot 'x86-false-positive'
New-PerturbationTree $x86FalsePositive
$x86Path = Join-Path $x86FalsePositive (
    'docs\design\lxr-port\P1.6\raw\x86-negotiation.csv')
$x86Rows = @(Import-Csv $x86Path)
$x86Rows[0].enabled_exit = '100'
$x86Rows | Export-Csv $x86Path -NoTypeInformation
Invoke-Verifier 'x86-false-positive' $x86FalsePositive $false (
    'x86 fail-closed evidence is incomplete')

$missingIdentityRow = Join-Path $runRoot 'missing-identity-row'
New-PerturbationTree $missingIdentityRow
$identityPath = Join-Path $missingIdentityRow (
    'docs\design\lxr-port\P1.6\raw\source-identities.csv')
$identityRows = @(Import-Csv $identityPath)
$identityRows[0..($identityRows.Count - 2)] |
    Export-Csv $identityPath -NoTypeInformation
Invoke-Verifier 'missing-identity-row' $missingIdentityRow $false (
    'Identity manifest path set mismatch')

$changedImplementation = Join-Path $runRoot 'changed-implementation'
New-PerturbationTree $changedImplementation
$implementationPath = Join-Path $changedImplementation (
    'src\coreclr\nativeaot\Runtime\ObjectLayout.cpp')
$implementationText = Get-Content -LiteralPath $implementationPath -Raw
$seqCstLoad = @'
    uint32_t value = static_cast<uint32_t>(PalInterlockedCompareExchange(
        reinterpret_cast<volatile int32_t*>(&m_uGCReservedBits),
        0,
        0));
'@
$plainLoad = @'
    uint32_t value = m_uGCReservedBits;
'@
$implementationMatches = [regex]::Matches(
    $implementationText,
    [regex]::Escape($seqCstLoad)).Count
if ($implementationMatches -ne 1) {
    throw "SeqCst implementation control matched $implementationMatches sites; expected one."
}
Set-Content -LiteralPath $implementationPath -Value (
    $implementationText.Replace($seqCstLoad, $plainLoad)) -NoNewline
Invoke-Verifier 'changed-implementation' $changedImplementation $false (
    'Identity mismatch: src\coreclr\nativeaot\Runtime\ObjectLayout.cpp')

$summary | Export-Csv (Join-Path $runRoot 'gate-summary.csv') -NoTypeInformation
Write-Host "PASS: exact archive and 5 perturbation controls"
Write-Host "Output: $runRoot"
exit 0
