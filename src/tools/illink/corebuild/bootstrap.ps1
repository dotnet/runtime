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

$rootToolVersions = Join-Path $RepositoryRoot ".toolversions"
$bootstrapComplete = Join-Path $ToolsLocalPath "bootstrap.complete"

# if the force switch is specified delete the semaphore file if it exists
if ($Force -and (Test-Path $bootstrapComplete))
{
    del $bootstrapComplete
}

# if the semaphore file exists and is identical to the specified version then exit
if ((Test-Path $bootstrapComplete) -and !(Compare-Object (Get-Content $rootToolVersions) (Get-Content $bootstrapComplete)))
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
$rootCliVersion = Join-Path $RepositoryRoot ".cliversion"
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

# create a junction to the shared FX version directory. this is
# so we have a stable path to dotnet.exe regardless of version.
$runtimesPath = Join-Path $CliLocalPath "shared\Microsoft.NETCore.App"
if ($SharedFrameworkVersion -eq "<auto>")
{
    $SharedFrameworkVersion = Get-ChildItem $runtimesPath -Directory | % { New-Object System.Version($_) } | Sort-Object -Descending | Select-Object -First 1
}
$junctionTarget = Join-Path $runtimesPath $SharedFrameworkVersion
$junctionParent = Split-Path $SharedFrameworkSymlinkPath -Parent
if (-Not (Test-Path $junctionParent))
{
    mkdir $junctionParent | Out-Null
}
if (-Not (Test-Path $SharedFrameworkSymlinkPath))
{
    cmd.exe /c mklink /j $SharedFrameworkSymlinkPath $junctionTarget | Out-Null
}

# create a project.csproj for the packages to restore
$projectCsproj = Join-Path $ToolsLocalPath "project.csproj"
$pcContent = "<Project Sdk=`"Microsoft.NET.Sdk`"> <PropertyGroup> <TargetFramework>netcoreapp1.0</TargetFramework> </PropertyGroup> <ItemGroup>"

$tools = Get-Content $rootToolVersions
foreach ($tool in $tools)
{
    $name, $version = $tool.split("=")
    $pcContent = $pcContent + "<PackageReference Include=`"$name`" Version=`"$version`" />"
}
$pcContent = $pcContent + "</ItemGroup> </Project>"
$pcContent | Out-File $projectCsproj

# now restore the packages
$buildToolsSource = "https://dotnet.myget.org/F/dotnet-buildtools/api/v3/index.json"
$nugetOrgSource = "https://api.nuget.org/v3/index.json"
if ($env:buildtools_source -ne $null)
{
    $buildToolsSource = $env:buildtools_source
}
$packagesPath = Join-Path $RepositoryRoot "packages"
$dotNetExe = Join-Path $cliLocalPath "dotnet.exe"
$restoreArgs = "restore $projectCsproj --packages $packagesPath --source $buildToolsSource --source $nugetOrgSource"
$process = Start-Process -Wait -NoNewWindow -FilePath $dotNetExe -ArgumentList $restoreArgs -PassThru
if ($process.ExitCode -ne 0)
{
    exit $process.ExitCode
}

# now stage the contents to tools directory and run any init scripts
foreach ($tool in $tools)
{
    $name, $version = $tool.split("=")

    # verify that the version we expect is what was restored
    $pkgVerPath = Join-Path $packagesPath "$name\$version"
    if ((Test-Path $pkgVerPath) -eq 0)
    {
        Write-Output "Directory '$pkgVerPath' doesn't exist, ensure that the version restored matches the version specified."
        exit 1
    }

    # at present we have the following conventions when staging package content:
    #   1.  if a package contains a "tools" directory then recursively copy its contents
    #       to a directory named the package ID that's under $ToolsLocalPath.
    #   2.  if a package contains a "libs" directory then recursively copy its contents
    #       under the $ToolsLocalPath directory.
    #   3.  if a package contains a file "lib\init-tools.cmd" execute it.

    if (Test-Path (Join-Path $pkgVerPath "tools"))
    {
        $destination = Join-Path $ToolsLocalPath $name
        mkdir $destination | Out-Null
        copy (Join-Path $pkgVerPath "tools\*") $destination -recurse
    }
    elseif (Test-Path (Join-Path $pkgVerPath "lib"))
    {
        copy (Join-Path $pkgVerPath "lib\*") $ToolsLocalPath -recurse
    }

    if (Test-Path (Join-Path $pkgVerPath "lib\init-tools.cmd"))
    {
        cmd.exe /c (Join-Path $pkgVerPath "lib\init-tools.cmd") $RepositoryRoot $dotNetExe $ToolsLocalPath | Out-File (Join-Path $RepositoryRoot "Init-$name.log")
    }
}

# write semaphore file
copy $rootToolVersions $bootstrapComplete
exit 0
