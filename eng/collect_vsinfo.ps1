<#
.PARAMETER ArchiveRunName
Name of the run for vs logs

.NOTES
Returns 0 if succeeds, 1 otherwise
#>
[CmdletBinding(PositionalBinding=$false)]
Param (
  [Parameter(Mandatory=$True)]
  [string] $ArchiveRunName
)

. $PSScriptRoot/common/tools.ps1

$ProgressPreference = "SilentlyContinue"
$LogDir = Join-Path $LogDir $ArchiveRunName
mkdir $LogDir

$vscollect_uri="http://aka.ms/vscollect.exe"
$vscollect="$env:TEMP\vscollect.exe"

if (-not (Test-Path $vscollect)) {
    Retry({
        Write-Host "GET $vscollect_uri"
        Invoke-WebRequest $vscollect_uri -OutFile $vscollect -UseBasicParsing
    })

    if (-not (Test-Path $vscollect)) {
        Write-PipelineTelemetryError -Category 'InitializeToolset' -Message "Unable to download vscollect."
        exit 1
    }
}

&"$vscollect"
Move-Item $env:TEMP\vslogs.zip "$LogDir"

$vswhere = "C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe"
if (-not (Test-Path -Path "$vswhere" -PathType Leaf))
{
    Write-Error "Couldn't locate vswhere at $vswhere"
    exit 1
}

&"$vswhere" -all -prerelease -products * |  Tee-Object -FilePath "$LogDir\vs_where.log"

$vsdir = &"$vswhere" -latest -prerelease -products * -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath

if (-not (Test-Path $vsdir))
{
    $procDumpDir = Join-Path $ToolsDir "procdump"
    $procDumpToolPath = Join-Path $procDumpDir "procdump.exe"
    $procdump_uri = "https://download.sysinternals.com/files/Procdump.zip"

    if (-not (Test-Path $procDumpToolPath)) {
        Retry({
            Write-Host "GET $procdump_uri"
            Invoke-WebRequest $procdump_uri -OutFile "$TempDir\Procdump.zip" -UseBasicParsing
        })

        Expand-Archive -Path "$TempDir\Procdump.zip" $procDumpDir
    }

    &"$procDumpToolPath" -ma -accepteula VSIXAutoUpdate.exe "$LogDir"
}
