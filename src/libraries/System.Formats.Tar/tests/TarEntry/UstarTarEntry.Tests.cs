// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace System.Formats.Tar.Tests
{
    public class UstarTarEntry_Tests : TarTestsBase
    {
        [Fact]
        public void Constructor_InvalidEntryName()
        {
            Assert.Throws<ArgumentNullException>(() => new UstarTarEntry(TarEntryType.RegularFile, entryName: null));
            Assert.Throws<ArgumentException>(() => new UstarTarEntry(TarEntryType.RegularFile, entryName: string.Empty));
        }

        [Fact]
        public void Constructor_UnsupportedEntryTypes()
        {
            Assert.Throws<ArgumentException>(() => new UstarTarEntry((TarEntryType)byte.MaxValue, InitialEntryName));

            Assert.Throws<ArgumentException>(() => new UstarTarEntry(TarEntryType.ContiguousFile, InitialEntryName));
            Assert.Throws<ArgumentException>(() => new UstarTarEntry(TarEntryType.DirectoryList, InitialEntryName));
            Assert.Throws<ArgumentException>(() => new UstarTarEntry(TarEntryType.ExtendedAttributes, InitialEntryName));
            Assert.Throws<ArgumentException>(() => new UstarTarEntry(TarEntryType.GlobalExtendedAttributes, InitialEntryName));
            Assert.Throws<ArgumentException>(() => new UstarTarEntry(TarEntryType.LongLink, InitialEntryName));
            Assert.Throws<ArgumentException>(() => new UstarTarEntry(TarEntryType.LongPath, InitialEntryName));
            Assert.Throws<ArgumentException>(() => new UstarTarEntry(TarEntryType.MultiVolume, InitialEntryName));
            Assert.Throws<ArgumentException>(() => new UstarTarEntry(TarEntryType.V7RegularFile, InitialEntryName));
            Assert.Throws<ArgumentException>(() => new UstarTarEntry(TarEntryType.RenamedOrSymlinked, InitialEntryName));
            Assert.Throws<ArgumentException>(() => new UstarTarEntry(TarEntryType.SparseFile, InitialEntryName));
            Assert.Throws<ArgumentException>(() => new UstarTarEntry(TarEntryType.TapeVolume, InitialEntryName));
        }

        [Fact]
        public void SupportedEntryType_RegularFile()
        {
            UstarTarEntry regularFile = new UstarTarEntry(TarEntryType.RegularFile, InitialEntryName);
            SetRegularFile(regularFile);
            VerifyRegularFile(regularFile, isWritable: true);
        }

        [Fact]
        public void SupportedEntryType_Directory()
        {
            UstarTarEntry directory = new UstarTarEntry(TarEntryType.Directory, InitialEntryName);
            SetDirectory(directory);
            VerifyDirectory(directory);
        }

        [Fact]
        public void SupportedEntryType_HardLink()
        {
            UstarTarEntry hardLink = new UstarTarEntry(TarEntryType.HardLink, InitialEntryName);
            SetHardLink(hardLink);
            VerifyHardLink(hardLink);
        }

        [Fact]
        public void SupportedEntryType_SymbolicLink()
        {
            UstarTarEntry symbolicLink = new UstarTarEntry(TarEntryType.SymbolicLink, InitialEntryName);
            SetSymbolicLink(symbolicLink);
            VerifySymbolicLink(symbolicLink);
        }

        [Fact]
        public void SupportedEntryType_BlockDevice()
        {
            UstarTarEntry blockDevice = new UstarTarEntry(TarEntryType.BlockDevice, InitialEntryName);
            SetBlockDevice(blockDevice);
            VerifyBlockDevice(blockDevice);
        }

        [Fact]
        public void SupportedEntryType_CharacterDevice()
        {
            UstarTarEntry characterDevice = new UstarTarEntry(TarEntryType.CharacterDevice, InitialEntryName);
            SetCharacterDevice(characterDevice);
            VerifyCharacterDevice(characterDevice);
        }

        [Fact]
        public void SupportedEntryType_Fifo()
        {
            UstarTarEntry fifo = new UstarTarEntry(TarEntryType.Fifo, InitialEntryName);
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
                UstarTarEntry entry = new UstarTarEntry(TarEntryType.RegularFile, InitialEntryName);
                Assert.Equal(-1, entry.DataOffset);
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
            // Ustar header length is 512, data starts in the next position
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
                UstarTarEntry entry = new UstarTarEntry(TarEntryType.RegularFile, InitialEntryName);
                Assert.Equal(-1, entry.DataOffset);
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
            // Ustar header length is 512, data starts in the next position
            long expectedDataOffset = canSeek ? 512 : -1;
            Assert.Equal(expectedDataOffset, actualEntry.DataOffset);

            if (canSeek)
            {
                ms.Position = actualEntry.DataOffset;
                byte actualData = (byte)ms.ReadByte();
                Assert.Equal(ExpectedOffsetDataSingleByte, actualData);
            }
        }

        [Fact]
        public void DataOffset_BeforeAndAfterArchive()
        {
            UstarTarEntry entry = new UstarTarEntry(TarEntryType.RegularFile, InitialEntryName);
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
            UstarTarEntry entry = new UstarTarEntry(TarEntryType.RegularFile, InitialEntryName);
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
                UstarTarEntry entry = new UstarTarEntry(TarEntryType.RegularFile, InitialEntryName);
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
            // Ustar header length is 512, data starts in the next position
            Assert.Equal(512, actualEntry.DataOffset);
        }

        [Fact]
        public async Task DataOffset_UnseekableDataStream_Async()
        {
            await using MemoryStream ms = new();
            await using (TarWriter writer = new(ms, leaveOpen: true))
            {
                UstarTarEntry entry = new UstarTarEntry(TarEntryType.RegularFile, InitialEntryName);
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
            // Ustar header length is 512, data starts in the next position
            Assert.Equal(512, actualEntry.DataOffset);
        }
        
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void DataOffset_RegularFile_SecondEntry_MultiByte(bool canSeek)
        {
            using MemoryStream ms = new();
            using (TarWriter writer = new(ms, leaveOpen: true))
            {
                UstarTarEntry entry1 = new UstarTarEntry(TarEntryType.RegularFile, InitialEntryName);
                Assert.Equal(-1, entry1.DataOffset);
                entry1.DataStream = new MemoryStream();
                entry1.DataStream.Write(ExpectedOffsetDataMultiByte);
                entry1.DataStream.Position = 0;
                writer.WriteEntry(entry1);
                
                UstarTarEntry entry2 = new UstarTarEntry(TarEntryType.RegularFile, InitialEntryName);
                Assert.Equal(-1, entry2.DataOffset);
                entry2.DataStream = new MemoryStream();
                entry2.DataStream.Write(ExpectedOffsetDataMultiByte);
                entry2.DataStream.Position = 0;
                writer.WriteEntry(entry2);
            }
            ms.Position = 0;

            using Stream streamToRead = new WrappedStream(ms, canWrite: true, canRead: true, canSeek: canSeek);
            using TarReader reader = new(streamToRead);
            TarEntry firstEntry = reader.GetNextEntry();
            Assert.NotNull(firstEntry);
            // Ustar header length is 512, data starts in the next position
            long firstExpectedDataOffset = canSeek ? 512 : -1;
            Assert.Equal(firstExpectedDataOffset, firstEntry.DataOffset);
            
            if (canSeek)
            {
                byte[] actualData = new byte[ExpectedOffsetDataMultiByte.Length];
                ms.Position = firstEntry.DataOffset; // Reposition the archive stream to confirm the reader will autorestore its position later
                ms.ReadExactly(actualData);
                AssertExtensions.SequenceEqual(ExpectedOffsetDataMultiByte, actualData);
            }

            // If the archive stream is seekable, this should autorestore archive stream position internally
            TarEntry secondEntry = reader.GetNextEntry();
            Assert.NotNull(secondEntry);
            // First entry ends at 512 (header) + 4 (data) + 508 (padding) = 1024
            // Second entry also has 512 header, so data starts one byte after 1536
            long secondExpectedDataOffset = canSeek ? 1536 : -1;
            Assert.Equal(secondExpectedDataOffset, secondEntry.DataOffset);
        }
        
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task DataOffset_RegularFile_SecondEntry_MultiByte_Async(bool canSeek)
        {
            await using MemoryStream ms = new();
            await using (TarWriter writer = new(ms, leaveOpen: true))
            {
                UstarTarEntry entry1 = new UstarTarEntry(TarEntryType.RegularFile, InitialEntryName);
                Assert.Equal(-1, entry1.DataOffset);
                entry1.DataStream = new MemoryStream();
                entry1.DataStream.Write(ExpectedOffsetDataMultiByte);
                entry1.DataStream.Position = 0;
                await writer.WriteEntryAsync(entry1);
                
                UstarTarEntry entry2 = new UstarTarEntry(TarEntryType.RegularFile, InitialEntryName);
                Assert.Equal(-1, entry2.DataOffset);
                entry2.DataStream = new MemoryStream();
                entry2.DataStream.Write(ExpectedOffsetDataMultiByte);
                entry2.DataStream.Position = 0;
                await writer.WriteEntryAsync(entry2);
            }
            ms.Position = 0;

            await using Stream streamToRead = new WrappedStream(ms, canWrite: true, canRead: true, canSeek: canSeek);
            await using TarReader reader = new(streamToRead);
            TarEntry firstEntry = await reader.GetNextEntryAsync();
            Assert.NotNull(firstEntry);
            // Ustar header length is 512, data starts in the next position
            long firstExpectedDataOffset = canSeek ? 512 : -1;
            Assert.Equal(firstExpectedDataOffset, firstEntry.DataOffset);

            if (canSeek)
            {
                byte[] actualData = new byte[ExpectedOffsetDataMultiByte.Length];
                ms.Position = firstEntry.DataOffset; // Reposition the archive stream to confirm the reader will autorestore its position later
                await ms.ReadExactlyAsync(actualData);
                AssertExtensions.SequenceEqual(ExpectedOffsetDataMultiByte, actualData);
            }

            // If the archive stream is seekable, this should autorestore archive stream position internally
            TarEntry secondEntry = await reader.GetNextEntryAsync();
            Assert.NotNull(secondEntry);
            // First entry ends at 512 (header) + 4 (data) + 508 (padding) = 1024
            // Second entry also has 512 header, so data starts one byte after 1536
            long secondExpectedDataOffset = canSeek ? 1536 : -1;
            Assert.Equal(secondExpectedDataOffset, secondEntry.DataOffset);
        }
    }
}
