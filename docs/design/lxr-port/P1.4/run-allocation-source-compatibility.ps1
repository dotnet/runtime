# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

[CmdletBinding()]
param(
    [string]$RepositoryRoot,
    [string]$RuntimeRoot,
    [string]$OutputDirectory,
    [string]$SummaryPath,
    [string]$WorkingDirectory,
    [ValidatePattern('^[D-Z]$')]
    [string]$DriveLetter,
    [ValidatePattern('^[0-9a-f]{40}$')]
    [string]$PreviousRevision = '8b4f037f82a92395f304b716c8a630aba2ec20d3',
    [switch]$KeepWorkingDirectory
)

$ErrorActionPreference = 'Stop'
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$customOutputDirectory = $PSBoundParameters.ContainsKey('OutputDirectory')
if (-not $RepositoryRoot) {
    $RepositoryRoot = (Resolve-Path (Join-Path $scriptRoot '..\..\..\..')).Path
}
if (-not $RuntimeRoot) {
    $RuntimeRoot = Join-Path $RepositoryRoot (
        'artifacts\tests\coreclr\windows.x64.Debug\Tests\Core_Root')
}
if (-not $OutputDirectory) {
    $OutputDirectory = Join-Path $RepositoryRoot 'artifacts\p14-source-compatibility'
}
if (-not $SummaryPath) {
    $SummaryPath = if ($customOutputDirectory) {
        Join-Path $OutputDirectory 'source-compatibility-summary.csv'
    } else {
        Join-Path $scriptRoot 'raw\source-compatibility-summary.csv'
    }
}
if (-not $WorkingDirectory) {
    $WorkingDirectory = Join-Path $OutputDirectory 'work'
}
if (-not $IsWindows) {
    throw 'The old-source/current-header build control requires Windows.'
}

$dotnet = Join-Path $RepositoryRoot '.dotnet\dotnet.exe'
$currentHeader = Join-Path $RepositoryRoot 'src\coreclr\gc\gcinterface.h'
$corerun = Join-Path $RuntimeRoot 'corerun.exe'
$smokeProject = Join-Path $RepositoryRoot (
    'docs\design\lxr-port\P1.1\runtime-smoke\runtime-smoke.csproj')
$smokeOutput = Join-Path $OutputDirectory 'smoke'
$smokeAssembly = Join-Path $smokeOutput 'runtime-smoke.dll'
foreach ($path in @($dotnet, $currentHeader, $corerun, $smokeProject)) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Required file not found: $path"
    }
}

if (-not $DriveLetter) {
    $usedDriveLetters = @(Get-PSDrive -PSProvider FileSystem | ForEach-Object Name)
    $DriveLetter = @('T', 'U', 'V', 'W', 'X', 'Y', 'Z') |
        Where-Object { $usedDriveLetters -notcontains $_ } |
        Select-Object -First 1
    if (-not $DriveLetter) {
        throw 'No short-path drive letter is available.'
    }
}

$mappedRoot = "$DriveLetter`:\"
$sourceRoot = Join-Path $WorkingDirectory 'source'
$archive = Join-Path $WorkingDirectory 'p13-source.tar'
$sourceHeader = Join-Path $sourceRoot 'src\coreclr\gc\gcinterface.h'
$sourceGcImpl = Join-Path $sourceRoot 'src\coreclr\gc\gcimpl.h'
$sourceInterface = Join-Path $sourceRoot 'src\coreclr\gc\interface.cpp'
$dotnetJunction = Join-Path $sourceRoot '.dotnet'
$oldBinaryDirectory = Join-Path $WorkingDirectory 'old-binaries'
$oldRuntimeRoot = Join-Path $WorkingDirectory 'old-runtime-root'
$summary = [Collections.Generic.List[object]]::new()
$driveMapped = $false

$defaultImplementation = @'
    virtual AllocationNotificationParameters* GetAllocationNotificationParameters()
    {
        static AllocationNotificationParameters parameters = {};
        return &parameters;
    }
