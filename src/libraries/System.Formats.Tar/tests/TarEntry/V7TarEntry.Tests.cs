// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace System.Formats.Tar.Tests
{
    public class V7TarEntry_Tests : TarTestsBase
    {
        [Fact]
        public void Constructor_InvalidEntryName()
        {
            Assert.Throws<ArgumentNullException>(() => new V7TarEntry(TarEntryType.V7RegularFile, entryName: null));
            Assert.Throws<ArgumentException>(() => new V7TarEntry(TarEntryType.V7RegularFile, entryName: string.Empty));
        }

        [Fact]
        public void Constructor_UnsupportedEntryTypes()
        {
            Assert.Throws<ArgumentException>(() => new V7TarEntry((TarEntryType)byte.MaxValue, InitialEntryName));

            Assert.Throws<ArgumentException>(() => new V7TarEntry(TarEntryType.BlockDevice, InitialEntryName));
            Assert.Throws<ArgumentException>(() => new V7TarEntry(TarEntryType.CharacterDevice, InitialEntryName));
            Assert.Throws<ArgumentException>(() => new V7TarEntry(TarEntryType.ContiguousFile, InitialEntryName));
            Assert.Throws<ArgumentException>(() => new V7TarEntry(TarEntryType.DirectoryList, InitialEntryName));
            Assert.Throws<ArgumentException>(() => new V7TarEntry(TarEntryType.ExtendedAttributes, InitialEntryName));
            Assert.Throws<ArgumentException>(() => new V7TarEntry(TarEntryType.Fifo, InitialEntryName));
            Assert.Throws<ArgumentException>(() => new V7TarEntry(TarEntryType.GlobalExtendedAttributes, InitialEntryName));
            Assert.Throws<ArgumentException>(() => new V7TarEntry(TarEntryType.LongLink, InitialEntryName));
            Assert.Throws<ArgumentException>(() => new V7TarEntry(TarEntryType.LongPath, InitialEntryName));
            Assert.Throws<ArgumentException>(() => new V7TarEntry(TarEntryType.MultiVolume, InitialEntryName));
            Assert.Throws<ArgumentException>(() => new V7TarEntry(TarEntryType.RegularFile, InitialEntryName));
            Assert.Throws<ArgumentException>(() => new V7TarEntry(TarEntryType.RenamedOrSymlinked, InitialEntryName));
            Assert.Throws<ArgumentException>(() => new V7TarEntry(TarEntryType.SparseFile, InitialEntryName));
            Assert.Throws<ArgumentException>(() => new V7TarEntry(TarEntryType.TapeVolume, InitialEntryName));
        }

        [Fact]
        public void SupportedEntryType_V7RegularFile()
        {
            V7TarEntry oldRegularFile = new V7TarEntry(TarEntryType.V7RegularFile, InitialEntryName);
            SetRegularFile(oldRegularFile);
            VerifyRegularFile(oldRegularFile, isWritable: true);
        }

        [Fact]
        public void SupportedEntryType_Directory()
        {
            V7TarEntry directory = new V7TarEntry(TarEntryType.Directory, InitialEntryName);
            SetDirectory(directory);
            VerifyDirectory(directory);
        }

        [Fact]
        public void SupportedEntryType_HardLink()
        {
            V7TarEntry hardLink = new V7TarEntry(TarEntryType.HardLink, InitialEntryName);
            SetHardLink(hardLink);
            VerifyHardLink(hardLink);
        }

        [Fact]
        public void SupportedEntryType_SymbolicLink()
        {
            V7TarEntry symbolicLink = new V7TarEntry(TarEntryType.SymbolicLink, InitialEntryName);
            SetSymbolicLink(symbolicLink);
            VerifySymbolicLink(symbolicLink);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void DataOffset_RegularFile(bool canSeek)
        {
            byte expectedData = 5;
            using MemoryStream ms = new();
            using (TarWriter writer = new(ms, leaveOpen: true))
            {
                V7TarEntry entry = new V7TarEntry(TarEntryType.V7RegularFile, InitialEntryName);
                Assert.Equal(-1, entry.DataOffset);
                entry.DataStream = new MemoryStream();
                entry.DataStream.WriteByte(expectedData);
                entry.DataStream.Position = 0;
                writer.WriteEntry(entry);
            }
            ms.Position = 0;

            using Stream streamToRead = new WrappedStream(ms, canWrite: true, canRead: true, canSeek: canSeek);
            using TarReader reader = new(streamToRead);
            TarEntry actualEntry = reader.GetNextEntry();
            Assert.NotNull(actualEntry);
            // V7 header length is 512, data starts in the next position
            long expectedDataOffset = canSeek ? 512 : -1;
            Assert.Equal(expectedDataOffset, actualEntry.DataOffset);

            if (canSeek)
            {
                ms.Position = actualEntry.DataOffset;
                byte actualData = (byte)ms.ReadByte();
                Assert.Equal(expectedData, actualData);
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task DataOffset_RegularFile_Async(bool canSeek)
        {
            byte expectedData = 5;
            await using MemoryStream ms = new();
            await using (TarWriter writer = new(ms, leaveOpen: true))
            {
                V7TarEntry entry = new V7TarEntry(TarEntryType.V7RegularFile, InitialEntryName);
                Assert.Equal(-1, entry.DataOffset);
                entry.DataStream = new MemoryStream();
                entry.DataStream.WriteByte(expectedData);
                entry.DataStream.Position = 0;
                await writer.WriteEntryAsync(entry);
            }
            ms.Position = 0;

            await using Stream streamToRead = new WrappedStream(ms, canWrite: true, canRead: true, canSeek: canSeek);
            await using TarReader reader = new(streamToRead);
            TarEntry actualEntry = await reader.GetNextEntryAsync();
            Assert.NotNull(actualEntry);
            // V7 header length is 512, data starts in the next position
            long expectedDataOffset = canSeek ? 512 : -1;
            Assert.Equal(expectedDataOffset, actualEntry.DataOffset);

            if (canSeek)
            {
                ms.Position = actualEntry.DataOffset;
                byte actualData = (byte)ms.ReadByte();
                Assert.Equal(expectedData, actualData);
            }
        }

        [Fact]
        public void DataOffset_BeforeAndAfterArchive()
        {
            V7TarEntry entry = new V7TarEntry(TarEntryType.V7RegularFile, InitialEntryName);
            Assert.Equal(-1, entry.DataOffset);
            entry.DataStream = new MemoryStream();
            entry.DataStream.WriteByte(5);
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
            V7TarEntry entry = new V7TarEntry(TarEntryType.V7RegularFile, InitialEntryName);
            Assert.Equal(-1, entry.DataOffset);
            entry.DataStream = new MemoryStream();
            entry.DataStream.WriteByte(5);
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
                V7TarEntry entry = new V7TarEntry(TarEntryType.V7RegularFile, InitialEntryName);
                Assert.Equal(-1, entry.DataOffset);

                using MemoryStream dataStream = new();
                dataStream.WriteByte(5);
                dataStream.Position = 0;
                using WrappedStream wds = new(dataStream, canWrite: true, canRead: true, canSeek: false);
                entry.DataStream = wds;

                writer.WriteEntry(entry);
            }
            ms.Position = 0;

            using TarReader reader = new(ms);
            TarEntry actualEntry = reader.GetNextEntry();
            Assert.NotNull(actualEntry);
        }

        [Fact]
        public async Task DataOffset_UnseekableDataStream_Async()
        {
            await using MemoryStream ms = new();
            await using (TarWriter writer = new(ms, leaveOpen: true))
            {
                V7TarEntry entry = new V7TarEntry(TarEntryType.V7RegularFile, InitialEntryName);
                Assert.Equal(-1, entry.DataOffset);

                await using MemoryStream dataStream = new();
                dataStream.WriteByte(5);
                dataStream.Position = 0;
                await using WrappedStream wds = new(dataStream, canWrite: true, canRead: true, canSeek: false);
                entry.DataStream = wds;

                await writer.WriteEntryAsync(entry);
            }
            ms.Position = 0;

            await using TarReader reader = new(ms);
            TarEntry actualEntry = await reader.GetNextEntryAsync();
            Assert.NotNull(actualEntry);
            // V7 header length is 512, data starts in the next position
            Assert.Equal(512, actualEntry.DataOffset);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void DataOffset_RegularFile_SecondEntry(bool canSeek)
        {
            using MemoryStream ms = new();
            using (TarWriter writer = new(ms, leaveOpen: true))
            {
                V7TarEntry entry1 = new V7TarEntry(TarEntryType.V7RegularFile, InitialEntryName);
                Assert.Equal(-1, entry1.DataOffset);
                entry1.DataStream = new MemoryStream();
                entry1.DataStream.WriteByte(5);
                entry1.DataStream.Position = 0;
                writer.WriteEntry(entry1);
                
                V7TarEntry entry2 = new V7TarEntry(TarEntryType.V7RegularFile, InitialEntryName);
                Assert.Equal(-1, entry2.DataOffset);
                entry2.DataStream = new MemoryStream();
                entry2.DataStream.WriteByte(5);
                entry2.DataStream.Position = 0;
                writer.WriteEntry(entry2);
            }
            ms.Position = 0;

            using Stream streamToRead = new WrappedStream(ms, canWrite: true, canRead: true, canSeek: canSeek);
            using TarReader reader = new(streamToRead);
            TarEntry firstEntry = reader.GetNextEntry();
            Assert.NotNull(firstEntry);
            // V7 header length is 512, data starts in the next position
            long firstExpectedDataOffset = canSeek ? 512 : -1;
            Assert.Equal(firstExpectedDataOffset, firstEntry.DataOffset);
            
            TarEntry secondEntry = reader.GetNextEntry();
            Assert.NotNull(secondEntry);
            // First entry ends at 512 (header) + 1 (data) + 511 (padding) = 1024
            // Second entry also has 512 header, so data starts one byte after 1536
            long secondExpectedDataOffset = canSeek ? 1536 : -1;
            Assert.Equal(secondExpectedDataOffset, secondEntry.DataOffset);
        }
        
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task DataOffset_RegularFile_SecondEntryAsync(bool canSeek)
        {
            await using MemoryStream ms = new();
            await using (TarWriter writer = new(ms, leaveOpen: true))
            {
                V7TarEntry entry1 = new V7TarEntry(TarEntryType.V7RegularFile, InitialEntryName);
                Assert.Equal(-1, entry1.DataOffset);
                entry1.DataStream = new MemoryStream();
                entry1.DataStream.WriteByte(5);
                entry1.DataStream.Position = 0;
                await writer.WriteEntryAsync(entry1);
                
                V7TarEntry entry2 = new V7TarEntry(TarEntryType.V7RegularFile, InitialEntryName);
                Assert.Equal(-1, entry2.DataOffset);
                entry2.DataStream = new MemoryStream();
                entry2.DataStream.WriteByte(5);
                entry2.DataStream.Position = 0;
                await writer.WriteEntryAsync(entry2);
            }
            ms.Position = 0;

            await using Stream streamToRead = new WrappedStream(ms, canWrite: true, canRead: true, canSeek: canSeek);
            await using TarReader reader = new(streamToRead);
            TarEntry firstEntry = await reader.GetNextEntryAsync();
            Assert.NotNull(firstEntry);
            // V7 header length is 512, data starts in the next position
            long firstExpectedDataOffset = canSeek ? 512 : -1;
            Assert.Equal(firstExpectedDataOffset, firstEntry.DataOffset);
            
            TarEntry secondEntry = await reader.GetNextEntryAsync();
            Assert.NotNull(secondEntry);
            // First entry ends at 512 (header) + 1 (data) + 511 (padding) = 1024
            // Second entry also has 512 header, so data starts one byte after 1536
            long secondExpectedDataOffset = canSeek ? 1536 : -1;
            Assert.Equal(secondExpectedDataOffset, secondEntry.DataOffset);
        }
    }
}
