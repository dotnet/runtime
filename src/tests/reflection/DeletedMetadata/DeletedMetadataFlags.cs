// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;
using Xunit;

public class DeletedMetadataFlags
{
    private const byte DeltaOnly = 0x20;
    private const byte HasDelete = 0x80;
    private const byte HasDeleteAndDeltaOnly = HasDelete | DeltaOnly;

    [Theory]
    [InlineData(false, 0)]
    [InlineData(true, 0)]
    [InlineData(false, HasDelete)]
    [InlineData(true, HasDelete)]
    public static void MetadataLoadsWithOrWithoutReservedDeleteFlag(bool uncompressed, byte heapSizes)
    {
        Assembly assembly = Assembly.Load(BuildImage(uncompressed, heapSizes));
        Assert.NotNull(assembly.ManifestModule.GetField("Live"));
        Assert.NotNull(assembly.ManifestModule.GetField("_Deleted"));
    }

    [Theory]
    [InlineData(false, HasDeleteAndDeltaOnly)]
    [InlineData(true, HasDeleteAndDeltaOnly)]
    [InlineData(false, DeltaOnly)]
    [InlineData(true, DeltaOnly)]
    public static void MetadataWithDeltaOnlyFlagIsRejected(bool uncompressed, byte heapSizes)
    {
        Assert.Throws<BadImageFormatException>(() => Assembly.Load(BuildImage(uncompressed, heapSizes)));
    }

    private static byte[] BuildImage(bool uncompressed, byte heapSizes)
    {
        var strings = new MemoryStream();
        strings.WriteByte(0);
        int AddString(string s)
        {
            int offset = (int)strings.Position;
            strings.Write(Encoding.UTF8.GetBytes(s));
            strings.WriteByte(0);
            return offset;
        }

        int moduleTypeName = AddString("<Module>");
        int moduleName = AddString("DeletedMetadataFlags.Generated.dll");
        int liveName = AddString("Live");
        int deletedName = AddString("_Deleted");
        int assemblyName = AddString("DeletedMetadataFlags.Generated");

        // Blob heap: empty blob followed by the field signature FIELD I4.
        byte[] blob = [0x00, 0x02, 0x06, 0x08];
        const ushort fieldSignature = 1;

        var tables = new BinaryWriter(new MemoryStream());
        tables.Write(0u);           // Reserved
        tables.Write((byte)2);      // MajorVersion
        tables.Write((byte)0);      // MinorVersion
        tables.Write(heapSizes);    // HeapSizes flags
        tables.Write((byte)1);      // Reserved
        tables.Write((1UL << 0x00) | (1UL << 0x02) | (uncompressed ? 1UL << 0x03 : 0) | (1UL << 0x04) | (1UL << 0x20)); // Valid
        tables.Write(0UL);          // Sorted
        tables.Write(1u);           // Module
        tables.Write(1u);           // TypeDef
        if (uncompressed)
        {
            tables.Write(2u);       // FieldPtr
        }
        tables.Write(2u);           // Field
        tables.Write(1u);           // Assembly

        // Module: Generation, Name, Mvid, EncId, EncBaseId
        tables.Write((ushort)0);
        tables.Write((ushort)moduleName);
        tables.Write((ushort)1);
        tables.Write((ushort)0);
        tables.Write((ushort)0);

        // TypeDef <Module>: Flags, Name, Namespace, Extends, FieldList, MethodList
        tables.Write(0u);
        tables.Write((ushort)moduleTypeName);
        tables.Write((ushort)0);
        tables.Write((ushort)0);
        tables.Write((ushort)1);
        tables.Write((ushort)1);

        if (uncompressed)
        {
            // FieldPtr: the logical field order is the reverse of the physical Field rows.
            tables.Write((ushort)2);
            tables.Write((ushort)1);
        }

        // Field 1: public static int Live
        tables.Write((ushort)(FieldAttributes.Public | FieldAttributes.Static));
        tables.Write((ushort)liveName);
        tables.Write(fieldSignature);

        // Field 2: a field with the historical deleted-record name.
        tables.Write((ushort)(FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.SpecialName | FieldAttributes.RTSpecialName));
        tables.Write((ushort)deletedName);
        tables.Write(fieldSignature);

        // Assembly: HashAlgId, Version, Flags, PublicKey, Name, Culture
        tables.Write((uint)AssemblyHashAlgorithm.Sha1);
        tables.Write((ushort)1);
        tables.Write((ushort)0);
        tables.Write((ushort)0);
        tables.Write((ushort)0);
        tables.Write(0u);
        tables.Write((ushort)0);
        tables.Write((ushort)assemblyName);
        tables.Write((ushort)0);

        (string Name, byte[] Data)[] streams =
        [
            (uncompressed ? "#-" : "#~", Pad(((MemoryStream)tables.BaseStream).ToArray())),
            ("#Strings", Pad(strings.ToArray())),
            ("#US", new byte[4]),
            ("#GUID", new Guid("00000000-0000-0000-0000-000000000001").ToByteArray()),
            ("#Blob", Pad(blob)),
        ];

        var metadata = new BinaryWriter(new MemoryStream());
        metadata.Write(0x424A5342u); // Signature
        metadata.Write((ushort)1);   // MajorVersion
        metadata.Write((ushort)1);   // MinorVersion
        metadata.Write(0u);          // Reserved
        byte[] version = Pad(Encoding.ASCII.GetBytes("v4.0.30319\0"));
        metadata.Write(version.Length);
        metadata.Write(version);
        metadata.Write((ushort)0);   // Flags
        metadata.Write((ushort)streams.Length);

        int offset = (int)metadata.BaseStream.Position + streams.Sum(s => 8 + Pad(Encoding.ASCII.GetBytes(s.Name + "\0")).Length);
        foreach ((string name, byte[] data) in streams)
        {
            metadata.Write(offset);
            metadata.Write(data.Length);
            metadata.Write(Pad(Encoding.ASCII.GetBytes(name + "\0")));
            offset += data.Length;
        }

        foreach ((_, byte[] data) in streams)
        {
            metadata.Write(data);
        }

        var peBuilder = new RawMetadataPEBuilder(((MemoryStream)metadata.BaseStream).ToArray());
        var image = new BlobBuilder();
        peBuilder.Serialize(image);
        return image.ToArray();
    }

