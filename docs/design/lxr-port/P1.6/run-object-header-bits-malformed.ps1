# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

[CmdletBinding()]
param(
    [string]$RepositoryRoot
)

$ErrorActionPreference = 'Stop'
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path

if (-not $RepositoryRoot) {
    $RepositoryRoot = (Resolve-Path (Join-Path $scriptRoot '..\..\..\..')).Path
}

$coreRoot = Join-Path $RepositoryRoot (
    'artifacts\tests\coreclr\windows.x64.Checked\Tests\Core_Root')
$project = Join-Path $scriptRoot 'startup-smoke\startup-smoke.csproj'
$output = Join-Path $RepositoryRoot 'artifacts\p16-object-header-bits-startup'
$corerun = Join-Path $coreRoot 'corerun.exe'
$assembly = Join-Path $output 'startup-smoke.dll'
$results = [System.Collections.Generic.List[object]]::new()

& (Join-Path $RepositoryRoot 'dotnet.cmd') publish $project -c Release -o $output
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

function Invoke-Startup {
    param(
        [bool]$Enabled,
        [int]$Malformed,
        [bool]$ExpectSuccess
    )

    $savedEnabled = [Environment]::GetEnvironmentVariable('DOTNET_GCObjectHeaderBitsTest')
    $savedMalformed = [Environment]::GetEnvironmentVariable(
        'DOTNET_GCObjectHeaderBitsTestMalformed')
    try {
        [Environment]::SetEnvironmentVariable(
            'DOTNET_GCObjectHeaderBitsTest',
            $(if ($Enabled) { '1' } else { $null }))
        [Environment]::SetEnvironmentVariable(
            'DOTNET_GCObjectHeaderBitsTestMalformed',
            $(if ($Malformed -eq 0) { $null } else { $Malformed.ToString('X') }))

        $log = Join-Path $output "startup-$Enabled-$Malformed.log"
        & $corerun $assembly *> $log
        $exitCode = $LASTEXITCODE
        $passed = if ($ExpectSuccess) {
            $exitCode -eq 100
        } else {
            $exitCode -ne 100
        }

        $results.Add(
            [pscustomobject]@{
                enabled = $Enabled
                malformed = $Malformed
                expected = $(if ($ExpectSuccess) { 'start' } else { 'fail' })
                exit_code = $exitCode
                passed = $passed
                log = $log
            })

        if (-not $passed) {
            throw "Unexpected startup result for enabled=$Enabled malformed=$Malformed exit=$exitCode"
        }
    }
    finally {
        [Environment]::SetEnvironmentVariable(
            'DOTNET_GCObjectHeaderBitsTest',
            $savedEnabled)
        [Environment]::SetEnvironmentVariable(
            'DOTNET_GCObjectHeaderBitsTestMalformed',
            $savedMalformed)
    }
}

Invoke-Startup -Enabled $false -Malformed 0 -ExpectSuccess $true
Invoke-Startup -Enabled $true -Malformed 0 -ExpectSuccess $true
foreach ($malformed in 1..10) {
    Invoke-Startup -Enabled $true -Malformed $malformed -ExpectSuccess $false
}

$summary = Join-Path $output 'malformed-summary.csv'
$results | Export-Csv $summary -NoTypeInformation
Write-Host "$($results.Count)/$($results.Count) startup controls passed"
exit 0
