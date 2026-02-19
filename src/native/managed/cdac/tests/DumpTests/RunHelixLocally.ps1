<#
.SYNOPSIS
    Simulates the Helix cDAC dump test workflow locally.

.DESCRIPTION
    This script reproduces what the Helix work item does in CI:
      1. Build the DumpTests project + debuggees (SkipDumpGeneration, PrepareHelixPayload)
      2. Simulate Helix directory layout (correlation payload = testhost, work item payload = tests + debuggees)
      3. Run each debuggee to produce crash dumps (using the testhost dotnet)
      4. Run the dump tests using xunit.console.dll (same command as Helix)

    This validates the full Helix pipeline locally before pushing to CI.

.PARAMETER SkipBuild
    Skip the build and payload preparation steps (reuse existing payload).

.PARAMETER SkipDumpGeneration
    Skip running the debuggees to generate dumps (reuse existing dumps).

.PARAMETER Filter
    xUnit trait or method filter passed to xunit.console.dll.
    Example: -Filter "-trait category=failing"

.PARAMETER TestHostConfiguration
    Configuration of the testhost. Default: Release.

.EXAMPLE
    .\RunHelixLocally.ps1

.EXAMPLE
    .\RunHelixLocally.ps1 -SkipBuild

.EXAMPLE
    .\RunHelixLocally.ps1 -SkipBuild -SkipDumpGeneration -Filter "-method *StackWalk*"
#>

[CmdletBinding()]
param(
    [switch]$SkipBuild,
    [switch]$SkipDumpGeneration,
    [string]$Filter = "",
    [string]$TestHostConfiguration = "Release"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# --- Resolve paths ---
function Find-RepoRoot([string]$startDir) {
    $dir = $startDir
    while ($dir) {
        if (Test-Path (Join-Path $dir "global.json")) {
            return $dir
        }
        $parent = Split-Path -Parent $dir
        if ($parent -eq $dir) { break }
        $dir = $parent
    }
    Write-Error "Could not find repository root (no global.json found above $startDir)"
    exit 1
}

$repoRoot = Find-RepoRoot $PSScriptRoot
$dotnet = Join-Path $repoRoot ".dotnet\dotnet.exe"
$dumpTestsProj = Join-Path $PSScriptRoot "Microsoft.Diagnostics.DataContractReader.DumpTests.csproj"

if (-not (Test-Path $dotnet)) {
    Write-Error "Repo dotnet not found at $dotnet. Run build.cmd first."
    exit 1
}

# --- Find testhost (simulates HELIX_CORRELATION_PAYLOAD) ---
$testhostParent = Join-Path $repoRoot "artifacts\bin\testhost"
$testhostDir = Get-ChildItem -Directory -Path (Join-Path $testhostParent "net*") -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $testhostDir) {
    Write-Error "No testhost found under $testhostParent. Run 'build.cmd clr+libs' first."
    exit 1
}
$testhostDotnet = Join-Path $testhostDir.FullName "dotnet.exe"
if (-not (Test-Path $testhostDotnet)) {
    Write-Error "dotnet.exe not found in testhost: $($testhostDir.FullName)"
    exit 1
}

# --- Simulated Helix directories ---
$helixRoot = Join-Path $repoRoot "artifacts\tmp\helix-local"
$correlationPayload = $testhostDir.FullName   # testhost IS the correlation payload
$workItemPayload = Join-Path $helixRoot "workitem"
$dumpsDir = Join-Path $workItemPayload "dumps"
$testsDir = Join-Path $workItemPayload "tests"
$debuggeesDir = Join-Path $workItemPayload "debuggees"

# --- Debuggee list (must match DumpTests.targets and cdac-dump-helix.proj) ---
$debuggees = @("BasicThreads", "TypeHierarchy", "ExceptionState", "MultiModule", "GCRoots", "ServerGC", "StackWalk")

Write-Host ""
Write-Host "=== cDAC Helix Local Simulation ===" -ForegroundColor Cyan
Write-Host "  Repo root:       $repoRoot"
Write-Host "  TestHost:        $($testhostDir.FullName)"
Write-Host "  Helix root:      $helixRoot"
Write-Host "  Skip build:      $SkipBuild"
Write-Host "  Skip dump gen:   $SkipDumpGeneration"
if ($Filter) { Write-Host "  Filter:          $Filter" }
Write-Host ""

