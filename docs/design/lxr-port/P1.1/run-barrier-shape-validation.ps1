# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

[CmdletBinding()]
param(
    [string]$RepositoryRoot,
    [string]$OutputDirectory,
    [ValidateSet('x64', 'x86')]
    [string]$Architecture = 'x64'
)

$ErrorActionPreference = 'Stop'
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path

if (-not $RepositoryRoot) {
    $RepositoryRoot = (Resolve-Path (Join-Path $scriptRoot '..\..\..\..')).Path
}

if (-not $OutputDirectory) {
    $OutputDirectory = Join-Path $RepositoryRoot "artifacts\p11-barrier-shape-validation\$Architecture"
}

$gcRoot = Join-Path $RepositoryRoot 'src\coreclr\gc'
$source = Join-Path $scriptRoot 'barrier-shape-validation.cpp'
$executable = Join-Path $OutputDirectory 'barrier-shape-validation.exe'
$object = Join-Path $OutputDirectory 'barrier-shape-validation.obj'
$majorMismatchSource = Join-Path $scriptRoot 'major-mismatch-gc.cpp'
$olderMajorLibrary = Join-Path $OutputDirectory 'older-major-gc.dll'
$olderMajorObject = Join-Path $OutputDirectory 'older-major-gc.obj'
$newerMajorLibrary = Join-Path $OutputDirectory 'newer-major-gc.dll'
$newerMajorObject = Join-Path $OutputDirectory 'newer-major-gc.obj'
$initializeVisualStudio = Join-Path $RepositoryRoot 'eng\native\init-vs-env.cmd'
$targetDefines = if ($Architecture -eq 'x64') {
    '/DTARGET_AMD64 /DHOST_AMD64 /DTARGET_64BIT /DHOST_64BIT'
} else {
    '/DTARGET_X86 /DHOST_X86'
}

New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null

$command = @(
    "call `"$initializeVisualStudio`" $Architecture",
    "cl /nologo /std:c++17 /EHsc /DBUILD_AS_STANDALONE /DTARGET_WINDOWS /DHOST_WINDOWS /DWIN32 $targetDefines /I`"$gcRoot`" /I`"$gcRoot\env`" /I`"$RepositoryRoot\src\native`" /I`"$RepositoryRoot\src\native\inc`" `"$source`" /Fo:`"$object`" /Fe:`"$executable`"",
    "cl /nologo /std:c++17 /EHsc /LD /DBUILD_AS_STANDALONE /DTARGET_WINDOWS /DHOST_WINDOWS /DWIN32 $targetDefines /I`"$gcRoot`" /I`"$gcRoot\env`" /I`"$RepositoryRoot\src\native`" /I`"$RepositoryRoot\src\native\inc`" `"$majorMismatchSource`" /Fo:`"$olderMajorObject`" /Fe:`"$olderMajorLibrary`"",
    "cl /nologo /std:c++17 /EHsc /LD /DP11_GC_MAJOR_VERSION_DELTA=1 /DBUILD_AS_STANDALONE /DTARGET_WINDOWS /DHOST_WINDOWS /DWIN32 $targetDefines /I`"$gcRoot`" /I`"$gcRoot\env`" /I`"$RepositoryRoot\src\native`" /I`"$RepositoryRoot\src\native\inc`" `"$majorMismatchSource`" /Fo:`"$newerMajorObject`" /Fe:`"$newerMajorLibrary`"",
    "`"$executable`""
) -join ' && '

& $env:ComSpec /d /s /c $command
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

$constructors = Get-ChildItem -LiteralPath $gcRoot -File -Recurse -Include '*.h', '*.cpp' |
    Select-String -Pattern 'WriteBarrierParameters\s+\w+\s*=\s*\{\s*\}\s*;' |
    ForEach-Object { $_.Line.Trim() }

if ($constructors.Count -ne 5) {
    throw "Expected exactly 5 zero-initialized WriteBarrierParameters constructions, found $($constructors.Count)."
}

$obsoletePatterns = @(
    'GCWriteBarrierCapabilities',
    'GetWriteBarrierCapabilities',
    'SelectWriteBarrierCapabilities',
    'GC_WRITE_BARRIER_CAPABILITIES'
)
$obsoleteMatches = Get-ChildItem -LiteralPath (Join-Path $RepositoryRoot 'src\coreclr') -File -Recurse -Include '*.h', '*.cpp' |
    Select-String -Pattern $obsoletePatterns
if ($obsoleteMatches) {
    throw "Obsolete capability-negotiation symbols remain:`n$($obsoleteMatches -join [Environment]::NewLine)"
}

Write-Host "PASS: 5/5 parameter constructions are aggregate-zero-initialized"
Write-Host "PASS: obsolete capability-negotiation symbols are absent"
Write-Host "PASS: built older/newer major-version GCs at $olderMajorLibrary and $newerMajorLibrary"
