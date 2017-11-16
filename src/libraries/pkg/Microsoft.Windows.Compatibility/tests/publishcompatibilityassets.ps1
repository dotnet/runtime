$repoRoot = ((get-item $PSScriptRoot).parent.parent.parent.FullName);
$winRID = "win7-x64";
$dotnetPath = -join($repoRoot, "\Tools\dotnetcli\dotnet.exe")
$csprojPath = -join($PSScriptRoot, "\", (Get-ChildItem $PSScriptRoot"\*.csproj" | Select-Object -ExpandProperty Name))
$packagesCachePath = -join($repoRoot, "\packages")
$localPackageSourcePath = -join($repoRoot, "\bin\packages\Debug\")
$packageName = "Microsoft.Windows.Compatibility"

if (!(Test-Path $localPackageSourcePath))
{
	$localPackageSourcePath = -join($repoRoot, "\bin\packages\Release\")
	if (!(Test-Path $localPackageSourcePath))
	{
		Write-Error -Message "Local package source must exist.";
		Exit;
	}
}

function _getPackageVersion()
{
	$searchPattern = -join($localPackageSourcePath, $packageName, ".[0-9].[0-9].[0-9]*.nupkg")
	if (!(Test-Path $searchPattern))
	{
		Write-Error -Message (-join("Didn't find package: Microsoft.Windows.Compatibility in source: ", $localPackageSourcePath, " please run build -allConfigurations"))
		Exit;
	}

	if (!((get-item $searchPattern).FullName -match '([0-9].[0-9].[0-9][-a-z0-9]*)'))
	{
		Write-Error -Message "Package name is invalid"
		Exit;
	}

	return $matches[0]
}

function _restoreAndPublish($targetFramework, $rid, $runtimeFramework, $refDirName)
{
	$packageVersion = _getPackageVersion
    & $dotnetPath restore --packages $packagesCachePath /p:RestoreSources="https://api.nuget.org/v3/index.json;$localPackageSourcePath" /p:TargetFramework=$targetFramework /p:CompatibilityPackageVersion=$packageVersion $csprojPath
    & $dotnetPath publish -r $rid /p:RestoreSources="https://api.nuget.org/v3/index.json;$localPackageSourcePath" /p:TargetFramework=$targetFramework /p:CompatibilityPackageVersion=$packageVersion /p:RuntimeFrameworkVersion=$runtimeFramework $csprojPath

    $outputPath = -join($PSScriptRoot, "\bin\Debug\", $targetFramework, "\", $rid, "\publish\refs\")

    if (!(Test-Path $outputPath))
    {
    	Write-Error -Message (-join("There was an error while publishing for framework: ", $targetFramework))
		Exit;
    }

	Write-Output (-join("Published succedded for: ", $targetFramework))
	
	$refPath = -join($repoRoot, "\bin\ref\", $refDirName)

	if (Test-Path $refPath)
	{
		Remove-Item $refPath -r -force
	}

	New-Item $refPath -ItemType directory
	Copy-Item (-join($outputPath, "*")) $refPath
}

_restoreAndPublish "netcoreapp2.0" $winRID "2.0.0" "netcoreapp20_compat"
_restoreAndPublish "netstandard2.0" $winRID "2.0.0" "netstandard20_compat"