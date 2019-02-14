# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

param(
    [Parameter(Mandatory=$true)][string]$TargetingPackPublishRoot,
    [Parameter(Mandatory=$true)][string]$TargetingPackMSIOutput,
    [Parameter(Mandatory=$true)][string]$WixRoot,
    [Parameter(Mandatory=$true)][string]$ProductMoniker,
    [Parameter(Mandatory=$true)][string]$TargetingPackMSIVersion,
    [Parameter(Mandatory=$true)][string]$TargetingPackNugetVersion,
    [Parameter(Mandatory=$true)][string]$Architecture,
    [Parameter(Mandatory=$true)][string]$TargetArchitecture,
    [Parameter(Mandatory=$true)][string]$WixObjRoot,
    [Parameter(Mandatory=$true)][string]$TargetingPackUpgradeCode
)

$RepoRoot = Convert-Path "$PSScriptRoot\..\..\..\..\.."
$CommonScript = "$RepoRoot\tools-local\scripts\common\_common.ps1"

if(-Not (Test-Path "$CommonScript"))
{
    Exit -1
} 
. "$CommonScript"

$PackagingRoot = Join-Path $RepoRoot "src\pkg\packaging"

$InstallFileswsx = "$WixObjRoot\install-files.wxs"
$InstallFilesWixobj = "$WixObjRoot\install-files.wixobj"

function RunHeat
{
    $Result = $true
    pushd "$WixRoot"

    Write-Host Running heat.. to $InstallFileswsx
    Write-Host "Root $TargetingPackPublishRoot"

    .\heat.exe dir `"$TargetingPackPublishRoot`" `
        -nologo `
        -template fragment `
        -sreg -gg `
        -var var.TargetingPackSrc `
        -cg InstallFiles `
        -srd `
        -dr DOTNETHOME `
        -out $InstallFileswsx | Out-Host

    if($LastExitCode -ne 0)
    {
        $Result = $false
        Write-Host "Heat failed with exit code $LastExitCode."
    }

    popd
    return $Result
}

function RunCandle
{
    $Result = $true
    pushd "$WixRoot"

    Write-Host Running candle..
    $AuthWsxRoot =  Join-Path $PackagingRoot "windows\targetingpack"

    $ComponentVersion = $TargetingPackNugetVersion.Replace('-', '_');

    .\candle.exe -nologo `
        -out "$WixObjRoot\" `
        -dTargetingPackSrc="$TargetingPackPublishRoot" `
        -dMicrosoftEula="$PackagingRoot\windows\eula.rtf" `
        -dProductMoniker="$ProductMoniker" `
        -dBuildVersion="$TargetingPackMSIVersion" `
        -dNugetVersion="$TargetingPackNugetVersion" `
        -dComponentVersion="$ComponentVersion" `
        -dTargetArchitecture="$TargetArchitecture" `
        -dUpgradeCode="$TargetingPackUpgradeCode" `
        -arch $Architecture `
        -ext WixDependencyExtension.dll `
        "$AuthWsxRoot\targetingpack.wxs" `
        "$AuthWsxRoot\provider.wxs" `
        $InstallFileswsx | Out-Host

    if($LastExitCode -ne 0)
    {
        $Result = $false
        Write-Host "Candle failed with exit code $LastExitCode."
    }

    popd
    return $Result
}

function RunLight
{
    $Result = $true
    pushd "$WixRoot"

    Write-Host Running light..

    .\light.exe -nologo `
        -ext WixUIExtension.dll `
        -ext WixDependencyExtension.dll `
        -ext WixUtilExtension.dll `
        -cultures:en-us `
        "$WixObjRoot\targetingpack.wixobj" `
        "$WixObjRoot\provider.wixobj" `
        "$InstallFilesWixobj" `
        -out $TargetingPackMSIOutput | Out-Host

    if($LastExitCode -ne 0)
    {
        $Result = $false
        Write-Host "Light failed with exit code $LastExitCode."
    }

    popd
    return $Result
}

if(!(Test-Path $TargetingPackPublishRoot))
{
    throw "$TargetingPackPublishRoot not found"
}

if(!(Test-Path $WixObjRoot))
{
    throw "$WixObjRoot not found"
}

Write-Host "Creating Targeting Pack MSI at $TargetingPackMSIOutput"

if([string]::IsNullOrEmpty($WixRoot))
{
    Exit -1
}

if(-Not (RunHeat))
{
    Exit -1
}

if(-Not (RunCandle))
{
    Exit -1
}

if(-Not (RunLight))
{
    Exit -1
}

if(!(Test-Path $TargetingPackMSIOutput))
{
    throw "Unable to create the Targeting Pack MSI."
}

Write-Host -ForegroundColor Green "Successfully created Targeting Pack MSI - $TargetingPackMSIOutput"

exit $LastExitCode
