// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Microsoft.NET.WebAssembly.Webcil;

namespace Microsoft.WebAssembly.Diagnostics;

public class WebcilDebugMetadataProvider : IDebugMetadataProvider
{
    private readonly WebcilReader _webcilReader;

    public WebcilDebugMetadataProvider(WebcilReader webcilReader)
    {
        _webcilReader = webcilReader;
    }
    public ImmutableArray<DebugDirectoryEntry> ReadDebugDirectory() => _webcilReader.ReadDebugDirectory();

    public CodeViewDebugDirectoryData ReadCodeViewDebugDirectoryData(DebugDirectoryEntry entry) => _webcilReader.ReadCodeViewDebugDirectoryData(entry);

    public PdbChecksumDebugDirectoryData ReadPdbChecksumDebugDirectoryData(DebugDirectoryEntry entry) => _webcilReader.ReadPdbChecksumDebugDirectoryData(entry);

    public MetadataReaderProvider ReadEmbeddedPortablePdbDebugDirectoryData(DebugDirectoryEntry entry) => _webcilReader.ReadEmbeddedPortablePdbDebugDirectoryData(entry);
}
