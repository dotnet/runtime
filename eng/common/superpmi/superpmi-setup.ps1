# Setups up machine to send payload to Helix machine to do superpmi collection
#
# 1. Copies CORE_ROOT in Payload\PmiAssembliesDirectory\Core_Root directory.
# 2. Copies Test binaries in Payload\PmiAssembliesDirectory\Tests directory.
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

$PayloadDirectory = (Join-Path $SourceDirectory "Payload")
$SuperPmiDirectory = (Join-Path $PayloadDirectory "superpmi")
$JitUtilsDirectory = (Join-Path $PayloadDirectory "jitutils")
$PmiAssembliesDirectory = (Join-Path $PayloadDirectory "PmiAssembliesDirectory")
$WorkItemDirectory = (Join-Path $SourceDirectory "workitem")
$Queue = "Windows.10.Amd64"

if($Architecture -eq 'arm64') {
    $Queue = "Windows.10.Arm64"
}

$HelixSourcePrefix = "official"
$Creator = ""

# Prepare payloads
robocopy $SourceDirectory\src\coreclr\scripts $SuperPmiDirectory /E /XD $PayloadDirectory $SourceDirectory\artifacts $SourceDirectory\.git
robocopy $CoreRootDirectory $PmiAssembliesDirectory\Core_Root /E
# robocopy $ManagedTestArtifactDirectory $PmiAssembliesDirectory\Tests /E /XD $CoreRootDirectory

New-Item -Path $WorkItemDirectory -Name "placeholder.txt" -ItemType "file" -Value "Placeholder file." -Force

Write-Host "Cloning and building JitUtilsDirectory"

git clone --branch master --depth 1 --quiet https://github.com/dotnet/jitutils $JitUtilsDirectory
pushd $JitUtilsDirectory

#TODO: Try using UseDotNet so we don't have to do this
$env:PATH = "$SourceDirectory\.dotnet;$env:PATH"
Write-Host "dotnet PATH: $env:PATH"
.\bootstrap.cmd

# Set variables that we will need to have in future steps
$ci = $true

. "$PSScriptRoot\..\pipeline-logging-functions.ps1"

# Directories
Write-PipelineSetVariable -Name 'PayloadDirectory' -Value "$PayloadDirectory" -IsMultiJobVariable $false
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