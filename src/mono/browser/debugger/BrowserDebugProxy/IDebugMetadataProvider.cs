// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace Microsoft.WebAssembly.Diagnostics;

/// <summary>
///   An adapter on top of MetadataReader and WebcilReader for DebugStore compensating
///   for the lack of a common base class on those two types.
/// </summary>
public interface IDebugMetadataProvider
{
    public ImmutableArray<DebugDirectoryEntry> ReadDebugDirectory();
    public CodeViewDebugDirectoryData ReadCodeViewDebugDirectoryData(DebugDirectoryEntry entry);
    public PdbChecksumDebugDirectoryData ReadPdbChecksumDebugDirectoryData(DebugDirectoryEntry entry);

    public MetadataReaderProvider ReadEmbeddedPortablePdbDebugDirectoryData(DebugDirectoryEntry entry);
}
