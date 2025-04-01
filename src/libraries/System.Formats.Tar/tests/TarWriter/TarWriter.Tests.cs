// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Linq;
using Xunit;

namespace System.Formats.Tar.Tests
{
    public partial class TarWriter_Tests : TarTestsBase
    {
        [Fact]
        public void Constructors_NullStream()
        {
            Assert.Throws<ArgumentNullException>(() => new TarWriter(archiveStream: null));
            Assert.Throws<ArgumentNullException>(() => new TarWriter(archiveStream: null, TarEntryFormat.V7));
        }

        [Fact]
        public void Constructors_LeaveOpen()
        {
            using MemoryStream archiveStream = new MemoryStream();

            TarWriter writer1 = new TarWriter(archiveStream, TarEntryFormat.Pax, leaveOpen: true);
            writer1.Dispose();
            archiveStream.WriteByte(0); // Should succeed because stream was not closed

            TarWriter writer2 = new TarWriter(archiveStream, TarEntryFormat.Pax, leaveOpen: false);
            writer2.Dispose();
            Assert.Throws<ObjectDisposedException>(() => archiveStream.WriteByte(0)); // Should fail because stream was closed
        }

        [Fact]
        public void Constructor_Format()
        {
            using MemoryStream archiveStream = new MemoryStream();

            using TarWriter writerDefault = new TarWriter(archiveStream, TarEntryFormat.Pax, leaveOpen: true);
            Assert.Equal(TarEntryFormat.Pax, writerDefault.Format);

            using TarWriter writerV7 = new TarWriter(archiveStream, TarEntryFormat.V7, leaveOpen: true);
            Assert.Equal(TarEntryFormat.V7, writerV7.Format);

            using TarWriter writerUstar = new TarWriter(archiveStream, TarEntryFormat.Ustar, leaveOpen: true);
            Assert.Equal(TarEntryFormat.Ustar, writerUstar.Format);

            using TarWriter writerPax = new TarWriter(archiveStream, TarEntryFormat.Pax, leaveOpen: true);
            Assert.Equal(TarEntryFormat.Pax, writerPax.Format);

            using TarWriter writerGnu = new TarWriter(archiveStream, TarEntryFormat.Gnu, leaveOpen: true);
            Assert.Equal(TarEntryFormat.Gnu, writerGnu.Format);

            using TarWriter writerNoFormat = new TarWriter(archiveStream, leaveOpen: true);
            Assert.Equal(TarEntryFormat.Pax, writerNoFormat.Format);

            Assert.Throws<ArgumentOutOfRangeException>(() => new TarWriter(archiveStream, TarEntryFormat.Unknown));
            Assert.Throws<ArgumentOutOfRangeException>(() => new TarWriter(archiveStream, (TarEntryFormat)int.MinValue));
            Assert.Throws<ArgumentOutOfRangeException>(() => new TarWriter(archiveStream, (TarEntryFormat)int.MaxValue));
        }

        [Fact]
        public void Constructors_UnwritableStream_Throws()
        {
            using MemoryStream archiveStream = new MemoryStream();
            using WrappedStream wrappedStream = new WrappedStream(archiveStream, canRead: true, canWrite: false, canSeek: false);
            Assert.Throws<ArgumentException>(() => new TarWriter(wrappedStream));
            Assert.Throws<ArgumentException>(() => new TarWriter(wrappedStream, TarEntryFormat.V7));
        }

        [Fact]
        public void Constructor_NoEntryInsertion_WritesNothing()
        {
            using MemoryStream archiveStream = new MemoryStream();
            TarWriter writer = new TarWriter(archiveStream, TarEntryFormat.Pax, leaveOpen: true);
            writer.Dispose(); // No entries inserted, should write no empty records
            Assert.Equal(0, archiveStream.Length);
        }

        [Fact]
        public void Write_To_UnseekableStream()
        {
            using MemoryStream inner = new MemoryStream();
            using WrappedStream wrapped = new WrappedStream(inner, canRead: true, canWrite: true, canSeek: false);

            using (TarWriter writer = new TarWriter(wrapped, TarEntryFormat.Pax, leaveOpen: true))
            {
                PaxTarEntry paxEntry = new PaxTarEntry(TarEntryType.RegularFile, "file.txt");
                writer.WriteEntry(paxEntry);
            } // The final records should get written, and the length should not be set because position cannot be read

            inner.Seek(0, SeekOrigin.Begin); // Rewind the base stream (wrapped cannot be rewound)

            using (TarReader reader = new TarReader(wrapped))
            {
                TarEntry entry = reader.GetNextEntry();
                Assert.Equal(TarEntryFormat.Pax, entry.Format);
                Assert.Equal(TarEntryType.RegularFile, entry.EntryType);
                Assert.Null(reader.GetNextEntry());
            }
        }

