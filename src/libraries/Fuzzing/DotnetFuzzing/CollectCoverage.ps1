# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

<#
.SYNOPSIS
Collects source coverage by replaying a fuzzer's saved corpus.
.EXAMPLE
.\CollectCoverage.ps1 KerberosPacLogonInfoFuzzer -DeploymentDirectory ..\..\..\..\artifacts\pac-fuzz-deployment
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory, Position = 0)]
    [ValidatePattern('^[A-Za-z0-9_]+$')]
    [string] $FuzzerName,

    [string] $DeploymentDirectory = "$PSScriptRoot\deployment",

    [string] $BuildDirectory = "$PSScriptRoot\..\..\..\..\artifacts\bin\DotnetFuzzing\Debug\net10.0\win-x64",

    [string[]] $CorpusDirectories,

    [string] $OutputDirectory = "$PSScriptRoot\..\..\..\..\artifacts\fuzz-coverage\$FuzzerName"
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

foreach ($tool in @('dotnet-coverage', 'reportgenerator')) {
    if (-not (Get-Command $tool -ErrorAction SilentlyContinue)) {
        throw "Missing $tool. Install dotnet-coverage and dotnet-reportgenerator-globaltool with dotnet tool install --global."
    }
}

$BuildDirectory = (Resolve-Path $BuildDirectory).Path
$targetDirectory = Join-Path (Resolve-Path $DeploymentDirectory).Path $FuzzerName
$config = Get-Content (Join-Path $targetDirectory 'OneFuzzConfig.json') -Raw | ConvertFrom-Json
$assemblies = @($config.Entries[0].Fuzzer.FuzzingTargetBinaries)
if ($assemblies.Count -eq 0) {
    throw "No instrumentation targets found for $FuzzerName."
}

if (-not $CorpusDirectories) {
    $CorpusDirectories = @(
        Join-Path $targetDirectory 'corpus'
    ) | Where-Object { Test-Path $_ -PathType Container }
}
if ($CorpusDirectories.Count -eq 0) {
    throw 'No corpus directories found. Save fuzz inputs first or specify -CorpusDirectories.'
}
$corpora = @($CorpusDirectories | ForEach-Object {
        $path = (Resolve-Path $_).Path
        if (-not (Test-Path $path -PathType Container)) {
            throw "Not a corpus directory: $path"
        }
        if (@(Get-ChildItem $path -File).Count -eq 0) {
            throw "Corpus directory is empty: $path"
        }
        $path
    })

New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$OutputDirectory = (Resolve-Path $OutputDirectory).Path
$includeFiles = @($assemblies | ForEach-Object {
        $path = Join-Path $BuildDirectory $_
        if (-not (Test-Path $path -PathType Leaf)) {
            throw "Target assembly missing from uninstrumented build: $path"
        }
        $path
    }) -join ';'

$reports = @()
for ($i = 0; $i -lt $corpora.Count; $i++) {
    $report = Join-Path $OutputDirectory "corpus-$i.cobertura.xml"
    & dotnet-coverage collect --include-files $includeFiles `
        --output $report --output-format cobertura `
        "$BuildDirectory\DotnetFuzzing.exe" $FuzzerName $corpora[$i]
    if ($LASTEXITCODE -ne 0) {
        throw "Coverage collection failed for $($corpora[$i]) (exit $LASTEXITCODE)."
    }
    $reports += $report
}

$assemblyFilters = ($assemblies | ForEach-Object { '+' + [IO.Path]::GetFileNameWithoutExtension($_) }) -join ';'
$repoRoot = (Resolve-Path "$PSScriptRoot\..\..\..\..").Path
& reportgenerator "-reports:$($reports -join ';')" `
    "-targetdir:$OutputDirectory\html" "-reporttypes:Html;TextSummary" `
    "-assemblyfilters:$assemblyFilters" "-sourcedirs:$repoRoot\src"
if ($LASTEXITCODE -ne 0) {
    throw "Report generation failed (exit $LASTEXITCODE)."
}
Write-Host "Coverage report: $OutputDirectory\html\index.html"
