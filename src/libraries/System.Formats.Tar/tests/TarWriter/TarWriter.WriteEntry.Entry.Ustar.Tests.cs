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
        [Theory]
        [MemberData(nameof(GetBooleanData))]
        public async Task WriteEntry_Null_Throws(bool async)
        {
            using MemoryStream archiveStream = new MemoryStream();
            TarWriter writer = CreateTarWriter(archiveStream, TarEntryFormat.Ustar, leaveOpen: false);
            try
            {
                if (async)
                {
                    await Assert.ThrowsAsync<ArgumentNullException>(() => writer.WriteEntryAsync(null));
                }
                else
                {
                    Assert.Throws<ArgumentNullException>(() => writer.WriteEntry(null));
                }
            }
            finally
            {
                await DisposeTarWriter(writer, async);
            }
        }

        [Theory]
        [MemberData(nameof(GetBooleanData))]
        public async Task WriteRegularFile(bool async)
        {
            using MemoryStream archiveStream = new MemoryStream();
            TarWriter writer = CreateTarWriter(archiveStream, TarEntryFormat.Ustar, leaveOpen: true);
            try
            {
                UstarTarEntry regularFile = new UstarTarEntry(TarEntryType.RegularFile, InitialEntryName);
                SetRegularFile(regularFile);
                VerifyRegularFile(regularFile, isWritable: true);
                await WriteEntry(writer, regularFile, async);
            }
            finally
            {
                await DisposeTarWriter(writer, async);
            }

            archiveStream.Position = 0;
            TarReader reader = CreateTarReader(archiveStream);
            try
            {
                UstarTarEntry regularFile = await GetNextEntry(reader, async: async) as UstarTarEntry;
                VerifyRegularFile(regularFile, isWritable: false);
            }
            finally
            {
                await DisposeTarReader(reader, async);
            }
        }

        [Theory]
        [MemberData(nameof(GetBooleanData))]
        public async Task WriteHardLink(bool async)
        {
            using MemoryStream archiveStream = new MemoryStream();
            TarWriter writer = CreateTarWriter(archiveStream, TarEntryFormat.Ustar, leaveOpen: true);
            try
            {
                UstarTarEntry hardLink = new UstarTarEntry(TarEntryType.HardLink, InitialEntryName);
                SetHardLink(hardLink);
                VerifyHardLink(hardLink);
                await WriteEntry(writer, hardLink, async);
            }
            finally
            {
                await DisposeTarWriter(writer, async);
            }

            archiveStream.Position = 0;
            TarReader reader = CreateTarReader(archiveStream);
            try
            {
                UstarTarEntry hardLink = await GetNextEntry(reader, async: async) as UstarTarEntry;
                VerifyHardLink(hardLink);
            }
            finally
            {
                await DisposeTarReader(reader, async);
            }
        }

        [Theory]
        [MemberData(nameof(GetBooleanData))]
        public async Task WriteSymbolicLink(bool async)
        {
            using MemoryStream archiveStream = new MemoryStream();
            TarWriter writer = CreateTarWriter(archiveStream, TarEntryFormat.Ustar, leaveOpen: true);
            try
            {
                UstarTarEntry symbolicLink = new UstarTarEntry(TarEntryType.SymbolicLink, InitialEntryName);
                SetSymbolicLink(symbolicLink);
                VerifySymbolicLink(symbolicLink);
                await WriteEntry(writer, symbolicLink, async);
            }
            finally
            {
                await DisposeTarWriter(writer, async);
            }

            archiveStream.Position = 0;
            TarReader reader = CreateTarReader(archiveStream);
            try
            {
                UstarTarEntry symbolicLink = await GetNextEntry(reader, async: async) as UstarTarEntry;
                VerifySymbolicLink(symbolicLink);
            }
            finally
            {
                await DisposeTarReader(reader, async);
            }
        }

        [Theory]
        [MemberData(nameof(GetBooleanData))]
        public async Task WriteDirectory(bool async)
        {
            using MemoryStream archiveStream = new MemoryStream();
            TarWriter writer = CreateTarWriter(archiveStream, TarEntryFormat.Ustar, leaveOpen: true);
            try
            {
                UstarTarEntry directory = new UstarTarEntry(TarEntryType.Directory, InitialEntryName);
                SetDirectory(directory);
                VerifyDirectory(directory);
                await WriteEntry(writer, directory, async);
            }
            finally
            {
                await DisposeTarWriter(writer, async);
            }

            archiveStream.Position = 0;
            TarReader reader = CreateTarReader(archiveStream);
            try
            {
                UstarTarEntry directory = await GetNextEntry(reader, async: async) as UstarTarEntry;
                VerifyDirectory(directory);
            }
            finally
            {
                await DisposeTarReader(reader, async);
            }
        }

        [Theory]
        [MemberData(nameof(GetBooleanData))]
        public async Task WriteCharacterDevice(bool async)
        {
            using MemoryStream archiveStream = new MemoryStream();
            TarWriter writer = CreateTarWriter(archiveStream, TarEntryFormat.Ustar, leaveOpen: true);
            try
            {
                UstarTarEntry charDevice = new UstarTarEntry(TarEntryType.CharacterDevice, InitialEntryName);
                SetCharacterDevice(charDevice);
                VerifyCharacterDevice(charDevice);
                await WriteEntry(writer, charDevice, async);
            }
            finally
            {
                await DisposeTarWriter(writer, async);
            }

            archiveStream.Position = 0;
            TarReader reader = CreateTarReader(archiveStream);
            try
            {
                UstarTarEntry charDevice = await GetNextEntry(reader, async: async) as UstarTarEntry;
                VerifyCharacterDevice(charDevice);
            }
            finally
            {
                await DisposeTarReader(reader, async);
            }
        }

        [Theory]
        [MemberData(nameof(GetBooleanData))]
        public async Task WriteBlockDevice(bool async)
        {
            using MemoryStream archiveStream = new MemoryStream();
            TarWriter writer = CreateTarWriter(archiveStream, TarEntryFormat.Ustar, leaveOpen: true);
            try
            {
                UstarTarEntry blockDevice = new UstarTarEntry(TarEntryType.BlockDevice, InitialEntryName);
                SetBlockDevice(blockDevice);
                VerifyBlockDevice(blockDevice);
                await WriteEntry(writer, blockDevice, async);
            }
            finally
            {
                await DisposeTarWriter(writer, async);
            }

            archiveStream.Position = 0;
            TarReader reader = CreateTarReader(archiveStream);
            try
            {
                UstarTarEntry blockDevice = await GetNextEntry(reader, async: async) as UstarTarEntry;
                VerifyBlockDevice(blockDevice);
            }
            finally
            {
                await DisposeTarReader(reader, async);
            }
        }

        [Theory]
        [MemberData(nameof(GetBooleanData))]
        public async Task WriteFifo(bool async)
        {
            using MemoryStream archiveStream = new MemoryStream();
            TarWriter writer = CreateTarWriter(archiveStream, TarEntryFormat.Ustar, leaveOpen: true);
            try
            {
                UstarTarEntry fifo = new UstarTarEntry(TarEntryType.Fifo, InitialEntryName);
                SetFifo(fifo);
                VerifyFifo(fifo);
                await WriteEntry(writer, fifo, async);
            }
            finally
            {
                await DisposeTarWriter(writer, async);
            }

            archiveStream.Position = 0;
            TarReader reader = CreateTarReader(archiveStream);
            try
            {
                UstarTarEntry fifo = await GetNextEntry(reader, async: async) as UstarTarEntry;
                VerifyFifo(fifo);
            }
            finally
            {
                await DisposeTarReader(reader, async);
            }
        }

        [Theory]
        [InlineData(TarEntryType.HardLink, false)]
        [InlineData(TarEntryType.HardLink, true)]
        [InlineData(TarEntryType.SymbolicLink, false)]
        [InlineData(TarEntryType.SymbolicLink, true)]
        public async Task Write_LinkEntry_EmptyLinkName_Throws(TarEntryType entryType, bool async)
        {
            using MemoryStream archiveStream = new MemoryStream();
            TarWriter writer = CreateTarWriter(archiveStream, leaveOpen: false);
            try
            {
                if (async)
                {
                    await Assert.ThrowsAsync<ArgumentException>("entry", () => writer.WriteEntryAsync(new UstarTarEntry(entryType, "link")));
                }
                else
                {
                    Assert.Throws<ArgumentException>("entry", () => writer.WriteEntry(new UstarTarEntry(entryType, "link")));
                }
            }
            finally
            {
                await DisposeTarWriter(writer, async);
            }
        }
    }
}