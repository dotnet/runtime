// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;

using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace ILCompiler.Metadata.Ecma;

/// <summary>
/// Serializes reflection metadata as ECMA-335 metadata roots sharing a string heap.
/// </summary>
public static class EcmaMetadataTransform
{
    private const int MetadataVersionLengthOffset = 12;
    private const int MetadataVersionOffset = 16;
    private const int StorageHeaderSize = 4;
    private const int StreamHeaderPrefixSize = 8;
    private const int StreamAlignment = 4;

    /// <summary>
    /// Generates concatenated metadata roots for the modules selected by the metadata policy.
    /// Optionally writes metadata-only DLLs to <paramref name="outputDirectory"/>.
    /// </summary>
    /// <remarks>
    /// Includes reflection metadata only, not the additional records used exclusively for stack traces.
    /// Each root's <c>#Strings</c> stream points to a shared heap following the concatenated roots.
    /// A reader's memory span must extend from its root through the shared heap.
    /// The inspection DLLs retain complete, self-contained metadata.
    /// </remarks>
    public static byte[] Run<TPolicy>(TPolicy policy, IEnumerable<ModuleDesc> modules, string outputDirectory = null)
        where TPolicy : struct, IMetadataPolicy
    {
        var sortedModules = new List<EcmaModule>();
        foreach (ModuleDesc module in modules)
            sortedModules.Add((EcmaModule)module);
        sortedModules.Sort(TypeSystemComparer.Instance.Compare);

        if (outputDirectory is not null)
            Directory.CreateDirectory(outputDirectory);

        var strings = new HashSet<string>(StringComparer.Ordinal);
        var builders = new List<(EcmaModule Module, MetadataBuilder Builder)>();
        foreach (EcmaModule module in sortedModules)
        {
            var transform = new Transform<TPolicy>(module, policy, strings);
            if (!transform.HasMetadata)
                continue;

            builders.Add((module, transform.Generate()));
        }

        if (builders.Count == 0)
            return Array.Empty<byte>();

        var roots = new List<(byte[] Data, int StringsHeaderOffset)>(builders.Count);
        byte[] sharedStrings = null;
        int sharedStringsOffset = 0;
        foreach ((EcmaModule module, MetadataBuilder builder) in builders)
        {
            // SRM sorts and suffix-folds the same set into an identical heap, independent of
            // handle assignment order. It also selects the index width for the global heap.
            foreach (string value in strings)
                builder.GetOrAddString(value);

            var metadataRoot = new MetadataRootBuilder(builder, module.MetadataReader.MetadataVersion);
            byte[] metadata;
            if (outputDirectory is null)
            {
                var blob = new BlobBuilder();
                metadataRoot.Serialize(blob, methodBodyStreamRva: 0, mappedFieldDataStreamRva: 0);
                metadata = blob.ToArray();
            }
            else
            {
                var pe = new ManagedPEBuilder(
                    new PEHeaderBuilder(imageCharacteristics: Characteristics.ExecutableImage | Characteristics.Dll),
                    metadataRoot,
                    ilStream: new BlobBuilder(),
                    deterministicIdProvider: static _ => default);
                var peBlob = new BlobBuilder();
                pe.Serialize(peBlob);
                // SRM serialization consumes the heap builders. Extract the metadata from
                // this serialization instead of attempting to serialize the root again.
                using var reader = new PEReader(peBlob.ToImmutableArray());
                metadata = ImmutableCollectionsMarshal.AsArray(reader.GetMetadata().GetContent());
                AssemblyNameInfo name = module.Assembly.GetName();
                string directory = outputDirectory;
                if (!string.IsNullOrEmpty(name.CultureName))
                {
                    directory = Path.Combine(directory, name.CultureName);
                    Directory.CreateDirectory(directory);
                }
                using FileStream stream = File.Create(Path.Combine(directory, name.Name + ".dll"));
                peBlob.WriteContentTo(stream);
            }

            (byte[] data, int stringsHeaderOffset) = CompactRoot(metadata, ref sharedStrings);
            sharedStringsOffset = checked(sharedStringsOffset + data.Length);
            roots.Add((data, stringsHeaderOffset));
        }

        var result = new BlobBuilder();
        foreach ((byte[] data, int stringsHeaderOffset) in roots)
        {
            BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(stringsHeaderOffset), sharedStringsOffset - result.Count);
            result.WriteBytes(data);
        }
        result.WriteBytes(sharedStrings);

        return result.ToArray();
    }

    private static (byte[] Data, int StringsHeaderOffset) CompactRoot(byte[] metadata, ref byte[] sharedStrings)
    {
        // ECMA-335 II.24.2.1: the padded version string is followed by the storage
        // header and variable-length stream headers, whose offsets are root-relative.
        ReadOnlySpan<byte> source = metadata;
        int versionLength = BinaryPrimitives.ReadInt32LittleEndian(source.Slice(MetadataVersionLengthOffset));
        int storageHeaderOffset = checked(MetadataVersionOffset + versionLength);
        int streamCount = BinaryPrimitives.ReadUInt16LittleEndian(source.Slice(storageHeaderOffset + sizeof(ushort)));
        int position = checked(storageHeaderOffset + StorageHeaderSize);
        var streamHeaders = new int[streamCount];
        int stringsHeaderOffset = -1;
        int stringsOffset = 0;
        int stringsSize = 0;
        for (int i = 0; i < streamCount; i++)
        {
            streamHeaders[i] = position;
            int nameOffset = checked(position + StreamHeaderPrefixSize);
            int nameLength = source.Slice(nameOffset).IndexOf((byte)0);
            if (source.Slice(nameOffset, nameLength).SequenceEqual("#Strings"u8))
            {
                stringsHeaderOffset = position;
                stringsOffset = BinaryPrimitives.ReadInt32LittleEndian(source.Slice(position));
                stringsSize = BinaryPrimitives.ReadInt32LittleEndian(source.Slice(position + sizeof(int)));
            }
            position = checked((nameOffset + nameLength + 1 + StreamAlignment - 1) & -StreamAlignment);
        }

        if (stringsHeaderOffset < 0)
            throw new InvalidOperationException("Serialized ECMA metadata has no #Strings stream.");

        ReadOnlySpan<byte> heap = source.Slice(stringsOffset, stringsSize);
        if (sharedStrings is null)
            sharedStrings = heap.ToArray();
        else if (!heap.SequenceEqual(sharedStrings))
            throw new InvalidOperationException("Serialized ECMA metadata string heaps are not identical.");

        var compacted = new byte[metadata.Length - stringsSize];
        source.Slice(0, stringsOffset).CopyTo(compacted);
        source.Slice(checked(stringsOffset + stringsSize)).CopyTo(compacted.AsSpan(stringsOffset));
        foreach (int headerOffset in streamHeaders)
        {
            int offset = BinaryPrimitives.ReadInt32LittleEndian(source.Slice(headerOffset));
            if (headerOffset != stringsHeaderOffset && offset >= stringsOffset + stringsSize)
                BinaryPrimitives.WriteInt32LittleEndian(compacted.AsSpan(headerOffset), offset - stringsSize);
        }

        return (compacted, stringsHeaderOffset);
    }
}
