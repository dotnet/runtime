<#
.SYNOPSIS
    Build the CoreCLR runtime.
.DESCRIPTION
    This script builds the CoreCLR runtime. It can build various architectures and configurations.
.EXAMPLE
    .\build-runtime.ps1
    Builds x64 Debug, all components.
.EXAMPLE
    .\build-runtime.ps1 -component jit
    Builds x64 Debug, just the JIT.
.EXAMPLE
    .\build-runtime.ps1 -arm64 -release
    Builds arm64 Release.
#>

param(
    [Parameter(ValueFromRemainingArguments=$true)]
    [string[]]$Arguments
)

Set-StrictMode -Version 3
$ErrorActionPreference = 'Stop'

# Define a prefix for most output progress messages
$script:MsgPrefix = "BUILD: "
$script:ErrMsgPrefix = ""

function Write-BuildMessage {
    param([string]$Message)
    Write-Host "$MsgPrefix$Message"
}

function Write-BuildError {
    param([string]$Message)
    Write-Host "$ErrMsgPrefix$MsgPrefix$Message" -ForegroundColor Red
}

function Show-Usage {
    Write-Host ""
    Write-Host "Build the CoreCLR repo."
    Write-Host ""
    Write-Host "Usage:"
    Write-Host "    build-runtime.ps1 [option1] [option2]"
    Write-Host "or:"
    Write-Host "    build-runtime.ps1 all [option1] [option2]"
    Write-Host ""
    Write-Host "All arguments are optional. The options are:"
    Write-Host ""
    Write-Host "-? -h -help: view this message."
    Write-Host "-all: Builds all configurations and platforms."
    Write-Host "Build architecture: one of -x64, -x86, -arm, -arm64, -loongarch64, -riscv64 (default: -x64)."
    Write-Host "Build type: one of -Debug, -Checked, -Release (default: -Debug)."
    Write-Host "-component <name> : specify this option one or more times to limit components built to those specified."
    Write-Host "                    Allowed <name>: hosts jit alljits runtime paltests iltools nativeaot spmi"
    Write-Host "-enforcepgo: verify after the build that PGO was used for key DLLs, and fail the build if not"
    Write-Host "-pgoinstrument: generate instrumented code for profile guided optimization enabled binaries."
    Write-Host "-cmakeargs: user-settable additional arguments passed to CMake."
    Write-Host "-configureonly: skip all builds; only run CMake (default: CMake and builds are run)"
    Write-Host "-skipconfigure: skip CMake (default: CMake is run)"
    Write-Host "-skipnative: skip building native components (default: native components are built)."
    Write-Host "-fsanitize <name>: Enable the specified sanitizers."
    Write-Host ""
    Write-Host "Examples:"
    Write-Host "    build-runtime.ps1"
    Write-Host "       -- builds x64 debug, all components"
    Write-Host "    build-runtime.ps1 -component jit"
    Write-Host "       -- builds x64 debug, just the JIT"
    Write-Host "    build-runtime.ps1 -component jit -component runtime"
    Write-Host "       -- builds x64 debug, just the JIT and runtime"
    Write-Host ""
    Write-Host "If `"all`" is specified, then all build architectures and types are built. If, in addition,"
    Write-Host "one or more build architectures or types is specified, then only those build architectures"
    Write-Host "and types are built."
    Write-Host ""
    Write-Host "For example:"
    Write-Host "    build-runtime.ps1 -all"
    Write-Host "       -- builds all architectures, and all build types per architecture"
    Write-Host "    build-runtime.ps1 -all -x86"
    Write-Host "       -- builds all build types for x86"
    Write-Host "    build-runtime.ps1 -all -x64 -x86 -Checked -Release"
    Write-Host "       -- builds x64 and x86 architectures, Checked and Release build types for each"
}

Write-BuildMessage "Starting Build at $(Get-Date -Format 'HH:mm:ss')"

$ThisScriptFull = $PSCommandPath

# Set the default arguments for build
$TargetArch = "x64"
$BuildType = "Debug"
$TargetOS = "windows"

# Set the various build properties
$ProjectDir = Split-Path -Parent $PSCommandPath
$RepoRootDir = (Resolve-Path (Join-Path $ProjectDir "..\..")).Path
$ProjectFilesDir = $ProjectDir
$RootBinDir = Join-Path $RepoRootDir "artifacts"

$BuildAll = $false

$TargetArchX64 = $false
$TargetArchX86 = $false
$TargetArchArm = $false
$TargetArchArm64 = $false
$TargetArchLoongArch64 = $false
$TargetArchRiscV64 = $false
$TargetArchWasm = $false

$BuildTypeDebug = $false
$BuildTypeChecked = $false
$BuildTypeRelease = $false

$PgoInstrument = $false
$PgoOptimize = $false
$EnforcePgo = $false

$PassThroughArgs = @()
$UnprocessedBuildArgs = @()

$BuildNative = $true
$RestoreOptData = $true
$CrossTarget = $false
$HostOS = ""
$HostArch = ""
$PgoOptDataPath = ""
$CMakeArgs = @()
$Ninja = $true
$RequestedBuildComponents = @()
$TargetRid = ""
$SubDir = ""
$ConfigureOnly = $false
$SkipConfigure = $false

# Parse arguments
$i = 0
while ($i -lt $Arguments.Length) {
    $arg = $Arguments[$i]
    
    switch -wildcard ($arg) {
        { $_ -in '-?', '-h', '-help', '--help' } {
            Show-Usage
            exit 0
        }
        
        '-all' {
            $BuildAll = $true
        }
        
        { $_ -in '-x64', 'x64' } {
            $TargetArchX64 = $true
        }
        
        { $_ -in '-x86', 'x86' } {
            $TargetArchX86 = $true
        }
        
        { $_ -in '-arm', 'arm' } {
            $TargetArchArm = $true
        }
        
        { $_ -in '-arm64', 'arm64' } {
            $TargetArchArm64 = $true
        }
        
        { $_ -in '-loongarch64', 'loongarch64' } {
            $TargetArchLoongArch64 = $true
        }
        
        { $_ -in '-riscv64', 'riscv64' } {
            $TargetArchRiscV64 = $true
        }
        
        { $_ -in '-wasm', 'wasm' } {
            $TargetArchWasm = $true
        }
        
        { $_ -in '-debug', 'debug' } {
            $BuildTypeDebug = $true
        }
        
        { $_ -in '-checked', 'checked' } {
            $BuildTypeChecked = $true
        }
        
        { $_ -in '-release', 'release' } {
            $BuildTypeRelease = $true
        }
        
        '-ci' {
            $script:ErrMsgPrefix = "##vso[task.logissue type=error]"
        }
        
        { $_ -in '-rebuild', 'rebuild' } {
            Write-Host "ERROR: 'Rebuild' is not supported. Please remove it."
            Show-Usage
            exit 1
        }
        
        '-hostos' {
            $HostOS = $Arguments[$i + 1]
            $PassThroughArgs += $arg, $Arguments[$i + 1]
            $i++
        }
        
        '-hostarch' {
            $HostArch = $Arguments[$i + 1]
            $PassThroughArgs += $arg, $Arguments[$i + 1]
            $i++
        }
        
        '-os' {
            $TargetOS = $Arguments[$i + 1]
            $PassThroughArgs += $arg, $Arguments[$i + 1]
            $i++
        }
        
        { $_ -in '-targetrid', '-outputrid' } {
            $TargetRid = $Arguments[$i + 1]
            $PassThroughArgs += $arg, $Arguments[$i + 1]
            $i++
        }
        
        '-subdir' {
            $SubDir = $Arguments[$i + 1]
            $PassThroughArgs += $arg, $Arguments[$i + 1]
            $i++
        }
        
        '-cmakeargs' {
            $CMakeArgs += $Arguments[$i + 1]
            $PassThroughArgs += $arg, $Arguments[$i + 1]
            $i++
        }
        
        { $_ -in '-configureonly', 'configureonly' } {
            $ConfigureOnly = $true
            $BuildNative = $true
            $PassThroughArgs += $arg
        }
        
        { $_ -in '-skipconfigure', 'skipconfigure' } {
            $SkipConfigure = $true
            $PassThroughArgs += $arg
        }
        
        { $_ -in '-skipnative', 'skipnative' } {
            $BuildNative = $false
            $PassThroughArgs += $arg
        }
        
        '-ninja' {
            # Ninja is now the default, this is a no-op
            $PassThroughArgs += $arg
        }
        
        '-msbuild' {
            $Ninja = $false
            $PassThroughArgs += $arg
        }
        
        { $_ -in '-pgoinstrument', 'pgoinstrument' } {
            $PgoInstrument = $true
            $PassThroughArgs += $arg
        }
        
        { $_ -in '-enforcepgo', 'enforcepgo' } {
            $EnforcePgo = $true
            $PassThroughArgs += $arg
        }
        
        '-pgodatapath' {
            $PgoOptDataPath = $Arguments[$i + 1]
            $PgoOptimize = $true
            $PassThroughArgs += $arg, $Arguments[$i + 1]
            $i++
        }
        
        '-component' {
            $RequestedBuildComponents += $Arguments[$i + 1]
            $PassThroughArgs += $arg, $Arguments[$i + 1]
            $i++
        }
        
        '-fsanitize' {
            $CMakeArgs += "-DCLR_CMAKE_ENABLE_SANITIZERS=$($Arguments[$i + 1])"
            $PassThroughArgs += $arg, $Arguments[$i + 1]
            $i++
        }
        
        '-keepnativesymbols' {
            $CMakeArgs += "-DCLR_CMAKE_KEEP_NATIVE_SYMBOLS=true"
            $PassThroughArgs += $arg
        }
        
        default {
            $UnprocessedBuildArgs += $arg
        }
    }
    
    $i++
}

# Initialize VS environment
$initVsEnvScript = Join-Path $RepoRootDir "eng\native\init-vs-env.cmd"
& cmd.exe /c "`"$initVsEnvScript`" >NUL 2>&1 && set" | ForEach-Object {
    if ($_ -match '^([^=]+)=(.*)$') {
        [System.Environment]::SetEnvironmentVariable($matches[1], $matches[2])
    }
}

