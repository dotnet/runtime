// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Xunit;

namespace System.Formats.Tar.Tests
{
    // Tests specific to Gnu format.
    public class TarWriter_WriteEntry_Gnu_Tests : TarTestsBase
    {
        [Fact]
        public void Write_V7RegularFileEntry_As_RegularFileEntry()
        {
            using MemoryStream archive = new MemoryStream();
            using (TarWriter writer = new TarWriter(archive, archiveFormat: TarFormat.Gnu, leaveOpen: true))
            {
                V7TarEntry entry = new V7TarEntry(TarEntryType.V7RegularFile, InitialEntryName);

                // Should be written as RegularFile
                writer.WriteEntry(entry);
            }

            archive.Seek(0, SeekOrigin.Begin);
            using (TarReader reader = new TarReader(archive))
            {
                GnuTarEntry entry = reader.GetNextEntry() as GnuTarEntry;
                Assert.NotNull(entry);
                Assert.Equal(TarEntryType.RegularFile, entry.EntryType);

                Assert.Null(reader.GetNextEntry());
            }
        }

        [Fact]
        public void WriteRegularFile()
        {
            using MemoryStream archiveStream = new MemoryStream();
            using (TarWriter writer = new TarWriter(archiveStream, TarFormat.Gnu, leaveOpen: true))
            {
                GnuTarEntry regularFile = new GnuTarEntry(TarEntryType.RegularFile, InitialEntryName);
                SetRegularFile(regularFile);
                VerifyRegularFile(regularFile, isWritable: true);
                writer.WriteEntry(regularFile);
            }

            archiveStream.Position = 0;
            using (TarReader reader = new TarReader(archiveStream))
            {
                GnuTarEntry regularFile = reader.GetNextEntry() as GnuTarEntry;
                VerifyRegularFile(regularFile, isWritable: false);
            }
        }

        [Fact]
        public void WriteHardLink()
        {
            using MemoryStream archiveStream = new MemoryStream();
            using (TarWriter writer = new TarWriter(archiveStream, TarFormat.Gnu, leaveOpen: true))
            {
                GnuTarEntry hardLink = new GnuTarEntry(TarEntryType.HardLink, InitialEntryName);
                SetHardLink(hardLink);
                VerifyHardLink(hardLink);
                writer.WriteEntry(hardLink);
            }

            archiveStream.Position = 0;
            using (TarReader reader = new TarReader(archiveStream))
            {
                GnuTarEntry hardLink = reader.GetNextEntry() as GnuTarEntry;
                VerifyHardLink(hardLink);
            }
        }

        [Fact]
        public void WriteSymbolicLink()
        {
            using MemoryStream archiveStream = new MemoryStream();
            using (TarWriter writer = new TarWriter(archiveStream, TarFormat.Gnu, leaveOpen: true))
            {
                GnuTarEntry symbolicLink = new GnuTarEntry(TarEntryType.SymbolicLink, InitialEntryName);
                SetSymbolicLink(symbolicLink);
                VerifySymbolicLink(symbolicLink);
                writer.WriteEntry(symbolicLink);
            }

            archiveStream.Position = 0;
            using (TarReader reader = new TarReader(archiveStream))
            {
                GnuTarEntry symbolicLink = reader.GetNextEntry() as GnuTarEntry;
                VerifySymbolicLink(symbolicLink);
            }
        }

        [Fact]
        public void WriteDirectory()
        {
            using MemoryStream archiveStream = new MemoryStream();
            using (TarWriter writer = new TarWriter(archiveStream, TarFormat.Gnu, leaveOpen: true))
            {
                GnuTarEntry directory = new GnuTarEntry(TarEntryType.Directory, InitialEntryName);
                SetDirectory(directory);
                VerifyDirectory(directory);
                writer.WriteEntry(directory);
            }

            archiveStream.Position = 0;
            using (TarReader reader = new TarReader(archiveStream))
            {
                GnuTarEntry directory = reader.GetNextEntry() as GnuTarEntry;
                VerifyDirectory(directory);
            }
        }

        [Fact]
        public void WriteCharacterDevice()
        {
            using MemoryStream archiveStream = new MemoryStream();
            using (TarWriter writer = new TarWriter(archiveStream, TarFormat.Gnu, leaveOpen: true))
            {
                GnuTarEntry charDevice = new GnuTarEntry(TarEntryType.CharacterDevice, InitialEntryName);
                SetCharacterDevice(charDevice);
                VerifyCharacterDevice(charDevice);
                writer.WriteEntry(charDevice);
            }

            archiveStream.Position = 0;
            using (TarReader reader = new TarReader(archiveStream))
            {
                GnuTarEntry charDevice = reader.GetNextEntry() as GnuTarEntry;
                VerifyCharacterDevice(charDevice);
            }
        }