        private readonly DateTimeOffset TimestampForChecksum = new DateTimeOffset(2022, 1, 2, 3, 45, 00, TimeSpan.Zero);

        [Theory]
        [InlineData(TarEntryFormat.V7)]
        [InlineData(TarEntryFormat.Ustar)]
        [InlineData(TarEntryFormat.Pax)]
        [InlineData(TarEntryFormat.Gnu)]
        public void Verify_Checksum_RegularFile(TarEntryFormat format) =>
            Verify_Checksum_Internal(
                format,
                // Convert to V7RegularFile if format is V7
                GetTarEntryTypeForTarEntryFormat(TarEntryType.RegularFile, format),
                longLink: false,
                longPath: false);

        [Theory] // V7 does not support BlockDevice
        [InlineData(TarEntryFormat.Ustar)]
        [InlineData(TarEntryFormat.Pax)]
        [InlineData(TarEntryFormat.Gnu)]
        public void Verify_Checksum_BlockDevice(TarEntryFormat format) =>
            Verify_Checksum_Internal(format, TarEntryType.BlockDevice, longPath: false, longLink: false);

        [Theory]
        [InlineData(TarEntryFormat.V7)]
        [InlineData(TarEntryFormat.Ustar)]
        [InlineData(TarEntryFormat.Pax)]
        [InlineData(TarEntryFormat.Gnu)]
        public void Verify_Checksum_Directory_LongPath(TarEntryFormat format) =>
            Verify_Checksum_Internal(format, TarEntryType.Directory, longPath: true, longLink: false);

        [Theory]
        [InlineData(TarEntryFormat.V7)]
        [InlineData(TarEntryFormat.Ustar)]
        [InlineData(TarEntryFormat.Pax)]
        [InlineData(TarEntryFormat.Gnu)]
        public void Verify_Checksum_SymbolicLink_LongLink(TarEntryFormat format) =>
            Verify_Checksum_Internal(format, TarEntryType.SymbolicLink, longPath: false, longLink: true);

        [Theory]
        [InlineData(TarEntryFormat.V7)]
        [InlineData(TarEntryFormat.Ustar)]
        [InlineData(TarEntryFormat.Pax)]
        [InlineData(TarEntryFormat.Gnu)]
        public void Verify_Checksum_SymbolicLink_LongLink_LongPath(TarEntryFormat format) =>
            Verify_Checksum_Internal(format, TarEntryType.SymbolicLink, longPath: true, longLink: true);

        [Fact]
        public void Verify_Size_RegularFile_Empty()
        {
            using MemoryStream archiveStream = new();
            string entryName = "entry.txt";
            using (TarWriter archive = new(archiveStream, TarEntryFormat.V7, leaveOpen: true))
            {
                V7TarEntry e = new(TarEntryType.V7RegularFile, entryName)
                {
                    DataStream = new MemoryStream(0)
                };
                archive.WriteEntry(e);
            }

            int sizeLocation = 100 + // Name
                               8 +   // Mode
                               8 +   // Uid
                               8;    // Gid
            int sizeLength = 12;

            archiveStream.Position = 0;
            byte[] actual = archiveStream.GetBuffer()[sizeLocation..(sizeLocation + sizeLength)];

            byte[] expected = [0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0];
            AssertExtensions.SequenceEqual(expected, actual);

            archiveStream.Position = 0;
            using TarReader reader = new(archiveStream);

            TarEntry? actualEntry = reader.GetNextEntry();
            Assert.NotNull(actualEntry);
            Assert.Equal(0, actualEntry.Length);
            Assert.Null(actualEntry.DataStream); // No stream created when size field's value is 0
        }

        [Fact]
        public void Verify_Compatibility_RegularFile_EmptyFile_NoSizeStored()
        {
            // Filling archiveStream contents without depending on the Tar implementation.
            // The contents of the archiveStream are equivalent to creating the archive using TarWriter with this code:

            // // using MemoryStream archiveStream = new();
            // // string entryName = "entry.txt";
            // // using (TarWriter archive = new(archiveStream, TarEntryFormat.V7, leaveOpen: true))
            // // {
            // //     V7TarEntry e = new(TarEntryType.V7RegularFile, entryName)
            // //     {
            // //         DataStream = new MemoryStream(0)
            // //     };
            // //     archive.WriteEntry(e);
            // // }
            using MemoryStream archiveStream = new(Convert.FromBase64String("ZW50cnkudHh0AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAADAwMDA2NDQAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAADE0NzMwMzQ0MjQwADA1MTM2AAAgAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"));

            int sizeLocation = 100 + // Name
                               8 +   // Mode
                               8 +   // Uid
                               8;    // Gid

            // Fill the size field with 12 zeros as we used to before the bug fix
            byte[] replacement = [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0];
            archiveStream.Seek(sizeLocation, SeekOrigin.Begin);
            archiveStream.Write(replacement);

            archiveStream.Position = 0;
            using TarReader reader = new(archiveStream);

            TarEntry? actualEntry = reader.GetNextEntry(); // Should succeed to read the entry with a malformed size field value
            Assert.NotNull(actualEntry);
            Assert.Equal(0, actualEntry.Length); // Should succeed to detect the size field's value as zero
            Assert.Null(actualEntry.DataStream); // No stream created when size field's value is 0
        }

