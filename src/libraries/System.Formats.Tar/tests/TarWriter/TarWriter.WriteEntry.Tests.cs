// Licensed to the .NET Foundation under one or more agreements.
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

        [Fact]
        public void WriteEntry_FromUnseekableStream_AdvanceDataStream_WriteFromThatPosition()
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
                    _ => throw new FormatException($"Unexpected format: {entryFormat}")
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
    }
}
