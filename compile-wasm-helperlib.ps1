<#
.SYNOPSIS
    Compiles HelperLib.dll to HelperLib.wasm using crossgen2 targeting browser/wasm.

.DESCRIPTION
    Uses crossgen2 to AOT-compile HelperLib.dll into a .wasm file, referencing
    the managed DLLs in the IL directory. Output is placed into the IL directory.

    Prerequisites:
    - Run .\build.cmd -os browser -c Debug -subset clr+libs
    - Run .\prepare-wasm-test.ps1 to populate the IL directory
#>
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug"
)

$ErrorActionPreference = "Stop"

$RepoRoot = $PSScriptRoot
$DotnetExe = Join-Path $RepoRoot ".dotnet\dotnet.exe"
$Crossgen2Dll = Join-Path $RepoRoot "artifacts\bin\coreclr\windows.x64.debug\crossgen2\crossgen2.dll"
$WasmBinDir = Join-Path $RepoRoot "artifacts\bin\coreclr\browser.wasm.$Configuration"
$ILDir = Join-Path $WasmBinDir "IL"
$InputDll = Join-Path $ILDir "HelperLib.dll"
$baseName = [System.IO.Path]::GetFileNameWithoutExtension($InputDll)
$OutputWasm = Join-Path $WasmBinDir "$baseName.wasm"
$OutputVerifyWasm = Join-Path $WasmBinDir "$baseName.Verify.wasm"
$OutputWat = Join-Path $WasmBinDir "$baseName.wat"
$Wasm2Wat = "c:\git\wabt\bin\wasm2wat.exe"
$Wat2Wasm = "c:\git\wabt\bin\wat2wasm.exe"

# --- Validate prerequisites ---

if (-not (Test-Path $DotnetExe)) {
    Write-Error "dotnet not found at $DotnetExe"
    exit 1
}
if (-not (Test-Path $Crossgen2Dll)) {
    Write-Error "crossgen2.dll not found at $Crossgen2Dll. Build with: .\build.cmd clr -c Debug"
    exit 1
}
if (-not (Test-Path $InputDll)) {
    Write-Error "HelperLib.dll not found at $InputDll. Run .\prepare-wasm-test.ps1 first."
    exit 1
}
if (-not (Test-Path $Wasm2Wat)) {
    Write-Error "wasm2wat.exe not found at $Wasm2Wat"
    exit 1
}

# --- Build reference assembly list ---

$referenceDlls = Get-ChildItem "$ILDir\*.dll"
$referenceArgs = @()
foreach ($dll in $referenceDlls) {
    $referenceArgs += "-r"
    $referenceArgs += $dll.FullName
}

# --- Run crossgen2 ---

Write-Host "Compiling HelperLib.dll -> HelperLib.wasm" -ForegroundColor Cyan
Write-Host "  Input : $InputDll"
Write-Host "  Output: $OutputWasm"
Write-Host "  References: $($referenceDlls.Count) DLLs from IL directory"
Write-Host ""

$crossgen2Args = @(
    "--targetos", "browser"
    "--targetarch", "wasm"
    "-f", "wasm"
    "-o", $OutputWasm
) + $referenceArgs + @(
    $InputDll
)

$RspFile = Join-Path $WasmBinDir "crossgen2-helperlib.rsp"
$crossgen2Args | Set-Content -Path $RspFile
Write-Host "Response file: $RspFile" -ForegroundColor Yellow

Write-Host "$DotnetExe $Crossgen2Dll @$RspFile" -ForegroundColor Yellow

& $DotnetExe $Crossgen2Dll "@$RspFile"
if ($LASTEXITCODE -ne 0) {
    Write-Error "crossgen2 compilation failed with exit code $LASTEXITCODE"
    exit 1
}

Write-Host ""
Write-Host "=== crossgen2 succeeded ===" -ForegroundColor Green
Write-Host "  Output: $OutputWasm"
Write-Host "  Size  : $((Get-Item $OutputWasm).Length) bytes"

# --- Convert to WAT ---

Write-Host ""
Write-Host "Converting HelperLib.wasm -> HelperLib.wat" -ForegroundColor Cyan

& $Wasm2Wat $OutputWasm --enable-all --no-check -o $OutputWat
if ($LASTEXITCODE -ne 0) {
    Write-Error "wasm2wat conversion failed with exit code $LASTEXITCODE"
    exit 1
}

Write-Host ""
Write-Host "Converting HelperLib.wat -> HelperLib.Verify.wasm" -ForegroundColor Cyan

& $Wat2Wasm $OutputWat --enable-extended-const -o $OutputVerifyWasm
if ($LASTEXITCODE -ne 0) {
    Write-Error "wasm2wat conversion failed with exit code $LASTEXITCODE"
    exit 1
}

Write-Host ""
Write-Host "=== WAT conversion succeeded ===" -ForegroundColor Green
Write-Host "  Output: $OutputWat"
Write-Host "  Size  : $((Get-Item $OutputWat).Length) bytes"
