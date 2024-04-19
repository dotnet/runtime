. "$PSScriptRoot/../common/tools.ps1" # for Retry function

function DownloadClangTool {
    param (
        [string]
        $toolName,
        [string]
        $downloadOutputPath
    )

    $clangVersion = "17.0.6"
    $clangToolsRootUrl = "https://clrjit2.blob.core.windows.net/clang-tools"
    $clangPlatform = "windows-x64"

    $toolUrl = "$clangToolsRootUrl/$clangVersion/$clangPlatform/$toolName.exe"
    $targetPath = "$downloadOutputPath\$toolName.exe" 

    if (-not $(ls $downloadOutputPath | Where-Object { $_.Name -eq "$toolName.exe" })) {

        Retry({
            Write-Output "Downloading '$toolUrl' to '$targetPath'"
            # Pass -PassThru as otherwise Invoke-WebRequest leaves a corrupted file if the download fails. With -PassThru the download is buffered first.
            # -UseBasicParsing is necessary for older PowerShells when Internet Explorer might not be installed/configured
            $null = Invoke-WebRequest -Uri "$toolUrl" -OutFile $(Join-Path $downloadOutputPath -ChildPath "$toolName.exe") -PassThru -UseBasicParsing
        })
    }
    else {
        Write-Output "Found '$targetPath'"
    }
}

$downloadPathFolder = Split-Path $PSScriptRoot -Parent | Split-Path -Parent | Join-Path -ChildPath "artifacts" | Join-Path -ChildPath "tools"

mkdir $downloadPathFolder -ErrorAction SilentlyContinue

DownloadClangTool "clang-format" "$downloadPathFolder"
DownloadClangTool "clang-tidy" "$downloadPathFolder"

# Add to path to enable scripts to skip additional downloading steps since the tools will already be on the path.
$env:PATH = "$downloadPathFolder;$env:PATH"
