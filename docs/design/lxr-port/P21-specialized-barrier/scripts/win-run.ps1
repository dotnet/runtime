# P21 (D1): Windows run helper. Builds the managed fixture, runs the LXR smoke test (R2R on) and the r2r-stores cell.
param([string]$Frame = 'C:\lxr-d1\frame-win-Release')
$ErrorActionPreference = 'Continue'
$repo = 'W:\'
$fix = 'C:\lxr-d1\fixtures\fixture-scope5-Release'
if (-not (Test-Path "$fix\runtime-tests.dll")) {
    $src = 'C:\lxr-d1\fixsrc'
    Remove-Item $src -Recurse -Force -ErrorAction SilentlyContinue
    New-Item -ItemType Directory $src | Out-Null
    Copy-Item "$repo\docs\design\lxr-port\P5\runtime-tests\*.cs", "$repo\docs\design\lxr-port\P5\runtime-tests\runtime-tests.csproj" $src
    Push-Location C:\lxr-d1
    & "$repo\.dotnet\dotnet.exe" build "$src\runtime-tests.csproj" -c Release -o $fix 2>&1 | Select-Object -Last 3
    Pop-Location
}
$envs = @{ DOTNET_TieredCompilation = '0'; DOTNET_gcServer = '0'; DOTNET_gcConcurrent = '1'; DOTNET_GCHeapCount = '2'; DOTNET_LXRLegacyCapacity = '1';
    DOTNET_GCPath = "$Frame\clrlxrgc.dll"; DOTNET_LXREvacuateNursery = '0'; DOTNET_LXREvacuateMature = '0' }
foreach ($k in $envs.Keys) { Set-Item "Env:$k" $envs[$k] }
Remove-Item Env:DOTNET_ReadyToRun -ErrorAction SilentlyContinue
"== smoke binarytrees (LXR, ReadyToRun on)"
$env:DOTNET_LXRImmixInitialMB = '80'; $env:DOTNET_LXRLOSInitialMB = '20'; $env:DOTNET_LXRManagedHeapHardLimitMB = '100'
& "$Frame\corerun.exe" C:\lxr-int\gcbench\gcbench.dll binarytrees 16 0 run 2>&1 | Select-Object -Last 2
Remove-Item Env:DOTNET_LXRImmixInitialMB, Env:DOTNET_LXRLOSInitialMB, Env:DOTNET_LXRManagedHeapHardLimitMB
"== r2r-stores cell"
& "$Frame\corerun.exe" "$fix\runtime-tests.dll" --p7-lifecycle r2r-stores "$Frame\clrlxrgc.dll" 2>&1 | Select-Object -Last 5
"== default GC reference"
Remove-Item Env:DOTNET_GCPath
& "$Frame\corerun.exe" C:\lxr-int\gcbench\gcbench.dll binarytrees 16 0 run 2>&1 | Select-Object -Last 1