'@
$pureVirtualImplementation = @'
    virtual AllocationNotificationParameters* GetAllocationNotificationParameters() PURE_VIRTUAL;
'@

function Assert-LiteralCount(
    [string]$Text,
    [string]$Value,
    [int]$ExpectedCount,
    [string]$Description
) {
    $actualCount = [regex]::Matches($Text, [regex]::Escape($Value)).Count
    if ($actualCount -ne $ExpectedCount) {
        throw "$Description matched $actualCount sites; expected $ExpectedCount."
    }
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
    } finally {
        Pop-Location
    }

    if ($ExpectSuccess) {
        if ($exitCode -ne 0) {
            throw "Build failed: $Name. See $log."
        }
    } else {
        $output = Get-Content -LiteralPath $log -Raw
        if (($exitCode -eq 0) -or
            ($output -notmatch 'C2259') -or
            ($output -notmatch 'cannot instantiate abstract class') -or
            ($output -notmatch 'GetAllocationNotificationParameters')) {
            throw "PURE_VIRTUAL control did not fail as an abstract-class build. See $log."
        }
    }

    $summary.Add([pscustomobject][ordered]@{
        Name = $Name
        Expected = if ($ExpectSuccess) { 'success' } else { 'abstract-class failure' }
        Result = 'PASS'
        ExitCode = $exitCode
        Evidence = [IO.Path]::GetFileName($log)
    })
}

function Invoke-Startup(
    [string]$Name,
    [string]$Server,
    [string]$StandaloneGC
) {
    $log = Join-Path $OutputDirectory "$Name.log"
    $env:DOTNET_GCPath = $StandaloneGC
    $env:DOTNET_gcServer = $Server
    $env:DOTNET_ReadyToRun = '0'
    & $corerun $smokeAssembly *> $log
    $exitCode = $LASTEXITCODE
    $output = Get-Content -LiteralPath $log -Raw
    if (($exitCode -ne 0) -or ($output -notmatch '(?m)^PASS:')) {
        throw "Startup failed: $Name. See $log."
    }

    $summary.Add([pscustomobject][ordered]@{
        Name = $Name
        Expected = 'startup success'
        Result = 'PASS'
        ExitCode = $exitCode
        Evidence = [IO.Path]::GetFileName($log)
    })
}

