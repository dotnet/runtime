# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Assembly,
    [Parameter(Mandatory)][string]$Output
)

$ErrorActionPreference = 'Stop'
$assemblyPath = (Resolve-Path -LiteralPath $Assembly).ProviderPath
$pdbPath = [System.IO.Path]::ChangeExtension($assemblyPath, '.pdb')
$peStream = [System.IO.File]::OpenRead($assemblyPath)
$pdbStream = [System.IO.File]::OpenRead($pdbPath)
$pe = [System.Reflection.PortableExecutable.PEReader]::new($peStream)
$provider = [System.Reflection.Metadata.MetadataReaderProvider]::FromPortablePdbStream($pdbStream)
try {
    $reader = $provider.GetMetadataReader()
    $id = [byte[]]$reader.DebugMetadataHeader.Id
    $pdbGuid = [guid]::new([byte[]]$id[0..15])
    $codeView = @($pe.ReadDebugDirectory() | Where-Object Type -eq CodeView)
    if ($codeView.Count -ne 1) {
        throw 'Expected one PE CodeView record.'
    }
    $binding = $pe.ReadCodeViewDebugDirectoryData($codeView[0])
    if ($binding.Guid -ne $pdbGuid -or $binding.Age -ne 1) {
        throw 'Portable PDB does not match the assembly CodeView identity.'
    }
    $documents = foreach ($handle in $reader.Documents) {
        $document = $reader.GetDocument($handle)
        $path = $reader.GetString($document.Name)
        $algorithm = $reader.GetGuid($document.HashAlgorithm)
        if ($algorithm -ne [guid]'8829d00f-11b8-4213-878b-770e8597ac16') {
            throw "Expected SHA256 for compiler document $path."
        }
        $expected = [Convert]::ToHexString($reader.GetBlobBytes($document.Hash)).ToLowerInvariant()
        $actual = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()
        if ($actual -ne $expected) {
            throw "Compiler document differs from portable PDB: $path."
        }
        [pscustomobject]@{ path = $path; sha256 = $actual }
    }
    [pscustomobject]@{
        assembly = $assemblyPath
        assembly_sha256 = (Get-FileHash -LiteralPath $assemblyPath -Algorithm SHA256).Hash.ToLowerInvariant()
        pdb = $pdbPath
        pdb_sha256 = (Get-FileHash -LiteralPath $pdbPath -Algorithm SHA256).Hash.ToLowerInvariant()
        guid = $pdbGuid.ToString()
        age = $binding.Age
        documents = @($documents)
    } | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $Output
}
finally {
    $provider.Dispose()
    $pe.Dispose()
    $pdbStream.Dispose()
    $peStream.Dispose()
}
