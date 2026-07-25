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

# The actual path to assemblies is defined by the information in data/FrameworkList.xml, but we don't need to read that.  Instead just find the path to System.Runtime.dll and use its folder.
$refPackBase = "$dotnetRoot/packs/Microsoft.NETCore.App.Ref/$refPackVersion/ref"
$systemRuntimeDll = Get-ChildItem -Path $refPackBase -Recurse -Filter "System.Runtime.dll" | Select-Object -First 1

if (-not $systemRuntimeDll)
{
    Write-Error "Reference assemblies not found in the SDK folder. Did the SDK layout change? Did the SDK change how it describes the bundled runtime version?"
    exit 1
}

$refPackPath = Split-Path $systemRuntimeDll.FullName -Parent

Write-Output "refPackPath=$refPackPath"
