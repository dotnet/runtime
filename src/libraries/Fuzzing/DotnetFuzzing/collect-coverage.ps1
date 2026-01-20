# Usage: .\collect-coverage.ps1 JsonDocumentFuzzer .\json-inputs\
param(
    [Parameter(Mandatory=$true, Position=0)]
    [string]$FuzzerName,

    [Parameter(Mandatory=$true, Position=1)]
    [string]$InputPath,

    [Parameter(Mandatory=$false)]
    [string]$OutputDir = "./coverage-report"
)

$ErrorActionPreference = "Stop"

# Use the local dotnet installation from the repository root
$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\..\..\.."))
$dotnetPath = Join-Path $repoRoot ".dotnet\dotnet.exe"

if (-not (Test-Path $dotnetPath)) {
    Write-Error "Local dotnet installation not found at $dotnetPath"
    exit 1
}

Write-Host "Using dotnet: $dotnetPath" -ForegroundColor Cyan

# Check and install required dotnet tools
Write-Host "Checking for required global tools..." -ForegroundColor Cyan
$installedTools = & $dotnetPath tool list -g

$coverletInstalled = $installedTools | Select-String "coverlet.console"
if (-not $coverletInstalled) {
    Write-Host "  Installing coverlet.console..." -ForegroundColor Yellow
    & $dotnetPath tool install --global coverlet.console
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to install coverlet.console"
        exit 1
    }
} else {
    Write-Host "  coverlet.console: installed" -ForegroundColor Gray
}

$reportGeneratorInstalled = $installedTools | Select-String "dotnet-reportgenerator-globaltool"
if (-not $reportGeneratorInstalled) {
    Write-Host "  Installing dotnet-reportgenerator-globaltool..." -ForegroundColor Yellow
    & $dotnetPath tool install -g dotnet-reportgenerator-globaltool
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to install reportgenerator."
        exit 1
    }
} else {
    Write-Host "  dotnet-reportgenerator-globaltool: installed" -ForegroundColor Gray
}

# Build the project
Write-Host "Building the project..." -ForegroundColor Cyan
& $dotnetPath build $PSScriptRoot
if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed"
    exit 1
}

# Get the path to the test assembly
$artifactsPath = Join-Path $repoRoot "artifacts\bin\DotnetFuzzing"
$assemblyPath = (Get-ChildItem -Path $artifactsPath -Recurse -Filter "DotnetFuzzing.dll" | Select-Object -First 1).FullName

if (-not $assemblyPath) {
    Write-Error "Could not find DotnetFuzzing.dll in $artifactsPath"
    exit 1
}

Write-Host "Found assembly: $assemblyPath" -ForegroundColor Green

# Get the list of instrumented assemblies
Write-Host "Getting instrumented assemblies for $FuzzerName..." -ForegroundColor Cyan
$instrumentedAssemblies = & $dotnetPath run --project $PSScriptRoot --no-build -- $FuzzerName --get-instrumented-assemblies
if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to get instrumented assemblies"
    exit 1
}

$instrumentedAssembliesArray = $instrumentedAssemblies | Where-Object { $_ -ne "" }

if ($instrumentedAssembliesArray.Count -eq 0) {
    Write-Error "No instrumented assemblies found for $FuzzerName"
    exit 1
}

Write-Host "Instrumented assemblies:" -ForegroundColor Green

# Parse assembly and type prefix information
# Format: "AssemblyName.dll [prefix1 prefix2 ...]"
$instrumentationTargets = @()
foreach ($line in $instrumentedAssembliesArray) {
    $parts = $line -split ' ', 2
    $dllName = $parts[0]
    $prefixes = if ($parts.Length -gt 1) { $parts[1].Trim() } else { "" }

    $instrumentationTargets += @{
        DllName = $dllName
        Prefixes = $prefixes
    }

    if ($prefixes) {
        Write-Host "  - $dllName (types: $prefixes)" -ForegroundColor Gray
    } else {
        Write-Host "  - $dllName" -ForegroundColor Gray
    }
}

# Copy PDB files for instrumented assemblies to the DotnetFuzzing output directory
# so coverlet can find them alongside the DLLs
Write-Host "Copying PDB files for instrumented assemblies..." -ForegroundColor Cyan

# Find the runtime directory (contains library assemblies)
$runtimeBaseDir = Join-Path $repoRoot "artifacts\bin\runtime"
$runtimeDir = Get-ChildItem -Path $runtimeBaseDir -Directory -Filter "net*" -ErrorAction SilentlyContinue | Select-Object -First 1 -ExpandProperty FullName

if (-not $runtimeDir) {
    Write-Error "Runtime directory not found in $runtimeBaseDir"
    exit 1
}

Write-Host "  Using runtime directory: $runtimeDir" -ForegroundColor Gray

