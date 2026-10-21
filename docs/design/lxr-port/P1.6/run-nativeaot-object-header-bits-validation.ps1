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
if (-not $OutputDirectory) {
    $OutputDirectory = Join-Path $RepositoryRoot (
        'artifacts\p16-nativeaot-object-header-bits')
}

$validation = Join-Path $scriptRoot 'nativeaot-object-header-bits-validation.cpp'
$implementation = Join-Path $RepositoryRoot 'src\coreclr\nativeaot\Runtime\ObjectLayout.cpp'
$initializeVisualStudio = Join-Path $RepositoryRoot 'eng\native\init-vs-env.cmd'
$executable = Join-Path $OutputDirectory 'nativeaot-object-header-bits-validation.exe'
$log = Join-Path $OutputDirectory 'compile.log'

$includeDirectories = @(
    'src\coreclr\nativeaot\Runtime',
    'src\coreclr\nativeaot\Runtime\inc',
    'src\coreclr\nativeaot\Runtime\windows',
    'src\coreclr\nativeaot\Runtime\amd64',
    'src\coreclr\gc',
    'src\coreclr\gc\env',
    'src\coreclr\minipal',
    'src\native',
    'src\native\inc',
    'src\coreclr\pal\prebuilt\inc',
    'artifacts\obj'
)
$includeArguments = ($includeDirectories | ForEach-Object {
    "/I`"$(Join-Path $RepositoryRoot $_)`""
}) -join ' '
$defines = @(
    '/DFEATURE_NATIVEAOT',
    '/DGC_DESCRIPTOR',
    '/DTARGET_WINDOWS',
    '/DHOST_WINDOWS',
    '/DWIN32',
    '/DTARGET_AMD64',
    '/DHOST_AMD64',
    '/DTARGET_64BIT',
    '/DHOST_64BIT'
) -join ' '

New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$command = @(
    "call `"$initializeVisualStudio`" x64",
    "cl /nologo /std:c++17 /permissive- /EHsc /Gy /wd4005 $defines $includeArguments `"$validation`" `"$implementation`" /Fe:`"$executable`" /link /OPT:REF"
) -join ' && '

& $env:ComSpec /d /s /c $command *> $log
if ($LASTEXITCODE -ne 0) {
    Get-Content -LiteralPath $log -Tail 80
    exit $LASTEXITCODE
}

& $executable
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}
