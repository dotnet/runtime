# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

[CmdletBinding()]
param(
    [string]$RepositoryRoot,
    [string]$OutputDirectory,
    [ValidatePattern('^[0-9a-f]{40}$')]
    [string]$PreviousRevision = '04c9b4c959193b8adb29924abd4c1da2336c1014'
)

$ErrorActionPreference = 'Stop'
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
if (-not $RepositoryRoot) {
    $RepositoryRoot = (Resolve-Path (Join-Path $scriptRoot '..\..\..\..')).Path
}
if (-not $OutputDirectory) {
    $OutputDirectory = Join-Path $RepositoryRoot (
        'artifacts\p15-nativeaot-reference-enumeration')
}

$source = Join-Path $scriptRoot 'nativeaot-reference-enumeration-validation.cpp'
$header = Join-Path $RepositoryRoot 'src\coreclr\gc\gcref.h'
$initializeVisualStudio = Join-Path $RepositoryRoot 'eng\native\init-vs-env.cmd'
$validationSummary = Join-Path $OutputDirectory 'validation-summary.csv'
$controlSummary = Join-Path $OutputDirectory 'control-summary.csv'
$identitySummary = Join-Path $OutputDirectory 'identities.csv'
$validationRows = [Collections.Generic.List[object]]::new()
$controlRows = [Collections.Generic.List[object]]::new()
$repositoryPrefix = (
    [IO.Path]::GetFullPath($RepositoryRoot).TrimEnd('\') + '\')

$includeDirectories = @(
    'src\coreclr\nativeaot\Runtime',
    'src\coreclr\nativeaot\Runtime\inc',
    'src\coreclr\nativeaot\Runtime\windows',
    'src\coreclr\nativeaot\Runtime\amd64',
    'src\coreclr\gc',
    'src\coreclr\gc\env',
    'src\coreclr\minipal',
    'src\native',
    'src\native\inc',
    'src\coreclr\pal\prebuilt\inc',
    'artifacts\obj'
)
$includeArguments = ($includeDirectories | ForEach-Object {
    $path = Join-Path $RepositoryRoot $_
    "/I`"$path`""
}) -join ' '
$defines = @(
    '/DFEATURE_NATIVEAOT',
    '/DGC_DESCRIPTOR',
    '/DTARGET_WINDOWS',
    '/DHOST_WINDOWS',
    '/DWIN32',
    '/DTARGET_AMD64',
    '/DHOST_AMD64',
    '/DTARGET_64BIT',
    '/DHOST_64BIT'
) -join ' '

New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$currentHeader = Get-Content -LiteralPath $header -Raw
$productCommit = (git -C $RepositoryRoot rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0) {
    throw 'Unable to resolve the product commit.'
}
$identitySources = @(
    'src/coreclr/gc/gcref.h',
    'docs/design/lxr-port/P1.5/nativeaot-reference-enumeration-validation.cpp',
    'docs/design/lxr-port/P1.5/run-nativeaot-reference-enumeration-validation.ps1'
)
git -C $RepositoryRoot diff --quiet HEAD -- $identitySources
if ($LASTEXITCODE -ne 0) {
    throw 'Commit the NativeAOT fix and controls before generating final identities.'
}
foreach ($identitySource in $identitySources) {
    git -C $RepositoryRoot ls-files --error-unmatch $identitySource *> $null
    if ($LASTEXITCODE -ne 0) {
        throw "Identity source is not committed: $identitySource"
    }
}

function New-ControlHeader(
    [string]$Name,
    [string]$Text
) {
    $directory = Join-Path $OutputDirectory $Name
    New-Item -ItemType Directory -Path $directory -Force | Out-Null
    $controlHeader = Join-Path $directory 'gcref-control.h'
    $wrapper = Join-Path $directory 'force-include.h'
    Set-Content -LiteralPath $controlHeader -Value $Text -NoNewline
    Set-Content -LiteralPath $wrapper -Value @"
#include "gcenv.h"
#include "gcinterface.h"
#include "gcref-control.h"
"@
    return [pscustomobject]@{
        Directory = $directory
        Header = $controlHeader
        Wrapper = $wrapper
    }
}

function Normalize-EvidencePath([string]$text) {
    return $text.Replace($repositoryPrefix, '')
}

function Invoke-Compile(
    [string]$Name,
    [string]$ForceInclude,
    [bool]$Link
) {
    $directory = Join-Path $OutputDirectory $Name
    New-Item -ItemType Directory -Path $directory -Force | Out-Null
    $object = Join-Path $directory 'validation.obj'
    $executable = Join-Path $directory 'validation.exe'
    $log = Join-Path $directory 'compile.log'
    $forceIncludeArgument = if ($ForceInclude) {
        $controlDirectory = Split-Path -Parent $ForceInclude
        "/FI`"$ForceInclude`" /I`"$controlDirectory`""
    } else {
        ''
    }
    $outputArgument = if ($Link) {
        "/Fo:`"$object`" /Fe:`"$executable`""
    } else {
        "/c /Fo:`"$object`""
    }
    $command = @(
        "call `"$initializeVisualStudio`" x64",
        "cl /nologo /std:c++17 /permissive- /EHsc /wd4005 $defines $includeArguments $forceIncludeArgument `"$source`" $outputArgument"
    ) -join ' && '

    & $env:ComSpec /d /s /c $command *> $log
    return [pscustomobject]@{
        Directory = $directory
        Executable = $executable
        Log = $log
        ExitCode = $LASTEXITCODE
    }
}

$final = Invoke-Compile 'final' '' $true
if ($final.ExitCode -ne 0) {
    throw "Final NativeAOT-flavor compile failed. See $($final.Log)."
}
& $final.Executable *> (Join-Path $final.Directory 'run.log')
$finalExitCode = $LASTEXITCODE
$finalOutput = Get-Content -LiteralPath (
    Join-Path $final.Directory 'run.log') -Raw
if (($finalExitCode -ne 0) -or
    ($finalOutput -notmatch '21/21 NativeAOT reference enumeration checks passed')) {
    throw 'Final NativeAOT-flavor execution failed.'
}
$validationRows.Add([pscustomobject][ordered]@{
    Name = 'final-nativeaot-shared-gc'
    ProductCommit = $productCommit
    Expected = 'compile and 21/21 execution'
    Observed = '21/21 NativeAOT reference enumeration checks passed'
    Result = 'PASS'
    ExitCode = $finalExitCode
    Evidence = 'final\compile.log; final\run.log'
})

$previousHeaderLines = git -C $RepositoryRoot show (
    "$PreviousRevision`:src/coreclr/gc/gcref.h")
if ($LASTEXITCODE -ne 0) {
    throw "Unable to load pre-fix gcref.h from $PreviousRevision."
}
$previousHeader = ($previousHeaderLines -join [Environment]::NewLine) +
    [Environment]::NewLine
$previous = New-ControlHeader 'pre-fix' $previousHeader
$previousCompile = Invoke-Compile 'pre-fix' $previous.Wrapper $false
$previousOutput = Get-Content -LiteralPath $previousCompile.Log -Raw
if (($previousCompile.ExitCode -eq 0) -or
    ($previousOutput -notmatch (
        "cannot convert from 'Object \*' to 'ArrayBase \*'|inaccessible"))) {
    throw 'Exact pre-fix header did not reproduce the NativeAOT cast failure.'
}
$previousError = Normalize-EvidencePath (@(
    $previousOutput -split '\r?\n' |
        Where-Object {
            $_ -match "cannot convert from 'Object \*' to 'ArrayBase \*'|inaccessible"
        } |
        Select-Object -First 1
)[0].Trim())
$validationRows.Add([pscustomobject][ordered]@{
    Name = 'pre-fix-nativeaot-compile'
    ProductCommit = $PreviousRevision
    Expected = 'inaccessible ArrayBase conversion'
    Observed = $previousError
    Result = 'PASS'
    ExitCode = $previousCompile.ExitCode
    Evidence = 'pre-fix\compile.log'
})

$castOld = 'reinterpret_cast<ArrayBase*>(object)->GetNumComponents();'
$castNew = 'static_cast<ArrayBase*>(object)->GetNumComponents();'
$castMatches = [regex]::Matches(
    $currentHeader,
    [regex]::Escape($castOld)).Count
if ($castMatches -ne 1) {
    throw "Static-cast control matched $castMatches sites; expected one."
}
$castControl = New-ControlHeader (
    'static-cast-control') ($currentHeader.Replace($castOld, $castNew))
$castCompile = Invoke-Compile (
    'static-cast-control') $castControl.Wrapper $false
$castOutput = Get-Content -LiteralPath $castCompile.Log -Raw
if (($castCompile.ExitCode -eq 0) -or
    ($castOutput -notmatch (
        "cannot convert from 'Object \*' to 'ArrayBase \*'|inaccessible"))) {
    throw 'Static-cast perturbation did not reproduce the NativeAOT failure.'
}
$castError = Normalize-EvidencePath (@(
    $castOutput -split '\r?\n' |
        Where-Object {
            $_ -match "cannot convert from 'Object \*' to 'ArrayBase \*'|inaccessible"
        } |
        Select-Object -First 1
)[0].Trim())
$controlRows.Add([pscustomobject][ordered]@{
    Name = 'nativeaot-static-cast'
    ProductCommit = $productCommit
    PerturbationCount = 1
    Expected = 'NativeAOT compile failure'
    Observed = $castError
    Result = 'PASS'
    ExitCode = $castCompile.ExitCode
    Evidence = 'static-cast-control\compile.log'
})

$sizeOld =
    'GCReferenceObjectLayout layout = { methodTable->GetBaseSize(), 0 };'
$sizeNew =
    'GCReferenceObjectLayout layout = { object->GetSize(), 0 };'
$sizeMatches = [regex]::Matches(
    $currentHeader,
    [regex]::Escape($sizeOld)).Count
if ($sizeMatches -ne 1) {
    throw "Unmasked-size control matched $sizeMatches sites; expected one."
}
$sizeControl = New-ControlHeader (
    'unmasked-size-control') ($currentHeader.Replace($sizeOld, $sizeNew))
$sizeCompile = Invoke-Compile (
    'unmasked-size-control') $sizeControl.Wrapper $true
if ($sizeCompile.ExitCode -ne 0) {
    throw "Unmasked-size control did not compile. See $($sizeCompile.Log)."
}
$sizeRunLog = Join-Path $sizeCompile.Directory 'run.log'
& $sizeCompile.Executable *> $sizeRunLog
$sizeExitCode = $LASTEXITCODE
$sizeOutput = Get-Content -LiteralPath $sizeRunLog -Raw
if (($sizeExitCode -eq 0) -or
    ($sizeOutput -notmatch 'FAIL: NativeAOT .* avoids Object::GetSize')) {
    throw 'Unmasked-size perturbation did not fail the marked-MT control.'
}
$sizeFailure = @(
    $sizeOutput -split '\r?\n' |
        Where-Object { $_ -match 'FAIL: NativeAOT .* avoids Object::GetSize' } |
        Select-Object -First 1
)[0].Trim()
$controlRows.Add([pscustomobject][ordered]@{
    Name = 'nativeaot-unmasked-object-size'
    ProductCommit = $productCommit
    PerturbationCount = 1
    Expected = 'marked-MT executable validation failure'
    Observed = $sizeFailure
    Result = 'PASS'
    ExitCode = $sizeExitCode
    Evidence = 'unmasked-size-control\compile.log; unmasked-size-control\run.log'
})

if (($validationRows.Count -ne 2) -or ($controlRows.Count -ne 2)) {
    throw 'NativeAOT validation/control cardinality is incomplete.'
}
$validationRows | Export-Csv -LiteralPath $validationSummary -NoTypeInformation
$controlRows | Export-Csv -LiteralPath $controlSummary -NoTypeInformation

$identityRows = @(
    [pscustomobject][ordered]@{
        Name = 'src\coreclr\gc\gcref.h'
        ProductCommit = $productCommit
        Sha256 = (Get-FileHash -LiteralPath $header -Algorithm SHA256).Hash
        Length = (Get-Item -LiteralPath $header).Length
    },
    [pscustomobject][ordered]@{
        Name = 'final\validation.exe'
        ProductCommit = $productCommit
        Sha256 = (Get-FileHash -LiteralPath $final.Executable -Algorithm SHA256).Hash
        Length = (Get-Item -LiteralPath $final.Executable).Length
    },
    [pscustomobject][ordered]@{
        Name = 'pre-fix\gcref-control.h'
        ProductCommit = $PreviousRevision
        Sha256 = (Get-FileHash -LiteralPath $previous.Header -Algorithm SHA256).Hash
        Length = (Get-Item -LiteralPath $previous.Header).Length
    }
)
$identityRows | Export-Csv -LiteralPath $identitySummary -NoTypeInformation

Write-Host 'PASS: final NativeAOT shared-GC compile and marked-MT execution'
Write-Host 'PASS: exact pre-fix inaccessible-base compile rejection'
Write-Host 'PASS: 2 exact-cardinality NativeAOT controls'
$global:LASTEXITCODE = 0