        private void Verify_Checksum_Internal(TarEntryFormat format, TarEntryType entryType, bool longPath, bool longLink)
        {
            using MemoryStream archive = new MemoryStream();
            int expectedChecksum;
            using (TarWriter writer = new TarWriter(archive, format, leaveOpen: true))
            {
                TarEntry entry = CreateTarEntryAndGetExpectedChecksum(format, entryType, longPath, longLink, out expectedChecksum);
                writer.WriteEntry(entry);
                Assert.Equal(expectedChecksum, entry.Checksum);
            }

            archive.Seek(0, SeekOrigin.Begin);
            using (TarReader reader = new TarReader(archive))
            {
                TarEntry entry = reader.GetNextEntry();
                Assert.Equal(expectedChecksum, entry.Checksum);
            }
        }

        private TarEntry CreateTarEntryAndGetExpectedChecksum(TarEntryFormat format, TarEntryType entryType, bool longPath, bool longLink, out int expectedChecksum)
        {
            expectedChecksum = 0;

            expectedChecksum += GetNameChecksum(format, longPath, out string entryName);

            TarEntry entry = InvokeTarEntryCreationConstructor(format, entryType, entryName);

            if (entryType is TarEntryType.SymbolicLink)
            {
                expectedChecksum += GetLinkChecksum(format, longLink, out string linkName);
                entry.LinkName = linkName;
            }

            expectedChecksum += GetChecksumForCommonFields(entry, entryType);
            expectedChecksum += GetChecksumForFormatSpecificFields(entry, format);

            return entry;
        }

        private int GetNameChecksum(TarEntryFormat format, bool longPath, out string entryName)
        {
            int expectedChecksum = 0;
            if (!longPath)
            {
                // 'a.b' = 97 + 46 + 98 = 241
                entryName = "a.b";
                expectedChecksum += 241;
            }
            else
            {
                entryName = new string('a', 100);
                expectedChecksum += 9700; // 100 * 97 = 9700 (first 100 bytes go into 'name' field)

                // V7 does not support name fields larger than 100
                if (format is not TarEntryFormat.V7)
                {
                    entryName += "/" + new string('a', 50);
                }

                // Gnu and Pax writes first 100 bytes in 'name' field, then the full name is written in a metadata entry that precedes this one.
                if (format is TarEntryFormat.Ustar)
                {
                    // Ustar can write the directory into prefix.
                    expectedChecksum += 4850; // 50 * 97 = 4850
                }
            }
            return expectedChecksum;
        }

        private int GetLinkChecksum(TarEntryFormat format, bool longLink, out string linkName)
        {
            int expectedChecksum = 0;
            if (!longLink)
            {
                // 'a.b' = 97 + 46 + 98 = 241
                linkName = "a.b";
                expectedChecksum += 241;
            }
            else
            {
                linkName = new string('a', 100); // 100 * 97 = 9700 (first 100 bytes go into 'linkName' field)
                expectedChecksum += 9700;

                // V7 and Ustar does not support name fields larger than 100
                // Pax and Gnu write first 100 bytes in 'linkName' field, then the full link name is written in the
                // preceding metadata entry (extended attributes for PAX, LongLink for GNU).
                if (format is not TarEntryFormat.V7 and not TarEntryFormat.Ustar)
                {
                    linkName += "/" + new string('a', 50);
                }
            }
            return expectedChecksum;

        }

