// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        public void Errors()
        {
            var b = new DebugDirectoryBuilder();
            Assert.Throws<ArgumentNullException>(() => b.AddCodeViewEntry(null, default(BlobContentId), 0));
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
            b.AddCodeViewEntry("foo.pdb" + new string('\0', 260 - "foo.pdb".Length - 1), id, 0xABCD);

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
                0x01, 0x00, 0x00, 0x00, // age
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

            using (var pinned = new PinnedBlob(bytes))
            {
                var actual = PEReader.ReadDebugDirectoryEntries(pinned.CreateReader(0, DebugDirectoryEntry.Size));
                Assert.Equal(1, actual.Length);
                Assert.Equal(0u, actual[0].Stamp);
                Assert.Equal(0x0100, actual[0].MajorVersion);
                Assert.Equal(0x0100, actual[0].MinorVersion);
                Assert.Equal(DebugDirectoryEntryType.EmbeddedPortablePdb, actual[0].Type);
                Assert.Equal(0x00000012, actual[0].DataSize);
                Assert.Equal(0x0000001c, actual[0].DataRelativeVirtualAddress);
                Assert.Equal(0x0000001c, actual[0].DataPointer);

                var provider = new ByteArrayMemoryProvider(bytes);
                using (var block = provider.GetMemoryBlock(actual[0].DataPointer, actual[0].DataSize))
                {
                    var decoded = PEReader.DecodeEmbeddedPortablePdbDebugDirectoryData(block);
                    AssertEx.Equal(new byte[] { 0x88, 0x77, 0x66, 0x55, 0x44, 0x33, 0x22, 0x11 }, decoded);
                }
            }
        }

        [Fact]
        public void EmbeddedPortablePdb_Errors()
        {
            var bytes1 = ImmutableArray.Create(new byte[]
            {
                0x4D, 0x50, 0x44, 0x42, // signature
                0xFF, 0xFF, 0xFF, 0xFF, // uncompressed size
                0xEB, 0x28, 0x4F, 0x0B, 0x75, 0x31, 0x56, 0x12, 0x04, 0x00 // compressed data
            });

            using (var block = new ByteArrayMemoryProvider(bytes1).GetMemoryBlock(0, bytes1.Length))
            {
                Assert.Throws<BadImageFormatException>(() => PEReader.DecodeEmbeddedPortablePdbDebugDirectoryData(block));
            }

            var bytes2 = ImmutableArray.Create(new byte[]
            {
                0x4D, 0x50, 0x44, 0x42, // signature
                0x09, 0x00, 0x00, 0x00, // uncompressed size
                0xEB, 0x28, 0x4F, 0x0B, 0x75, 0x31, 0x56, 0x12, 0x04, 0x00 // compressed data
            });

            using (var block = new ByteArrayMemoryProvider(bytes2).GetMemoryBlock(0, bytes2.Length))
            {
                Assert.Throws<BadImageFormatException>(() => PEReader.DecodeEmbeddedPortablePdbDebugDirectoryData(block));
            }

            var bytes3 = ImmutableArray.Create(new byte[]
            {
                0x4D, 0x50, 0x44, 0x42, // signature
                0x00, 0x00, 0x00, 0x00, // uncompressed size
                0xEB, 0x28, 0x4F, 0x0B, 0x75, 0x31, 0x56, 0x12, 0x04, 0x00 // compressed data
            });

            using (var block = new ByteArrayMemoryProvider(bytes3).GetMemoryBlock(0, bytes3.Length))
            {
                Assert.Throws<BadImageFormatException>(() => PEReader.DecodeEmbeddedPortablePdbDebugDirectoryData(block));
            }

            var bytes4 = ImmutableArray.Create(new byte[]
            {
                0x4D, 0x50, 0x44, 0x42, // signature
                0xff, 0xff, 0xff, 0x7f, // uncompressed size
                0xEB, 0x28, 0x4F, 0x0B, 0x75, 0x31, 0x56, 0x12, 0x04, 0x00 // compressed data
            });

            using (var block = new ByteArrayMemoryProvider(bytes4).GetMemoryBlock(0, bytes4.Length))
            {
                Assert.Throws<BadImageFormatException>(() => PEReader.DecodeEmbeddedPortablePdbDebugDirectoryData(block));
            }

            var bytes5 = ImmutableArray.Create(new byte[]
            {
                0x4D, 0x50, 0x44, 0x42, // signature
                0x08, 0x00, 0x00, 0x00, // uncompressed size
                0xEF, 0xFF, 0x4F, 0xFF, 0x75, 0x31, 0x56, 0x12, 0x04, 0x00 // compressed data
            });

            using (var block = new ByteArrayMemoryProvider(bytes4).GetMemoryBlock(0, bytes4.Length))
            {
                Assert.Throws<BadImageFormatException>(() => PEReader.DecodeEmbeddedPortablePdbDebugDirectoryData(block));
            }

            var bytes6 = ImmutableArray.Create(new byte[]
            {
                0x4D, 0x50, 0x44, 0x43, // signature
                0x08, 0x00, 0x00, 0x00, // uncompressed size
                0xEB, 0x28, 0x4F, 0x0B, 0x75, 0x31, 0x56, 0x12, 0x04, 0x00 // compressed data
            });

            using (var block = new ByteArrayMemoryProvider(bytes6).GetMemoryBlock(0, bytes6.Length))
            {
                Assert.Throws<BadImageFormatException>(() => PEReader.DecodeEmbeddedPortablePdbDebugDirectoryData(block));
            }

            var bytes7 = ImmutableArray.Create(new byte[]
            {
                0x4D, 0x50, 0x44, 0x43, // signature
                0x08, 0x00, 0x00, 
            });

            using (var block = new ByteArrayMemoryProvider(bytes7).GetMemoryBlock(0, bytes7.Length))
            {
                Assert.Throws<BadImageFormatException>(() => PEReader.DecodeEmbeddedPortablePdbDebugDirectoryData(block));
            }

            var bytes8 = ImmutableArray.Create(new byte[]
            {
                0x4D, 0x50, 0x44, 0x43, // signature
                0x08, 0x00, 0x00,
            });

            using (var block = new ByteArrayMemoryProvider(bytes8).GetMemoryBlock(0, bytes8.Length))
            {
                Assert.Throws<BadImageFormatException>(() => PEReader.DecodeEmbeddedPortablePdbDebugDirectoryData(block));
            }
        }

        [Fact]
        public void ValidateEmbeddedPortablePdbVersion()
        {
            // major version (Portable PDB format):
            PEReader.ValidateEmbeddedPortablePdbVersion(new DebugDirectoryEntry(0, 0x0100, 0x0100, DebugDirectoryEntryType.EmbeddedPortablePdb, 0, 0, 0));
            PEReader.ValidateEmbeddedPortablePdbVersion(new DebugDirectoryEntry(0, 0x0101, 0x0100, DebugDirectoryEntryType.EmbeddedPortablePdb, 0, 0, 0));
            PEReader.ValidateEmbeddedPortablePdbVersion(new DebugDirectoryEntry(0, 0xffff, 0x0100, DebugDirectoryEntryType.EmbeddedPortablePdb, 0, 0, 0));

            Assert.Throws<BadImageFormatException>(() => PEReader.ValidateEmbeddedPortablePdbVersion(new DebugDirectoryEntry(0, 0x0000, 0x0100, DebugDirectoryEntryType.EmbeddedPortablePdb, 0, 0, 0)));
            Assert.Throws<BadImageFormatException>(() => PEReader.ValidateEmbeddedPortablePdbVersion(new DebugDirectoryEntry(0, 0x00ff, 0x0100, DebugDirectoryEntryType.EmbeddedPortablePdb, 0, 0, 0)));

            // minor version (Embedded blob format):
            Assert.Throws<BadImageFormatException>(() => PEReader.ValidateEmbeddedPortablePdbVersion(new DebugDirectoryEntry(0, 0x0100, 0x0101, DebugDirectoryEntryType.EmbeddedPortablePdb, 0, 0, 0)));
            Assert.Throws<BadImageFormatException>(() => PEReader.ValidateEmbeddedPortablePdbVersion(new DebugDirectoryEntry(0, 0x0100, 0x0000, DebugDirectoryEntryType.EmbeddedPortablePdb, 0, 0, 0)));
            Assert.Throws<BadImageFormatException>(() => PEReader.ValidateEmbeddedPortablePdbVersion(new DebugDirectoryEntry(0, 0x0100, 0x00ff, DebugDirectoryEntryType.EmbeddedPortablePdb, 0, 0, 0)));
            Assert.Throws<BadImageFormatException>(() => PEReader.ValidateEmbeddedPortablePdbVersion(new DebugDirectoryEntry(0, 0x0100, 0x0200, DebugDirectoryEntryType.EmbeddedPortablePdb, 0, 0, 0)));
        }
    }
}
