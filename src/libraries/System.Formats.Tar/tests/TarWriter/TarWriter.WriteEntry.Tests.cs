﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using Xunit;

namespace System.Formats.Tar.Tests
{
    // Tests that are independent of the archive format.
    public class TarWriter_WriteEntry_Tests : TarWriter_WriteEntry_Base
    {
        [Fact]
        public void WriteEntry_AfterDispose_Throws()
        {
            using MemoryStream archiveStream = new MemoryStream();
            TarWriter writer = new TarWriter(archiveStream);
            writer.Dispose();

            PaxTarEntry entry = new PaxTarEntry(TarEntryType.RegularFile, InitialEntryName);
            Assert.Throws<ObjectDisposedException>(() => writer.WriteEntry(entry));
        }


        [Theory]
        [InlineData(TarEntryFormat.V7)]
        [InlineData(TarEntryFormat.Ustar)]
        [InlineData(TarEntryFormat.Pax)]
        [InlineData(TarEntryFormat.Gnu)]
        public void WriteEntry_FromUnseekableStream_AdvanceDataStream_WriteFromThatPosition(TarEntryFormat format)
        {
            using MemoryStream source = GetTarMemoryStream(CompressionMethod.Uncompressed, TestTarFormat.ustar, "file");
            using WrappedStream unseekable = new WrappedStream(source, canRead: true, canWrite: true, canSeek: false);

            using MemoryStream destination = new MemoryStream();
            using (TarReader reader1 = new TarReader(unseekable))
            {
                TarEntry entry = reader1.GetNextEntry();
                Assert.NotNull(entry);
                Assert.NotNull(entry.DataStream);
                entry.DataStream.ReadByte(); // Advance one byte, now the expected string would be "ello file"

                using (TarWriter writer = new TarWriter(destination, TarEntryFormat.Ustar, leaveOpen: true))
                {
                    writer.WriteEntry(entry);
                    TarEntry dirEntry = InvokeTarEntryCreationConstructor(format, TarEntryType.Directory, "dir");
                    writer.WriteEntry(dirEntry); // To validate that next entry is not affected
                }
            }

            destination.Seek(0, SeekOrigin.Begin);
            using (TarReader reader2 = new TarReader(destination))
            {
                TarEntry entry = reader2.GetNextEntry();
                Assert.NotNull(entry);
                Assert.NotNull(entry.DataStream);

                using (StreamReader streamReader = new StreamReader(entry.DataStream, leaveOpen: true))
                {
                    string contents = streamReader.ReadLine();
                    Assert.Equal("ello file", contents);
                }

                TarEntry dirEntry = reader2.GetNextEntry();
                Assert.NotNull(dirEntry);
                Assert.Equal(format, dirEntry.Format);
                Assert.Equal(TarEntryType.Directory, dirEntry.EntryType);
                Assert.Equal("dir", dirEntry.Name);

                Assert.Null(reader2.GetNextEntry());
            }
        }

        [Theory]
        [InlineData(TarEntryFormat.V7)]
        [InlineData(TarEntryFormat.Ustar)]
        [InlineData(TarEntryFormat.Pax)]
        [InlineData(TarEntryFormat.Gnu)]
        public void WriteEntry_RespectDefaultWriterFormat(TarEntryFormat expectedFormat)
        {
            using TempDirectory root = new TempDirectory();

            string path = Path.Join(root.Path, "file.txt");
            File.Create(path).Dispose();

            using MemoryStream archiveStream = new MemoryStream();
            using (TarWriter writer = new TarWriter(archiveStream, expectedFormat, leaveOpen: true))
            {
                writer.WriteEntry(path, "file.txt");
            }

            archiveStream.Position = 0;
            using (TarReader reader = new TarReader(archiveStream, leaveOpen: false))
            {
                TarEntry entry = reader.GetNextEntry();
                Assert.Equal(expectedFormat, entry.Format);

                Type expectedType = GetTypeForFormat(expectedFormat);

                Assert.Equal(expectedType, entry.GetType());
            }
        }