if ($env:ERRORLEVEL -ne '0' -and $env:ERRORLEVEL -ne $null) {
    exit 1
}

$VCToolsRoot = $null
if ($env:VCINSTALLDIR) {
    $VCToolsRoot = Join-Path $env:VCINSTALLDIR "Auxiliary\Build"
}

if ($BuildAll) {
    $ArchList = @()
    
    $TotalSpecifiedTargetArch = [int]$TargetArchX64 + [int]$TargetArchX86 + [int]$TargetArchArm + [int]$TargetArchArm64 + [int]$TargetArchLoongArch64 + [int]$TargetArchRiscV64 + [int]$TargetArchWasm
    
    if ($TotalSpecifiedTargetArch -eq 0) {
        # Nothing specified means we want to build all architectures
        $ArchList = @('x64', 'x86', 'arm', 'arm64')
        
        # Add community architectures if building alljitscommunity
        if ($RequestedBuildComponents -contains 'alljitscommunity') {
            $ArchList += @('loongarch64', 'riscv64')
        }
    } else {
        # Add all specified architectures
        if ($TargetArchX64) { $ArchList += 'x64' }
        if ($TargetArchX86) { $ArchList += 'x86' }
        if ($TargetArchArm) { $ArchList += 'arm' }
        if ($TargetArchArm64) { $ArchList += 'arm64' }
        if ($TargetArchLoongArch64) { $ArchList += 'loongarch64' }
        if ($TargetArchRiscV64) { $ArchList += 'riscv64' }
        if ($TargetArchWasm) { $ArchList += 'wasm' }
    }
    
    $BuildTypeList = @()
    
    $TotalSpecifiedBuildType = [int]$BuildTypeDebug + [int]$BuildTypeChecked + [int]$BuildTypeRelease
    
    if ($TotalSpecifiedBuildType -eq 0) {
        # Nothing specified means we want to build all build types
        $BuildTypeList = @('Debug', 'Checked', 'Release')
    } else {
        if ($BuildTypeDebug) { $BuildTypeList += 'Debug' }
        if ($BuildTypeChecked) { $BuildTypeList += 'Checked' }
        if ($BuildTypeRelease) { $BuildTypeList += 'Release' }
    }
    
    $AllBuildSuccess = $true
    $BuildResults = @()
    
    foreach ($Arch in $ArchList) {
        foreach ($Config in $BuildTypeList) {
            Write-BuildMessage "Invoking: $ThisScriptFull -$Arch -$Config $($PassThroughArgs -join ' ')"
            
            $buildArgs = @("-$Arch", "-$Config") + $PassThroughArgs
            
            & $ThisScriptFull $buildArgs
            if ($LASTEXITCODE -ne 0) {
                $BuildResults += "$Arch $Config $($PassThroughArgs -join ' ')"
                $AllBuildSuccess = $false
            }
        }
    }
    
    if ($AllBuildSuccess) {
        Write-BuildMessage "All builds succeeded!"
        exit 0
    } else {
        Write-BuildError "Builds failed:"
        foreach ($result in $BuildResults) {
            Write-Host "    $result"
        }
        exit 1
    }
}

