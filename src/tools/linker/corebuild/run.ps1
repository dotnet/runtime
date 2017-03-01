# set the base tools directory
$toolsLocalPath = Join-Path $PSScriptRoot "Tools"
$bootStrapperPath = Join-Path $toolsLocalPath "bootstrap.ps1"

# if the boot-strapper script doesn't exist copy it to the tools path
if ((Test-Path $bootStrapperPath) -eq 0)
{
    if ((Test-Path $toolsLocalPath) -eq 0)
    {
        mkdir $toolsLocalPath | Out-Null
    }

    cp "bootstrap.ps1" $bootStrapperPath
}


# now execute it
& $bootStrapperPath (Get-Location) $toolsLocalPath | Out-File (Join-Path (Get-Location) "bootstrap.log")
if ($LastExitCode -ne 0)
{
    Write-Output "Boot-strapping failed with exit code $LastExitCode, see bootstrap.log for more information."
    exit $LastExitCode
}

# execute the tool using the dotnet.exe host
$dotNetExe = Join-Path $toolsLocalPath "dotnetcli\dotnet.exe"
$runExe = Join-Path $toolsLocalPath "Microsoft.DotNet.BuildTools.Run\netcoreapp1.0\run.exe"
& $dotNetExe $runExe $args
exit $LastExitCode
