# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

param(
    [Parameter(Mandatory=$true)][string]$SharedHostPublishRoot,
    [Parameter(Mandatory=$true)][string]$DotnetHostfxrMSIOutput,
    [Parameter(Mandatory=$true)][string]$WixRoot,
    [Parameter(Mandatory=$true)][string]$ProductMoniker,
    [Parameter(Mandatory=$true)][string]$SharedHostFxrMSIVersion,
    [Parameter(Mandatory=$true)][string]$SharedHostFxrNugetVersion,
    [Parameter(Mandatory=$true)][string]$Architecture,
    [Parameter(Mandatory=$true)][string]$WixObjRoot
)

. "$PSScriptRoot\..\..\..\scripts\common\_common.ps1"
$RepoRoot = Convert-Path "$PSScriptRoot\..\..\.."

function RunCandle
{
    $result = $true
    pushd "$WixRoot"

    Write-Host Running candle..
    $AuthWsxRoot =  Join-Path $RepoRoot "packaging\windows\hostfxr"

    .\candle.exe -nologo `
        -out "$WixObjRoot\" `
        -ext WixDependencyExtension.dll `
        -dHostSrc="$SharedHostPublishRoot" `
        -dMicrosoftEula="$RepoRoot\packaging\osx\sharedhost\resources\en.lproj\eula.rtf" `
        -dProductMoniker="$ProductMoniker" `
        -dBuildVersion="$SharedHostFxrMSIVersion" `
        -dNugetVersion="$SharedHostFxrNugetVersion" `
        -arch $Architecture `
        "$AuthWsxRoot\hostfxr.wxs" `
        "$AuthWsxRoot\provider.wxs" `
        "$AuthWsxRoot\registrykeys.wxs" | Out-Host        

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
        -out $DotnetHostMSIOutput | Out-Host

    if($LastExitCode -ne 0)
    {
        $result = $false
        Write-Host "Light failed with exit code $LastExitCode."
    }

    popd
    return $result
}

if(!(Test-Path $SharedHostPublishRoot))
{
    throw "$SharedHostPublishRoot not found"
}

if(!(Test-Path $WixObjRoot))
{
    throw "$WixObjRoot not found"
}

Write-Host "Creating shared Host FX Resolver MSI at $DotnetHostMSIOutput"

if([string]::IsNullOrEmpty($WixRoot))
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

if(!(Test-Path $DotnetHostMSIOutput))
{
    throw "Unable to create the shared host msi."
    Exit -1
}

Write-Host -ForegroundColor Green "Successfully created shared host MSI - $DotnetHostMSIOutput"

exit $LastExitCode
