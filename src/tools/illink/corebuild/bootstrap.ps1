param
(
    [Parameter(Mandatory=$false)][string]$RepositoryRoot = $PSScriptRoot,
    [Parameter(Mandatory=$false)][string]$ToolsLocalPath = (Join-Path $RepositoryRoot "Tools"),
    [Parameter(Mandatory=$false)][string]$CliLocalPath = (Join-Path $ToolsLocalPath "dotnetcli"),
    [Parameter(Mandatory=$false)][string]$SharedFrameworkSymlinkPath = (Join-Path $ToolsLocalPath "dotnetcli\shared\Microsoft.NETCore.App\version"),
    [Parameter(Mandatory=$false)][string]$SharedFrameworkVersion = "<auto>",
    [Parameter(Mandatory=$false)][string]$Architecture = "<auto>",
    [Parameter(Mandatory=$false)][string]$DotNetInstallBranch = "rel/1.0.0",
    [switch]$Force = $false
)

$rootCliVersion = Join-Path $RepositoryRoot ".cliversion"
$bootstrapComplete = Join-Path $ToolsLocalPath "bootstrap.complete"

# if the force switch is specified delete the semaphore file if it exists
if ($Force -and (Test-Path $bootstrapComplete))
{
    del $bootstrapComplete
}

# if the semaphore file exists and is identical to the specified version then exit
if ((Test-Path $bootstrapComplete) -and !(Compare-Object (Get-Content $rootCliVersion) (Get-Content $bootstrapComplete)))
{
    exit 0
}

$initCliScript = "dotnet-install.ps1"
$dotnetInstallPath = Join-Path $ToolsLocalPath $initCliScript

# blow away the tools directory so we can start from a known state
if (Test-Path $ToolsLocalPath)
{
    # if the bootstrap.ps1 script was downloaded to the tools directory don't delete it
    rd -recurse -force $ToolsLocalPath -exclude "bootstrap.ps1"
}
else
{
    mkdir $ToolsLocalPath | Out-Null
}

# download CLI boot-strapper script
Invoke-WebRequest "https://raw.githubusercontent.com/dotnet/cli/$DotNetInstallBranch/scripts/obtain/dotnet-install.ps1" -OutFile $dotnetInstallPath

# load the version of the CLI
$dotNetCliVersion = Get-Content $rootCliVersion

if (-Not (Test-Path $CliLocalPath))
{
    mkdir $CliLocalPath | Out-Null
}

# now execute the script
Write-Host "$dotnetInstallPath -Version $dotNetCliVersion -InstallDir $CliLocalPath -Architecture ""$Architecture"""
Invoke-Expression "$dotnetInstallPath -Version $dotNetCliVersion -InstallDir $CliLocalPath -Architecture ""$Architecture"""
if ($LastExitCode -ne 0)
{
    Write-Output "The .NET CLI installation failed with exit code $LastExitCode"
    exit $LastExitCode
}

# write semaphore file
copy $rootCliVersion $bootstrapComplete
exit 0
