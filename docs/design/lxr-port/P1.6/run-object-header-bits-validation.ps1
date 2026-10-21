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
    $OutputDirectory = Join-Path $RepositoryRoot (
        "artifacts\p16-object-header-bits-validation\$Architecture")
}

$gcRoot = Join-Path $RepositoryRoot 'src\coreclr\gc'
$source = Join-Path $scriptRoot 'object-header-bits-validation.cpp'
$executable = Join-Path $OutputDirectory 'object-header-bits-validation.exe'
$object = Join-Path $OutputDirectory 'object-header-bits-validation.obj'
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
    "`"$executable`""
) -join ' && '

& $env:ComSpec /d /s /c $command
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}
