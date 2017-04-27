Param(
    [string]$WixFilename="wix.3.10.2.zip",
    [string]$OutputDir="."
)

$uri = "https://dotnetcli.blob.core.windows.net/build/wix/" + $wixFilename
$outFile = $OutputDir + "\" + $wixFilename

if (!(Test-Path "$OutputDir"))
{
    mkdir "$OutputDir" | Out-Null
}

if(!(Test-Path "$outFile"))
{
    Write-Output "Downloading WixTools to $outFile.."
    Write-Output $uri
    Invoke-WebRequest -Uri $uri -OutFile $outFile
}