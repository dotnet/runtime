# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

param(
    [Parameter(Mandatory=$true)][string]$SharedFxMSIFile,
    [Parameter(Mandatory=$true)][string]$SharedHostMSIFile,
    [Parameter(Mandatory=$true)][string]$HostFxrMSIFile,
    [Parameter(Mandatory=$true)][string]$DotnetBundleOutput,
    [Parameter(Mandatory=$true)][string]$WixRoot,
    [Parameter(Mandatory=$true)][string]$ProductMoniker,
    [Parameter(Mandatory=$true)][string]$DotnetMSIVersion,
    [Parameter(Mandatory=$true)][string]$DotnetCLIVersion,
    [Parameter(Mandatory=$true)][string]$ProductBandVersion,
    [Parameter(Mandatory=$true)][string]$SharedFrameworkNugetName,
    [Parameter(Mandatory=$true)][string]$SharedFrameworkNugetVersion,
    [Parameter(Mandatory=$true)][string]$SharedFrameworkUpgradeCode,
    [Parameter(Mandatory=$true)][string]$TargetArchitecture,
    [Parameter(Mandatory=$true)][string]$Architecture
)

$RepoRoot = Convert-Path "$PSScriptRoot\..\..\..\..\.."
$CommonScript = "$RepoRoot\tools-local\scripts\common\_common.ps1"
if(-Not (Test-Path "$CommonScript"))
{
    Exit -1
} 
. "$CommonScript"

$PackagingRoot = Join-Path $RepoRoot "src\pkg\packaging"

function RunCandleForBundle
{
    $result = $true
    pushd "$WixRoot"

    Write-Host Running candle for bundle..
    $AuthWsxRoot =  Join-Path $PackagingRoot "windows\sharedframework"
    $SharedFrameworkComponentVersion = $SharedFrameworkNugetVersion.Replace('-', '_');

    $LcidList = (Get-ChildItem "$AuthWsxRoot\theme\*\bundle.wxl").Directory.Name -join ';'

    .\candle.exe -nologo `
        -dMicrosoftEula="$PackagingRoot\windows\eula.rtf" `
        -dProductMoniker="$ProductMoniker" `
        -dBuildVersion="$DotnetMSIVersion" `
        -dDisplayVersion="$DotnetCLIVersion" `
        -dProductBandVersion="$ProductBandVersion" `
        -dSharedFXMsiSourcePath="$SharedFxMSIFile" `
        -dSharedHostMsiSourcePath="$SharedHostMSIFile" `
        -dHostFxrMsiSourcePath="$HostFxrMSIFile" `
        -dFrameworkName="$SharedFrameworkNugetName" `
        -dFrameworkDisplayVersion="$SharedFrameworkNugetVersion" `
        -dFrameworkComponentVersion="$SharedFrameworkComponentVersion" `
        -dFrameworkUpgradeCode="$SharedFrameworkUpgradeCode" `
        -dTargetArchitecture="$TargetArchitecture" `
        -dLcidList="$LcidList" `
        -arch "$Architecture" `
        -ext WixBalExtension.dll `
        -ext WixUtilExtension.dll `
        -ext WixTagExtension.dll `
        "$AuthWsxRoot\bundle.wxs" | Out-Host

    if($LastExitCode -ne 0)
    {
        $result = $false
        Write-Host "Candle failed with exit code $LastExitCode."
    }

    popd
    return $result
}

function RunLightForBundle
{
    $result = $true
    pushd "$WixRoot"

    Write-Host Running light for bundle..
    $AuthWsxRoot =  Join-Path $PackagingRoot "windows\sharedframework"

    $lightLogFile = $null
    $maxRetries = 5
    foreach ($retry in 0..$maxRetries)
    {
        if ($retry)
        {
            # Wait one second per number of retries. This is intended to avoid
            # interference by Windows Defender by giving it time to catch up.
            $sleepTime = $retry
            Write-Host "Retrying after $($sleepTime)s of sleep..."
            Start-Sleep $sleepTime
            Write-Host "Retrying... (#$retry of $maxRetries)"
        }

        $lightLogFile = Join-Path $WixTempPath "runtime-bundle-light-$retry.log"

        .\light.exe -nologo `
            -cultures:en-us `
            bundle.wixobj `
            -ext WixBalExtension.dll `
            -ext WixUtilExtension.dll `
            -ext WixTagExtension.dll `
            -b "$AuthWsxRoot" `
            -out $DotnetBundleOutput > $lightLogFile

        if($LastExitCode -ne 0)
        {
            $result = $false
            
            Write-Host "Light failed with exit code $LastExitCode. Output written to $lightLogFile"
        }
        else
        {
            $result = $true
            break
        }
    }

    if ($lightLogFile)
    {
        Get-Content $lightLogFile | Out-Host
    }

    popd
    return $result
}

Write-Host "Creating shared framework bundle at $DotnetBundleOutput"

if([string]::IsNullOrEmpty($WixRoot))
{
    Exit -1
}

$WixTempPath = Join-Path $WixRoot "wix_temp"
New-Item -ItemType Directory -Path $WixTempPath -Force | Out-Null
$env:WIX_TEMP = $WixTempPath

if(-Not (RunCandleForBundle))
{
    Exit -1
}

if(-Not (RunLightForBundle))
{
    Exit -1
}

if(!(Test-Path $DotnetBundleOutput))
{
    throw "Unable to create the dotnet bundle."
    Exit -1
}

Write-Host -ForegroundColor Green "Successfully created shared framework bundle - $DotnetBundleOutput"

exit $LastExitCode
