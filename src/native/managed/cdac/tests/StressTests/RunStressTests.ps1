<#
.SYNOPSIS
    Build and test the cDAC stress verification mode.

.DESCRIPTION
    This script:
      1. Builds CoreCLR native + cDAC tools (incremental)
      2. Generates core_root layout
      3. Builds debuggee test apps from the Debuggees/ folder
      4. Runs each debuggee under corerun with configurable cDAC stress flags

    Supports Windows, Linux, and macOS.

    The DOTNET_CdacStress environment variable controls WHEN verification fires:
      TRIGGERS:
        0x001 = ALLOC — verify at every managed allocation
      MODIFIER:
        0x200 = VERBOSE — rich per-ref diagnostics in the log

    The runtime's own GC root enumeration is the single oracle. Any trigger
    causes cDAC's GetStackReferences output to be compared against it.

.PARAMETER Configuration
    Runtime configuration: Checked (default) or Debug.

.PARAMETER CdacConfiguration
    cDAC build configuration: Release (default) or Checked/Debug.
    Stress runs default to Release because the cDAC is compared against the
    runtime oracle on every trigger; cDAC-side asserts are not the oracle and
    NativeAOT Checked/Debug is roughly 5x slower, dominating stress wall-time.
    Override to Checked/Debug if you want cDAC asserts while reproducing a
    specific failure.

.PARAMETER CdacStress
    Hex value for DOTNET_CdacStress flags. Default: 0x001 (ALLOC).
    Common values:
      0x001 = ALLOC (allocation points only, every hit verified)

.PARAMETER Debuggee
    Which debuggee(s) to run. Default: All.
    Auto-discovered from the Debuggees directory.

.PARAMETER SkipBuild
    Skip the CoreCLR/cDAC build step (use existing artifacts).

.PARAMETER SkipBaseline
    Skip baseline verification steps.

.EXAMPLE
    ./RunStressTests.ps1 -SkipBuild
    ./RunStressTests.ps1 -Debuggee BasicAlloc -SkipBuild
    ./RunStressTests.ps1 -CdacStress 0x201 -SkipBuild              # ALLOC + VERBOSE
#>
param(
    [ValidateSet("Checked", "Debug")]
    [string]$Configuration = "Checked",

    [ValidateSet("Release", "Checked", "Debug")]
    [string]$CdacConfiguration = "Release",

    [string]$CdacStress = "0x001",

    [string[]]$Debuggee = @(),

    [switch]$SkipBuild,

    [switch]$SkipBaseline
)

$ErrorActionPreference = "Stop"
$scriptDir = $PSScriptRoot
$repoRoot = $scriptDir

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
$arch = switch ($arch) {
    "x64"   { "x64" }
    "arm64" { "arm64" }
    "arm"   { "arm" }
    "x86"   { "x86" }
    default { throw "Unsupported architecture: $arch" }
}

$platformId = "$osName.$arch"
$coreRoot = Join-Path $repoRoot "artifacts" "tests" "coreclr" "$platformId.$Configuration" "Tests" "Core_Root"
$buildCmd = Join-Path $repoRoot $buildScript
$dotnetName = if ($isWin) { "dotnet.exe" } else { "dotnet" }
$corerunName = if ($isWin) { "corerun.exe" } else { "corerun" }
$dotnetExe = Join-Path $repoRoot ".dotnet" $dotnetName
$corerunExe = Join-Path $coreRoot $corerunName
$cdacDll = if ($isWin) { "mscordaccore_universal.dll" } elseif ($IsMacOS) { "libmscordaccore_universal.dylib" } else { "libmscordaccore_universal.so" }
$debuggeesDir = Join-Path $scriptDir "Debuggees"

# Discover available debuggees
$allDebuggees = Get-ChildItem $debuggeesDir -Directory | Where-Object { Test-Path (Join-Path $_.FullName "*.csproj") } | ForEach-Object { $_.Name }

# Resolve which debuggees to run
if ($Debuggee.Count -eq 0) {
    $selectedDebuggees = $allDebuggees
} else {
    $selectedDebuggees = $Debuggee
    foreach ($d in $selectedDebuggees) {
        if ($d -notin $allDebuggees) {
            Write-Error "Unknown debuggee '$d'. Available: $($allDebuggees -join ', ')"
            exit 1
        }
    }
}

