<#
.SYNOPSIS
    Generates crash dumps and/or runs cDAC dump-based integration tests.

.DESCRIPTION
    This script orchestrates the cDAC dump test workflow on Windows:
      1. Invoke MSBuild GenerateAllDumps target to build debuggees and produce crash dumps
      2. Build and run the dump analysis tests

    Alternatively, use -DumpArchive to import a tar.gz archive of dumps
    downloaded from CI and run the tests against those dumps.

    Dump types are controlled per-debuggee via the DumpTypes property in each
    debuggee's csproj (default: Heap, set in Debuggees/Directory.Build.props).

    Dumps are written to: artifacts\dumps\cdac\{version}\{dumptype}\{debuggee}\
    The script must be run from the DumpTests directory.

.PARAMETER Action
    What to do: "dumps" (generate only), "test" (run tests only), or "all" (both).
    Default: "all"

.PARAMETER Versions
    Comma-separated list of runtime versions to target.
    Supported values: "local", "net10.0", or "all" for both.
    Default: "all"

.PARAMETER Force
    When set, deletes existing dumps before regenerating.

.PARAMETER TestHostConfiguration
    Configuration of the testhost used for the "local" runtime version.
    Default: "Release"

.PARAMETER Filter
    Glob-style filter for test names. Uses substring matching.
    Examples: "*StackWalk*", "*Thread*", "*GC_Heap*"
    Default: "" (run all tests)

.PARAMETER DumpArchive
    Path to a tar.gz archive of dumps downloaded from CI.
    When specified, the archive is extracted and tests are run against
    the extracted dumps. Skips dump generation entirely.
    The archive should contain: {version}/{dumptype}/{debuggee}/{debuggee}.dmp

.PARAMETER SetSignatureCheck
    When set, configures the DisableAuxProviderSignatureCheck registry key
    (Windows 11+ / Server 2022+) to allow heap dumps with an unsigned DAC.
    Requires running as Administrator. Used by CI and for first-time local setup.

.EXAMPLE
    .\RunDumpTests.ps1

.EXAMPLE
    .\RunDumpTests.ps1 -Action dumps -Versions net10.0

.EXAMPLE
    .\RunDumpTests.ps1 -Force

.EXAMPLE
    .\RunDumpTests.ps1 -Action test -Versions local

.EXAMPLE
    .\RunDumpTests.ps1 -Filter "*StackWalk*"

.EXAMPLE
    .\RunDumpTests.ps1 -DumpArchive C:\Downloads\CdacDumps_linux_x64.tar.gz

.EXAMPLE
    .\RunDumpTests.ps1 -SetSignatureCheck
#>

