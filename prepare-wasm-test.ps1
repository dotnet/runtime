<#
.SYNOPSIS
    Prepares the IL directory for CoreCLR WASM testing with a two-DLL HelloWorld test app.

.DESCRIPTION
    This script:
    1. Builds a HelperLib class library and a HelloWorld console app that references it
    2. Copies the framework DLLs into the IL directory
    3. Copies the built test DLLs into the IL directory

    Prerequisites: Run the CoreCLR WASM build first:
        .\build.cmd -os browser -c Debug -subset clr+libs

.PARAMETER Configuration
    Build configuration (Debug or Release). Must match the configuration used for the WASM build.

.EXAMPLE
    .\prepare-wasm-test.ps1
    .\prepare-wasm-test.ps1 -Configuration Release
#>
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug"
)

$ErrorActionPreference = "Stop"

$RepoRoot = $PSScriptRoot
$ArtifactsDir = Join-Path $RepoRoot "artifacts"
$WasmBinDir = Join-Path $ArtifactsDir "bin\coreclr\browser.wasm.$Configuration"
$ILDir = Join-Path $WasmBinDir "IL"
$FrameworkDllDir = Join-Path $ArtifactsDir "bin\microsoft.netcore.app.runtime.browser-wasm\$Configuration\runtimes\browser-wasm\lib\net11.0"
$DotnetExe = Join-Path $RepoRoot ".dotnet\dotnet.exe"
$TempDir = Join-Path $ArtifactsDir "tmp\wasm-hello-test"

# --- Validate prerequisites ---

if (-not (Test-Path $DotnetExe)) {
    Write-Error "dotnet not found at $DotnetExe. Run '.\build.cmd -os browser -c $Configuration -subset clr+libs' first."
    exit 1
}

if (-not (Test-Path $FrameworkDllDir)) {
    Write-Error "Framework DLLs not found at $FrameworkDllDir. Run '.\build.cmd -os browser -c $Configuration -subset clr+libs' first."
    exit 1
}

if (-not (Test-Path (Join-Path $WasmBinDir "corerun.js"))) {
    Write-Error "corerun.js not found in $WasmBinDir. Run '.\build.cmd -os browser -c $Configuration -subset clr+libs' first."
    exit 1
}

# --- Create temp workspace with test projects ---

if (Test-Path $TempDir) {
    Remove-Item $TempDir -Recurse -Force
}
New-Item -ItemType Directory -Path $TempDir -Force | Out-Null

# HelperLib project
$helperDir = Join-Path $TempDir "HelperLib"
New-Item -ItemType Directory -Path $helperDir -Force | Out-Null

Set-Content (Join-Path $helperDir "HelperLib.csproj") @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net11.0</TargetFramework>
        <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
  </PropertyGroup>
</Project>
"@

Set-Content (Join-Path $helperDir "Helper.cs") @"
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace HelperLib;