Write-Host "=== cDAC Stress Test ===" -ForegroundColor Cyan
Write-Host "  Repo root:     $repoRoot"
Write-Host "  Platform:      $platformId"
Write-Host "  Configuration: $Configuration"
Write-Host "  CdacConfig:    $CdacConfiguration"
Write-Host "  CdacStress:    $CdacStress"
Write-Host "  Debuggees:     $($selectedDebuggees -join ', ')"
Write-Host ""

# ---------------------------------------------------------------------------
# Step 1: Build CoreCLR + cDAC
# ---------------------------------------------------------------------------
if (-not $SkipBuild) {
    Write-Host ">>> Step 1: Building CoreCLR native ($Configuration) + cDAC tools ($CdacConfiguration)..." -ForegroundColor Yellow
    Push-Location $repoRoot
    try {
        # cDAC (mscordaccore_universal) is built via the 'tools' category, which
        # picks up ToolsConfiguration. Explicitly set it to $CdacConfiguration
        # (default: Release) so the in-process stress framework loads an
        # optimized NAOT shim. Otherwise it falls back to -c $Configuration
        # (default: Checked) and DebugOnlyCodeHolder/contract-asserts dominate
        # the profile and inflate wall time ~5x.
        $buildArgs = @("-subset", "clr.native+tools.cdac", "-c", $Configuration, "-rc", $Configuration, "-lc", "Release", "/p:ToolsConfiguration=$CdacConfiguration", "-bl")
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

    # Copy the cDAC NAOT shim (built into artifacts/bin/coreclr/<os>.<arch>.<CdacConfiguration>/)
    # into core_root. The generatelayoutonly step above populates core_root from
    # the runtime-config sharedFramework but does not include the cDAC binary
    # from a different config. Force-copy ours so the framework loads the right
    # build flavor regardless of -CdacConfiguration.
    $cdacSrc = Join-Path $repoRoot "artifacts" "bin" "coreclr" "$platformId.$CdacConfiguration" $cdacDll
    if (Test-Path $cdacSrc) {
        Copy-Item -Path $cdacSrc -Destination (Join-Path $coreRoot $cdacDll) -Force
        Write-Host "  Copied $cdacDll from $CdacConfiguration build into core_root." -ForegroundColor DarkGray
    } else {
        Write-Warning "$cdacDll not found at $cdacSrc -- core_root may have wrong-config cDAC."
    }
} else {
    Write-Host ">>> Step 1: Skipping build (-SkipBuild)" -ForegroundColor DarkGray
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
# Step 2: Build debuggees
# ---------------------------------------------------------------------------
Write-Host ">>> Step 2: Building debuggees..." -ForegroundColor Yellow
foreach ($d in $selectedDebuggees) {
    $csproj = Get-ChildItem (Join-Path $debuggeesDir $d) -Filter "*.csproj" | Select-Object -First 1
    & $dotnetExe build $csproj.FullName -c Release --nologo -v q
    if ($LASTEXITCODE -ne 0) { Write-Error "Failed to build debuggee '$d'"; exit 1 }
    Write-Host "  Built $d" -ForegroundColor DarkGray
}

# Helper: find the debuggee DLL in the build output
function Find-DebuggeeDll([string]$name) {
    $binDir = Join-Path $repoRoot "artifacts" "bin" "StressTests" $name "Release"
    if (!(Test-Path $binDir)) {
        # Fall back to checking the project output directly
        $projDir = Join-Path $debuggeesDir $name
        $binDir = Join-Path $projDir "bin" "Release"
    }
    $dll = Get-ChildItem $binDir -Recurse -Filter "$name.dll" | Select-Object -First 1
    if (-not $dll) {
        Write-Error "Could not find $name.dll in $binDir"
        exit 1
    }
    return $dll.FullName
}

# Helper: clear stress environment variables
function Clear-StressEnv {
    Remove-Item Env:\DOTNET_CdacStress -ErrorAction SilentlyContinue
    Remove-Item Env:\DOTNET_CdacStressLogFile -ErrorAction SilentlyContinue
    Remove-Item Env:\DOTNET_ContinueOnAssert -ErrorAction SilentlyContinue
}

# Helper: run a debuggee with corerun and return exit code
function Invoke-Debuggee([string]$dllPath) {
    $env:CORE_ROOT = $coreRoot
    & $corerunExe $dllPath
    return $LASTEXITCODE
}

# ---------------------------------------------------------------------------
# Step 3: Run baseline (optional)
# ---------------------------------------------------------------------------
if (-not $SkipBaseline) {
    Write-Host ">>> Step 3: Running baseline (no stress)..." -ForegroundColor Yellow
    Clear-StressEnv
    foreach ($d in $selectedDebuggees) {
        $dll = Find-DebuggeeDll $d
        $ec = Invoke-Debuggee $dll
        if ($ec -ne 100) {
            Write-Error "Baseline failed for '$d' (exit code $ec, expected 100)"
            exit 1
        }
        Write-Host "  $d — baseline passed" -ForegroundColor DarkGray
    }
    Write-Host "  All baselines passed." -ForegroundColor Green
} else {
    Write-Host ">>> Skipping baseline (-SkipBaseline)" -ForegroundColor DarkGray
}

# ---------------------------------------------------------------------------
# Step 4: Run with cDAC stress
# ---------------------------------------------------------------------------
Write-Host ">>> Step 4: Running with CdacStress=$CdacStress..." -ForegroundColor Yellow
$logDir = Join-Path $repoRoot "artifacts" "tests" "coreclr" "$platformId.$Configuration" "Tests" "cdacstresslogs"
New-Item -ItemType Directory -Force $logDir | Out-Null

$totalPasses = 0
$totalFails = 0
$totalKnown = 0
$failedDebuggees = @()
$sw = [System.Diagnostics.Stopwatch]::StartNew()

foreach ($d in $selectedDebuggees) {
    $dll = Find-DebuggeeDll $d
    $logFile = Join-Path $logDir "$d.log"

    Clear-StressEnv
    $env:CORE_ROOT = $coreRoot
    $env:DOTNET_CdacStress = $CdacStress
    $env:DOTNET_CdacStressLogFile = $logFile
    $env:DOTNET_ContinueOnAssert = "1"

    $dSw = [System.Diagnostics.Stopwatch]::StartNew()
    & $corerunExe $dll
    $ec = $LASTEXITCODE
    $dSw.Stop()

    # Parse results
    $passes = 0; $fails = 0; $known = 0
    if (Test-Path $logFile) {
        $logContent = Get-Content $logFile
        $passes = ($logContent | Select-String "^\[PASS\]").Count
        $fails = ($logContent | Select-String "^\[FAIL\]").Count
        $known = ($logContent | Select-String "^\[KNOWN_ISSUE\]").Count
    }

    $totalPasses += $passes
    $totalFails += $fails
    $totalKnown += $known

    $status = if ($ec -eq 100) { "PASS" } else { "FAIL"; $failedDebuggees += $d }
    $color = if ($ec -eq 100 -and $fails -eq 0) { "Green" } elseif ($ec -eq 100) { "Yellow" } else { "Red" }
    $detail = "refs=$passes/$($passes+$fails+$known)"
    if ($known -gt 0) { $detail += " known=$known" }
    Write-Host "  $d — $status ($detail) [$($dSw.Elapsed.ToString('mm\:ss'))]" -ForegroundColor $color
}

$sw.Stop()
Clear-StressEnv

# ---------------------------------------------------------------------------
# Summary
# ---------------------------------------------------------------------------
Write-Host ""
Write-Host "=== Summary ===" -ForegroundColor Cyan
Write-Host "  Elapsed:     $($sw.Elapsed.ToString('mm\:ss'))"
Write-Host "  Stress refs: $totalPasses PASS / $totalFails FAIL" -ForegroundColor $(if ($totalFails -eq 0) { "Green" } else { "Yellow" })
if ($totalKnown -gt 0) {
    Write-Host "  Known issues: $totalKnown (deferred-frame diffs, not real failures)" -ForegroundColor Yellow
}
Write-Host "  Logs:        $logDir"

if ($failedDebuggees.Count -gt 0) {
    Write-Host ""
    Write-Host "=== FAILED: $($failedDebuggees -join ', ') ===" -ForegroundColor Red
    exit 1
} else {
    Write-Host ""
    Write-Host "=== ALL PASSED ===" -ForegroundColor Green
    exit 0
}
