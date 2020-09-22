# Setups up machine to send payload to Helix machine to do superpmi collection
#
# 1. Copies CORE_ROOT in Payload\pmiAssembliesDirectory\Core_Root directory.
# 2. Copies Test binaries in Payload\pmiAssembliesDirectory\Tests directory.
# 3. Copies Sources\src\coreclr\scripts in Payload\superpmi directory.
# 4. Clones dotnet/jitutils repo in Payload/jitutils directory.
# 5. Build jitutils

Param(
    [string] $SourceDirectory=$env:BUILD_SOURCESDIRECTORY,
    [string] $CoreRootDirectory,
    [string] $ManagedTestArtifactDirectory,
    [string] $Architecture="x64",
    [string] $Framework="net5.0"
)

Write-Host "CORE_ROOT is" $CoreRootDirectory
Write-Host "Test artifacts is " $ManagedTestArtifactDirectory

# WorkItem Directories
$WorkItemDirectory = (Join-Path $SourceDirectory "workitem")
$PmiAssembliesDirectory = (Join-Path $WorkItemDirectory "pmiAssembliesDirectory")

# CorrelationPayload Directories
$CorrelationPayloadDirectory = (Join-Path $SourceDirectory "Payload")
$SuperPmiDirectory = (Join-Path $CorrelationPayloadDirectory "superpmi")
$JitUtilsDirectory = (Join-Path $CorrelationPayloadDirectory "jitutils")

$Queue = "Windows.10.Amd64"

if($Architecture -eq 'arm64') {
    $Queue = "Windows.10.Arm64"
}

$HelixSourcePrefix = "official"
$Creator = ""

# Minimum binaries from CORE_ROOT needed to run superpmi.py
$super_pmi_dlls = @(
 "clrjit.dll",
 "coreclr.dll",
 "CoreRun.exe",
 "mcs.exe",
 "superpmi.exe",
 "System.Private.CoreLib.dll"
)

# Prepare WorkItemDirectories
robocopy $CoreRootDirectory $PmiAssembliesDirectory\Core_Root\binaries /E /XF *.pdb
# robocopy $ManagedTestArtifactDirectory $PmiAssembliesDirectory\Tests /E /XD $CoreRootDirectory /XF *.pdb

# Prepare CorrelationPayloadDirectories
robocopy $SourceDirectory\src\coreclr\scripts $SuperPmiDirectory /E
robocopy $CoreRootDirectory $SuperPmiDirectory $($super_pmi_dlls)

Write-Host "Cloning and building JitUtilsDirectory"

git clone --branch master --depth 1 --quiet https://github.com/dotnet/jitutils $JitUtilsDirectory
pushd $JitUtilsDirectory

#TODO: Try using UseDotNet so we don't have to do this
$env:PATH = "$SourceDirectory\.dotnet;$env:PATH"
Write-Host "dotnet PATH: $env:PATH"
.\bootstrap.cmd

robocopy $JitUtilsDirectory\bin $SuperPmiDirectory "pmi.dll"
pushd $SourceDirectory
Remove-Item $JitUtilsDirectory -Recurse -Force

Write-Host "Printing files in $WorkItemDirectory"
Get-ChildItem -Path $WorkItemDirectory -Recurse -Name

# Set variables that we will need to have in future steps
$ci = $true

. "$PSScriptRoot\..\pipeline-logging-functions.ps1"

# Directories
Write-PipelineSetVariable -Name 'CorrelationPayloadDirectory' -Value "$CorrelationPayloadDirectory" -IsMultiJobVariable $false
Write-PipelineSetVariable -Name 'SuperPMIDirectory' -Value "$SuperPMIDirectory" -IsMultiJobVariable $false
Write-PipelineSetVariable -Name 'JitUtilsDirectory' -Value "$JitUtilsDirectory" -IsMultiJobVariable $false
Write-PipelineSetVariable -Name 'PmiAssembliesDirectory' -Value "$PmiAssembliesDirectory" -IsMultiJobVariable $false
Write-PipelineSetVariable -Name 'WorkItemDirectory' -Value "$WorkItemDirectory" -IsMultiJobVariable $false

# Script Arguments
Write-PipelineSetVariable -Name 'Python' -Value "py -3" -IsMultiJobVariable $false
Write-PipelineSetVariable -Name 'Architecture' -Value "$Architecture" -IsMultiJobVariable $false

# Helix Arguments
Write-PipelineSetVariable -Name 'Creator' -Value "$Creator" -IsMultiJobVariable $false
Write-PipelineSetVariable -Name 'Queue' -Value "$Queue" -IsMultiJobVariable $false
Write-PipelineSetVariable -Name 'HelixSourcePrefix' -Value "$HelixSourcePrefix" -IsMultiJobVariable $false
Write-PipelineSetVariable -Name '_BuildConfig' -Value "$Architecture.$Framework" -IsMultiJobVariable $false

exit 0