# From this point on, we're building a single architecture/configuration
$TotalSpecifiedTargetArch = [int]$TargetArchX64 + [int]$TargetArchX86 + [int]$TargetArchArm + [int]$TargetArchArm64 + [int]$TargetArchLoongArch64 + [int]$TargetArchRiscV64 + [int]$TargetArchWasm
if ($TotalSpecifiedTargetArch -gt 1) {
    Write-Host "Error: more than one build architecture specified, but `"all`" not specified."
    Show-Usage
    exit 1
}

if ($TargetArchX64) { $TargetArch = 'x64' }
if ($TargetArchX86) { $TargetArch = 'x86' }
if ($TargetArchArm) { $TargetArch = 'arm' }
if ($TargetArchArm64) { $TargetArch = 'arm64' }
if ($TargetArchLoongArch64) { $TargetArch = 'loongarch64' }
if ($TargetArchRiscV64) { $TargetArch = 'riscv64' }
if ($TargetArchWasm) { $TargetArch = 'wasm' }

if (-not $HostArch) {
    $HostArch = $TargetArch
}

$TotalSpecifiedBuildType = [int]$BuildTypeDebug + [int]$BuildTypeChecked + [int]$BuildTypeRelease
if ($TotalSpecifiedBuildType -gt 1) {
    Write-Host "Error: more than one build type specified, but `"all`" not specified."
    Show-Usage
    exit 1
}

