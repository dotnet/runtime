# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$BaselineRuntimeRoot,
    [Parameter(Mandatory)]
    [string]$ChangedRuntimeRoot,
    [ValidateSet('Workstation', 'Server')]
    [string]$GcMode,
    [string]$RepositoryRoot,
    [string]$OutputDirectory,
    [ValidateRange(1, 10)]
    [int]$LaunchCount = 3,
    [ValidateRange(1, 100)]
    [int]$WarmupCount = 8,
    [ValidateRange(1, 100)]
    [int]$IterationCount = 20,
    [switch]$SkipControlThresholds
)

$ErrorActionPreference = 'Stop'
$scriptRoot = $PSScriptRoot
if (-not $RepositoryRoot) {
    $RepositoryRoot = (Resolve-Path (Join-Path $scriptRoot '..\..\..\..')).ProviderPath
} else {
    $RepositoryRoot = (Resolve-Path -LiteralPath $RepositoryRoot).ProviderPath
}
$BaselineRuntimeRoot = (Resolve-Path -LiteralPath $BaselineRuntimeRoot).ProviderPath
$ChangedRuntimeRoot = (Resolve-Path -LiteralPath $ChangedRuntimeRoot).ProviderPath
if (-not $OutputDirectory) {
    $OutputDirectory = Join-Path $RepositoryRoot (
        'artifacts\P2.4\default-allocation\' +
        $GcMode.ToLowerInvariant() + '-' +
        [guid]::NewGuid().ToString('N'))
} else {
    $OutputDirectory = $PSCmdlet.GetUnresolvedProviderPathFromPSPath($OutputDirectory)
}
if (Test-Path -LiteralPath $OutputDirectory) {
    throw "OutputDirectory already exists: '$OutputDirectory'."
}

$dotnet = Join-Path $RepositoryRoot '.dotnet\dotnet.exe'
$project = Join-Path $scriptRoot (
    'immix-allocation-benchmark\immix-allocation-benchmark.csproj')
$evidenceManifestPath = Join-Path $scriptRoot 'evidence-manifest.json'
$baseline = Join-Path $BaselineRuntimeRoot 'corerun.exe'
$baselineCoreClr = Join-Path $BaselineRuntimeRoot 'coreclr.dll'
$baselineCoreLib = Join-Path $BaselineRuntimeRoot 'System.Private.CoreLib.dll'
$changed = Join-Path $ChangedRuntimeRoot 'corerun.exe'
$changedCoreClr = Join-Path $ChangedRuntimeRoot 'coreclr.dll'
$changedCoreLib = Join-Path $ChangedRuntimeRoot 'System.Private.CoreLib.dll'
foreach ($path in @(
        $dotnet,
        $project,
        $evidenceManifestPath,
        $baseline,
        $baselineCoreClr,
        $baselineCoreLib,
        $changed,
        $changedCoreClr,
        $changedCoreLib)) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Default allocation benchmark input is missing: '$path'."
    }
}

New-Item -ItemType Directory -Path $OutputDirectory | Out-Null
$controlRoot = Join-Path $OutputDirectory 'baseline-control-runtime'
Copy-Item -LiteralPath $BaselineRuntimeRoot -Destination $controlRoot -Recurse
$control = Join-Path $controlRoot 'corerun.exe'
$controlCoreClr = Join-Path $controlRoot 'coreclr.dll'
$controlCoreLib = Join-Path $controlRoot 'System.Private.CoreLib.dll'
if (-not (
        (Test-Path -LiteralPath $control -PathType Leaf) -and
        (Test-Path -LiteralPath $controlCoreClr -PathType Leaf) -and
        (Test-Path -LiteralPath $controlCoreLib -PathType Leaf))) {
    throw 'Baseline control runtime copy is incomplete.'
}

$buildLog = Join-Path $OutputDirectory 'managed-build.log'
$savedErrorActionPreference = $ErrorActionPreference
try {
    $ErrorActionPreference = 'Continue'
    & $dotnet build $project -c Release --nologo *> $buildLog
    $buildExitCode = $LASTEXITCODE
} finally {
    $ErrorActionPreference = $savedErrorActionPreference
}
if ($buildExitCode -ne 0) {
    throw "Managed benchmark build failed. See '$buildLog'."
}

$evidenceManifest = Get-Content -LiteralPath $evidenceManifestPath -Raw |
    ConvertFrom-Json
$ratioMinimum = [double]$evidenceManifest.defaultAllocation.ratioMinimum
$ratioMaximum = [double]$evidenceManifest.defaultAllocation.ratioMaximum

$savedGcServer = $env:DOTNET_gcServer
$savedGcPath = $env:DOTNET_GCPath
$savedLocation = Get-Location
$runtimeRows = [Collections.Generic.List[object]]::new()
$runtimeCases = @(
    [pscustomobject]@{ Role = 'baseline'; CoreRun = $baseline },
    [pscustomobject]@{ Role = 'control'; CoreRun = $control },
    [pscustomobject]@{ Role = 'changed'; CoreRun = $changed })