        private int GetChecksumForCommonFields(TarEntry entry, TarEntryType entryType)
        {
            // Add 8 spaces to the sum: (8 x 32) = 256
            int expectedChecksum = 256;

            // '0000744\0' = 48 + 48 + 48 + 48 + 55 + 52 + 52 + 0 = 351
            entry.Mode = AssetMode; // octal 744 => u+rxw, g+r, o+r
            expectedChecksum += 351;

            // '0017351\0' = 48 + 48 + 49 + 55 + 51 + 53 + 49 + 0 = 353
            entry.Uid = AssetUid; // 7913 (octal 17351)
            expectedChecksum += 353;

            // '0006773\0' = 48 + 48 + 48 + 54 + 55 + 55 + 51 + 0 = 359
            entry.Gid = AssetGid; // 3579 (octal 6773)
            expectedChecksum += 359;

            // '14164217674\0' = 49 + 52 + 49 + 54 + 52 + 50 + 49 + 55 + 54 + 55 + 52 + 0 = 571
            DateTimeOffset mtime = TimestampForChecksum; // ToUnixTimeSeconds() = 1641095100 (octal 14164217674)
            entry.ModificationTime = mtime;
            expectedChecksum += 571;

            if (entryType is TarEntryType.RegularFile or TarEntryType.V7RegularFile)
            {
                entry.DataStream = new MemoryStream();
                byte[] buffer = new byte[] { 72, 101, 108, 108, 111 }; // values don't matter, only length (5)

                // '0000000005\0' = 48 + 48 + 48 + 48 + 48 + 48 + 48 + 48 + 48 + 48 + 53 + 0 = 533
                entry.DataStream.Write(buffer);
                entry.DataStream.Seek(0, SeekOrigin.Begin); // Rewind to ensure it gets written from the beginning
                expectedChecksum += 533;
            }

            // If V7 regular file:            '\0' = 0
            // If Ustar/Pax/Gnu regular file: '0'  = 48
            // If block device:               '4'  = 52
            expectedChecksum += (byte)entryType;

            // Checksum so far: 256 + 351 + 353 + 359 + 571 = decimal 1890
            // If V7RegularFile: 1890 + 533 + 0  = 2423 (octal 4567) => '004567\0'
            // If RegularFile:   1890 + 533 + 48 = 2471 (octal 4647) => '004647\0'
            // If BlockDevice:   1890 + 0   + 52 = 1942 (octal 3626) => '003626\0'
            return expectedChecksum;
        }

        private int GetChecksumForFormatSpecificFields(TarEntry entry, TarEntryFormat format)
        {
            int checksum = 0;
            switch (format)
            {
                case TarEntryFormat.Ustar:
                case TarEntryFormat.Pax:
                    // Magic: 'ustar\0' = 117 + 115 + 116 + 97 + 114 + 0 = 559
                    checksum += 559;
                    // Version: '00' = 48 + 48 = 96
                    checksum += 96;
                    // Total: 655
                    break;
                case TarEntryFormat.Gnu:
                    // Magic: 'ustar ' = 117 + 115 + 116 + 97 + 114 + 32 = 591
                    checksum += 591;
                    // Version: ' \0' = 32 + 0 = 32
                    checksum += 32;
                    // Total: 623
                    break;
            }

            if (entry is PosixTarEntry posixEntry)
            {
                // 'user' = 117 + 115 + 101 + 114 = 447
                posixEntry.UserName = TestUName;
                checksum += 447;
                // 'group' = 103 + 114 + 111 + 117 + 112 = 557
                posixEntry.GroupName = TestGName;
                checksum += 557;
                // Total: 1004

                if (posixEntry.EntryType is TarEntryType.BlockDevice)
                {
                    // '0000075\0' = 48 + 48 + 48 + 48 + 48 + 55 + 53 + 0 = 348
                    posixEntry.DeviceMajor = TestBlockDeviceMajor; // 61 (octal 75)
                    checksum += 348;
                    // '0000101\0' = 48 + 48 + 48 + 48 + 49 + 48 + 49 + 0 = 338
                    posixEntry.DeviceMinor = TestBlockDeviceMinor; // 65 (octal 101)
                    checksum += 338;
                    // Total: 686
                }

                if (posixEntry is GnuTarEntry gnuEntry)
                {
                    // '14164217674\0' = 49 + 52 + 49 + 54 + 52 + 50 + 49 + 55 + 54 + 55 + 52 + 0 = 571
                    gnuEntry.AccessTime = TimestampForChecksum; // ToUnixTimeSeconds() = decimal 1641095100, octal 14164217674;
                    checksum += 571;

                    // '14164217674\0' = 49 + 52 + 49 + 54 + 52 + 50 + 49 + 55 + 54 + 55 + 52 + 0 = 571
                    gnuEntry.ChangeTime = TimestampForChecksum; // ToUnixTimeSeconds() = decimal 1641095100, octal 14164217674;
                    checksum += 571;
                    // Total: 1142
                }
            }

            // Totals:
            // V7RegularFile: 0
            // Ustar RegularFile: 655 + 1004 = 1659
            // Pax RegularFile: 655 + 1004 = 1659
            // Gnu RegularFile: 623 + 1004 + 1142 = 2769
            // Ustar BlockDevice: 655 + 1004 + 686 = 2345
            // Pax BlockDevice: 655 + 1004 + 686 = 2345
            // Gnu BlockDevice: 623 + 1004 + 686 + 1142 = 3455
            return checksum;
        }
    }
}
