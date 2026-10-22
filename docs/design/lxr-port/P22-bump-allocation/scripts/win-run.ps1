# P22 (D2): Windows run helper. Builds the managed fixture, runs the LXR smoke test (R2R on) and the new cells.
# usage: win-run.ps1 [-Frame <flat runtime dir>] [-Work <work dir>]   (run from a shell where W: is the repository, see P19 section 7.2)
param([string]$Frame = 'C:\lxr-d2\frame-win-Release', [string]$Work = 'C:\lxr-d2', [string]$Bench = 'C:\lxr-int\gcbench\gcbench.dll')
$ErrorActionPreference = 'Continue'
$repo = 'W:\'
$fix = "$Work\fixtures\fixture-scope5-Release"
if (-not (Test-Path "$fix\runtime-tests.dll")) {
    $src = "$Work\fixsrc"
    Remove-Item $src -Recurse -Force -ErrorAction SilentlyContinue
    New-Item -ItemType Directory $src | Out-Null
    Copy-Item "$repo\docs\design\lxr-port\P5\runtime-tests\*.cs", "$repo\docs\design\lxr-port\P5\runtime-tests\runtime-tests.csproj" $src
    Push-Location $Work
    & "$repo\.dotnet\dotnet.exe" build "$src\runtime-tests.csproj" -c Release -o $fix 2>&1 | Select-Object -Last 3
    Pop-Location
}
$envs = @{ DOTNET_TieredCompilation = '0'; DOTNET_gcServer = '0'; DOTNET_gcConcurrent = '1'; DOTNET_GCHeapCount = '2'; DOTNET_LXRLegacyCapacity = '1';
    DOTNET_GCPath = "$Frame\clrlxrgc.dll"; DOTNET_LXREvacuateNursery = '0'; DOTNET_LXREvacuateMature = '0' }
foreach ($k in $envs.Keys) { Set-Item "Env:$k" $envs[$k] }
Remove-Item Env:DOTNET_ReadyToRun -ErrorAction SilentlyContinue
"== smoke binarytrees (LXR, ReadyToRun on)"
$env:DOTNET_LXRImmixInitialMB = '80'; $env:DOTNET_LXRLOSInitialMB = '20'; $env:DOTNET_LXRManagedHeapHardLimitMB = '100'
& "$Frame\corerun.exe" $Bench binarytrees 16 0 run 2>&1 | Select-Object -Last 2
"== smoke binarytrees (LXR, nursery)"
$env:DOTNET_LXREvacuateNursery = '1'
& "$Frame\corerun.exe" $Bench binarytrees 16 0 run 2>&1 | Select-Object -Last 2
$env:DOTNET_LXREvacuateNursery = '0'
Remove-Item Env:DOTNET_LXRImmixInitialMB, Env:DOTNET_LXRLOSInitialMB, Env:DOTNET_LXRManagedHeapHardLimitMB
$env:DOTNET_LXRImmixInitialMB = '512'; $env:DOTNET_LXRLOSInitialMB = '64'; $env:DOTNET_LXRManagedHeapHardLimitMB = '576'
# With 192 + 64 MB the cell alloc-storm fails with OutOfMemory in the non-moving mode on Windows (D1 and D2 alike, see P22 section 10).
foreach ($cell in 'r2r-stores', 'young-interior', 'alloc-storm', 'alloc-sizes') {
    "== $cell"
    & "$Frame\corerun.exe" "$fix\runtime-tests.dll" --p7-lifecycle $cell "$Frame\clrlxrgc.dll" 2>&1 | Select-Object -Last 3
}
"== default GC"
Remove-Item Env:DOTNET_GCPath
& "$Frame\corerun.exe" $Bench binarytrees 16 0 run 2>&1 | Select-Object -Last 1
