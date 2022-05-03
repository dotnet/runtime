# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

$engNativeFolder = Split-Path $PSScriptRoot -Parent
$engFolder = Split-Path $engNativeFolder -Parent
$repoRoot = Split-Path $engFolder -Parent

. "$repoRoot/eng/common/tools.ps1"

$dotnetRoot = InitializeDotNetCli $true $false

$dotnetSdkVersion = $GlobalJson.tools.dotnet

$sdkBundledVersionsFile = "$dotnetRoot/sdk/$dotnetSdkVersion/Microsoft.NETCoreSdk.BundledVersions.props"

$refPackVersion = Select-Xml -Path $sdkBundledVersionsFile -XPath "/Project/PropertyGroup/BundledNETCoreAppPackageVersion" | %{$_.Node.InnerText}
$refPackTfmVersion = Select-Xml -Path $sdkBundledVersionsFile -XPath "/Project/PropertyGroup/BundledNETCoreAppTargetFrameworkVersion" | %{$_.Node.InnerText}

$refPackPath = "$dotnetRoot/packs/Microsoft.NETCore.App.Ref/$refPackVersion/ref/net$refPackTfmVersion"

if (-not (Test-Path $refPackPath))
{
    Write-Error "Reference assemblies not found in the SDK folder. Did the SDK layout change? Did the SDK change how it describes the bundled runtime version?"
    exit 1
}

Write-Output "refPackPath=$refPackPath"
