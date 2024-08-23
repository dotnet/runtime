// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace Microsoft.WebAssembly.Diagnostics;

public class PortableExecutableDebugMetadataProvider : IDebugMetadataProvider
{
    private readonly PEReader _peReader;
    public PortableExecutableDebugMetadataProvider(PEReader peReader)
    {
        _peReader = peReader;
    }
    public ImmutableArray<DebugDirectoryEntry> ReadDebugDirectory() => _peReader.ReadDebugDirectory();

    public CodeViewDebugDirectoryData ReadCodeViewDebugDirectoryData(DebugDirectoryEntry entry) => _peReader.ReadCodeViewDebugDirectoryData(entry);

    public PdbChecksumDebugDirectoryData ReadPdbChecksumDebugDirectoryData(DebugDirectoryEntry entry) => _peReader.ReadPdbChecksumDebugDirectoryData(entry);

    public MetadataReaderProvider ReadEmbeddedPortablePdbDebugDirectoryData(DebugDirectoryEntry entry) => _peReader.ReadEmbeddedPortablePdbDebugDirectoryData(entry);
}
