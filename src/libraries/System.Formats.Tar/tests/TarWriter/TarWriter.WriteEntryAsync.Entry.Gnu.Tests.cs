// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace System.Formats.Tar.Tests
{
    // Tests specific to Gnu format.
    public class TarWriter_WriteEntryAsync_Gnu_Tests : TarWriter_WriteEntry_Base
    {
        [Fact]
        public Task WriteEntry_Null_Throws_Async() =>
            WriteEntry_Null_Throws_Async_Internal(TarEntryFormat.Gnu);

        [Fact]
        public async Task WriteRegularFile_Async()
        {
            using MemoryStream archiveStream = new MemoryStream();
            TarWriter writer = new TarWriter(archiveStream, TarEntryFormat.Gnu, leaveOpen: true);
            await using (writer)
            {
                GnuTarEntry regularFile = new GnuTarEntry(TarEntryType.RegularFile, InitialEntryName);
                SetRegularFile(regularFile);
                VerifyRegularFile(regularFile, isWritable: true);
                await writer.WriteEntryAsync(regularFile);
            }

            archiveStream.Position = 0;
            TarReader reader = new TarReader(archiveStream);
            await using (reader)
            {
                GnuTarEntry regularFile = await reader.GetNextEntryAsync() as GnuTarEntry;
                VerifyRegularFile(regularFile, isWritable: false);
            }
        }

        [Fact]
        public async Task WriteHardLink_Async()
        {
            using MemoryStream archiveStream = new MemoryStream();
            TarWriter writer = new TarWriter(archiveStream, TarEntryFormat.Gnu, leaveOpen: true);
            await using (writer)
            {
                GnuTarEntry hardLink = new GnuTarEntry(TarEntryType.HardLink, InitialEntryName);
                SetHardLink(hardLink);
                VerifyHardLink(hardLink);
                await writer.WriteEntryAsync(hardLink);
            }

            archiveStream.Position = 0;
            TarReader reader = new TarReader(archiveStream);
            await using (reader)
            {
                GnuTarEntry hardLink = await reader.GetNextEntryAsync() as GnuTarEntry;
                VerifyHardLink(hardLink);
            }
        }

        [Fact]
        public async Task WriteSymbolicLink_Async()
        {
            using MemoryStream archiveStream = new MemoryStream();
            TarWriter writer = new TarWriter(archiveStream, TarEntryFormat.Gnu, leaveOpen: true);
            await using (writer)
            {
                GnuTarEntry symbolicLink = new GnuTarEntry(TarEntryType.SymbolicLink, InitialEntryName);
                SetSymbolicLink(symbolicLink);
                VerifySymbolicLink(symbolicLink);
                await writer.WriteEntryAsync(symbolicLink);
            }

            archiveStream.Position = 0;
            TarReader reader = new TarReader(archiveStream);
            await using (reader)
            {
                GnuTarEntry symbolicLink = await reader.GetNextEntryAsync() as GnuTarEntry;
                VerifySymbolicLink(symbolicLink);
            }
        }

        [Fact]
        public async Task WriteDirectory_Async()
        {
            using MemoryStream archiveStream = new MemoryStream();
            TarWriter writer = new TarWriter(archiveStream, TarEntryFormat.Gnu, leaveOpen: true);
            await using (writer)
            {
                GnuTarEntry directory = new GnuTarEntry(TarEntryType.Directory, InitialEntryName);
                SetDirectory(directory);
                VerifyDirectory(directory);
                await writer.WriteEntryAsync(directory);
            }

            archiveStream.Position = 0;
            TarReader reader = new TarReader(archiveStream);
            await using (reader)
            {
                GnuTarEntry directory = await reader.GetNextEntryAsync() as GnuTarEntry;
                VerifyDirectory(directory);
            }
        }

        [Fact]
        public async Task WriteCharacterDevice_Async()
        {
            using MemoryStream archiveStream = new MemoryStream();
            TarWriter writer = new TarWriter(archiveStream, TarEntryFormat.Gnu, leaveOpen: true);
            await using (writer)
            {
                GnuTarEntry charDevice = new GnuTarEntry(TarEntryType.CharacterDevice, InitialEntryName);
                SetCharacterDevice(charDevice);
                VerifyCharacterDevice(charDevice);
                await writer.WriteEntryAsync(charDevice);
            }

            archiveStream.Position = 0;
            TarReader reader = new TarReader(archiveStream);
            await using (reader)
            {
                GnuTarEntry charDevice = await reader.GetNextEntryAsync() as GnuTarEntry;
                VerifyCharacterDevice(charDevice);
            }
        }

        [Fact]
        public async Task WriteBlockDevice_Async()
        {
            using MemoryStream archiveStream = new MemoryStream();
            TarWriter writer = new TarWriter(archiveStream, TarEntryFormat.Gnu, leaveOpen: true);
            await using (writer)
            {
                GnuTarEntry blockDevice = new GnuTarEntry(TarEntryType.BlockDevice, InitialEntryName);
                SetBlockDevice(blockDevice);
                VerifyBlockDevice(blockDevice);
                await writer.WriteEntryAsync(blockDevice);
            }

            archiveStream.Position = 0;
            TarReader reader = new TarReader(archiveStream);
            await using (reader)
            {
                GnuTarEntry blockDevice = await reader.GetNextEntryAsync() as GnuTarEntry;
                VerifyBlockDevice(blockDevice);
            }
        }

        [Fact]
        public async Task WriteFifo_Async()
        {
            using MemoryStream archiveStream = new MemoryStream();
            TarWriter writer = new TarWriter(archiveStream, TarEntryFormat.Gnu, leaveOpen: true);
            await using (writer)
            {
                GnuTarEntry fifo = new GnuTarEntry(TarEntryType.Fifo, InitialEntryName);
                SetFifo(fifo);
                VerifyFifo(fifo);
                await writer.WriteEntryAsync(fifo);
            }

            archiveStream.Position = 0;
            TarReader reader = new TarReader(archiveStream);
            await using (reader)
            {
                GnuTarEntry fifo = await reader.GetNextEntryAsync() as GnuTarEntry;
                VerifyFifo(fifo);
            }
        }

        [Theory]
        [InlineData(TarEntryType.RegularFile)]
        [InlineData(TarEntryType.Directory)]
        [InlineData(TarEntryType.SymbolicLink)]
        [InlineData(TarEntryType.HardLink)]
        public async Task Write_Long_Name_Async(TarEntryType entryType)
        {
            // Name field in header only fits 100 bytes
            string longName = new string('a', 101);

            using MemoryStream archiveStream = new MemoryStream();
            TarWriter writer = new TarWriter(archiveStream, TarEntryFormat.Gnu, leaveOpen: true);
            await using (writer)
            {
                GnuTarEntry entry = new GnuTarEntry(entryType, longName);
                await writer.WriteEntryAsync(entry);
            }

            archiveStream.Position = 0;
            TarReader reader = new TarReader(archiveStream);
            await using (reader)
            {
                GnuTarEntry entry = await reader.GetNextEntryAsync() as GnuTarEntry;
                Assert.Equal(entryType, entry.EntryType);
                Assert.Equal(longName, entry.Name);
            }
        }

        [Theory]
        [InlineData(TarEntryType.SymbolicLink)]
        [InlineData(TarEntryType.HardLink)]
        public async Task Write_LongLinkName_Async(TarEntryType entryType)
        {
            // LinkName field in header only fits 100 bytes
            string longLinkName = new string('a', 101);

            using MemoryStream archiveStream = new MemoryStream();
            TarWriter writer = new TarWriter(archiveStream, TarEntryFormat.Gnu, leaveOpen: true);
            await using (writer)
            {
                GnuTarEntry entry = new GnuTarEntry(entryType, "file.txt");
                entry.LinkName = longLinkName;
                await writer.WriteEntryAsync(entry);
            }

            archiveStream.Position = 0;
            TarReader reader = new TarReader(archiveStream);
            await using (reader)
            {
                GnuTarEntry entry = await reader.GetNextEntryAsync() as GnuTarEntry;
                Assert.Equal(entryType, entry.EntryType);
                Assert.Equal("file.txt", entry.Name);
                Assert.Equal(longLinkName, entry.LinkName);
            }
        }

        [Theory]
        [InlineData(TarEntryType.SymbolicLink)]
        [InlineData(TarEntryType.HardLink)]
        public async Task Write_LongName_And_LongLinkName_Async(TarEntryType entryType)
        {
            // Both the Name and LinkName fields in header only fit 100 bytes
            string longName = new string('a', 101);
            string longLinkName = new string('a', 101);

            using MemoryStream archiveStream = new MemoryStream();
            TarWriter writer = new TarWriter(archiveStream, TarEntryFormat.Gnu, leaveOpen: true);
            await using (writer)
            {
                GnuTarEntry entry = new GnuTarEntry(entryType, longName);
                entry.LinkName = longLinkName;
                await writer.WriteEntryAsync(entry);
            }

            archiveStream.Position = 0;
            TarReader reader = new TarReader(archiveStream);
            await using (reader)
            {
                GnuTarEntry entry = await reader.GetNextEntryAsync() as GnuTarEntry;
                Assert.Equal(entryType, entry.EntryType);
                Assert.Equal(longName, entry.Name);
                Assert.Equal(longLinkName, entry.LinkName);
            }
        }
    }
}
