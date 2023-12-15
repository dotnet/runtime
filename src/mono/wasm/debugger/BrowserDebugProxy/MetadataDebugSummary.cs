// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Threading;
using Microsoft.FileFormats.PE;

namespace Microsoft.WebAssembly.Diagnostics;

/// <summary>
///   Information we can extract directly from the assembly image using metadata readers
/// </summary>
internal sealed class MetadataDebugSummary
{
    internal MetadataReader? PdbMetadataReader { get; private init; }
    internal bool IsPortableCodeView { get; private init; }
    internal PdbChecksum[] PdbChecksums { get; private init; }

    internal CodeViewDebugDirectoryData? CodeViewData { get; private init; }

    private MetadataDebugSummary(MetadataReader? pdbMetadataReader, bool isPortableCodeView, PdbChecksum[] pdbChecksums, CodeViewDebugDirectoryData? codeViewData)
    {
        PdbMetadataReader = pdbMetadataReader;
        IsPortableCodeView = isPortableCodeView;
        PdbChecksums = pdbChecksums;
        CodeViewData = codeViewData;
    }

    internal static MetadataDebugSummary Create(MonoProxy monoProxy, SessionId sessionId, string name, IDebugMetadataProvider provider, byte[]? pdb, CancellationToken token)
    {
        var entries = provider.ReadDebugDirectory();
        CodeViewDebugDirectoryData? codeViewData = null;
        bool isPortableCodeView = false;
        List<PdbChecksum> pdbChecksums = new();
        DebugDirectoryEntry? embeddedPdbEntry = null;
        foreach (var entry in entries)
        {
            switch (entry.Type)
            {
                case DebugDirectoryEntryType.CodeView:
                    codeViewData = provider.ReadCodeViewDebugDirectoryData(entry);
                    if (entry.IsPortableCodeView)
                        isPortableCodeView = true;
                    break;
                case DebugDirectoryEntryType.PdbChecksum:
                    var checksum = provider.ReadPdbChecksumDebugDirectoryData(entry);
                    pdbChecksums.Add(new PdbChecksum(checksum.AlgorithmName, checksum.Checksum.ToArray()));
                    break;
                case DebugDirectoryEntryType.EmbeddedPortablePdb:
                    embeddedPdbEntry = entry;
                    break;
                default:
                    break;
            }
        }

        MetadataReader? pdbMetadataReader = null;
        if (pdb != null)
        {
            var pdbStream = new MemoryStream(pdb);
            try
            {
                // MetadataReaderProvider.FromPortablePdbStream takes ownership of the stream
                pdbMetadataReader = MetadataReaderProvider.FromPortablePdbStream(pdbStream).GetMetadataReader();
            }
            catch (BadImageFormatException)
            {
                monoProxy.SendLog(sessionId, $"Warning: Unable to read debug information of: {name} (use DebugType=Portable/Embedded)", token);
            }
        }
        else
        {
            if (embeddedPdbEntry != null && embeddedPdbEntry.Value.DataSize != 0)
            {
                pdbMetadataReader = provider.ReadEmbeddedPortablePdbDebugDirectoryData(embeddedPdbEntry.Value).GetMetadataReader();
            }
        }

        return new MetadataDebugSummary(pdbMetadataReader, isPortableCodeView, pdbChecksums.ToArray(), codeViewData);
    }
}
