param (
$targetFramework = "netcoreapp2.1",
$runtimeVersion = "2.1.0-*",
$refDirName = "netcoreapp21_compat",
$rid = "win7-x64"
)

function _getPackageVersion($packageName)
{
	$searchPattern = -join($localPackageSourcePath, $packageName, ".[0-9].[0-9].[0-9]*.nupkg")
	if (!(Test-Path $searchPattern))
	{
		Write-Error -Message (-join("Didn't find package: ", $packageName, " in source: ", $localPackageSourcePath, " please run build -allConfigurations"))
		Exit;
	}

	if (!((get-item $searchPattern).FullName -match '([0-9].[0-9].[0-9][-a-z0-9]*)'))
	{
		Write-Error -Message "Package name is invalid"
		Exit;
	}

	return $matches[0]
}

$repoRoot = ((get-item $PSScriptRoot).parent.parent.parent.FullName);
$dotnetPath = -join($repoRoot, "\Tools\dotnetcli\dotnet.exe")
$csprojPath = -join($PSScriptRoot, "\", (Get-ChildItem $PSScriptRoot"\*.csproj" | Select-Object -ExpandProperty Name))
$packagesCachePath = -join($repoRoot, "\packages")
$localPackageSourcePath = -join($repoRoot, "\bin\packages\Debug\")

if (!(Test-Path $localPackageSourcePath))
{
	$localPackageSourcePath = -join($repoRoot, "\bin\packages\Release\")
	if (!(Test-Path $localPackageSourcePath))
	{
		Write-Error -Message "Local package source must exist.";
		Exit;
	}
}

$restoreSources = -join("https://dotnet.myget.org/F/aspnetcore-dev/api/v3/index.json;https://api.nuget.org/v3/index.json;https://dotnet.myget.org/F/dotnet-core/api/v3/index.json;", $localPackageSourcePath)

$compatPackageVersion = _getPackageVersion "Microsoft.Windows.Compatibility"
$privatePackageVersion = _getPackageVersion "Microsoft.Private.CoreFx.NETCoreApp"

& $dotnetPath restore --packages $packagesCachePath /p:RestoreSources="$restoreSources" /p:TargetFramework=$targetFramework /p:CompatibilityPackageVersion=$compatPackageVersion /p:PrivateCorefxPackageVersion=$privatePackageVersion /p:RID=$rid $csprojPath

& $dotnetPath publish -r $rid /p:RestoreSources="$restoreSources" /p:TargetFramework=$targetFramework /p:CompatibilityPackageVersion=$compatPackageVersion /p:RuntimeFrameworkVersion=$runtimeFramework /p:PrivateCorefxPackageVersion=$privatePackageVersion /p:RID=$rid $csprojPath

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
