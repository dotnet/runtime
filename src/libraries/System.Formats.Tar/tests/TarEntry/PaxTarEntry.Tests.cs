// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace System.Formats.Tar.Tests
{
    public class PaxTarEntry_Tests : TarTestsBase
    {
        [Fact]
        public void Constructor_InvalidEntryName()
        {
            Assert.Throws<ArgumentNullException>(() => new PaxTarEntry(TarEntryType.RegularFile, entryName: null));
            Assert.Throws<ArgumentException>(() => new PaxTarEntry(TarEntryType.RegularFile, entryName: string.Empty));
        }

        [Fact]
        public void Constructor_UnsupportedEntryTypes()
        {
            Assert.Throws<ArgumentException>(() => new PaxTarEntry((TarEntryType)byte.MaxValue, InitialEntryName));

            Assert.Throws<ArgumentException>(() => new PaxTarEntry(TarEntryType.ContiguousFile, InitialEntryName));
            Assert.Throws<ArgumentException>(() => new PaxTarEntry(TarEntryType.DirectoryList, InitialEntryName));
            Assert.Throws<ArgumentException>(() => new PaxTarEntry(TarEntryType.LongLink, InitialEntryName));
            Assert.Throws<ArgumentException>(() => new PaxTarEntry(TarEntryType.LongPath, InitialEntryName));
            Assert.Throws<ArgumentException>(() => new PaxTarEntry(TarEntryType.MultiVolume, InitialEntryName));
            Assert.Throws<ArgumentException>(() => new PaxTarEntry(TarEntryType.V7RegularFile, InitialEntryName));
            Assert.Throws<ArgumentException>(() => new PaxTarEntry(TarEntryType.RenamedOrSymlinked, InitialEntryName));
            Assert.Throws<ArgumentException>(() => new PaxTarEntry(TarEntryType.SparseFile, InitialEntryName));
            Assert.Throws<ArgumentException>(() => new PaxTarEntry(TarEntryType.TapeVolume, InitialEntryName));

            // The user should not be creating these entries manually in pax
            Assert.Throws<ArgumentException>(() => new PaxTarEntry(TarEntryType.ExtendedAttributes, InitialEntryName));
            Assert.Throws<ArgumentException>(() => new PaxTarEntry(TarEntryType.GlobalExtendedAttributes, InitialEntryName));
        }


        [Theory]
        [InlineData("\n", "value")]
        [InlineData("=", "value")]
        [InlineData("key", "\n")]
        [InlineData("\nkey", "value")]
        [InlineData("k\ney", "value")]
        [InlineData("key\n", "value")]
        [InlineData("=key", "value")]
        [InlineData("ke=y", "value")]
        [InlineData("key=", "value")]
        [InlineData("key", "\nvalue")]
        [InlineData("key", "val\nue")]
        [InlineData("key", "value\n")]
        [InlineData("key=", "value\n")]
        [InlineData("key\n", "value\n")]
        public void Disallowed_ExtendedAttributes_SeparatorCharacters(string key, string value)
        {
            Dictionary<string, string> extendedAttribute = new Dictionary<string, string>() { { key, value } };

            Assert.Throws<ArgumentException>(() => new PaxTarEntry(TarEntryType.RegularFile, InitialEntryName, extendedAttribute));
            Assert.Throws<ArgumentException>(() => new PaxGlobalExtendedAttributesTarEntry(extendedAttribute));
        }

        [Fact]
        public void SupportedEntryType_RegularFile()
        {
            PaxTarEntry regularFile = new PaxTarEntry(TarEntryType.RegularFile, InitialEntryName);
            SetRegularFile(regularFile);
            VerifyRegularFile(regularFile, isWritable: true);
        }

        [Fact]
        public void SupportedEntryType_Directory()
        {
            PaxTarEntry directory = new PaxTarEntry(TarEntryType.Directory, InitialEntryName);
            SetDirectory(directory);
            VerifyDirectory(directory);
        }

        [Fact]
        public void SupportedEntryType_HardLink()
        {
            PaxTarEntry hardLink = new PaxTarEntry(TarEntryType.HardLink, InitialEntryName);
            SetHardLink(hardLink);
            VerifyHardLink(hardLink);
        }

        [Fact]
        public void SupportedEntryType_SymbolicLink()
        {
            PaxTarEntry symbolicLink = new PaxTarEntry(TarEntryType.SymbolicLink, InitialEntryName);
            SetSymbolicLink(symbolicLink);
            VerifySymbolicLink(symbolicLink);
        }

        [Fact]
        public void SupportedEntryType_BlockDevice()
        {
            PaxTarEntry blockDevice = new PaxTarEntry(TarEntryType.BlockDevice, InitialEntryName);
            SetBlockDevice(blockDevice);
            VerifyBlockDevice(blockDevice);
        }

        [Fact]
        public void SupportedEntryType_CharacterDevice()
        {
            PaxTarEntry characterDevice = new PaxTarEntry(TarEntryType.CharacterDevice, InitialEntryName);
            SetCharacterDevice(characterDevice);
            VerifyCharacterDevice(characterDevice);
        }

        [Fact]
        public void SupportedEntryType_Fifo()
        {
            PaxTarEntry fifo = new PaxTarEntry(TarEntryType.Fifo, InitialEntryName);
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
                PaxTarEntry entry = new PaxTarEntry(TarEntryType.RegularFile, InitialEntryName);
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
            // PAX first writes the extended attributes entry, containing:
            // * 512 bytes of the regular tar header
            // * 113 bytes of the default extended attributes in the data section (mdata)
            // * 399 bytes of padding after the data
            // Then it writes the actual regular file entry, containing:
            // * 512 bytes of the regular tar header
            // Totalling 1536.
            // The regular file data section starts on the next byte.
            long expectedDataOffset = canSeek ? 1536 : -1;
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
                PaxTarEntry entry = new PaxTarEntry(TarEntryType.RegularFile, InitialEntryName);
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
            // PAX first writes the extended attributes entry, containing:
            // * 512 bytes of the regular tar header
            // * 113 bytes of the default extended attributes in the data section (mdata)
            // * 399 bytes of padding after the data
            // Then it writes the actual regular file entry, containing:
            // * 512 bytes of the regular tar header
            // Totalling 1536.
            // The regular file data section starts on the next byte.
            long expectedDataOffset = canSeek ? 1536 : -1;
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
        public void DataOffset_GlobalExtendedAttributes(bool canSeek)
        {
            using MemoryStream ms = new();
            using (TarWriter writer = new(ms, leaveOpen: true))
            {
                PaxGlobalExtendedAttributesTarEntry entry =  new PaxGlobalExtendedAttributesTarEntry(new Dictionary<string, string>());
                Assert.Equal(-1, entry.DataOffset);
                writer.WriteEntry(entry);
            }
            ms.Position = 0;

            using Stream streamToRead = new WrappedStream(ms, canWrite: true, canRead: true, canSeek: canSeek);
            using TarReader reader = new(streamToRead);
            TarEntry actualEntry = reader.GetNextEntry();
            Assert.NotNull(actualEntry);
            Assert.Equal(TarEntryType.GlobalExtendedAttributes, actualEntry.EntryType);
            // The PAX global extended attributes header length is 512, data starts in the next position
            long expectedDataOffset = canSeek ? 512 : -1;
            Assert.Equal(expectedDataOffset, actualEntry.DataOffset);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task DataOffset_GlobalExtendedAttributes_Async(bool canSeek)
        {
            await using MemoryStream ms = new();
            await using (TarWriter writer = new(ms, leaveOpen: true))
            {
                PaxGlobalExtendedAttributesTarEntry entry =  new PaxGlobalExtendedAttributesTarEntry(new Dictionary<string, string>());
                Assert.Equal(-1, entry.DataOffset);
                await writer.WriteEntryAsync(entry);
            }
            ms.Position = 0;

            await using Stream streamToRead = new WrappedStream(ms, canWrite: true, canRead: true, canSeek: canSeek);
            await using TarReader reader = new(streamToRead);
            TarEntry actualEntry = await reader.GetNextEntryAsync();
            Assert.NotNull(actualEntry);
            Assert.Equal(TarEntryType.GlobalExtendedAttributes, actualEntry.EntryType);
            // The PAX global extended attributes header length is 512, data starts in the next position
            long expectedDataOffset = canSeek ? 512 : -1;
            Assert.Equal(expectedDataOffset, actualEntry.DataOffset);
        }
        
        [Fact]
        public void DataOffset_BeforeAndAfterArchive()
        {
            PaxTarEntry entry = new PaxTarEntry(TarEntryType.RegularFile, InitialEntryName);
            Assert.Equal(-1, entry.DataOffset);

            entry.DataStream = new MemoryStream();
            entry.DataStream.WriteByte(ExpectedOffsetDataSingleByte);
            entry.DataStream.Position = 0; // The data stream is written to the archive from the current position

            using MemoryStream ms = new();
            using TarWriter writer = new(ms);
            writer.WriteEntry(entry);
            // PAX first writes the extended attributes entry, containing:
            // * 512 bytes of the regular tar header
            // * 113 bytes of the default extended attributes in the data section (mdata)
            // * 399 bytes of padding after the data
            // Then it writes the actual regular file entry, containing:
            // * 512 bytes of the regular tar header
            // Totalling 1536.
            // The regular file data section starts on the next byte.
            Assert.Equal(1536, entry.DataOffset);

            // Write it again, the offset should now point to the second written entry
            // First entry 1536 + 1 (data) + 511 (padding) = 2048
            // Second entry 1536
            // 2048 + 1536 = 3584
            writer.WriteEntry(entry);
            Assert.Equal(3584, entry.DataOffset);
        }
        
        [Fact]
        public async Task DataOffset_BeforeAndAfterArchive_Async()
        {
            PaxTarEntry entry = new PaxTarEntry(TarEntryType.RegularFile, InitialEntryName);
            Assert.Equal(-1, entry.DataOffset);

            entry.DataStream = new MemoryStream();
            entry.DataStream.WriteByte(ExpectedOffsetDataSingleByte);
            entry.DataStream.Position = 0; // The data stream is written to the archive from the current position

            await using MemoryStream ms = new();
            await using TarWriter writer = new(ms);
            await writer.WriteEntryAsync(entry);
            // PAX first writes the extended attributes entry, containing:
            // * 512 bytes of the regular tar header
            // * 113 bytes of the default extended attributes in the data section (mdata)
            // * 399 bytes of padding after the data
            // Then it writes the actual regular file entry, containing:
            // * 512 bytes of the regular tar header
            // Totalling 1536.
            // The regular file data section starts on the next byte.
            Assert.Equal(1536, entry.DataOffset);

            // Write it again, the offset should now point to the second written entry
            // First entry 1536 + 1 (data) + 511 (padding) = 2048
            // Second entry 1536
            // 2048 + 1536 = 3584
            await writer.WriteEntryAsync(entry);
            Assert.Equal(3584, entry.DataOffset);
        }

        [Fact]
        public void DataOffset_UnseekableDataStream()
        {
            using MemoryStream ms = new();
            using (TarWriter writer = new(ms, leaveOpen: true))
            {
                PaxTarEntry entry = new PaxTarEntry(TarEntryType.RegularFile, InitialEntryName);
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
            // PAX first writes the extended attributes entry, containing:
            // * 512 bytes of the regular tar header
            // * 113 bytes of the default extended attributes in the data section (mdata)
            // * 399 bytes of padding after the data
            // Then it writes the actual regular file entry, containing:
            // * 512 bytes of the regular tar header
            // Totalling 1536.
            // The regular file data section starts on the next byte.
            Assert.Equal(1536, actualEntry.DataOffset);
        }

        [Fact]
        public async Task DataOffset_UnseekableDataStream_Async()
        {
            await using MemoryStream ms = new();
            await using (TarWriter writer = new(ms, leaveOpen: true))
            {
                PaxTarEntry entry = new PaxTarEntry(TarEntryType.RegularFile, InitialEntryName);
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
            // PAX first writes the extended attributes entry, containing:
            // * 512 bytes of the regular tar header
            // * 113 bytes of the default extended attributes in the data section (mdata)
            // * 399 bytes of padding after the data
            // Then it writes the actual regular file entry, containing:
            // * 512 bytes of the regular tar header
            // Totalling 1536.
            // The regular file data section starts on the next byte.
            Assert.Equal(1536, actualEntry.DataOffset);
        }
        
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void DataOffset_RegularFile_SecondEntry_MultiByte(bool canSeek)
        {
            using MemoryStream ms = new();
            using (TarWriter writer = new(ms, leaveOpen: true))
            {
                PaxTarEntry entry1 = new PaxTarEntry(TarEntryType.RegularFile, InitialEntryName);
                Assert.Equal(-1, entry1.DataOffset);
                entry1.DataStream = new MemoryStream();
                entry1.DataStream.Write(ExpectedOffsetDataMultiByte);
                entry1.DataStream.Position = 0;
                writer.WriteEntry(entry1);
                
                PaxTarEntry entry2 = new PaxTarEntry(TarEntryType.RegularFile, InitialEntryName);
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
            // PAX first writes the extended attributes entry, containing:
            // * 512 bytes of the regular tar header
            // * 113 bytes of the default extended attributes in the data section (mdata)
            // * 399 bytes of padding after the data
            // Then it writes the actual regular file entry, containing:
            // * 512 bytes of the regular tar header
            // Totalling 1536.
            // The regular file data section starts on the next byte.
            long firstExpectedDataOffset = canSeek ? 1536 : -1;
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
            // The first entry (including its extended attribute entry) end at 1536 + 4 (data) + 508 (padding) = 2048
            // Second entry's data also starts one byte after the 1536 bytes after the beginning of its header, so 2048 + 1536 = 3584
            long secondExpectedDataOffset = canSeek ? 3584 : -1;
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
                PaxTarEntry entry1 = new PaxTarEntry(TarEntryType.RegularFile, InitialEntryName);
                Assert.Equal(-1, entry1.DataOffset);
                entry1.DataStream = new MemoryStream();
                entry1.DataStream.Write(ExpectedOffsetDataMultiByte);
                entry1.DataStream.Position = 0;
                await writer.WriteEntryAsync(entry1);
                
                PaxTarEntry entry2 = new PaxTarEntry(TarEntryType.RegularFile, InitialEntryName);
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
            // PAX first writes the extended attributes entry, containing:
            // * 512 bytes of the regular tar header
            // * 113 bytes of the default extended attributes in the data section (mdata)
            // * 399 bytes of padding after the data
            // Then it writes the actual regular file entry, containing:
            // * 512 bytes of the regular tar header
            // Totalling 1536.
            // The regular file data section starts on the next byte.
            long firstExpectedDataOffset = canSeek ? 1536 : -1;
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
            // The first entry (including its extended attribute entry) end at 1536 + 4 (data) + 508 (padding) = 2048
            // Second entry's data also starts one byte after the 1536 bytes after the beginning of its header, so 2048 + 1536 = 3584
            long secondExpectedDataOffset = canSeek ? 3584 : -1;
            Assert.Equal(secondExpectedDataOffset, secondEntry.DataOffset);
        }
    }
}
