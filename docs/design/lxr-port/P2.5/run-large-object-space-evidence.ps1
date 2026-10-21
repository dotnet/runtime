# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$SourceCommit,
    [Parameter(Mandatory)]
    [string]$RuntimeRoot,
    [string]$RepositoryRoot
)

$ErrorActionPreference = 'Stop'
$scriptRoot = $PSScriptRoot
if (-not $RepositoryRoot) {
    $RepositoryRoot = (Resolve-Path (Join-Path $scriptRoot '..\..\..\..')).ProviderPath
} else {
    $RepositoryRoot = (Resolve-Path -LiteralPath $RepositoryRoot).ProviderPath
}
if ($SourceCommit -notmatch '^[0-9a-f]{40}$') {
    throw 'SourceCommit must be a full SHA.'
}
if ((git -C $RepositoryRoot rev-parse HEAD).Trim() -ne $SourceCommit) {
    throw 'Evidence must run with the frozen source commit at HEAD.'
}
if (@(git -C $RepositoryRoot -c core.longpaths=true status --porcelain=v1).Count -ne 0) {
    throw 'Evidence requires a clean source worktree.'
}

$raw = Join-Path $scriptRoot 'raw'
if (Test-Path $raw) {
    throw 'Raw evidence already exists.'
}
New-Item -ItemType Directory -Path $raw | Out-Null

