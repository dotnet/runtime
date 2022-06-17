// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Xunit;

namespace System.Formats.Tar.Tests
{
    // Tests specific to V7 format.
    public class TarWriter_WriteEntry_V7_Tests : TarTestsBase
    {
        [Fact]
        public void ThrowIf_WriteEntry_UnsupportedFile()
        {
            // Verify that entry types that can be manually constructed in other types, cannot be inserted in a v7 writer
            using MemoryStream archiveStream = new MemoryStream();
            using (TarWriter writer = new TarWriter(archiveStream, archiveFormat: TarEntryFormat.V7, leaveOpen: true))
            {
                // Entry types supported in ustar but not in v7
                Assert.Throws<InvalidOperationException>(() => writer.WriteEntry(new UstarTarEntry(TarEntryType.BlockDevice, InitialEntryName)));
                Assert.Throws<InvalidOperationException>(() => writer.WriteEntry(new UstarTarEntry(TarEntryType.CharacterDevice, InitialEntryName)));
                Assert.Throws<InvalidOperationException>(() => writer.WriteEntry(new UstarTarEntry(TarEntryType.Fifo, InitialEntryName)));

                // Entry types supported in pax but not in v7
                Assert.Throws<InvalidOperationException>(() => writer.WriteEntry(new PaxTarEntry(TarEntryType.BlockDevice, InitialEntryName)));
                Assert.Throws<InvalidOperationException>(() => writer.WriteEntry(new PaxTarEntry(TarEntryType.CharacterDevice, InitialEntryName)));
                Assert.Throws<InvalidOperationException>(() => writer.WriteEntry(new PaxTarEntry(TarEntryType.Fifo, InitialEntryName)));

                // Entry types supported in gnu but not in v7
                Assert.Throws<InvalidOperationException>(() => writer.WriteEntry(new GnuTarEntry(TarEntryType.BlockDevice, InitialEntryName)));
                Assert.Throws<InvalidOperationException>(() => writer.WriteEntry(new GnuTarEntry(TarEntryType.CharacterDevice, InitialEntryName)));
                Assert.Throws<InvalidOperationException>(() => writer.WriteEntry(new GnuTarEntry(TarEntryType.Fifo, InitialEntryName)));
            }
            // Verify nothing was written, not even the empty records
            Assert.Equal(0, archiveStream.Length);
        }

        [Theory]
        [InlineData(TarEntryFormat.Ustar)]
        [InlineData(TarEntryFormat.Pax)]
        [InlineData(TarEntryFormat.Gnu)]
        public void Write_RegularFileEntry_As_V7RegularFileEntry(TarEntryFormat entryFormat)
        {
            using MemoryStream archive = new MemoryStream();
            using (TarWriter writer = new TarWriter(archive, archiveFormat: TarEntryFormat.V7, leaveOpen: true))
            {
                TarEntry entry = entryFormat switch
                {
                    TarEntryFormat.Ustar => new UstarTarEntry(TarEntryType.RegularFile, InitialEntryName),
                    TarEntryFormat.Pax => new PaxTarEntry(TarEntryType.RegularFile, InitialEntryName),
                    TarEntryFormat.Gnu => new GnuTarEntry(TarEntryType.RegularFile, InitialEntryName),
                    _ => throw new FormatException()
                };

                // Should be written as V7RegularFile
                writer.WriteEntry(entry);
            }

            archive.Seek(0, SeekOrigin.Begin);
            using (TarReader reader = new TarReader(archive))
            {
                TarEntry entry = reader.GetNextEntry();
                Assert.True(entry is V7TarEntry);
                Assert.Equal(TarEntryType.V7RegularFile, entry.EntryType);

                Assert.Null(reader.GetNextEntry());
            }
        }


        [Fact]
        public void WriteRegularFile()
        {
            using MemoryStream archiveStream = new MemoryStream();
            using (TarWriter writer = new TarWriter(archiveStream, TarEntryFormat.V7, leaveOpen: true))
            {
                V7TarEntry oldRegularFile = new V7TarEntry(TarEntryType.V7RegularFile, InitialEntryName);
                SetRegularFile(oldRegularFile);
                VerifyRegularFile(oldRegularFile, isWritable: true);
                writer.WriteEntry(oldRegularFile);
            }

            archiveStream.Position = 0;
            using (TarReader reader = new TarReader(archiveStream))
            {
                V7TarEntry oldRegularFile = reader.GetNextEntry() as V7TarEntry;
                VerifyRegularFile(oldRegularFile, isWritable: false);
            }
        }

        [Fact]
        public void WriteHardLink()
        {
            using MemoryStream archiveStream = new MemoryStream();
            using (TarWriter writer = new TarWriter(archiveStream, TarEntryFormat.V7, leaveOpen: true))
            {
                V7TarEntry hardLink = new V7TarEntry(TarEntryType.HardLink, InitialEntryName);
                SetHardLink(hardLink);
                VerifyHardLink(hardLink);
                writer.WriteEntry(hardLink);
            }

            archiveStream.Position = 0;
            using (TarReader reader = new TarReader(archiveStream))
            {
                V7TarEntry hardLink = reader.GetNextEntry() as V7TarEntry;
                VerifyHardLink(hardLink);
            }
        }

        [Fact]
        public void WriteSymbolicLink()
        {
            using MemoryStream archiveStream = new MemoryStream();
            using (TarWriter writer = new TarWriter(archiveStream, TarEntryFormat.V7, leaveOpen: true))
            {
                V7TarEntry symbolicLink = new V7TarEntry(TarEntryType.SymbolicLink, InitialEntryName);
                SetSymbolicLink(symbolicLink);
                VerifySymbolicLink(symbolicLink);
                writer.WriteEntry(symbolicLink);
            }

            archiveStream.Position = 0;
            using (TarReader reader = new TarReader(archiveStream))
            {
                V7TarEntry symbolicLink = reader.GetNextEntry() as V7TarEntry;
                VerifySymbolicLink(symbolicLink);
            }
        }

        [Fact]
        public void WriteDirectory()
        {
            using MemoryStream archiveStream = new MemoryStream();
            using (TarWriter writer = new TarWriter(archiveStream, TarEntryFormat.V7, leaveOpen: true))
            {
                V7TarEntry directory = new V7TarEntry(TarEntryType.Directory, InitialEntryName);
                SetDirectory(directory);
                VerifyDirectory(directory);
                writer.WriteEntry(directory);
            }

            archiveStream.Position = 0;
            using (TarReader reader = new TarReader(archiveStream))
            {
                V7TarEntry directory = reader.GetNextEntry() as V7TarEntry;
                VerifyDirectory(directory);
            }
        }
    }
}
