
function DownloadClangTool {
    param (
        [string]
        $toolName,
        [string]
        $downloadOutputPath
    )

    $baseUri = "https://clrjit.blob.core.windows.net/clang-tools/windows"

    if (-not $(ls $downloadOutputPath | Where-Object { $_.Name -eq "$toolName.exe" })) {
        $baseBackoffSeconds = 2;

        $success = $false
        for ($i = 0; $i -lt 5; $i++) {
            Write-Output "Attempting download of '$baseUri/$toolName.exe'"
            try {
                # Pass -PassThru as otherwise Invoke-WebRequest leaves a corrupted file if the download fails. With -PassThru the download is buffered first.
                # -UseBasicParsing is necessary for older PowerShells when Internet Explorer might not be installed/configured
                $null = Invoke-WebRequest -Uri "$baseUri/$toolName.exe" -OutFile $(Join-Path $downloadOutputPath -ChildPath "$toolName.exe") -PassThru -UseBasicParsing
                $success = $true
                break
            }
            catch {
                Write-Output $_
            }
			
            Write-Output "Download attempt $($i+1) failed. Trying again in $($baseBackoffSeconds + $baseBackoffSeconds * $i) seconds"
            Start-Sleep -Seconds $($baseBackoffSeconds + $baseBackoffSeconds * $i)
        }
        if (-not $success) {
            Write-Output "Failed to download $toolName"
            throw [System.IO.IOException] "Could not download $toolName"
        }
    }
}

$downloadPathFolder = Split-Path $PSScriptRoot -Parent | Split-Path -Parent | Join-Path -ChildPath "artifacts" | Join-Path -ChildPath "tools"

mkdir $downloadPathFolder -ErrorAction SilentlyContinue

DownloadClangTool "clang-format" "$downloadPathFolder"
DownloadClangTool "clang-tidy" "$downloadPathFolder"

# Add to path to enable scripts to skip additional downloading steps since the tools will already be on the path.
$env:PATH = "$downloadPathFolder;$env:PATH"
