// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace System.Formats.Tar.Tests
{
    // Tests specific to V7 format.
    public class TarWriter_WriteEntry_V7_Tests : TarWriter_WriteEntry_Base
    {
        protected override TarEntryFormat TestFormat => TarEntryFormat.V7;

        [Theory]
        [MemberData(nameof(GetBooleanData))]
        public async Task WriteRegularFile(bool async)
        {
            using MemoryStream archiveStream = new MemoryStream();
            {
                await using TarWriterHolder writerHolder = CreateTarWriter(archiveStream, async, TarEntryFormat.V7, leaveOpen: true);
                TarWriter writer = writerHolder;

                V7TarEntry oldRegularFile = new V7TarEntry(TarEntryType.V7RegularFile, InitialEntryName);
                SetRegularFile(oldRegularFile);
                VerifyRegularFile(oldRegularFile, isWritable: true);
                await WriteEntry(writer, oldRegularFile, async);
                        }

            archiveStream.Position = 0;
            {
                await using TarReaderHolder readerHolder = CreateTarReader(archiveStream, async, leaveOpen: false);
                TarReader reader = readerHolder;

                V7TarEntry oldRegularFile = await GetNextEntry(reader, async: async) as V7TarEntry;
                VerifyRegularFile(oldRegularFile, isWritable: false);
                        }
        }

        [Theory]
        [MemberData(nameof(GetBooleanData))]
        public async Task WriteHardLink(bool async)
        {
            using MemoryStream archiveStream = new MemoryStream();
            {
                await using TarWriterHolder writerHolder = CreateTarWriter(archiveStream, async, TarEntryFormat.V7, leaveOpen: true);
                TarWriter writer = writerHolder;

                V7TarEntry hardLink = new V7TarEntry(TarEntryType.HardLink, InitialEntryName);
                SetHardLink(hardLink);
                VerifyHardLink(hardLink);
                await WriteEntry(writer, hardLink, async);
                        }

            archiveStream.Position = 0;
            {
                await using TarReaderHolder readerHolder = CreateTarReader(archiveStream, async, leaveOpen: false);
                TarReader reader = readerHolder;

                V7TarEntry hardLink = await GetNextEntry(reader, async: async) as V7TarEntry;
                VerifyHardLink(hardLink);
                        }
        }

        [Theory]
        [MemberData(nameof(GetBooleanData))]
        public async Task WriteSymbolicLink(bool async)
        {
            using MemoryStream archiveStream = new MemoryStream();
            {
                await using TarWriterHolder writerHolder = CreateTarWriter(archiveStream, async, TarEntryFormat.V7, leaveOpen: true);
                TarWriter writer = writerHolder;

                V7TarEntry symbolicLink = new V7TarEntry(TarEntryType.SymbolicLink, InitialEntryName);
                SetSymbolicLink(symbolicLink);
                VerifySymbolicLink(symbolicLink);
                await WriteEntry(writer, symbolicLink, async);
                        }

            archiveStream.Position = 0;
            {
                await using TarReaderHolder readerHolder = CreateTarReader(archiveStream, async, leaveOpen: false);
                TarReader reader = readerHolder;

                V7TarEntry symbolicLink = await GetNextEntry(reader, async: async) as V7TarEntry;
                VerifySymbolicLink(symbolicLink);
                        }
        }

        [Theory]
        [MemberData(nameof(GetBooleanData))]
        public async Task WriteDirectory(bool async)
        {
            using MemoryStream archiveStream = new MemoryStream();
            {
                await using TarWriterHolder writerHolder = CreateTarWriter(archiveStream, async, TarEntryFormat.V7, leaveOpen: true);
                TarWriter writer = writerHolder;

                V7TarEntry directory = new V7TarEntry(TarEntryType.Directory, InitialEntryName);
                SetDirectory(directory);
                VerifyDirectory(directory);
                await WriteEntry(writer, directory, async);
                        }

            archiveStream.Position = 0;
            {
                await using TarReaderHolder readerHolder = CreateTarReader(archiveStream, async, leaveOpen: false);
                TarReader reader = readerHolder;

                V7TarEntry directory = await GetNextEntry(reader, async: async) as V7TarEntry;
                VerifyDirectory(directory);
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
            {
                await using TarWriterHolder writerHolder = CreateTarWriter(archiveStream, async, leaveOpen: false);
                TarWriter writer = writerHolder;

                await Assert.ThrowsAsync<ArgumentException>("entry", () => WriteEntry(writer, new V7TarEntry(entryType, "link"), async));
                        }
        }
    }
}