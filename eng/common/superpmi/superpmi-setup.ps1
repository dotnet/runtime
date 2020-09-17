Param(
    [string] $SourceDirectory=$env:BUILD_SOURCESDIRECTORY,
    [string] $CoreRootDirectory,
    [string] $ManagedTestArtifactDirectory,
    [string] $BaselineCoreRootDirectory,
    [string] $Architecture="x64",
    [string] $Framework="net5.0",
    [string] $CompilationMode="Tiered",
    [string] $Repository=$env:BUILD_REPOSITORY_NAME,
    [string] $Branch=$env:BUILD_SOURCEBRANCH,
    [string] $CommitSha=$env:BUILD_SOURCEVERSION,
    [string] $BuildNumber=$env:BUILD_BUILDNUMBER,
    [string] $RunCategories="Libraries Runtime",
    [string] $Csproj="src\benchmarks\micro\MicroBenchmarks.csproj",
    [string] $Configurations="CompilationMode=$CompilationMode RunKind=$Kind"
)

# 1. clone the repo
# 2. do setup on dotnet/runtime
# 3. copy 

Write-Host "CORE_ROOT is" $CoreRootDirectory
Write-Host "Test artifacts is " $ManagedTestArtifactDirectory

$RunFromPerformanceRepo = ($Repository -eq "dotnet/jitutils") -or ($Repository -eq "dotnet-jitutils")
$PayloadDirectory = (Join-Path $SourceDirectory "Payload")
$SuperPmiDirectory = (Join-Path $PayloadDirectory "superpmi")
$JitUtilsDirectory = (Join-Path $PayloadDirectory "jitutils")
$PmiAssembliesDirectory = (Join-Path $PayloadDirectory "PmiAssembliesDirectory")
$WorkItemDirectory = (Join-Path $SourceDirectory "workitem")
$Queue = "windows.10.amd64.clientrs5"
$HelixSourcePrefix = "official"
$Creator = $env:BUILD_DEFINITIONNAME

robocopy $SourceDirectory\src\coreclr\scripts $SuperPmiDirectory /E /XD $PayloadDirectory $SourceDirectory\artifacts $SourceDirectory\.git

Write-Host "Downloading CoreClr_Build"
$url = "https://dev.azure.com/dnceng/_apis/resources/Containers/5103993/CoreCLRProduct__Windows_NT_x64_checked?itemPath=CoreCLRProduct__Windows_NT_x64_checked%2FCoreCLRProduct__Windows_NT_x64_checked.zip"
$tmp = New-TemporaryFile | Rename-Item -NewName { $_ -replace 'tmp$', 'zip' } -PassThru

$start_time = Get-Date
(New-Object System.Net.WebClient).DownloadFile($url, $tmp)
$tmp | Expand-Archive -DestinationPath $PmiAssembliesDirectory\Core_Root -Force
Write-Host "Time taken: $((Get-Date).Subtract($start_time).Seconds) second(s)"

# robocopy $CoreRootDirectory $PmiAssembliesDirectory\Core_Root /E
# robocopy $ManagedTestArtifactDirectory $PmiAssembliesDirectory\Tests /E /XD $CoreRootDirectory

New-Item -Path $WorkItemDirectory -Name "placeholder.txt" -ItemType "file" -Value "Placeholder file." -Force

Write-Host "Cloning into JitUtilsDirectory"

git clone --branch master --depth 1 --quiet https://github.com/dotnet/jitutils $JitUtilsDirectory
pushd $JitUtilsDirectory
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