        [Theory]
        [InlineData(TarEntryFormat.Ustar)]
        [InlineData(TarEntryFormat.Pax)]
        [InlineData(TarEntryFormat.Gnu)]
        public void Write_RegularFileEntry_In_V7Writer(TarEntryFormat entryFormat)
        {
            using MemoryStream archive = new MemoryStream();
            using (TarWriter writer = new TarWriter(archive, format: TarEntryFormat.V7, leaveOpen: true))
            {
                TarEntry entry = entryFormat switch
                {
                    TarEntryFormat.Ustar => new UstarTarEntry(TarEntryType.RegularFile, InitialEntryName),
                    TarEntryFormat.Pax => new PaxTarEntry(TarEntryType.RegularFile, InitialEntryName),
                    TarEntryFormat.Gnu => new GnuTarEntry(TarEntryType.RegularFile, InitialEntryName),
                    _ => throw new InvalidDataException($"Unexpected format: {entryFormat}")
                };

                // Should be written in the format of the entry
                writer.WriteEntry(entry);
            }

            archive.Seek(0, SeekOrigin.Begin);
            using (TarReader reader = new TarReader(archive))
            {
                TarEntry entry = reader.GetNextEntry();
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

                Assert.Null(reader.GetNextEntry());
            }
        }

        [Theory]
        [InlineData(TarEntryFormat.Ustar)]
        [InlineData(TarEntryFormat.Pax)]
        [InlineData(TarEntryFormat.Gnu)]
        public void Write_V7RegularFileEntry_In_OtherFormatsWriter(TarEntryFormat writerFormat)
        {
            using MemoryStream archive = new MemoryStream();
            using (TarWriter writer = new TarWriter(archive, format: writerFormat, leaveOpen: true))
            {
                V7TarEntry entry = new V7TarEntry(TarEntryType.V7RegularFile, InitialEntryName);

                // Should be written in the format of the entry
                writer.WriteEntry(entry);
            }

            archive.Seek(0, SeekOrigin.Begin);
            using (TarReader reader = new TarReader(archive))
            {
                TarEntry entry = reader.GetNextEntry();
                Assert.NotNull(entry);
                Assert.Equal(TarEntryFormat.V7, entry.Format);
                Assert.True(entry is V7TarEntry);

                Assert.Null(reader.GetNextEntry());
            }
        }

        [Theory]
        [InlineData(TarEntryFormat.V7)]
        [InlineData(TarEntryFormat.Ustar)]
        [InlineData(TarEntryFormat.Pax)]
        [InlineData(TarEntryFormat.Gnu)]
        public void ReadAndWriteMultipleGlobalExtendedAttributesEntries(TarEntryFormat format)
        {
            Dictionary<string, string> attrs = new Dictionary<string, string>()
            {
                { "hello", "world" },
                { "dotnet", "runtime" }
            };

            using MemoryStream archiveStream = new MemoryStream();
            using (TarWriter writer = new TarWriter(archiveStream, leaveOpen: true))
            {
                PaxGlobalExtendedAttributesTarEntry gea1 = new PaxGlobalExtendedAttributesTarEntry(attrs);
                writer.WriteEntry(gea1);

                TarEntry entry1 = InvokeTarEntryCreationConstructor(format, TarEntryType.Directory, "dir1");
                writer.WriteEntry(entry1);

                PaxGlobalExtendedAttributesTarEntry gea2 = new PaxGlobalExtendedAttributesTarEntry(attrs);
                writer.WriteEntry(gea2);

                TarEntry entry2 = InvokeTarEntryCreationConstructor(format, TarEntryType.Directory,  "dir2");
                writer.WriteEntry(entry2);
            }

            archiveStream.Position = 0;

            using (TarReader reader = new TarReader(archiveStream, leaveOpen: false))
            {
                VerifyGlobalExtendedAttributesEntry(reader.GetNextEntry(), attrs);
                VerifyDirectory(reader.GetNextEntry(), format, "dir1");
                VerifyGlobalExtendedAttributesEntry(reader.GetNextEntry(), attrs);
                VerifyDirectory(reader.GetNextEntry(), format, "dir2");
                Assert.Null(reader.GetNextEntry());
            }
        }

