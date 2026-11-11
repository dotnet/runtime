param (
    [Parameter(Mandatory = $true)]
    [string] $outputPath,

    [Parameter(Mandatory = $true)]
    [string] $fileVersion,

    [Parameter(Mandatory = $true)]
    [string] $productVersion,

    [Parameter(Mandatory = $true)]
    [string] $companyName,

    [Parameter(Mandatory = $true)]
    [string] $productName,

    [Parameter(Mandatory = $true)]
    [string] $legalCopyright
)

function Escape-ResourceString {
    param (
        [Parameter(Mandatory = $true)]
        [string] $value
    )

    return $value.Replace('\', '\\').Replace('"', '\"')
}

$versionParts = $fileVersion.Split('.')
if ($versionParts.Length -ne 4) {
    throw "The file version '$fileVersion' does not contain four components."
}

foreach ($part in $versionParts) {
    [uint16] $parsedPart = 0
    if (-not [uint16]::TryParse($part, [ref] $parsedPart)) {
        throw "The file version component '$part' is not a valid unsigned 16-bit integer."
    }
}

$kitsRoot = (Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Windows Kits\Installed Roots').KitsRoot10
$resourceCompiler = Get-ChildItem -Path (Join-Path $kitsRoot 'bin\*\x64\rc.exe') |
    Sort-Object -Property { [version] $_.Directory.Parent.Name } -Descending |
    Select-Object -First 1
if ($null -eq $resourceCompiler) {
    throw "Unable to locate the Windows SDK resource compiler under '$kitsRoot'."
}

$sdkVersion = $resourceCompiler.Directory.Parent.Name
$sdkIncludeRoot = Join-Path $kitsRoot "Include\$sdkVersion"
$sdkIncludeDirectories = @('shared', 'um', 'ucrt', 'winrt') |
    ForEach-Object { Join-Path $sdkIncludeRoot $_ }
foreach ($includeDirectory in $sdkIncludeDirectories) {
    if (-not [System.IO.Directory]::Exists($includeDirectory)) {
        throw "Unable to locate the Windows SDK include directory '$includeDirectory'."
    }
}

$outputDirectory = Split-Path -Parent $outputPath
[System.IO.Directory]::CreateDirectory($outputDirectory) | Out-Null
$resourceSource = Join-Path $outputDirectory 'mscordaccore.version.rc'
$numericVersion = $versionParts -join ','

$resourceContents = @"
#include <windows.h>

1 VERSIONINFO
FILEVERSION $numericVersion
PRODUCTVERSION $numericVersion
FILEFLAGSMASK VS_FFI_FILEFLAGSMASK
FILEFLAGS 0
FILEOS VOS__WINDOWS32
FILETYPE VFT_DLL
FILESUBTYPE VFT2_UNKNOWN
BEGIN
    BLOCK "StringFileInfo"
    BEGIN
        BLOCK "040904B0"
        BEGIN
            VALUE "CompanyName", "$(Escape-ResourceString $companyName)"
            VALUE "FileDescription", "mscordaccore"
            VALUE "FileVersion", "$(Escape-ResourceString $fileVersion)"
            VALUE "InternalName", "mscordaccore.dll"
            VALUE "LegalCopyright", "$(Escape-ResourceString $legalCopyright)"
            VALUE "OriginalFilename", "mscordaccore.dll"
            VALUE "ProductName", "$(Escape-ResourceString $productName)"
            VALUE "ProductVersion", "$(Escape-ResourceString $productVersion)"
        END
    END

    BLOCK "VarFileInfo"
    BEGIN
        VALUE "Translation", 0x0409, 1200
    END
END
"@

[System.IO.File]::WriteAllText(
    $resourceSource,
    $resourceContents,
    [System.Text.UnicodeEncoding]::new($false, $true))

$resourceCompilerArguments = @('/nologo', '/fo', $outputPath)
foreach ($includeDirectory in $sdkIncludeDirectories) {
    $resourceCompilerArguments += @('/i', $includeDirectory)
}
$resourceCompilerArguments += $resourceSource

& $resourceCompiler.FullName $resourceCompilerArguments
if ($LASTEXITCODE -ne 0) {
    throw "The Windows SDK resource compiler failed with exit code $LASTEXITCODE."
}
