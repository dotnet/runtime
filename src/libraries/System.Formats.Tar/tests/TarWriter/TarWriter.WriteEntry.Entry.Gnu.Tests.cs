// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace System.Formats.Tar.Tests
{
    // Tests specific to Gnu format.
    public class TarWriter_WriteEntry_Gnu_Tests : TarWriter_WriteEntry_Base
    {
        protected override TarEntryFormat TestFormat => TarEntryFormat.Gnu;

        [Theory]
        [MemberData(nameof(GetBooleanData))]
        public async Task WriteRegularFile(bool async)
        {
            using MemoryStream archiveStream = new MemoryStream();
            {
                await using TarWriterHolder writerHolder = CreateTarWriter(archiveStream, async, TarEntryFormat.Gnu, leaveOpen: true);
                TarWriter writer = writerHolder;

                GnuTarEntry regularFileToWrite = new GnuTarEntry(TarEntryType.RegularFile, InitialEntryName);
                SetRegularFile(regularFileToWrite);
                VerifyRegularFile(regularFileToWrite, isWritable: true);
                await WriteEntry(writer, regularFileToWrite, async);
            }

            archiveStream.Position = 0;
            await using TarReaderHolder readerHolder = CreateTarReader(archiveStream, async, leaveOpen: false);
            TarReader reader = readerHolder;

            GnuTarEntry regularFile = await GetNextEntry(reader, async: async) as GnuTarEntry;
            VerifyRegularFile(regularFile, isWritable: false);
        }

        [Theory]
        [MemberData(nameof(GetBooleanData))]
        public async Task WriteHardLink(bool async)
        {
            using MemoryStream archiveStream = new MemoryStream();
            {
                await using TarWriterHolder writerHolder = CreateTarWriter(archiveStream, async, TarEntryFormat.Gnu, leaveOpen: true);
                TarWriter writer = writerHolder;

                GnuTarEntry hardLinkToWrite = new GnuTarEntry(TarEntryType.HardLink, InitialEntryName);
                SetHardLink(hardLinkToWrite);
                VerifyHardLink(hardLinkToWrite);
                await WriteEntry(writer, hardLinkToWrite, async);
            }

            archiveStream.Position = 0;
            await using TarReaderHolder readerHolder = CreateTarReader(archiveStream, async, leaveOpen: false);
            TarReader reader = readerHolder;

            GnuTarEntry hardLink = await GetNextEntry(reader, async: async) as GnuTarEntry;
            VerifyHardLink(hardLink);
        }

        [Theory]
        [MemberData(nameof(GetBooleanData))]
        public async Task WriteSymbolicLink(bool async)
        {
            using MemoryStream archiveStream = new MemoryStream();
            {
                await using TarWriterHolder writerHolder = CreateTarWriter(archiveStream, async, TarEntryFormat.Gnu, leaveOpen: true);
                TarWriter writer = writerHolder;

                GnuTarEntry symbolicLinkToWrite = new GnuTarEntry(TarEntryType.SymbolicLink, InitialEntryName);
                SetSymbolicLink(symbolicLinkToWrite);
                VerifySymbolicLink(symbolicLinkToWrite);
                await WriteEntry(writer, symbolicLinkToWrite, async);
            }

            archiveStream.Position = 0;
            await using TarReaderHolder readerHolder = CreateTarReader(archiveStream, async, leaveOpen: false);
            TarReader reader = readerHolder;

            GnuTarEntry symbolicLink = await GetNextEntry(reader, async: async) as GnuTarEntry;
            VerifySymbolicLink(symbolicLink);
        }

        [Theory]
        [MemberData(nameof(GetBooleanData))]
        public async Task WriteDirectory(bool async)
        {
            using MemoryStream archiveStream = new MemoryStream();
            {
                await using TarWriterHolder writerHolder = CreateTarWriter(archiveStream, async, TarEntryFormat.Gnu, leaveOpen: true);
                TarWriter writer = writerHolder;

                GnuTarEntry directoryToWrite = new GnuTarEntry(TarEntryType.Directory, InitialEntryName);
                SetDirectory(directoryToWrite);
                VerifyDirectory(directoryToWrite);
                await WriteEntry(writer, directoryToWrite, async);
            }

            archiveStream.Position = 0;
            await using TarReaderHolder readerHolder = CreateTarReader(archiveStream, async, leaveOpen: false);
            TarReader reader = readerHolder;

            GnuTarEntry directory = await GetNextEntry(reader, async: async) as GnuTarEntry;
            VerifyDirectory(directory);
        }

        [Theory]
        [MemberData(nameof(GetBooleanData))]
        public async Task WriteCharacterDevice(bool async)
        {
            using MemoryStream archiveStream = new MemoryStream();
            {
                await using TarWriterHolder writerHolder = CreateTarWriter(archiveStream, async, TarEntryFormat.Gnu, leaveOpen: true);
                TarWriter writer = writerHolder;

                GnuTarEntry charDeviceToWrite = new GnuTarEntry(TarEntryType.CharacterDevice, InitialEntryName);
                SetCharacterDevice(charDeviceToWrite);
                VerifyCharacterDevice(charDeviceToWrite);
                await WriteEntry(writer, charDeviceToWrite, async);
            }

            archiveStream.Position = 0;
            await using TarReaderHolder readerHolder = CreateTarReader(archiveStream, async, leaveOpen: false);
            TarReader reader = readerHolder;

            GnuTarEntry charDevice = await GetNextEntry(reader, async: async) as GnuTarEntry;
            VerifyCharacterDevice(charDevice);
        }

        [Theory]
        [MemberData(nameof(GetBooleanData))]
        public async Task WriteBlockDevice(bool async)
        {
            using MemoryStream archiveStream = new MemoryStream();
            {
                await using TarWriterHolder writerHolder = CreateTarWriter(archiveStream, async, TarEntryFormat.Gnu, leaveOpen: true);
                TarWriter writer = writerHolder;

                GnuTarEntry blockDeviceToWrite = new GnuTarEntry(TarEntryType.BlockDevice, InitialEntryName);
                SetBlockDevice(blockDeviceToWrite);
                VerifyBlockDevice(blockDeviceToWrite);
                await WriteEntry(writer, blockDeviceToWrite, async);
            }

            archiveStream.Position = 0;
            await using TarReaderHolder readerHolder = CreateTarReader(archiveStream, async, leaveOpen: false);
            TarReader reader = readerHolder;

            GnuTarEntry blockDevice = await GetNextEntry(reader, async: async) as GnuTarEntry;
            VerifyBlockDevice(blockDevice);
        }

        [Theory]
        [MemberData(nameof(GetBooleanData))]
        public async Task WriteFifo(bool async)
        {
            using MemoryStream archiveStream = new MemoryStream();
            {
                await using TarWriterHolder writerHolder = CreateTarWriter(archiveStream, async, TarEntryFormat.Gnu, leaveOpen: true);
                TarWriter writer = writerHolder;

                GnuTarEntry fifoToWrite = new GnuTarEntry(TarEntryType.Fifo, InitialEntryName);
                SetFifo(fifoToWrite);
                VerifyFifo(fifoToWrite);
                await WriteEntry(writer, fifoToWrite, async);
            }

            archiveStream.Position = 0;
            await using TarReaderHolder readerHolder = CreateTarReader(archiveStream, async, leaveOpen: false);
            TarReader reader = readerHolder;

            GnuTarEntry fifo = await GetNextEntry(reader, async: async) as GnuTarEntry;
            VerifyFifo(fifo);
        }

        [Theory]
        [InlineData(TarEntryType.RegularFile, false)]
        [InlineData(TarEntryType.RegularFile, true)]
        [InlineData(TarEntryType.Directory, false)]
        [InlineData(TarEntryType.Directory, true)]
        [InlineData(TarEntryType.SymbolicLink, false)]
        [InlineData(TarEntryType.SymbolicLink, true)]
        [InlineData(TarEntryType.HardLink, false)]
        [InlineData(TarEntryType.HardLink, true)]
        public async Task Write_Long_Name(TarEntryType entryType, bool async)
        {
            // Name field in header only fits 100 bytes
            string longName = new string('a', 101);

            using MemoryStream archiveStream = new MemoryStream();
            {
                await using TarWriterHolder writerHolder = CreateTarWriter(archiveStream, async, TarEntryFormat.Gnu, leaveOpen: true);
                TarWriter writer = writerHolder;

                GnuTarEntry entryToWrite = new GnuTarEntry(entryType, longName);
                if (entryType is TarEntryType.HardLink or TarEntryType.SymbolicLink)
                {
                    entryToWrite.LinkName = "linktarget";
                }
                await WriteEntry(writer, entryToWrite, async);
            }

            archiveStream.Position = 0;
            await using TarReaderHolder readerHolder = CreateTarReader(archiveStream, async, leaveOpen: false);
            TarReader reader = readerHolder;

            GnuTarEntry entry = await GetNextEntry(reader, async: async) as GnuTarEntry;
            Assert.Equal(entryType, entry.EntryType);
            Assert.Equal(longName, entry.Name);
        }

        [Theory]
        [InlineData(TarEntryType.SymbolicLink, false)]
        [InlineData(TarEntryType.SymbolicLink, true)]
        [InlineData(TarEntryType.HardLink, false)]
        [InlineData(TarEntryType.HardLink, true)]
        public async Task Write_LongLinkName(TarEntryType entryType, bool async)
        {
            // LinkName field in header only fits 100 bytes
            string longLinkName = new string('a', 101);

            using MemoryStream archiveStream = new MemoryStream();
            {
                await using TarWriterHolder writerHolder = CreateTarWriter(archiveStream, async, TarEntryFormat.Gnu, leaveOpen: true);
                TarWriter writer = writerHolder;

                GnuTarEntry entryToWrite = new GnuTarEntry(entryType, "file.txt");
                entryToWrite.LinkName = longLinkName;
                await WriteEntry(writer, entryToWrite, async);
            }

            archiveStream.Position = 0;
            await using TarReaderHolder readerHolder = CreateTarReader(archiveStream, async, leaveOpen: false);
            TarReader reader = readerHolder;

            GnuTarEntry entry = await GetNextEntry(reader, async: async) as GnuTarEntry;
            Assert.Equal(entryType, entry.EntryType);
            Assert.Equal("file.txt", entry.Name);
            Assert.Equal(longLinkName, entry.LinkName);
        }

        [Theory]
        [InlineData(TarEntryType.SymbolicLink, false)]
        [InlineData(TarEntryType.SymbolicLink, true)]
        [InlineData(TarEntryType.HardLink, false)]
        [InlineData(TarEntryType.HardLink, true)]
        public async Task Write_LongName_And_LongLinkName(TarEntryType entryType, bool async)
        {
            // Both the Name and LinkName fields in header only fit 100 bytes
            string longName = new string('a', 101);
            string longLinkName = new string('a', 101);

            using MemoryStream archiveStream = new MemoryStream();
            {
                await using TarWriterHolder writerHolder = CreateTarWriter(archiveStream, async, TarEntryFormat.Gnu, leaveOpen: true);
                TarWriter writer = writerHolder;

                GnuTarEntry entryToWrite = new GnuTarEntry(entryType, longName);
                entryToWrite.LinkName = longLinkName;
                await WriteEntry(writer, entryToWrite, async);
            }

            archiveStream.Position = 0;
            await using TarReaderHolder readerHolder = CreateTarReader(archiveStream, async, leaveOpen: false);
            TarReader reader = readerHolder;

            GnuTarEntry entry = await GetNextEntry(reader, async: async) as GnuTarEntry;
            Assert.Equal(entryType, entry.EntryType);
            Assert.Equal(longName, entry.Name);
            Assert.Equal(longLinkName, entry.LinkName);
        }

        [Theory]
        [InlineData(TarEntryType.HardLink, false)]
        [InlineData(TarEntryType.HardLink, true)]
        [InlineData(TarEntryType.SymbolicLink, false)]
        [InlineData(TarEntryType.SymbolicLink, true)]
        public async Task Write_LinkEntry_EmptyLinkName_Throws(TarEntryType entryType, bool async)
        {
            using MemoryStream archiveStream = new MemoryStream();
            await using TarWriterHolder writerHolder = CreateTarWriter(archiveStream, async, leaveOpen: false);
            TarWriter writer = writerHolder;

            await Assert.ThrowsAsync<ArgumentException>("entry", () => WriteEntry(writer, new GnuTarEntry(entryType, "link"), async));
        }
    }
}