# ============================================================
# Step 1: Build and prepare Helix payload
# ============================================================
if (-not $SkipBuild) {
    Write-Host "--- Step 1: Build + Prepare Helix Payload ---" -ForegroundColor Cyan

    # Clean previous payload (preserve dumps)
    if (Test-Path $testsDir) { Remove-Item $testsDir -Recurse -Force }
    if (Test-Path $debuggeesDir) { Remove-Item $debuggeesDir -Recurse -Force }

    & $dotnet build $dumpTestsProj `
        /tl:off `
        /p:SkipDumpGeneration=true `
        /p:PrepareHelixPayload=true `
        /p:HelixPayloadDir=$workItemPayload `
        /p:SkipDumpVersions=net10.0
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to build and prepare Helix payload."
        exit 1
    }
    Write-Host "Payload prepared at $workItemPayload" -ForegroundColor Green
} else {
    Write-Host "--- Step 1: Skipped (reusing existing payload) ---" -ForegroundColor Yellow
    if (-not (Test-Path $testsDir)) {
        Write-Error "No payload found at $workItemPayload. Run without -SkipBuild first."
        exit 1
    }
}

# ============================================================
# Step 2: Generate crash dumps (simulate Helix dump generation)
# ============================================================
if (-not $SkipDumpGeneration) {
    Write-Host ""
    Write-Host "--- Step 2: Generate Crash Dumps ---" -ForegroundColor Cyan

    foreach ($name in $debuggees) {
        $debuggeeDll = Join-Path $debuggeesDir "$name\$name.dll"
        if (-not (Test-Path $debuggeeDll)) {
            Write-Host "  SKIP $name - DLL not found: $debuggeeDll" -ForegroundColor Yellow
            continue
        }

        $dumpDir = Join-Path $dumpsDir "local\$name"
        $dumpFile = Join-Path $dumpDir "$name.dmp"

        if (Test-Path $dumpFile) {
            $sizeMB = [math]::Round((Get-Item $dumpFile).Length / 1MB, 1)
            Write-Host "  SKIP $name - dump exists (${sizeMB} MB)" -ForegroundColor DarkGray
            continue
        }

        New-Item -ItemType Directory -Path $dumpDir -Force | Out-Null

        Write-Host "  Running $name..." -ForegroundColor White -NoNewline

        $env:DOTNET_DbgEnableMiniDump = "1"
        $env:DOTNET_DbgMiniDumpType = "4"
        $env:DOTNET_DbgMiniDumpName = $dumpFile

        $proc = Start-Process -FilePath $testhostDotnet `
            -ArgumentList "exec", $debuggeeDll `
            -WorkingDirectory $dumpDir `
            -NoNewWindow -PassThru -Wait

        Remove-Item Env:\DOTNET_DbgEnableMiniDump -ErrorAction SilentlyContinue
        Remove-Item Env:\DOTNET_DbgMiniDumpType -ErrorAction SilentlyContinue
        Remove-Item Env:\DOTNET_DbgMiniDumpName -ErrorAction SilentlyContinue

        if (Test-Path $dumpFile) {
            $sizeMB = [math]::Round((Get-Item $dumpFile).Length / 1MB, 1)
            Write-Host " dump created (${sizeMB} MB)" -ForegroundColor Green
        } else {
            Write-Host " FAILED - no dump file" -ForegroundColor Red
        }
    }
} else {
    Write-Host ""
    Write-Host "--- Step 2: Skipped (reusing existing dumps) ---" -ForegroundColor Yellow
}

# ============================================================
# Step 3: Run tests (simulate Helix test execution)
# ============================================================
Write-Host ""
Write-Host "--- Step 3: Run Tests via xunit.console.dll ---" -ForegroundColor Cyan

$xunitConsole = Join-Path $testsDir "xunit.console.dll"
$testDll = Join-Path $testsDir "Microsoft.Diagnostics.DataContractReader.DumpTests.dll"
$runtimeConfig = Join-Path $testsDir "Microsoft.Diagnostics.DataContractReader.DumpTests.runtimeconfig.json"
$depsFile = Join-Path $testsDir "Microsoft.Diagnostics.DataContractReader.DumpTests.deps.json"
$resultsXml = Join-Path $helixRoot "testResults.xml"

foreach ($f in @($xunitConsole, $testDll, $runtimeConfig, $depsFile)) {
    if (-not (Test-Path $f)) {
        Write-Error "Required file missing: $f"
        exit 1
    }
}

# Set CDAC_DUMP_ROOT so tests find the dumps (same as Helix pre-command)
$env:CDAC_DUMP_ROOT = $dumpsDir

$xunitArgs = @(
    "exec"
    "--runtimeconfig", $runtimeConfig
    "--depsfile", $depsFile
    $xunitConsole
    $testDll
    "-xml", $resultsXml
    "-nologo"
)
if ($Filter) {
    $xunitArgs += $Filter.Split(' ')
}

Write-Host "  Command: dotnet exec ... xunit.console.dll DumpTests.dll" -ForegroundColor DarkGray
Write-Host "  CDAC_DUMP_ROOT=$dumpsDir" -ForegroundColor DarkGray
Write-Host ""

& $testhostDotnet @xunitArgs
$testExitCode = $LASTEXITCODE

Remove-Item Env:\CDAC_DUMP_ROOT -ErrorAction SilentlyContinue

# ============================================================
# Results
# ============================================================
Write-Host ""
if ($testExitCode -eq 0) {
    Write-Host "=== ALL TESTS PASSED ===" -ForegroundColor Green
} else {
    Write-Host "=== TESTS FAILED (exit code: $testExitCode) ===" -ForegroundColor Red
}

if (Test-Path $resultsXml) {
    Write-Host "  Results: $resultsXml" -ForegroundColor DarkGray
}

exit $testExitCode
