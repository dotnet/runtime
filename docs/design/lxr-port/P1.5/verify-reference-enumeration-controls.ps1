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
        'artifacts\p15-reference-enumeration-controls')
}

$gcRoot = Join-Path $RepositoryRoot 'src\coreclr\gc'
$header = Join-Path $gcRoot 'gcref.h'
$source = Join-Path $scriptRoot 'reference-enumeration-validation.cpp'
$initializeVisualStudio = Join-Path $RepositoryRoot 'eng\native\init-vs-env.cmd'
$summaryPath = Join-Path $OutputDirectory 'control-summary.csv'
$summary = [Collections.Generic.List[object]]::new()

New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$originalHeader = Get-Content -LiteralPath $header -Raw

function Invoke-Control(
    [string]$Name,
    [string]$OldText,
    [string]$NewText,
    [string]$ExtraDefine
) {
    $controlDirectory = Join-Path $OutputDirectory $Name
    New-Item -ItemType Directory -Path $controlDirectory -Force | Out-Null
    $object = Join-Path $controlDirectory 'validation.obj'
    $executable = Join-Path $controlDirectory 'validation.exe'
    $log = Join-Path $controlDirectory 'validation.log'
    $forceInclude = ''

    if ($OldText) {
        $matchCount = [regex]::Matches(
            $originalHeader,
            [regex]::Escape($OldText)).Count
        if ($matchCount -ne 1) {
            throw "$Name matched $matchCount header sites; expected exactly one."
        }

        $controlHeader = Join-Path $controlDirectory 'gcref-control.h'
        $wrapper = Join-Path $controlDirectory 'force-include.h'
        Set-Content -LiteralPath $controlHeader -Value (
            $originalHeader.Replace($OldText, $NewText)) -NoNewline
        Set-Content -LiteralPath $wrapper -Value @"
#include "env/common.h"
#include "env/gcenv.h"
#include "gcref-control.h"
"@
        $forceInclude = "/FI`"$wrapper`" /I`"$controlDirectory`""
    }

    $command = @(
        "call `"$initializeVisualStudio`" x64",
        "cl /nologo /std:c++17 /EHsc /DBUILD_AS_STANDALONE /DTARGET_WINDOWS /DHOST_WINDOWS /DWIN32 /DTARGET_AMD64 /DHOST_AMD64 /DTARGET_64BIT /DHOST_64BIT $ExtraDefine $forceInclude /I`"$gcRoot`" /I`"$gcRoot\env`" /I`"$RepositoryRoot\src\native`" /I`"$RepositoryRoot\src\native\inc`" `"$source`" /Fo:`"$object`" /Fe:`"$executable`"",
        "`"$executable`""
    ) -join ' && '

    & $env:ComSpec /d /s /c $command *> $log
    $exitCode = $LASTEXITCODE
    $output = Get-Content -LiteralPath $log -Raw
    if (($exitCode -eq 0) -or ($output -notmatch '(?m)^FAIL:')) {
        throw "$Name did not fail through a validation assertion. See $log."
    }

    $summary.Add([pscustomobject][ordered]@{
        Name = $Name
        PerturbationCount = 1
        Expected = 'validation failure'
        Result = 'PASS'
        ExitCode = $exitCode
        Evidence = "$Name\validation.log"
    })
    Write-Host "PASS: $Name"
}

Invoke-Control `
    'positive-series-order' `
    'm_lastSeries = map->GetLowestSeries();' `
    'm_lastSeries = map->GetHighestSeries();' `
    ''
Invoke-Control `
    'cursor-positive-count' `
    'size_t count = rangeSize / sizeof(Object*);' `
    'size_t count = rangeSize / sizeof(Object*) + 1;' `
    ''
Invoke-Control `
    'visitor-positive-count' `
    'size_t rangeCount = rangeSize / sizeof(Object*);' `
    'size_t rangeCount = rangeSize / sizeof(Object*) + 1;' `
    ''
Invoke-Control `
    'cursor-repeating-stride' `
    'm_repeatingCursor += rangeSize + item->skip;' `
    'm_repeatingCursor += rangeSize + item->skip + sizeof(Object*);' `
    ''
Invoke-Control `
    'visitor-repeating-stride' `
    'reinterpret_cast<uint8_t*>(rangeStart + item->nptrs) +' `
    'reinterpret_cast<uint8_t*>(rangeStart + item->nptrs + 1) +' `
    ''
Invoke-Control `
    'null-filtering' `
    '' `
    '' `
    '/DP15_FILTER_NULL_CONTROL'

if ($summary.Count -ne 6) {
    throw "Produced $($summary.Count) control rows; expected 6."
}
$summary | Export-Csv -LiteralPath $summaryPath -NoTypeInformation
Write-Host "PASS: $($summary.Count) exact-cardinality controls"
$global:LASTEXITCODE = 0