    private static byte[] Pad(byte[] bytes)
    {
        byte[] result = new byte[(bytes.Length + 3) & ~3];
        bytes.CopyTo(result, 0);
        return result;
    }

    private sealed class RawMetadataPEBuilder : PEBuilder
    {
        private const int CorHeaderSize = 72;
        private readonly byte[] _metadata;
        private int _textRva;

        public RawMetadataPEBuilder(byte[] metadata)
            : base(PEHeaderBuilder.CreateLibraryHeader(), deterministicIdProvider: null)
        {
            _metadata = metadata;
        }

        protected override ImmutableArray<Section> CreateSections() =>
            [new Section(".text", SectionCharacteristics.ContainsCode | SectionCharacteristics.MemRead | SectionCharacteristics.MemExecute)];

        protected override BlobBuilder SerializeSection(string name, SectionLocation location)
        {
            _textRva = location.RelativeVirtualAddress;

            var section = new BlobBuilder();
            section.WriteInt32(CorHeaderSize);
            section.WriteUInt16(2); // MajorRuntimeVersion
            section.WriteUInt16(5); // MinorRuntimeVersion
            section.WriteInt32(_textRva + CorHeaderSize);
            section.WriteInt32(_metadata.Length);
            section.WriteInt32((int)CorFlags.ILOnly);
            section.WriteInt32(0); // EntryPointToken
            for (int i = 0; i < 6; i++)
            {
                section.WriteInt64(0); // Resources, StrongNameSignature, CodeManagerTable, VTableFixups, ExportAddressTableJumps, ManagedNativeHeader
            }

            section.WriteBytes(_metadata);
            return section;
        }

        protected override PEDirectoriesBuilder GetDirectories() =>
            new PEDirectoriesBuilder { CorHeaderTable = new DirectoryEntry(_textRva, CorHeaderSize) };
    }
}
