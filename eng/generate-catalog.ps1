<#
.SYNOPSIS
    Generates a catalog definition file (.cdf) and catalog file (.cat) for all .js files
    in the specified root directory. Used for VS signing compliance - the .js files are
    customer-modifiable and cannot be directly Authenticode-signed.

.PARAMETER RootPath
    Root directory to search for .js files recursively.

.PARAMETER CatOutputPath
    Full path for the output .cat file.
#>
param(
    [Parameter(Mandatory)][string]$RootPath,
    [Parameter(Mandatory)][string]$CatOutputPath,
    [string]$WindowsSdkDir = '',
    [switch]$ErrorIfMakecatNotFound
)

$ErrorActionPreference = 'Stop'

$cdfPath = [System.IO.Path]::ChangeExtension($CatOutputPath, '.cdf')

$files = Get-ChildItem -Path $RootPath -Recurse -Filter '*.js' -File
if ($files.Count -eq 0) {
    Write-Warning "No .js files found under $RootPath - skipping catalog generation."
    exit 0
}

# Ensure output directory exists before writing files
$catDir = [System.IO.Path]::GetDirectoryName($CatOutputPath)
if (-not (Test-Path $catDir)) {
    New-Item -ItemType Directory -Path $catDir -Force | Out-Null
}

$cdf = @()
$cdf += '[CatalogHeader]'
$cdf += "Name=$CatOutputPath"
$cdf += 'CatalogVersion=2'
$cdf += 'HashAlgorithms=SHA256'
$cdf += ''
$cdf += '[CatalogFiles]'

$i = 0
foreach ($f in $files) {
    $label = "js_${i}_" + ($f.Name -replace '[^\w\.-]', '_')
    $cdf += "<hash>$label=$($f.FullName)"
    $i++
}

$cdf | Set-Content -Path $cdfPath -Encoding ASCII
Write-Host "Generated CDF with $($files.Count) .js files at $cdfPath"

# Find makecat.exe - it ships with the Windows SDK and may not be on PATH.
$makecat = $null
if ($WindowsSdkDir -and (Test-Path $WindowsSdkDir)) {
    $makecat = Get-ChildItem -Path (Join-Path $WindowsSdkDir 'bin') -Recurse -Filter 'makecat.exe' -File |
        Where-Object { $_.DirectoryName -match 'x64' } |
        Sort-Object DirectoryName -Descending |
        Select-Object -First 1
}

if (-not $makecat) {
    $makecat = Get-Command makecat.exe -ErrorAction SilentlyContinue
}

if (-not $makecat) {
    # Fallback: search common Windows SDK locations
    $sdkRoot = "${env:ProgramFiles(x86)}\Windows Kits\10\bin"
    if (Test-Path $sdkRoot) {
        $makecat = Get-ChildItem -Path $sdkRoot -Recurse -Filter 'makecat.exe' -File |
            Where-Object { $_.DirectoryName -match 'x64' } |
            Sort-Object DirectoryName -Descending |
            Select-Object -First 1
    }
}

if (-not $makecat) {
    if ($ErrorIfMakecatNotFound) {
        throw "makecat.exe not found. Catalog signing requires the Windows SDK which must be available in CI builds."
    }
    Write-Warning "makecat.exe not found - skipping catalog generation. Catalog signing requires the Windows SDK."
    exit 0
}

$makecatPath = if ($makecat -is [System.Management.Automation.CommandInfo]) { $makecat.Source } else { $makecat.FullName }
Write-Host "Using makecat.exe at: $makecatPath"

& $makecatPath $cdfPath
if ($LASTEXITCODE -ne 0) {
    throw "makecat.exe failed with exit code $LASTEXITCODE"
}

Write-Host "Generated catalog file: $CatOutputPath"
