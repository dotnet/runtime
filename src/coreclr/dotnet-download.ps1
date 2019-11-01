param(
    [parameter(Mandatory=$true)]$DotnetRemotePath,
    [parameter(Mandatory=$true)]$DotnetLocalPath,
    [parameter(Mandatory=$true)]$DotnetPath
)

$retryCount = 0
$success = $false

do {
    try {
        Write-Output "Downloading from $DotnetRemotePath"
        (New-Object Net.WebClient).DownloadFile($DotnetRemotePath, $DotnetLocalPath)
        $success = $true
    } catch { 
        if ($retryCount -ge 6) {
            Write-Output "Maximum of 5 retries exceeded. Aborting"
            throw
        } 
        else { 
            $retryCount++
            $retryTime = 5 * $retryCount
            Write-Output "Download failed. Retrying in $retryTime seconds"
            Start-Sleep -Seconds (5 * $retryCount)
        }
    }
} while ($success -eq $false)

Write-Output "Download finished"
Add-Type -Assembly 'System.IO.Compression.FileSystem' -ErrorVariable AddTypeErrors

if ($AddTypeErrors.Count -eq 0) { 
    [System.IO.Compression.ZipFile]::ExtractToDirectory($DotnetLocalPath, $DotnetPath) 
}
else { 
    (New-Object -com shell.application).namespace($DotnetPath).CopyHere((new-object -com shell.application).namespace($DotnetLocalPath).Items(), 16)
}