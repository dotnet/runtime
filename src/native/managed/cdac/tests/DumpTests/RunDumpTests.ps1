<#
.SYNOPSIS
    Generates crash dumps and/or runs cDAC dump-based integration tests.

.DESCRIPTION
    This script orchestrates the cDAC dump test workflow:
      1. Build debuggee apps for the selected runtime version(s)
      2. Run them to produce crash dumps
      3. Build and run the dump analysis tests

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

.EXAMPLE
    .\RunDumpTests.ps1

.EXAMPLE
    .\RunDumpTests.ps1 -Action dumps -Versions net10.0

.EXAMPLE
    .\RunDumpTests.ps1 -Force

.EXAMPLE
    .\RunDumpTests.ps1 -Action test -Versions local
#>

[CmdletBinding()]
param(
    [ValidateSet("dumps", "test", "all")]
    [string]$Action = "all",

    [string]$Versions = "all",

    [switch]$Force,

    [string]$TestHostConfiguration = "Release"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# --- Resolve paths ---
$repoRoot = Split-Path -Parent (Split-Path -Parent (Split-Path -Parent (Split-Path -Parent (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)))))
$dotnet = Join-Path $repoRoot ".dotnet\dotnet.exe"
$dumpTestsProj = Join-Path $PSScriptRoot "Microsoft.Diagnostics.DataContractReader.DumpTests.csproj"
$dumpOutputDir = Join-Path $repoRoot "artifacts\dumps\cdac"
$debuggeesDir = Join-Path $PSScriptRoot "Debuggees"

if (-not (Test-Path $dotnet)) {
    Write-Error "Repo dotnet not found at $dotnet. Run build.cmd first."
    exit 1
}

# --- Debuggees and versions ---
$allDebugees = @("BasicThreads", "TypeHierarchy", "ExceptionState", "MultiModule", "GCRoots")
$allVersions = @("local", "net10.0")

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
    $env:DOTNET_DbgMiniDumpType = "4"
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
                & "$testHostDir\dotnet.exe" exec "$binDir\$debuggee.dll" 2>&1 | ForEach-Object { Write-Host "    $_" }
                Clear-DumpEnvVars
            }
            else {
                $publishDir = Join-Path $repoRoot "artifacts\bin\DumpTests\$debuggee\Release\$version-publish"

                & $dotnet publish $debuggeeCsproj -c Release -f $version -r win-x64 --self-contained -o $publishDir --nologo -v q 2>&1 | Out-Null
                if ($LASTEXITCODE -ne 0) { Write-Error "  [$version/$debuggee] Publish failed."; exit 1 }

                Set-DumpEnvVars (Join-Path $dumpDir "$debuggee.dmp")
                & "$publishDir\$debuggee.exe" 2>&1 | ForEach-Object { Write-Host "    $_" }
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

    & $dotnet test $dumpTestsProj --no-build --filter $filterExpr --logger "console;verbosity=detailed" 2>&1 | ForEach-Object { Write-Host "  $_" }

    if ($LASTEXITCODE -ne 0) {
        Write-Host ""
        Write-Host "TESTS FAILED" -ForegroundColor Red
        exit 1
    }

    Write-Host ""
    Write-Host "ALL TESTS PASSED" -ForegroundColor Green
}
