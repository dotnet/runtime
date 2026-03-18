<#
.SYNOPSIS
    Build and test the cDAC GC stress verification mode (GCSTRESS_CDAC = 0x20).

.DESCRIPTION
    This script:
      1. Builds CoreCLR native + cDAC tools (incremental)
      2. Generates core_root layout
      3. Compiles a small managed test app
      4. Runs the test with DOTNET_GCStress=0x24 (instruction-level JIT stress + cDAC verification)

    Supports Windows, Linux, and macOS.

.PARAMETER Configuration
    Runtime configuration: Checked (default) or Debug.

.PARAMETER FailFast
    If set, assert on cDAC/runtime mismatch. Otherwise log and continue.

.PARAMETER SkipBuild
    Skip the build step (use existing artifacts).

.EXAMPLE
    ./test-cdac-gcstress.ps1
    ./test-cdac-gcstress.ps1 -Configuration Debug -FailFast
    ./test-cdac-gcstress.ps1 -SkipBuild
#>
param(
    [ValidateSet("Checked", "Debug")]
    [string]$Configuration = "Checked",

    [switch]$FailFast,

    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"
$repoRoot = $PSScriptRoot

# Resolve repo root — walk up from script location to find build script
$buildScript = if ($IsWindows -or $env:OS -eq "Windows_NT") { "build.cmd" } else { "build.sh" }
while ($repoRoot -and !(Test-Path (Join-Path $repoRoot $buildScript))) {
    $parent = Split-Path $repoRoot -Parent
    if ($parent -eq $repoRoot) { $repoRoot = $null; break }
    $repoRoot = $parent
}
if (-not $repoRoot) {
    Write-Error "Could not find repo root ($buildScript). Place this script inside the runtime repo."
    exit 1
}

# Detect platform
$isWin = ($IsWindows -or $env:OS -eq "Windows_NT")
$osName = if ($isWin) { "windows" } elseif ($IsMacOS) { "osx" } else { "linux" }
$arch = [System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture.ToString().ToLowerInvariant()
# Map .NET arch names to runtime conventions
$arch = switch ($arch) {
    "x64"   { "x64" }
    "arm64" { "arm64" }
    "arm"   { "arm" }
    "x86"   { "x86" }
    default { "x64" }
}

$platformId = "$osName.$arch"
$coreRoot = Join-Path $repoRoot "artifacts" "tests" "coreclr" "$platformId.$Configuration" "Tests" "Core_Root"
$testDir  = Join-Path $repoRoot "artifacts" "tests" "coreclr" "$platformId.$Configuration" "Tests" "cdacgcstresstest"
$buildCmd = Join-Path $repoRoot $buildScript
$dotnetName = if ($isWin) { "dotnet.exe" } else { "dotnet" }
$corerunName = if ($isWin) { "corerun.exe" } else { "corerun" }
$dotnetExe = Join-Path $repoRoot ".dotnet" $dotnetName
$corerunExe = Join-Path $coreRoot $corerunName
$cdacDll = if ($isWin) { "mscordaccore_universal.dll" } elseif ($IsMacOS) { "libmscordaccore_universal.dylib" } else { "libmscordaccore_universal.so" }

Write-Host "=== cDAC GC Stress Test ===" -ForegroundColor Cyan
Write-Host "  Repo root:     $repoRoot"
Write-Host "  Platform:      $platformId"
Write-Host "  Configuration: $Configuration"
Write-Host "  FailFast:      $FailFast"
Write-Host ""

# ---------------------------------------------------------------------------
# Step 1: Build
# ---------------------------------------------------------------------------
if (-not $SkipBuild) {
    Write-Host ">>> Step 1: Building CoreCLR native + cDAC tools ($Configuration)..." -ForegroundColor Yellow
    Push-Location $repoRoot
    try {
        $buildArgs = @("-subset", "clr.native+tools.cdac", "-c", $Configuration, "-rc", $Configuration, "-lc", "Release", "-bl")
        & $buildCmd @buildArgs
        if ($LASTEXITCODE -ne 0) { Write-Error "Build failed with exit code $LASTEXITCODE"; exit 1 }
    } finally {
        Pop-Location
    }

    Write-Host ">>> Step 1b: Generating core_root layout..." -ForegroundColor Yellow
    $testBuildScript = if ($isWin) {
        Join-Path $repoRoot "src" "tests" "build.cmd"
    } else {
        Join-Path $repoRoot "src" "tests" "build.sh"
    }
    & $testBuildScript $Configuration generatelayoutonly -SkipRestorePackages /p:LibrariesConfiguration=Release
    if ($LASTEXITCODE -ne 0) { Write-Error "Core_root generation failed"; exit 1 }
} else {
    Write-Host ">>> Step 1: Skipping build (--SkipBuild)" -ForegroundColor DarkGray
    if (!(Test-Path $corerunExe)) {
        Write-Error "Core_root not found at $coreRoot. Run without -SkipBuild first."
        exit 1
    }
}

# Verify cDAC library exists
if (!(Test-Path (Join-Path $coreRoot $cdacDll))) {
    Write-Error "$cdacDll not found in core_root. Ensure cDAC was built."
    exit 1
}

# ---------------------------------------------------------------------------
# Step 2: Compile test app
# ---------------------------------------------------------------------------
Write-Host ">>> Step 2: Compiling test app..." -ForegroundColor Yellow
New-Item -ItemType Directory -Force $testDir | Out-Null

$testSource = @"
using System;
using System.Runtime.CompilerServices;

class CdacGcStressTest
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static object AllocAndHold()
    {
        object o = new object();
        string s = "hello world";
        int[] arr = new int[] { 1, 2, 3 };
        GC.KeepAlive(o);
        GC.KeepAlive(s);
        GC.KeepAlive(arr);
        return o;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void NestedCall(int depth)
    {
        object o = new object();
        if (depth > 0)
            NestedCall(depth - 1);
        GC.KeepAlive(o);
    }

    static int Main()
    {
        Console.WriteLine("Starting cDAC GC Stress test...");
        for (int i = 0; i < 5; i++)
        {
            AllocAndHold();
            NestedCall(3);
        }
        Console.WriteLine("cDAC GC Stress test completed successfully.");
        return 100;
    }
}
"@
$testCs  = Join-Path $testDir "test.cs"
$testDll = Join-Path $testDir "test.dll"

Set-Content $testCs $testSource

$cscPath = Get-ChildItem (Join-Path $repoRoot ".dotnet" "sdk") -Recurse -Filter "csc.dll" | Select-Object -First 1
if (-not $cscPath) { Write-Error "Could not find csc.dll in .dotnet SDK"; exit 1 }

$sysRuntime  = Join-Path $coreRoot "System.Runtime.dll"
$sysConsole  = Join-Path $coreRoot "System.Console.dll"
$sysCoreLib  = Join-Path $coreRoot "System.Private.CoreLib.dll"

& $dotnetExe exec $cscPath.FullName `
    "/out:$testDll" /target:exe /nologo `
    "/r:$sysRuntime" `
    "/r:$sysConsole" `
    "/r:$sysCoreLib" `
    $testCs
if ($LASTEXITCODE -ne 0) { Write-Error "Test compilation failed"; exit 1 }

# ---------------------------------------------------------------------------
# Step 3: Run baseline (no GCStress) to verify test works
# ---------------------------------------------------------------------------
Write-Host ">>> Step 3: Running baseline (no GCStress)..." -ForegroundColor Yellow
$env:CORE_ROOT = $coreRoot

# Clear any leftover GCStress env vars
Remove-Item Env:\DOTNET_GCStress -ErrorAction SilentlyContinue
Remove-Item Env:\DOTNET_GCStressCdacFailFast -ErrorAction SilentlyContinue
Remove-Item Env:\DOTNET_ContinueOnAssert -ErrorAction SilentlyContinue

& $corerunExe (Join-Path $testDir "test.dll")
if ($LASTEXITCODE -ne 100) {
    Write-Error "Baseline test failed with exit code $LASTEXITCODE (expected 100)"
    exit 1
}
Write-Host "  Baseline passed." -ForegroundColor Green

# ---------------------------------------------------------------------------
# Step 4: Run with GCStress=0x4 only (no cDAC) to verify GCStress works
# ---------------------------------------------------------------------------
Write-Host ">>> Step 4: Running with GCStress=0x4 (baseline, no cDAC)..." -ForegroundColor Yellow
$env:DOTNET_GCStress = "0x4"
$env:DOTNET_ContinueOnAssert = "1"

& $corerunExe (Join-Path $testDir "test.dll")
if ($LASTEXITCODE -ne 100) {
    Write-Error "GCStress=0x4 baseline failed with exit code $LASTEXITCODE (expected 100)"
    exit 1
}
Write-Host "  GCStress=0x4 baseline passed." -ForegroundColor Green

# ---------------------------------------------------------------------------
# Step 5: Run with GCStress=0x24 (instruction JIT + cDAC verification)
# ---------------------------------------------------------------------------
Write-Host ">>> Step 5: Running with GCStress=0x24 (cDAC verification)..." -ForegroundColor Yellow
$logFile = Join-Path $testDir "cdac-gcstress-results.txt"
$env:DOTNET_GCStress = "0x24"
$env:DOTNET_GCStressCdacFailFast = if ($FailFast) { "1" } else { "0" }
$env:DOTNET_GCStressCdacLogFile = $logFile
$env:DOTNET_ContinueOnAssert = "1"

& $corerunExe (Join-Path $testDir "test.dll")
$testExitCode = $LASTEXITCODE

# ---------------------------------------------------------------------------
# Cleanup
# ---------------------------------------------------------------------------
Remove-Item Env:\DOTNET_GCStress -ErrorAction SilentlyContinue
Remove-Item Env:\DOTNET_GCStressCdacFailFast -ErrorAction SilentlyContinue
Remove-Item Env:\DOTNET_GCStressCdacLogFile -ErrorAction SilentlyContinue
Remove-Item Env:\DOTNET_ContinueOnAssert -ErrorAction SilentlyContinue

# ---------------------------------------------------------------------------
# Report results
# ---------------------------------------------------------------------------
Write-Host ""
if ($testExitCode -eq 100) {
    Write-Host "=== ALL TESTS PASSED ===" -ForegroundColor Green
    Write-Host "  cDAC GC stress verification completed successfully."
    Write-Host "  GCStress=0x24 ran without fatal errors."
} else {
    Write-Host "=== TEST FAILED ===" -ForegroundColor Red
    Write-Host "  GCStress=0x24 test exited with code $testExitCode (expected 100)."
}

if (Test-Path $logFile) {
    Write-Host ""
    Write-Host "Results written to: $logFile" -ForegroundColor Cyan
    Write-Host ""
    Get-Content $logFile | Select-Object -Last 20
}

exit $(if ($testExitCode -eq 100) { 0 } else { 1 })
