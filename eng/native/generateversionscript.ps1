param (
    [string]$inputFile,
    [string]$prefix
)

# Print the header
Write-Output "V1.0 {"
Write-Output "    global:"

# Read the input file line by line
Get-Content $inputFile | ForEach-Object {
    $line = $_.Trim()

    # Skip empty lines and comment lines starting with semicolon
    if ($line -match '^\;.*$' -or $line -match '^[\s]*$') {
        return
    }

    # Remove the CR character in case the sources are mapped from
    # a Windows share and contain CRLF line endings
    $line = $line -replace "`r", ""

    # Only prefix the entries that start with "#"
    if ($line -match '^#.*$') {
        $line = $line -replace '^#', ''
        Write-Output "        $prefix$line;"
    } else {
        Write-Output "        $line;"
    }
}

# Print the footer
Write-Output "    local: *;"
Write-Output "};"