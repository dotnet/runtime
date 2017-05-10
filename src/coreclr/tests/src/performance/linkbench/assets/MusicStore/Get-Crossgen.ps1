# Gets crossgen.exe
#
# Downloads NuGet.exe
# Installs crossgen.exe and clrjit.dll using NuGet
# Copies the files to $path

param([string]$path = ".")

$ErrorActionPreference = "Stop"

Write-Host -ForegroundColor Green "Installing crossgen.exe to $path"

if (-not (Test-Path $path))
{
    New-Item -Path $path -ItemType Directory -Force | Out-Null
}

$path = Get-Item $path

function Get-NETCoreAppVersion()
{
    if (-not (Test-Path $PSScriptRoot\obj\project.assets.json))
    {
        Write-Error "project.assets.json is missing. do a dotnet restore."
        exit
    }
    
    # ConvertFrom-Json can't be used here as it has an arbitrary size limit.
    [void][System.Reflection.Assembly]::LoadWithPartialName("System.Web.Extensions")
    $serializer = New-Object -TypeName System.Web.Script.Serialization.JavaScriptSerializer 
    $serializer.MaxJsonLength  = 67108864
    
    $json = $serializer.DeserializeObject((Get-Content $PSScriptRoot\obj\project.assets.json -Raw))

    foreach ($name in $json["libraries"].Keys)
    {
        if ($name.StartsWith("Microsoft.NETCore.App/"))
        {
            $version = $name.SubString("Microsoft.NETCore.App/".Length)
            break
        }
    }
    
    return $version
}

$version = Get-NETCoreAppVersion
Write-Host -ForegroundColor Green "autodetected shared framework version $version"

$platform = "win-x64"

$netcoreapppackage = "runtime.$platform.microsoft.netcore.app"
$netcoreappversion = $version

Write-Host -ForegroundColor Green "Getting NuGet.exe"

$nugeturl = "https://dist.nuget.org/win-x86-commandline/v3.4.4/NuGet.exe"
$nugetfeed = "https://dotnet.myget.org/F/dotnet-core/api/v3/index.json"
$nugetexepath = "$path\NuGet.exe"
$wc = New-Object System.Net.WebClient
$wc.DownloadFile($nugeturl, $nugetexepath)


Write-Host -ForegroundColor Green "Getting $netcoreapppackage $netcoreappversion"

& "$nugetexepath" "install", "$netcoreapppackage", "-Source", "$nugetfeed", "-Version", "$netcoreappversion", "-OutputDirectory", "$path"
if ($LastExitCode -ne 0) {
    throw "NuGet install of $netcoreapppackage failed."
}

Copy-Item "$path\$netcoreapppackage.$netcoreappversion\tools\crossgen.exe" "$path\crossgen.exe" -Force
Copy-Item "$path\$netcoreapppackage.$netcoreappversion\runtimes\$platform\native\clrjit.dll" "$path\clrjit.dll" -Force
Remove-Item "$path\$netcoreapppackage.$netcoreappversion\" -recurse

Remove-Item "$path\NuGet.exe"

Write-Host -ForegroundColor Green "Success"
