// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Formats.Tar.Tests
{
    public class TarReader_GetNextEntry_Tests : TarTestsBase
    {
        private const int MaxMetadataBlockSize = 1024 * 1024;

        [Fact]
        public async Task GetNextEntryAsync_Cancel()
        {
            CancellationTokenSource cs = new CancellationTokenSource();
            cs.Cancel();
            using MemoryStream archiveStream = new MemoryStream();
            await using TarReader reader = new TarReader(archiveStream, leaveOpen: false);
            await Assert.ThrowsAsync<TaskCanceledException>(async () => await reader.GetNextEntryAsync(copyData: false, cs.Token));
        }

        [Theory]
        [MemberData(nameof(Get_Boolean_Data))]
        public async Task MalformedArchive_TooSmall(bool async)
        {
            using MemoryStream malformed = new MemoryStream();
            byte[] buffer = new byte[] { 0x1 };
            malformed.Write(buffer);
            malformed.Seek(0, SeekOrigin.Begin);

            TarReader reader = await CreateTarReader(malformed, async);
            try
            {
                if (async)
                {
                    await Assert.ThrowsAsync<EndOfStreamException>(async () => await GetNextEntry(reader, async));
                }
                else
                {
                    Assert.Throws<EndOfStreamException>(() => GetNextEntry(reader, async).GetAwaiter().GetResult());
                }
            }
            finally
            {
                await DisposeTarReader(reader, async);
            }
        }

        [Theory]
        [MemberData(nameof(Get_Boolean_Data))]
        public async Task MalformedArchive_HeaderSize(bool async)
        {
            using MemoryStream malformed = new MemoryStream();
            byte[] buffer = new byte[512];
            Array.Fill<byte>(buffer, 0x1);
            malformed.Write(buffer);
            malformed.Seek(0, SeekOrigin.Begin);

            TarReader reader = await CreateTarReader(malformed, async);
            try
            {
                if (async)
                {
                    await Assert.ThrowsAsync<InvalidDataException>(async () => await GetNextEntry(reader, async));
                }
                else
                {
                    Assert.Throws<InvalidDataException>(() => GetNextEntry(reader, async).GetAwaiter().GetResult());
                }
            }
            finally
            {
                await DisposeTarReader(reader, async);
            }
        }

        [Theory]
        [MemberData(nameof(Get_Boolean_Data))]
        public async Task EmptyArchive(bool async)
        {
            using MemoryStream empty = new MemoryStream();
            TarReader reader = await CreateTarReader(empty, async);
            try
            {
                Assert.Null(await GetNextEntry(reader, async));
            }
            finally
            {
                await DisposeTarReader(reader, async);
            }
        }

        [Theory]
        [MemberData(nameof(Get_Boolean_Data))]
        public async Task LongEndMarkers_DoNotAdvanceStream(bool async)
        {
            using MemoryStream archive = new MemoryStream();
            TarWriter writer = await CreateTarWriter(archive, async, TarEntryFormat.Ustar, leaveOpen: true);
            try
            {
                UstarTarEntry entry = new UstarTarEntry(TarEntryType.Directory, "dir");
                await WriteEntry(writer, entry, async);
            }
            finally
            {
                await DisposeTarWriter(writer, async);
            }

            byte[] buffer = new byte[2048];
            Array.Fill<byte>(buffer, 0x0);
            archive.Write(buffer);
            archive.Seek(0, SeekOrigin.Begin);

            TarReader reader = await CreateTarReader(archive, async);
            try
            {
                Assert.NotNull(await GetNextEntry(reader, async));
                Assert.Null(await GetNextEntry(reader, async));
                long expectedPosition = archive.Position;
                Assert.Null(await GetNextEntry(reader, async));
                Assert.Equal(expectedPosition, archive.Position);
            }
            finally
            {
                await DisposeTarReader(reader, async);
            }
        }

        [Theory]
        [MemberData(nameof(Get_Boolean_Data))]
        public async Task GetNextEntry_CopyDataTrue_SeekableArchive(bool async)
        {
            string expectedText = "Hello world!";
            using MemoryStream archive = new MemoryStream();
            TarWriter writer = await CreateTarWriter(archive, async, TarEntryFormat.Ustar, leaveOpen: true);
            try
            {
                UstarTarEntry entry1 = new UstarTarEntry(TarEntryType.RegularFile, "file.txt");
                entry1.DataStream = new MemoryStream();
                using (StreamWriter streamWriter = new StreamWriter(entry1.DataStream, leaveOpen: true))
                {
                    streamWriter.WriteLine(expectedText);
                }
                entry1.DataStream.Seek(0, SeekOrigin.Begin);
                await WriteEntry(writer, entry1, async);

                UstarTarEntry entry2 = new UstarTarEntry(TarEntryType.Directory, "dir");
                await WriteEntry(writer, entry2, async);
            }
            finally
            {
                await DisposeTarWriter(writer, async);
            }

            archive.Seek(0, SeekOrigin.Begin);

            UstarTarEntry entry;
            TarReader reader = await CreateTarReader(archive, async);
            try
            {
                entry = await GetNextEntry(reader, async, copyData: true) as UstarTarEntry;
                Assert.NotNull(entry);
                Assert.Equal(TarEntryType.RegularFile, entry.EntryType);

                Assert.NotNull(await GetNextEntry(reader, async));
                Assert.Null(await GetNextEntry(reader, async));

                entry.DataStream.Seek(0, SeekOrigin.Begin);
                using (StreamReader streamReader = new StreamReader(entry.DataStream))
                {
                    string actualText = streamReader.ReadLine();
                    Assert.Equal(expectedText, actualText);
                }
            }
            finally
            {
                await DisposeTarReader(reader, async);
            }

            Assert.Throws<ObjectDisposedException>(() => entry.DataStream.Read(new byte[1]));
        }

        [Theory]
        [MemberData(nameof(Get_Boolean_Data))]
        public async Task GetNextEntry_CopyDataTrue_UnseekableArchive(bool async)
        {
            string expectedText = "Hello world!";
            using MemoryStream archive = new MemoryStream();
            TarWriter writer = await CreateTarWriter(archive, async, TarEntryFormat.Ustar, leaveOpen: true);
            try
            {
                UstarTarEntry entry1 = new UstarTarEntry(TarEntryType.RegularFile, "file.txt");
                entry1.DataStream = new MemoryStream();
                using (StreamWriter streamWriter = new StreamWriter(entry1.DataStream, leaveOpen: true))
                {
                    streamWriter.WriteLine(expectedText);
                }
                entry1.DataStream.Seek(0, SeekOrigin.Begin);
                await WriteEntry(writer, entry1, async);

                UstarTarEntry entry2 = new UstarTarEntry(TarEntryType.Directory, "dir");
                await WriteEntry(writer, entry2, async);
            }
            finally
            {
                await DisposeTarWriter(writer, async);
            }

            archive.Seek(0, SeekOrigin.Begin);
            using WrappedStream wrapped = new WrappedStream(archive, canRead: true, canWrite: false, canSeek: false);

            UstarTarEntry entry;
            TarReader reader = await CreateTarReader(wrapped, async, leaveOpen: true);
            try
            {
                entry = await GetNextEntry(reader, async, copyData: true) as UstarTarEntry;
                Assert.NotNull(entry);
                Assert.Equal(TarEntryType.RegularFile, entry.EntryType);

                Assert.NotNull(await GetNextEntry(reader, async));
                Assert.Null(await GetNextEntry(reader, async));

                Assert.NotNull(entry.DataStream);
                entry.DataStream.Seek(0, SeekOrigin.Begin);
                using (StreamReader streamReader = new StreamReader(entry.DataStream))
                {
                    string actualText = streamReader.ReadLine();
                    Assert.Equal(expectedText, actualText);
                }
            }
            finally
            {
                await DisposeTarReader(reader, async);
            }

            Assert.Throws<ObjectDisposedException>(() => entry.DataStream.Read(new byte[1]));
        }

        [Theory]
        [InlineData(TarEntryFormat.V7, false)]
        [InlineData(TarEntryFormat.V7, true)]
        [InlineData(TarEntryFormat.Ustar, false)]
        [InlineData(TarEntryFormat.Ustar, true)]
        [InlineData(TarEntryFormat.Pax, false)]
        [InlineData(TarEntryFormat.Pax, true)]
        [InlineData(TarEntryFormat.Gnu, false)]
        [InlineData(TarEntryFormat.Gnu, true)]
        public async Task GetNextEntry_CopyDataFalse_UnseekableArchive_Exceptions(TarEntryFormat format, bool async)
        {
            TarEntryType fileEntryType = GetTarEntryTypeForTarEntryFormat(TarEntryType.RegularFile, format);
            using MemoryStream archive = new MemoryStream();
            TarWriter writer = await CreateTarWriter(archive, async, format, leaveOpen: true);
            try
            {
                TarEntry entry1 = InvokeTarEntryCreationConstructor(format, fileEntryType, "file.txt");
                entry1.DataStream = new MemoryStream();
                using (StreamWriter streamWriter = new StreamWriter(entry1.DataStream, leaveOpen: true))
                {
                    streamWriter.WriteLine("Hello world!");
                }
                entry1.DataStream.Seek(0, SeekOrigin.Begin);
                await WriteEntry(writer, entry1, async);

                TarEntry entry2 = InvokeTarEntryCreationConstructor(format, TarEntryType.Directory, "dir");
                await WriteEntry(writer, entry2, async);
            }
            finally
            {
                await DisposeTarWriter(writer, async);
            }

            archive.Seek(0, SeekOrigin.Begin);
            using WrappedStream wrapped = new WrappedStream(archive, canRead: true, canWrite: false, canSeek: false);
            TarEntry entry;
            byte[] b = new byte[1];
            TarReader reader = await CreateTarReader(wrapped, async);
            try
            {
                entry = await GetNextEntry(reader, async, copyData: false);
                Assert.NotNull(entry);
                Assert.Equal(format, entry.Format);
                Assert.Equal(fileEntryType, entry.EntryType);
                entry.DataStream.ReadByte();

                TarEntry entry2 = await GetNextEntry(reader, async);
                Assert.NotNull(entry2);
                Assert.Equal(format, entry2.Format);
                Assert.Equal(TarEntryType.Directory, entry2.EntryType);
                Assert.Null(await GetNextEntry(reader, async));

                Assert.Throws<EndOfStreamException>(() => entry.DataStream.Read(b));
            }
            finally
            {
                await DisposeTarReader(reader, async);
            }

            Assert.Throws<ObjectDisposedException>(() => entry.DataStream.Read(b));
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public async Task GetNextEntry_UnseekableArchive_ReplaceDataStream_ExcludeFromDisposing(bool copyData, bool async)
        {
            using MemoryStream archive = new MemoryStream();
            TarWriter writer = await CreateTarWriter(archive, async, TarEntryFormat.Ustar, leaveOpen: true);
            try
            {
                UstarTarEntry entry1 = new UstarTarEntry(TarEntryType.RegularFile, "file.txt");
                entry1.DataStream = new MemoryStream();
                using (StreamWriter streamWriter = new StreamWriter(entry1.DataStream, leaveOpen: true))
                {
                    streamWriter.WriteLine("Hello world!");
                }
                entry1.DataStream.Seek(0, SeekOrigin.Begin);
                await WriteEntry(writer, entry1, async);

                UstarTarEntry entry2 = new UstarTarEntry(TarEntryType.Directory, "dir");
                await WriteEntry(writer, entry2, async);
            }
            finally
            {
                await DisposeTarWriter(writer, async);
            }

            archive.Seek(0, SeekOrigin.Begin);
            using WrappedStream wrapped = new WrappedStream(archive, canRead: true, canWrite: false, canSeek: false);
            UstarTarEntry entry;
            Stream oldStream;
            TarReader reader = await CreateTarReader(wrapped, async);
            try
            {
                entry = await GetNextEntry(reader, async, copyData) as UstarTarEntry;
                Assert.NotNull(entry);
                Assert.Equal(TarEntryType.RegularFile, entry.EntryType);

                oldStream = entry.DataStream;

                entry.DataStream = new MemoryStream();
                using (StreamWriter streamWriter = new StreamWriter(entry.DataStream, leaveOpen: true))
                {
                    streamWriter.WriteLine("Substituted");
                }
            }
            finally
            {
                await DisposeTarReader(reader, async);
            }

            Assert.Throws<ObjectDisposedException>(() => oldStream.Read(new byte[1]));

            entry.DataStream.Seek(0, SeekOrigin.Begin);
            using (StreamReader streamReader = new StreamReader(entry.DataStream))
            {
                Assert.Equal("Substituted", streamReader.ReadLine());
            }
        }

        [Theory]
        [InlineData(512, false, false)]
        [InlineData(512, false, true)]
        [InlineData(512, true, false)]
        [InlineData(512, true, true)]
        [InlineData(513, false, false)]
        [InlineData(513, false, true)]
        [InlineData(513, true, false)]
        [InlineData(513, true, true)]
        [InlineData(1023, false, false)]
        [InlineData(1023, false, true)]
        [InlineData(1023, true, false)]
        [InlineData(1023, true, true)]
        public async Task BlockAlignmentPadding_DoesNotAffectNextEntries(int contentSize, bool copyData, bool async)
        {
            byte[] fileContents = new byte[contentSize];
            Array.Fill<byte>(fileContents, 0x1);

            using MemoryStream archive = new MemoryStream();
            TarWriter writer = await CreateTarWriter(archive, async, leaveOpen: true);
            try
            {
                PaxTarEntry entry1 = new PaxTarEntry(TarEntryType.RegularFile, "file");
                entry1.DataStream = new MemoryStream(fileContents);
                await WriteEntry(writer, entry1, async);

                PaxTarEntry entry2 = new PaxTarEntry(TarEntryType.RegularFile, "next-file");
                await WriteEntry(writer, entry2, async);
            }
            finally
            {
                await DisposeTarWriter(writer, async);
            }

            archive.Position = 0;
            using WrappedStream unseekable = new WrappedStream(archive, archive.CanRead, archive.CanWrite, canSeek: false);
            TarReader reader = await CreateTarReader(unseekable, async);
            try
            {
                TarEntry e = await GetNextEntry(reader, async, copyData);
                Assert.Equal(contentSize, e.Length);

                byte[] buffer = new byte[contentSize];
                while (e.DataStream.Read(buffer) > 0) ;
                AssertExtensions.SequenceEqual(fileContents, buffer);

                e = await GetNextEntry(reader, async, copyData);
                Assert.Equal(0, e.Length);

                e = await GetNextEntry(reader, async, copyData);
                Assert.Null(e);
            }
            finally
            {
                await DisposeTarReader(reader, async);
            }
        }

        [Theory]
        [MemberData(nameof(Get_Boolean_Data))]
        public async Task GetNextEntry_UnseekableArchive_DisposedDataStream_DoesNotThrow(bool async)
        {
            using MemoryStream archive = new MemoryStream();
            TarWriter writer = await CreateTarWriter(archive, async, leaveOpen: true);
            try
            {
                PaxTarEntry entry1 = new PaxTarEntry(TarEntryType.RegularFile, "file1.txt");
                entry1.DataStream = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 });
                await WriteEntry(writer, entry1, async);

                PaxTarEntry entry2 = new PaxTarEntry(TarEntryType.RegularFile, "file2.txt");
                entry2.DataStream = new MemoryStream(new byte[] { 6, 7, 8, 9, 10 });
                await WriteEntry(writer, entry2, async);
            }
            finally
            {
                await DisposeTarWriter(writer, async);
            }

            archive.Position = 0;
            using WrappedStream unseekable = new WrappedStream(archive, archive.CanRead, archive.CanWrite, canSeek: false);
            TarReader reader = await CreateTarReader(unseekable, async);
            try
            {
                TarEntry entry = await GetNextEntry(reader, async, copyData: false);
                Assert.NotNull(entry);
                Assert.Equal("file1.txt", entry.Name);

                Stream dataStream = entry.DataStream;
                Assert.NotNull(dataStream);

                byte[] buffer = new byte[5];
                int bytesRead = dataStream.Read(buffer, 0, buffer.Length);
                Assert.Equal(5, bytesRead);

                dataStream.Dispose();

                TarEntry nextEntry = await GetNextEntry(reader, async, copyData: false);
                Assert.NotNull(nextEntry);
                Assert.Equal("file2.txt", nextEntry.Name);

                Assert.Null(await GetNextEntry(reader, async));
            }
            finally
            {
                await DisposeTarReader(reader, async);
            }
        }

        [Theory]
        [MemberData(nameof(Get_Boolean_Data))]
        public async Task GetNextEntry_UnseekableArchive_DisposedDataStream_PartiallyRead_DoesNotThrow(bool async)
        {
            using MemoryStream archive = new MemoryStream();
            TarWriter writer = await CreateTarWriter(archive, async, leaveOpen: true);
            try
            {
                PaxTarEntry entry1 = new PaxTarEntry(TarEntryType.RegularFile, "file1.txt");
                entry1.DataStream = new MemoryStream(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 });
                await WriteEntry(writer, entry1, async);

                PaxTarEntry entry2 = new PaxTarEntry(TarEntryType.RegularFile, "file2.txt");
                entry2.DataStream = new MemoryStream(new byte[] { 11, 12, 13, 14, 15 });
                await WriteEntry(writer, entry2, async);
            }
            finally
            {
                await DisposeTarWriter(writer, async);
            }

            archive.Position = 0;
            using WrappedStream unseekable = new WrappedStream(archive, archive.CanRead, archive.CanWrite, canSeek: false);
            TarReader reader = await CreateTarReader(unseekable, async);
            try
            {
                TarEntry entry = await GetNextEntry(reader, async, copyData: false);
                Assert.NotNull(entry);
                Assert.Equal("file1.txt", entry.Name);

                Stream dataStream = entry.DataStream;
                Assert.NotNull(dataStream);

                byte[] buffer = new byte[3];
                int bytesRead = dataStream.Read(buffer, 0, buffer.Length);
                Assert.Equal(3, bytesRead);

                dataStream.Dispose();

                TarEntry nextEntry = await GetNextEntry(reader, async, copyData: false);
                Assert.NotNull(nextEntry);
                Assert.Equal("file2.txt", nextEntry.Name);

                Assert.Null(await GetNextEntry(reader, async));
            }
            finally
            {
                await DisposeTarReader(reader, async);
            }
        }

        [Theory]
        [MemberData(nameof(Get_Boolean_Data))]
        public async Task GetNextEntry_UnseekableArchive_DisposedDataStream_NotRead_DoesNotThrow(bool async)
        {
            using MemoryStream archive = new MemoryStream();
            TarWriter writer = await CreateTarWriter(archive, async, leaveOpen: true);
            try
            {
                PaxTarEntry entry1 = new PaxTarEntry(TarEntryType.RegularFile, "file1.txt");
                entry1.DataStream = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 });
                await WriteEntry(writer, entry1, async);

                PaxTarEntry entry2 = new PaxTarEntry(TarEntryType.RegularFile, "file2.txt");
                entry2.DataStream = new MemoryStream(new byte[] { 6, 7, 8, 9, 10 });
                await WriteEntry(writer, entry2, async);
            }
            finally
            {
                await DisposeTarWriter(writer, async);
            }

            archive.Position = 0;
            using WrappedStream unseekable = new WrappedStream(archive, archive.CanRead, archive.CanWrite, canSeek: false);
            TarReader reader = await CreateTarReader(unseekable, async);
            try
            {
                TarEntry entry = await GetNextEntry(reader, async, copyData: false);
                Assert.NotNull(entry);
                Assert.Equal("file1.txt", entry.Name);

                Stream dataStream = entry.DataStream;
                Assert.NotNull(dataStream);

                dataStream.Dispose();

                TarEntry nextEntry = await GetNextEntry(reader, async, copyData: false);
                Assert.NotNull(nextEntry);
                Assert.Equal("file2.txt", nextEntry.Name);

                Assert.Null(await GetNextEntry(reader, async));
            }
            finally
            {
                await DisposeTarReader(reader, async);
            }
        }

        public static IEnumerable<object[]> EAPathOverrideData()
        {
            // (headerName, eaPath, expectedName)
            yield return new object[] { "data/report.txt", "config/settings.txt", "config/settings.txt" };
            yield return new object[] { "../../escape.txt", "safe.txt", "safe.txt" };
            yield return new object[] { "safe.txt", "../../escape.txt", "../../escape.txt" };
        }

        [Theory]
        [MemberData(nameof(EAPathOverrideData))]
        public void PaxReader_EAPathOverridesHeaderName(string headerName, string eaPath, string expectedName)
        {
            byte[] content = "test data"u8.ToArray();
            byte[] archive = BuildRawPaxArchiveWithEAPathOverride(headerName, eaPath, content);

            using var stream = new MemoryStream(archive);
            using var reader = new TarReader(stream);
            TarEntry entry = reader.GetNextEntry();

            Assert.NotNull(entry);
            Assert.Equal(expectedName, entry.Name);
        }

        [Fact]
        public void PaxReader_EALinkpathOverridesHeaderLinkname()
        {
            byte[] archive = BuildRawPaxArchiveSymlink("mylink", "mylink", "./safe.txt", "./other.txt");

            using var stream = new MemoryStream(archive);
            using var reader = new TarReader(stream);
            TarEntry entry = reader.GetNextEntry();

            Assert.NotNull(entry);
            Assert.Equal("./other.txt", entry.LinkName);
        }

        public static IEnumerable<object[]> EASizeOverrideData()
        {
            // (actualDataSize, headerSize, eaSize) — EA size always takes precedence
            yield return new object[] { 10, 10L, 50L };   // eaSize > headerSize (larger)
            yield return new object[] { 100, 100L, 25L }; // eaSize < headerSize (smaller)
        }

        [Theory]
        [MemberData(nameof(EASizeOverrideData))]
        public void PaxReader_EASizeOverridesHeaderSize(int actualDataSize, long headerSize, long eaSize)
        {
            byte[] actualData = new byte[actualDataSize];
            Array.Fill<byte>(actualData, (byte)'X');

            byte[] archive = BuildRawPaxArchiveWithSizeOverride("file.bin", "file.bin", actualData, headerSize, eaSize);

            using var stream = new MemoryStream(archive);
            using var reader = new TarReader(stream);
            TarEntry entry = reader.GetNextEntry(copyData: true);

            Assert.NotNull(entry);
            Assert.Equal(eaSize, entry.Length);
        }

        [Fact]
        public void PaxReader_EntryLengthAndDataStreamLengthAreConsistent()
        {
            byte[] actualData = "ABCDEFGHIJ"u8.ToArray();
            long headerSize = 10;
            long eaSize = 50;

            byte[] archive = BuildRawPaxArchiveWithSizeOverride("file.bin", "file.bin", actualData, headerSize, eaSize);

            using var stream = new MemoryStream(archive);
            using var reader = new TarReader(stream);
            TarEntry entry = reader.GetNextEntry(copyData: true);

            Assert.NotNull(entry);
            Assert.NotNull(entry.DataStream);
            Assert.Equal(entry.Length, entry.DataStream.Length);
        }

        [Fact]
        public void Read_Archive_With_Unsupported_EntryType()
        {
            using MemoryStream archiveStream = new MemoryStream();

            byte[] header = new byte[512];

            byte[] nameBytes = System.Text.Encoding.UTF8.GetBytes("unsupported_entry");
            nameBytes.CopyTo(header.AsSpan(0, nameBytes.Length));

            // Set mode field (octal 644 = rw-r--r--)
            System.Text.Encoding.UTF8.GetBytes("0000644 ").CopyTo(header.AsSpan(100, 8));
            // Set uid field
            System.Text.Encoding.UTF8.GetBytes("0000000 ").CopyTo(header.AsSpan(108, 8));
            // Set gid field
            System.Text.Encoding.UTF8.GetBytes("0000000 ").CopyTo(header.AsSpan(116, 8));
            // Set size field
            System.Text.Encoding.UTF8.GetBytes("00000000000 ").CopyTo(header.AsSpan(124, 12));
            // Set mtime field
            System.Text.Encoding.UTF8.GetBytes("00000000000 ").CopyTo(header.AsSpan(136, 12));

            header[156] = (byte)TarEntryType.SparseFile; // Unsupported entry type

            System.Text.Encoding.UTF8.GetBytes("ustar ").CopyTo(header.AsSpan(257, 6));
            System.Text.Encoding.UTF8.GetBytes(" \0").CopyTo(header.AsSpan(263, 2));

            // Calculate checksum - the checksum field itself should be treated as spaces
            int checksum = 0;
            for (int i = 0; i < header.Length; i++)
            {
                if (i >= 148 && i < 156)
                {
                    checksum += (byte)' ';
                }
                else
                {
                    checksum += header[i];
                }
            }

            string checksumStr = Convert.ToString(checksum, 8).PadLeft(6, '0') + "\0 ";
            System.Text.Encoding.UTF8.GetBytes(checksumStr).CopyTo(header.AsSpan(148, 8));

            archiveStream.Write(header);
            archiveStream.Write(new byte[1024]);

            archiveStream.Seek(0, SeekOrigin.Begin);

            using TarReader reader = new TarReader(archiveStream);
            Assert.Throws<NotSupportedException>(() => reader.GetNextEntry());
        }
        [Theory]
        [MemberData(nameof(Get_Boolean_Data))]
        public async Task Read_PaxEntryWithOnlyLinkpath_PreservesUstarPrefix(bool async)
        {
            string expectedName = "./sdk/tools/net11.0/any/SomeAssembly.dll";
            string prefix = "./sdk";
            string nameField = "tools/net11.0/any/SomeAssembly.dll";
            string longLinkTarget = "../../../../../dotnet-format/BuildHost-netcore/Microsoft.CodeAnalysis.Workspaces.MSBuild.BuildHost.dll";
            using MemoryStream archiveStream = new MemoryStream();

            byte[] paxHeader = new byte[512];
            Encoding.UTF8.GetBytes("./PaxHeaders.12345/SomeAssembly.dll").CopyTo(paxHeader.AsSpan(0));
            Encoding.UTF8.GetBytes("0000644\0").CopyTo(paxHeader.AsSpan(100, 8));
            Encoding.UTF8.GetBytes("0000000\0").CopyTo(paxHeader.AsSpan(108, 8));
            Encoding.UTF8.GetBytes("0000000\0").CopyTo(paxHeader.AsSpan(116, 8));
            Encoding.UTF8.GetBytes("00000000000\0").CopyTo(paxHeader.AsSpan(136, 12));
            paxHeader[156] = (byte)'x';
            Encoding.UTF8.GetBytes("ustar\0").CopyTo(paxHeader.AsSpan(257, 6));
            Encoding.UTF8.GetBytes("00").CopyTo(paxHeader.AsSpan(263, 2));

            string paxPayload = $"linkpath={longLinkTarget}\n";
            int totalLen = 1 + paxPayload.Length;
            while (totalLen.ToString().Length + 1 + paxPayload.Length != totalLen)
            {
                totalLen = totalLen.ToString().Length + 1 + paxPayload.Length;
            }

            string paxData = $"{totalLen} {paxPayload}";
            byte[] paxDataBytes = Encoding.UTF8.GetBytes(paxData);

            string sizeOctal = Convert.ToString(paxDataBytes.Length, 8).PadLeft(11, '0') + "\0";
            Encoding.UTF8.GetBytes(sizeOctal).CopyTo(paxHeader.AsSpan(124, 12));

            WriteHeaderChecksum(paxHeader);
            archiveStream.Write(paxHeader);
            archiveStream.Write(paxDataBytes);
            int padding = (512 - (paxDataBytes.Length % 512)) % 512;
            if (padding > 0)
            {
                archiveStream.Write(new byte[padding]);
            }

            byte[] entryHeader = new byte[512];
            Encoding.UTF8.GetBytes(nameField).CopyTo(entryHeader.AsSpan(0));
            Encoding.UTF8.GetBytes("0000777\0").CopyTo(entryHeader.AsSpan(100, 8));
            Encoding.UTF8.GetBytes("0000000\0").CopyTo(entryHeader.AsSpan(108, 8));
            Encoding.UTF8.GetBytes("0000000\0").CopyTo(entryHeader.AsSpan(116, 8));
            Encoding.UTF8.GetBytes("00000000000\0").CopyTo(entryHeader.AsSpan(124, 12));
            Encoding.UTF8.GetBytes("14751414000\0").CopyTo(entryHeader.AsSpan(136, 12));
            entryHeader[156] = (byte)'2';
            Encoding.UTF8.GetBytes(longLinkTarget.Substring(0, Math.Min(100, longLinkTarget.Length)))
                .CopyTo(entryHeader.AsSpan(157));
            Encoding.UTF8.GetBytes("ustar\0").CopyTo(entryHeader.AsSpan(257, 6));
            Encoding.UTF8.GetBytes("00").CopyTo(entryHeader.AsSpan(263, 2));
            Encoding.UTF8.GetBytes(prefix).CopyTo(entryHeader.AsSpan(345));

            WriteHeaderChecksum(entryHeader);
            archiveStream.Write(entryHeader);
            archiveStream.Write(new byte[1024]);
            archiveStream.Seek(0, SeekOrigin.Begin);

            TarReader reader = await CreateTarReader(archiveStream, async);
            try
            {
                TarEntry entry = await GetNextEntry(reader, async);
                Assert.NotNull(entry);
                Assert.Equal(expectedName, entry.Name);
                Assert.Equal(longLinkTarget, entry.LinkName);
                Assert.Equal(TarEntryType.SymbolicLink, entry.EntryType);
                Assert.Null(await GetNextEntry(reader, async));
            }
            finally
            {
                await DisposeTarReader(reader, async);
            }
        }

        [Theory]
        [InlineData("PaxExtendedAttributes", MaxMetadataBlockSize - 100, false)]
        [InlineData("PaxExtendedAttributes", MaxMetadataBlockSize - 100, true)]
        [InlineData("GnuLongPath", MaxMetadataBlockSize, false)]
        [InlineData("GnuLongPath", MaxMetadataBlockSize, true)]
        [InlineData("GnuLongLink", MaxMetadataBlockSize, false)]
        [InlineData("GnuLongLink", MaxMetadataBlockSize, true)]
        public async Task MetadataBlock_UnderMaxSize_Succeeds(string metadataType, int size, bool async)
        {
            using MemoryStream archive = new MemoryStream();
            await WriteMetadataEntry(async, archive, metadataType, size);

            archive.Seek(0, SeekOrigin.Begin);
            TarReader reader = await CreateTarReader(archive, async);
            try
            {
                Assert.NotNull(await GetNextEntry(reader, async));
            }
            finally
            {
                await DisposeTarReader(reader, async);
            }
        }

        [Theory]
        [InlineData("PaxExtendedAttributes", MaxMetadataBlockSize, false)]
        [InlineData("PaxExtendedAttributes", MaxMetadataBlockSize, true)]
        [InlineData("GnuLongPath", MaxMetadataBlockSize + 1, false)]
        [InlineData("GnuLongPath", MaxMetadataBlockSize + 1, true)]
        [InlineData("GnuLongLink", MaxMetadataBlockSize + 1, false)]
        [InlineData("GnuLongLink", MaxMetadataBlockSize + 1, true)]
        public async Task MetadataBlock_ExceedsMaxSize_Throws(string metadataType, int size, bool async)
        {
            using MemoryStream archive = new MemoryStream();
            await WriteMetadataEntry(async, archive, metadataType, size);

            archive.Seek(0, SeekOrigin.Begin);
            TarReader reader = await CreateTarReader(archive, async);
            try
            {
                if (async)
                {
                    await Assert.ThrowsAsync<InvalidOperationException>(async () => await GetNextEntry(reader, async));
                }
                else
                {
                    Assert.Throws<InvalidOperationException>(() => GetNextEntry(reader, async).GetAwaiter().GetResult());
                }
            }
            finally
            {
                await DisposeTarReader(reader, async);
            }
        }

        private static async Task WriteMetadataEntry(bool async, MemoryStream archive, string metadataType, int size)
        {
            switch (metadataType)
            {
                case "PaxExtendedAttributes":
                {
                    var extendedAttributes = new Dictionary<string, string>
                    {
                        ["bigkey"] = new string('x', size)
                    };
                    TarWriter paxWriter = await CreateTarWriter(archive, async, TarEntryFormat.Pax, leaveOpen: true);
                    try
                    {
                        await WriteEntry(paxWriter, new PaxTarEntry(TarEntryType.RegularFile, "test.txt", extendedAttributes), async);
                    }
                    finally
                    {
                        await DisposeTarWriter(paxWriter, async);
                    }
                    break;
                }

                case "GnuLongPath":
                {
                    TarWriter gnuPathWriter = await CreateTarWriter(archive, async, TarEntryFormat.Gnu, leaveOpen: true);
                    try
                    {
                        await WriteEntry(gnuPathWriter, new GnuTarEntry(TarEntryType.RegularFile, new string('a', size - 1)), async);
                    }
                    finally
                    {
                        await DisposeTarWriter(gnuPathWriter, async);
                    }
                    break;
                }

                case "GnuLongLink":
                {
                    TarWriter gnuLinkWriter = await CreateTarWriter(archive, async, TarEntryFormat.Gnu, leaveOpen: true);
                    try
                    {
                        GnuTarEntry entry = new GnuTarEntry(TarEntryType.SymbolicLink, "test.txt");
                        entry.LinkName = new string('a', size - 1);
                        await WriteEntry(gnuLinkWriter, entry, async);
                    }
                    finally
                    {
                        await DisposeTarWriter(gnuLinkWriter, async);
                    }
                    break;
                }
            }
        }
    }
}