[CmdletBinding()]
param(
    [ValidateSet("dumps", "test", "all")]
    [string]$Action = "all",

    [string]$Versions = "all",

    [switch]$Force,

    [string]$TestHostConfiguration = "Release",

    [string]$Filter = "",

    [string]$DumpArchive = "",

    [switch]$SetSignatureCheck
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

# --- Debuggees and versions ---
$allVersions = @("local", "net10.0")

# --- DumpArchive mode: extract and test CI dumps ---
if ($DumpArchive) {
    if (-not (Test-Path $DumpArchive)) {
        Write-Error "Dump archive not found: $DumpArchive"
        exit 1
    }

    $extractDir = Join-Path $repoRoot "artifacts\dumps\ci-imported"
    if (Test-Path $extractDir) {
        Write-Host "Cleaning previous import: $extractDir" -ForegroundColor Yellow
        Remove-Item $extractDir -Recurse -Force
    }
    New-Item -ItemType Directory -Path $extractDir -Force | Out-Null

    Write-Host ""
    Write-Host "=== cDAC Dump Tests (CI Archive) ===" -ForegroundColor Cyan
    Write-Host "  Archive: $DumpArchive"
    Write-Host "  Extract: $extractDir"
    if ($Filter) { Write-Host "  Filter:  $Filter" }
    Write-Host ""

    Write-Host "--- Extracting archive ---" -ForegroundColor Cyan
    tar -xzf $DumpArchive -C $extractDir
    if ($LASTEXITCODE -ne 0) { Write-Error "Failed to extract archive."; exit 1 }

    # List extracted dumps
    $dumpFiles = Get-ChildItem -Path $extractDir -Recurse -Filter "*.dmp"
    if ($dumpFiles.Count -eq 0) {
        Write-Error "No .dmp files found in archive."
        exit 1
    }
    foreach ($f in $dumpFiles) {
        $rel = $f.FullName.Replace("$extractDir\", "")
        $size = [math]::Round($f.Length / 1MB, 1)
        Write-Host "  Found: $rel (${size} MB)" -ForegroundColor Green
    }

    # Detect which versions are present in the archive and skip the rest
    $presentVersions = Get-ChildItem -Path $extractDir -Directory | Select-Object -ExpandProperty Name
    $skipVersions = $allVersions | Where-Object { $_ -notin $presentVersions }
    if ($skipVersions) {
        $skipVersionsStr = $skipVersions -join ";"
        Write-Host "  Versions in archive: $($presentVersions -join ', ')" -ForegroundColor Green
        Write-Host "  Skipping versions:   $($skipVersions -join ', ')" -ForegroundColor Yellow
    }
    else {
        $skipVersionsStr = ""
        Write-Host "  Versions in archive: $($presentVersions -join ', ')" -ForegroundColor Green
    }

    Write-Host ""
    Write-Host "--- Building test project ---" -ForegroundColor Cyan
    $buildArgs = @($dumpTestsProj, "--nologo", "-v", "q")
    if ($skipVersionsStr) {
        $buildArgs += "/p:SkipDumpVersions=$skipVersionsStr"
    }
    & $dotnet build @buildArgs 2>&1 | ForEach-Object { Write-Host "  $_" }
    if ($LASTEXITCODE -ne 0) { Write-Error "Test project build failed."; exit 1 }

    Write-Host ""
    Write-Host "--- Running tests against CI dumps ---" -ForegroundColor Cyan

    $filterExpr = ""
    if ($Filter) {
        $namePattern = $Filter.Replace("*", "")
        $filterExpr = "FullyQualifiedName~$namePattern"
    }

    $env:CDAC_DUMP_ROOT = $extractDir
    $saved = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    if ($filterExpr) {
        & $dotnet test $dumpTestsProj --no-build --filter $filterExpr --logger "console;verbosity=detailed" 2>&1 | ForEach-Object { Write-Host "  $_" }
    }
    else {
        & $dotnet test $dumpTestsProj --no-build --logger "console;verbosity=detailed" 2>&1 | ForEach-Object { Write-Host "  $_" }
    }
    $testExitCode = $LASTEXITCODE
    $ErrorActionPreference = $saved
    Remove-Item Env:\CDAC_DUMP_ROOT -ErrorAction SilentlyContinue

    if ($testExitCode -ne 0) {
        Write-Host ""
        Write-Host "TESTS FAILED" -ForegroundColor Red
        exit 1
    }

    Write-Host ""
    Write-Host "ALL TESTS PASSED" -ForegroundColor Green
    exit 0
}

if ($Versions -eq "all") {
    $selectedVersions = $allVersions
}
else {
    $selectedVersions = @($Versions -split "," | ForEach-Object { $_.Trim() })
    foreach ($v in $selectedVersions) {
        if ($v -notin $allVersions) {
            Write-Error "Unknown version '$v'. Supported: $($allVersions -join ', '), all"
            exit 1
        }
    }
}

Write-Host ""
Write-Host "=== cDAC Dump Tests ===" -ForegroundColor Cyan
Write-Host "  Action:    $Action"
Write-Host "  Versions:  $($selectedVersions -join ', ')"
Write-Host "  Force:     $Force"
if ($Filter) { Write-Host "  Filter:    $Filter" }
Write-Host ""

# --- Force: delete existing dumps ---
if ($Force -and $Action -in @("dumps", "all")) {
    $dumpOutputDir = Join-Path $repoRoot "artifacts\dumps\cdac"
    if (Test-Path $dumpOutputDir) {
        Write-Host "Deleting existing dumps: $dumpOutputDir" -ForegroundColor Yellow
        Remove-Item $dumpOutputDir -Recurse -Force
    }
}

# --- Generate dumps via MSBuild target ---
# Dump generation (including registry setup for heap dumps on Windows) is handled
# entirely by DumpTests.targets. Each debuggee's DumpTypes property controls what
# types are generated (see Debuggees/Directory.Build.props for the default).
if ($Action -in @("dumps", "all")) {
    Write-Host "--- Generating dumps ---" -ForegroundColor Cyan

    $msbuildArgs = @(
        "msbuild", $dumpTestsProj,
        "/t:GenerateAllDumps",
        "/p:TestHostConfiguration=$TestHostConfiguration",
        "/v:minimal"
    )

    if ($selectedVersions.Count -eq 1 -and $selectedVersions[0] -eq "local") {
        $msbuildArgs += "/p:CIDumpVersionsOnly=true"
    }

    if ($SetSignatureCheck) {
        $msbuildArgs += "/p:SetDisableAuxProviderSignatureCheck=true"
    }

    & $dotnet @msbuildArgs 2>&1 | ForEach-Object { Write-Host "  $_" }
    if ($LASTEXITCODE -ne 0) {
        Write-Host ""
        Write-Host "DUMP GENERATION FAILED" -ForegroundColor Red
        exit 1
    }
}

# --- Run tests ---
if ($Action -in @("test", "all")) {
    Write-Host ""
    Write-Host "--- Building test project ---" -ForegroundColor Cyan
    & $dotnet build $dumpTestsProj --nologo -v q 2>&1 | ForEach-Object { Write-Host "  $_" }
    if ($LASTEXITCODE -ne 0) { Write-Error "Test project build failed."; exit 1 }

    Write-Host ""
    Write-Host "--- Running tests ---" -ForegroundColor Cyan

    # Build a filter for the selected versions
    $filters = @()
    foreach ($version in $selectedVersions) {
        $suffix = switch ($version) {
            "local"   { "_Local" }
            "net10.0" { "_Net10" }
        }
        $filters += "FullyQualifiedName~$suffix"
    }
    $filterExpr = $filters -join " | "

    # Apply user-supplied name filter (glob-style: * maps to dotnet test's ~ operator)
    if ($Filter) {
        # Convert glob wildcards to dotnet test FullyQualifiedName contains filter
        $namePattern = $Filter.Replace("*", "")
        $filterExpr = "($filterExpr) & FullyQualifiedName~$namePattern"
    }

    # dotnet test writes failure details to stderr; suppress termination so we see full results.
    $saved = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    & $dotnet test $dumpTestsProj --no-build --filter $filterExpr --logger "console;verbosity=detailed" 2>&1 | ForEach-Object { Write-Host "  $_" }
    $testExitCode = $LASTEXITCODE
    $ErrorActionPreference = $saved

    if ($testExitCode -ne 0) {
        Write-Host ""
        Write-Host "TESTS FAILED" -ForegroundColor Red
        exit 1
    }

    Write-Host ""
    Write-Host "ALL TESTS PASSED" -ForegroundColor Green
}
