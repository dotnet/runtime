# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$RuntimeRoot,
    [string]$RepositoryRoot,
    [string]$OutputDirectory,
    [ValidateRange(1, 10)]
    [int]$LaunchCount = 3,
    [ValidateRange(1, 100)]
    [int]$WarmupCount = 8,
    [ValidateRange(1, 100)]
    [int]$IterationCount = 20
)

$ErrorActionPreference = 'Stop'
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
if (-not $RepositoryRoot) {
    $RepositoryRoot = (Resolve-Path (Join-Path $scriptRoot '..\..\..\..')).Path
}
$RepositoryRoot = (Resolve-Path -LiteralPath $RepositoryRoot).ProviderPath
$RuntimeRoot = (Resolve-Path -LiteralPath $RuntimeRoot).ProviderPath
if (-not $OutputDirectory) {
    $OutputDirectory = Join-Path $RepositoryRoot (
        'artifacts\P2.1\full-evidence\' + [guid]::NewGuid().ToString('N'))
} else {
    $OutputDirectory = $PSCmdlet.GetUnresolvedProviderPathFromPSPath($OutputDirectory)
}

$requiredCoreRootFiles = @(
    'corerun.exe',
    'coreclr.dll',
    'clrgc.dll',
    'clrjit.dll',
    'System.Private.CoreLib.dll',
    'System.Runtime.dll',
    'System.Console.dll'
)
foreach ($file in $requiredCoreRootFiles) {
    $path = Join-Path $RuntimeRoot $file
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw (
            "RuntimeRoot must be a complete CoreRoot; missing '$file' in " +
            "'$RuntimeRoot'.")
    }
}

New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$steps = [Collections.Generic.List[object]]::new()

function Get-PhysicalWindowsPath([string]$Path) {
    $physical = $Path
    $drive = [IO.Path]::GetPathRoot($physical).TrimEnd('\')
    $mapping = @(
        & $env:ComSpec /d /c subst |
            Where-Object { $_.StartsWith($drive, [StringComparison]::OrdinalIgnoreCase) })
    if ($mapping.Count -eq 1) {
        $arrow = $mapping[0].IndexOf('=>')
        $target = $mapping[0].Substring($arrow + 2).Trim().TrimEnd('\')
        $physical = $target + $physical.Substring($drive.Length)
    }
    return $physical
}

function Convert-ToWslPath([string]$Path) {
    $physical = Get-PhysicalWindowsPath $Path
    $driveLetter = $physical.Substring(0, 1).ToLowerInvariant()
    return '/mnt/' + $driveLetter + $physical.Substring(2).Replace('\', '/')
}

function Invoke-Step([string]$name, [scriptblock]$body) {
    $started = [DateTimeOffset]::UtcNow
    & $body
    if ($LASTEXITCODE -ne 0) {
        throw "Evidence step failed: $name"
    }
    $steps.Add([pscustomobject][ordered]@{
        name = $name
        started_utc = $started.ToString('O')
        ended_utc = [DateTimeOffset]::UtcNow.ToString('O')
        result = 'PASS'
    })
}

Invoke-Step 'windows-validation' {
    & (Join-Path $scriptRoot 'run-side-metadata-validation.ps1') `
        -RepositoryRoot $RepositoryRoot `
        -OutputDirectory (Join-Path $OutputDirectory 'windows-validation')
}

Invoke-Step 'linux-validation' {
    $linuxRoot = Convert-ToWslPath $RepositoryRoot
    $windowsLinuxOutput = Join-Path $OutputDirectory 'linux-validation'
    $linuxOutput = Convert-ToWslPath $windowsLinuxOutput
    $savedErrorActionPreference = $ErrorActionPreference
    try {
        $ErrorActionPreference = 'Continue'
        & wsl.exe bash -lc (
            "cd '$linuxRoot' && " +
            "bash docs/design/lxr-port/P2.1/run-side-metadata-validation.sh " +
            "'$linuxRoot' '$linuxOutput'")
        $linuxExitCode = $LASTEXITCODE
    } finally {
        $ErrorActionPreference = $savedErrorActionPreference
    }
    if ($linuxExitCode -ne 0) {
        throw "Linux validation failed with exit code $linuxExitCode."
    }
}

Invoke-Step 'runtime-smoke' {
    & (Join-Path $scriptRoot 'run-side-metadata-runtime-smoke.ps1') `
        -RepositoryRoot $RepositoryRoot `
        -RuntimeRoot $RuntimeRoot `
        -OutputDirectory (Join-Path $OutputDirectory 'runtime-smoke')
}

Invoke-Step 'benchmark' {
    & (Join-Path $scriptRoot 'run-side-metadata-benchmark.ps1') `
        -RepositoryRoot $RepositoryRoot `
        -OutputDirectory (Join-Path $OutputDirectory 'benchmark') `
        -LaunchCount $LaunchCount `
        -WarmupCount $WarmupCount `
        -IterationCount $IterationCount
}

$steps | Export-Csv (Join-Path $OutputDirectory 'full-evidence-summary.csv') -NoTypeInformation
[pscustomobject][ordered]@{
    commit = (git -C $RepositoryRoot rev-parse HEAD).Trim()
    repository_root = $RepositoryRoot
    runtime_root = (Resolve-Path $RuntimeRoot).Path
    output = (Resolve-Path $OutputDirectory).Path
    result = 'PASS'
} | ConvertTo-Json | Set-Content (Join-Path $OutputDirectory 'identity.json')
Write-Host "PASS: full P2.1 evidence path ($($steps.Count) steps)"
Write-Host "Output: $OutputDirectory"
