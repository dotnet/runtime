param (
    [string]$filename = "",
    [string]$jump = "",
    [string]$prefix1 = "",
    [string]$prefix2 = ""
)

# Function to display usage information
function Show-Usage {
    Write-Host "Usage:"
    Write-Host "generateredefinesfile.ps1 <filename> <jump instruction for the platform> <prefix of what is being mapped from> <prefix of what is being mapped to>"
    exit 1
}

if ($filename.Length -eq 0) {
    Show-Usage
}

# Read the file line by line
Get-Content $filename | ForEach-Object {
    $line = $_.Trim()

    # Skip empty lines and comment lines starting with semicolon
    if ($line -match '^\;.*$' -or $line -match '^[\s]*$') {
        return
    }

    # Remove the CR character in case the sources are mapped from
    # a Windows share and contain CRLF line endings
    $line = $line -replace "`r", ""

    # Only process the entries that begin with "#"
    if ($line -match '^#.*$') {
        $line = $line -replace '^#', ''
        Write-Output "LEAF_ENTRY ${prefix1}${line}, _TEXT"
        Write-Output "    ${jump} EXTERNAL_C_FUNC(${prefix2}${line})"
        Write-Output "LEAF_END ${prefix1}${line}, _TEXT"
        Write-Output ""
    }
}
