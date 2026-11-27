$ErrorActionPreference = 'Continue'

$batchDir = (Get-Location).Path
$originalUploadRoot = if ([string]::IsNullOrEmpty($env:HELIX_WORKITEM_UPLOAD_ROOT)) {
    Join-Path $batchDir "test-results"
} else {
    $env:HELIX_WORKITEM_UPLOAD_ROOT
}

Write-Host "=== WasmBatchRunner ==="
Write-Host "BATCH_DIR=$batchDir"
Write-Host "ORIGINAL_UPLOAD_ROOT=$originalUploadRoot"

$zipFiles = @(Get-ChildItem -LiteralPath $batchDir -Filter "*.zip" -File | Sort-Object Name)
if ($zipFiles.Count -eq 0) {
    Write-Host "No .zip files found in $batchDir"
    exit 1
}

Add-Type -AssemblyName System.IO.Compression.FileSystem
$results = New-Object System.Collections.Generic.List[psobject]

foreach ($zipFile in $zipFiles) {
    $suiteName = [System.IO.Path]::GetFileNameWithoutExtension($zipFile.Name)
    $suiteDir = Join-Path $batchDir $suiteName

    Write-Host ""
    Write-Host "========================= BEGIN $suiteName ============================="

    if (Test-Path -LiteralPath $suiteDir) {
        Remove-Item -LiteralPath $suiteDir -Recurse -Force -ErrorAction SilentlyContinue
    }
    $null = New-Item -ItemType Directory -Path $suiteDir -Force

    try {
        [System.IO.Compression.ZipFile]::ExtractToDirectory($zipFile.FullName, $suiteDir)
    } catch {
        Write-Host "ERROR: Failed to extract $($zipFile.FullName): $($_.Exception.Message)"
        $results.Add([pscustomobject]@{ Name = $suiteName; ExitCode = 1; Duration = 0 })
        Remove-Item -LiteralPath $suiteDir -Recurse -Force -ErrorAction SilentlyContinue
        continue
    }

    $env:HELIX_WORKITEM_UPLOAD_ROOT = Join-Path $originalUploadRoot $suiteName
    $null = New-Item -ItemType Directory -Path $env:HELIX_WORKITEM_UPLOAD_ROOT -Force

    Push-Location -LiteralPath $suiteDir
    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    $suiteExitCode = 1
    try {
        if (-not (Test-Path -LiteralPath "RunTests.cmd")) {
            Write-Host "ERROR: RunTests.cmd not found in $suiteDir"
        } else {
            $global:LASTEXITCODE = $null
            & .\RunTests.cmd @args
            $suiteExitCode = if ($null -eq $LASTEXITCODE) { 1 } else { $LASTEXITCODE }
        }
    } catch {
        Write-Host "ERROR: Failed to execute RunTests.cmd: $($_.Exception.Message)"
    } finally {
        $stopwatch.Stop()
        Pop-Location
    }

    Remove-Item -LiteralPath $suiteDir -Recurse -Force -ErrorAction SilentlyContinue

    $duration = [int][Math]::Floor($stopwatch.Elapsed.TotalSeconds)
    $results.Add([pscustomobject]@{ Name = $suiteName; ExitCode = $suiteExitCode; Duration = $duration })

    if ($suiteExitCode -ne 0) {
        Write-Host "----- FAIL $suiteName - exit code $suiteExitCode - ${duration}s -----"
    } else {
        Write-Host "----- PASS $suiteName - ${duration}s -----"
    }

    Write-Host "========================= END $suiteName ==============================="
}

Write-Host ""
Write-Host "=== Batch Summary ==="
Write-Host ("{0,-60} {1,-6} {2}" -f "Suite", "Status", "Duration")
Write-Host ("{0,-60} {1,-6} {2}" -f "-----", "------", "--------")

foreach ($entry in $results) {
    $status = if ($entry.ExitCode -eq 0) { "PASS" } else { "FAIL" }
    Write-Host ("{0,-60} {1,-6} {2}s" -f $entry.Name, $status, $entry.Duration)
}

$suiteCount = $results.Count
$failCount = @($results | Where-Object { $_.ExitCode -ne 0 }).Count
Write-Host ""
Write-Host "Total: $suiteCount | Passed: $($suiteCount - $failCount) | Failed: $failCount"

if ($failCount -ne 0) {
    exit 1
}

exit 0