if ($BuildTypeDebug) { $BuildType = 'Debug' }
if ($BuildTypeChecked) { $BuildType = 'Checked' }
if ($BuildTypeRelease) { $BuildType = 'Release' }

# EnforcePgo doesn't apply to ARM architectures
if ($EnforcePgo -and ($TargetArch -in 'arm', 'arm64')) {
    Write-BuildMessage "NOTICE: enforcepgo does nothing on $TargetArch architecture"
    $EnforcePgo = $false
}

# PGO optimization is only applied to release builds
if ($BuildType -ne 'Release') {
    $PgoOptimize = $false
}

# Set target OS directory name
$TargetOSDirName = $TargetOS
if ($TargetOS -eq 'alpine') {
    $TargetOSDirName = 'linux_musl'
}

# Set up directories
$BinDir = Join-Path $RootBinDir "bin\coreclr\$TargetOSDirName.$TargetArch.$BuildType"
$IntermediatesDir = Join-Path $RootBinDir "obj\coreclr\$TargetOSDirName.$TargetArch.$BuildType"
$LogsDir = Join-Path $RootBinDir "log\$BuildType"
$MsbuildDebugLogsDir = Join-Path $LogsDir "MsbuildDebugLogs"
$ArtifactsIntermediatesDir = Join-Path $RepoRootDir "artifacts\obj\coreclr"

