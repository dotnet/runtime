# collect-obj-files.ps1
# Collects compiled object files (.obj on Windows, .o on Linux/macOS) for
# the 102 source files modified in PR #127295 ("Remove Unused Includes").
# Usage: pwsh collect-obj-files.ps1 -ArtifactsObjDir <path> -OutputDir <path>

param(
    [Parameter(Mandatory)]
    [string]$ArtifactsObjDir,

    [Parameter(Mandatory)]
    [string]$OutputDir
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# The 102 source file basenames from PR #127295 (dotnet/runtime).
# We search for object files whose name matches "<basename>.obj" or "<basename>.o".
$sourceBasenames = @(
    "assembly"
    "dacfn"
    "processdescriptor"
    "walker"
    "dactable"
    "debuggermodule"
    "funceval"
    "ceefilegenwriter"
    "arraylist"
    "dres"
    "windasm"
    "interoplib"
    "block"
    "dllmain"
    "ee_il_dll"
    "importer"
    "loopcloning"
    "promotion"
    "promotiondecomposition"
    "simdcodegenxarch"
    "custattr_import"
    "regmeta"
    "regmeta_compilersupport"
    "regmeta_emit"
    "regmeta_import"
    "regmeta_vm"
    "stgtiggerstream"
    "mdinternaldisp"
    "memory"
    "AsmOffsetsVerify"
    "Crst"
    "DebugHeader"
    "EHHelpers"
    "FinalizerHelpers"
    "GCHelpers"
    "GcStressControl"
    "HandleTableHelpers"
    "MathHelpers"
    "MiscHelpers"
    "RestrictedCallouts"
    "RuntimeInstance"
    "SyncClean"
    "ThunksMapping"
    "TypeManager"
    "UniversalTransitionHelpers"
    "event"
    "ep-rt-aot"
    "eventpipeinternal"
    "eventtrace"
    "eventtrace_gcheap"
    "interoplibinterface_java"
    "portable"
    "rhassert"
    "startup"
    "stressLog"
    "threadstore"
    "PalCommon"
    "PalMinWin"
    "yieldprocessornormalized"
    "mcs"
    "removedup"
    "icorjitinfo_generated"
    "jithost"
    "superpmi-shim-counter"
    "superpmi-shim-simple"
    "streamingsuperpmi"
    "hostimpl"
    "rangelist"
    "cgenamd64"
    "appdomain"
    "callcounting"
    "ceeload"
    "class"
    "classcompat"
    "clsload"
    "comcallablewrapper"
    "cominterfacemarshaler"
    "commodule"
    "comutilnative"
    "corelib"
    "dllimport"
    "exceptionhandling"
    "fieldmarshaler"
    "interoplibinterface_comwrappers"
    "interoputil"
    "jitinterfacegen"
    "method"
    "methodtable"
    "onstackreplacement"
    "peassembly"
    "prestub"
    "qcallentrypoints"
    "stdinterfaces"
    "stdinterfaces_wrapper"
    "stubhelpers"
    "stublink"
    "threadsuspend"
    "tieredcompilation"
    "longfile.windows"
)

# Deduplicate basenames (some appear more than once across different source dirs)
$uniqueBasenames = $sourceBasenames | Sort-Object -Unique

# Build a set for fast lookup
$basenameSet = [System.Collections.Generic.HashSet[string]]::new(
    [System.StringComparer]::OrdinalIgnoreCase
)
foreach ($name in $uniqueBasenames) {
    [void]$basenameSet.Add($name)
}

if (-not (Test-Path $ArtifactsObjDir)) {
    Write-Host "##[warning]Artifacts obj directory not found: $ArtifactsObjDir"
    exit 0
}

# Create output directory
New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null

$totalCopied = 0

# Search for .obj and .o files recursively
$extensions = @('*.obj', '*.o')
foreach ($ext in $extensions) {
    $files = Get-ChildItem -Path $ArtifactsObjDir -Filter $ext -Recurse -File -ErrorAction SilentlyContinue
    foreach ($file in $files) {
        # Extract the source basename: e.g., "assembly.cpp.obj" -> "assembly"
        # CMake names obj files as <sourcename>.cpp.obj (or .c.obj) on Windows,
        # and <sourcename>.cpp.o (or .c.o) on Linux/macOS.
        $objName = $file.Name
        # Remove the object extension first
        $withoutObjExt = $objName
        if ($objName.EndsWith('.obj')) {
            $withoutObjExt = $objName.Substring(0, $objName.Length - 4)
        }
        elseif ($objName.EndsWith('.o')) {
            $withoutObjExt = $objName.Substring(0, $objName.Length - 2)
        }

        # Remove the source extension (.cpp, .c)
        $baseName = $withoutObjExt
        if ($withoutObjExt.EndsWith('.cpp')) {
            $baseName = $withoutObjExt.Substring(0, $withoutObjExt.Length - 4)
        }
        elseif ($withoutObjExt.EndsWith('.c')) {
            $baseName = $withoutObjExt.Substring(0, $withoutObjExt.Length - 2)
        }

        if ($basenameSet.Contains($baseName)) {
            # Preserve relative path from artifacts/obj
            $relativePath = $file.FullName.Substring($ArtifactsObjDir.TrimEnd([IO.Path]::DirectorySeparatorChar, '/').Length + 1)
            $destPath = Join-Path $OutputDir $relativePath
            $destDir = Split-Path $destPath -Parent
            if (-not (Test-Path $destDir)) {
                New-Item -ItemType Directory -Path $destDir -Force | Out-Null
            }
            Copy-Item -Path $file.FullName -Destination $destPath -Force
            Write-Host "Copied: $relativePath ($($file.Length) bytes)"
            $totalCopied++
        }
    }
}

Write-Host ""
Write-Host "Total object files collected: $totalCopied"
if ($totalCopied -eq 0) {
    Write-Host "##[warning]No matching object files found. The build may not have compiled these source files on this platform."
}
