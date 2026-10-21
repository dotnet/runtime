# Build the LXR collector (target clrlxrgc) on Windows.
# The module is run with DOTNET_GCPath from the accepted e8d2d23 layout, so no layout copy is needed.
# usage: build-windows.ps1 -Config Release|Debug -Label <label> [-Repo <path>]
param(
    [Parameter(Mandatory)][ValidateSet('Release', 'Debug')][string]$Config,
    [Parameter(Mandatory)][string]$Label,
    [string]$Repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..\..')).Path
)
$ErrorActionPreference = 'Stop'
$vs = 'C:\Program Files\Microsoft Visual Studio\18\IntPreview'
$cmake = "$vs\Common7\IDE\CommonExtensions\Microsoft\CMake\CMake\bin\cmake.exe"
$ninja = "$vs\Common7\IDE\CommonExtensions\Microsoft\CMake\Ninja\ninja.exe"
$root = Join-Path $Repo 'artifacts\P17'
$obj = Join-Path $root "obj-windows-$Config"
$out = Join-Path $root "modules\$Label-$Config"
New-Item -ItemType Directory -Force $root, (Join-Path $root 'logs'), $out | Out-Null
if (-not (Test-Path (Join-Path $obj 'build.ninja'))) {
    $cfg = "call `"$vs\VC\Auxiliary\Build\vcvarsall.bat`" amd64 >nul && `"$cmake`" -S `"$Repo\src\coreclr`" -B `"$obj`" -G Ninja -DCMAKE_MAKE_PROGRAM=`"$ninja`" -DCMAKE_BUILD_TYPE=$Config -DCLR_CMAKE_TARGET_ARCH=x64 -DCLR_CMAKE_HOST_ARCH=x64 -DCLR_CMAKE_TARGET_OS=windows -DCLI_CMAKE_FALLBACK_OS=win10 -DCMAKE_SYSTEM_VERSION=10.0 -DCLR_CMAKE_PGO_INSTRUMENT=0 -DCLR_CMAKE_PGO_OPTIMIZE=0 -DCLR_CMAKE_ENABLE_LXR_GC=1"
    & $env:ComSpec /d /s /c $cfg *> (Join-Path $root "logs\configure-$Config.log")
    if ($LASTEXITCODE -ne 0) { throw "configure failed; see logs\configure-$Config.log" }
}
$bld = "call `"$vs\VC\Auxiliary\Build\vcvarsall.bat`" amd64 >nul && `"$cmake`" --build `"$obj`" --target clrlxrgc --parallel 4"
& $env:ComSpec /d /s /c $bld *> (Join-Path $root "logs\build-$Label-$Config.log")
if ($LASTEXITCODE -ne 0) { throw "build failed; see logs\build-$Label-$Config.log" }
$dll = Get-ChildItem $obj -Recurse -Filter clrlxrgc.dll | Select-Object -First 1
Copy-Item $dll.FullName (Join-Path $out 'clrlxrgc.dll') -Force
$pdb = Join-Path $dll.DirectoryName 'clrlxrgc.pdb'
if (Test-Path $pdb) { Copy-Item $pdb (Join-Path $out 'clrlxrgc.pdb') -Force }
(Get-FileHash (Join-Path $out 'clrlxrgc.dll')).Hash
