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

        [Fact]
        public void TarReader_LeaveOpen_False()
        {
            using MemoryStream ms = GetTarMemoryStream(CompressionMethod.Uncompressed, TestTarFormat.pax, "many_small_files");
            List<Stream> dataStreams = new List<Stream>();
            using (TarReader reader = new TarReader(ms, leaveOpen: false))
            {
                TarEntry entry;
                while ((entry = reader.GetNextEntry()) != null)
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

        [Fact]
        public void TarReader_LeaveOpen_True()
        {
            using MemoryStream ms = GetTarMemoryStream(CompressionMethod.Uncompressed, TestTarFormat.pax, "many_small_files");
            List<Stream> dataStreams = new List<Stream>();
            using (TarReader reader = new TarReader(ms, leaveOpen: true))
            {
                TarEntry entry;
                while ((entry = reader.GetNextEntry()) != null)
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

        [Fact]
        public void TarReader_LeaveOpen_False_CopiedDataNotDisposed()
        {
            using MemoryStream ms = GetTarMemoryStream(CompressionMethod.Uncompressed, TestTarFormat.pax, "many_small_files");
            List<Stream> dataStreams = new List<Stream>();
            using (TarReader reader = new TarReader(ms, leaveOpen: false))
            {
                TarEntry entry;
                while ((entry = reader.GetNextEntry(copyData: true)) != null)
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
        [MemberData(nameof(GetPaxExtendedAttributesRoundtripTestData))]
        public void PaxExtendedAttribute_Roundtrips(string key, string value)
        {
            var stream = new MemoryStream();
            using (var writer = new TarWriter(stream, leaveOpen: true))
            {
                writer.WriteEntry(new PaxTarEntry(TarEntryType.Directory, "entryName", new Dictionary<string, string>() { { key, value } }));
            }

            stream.Position = 0;
            using (var reader = new TarReader(stream))
            {
                PaxTarEntry entry = Assert.IsType<PaxTarEntry>(reader.GetNextEntry());
                Assert.Equal(3, entry.ExtendedAttributes.Count);
                Assert.Contains(KeyValuePair.Create(key, value), entry.ExtendedAttributes);
                Assert.Null(reader.GetNextEntry());
            }
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

        // Writes a GNU entry, then overwrites its uid and gid fields with the given raw bytes and fixes up the checksum.
        private static byte[] CreateEntryWithRawUidGid(byte[] uid, byte[] gid)
        {
            const int UidOffset = 108, GidOffset = 116, ChecksumOffset = 148, FieldLength = 8;

            MemoryStream ms = new MemoryStream();
            using (TarWriter writer = new TarWriter(ms, TarEntryFormat.Gnu, leaveOpen: true))
            {
                writer.WriteEntry(new GnuTarEntry(TarEntryType.Directory, "dir"));
            }

            byte[] tarData = ms.ToArray();
            uid.CopyTo(tarData, UidOffset);
            gid.CopyTo(tarData, GidOffset);

            // The checksum is computed with the checksum field itself filled with spaces.
            tarData.AsSpan(ChecksumOffset, FieldLength).Fill((byte)' ');
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