if (-not $Ninja) {
    $IntermediatesDir = Join-Path $IntermediatesDir "ide"
}

$PackagesBinDir = Join-Path $BinDir ".nuget"

if ($SubDir) {
    $BinDir = Join-Path $BinDir $SubDir
    $IntermediatesDir = Join-Path $IntermediatesDir $SubDir
}

# Generate path for CMAKE_INSTALL_PREFIX with forward slashes
$CMakeBinDir = $BinDir -replace '\\', '/'

# Create directories
New-Item -ItemType Directory -Force -Path $BinDir | Out-Null
New-Item -ItemType Directory -Force -Path $IntermediatesDir | Out-Null
New-Item -ItemType Directory -Force -Path $LogsDir | Out-Null
New-Item -ItemType Directory -Force -Path $MsbuildDebugLogsDir | Out-Null

# Set up the directory for MSBuild debug logs
$env:MSBUILDDEBUGPATH = $MsbuildDebugLogsDir

Write-BuildMessage "Commencing CoreCLR product build"
Write-BuildMessage "Checking prerequisites"

if ($BuildNative) {
    # Locate CMake
    $setCMakePathScript = Join-Path $RepoRootDir "eng\native\set-cmake-path.ps1"
    $cmakeOutput = & powershell -NoProfile -ExecutionPolicy ByPass -File $setCMakePathScript 2>&1
    
    $CMakePath = $null
    foreach ($line in $cmakeOutput) {
        if ($line -match 'set CMakePath=(.+)') {
            $CMakePath = $matches[1]
            break
        }
    }
    
    if (-not $CMakePath) {
        Write-BuildError "Error: Could not determine CMake path"
        exit 1
    }
    
    Write-BuildMessage "Using CMake from $CMakePath"
}

# Determine number of processor cores
$NumberOfCores = 0
$wmicOutput = wmic cpu get NumberOfCores /value 2>$null | Where-Object { $_ -match 'NumberOfCores=' }
foreach ($line in $wmicOutput) {
    if ($line -match 'NumberOfCores=(\d+)') {
        $NumberOfCores += [int]$matches[1]
    }
}

if ($NumberOfCores -eq 0) {
    $NumberOfCores = $env:NUMBER_OF_PROCESSORS
}

Write-BuildMessage "Number of processor cores $NumberOfCores"

# Handle cross-compilation targets
if ($TargetOS -in 'android', 'browser') {
    $CrossTarget = $true
}

# Copy version files
if (-not $CrossTarget) {
    $copyVersionScript = Join-Path $RepoRootDir "eng\native\version\copy_version_files.cmd"
    & cmd.exe /c "`"$copyVersionScript`""
} else {
    $copyVersionScript = Join-Path $RepoRootDir "eng\native\version\copy_version_files.ps1"
    & powershell -NoProfile -ExecutionPolicy ByPass -File $copyVersionScript
}

# Locate Python
$IntermediatesIncDir = Join-Path $IntermediatesDir "src\inc"
$IntermediatesEventingDir = Join-Path $ArtifactsIntermediatesDir "Eventing\$TargetArch\$BuildType"

$PythonCmd = $null
$pythonCommands = @('py -3', 'py -2', 'python3', 'python2', 'python')
foreach ($pyCmd in $pythonCommands) {
    try {
        $tempFile = [System.IO.Path]::GetTempFileName()
        & cmd.exe /c "$pyCmd -c `"import sys; sys.stdout.write(sys.executable)`" >$tempFile 2>NUL"
        if ($LASTEXITCODE -eq 0) {
            $pythonExe = Get-Content $tempFile -Raw
            if ($pythonExe) {
                $PythonCmd = $pythonExe.Trim()
                Remove-Item $tempFile
                break
            }
        }
        Remove-Item $tempFile -ErrorAction SilentlyContinue
    } catch {
        continue
    }
}

if (-not $PythonCmd) {
    Write-BuildError "Error: Could not find a Python installation."
    exit 1
}

$env:PYTHON = $PythonCmd

