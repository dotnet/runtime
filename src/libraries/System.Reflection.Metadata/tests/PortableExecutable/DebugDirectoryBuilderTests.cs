// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Reflection.Internal;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Tests;
using Xunit;

namespace System.Reflection.PortableExecutable.Tests
{
    public class DebugDirectoryBuilderTests
    {
        [Fact]
        public void AddEmbeddedPortablePdbEntry_Args()
        {
            var bb = new BlobBuilder();

            var builder = new DebugDirectoryBuilder();
            Assert.Throws<ArgumentNullException>(() => builder.AddEmbeddedPortablePdbEntry(null, 0x0100));
            Assert.Throws<ArgumentOutOfRangeException>(() => builder.AddEmbeddedPortablePdbEntry(bb, 0x0000));
            Assert.Throws<ArgumentOutOfRangeException>(() => builder.AddEmbeddedPortablePdbEntry(bb, 0x00ff));
            builder.AddEmbeddedPortablePdbEntry(bb, 0x0100);
            builder.AddEmbeddedPortablePdbEntry(bb, 0xffff);
        }

        [Fact]
        public void AddCodeViewEntry_Args()
        {
            var builder = new DebugDirectoryBuilder();
            AssertExtensions.Throws<ArgumentException>("pdbPath", () => builder.AddCodeViewEntry("", default(BlobContentId), 0x0100));
            AssertExtensions.Throws<ArgumentException>("pdbPath", () => builder.AddCodeViewEntry("\0", default(BlobContentId), 0x0100));
            AssertExtensions.Throws<ArgumentException>("pdbPath", () => builder.AddCodeViewEntry("\0xx", default(BlobContentId), 0x0100));
            Assert.Throws<ArgumentOutOfRangeException>(() => builder.AddCodeViewEntry("xx", default(BlobContentId), 0x0100, int.MinValue));
            Assert.Throws<ArgumentOutOfRangeException>(() => builder.AddCodeViewEntry("xx", default(BlobContentId), 0x0100, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => builder.AddCodeViewEntry("xx", default(BlobContentId), 0x0100, 0));
            builder.AddCodeViewEntry("foo\0", default(BlobContentId), 0x0100);
            builder.AddCodeViewEntry("baz\0", default(BlobContentId), 0x0100, int.MaxValue);
            Assert.Throws<ArgumentNullException>(() => builder.AddCodeViewEntry(null, default(BlobContentId), 0x0100));
            builder.AddCodeViewEntry("foo", default(BlobContentId), 0);
            Assert.Throws<ArgumentOutOfRangeException>(() => builder.AddCodeViewEntry("foo", default(BlobContentId), 0x0001));
            Assert.Throws<ArgumentOutOfRangeException>(() => builder.AddCodeViewEntry("foo", default(BlobContentId), 0x00ff));
            builder.AddCodeViewEntry("foo", default(BlobContentId), 0x0100);
            builder.AddCodeViewEntry("foo", default(BlobContentId), 0xffff);
        }

        [Fact]
        public void AddPdbChecksumEntry_Args()
        {
            var builder = new DebugDirectoryBuilder();
            AssertExtensions.Throws<ArgumentNullException>("algorithmName", () => builder.AddPdbChecksumEntry(null, ImmutableArray.Create((byte)1)));
            AssertExtensions.Throws<ArgumentException>("algorithmName", () => builder.AddPdbChecksumEntry("", ImmutableArray.Create((byte)1)));
            AssertExtensions.Throws<ArgumentNullException>("checksum", () => builder.AddPdbChecksumEntry("XXX", default));
            AssertExtensions.Throws<ArgumentException>("checksum", () => builder.AddPdbChecksumEntry("XXX", ImmutableArray<byte>.Empty));
        }

        [Fact]
        public void Empty()
        {
            var b = new DebugDirectoryBuilder();
            var id = new BlobContentId(new Guid("3C88E66E-E0B9-4508-9290-11E0DB51A1C5"), 0x12345678);

            var blob = new BlobBuilder();
            b.Serialize(blob, new SectionLocation(0x1000, 0x2000), 0x50);
            AssertEx.Equal(new byte[0], blob.ToArray());
        }

        [Fact]
        public void AddCodeViewEntry1()
        {
            var b = new DebugDirectoryBuilder();
            var id = new BlobContentId(new Guid("3C88E66E-E0B9-4508-9290-11E0DB51A1C5"), 0x12345678);
            b.AddCodeViewEntry("foo.pdb", id, 0);

            var blob = new BlobBuilder();
            b.Serialize(blob, new SectionLocation(0x1000, 0x2000), 0x50);
            var bytes = blob.ToArray();

            AssertEx.Equal(new byte[]
            {
                0x00, 0x00, 0x00, 0x00, // Characteristics
                0x78, 0x56, 0x34, 0x12, // Stamp
                0x00, 0x00, 0x00, 0x00, // Version
                0x02, 0x00, 0x00, 0x00, // Type
                0x20, 0x00, 0x00, 0x00, // SizeOfData
                0x6C, 0x10, 0x00, 0x00, // AddressOfRawData
                0x6C, 0x20, 0x00, 0x00, // PointerToRawData
                // data
                (byte)'R', (byte)'S', (byte)'D', (byte)'S',
                0x6E, 0xE6, 0x88, 0x3C, 0xB9, 0xE0, 0x08, 0x45, 0x92, 0x90, 0x11, 0xE0, 0xDB, 0x51, 0xA1, 0xC5, // GUID
                0x01, 0x00, 0x00, 0x00, // age
                (byte)'f', (byte)'o', (byte)'o', (byte)'.', (byte)'p', (byte)'d', (byte)'b', 0x00 // path
            }, bytes);

            using (var pinned = new PinnedBlob(bytes))
            {
                var actual = PEReader.ReadDebugDirectoryEntries(pinned.CreateReader(0, DebugDirectoryEntry.Size));
                Assert.Equal(1, actual.Length);
                Assert.Equal(id.Stamp, actual[0].Stamp);
                Assert.Equal(0, actual[0].MajorVersion);
                Assert.Equal(0, actual[0].MinorVersion);
                Assert.Equal(DebugDirectoryEntryType.CodeView, actual[0].Type);
                Assert.False(actual[0].IsPortableCodeView);
                Assert.Equal(0x00000020, actual[0].DataSize);
                Assert.Equal(0x0000106c, actual[0].DataRelativeVirtualAddress);
                Assert.Equal(0x0000206c, actual[0].DataPointer);
            }
        }

        [Fact]
        public void AddCodeViewEntry2()
        {
            var b = new DebugDirectoryBuilder();
            var id = new BlobContentId(new Guid("3C88E66E-E0B9-4508-9290-11E0DB51A1C5"), 0x12345678);
            b.AddCodeViewEntry("foo.pdb" + new string('\0', 260 - "foo.pdb".Length - 1), id, 0xABCD, 0x99);

            var blob = new BlobBuilder();
            b.Serialize(blob, new SectionLocation(0x1000, 0x2000), 0x50);
            var bytes = blob.ToArray();

            AssertEx.Equal(new byte[]
            {
                0x00, 0x00, 0x00, 0x00, // Characteristics
                0x78, 0x56, 0x34, 0x12, // Stamp
                0xCD, 0xAB, 0x4D, 0x50, // Version
                0x02, 0x00, 0x00, 0x00, // Type
                0x1C, 0x01, 0x00, 0x00, // SizeOfData
                0x6C, 0x10, 0x00, 0x00, // AddressOfRawData
                0x6C, 0x20, 0x00, 0x00, // PointerToRawData
                // data
                (byte)'R', (byte)'S', (byte)'D', (byte)'S',
                0x6E, 0xE6, 0x88, 0x3C, 0xB9, 0xE0, 0x08, 0x45, 0x92, 0x90, 0x11, 0xE0, 0xDB, 0x51, 0xA1, 0xC5, // GUID
                0x99, 0x00, 0x00, 0x00, // age
                (byte)'f', (byte)'o', (byte)'o', (byte)'.', (byte)'p', (byte)'d', (byte)'b', 0x00, // path
                // path padding:
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            }, bytes);

            using (var pinned = new PinnedBlob(bytes))
            {
                var actual = PEReader.ReadDebugDirectoryEntries(pinned.CreateReader(0, DebugDirectoryEntry.Size));
                Assert.Equal(1, actual.Length);
                Assert.Equal(id.Stamp, actual[0].Stamp);
                Assert.Equal(0xABCD, actual[0].MajorVersion);
                Assert.Equal(0x504d, actual[0].MinorVersion);
                Assert.Equal(DebugDirectoryEntryType.CodeView, actual[0].Type);
                Assert.True(actual[0].IsPortableCodeView);
                Assert.Equal(0x0000011c, actual[0].DataSize);
                Assert.Equal(0x0000106c, actual[0].DataRelativeVirtualAddress);
                Assert.Equal(0x0000206c, actual[0].DataPointer);
            }
        }

        [Fact]
        public void AddReproducibleEntry()
        {
            var b = new DebugDirectoryBuilder();
            b.AddReproducibleEntry();

            var blob = new BlobBuilder();
            b.Serialize(blob, new SectionLocation(0x1000, 0x2000), 0x50);
            var bytes = blob.ToArray();

            AssertEx.Equal(new byte[]
            {
                0x00, 0x00, 0x00, 0x00, // Characteristics
                0x00, 0x00, 0x00, 0x00, // Stamp
                0x00, 0x00, 0x00, 0x00, // Version
                0x10, 0x00, 0x00, 0x00, // Type
                0x00, 0x00, 0x00, 0x00, // SizeOfData
                0x00, 0x00, 0x00, 0x00, // AddressOfRawData
                0x00, 0x00, 0x00, 0x00, // PointerToRawData
            }, bytes);

            using (var pinned = new PinnedBlob(bytes))
            {
                var actual = PEReader.ReadDebugDirectoryEntries(pinned.CreateReader(0, DebugDirectoryEntry.Size));
                Assert.Equal(1, actual.Length);
                Assert.Equal(0u, actual[0].Stamp);
                Assert.Equal(0, actual[0].MajorVersion);
                Assert.Equal(0, actual[0].MinorVersion);
                Assert.Equal(DebugDirectoryEntryType.Reproducible, actual[0].Type);
                Assert.False(actual[0].IsPortableCodeView);
                Assert.Equal(0, actual[0].DataSize);
                Assert.Equal(0, actual[0].DataRelativeVirtualAddress);
                Assert.Equal(0, actual[0].DataPointer);
            }
        }

        [Fact]
        public void MultipleEntries()
        {
            var b = new DebugDirectoryBuilder();
            var id = new BlobContentId(new Guid("3C88E66E-E0B9-4508-9290-11E0DB51A1C5"), 0x12345678);

            b.AddReproducibleEntry();
            b.AddCodeViewEntry("x", id, 0);
            b.AddReproducibleEntry();
            b.AddCodeViewEntry("y", id, 0xABCD);

            var blob = new BlobBuilder();
            b.Serialize(blob, new SectionLocation(0x1000, 0x2000), 0x50);
            AssertEx.Equal(new byte[]
            {
                0x00, 0x00, 0x00, 0x00, // Characteristics
                0x00, 0x00, 0x00, 0x00, // Stamp
                0x00, 0x00, 0x00, 0x00, // Version
                0x10, 0x00, 0x00, 0x00, // Type
                0x00, 0x00, 0x00, 0x00, // SizeOfData
                0x00, 0x00, 0x00, 0x00, // AddressOfRawData
                0x00, 0x00, 0x00, 0x00, // PointerToRawData

                0x00, 0x00, 0x00, 0x00, // Characteristics
                0x78, 0x56, 0x34, 0x12, // Stamp
                0x00, 0x00, 0x00, 0x00, // Version
                0x02, 0x00, 0x00, 0x00, // Type
                0x1A, 0x00, 0x00, 0x00, // SizeOfData
                0xC0, 0x10, 0x00, 0x00, // AddressOfRawData
                0xC0, 0x20, 0x00, 0x00, // PointerToRawData

                0x00, 0x00, 0x00, 0x00, // Characteristics
                0x00, 0x00, 0x00, 0x00, // Stamp
                0x00, 0x00, 0x00, 0x00, // Version
                0x10, 0x00, 0x00, 0x00, // Type
                0x00, 0x00, 0x00, 0x00, // SizeOfData
                0x00, 0x00, 0x00, 0x00, // AddressOfRawData
                0x00, 0x00, 0x00, 0x00, // PointerToRawData

                0x00, 0x00, 0x00, 0x00, // Characteristics
                0x78, 0x56, 0x34, 0x12, // Stamp
                0xCD, 0xAB, 0x4D, 0x50, // Version
                0x02, 0x00, 0x00, 0x00, // Type
                0x1A, 0x00, 0x00, 0x00, // SizeOfData
                0xDA, 0x10, 0x00, 0x00, // AddressOfRawData
                0xDA, 0x20, 0x00, 0x00, // PointerToRawData

                // data
                (byte)'R', (byte)'S', (byte)'D', (byte)'S',
                0x6E, 0xE6, 0x88, 0x3C, 0xB9, 0xE0, 0x08, 0x45, 0x92, 0x90, 0x11, 0xE0, 0xDB, 0x51, 0xA1, 0xC5, // GUID
                0x01, 0x00, 0x00, 0x00, // age
                (byte)'x', 0x00, // path

                // data
                (byte)'R', (byte)'S', (byte)'D', (byte)'S',
                0x6E, 0xE6, 0x88, 0x3C, 0xB9, 0xE0, 0x08, 0x45, 0x92, 0x90, 0x11, 0xE0, 0xDB, 0x51, 0xA1, 0xC5, // GUID
                0x01, 0x00, 0x00, 0x00, // age
                (byte)'y', 0x00, // path
            }, blob.ToArray());
        }

        [Fact]
        public void EmbeddedPortablePdb()
        {
            var b = new DebugDirectoryBuilder();

            var pdb = new BlobBuilder();
            pdb.WriteInt64(0x1122334455667788);

            b.AddEmbeddedPortablePdbEntry(pdb, portablePdbVersion: 0x0100);

            var blob = new BlobBuilder();
            b.Serialize(blob, new SectionLocation(0, 0), sectionOffset: 0);
            var bytes = blob.ToImmutableArray();

            AssertEx.Equal(new byte[]
            {
                0x00, 0x00, 0x00, 0x00, // Characteristics
                0x00, 0x00, 0x00, 0x00, // Stamp
                0x00, 0x01, 0x00, 0x01, // Version
                0x11, 0x00, 0x00, 0x00, // Type
                0x12, 0x00, 0x00, 0x00, // SizeOfData
                0x1C, 0x00, 0x00, 0x00, // AddressOfRawData
                0x1C, 0x00, 0x00, 0x00, // PointerToRawData

                0x4D, 0x50, 0x44, 0x42, // signature
                0x08, 0x00, 0x00, 0x00, // uncompressed size
                0xEB, 0x28, 0x4F, 0x0B, 0x75, 0x31, 0x56, 0x12, 0x04, 0x00 // compressed data
            }, bytes);

            using var pinned = new PinnedBlob(bytes);
            var actual = PEReader.ReadDebugDirectoryEntries(pinned.CreateReader(0, DebugDirectoryEntry.Size));
            Assert.Equal(1, actual.Length);
            Assert.Equal(0u, actual[0].Stamp);
            Assert.Equal(0x0100, actual[0].MajorVersion);
            Assert.Equal(0x0100, actual[0].MinorVersion);
            Assert.Equal(DebugDirectoryEntryType.EmbeddedPortablePdb, actual[0].Type);
            Assert.False(actual[0].IsPortableCodeView);
            Assert.Equal(0x00000012, actual[0].DataSize);
            Assert.Equal(0x0000001c, actual[0].DataRelativeVirtualAddress);
            Assert.Equal(0x0000001c, actual[0].DataPointer);

            var provider = new ByteArrayMemoryProvider(bytes);
            using var block = provider.GetMemoryBlock(actual[0].DataPointer, actual[0].DataSize);
            using var decoded = PEReader.DecodeEmbeddedPortablePdbDebugDirectoryData(block);
            AssertEx.Equal(new byte[] { 0x88, 0x77, 0x66, 0x55, 0x44, 0x33, 0x22, 0x11 }, decoded.GetContentUnchecked(0, decoded.Size));
        }

        [Fact]
        public void AddPdbChecksumEntry()
        {
            var b = new DebugDirectoryBuilder();

            b.AddPdbChecksumEntry("A", ImmutableArray.Create(new byte[] { 0x01 }));
            b.AddPdbChecksumEntry("B", ImmutableArray.Create(new byte[] { 0x02, 0x03 }));
            b.AddPdbChecksumEntry("XYZ", ImmutableArray.Create(new byte[] { 0x04, 0x05, 0x06 }));

            var blob = new BlobBuilder();
            b.Serialize(blob, new SectionLocation(0x1000, 0x2000), 0x50);
            AssertEx.Equal(new byte[]
            {
                0x00, 0x00, 0x00, 0x00, // Characteristics
                0x00, 0x00, 0x00, 0x00, // Stamp
                0x01, 0x00, 0x00, 0x00, // Version
                0x13, 0x00, 0x00, 0x00, // Type
                0x03, 0x00, 0x00, 0x00, // SizeOfData
                0xA4, 0x10, 0x00, 0x00, // AddressOfRawData
                0xA4, 0x20, 0x00, 0x00, // PointerToRawData

                0x00, 0x00, 0x00, 0x00, // Characteristics
                0x00, 0x00, 0x00, 0x00, // Stamp
                0x01, 0x00, 0x00, 0x00, // Version
                0x13, 0x00, 0x00, 0x00, // Type
                0x04, 0x00, 0x00, 0x00, // SizeOfData
                0xA7, 0x10, 0x00, 0x00, // AddressOfRawData
                0xA7, 0x20, 0x00, 0x00, // PointerToRawData

                0x00, 0x00, 0x00, 0x00, // Characteristics
                0x00, 0x00, 0x00, 0x00, // Stamp
                0x01, 0x00, 0x00, 0x00, // Version
                0x13, 0x00, 0x00, 0x00, // Type
                0x07, 0x00, 0x00, 0x00, // SizeOfData
                0xAB, 0x10, 0x00, 0x00, // AddressOfRawData
                0xAB, 0x20, 0x00, 0x00, // PointerToRawData

                // data
                (byte)'A', 0x00,
                0x01,

                // data
                (byte)'B', 0x00,
                0x02, 0x03,

                // data
                (byte)'X', (byte)'Y', (byte)'Z', 0x00,
                0x04, 0x05, 0x06,
            }, blob.ToArray());
        }

        [Fact]
        public void AddCustomEntry()
        {
            var b = new DebugDirectoryBuilder();

            b.AddEntry((DebugDirectoryEntryType)0xA1, version: 0x12345678, stamp: 0xB1C1D1E1);

            b.AddEntry((DebugDirectoryEntryType)0xA2, version: 0xFFFFFFFF, stamp: 0xFFFFFFFF, (a: 1, b: 2), (builder, data) =>
            {
                builder.WriteInt32(data.a);
                builder.WriteInt32(data.b);
            });

            var blob = new BlobBuilder();
            b.Serialize(blob, new SectionLocation(0x1000, 0x2000), 0x50);
            AssertEx.Equal(new byte[]
            {
                0x00, 0x00, 0x00, 0x00, // Characteristics
                0xE1, 0xD1, 0xC1, 0xB1, // Stamp
                0x78, 0x56, 0x34, 0x12, // Version
                0xA1, 0x00, 0x00, 0x00, // Type
                0x00, 0x00, 0x00, 0x00, // SizeOfData
                0x00, 0x00, 0x00, 0x00, // AddressOfRawData
                0x00, 0x00, 0x00, 0x00, // PointerToRawData

                0x00, 0x00, 0x00, 0x00, // Characteristics
                0xFF, 0xFF, 0xFF, 0xFF, // Stamp
                0xFF, 0xFF, 0xFF, 0xFF, // Version
                0xA2, 0x00, 0x00, 0x00, // Type
                0x08, 0x00, 0x00, 0x00, // SizeOfData
                0x88, 0x10, 0x00, 0x00, // AddressOfRawData
                0x88, 0x20, 0x00, 0x00, // PointerToRawData

                // data
                0x01, 0x00, 0x00, 0x00,
                0x02, 0x00, 0x00, 0x00
            }, blob.ToArray());
        }
    }
}
