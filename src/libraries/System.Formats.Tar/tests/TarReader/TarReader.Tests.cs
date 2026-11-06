// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace System.Formats.Tar.Tests
{
    public partial class TarReader_Tests : TarTestsBase
    {
        [Fact]
        public void TarReader_NullArchiveStream() => Assert.Throws<ArgumentNullException>(() => new TarReader(archiveStream: null));

        [Fact]
        public void TarReader_UnreadableStream()
        {
            using MemoryStream ms = new MemoryStream();
            using WrappedStream ws = new WrappedStream(ms, canRead: false, canWrite: true, canSeek: true);
            Assert.Throws<ArgumentException>(() => new TarReader(ws));
        }

        [Theory]
        [MemberData(nameof(GetBooleanData))]
        public async Task TarReader_LeaveOpen_False(bool async)
        {
            using MemoryStream ms = GetTarMemoryStream(CompressionMethod.Uncompressed, TestTarFormat.pax, "many_small_files");
            List<Stream> dataStreams = new List<Stream>();
            {
                await using TarReaderHolder readerHolder = CreateTarReader(ms, async, leaveOpen: false);
                TarReader reader = readerHolder;

                TarEntry entry;
                while ((entry = await GetNextEntry(reader, async: async)) != null)
                {
                    if (entry.DataStream != null)
                    {
                        dataStreams.Add(entry.DataStream);
                    }
                }
            }

            Assert.Throws<ObjectDisposedException>(() => ms.ReadByte());

            Assert.True(dataStreams.Any());
            foreach (Stream ds in dataStreams)
            {
                Assert.Throws<ObjectDisposedException>(() => ds.ReadByte());
            }
        }

        [Theory]
        [MemberData(nameof(GetBooleanData))]
        public async Task TarReader_LeaveOpen_True(bool async)
        {
            using MemoryStream ms = GetTarMemoryStream(CompressionMethod.Uncompressed, TestTarFormat.pax, "many_small_files");
            List<Stream> dataStreams = new List<Stream>();
            {
                await using TarReaderHolder readerHolder = CreateTarReader(ms, async, leaveOpen: true);
                TarReader reader = readerHolder;

                TarEntry entry;
                while ((entry = await GetNextEntry(reader, async: async)) != null)
                {
                    if (entry.DataStream != null)
                    {
                        dataStreams.Add(entry.DataStream);
                    }
                }
            }

            ms.ReadByte(); // Should not throw

            Assert.True(dataStreams.Any());
            foreach (Stream ds in dataStreams)
            {
                ds.ReadByte(); // Should not throw
                ds.Dispose();
            }
        }

        [Theory]
        [MemberData(nameof(GetBooleanData))]
        public async Task TarReader_LeaveOpen_False_CopiedDataNotDisposed(bool async)
        {
            using MemoryStream ms = GetTarMemoryStream(CompressionMethod.Uncompressed, TestTarFormat.pax, "many_small_files");
            List<Stream> dataStreams = new List<Stream>();
            {
                await using TarReaderHolder readerHolder = CreateTarReader(ms, async, leaveOpen: false);
                TarReader reader = readerHolder;

                TarEntry entry;
                while ((entry = await GetNextEntry(reader, copyData: true, async: async)) != null)
                {
                    if (entry.DataStream != null)
                    {
                        dataStreams.Add(entry.DataStream);
                    }
                }
            }

            Assert.True(dataStreams.Any());
            foreach (Stream ds in dataStreams)
            {
                ds.ReadByte(); // Should not throw, copied streams, user should dispose
                ds.Dispose();
            }
        }

        [Theory]
        [MemberData(nameof(GetPaxExtendedAttributesRoundtripTestDataAndBooleanData))]
        public async Task PaxExtendedAttribute_Roundtrips(string key, string value, bool async)
        {
            // Verify the same roundtrip works with sync and async APIs
            using MemoryStream stream = new MemoryStream();
            {
                await using TarWriterHolder writerHolder = CreateTarWriter(stream, async, leaveOpen: true);
                TarWriter writer = writerHolder;

                await WriteEntry(writer, new PaxTarEntry(TarEntryType.Directory, "entryName", new Dictionary<string, string>() { { key, value } }), async);
            }

            stream.Position = 0;
            await using TarReaderHolder readerHolder = CreateTarReader(stream, async, leaveOpen: false);
            TarReader reader = readerHolder;

            PaxTarEntry entry = Assert.IsType<PaxTarEntry>(await GetNextEntry(reader, async: async));
            Assert.Equal(3, entry.ExtendedAttributes.Count);
            Assert.Contains(KeyValuePair.Create(key, value), entry.ExtendedAttributes);
            Assert.Null(await GetNextEntry(reader, async: async));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TarReader_InvalidChecksum_ThrowsException(bool corrupted)
        {
            // Create a simple tar file in memory
            using MemoryStream ms = new MemoryStream();
            using (TarWriter writer = new TarWriter(ms, TarEntryFormat.Ustar, leaveOpen: true))
            {
                UstarTarEntry entry = new UstarTarEntry(TarEntryType.RegularFile, "test.txt");
                writer.WriteEntry(entry);
            }

            // Reset position and get the bytes
            ms.Position = 0;
            byte[] tarData = ms.ToArray();

            // Corrupt the checksum field (starting at byte 148)
            // The checksum is written as an octal number in ASCII
            if (corrupted)
            {
                tarData[150] = (byte)'9'; // invalid digit
            }
            else
            {
                // increment the digit at position 150, wrapping around if necessary
                byte digit = (byte)(tarData[150] - (byte)'0');
                digit = (byte)((digit + 1) % 8);
                tarData[150] = (byte)('0' + digit);
            }

            // Create a new stream with corrupted data
            using MemoryStream corruptedStream = new MemoryStream(tarData);

            // Verify that reading the corrupted tar file throws an InvalidDataException
            using TarReader reader = new TarReader(corruptedStream);
            InvalidDataException exception = Assert.Throws<InvalidDataException>(() => reader.GetNextEntry());

            if (corrupted)
            {
                Assert.Contains("corrupted", exception.Message);
            }
            else
            {
                Assert.Contains("Checksum", exception.Message);
            }
        }

        public static IEnumerable<object[]> GnuBase256FieldOutOfRangeTestData()
        {
            const int ModeOffset = 100, MTimeOffset = 136, DevMajorOffset = 329, DevMinorOffset = 337, ATimeOffset = 345, CTimeOffset = 357;
            const char DirectoryTypeFlag = '5', CharacterDeviceTypeFlag = '3';

            // A positive base-256 value that is wider than the type the field is parsed as.
            byte[] tooLargeForInt32 = new byte[] { 0x80, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
            byte[] tooLargeForInt64 = new byte[] { 0x80, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };

            yield return new object[] { ModeOffset, tooLargeForInt32, DirectoryTypeFlag };
            // DevMajor and DevMinor are only read for character device and block device entries.
            yield return new object[] { DevMajorOffset, tooLargeForInt32, CharacterDeviceTypeFlag };
            yield return new object[] { DevMinorOffset, tooLargeForInt32, CharacterDeviceTypeFlag };
            yield return new object[] { MTimeOffset, tooLargeForInt64, DirectoryTypeFlag };
            yield return new object[] { ATimeOffset, tooLargeForInt64, DirectoryTypeFlag };
            yield return new object[] { CTimeOffset, tooLargeForInt64, DirectoryTypeFlag };

            // A timestamp that fits Int64 but is not a date DateTimeOffset can represent: 2^48 seconds.
            byte[] secondsOutOfDateTimeRange = new byte[] { 0x80, 0, 0, 0, 0, 0x01, 0, 0, 0, 0, 0, 0 };

            yield return new object[] { MTimeOffset, secondsOutOfDateTimeRange, DirectoryTypeFlag };
            yield return new object[] { ATimeOffset, secondsOutOfDateTimeRange, DirectoryTypeFlag };
            yield return new object[] { CTimeOffset, secondsOutOfDateTimeRange, DirectoryTypeFlag };
        }

        [Theory]
        [MemberData(nameof(GnuBase256FieldOutOfRangeTestData))]
        public void GnuBase256Field_OutOfRange_Throws(int offset, byte[] fieldBytes, char typeFlag)
        {
            byte[] tarData = CreateEntryWithRawFields((offset, fieldBytes), (TypeFlagOffset, new byte[] { (byte)typeFlag }));

            using TarReader reader = new TarReader(new MemoryStream(tarData));
            Assert.Throws<InvalidDataException>(() => reader.GetNextEntry());
        }

        [Theory]
        [MemberData(nameof(GnuBase256FieldOutOfRangeTestData))]
        public async Task GnuBase256Field_OutOfRange_Throws_Async(int offset, byte[] fieldBytes, char typeFlag)
        {
            byte[] tarData = CreateEntryWithRawFields((offset, fieldBytes), (TypeFlagOffset, new byte[] { (byte)typeFlag }));

            await using TarReader reader = new TarReader(new MemoryStream(tarData));
            await Assert.ThrowsAsync<InvalidDataException>(async () => await reader.GetNextEntryAsync());
        }

        public static IEnumerable<object[]> PaxNumericExtendedAttributeOutOfRangeTestData()
        {
            foreach (string field in new[] { "size", "mode", "devmajor", "devminor", "uid", "gid" })
            {
                yield return new object[] { field, "not-a-number" };
                yield return new object[] { field, "99999999999999999999" }; // Larger than Int64.MaxValue.
            }

            // Larger than Int32.MaxValue, which is what these fields are parsed as.
            yield return new object[] { "mode", "4294967296" };
            yield return new object[] { "devmajor", "4294967296" };
            yield return new object[] { "devminor", "4294967296" };

            // Larger than Int64.MaxValue.
            yield return new object[] { "size", "777777777777777777773" };

            // Timestamps that parse as a number but are not a date DateTimeOffset can represent.
            yield return new object[] { "mtime", "17930586772622498093" };
            yield return new object[] { "mtime", "253402300800" };  // One second past DateTime.MaxValue.
            yield return new object[] { "mtime", "-62135596801" };  // One second before DateTime.MinValue.
        }

        [Theory]
        [MemberData(nameof(PaxNumericExtendedAttributeOutOfRangeTestData))]
        public void PaxNumericExtendedAttribute_OutOfRange_Throws(string field, string value)
        {
            byte[] tarData = BuildRawPaxArchive("file.bin", "file.bin",
                extraEAs: new Dictionary<string, string> { [field] = value });

            using TarReader reader = new TarReader(new MemoryStream(tarData));
            Assert.Throws<InvalidDataException>(() => reader.GetNextEntry());
        }

        [Theory]
        [MemberData(nameof(PaxNumericExtendedAttributeOutOfRangeTestData))]
        public async Task PaxNumericExtendedAttribute_OutOfRange_Throws_Async(string field, string value)
        {
            byte[] tarData = BuildRawPaxArchive("file.bin", "file.bin",
                extraEAs: new Dictionary<string, string> { [field] = value });

            await using TarReader reader = new TarReader(new MemoryStream(tarData));
            await Assert.ThrowsAsync<InvalidDataException>(async () => await reader.GetNextEntryAsync());
        }

        [Theory]
        [InlineData("mtime", "253402300799")]  // DateTime.MaxValue, truncated to whole seconds.
        [InlineData("mtime", "-62135596800")]  // DateTime.MinValue.
        [InlineData("mtime", "1700000000.123456")]
        [InlineData("mode", "511")]
        [InlineData("devmajor", "2147483647")]
        [InlineData("size", "0")]
        public void PaxNumericExtendedAttribute_InRange_DoesNotThrow(string field, string value)
        {
            byte[] tarData = BuildRawPaxArchive("file.bin", "file.bin",
                extraEAs: new Dictionary<string, string> { [field] = value });

            using TarReader reader = new TarReader(new MemoryStream(tarData));
            Assert.NotNull(reader.GetNextEntry());
        }

        public static IEnumerable<object[]> GnuBase256UidGidTestData()
        {
            // Leading 0x80 byte: the remaining bytes are a positive big-endian value.
            yield return new object[] { new byte[] { 0x80, 0, 0, 0, 0x7F, 0xFF, 0xFF, 0xFF }, int.MaxValue };
            yield return new object[] { new byte[] { 0x80, 0, 0, 0, 0xB6, 0x5A, 0x65, 0x38 }, unchecked((int)0xB65A6538u) };
            yield return new object[] { new byte[] { 0x80, 0, 0, 0, 0xFF, 0x5A, 0x65, 0x38 }, unchecked((int)0xFF5A6538u) };
            yield return new object[] { new byte[] { 0x80, 0, 0, 0, 0xFF, 0xFF, 0xFF, 0xFF }, -1 };
            // Leading 0xFF byte: the field is a negative big-endian value.
            yield return new object[] { new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFE }, -2 };
        }

        [Theory]
        [MemberData(nameof(GnuBase256UidGidTestData))]
        public void GnuBase256UidGid_LargerThanInt32MaxValue_DoesNotThrow(byte[] fieldBytes, int expected)
        {
            byte[] tarData = CreateEntryWithRawUidGid(fieldBytes, fieldBytes);

            using TarReader reader = new TarReader(new MemoryStream(tarData));
            TarEntry entry = reader.GetNextEntry();
            Assert.NotNull(entry);
            Assert.Equal(expected, entry.Uid);
            Assert.Equal(expected, entry.Gid);
            Assert.Null(reader.GetNextEntry());
        }

        [Theory]
        [MemberData(nameof(GnuBase256UidGidTestData))]
        public async Task GnuBase256UidGid_LargerThanInt32MaxValue_DoesNotThrow_Async(byte[] fieldBytes, int expected)
        {
            byte[] tarData = CreateEntryWithRawUidGid(fieldBytes, fieldBytes);

            await using TarReader reader = new TarReader(new MemoryStream(tarData));
            TarEntry entry = await reader.GetNextEntryAsync();
            Assert.NotNull(entry);
            Assert.Equal(expected, entry.Uid);
            Assert.Equal(expected, entry.Gid);
            Assert.Null(await reader.GetNextEntryAsync());
        }

        [Theory]
        [InlineData(new byte[] { 0x80, 0, 0, 1, 0, 0, 0, 0 })] // uint.MaxValue + 1
        [InlineData(new byte[] { 0x80, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF })]
        [InlineData(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0x7F, 0xFF, 0xFF, 0xFF })] // int.MinValue - 1
        public void GnuBase256UidGid_LargerThan32Bits_Throws(byte[] fieldBytes)
        {
            byte[] tarData = CreateEntryWithRawUidGid(fieldBytes, fieldBytes);

            using TarReader reader = new TarReader(new MemoryStream(tarData));
            Assert.Throws<InvalidDataException>(() => reader.GetNextEntry());
        }

        [Fact]
        public void PaxUidGid_LargerThan32Bits_Throws()
        {
            // Write a valid 10 digit uid, then replace it in the extended attributes with one larger than uint.MaxValue.
            MemoryStream stream = new MemoryStream();
            using (TarWriter writer = new TarWriter(stream, leaveOpen: true))
            {
                writer.WriteEntry(new PaxTarEntry(TarEntryType.Directory, "dir", new Dictionary<string, string>() { { "uid", "3059377464" } }));
            }

            byte[] tarData = stream.ToArray();
            byte[] original = System.Text.Encoding.ASCII.GetBytes("uid=3059377464");
            int index = tarData.AsSpan().IndexOf(original);
            Assert.True(index >= 0);
            System.Text.Encoding.ASCII.GetBytes("uid=9999999999").CopyTo(tarData, index);

            using TarReader reader = new TarReader(new MemoryStream(tarData));
            Assert.Throws<InvalidDataException>(() => reader.GetNextEntry());
        }

        [Theory]
        [InlineData("2147483647", int.MaxValue)]
        [InlineData("3059377464", unchecked((int)3059377464u))]
        [InlineData("4294967295", -1)]
        public void PaxUidGid_LargerThanInt32MaxValue_DoesNotThrow(string value, int expected)
        {
            MemoryStream stream = new MemoryStream();
            using (TarWriter writer = new TarWriter(stream, leaveOpen: true))
            {
                writer.WriteEntry(new PaxTarEntry(TarEntryType.Directory, "dir", new Dictionary<string, string>() { { "uid", value }, { "gid", value } }));
            }

            stream.Position = 0;
            using TarReader reader = new TarReader(stream);
            PaxTarEntry entry = Assert.IsType<PaxTarEntry>(reader.GetNextEntry());
            Assert.Equal(expected, entry.Uid);
            Assert.Equal(expected, entry.Gid);
            Assert.Equal(value, entry.ExtendedAttributes["uid"]);
            Assert.Equal(value, entry.ExtendedAttributes["gid"]);
        }

        private const int TypeFlagOffset = 156;

        // Writes a GNU entry, then overwrites its uid and gid fields with the given raw bytes and fixes up the checksum.
        private static byte[] CreateEntryWithRawUidGid(byte[] uid, byte[] gid)
        {
            const int UidOffset = 108, GidOffset = 116;
            return CreateEntryWithRawFields((UidOffset, uid), (GidOffset, gid));
        }

        // Writes a GNU entry, then overwrites the specified header fields with the given raw bytes and fixes up the checksum.
        private static byte[] CreateEntryWithRawFields(params (int Offset, byte[] Value)[] fields)
        {
            const int ChecksumOffset = 148, ChecksumLength = 8;

            MemoryStream ms = new MemoryStream();
            using (TarWriter writer = new TarWriter(ms, TarEntryFormat.Gnu, leaveOpen: true))
            {
                writer.WriteEntry(new GnuTarEntry(TarEntryType.Directory, "dir"));
            }

            byte[] tarData = ms.ToArray();
            foreach ((int offset, byte[] value) in fields)
            {
                value.CopyTo(tarData, offset);
            }

            // The checksum is computed with the checksum field itself filled with spaces.
            tarData.AsSpan(ChecksumOffset, ChecksumLength).Fill((byte)' ');
            int checksum = 0;
            for (int i = 0; i < 512; i++)
            {
                checksum += tarData[i];
            }
            System.Text.Encoding.ASCII.GetBytes(Convert.ToString(checksum, 8).PadLeft(6, '0') + "\0 ").CopyTo(tarData, ChecksumOffset);

            return tarData;
        }
    }
}
