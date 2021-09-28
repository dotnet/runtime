
function DownloadClangTool
{
    param (
        [string]
        $toolName,
        [string]
        $downloadOutputPath
    )

    $baseUri = "https://clrjit.blob.core.windows.net/clang-tools/windows"

    if (-not $(ls $downloadOutputPath | Where-Object {$_.Name -eq "$toolName.exe"}))
    {
        $baseBackoffSeconds = 2;

        $success = $false
        for ($i = 0; $i -lt 5; $i++) {
            echo "Attempting download of '$baseUri/$toolName.exe'"
            $status = Invoke-WebRequest -Uri "$baseUri/$toolName.exe" -OutFile $(Join-Path $downloadOutputPath -ChildPath "$toolName.exe")
            if ($status.StatusCode -lt 400)
            {
                $success = $true
                break
            } else {
                echo "Download attempt $($i+1) failed. Trying again in $($baseBackoffSeconds + $baseBackoffSeconds * $i) seconds"
                Start-Sleep -Seconds $($baseBackoffSeconds + $baseBackoffSeconds * $i)
            }
        }
        if (-not $success)
        {
            Write-Output "Failed to download clang-format"
            return 1
        }
    }
}

$downloadPathFolder = Split-Path $PSScriptRoot -Parent | Split-Path -Parent | Join-Path -ChildPath "artifacts" | Join-Path -ChildPath "tools"

mkdir $downloadPathFolder -ErrorAction SilentlyContinue

DownloadClangTool "clang-format" "$downloadPathFolder"
DownloadClangTool "clang-tidy" "$downloadPathFolder"

# Add to path to enable scripts to skip additional downloading steps since the tools will already be on the path.
$env:PATH = "$downloadPathFolder;$env:PATH"
