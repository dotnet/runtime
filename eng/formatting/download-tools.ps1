$downloadPathFolder = Split-Path $PSScriptRoot -Parent | Split-Path -Parent | Join-Path -ChildPath "artifacts" | Join-Path -ChildPath "tools"

mkdir $downloadPathFolder -ErrorAction SilentlyContinue

if (-not $(ls $downloadPathFolder | Where-Object {$_.Name -eq "clang-format.exe"}))
{
    $baseBackoffSeconds = 15;

    $success = $false
    for ($i = 0; $i -lt 5; $i++) {
        $status = Invoke-WebRequest -Uri "https://clrjit.blob.core.windows.net/clang-tools/windows/clang-format.exe" -OutFile $(Join-Path $downloadPathFolder -ChildPath "clang-format.exe")
        echo $status
        if ($status.StatusCode -lt 400)
        {
            $success = $true
            break
        } else {
            Write-Output "Download attempt $($i+1) failed. Trying again in $($baseBackoffSeconds + $baseBackoffSeconds * $i) seconds"
            Start-Sleep -Seconds $($baseBackoffSeconds + $baseBackoffSeconds * $i)
        }
    }
    if (-not $success)
    {
        Write-Output "Failed to download clang-format"
        return 1
    }
}

# Add to path to enable scripts to skip additional downloading steps since the tools will already be on the path.
$env:PATH = "$downloadPathFolder;$env:PATH"
