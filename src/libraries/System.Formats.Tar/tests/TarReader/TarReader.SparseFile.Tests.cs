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
    /// Tests for GNU sparse format 1.0 (PAX) reading. Since GnuSparseStream is internal,
    /// it is exercised through TarReader's public DataStream property using
    /// programmatically constructed PAX 1.0 sparse archives.
    /// </summary>
    public class TarReader_SparseFileTests : TarTestsBase
    {
        // Builds a PAX 1.0 sparse archive in memory and returns a TarEntry whose
        // DataStream is a GnuSparseStream. segments is an array of (virtualOffset, length)
        // pairs; packed data for each segment is filled with its 1-based index value.
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

        // Builds a raw archive byte array where the sparse map text is injected directly,
        // bypassing TarWriter validation. Used to construct malformed archives.
        private static MemoryStream BuildRawSparseArchive(string sparseMapContent, string realName, long realSize)
        {
            byte[] mapBytes = Encoding.ASCII.GetBytes(sparseMapContent);
            int padding = (512 - (mapBytes.Length % 512)) % 512;
            byte[] rawData = new byte[mapBytes.Length + padding];
            mapBytes.CopyTo(rawData, 0);

            var gnuSparseAttributes = new Dictionary<string, string>
            {
                ["GNU.sparse.major"] = "1",
                ["GNU.sparse.minor"] = "0",
                ["GNU.sparse.name"] = realName,
                ["GNU.sparse.realsize"] = realSize.ToString(),
            };

            var archive = new MemoryStream();
            using (var writer = new TarWriter(archive, TarEntryFormat.Pax, leaveOpen: true))
            {
                var entry = new PaxTarEntry(TarEntryType.RegularFile, "GNUSparseFile.0/" + realName, gnuSparseAttributes);
                entry.DataStream = new MemoryStream(rawData);
                writer.WriteEntry(entry);
            }
            archive.Position = 0;
            return archive;
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

        // --- Corrupted format tests ---

        [Theory]
        [InlineData("abc\n0\n256\n")]        // non-numeric segment count
        [InlineData("\n0\n256\n")]            // empty segment count line
        [InlineData("1\nabc\n256\n")]         // non-numeric offset
        [InlineData("1\n0\nabc\n")]           // non-numeric length
        [InlineData("1\n-1\n256\n")]          // negative offset
        [InlineData("1\n0\n-1\n")]            // negative length
        [InlineData("1\n0\n")]                // truncated: missing length line
        [InlineData("1\n")]                   // truncated: missing offset and length lines
        [InlineData("2\n" + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" + "\n256\n")] // line exceeding buffer capacity
        public void CorruptedSparseMap_InvalidDataException(string sparseMapContent)
        {
            var archive = BuildRawSparseArchive(sparseMapContent, "file.bin", 1024);
            using var reader = new TarReader(archive);
            Assert.Throws<InvalidDataException>(() => reader.GetNextEntry(copyData: false));
        }

        [Theory]
        [InlineData("abc\n0\n256\n")]
        [InlineData("\n0\n256\n")]
        [InlineData("1\nabc\n256\n")]
        [InlineData("1\n0\nabc\n")]
        [InlineData("1\n-1\n256\n")]
        [InlineData("1\n0\n-1\n")]
        [InlineData("1\n0\n")]
        [InlineData("1\n")]
        [InlineData("2\n" + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" + "\n256\n")]
        public async Task CorruptedSparseMap_InvalidDataException_Async(string sparseMapContent)
        {
            var archive = BuildRawSparseArchive(sparseMapContent, "file.bin", 1024);
            using var reader = new TarReader(archive);
            await Assert.ThrowsAsync<InvalidDataException>(() => reader.GetNextEntryAsync(copyData: false).AsTask());
        }

        [Fact]
        public void CorruptedSparseMap_TruncatedAfterSegmentCount_InvalidDataException()
        {
            // The sparse map contains the segment count but is truncated before the offset/length.
            // The data section has 512 bytes (so _size > 0 and the sparse stream will be created),
            // but the map text ends before providing the required offset and length lines.
            string sparseMapContent = "1\n"; // claims 1 segment but provides neither offset nor length
            var archive = BuildRawSparseArchive(sparseMapContent, "file.bin", 1024);
            using var reader = new TarReader(archive);
            Assert.Throws<InvalidDataException>(() => reader.GetNextEntry(copyData: false));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void MissingSparseAttributes_EntryReadAsNormal(bool copyData)
        {
            // An entry with GNU.sparse.major but no GNU.sparse.minor should NOT be treated
            // as sparse format 1.0. It should be read as a plain regular file.
            byte[] content = Encoding.ASCII.GetBytes("plain content");

            var attributes = new Dictionary<string, string>
            {
                ["GNU.sparse.major"] = "1",
                // GNU.sparse.minor intentionally omitted
                ["GNU.sparse.name"] = "real.bin",
                ["GNU.sparse.realsize"] = "1024",
            };

            var archive = new MemoryStream();
            using (var writer = new TarWriter(archive, TarEntryFormat.Pax, leaveOpen: true))
            {
                var entry = new PaxTarEntry(TarEntryType.RegularFile, "GNUSparseFile.0/real.bin", attributes);
                entry.DataStream = new MemoryStream(content);
                writer.WriteEntry(entry);
            }
            archive.Position = 0;

            using var reader = new TarReader(archive);
            TarEntry? e = reader.GetNextEntry(copyData);
            Assert.NotNull(e);
            // Without both major=1 and minor=0, the entry is NOT treated as sparse 1.0:
            // the name is overridden by GNU.sparse.name but the DataStream is not wrapped.
            Assert.Equal("real.bin", e.Name);
            Assert.NotNull(e.DataStream);
            byte[] buf = new byte[content.Length];
            e.DataStream.ReadExactly(buf);
            Assert.Equal(content, buf);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void WrongMajorMinor_EntryReadAsNormal(bool copyData)
        {
            // GNU.sparse.major=2 (not 1.0) should not trigger sparse expansion.
            byte[] content = Encoding.ASCII.GetBytes("plain content");

            var attributes = new Dictionary<string, string>
            {
                ["GNU.sparse.major"] = "2",
                ["GNU.sparse.minor"] = "0",
                ["GNU.sparse.name"] = "real.bin",
                ["GNU.sparse.realsize"] = "1024",
            };

            var archive = new MemoryStream();
            using (var writer = new TarWriter(archive, TarEntryFormat.Pax, leaveOpen: true))
            {
                var entry = new PaxTarEntry(TarEntryType.RegularFile, "GNUSparseFile.0/real.bin", attributes);
                entry.DataStream = new MemoryStream(content);
                writer.WriteEntry(entry);
            }
            archive.Position = 0;

            using var reader = new TarReader(archive);
            TarEntry? e = reader.GetNextEntry(copyData);
            Assert.NotNull(e);
            Assert.Equal("real.bin", e.Name);
            Assert.NotNull(e.DataStream);
            byte[] buf = new byte[content.Length];
            e.DataStream.ReadExactly(buf);
            Assert.Equal(content, buf);
        }
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void GnuSparse10Pax_DataStreamExpandsSparseSections(bool copyData)
        {
            // Virtual file layout (realsize=2048):
            //   [0..255]     = sparse hole (zeros)
            //   [256..511]   = segment 0 data (0x42)
            //   [512..767]   = sparse hole (zeros)
            //   [768..1023]  = segment 1 data (0x43)
            //   [1024..2047] = sparse hole (zeros, trailing)
            const string PlaceholderName = "GNUSparseFile.0/realfile.txt";
            const string RealName = "realfile.txt";
            const long RealSize = 2048;
            const long Seg0Offset = 256, Seg0Length = 256;
            const long Seg1Offset = 768, Seg1Length = 256;

            byte[] packedData0 = new byte[Seg0Length];
            Array.Fill<byte>(packedData0, 0x42);
            byte[] packedData1 = new byte[Seg1Length];
            Array.Fill<byte>(packedData1, 0x43);

            byte[] mapText = Encoding.ASCII.GetBytes("2\n256\n256\n768\n256\n");
            byte[] rawSparseData = new byte[512 + Seg0Length + Seg1Length];
            mapText.CopyTo(rawSparseData, 0);
            packedData0.CopyTo(rawSparseData, 512);
            packedData1.CopyTo(rawSparseData, 512 + (int)Seg0Length);

            var gnuSparseAttributes = new Dictionary<string, string>
            {
                ["GNU.sparse.major"] = "1",
                ["GNU.sparse.minor"] = "0",
                ["GNU.sparse.name"] = RealName,
                ["GNU.sparse.realsize"] = RealSize.ToString(),
            };

            using var archive = new MemoryStream();
            using (var writer = new TarWriter(archive, TarEntryFormat.Pax, leaveOpen: true))
            {
                var entry = new PaxTarEntry(TarEntryType.RegularFile, PlaceholderName, gnuSparseAttributes);
                entry.DataStream = new MemoryStream(rawSparseData);
                writer.WriteEntry(entry);
            }

            archive.Position = 0;
            using var reader = new TarReader(archive);
            TarEntry readEntry = reader.GetNextEntry(copyData);
            Assert.NotNull(readEntry);

            Assert.Equal(TarEntryType.RegularFile, readEntry.EntryType);
            Assert.Equal(RealName, readEntry.Name);
            Assert.Equal(RealSize, readEntry.Length);
            Assert.NotNull(readEntry.DataStream);
            Assert.Equal(RealSize, readEntry.DataStream.Length);

            byte[] expanded = new byte[RealSize];
            readEntry.DataStream.ReadExactly(expanded);

            for (int i = 0; i < Seg0Offset; i++) Assert.Equal(0, expanded[i]);
            for (int i = (int)Seg0Offset; i < (int)(Seg0Offset + Seg0Length); i++) Assert.Equal(0x42, expanded[i]);
            for (int i = (int)(Seg0Offset + Seg0Length); i < Seg1Offset; i++) Assert.Equal(0, expanded[i]);
            for (int i = (int)Seg1Offset; i < (int)(Seg1Offset + Seg1Length); i++) Assert.Equal(0x43, expanded[i]);
            for (int i = (int)(Seg1Offset + Seg1Length); i < RealSize; i++) Assert.Equal(0, expanded[i]);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void GnuSparse10Pax_NilSparseData(bool copyData)
        {
            // pax-nil-sparse-data: one segment (offset=0, length=1000), realsize=1000, no holes.
            // The packed data is 1000 bytes of "0123456789" repeating.
            using MemoryStream archiveStream = GetTarMemoryStream(CompressionMethod.Uncompressed, "golang_tar", "pax-nil-sparse-data");
            using TarReader reader = new TarReader(archiveStream);

            TarEntry? entry = reader.GetNextEntry(copyData);
            Assert.NotNull(entry);

            Assert.Equal(TarEntryType.RegularFile, entry.EntryType);
            Assert.Equal("sparse.db", entry.Name);
            Assert.Equal(1000, entry.Length);
            Assert.NotNull(entry.DataStream);
            Assert.Equal(1000, entry.DataStream.Length);

            byte[] content = new byte[1000];
            entry.DataStream.ReadExactly(content);

            for (int i = 0; i < 1000; i++)
            {
                Assert.Equal((byte)'0' + (i % 10), content[i]);
            }

            Assert.Null(reader.GetNextEntry());
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void GnuSparse10Pax_NilSparseHole(bool copyData)
        {
            // pax-nil-sparse-hole: one segment (offset=1000, length=0), realsize=1000, all zeros.
            using MemoryStream archiveStream = GetTarMemoryStream(CompressionMethod.Uncompressed, "golang_tar", "pax-nil-sparse-hole");
            using TarReader reader = new TarReader(archiveStream);

            TarEntry? entry = reader.GetNextEntry(copyData);
            Assert.NotNull(entry);

            Assert.Equal(TarEntryType.RegularFile, entry.EntryType);
            Assert.Equal("sparse.db", entry.Name);
            Assert.Equal(1000, entry.Length);
            Assert.NotNull(entry.DataStream);
            Assert.Equal(1000, entry.DataStream.Length);

            byte[] content = new byte[1000];
            entry.DataStream.ReadExactly(content);

            Assert.All(content, b => Assert.Equal(0, b));
            Assert.Null(reader.GetNextEntry());
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void CopySparseEntryToNewArchive_PreservesExpandedContent(bool copyData)
        {
            const string RealName = "realfile.txt";
            const long RealSize = 2048;
            const long Seg0Offset = 256, Seg0Length = 256;
            const long Seg1Offset = 768, Seg1Length = 256;

            byte[] packedData0 = new byte[Seg0Length];
            Array.Fill<byte>(packedData0, 0x42);
            byte[] packedData1 = new byte[Seg1Length];
            Array.Fill<byte>(packedData1, 0x43);

            byte[] mapText = Encoding.ASCII.GetBytes("2\n256\n256\n768\n256\n");
            byte[] rawSparseData = new byte[512 + Seg0Length + Seg1Length];
            mapText.CopyTo(rawSparseData, 0);
            packedData0.CopyTo(rawSparseData, 512);
            packedData1.CopyTo(rawSparseData, 512 + (int)Seg0Length);

            var gnuSparseAttributes = new Dictionary<string, string>
            {
                ["GNU.sparse.major"] = "1",
                ["GNU.sparse.minor"] = "0",
                ["GNU.sparse.name"] = RealName,
                ["GNU.sparse.realsize"] = RealSize.ToString(),
            };

            using var sourceArchive = new MemoryStream();
            using (var writer = new TarWriter(sourceArchive, TarEntryFormat.Pax, leaveOpen: true))
            {
                var entry = new PaxTarEntry(TarEntryType.RegularFile, "GNUSparseFile.0/" + RealName, gnuSparseAttributes);
                entry.DataStream = new MemoryStream(rawSparseData);
                writer.WriteEntry(entry);
            }

            sourceArchive.Position = 0;

            using var destArchive = new MemoryStream();
            using (var reader = new TarReader(sourceArchive))
            {
                TarEntry readEntry = reader.GetNextEntry(copyData);
                Assert.NotNull(readEntry);
                Assert.Equal(RealName, readEntry.Name);
                Assert.Equal(RealSize, readEntry.Length);

                // Create a fresh entry to write the expanded content as a plain regular file.
                // The original PAX extended attributes include GNU.sparse.* keys that would
                // cause a second TarReader to attempt sparse map parsing on the already-expanded data.
                var freshEntry = new PaxTarEntry(TarEntryType.RegularFile, readEntry.Name);
                freshEntry.DataStream = readEntry.DataStream;

                using var writer2 = new TarWriter(destArchive, TarEntryFormat.Pax, leaveOpen: true);
                writer2.WriteEntry(freshEntry);
            }

            destArchive.Position = 0;
            using var reader2 = new TarReader(destArchive);
            TarEntry copiedEntry = reader2.GetNextEntry();
            Assert.NotNull(copiedEntry);
            Assert.Equal(RealName, copiedEntry.Name);
            Assert.Equal(RealSize, copiedEntry.Length);
            Assert.NotNull(copiedEntry.DataStream);

            byte[] content = new byte[RealSize];
            copiedEntry.DataStream.ReadExactly(content);

            for (int i = 0; i < Seg0Offset; i++) Assert.Equal(0, content[i]);
            for (int i = (int)Seg0Offset; i < (int)(Seg0Offset + Seg0Length); i++) Assert.Equal(0x42, content[i]);
            for (int i = (int)(Seg0Offset + Seg0Length); i < Seg1Offset; i++) Assert.Equal(0, content[i]);
            for (int i = (int)Seg1Offset; i < (int)(Seg1Offset + Seg1Length); i++) Assert.Equal(0x43, content[i]);
            for (int i = (int)(Seg1Offset + Seg1Length); i < RealSize; i++) Assert.Equal(0, content[i]);

            Assert.Null(reader2.GetNextEntry());
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task GnuSparse10Pax_DataStreamExpandsSparseSections_Async(bool copyData)
        {
            const string PlaceholderName = "GNUSparseFile.0/realfile.txt";
            const string RealName = "realfile.txt";
            const long RealSize = 1024;
            const long SegmentLength = 256;
            byte[] packedData = new byte[SegmentLength];
            Array.Fill<byte>(packedData, 0x42);

            byte[] mapText = Encoding.ASCII.GetBytes("1\n0\n256\n");
            byte[] rawSparseData = new byte[512 + SegmentLength];
            mapText.CopyTo(rawSparseData, 0);
            packedData.CopyTo(rawSparseData, 512);

            var gnuSparseAttributes = new Dictionary<string, string>
            {
                ["GNU.sparse.major"] = "1",
                ["GNU.sparse.minor"] = "0",
                ["GNU.sparse.name"] = RealName,
                ["GNU.sparse.realsize"] = RealSize.ToString(),
            };

            using var archive = new MemoryStream();
            await using (var writer = new TarWriter(archive, TarEntryFormat.Pax, leaveOpen: true))
            {
                var entry = new PaxTarEntry(TarEntryType.RegularFile, PlaceholderName, gnuSparseAttributes);
                entry.DataStream = new MemoryStream(rawSparseData);
                await writer.WriteEntryAsync(entry);
            }

            archive.Position = 0;
            await using var reader = new TarReader(archive);
            TarEntry readEntry = await reader.GetNextEntryAsync(copyData);
            Assert.NotNull(readEntry);

            Assert.Equal(RealName, readEntry.Name);
            Assert.Equal(RealSize, readEntry.Length);
            Assert.NotNull(readEntry.DataStream);
            Assert.Equal(RealSize, readEntry.DataStream.Length);

            byte[] expanded = new byte[RealSize];
            int totalRead = 0;
            while (totalRead < expanded.Length)
            {
                int read = await readEntry.DataStream.ReadAsync(expanded, totalRead, expanded.Length - totalRead);
                Assert.True(read > 0);
                totalRead += read;
            }

            for (int i = 0; i < SegmentLength; i++)
            {
                Assert.Equal(0x42, expanded[i]);
            }
            for (int i = (int)SegmentLength; i < RealSize; i++)
            {
                Assert.Equal(0, expanded[i]);
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task GnuSparse10Pax_NilSparseData_Async(bool copyData)
        {
            using MemoryStream archiveStream = GetTarMemoryStream(CompressionMethod.Uncompressed, "golang_tar", "pax-nil-sparse-data");
            await using TarReader reader = new TarReader(archiveStream);

            TarEntry? entry = await reader.GetNextEntryAsync(copyData);
            Assert.NotNull(entry);

            Assert.Equal("sparse.db", entry.Name);
            Assert.Equal(1000, entry.Length);
            Assert.NotNull(entry.DataStream);
            Assert.Equal(1000, entry.DataStream.Length);

            byte[] content = new byte[1000];
            int totalRead = 0;
            while (totalRead < content.Length)
            {
                int read = await entry.DataStream.ReadAsync(content, totalRead, content.Length - totalRead);
                Assert.True(read > 0);
                totalRead += read;
            }

            for (int i = 0; i < 1000; i++)
            {
                Assert.Equal((byte)'0' + (i % 10), content[i]);
            }

            Assert.Null(await reader.GetNextEntryAsync());
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task GnuSparse10Pax_NilSparseHole_Async(bool copyData)
        {
            using MemoryStream archiveStream = GetTarMemoryStream(CompressionMethod.Uncompressed, "golang_tar", "pax-nil-sparse-hole");
            await using TarReader reader = new TarReader(archiveStream);

            TarEntry? entry = await reader.GetNextEntryAsync(copyData);
            Assert.NotNull(entry);

            Assert.Equal("sparse.db", entry.Name);
            Assert.Equal(1000, entry.Length);
            Assert.NotNull(entry.DataStream);
            Assert.Equal(1000, entry.DataStream.Length);

            byte[] content = new byte[1000];
            int totalRead = 0;
            while (totalRead < content.Length)
            {
                int read = await entry.DataStream.ReadAsync(content, totalRead, content.Length - totalRead);
                Assert.True(read > 0);
                totalRead += read;
            }

            Assert.All(content, b => Assert.Equal(0, b));

            Assert.Null(await reader.GetNextEntryAsync());
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void GnuSparse10Pax_SparseBig_NameAndLength(bool copyData)
        {
            // pax-sparse-big: 6 segments scattered across a 60 GB virtual file, realsize=60000000000.
            using MemoryStream archiveStream = GetTarMemoryStream(CompressionMethod.Uncompressed, "golang_tar", "pax-sparse-big");
            using TarReader reader = new TarReader(archiveStream);

            TarEntry? entry = reader.GetNextEntry(copyData);
            Assert.NotNull(entry);

            Assert.Equal(TarEntryType.RegularFile, entry.EntryType);
            Assert.Equal("pax-sparse", entry.Name);
            Assert.Equal(60000000000L, entry.Length);
            Assert.NotNull(entry.DataStream);
            Assert.Equal(60000000000L, entry.DataStream.Length);
        }
    }
}