        [Fact]
        public void WriteBlockDevice()
        {
            using MemoryStream archiveStream = new MemoryStream();
            using (TarWriter writer = new TarWriter(archiveStream, TarFormat.Gnu, leaveOpen: true))
            {
                GnuTarEntry blockDevice = new GnuTarEntry(TarEntryType.BlockDevice, InitialEntryName);
                SetBlockDevice(blockDevice);
                VerifyBlockDevice(blockDevice);
                writer.WriteEntry(blockDevice);
            }

            archiveStream.Position = 0;
            using (TarReader reader = new TarReader(archiveStream))
            {
                GnuTarEntry blockDevice = reader.GetNextEntry() as GnuTarEntry;
                VerifyBlockDevice(blockDevice);
            }
        }

        [Fact]
        public void WriteFifo()
        {
            using MemoryStream archiveStream = new MemoryStream();
            using (TarWriter writer = new TarWriter(archiveStream, TarFormat.Gnu, leaveOpen: true))
            {
                GnuTarEntry fifo = new GnuTarEntry(TarEntryType.Fifo, InitialEntryName);
                SetFifo(fifo);
                VerifyFifo(fifo);
                writer.WriteEntry(fifo);
            }

            archiveStream.Position = 0;
            using (TarReader reader = new TarReader(archiveStream))
            {
                GnuTarEntry fifo = reader.GetNextEntry() as GnuTarEntry;
                VerifyFifo(fifo);
            }
        }

        [Theory]
        [InlineData(TarEntryType.RegularFile)]
        [InlineData(TarEntryType.Directory)]
        [InlineData(TarEntryType.SymbolicLink)]
        [InlineData(TarEntryType.HardLink)]
        public void Write_Long_Name(TarEntryType entryType)
        {
            // Name field in header only fits 100 bytes
            string longName = new string('a', 101);

            using MemoryStream archiveStream = new MemoryStream();
            using (TarWriter writer = new TarWriter(archiveStream, TarFormat.Gnu, leaveOpen: true))
            {
                GnuTarEntry entry = new GnuTarEntry(entryType, longName);
                writer.WriteEntry(entry);
            }

            archiveStream.Position = 0;
            using (TarReader reader = new TarReader(archiveStream))
            {
                GnuTarEntry entry = reader.GetNextEntry() as GnuTarEntry;
                Assert.Equal(entryType, entry.EntryType);
                Assert.Equal(longName, entry.Name);
            }
        }

        [Theory]
        [InlineData(TarEntryType.SymbolicLink)]
        [InlineData(TarEntryType.HardLink)]
        public void Write_LongLinKName(TarEntryType entryType)
        {
            // LinkName field in header only fits 100 bytes
            string longLinkName = new string('a', 101);

            using MemoryStream archiveStream = new MemoryStream();
            using (TarWriter writer = new TarWriter(archiveStream, TarFormat.Gnu, leaveOpen: true))
            {
                GnuTarEntry entry = new GnuTarEntry(entryType, "file.txt");
                entry.LinkName = longLinkName;
                writer.WriteEntry(entry);
            }

            archiveStream.Position = 0;
            using (TarReader reader = new TarReader(archiveStream))
            {
                GnuTarEntry entry = reader.GetNextEntry() as GnuTarEntry;
                Assert.Equal(entryType, entry.EntryType);
                Assert.Equal("file.txt", entry.Name);
                Assert.Equal(longLinkName, entry.LinkName);
            }
        }

        [Theory]
        [InlineData(TarEntryType.SymbolicLink)]
        [InlineData(TarEntryType.HardLink)]
        public void Write_LongName_And_LongLinKName(TarEntryType entryType)
        {
            // Both the Name and LinkName fields in header only fit 100 bytes
            string longName = new string('a', 101);
            string longLinkName = new string('a', 101);

            using MemoryStream archiveStream = new MemoryStream();
            using (TarWriter writer = new TarWriter(archiveStream, TarFormat.Gnu, leaveOpen: true))
            {
                GnuTarEntry entry = new GnuTarEntry(entryType, longName);
                entry.LinkName = longLinkName;
                writer.WriteEntry(entry);
            }

            archiveStream.Position = 0;
            using (TarReader reader = new TarReader(archiveStream))
            {
                GnuTarEntry entry = reader.GetNextEntry() as GnuTarEntry;
                Assert.Equal(entryType, entry.EntryType);
                Assert.Equal(longName, entry.Name);
                Assert.Equal(longLinkName, entry.LinkName);
            }
        }
    }
}
