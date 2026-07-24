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
    public class TarWriter_WriteEntry_Tests : TarWriter_WriteEntry_Base
    {
        [Theory]
        [MemberData(nameof(GetBooleanData))]
        public async Task WriteEntry_AfterDispose_Throws(bool async)
        {
            using MemoryStream archiveStream = new MemoryStream();
            TarWriter writer = CreateTarWriter(archiveStream);
            await DisposeTarWriter(writer, async);

            PaxTarEntry entry = new PaxTarEntry(TarEntryType.RegularFile, InitialEntryName);
            if (async)
            {
                await Assert.ThrowsAsync<ObjectDisposedException>(() => writer.WriteEntryAsync(entry));
            }
            else
            {
                Assert.Throws<ObjectDisposedException>(() => writer.WriteEntry(entry));
            }
        }

        [Theory]
        [InlineData(TarEntryFormat.V7)]
        [InlineData(TarEntryFormat.Ustar)]
        [InlineData(TarEntryFormat.Pax)]
        [InlineData(TarEntryFormat.Gnu)]
        public async Task WriteEntryAsync_Cancel(TarEntryFormat format)
        {
            CancellationTokenSource cs = new CancellationTokenSource();
            cs.Cancel();
            using MemoryStream archiveStream = new MemoryStream();
            await using (TarWriter writer = new TarWriter(archiveStream, leaveOpen: false))
            {
                TarEntry entry = InvokeTarEntryCreationConstructor(format, TarEntryType.Directory, "dir");
                await Assert.ThrowsAsync<TaskCanceledException>(() => writer.WriteEntryAsync(entry, cs.Token));
                await Assert.ThrowsAsync<TaskCanceledException>(() => writer.WriteEntryAsync("file.txt", "file.txt", cs.Token));
            }
        }

        [Theory]
        [InlineData(TarEntryFormat.V7)]
        [InlineData(TarEntryFormat.Ustar)]
        [InlineData(TarEntryFormat.Pax)]
        [InlineData(TarEntryFormat.Gnu)]
        public async Task WriteEntry_FromUnseekableStream_AdvanceDataStream_WriteFromThatPosition(TarEntryFormat format)
        {
            foreach (bool async in Booleans)
            {
                using MemoryStream source = GetTarMemoryStream(CompressionMethod.Uncompressed, TestTarFormat.ustar, "file");
                using WrappedStream unseekable = new WrappedStream(source, canRead: true, canWrite: true, canSeek: false);

                using MemoryStream destination = new MemoryStream();
                TarReader reader1 = CreateTarReader(unseekable);
                try
                {
                    TarEntry entry = await GetNextEntry(reader1, async: async);
                    Assert.NotNull(entry);
                    Assert.NotNull(entry.DataStream);
                    entry.DataStream.ReadByte();

                    TarWriter writer = CreateTarWriter(destination, format, leaveOpen: true);
                    try
                    {
                        await WriteEntry(writer, entry, async);
                        TarEntry dirEntry = InvokeTarEntryCreationConstructor(format, TarEntryType.Directory, "dir");
                        await WriteEntry(writer, dirEntry, async);
                    }
                    finally
                    {
                        await DisposeTarWriter(writer, async);
                    }
                }
                finally
                {
                    await DisposeTarReader(reader1, async);
                }

                destination.Seek(0, SeekOrigin.Begin);
                TarReader reader2 = CreateTarReader(destination);
                try
                {
                    TarEntry entry = await GetNextEntry(reader2, async: async);
                    Assert.NotNull(entry);
                    Assert.NotNull(entry.DataStream);

                    using (StreamReader streamReader = new StreamReader(entry.DataStream, leaveOpen: true))
                    {
                        string contents = streamReader.ReadLine();
                        Assert.Equal("ello file", contents);
                    }

                    TarEntry dirEntry = await GetNextEntry(reader2, async: async);
                    Assert.NotNull(dirEntry);
                    Assert.Equal(format, dirEntry.Format);
                    Assert.Equal(TarEntryType.Directory, dirEntry.EntryType);
                    Assert.Equal("dir", dirEntry.Name);

                    Assert.Null(await GetNextEntry(reader2, async: async));
                }
                finally
                {
                    await DisposeTarReader(reader2, async);
                }
            }
        }

        [Theory]
        [InlineData(TarEntryFormat.V7)]
        [InlineData(TarEntryFormat.Ustar)]
        [InlineData(TarEntryFormat.Pax)]
        [InlineData(TarEntryFormat.Gnu)]
        public async Task WriteEntry_RespectDefaultWriterFormat(TarEntryFormat expectedFormat)
        {
            foreach (bool async in Booleans)
            {
                using TempDirectory root = new TempDirectory();

                string path = Path.Join(root.Path, "file.txt");
                File.Create(path).Dispose();

                using MemoryStream archiveStream = new MemoryStream();
                TarWriter writer = CreateTarWriter(archiveStream, expectedFormat, leaveOpen: true);
                try
                {
                    await WriteEntry(writer, path, "file.txt", async);
                }
                finally
                {
                    await DisposeTarWriter(writer, async);
                }

                archiveStream.Position = 0;
                TarReader reader = CreateTarReader(archiveStream, leaveOpen: false);
                try
                {
                    TarEntry entry = await GetNextEntry(reader, async: async);
                    Assert.Equal(expectedFormat, entry.Format);

                    Type expectedType = GetTypeForFormat(expectedFormat);
                    Assert.Equal(expectedType, entry.GetType());
                }
                finally
                {
                    await DisposeTarReader(reader, async);
                }
            }
        }

        [Theory]
        [InlineData(TarEntryFormat.Ustar)]
        [InlineData(TarEntryFormat.Pax)]
        [InlineData(TarEntryFormat.Gnu)]
        public async Task Write_RegularFileEntry_In_V7Writer(TarEntryFormat entryFormat)
        {
            foreach (bool async in Booleans)
            {
                using MemoryStream archive = new MemoryStream();
                TarWriter writer = CreateTarWriter(archive, TarEntryFormat.V7, leaveOpen: true);
                try
                {
                    TarEntry entry = entryFormat switch
                    {
                        TarEntryFormat.Ustar => new UstarTarEntry(TarEntryType.RegularFile, InitialEntryName),
                        TarEntryFormat.Pax => new PaxTarEntry(TarEntryType.RegularFile, InitialEntryName),
                        TarEntryFormat.Gnu => new GnuTarEntry(TarEntryType.RegularFile, InitialEntryName),
                        _ => throw new InvalidDataException($"Unexpected format: {entryFormat}")
                    };

                    await WriteEntry(writer, entry, async);
                }
                finally
                {
                    await DisposeTarWriter(writer, async);
                }

                archive.Seek(0, SeekOrigin.Begin);
                TarReader reader = CreateTarReader(archive);
                try
                {
                    TarEntry entry = await GetNextEntry(reader, async: async);
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

                    Assert.Null(await GetNextEntry(reader, async: async));
                }
                finally
                {
                    await DisposeTarReader(reader, async);
                }
            }
        }

        [Theory]
        [InlineData(TarEntryFormat.Ustar)]
        [InlineData(TarEntryFormat.Pax)]
        [InlineData(TarEntryFormat.Gnu)]
        public async Task Write_V7RegularFileEntry_In_OtherFormatsWriter(TarEntryFormat writerFormat)
        {
            foreach (bool async in Booleans)
            {
                using MemoryStream archive = new MemoryStream();
                TarWriter writer = CreateTarWriter(archive, writerFormat, leaveOpen: true);
                try
                {
                    V7TarEntry entry = new V7TarEntry(TarEntryType.V7RegularFile, InitialEntryName);
                    await WriteEntry(writer, entry, async);
                }
                finally
                {
                    await DisposeTarWriter(writer, async);
                }

                archive.Seek(0, SeekOrigin.Begin);
                TarReader reader = CreateTarReader(archive);
                try
                {
                    TarEntry entry = await GetNextEntry(reader, async: async);
                    Assert.NotNull(entry);
                    Assert.Equal(TarEntryFormat.V7, entry.Format);
                    Assert.True(entry is V7TarEntry);

                    Assert.Null(await GetNextEntry(reader, async: async));
                }
                finally
                {
                    await DisposeTarReader(reader, async);
                }
            }
        }

        [Theory]
        [InlineData(TarEntryFormat.V7)]
        [InlineData(TarEntryFormat.Ustar)]
        [InlineData(TarEntryFormat.Pax)]
        [InlineData(TarEntryFormat.Gnu)]
        public async Task ReadAndWriteMultipleGlobalExtendedAttributesEntries(TarEntryFormat format)
        {
            foreach (bool async in Booleans)
            {
                Dictionary<string, string> attrs = new Dictionary<string, string>()
                {
                    { "hello", "world" },
                    { "dotnet", "runtime" }
                };

                using MemoryStream archiveStream = new MemoryStream();
                TarWriter writer = CreateTarWriter(archiveStream, TarEntryFormat.Pax, leaveOpen: true);
                try
                {
                    PaxGlobalExtendedAttributesTarEntry gea1 = new PaxGlobalExtendedAttributesTarEntry(attrs);
                    await WriteEntry(writer, gea1, async);

                    TarEntry entry1 = InvokeTarEntryCreationConstructor(format, TarEntryType.Directory, "dir1");
                    await WriteEntry(writer, entry1, async);

                    PaxGlobalExtendedAttributesTarEntry gea2 = new PaxGlobalExtendedAttributesTarEntry(attrs);
                    await WriteEntry(writer, gea2, async);

                    TarEntry entry2 = InvokeTarEntryCreationConstructor(format, TarEntryType.Directory, "dir2");
                    await WriteEntry(writer, entry2, async);
                }
                finally
                {
                    await DisposeTarWriter(writer, async);
                }

                archiveStream.Position = 0;
                TarReader reader = CreateTarReader(archiveStream, leaveOpen: false);
                try
                {
                    VerifyGlobalExtendedAttributesEntry(await GetNextEntry(reader, async: async), attrs);
                    VerifyDirectory(await GetNextEntry(reader, async: async), format, "dir1");
                    VerifyGlobalExtendedAttributesEntry(await GetNextEntry(reader, async: async), attrs);
                    VerifyDirectory(await GetNextEntry(reader, async: async), format, "dir2");
                    Assert.Null(await GetNextEntry(reader, async: async));
                }
                finally
                {
                    await DisposeTarReader(reader, async);
                }
            }
        }

        [Theory]
        [MemberData(nameof(WriteTimeStampsWithFormats_TheoryData))]
        public async Task WriteTimeStamps(TarEntryFormat format, DateTimeOffset timestamp)
        {
            foreach (bool async in Booleans)
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
                TarWriter writer = CreateTarWriter(archiveStream, TarEntryFormat.Pax, leaveOpen: true);
                try
                {
                    await WriteEntry(writer, entry, async);
                }
                finally
                {
                    await DisposeTarWriter(writer, async);
                }

                archiveStream.Position = 0;
                TarReader reader = CreateTarReader(archiveStream);
                try
                {
                    TarEntry readEntry = await GetNextEntry(reader, async: async);
                    Assert.NotNull(readEntry);

                    Assert.Equal(timestamp, readEntry.ModificationTime);

                    if (readEntry is GnuTarEntry gnuReadEntry)
                    {
                        Assert.Equal(timestamp, gnuReadEntry.AccessTime);
                        Assert.Equal(timestamp, gnuReadEntry.ChangeTime);
                    }
                }
                finally
                {
                    await DisposeTarReader(reader, async);
                }
            }
        }

        [Theory]
        [MemberData(nameof(WriteIntField_TheoryData))]
        public async Task WriteUid(TarEntryFormat format, int value)
        {
            foreach (bool async in Booleans)
            {
                TarEntry entry = InvokeTarEntryCreationConstructor(format, TarEntryType.Directory, "dir");

                entry.Uid = value;
                Assert.Equal(value, entry.Uid);

                using MemoryStream archiveStream = new MemoryStream();
                TarWriter writer = CreateTarWriter(archiveStream, TarEntryFormat.Pax, leaveOpen: true);
                try
                {
                    await WriteEntry(writer, entry, async);
                }
                finally
                {
                    await DisposeTarWriter(writer, async);
                }

                archiveStream.Position = 0;
                TarReader reader = CreateTarReader(archiveStream);
                try
                {
                    TarEntry readEntry = await GetNextEntry(reader, async: async);
                    Assert.NotNull(readEntry);

                    Assert.Equal(value, readEntry.Uid);
                }
                finally
                {
                    await DisposeTarReader(reader, async);
                }
            }
        }

        [Theory]
        [MemberData(nameof(WriteIntField_TheoryData))]
        public async Task WriteGid(TarEntryFormat format, int value)
        {
            foreach (bool async in Booleans)
            {
                TarEntry entry = InvokeTarEntryCreationConstructor(format, TarEntryType.Directory, "dir");

                entry.Gid = value;
                Assert.Equal(value, entry.Gid);

                using MemoryStream archiveStream = new MemoryStream();
                TarWriter writer = CreateTarWriter(archiveStream, TarEntryFormat.Pax, leaveOpen: true);
                try
                {
                    await WriteEntry(writer, entry, async);
                }
                finally
                {
                    await DisposeTarWriter(writer, async);
                }

                archiveStream.Position = 0;
                TarReader reader = CreateTarReader(archiveStream);
                try
                {
                    TarEntry readEntry = await GetNextEntry(reader, async: async);
                    Assert.NotNull(readEntry);

                    Assert.Equal(value, readEntry.Gid);
                }
                finally
                {
                    await DisposeTarReader(reader, async);
                }
            }
        }

        [Theory]
        [MemberData(nameof(WriteIntField_TheoryData))]
        public async Task WriteDeviceMajor(TarEntryFormat format, int value)
        {
            if (format == TarEntryFormat.V7)
            {
                return;
            }

            foreach (bool async in Booleans)
            {
                PosixTarEntry? entry = InvokeTarEntryCreationConstructor(format, TarEntryType.BlockDevice, "dir") as PosixTarEntry;
                Assert.NotNull(entry);

                entry.DeviceMajor = value;
                Assert.Equal(value, entry.DeviceMajor);

                using MemoryStream archiveStream = new MemoryStream();
                TarWriter writer = CreateTarWriter(archiveStream, TarEntryFormat.Pax, leaveOpen: true);
                try
                {
                    await WriteEntry(writer, entry, async);
                }
                finally
                {
                    await DisposeTarWriter(writer, async);
                }

                archiveStream.Position = 0;
                TarReader reader = CreateTarReader(archiveStream);
                try
                {
                    PosixTarEntry? readEntry = await GetNextEntry(reader, async: async) as PosixTarEntry;
                    Assert.NotNull(readEntry);

                    Assert.Equal(value, readEntry.DeviceMajor);
                }
                finally
                {
                    await DisposeTarReader(reader, async);
                }
            }
        }

        [Theory]
        [MemberData(nameof(WriteIntField_TheoryData))]
        public async Task WriteDeviceMinor(TarEntryFormat format, int value)
        {
            if (format == TarEntryFormat.V7)
            {
                return;
            }

            foreach (bool async in Booleans)
            {
                PosixTarEntry? entry = InvokeTarEntryCreationConstructor(format, TarEntryType.BlockDevice, "dir") as PosixTarEntry;
                Assert.NotNull(entry);

                entry.DeviceMinor = value;
                Assert.Equal(value, entry.DeviceMinor);

                using MemoryStream archiveStream = new MemoryStream();
                TarWriter writer = CreateTarWriter(archiveStream, TarEntryFormat.Pax, leaveOpen: true);
                try
                {
                    await WriteEntry(writer, entry, async);
                }
                finally
                {
                    await DisposeTarWriter(writer, async);
                }

                archiveStream.Position = 0;
                TarReader reader = CreateTarReader(archiveStream);
                try
                {
                    PosixTarEntry? readEntry = await GetNextEntry(reader, async: async) as PosixTarEntry;
                    Assert.NotNull(readEntry);

                    Assert.Equal(value, readEntry.DeviceMinor);
                }
                finally
                {
                    await DisposeTarReader(reader, async);
                }
            }
        }

        [Theory]
        [InlineData(TarEntryFormat.Pax)]
        [InlineData(TarEntryFormat.Gnu)]
        public void WriteLongName(TarEntryFormat format)
        {
            string maxPathComponent = new string('a', 255);
            WriteLongNameCore(format, maxPathComponent);

            maxPathComponent = new string('a', 90) + new string('b', 165);
            WriteLongNameCore(format, maxPathComponent);

            maxPathComponent = new string('a', 165) + new string('b', 90);
            WriteLongNameCore(format, maxPathComponent);
        }

        private void WriteLongNameCore(TarEntryFormat format, string maxPathComponent)
        {
            Assert.Equal(255, maxPathComponent.Length);

            TarEntry entry;
            MemoryStream ms = new();
            using (TarWriter writer = new(ms, true))
            {
                TarEntryType entryType = GetRegularFileEntryTypeForFormat(format);
                entry = InvokeTarEntryCreationConstructor(format, entryType, maxPathComponent);
                writer.WriteEntry(entry);

                entry = InvokeTarEntryCreationConstructor(format, entryType, Path.Join(maxPathComponent, maxPathComponent));
                writer.WriteEntry(entry);
            }

            ms.Position = 0;
            using TarReader reader = new(ms);

            entry = reader.GetNextEntry();
            string expectedName = GetExpectedNameForFormat(format, maxPathComponent);
            Assert.Equal(expectedName, entry.Name);

            entry = reader.GetNextEntry();
            expectedName = GetExpectedNameForFormat(format, Path.Join(maxPathComponent, maxPathComponent));
            Assert.Equal(expectedName, entry.Name);

            Assert.Null(reader.GetNextEntry());

            string GetExpectedNameForFormat(TarEntryFormat format, string expectedName)
            {
                if (format is TarEntryFormat.V7)
                {
                    return expectedName.Substring(0, 100);
                }
                return expectedName;
            }
        }

        public static IEnumerable<object[]> WriteEntry_TooLongName_Throws_TheoryData()
        {
            foreach (TarEntryType entryType in new[] { TarEntryType.RegularFile, TarEntryType.Directory })
            {
                foreach (string name in GetTooLongNamesTestData(NameCapabilities.Name))
                {
                    TarEntryType v7EntryType = entryType is TarEntryType.RegularFile ? TarEntryType.V7RegularFile : entryType;
                    yield return new object[] { TarEntryFormat.V7, v7EntryType, name };
                }

                foreach (string name in GetTooLongNamesTestData(NameCapabilities.NameAndPrefix))
                {
                    yield return new object[] { TarEntryFormat.Ustar, entryType, name };
                }
            }
        }

        [Theory]
        [MemberData(nameof(WriteEntry_TooLongName_Throws_TheoryData))]
        public async Task WriteEntry_TooLongName_Throws(TarEntryFormat entryFormat, TarEntryType entryType, string name)
        {
            foreach (bool async in Booleans)
            {
                using MemoryStream archiveStream = new MemoryStream();
                TarWriter writer = CreateTarWriter(archiveStream);
                try
                {
                    TarEntry entry = InvokeTarEntryCreationConstructor(entryFormat, entryType, name);
                    if (async)
                    {
                        await Assert.ThrowsAsync<ArgumentException>("entry", () => writer.WriteEntryAsync(entry));
                    }
                    else
                    {
                        Assert.Throws<ArgumentException>("entry", () => writer.WriteEntry(entry));
                    }
                }
                finally
                {
                    await DisposeTarWriter(writer, async);
                }
            }
        }

        public static IEnumerable<object[]> WriteEntry_TooLongLinkName_Throws_TheoryData()
        {
            foreach (TarEntryType entryType in new[] { TarEntryType.SymbolicLink, TarEntryType.HardLink })
            {
                foreach (string name in GetTooLongNamesTestData(NameCapabilities.Name))
                {
                    yield return new object[] { TarEntryFormat.V7, entryType, name };
                }

                foreach (string name in GetTooLongNamesTestData(NameCapabilities.NameAndPrefix))
                {
                    yield return new object[] { TarEntryFormat.Ustar, entryType, name };
                }
            }
        }

        [Theory]
        [MemberData(nameof(WriteEntry_TooLongLinkName_Throws_TheoryData))]
        public async Task WriteEntry_TooLongLinkName_Throws(TarEntryFormat entryFormat, TarEntryType entryType, string linkName)
        {
            foreach (bool async in Booleans)
            {
                using MemoryStream archiveStream = new MemoryStream();
                TarWriter writer = CreateTarWriter(archiveStream);
                try
                {
                    TarEntry entry = InvokeTarEntryCreationConstructor(entryFormat, entryType, "foo");
                    entry.LinkName = linkName;

                    if (async)
                    {
                        await Assert.ThrowsAsync<ArgumentException>("entry", () => writer.WriteEntryAsync(entry));
                    }
                    else
                    {
                        Assert.Throws<ArgumentException>("entry", () => writer.WriteEntry(entry));
                    }
                }
                finally
                {
                    await DisposeTarWriter(writer, async);
                }
            }
        }

        public static IEnumerable<object[]> WriteEntry_TooLongUserGroupName_Throws_TheoryData()
        {
            foreach (TarEntryFormat entryFormat in new[] { TarEntryFormat.Ustar, TarEntryFormat.Gnu })
            {
                yield return new object[] { entryFormat, Repeat(OneByteCharacter, 32 + 1) };
                yield return new object[] { entryFormat, Repeat(TwoBytesCharacter, 32 / 2 + 1) };
                yield return new object[] { entryFormat, Repeat(FourBytesCharacter, 32 / 4 + 1) };

                yield return new object[] { entryFormat, Repeat(TwoBytesCharacter, 32 - 2 + 1) + TwoBytesCharacter };
                yield return new object[] { entryFormat, Repeat(FourBytesCharacter, 32 - 4 + 1) + FourBytesCharacter };
            }
        }

        [Theory]
        [MemberData(nameof(WriteEntry_TooLongUserGroupName_Throws_TheoryData))]
        public async Task WriteEntry_TooLongUserName_Throws(TarEntryFormat entryFormat, string userName)
        {
            foreach (bool async in Booleans)
            {
                using MemoryStream archiveStream = new MemoryStream();
                TarWriter writer = CreateTarWriter(archiveStream);
                try
                {
                    TarEntry entry = InvokeTarEntryCreationConstructor(entryFormat, TarEntryType.RegularFile, "foo");
                    PosixTarEntry posixEntry = Assert.IsAssignableFrom<PosixTarEntry>(entry);
                    posixEntry.UserName = userName;

                    if (async)
                    {
                        await Assert.ThrowsAsync<ArgumentException>("entry", () => writer.WriteEntryAsync(entry));
                    }
                    else
                    {
                        Assert.Throws<ArgumentException>("entry", () => writer.WriteEntry(entry));
                    }
                }
                finally
                {
                    await DisposeTarWriter(writer, async);
                }
            }
        }

        [Theory]
        [MemberData(nameof(WriteEntry_TooLongUserGroupName_Throws_TheoryData))]
        public async Task WriteEntry_TooLongGroupName_Throws(TarEntryFormat entryFormat, string groupName)
        {
            foreach (bool async in Booleans)
            {
                using MemoryStream archiveStream = new MemoryStream();
                TarWriter writer = CreateTarWriter(archiveStream);
                try
                {
                    TarEntry entry = InvokeTarEntryCreationConstructor(entryFormat, TarEntryType.RegularFile, "foo");
                    PosixTarEntry posixEntry = Assert.IsAssignableFrom<PosixTarEntry>(entry);
                    posixEntry.GroupName = groupName;

                    if (async)
                    {
                        await Assert.ThrowsAsync<ArgumentException>("entry", () => writer.WriteEntryAsync(entry));
                    }
                    else
                    {
                        Assert.Throws<ArgumentException>("entry", () => writer.WriteEntry(entry));
                    }
                }
                finally
                {
                    await DisposeTarWriter(writer, async);
                }
            }
        }

        public static IEnumerable<object[]> WriteEntry_UsingTarEntry_FromTarReader_IntoTarWriter_TheoryData()
        {
            foreach (var entryFormat in new[] { TarEntryFormat.V7, TarEntryFormat.Ustar, TarEntryFormat.Pax, TarEntryFormat.Gnu })
            {
                foreach (var entryType in new[] { GetRegularFileEntryTypeForFormat(entryFormat), TarEntryType.Directory, TarEntryType.SymbolicLink })
                {
                    foreach (bool unseekableStream in new[] { false, true })
                    {
                        yield return new object[] { entryFormat, entryType, unseekableStream };
                    }
                }
            }
        }

        [Theory]
        [MemberData(nameof(WriteEntry_UsingTarEntry_FromTarReader_IntoTarWriter_TheoryData))]
        public async Task WriteEntry_UsingTarEntry_FromTarReader_IntoTarWriter(TarEntryFormat entryFormat, TarEntryType entryType, bool unseekableStream)
        {
            foreach (bool async in Booleans)
            {
                using MemoryStream msSource = new();
                using MemoryStream msDestination = new();

                WriteTarArchiveWithOneEntry(msSource, entryFormat, entryType);
                msSource.Position = 0;

                Stream source = new WrappedStream(msSource, msSource.CanRead, msSource.CanWrite, canSeek: !unseekableStream);
                Stream destination = new WrappedStream(msDestination, msDestination.CanRead, msDestination.CanWrite, canSeek: !unseekableStream);

                TarReader reader = CreateTarReader(source);
                try
                {
                    TarWriter writer = CreateTarWriter(destination);
                    try
                    {
                        TarEntry entry;
                        while ((entry = await GetNextEntry(reader, async: async)) != null)
                        {
                            await WriteEntry(writer, entry, async);
                        }
                    }
                    finally
                    {
                        await DisposeTarWriter(writer, async);
                    }
                }
                finally
                {
                    await DisposeTarReader(reader, async);
                }

                AssertExtensions.SequenceEqual(msSource.ToArray(), msDestination.ToArray());
            }
        }

        [Theory]
        [InlineData(TarEntryFormat.V7)]
        [InlineData(TarEntryFormat.Ustar)]
        [InlineData(TarEntryFormat.Pax)]
        [InlineData(TarEntryFormat.Gnu)]
        public async Task WritingUnseekableDataStream_To_UnseekableArchiveStream_Throws(TarEntryFormat entryFormat)
        {
            foreach (bool async in Booleans)
            {
                using MemoryStream internalDataStream = new();
                using WrappedStream unseekableDataStream = new(internalDataStream, canRead: true, canWrite: false, canSeek: false);

                using MemoryStream internalArchiveStream = new();
                using WrappedStream unseekableArchiveStream = new(internalArchiveStream, canRead: true, canWrite: true, canSeek: false);

                TarWriter writer = CreateTarWriter(unseekableArchiveStream);
                try
                {
                    TarEntry entry = InvokeTarEntryCreationConstructor(entryFormat, GetTarEntryTypeForTarEntryFormat(TarEntryType.RegularFile, entryFormat), "file.txt");
                    entry.DataStream = unseekableDataStream;
                    if (async)
                    {
                        await Assert.ThrowsAsync<IOException>(() => writer.WriteEntryAsync(entry));
                    }
                    else
                    {
                        Assert.Throws<IOException>(() => writer.WriteEntry(entry));
                    }
                }
                finally
                {
                    await DisposeTarWriter(writer, async);
                }
            }
        }

        [Theory]
        [InlineData(TarEntryFormat.V7)]
        [InlineData(TarEntryFormat.Ustar)]
        [InlineData(TarEntryFormat.Pax)]
        [InlineData(TarEntryFormat.Gnu)]
        public async Task Write_TwoEntries_With_UnseekableDataStreams(TarEntryFormat entryFormat)
        {
            foreach (bool async in Booleans)
            {
                byte[] expectedBytes = new byte[] { 0x1, 0x2, 0x3, 0x4, 0x5 };

                using MemoryStream internalDataStream1 = new();
                internalDataStream1.Write(expectedBytes.AsSpan());
                internalDataStream1.Position = 0;

                TarEntryType fileEntryType = GetTarEntryTypeForTarEntryFormat(TarEntryType.RegularFile, entryFormat);

                using WrappedStream unseekableDataStream1 = new(internalDataStream1, canRead: true, canWrite: false, canSeek: false);
                TarEntry entry1 = InvokeTarEntryCreationConstructor(entryFormat, fileEntryType, "file1.txt");
                entry1.DataStream = unseekableDataStream1;

                using MemoryStream internalDataStream2 = new();
                internalDataStream2.Write(expectedBytes.AsSpan());
                internalDataStream2.Position = 0;

                using WrappedStream unseekableDataStream2 = new(internalDataStream2, canRead: true, canWrite: false, canSeek: false);
                TarEntry entry2 = InvokeTarEntryCreationConstructor(entryFormat, fileEntryType, "file2.txt");
                entry2.DataStream = unseekableDataStream2;

                using MemoryStream archiveStream = new();
                TarWriter writer = CreateTarWriter(archiveStream, TarEntryFormat.Pax, leaveOpen: true);
                try
                {
                    await WriteEntry(writer, entry1, async);
                    await WriteEntry(writer, entry2, async);
                }
                finally
                {
                    await DisposeTarWriter(writer, async);
                }

                archiveStream.Position = 0;
                byte[] actualBytes = new byte[] { 0, 0, 0, 0, 0 };
                TarReader reader = CreateTarReader(archiveStream);
                try
                {
                    TarEntry readEntry = await GetNextEntry(reader, async: async);
                    Assert.NotNull(readEntry);
                    if (async)
                    {
                        await readEntry.DataStream.ReadExactlyAsync(actualBytes);
                    }
                    else
                    {
                        readEntry.DataStream.ReadExactly(actualBytes);
                    }
                    Assert.Equal(expectedBytes, actualBytes);

                    readEntry = await GetNextEntry(reader, async: async);
                    Assert.NotNull(readEntry);
                    if (async)
                    {
                        await readEntry.DataStream.ReadExactlyAsync(actualBytes);
                    }
                    else
                    {
                        readEntry.DataStream.ReadExactly(actualBytes);
                    }
                    Assert.Equal(expectedBytes, actualBytes);

                    Assert.Null(await GetNextEntry(reader, async: async));
                }
                finally
                {
                    await DisposeTarReader(reader, async);
                }
            }
        }
    }
}
