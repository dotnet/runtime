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
        'artifacts\P2.4\validation\' + [guid]::NewGuid().ToString('N'))
} else {
    $OutputDirectory = $PSCmdlet.GetUnresolvedProviderPathFromPSPath($OutputDirectory)
}
if (Test-Path -LiteralPath $OutputDirectory) {
    throw "OutputDirectory already exists: '$OutputDirectory'."
}

$gcRoot = Join-Path $RepositoryRoot 'src\coreclr\gc'
$nativeRoot = Join-Path $RepositoryRoot 'src\native'
$initializeVisualStudio = Join-Path $RepositoryRoot 'eng\native\init-vs-env.cmd'
$validation = Join-Path $scriptRoot 'immix-allocation-validation.cpp'
$platform = Join-Path $scriptRoot '..\P2.1\side-metadata-test-platform.cpp'
$sources = @(
    $validation,
    $platform,
    (Join-Path $gcRoot 'side_metadata.cpp'),
    (Join-Path $gcRoot 'immix_block.cpp'),
    (Join-Path $gcRoot 'immix_allocation.cpp')
)
foreach ($path in @($initializeVisualStudio) + $sources) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Required validation input is missing: '$path'."
    }
}

New-Item -ItemType Directory -Path $OutputDirectory | Out-Null
$rows = [Collections.Generic.List[object]]::new()
foreach ($architecture in @('x64', 'x86')) {
    $target = Join-Path $OutputDirectory $architecture
    New-Item -ItemType Directory -Path $target | Out-Null
    $executable = Join-Path $target 'immix-allocation-validation.exe'
    $log = Join-Path $target 'run.log'
    $defines = if ($architecture -eq 'x64') {
        '/DTARGET_AMD64 /DHOST_AMD64 /DTARGET_64BIT /DHOST_64BIT'
    } else {
        '/DTARGET_X86 /DHOST_X86'
    }
    $quotedSources = $sources | ForEach-Object { "`"$_`"" }
    $command = @(
        "call `"$initializeVisualStudio`" $architecture",
        "cd /d `"$target`"",
        (
            'cl /nologo /std:c++17 /EHsc /W4 /WX /wd4100 /wd4324 ' +
            '/DBUILD_AS_STANDALONE /DTARGET_WINDOWS /DHOST_WINDOWS /DWIN32 ' +
            "$defines /I`"$gcRoot`" /I`"$gcRoot\env`" " +
            "/I`"$nativeRoot`" /I`"$nativeRoot\inc`" " +
            ($quotedSources -join ' ') +
            " /Fe:`"$executable`""
        ),
        "`"$executable`""
    ) -join ' && '

    $savedErrorActionPreference = $ErrorActionPreference
    try {
        $ErrorActionPreference = 'Continue'
        & $env:ComSpec /d /s /c $command *> $log
        $exitCode = $LASTEXITCODE
    } finally {
        $ErrorActionPreference = $savedErrorActionPreference
    }
    if ($exitCode -ne 0) {
        throw "Immix allocation validation failed for $architecture. See '$log'."
    }

    $match = Select-String -LiteralPath $log `
        -Pattern '(\d+)/(\d+) immix allocation checks passed'
    if ($match.Count -ne 1) {
        throw "Validation result is missing for $architecture."
    }
    $passed = [int]$match.Matches[0].Groups[1].Value
    $total = [int]$match.Matches[0].Groups[2].Value
    if (($passed -ne $total) -or ($total -le 0)) {
        throw "Validation was incomplete for $architecture."
    }

    $rows.Add([pscustomobject][ordered]@{
        platform = "windows-$architecture"
        passed = $passed
        total = $total
        result = 'PASS'
        log = [IO.Path]::GetFileName($log)
    })
}

$rows | Export-Csv (Join-Path $OutputDirectory 'validation-summary.csv') -NoTypeInformation
Write-Host "PASS: $($rows.Count) Windows Immix allocation validation targets"
Write-Host "Output: $OutputDirectory"
