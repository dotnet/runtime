// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.Formats.Tar.Tests
{
    public class GnuTarEntry_Tests : TarTestsBase
    {
        [Fact]
        public void Constructor_InvalidEntryName()
        {
            Assert.Throws<ArgumentNullException>(() => new GnuTarEntry(TarEntryType.RegularFile, entryName: null));
            Assert.Throws<ArgumentException>(() => new GnuTarEntry(TarEntryType.RegularFile, entryName: string.Empty));
        }

        [Fact]
        public void Constructor_UnsupportedEntryTypes()
        {
            Assert.Throws<ArgumentException>(() => new GnuTarEntry((TarEntryType)byte.MaxValue, InitialEntryName));

            Assert.Throws<ArgumentException>(() => new GnuTarEntry(TarEntryType.ExtendedAttributes, InitialEntryName));
            Assert.Throws<ArgumentException>(() => new GnuTarEntry(TarEntryType.GlobalExtendedAttributes, InitialEntryName));
            Assert.Throws<ArgumentException>(() => new GnuTarEntry(TarEntryType.V7RegularFile, InitialEntryName));

            // These are specific to GNU, but currently the user cannot create them manually
            Assert.Throws<ArgumentException>(() => new GnuTarEntry(TarEntryType.ContiguousFile, InitialEntryName));
            Assert.Throws<ArgumentException>(() => new GnuTarEntry(TarEntryType.DirectoryList, InitialEntryName));
            Assert.Throws<ArgumentException>(() => new GnuTarEntry(TarEntryType.MultiVolume, InitialEntryName));
            Assert.Throws<ArgumentException>(() => new GnuTarEntry(TarEntryType.RenamedOrSymlinked, InitialEntryName));
            Assert.Throws<ArgumentException>(() => new GnuTarEntry(TarEntryType.SparseFile, InitialEntryName));
            Assert.Throws<ArgumentException>(() => new GnuTarEntry(TarEntryType.TapeVolume, InitialEntryName));

            // The user should not create these entries manually
            Assert.Throws<ArgumentException>(() => new GnuTarEntry(TarEntryType.LongLink, InitialEntryName));
            Assert.Throws<ArgumentException>(() => new GnuTarEntry(TarEntryType.LongPath, InitialEntryName));
        }

        [Fact]
        public void SupportedEntryType_RegularFile()
        {
            GnuTarEntry regularFile = new GnuTarEntry(TarEntryType.RegularFile, InitialEntryName);
            SetRegularFile(regularFile);
            VerifyRegularFile(regularFile, isWritable: true);
        }

        [Fact]
        public void SupportedEntryType_Directory()
        {
            GnuTarEntry directory = new GnuTarEntry(TarEntryType.Directory, InitialEntryName);
            SetDirectory(directory);
            VerifyDirectory(directory);
        }

        [Fact]
        public void SupportedEntryType_HardLink()
        {
            GnuTarEntry hardLink = new GnuTarEntry(TarEntryType.HardLink, InitialEntryName);
            SetHardLink(hardLink);
            VerifyHardLink(hardLink);
        }

        [Fact]
        public void SupportedEntryType_SymbolicLink()
        {
            GnuTarEntry symbolicLink = new GnuTarEntry(TarEntryType.SymbolicLink, InitialEntryName);
            SetSymbolicLink(symbolicLink);
            VerifySymbolicLink(symbolicLink);
        }

        [Fact]
        public void SupportedEntryType_BlockDevice()
        {
            GnuTarEntry blockDevice = new GnuTarEntry(TarEntryType.BlockDevice, InitialEntryName);
            SetBlockDevice(blockDevice);
            VerifyBlockDevice(blockDevice);
        }

        [Fact]
        public void SupportedEntryType_CharacterDevice()
        {
            GnuTarEntry characterDevice = new GnuTarEntry(TarEntryType.CharacterDevice, InitialEntryName);
            SetCharacterDevice(characterDevice);
            VerifyCharacterDevice(characterDevice);
        }

        [Fact]
        public void SupportedEntryType_Fifo()
        {
            GnuTarEntry fifo = new GnuTarEntry(TarEntryType.Fifo, InitialEntryName);
            SetFifo(fifo);
            VerifyFifo(fifo);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void DataOffset_RegularFile(bool canSeek)
        {
            using MemoryStream ms = new();
            using (TarWriter writer = new(ms, leaveOpen: true))
            {
                GnuTarEntry entry = new GnuTarEntry(TarEntryType.RegularFile, InitialEntryName);
                entry.DataStream = new MemoryStream();
                entry.DataStream.WriteByte(ExpectedOffsetDataSingleByte);
                entry.DataStream.Position = 0;
                writer.WriteEntry(entry);
            }
            ms.Position = 0;

            using Stream streamToRead = new WrappedStream(ms, canWrite: true, canRead: true, canSeek: canSeek);
            using TarReader reader = new(streamToRead);
            TarEntry actualEntry = reader.GetNextEntry();
            Assert.NotNull(actualEntry);

            // Then it writes the actual regular file entry, containing:
            // * 512 bytes of the regular tar header
            // Totalling 2560.
            // The regular file data section starts on the next byte.
            long expectedDataOffset = canSeek ? 512 : -1;
            Assert.Equal(expectedDataOffset, actualEntry.DataOffset);

            if (canSeek)
            {
                ms.Position = actualEntry.DataOffset;
                byte actualData = (byte)ms.ReadByte();
                Assert.Equal(ExpectedOffsetDataSingleByte, actualData);
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task DataOffset_RegularFile_Async(bool canSeek)
        {
            await using MemoryStream ms = new();
            await using (TarWriter writer = new(ms, leaveOpen: true))
            {
                GnuTarEntry entry = new GnuTarEntry(TarEntryType.RegularFile, InitialEntryName);
                entry.DataStream = new MemoryStream();
                entry.DataStream.WriteByte(ExpectedOffsetDataSingleByte);
                entry.DataStream.Position = 0;
                await writer.WriteEntryAsync(entry);
            }
            ms.Position = 0;

            await using Stream streamToRead = new WrappedStream(ms, canWrite: true, canRead: true, canSeek: canSeek);
            await using TarReader reader = new(streamToRead);
            TarEntry actualEntry = await reader.GetNextEntryAsync();
            Assert.NotNull(actualEntry);

            // Then it writes the actual regular file entry, containing:
            // * 512 bytes of the regular tar header
            // Totalling 2560.
            // The regular file data section starts on the next byte.
            long expectedDataOffset = canSeek ? 512 : -1;
            Assert.Equal(expectedDataOffset, actualEntry.DataOffset);

            if (canSeek)
            {
                ms.Position = actualEntry.DataOffset;
                byte actualData = (byte)ms.ReadByte();
                Assert.Equal(ExpectedOffsetDataSingleByte, actualData);
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void DataOffset_RegularFile_LongPath(bool canSeek)
        {
            using MemoryStream ms = new();
            using (TarWriter writer = new(ms, leaveOpen: true))
            {
                string veryLongName = new string('a', 1234); // Forces using a GNU LongPath entry
                GnuTarEntry entry = new GnuTarEntry(TarEntryType.RegularFile, veryLongName);
                entry.DataStream = new MemoryStream();
                entry.DataStream.WriteByte(ExpectedOffsetDataSingleByte);
                entry.DataStream.Position = 0;
                writer.WriteEntry(entry);
            }
            ms.Position = 0;

            using Stream streamToRead = new WrappedStream(ms, canWrite: true, canRead: true, canSeek: canSeek);
            using TarReader reader = new(streamToRead);
            TarEntry actualEntry = reader.GetNextEntry();
            Assert.NotNull(actualEntry);

            // GNU first writes the long path entry, containing:
            // * 512 bytes of the regular tar header
            // * 1234 bytes for the data section containing the full long path
            // * 302 bytes of padding
            // Then it writes the actual regular file entry, containing:
            // * 512 bytes of the regular tar header
            // Totalling 2560.
            // The regular file data section starts on the next byte.
            long expectedDataOffset = canSeek ? 2560 : -1;
            Assert.Equal(expectedDataOffset, actualEntry.DataOffset);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task DataOffset_RegularFile_LongPath_Async(bool canSeek)
        {
            await using MemoryStream ms = new();
            await using (TarWriter writer = new(ms, leaveOpen: true))
            {
                string veryLongName = new string('a', 1234); // Forces using a GNU LongPath entry
                GnuTarEntry entry = new GnuTarEntry(TarEntryType.RegularFile, veryLongName);
                entry.DataStream = new MemoryStream();
                entry.DataStream.WriteByte(ExpectedOffsetDataSingleByte);
                entry.DataStream.Position = 0;
                await writer.WriteEntryAsync(entry);
            }
            ms.Position = 0;

            await using Stream streamToRead = new WrappedStream(ms, canWrite: true, canRead: true, canSeek: canSeek);
            await using TarReader reader = new(streamToRead);
            TarEntry actualEntry = await reader.GetNextEntryAsync();
            Assert.NotNull(actualEntry);

            // GNU first writes the long path entry, containing:
            // * 512 bytes of the regular tar header
            // * 1234 bytes for the data section containing the full long path
            // * 302 bytes of padding
            // Then it writes the actual regular file entry, containing:
            // * 512 bytes of the regular tar header
            // Totalling 2560.
            // The regular file data section starts on the next byte.
            long expectedDataOffset = canSeek ? 2560 : -1;
            Assert.Equal(expectedDataOffset, actualEntry.DataOffset);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void DataOffset_RegularFile_LongLink(bool canSeek)
        {
            using MemoryStream ms = new();
            using (TarWriter writer = new(ms, leaveOpen: true))
            {
                GnuTarEntry entry = new GnuTarEntry(TarEntryType.SymbolicLink, InitialEntryName);
                entry.LinkName = new string('a', 1234); // Forces using a GNU LongLink entry
                writer.WriteEntry(entry);
            }
            ms.Position = 0;

            using Stream streamToRead = new WrappedStream(ms, canWrite: true, canRead: true, canSeek: canSeek);
            using TarReader reader = new(streamToRead);
            TarEntry actualEntry = reader.GetNextEntry();
            Assert.NotNull(actualEntry);

            // GNU first writes the long link entry, containing:
            // * 512 bytes of the regular tar header
            // * 1234 bytes for the data section containing the full long link
            // * 302 bytes of padding
            // Then it writes the actual regular file entry, containing:
            // * 512 bytes of the regular tar header
            // Totalling 2560.
            // The regular file data section starts on the next byte.
            long expectedDataOffset = canSeek ? 2560 : -1;
            Assert.Equal(expectedDataOffset, actualEntry.DataOffset);
        }
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task DataOffset_RegularFile_LongLink_Async(bool canSeek)
        {
            await using MemoryStream ms = new();
            await using (TarWriter writer = new(ms, leaveOpen: true))
            {
                GnuTarEntry entry = new GnuTarEntry(TarEntryType.SymbolicLink, InitialEntryName);
                entry.LinkName = new string('b', 1234); // Forces using a GNU LongLink entry
                await writer.WriteEntryAsync(entry);
            }
            ms.Position = 0;

            await using Stream streamToRead = new WrappedStream(ms, canWrite: true, canRead: true, canSeek: canSeek);
            await using TarReader reader = new(streamToRead);
            TarEntry actualEntry = await reader.GetNextEntryAsync();
            Assert.NotNull(actualEntry);

            // GNU first writes the long link entry, containing:
            // * 512 bytes of the regular tar header
            // * 1234 bytes for the data section containing the full long link
            // * 302 bytes of padding
            // Then it writes the actual regular file entry, containing:
            // * 512 bytes of the regular tar header
            // Totalling 2560.
            // The regular file data section starts on the next byte.
            long expectedDataOffset = canSeek ? 2560 : -1;
            Assert.Equal(expectedDataOffset, actualEntry.DataOffset);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void DataOffset_RegularFile_LongLink_LongPath(bool canSeek)
        {
            using MemoryStream ms = new();
            using (TarWriter writer = new(ms, leaveOpen: true))
            {
                string veryLongName = new string('a', 1234); // Forces using a GNU LongPath entry
                GnuTarEntry entry = new GnuTarEntry(TarEntryType.SymbolicLink, veryLongName);
                entry.LinkName = new string('b', 1234); // Forces using a GNU LongLink entry
                writer.WriteEntry(entry);
            }
            ms.Position = 0;

            using Stream streamToRead = new WrappedStream(ms, canWrite: true, canRead: true, canSeek: canSeek);
            using TarReader reader = new(streamToRead);
            TarEntry actualEntry = reader.GetNextEntry();
            Assert.NotNull(actualEntry);

            // GNU first writes the long link and long path entries, containing:
            // * 512 bytes of the regular long link tar header
            // * 1234 bytes for the data section containing the full long link
            // * 302 bytes of padding
            // * 512 bytes of the regular long path tar header
            // * 1234 bytes for the data section containing the full long path
            // * 302 bytes of padding
            // Then it writes the actual entry, containing:
            // * 512 bytes of the regular tar header
            // Totalling 4608.
            // The data section starts on the next byte.
            long expectedDataOffset = canSeek ? 4608 : -1;
            Assert.Equal(expectedDataOffset, actualEntry.DataOffset);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task DataOffset_RegularFile_LongLink_LongPath_Async(bool canSeek)
        {
            await using MemoryStream ms = new();
            await using (TarWriter writer = new(ms, leaveOpen: true))
            {
                string veryLongName = new string('a', 1234); // Forces using a GNU LongPath entry
                GnuTarEntry entry = new GnuTarEntry(TarEntryType.SymbolicLink, veryLongName);
                entry.LinkName = new string('b', 1234); // Forces using a GNU LongLink entry
                await writer.WriteEntryAsync(entry);
            }
            ms.Position = 0;

            await using Stream streamToRead = new WrappedStream(ms, canWrite: true, canRead: true, canSeek: canSeek);
            await using TarReader reader = new(streamToRead);
            TarEntry actualEntry = await reader.GetNextEntryAsync();
            Assert.NotNull(actualEntry);

            // GNU first writes the long link and long path entries, containing:
            // * 512 bytes of the regular long link tar header
            // * 1234 bytes for the data section containing the full long link
            // * 302 bytes of padding
            // * 512 bytes of the regular long path tar header
            // * 1234 bytes for the data section containing the full long path
            // * 302 bytes of padding
            // Then it writes the actual entry, containing:
            // * 512 bytes of the regular tar header
            // Totalling 4608.
            // The data section starts on the next byte.
            long expectedDataOffset = canSeek ? 4608 : -1;
            Assert.Equal(expectedDataOffset, actualEntry.DataOffset);
        }

        [Fact]
        public void DataOffset_BeforeAndAfterArchive()
        {
            GnuTarEntry entry = new GnuTarEntry(TarEntryType.RegularFile, InitialEntryName);
            Assert.Equal(-1, entry.DataOffset);
            entry.DataStream = new MemoryStream();
            entry.DataStream.WriteByte(ExpectedOffsetDataSingleByte);
            entry.DataStream.Position = 0; // The data stream is written to the archive from the current position

            using MemoryStream ms = new();
            using TarWriter writer = new(ms);
            writer.WriteEntry(entry);
            Assert.Equal(512, entry.DataOffset);

            // Write it again, the offset should now point to the second written entry
            // First entry 512 (header) + 1 (data) + 511 (padding)
            // Second entry 512 (header)
            // 512 + 512 + 512 = 1536
            writer.WriteEntry(entry);
            Assert.Equal(1536, entry.DataOffset);
        }

        [Fact]
        public async Task DataOffset_BeforeAndAfterArchive_Async()
        {
            GnuTarEntry entry = new GnuTarEntry(TarEntryType.RegularFile, InitialEntryName);
            Assert.Equal(-1, entry.DataOffset);

            entry.DataStream = new MemoryStream();
            entry.DataStream.WriteByte(ExpectedOffsetDataSingleByte);
            entry.DataStream.Position = 0; // The data stream is written to the archive from the current position

            await using MemoryStream ms = new();
            await using TarWriter writer = new(ms);
            await writer.WriteEntryAsync(entry);
            Assert.Equal(512, entry.DataOffset);

            // Write it again, the offset should now point to the second written entry
            // First entry 512 (header) + 1 (data) + 511 (padding)
            // Second entry 512 (header)
            // 512 + 512 + 512 = 1536
            await writer.WriteEntryAsync(entry);
            Assert.Equal(1536, entry.DataOffset);
        }

        [Fact]
        public void DataOffset_UnseekableDataStream()
        {
            using MemoryStream ms = new();
            using (TarWriter writer = new(ms, leaveOpen: true))
            {
                GnuTarEntry entry = new GnuTarEntry(TarEntryType.RegularFile, InitialEntryName);
                Assert.Equal(-1, entry.DataOffset);

                using MemoryStream dataStream = new();
                dataStream.WriteByte(ExpectedOffsetDataSingleByte);
                dataStream.Position = 0;
                using WrappedStream wds = new(dataStream, canWrite: true, canRead: true, canSeek: false);
                entry.DataStream = wds;

                writer.WriteEntry(entry);
            }
            ms.Position = 0;

            using TarReader reader = new(ms);
            TarEntry actualEntry = reader.GetNextEntry();
            Assert.NotNull(actualEntry);
            // Gnu header length is 512, data starts in the next position
            Assert.Equal(512, actualEntry.DataOffset);
        }

        [Fact]
        public async Task DataOffset_UnseekableDataStream_Async()
        {
            await using MemoryStream ms = new();
            await using (TarWriter writer = new(ms, leaveOpen: true))
            {
                GnuTarEntry entry = new GnuTarEntry(TarEntryType.RegularFile, InitialEntryName);
                Assert.Equal(-1, entry.DataOffset);

                await using MemoryStream dataStream = new();
                dataStream.WriteByte(ExpectedOffsetDataSingleByte);
                dataStream.Position = 0;
                await using WrappedStream wds = new(dataStream, canWrite: true, canRead: true, canSeek: false);
                entry.DataStream = wds;

                await writer.WriteEntryAsync(entry);
            }
            ms.Position = 0;

            await using TarReader reader = new(ms);
            TarEntry actualEntry = await reader.GetNextEntryAsync();
            Assert.NotNull(actualEntry);
            // Gnu header length is 512, data starts in the next position
            Assert.Equal(512, actualEntry.DataOffset);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void DataOffset_LongPath_LongLink_SecondEntry(bool canSeek)
        {
            string veryLongPathName = new string('a', 1234); // Forces using a GNU LongPath entry
            string veryLongLinkName = new string('b', 1234); // Forces using a GNU LongLink entry

            using MemoryStream ms = new();
            using (TarWriter writer = new(ms, leaveOpen: true))
            {
                GnuTarEntry entry1 = new GnuTarEntry(TarEntryType.SymbolicLink, veryLongPathName);
                entry1.LinkName = veryLongLinkName;
                writer.WriteEntry(entry1);

                GnuTarEntry entry2 = new GnuTarEntry(TarEntryType.SymbolicLink, veryLongPathName);
                entry2.LinkName = veryLongLinkName;
                writer.WriteEntry(entry2);
            }
            ms.Position = 0;

            using Stream streamToRead = new WrappedStream(ms, canWrite: true, canRead: true, canSeek: canSeek);
            using TarReader reader = new(streamToRead);
            TarEntry firstEntry = reader.GetNextEntry();
            Assert.NotNull(firstEntry);
            // GNU first writes the long link and long path entries, containing:
            // * 512 bytes of the regular long link tar header
            // * 1234 bytes for the data section containing the full long link
            // * 302 bytes of padding
            // * 512 bytes of the regular long path tar header
            // * 1234 bytes for the data section containing the full long path
            // * 302 bytes of padding
            // Then it writes the actual regular file entry, containing:
            // * 512 bytes of the regular tar header
            // Totalling 4608.
            // The regular file data section starts on the next byte.
            long firstExpectedDataOffset = canSeek ? 4608 : -1;
            Assert.Equal(firstExpectedDataOffset, firstEntry.DataOffset);

            TarEntry secondEntry = reader.GetNextEntry();
            Assert.NotNull(secondEntry);
            // First entry (including its long link and long path entries) end at 4608 (no padding, empty, as symlink has no data)
            // Second entry (including its long link and long path entries) data section starts one byte after 4608 + 4608 = 9216
            long secondExpectedDataOffset = canSeek ? 9216 : -1;
            Assert.Equal(secondExpectedDataOffset, secondEntry.DataOffset);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task DataOffset_LongPath_LongLink_SecondEntry_Async(bool canSeek)
        {
            string veryLongPathName = new string('a', 1234); // Forces using a GNU LongPath entry
            string veryLongLinkName = new string('b', 1234); // Forces using a GNU LongLink entry

            await using MemoryStream ms = new();
            await using (TarWriter writer = new(ms, leaveOpen: true))
            {
                GnuTarEntry entry1 = new GnuTarEntry(TarEntryType.SymbolicLink, veryLongPathName);
                entry1.LinkName = veryLongLinkName;
                await writer.WriteEntryAsync(entry1);

                GnuTarEntry entry2 = new GnuTarEntry(TarEntryType.SymbolicLink, veryLongPathName);
                entry2.LinkName = veryLongLinkName;
                await writer.WriteEntryAsync(entry2);
            }
            ms.Position = 0;

            await using Stream streamToRead = new WrappedStream(ms, canWrite: true, canRead: true, canSeek: canSeek);
            await using TarReader reader = new(streamToRead);
            TarEntry firstEntry = await reader.GetNextEntryAsync();
            Assert.NotNull(firstEntry);
            // GNU first writes the long link and long path entries, containing:
            // * 512 bytes of the regular long link tar header
            // * 1234 bytes for the data section containing the full long link
            // * 302 bytes of padding
            // * 512 bytes of the regular long path tar header
            // * 1234 bytes for the data section containing the full long path
            // * 302 bytes of padding
            // Then it writes the actual regular file entry, containing:
            // * 512 bytes of the regular tar header
            // Totalling 4608.
            // The regular file data section starts on the next byte.
            long firstExpectedDataOffset = canSeek ? 4608 : -1;
            Assert.Equal(firstExpectedDataOffset, firstEntry.DataOffset);

            TarEntry secondEntry = await reader.GetNextEntryAsync();
            Assert.NotNull(secondEntry);
            // First entry (including its long link and long path entries) end at 4608 (no padding, empty, as symlink has no data)
            // Second entry (including its long link and long path entries) data section starts one byte after 4608 + 4608 = 9216
            long secondExpectedDataOffset = canSeek ? 9216 : -1;
            Assert.Equal(secondExpectedDataOffset, secondEntry.DataOffset);
        }

        [Fact]
        public void UnusedBytesInSizeFieldShouldBeZeroChars()
        {
            // The GNU format sets the unused bytes in the size field to "0" characters.

            using MemoryStream ms = new();

            using (TarWriter writer = new(ms, TarEntryFormat.Gnu, leaveOpen: true))
            {
                // Start with a regular file entry without data
                GnuTarEntry entry = new GnuTarEntry(TarEntryType.RegularFile, InitialEntryName);
                writer.WriteEntry(entry);
            }
            ms.Position = 0;

            using (TarReader reader = new(ms, leaveOpen: true))
            {
                GnuTarEntry entry = reader.GetNextEntry() as GnuTarEntry;
                Assert.NotNull(entry);
                Assert.Equal(0, entry.Length);
            }
            ms.Position = 0;
            ValidateUnusedBytesInSizeField(ms, 0);

            ms.SetLength(0); // Reset the stream
            byte[] data = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }; // 8 bytes of data means a size of 10 in octal
            using (TarWriter writer = new(ms, TarEntryFormat.Gnu, leaveOpen: true))
            {
                // Start with a regular file entry with data
                GnuTarEntry entry = new GnuTarEntry(TarEntryType.RegularFile, InitialEntryName);
                entry.DataStream = new MemoryStream(data);
                writer.WriteEntry(entry);
            }

            ms.Position = 0;
            using (TarReader reader = new(ms, leaveOpen: true))
            {
                GnuTarEntry entry = reader.GetNextEntry() as GnuTarEntry;
                Assert.NotNull(entry);
                Assert.Equal(data.Length, entry.Length);
            }
            ms.Position = 0;
            ValidateUnusedBytesInSizeField(ms, data.Length);
        }

        [Fact]
        public async Task UnusedBytesInSizeFieldShouldBeZeroChars_Async()
        {
            // The GNU format sets the unused bytes in the size field to "0" characters.

            await using MemoryStream ms = new();

            await using (TarWriter writer = new(ms, TarEntryFormat.Gnu, leaveOpen: true))
            {
                // Start with a regular file entry without data
                GnuTarEntry entry = new GnuTarEntry(TarEntryType.RegularFile, InitialEntryName);
                await writer.WriteEntryAsync(entry);
            }
            ms.Position = 0;

            await using (TarReader reader = new(ms, leaveOpen: true))
            {
                GnuTarEntry entry = await reader.GetNextEntryAsync() as GnuTarEntry;
                Assert.NotNull(entry);
                Assert.Equal(0, entry.Length);
            }
            ms.Position = 0;
            ValidateUnusedBytesInSizeField(ms, 0);

            ms.SetLength(0); // Reset the stream
            byte[] data = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }; // 8 bytes of data means a size of 10 in octal
            await using (TarWriter writer = new(ms, TarEntryFormat.Gnu, leaveOpen: true))
            {
                // Start with a regular file entry with data
                GnuTarEntry entry = new GnuTarEntry(TarEntryType.RegularFile, InitialEntryName);
                entry.DataStream = new MemoryStream(data);
                await writer.WriteEntryAsync(entry);
            }

            ms.Position = 0;
            await using (TarReader reader = new(ms, leaveOpen: true))
            {
                GnuTarEntry entry = await reader.GetNextEntryAsync() as GnuTarEntry;
                Assert.NotNull(entry);
                Assert.Equal(data.Length, entry.Length);
            }
            ms.Position = 0;
            ValidateUnusedBytesInSizeField(ms, data.Length);
        }

        private void ValidateUnusedBytesInSizeField(MemoryStream ms, long size)
        {
            // internally, the unused bytes in the size field should be "0" characters,
            // and the rest should be the octal value of the size field

            // name, mode, uid, gid,
            // size
            int sizeStart = 100 + 8 + 8 + 8;
            byte[] buffer = new byte[12]; // The size field is 12 bytes in length

            ms.Seek(sizeStart, SeekOrigin.Begin);
            ms.Read(buffer);

            // Convert the base 10 value of size to base 8
            string octalSize = Convert.ToString(size, 8).PadLeft(11, '0');
            byte[] octalSizeBytes = Encoding.ASCII.GetBytes(octalSize);
            // The last byte should be a null character
            Assert.Equal(octalSizeBytes, buffer.Take(octalSizeBytes.Length).ToArray());
        }

        public static IEnumerable<object[]> NameAndLink_TestData()
        {
            // Name and link have a max length of 100. Anything longer goes into LongPath or LongLink entries in GNU.
            yield return new object[] { InitialEntryName, InitialEntryName }; // Short name and short link
            yield return new object[] { InitialEntryName, new string('b', 101) }; // Short name and long link
            yield return new object[] { new string('a', 101), InitialEntryName }; // Long name and short link
            yield return new object[] { new string('a', 101), new string('b', 101) }; // Long name and long link
        }

        private GnuTarEntry CreateEntryForLongLinkLongPathChecks(string name, string linkName)
        {
            // A SymbolicLink entry can test both LongLink and LongPath entries if
            // the length of either string is longer than what fits in the header.
            return new GnuTarEntry(TarEntryType.SymbolicLink, name)
            {
                LinkName = linkName,
                ModificationTime = TestModificationTime,
                AccessTime = TestAccessTime, // This should only be set in the main entry
                ChangeTime = TestChangeTime, // This should only be set in the main entry
                Uid = TestUid, // This should only be set in the main entry
                Gid = TestGid, // This should only be set in the main entry
                Mode = TestMode, // This should only be set in the main entry
                UserName = TestUName, // This should only be set in the main entry
                GroupName = TestGName // This should only be set in the main entry
            };
        }

        private void ValidateEntryForRegularEntryInLongLinkAndLongPathChecks(GnuTarEntry entry, string name, string linkName)
        {
            Assert.NotNull(entry);
            Assert.Equal(TestModificationTime, entry.ModificationTime);
            Assert.Equal(TestAccessTime, entry.AccessTime); // This should be different in the long entry header
            Assert.Equal(TestChangeTime, entry.ChangeTime); // This should be different in the long entry header
            Assert.Equal(name, entry.Name);
            Assert.Equal(linkName, entry.LinkName);
            Assert.Equal(TestUid, entry.Uid); // This should be different in the long entry header
            Assert.Equal(TestGid, entry.Gid); // This should be different in the long entry header
            Assert.Equal(TestMode, entry.Mode); // This should be different in the long entry header
            Assert.Equal(TestUName, entry.UserName); // This should be different in the long entry header
            Assert.Equal(TestGName, entry.GroupName); // This should be different in the long entry header
            Assert.Equal(0, entry.Length); // No data in the main entry
        }

        [Theory]
        [MemberData(nameof(NameAndLink_TestData))]
        public void Check_LongLink_AndLongPath_Metadata(string name, string linkName)
        {
            // The GNU format sets the mtime, atime and ctime to nulls in headers when they are set to the unix epoch.
            // Also the uid and gid should be '0' in the long entries headers.
            // Also the uname and gname in the long entry headers should be set to those of the main entry.

            using MemoryStream ms = new();

            using (TarWriter writer = new(ms, TarEntryFormat.Gnu, leaveOpen: true))
            {
                GnuTarEntry entry = CreateEntryForLongLinkLongPathChecks(name, linkName);
                writer.WriteEntry(entry);
            }
            ms.Position = 0;

            using (TarReader reader = new(ms, leaveOpen: true))
            {
                GnuTarEntry entry = reader.GetNextEntry() as GnuTarEntry;
                ValidateEntryForRegularEntryInLongLinkAndLongPathChecks(entry, name, linkName);
            }

            ValidateLongEntryBytes(ms, name, linkName);
        }

        [Theory]
        [MemberData(nameof(NameAndLink_TestData))]
        public async Task Check_LongLink_AndLongPath_Metadata_Async(string name, string linkName)
        {
            // The GNU format sets the mtime, atime and ctime to nulls in headers when they are set to MinValue
            // Also the uid and gid should be '0' in the long entries headers, and uname and gname in the long entry headers should be set to root.

            await using MemoryStream ms = new();

            await using (TarWriter writer = new(ms, TarEntryFormat.Gnu, leaveOpen: true))
            {
                GnuTarEntry entry = CreateEntryForLongLinkLongPathChecks(name, linkName);
                await writer.WriteEntryAsync(entry);
            }
            ms.Position = 0;

            await using (TarReader reader = new(ms, leaveOpen: true))
            {
                GnuTarEntry entry = await reader.GetNextEntryAsync() as GnuTarEntry;
                ValidateEntryForRegularEntryInLongLinkAndLongPathChecks(entry, name, linkName);
            }

            ValidateLongEntryBytes(ms, name, linkName);
        }

        private void ValidateLongEntryBytes(MemoryStream ms, string name, string linkName)
        {
            bool isLongPath = name.Length >= 100;
            bool isLongLink = linkName.Length >= 100;

            ms.Position = 0;

            long nextEntryStart = 0;
            long reportedSize;

            if (isLongLink)
            {
                reportedSize = CheckHeaderMetadataAndGetReportedSize(ms, nextEntryStart, isLongLinkOrLongPath: true);
                CheckDataContainsExpectedString(ms, nextEntryStart + 512, reportedSize, linkName, shouldTrim: false); // Skip to the data section
                nextEntryStart += 512 + 512; // Skip the current header and the long link entry
                Assert.True(linkName.Length < 512, "Do not test paths longer than a 512 byte block");
            }

            if (isLongPath)
            {
                reportedSize = CheckHeaderMetadataAndGetReportedSize(ms, nextEntryStart, isLongLinkOrLongPath: true);
                CheckDataContainsExpectedString(ms, nextEntryStart + 512, reportedSize, name, shouldTrim: false); // Skip to the data section
                nextEntryStart += 512 + 512; // Skip the current header and the long path entry
                Assert.True(name.Length < 512, "Do not test paths longer than a 512 byte block");
            }

            CheckHeaderMetadataAndGetReportedSize(ms, nextEntryStart, isLongLinkOrLongPath: false);
        }

        private long CheckHeaderMetadataAndGetReportedSize(MemoryStream ms, long nextEntryStart, bool isLongLinkOrLongPath)
        {
            // internally, mtime, atime and ctime should be nulls
            // and if the entry is a long path or long link, the entry's data length should be
            // equal to the string plus a null character

            // name mode uid gid size mtime checksum typeflag linkname magic uname gname devmajor devminor atime ctime
            // 100  8    8   8   12   12    8        1        100      8     32    32    8        8        12    12
            long nameStart = nextEntryStart;
            long modeStart = nameStart + 100;
            long uidStart = modeStart + 8;
            long gidStart = uidStart + 8;
            long sizeStart = gidStart + 8;
            long mTimeStart = sizeStart + 12;
            long checksumStart = mTimeStart + 12;
            long typeflagStart = checksumStart + 8;
            long linkNameStart = typeflagStart + 1;
            long magicStart = linkNameStart + 100;
            long uNameStart = magicStart + 8;
            long gNameStart = uNameStart + 32;
            long devMajorStart = gNameStart + 32;
            long devMinorStart = devMajorStart + 8;
            long aTimeStart = devMinorStart + 8;
            long cTimeStart = aTimeStart + 12;

            Span<byte> buffer = stackalloc byte[12]; // size, mtime, atime, ctime all are 12 bytes in length (max length to check)

            if (isLongLinkOrLongPath)
            {
                CheckBytesAreNulls(ms, buffer, aTimeStart); // no atime
                CheckBytesAreNulls(ms, buffer, cTimeStart); // no ctime
                CheckBytesAreZeros(ms, buffer.Slice(0, 8), uidStart); // uid 0
                CheckBytesAreZeros(ms, buffer.Slice(0, 8), gidStart); // uid 0
                Span<byte> expectedOctalModeBytes = Encoding.ASCII.GetBytes("0000644\0"); // 644 is the default mode set in LongLink/LongPath
                CheckBytesAreSpecificSequence(ms, buffer.Slice(0, 8), modeStart, expectedOctalModeBytes);
                CheckDataContainsExpectedString(ms, uNameStart, 32, RootUNameGName, shouldTrim: true);
                CheckDataContainsExpectedString(ms, gNameStart, 32, RootUNameGName, shouldTrim: true);
                CheckBytesAreZeros(ms, buffer, mTimeStart);
            }
            else
            {
                Span<byte> expectedOctalUidBytes = Encoding.ASCII.GetBytes(Convert.ToString(TestUid, 8).PadLeft(7, '0') + '\0');
                Span<byte> expectedOctalGidBytes = Encoding.ASCII.GetBytes(Convert.ToString(TestGid, 8).PadLeft(7, '0') + '\0');
                Span<byte> expectedOctalModeBytes = Encoding.ASCII.GetBytes(Convert.ToString((int)TestMode, 8).PadLeft(7, '0') + '\0');
                CheckBytesAreSpecificSequence(ms, buffer.Slice(0, 8), uidStart, expectedOctalUidBytes);
                CheckBytesAreSpecificSequence(ms, buffer.Slice(0, 8), gidStart, expectedOctalGidBytes);
                CheckBytesAreSpecificSequence(ms, buffer.Slice(0, 8), modeStart, expectedOctalModeBytes);
                CheckDataContainsExpectedString(ms, uNameStart, 32, TestUName, shouldTrim: true);
                CheckDataContainsExpectedString(ms, gNameStart, 32, TestGName, shouldTrim: true);
            }

            ms.Seek(sizeStart, SeekOrigin.Begin);
            ms.Read(buffer);
            return ParseNumeric<long>(buffer);
        }

        private void CheckBytesAreSpecificChar(MemoryStream ms, Span<byte> buffer, long dataStart, byte charToCheck)
        {
            ms.Seek(dataStart, SeekOrigin.Begin);
            ms.Read(buffer);
            Span<byte> expectedSequence = stackalloc byte[buffer.Length - 1];
            expectedSequence.Fill(charToCheck);
            AssertExtensions.SequenceEqual(expectedSequence, buffer.Slice(0, buffer.Length - 1));
            Assert.Equal(0, buffer[^1]); // The last byte should be a null character
        }

        private void CheckBytesAreSpecificSequence(MemoryStream ms, Span<byte> buffer, long dataStart, ReadOnlySpan<byte> expectedSequence)
        {
            ms.Seek(dataStart, SeekOrigin.Begin);
            ms.Read(buffer);
            Assert.Equal(expectedSequence.Length, buffer.Length);
            AssertExtensions.SequenceEqual(expectedSequence, buffer.Slice(0, expectedSequence.Length));
            Assert.Equal(0, buffer[expectedSequence.Length - 1]); // The last byte should be a null character
        }

        private void CheckBytesAreNulls(MemoryStream ms, Span<byte> buffer, long dataStart) => CheckBytesAreSpecificChar(ms, buffer, dataStart, 0); // null char

        private void CheckBytesAreZeros(MemoryStream ms, Span<byte> buffer, long dataStart) => CheckBytesAreSpecificChar(ms, buffer, dataStart, 0x30); // '0' char

        private void CheckDataContainsExpectedString(MemoryStream ms, long dataStart, long actualDataLength, string expectedString, bool shouldTrim)
        {
            ms.Seek(dataStart, SeekOrigin.Begin);
            byte[] buffer = new byte[actualDataLength];
            ms.Read(buffer);

            if (shouldTrim)
            {
                string actualString = Encoding.ASCII.GetString(TrimEndingNullsAndSpaces(buffer));
                Assert.Equal(expectedString, actualString);
            }
            else
            {
                string actualString = Encoding.ASCII.GetString(buffer);
                Assert.Equal(expectedString, actualString[..^1]); // The last byte should be a null character
            }
        }

        private static T ParseNumeric<T>(ReadOnlySpan<byte> buffer) where T : struct, INumber<T>, IBinaryInteger<T>
        {
            byte leadingByte = buffer[0];
            if (leadingByte == 0xff)
            {
                return T.ReadBigEndian(buffer, isUnsigned: false);
            }
            else if (leadingByte == 0x80)
            {
                return T.ReadBigEndian(buffer.Slice(1), isUnsigned: true);
            }
            else
            {
                return ParseOctal<T>(buffer);
            }
        }

        private static T ParseOctal<T>(ReadOnlySpan<byte> buffer) where T : struct, INumber<T>
        {
            buffer = TrimEndingNullsAndSpaces(buffer);
            buffer = TrimLeadingNullsAndSpaces(buffer);

            if (buffer.Length == 0)
            {
                return T.Zero;
            }

            T octalFactor = T.CreateTruncating(8u);
            T value = T.Zero;
            foreach (byte b in buffer)
            {
                uint digit = (uint)(b - '0');
                if (digit >= 8)
                {
                    throw new InvalidDataException(SR.Format(SR.TarInvalidNumber));
                }

                value = checked((value * octalFactor) + T.CreateTruncating(digit));
            }

            return value;
        }

        private static ReadOnlySpan<byte> TrimEndingNullsAndSpaces(ReadOnlySpan<byte> buffer)
        {
            int trimmedLength = buffer.Length;
            while (trimmedLength > 0 && buffer[trimmedLength - 1] is 0 or 32)
            {
                trimmedLength--;
            }

            return buffer.Slice(0, trimmedLength);
        }

        private static ReadOnlySpan<byte> TrimLeadingNullsAndSpaces(ReadOnlySpan<byte> buffer)
        {
            int newStart = 0;
            while (newStart < buffer.Length && buffer[newStart] is 0 or 32)
            {
                newStart++;
            }

            return buffer.Slice(newStart);
        }
    }
}
