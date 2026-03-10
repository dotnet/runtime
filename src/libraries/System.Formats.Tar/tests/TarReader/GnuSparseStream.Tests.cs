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
    /// <summary>
    /// Unit tests for GnuSparseStream behavior. Since GnuSparseStream is internal,
    /// it is exercised through TarReader's public DataStream property using
    /// programmatically constructed PAX 1.0 sparse archives.
    /// </summary>
    public class GnuSparseStreamTests : TarTestsBase
    {
        // Builds a PAX 1.0 sparse archive in memory and returns a TarEntry whose
        // DataStream is a GnuSparseStream. segments is an array of (virtualOffset, length)
        // pairs and rawPackedData is the concatenated packed bytes for all segments.
        private static (MemoryStream archive, byte[] rawPackedData) BuildSparseArchive(
            string realName, long realSize,
            (long Offset, long Length)[] segments)
        {
            // Build the sparse map text: numSegs\n, then pairs offset\n length\n
            var sb = new StringBuilder();
            sb.Append(segments.Length).Append('\n');
            foreach (var (off, len) in segments)
            {
                sb.Append(off).Append('\n');
                sb.Append(len).Append('\n');
            }
            byte[] mapText = Encoding.ASCII.GetBytes(sb.ToString());

            // Pad to the next 512-byte block boundary, then append placeholder packed data.
            int padding = (512 - (mapText.Length % 512)) % 512;
            long totalPackedBytes = 0;
            foreach (var (_, len) in segments) totalPackedBytes += len;

            byte[] rawSparseData = new byte[mapText.Length + padding + totalPackedBytes];
            mapText.CopyTo(rawSparseData, 0);

            // Fill each segment's packed data with its 1-based segment index value.
            int writePos = mapText.Length + padding;
            for (int i = 0; i < segments.Length; i++)
            {
                for (long j = 0; j < segments[i].Length; j++)
                {
                    rawSparseData[writePos++] = (byte)(i + 1);
                }
            }

            var gnuSparseAttributes = new Dictionary<string, string>
            {
                ["GNU.sparse.major"] = "1",
                ["GNU.sparse.minor"] = "0",
                ["GNU.sparse.name"] = realName,
                ["GNU.sparse.realsize"] = realSize.ToString(),
            };

            string placeholderName = "GNUSparseFile.0/" + realName;
            var archive = new MemoryStream();
            using (var writer = new TarWriter(archive, TarEntryFormat.Pax, leaveOpen: true))
            {
                var entry = new PaxTarEntry(TarEntryType.RegularFile, placeholderName, gnuSparseAttributes);
                entry.DataStream = new MemoryStream(rawSparseData);
                writer.WriteEntry(entry);
            }
            archive.Position = 0;
            return (archive, rawSparseData[(mapText.Length + padding)..]);
        }

        // Reads the DataStream of the first entry from the given archive and returns it.
        private static Stream GetSparseDataStream(MemoryStream archiveStream, bool copyData)
        {
            archiveStream.Position = 0;
            var reader = new TarReader(archiveStream);
            TarEntry? entry = reader.GetNextEntry(copyData);
            Assert.NotNull(entry);
            Assert.NotNull(entry.DataStream);
            return entry.DataStream;
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void SingleSegmentAtStart_NoHoles(bool copyData)
        {
            // Virtual file: [0..511] = data (0x01), no trailing hole.
            var segments = new[] { (0L, 512L) };
            var (archive, _) = BuildSparseArchive("file.bin", 512, segments);

            using var dataStream = GetSparseDataStream(archive, copyData);

            Assert.Equal(512L, dataStream.Length);
            byte[] buf = new byte[512];
            dataStream.ReadExactly(buf);
            Assert.All(buf, b => Assert.Equal(1, b));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void SingleSegmentInMiddle_LeadingAndTrailingHoles(bool copyData)
        {
            // Virtual file (1024 bytes):
            //   [0..255]   = zeros (leading hole)
            //   [256..511] = data (0x01)
            //   [512..1023] = zeros (trailing hole)
            var segments = new[] { (256L, 256L) };
            var (archive, _) = BuildSparseArchive("file.bin", 1024, segments);

            using var dataStream = GetSparseDataStream(archive, copyData);

            Assert.Equal(1024L, dataStream.Length);
            byte[] buf = new byte[1024];
            dataStream.ReadExactly(buf);

            for (int i = 0; i < 256; i++) Assert.Equal(0, buf[i]);
            for (int i = 256; i < 512; i++) Assert.Equal(1, buf[i]);
            for (int i = 512; i < 1024; i++) Assert.Equal(0, buf[i]);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void MultipleSegmentsWithHolesInBetween(bool copyData)
        {
            // Virtual file (2048 bytes):
            //   [0..255]    = data seg 0 (0x01)
            //   [256..511]  = hole
            //   [512..767]  = data seg 1 (0x02)
            //   [768..1023] = hole
            //   [1024..1279] = data seg 2 (0x03)
            //   [1280..2047] = hole
            var segments = new[] { (0L, 256L), (512L, 256L), (1024L, 256L) };
            var (archive, _) = BuildSparseArchive("file.bin", 2048, segments);

            using var dataStream = GetSparseDataStream(archive, copyData);

            Assert.Equal(2048L, dataStream.Length);
            byte[] buf = new byte[2048];
            dataStream.ReadExactly(buf);

            for (int i = 0; i < 256; i++) Assert.Equal(1, buf[i]);
            for (int i = 256; i < 512; i++) Assert.Equal(0, buf[i]);
            for (int i = 512; i < 768; i++) Assert.Equal(2, buf[i]);
            for (int i = 768; i < 1024; i++) Assert.Equal(0, buf[i]);
            for (int i = 1024; i < 1280; i++) Assert.Equal(3, buf[i]);
            for (int i = 1280; i < 2048; i++) Assert.Equal(0, buf[i]);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void PartialReadsProduceSameResultAsFullRead(bool copyData)
        {
            // Read the data in small (13-byte) chunks and verify it matches a full read.
            var segments = new[] { (0L, 256L), (512L, 256L), (1024L, 256L) };
            var (archive1, _) = BuildSparseArchive("file.bin", 2048, segments);
            var (archive2, _) = BuildSparseArchive("file.bin", 2048, segments);

            byte[] fullRead = new byte[2048];
            using (var s = GetSparseDataStream(archive1, copyData))
                s.ReadExactly(fullRead);

            byte[] partialRead = new byte[2048];
            using var chunkedStream = GetSparseDataStream(archive2, copyData);
            int pos = 0;
            while (pos < 2048)
            {
                int chunk = Math.Min(13, 2048 - pos);
                int read = chunkedStream.Read(partialRead, pos, chunk);
                Assert.True(read > 0);
                pos += read;
            }

            Assert.Equal(fullRead, partialRead);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void AllHoles_ReadsAsAllZeros(bool copyData)
        {
            // Virtual file: 1000 bytes, one segment at offset=1000 length=0 (nothing packed).
            var segments = new[] { (1000L, 0L) };
            var (archive, _) = BuildSparseArchive("file.bin", 1000, segments);

            using var dataStream = GetSparseDataStream(archive, copyData);

            Assert.Equal(1000L, dataStream.Length);
            byte[] buf = new byte[1000];
            dataStream.ReadExactly(buf);
            Assert.All(buf, b => Assert.Equal(0, b));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ReadAtEndReturnsZero(bool copyData)
        {
            var segments = new[] { (0L, 512L) };
            var (archive, _) = BuildSparseArchive("file.bin", 512, segments);

            using var dataStream = GetSparseDataStream(archive, copyData);

            // Read the whole stream.
            byte[] buf = new byte[512];
            dataStream.ReadExactly(buf);

            // Any further read should return 0.
            int read = dataStream.Read(buf, 0, buf.Length);
            Assert.Equal(0, read);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task SingleSegmentInMiddle_Async(bool copyData)
        {
            var segments = new[] { (256L, 256L) };
            var (archive, _) = BuildSparseArchive("file.bin", 1024, segments);

            using var dataStream = GetSparseDataStream(archive, copyData);

            Assert.Equal(1024L, dataStream.Length);
            byte[] buf = new byte[1024];
            await dataStream.ReadExactlyAsync(buf, CancellationToken.None);

            for (int i = 0; i < 256; i++) Assert.Equal(0, buf[i]);
            for (int i = 256; i < 512; i++) Assert.Equal(1, buf[i]);
            for (int i = 512; i < 1024; i++) Assert.Equal(0, buf[i]);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task MultipleSegments_Async(bool copyData)
        {
            var segments = new[] { (0L, 256L), (512L, 256L), (1024L, 256L) };
            var (archive, _) = BuildSparseArchive("file.bin", 2048, segments);

            using var dataStream = GetSparseDataStream(archive, copyData);

            byte[] buf = new byte[2048];
            await dataStream.ReadExactlyAsync(buf, CancellationToken.None);

            for (int i = 0; i < 256; i++) Assert.Equal(1, buf[i]);
            for (int i = 256; i < 512; i++) Assert.Equal(0, buf[i]);
            for (int i = 512; i < 768; i++) Assert.Equal(2, buf[i]);
            for (int i = 768; i < 1024; i++) Assert.Equal(0, buf[i]);
            for (int i = 1024; i < 1280; i++) Assert.Equal(3, buf[i]);
            for (int i = 1280; i < 2048; i++) Assert.Equal(0, buf[i]);
        }

        [Fact]
        public void SeekableStream_SeekAndRead()
        {
            // Build a seekable archive (from MemoryStream) and verify random access.
            var segments = new[] { (0L, 256L), (512L, 256L), (1024L, 256L) };
            var (archive, _) = BuildSparseArchive("file.bin", 2048, segments);

            using var dataStream = GetSparseDataStream(archive, copyData: false);

            if (!dataStream.CanSeek)
            {
                return; // Only test on seekable streams.
            }

            // Seek to segment 1 (offset 512) and read.
            dataStream.Seek(512, SeekOrigin.Begin);
            byte[] buf = new byte[256];
            dataStream.ReadExactly(buf);
            Assert.All(buf, b => Assert.Equal(2, b));

            // Seek to segment 0 (offset 0) and read.
            dataStream.Seek(0, SeekOrigin.Begin);
            dataStream.ReadExactly(buf);
            Assert.All(buf, b => Assert.Equal(1, b));

            // Seek into a hole.
            dataStream.Seek(300, SeekOrigin.Begin);
            int read = dataStream.Read(buf, 0, 10);
            Assert.True(read > 0);
            for (int i = 0; i < read; i++) Assert.Equal(0, buf[i]);
        }

        [Fact]
        public void AdvancePastEntry_DoesNotCorruptNextEntry()
        {
            // Write two entries in a PAX archive: first a sparse entry, then a regular entry.
            // Verify that after reading the first entry without consuming its DataStream,
            // the second entry is still readable with correct content.
            const string RegularName = "regular.txt";
            byte[] regularContent = Encoding.UTF8.GetBytes("Hello, world!");

            var segments = new[] { (0L, 256L) };
            string sparseMapText = "1\n0\n256\n";
            byte[] sparseMapBytes = Encoding.ASCII.GetBytes(sparseMapText);
            byte[] packedData = new byte[256];
            Array.Fill<byte>(packedData, 0x42);
            byte[] rawSparseData = new byte[512 + 256];
            sparseMapBytes.CopyTo(rawSparseData, 0);
            packedData.CopyTo(rawSparseData, 512);

            var gnuSparseAttributes = new Dictionary<string, string>
            {
                ["GNU.sparse.major"] = "1",
                ["GNU.sparse.minor"] = "0",
                ["GNU.sparse.name"] = "sparse.bin",
                ["GNU.sparse.realsize"] = "256",
            };

            var archive = new MemoryStream();
            using (var writer = new TarWriter(archive, TarEntryFormat.Pax, leaveOpen: true))
            {
                var sparseEntry = new PaxTarEntry(TarEntryType.RegularFile, "GNUSparseFile.0/sparse.bin", gnuSparseAttributes);
                sparseEntry.DataStream = new MemoryStream(rawSparseData);
                writer.WriteEntry(sparseEntry);

                var regularEntry = new PaxTarEntry(TarEntryType.RegularFile, RegularName);
                regularEntry.DataStream = new MemoryStream(regularContent);
                writer.WriteEntry(regularEntry);
            }

            archive.Position = 0;
            using var reader = new TarReader(archive);

            // Read the sparse entry but don't consume its DataStream.
            TarEntry? first = reader.GetNextEntry(copyData: false);
            Assert.NotNull(first);
            Assert.Equal("sparse.bin", first.Name);

            // Read the next entry without having consumed the sparse DataStream.
            TarEntry? second = reader.GetNextEntry(copyData: false);
            Assert.NotNull(second);
            Assert.Equal(RegularName, second.Name);

            Assert.NotNull(second.DataStream);
            byte[] buf = new byte[regularContent.Length];
            second.DataStream.ReadExactly(buf);
            Assert.Equal(regularContent, buf);
        }
    }
}
