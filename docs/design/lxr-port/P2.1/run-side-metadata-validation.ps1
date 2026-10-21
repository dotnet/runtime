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
$RepositoryRoot = (Resolve-Path -LiteralPath $RepositoryRoot).ProviderPath
if (-not $OutputDirectory) {
    $OutputDirectory = Join-Path $RepositoryRoot (
        'artifacts\P2.1\validation\' + [guid]::NewGuid().ToString('N'))
} else {
    $OutputDirectory = $PSCmdlet.GetUnresolvedProviderPathFromPSPath($OutputDirectory)
}

$gcRoot = Join-Path $RepositoryRoot 'src\coreclr\gc'
$nativeRoot = Join-Path $RepositoryRoot 'src\native'
$initializeVisualStudio = Join-Path $RepositoryRoot 'eng\native\init-vs-env.cmd'
$validation = Join-Path $scriptRoot 'side-metadata-validation.cpp'
$platform = Join-Path $scriptRoot 'side-metadata-test-platform.cpp'
$implementation = Join-Path $gcRoot 'side_metadata.cpp'
$summary = [Collections.Generic.List[object]]::new()
$attempts = [Collections.Generic.List[object]]::new()

New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null

foreach ($architecture in @('x64', 'x86')) {
    $target = Join-Path $OutputDirectory $architecture
    New-Item -ItemType Directory -Path $target -Force | Out-Null
    $executable = Join-Path $target 'side-metadata-validation.exe'
    $log = Join-Path $target 'run.log'
    $defines = if ($architecture -eq 'x64') {
        '/DTARGET_AMD64 /DHOST_AMD64 /DTARGET_64BIT /DHOST_64BIT'
    } else {
        '/DTARGET_X86 /DHOST_X86'
    }
    $command = @(
        "call `"$initializeVisualStudio`" $architecture",
        "cd /d `"$target`"",
        "cl /nologo /std:c++17 /EHsc /W4 /WX /wd4100 /wd4324 /DBUILD_AS_STANDALONE /DTARGET_WINDOWS /DHOST_WINDOWS /DWIN32 $defines /I`"$gcRoot`" /I`"$gcRoot\env`" /I`"$nativeRoot`" /I`"$nativeRoot\inc`" `"$validation`" `"$platform`" `"$implementation`" /Fe:`"$executable`"",
        "`"$executable`""
    ) -join ' && '

    $started = [DateTimeOffset]::UtcNow
    $savedErrorActionPreference = $ErrorActionPreference
    try {
        $ErrorActionPreference = 'Continue'
        & $env:ComSpec /d /s /c $command *> $log
        $exitCode = $LASTEXITCODE
    } finally {
        $ErrorActionPreference = $savedErrorActionPreference
    }
    $ended = [DateTimeOffset]::UtcNow
    $attempts.Add([pscustomobject][ordered]@{
        architecture = $architecture
        started_utc = $started.ToString('O')
        ended_utc = $ended.ToString('O')
        exit_code = $exitCode
        command = $command
        log = $log
    })
    if ($exitCode -ne 0) {
        throw "Side-metadata validation failed for $architecture. See $log."
    }

    $match = Select-String -LiteralPath $log -Pattern '(\d+)/(\d+) side metadata checks passed'
    if ($match.Count -ne 1) {
        throw "Side-metadata validation result is missing for $architecture."
    }
    $passed = [int]$match.Matches[0].Groups[1].Value
    $total = [int]$match.Matches[0].Groups[2].Value
    if (($passed -ne $total) -or ($total -le 0)) {
        throw "Side-metadata validation was incomplete for $architecture."
    }

    $summary.Add([pscustomobject][ordered]@{
        platform = "windows-$architecture"
        passed = $passed
        total = $total
        result = 'PASS'
        log = $log
    })
}

$summary | Export-Csv (Join-Path $OutputDirectory 'validation-summary.csv') -NoTypeInformation
$attempts | Export-Csv (Join-Path $OutputDirectory 'attempts.csv') -NoTypeInformation
Write-Host "PASS: $($summary.Count) Windows side-metadata validation targets"
Write-Host "Output: $OutputDirectory"
