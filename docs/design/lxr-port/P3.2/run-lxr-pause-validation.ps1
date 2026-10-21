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
    $OutputDirectory = Join-Path $RepositoryRoot 'artifacts\P3.2\validation\windows-x64'
} else {
    $OutputDirectory = $PSCmdlet.GetUnresolvedProviderPathFromPSPath($OutputDirectory)
}
if (Test-Path -LiteralPath $OutputDirectory) {
    throw "OutputDirectory already exists: '$OutputDirectory'."
}

$vcvars = 'C:\Program Files\Microsoft Visual Studio\18\IntPreview\VC\Auxiliary\Build\vcvars64.bat'
if (-not (Test-Path -LiteralPath $vcvars -PathType Leaf)) {
    throw "Visual C++ environment is missing: '$vcvars'."
}

New-Item -ItemType Directory -Path $OutputDirectory | Out-Null
$sources = @(
    (Join-Path $RepositoryRoot 'src\coreclr\gc\lxr_work_packet.cpp'),
    (Join-Path $RepositoryRoot 'src\coreclr\gc\lxr_work_scheduler.cpp'),
    (Join-Path $RepositoryRoot 'src\coreclr\gc\lxr_pause_schedule.cpp'),
    (Join-Path $RepositoryRoot 'src\coreclr\gc\lxr_pause_coordinator.cpp'),
    (Join-Path $scriptRoot 'lxr-pause-validation.cpp')
)
$executable = Join-Path $OutputDirectory 'lxr-pause-validation.exe'
$quotedSources = ($sources | ForEach-Object { "`"$_`"" }) -join ' '
$compile = @(
    'cl.exe /nologo /std:c++17 /EHsc /W4 /WX /O2',
    $quotedSources,
    "/Fe:`"$executable`""
) -join ' '
& $env:ComSpec /d /s /c "call `"$vcvars`" >nul && $compile" `
    *> (Join-Path $OutputDirectory 'build.log')
if ($LASTEXITCODE -ne 0) {
    throw "P3.2 validator build failed with exit code $LASTEXITCODE."
}

& $executable *> (Join-Path $OutputDirectory 'validation.log')
$exitCode = $LASTEXITCODE
$output = Get-Content -LiteralPath (Join-Path $OutputDirectory 'validation.log') -Raw
if ($exitCode -ne 0 -or $output -notmatch 'PASS: (\d+)/(\d+) P3\.2 pause checks') {
    throw "P3.2 validator failed with exit code $exitCode."
}

[pscustomobject][ordered]@{
    platform = 'windows-x64'
    passed = [int]$Matches[1]
    total = [int]$Matches[2]
    exit_code = $exitCode
    result = 'PASS'
} | Export-Csv (Join-Path $OutputDirectory 'validation-summary.csv') -NoTypeInformation

Write-Host $output.Trim()
