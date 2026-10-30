#!/usr/bin/env pwsh
# cdac-dump-inspect.ps1 — Wrapper to build and run the cDAC dump inspection tool.
#
# Usage:
#   ./cdac-dump-inspect.ps1 descriptor <dump-path>   Print contract descriptor
#   ./cdac-dump-inspect.ps1 threads <dump-path>       List managed threads
#   ./cdac-dump-inspect.ps1 stacks <dump-path>        Print managed stack traces

param(
    [Parameter(Position = 0)]
    [ValidateSet("descriptor", "threads", "stacks")]
    [string]$Command,

    [Parameter(Position = 1)]
    [string]$DumpPath,

    [switch]$Release
)

$ErrorActionPreference = "Stop"
$scriptDir = $PSScriptRoot
$repoRoot = Resolve-Path (Join-Path $scriptDir "../../../../..")
$dotnetDir = Join-Path $repoRoot ".dotnet"
$dotnetExe = if ($IsWindows -or $PSVersionTable.PSVersion.Major -lt 6) { "dotnet.exe" } else { "dotnet" }
$projFile = Join-Path $scriptDir "cdac-dump-inspect.csproj"
$config = if ($Release) { "Release" } else { "Debug" }

# Prefer repo-local SDK, fall back to dotnet on PATH.
$localDotnet = Join-Path $dotnetDir $dotnetExe
if (Test-Path $localDotnet) {
    $dotnet = $localDotnet
}
else {
    $dotnet = Get-Command $dotnetExe -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Source
    if (-not $dotnet) {
        Write-Error "dotnet SDK not found. Either run 'build.cmd'/'build.sh' to install the repo-local SDK, or ensure 'dotnet' is on your PATH."
        exit 1
    }
}

if (-not $Command -or -not $DumpPath) {
    Write-Host "Usage: ./cdac-dump-inspect.ps1 <command> <dump-path>"
    Write-Host ""
    Write-Host "Commands:"
    Write-Host "  descriptor   Print the raw contract descriptor (contracts, types, globals)"
    Write-Host "  threads      List managed threads"
    Write-Host "  stacks       Print managed stack traces for all threads"
    Write-Host ""
    Write-Host "Options:"
    Write-Host "  -Release     Build in Release configuration (default: Debug)"
    exit 1
}

if (-not (Test-Path $DumpPath)) {
    Write-Error "Dump not found: $DumpPath"
    exit 1
}

# Build
Write-Host "Building cdac-dump-inspect ($config)..." -ForegroundColor DarkGray
& $dotnet build $projFile -c $config --nologo -v minimal
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# Run
& $dotnet run --project $projFile -c $config --no-build -- $Command $DumpPath
exit $LASTEXITCODE
