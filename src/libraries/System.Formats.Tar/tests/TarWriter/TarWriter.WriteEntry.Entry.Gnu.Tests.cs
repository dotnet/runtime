// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace System.Formats.Tar.Tests
{
    // Tests specific to Gnu format.
    public class TarWriter_WriteEntry_Gnu_Tests : TarWriter_WriteEntry_Posix_Base
    {
        protected override TarEntryFormat TestFormat => TarEntryFormat.Gnu;

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
            await using (TarWriterHolder writerHolder = CreateTarWriter(archiveStream, async, TarEntryFormat.Gnu, leaveOpen: true))
            {
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
            await using (TarWriterHolder writerHolder = CreateTarWriter(archiveStream, async, TarEntryFormat.Gnu, leaveOpen: true))
            {
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
            await using (TarWriterHolder writerHolder = CreateTarWriter(archiveStream, async, TarEntryFormat.Gnu, leaveOpen: true))
            {
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
    }
}
