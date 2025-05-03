[CmdletBinding()]
param (
    [Parameter(Mandatory)]
    [string]
    $DownloadDirectory,
    [Parameter(Mandatory)]
    [string]
    $ExtractDirectory
)

Add-Type -Assembly 'System.IO.Compression.FileSystem'

Write-Host "Looking for packages under $DownloadDirectory"

foreach ($file in Get-ChildItem $DownloadDirectory -Recurse -Filter '*.nupkg') {
    Write-Host "Found Package: $($file.FullName)"
    if ($file.Name -match '^(?<id>Microsoft.NETCore.App.Runtime.linux(-musl)?-((arm(64)?)|x64)).(?<ver>.+).nupkg$') {
        $id = $matches['id']
        $ver = $matches['ver']
        Write-Host "Extracting Package: $id $ver to $ExtractDirectory/$($id.ToLowerInvariant())/$ver"
        [System.IO.Compression.ZipFile]::ExtractToDirectory($file.FullName, "$ExtractDirectory/$($id.ToLowerInvariant())/$ver")
    } else {
        Write-Host "Skipping non-runtime pack: $($file.Name)"
    }
}
