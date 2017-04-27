# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

param(
    [Parameter(Mandatory=$true)][string]$SharedFrameworkPublishRoot,
    [Parameter(Mandatory=$true)][string]$SharedFrameworkMSIOutput,
    [Parameter(Mandatory=$true)][string]$WixRoot,
    [Parameter(Mandatory=$true)][string]$ProductMoniker,
    [Parameter(Mandatory=$true)][string]$DotnetMSIVersion,
    [Parameter(Mandatory=$true)][string]$SharedFrameworkNugetName,
    [Parameter(Mandatory=$true)][string]$SharedFrameworkNugetVersion,
    [Parameter(Mandatory=$true)][string]$SharedFrameworkUpgradeCode,
    [Parameter(Mandatory=$true)][string]$Architecture,
    [Parameter(Mandatory=$true)][string]$TargetArchitecture,
    [Parameter(Mandatory=$true)][string]$WixObjRoot
)

$RepoRoot = Convert-Path "$PSScriptRoot\..\..\..\..\.."
$CommonScript = "$RepoRoot\tools-local\scripts\common\_common.ps1"
if(-Not (Test-Path "$CommonScript"))
{
    Exit -1
} 
. "$CommonScript"

$CompressionRoot = Join-Path $RepoRoot "src\pkg\packaging"

$InstallFileswsx = "$WixObjRoot\install-files.wxs"
$InstallFilesWixobj = "$WixObjRoot\install-files.wixobj"


function RunHeat
{
    $result = $true
    pushd "$WixRoot"

    Write-Host Running heat..

    .\heat.exe dir `"$SharedFrameworkPublishRoot`" `
    -nologo `
    -template fragment `
    -sreg -gg `
    -var var.SharedFrameworkSource `
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
    $AuthWsxRoot = Join-Path $CompressionRoot "windows\sharedframework"
    $SharedFrameworkComponentVersion = $SharedFrameworkNugetVersion.Replace('-', '_');

    .\candle.exe -nologo `
        -out "$WixObjRoot\" `
        -dSharedFrameworkSource="$SharedFrameworkPublishRoot" `
        -dMicrosoftEula="$CompressionRoot\osx\sharedframework\resources\en.lproj\eula.rtf" `
        -dProductMoniker="$ProductMoniker" `
        -dFrameworkName="$SharedFrameworkNugetName" `
        -dFrameworkDisplayVersion="$SharedFrameworkNugetVersion" `
        -dFrameworkComponentVersion="$SharedFrameworkComponentVersion" `
        -dFrameworkUpgradeCode="$SharedFrameworkUpgradeCode" `
        -dTargetArchitecture="$TargetArchitecture" `
        -dBuildVersion="$DotnetMSIVersion" `
        -arch $Architecture `
        -ext WixDependencyExtension.dll `
        "$AuthWsxRoot\sharedframework.wxs" `
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
    $CabCache = Join-Path $WixRoot "cabcache"

    .\light.exe -nologo -ext WixUIExtension -ext WixDependencyExtension -ext WixUtilExtension `
        -cultures:en-us `
        "$WixObjRoot\sharedframework.wixobj" `
        "$WixObjRoot\provider.wixobj" `
        "$WixObjRoot\registrykeys.wixobj" `
        "$InstallFilesWixobj" `
        -out $SharedFrameworkMSIOutput | Out-Host

    if($LastExitCode -ne 0)
    {
        $result = $false
        Write-Host "Light failed with exit code $LastExitCode."
    }

    popd
    return $result
}

if(!(Test-Path $SharedFrameworkPublishRoot))
{
    throw "$SharedHostPublishRoot not found"
}

if(!(Test-Path $WixObjRoot))
{
    throw "$WixObjRoot not found"
}

Write-Host "Creating dotnet shared framework MSI at $SharedFrameworkMSIOutput"

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

if(!(Test-Path $SharedFrameworkMSIOutput))
{
    throw "Unable to create the dotnet shared framework msi."
    Exit -1
}

Write-Host -ForegroundColor Green "Successfully created shared framework MSI - $SharedFrameworkMSIOutput"

exit $LastExitCode
