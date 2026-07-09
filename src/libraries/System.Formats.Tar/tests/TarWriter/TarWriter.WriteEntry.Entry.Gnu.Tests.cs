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
        [Theory]
        [MemberData(nameof(Get_Boolean_Data))]
        public async Task WriteEntry_Null_Throws(bool async)
        {
            using MemoryStream archiveStream = new MemoryStream();
            TarWriter writer = CreateTarWriter(archiveStream, TarEntryFormat.Gnu, leaveOpen: false);
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
        [MemberData(nameof(Get_Boolean_Data))]
        public async Task WriteRegularFile(bool async)
        {
            using MemoryStream archiveStream = new MemoryStream();
            TarWriter writer = CreateTarWriter(archiveStream, TarEntryFormat.Gnu, leaveOpen: true);
            try
            {
                GnuTarEntry regularFile = new GnuTarEntry(TarEntryType.RegularFile, InitialEntryName);
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
                GnuTarEntry regularFile = await GetNextEntry(reader, async: async) as GnuTarEntry;
                VerifyRegularFile(regularFile, isWritable: false);
            }
            finally
            {
                await DisposeTarReader(reader, async);
            }
        }

        [Theory]
        [MemberData(nameof(Get_Boolean_Data))]
        public async Task WriteHardLink(bool async)
        {
            using MemoryStream archiveStream = new MemoryStream();
            TarWriter writer = CreateTarWriter(archiveStream, TarEntryFormat.Gnu, leaveOpen: true);
            try
            {
                GnuTarEntry hardLink = new GnuTarEntry(TarEntryType.HardLink, InitialEntryName);
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
                GnuTarEntry hardLink = await GetNextEntry(reader, async: async) as GnuTarEntry;
                VerifyHardLink(hardLink);
            }
            finally
            {
                await DisposeTarReader(reader, async);
            }
        }

        [Theory]
        [MemberData(nameof(Get_Boolean_Data))]
        public async Task WriteSymbolicLink(bool async)
        {
            using MemoryStream archiveStream = new MemoryStream();
            TarWriter writer = CreateTarWriter(archiveStream, TarEntryFormat.Gnu, leaveOpen: true);
            try
            {
                GnuTarEntry symbolicLink = new GnuTarEntry(TarEntryType.SymbolicLink, InitialEntryName);
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
                GnuTarEntry symbolicLink = await GetNextEntry(reader, async: async) as GnuTarEntry;
                VerifySymbolicLink(symbolicLink);
            }
            finally
            {
                await DisposeTarReader(reader, async);
            }
        }

        [Theory]
        [MemberData(nameof(Get_Boolean_Data))]
        public async Task WriteDirectory(bool async)
        {
            using MemoryStream archiveStream = new MemoryStream();
            TarWriter writer = CreateTarWriter(archiveStream, TarEntryFormat.Gnu, leaveOpen: true);
            try
            {
                GnuTarEntry directory = new GnuTarEntry(TarEntryType.Directory, InitialEntryName);
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
                GnuTarEntry directory = await GetNextEntry(reader, async: async) as GnuTarEntry;
                VerifyDirectory(directory);
            }
            finally
            {
                await DisposeTarReader(reader, async);
            }
        }

        [Theory]
        [MemberData(nameof(Get_Boolean_Data))]
        public async Task WriteCharacterDevice(bool async)
        {
            using MemoryStream archiveStream = new MemoryStream();
            TarWriter writer = CreateTarWriter(archiveStream, TarEntryFormat.Gnu, leaveOpen: true);
            try
            {
                GnuTarEntry charDevice = new GnuTarEntry(TarEntryType.CharacterDevice, InitialEntryName);
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
                GnuTarEntry charDevice = await GetNextEntry(reader, async: async) as GnuTarEntry;
                VerifyCharacterDevice(charDevice);
            }
            finally
            {
                await DisposeTarReader(reader, async);
            }
        }

        [Theory]
        [MemberData(nameof(Get_Boolean_Data))]
        public async Task WriteBlockDevice(bool async)
        {
            using MemoryStream archiveStream = new MemoryStream();
            TarWriter writer = CreateTarWriter(archiveStream, TarEntryFormat.Gnu, leaveOpen: true);
            try
            {
                GnuTarEntry blockDevice = new GnuTarEntry(TarEntryType.BlockDevice, InitialEntryName);
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
                GnuTarEntry blockDevice = await GetNextEntry(reader, async: async) as GnuTarEntry;
                VerifyBlockDevice(blockDevice);
            }
            finally
            {
                await DisposeTarReader(reader, async);
            }
        }

        [Theory]
        [MemberData(nameof(Get_Boolean_Data))]
        public async Task WriteFifo(bool async)
        {
            using MemoryStream archiveStream = new MemoryStream();
            TarWriter writer = CreateTarWriter(archiveStream, TarEntryFormat.Gnu, leaveOpen: true);
            try
            {
                GnuTarEntry fifo = new GnuTarEntry(TarEntryType.Fifo, InitialEntryName);
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
                GnuTarEntry fifo = await GetNextEntry(reader, async: async) as GnuTarEntry;
                VerifyFifo(fifo);
            }
            finally
            {
                await DisposeTarReader(reader, async);
            }
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
            TarWriter writer = CreateTarWriter(archiveStream, TarEntryFormat.Gnu, leaveOpen: true);
            try
            {
                GnuTarEntry entry = new GnuTarEntry(entryType, longName);
                if (entryType is TarEntryType.HardLink or TarEntryType.SymbolicLink)
                {
                    entry.LinkName = "linktarget";
                }
                await WriteEntry(writer, entry, async);
            }
            finally
            {
                await DisposeTarWriter(writer, async);
            }

            archiveStream.Position = 0;
            TarReader reader = CreateTarReader(archiveStream);
            try
            {
                GnuTarEntry entry = await GetNextEntry(reader, async: async) as GnuTarEntry;
                Assert.Equal(entryType, entry.EntryType);
                Assert.Equal(longName, entry.Name);
            }
            finally
            {
                await DisposeTarReader(reader, async);
            }
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
            TarWriter writer = CreateTarWriter(archiveStream, TarEntryFormat.Gnu, leaveOpen: true);
            try
            {
                GnuTarEntry entry = new GnuTarEntry(entryType, "file.txt");
                entry.LinkName = longLinkName;
                await WriteEntry(writer, entry, async);
            }
            finally
            {
                await DisposeTarWriter(writer, async);
            }

            archiveStream.Position = 0;
            TarReader reader = CreateTarReader(archiveStream);
            try
            {
                GnuTarEntry entry = await GetNextEntry(reader, async: async) as GnuTarEntry;
                Assert.Equal(entryType, entry.EntryType);
                Assert.Equal("file.txt", entry.Name);
                Assert.Equal(longLinkName, entry.LinkName);
            }
            finally
            {
                await DisposeTarReader(reader, async);
            }
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
            TarWriter writer = CreateTarWriter(archiveStream, TarEntryFormat.Gnu, leaveOpen: true);
            try
            {
                GnuTarEntry entry = new GnuTarEntry(entryType, longName);
                entry.LinkName = longLinkName;
                await WriteEntry(writer, entry, async);
            }
            finally
            {
                await DisposeTarWriter(writer, async);
            }

            archiveStream.Position = 0;
            TarReader reader = CreateTarReader(archiveStream);
            try
            {
                GnuTarEntry entry = await GetNextEntry(reader, async: async) as GnuTarEntry;
                Assert.Equal(entryType, entry.EntryType);
                Assert.Equal(longName, entry.Name);
                Assert.Equal(longLinkName, entry.LinkName);
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
                    await Assert.ThrowsAsync<ArgumentException>("entry", () => writer.WriteEntryAsync(new GnuTarEntry(entryType, "link")));
                }
                else
                {
                    Assert.Throws<ArgumentException>("entry", () => writer.WriteEntry(new GnuTarEntry(entryType, "link")));
                }
            }
            finally
            {
                await DisposeTarWriter(writer, async);
            }
        }
    }
}