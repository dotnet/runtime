# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$IlcPath,

    [Parameter(Mandatory)]
    [string]$ResponseFile,

    [Parameter(Mandatory)]
    [string]$BaselineAssembly,

    [Parameter(Mandatory)]
    [string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'

if ($env:OS -eq 'Windows_NT' -and ![IO.Path]::HasExtension($IlcPath)) {
    $IlcPath += '.exe'
}
$IlcPath = (Resolve-Path -LiteralPath $IlcPath).Path
$ResponseFile = (Resolve-Path -LiteralPath $ResponseFile).Path
$BaselineAssembly = (Resolve-Path -LiteralPath $BaselineAssembly).Path
$OutputDirectory = [IO.Path]::GetFullPath($OutputDirectory)
[IO.Directory]::CreateDirectory($OutputDirectory) | Out-Null

$workDirectory = Join-Path $OutputDirectory 'incremental-differential'
if (Test-Path -LiteralPath $workDirectory) {
    Remove-Item -LiteralPath $workDirectory -Recurse -Force
}
[IO.Directory]::CreateDirectory($workDirectory) | Out-Null

$updatedAssembly = Join-Path $workDirectory 'updated.dll'
$revertedAssembly = Join-Path $workDirectory 'reverted.dll'
$baselineObject = Join-Path $workDirectory 'baseline.obj'
$incrementalUpdatedObject = Join-Path $workDirectory 'incremental-updated.obj'
$incrementalRevertedObject = Join-Path $workDirectory 'incremental-reverted.obj'
$cleanUpdatedObject = Join-Path $workDirectory 'clean-updated.obj'
$baselineResponseFile = Join-Path $workDirectory 'baseline.rsp'
$cleanResponseFile = Join-Path $workDirectory 'clean-updated.rsp'
$logPath = Join-Path $workDirectory 'run.log'

$image = [IO.File]::ReadAllBytes($BaselineAssembly)
$oldConstant = [BitConverter]::GetBytes([int]0x61234567)
$newConstant = [BitConverter]::GetBytes([int]0x61234568)
$match = -1
for ($i = 0; $i -le $image.Length - $oldConstant.Length; $i++) {
    $equal = $true
    for ($j = 0; $j -lt $oldConstant.Length; $j++) {
        if ($image[$i + $j] -ne $oldConstant[$j]) {
            $equal = $false
            break
        }
    }
    if ($equal) {
        if ($match -ge 0) {
            throw 'The fixture constant is not unique in the baseline assembly.'
        }
        $match = $i
    }
}
if ($match -lt 0) {
    throw 'The fixture constant was not found in the baseline assembly.'
}
[Array]::Copy($newConstant, 0, $image, $match, $newConstant.Length)
[IO.File]::WriteAllBytes($updatedAssembly, $image)
[IO.File]::Copy($BaselineAssembly, $revertedAssembly)

function Get-NormalizedPath([string]$value) {
    $trimmed = $value.Trim().Trim('"')
    try {
        return [IO.Path]::GetFullPath($trimmed)
    }
    catch {
        return $null
    }
}

function New-ResponseFile(
    [string]$path,
    [string]$inputAssembly,
    [string]$outputObject) {
    $foundInput = $false
    $foundOutput = $false
    $lines = [Collections.Generic.List[string]]::new()
    foreach ($line in [IO.File]::ReadAllLines($ResponseFile)) {
        $trimmed = $line.Trim()
        if ($trimmed -match '(?i)^(-o|--out):') {
            $lines.Add("-o:$outputObject")
            $foundOutput = $true
            continue
        }
        if ($trimmed -match '(?i)^(--parallelism|-O|--Os|--Ot|--optimize|--optimize-space|--optimize-time|--debug|-g|--exportsfile|--export-dynamic-symbol|--export-unmanaged-entrypoints|--generateunmanagedentrypoints|--dgmllog|--scandgmllog|--guard|--ildump|--map|--mstat|--sourcelink|--metadatalog|--methodbodyfolding|--reachability|--resilient)(:.*)?$') {
            continue
        }
        if ((Get-NormalizedPath $trimmed) -eq $BaselineAssembly) {
            $lines.Add($inputAssembly)
            $foundInput = $true
            continue
        }
        $lines.Add($line)
    }
    if (!$foundInput -or !$foundOutput) {
        throw "Could not identify the input assembly and output object in '$ResponseFile'."
    }
    $lines.Add('--parallelism:1')
    $lines.Add('--noscan')
    $lines.Add('--nopreinitstatics')
    $lines.Add('--methodbodyfolding:none')
    [IO.File]::WriteAllLines($path, $lines)
}

New-ResponseFile $baselineResponseFile $BaselineAssembly $baselineObject
New-ResponseFile $cleanResponseFile $updatedAssembly $cleanUpdatedObject

function Write-LogLine([string]$value) {
    [IO.File]::AppendAllText($logPath, $value + [Environment]::NewLine)
    Write-Host $value
}

function Invoke-Ilc(
    [string]$label,
    [string]$response,
    [string]$updatedAssemblies,
    [string]$outputObjects) {
    $oldEnable = $env:DOTNET_ILC_INCREMENTAL
    $oldAssemblies = $env:DOTNET_ILC_INCREMENTAL_UPDATED_ASSEMBLIES
    $oldObjects = $env:DOTNET_ILC_INCREMENTAL_OUTPUT_OBJECTS
    try {
        if ([string]::IsNullOrEmpty($updatedAssemblies)) {
            Remove-Item Env:DOTNET_ILC_INCREMENTAL -ErrorAction SilentlyContinue
            Remove-Item Env:DOTNET_ILC_INCREMENTAL_UPDATED_ASSEMBLIES -ErrorAction SilentlyContinue
            Remove-Item Env:DOTNET_ILC_INCREMENTAL_OUTPUT_OBJECTS -ErrorAction SilentlyContinue
        }
        else {
            $env:DOTNET_ILC_INCREMENTAL = '1'
            $env:DOTNET_ILC_INCREMENTAL_UPDATED_ASSEMBLIES = $updatedAssemblies
            $env:DOTNET_ILC_INCREMENTAL_OUTPUT_OBJECTS = $outputObjects
        }

        $stopwatch = [Diagnostics.Stopwatch]::StartNew()
        $startInfo = New-Object Diagnostics.ProcessStartInfo
        $startInfo.FileName = $IlcPath
        $startInfo.Arguments = '@"' + $response + '"'
        $startInfo.WorkingDirectory = (Get-Location).Path
        $startInfo.UseShellExecute = $false
        $startInfo.CreateNoWindow = $true
        $startInfo.RedirectStandardOutput = $true
        $startInfo.RedirectStandardError = $true
        $process = New-Object Diagnostics.Process
        $process.StartInfo = $startInfo
        try {
            $null = $process.Start()
            $standardOutput = $process.StandardOutput.ReadToEndAsync()
            $standardError = $process.StandardError.ReadToEndAsync()
            $process.WaitForExit()
            [IO.File]::AppendAllText($logPath, $standardOutput.Result)
            [IO.File]::AppendAllText($logPath, $standardError.Result)
            $exitCode = $process.ExitCode
        }
        finally {
            $process.Dispose()
        }
        $stopwatch.Stop()
        Write-LogLine "$label milliseconds=$($stopwatch.Elapsed.TotalMilliseconds.ToString('F3', [Globalization.CultureInfo]::InvariantCulture)) exit=$exitCode"
        if ($exitCode -ne 0) {
            if ($exitCode -eq 85) {
                throw "$label requested an explicit clean fallback (exit 85). See '$logPath'."
            }
            throw "$label failed with exit code $exitCode. See '$logPath'."
        }
    }
    finally {
        $env:DOTNET_ILC_INCREMENTAL = $oldEnable
        $env:DOTNET_ILC_INCREMENTAL_UPDATED_ASSEMBLIES = $oldAssemblies
        $env:DOTNET_ILC_INCREMENTAL_OUTPUT_OBJECTS = $oldObjects
    }
}

function Get-Sha256([string]$path) {
    $stream = [IO.File]::OpenRead($path)
    try {
        $algorithm = [Security.Cryptography.SHA256]::Create()
        try {
            return [BitConverter]::ToString($algorithm.ComputeHash($stream)).Replace('-', '')
        }
        finally {
            $algorithm.Dispose()
        }
    }
    finally {
        $stream.Dispose()
    }
}

$separator = [IO.Path]::PathSeparator
Invoke-Ilc `
    'incremental-edit-revert' `
    $baselineResponseFile `
    "$updatedAssembly$separator$revertedAssembly" `
    "$incrementalUpdatedObject$separator$incrementalRevertedObject"
Invoke-Ilc 'clean-updated' $cleanResponseFile $null $null

$baselineHash = Get-Sha256 $baselineObject
$updatedHash = Get-Sha256 $incrementalUpdatedObject
$cleanHash = Get-Sha256 $cleanUpdatedObject
$revertedHash = Get-Sha256 $incrementalRevertedObject

if ($updatedHash -ne $cleanHash) {
    throw "Incremental and clean updated objects differ: $updatedHash != $cleanHash"
}
if ($revertedHash -ne $baselineHash) {
    throw "Incremental revert and baseline objects differ: $revertedHash != $baselineHash"
}

Write-LogLine "baseline_sha256=$baselineHash"
Write-LogLine "incremental_updated_sha256=$updatedHash"
Write-LogLine "clean_updated_sha256=$cleanHash"
Write-LogLine "incremental_reverted_sha256=$revertedHash"
Write-Host "Incremental differential comparison passed. Log: $logPath"
