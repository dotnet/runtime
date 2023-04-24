# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

$engNativeFolder = Split-Path $PSScriptRoot -Parent
$engFolder = Split-Path $engNativeFolder -Parent
$repoRoot = Split-Path $engFolder -Parent

$versionPropsFile = "$repoRoot/eng/Versions.props"

$majorVersion = Select-Xml -Path $versionPropsFile -XPath "/Project/PropertyGroup/MajorVersion" | %{$_.Node.InnerText}
$minorVersion = Select-Xml -Path $versionPropsFile -XPath "/Project/PropertyGroup/MinorVersion" | %{$_.Node.InnerText}

$refPackPath = "$repoRoot/artifacts/bin/ref/net$majorVersion.$minorVersion"

if (-not (Test-Path $refPackPath))
{
    Write-Error "Reference assemblies not found in the artifacts folder at '$refPackPath'. Did you invoke 'build.cmd libs.sfx+libs.oob /p:RefOnly=true' to make sure that refs are built? Did the repo layout change?"
    exit 1
}

Write-Output "refPackPath=$refPackPath"
