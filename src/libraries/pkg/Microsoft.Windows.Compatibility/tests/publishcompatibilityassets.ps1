param (
$frameworkVersion = "3.0",
$runtimeVersion = "3.0.0-*",
$refDirName = "netcoreapp30_compat",
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

	if (!([string]((get-item $searchPattern).FullName) -match '([0-9].[0-9].[0-9][-a-z]*.[0-9]*.[0-9]*)'))
	{
		Write-Error -Message "Package name is invalid"
		Exit;
	}

	if (!$matches)
	{
		Write-Error -Message (-join("Couldn't get package version for: ", $packageName))
		Exit;
	}
	return $matches[1]
}

$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = 1

$repoRoot = ((get-item $PSScriptRoot).parent.parent.parent.FullName)
$dotnetPath = -join($repoRoot, "\.dotnet\dotnet.exe")
$csprojPath = -join($PSScriptRoot, "\", (Get-ChildItem $PSScriptRoot"\*.csproj" | Select-Object -ExpandProperty Name))
$packagesCachePath = -join($repoRoot, "\packages")
$localPackageSourcePath = -join($repoRoot, "\artifacts\packages\Debug\")
$targetFramework = -join("netcoreapp", $frameworkVersion)

if (!(Test-Path $localPackageSourcePath))
{
	$localPackageSourcePath = -join($repoRoot, "\artifacts\packages\Release\")
	if (!(Test-Path $localPackageSourcePath))
	{
		Write-Error -Message (-join('Local package source must exist ', $localPackageSourcePath))
		Exit;
	}
}

$restoreSources = -join("https://dotnet.myget.org/F/aspnetcore-dev/api/v3/index.json;https://api.nuget.org/v3/index.json;https://dotnet.myget.org/F/dotnet-core/api/v3/index.json;https://dotnetfeed.blob.core.windows.net/dotnet-core/index.json;", $localPackageSourcePath)

$compatPackageVersion = _getPackageVersion "Microsoft.Windows.Compatibility"
$privatePackageVersion = _getPackageVersion "Microsoft.Private.CoreFx.NETCoreApp"

Write-Output "Calling dotnet restore"
& $dotnetPath restore --packages $packagesCachePath /p:RestoreSources="$restoreSources" /p:TargetFramework=$targetFramework /p:CompatibilityPackageVersion=$compatPackageVersion /p:PrivateCorefxPackageVersion=$privatePackageVersion /p:RuntimeIdentifiers=$rid $csprojPath

$outputPath = -join($PSScriptRoot, "\bin\Debug\", $targetFramework, "\", $rid, "\publish\")

Write-Output "Calling dotnet publish"
& $dotnetPath publish -r $rid -o $outputPath /p:NugetMonikerVersion=$frameworkVersion /p:RestoreSources="$restoreSources" /p:TargetFramework=$targetFramework /p:CompatibilityPackageVersion=$compatPackageVersion /p:RuntimeFrameworkVersion=$runtimeFramework /p:PrivateCorefxPackageVersion=$privatePackageVersion /p:RuntimeIdentifiers=$rid $csprojPath

if (!(Test-Path $outputPath))
{
	Write-Error -Message (-join("There was an error while publishing for framework: ", $targetFramework))
	Exit;
}

Write-Output (-join("Published succedded for: ", $targetFramework))

$refPath = -join($repoRoot, "\artifacts\bin\ref\", $refDirName)

if (Test-Path $refPath)
{
	Remove-Item $refPath -r -force
}

New-Item $refPath -ItemType directory
Remove-Item (-join($outputPath, "Microsoft.Windows.Compatibility.Validation.dll")) -force
Copy-Item (-join($outputPath,"\refs\", "*.dll")) $refPath
