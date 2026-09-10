// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace System.Formats.Tar.Tests
{
    // Tests specific to Ustar format.
    public class TarWriter_WriteEntry_Ustar_Tests : TarWriter_WriteEntry_Base
    {
        protected override TarEntryFormat TestFormat => TarEntryFormat.Ustar;

        [Theory]
        [MemberData(nameof(GetBooleanData))]
        public async Task WriteRegularFile(bool async)
        {
            using MemoryStream archiveStream = new MemoryStream();
            {
                await using TarWriterHolder writerHolder = CreateTarWriter(archiveStream, async, TarEntryFormat.Ustar, leaveOpen: true);
                TarWriter writer = writerHolder;

                UstarTarEntry regularFileToWrite = new UstarTarEntry(TarEntryType.RegularFile, InitialEntryName);
                SetRegularFile(regularFileToWrite);
                VerifyRegularFile(regularFileToWrite, isWritable: true);
                await WriteEntry(writer, regularFileToWrite, async);
            }

            archiveStream.Position = 0;
            await using TarReaderHolder readerHolder = CreateTarReader(archiveStream, async, leaveOpen: false);
            TarReader reader = readerHolder;

            UstarTarEntry regularFile = await GetNextEntry(reader, async: async) as UstarTarEntry;
            VerifyRegularFile(regularFile, isWritable: false);
        }

        [Theory]
        [MemberData(nameof(GetBooleanData))]
        public async Task WriteHardLink(bool async)
        {
            using MemoryStream archiveStream = new MemoryStream();
            {
                await using TarWriterHolder writerHolder = CreateTarWriter(archiveStream, async, TarEntryFormat.Ustar, leaveOpen: true);
                TarWriter writer = writerHolder;

                UstarTarEntry hardLinkToWrite = new UstarTarEntry(TarEntryType.HardLink, InitialEntryName);
                SetHardLink(hardLinkToWrite);
                VerifyHardLink(hardLinkToWrite);
                await WriteEntry(writer, hardLinkToWrite, async);
            }

            archiveStream.Position = 0;
            await using TarReaderHolder readerHolder = CreateTarReader(archiveStream, async, leaveOpen: false);
            TarReader reader = readerHolder;

            UstarTarEntry hardLink = await GetNextEntry(reader, async: async) as UstarTarEntry;
            VerifyHardLink(hardLink);
        }

        [Theory]
        [MemberData(nameof(GetBooleanData))]
        public async Task WriteSymbolicLink(bool async)
        {
            using MemoryStream archiveStream = new MemoryStream();
            {
                await using TarWriterHolder writerHolder = CreateTarWriter(archiveStream, async, TarEntryFormat.Ustar, leaveOpen: true);
                TarWriter writer = writerHolder;

                UstarTarEntry symbolicLinkToWrite = new UstarTarEntry(TarEntryType.SymbolicLink, InitialEntryName);
                SetSymbolicLink(symbolicLinkToWrite);
                VerifySymbolicLink(symbolicLinkToWrite);
                await WriteEntry(writer, symbolicLinkToWrite, async);
            }

            archiveStream.Position = 0;
            await using TarReaderHolder readerHolder = CreateTarReader(archiveStream, async, leaveOpen: false);
            TarReader reader = readerHolder;

            UstarTarEntry symbolicLink = await GetNextEntry(reader, async: async) as UstarTarEntry;
            VerifySymbolicLink(symbolicLink);
        }

        [Theory]
        [MemberData(nameof(GetBooleanData))]
        public async Task WriteDirectory(bool async)
        {
            using MemoryStream archiveStream = new MemoryStream();
            {
                await using TarWriterHolder writerHolder = CreateTarWriter(archiveStream, async, TarEntryFormat.Ustar, leaveOpen: true);
                TarWriter writer = writerHolder;

                UstarTarEntry directoryToWrite = new UstarTarEntry(TarEntryType.Directory, InitialEntryName);
                SetDirectory(directoryToWrite);
                VerifyDirectory(directoryToWrite);
                await WriteEntry(writer, directoryToWrite, async);
            }

            archiveStream.Position = 0;
            await using TarReaderHolder readerHolder = CreateTarReader(archiveStream, async, leaveOpen: false);
            TarReader reader = readerHolder;

            UstarTarEntry directory = await GetNextEntry(reader, async: async) as UstarTarEntry;
            VerifyDirectory(directory);
        }

        [Theory]
        [MemberData(nameof(GetBooleanData))]
        public async Task WriteCharacterDevice(bool async)
        {
            using MemoryStream archiveStream = new MemoryStream();
            {
                await using TarWriterHolder writerHolder = CreateTarWriter(archiveStream, async, TarEntryFormat.Ustar, leaveOpen: true);
                TarWriter writer = writerHolder;

                UstarTarEntry charDeviceToWrite = new UstarTarEntry(TarEntryType.CharacterDevice, InitialEntryName);
                SetCharacterDevice(charDeviceToWrite);
                VerifyCharacterDevice(charDeviceToWrite);
                await WriteEntry(writer, charDeviceToWrite, async);
            }

            archiveStream.Position = 0;
            await using TarReaderHolder readerHolder = CreateTarReader(archiveStream, async, leaveOpen: false);
            TarReader reader = readerHolder;

            UstarTarEntry charDevice = await GetNextEntry(reader, async: async) as UstarTarEntry;
            VerifyCharacterDevice(charDevice);
        }

        [Theory]
        [MemberData(nameof(GetBooleanData))]
        public async Task WriteBlockDevice(bool async)
        {
            using MemoryStream archiveStream = new MemoryStream();
            {
                await using TarWriterHolder writerHolder = CreateTarWriter(archiveStream, async, TarEntryFormat.Ustar, leaveOpen: true);
                TarWriter writer = writerHolder;

                UstarTarEntry blockDeviceToWrite = new UstarTarEntry(TarEntryType.BlockDevice, InitialEntryName);
                SetBlockDevice(blockDeviceToWrite);
                VerifyBlockDevice(blockDeviceToWrite);
                await WriteEntry(writer, blockDeviceToWrite, async);
            }

            archiveStream.Position = 0;
            await using TarReaderHolder readerHolder = CreateTarReader(archiveStream, async, leaveOpen: false);
            TarReader reader = readerHolder;

            UstarTarEntry blockDevice = await GetNextEntry(reader, async: async) as UstarTarEntry;
            VerifyBlockDevice(blockDevice);
        }

        [Theory]
        [MemberData(nameof(GetBooleanData))]
        public async Task WriteFifo(bool async)
        {
            using MemoryStream archiveStream = new MemoryStream();
            {
                await using TarWriterHolder writerHolder = CreateTarWriter(archiveStream, async, TarEntryFormat.Ustar, leaveOpen: true);
                TarWriter writer = writerHolder;

                UstarTarEntry fifoToWrite = new UstarTarEntry(TarEntryType.Fifo, InitialEntryName);
                SetFifo(fifoToWrite);
                VerifyFifo(fifoToWrite);
                await WriteEntry(writer, fifoToWrite, async);
            }

            archiveStream.Position = 0;
            await using TarReaderHolder readerHolder = CreateTarReader(archiveStream, async, leaveOpen: false);
            TarReader reader = readerHolder;

            UstarTarEntry fifo = await GetNextEntry(reader, async: async) as UstarTarEntry;
            VerifyFifo(fifo);
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

            await Assert.ThrowsAsync<ArgumentException>("entry", () => WriteEntry(writer, new UstarTarEntry(entryType, "link"), async));
        }
    }
}