$VersionFolder = $PSScriptRoot
$RepoRoot = (Resolve-Path "$VersionFolder/../../../").Path.TrimEnd("\")

Get-ChildItem -Path "$VersionFolder" -Filter "_version.*" | ForEach-Object {
    $path = $_.FullName
    if ($_.Name -eq "_version.c") {
        # For _version.c, update the commit ID if it has changed from the last build.
        $commit = (git rev-parse HEAD 2>$null)
        if (-not $commit) { $commit = "N/A" }
        $substitute = "static char sccsid[] __attribute__((used)) = `"@(#)Version N/A @Commit: $commit`";"
        $version_file_contents = Get-Content -Path $path | ForEach-Object { $_ -replace "^static.*", $substitute }
        $version_file_destination = "$RepoRoot\\artifacts\\obj\\_version.c"
        $current_contents = ""
        $is_placeholder_file = $false
        if (Test-Path -Path $version_file_destination) {
            $current_contents = Get-Content -Path $version_file_destination -Raw
            $is_placeholder_file = $current_contents -match "@\(#\)Version N/A @Commit:"
        } else {
            $is_placeholder_file = $true
        }
        if ($is_placeholder_file -and $version_file_contents -ne $current_contents) {
            $version_file_contents | Set-Content -Path $version_file_destination
        }
    } elseif (-not (Test-Path -Path "$RepoRoot\\artifacts\\obj\\$($_.Name)")) {
        Copy-Item -Path $path -Destination "$RepoRoot\\artifacts\\obj\\"
    }
}
