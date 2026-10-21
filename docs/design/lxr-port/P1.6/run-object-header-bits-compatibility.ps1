# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

[CmdletBinding()]
param(
    [string]$RepositoryRoot,
    [string]$RuntimeRoot,
    [string]$SmokeAssembly,
    [string]$OutputDirectory,
    [ValidatePattern('^[D-Z]$')]
    [string]$DriveLetter = 'S',
    [ValidatePattern('^[0-9a-f]{40}$')]
    [string]$PreviousRevision = '02fbc68684994a6071c07ce00bacac95d693b13e',
    [switch]$KeepWorkingDirectory
)

$ErrorActionPreference = 'Stop'
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path

if (-not $RepositoryRoot) {
    $RepositoryRoot = (Resolve-Path (Join-Path $scriptRoot '..\..\..\..')).Path
}
if (-not $RuntimeRoot) {
    $RuntimeRoot = Join-Path $RepositoryRoot (
        'artifacts\tests\coreclr\windows.x64.Checked\Tests\Core_Root')
}
if (-not $SmokeAssembly) {
    $SmokeAssembly = Join-Path $RepositoryRoot (
        'artifacts\p16-object-header-bits-startup\startup-smoke.dll')
}
if (-not $OutputDirectory) {
    $OutputDirectory = Join-Path $RepositoryRoot (
        'artifacts\p16-object-header-bits-compatibility')
}

$workingDirectory = Join-Path $OutputDirectory 'work'
$mappedRoot = "$DriveLetter`:\"
$sourceRoot = Join-Path $workingDirectory 'source'
$archive = Join-Path $workingDirectory 'p15-source.tar'
$sourceHeader = Join-Path $sourceRoot 'src\coreclr\gc\gcinterface.h'
$currentHeader = Join-Path $RepositoryRoot 'src\coreclr\gc\gcinterface.h'
$dotnetJunction = Join-Path $sourceRoot '.dotnet'
$oldBinaryDirectory = Join-Path $workingDirectory 'old-binaries'
$oldRuntimeRoot = Join-Path $workingDirectory 'old-runtime-root'
$currentRuntime = Join-Path $RuntimeRoot 'corerun.exe'
$currentStandaloneGC = Join-Path $RepositoryRoot (
    'artifacts\bin\coreclr\windows.x64.Checked\clrgc.dll')
$summaryPath = Join-Path $OutputDirectory 'compatibility-summary.csv'
$summary = [Collections.Generic.List[object]]::new()
$driveMapped = $false

foreach ($path in @(
    $currentHeader,
    $currentRuntime,
    $currentStandaloneGC,
    $SmokeAssembly
)) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Required file not found: $path"
    }
}

function Add-Result(
    [string]$Name,
    [string]$Expected,
    [int]$ExitCode,
    [string]$Evidence
) {
    $summary.Add([pscustomobject][ordered]@{
        Name = $Name
        Expected = $Expected
        Result = 'PASS'
        ExitCode = $ExitCode
        Evidence = $Evidence
    })
}

function Invoke-SourceBuild(
    [string]$Name,
    [bool]$ExpectSuccess
) {
    $log = Join-Path $OutputDirectory "$Name.log"
    Push-Location $mappedRoot
    try {
        & .\build.cmd clr.runtime -rc Debug `
            "/p:RepositoryCommit=$PreviousRevision" `
            -cmakeargs "-DCLI_CMAKE_COMMIT_HASH=$PreviousRevision" *> $log
        $exitCode = $LASTEXITCODE
    }
    finally {
        Pop-Location
    }

    if ($ExpectSuccess) {
        if ($exitCode -ne 0) {
            throw "Build failed: $Name. See $log."
        }
        Add-Result $Name 'success' $exitCode ([IO.Path]::GetFileName($log))
        return
    }

    $output = Get-Content -LiteralPath $log -Raw
    if (($exitCode -eq 0) -or
        ($output -notmatch 'cannot instantiate abstract class') -or
        ($output -notmatch 'GetObjectHeaderBitsParameters')) {
        throw "PURE_VIRTUAL control did not fail as expected. See $log."
    }
    Add-Result $Name 'abstract-class failure' $exitCode ([IO.Path]::GetFileName($log))
}

function Invoke-Startup(
    [string]$Name,
    [string]$Runtime,
    [string]$StandaloneGC,
    [bool]$EnableHeaderBits,
    [bool]$ExpectSuccess
) {
    $log = Join-Path $OutputDirectory "$Name.log"
    $savedGCPath = [Environment]::GetEnvironmentVariable('DOTNET_GCPath')
    $savedReadyToRun = [Environment]::GetEnvironmentVariable('DOTNET_ReadyToRun')
    $savedHeaderBits = [Environment]::GetEnvironmentVariable('DOTNET_GCObjectHeaderBitsTest')
    try {
        [Environment]::SetEnvironmentVariable('DOTNET_GCPath', $StandaloneGC)
        [Environment]::SetEnvironmentVariable('DOTNET_ReadyToRun', '0')
        [Environment]::SetEnvironmentVariable(
            'DOTNET_GCObjectHeaderBitsTest',
            $(if ($EnableHeaderBits) { '1' } else { $null }))

        & $Runtime $SmokeAssembly *> $log
        $exitCode = $LASTEXITCODE
    }
    finally {
        [Environment]::SetEnvironmentVariable('DOTNET_GCPath', $savedGCPath)
        [Environment]::SetEnvironmentVariable('DOTNET_ReadyToRun', $savedReadyToRun)
        [Environment]::SetEnvironmentVariable('DOTNET_GCObjectHeaderBitsTest', $savedHeaderBits)
    }

    if ($ExpectSuccess) {
        if ($exitCode -ne 100) {
            throw "Startup failed: $Name. See $log."
        }
        Add-Result $Name 'startup success' $exitCode ([IO.Path]::GetFileName($log))
    } else {
        if (($exitCode -eq 0) -or ($exitCode -eq 100)) {
            throw "Startup unexpectedly succeeded: $Name. See $log."
        }
        Add-Result $Name 'startup rejection' $exitCode ([IO.Path]::GetFileName($log))
    }
}

