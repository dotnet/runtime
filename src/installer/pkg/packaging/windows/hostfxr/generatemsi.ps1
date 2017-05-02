# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

param(
    [Parameter(Mandatory=$true)][string]$HostFxrPublishRoot,
    [Parameter(Mandatory=$true)][string]$HostFxrMSIOutput,
    [Parameter(Mandatory=$true)][string]$WixRoot,
    [Parameter(Mandatory=$true)][string]$ProductMoniker,
    [Parameter(Mandatory=$true)][string]$HostFxrMSIVersion,
    [Parameter(Mandatory=$true)][string]$HostFxrNugetVersion,
    [Parameter(Mandatory=$true)][string]$Architecture,
    [Parameter(Mandatory=$true)][string]$TargetArchitecture,
    [Parameter(Mandatory=$true)][string]$WixObjRoot,
    [Parameter(Mandatory=$true)][string]$HostFxrUpgradeCode
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
    $result = $true
    pushd "$WixRoot"

    Write-Host Running heat..

    .\heat.exe dir `"$HostFxrPublishRoot`" `
    -nologo `
    -template fragment `
    -sreg -gg `
    -var var.HostFxrSrc `
    -cg InstallFiles `
    -srd `
    -dr DOTNETHOME `
    -out $InstallFileswsx | Out-Host

    if($LastExitCode -ne 0)
    {
        $result = $false
        Write-Host "Heat failed with exit code $LastExitCode."
    }

    popd
    return $result
}

function RunCandle
{
    $result = $true
    pushd "$WixRoot"

    Write-Host Running candle..
    $AuthWsxRoot =  Join-Path $PackagingRoot "windows\hostfxr"

    $ComponentVersion = $HostFxrNugetVersion.Replace('-', '_');

    .\candle.exe -nologo `
        -out "$WixObjRoot\" `
        -dHostFxrSrc="$HostFxrPublishRoot" `
        -dMicrosoftEula="$PackagingRoot\osx\hostfxr\resources\en.lproj\eula.rtf" `
        -dProductMoniker="$ProductMoniker" `
        -dBuildVersion="$HostFxrMSIVersion" `
        -dNugetVersion="$HostFxrNugetVersion" `
        -dComponentVersion="$ComponentVersion" `
        -dTargetArchitecture="$TargetArchitecture" `
        -dUpgradeCode="$HostFxrUpgradeCode" `
        -arch $Architecture `
        -ext WixDependencyExtension.dll `
        "$AuthWsxRoot\hostfxr.wxs" `
        "$AuthWsxRoot\provider.wxs" `
        "$AuthWsxRoot\registrykeys.wxs" `
        $InstallFileswsx | Out-Host

    if($LastExitCode -ne 0)
    {
        $result = $false
        Write-Host "Candle failed with exit code $LastExitCode."
    }

    popd
    return $result
}

function RunLight
{
    $result = $true
    pushd "$WixRoot"

    Write-Host Running light..

    .\light.exe -nologo `
        -ext WixUIExtension.dll `
        -ext WixDependencyExtension.dll `
        -ext WixUtilExtension.dll `
        -cultures:en-us `
        "$WixObjRoot\hostfxr.wixobj" `
        "$WixObjRoot\provider.wixobj" `
        "$WixObjRoot\registrykeys.wixobj" `
        "$InstallFilesWixobj" `
        -out $HostFxrMSIOutput | Out-Host

    if($LastExitCode -ne 0)
    {
        $result = $false
        Write-Host "Light failed with exit code $LastExitCode."
    }

    popd
    return $result
}

if(!(Test-Path $HostFxrPublishRoot))
{
    throw "$SharedHostPublishRoot not found"
}

if(!(Test-Path $WixObjRoot))
{
    throw "$WixObjRoot not found"
}

Write-Host "Creating shared Host FX Resolver MSI at $HostFxrMSIOutput"

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

if(!(Test-Path $HostFxrMSIOutput))
{
    throw "Unable to create the shared host msi."
    Exit -1
}

Write-Host -ForegroundColor Green "Successfully created shared host MSI - $HostFxrMSIOutput"

exit $LastExitCode
