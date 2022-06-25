// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace System.Formats.Tar.Tests
{
    // Tests specific to Ustar format.
    public class TarWriter_WriteEntryAsync_Ustar_Tests : TarTestsBase
    {
        [Fact]
        public async Task WriteRegularFile_Async()
        {
            using MemoryStream archiveStream = new MemoryStream();
            TarWriter writer = new TarWriter(archiveStream, TarEntryFormat.Ustar, leaveOpen: true);
            await using (writer)
            {
                UstarTarEntry regularFile = new UstarTarEntry(TarEntryType.RegularFile, InitialEntryName);
                SetRegularFile(regularFile);
                VerifyRegularFile(regularFile, isWritable: true);
                await writer.WriteEntryAsync(regularFile);
            }

            archiveStream.Position = 0;
            TarReader reader = new TarReader(archiveStream);
            await using (reader)
            {
                UstarTarEntry regularFile = await reader.GetNextEntryAsync() as UstarTarEntry;
                VerifyRegularFile(regularFile, isWritable: false);
            }
        }

        [Fact]
        public async Task WriteHardLink_Async()
        {
            using MemoryStream archiveStream = new MemoryStream();
            TarWriter writer = new TarWriter(archiveStream, TarEntryFormat.Ustar, leaveOpen: true);
            await using (writer)
            {
                UstarTarEntry hardLink = new UstarTarEntry(TarEntryType.HardLink, InitialEntryName);
                SetHardLink(hardLink);
                VerifyHardLink(hardLink);
                await writer.WriteEntryAsync(hardLink);
            }

            archiveStream.Position = 0;
            TarReader reader = new TarReader(archiveStream);
            await using (reader)
            {
                UstarTarEntry hardLink = await reader.GetNextEntryAsync() as UstarTarEntry;
                VerifyHardLink(hardLink);
            }
        }

        [Fact]
        public async Task WriteSymbolicLink_Async()
        {
            using MemoryStream archiveStream = new MemoryStream();
            TarWriter writer = new TarWriter(archiveStream, TarEntryFormat.Ustar, leaveOpen: true);
            await using (writer)
            {
                UstarTarEntry symbolicLink = new UstarTarEntry(TarEntryType.SymbolicLink, InitialEntryName);
                SetSymbolicLink(symbolicLink);
                VerifySymbolicLink(symbolicLink);
                await writer.WriteEntryAsync(symbolicLink);
            }

            archiveStream.Position = 0;
            TarReader reader = new TarReader(archiveStream);
            await using (reader)
            {
                UstarTarEntry symbolicLink = await reader.GetNextEntryAsync() as UstarTarEntry;
                VerifySymbolicLink(symbolicLink);
            }
        }

        [Fact]
        public async Task WriteDirectory_Async()
        {
            using MemoryStream archiveStream = new MemoryStream();
            TarWriter writer = new TarWriter(archiveStream, TarEntryFormat.Ustar, leaveOpen: true);
            await using (writer)
            {
                UstarTarEntry directory = new UstarTarEntry(TarEntryType.Directory, InitialEntryName);
                SetDirectory(directory);
                VerifyDirectory(directory);
                await writer.WriteEntryAsync(directory);
            }

            archiveStream.Position = 0;
            TarReader reader = new TarReader(archiveStream);
            await using (reader)
            {
                UstarTarEntry directory = await reader.GetNextEntryAsync() as UstarTarEntry;
                VerifyDirectory(directory);
            }
        }

        [Fact]
        public async Task WriteCharacterDevice_Async()
        {
            using MemoryStream archiveStream = new MemoryStream();
            TarWriter writer = new TarWriter(archiveStream, TarEntryFormat.Ustar, leaveOpen: true);
            await using (writer)
            {
                UstarTarEntry charDevice = new UstarTarEntry(TarEntryType.CharacterDevice, InitialEntryName);
                SetCharacterDevice(charDevice);
                VerifyCharacterDevice(charDevice);
                await writer.WriteEntryAsync(charDevice);
            }

            archiveStream.Position = 0;
            TarReader reader = new TarReader(archiveStream);
            await using (reader)
            {
                UstarTarEntry charDevice = await reader.GetNextEntryAsync() as UstarTarEntry;
                VerifyCharacterDevice(charDevice);
            }
        }

        [Fact]
        public async Task WriteBlockDevice_Async()
        {
            using MemoryStream archiveStream = new MemoryStream();
            TarWriter writer = new TarWriter(archiveStream, TarEntryFormat.Ustar, leaveOpen: true);
            await using (writer)
            {
                UstarTarEntry blockDevice = new UstarTarEntry(TarEntryType.BlockDevice, InitialEntryName);
                SetBlockDevice(blockDevice);
                VerifyBlockDevice(blockDevice);
                await writer.WriteEntryAsync(blockDevice);
            }

            archiveStream.Position = 0;
            TarReader reader = new TarReader(archiveStream);
            await using (reader)
            {
                UstarTarEntry blockDevice = await reader.GetNextEntryAsync() as UstarTarEntry;
                VerifyBlockDevice(blockDevice);
            }
        }

        [Fact]
        public async Task WriteFifo_Async()
        {
            using MemoryStream archiveStream = new MemoryStream();
            TarWriter writer = new TarWriter(archiveStream, TarEntryFormat.Ustar, leaveOpen: true);
            await using (writer)
            {
                UstarTarEntry fifo = new UstarTarEntry(TarEntryType.Fifo, InitialEntryName);
                SetFifo(fifo);
                VerifyFifo(fifo);
                await writer.WriteEntryAsync(fifo);
            }

            archiveStream.Position = 0;
            TarReader reader = new TarReader(archiveStream);
            await using (reader)
            {
                UstarTarEntry fifo = await reader.GetNextEntryAsync() as UstarTarEntry;
                VerifyFifo(fifo);
            }
        }
    }
}
