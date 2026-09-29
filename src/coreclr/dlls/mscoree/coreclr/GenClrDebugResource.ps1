param (
    [string]
    $dac,
    [string]
    $dbi,
    [string]
    $out
)

function Parse-Int {
    param (
        [parameter(Mandatory = $true, ValueFromPipeline = $true)]
        [string]$hexValue
    )
    return [System.Int32]::Parse($hexValue, [System.Globalization.NumberStyles]::HexNumber)
}

$resourceStream = [System.IO.MemoryStream]::new()
$clrDebugResource = [System.IO.BinaryWriter]::new($resourceStream)

try {
    # We're creating the resource with the following layout (represented as a C struct)
    # struct CLR_DEBUG_RESOURCE
    # {
    #   int version;
    #   GUID clrSkuGuid;
    #   int dacTimeStamp;
    #   int dacImageSize;
    #   int dbiTimeStamp;
    #   int dbiImageSize;
    # };

    # Write the debug resource version
    $clrDebugResource.Write(0);
    # Write the GUID for CoreCLR (should match the CLR_ID_ONECORE_CLR GUID in clrinternal.idl)
    $clrDebugResource.Write([System.Guid]::Parse("{0xb1ee760d,0x6c4a,0x4533,{0xba,0x41,0x6f,0x4f,0x66,0x1f,0xab,0xaf}}").ToByteArray())
    [int]$dacTimeStamp = dumpbin $dac /HEADERS | Select-String "([0-9A-Fa-f]+) time date stamp" | %{ $_.Matches.Groups[1].Value } | Parse-Int
    [int]$dacImageSize = dumpbin $dac /HEADERS | Select-String "([0-9A-Fa-f]+) size of image" | %{ $_.Matches.Groups[1].Value } | Parse-Int
    [int]$dbiTimeStamp = dumpbin $dbi /HEADERS | Select-String "([0-9A-Fa-f]+) time date stamp" | %{ $_.Matches.Groups[1].Value } | Parse-Int
    [int]$dbiImageSize = dumpbin $dbi /HEADERS | Select-String "([0-9A-Fa-f]+) size of image" | %{ $_.Matches.Groups[1].Value } | Parse-Int
    $clrDebugResource.Write($dacTimeStamp)
    $clrDebugResource.Write($dacImageSize)
    $clrDebugResource.Write($dbiTimeStamp)
    $clrDebugResource.Write($dbiImageSize)
}
finally {
    $clrDebugResource.Dispose()
}

$resourceBytes = $resourceStream.ToArray()
$writeResource = -not [System.IO.File]::Exists($out)

if (-not $writeResource) {
    $existingBytes = [System.IO.File]::ReadAllBytes($out)
    $writeResource = $existingBytes.Length -ne $resourceBytes.Length

    for ($i = 0; -not $writeResource -and $i -lt $resourceBytes.Length; $i++) {
        $writeResource = $existingBytes[$i] -ne $resourceBytes[$i]
    }
}

if ($writeResource) {
    [System.IO.File]::WriteAllBytes($out, $resourceBytes)
}
