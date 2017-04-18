// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection.Internal;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Tests;
using Xunit;

namespace System.Reflection.PortableExecutable.Tests
{
    public class DebugDirectoryTests
    {
        [Fact]
        public void NoDebugDirectory()
        {
            var peStream = new MemoryStream(Misc.Members);
            using (var reader = new PEReader(peStream))
            {
                var entries = reader.ReadDebugDirectory();
                Assert.Empty(entries);
            }
        }

        [Fact]
        public void CodeView()
        {
            var peStream = new MemoryStream(Misc.Debug);
            using (var reader = new PEReader(peStream))
            {
                ValidateCodeView(reader);
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]  // Uses P/Invokes to get module handles
        public void CodeView_Loaded()
        {
            LoaderUtilities.LoadPEAndValidate(Misc.Debug, ValidateCodeView);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]  // Uses P/Invokes to get module handles
        public void CodeView_Loaded_FromStream()
        {
            LoaderUtilities.LoadPEAndValidate(Misc.Debug, ValidateCodeView, useStream: true);
        }

        private void ValidateCodeView(PEReader reader)
        {
            // dumpbin:
            //
            // Debug Directories
            // 
            //     Time Type        Size RVA  Pointer
            // -------------- - ------------------------
            // 5670C4E6 cv           11C 0000230C      50C Format: RSDS, { 0C426227-31E6-4EC2-BD5F-712C4D96C0AB}, 1, C:\Temp\Debug.pdb

            var cvEntry = reader.ReadDebugDirectory().Single();
            Assert.Equal(DebugDirectoryEntryType.CodeView, cvEntry.Type);
            Assert.False(cvEntry.IsPortableCodeView);
            Assert.Equal(0x050c, cvEntry.DataPointer);
            Assert.Equal(0x230c, cvEntry.DataRelativeVirtualAddress);
            Assert.Equal(0x011c, cvEntry.DataSize); // includes NUL padding
            Assert.Equal(0, cvEntry.MajorVersion);
            Assert.Equal(0, cvEntry.MinorVersion);
            Assert.Equal(0x5670c4e6u, cvEntry.Stamp);

            var cv = reader.ReadCodeViewDebugDirectoryData(cvEntry);
            Assert.Equal(1, cv.Age);
            Assert.Equal(new Guid("0C426227-31E6-4EC2-BD5F-712C4D96C0AB"), cv.Guid);
            Assert.Equal(@"C:\Temp\Debug.pdb", cv.Path);
        }

        [Fact]
        public void Deterministic()
        {
            var peStream = new MemoryStream(Misc.Deterministic);
            using (var reader = new PEReader(peStream))
            {
                ValidateDeterministic(reader);
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]  // Uses P/Invokes to get module handles
        public void Deterministic_Loaded()
        {
            LoaderUtilities.LoadPEAndValidate(Misc.Deterministic, ValidateDeterministic);
        }

        private void ValidateDeterministic(PEReader reader)
        {
            // dumpbin:
            //
            // Debug Directories
            // 
            //       Time Type        Size      RVA  Pointer
            //   -------- ------- -------- -------- --------
            //   D2FC74D3 cv            32 00002338      538    Format: RSDS, {814C578F-7676-0263-4F8A-2D3E8528EAF1}, 1, C:\Temp\Deterministic.pdb
            //   00000000 repro          0 00000000        0

            var entries = reader.ReadDebugDirectory();

            var cvEntry = entries[0];
            Assert.Equal(DebugDirectoryEntryType.CodeView, cvEntry.Type);
            Assert.False(cvEntry.IsPortableCodeView);
            Assert.Equal(0x0538, cvEntry.DataPointer);
            Assert.Equal(0x2338, cvEntry.DataRelativeVirtualAddress);
            Assert.Equal(0x0032, cvEntry.DataSize); // no NUL padding
            Assert.Equal(0, cvEntry.MajorVersion);
            Assert.Equal(0, cvEntry.MinorVersion);
            Assert.Equal(0xD2FC74D3u, cvEntry.Stamp);

            var cv = reader.ReadCodeViewDebugDirectoryData(cvEntry);
            Assert.Equal(1, cv.Age);
            Assert.Equal(new Guid("814C578F-7676-0263-4F8A-2D3E8528EAF1"), cv.Guid);
            Assert.Equal(@"C:\Temp\Deterministic.pdb", cv.Path);

            var detEntry = entries[1];
            Assert.Equal(DebugDirectoryEntryType.Reproducible, detEntry.Type);
            Assert.False(detEntry.IsPortableCodeView);
            Assert.Equal(0, detEntry.DataPointer);
            Assert.Equal(0, detEntry.DataRelativeVirtualAddress);
            Assert.Equal(0, detEntry.DataSize);
            Assert.Equal(0, detEntry.MajorVersion);
            Assert.Equal(0, detEntry.MinorVersion);
            Assert.Equal(0u, detEntry.Stamp);

            Assert.Equal(2, entries.Length);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]  // Uses P/Invokes to get module handles
        public void EmbeddedPortablePdb_Loaded()
        {
            LoaderUtilities.LoadPEAndValidate(PortablePdbs.DocumentsEmbeddedDll, ValidateEmbeddedPortablePdb);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]  // Uses P/Invokes to get module handles
        public void EmbeddedPortablePdb_Loaded_FromStream()
        {
            LoaderUtilities.LoadPEAndValidate(PortablePdbs.DocumentsEmbeddedDll, ValidateEmbeddedPortablePdb, useStream: true);
        }

        private void ValidateEmbeddedPortablePdb(PEReader reader)
        {
            var entries = reader.ReadDebugDirectory();
            Assert.Equal(DebugDirectoryEntryType.CodeView, entries[0].Type);
            Assert.Equal(DebugDirectoryEntryType.Reproducible, entries[1].Type);
            Assert.Equal(DebugDirectoryEntryType.EmbeddedPortablePdb, entries[2].Type);

            using (MetadataReaderProvider provider = reader.ReadEmbeddedPortablePdbDebugDirectoryData(entries[2]))
            {
                var pdbReader = provider.GetMetadataReader();
                var document = pdbReader.GetDocument(pdbReader.Documents.First());
                Assert.Equal(@"C:\Documents.cs", pdbReader.GetString(document.Name));
            }
        }

        [Fact]
        public void DebugDirectoryData_Errors()
        {
            var reader = new PEReader(new MemoryStream(Misc.Members));

            AssertExtensions.Throws<ArgumentException>("entry", () => reader.ReadCodeViewDebugDirectoryData(new DebugDirectoryEntry(0, 0, 0, DebugDirectoryEntryType.Coff, 0, 0, 0)));
            Assert.Throws<BadImageFormatException>(() => reader.ReadCodeViewDebugDirectoryData(new DebugDirectoryEntry(0, 0, 0, DebugDirectoryEntryType.CodeView, 0, 0, 0)));

            AssertExtensions.Throws<ArgumentException>("entry", () => reader.ReadEmbeddedPortablePdbDebugDirectoryData(new DebugDirectoryEntry(0, 0, 0, DebugDirectoryEntryType.Coff, 0, 0, 0)));
            Assert.Throws<BadImageFormatException>(() => reader.ReadEmbeddedPortablePdbDebugDirectoryData(new DebugDirectoryEntry(0, 0, 0, DebugDirectoryEntryType.EmbeddedPortablePdb, 0, 0, 0)));
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

        [Fact]
        public void CodeView_PathPadding()
        {
            var bytes = ImmutableArray.Create(new byte[]
            {
                (byte)'R', (byte)'S', (byte)'D', (byte)'S', // signature
                0x6E, 0xE6, 0x88, 0x3C, 0xB9, 0xE0, 0x08, 0x45, 0x92, 0x90, 0x11, 0xE0, 0xDB, 0x51, 0xA1, 0xC5, // GUID
                0x01, 0x00, 0x00, 0x00, // age
                (byte)'x', 0x00, 0x20, 0xff, // path
            });

            using (var block = new ByteArrayMemoryProvider(bytes).GetMemoryBlock(0, bytes.Length))
            {
                Assert.Equal("x", PEReader.DecodeCodeViewDebugDirectoryData(block).Path);
            }

            using (var block = new ByteArrayMemoryProvider(bytes).GetMemoryBlock(0, bytes.Length - 1))
            {
                Assert.Equal("x", PEReader.DecodeCodeViewDebugDirectoryData(block).Path);
            }

            using (var block = new ByteArrayMemoryProvider(bytes).GetMemoryBlock(0, bytes.Length - 2))
            {
                Assert.Equal("x", PEReader.DecodeCodeViewDebugDirectoryData(block).Path);
            }

            using (var block = new ByteArrayMemoryProvider(bytes).GetMemoryBlock(0, bytes.Length - 3))
            {
                Assert.Equal("x", PEReader.DecodeCodeViewDebugDirectoryData(block).Path);
            }

            using (var block = new ByteArrayMemoryProvider(bytes).GetMemoryBlock(0, bytes.Length - 4))
            {
                Assert.Equal("", PEReader.DecodeCodeViewDebugDirectoryData(block).Path);
            }
        }

        [Fact]
        public void CodeView_Errors()
        {
            var bytes = ImmutableArray.Create(new byte[]
            {
                (byte)'R', (byte)'S', (byte)'D', (byte)'S', // signature
                0x6E, 0xE6, 0x88, 0x3C, 0xB9, 0xE0, 0x08, 0x45, 0x92, 0x90, 0x11, 0xE0, 0xDB, 0x51, 0xA1, 0xC5, // GUID
                0x01, 0x00, 0x00, 0x00, // age
                (byte)'x', 0x00, // path
            });

            using (var block = new ByteArrayMemoryProvider(bytes).GetMemoryBlock(0, 1))
            {
                Assert.Throws<BadImageFormatException>(() => PEReader.DecodeCodeViewDebugDirectoryData(block));
            }

            using (var block = new ByteArrayMemoryProvider(bytes).GetMemoryBlock(0, 4))
            {
                Assert.Throws<BadImageFormatException>(() => PEReader.DecodeCodeViewDebugDirectoryData(block));
            }

            using (var block = new ByteArrayMemoryProvider(bytes).GetMemoryBlock(0, bytes.Length - 3))
            {
                Assert.Throws<BadImageFormatException>(() => PEReader.DecodeCodeViewDebugDirectoryData(block));
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

            var bytes9 = ImmutableArray.Create(new byte[]
            {
                0x4D, 0x50, 0x44, 0x43, // signature
                0x08, 0x00, 0x00, 0x00
            });

            using (var block = new ByteArrayMemoryProvider(bytes9).GetMemoryBlock(0, 1))
            {
                Assert.Throws<BadImageFormatException>(() => PEReader.DecodeEmbeddedPortablePdbDebugDirectoryData(block));
            }
        }
    }
}