# Build CMake target based on requested components
$CMakeTarget = @()
$BuildAllJitsCommunity = $false

foreach ($comp in $RequestedBuildComponents) {
    switch ($comp.ToLower()) {
        'hosts' { $CMakeTarget += 'hosts' }
        'jit' { $CMakeTarget += 'jit' }
        'alljits' { $CMakeTarget += 'alljits' }
        'alljitscommunity' {
            $CMakeTarget += 'alljitscommunity'
            $BuildAllJitsCommunity = $true
        }
        'runtime' { $CMakeTarget += 'runtime' }
        'paltests' { $CMakeTarget += 'paltests_install' }
        'iltools' { $CMakeTarget += 'iltools' }
        'nativeaot' { $CMakeTarget += 'nativeaot' }
        'spmi' { $CMakeTarget += 'spmi' }
        'debug' { $CMakeTarget += 'debug' }
    }
}

if ($CMakeTarget.Count -eq 0) {
    $CMakeTarget = @('install')
}

# Build native assets including CLR runtime
if ($BuildNative) {
    Write-BuildMessage "Commencing build of native components for $TargetOS.$TargetArch.$BuildType"

    # Set the environment for the native build
    $VCBuildArch = 'amd64'
    if ($env:PROCESSOR_ARCHITECTURE -eq 'ARM64') {
        $VCBuildArch = 'arm64'
        if ($HostArch -eq 'x64') { $VCBuildArch = 'arm64_amd64' }
        if ($HostArch -eq 'x86') { $VCBuildArch = 'arm64_x86' }
    } else {
        if ($HostArch -eq 'x86') { $VCBuildArch = 'amd64_x86' }
        if ($HostArch -eq 'arm64') { $VCBuildArch = 'amd64_arm64' }
    }

    if (-not $env:SkipVCEnvInit -and $VCToolsRoot) {
        $vcvarsall = Join-Path $VCToolsRoot "vcvarsall.bat"
        Write-BuildMessage "Using environment: `"$vcvarsall`" $VCBuildArch"
        
        & cmd.exe /c "`"$vcvarsall`" $VCBuildArch >NUL 2>&1 && set" | ForEach-Object {
            if ($_ -match '^([^=]+)=(.*)$') {
                [System.Environment]::SetEnvironmentVariable($matches[1], $matches[2])
            }
        }
    }

    if (-not $SkipConfigure) {
        Write-BuildMessage "Regenerating the Visual Studio solution"

        $ExtraCmakeArgs = @()
        
        if ($Ninja) {
            $ExtraCmakeArgs += "-DCMAKE_BUILD_TYPE=$BuildType"
        }

        $ExtraCmakeArgs += "-DCLR_CMAKE_TARGET_ARCH=$TargetArch"
        $ExtraCmakeArgs += "-DCLR_CMAKE_TARGET_OS=$TargetOS"

        # Host fallback OS
        $HostFallbackOS = if ($TargetRid) { ($TargetRid -split '-')[0] } else { '' }
        if ($HostFallbackOS -eq 'win') {
            $HostFallbackOS = 'win10'
        }
        if (-not $HostFallbackOS) {
            $HostFallbackOS = 'win10'
        }

        $ExtraCmakeArgs += "-DCLI_CMAKE_FALLBACK_OS=$HostFallbackOS"
        $ExtraCmakeArgs += "-DCLR_CMAKE_PGO_INSTRUMENT=$(if ($PgoInstrument) { '1' } else { '0' })"
        $ExtraCmakeArgs += "-DCLR_CMAKE_OPTDATA_PATH=$PgoOptDataPath"
        $ExtraCmakeArgs += "-DCLR_CMAKE_PGO_OPTIMIZE=$(if ($PgoOptimize) { '1' } else { '0' })"

        $BuildHostOS = if ($HostOS) { $HostOS } else { $TargetOS }

        if ($CMakeArgs) {
            $ExtraCmakeArgs += $CMakeArgs
        }

        $genBuildsysScript = Join-Path $RepoRootDir "eng\native\gen-buildsys.cmd"
        $genBuildsysArgs = @("`"$ProjectDir`"", "`"$IntermediatesDir`"", $env:VisualStudioVersion, $HostArch, $BuildHostOS) + $ExtraCmakeArgs
        
        Write-Host "Calling `"$genBuildsysScript`" $($genBuildsysArgs -join ' ')"
        & cmd.exe /c "`"$genBuildsysScript`" $($genBuildsysArgs -join ' ')"
        
        if ($LASTEXITCODE -ne 0) {
            Write-BuildError "Error: failed to generate native component build project!"
            exit 1
        }
    }

    $cmakeCacheFile = Join-Path $IntermediatesDir "CMakeCache.txt"
    if (-not (Test-Path $cmakeCacheFile)) {
        Write-BuildError "Error: unable to find generated native component build project!"
        exit 1
    }

    if (-not $ConfigureOnly) {
        $BuildLogRootName = "CoreCLR"
        $BuildLog = Join-Path $LogsDir "${BuildLogRootName}_${TargetOS}__${TargetArch}__${BuildType}__${HostArch}.log"
        $BuildWrn = Join-Path $LogsDir "${BuildLogRootName}_${TargetOS}__${TargetArch}__${BuildType}__${HostArch}.wrn"
        $BuildErr = Join-Path $LogsDir "${BuildLogRootName}_${TargetOS}__${TargetArch}__${BuildType}__${HostArch}.err"
        $BinLog = Join-Path $LogsDir "${BuildLogRootName}_${TargetOS}__${TargetArch}__${BuildType}__${HostArch}.binlog"

        $MsbuildLog = "/flp:Verbosity=normal;LogFile=`"$BuildLog`""
        $MsbuildWrn = "/flp1:WarningsOnly;LogFile=`"$BuildWrn`""
        $MsbuildErr = "/flp2:ErrorsOnly;LogFile=`"$BuildErr`""
        $MsbuildBinLog = "/bl:`"$BinLog`""
        $Logging = "$MsbuildLog $MsbuildWrn $MsbuildErr $MsbuildBinLog"

        $CmakeBuildToolArgs = @()
        if (-not $Ninja) {
            # Pass /m flag for MSBuild parallelism
            $CmakeBuildToolArgs = @('/nologo', '/m', $Logging -split ' ')
        }

        $cmakeTargetStr = $CMakeTarget -join ' '
        Write-Host "running `"$CMakePath`" --build `"$IntermediatesDir`" --target $cmakeTargetStr --config $BuildType -- $($CmakeBuildToolArgs -join ' ')"
        
        if ($CmakeBuildToolArgs.Count -gt 0) {
            & $CMakePath --build $IntermediatesDir --target $cmakeTargetStr --config $BuildType -- @CmakeBuildToolArgs
        } else {
            & $CMakePath --build $IntermediatesDir --target $cmakeTargetStr --config $BuildType
        }
        
        if ($LASTEXITCODE -ne 0) {
            Write-BuildError "Error: native component build failed. Refer to the build log files for details."
            Write-Host "    $BuildLog"
            Write-Host "    $BuildWrn"
            Write-Host "    $BuildErr"
            exit $LASTEXITCODE
        }

        if ($EnforcePgo) {
            $coreclrDll = Join-Path $BinDir "coreclr.dll"
            $clrjitDll = Join-Path $BinDir "clrjit.dll"
            $pgoCheckScript = Join-Path $ProjectDir "scripts\pgocheck.py"
            
            Write-Host "`"$PythonCmd`" `"$pgoCheckScript`" `"$coreclrDll`" `"$clrjitDll`""
            & $PythonCmd $pgoCheckScript $coreclrDll $clrjitDll
            
            if ($LASTEXITCODE -ne 0) {
                Write-BuildError "Error: Error running pgocheck.py on coreclr and clrjit."
                exit $LASTEXITCODE
            }
        }
    }
}

Write-BuildMessage "Build succeeded. Finished at $(Get-Date -Format 'HH:mm:ss')"
Write-BuildMessage "Product binaries are available at $BinDir"

exit 0
