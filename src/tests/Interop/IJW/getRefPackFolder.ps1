# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
$interopFolder = Split-Path $PSScriptRoot -Parent
$testsFolder = Split-Path $interopFolder -Parent
$srcFolder = Split-Path $testsFolder -Parent
$repoRoot = Split-Path $srcFolder -Parent


. "$repoRoot/eng/common/tools.ps1"

$dotnetRoot = InitializeDotNetCli $true $false

$dotnetSdkVersion = $GlobalJson.tools.dotnet

$sdkBundledVersionsFile = "$dotnetRoot/sdk/$dotnetSdkVersion/Microsoft.NETCoreSdk.BundledVersions.props"

$refPackVersion = Select-Xml -Path $sdkBundledVersionsFile -XPath "/Project/PropertyGroup/BundledNETCoreAppPackageVersion" | %{$_.Node.InnerText}
$refPackTfmVersion = Select-Xml -Path $sdkBundledVersionsFile -XPath "/Project/PropertyGroup/BundledNETCoreAppTargetFrameworkVersion" | %{$_.Node.InnerText}

echo "$dotnetRoot/packs/Microsoft.NETCore.App.Ref/$refPackVersion/ref/net$refPackTfmVersion"