        // Y2K38 will happen one second after "2038/19/01 03:14:07 +00:00". This timestamp represents the seconds since the Unix epoch with a
        // value of int.MaxValue: 2,147,483,647.
        // The fixed size fields for mtime, atime and ctime can fit 12 ASCII characters, but the last character is reserved for an ASCII space.
        // All our entry types should survive the Epochalypse because we internally use long to represent the seconds since Unix epoch, not int.
        // So if the max allowed value is 77,777,777,777 in octal, then the max allowed seconds since the Unix epoch are 8,589,934,591, which
        // is way past int MaxValue, but still within the long limits. That number represents the date "2242/16/03 12:56:32 +00:00".
        [Theory]
        [InlineData(TarEntryFormat.V7)]
        [InlineData(TarEntryFormat.Ustar)]
        [InlineData(TarEntryFormat.Gnu)]
        public void WriteTimestampsBeyondEpochalypse(TarEntryFormat format)
        {
            DateTimeOffset epochalypse = new DateTimeOffset(2038, 1, 19, 3, 14, 8, TimeSpan.Zero); // One second past Y2K38
            TarEntry entry = InvokeTarEntryCreationConstructor(format, TarEntryType.Directory, "dir");

            entry.ModificationTime = epochalypse;
            Assert.Equal(epochalypse, entry.ModificationTime);

            if (entry is GnuTarEntry gnuEntry)
            {
                gnuEntry.AccessTime = epochalypse;
                Assert.Equal(epochalypse, gnuEntry.AccessTime);

                gnuEntry.ChangeTime = epochalypse;
                Assert.Equal(epochalypse, gnuEntry.ChangeTime);
            }

            using MemoryStream archiveStream = new MemoryStream();
            using (TarWriter writer = new TarWriter(archiveStream, leaveOpen: true))
            {
                writer.WriteEntry(entry);
            }

            archiveStream.Position = 0;
            using (TarReader reader = new TarReader(archiveStream))
            {
                TarEntry readEntry = reader.GetNextEntry();
                Assert.NotNull(readEntry);

                Assert.Equal(epochalypse, readEntry.ModificationTime);

                if (readEntry is GnuTarEntry gnuReadEntry)
                {
                    Assert.Equal(epochalypse, gnuReadEntry.AccessTime);
                    Assert.Equal(epochalypse, gnuReadEntry.ChangeTime);
                }
            }
        }

