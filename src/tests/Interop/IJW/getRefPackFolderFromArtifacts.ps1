# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
$interopFolder = Split-Path $PSScriptRoot -Parent
$testsFolder = Split-Path $interopFolder -Parent
$srcFolder = Split-Path $testsFolder -Parent
$repoRoot = Split-Path $srcFolder -Parent

$versionPropsFile = "$repoRoot/eng/Versions.props"

$majorVersion = Select-Xml -Path $versionPropsFile -XPath "/Project/PropertyGroup/MajorVersion" | %{$_.Node.InnerText}
$minorVersion = Select-Xml -Path $versionPropsFile -XPath "/Project/PropertyGroup/MinorVersion" | %{$_.Node.InnerText}

$refPackPath = "$repoRoot/artifacts/bin/ref/net$majorVersion.$minorVersion"

if (-not (Test-Path $refPackPath))
{
    Write-Error "Reference assemblies not found in the artifacts folder at '$refPackPath'. Did you build the libs.ref subset? Did the repo layout change?"
    return 1
}

Write-Output "refPackPath=$refPackPath"
