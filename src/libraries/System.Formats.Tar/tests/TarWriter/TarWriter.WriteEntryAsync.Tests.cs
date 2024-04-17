// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Formats.Tar.Tests
{
    // Tests that are independent of the archive format.
    public class TarWriter_WriteEntryAsync_Tests : TarWriter_WriteEntry_Base
    {
        [Theory]
        [InlineData(TarEntryFormat.V7)]
        [InlineData(TarEntryFormat.Ustar)]
        [InlineData(TarEntryFormat.Pax)]
        [InlineData(TarEntryFormat.Gnu)]
        public async Task WriteEntryAsync_Cancel(TarEntryFormat format)
        {
            CancellationTokenSource cs = new CancellationTokenSource();
            cs.Cancel();
            await using (MemoryStream archiveStream = new MemoryStream())
            {
                await using (TarWriter writer = new TarWriter(archiveStream, leaveOpen: false))
                {
                    TarEntry entry = InvokeTarEntryCreationConstructor(format, TarEntryType.Directory, "dir");
                    await Assert.ThrowsAsync<TaskCanceledException>(() => writer.WriteEntryAsync(entry, cs.Token));
                    await Assert.ThrowsAsync<TaskCanceledException>(() => writer.WriteEntryAsync("file.txt", "file.txt", cs.Token));
                }
            }
        }

        [Fact]
        public async Task WriteEntry_AfterDispose_Throws_Async()
        {
            using MemoryStream archiveStream = new MemoryStream();
            TarWriter writer = new TarWriter(archiveStream);
            await writer.DisposeAsync();

            PaxTarEntry entry = new PaxTarEntry(TarEntryType.RegularFile, InitialEntryName);
            await Assert.ThrowsAsync<ObjectDisposedException>(() => writer.WriteEntryAsync(entry));
        }

        [Theory]
        [InlineData(TarEntryFormat.V7)]
        [InlineData(TarEntryFormat.Ustar)]
        [InlineData(TarEntryFormat.Pax)]
        [InlineData(TarEntryFormat.Gnu)]
        public async Task WriteEntry_FromUnseekableStream_AdvanceDataStream_WriteFromThatPosition_Async(TarEntryFormat format)
        {
            using MemoryStream source = GetTarMemoryStream(CompressionMethod.Uncompressed, TestTarFormat.ustar, "file");
            using WrappedStream unseekable = new WrappedStream(source, canRead: true, canWrite: true, canSeek: false);

            using MemoryStream destination = new MemoryStream();
            await using (TarReader reader1 = new TarReader(unseekable))
            {
                TarEntry entry = await reader1.GetNextEntryAsync();
                Assert.NotNull(entry);
                Assert.NotNull(entry.DataStream);
                entry.DataStream.ReadByte(); // Advance one byte, now the expected string would be "ello file"

                await using (TarWriter writer = new TarWriter(destination, format, leaveOpen: true))
                {
                    await writer.WriteEntryAsync(entry);
                    TarEntry dirEntry = InvokeTarEntryCreationConstructor(format, TarEntryType.Directory, "dir");
                    await writer.WriteEntryAsync(dirEntry); // To validate that next entry is not affected
                }
            }

            destination.Seek(0, SeekOrigin.Begin);
            await using (TarReader reader2 = new TarReader(destination))
            {
                TarEntry entry = await reader2.GetNextEntryAsync();
                Assert.NotNull(entry);
                Assert.NotNull(entry.DataStream);

                using (StreamReader streamReader = new StreamReader(entry.DataStream, leaveOpen: true))
                {
                    string contents = streamReader.ReadLine();
                    Assert.Equal("ello file", contents);
                }

                TarEntry dirEntry = await reader2.GetNextEntryAsync();
                Assert.NotNull(dirEntry);
                Assert.Equal(format, dirEntry.Format);
                Assert.Equal(TarEntryType.Directory, dirEntry.EntryType);
                Assert.Equal("dir", dirEntry.Name);

                Assert.Null(await reader2.GetNextEntryAsync());
            }
        }

        [Theory]
        [InlineData(TarEntryFormat.V7)]
        [InlineData(TarEntryFormat.Ustar)]
        [InlineData(TarEntryFormat.Pax)]
        [InlineData(TarEntryFormat.Gnu)]
        public async Task WriteEntry_RespectDefaultWriterFormat_Async(TarEntryFormat expectedFormat)
        {
            using (TempDirectory root = new TempDirectory())
            {
                string path = Path.Join(root.Path, "file.txt");
                File.Create(path).Dispose();

                await using (MemoryStream archiveStream = new MemoryStream())
                {
                    await using (TarWriter writer = new TarWriter(archiveStream, expectedFormat, leaveOpen: true))
                    {
                        await writer.WriteEntryAsync(path, "file.txt");
                    }

                    archiveStream.Position = 0;
                    await using (TarReader reader = new TarReader(archiveStream, leaveOpen: false))
                    {
                        TarEntry entry = await reader.GetNextEntryAsync();
                        Assert.Equal(expectedFormat, entry.Format);

                        Type expectedType = GetTypeForFormat(expectedFormat);

                        Assert.Equal(expectedType, entry.GetType());
                    }
                }
            }
        }

        [Theory]
        [InlineData(TarEntryFormat.Ustar)]
        [InlineData(TarEntryFormat.Pax)]
        [InlineData(TarEntryFormat.Gnu)]
        public async Task Write_RegularFileEntry_In_V7Writer_Async(TarEntryFormat entryFormat)
        {
            using MemoryStream archive = new MemoryStream();
            await using (TarWriter writer = new TarWriter(archive, format: TarEntryFormat.V7, leaveOpen: true))
            {
                TarEntry entry = entryFormat switch
                {
                    TarEntryFormat.Ustar => new UstarTarEntry(TarEntryType.RegularFile, InitialEntryName),
                    TarEntryFormat.Pax => new PaxTarEntry(TarEntryType.RegularFile, InitialEntryName),
                    TarEntryFormat.Gnu => new GnuTarEntry(TarEntryType.RegularFile, InitialEntryName),
                    _ => throw new InvalidDataException($"Unexpected format: {entryFormat}")
                };

                // Should be written in the format of the entry
                await writer.WriteEntryAsync(entry);
            }

            archive.Seek(0, SeekOrigin.Begin);
            await using (TarReader reader = new TarReader(archive))
            {
                TarEntry entry = await reader.GetNextEntryAsync();
                Assert.NotNull(entry);
                Assert.Equal(entryFormat, entry.Format);

                switch (entryFormat)
                {
                    case TarEntryFormat.Ustar:
                        Assert.True(entry is UstarTarEntry);
                        break;
                    case TarEntryFormat.Pax:
                        Assert.True(entry is PaxTarEntry);
                        break;
                    case TarEntryFormat.Gnu:
                        Assert.True(entry is GnuTarEntry);
                        break;
                }

                Assert.Null(await reader.GetNextEntryAsync());
            }
        }

        [Theory]
        [InlineData(TarEntryFormat.Ustar)]
        [InlineData(TarEntryFormat.Pax)]
        [InlineData(TarEntryFormat.Gnu)]
        public async Task Write_V7RegularFileEntry_In_OtherFormatsWriter_Async(TarEntryFormat writerFormat)
        {
            using MemoryStream archive = new MemoryStream();
            await using (TarWriter writer = new TarWriter(archive, format: writerFormat, leaveOpen: true))
            {
                V7TarEntry entry = new V7TarEntry(TarEntryType.V7RegularFile, InitialEntryName);

                // Should be written in the format of the entry
                await writer.WriteEntryAsync(entry);
            }

            archive.Seek(0, SeekOrigin.Begin);
            await using (TarReader reader = new TarReader(archive))
            {
                TarEntry entry = await reader.GetNextEntryAsync();
                Assert.NotNull(entry);
                Assert.Equal(TarEntryFormat.V7, entry.Format);
                Assert.True(entry is V7TarEntry);

                Assert.Null(await reader.GetNextEntryAsync());
            }
        }

        [Theory]
        [InlineData(TarEntryFormat.V7)]
        [InlineData(TarEntryFormat.Ustar)]
        [InlineData(TarEntryFormat.Pax)]
        [InlineData(TarEntryFormat.Gnu)]
        public async Task ReadAndWriteMultipleGlobalExtendedAttributesEntries_Async(TarEntryFormat format)
        {
            Dictionary<string, string> attrs = new Dictionary<string, string>()
            {
                { "hello", "world" },
                { "dotnet", "runtime" }
            };

            using MemoryStream archiveStream = new MemoryStream();
            await using (TarWriter writer = new TarWriter(archiveStream, leaveOpen: true))
            {
                PaxGlobalExtendedAttributesTarEntry gea1 = new PaxGlobalExtendedAttributesTarEntry(attrs);
                await writer.WriteEntryAsync(gea1);

                TarEntry entry1 = InvokeTarEntryCreationConstructor(format, TarEntryType.Directory, "dir1");
                await writer.WriteEntryAsync(entry1);

                PaxGlobalExtendedAttributesTarEntry gea2 = new PaxGlobalExtendedAttributesTarEntry(attrs);
                await writer.WriteEntryAsync(gea2);

                TarEntry entry2 = InvokeTarEntryCreationConstructor(format, TarEntryType.Directory, "dir2");
                await writer.WriteEntryAsync(entry2);
            }

            archiveStream.Position = 0;
            await using (TarReader reader = new TarReader(archiveStream, leaveOpen: false))
            {
                VerifyGlobalExtendedAttributesEntry(await reader.GetNextEntryAsync(), attrs);
                VerifyDirectory(await reader.GetNextEntryAsync(), format, "dir1");
                VerifyGlobalExtendedAttributesEntry(await reader.GetNextEntryAsync(), attrs);
                VerifyDirectory(await reader.GetNextEntryAsync(), format, "dir2");
                Assert.Null(await reader.GetNextEntryAsync());
            }
        }

        [Theory]
        [MemberData(nameof(WriteTimeStampsWithFormats_TheoryData))]
        public async Task WriteTimeStamps_Async(TarEntryFormat format, DateTimeOffset timestamp)
        {
            TarEntry entry = InvokeTarEntryCreationConstructor(format, TarEntryType.Directory, "dir");

            entry.ModificationTime = timestamp;
            Assert.Equal(timestamp, entry.ModificationTime);

            if (entry is GnuTarEntry gnuEntry)
            {
                gnuEntry.AccessTime = timestamp;
                Assert.Equal(timestamp, gnuEntry.AccessTime);

                gnuEntry.ChangeTime = timestamp;
                Assert.Equal(timestamp, gnuEntry.ChangeTime);
            }

            using MemoryStream archiveStream = new MemoryStream();
            await using (TarWriter writer = new TarWriter(archiveStream, leaveOpen: true))
            {
                await writer.WriteEntryAsync(entry);
            }

            archiveStream.Position = 0;
            await using (TarReader reader = new TarReader(archiveStream))
            {
                TarEntry readEntry = await reader.GetNextEntryAsync();
                Assert.NotNull(readEntry);

                Assert.Equal(timestamp, readEntry.ModificationTime);

                if (readEntry is GnuTarEntry gnuReadEntry)
                {
                    Assert.Equal(timestamp, gnuReadEntry.AccessTime);
                    Assert.Equal(timestamp, gnuReadEntry.ChangeTime);
                }
            }
        }

        [Theory]
        [MemberData(nameof(WriteIntField_TheoryData))]
        public async Task WriteUid_Async(TarEntryFormat format, int value)
        {
            TarEntry entry = InvokeTarEntryCreationConstructor(format, TarEntryType.Directory, "dir");

            entry.Uid = value;
            Assert.Equal(value, entry.Uid);

            using MemoryStream archiveStream = new MemoryStream();
            await using (TarWriter writer = new TarWriter(archiveStream, leaveOpen: true))
            {
                await writer.WriteEntryAsync(entry);
            }

            archiveStream.Position = 0;
            await using (TarReader reader = new TarReader(archiveStream))
            {
                TarEntry readEntry = await reader.GetNextEntryAsync();
                Assert.NotNull(readEntry);

                Assert.Equal(value, readEntry.Uid);
            }
        }

        [Theory]
        [MemberData(nameof(WriteIntField_TheoryData))]
        public async Task WriteGid_Async(TarEntryFormat format, int value)
        {
            TarEntry entry = InvokeTarEntryCreationConstructor(format, TarEntryType.Directory, "dir");

            entry.Gid = value;
            Assert.Equal(value, entry.Gid);

            using MemoryStream archiveStream = new MemoryStream();
            await using (TarWriter writer = new TarWriter(archiveStream, leaveOpen: true))
            {
                await writer.WriteEntryAsync(entry);
            }

            archiveStream.Position = 0;
            await using (TarReader reader = new TarReader(archiveStream))
            {
                TarEntry readEntry = await reader.GetNextEntryAsync();
                Assert.NotNull(readEntry);

                Assert.Equal(value, readEntry.Gid);
            }
        }

        [Theory]
        [MemberData(nameof(WriteIntField_TheoryData))]
        public async Task WriteDeviceMajor_Async(TarEntryFormat format, int value)
        {
            if (format == TarEntryFormat.V7)
            {
                return; // No DeviceMajor
            }

            PosixTarEntry? entry = InvokeTarEntryCreationConstructor(format, TarEntryType.BlockDevice, "dir") as PosixTarEntry;
            Assert.NotNull(entry);

            entry.DeviceMajor = value;
            Assert.Equal(value, entry.DeviceMajor);

            using MemoryStream archiveStream = new MemoryStream();
            await using (TarWriter writer = new TarWriter(archiveStream, leaveOpen: true))
            {
                await writer.WriteEntryAsync(entry);
            }

            archiveStream.Position = 0;
            await using (TarReader reader = new TarReader(archiveStream))
            {
                PosixTarEntry? readEntry = await reader.GetNextEntryAsync() as PosixTarEntry;
                Assert.NotNull(readEntry);

                Assert.Equal(value, readEntry.DeviceMajor);
            }
        }

        [Theory]
        [MemberData(nameof(WriteIntField_TheoryData))]
        public async Task WriteDeviceMinor_Async(TarEntryFormat format, int value)
        {
            if (format == TarEntryFormat.V7)
            {
                return; // No DeviceMinor
            }

            PosixTarEntry? entry = InvokeTarEntryCreationConstructor(format, TarEntryType.BlockDevice, "dir") as PosixTarEntry;
            Assert.NotNull(entry);

            entry.DeviceMinor = value;
            Assert.Equal(value, entry.DeviceMinor);

            using MemoryStream archiveStream = new MemoryStream();
            await using (TarWriter writer = new TarWriter(archiveStream, leaveOpen: true))
            {
                await writer.WriteEntryAsync(entry);
            }

            archiveStream.Position = 0;
            await using (TarReader reader = new TarReader(archiveStream))
            {
                PosixTarEntry? readEntry = await reader.GetNextEntryAsync() as PosixTarEntry;
                Assert.NotNull(readEntry);

                Assert.Equal(value, readEntry.DeviceMinor);
            }
        }

        public static IEnumerable<object[]> WriteEntry_TooLongName_Throws_Async_TheoryData()
            => TarWriter_WriteEntry_Tests.WriteEntry_TooLongName_Throws_TheoryData();

        [Theory]
        [MemberData(nameof(WriteEntry_TooLongName_Throws_Async_TheoryData))]
        public async Task WriteEntry_TooLongName_Throws_Async(TarEntryFormat entryFormat, TarEntryType entryType, string name)
        {
            await using TarWriter writer = new(new MemoryStream());

            TarEntry entry = InvokeTarEntryCreationConstructor(entryFormat, entryType, name);
            await Assert.ThrowsAsync<ArgumentException>("entry", () => writer.WriteEntryAsync(entry));
        }

        public static IEnumerable<object[]> WriteEntry_TooLongLinkName_Throws_Async_TheoryData()
            => TarWriter_WriteEntry_Tests.WriteEntry_TooLongLinkName_Throws_TheoryData();

        [Theory]
        [MemberData(nameof(WriteEntry_TooLongLinkName_Throws_Async_TheoryData))]
        public async Task WriteEntry_TooLongLinkName_Throws_Async(TarEntryFormat entryFormat, TarEntryType entryType, string linkName)
        {
            await using TarWriter writer = new(new MemoryStream());

            TarEntry entry = InvokeTarEntryCreationConstructor(entryFormat, entryType, "foo");
            entry.LinkName = linkName;

            await Assert.ThrowsAsync<ArgumentException>("entry", () => writer.WriteEntryAsync(entry));
        }

        public static IEnumerable<object[]> WriteEntry_TooLongUserGroupName_Throws_Async_TheoryData()
            => TarWriter_WriteEntry_Tests.WriteEntry_TooLongUserGroupName_Throws_TheoryData();

        [Theory]
        [MemberData(nameof(WriteEntry_TooLongUserGroupName_Throws_Async_TheoryData))]
        public async Task WriteEntry_TooLongUserName_Throws_Async(TarEntryFormat entryFormat, string userName)
        {
            await using TarWriter writer = new(new MemoryStream());

            TarEntry entry = InvokeTarEntryCreationConstructor(entryFormat, TarEntryType.RegularFile, "foo");
            PosixTarEntry posixEntry = Assert.IsAssignableFrom<PosixTarEntry>(entry);
            posixEntry.UserName = userName;

            await Assert.ThrowsAsync<ArgumentException>("entry", () => writer.WriteEntryAsync(entry));
        }

        [Theory]
        [MemberData(nameof(WriteEntry_TooLongUserGroupName_Throws_Async_TheoryData))]
        public async Task WriteEntry_TooLongGroupName_Throws_Async(TarEntryFormat entryFormat, string groupName)
        {
            await using TarWriter writer = new(new MemoryStream());

            TarEntry entry = InvokeTarEntryCreationConstructor(entryFormat, TarEntryType.RegularFile, "foo");
            PosixTarEntry posixEntry = Assert.IsAssignableFrom<PosixTarEntry>(entry);
            posixEntry.GroupName = groupName;

            await Assert.ThrowsAsync<ArgumentException>("entry", () => writer.WriteEntryAsync(entry));
        }

        public static IEnumerable<object[]> WriteEntry_UsingTarEntry_FromTarReader_IntoTarWriter_Async_TheoryData()
            => TarWriter_WriteEntry_Tests.WriteEntry_UsingTarEntry_FromTarReader_IntoTarWriter_TheoryData();

        [Theory]
        [MemberData(nameof(WriteEntry_UsingTarEntry_FromTarReader_IntoTarWriter_Async_TheoryData))]
        public async Task WriteEntry_UsingTarEntry_FromTarReader_IntoTarWriter_Async(TarEntryFormat entryFormat, TarEntryType entryType, bool unseekableStream)
        {
            using MemoryStream msSource = new();
            using MemoryStream msDestination = new();

            WriteTarArchiveWithOneEntry(msSource, entryFormat, entryType);
            msSource.Position = 0;

            Stream source = new WrappedStream(msSource, msSource.CanRead, msSource.CanWrite, canSeek: !unseekableStream);
            Stream destination = new WrappedStream(msDestination, msDestination.CanRead, msDestination.CanWrite, canSeek: !unseekableStream);

            await using (TarReader reader = new(source))
            await using (TarWriter writer = new(destination))
            {
                TarEntry entry;
                while ((entry = await reader.GetNextEntryAsync()) != null)
                {
                    await writer.WriteEntryAsync(entry);
                }
            }

            AssertExtensions.SequenceEqual(msSource.ToArray(), msDestination.ToArray());
        }

        [Theory]
        [InlineData(TarEntryFormat.V7)]
        [InlineData(TarEntryFormat.Ustar)]
        [InlineData(TarEntryFormat.Pax)]
        [InlineData(TarEntryFormat.Gnu)]
        public async Task WritingUnseekableDataStream_To_UnseekableArchiveStream_Throws_Async(TarEntryFormat entryFormat)
        {
            await using MemoryStream internalDataStream = new();
            await using WrappedStream unseekableDataStream = new(internalDataStream, canRead: true, canWrite: false, canSeek: false);

            await using MemoryStream internalArchiveStream = new();
            await using WrappedStream unseekableArchiveStream = new(internalArchiveStream, canRead: true, canWrite: true, canSeek: false);

            await using TarWriter writer = new(unseekableArchiveStream);
            TarEntry entry = InvokeTarEntryCreationConstructor(entryFormat, GetTarEntryTypeForTarEntryFormat(TarEntryType.RegularFile, entryFormat), "file.txt");
            entry.DataStream = unseekableDataStream;
            await Assert.ThrowsAsync<IOException>(() => writer.WriteEntryAsync(entry));
        }

        [Theory]
        [InlineData(TarEntryFormat.V7)]
        [InlineData(TarEntryFormat.Ustar)]
        [InlineData(TarEntryFormat.Pax)]
        [InlineData(TarEntryFormat.Gnu)]
        public async Task Write_TwoEntries_With_UnseekableDataStreams_Async(TarEntryFormat entryFormat)
        {
            byte[] expectedBytes = new byte[] { 0x1, 0x2, 0x3, 0x4, 0x5 };

            await using MemoryStream internalDataStream1 = new();
            await internalDataStream1.WriteAsync(expectedBytes.AsMemory());
            internalDataStream1.Position = 0;

            TarEntryType fileEntryType = GetTarEntryTypeForTarEntryFormat(TarEntryType.RegularFile, entryFormat);

            await using WrappedStream unseekableDataStream1 = new(internalDataStream1, canRead: true, canWrite: false, canSeek: false);
            TarEntry entry1 = InvokeTarEntryCreationConstructor(entryFormat, fileEntryType, "file1.txt");
            entry1.DataStream = unseekableDataStream1;

            await using MemoryStream internalDataStream2 = new();
            await internalDataStream2.WriteAsync(expectedBytes.AsMemory());
            internalDataStream2.Position = 0;

            await using WrappedStream unseekableDataStream2 = new(internalDataStream2, canRead: true, canWrite: false, canSeek: false);
            TarEntry entry2 = InvokeTarEntryCreationConstructor(entryFormat, fileEntryType, "file2.txt");
            entry2.DataStream = unseekableDataStream2;

            await using MemoryStream archiveStream = new();
            await using (TarWriter writer = new(archiveStream, leaveOpen: true))
            {
                await writer.WriteEntryAsync(entry1); // Should not throw
                await writer.WriteEntryAsync(entry2); // To verify that second entry is written in correct place
            }

            // Verify
            archiveStream.Position = 0;
            byte[] actualBytes = new byte[] { 0, 0, 0, 0, 0 };
            await using (TarReader reader = new(archiveStream))
            {
                TarEntry readEntry = await reader.GetNextEntryAsync();
                Assert.NotNull(readEntry);
                await readEntry.DataStream.ReadExactlyAsync(actualBytes);
                Assert.Equal(expectedBytes, actualBytes);

                readEntry = await reader.GetNextEntryAsync();
                Assert.NotNull(readEntry);
                await readEntry.DataStream.ReadExactlyAsync(actualBytes);
                Assert.Equal(expectedBytes, actualBytes);

                Assert.Null(await reader.GetNextEntryAsync());
            }
        }
    }
}