        // The fixed size fields for mtime, atime and ctime can fit 12 ASCII characters, but the last character is reserved for an ASCII space.
        // We internally use long to represent the seconds since Unix epoch, not int.
        // If the max allowed value is 77,777,777,777 in octal, then the max allowed seconds since the Unix epoch are 8,589,934,591,
        // which represents the date "2242/03/16 12:56:32 +00:00".
        // V7, Ustar and GNU would not survive after this date because they only have the fixed size fields to store timestamps.
        [Theory]
        [InlineData(TarEntryFormat.V7)]
        [InlineData(TarEntryFormat.Ustar)]
        [InlineData(TarEntryFormat.Gnu)]
        public void WriteTimestampsBeyondOctalLimit(TarEntryFormat format)
        {
            DateTimeOffset overLimitTimestamp = new DateTimeOffset(2242, 3, 16, 12, 56, 33, TimeSpan.Zero); // One second past the octal limit

            TarEntry entry = InvokeTarEntryCreationConstructor(format, TarEntryType.Directory, "dir");

            // Before writing the entry, the timestamps should have no issue
            entry.ModificationTime = overLimitTimestamp;
            Assert.Equal(overLimitTimestamp, entry.ModificationTime);

            if (entry is GnuTarEntry gnuEntry)
            {
                gnuEntry.AccessTime = overLimitTimestamp;
                Assert.Equal(overLimitTimestamp, gnuEntry.AccessTime);

                gnuEntry.ChangeTime = overLimitTimestamp;
                Assert.Equal(overLimitTimestamp, gnuEntry.ChangeTime);
            }

            using MemoryStream archiveStream = new MemoryStream();
            using (TarWriter writer = new TarWriter(archiveStream, leaveOpen: true))
            {
                writer.WriteEntry(entry);
            }

            archiveStream.Position = 0;
            using (TarReader reader = new TarReader(archiveStream))
            {
                TarEntry readEntry = reader.GetNextEntry();
                Assert.NotNull(readEntry);

                // The timestamps get stored as '{1970-01-01 12:00:00 AM +00:00}' due to the +1 overflow
                Assert.NotEqual(overLimitTimestamp, readEntry.ModificationTime);

                if (readEntry is GnuTarEntry gnuReadEntry)
                {
                    Assert.NotEqual(overLimitTimestamp, gnuReadEntry.AccessTime);
                    Assert.NotEqual(overLimitTimestamp, gnuReadEntry.ChangeTime);
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
                TarEntryType entryType = format == TarEntryFormat.V7 ? TarEntryType.V7RegularFile : TarEntryType.RegularFile;
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
                if (format is TarEntryFormat.V7) // V7 truncates names at 100 characters.
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
        public void WriteEntry_TooLongName_Throws(TarEntryFormat entryFormat, TarEntryType entryType, string name)
        {
            using TarWriter writer = new(new MemoryStream());

            TarEntry entry = InvokeTarEntryCreationConstructor(entryFormat, entryType, name);
            Assert.Throws<ArgumentException>("entry", () => writer.WriteEntry(entry));
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
        public void WriteEntry_TooLongLinkName_Throws(TarEntryFormat entryFormat, TarEntryType entryType, string linkName)
        {
            using TarWriter writer = new(new MemoryStream());

            TarEntry entry = InvokeTarEntryCreationConstructor(entryFormat, entryType, "foo");
            entry.LinkName = linkName;

            Assert.Throws<ArgumentException>("entry", () => writer.WriteEntry(entry));
        }

        public static IEnumerable<object[]> WriteEntry_TooLongUserGroupName_Throws_TheoryData()
        {
            // Not testing Pax as it supports unlimited size uname/gname.
            foreach (TarEntryFormat entryFormat in new[] { TarEntryFormat.Ustar, TarEntryFormat.Gnu })
            {
                // Last character doesn't fit fully.
                yield return new object[] { entryFormat, Repeat(OneByteCharacter, 32 + 1) };
                yield return new object[] { entryFormat, Repeat(TwoBytesCharacter, 32 / 2 + 1) };
                yield return new object[] { entryFormat, Repeat(FourBytesCharacter, 32 / 4 + 1) };

                // Last character doesn't fit by one byte.
                yield return new object[] { entryFormat, Repeat(TwoBytesCharacter, 32 - 2 + 1) + TwoBytesCharacter };
                yield return new object[] { entryFormat, Repeat(FourBytesCharacter, 32 - 4 + 1) + FourBytesCharacter };
            }
        }

        [Theory]
        [MemberData(nameof(WriteEntry_TooLongUserGroupName_Throws_TheoryData))]
        public void WriteEntry_TooLongUserName_Throws(TarEntryFormat entryFormat, string userName)
        {
            using TarWriter writer = new(new MemoryStream());

            TarEntry entry = InvokeTarEntryCreationConstructor(entryFormat, TarEntryType.RegularFile, "foo");
            PosixTarEntry posixEntry = Assert.IsAssignableFrom<PosixTarEntry>(entry);
            posixEntry.UserName = userName;

            Assert.Throws<ArgumentException>("entry", () => writer.WriteEntry(entry));
        }

        [Theory]
        [MemberData(nameof(WriteEntry_TooLongUserGroupName_Throws_TheoryData))]
        public void WriteEntry_TooLongGroupName_Throws(TarEntryFormat entryFormat, string groupName)
        {
            using TarWriter writer = new(new MemoryStream());

            TarEntry entry = InvokeTarEntryCreationConstructor(entryFormat, TarEntryType.RegularFile, "foo");
            PosixTarEntry posixEntry = Assert.IsAssignableFrom<PosixTarEntry>(entry);
            posixEntry.GroupName = groupName;

            Assert.Throws<ArgumentException>("entry", () => writer.WriteEntry(entry));
        }

        public static IEnumerable<object[]> WriteEntry_UsingTarEntry_FromTarReader_IntoTarWriter_TheoryData()
        {
            foreach (var entryFormat in new[] { TarEntryFormat.V7, TarEntryFormat.Ustar, TarEntryFormat.Pax, TarEntryFormat.Gnu })
            {
                foreach (var entryType in new[] { entryFormat == TarEntryFormat.V7 ? TarEntryType.V7RegularFile : TarEntryType.RegularFile, TarEntryType.Directory, TarEntryType.SymbolicLink })
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
        public void WriteEntry_UsingTarEntry_FromTarReader_IntoTarWriter(TarEntryFormat entryFormat, TarEntryType entryType, bool unseekableStream)
        {
            MemoryStream msSource = new();
            MemoryStream msDestination = new();

            WriteTarArchiveWithOneEntry(msSource, entryFormat, entryType);
            msSource.Position = 0;

            Stream source = new WrappedStream(msSource, msSource.CanRead, msSource.CanWrite, canSeek: !unseekableStream);
            Stream destination = new WrappedStream(msDestination, msDestination.CanRead, msDestination.CanWrite, canSeek: !unseekableStream);

            using (TarReader reader = new(source))
            using (TarWriter writer = new(destination))
            {
                TarEntry entry;
                while ((entry = reader.GetNextEntry()) != null)
                {
                    writer.WriteEntry(entry);
                }
            }

            AssertExtensions.SequenceEqual(msSource.ToArray(), msDestination.ToArray());
        }

        [Theory]
        [InlineData(TarEntryFormat.V7, false)]
        [InlineData(TarEntryFormat.Ustar, false)]
        [InlineData(TarEntryFormat.Gnu, false)]
        [InlineData(TarEntryFormat.V7, true)]
        [InlineData(TarEntryFormat.Ustar, true)]
        [InlineData(TarEntryFormat.Gnu, true)]
        public void WriteEntry_FileSizeOverLegacyLimit_Throws(TarEntryFormat entryFormat, bool unseekableStream)
        {
            const long FileSizeOverLimit = LegacyMaxFileSize + 1;

            using MemoryStream ms = new();
            using Stream s = unseekableStream ? new WrappedStream(ms, ms.CanRead, ms.CanWrite, canSeek: false) : ms;

            using TarWriter writer = new(s);
            TarEntry writeEntry = InvokeTarEntryCreationConstructor(entryFormat, entryFormat is TarEntryFormat.V7 ? TarEntryType.V7RegularFile : TarEntryType.RegularFile, "foo");
            writeEntry.DataStream = new SimulatedDataStream(FileSizeOverLimit);

            Assert.Equal(FileSizeOverLimit, writeEntry.Length);

            Assert.Throws<ArgumentException>(() => writer.WriteEntry(writeEntry));
        }

        [Theory]
        [InlineData(TarEntryFormat.V7)]
        [InlineData(TarEntryFormat.Ustar)]
        [InlineData(TarEntryFormat.Pax)]
        [InlineData(TarEntryFormat.Gnu)]
        public void WritingUnseekableDataStream_To_UnseekableArchiveStream_Throws(TarEntryFormat entryFormat)
        {
            using MemoryStream internalDataStream = new();
            using WrappedStream unseekableDataStream = new(internalDataStream, canRead: true, canWrite: false, canSeek: false);

            using MemoryStream internalArchiveStream = new();
            using WrappedStream unseekableArchiveStream = new(internalArchiveStream, canRead: true, canWrite: true, canSeek: false);

            using TarWriter writer = new(unseekableArchiveStream);
            TarEntry entry = InvokeTarEntryCreationConstructor(entryFormat, GetTarEntryTypeForTarEntryFormat(TarEntryType.RegularFile, entryFormat), "file.txt");
            entry.DataStream = unseekableDataStream;
            Assert.Throws<IOException>(() => writer.WriteEntry(entry));
        }

        [Theory]
        [InlineData(TarEntryFormat.V7)]
        [InlineData(TarEntryFormat.Ustar)]
        [InlineData(TarEntryFormat.Pax)]
        [InlineData(TarEntryFormat.Gnu)]
        public void Write_TwoEntries_With_UnseekableDataStreams(TarEntryFormat entryFormat)
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
            using (TarWriter writer = new(archiveStream, leaveOpen: true))
            {
                writer.WriteEntry(entry1); // Should not throw
                writer.WriteEntry(entry2); // To verify that second entry is written in correct place
            }

            // Verify
            archiveStream.Position = 0;
            byte[] actualBytes = new byte[] { 0, 0, 0, 0, 0 };
            using (TarReader reader = new(archiveStream))
            {
                TarEntry readEntry = reader.GetNextEntry();
                Assert.NotNull(readEntry);
                readEntry.DataStream.ReadExactly(actualBytes);
                Assert.Equal(expectedBytes, actualBytes);

                readEntry = reader.GetNextEntry();
                Assert.NotNull(readEntry);
                readEntry.DataStream.ReadExactly(actualBytes);
                Assert.Equal(expectedBytes, actualBytes);

                Assert.Null(reader.GetNextEntry());
            }
        }
    }
}
