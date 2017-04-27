# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

param(
    [Parameter(Mandatory=$true)][string]$SharedHostPublishRoot,
    [Parameter(Mandatory=$true)][string]$DotnetHostMSIOutput,
    [Parameter(Mandatory=$true)][string]$WixRoot,
    [Parameter(Mandatory=$true)][string]$ProductMoniker,
    [Parameter(Mandatory=$true)][string]$SharedHostMSIVersion,
    [Parameter(Mandatory=$true)][string]$SharedHostNugetVersion,
    [Parameter(Mandatory=$true)][string]$Architecture,
    [Parameter(Mandatory=$true)][string]$TargetArchitecture,
    [Parameter(Mandatory=$true)][string]$WixObjRoot,
    [Parameter(Mandatory=$true)][string]$SharedHostUpgradeCode
)

$RepoRoot = Convert-Path "$PSScriptRoot\..\..\..\..\.."
$CommonScript = "$RepoRoot\tools-local\scripts\common\_common.ps1"
if(-Not (Test-Path "$CommonScript"))
{
    Exit -1
} 
. "$CommonScript"

$CompressionRoot = Join-Path $RepoRoot "src\pkg\packaging"
function RunCandle
{
    $result = $true
    pushd "$WixRoot"

    Write-Host Running candle..
    $AuthWsxRoot =  Join-Path $CompressionRoot "windows\host"

    .\candle.exe -nologo `
        -out "$WixObjRoot\" `
        -ext WixDependencyExtension.dll `
        -dHostSrc="$SharedHostPublishRoot" `
        -dMicrosoftEula="$CompressionRoot\osx\sharedhost\resources\en.lproj\eula.rtf" `
        -dProductMoniker="$ProductMoniker" `
        -dBuildVersion="$SharedHostMSIVersion" `
        -dNugetVersion="$SharedHostNugetVersion" `
        -dTargetArchitecture="$TargetArchitecture" `
        -dUpgradeCode="$SharedHostUpgradeCode" `
        -arch $Architecture `
        "$AuthWsxRoot\host.wxs" `
        "$AuthWsxRoot\provider.wxs" | Out-Host

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
        "$WixObjRoot\host.wixobj" `
        "$WixObjRoot\provider.wixobj" `
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

Write-Host "Creating shared host MSI at $DotnetHostMSIOutput"

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
