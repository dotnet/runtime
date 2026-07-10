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
            TarReader reader = CreateTarReader(ms, leaveOpen: false);
            try
            {
                TarEntry entry;
                while ((entry = await GetNextEntry(reader, async: async)) != null)
                {
                    if (entry.DataStream != null)
                    {
                        dataStreams.Add(entry.DataStream);
                    }
                }
            }
            finally
            {
                await DisposeTarReader(reader, async);
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
            TarReader reader = CreateTarReader(ms, leaveOpen: true);
            try
            {
                TarEntry entry;
                while ((entry = await GetNextEntry(reader, async: async)) != null)
                {
                    if (entry.DataStream != null)
                    {
                        dataStreams.Add(entry.DataStream);
                    }
                }
            }
            finally
            {
                await DisposeTarReader(reader, async);
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
            TarReader reader = CreateTarReader(ms, leaveOpen: false);
            try
            {
                TarEntry entry;
                while ((entry = await GetNextEntry(reader, copyData: true, async: async)) != null)
                {
                    if (entry.DataStream != null)
                    {
                        dataStreams.Add(entry.DataStream);
                    }
                }
            }
            finally
            {
                await DisposeTarReader(reader, async);
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
        public async Task PaxExtendedAttribute_Roundtrips(string key, string value)
        {
            var stream = new MemoryStream();
            using (var writer = new TarWriter(stream, leaveOpen: true))
            {
                writer.WriteEntry(new PaxTarEntry(TarEntryType.Directory, "entryName", new Dictionary<string, string>() { { key, value } }));
            }

            stream.Position = 0;
            using (var reader = new TarReader(stream, leaveOpen: true))
            {
                PaxTarEntry entry = Assert.IsType<PaxTarEntry>(reader.GetNextEntry());
                Assert.Equal(3, entry.ExtendedAttributes.Count);
                Assert.Contains(KeyValuePair.Create(key, value), entry.ExtendedAttributes);
                Assert.Null(reader.GetNextEntry());
            }

            // Verify the same roundtrip works with async APIs
            stream.Position = 0;
            await using (var reader = new TarReader(stream))
            {
                PaxTarEntry entry = Assert.IsType<PaxTarEntry>(await reader.GetNextEntryAsync());
                Assert.Equal(3, entry.ExtendedAttributes.Count);
                Assert.Contains(KeyValuePair.Create(key, value), entry.ExtendedAttributes);
                Assert.Null(await reader.GetNextEntryAsync());
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
    }
}