# Find the CoreCLR IL directory (contains System.Private.CoreLib)
$coreClrBaseDir = Join-Path $repoRoot "artifacts\bin\coreclr"
$coreClrDir = $null
if (Test-Path $coreClrBaseDir) {
    $coreClrDir = Get-ChildItem -Path $coreClrBaseDir -Directory -Recurse -Filter "IL" -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName -match "windows\.[^.]+\.(Debug|Checked|Release)" } |
        Select-Object -First 1 -ExpandProperty FullName
}

if (-not $coreClrDir) {
    Write-Error "CoreCLR IL directory not found in $coreClrBaseDir"
    exit 1
}

Write-Host "  Using CoreCLR directory: $coreClrDir" -ForegroundColor Gray

$assemblyDir = Split-Path $assemblyPath -Parent

foreach ($target in $instrumentationTargets) {
    $pdbName = [System.IO.Path]::ChangeExtension($target.DllName, ".pdb")
    $destPdb = Join-Path $assemblyDir $pdbName

    # Try runtime directory first
    $sourcePdb = Join-Path $runtimeDir $pdbName

    # For System.Private.CoreLib, check the coreclr IL directory
    if (-not (Test-Path $sourcePdb) -and $pdbName -eq "System.Private.CoreLib.pdb") {
        $sourcePdb = Join-Path $coreClrDir $pdbName
    }

    if (Test-Path $sourcePdb) {
        Copy-Item -Path $sourcePdb -Destination $destPdb -Force
        Write-Host "  Copied $pdbName" -ForegroundColor Gray
    } else {
        Write-Warning "PDB not found: $sourcePdb"
    }
}

# Create output directory
New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null

# Build include filters for coverlet
# Format: [AssemblyName]Type.Prefix* for specific prefixes, or [AssemblyName]* for all types
$includeFilters = @()
foreach ($target in $instrumentationTargets) {
    $assemblyName = [System.IO.Path]::GetFileNameWithoutExtension($target.DllName)

    if ($target.Prefixes) {
        # If type prefixes are specified, create filters for each prefix
        $prefixList = $target.Prefixes -split ' '
        foreach ($prefix in $prefixList) {
            if ($prefix) {
                $includeFilters += "[${assemblyName}]${prefix}*"
            }
        }
    } else {
        # No prefixes specified, include all types from the assembly
        $includeFilters += "[${assemblyName}]*"
    }
}

# Build coverlet arguments
$coverletArgs = @(
    $assemblyPath,
    "--target", $dotnetPath,
    "--targetargs", "run --project $PSScriptRoot --no-build -- $FuzzerName $InputPath",
    "--output", "$OutputDir/",
    "--format", "opencover",
    "--use-source-link",
    "--does-not-return-attribute", "DoesNotReturn"
)

# Exclude files based on runtime repository defaults
# From src/libraries/Directory.Build.props
$librariesRoot = Join-Path $repoRoot "src\libraries"
$excludeByFile = @(
    [System.IO.Path]::Combine($librariesRoot, "Common", "src", "System", "SR.*"),
    [System.IO.Path]::Combine($librariesRoot, "Common", "src", "System", "NotImplemented.cs")
)

# Also exclude source-generated files
$excludeByFile += [System.IO.Path]::Combine($repoRoot, "artifacts", "obj", "**", "*.g.cs")

foreach ($filePattern in $excludeByFile) {
    $coverletArgs += "--exclude-by-file"
    $coverletArgs += $filePattern
}

# Add include filters
foreach ($filter in $includeFilters) {
    $coverletArgs += "--include"
    $coverletArgs += $filter
}

# Run coverlet
Write-Host ""
Write-Host "Collecting coverage..." -ForegroundColor Cyan
Write-Host "Running: coverlet $($coverletArgs -join ' ')" -ForegroundColor Gray
Write-Host ""
& coverlet @coverletArgs

if ($LASTEXITCODE -ne 0) {
    Write-Error "Coverage collection failed with exit code $LASTEXITCODE"
    exit $LASTEXITCODE
}

Write-Host ""
Write-Host "Coverage report generated in: $OutputDir" -ForegroundColor Green

# Find the generated OpenCover XML file
$coverageFile = Get-ChildItem -Path $OutputDir -Filter "*.opencover.xml" | Select-Object -First 1
if ($coverageFile) {
    Write-Host "  - OpenCover report: $($coverageFile.FullName)" -ForegroundColor Gray
}

# Generate HTML report using ReportGenerator
Write-Host ""
Write-Host "Generating HTML report..." -ForegroundColor Cyan

if ($coverageFile) {
    $htmlOutputDir = Join-Path $OutputDir "html"
    & reportgenerator "-reports:$($coverageFile.FullName)" "-targetdir:$htmlOutputDir" "-reporttypes:Html"

    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "HTML report generated: $htmlOutputDir\index.html" -ForegroundColor Green
    } else {
        Write-Warning "Failed to generate HTML report"
    }
}