function Invoke-RejectedStartup(
    [string]$Name,
    [string]$Runtime,
    [string]$Server,
    [string]$StandaloneGC,
    [string]$ExpectedError,
    [string]$Malformed
) {
    $log = Join-Path $OutputDirectory "$Name.log"
    $env:DOTNET_GCPath = $StandaloneGC
    $env:DOTNET_gcServer = $Server
    $env:DOTNET_ReadyToRun = '0'
    $env:DOTNET_GCAllocationNotificationTest = '1'
    if ($Malformed) {
        $env:DOTNET_GCAllocationNotificationTestMalformed = $Malformed
    } else {
        Remove-Item Env:\DOTNET_GCAllocationNotificationTestMalformed `
            -ErrorAction SilentlyContinue
    }
    & $Runtime $smokeAssembly *> $log
    $exitCode = $LASTEXITCODE
    $output = Get-Content -LiteralPath $log -Raw
    if (($exitCode -eq 0) -or
        ($output -notmatch [regex]::Escape($ExpectedError)) -or
        ($output -match '(?m)^PASS:')) {
        throw "Startup rejection failed: $Name. See $log."
    }

    $summary.Add([pscustomobject][ordered]@{
        Name = $Name
        Expected = "startup rejection $ExpectedError"
        Result = 'PASS'
        ExitCode = $exitCode
        Evidence = [IO.Path]::GetFileName($log)
    })
    Remove-Item Env:\DOTNET_GCAllocationNotificationTest -ErrorAction SilentlyContinue
    Remove-Item Env:\DOTNET_GCAllocationNotificationTestMalformed -ErrorAction SilentlyContinue
}

try {
    New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
    if (Test-Path -LiteralPath $WorkingDirectory) {
        Remove-Item -LiteralPath $WorkingDirectory -Recurse -Force
    }
    New-Item -ItemType Directory -Path $sourceRoot -Force | Out-Null

    git -C $RepositoryRoot cat-file -e "$PreviousRevision^{commit}"
    if ($LASTEXITCODE -ne 0) {
        throw "Previous revision is unavailable: $PreviousRevision"
    }
    git -C $RepositoryRoot archive --format=tar --output=$archive $PreviousRevision
    if ($LASTEXITCODE -ne 0) {
        throw "Unable to archive previous revision: $PreviousRevision"
    }
    tar -xf $archive -C $sourceRoot
    if ($LASTEXITCODE -ne 0) {
        throw 'Unable to extract previous source.'
    }

    $oldGcImplText = Get-Content -LiteralPath $sourceGcImpl -Raw
    $oldInterfaceText = Get-Content -LiteralPath $sourceInterface -Raw
    Assert-LiteralCount $oldGcImplText 'GetAllocationNotificationParameters' 0 (
        'old GCHeap declaration')
    Assert-LiteralCount $oldInterfaceText 'GetAllocationNotificationParameters' 0 (
        'old GCHeap implementation')

    New-Item -ItemType Junction -Path $dotnetJunction `
        -Target (Join-Path $RepositoryRoot '.dotnet') | Out-Null
    subst "$DriveLetter`:" $sourceRoot
    if ($LASTEXITCODE -ne 0) {
        throw "Unable to map $mappedRoot to $sourceRoot."
    }
    $driveMapped = $true

    & $dotnet build $smokeProject -c Release -o $smokeOutput --nologo
    if ($LASTEXITCODE -ne 0) {
        throw 'Unable to build the compatibility smoke application.'
    }

    Invoke-SourceBuild 'old-binary-build' $true
    New-Item -ItemType Directory -Path $oldBinaryDirectory -Force | Out-Null
    foreach ($fileName in @('clrgc.dll', 'coreclr.dll', 'corerun.exe')) {
        Copy-Item -LiteralPath (
            Join-Path $mappedRoot "artifacts\bin\coreclr\windows.x64.Debug\$fileName") `
            -Destination (Join-Path $oldBinaryDirectory $fileName) -Force
    }
    $oldStandaloneGC = Join-Path $oldBinaryDirectory 'clrgc.dll'
    Invoke-Startup 'old-binary-current-runtime-workstation' '0' $oldStandaloneGC
    Invoke-Startup 'old-binary-current-runtime-server' '1' $oldStandaloneGC

    New-Item -ItemType Directory -Path $oldRuntimeRoot -Force | Out-Null
    Get-ChildItem -LiteralPath $RuntimeRoot -Force |
        Copy-Item -Destination $oldRuntimeRoot -Recurse -Force
    Copy-Item -LiteralPath (Join-Path $oldBinaryDirectory 'coreclr.dll') `
        -Destination (Join-Path $oldRuntimeRoot 'coreclr.dll') -Force
    Copy-Item -LiteralPath (Join-Path $oldBinaryDirectory 'corerun.exe') `
        -Destination (Join-Path $oldRuntimeRoot 'corerun.exe') -Force
    $oldCorerun = Join-Path $oldRuntimeRoot 'corerun.exe'
    $currentStandaloneGC = Join-Path $RuntimeRoot 'clrgc.dll'
    Invoke-RejectedStartup `
        'enabled-current-binary-old-runtime-workstation' `
        $oldCorerun '0' $currentStandaloneGC '0x80004001'
    Invoke-RejectedStartup `
        'enabled-current-binary-old-runtime-server' `
        $oldCorerun '1' $currentStandaloneGC '0x80004001'
    Invoke-RejectedStartup `
        'null-descriptor-old-runtime-rejection' `
        $oldCorerun '0' $currentStandaloneGC '0x80004005' '3'

    Copy-Item -LiteralPath $currentHeader -Destination $sourceHeader -Force
    Invoke-SourceBuild 'old-source-current-header-build' $true
    $standaloneGC = Join-Path $mappedRoot (
        'artifacts\bin\coreclr\windows.x64.Debug\clrgc.dll')
    if (-not (Test-Path -LiteralPath $standaloneGC -PathType Leaf)) {
        throw "Rebuilt standalone GC not found: $standaloneGC"
    }
    Invoke-Startup 'old-source-current-header-workstation' '0' $standaloneGC
    Invoke-Startup 'old-source-current-header-server' '1' $standaloneGC

    Remove-Item Env:\DOTNET_GCPath -ErrorAction SilentlyContinue
    Remove-Item Env:\DOTNET_gcServer -ErrorAction SilentlyContinue
    $env:DOTNET_GCAllocationNotificationTest = '1'
    $env:DOTNET_GCAllocationNotificationTestMalformed = '3'
    $nullLog = Join-Path $OutputDirectory 'null-descriptor-rejection.log'
    & $corerun $smokeAssembly *> $nullLog
    $nullExitCode = $LASTEXITCODE
    $nullOutput = Get-Content -LiteralPath $nullLog -Raw
    if (($nullExitCode -eq 0) -or
        ($nullOutput -notmatch 'GC allocation notification descriptor is null') -or
        ($nullOutput -match '(?m)^PASS:')) {
        throw "Null-descriptor control did not fail before managed execution. See $nullLog."
    }
    $summary.Add([pscustomobject][ordered]@{
        Name = 'null-descriptor-rejection'
        Expected = 'startup rejection'
        Result = 'PASS'
        ExitCode = $nullExitCode
        Evidence = [IO.Path]::GetFileName($nullLog)
    })
    Remove-Item Env:\DOTNET_GCAllocationNotificationTest -ErrorAction SilentlyContinue
    Remove-Item Env:\DOTNET_GCAllocationNotificationTestMalformed -ErrorAction SilentlyContinue

    $headerText = Get-Content -LiteralPath $sourceHeader -Raw
    Assert-LiteralCount $headerText $defaultImplementation 1 'default implementation'
    $pureVirtualText = $headerText.Replace(
        $defaultImplementation,
        $pureVirtualImplementation)
    Assert-LiteralCount $pureVirtualText $defaultImplementation 0 (
        'default implementation after perturbation')
    Assert-LiteralCount $pureVirtualText $pureVirtualImplementation 1 (
        'PURE_VIRTUAL implementation')
    Set-Content -LiteralPath $sourceHeader -Value $pureVirtualText -NoNewline
    try {
        Invoke-SourceBuild 'pure-virtual-negative-control' $false
    } finally {
        Set-Content -LiteralPath $sourceHeader -Value $headerText -NoNewline
    }

    Invoke-SourceBuild 'restored-old-source-current-header-build' $true
    Invoke-Startup 'restored-old-source-current-header-workstation' '0' $standaloneGC
    Invoke-Startup 'restored-old-source-current-header-server' '1' $standaloneGC

    New-Item -ItemType Directory -Path (Split-Path -Parent $SummaryPath) -Force | Out-Null
    $summary | Export-Csv -LiteralPath $SummaryPath -NoTypeInformation
    Write-Host "RESULT: PASS ($($summary.Count) source-compatibility controls)"
} finally {
    Remove-Item Env:\DOTNET_GCPath -ErrorAction SilentlyContinue
    Remove-Item Env:\DOTNET_gcServer -ErrorAction SilentlyContinue
    Remove-Item Env:\DOTNET_ReadyToRun -ErrorAction SilentlyContinue
    Remove-Item Env:\DOTNET_GCAllocationNotificationTest -ErrorAction SilentlyContinue
    Remove-Item Env:\DOTNET_GCAllocationNotificationTestMalformed -ErrorAction SilentlyContinue
    if ($driveMapped) {
        subst "$DriveLetter`:" /d
    }
    if (-not $KeepWorkingDirectory -and (Test-Path -LiteralPath $WorkingDirectory)) {
        if (Test-Path -LiteralPath $dotnetJunction) {
            Remove-Item -LiteralPath $dotnetJunction -Force
        }
        Remove-Item -LiteralPath $WorkingDirectory -Recurse -Force
    }
}