try {
    New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
    if (Test-Path -LiteralPath $workingDirectory) {
        Remove-Item -LiteralPath $workingDirectory -Recurse -Force
    }
    New-Item -ItemType Directory -Path $sourceRoot -Force | Out-Null

    git -C $RepositoryRoot cat-file -e "$PreviousRevision^{commit}"
    if ($LASTEXITCODE -ne 0) {
        throw "Previous revision is unavailable: $PreviousRevision"
    }
    git -C $RepositoryRoot archive --format=tar --output=$archive $PreviousRevision
    if ($LASTEXITCODE -ne 0) {
        throw 'Unable to create previous-revision archive.'
    }
    tar -xf $archive -C $sourceRoot
    if ($LASTEXITCODE -ne 0) {
        throw 'Unable to extract previous-revision archive.'
    }

    $oldHeader = Get-Content -LiteralPath $sourceHeader -Raw
    if ($oldHeader.Contains('GetObjectHeaderBitsParameters')) {
        throw 'Previous revision unexpectedly contains the P1.6 interface.'
    }

    New-Item -ItemType Junction -Path $dotnetJunction `
        -Target (Join-Path $RepositoryRoot '.dotnet') | Out-Null
    if (Test-Path -LiteralPath $mappedRoot) {
        throw "$DriveLetter`: is already in use."
    }
    subst "$DriveLetter`:" $sourceRoot
    if ($LASTEXITCODE -ne 0) {
        throw "Unable to map $mappedRoot to the archived source."
    }
    $driveMapped = $true

    Invoke-SourceBuild 'old-binary-build' $true

    New-Item -ItemType Directory -Path $oldBinaryDirectory -Force | Out-Null
    $oldBuildOutput = Join-Path $sourceRoot (
        'artifacts\bin\coreclr\windows.x64.Debug')
    foreach ($fileName in @('clrgc.dll', 'coreclr.dll', 'corerun.exe')) {
        Copy-Item -LiteralPath (Join-Path $oldBuildOutput $fileName) `
            -Destination (Join-Path $oldBinaryDirectory $fileName) -Force
    }

    $oldStandaloneGC = Join-Path $oldBinaryDirectory 'clrgc.dll'
    Invoke-Startup 'old-binary-current-runtime' $currentRuntime `
        $oldStandaloneGC $false $true

    New-Item -ItemType Directory -Path $oldRuntimeRoot -Force | Out-Null
    Get-ChildItem -LiteralPath $RuntimeRoot -Force |
        Copy-Item -Destination $oldRuntimeRoot -Recurse -Force
    Copy-Item -LiteralPath (Join-Path $oldBinaryDirectory 'coreclr.dll') `
        -Destination (Join-Path $oldRuntimeRoot 'coreclr.dll') -Force
    Copy-Item -LiteralPath (Join-Path $oldBinaryDirectory 'corerun.exe') `
        -Destination (Join-Path $oldRuntimeRoot 'corerun.exe') -Force
    $oldRuntime = Join-Path $oldRuntimeRoot 'corerun.exe'

    Invoke-Startup 'new-disabled-gc-old-runtime' $oldRuntime `
        $currentStandaloneGC $false $true
    Invoke-Startup 'new-enabled-gc-old-runtime' $oldRuntime `
        $currentStandaloneGC $true $false

    Copy-Item -LiteralPath $currentHeader -Destination $sourceHeader -Force
    Invoke-SourceBuild 'old-source-current-header-build' $true
    $rebuiltStandaloneGC = Join-Path $oldBuildOutput 'clrgc.dll'
    Invoke-Startup 'old-source-current-header-current-runtime' $currentRuntime `
        $rebuiltStandaloneGC $false $true

    $headerText = Get-Content -LiteralPath $sourceHeader -Raw
    $defaultMethod = @'
    virtual ObjectHeaderBitsParameters* GetObjectHeaderBitsParameters()
    {
        static ObjectHeaderBitsParameters parameters = {};
        return &parameters;
    }
'@
    $pureVirtualMethod = @'
    virtual ObjectHeaderBitsParameters* GetObjectHeaderBitsParameters() PURE_VIRTUAL;
'@
    $matchCount = [regex]::Matches(
        $headerText,
        [regex]::Escape($defaultMethod)).Count
    if ($matchCount -ne 1) {
        throw "Default method matched $matchCount sites; expected exactly one."
    }
    Set-Content -LiteralPath $sourceHeader -Value (
        $headerText.Replace($defaultMethod, $pureVirtualMethod)) -NoNewline
    Invoke-SourceBuild 'pure-virtual-control-build' $false

    $summary | Export-Csv -LiteralPath $summaryPath -NoTypeInformation
    Write-Host "PASS: $($summary.Count) compatibility controls"
}
finally {
    if ($driveMapped) {
        subst "$DriveLetter`:" /D
    }
    if (-not $KeepWorkingDirectory -and
        (Test-Path -LiteralPath $workingDirectory)) {
        Remove-Item -LiteralPath $workingDirectory -Recurse -Force
    }
}

exit 0
