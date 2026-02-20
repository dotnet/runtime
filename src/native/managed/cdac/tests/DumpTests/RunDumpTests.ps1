<#
.SYNOPSIS
    Generates crash dumps and/or runs cDAC dump-based integration tests.

.DESCRIPTION
    This script orchestrates the cDAC dump test workflow on Windows:
      1. Build debuggee apps for the selected runtime version(s)
      2. Run them to produce crash dumps
      3. Build and run the dump analysis tests

    Alternatively, use -DumpArchive to import a tar.gz archive of dumps
    downloaded from CI and run the tests against those dumps.

    NOTE: This script is Windows-only. For cross-platform CI builds,
    the MSBuild-based DumpTests.targets handles platform detection
    automatically via $(HostOS), $(ExeSuffix), and $(PortableTargetRid).

    Dumps are written to: artifacts\dumps\cdac\{version}\{debuggee}\
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
    The archive should contain: {version}/{debuggee}/{debuggee}.dmp

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
#>

[CmdletBinding()]
param(
    [ValidateSet("dumps", "test", "all")]
    [string]$Action = "all",

    [string]$Versions = "all",

    [switch]$Force,

    [string]$TestHostConfiguration = "Release",

    [string]$Filter = "",

    [string]$DumpArchive = ""
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
$dumpOutputDir = Join-Path $repoRoot "artifacts\dumps\cdac"
$debuggeesDir = Join-Path $PSScriptRoot "Debuggees"

if (-not (Test-Path $dotnet)) {
    Write-Error "Repo dotnet not found at $dotnet. Run build.cmd first."
    exit 1
}

# --- Debuggees and versions ---
$allDebugees = @("BasicThreads", "TypeHierarchy", "ExceptionState", "MultiModule", "GCRoots", "ServerGC", "StackWalk")
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
        $rel = $f.FullName.Substring($extractDir.Length + 1)
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
    $selectedVersions = $Versions -split "," | ForEach-Object { $_.Trim() }
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
Write-Host "  Debuggees: $($allDebugees -join ', ')"
Write-Host "  Force:     $Force"
if ($Filter) { Write-Host "  Filter:    $Filter" }
Write-Host ""

# --- Force: delete existing dumps ---
if ($Force -and $Action -in @("dumps", "all")) {
    foreach ($version in $selectedVersions) {
        $versionDumpDir = Join-Path $dumpOutputDir $version
        if (Test-Path $versionDumpDir) {
            Write-Host "Deleting existing dumps: $versionDumpDir" -ForegroundColor Yellow
            Remove-Item $versionDumpDir -Recurse -Force
        }
    }
}

# --- Windows: allow unsigned DAC for heap dumps ---
# Heap dumps (type 2) require the DAC which is unsigned in local builds.
# Set the DisableAuxProviderSignatureCheck registry value so that
# MiniDumpWriteDump accepts the unsigned DAC (Windows 11+ only).
# This mirrors the approach used by dotnet/diagnostics DumpGenerationFixture.
if ($Action -in @("dumps", "all")) {
    $regPath = "HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\MiniDumpSettings"
    try {
        if (-not (Test-Path $regPath)) {
            New-Item -Path $regPath -Force | Out-Null
        }
        Set-ItemProperty -Path $regPath -Name "DisableAuxProviderSignatureCheck" -Value 1 -Type DWord
        Write-Host "  Set DisableAuxProviderSignatureCheck=1 for unsigned DAC support" -ForegroundColor Green
    }
    catch [System.UnauthorizedAccessException] {
        Write-Host "  Warning: Could not set DisableAuxProviderSignatureCheck (run as admin for heap dump support)" -ForegroundColor Yellow
    }
}

# --- Helper: resolve TFM for local builds ---
$localTfm = $null
function Get-LocalTfm {
    if ($null -eq $script:localTfm) {
        $script:localTfm = & $dotnet msbuild $dumpTestsProj /nologo /v:m "/getProperty:NetCoreAppCurrent" 2>$null
        if ([string]::IsNullOrWhiteSpace($script:localTfm)) { $script:localTfm = "net11.0" }
    }
    return $script:localTfm
}

# --- Helper: set dump env vars ---
function Set-DumpEnvVars($dumpFilePath) {
    $env:DOTNET_DbgEnableMiniDump = "1"
    $env:DOTNET_DbgMiniDumpType = "2"
    $env:DOTNET_DbgMiniDumpName = $dumpFilePath
}

function Clear-DumpEnvVars {
    Remove-Item Env:\DOTNET_DbgEnableMiniDump -ErrorAction SilentlyContinue
    Remove-Item Env:\DOTNET_DbgMiniDumpType -ErrorAction SilentlyContinue
    Remove-Item Env:\DOTNET_DbgMiniDumpName -ErrorAction SilentlyContinue
}

# --- Generate dumps ---
if ($Action -in @("dumps", "all")) {
    Write-Host "--- Generating dumps ---" -ForegroundColor Cyan

    foreach ($version in $selectedVersions) {
        foreach ($debuggee in $allDebugees) {
            $dumpFile = Join-Path $dumpOutputDir "$version\$debuggee\$debuggee.dmp"

            if (Test-Path $dumpFile) {
                $size = [math]::Round((Get-Item $dumpFile).Length / 1MB, 1)
                Write-Host "  [$version/$debuggee] Already exists (${size} MB). Use -Force to regenerate." -ForegroundColor DarkGray
                continue
            }

            Write-Host "  [$version/$debuggee] Generating dump..." -ForegroundColor Green
            $debuggeeCsproj = Join-Path $debuggeesDir "$debuggee\$debuggee.csproj"
            $dumpDir = Join-Path $dumpOutputDir "$version\$debuggee"
            New-Item -ItemType Directory -Path $dumpDir -Force | Out-Null

            if ($version -eq "local") {
                $tfm = Get-LocalTfm
                $binDir = Join-Path $repoRoot "artifacts\bin\DumpTests\$debuggee\Release\$tfm"
                $testHostDir = Join-Path $repoRoot "artifacts\bin\testhost\$tfm-windows-$TestHostConfiguration-x64"

                if (-not (Test-Path "$testHostDir\dotnet.exe")) {
                    Write-Error "  [$version/$debuggee] Testhost not found at $testHostDir. Run 'build.cmd clr+libs -rc release' first."
                    exit 1
                }

                & $dotnet build $debuggeeCsproj -c Release -f $tfm --nologo -v q 2>&1 | Out-Null
                if ($LASTEXITCODE -ne 0) { Write-Error "  [$version/$debuggee] Build failed."; exit 1 }

                Set-DumpEnvVars (Join-Path $dumpDir "$debuggee.dmp")
                # The debuggee crashes on purpose (FailFast), so suppress stderr errors.
                $saved = $ErrorActionPreference
                $ErrorActionPreference = "Continue"
                & "$testHostDir\dotnet.exe" exec "$binDir\$debuggee.dll" 2>&1 | ForEach-Object { Write-Host "    $_" }
                $ErrorActionPreference = $saved
                Clear-DumpEnvVars
            }
            else {
                $publishDir = Join-Path $repoRoot "artifacts\bin\DumpTests\$debuggee\Release\$version-publish"

                & $dotnet publish $debuggeeCsproj -c Release -f $version -r win-x64 --self-contained -o $publishDir --nologo -v q 2>&1 | Out-Null
                if ($LASTEXITCODE -ne 0) { Write-Error "  [$version/$debuggee] Publish failed."; exit 1 }

                Set-DumpEnvVars (Join-Path $dumpDir "$debuggee.dmp")
                # The debuggee crashes on purpose (FailFast), so suppress stderr errors.
                $saved = $ErrorActionPreference
                $ErrorActionPreference = "Continue"
                & "$publishDir\$debuggee.exe" 2>&1 | ForEach-Object { Write-Host "    $_" }
                $ErrorActionPreference = $saved
                Clear-DumpEnvVars
            }

            $dumpFile = Join-Path $dumpDir "$debuggee.dmp"
            if (Test-Path $dumpFile) {
                $size = [math]::Round((Get-Item $dumpFile).Length / 1MB, 1)
                Write-Host "  [$version/$debuggee] Dump created (${size} MB)" -ForegroundColor Green
            }
            else {
                Write-Error "  [$version/$debuggee] Dump was not created!"
                exit 1
            }
        }
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