$scratch = "C:\p25e-$([guid]::NewGuid().ToString('N').Substring(0,8))"
$archive = "$scratch\tree.tar"
$tree = "$scratch\tree"
New-Item -ItemType Directory -Path $tree -Force | Out-Null
try {
    git -C $RepositoryRoot `
        -c core.autocrlf=false `
        -c core.eol=lf `
        archive --format=tar --output=$archive $SourceCommit
    Add-Type -AssemblyName System.Formats.Tar
    [System.Formats.Tar.TarFile]::ExtractToDirectory($archive, $tree, $false)

    $sourcePaths = @(
        Get-Content (Join-Path $tree 'docs\design\lxr-port\P2.5\source-manifest.txt') |
            Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
    )
    $identities = foreach ($relative in $sourcePaths) {
        [pscustomobject][ordered]@{
            path = $relative
            sha256 = (Get-FileHash (Join-Path $tree $relative) -Algorithm SHA256).Hash
        }
    }
    $identities | Export-Csv (Join-Path $raw 'source-identities.csv') -NoTypeInformation
    Set-Content (Join-Path $raw 'source-commit.txt') $SourceCommit

    $windowsValidation = Join-Path $scratch 'windows-validation'
    & (Join-Path $tree 'docs\design\lxr-port\P2.5\run-large-object-space-validation.ps1') `
        -RepositoryRoot $tree `
        -OutputDirectory $windowsValidation

    $normalized = $tree.Replace('\', '/')
    $linuxTree = '/mnt/' + $normalized.Substring(0,1).ToLowerInvariant() + '/' + $normalized.Substring(3)
    $linuxValidation = "$linuxTree-linux-validation"
    & wsl.exe --cd /root bash `
        "$linuxTree/docs/design/lxr-port/P2.5/run-large-object-space-validation.sh" `
        $linuxTree `
        $linuxValidation
    if ($LASTEXITCODE -ne 0) {
        throw 'Linux validation failed.'
    }
    @(
        Import-Csv (Join-Path $windowsValidation 'validation-summary.csv')
        Import-Csv ($tree + '-linux-validation\validation-summary.csv')
    ) | Export-Csv (Join-Path $raw 'validation-summary.csv') -NoTypeInformation

    $runtimeOutput = Join-Path $scratch 'runtime-smoke'
    & (Join-Path $tree 'docs\design\lxr-port\P2.5\run-large-object-space-runtime-smoke.ps1') `
        -RepositoryRoot $RepositoryRoot `
        -RuntimeRoot $RuntimeRoot `
        -OutputDirectory $runtimeOutput
    Copy-Item (Join-Path $runtimeOutput 'runtime-smoke-summary.csv') $raw

    $benchmarkOutput = Join-Path $scratch 'benchmark'
    & (Join-Path $tree 'docs\design\lxr-port\P2.5\run-large-object-space-benchmark.ps1') `
        -RepositoryRoot $RepositoryRoot `
        -OutputDirectory $benchmarkOutput
    Copy-Item (Join-Path $benchmarkOutput 'benchmark-raw.csv') $raw
    Copy-Item (Join-Path $benchmarkOutput 'benchmark-identity.csv') $raw
    Copy-Item (Join-Path $benchmarkOutput 'benchmark-controls.csv') $raw

    $pressure = [Collections.Generic.List[object]]::new()
    foreach ($run in @(
        'p25.preedit.large-object-pressure.sdk.a',
        'p25.preedit.large-object-pressure.sdk.b')) {
        $result = Join-Path $RepositoryRoot "artifacts\lxr-harness\runs\$run\results.json"
        $document = Get-Content $result -Raw | ConvertFrom-Json
        if (($document.schemaVersion -ne 2) -or
            ($document.checkpoints.Count -ne 1) -or
            ($document.checkpoints[0].results.Count -ne 2)) {
            throw "Large-object pressure result shape differs: $run"
        }
        $collectors = @($document.checkpoints[0].results.collector | Sort-Object)
        if (($collectors -join ';') -ne 'srv;wks') {
            throw "Large-object pressure collectors differ: $run"
        }
        foreach ($row in $document.checkpoints[0].results) {
            if (-not $row.valid -or
                ($row.invocations -ne 5) -or
                ($row.scenario -ne 'large-object-pressure') -or
                ($row.operationsPerSecond -le 0)) {
                throw "Large-object pressure result is invalid: $run/$($row.collector)"
            }
            $pressure.Add([pscustomobject][ordered]@{
                run = $run
                collector = $row.collector
                invocations = $row.invocations
                operations_per_second = $row.operationsPerSecond
                pause_p99_ms = $row.pauseP99Ms
                source_sha256 = (Get-FileHash $result -Algorithm SHA256).Hash
                result = 'PASS'
            })
        }
    }
    $pressure | Export-Csv (Join-Path $raw 'large-object-pressure-summary.csv') -NoTypeInformation

    $buildInputs = @(
        @{ name = 'baseline-exact-failure'; path = 'artifacts\P2.5\baseline\build.status'; exit = 1; result = 'EXPECTED-FAIL' }
        @{ name = 'baseline-audit-preserving-pdb-failure'; path = 'artifacts\P2.5\baseline\build-attempt-2.status'; exit = 1; result = 'EXPECTED-FAIL' }
        @{ name = 'baseline-r-drive-pass'; path = 'artifacts\P2.5\baseline\build-attempt-3.status'; exit = 0; result = 'PASS' }
        @{ name = 'default-windows-x64'; path = 'artifacts\P2.5\builds\default-windows-x64-final.status'; exit = 0; result = 'PASS' }
    )
    $buildRows = foreach ($input in $buildInputs) {
        $statusPath = Join-Path $RepositoryRoot $input.path
        $exitLines = @(Get-Content $statusPath | Where-Object { $_ -like 'exit=*' })
        if (($exitLines.Count -ne 1) -or
            ($exitLines[0].Substring('exit='.Length) -ne "$($input.exit)")) {
            throw "Build status differs: $($input.name)"
        }
        [pscustomobject][ordered]@{
            name = $input.name
            status_sha256 = (Get-FileHash $statusPath -Algorithm SHA256).Hash
            result = $input.result
        }
    }
    foreach ($component in @(
        @{ name = 'test-enabled-windows-x64'; path = 'artifacts\P2.5\builds\windows-x64-runtime-review-fixed-2.log' }
        @{ name = 'windows-x86-runtime'; path = 'artifacts\P2.5\builds\windows-x86-runtime-final.log' }
        @{ name = 'windows-arm64-runtime'; path = 'artifacts\P2.5\builds\windows-arm64-runtime-final.log' }
        @{ name = 'windows-x64-combined-runtime'; path = 'artifacts\P2.5\builds\windows-x64-combined-runtime-final.log' })) {
        $logPath = Join-Path $RepositoryRoot $component.path
        if (-not ((Get-Content $logPath -Raw).Contains('BUILD: Build succeeded.'))) {
            throw "Targeted build did not pass: $($component.name)"
        }
        $buildRows += [pscustomobject][ordered]@{
            name = $component.name
            status_sha256 = (Get-FileHash $logPath -Algorithm SHA256).Hash
            result = 'PASS'
        }
    }
    $buildRows | Export-Csv (Join-Path $raw 'build-summary.csv') -NoTypeInformation

    $defaultBuildLog = Join-Path $RepositoryRoot 'artifacts\P2.5\builds\default-windows-x64-final.log'
    $defaultBuildText = Get-Content $defaultBuildLog -Raw
    if ($defaultBuildText.Contains('large_object_space.cpp')) {
        throw 'Default build compiled the P2.5 product source.'
    }
    $defaultRows = foreach ($binaryName in @('coreclr.dll', 'clrgc.dll')) {
        $binary = Join-Path $RepositoryRoot "artifacts\bin\coreclr\windows.x64.Debug\$binaryName"
        $bytes = [IO.File]::ReadAllBytes($binary)
        $text = [Text.Encoding]::ASCII.GetString($bytes)
        if ($text.Contains('GC_LargeObjectSpaceTest_Run')) {
            throw "Default binary exports or contains the P2.5 hook: $binaryName"
        }
        [pscustomobject][ordered]@{
            binary = $binaryName
            p25_source_compiled = 'False'
            p25_hook_present = 'False'
            sha256 = (Get-FileHash $binary -Algorithm SHA256).Hash
            result = 'PASS'
        }
    }
    $defaultRows | Export-Csv (Join-Path $raw 'default-off-summary.csv') -NoTypeInformation
} finally {
    if (Test-Path $scratch) {
        Remove-Item $scratch -Recurse -Force
    }
}

Write-Host 'PASS: P2.5 evidence captured from frozen source'
