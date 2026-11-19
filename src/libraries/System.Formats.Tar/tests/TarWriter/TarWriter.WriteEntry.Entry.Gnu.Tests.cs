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

        // The GNU header fields 'offset', 'longnames', 'unused', 'sparse', 'isextended' and 'realsize'
        // are not exposed publicly. They must be preserved when a multi-volume entry is written into another GNU archive,
        // and cleared for entry types that GNU tar never sets them for.
        private const int GnuUnusedFieldsOffset = 369;
        private const int GnuUnusedFieldsLength = 126;

        [Theory]
        [InlineData(TarEntryType.RegularFile, false, false)]
        [InlineData(TarEntryType.RegularFile, false, true)]
        [InlineData(TarEntryType.RegularFile, true, false)]
        [InlineData(TarEntryType.RegularFile, true, true)]
        [InlineData(TarEntryType.MultiVolume, false, false)]
        [InlineData(TarEntryType.MultiVolume, false, true)]
        public async Task Write_UnusedGnuFields(TarEntryType entryType, bool convertWithConstructor, bool async)
        {
            byte[] content = "Hello, GNU!"u8.ToArray();
            byte[] sourceArchive = BuildRawGnuArchiveWithUnusedFields(entryType, content);
            byte[] expectedUnusedFields = entryType is TarEntryType.MultiVolume
                ? sourceArchive.AsSpan(GnuUnusedFieldsOffset, GnuUnusedFieldsLength).ToArray()
                : new byte[GnuUnusedFieldsLength];

            using MemoryStream destination = new MemoryStream();
            await using (TarReaderHolder readerHolder = CreateTarReader(new MemoryStream(sourceArchive), async, leaveOpen: false))
            await using (TarWriterHolder writerHolder = CreateTarWriter(destination, async, TarEntryFormat.Gnu, leaveOpen: true))
            {
                TarReader reader = readerHolder;
                TarWriter writer = writerHolder;

                TarEntry entry = await GetNextEntry(reader, async: async);
                Assert.IsType<GnuTarEntry>(entry);
                Assert.Equal(entryType, entry.EntryType);

                if (convertWithConstructor)
                {
                    entry = new GnuTarEntry(entry);
                }

                await WriteEntry(writer, entry, async);
            }

            byte[] destinationArchive = destination.ToArray();
            Assert.Equal(expectedUnusedFields, destinationArchive.AsSpan(GnuUnusedFieldsOffset, GnuUnusedFieldsLength).ToArray());
            Assert.False(destinationArchive.AsSpan(GnuUnusedFieldsOffset + GnuUnusedFieldsLength, 512 - GnuUnusedFieldsOffset - GnuUnusedFieldsLength).ContainsAnyExcept((byte)0));

            // The checksum must account for the preserved bytes, so the archive must be readable.
            destination.Position = 0;
            await using TarReaderHolder destinationReaderHolder = CreateTarReader(destination, async, leaveOpen: false);
            TarReader destinationReader = destinationReaderHolder;

            TarEntry roundtripped = await GetNextEntry(destinationReader, async: async);
            Assert.IsType<GnuTarEntry>(roundtripped);
            Assert.Equal(entryType, roundtripped.EntryType);
            Assert.Equal("file.txt", roundtripped.Name);
            using (MemoryStream data = new MemoryStream())
            {
                roundtripped.DataStream.CopyTo(data);
                Assert.Equal(content, data.ToArray());
            }

            Assert.Null(await GetNextEntry(destinationReader, async: async));
        }

        // Builds a GNU archive with one entry whose header has the fields that are not exposed publicly set,
        // like GNU tar does for multi-volume and old GNU sparse entries.
        private static byte[] BuildRawGnuArchiveWithUnusedFields(TarEntryType entryType, byte[] content)
        {
            byte[] header = new byte[512];
            "file.txt"u8.CopyTo(header);
            WriteRawOctalField(header, 100, 8, Convert.ToInt32("644", 8)); // mode
            WriteRawOctalField(header, 108, 8, 0); // uid
            WriteRawOctalField(header, 116, 8, 0); // gid
            WriteRawOctalField(header, 124, 12, content.Length); // size
            WriteRawOctalField(header, 136, 12, 1700000000); // mtime
            header[156] = (byte)entryType;
            "ustar "u8.CopyTo(header.AsSpan(257)); // GNU magic
            " \0"u8.CopyTo(header.AsSpan(263)); // GNU version

            WriteRawOctalField(header, 369, 12, 512); // offset
            "abcd"u8.CopyTo(header.AsSpan(381)); // longnames: not used by GNU tar, set to verify every byte is preserved
            header[385] = (byte)'x'; // unused: set to verify every byte is preserved
            for (int i = 0; i < 4; i++)
            {
                WriteRawOctalField(header, 386 + (i * 24), 12, i * 1024); // sparse[i].offset
                WriteRawOctalField(header, 398 + (i * 24), 12, 16); // sparse[i].numbytes
            }
            header[482] = 1; // isextended
            WriteRawOctalField(header, 483, 12, 4096); // realsize
            header[494] = 0x80; // last byte of realsize: set to verify the whole range is preserved

            WriteHeaderChecksum(header);

            using MemoryStream archive = new MemoryStream();
            archive.Write(header);
            archive.Write(content);
            PadToTarBlockBoundary(archive);
            archive.Write(new byte[1024]);
            return archive.ToArray();
        }
    }
}
