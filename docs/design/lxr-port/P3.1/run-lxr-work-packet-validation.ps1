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
    $OutputDirectory = Join-Path $RepositoryRoot 'artifacts\P3.1\validation\windows-x64'
} else {
    $OutputDirectory = $PSCmdlet.GetUnresolvedProviderPathFromPSPath($OutputDirectory)
}
if (Test-Path -LiteralPath $OutputDirectory) {
    throw "OutputDirectory already exists: '$OutputDirectory'."
}

$vcvars = 'C:\Program Files\Microsoft Visual Studio\18\IntPreview\VC\Auxiliary\Build\vcvars64.bat'
if (-not (Test-Path -LiteralPath $vcvars -PathType Leaf)) {
    throw "Visual C++ environment script not found: '$vcvars'."
}

New-Item -ItemType Directory -Path $OutputDirectory | Out-Null
$binary = Join-Path $OutputDirectory 'lxr-work-packet-validation.exe'
$buildLog = Join-Path $OutputDirectory 'build.log'
$runLog = Join-Path $OutputDirectory 'run.log'
$packetSource = Join-Path $RepositoryRoot 'src\coreclr\gc\lxr_work_packet.cpp'
$schedulerSource = Join-Path $RepositoryRoot 'src\coreclr\gc\lxr_work_scheduler.cpp'
$validationSource = Join-Path $scriptRoot 'lxr-work-packet-validation.cpp'

$compile = @(
    "cl.exe /nologo /std:c++17 /EHsc /O2",
    "`"$packetSource`"",
    "`"$schedulerSource`"",
    "`"$validationSource`"",
    "/Fe:`"$binary`""
) -join ' '
$command = "call `"$vcvars`" >nul && $compile"

& $env:ComSpec /d /s /c $command *> $buildLog
if ($LASTEXITCODE -ne 0) {
    throw "P3.1 Windows validation build failed with exit code $LASTEXITCODE."
}

& $binary *> $runLog
if ($LASTEXITCODE -ne 0) {
    throw "P3.1 Windows validation failed with exit code $LASTEXITCODE."
}

$match = [regex]::Match(
    (Get-Content -LiteralPath $runLog -Raw),
    'PASS: (?<passed>\d+)/(?<total>\d+) P3\.1 validation checks')
if (-not $match.Success -or $match.Groups['passed'].Value -ne $match.Groups['total'].Value) {
    throw 'P3.1 Windows validation success count is missing or incomplete.'
}

[pscustomobject][ordered]@{
    platform = 'windows-x64'
    passed = $match.Groups['passed'].Value
    total = $match.Groups['total'].Value
    result = 'PASS'
} | Export-Csv (Join-Path $OutputDirectory 'validation-summary.csv') -NoTypeInformation

Write-Host $match.Value