try {
    Remove-Item Env:\DOTNET_GCPath -ErrorAction SilentlyContinue
    $env:DOTNET_gcServer = if ($GcMode -eq 'Server') { '1' } else { '0' }
    Set-Location -LiteralPath (Split-Path -Parent $project)
    foreach ($runtimeCase in $runtimeCases) {
        $results = Join-Path $OutputDirectory (
            'BenchmarkDotNet-' + $runtimeCase.Role)
        $benchmarkLog = Join-Path $OutputDirectory (
            $runtimeCase.Role + '-benchmark.log')
        try {
            $ErrorActionPreference = 'Continue'
            & $dotnet run --no-build -c Release --project $project -- `
                --filter '*DefaultAllocationBenchmarks*' `
                --launchCount $LaunchCount `
                --warmupCount $WarmupCount `
                --iterationCount $IterationCount `
                --coreRun $runtimeCase.CoreRun `
                --artifacts $results *> $benchmarkLog
            $benchmarkExitCode = $LASTEXITCODE
        } finally {
            $ErrorActionPreference = $savedErrorActionPreference
        }
        if ($benchmarkExitCode -ne 0) {
            throw "Default allocation benchmark failed for " +
                "$($runtimeCase.Role). See '$benchmarkLog'."
        }

        $report = @(
            Get-ChildItem -LiteralPath (Join-Path $results 'results') `
                -Filter '*-report.csv' -File -Recurse)
        if ($report.Count -ne 1) {
            throw "Expected one $($runtimeCase.Role) benchmark report, " +
                "found $($report.Count)."
        }
        $caseRows = @(Import-Csv $report[0].FullName)
        if (($caseRows.Count -ne 1) -or (-not $caseRows[0].Mean)) {
            throw "The $($runtimeCase.Role) allocation report is incomplete."
        }
        $runtimeRows.Add(
            ($caseRows[0] | Select-Object `
                @{Name = 'RuntimeRole'; Expression = {
                    $runtimeCase.Role
                }}, *))
    }
} finally {
    Set-Location -LiteralPath $savedLocation.ProviderPath
    $env:DOTNET_gcServer = $savedGcServer
    $env:DOTNET_GCPath = $savedGcPath
}

function Convert-ToNanoseconds([string]$value) {
    $value = $value.Replace(',', '')
    $microseconds = ([char]0x03BC) + 's'
    $pattern = '^([\d.]+)\s*(ns|' + [regex]::Escape($microseconds) + '|ms|s)$'
    $match = [regex]::Match($value, $pattern)
    if (-not $match.Success) {
        throw "Unsupported benchmark time '$value'."
    }
    $number = [double]$match.Groups[1].Value
    switch ($match.Groups[2].Value) {
        'ns' { return $number }
        $microseconds { return $number * 1000 }
        'ms' { return $number * 1000 * 1000 }
        's' { return $number * 1000 * 1000 * 1000 }
    }
}

$baselineRow = $runtimeRows | Where-Object RuntimeRole -eq 'baseline'
$controlRow = $runtimeRows | Where-Object RuntimeRole -eq 'control'
$changedRow = $runtimeRows | Where-Object RuntimeRole -eq 'changed'
$baselineMean = Convert-ToNanoseconds $baselineRow.Mean
$controlRatio = (Convert-ToNanoseconds $controlRow.Mean) / $baselineMean
$changedRatio = (Convert-ToNanoseconds $changedRow.Mean) / $baselineMean
if (-not $SkipControlThresholds -and
    (($controlRatio -lt $ratioMinimum) -or
     ($controlRatio -gt $ratioMaximum))) {
    throw "Default allocation A/A ratio $controlRatio is outside " +
        "[$ratioMinimum,$ratioMaximum]."
}
if (-not $SkipControlThresholds -and
    (($changedRatio -lt $ratioMinimum) -or
     ($changedRatio -gt $ratioMaximum))) {
    throw "Default allocation changed ratio $changedRatio is outside " +
        "[$ratioMinimum,$ratioMaximum]."
}

$runtimeRows | Export-Csv (
   Join-Path $OutputDirectory 'default-allocation-raw.csv') `
   -NoTypeInformation
[pscustomobject][ordered]@{
    gc_mode = $GcMode
    launch_count = $LaunchCount
    warmup_count = $WarmupCount
    iteration_count = $IterationCount
    aa_ratio = $controlRatio
    changed_ratio = $changedRatio
    baseline_corerun_sha256 = (Get-FileHash $baseline -Algorithm SHA256).Hash
    control_corerun_sha256 = (Get-FileHash $control -Algorithm SHA256).Hash
    changed_corerun_sha256 = (Get-FileHash $changed -Algorithm SHA256).Hash
    baseline_coreclr_sha256 = (Get-FileHash $baselineCoreClr -Algorithm SHA256).Hash
    control_coreclr_sha256 = (Get-FileHash $controlCoreClr -Algorithm SHA256).Hash
    changed_coreclr_sha256 = (Get-FileHash $changedCoreClr -Algorithm SHA256).Hash
    baseline_corelib_sha256 = (Get-FileHash $baselineCoreLib -Algorithm SHA256).Hash
    control_corelib_sha256 = (Get-FileHash $controlCoreLib -Algorithm SHA256).Hash
    changed_corelib_sha256 = (Get-FileHash $changedCoreLib -Algorithm SHA256).Hash
    controls_enforced = -not $SkipControlThresholds
    result = 'PASS'
} | Export-Csv (
    Join-Path $OutputDirectory 'default-allocation-controls.csv') `
    -NoTypeInformation

Write-Host "PASS: $GcMode default allocation A/A and changed-runtime controls"
Write-Host "Output: $OutputDirectory"