public static class Helper
{
    public static unsafe string DoWork(string context,
        delegate* managed<int, int, int, int, int, int, int, int, int, int, int, int, int, int, int, void> callback,
        int arg1,
        int arg2,
        int arg3,
        int arg4,
        int arg5,
        int arg6,
        int arg7,
        int arg8,
        int arg9,
        int arg10,
        int arg11,
        int arg12,
        int arg13,
        int arg14,
        int arg15)
    {
        callback(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15);
        return `$"[HelperLib] Performed work for: {context} {OtherFunction()}";
    }
    public static string OtherFunction()
    {
        return "in OtherFunction also compiled via R2R";
    }
}
"@

# HelloWorld project
$helloDir = Join-Path $TempDir "HelloWorld"
New-Item -ItemType Directory -Path $helloDir -Force | Out-Null

Set-Content (Join-Path $helloDir "HelloWorld.csproj") @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net11.0</TargetFramework>
    <OutputType>Exe</OutputType>
        <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\HelperLib\HelperLib.csproj" />
  </ItemGroup>
</Project>
"@

Set-Content (Join-Path $helloDir "Program.cs") @"
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using HelperLib;

namespace HelloWorld;

public static class Program
{

    public static unsafe void Main()
    {
        Console.WriteLine("[HelloWorld] Starting up...");

        Console.WriteLine(Helper.DoWork("Call1", &PrintEachArgOnNewLine, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15));
        Console.WriteLine(Helper.DoWork("Call2", &PrintEachArgOnNewLine, 11, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 115));
        Console.WriteLine(Helper.DoWork("Call3", &PrintEachArgOnNewLine, 21, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 115));

        Console.WriteLine("[HelloWorld] All done!");
    }

    public static unsafe void PrintEachArgOnNewLine(
        int a1, int a2, int a3, int a4, int a5,
        int a6, int a7, int a8, int a9, int a10,
        int a11, int a12, int a13, int a14, int a15)
    {
        Console.WriteLine(a1);
        Console.WriteLine(a2);
        Console.WriteLine(a3);
        Console.WriteLine(a4);
        Console.WriteLine(a5);
        Console.WriteLine(a6);
        Console.WriteLine(a7);
        Console.WriteLine(a8);
        Console.WriteLine(a9);
        Console.WriteLine(a10);
        Console.WriteLine(a11);
        Console.WriteLine(a12);
        Console.WriteLine(a13);
        Console.WriteLine(a14);
        Console.WriteLine(a15);
    }
}
"@

# --- Build the test projects ---

Write-Host ""
Write-Host "=== Building test projects ===" -ForegroundColor Cyan

Write-Host "Building HelloWorld (includes HelperLib via ProjectReference)..."
& $DotnetExe build (Join-Path $helloDir "HelloWorld.csproj") -c Debug --nologo -v quiet
if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed."
    exit 1
}
Write-Host "Build succeeded." -ForegroundColor Green

# --- Populate the IL directory ---

Write-Host ""
Write-Host "=== Populating IL directory ===" -ForegroundColor Cyan
Write-Host "  Target: $ILDir"

New-Item -ItemType Directory -Path $ILDir -Force | Out-Null

# Copy framework DLLs (these are the managed BCL assemblies for browser-wasm)
Write-Host "Copying framework DLLs..."
$frameworkDlls = Get-ChildItem "$FrameworkDllDir\*.dll"
$frameworkDlls | Copy-Item -Destination $ILDir -Force
Write-Host "  Copied $($frameworkDlls.Count) framework DLLs"

# Copy built test DLLs (repo Directory.Build.props redirects output to artifacts/bin/<ProjectName>)
$builtOutputDir = Join-Path $ArtifactsDir "bin\HelloWorld\Debug\net11.0"
$helloDll = Join-Path $builtOutputDir "HelloWorld.dll"
$helperDll = Join-Path $builtOutputDir "HelperLib.dll"

if (-not (Test-Path $helloDll)) {
    Write-Error "HelloWorld.dll not found at $builtOutputDir"
    exit 1
}
if (-not (Test-Path $helperDll)) {
    Write-Error "HelperLib.dll not found at $builtOutputDir"
    exit 1
}

Copy-Item $helloDll $ILDir -Force
Copy-Item $helperDll $ILDir -Force
Write-Host "  Copied HelloWorld.dll"
Write-Host "  Copied HelperLib.dll"

# --- Summary ---

$totalDlls = (Get-ChildItem "$ILDir\*.dll").Count

# Convert to Unix-style absolute path for node usage (required even on Windows)
$unixILPath = ($ILDir -replace '\\', '/') -replace '^[A-Za-z]:', ''

Write-Host ""
Write-Host "=== IL directory ready ===" -ForegroundColor Green
Write-Host "  Location : $ILDir"
Write-Host "  Total DLLs: $totalDlls"
Write-Host ""
Write-Host "--- To run with Node.js ---" -ForegroundColor Yellow
Write-Host "  cd $WasmBinDir"
Write-Host "  node ./corerun.js -c $unixILPath $unixILPath/HelloWorld.dll"
Write-Host ""
Write-Host "--- To run in browser ---" -ForegroundColor Yellow
Write-Host "  1. Edit src\coreclr\hosts\corerun\CMakeLists.txt -> set CORERUN_IN_BROWSER to 1"
Write-Host "  2. Rebuild: .\build.cmd -os browser -c $Configuration -subset clr"
Write-Host "  3. Serve:   dotnet-serve --directory `"$WasmBinDir`""
Write-Host "  4. Open corerun.html in your browser"
