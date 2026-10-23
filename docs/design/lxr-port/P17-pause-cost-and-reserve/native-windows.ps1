# Compile the native LXR validation program with MSVC and run every selector.
# usage: native-windows.ps1 -Config Debug|Release -Output <dir> [-Repo <path>]
param(
    [Parameter(Mandatory)][ValidateSet('Debug', 'Release')][string]$Config,
    [Parameter(Mandatory)][string]$Output,
    [string]$Repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..\..')).Path
)
$ErrorActionPreference = 'Stop'
New-Item -ItemType Directory -Force $Output | Out-Null
$Output = (Resolve-Path $Output).Path
$gc = Join-Path $Repo 'src\coreclr\gc'
$names = 'side_metadata.cpp', 'immix_block.cpp', 'immix_allocation.cpp', 'reference_count.cpp', 'large_object_space.cpp',
    'lxr_work_packet.cpp', 'lxr_work_scheduler.cpp', 'lxr_pause_schedule.cpp', 'lxr_pause_coordinator.cpp',
    'lxr_completion_counter.cpp', 'lxr_reference_counting.cpp'
$sources = @($names | ForEach-Object { '"' + (Join-Path $gc $_) + '"' })
$sources += '"' + (Join-Path $Repo 'docs\design\lxr-port\P2.5\large-object-space-test-platform.cpp') + '"'
$sources += '"' + (Join-Path $Repo 'docs\design\lxr-port\P4\lxr-rc-validation.cpp') + '"'
$flags = if ($Config -eq 'Debug') { '/Od /MTd' } else { '/O2 /MT /DNDEBUG' }
$native = Join-Path $Repo 'src\native'
$exe = Join-Path $Output 'lxr-rc-validation.exe'
$cmd = "call `"$Repo\eng\native\init-vs-env.cmd`" x64 >nul && cd /d `"$Output`" && cl.exe /nologo /std:c++17 /EHsc /W4 /WX /wd4100 /wd4324 /wd4458 $flags /DBUILD_AS_STANDALONE /DTARGET_WINDOWS /DHOST_WINDOWS /DWIN32 /DTARGET_AMD64 /DHOST_AMD64 /DTARGET_64BIT /DHOST_64BIT /DLXR_SIDE_METADATA_TEST_PLATFORM /DLXR_ENABLE_LAZY_COMPLETION_COUNTER /I`"$gc`" /I`"$gc\env`" /I`"$native`" /I`"$native\inc`" $($sources -join ' ') /Fe:`"$exe`""
& $env:ComSpec /d /s /c $cmd *> (Join-Path $Output 'build.log')
Write-Output "build exit=$LASTEXITCODE"
if (-not (Test-Path $exe)) { Get-Content (Join-Path $Output 'build.log') | Select-Object -Last 20; exit 1 }
$failed = 0
$selectors = '', '--log-storage', '--mature', '--mature-remset', '--mature-reader', '--mature-retained-owner', '--mature-released-candidate',
    '--mature-headroom', '--mature-completed-scan', '--mature-remset-whole', '--mature-remset-wrap', '--mature-remset-same-shape',
    '--mature-remset-boundaries', '--nursery', '--nursery-reader', '--root-batching', '--trace-batching', '--aux-retirement',
    '--object-planes', '--storage-diagnostics', '--lifetime', '--p8-policy', '--p8-storage', '--trace', '--first-store-churn', '--inline-metadata', '--lazy-large-release', '--s3-bookkeeping', '--s4-sweep', '--s5b-overshoot', '--s6-registry', '--s7-pins', '--fresh-block-race', '--d2-spans', '--f1-trace', '--f5-policy', '--f4-scheduler', '--f2-pause', '--f4b-scheduler', '--f8-lazy', '--f10-rc'
foreach ($sel in $selectors) {
    $name = if ($sel) { $sel } else { 'default' }
    $log = Join-Path $Output "run$name.log"
    if ($sel) { & $exe $sel *> $log } else { & $exe *> $log }
    $code = $LASTEXITCODE
    $line = (Select-String -Path $log -Pattern '^(PASS|FAIL):' | Select-Object -Last 1).Line
    $flip = (Select-String -Path $log -Pattern '^flip_overlaps' | Select-Object -Last 1).Line
    Write-Output "$Config $name exit=$code :: $line :: $flip"
    if ($code -ne 0) { $failed = 1 }
}
Write-Output "native $Config failed=$failed"
