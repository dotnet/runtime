// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace System.Formats.Tar.Tests
{
    // Tests specific to Ustar format.
    public class TarWriter_WriteEntryAsync_Ustar_Tests : TarWriter_WriteEntry_Base
    {
        [Fact]
        public Task WriteEntry_Null_Throws_Async() =>
            WriteEntry_Null_Throws_Async_Internal(TarEntryFormat.Ustar);

        [Fact]
        public async Task WriteRegularFile_Async()
        {
            using MemoryStream archiveStream = new MemoryStream();
            await using (TarWriter writer = new TarWriter(archiveStream, TarEntryFormat.Ustar, leaveOpen: true))
            {
                UstarTarEntry regularFile = new UstarTarEntry(TarEntryType.RegularFile, InitialEntryName);
                SetRegularFile(regularFile);
                VerifyRegularFile(regularFile, isWritable: true);
                await writer.WriteEntryAsync(regularFile);
            }

            archiveStream.Position = 0;
            await using (TarReader reader = new TarReader(archiveStream))
            {
                UstarTarEntry regularFile = await reader.GetNextEntryAsync() as UstarTarEntry;
                VerifyRegularFile(regularFile, isWritable: false);
            }
        }

        [Fact]
        public async Task WriteHardLink_Async()
        {
            using MemoryStream archiveStream = new MemoryStream();
            await using (TarWriter writer = new TarWriter(archiveStream, TarEntryFormat.Ustar, leaveOpen: true))
            {
                UstarTarEntry hardLink = new UstarTarEntry(TarEntryType.HardLink, InitialEntryName);
                SetHardLink(hardLink);
                VerifyHardLink(hardLink);
                await writer.WriteEntryAsync(hardLink);
            }

            archiveStream.Position = 0;
            await using (TarReader reader = new TarReader(archiveStream))
            {
                UstarTarEntry hardLink = await reader.GetNextEntryAsync() as UstarTarEntry;
                VerifyHardLink(hardLink);
            }
        }

        [Fact]
        public async Task WriteSymbolicLink_Async()
        {
            using MemoryStream archiveStream = new MemoryStream();
            await using (TarWriter writer = new TarWriter(archiveStream, TarEntryFormat.Ustar, leaveOpen: true))
            {
                UstarTarEntry symbolicLink = new UstarTarEntry(TarEntryType.SymbolicLink, InitialEntryName);
                SetSymbolicLink(symbolicLink);
                VerifySymbolicLink(symbolicLink);
                await writer.WriteEntryAsync(symbolicLink);
            }

            archiveStream.Position = 0;
            await using (TarReader reader = new TarReader(archiveStream))
            {
                UstarTarEntry symbolicLink = await reader.GetNextEntryAsync() as UstarTarEntry;
                VerifySymbolicLink(symbolicLink);
            }
        }

        [Fact]
        public async Task WriteDirectory_Async()
        {
            using MemoryStream archiveStream = new MemoryStream();
            await using (TarWriter writer = new TarWriter(archiveStream, TarEntryFormat.Ustar, leaveOpen: true))
            {
                UstarTarEntry directory = new UstarTarEntry(TarEntryType.Directory, InitialEntryName);
                SetDirectory(directory);
                VerifyDirectory(directory);
                await writer.WriteEntryAsync(directory);
            }

            archiveStream.Position = 0;
            await using (TarReader reader = new TarReader(archiveStream))
            {
                UstarTarEntry directory = await reader.GetNextEntryAsync() as UstarTarEntry;
                VerifyDirectory(directory);
            }
        }

        [Fact]
        public async Task WriteCharacterDevice_Async()
        {
            using MemoryStream archiveStream = new MemoryStream();
            await using (TarWriter writer = new TarWriter(archiveStream, TarEntryFormat.Ustar, leaveOpen: true))
            {
                UstarTarEntry charDevice = new UstarTarEntry(TarEntryType.CharacterDevice, InitialEntryName);
                SetCharacterDevice(charDevice);
                VerifyCharacterDevice(charDevice);
                await writer.WriteEntryAsync(charDevice);
            }

            archiveStream.Position = 0;
            await using (TarReader reader = new TarReader(archiveStream))
            {
                UstarTarEntry charDevice = await reader.GetNextEntryAsync() as UstarTarEntry;
                VerifyCharacterDevice(charDevice);
            }
        }

        [Fact]
        public async Task WriteBlockDevice_Async()
        {
            using MemoryStream archiveStream = new MemoryStream();
            await using (TarWriter writer = new TarWriter(archiveStream, TarEntryFormat.Ustar, leaveOpen: true))
            {
                UstarTarEntry blockDevice = new UstarTarEntry(TarEntryType.BlockDevice, InitialEntryName);
                SetBlockDevice(blockDevice);
                VerifyBlockDevice(blockDevice);
                await writer.WriteEntryAsync(blockDevice);
            }

            archiveStream.Position = 0;
            await using (TarReader reader = new TarReader(archiveStream))
            {
                UstarTarEntry blockDevice = await reader.GetNextEntryAsync() as UstarTarEntry;
                VerifyBlockDevice(blockDevice);
            }
        }

        [Fact]
        public async Task WriteFifo_Async()
        {
            using MemoryStream archiveStream = new MemoryStream();
            await using (TarWriter writer = new TarWriter(archiveStream, TarEntryFormat.Ustar, leaveOpen: true))
            {
                UstarTarEntry fifo = new UstarTarEntry(TarEntryType.Fifo, InitialEntryName);
                SetFifo(fifo);
                VerifyFifo(fifo);
                await writer.WriteEntryAsync(fifo);
            }

            archiveStream.Position = 0;
            await using (TarReader reader = new TarReader(archiveStream))
            {
                UstarTarEntry fifo = await reader.GetNextEntryAsync() as UstarTarEntry;
                VerifyFifo(fifo);
            }
        }
    }
}
