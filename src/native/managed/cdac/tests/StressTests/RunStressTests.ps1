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

    The DOTNET_CdacStress environment variable controls WHERE and WHAT is verified:
      WHERE (low nibble):
        0x1 = ALLOC  — verify at allocation points
        0x2 = GC     — verify at GC points
        0x4 = INSTR  — verify at instruction-level GC stress points (requires DOTNET_GCStress)
      WHAT (high nibble):
        0x10 = REFS   — compare GC stack references (cDAC vs runtime)
        0x20 = WALK   — compare stack walk frames (cDAC vs DAC)
        0x40 = USE_DAC — also compare GC refs against DAC
      MODIFIER:
        0x100 = UNIQUE — only verify each IP once

.PARAMETER Configuration
    Runtime configuration: Checked (default) or Debug.

.PARAMETER CdacStress
    Hex value for DOTNET_CdacStress flags. Default: 0x11 (ALLOC|REFS).
    Common values:
      0x11 = ALLOC|REFS (fast, allocation points only)
      0x14 = INSTR|REFS (thorough, requires GCStress)
      0x74 = INSTR|REFS|WALK|USE_DAC (full comparison, slow)

.PARAMETER GCStress
    Hex value for DOTNET_GCStress. Default: empty (disabled).
    Set to 0x4 for instruction-level stress.

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
    ./RunStressTests.ps1 -CdacStress 0x74 -GCStress 0x4       # Full comparison with GCStress
    ./RunStressTests.ps1 -CdacStress 0x114 -SkipBuild          # Unique IPs only
#>
param(
    [ValidateSet("Checked", "Debug")]
    [string]$Configuration = "Checked",

    [string]$CdacStress = "0x11",

    [string]$GCStress = "",

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
Write-Host "  CdacStress:    $CdacStress"
Write-Host "  GCStress:      $(if ($GCStress) { $GCStress } else { '(disabled)' })"
Write-Host "  Debuggees:     $($selectedDebuggees -join ', ')"
Write-Host ""

# ---------------------------------------------------------------------------
# Step 1: Build CoreCLR + cDAC
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
    Remove-Item Env:\DOTNET_GCStress -ErrorAction SilentlyContinue
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
Write-Host ">>> Step 4: Running with CdacStress=$CdacStress$(if ($GCStress) { " GCStress=$GCStress" })..." -ForegroundColor Yellow
$logDir = Join-Path $repoRoot "artifacts" "tests" "coreclr" "$platformId.$Configuration" "Tests" "cdacstresslogs"
New-Item -ItemType Directory -Force $logDir | Out-Null

$totalPasses = 0
$totalFails = 0
$totalWalkOK = 0
$totalWalkMM = 0
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
    if ($GCStress) {
        $env:DOTNET_GCStress = $GCStress
    }

    $dSw = [System.Diagnostics.Stopwatch]::StartNew()
    & $corerunExe $dll
    $ec = $LASTEXITCODE
    $dSw.Stop()

    # Parse results
    $passes = 0; $fails = 0; $walkOK = 0; $walkMM = 0
    if (Test-Path $logFile) {
        $logContent = Get-Content $logFile
        $passes = ($logContent | Select-String "^\[PASS\]").Count
        $fails = ($logContent | Select-String "^\[FAIL\]").Count
        $walkOK = ($logContent | Select-String "WALK_OK").Count
        $walkMM = ($logContent | Select-String "WALK_MISMATCH").Count
    }

    $totalPasses += $passes
    $totalFails += $fails
    $totalWalkOK += $walkOK
    $totalWalkMM += $walkMM

    $status = if ($ec -eq 100) { "PASS" } else { "FAIL"; $failedDebuggees += $d }
    $color = if ($ec -eq 100 -and $fails -eq 0) { "Green" } elseif ($ec -eq 100) { "Yellow" } else { "Red" }
    $detail = "refs=$passes/$($passes+$fails)"
    if ($walkOK -gt 0 -or $walkMM -gt 0) { $detail += " walk=$walkOK/$($walkOK+$walkMM)" }
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
if ($totalWalkOK -gt 0 -or $totalWalkMM -gt 0) {
    Write-Host "  Walk parity: $totalWalkOK OK / $totalWalkMM MISMATCH" -ForegroundColor $(if ($totalWalkMM -eq 0) { "Green" } else { "Yellow" })
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
