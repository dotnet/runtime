// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Linq;
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
                entry.DataStream.WriteByte(5);
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
            long expectedDataOffset = canSeek ? 513 : -1;
            Assert.Equal(expectedDataOffset, actualEntry.DataOffset);
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
                entry.DataStream.WriteByte(5);
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
            long expectedDataOffset = canSeek ? 513 : -1;
            Assert.Equal(expectedDataOffset, actualEntry.DataOffset);
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
                entry.DataStream.WriteByte(5);
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
            long expectedDataOffset = canSeek ? 2561 : -1;
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
                entry.DataStream.WriteByte(5);
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
            long expectedDataOffset = canSeek ? 2561 : -1;
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
            long expectedDataOffset = canSeek ? 2561 : -1;
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
            long expectedDataOffset = canSeek ? 2561 : -1;
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
            long expectedDataOffset = canSeek ? 4609 : -1;
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
            long expectedDataOffset = canSeek ? 4609 : -1;
            Assert.Equal(expectedDataOffset, actualEntry.DataOffset);
        }

        [Fact]
        public void DataOffset_BeforeAndAfterArchive()
        {
            GnuTarEntry entry = new GnuTarEntry(TarEntryType.RegularFile, InitialEntryName);
            Assert.Equal(-1, entry.DataOffset);
            entry.DataStream = new MemoryStream();
            entry.DataStream.WriteByte(5);

            using MemoryStream ms = new();
            using TarWriter writer = new(ms);
            writer.WriteEntry(entry);
            Assert.Equal(513, entry.DataOffset);
        }
        
        [Fact]
        public async Task DataOffset_BeforeAndAfterArchive_Async()
        {
            GnuTarEntry entry = new GnuTarEntry(TarEntryType.RegularFile, InitialEntryName);
            Assert.Equal(-1, entry.DataOffset);

            entry.DataStream = new MemoryStream();
            entry.DataStream.WriteByte(5);

            await using MemoryStream ms = new();
            await using TarWriter writer = new(ms);
            await writer.WriteEntryAsync(entry);
            Assert.Equal(513, entry.DataOffset);
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
            // Gnu header length is 512, data starts in the next position
            Assert.Equal(513, actualEntry.DataOffset);
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
            // Gnu header length is 512, data starts in the next position
            Assert.Equal(513, actualEntry.DataOffset);
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
            long firstExpectedDataOffset = canSeek ? 4609 : -1;
            Assert.Equal(firstExpectedDataOffset, firstEntry.DataOffset);
            
            TarEntry secondEntry = reader.GetNextEntry();
            Assert.NotNull(secondEntry);
            // First entry (including its long link and long path entries) end at 4608 (no padding, empty, as symlink has no data)
            // Second entry (including its long link and long path entries) data section starts at 4608 + 4608 = 9216 + 1
            long secondExpectedDataOffset = canSeek ? 9217 : -1;
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
            long firstExpectedDataOffset = canSeek ? 4609 : -1;
            Assert.Equal(firstExpectedDataOffset, firstEntry.DataOffset);
            
            TarEntry secondEntry = await reader.GetNextEntryAsync();
            Assert.NotNull(secondEntry);
            // First entry (including its long link and long path entries) end at 4608 (no padding, empty, as symlink has no data)
            // Second entry (including its long link and long path entries) data section starts at 4608 + 4608 = 9216 + 1
            long secondExpectedDataOffset = canSeek ? 9217 : -1;
            Assert.Equal(secondExpectedDataOffset, secondEntry.DataOffset);
        }
    }
}
