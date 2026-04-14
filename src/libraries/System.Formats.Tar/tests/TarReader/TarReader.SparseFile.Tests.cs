// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.CompilerServices;
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
        // Writes a single PAX 1.0 sparse entry to the given writer.
        // realSize can be negative to produce an intentionally invalid archive (for negative-size tests).
        private static void WriteSparseEntry(TarWriter writer, string realName, long realSize, byte[] rawSparseData)
        {
            // GNU.sparse.realsize is intentionally omitted here to avoid PaxTarEntry constructor
            // validation (ReplaceNormalAttributesWithExtended rejects negative values). It is
            // injected directly into the EA dictionary after construction so that:
            //  (a) valid archives work correctly — the attribute is still written to the archive, and
            //  (b) intentionally invalid archives (negative realsize) can be constructed for tests.
            var gnuSparseAttributes = new Dictionary<string, string>
            {
                ["GNU.sparse.major"] = "1",
                ["GNU.sparse.minor"] = "0",
                ["GNU.sparse.name"] = realName,
            };
            var entry = new PaxTarEntry(TarEntryType.RegularFile, "GNUSparseFile.0/" + realName, gnuSparseAttributes);
            // Inject GNU.sparse.realsize into the internal EA dictionary via UnsafeAccessor, bypassing
            // the public ReadOnlyDictionary façade and constructor validation. This allows
            // intentionally-invalid archives (negative realsize) to be constructed for tests.
            var ea = (Dictionary<string, string>)ReadOnlyDictionaryAccessors<string, string>.GetInnerDictionary((ReadOnlyDictionary<string, string>)entry.ExtendedAttributes);
            ea["GNU.sparse.realsize"] = realSize.ToString();
            entry.DataStream = new MemoryStream(rawSparseData);
            writer.WriteEntry(entry);
        }

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

            var archive = new MemoryStream();
            using (var writer = new TarWriter(archive, TarEntryFormat.Pax, leaveOpen: true))
            {
                WriteSparseEntry(writer, realName, realSize, rawSparseData);
            }
            archive.Position = 0;
            return (archive, rawSparseData[(mapText.Length + padding)..]);
        }

        // Reads the DataStream of the first entry from the given archive and returns it.
        private static Stream GetSparseDataStream(MemoryStream archiveStream, bool copyData)
        {
            archiveStream.Position = 0;
            using var reader = new TarReader(archiveStream, leaveOpen: true);
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

            var archive = new MemoryStream();
            using (var writer = new TarWriter(archive, TarEntryFormat.Pax, leaveOpen: true))
            {
                WriteSparseEntry(writer, realName, realSize, rawData);
            }
            archive.Position = 0;
            return archive;
        }

        public static IEnumerable<object[]> SparseLayoutTestCases()
        {
            // (realSize, segments as flat array [off0, len0, off1, len1, ...], copyData, useAsync)
            (long, long[])[] layouts =
            [
                (512, [0, 512]),                        // single segment, no holes
                (1024, [256, 256]),                     // leading + trailing hole
                (2048, [0, 256, 512, 256, 1024, 256]),  // 3 segments with holes in between
                (1000, [1000, 0]),                      // all holes (zero-length segment)
            ];

            foreach ((long size, long[] layout) in layouts)
            {
                foreach (bool copyData in new[] { false, true })
                {
                    foreach (bool useAsync in new[] { false, true })
                    {
                        yield return new object[] { size, layout, copyData, useAsync };
                    }
                }
            }
        }

        [Theory]
        [MemberData(nameof(SparseLayoutTestCases))]
        public async Task SparseLayout_ExpandsCorrectly(long realSize, long[] segmentPairs, bool copyData, bool useAsync)
        {
            var segments = PairsToSegments(segmentPairs);
            var (archive, _) = BuildSparseArchive("file.bin", realSize, segments);

            using var dataStream = GetSparseDataStream(archive, copyData);

            Assert.Equal(realSize, dataStream.Length);
            byte[] buf = new byte[(int)realSize];
            if (useAsync)
            {
                await dataStream.ReadExactlyAsync(buf, CancellationToken.None);
            }
            else
            {
                dataStream.ReadExactly(buf);
            }
            VerifyExpandedContent(buf, realSize, segments);
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

            var (sparseArchive, _) = BuildSparseArchive("sparse.bin", 256, [(0L, 256L)]);

            // Re-create the archive with the sparse entry followed by a regular entry.
            var archive = new MemoryStream();
            using (var writer = new TarWriter(archive, TarEntryFormat.Pax, leaveOpen: true))
            {
                sparseArchive.Position = 0;
                using (var srcReader = new TarReader(sparseArchive, leaveOpen: true))
                {
                    TarEntry? sparseEntry = srcReader.GetNextEntry(copyData: false);
                    Assert.NotNull(sparseEntry);
                    writer.WriteEntry(sparseEntry);
                }

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

        public static IEnumerable<object[]> CorruptedSparseMapTestCases()
        {
            string[] corruptedMaps =
            [
                "abc\n0\n256\n",       // non-numeric segment count
                "\n0\n256\n",          // empty segment count line
                "1\nabc\n256\n",       // non-numeric offset
                "1\n0\nabc\n",         // non-numeric length
                "1\n-1\n256\n",        // negative offset
                "1\n0\n-1\n",          // negative length
                "1\n0\n",              // truncated: missing length line
                "1\n",                 // truncated: missing offset and length lines
                "1\n0\n2048\n",        // segment extends past realSize
                "2\n256\n256\n0\n256\n", // segments not in ascending order
                "2\n" + new string('A', 512) + "\n256\n", // line exceeding buffer capacity
            ];

            foreach (string map in corruptedMaps)
            {
                yield return new object[] { map, false };
                yield return new object[] { map, true };
            }
        }

        [Theory]
        [MemberData(nameof(CorruptedSparseMapTestCases))]
        public async Task CorruptedSparseMap_InvalidDataException(string sparseMapContent, bool useAsync)
        {
            var archive = BuildRawSparseArchive(sparseMapContent, "file.bin", 1024);
            using var reader = new TarReader(archive);
            TarEntry? entry = useAsync
                ? await reader.GetNextEntryAsync(copyData: false)
                : reader.GetNextEntry(copyData: false);
            Assert.NotNull(entry);
            Assert.NotNull(entry.DataStream);

            if (useAsync)
            {
                await Assert.ThrowsAsync<InvalidDataException>(async () => await entry.DataStream.ReadAsync(new byte[1]));
            }
            else
            {
                Assert.Throws<InvalidDataException>(() => entry.DataStream.ReadByte());
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task NegativeSparseRealSize_InvalidDataException(bool useAsync)
        {
            // BuildRawSparseArchive uses WriteSparseEntry, which stores realSize.ToString() = "-1"
            // as the GNU.sparse.realsize PAX attribute, which TarReader should reject.
            var archive = BuildRawSparseArchive("1\n0\n1\n", "file.bin", -1L);

            using var reader = new TarReader(archive);
            if (useAsync)
            {
                await Assert.ThrowsAsync<InvalidDataException>(async () => await reader.GetNextEntryAsync());
            }
            else
            {
                Assert.Throws<InvalidDataException>(() => reader.GetNextEntry());
            }
        }

        [Theory]
        [InlineData(false, false)]  // missing minor
        [InlineData(true, false)]
        [InlineData(false, true)]   // wrong major
        [InlineData(true, true)]
        public void WrongSparseVersion_EntryReadAsNormal(bool copyData, bool wrongMajor)
        {
            byte[] content = Encoding.ASCII.GetBytes("plain content");

            var attributes = new Dictionary<string, string>
            {
                ["GNU.sparse.name"] = "real.bin",
                ["GNU.sparse.realsize"] = "1024",
            };

            if (wrongMajor)
            {
                attributes["GNU.sparse.major"] = "2";
                attributes["GNU.sparse.minor"] = "0";
            }
            else
            {
                attributes["GNU.sparse.major"] = "1";
                // minor intentionally omitted
            }

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
        [InlineData(false, true)]
        [InlineData(true, true)]
        [InlineData(false, false)]
        [InlineData(true, false)]
        public void CopySparseEntryToNewArchive_PreservesExpandedContent(bool copyData, bool seekableSource)
        {
            const string RealName = "realfile.txt";
            const long RealSize = 2048;
            var segments = new[] { (256L, 256L), (768L, 256L) };

            var (sourceArchive, _) = BuildSparseArchive(RealName, RealSize, segments);
            int sourceLength = (int)sourceArchive.Length;
            sourceArchive.Position = 0;

            // Copy the sparse entry directly to a new archive.
            using var destArchive = new MemoryStream();
            Stream readerStream = seekableSource
                ? sourceArchive
                : new NonSeekableStream(sourceArchive);
            using (var reader = new TarReader(readerStream))
            {
                TarEntry readEntry = reader.GetNextEntry(copyData);
                Assert.NotNull(readEntry);
                Assert.Equal(RealName, readEntry.Name);
                Assert.Equal(RealSize, readEntry.Length);

                using var writer2 = new TarWriter(destArchive, TarEntryFormat.Pax, leaveOpen: true);
                writer2.WriteEntry(readEntry);
            }

            // Re-read the destination archive and verify the sparse entry round-trips correctly.
            Assert.Equal(sourceLength, destArchive.Length);

            destArchive.Position = 0;
            using var reader2 = new TarReader(destArchive);
            TarEntry copiedEntry = reader2.GetNextEntry();
            Assert.NotNull(copiedEntry);
            Assert.Equal(RealName, copiedEntry.Name);
            Assert.Equal(RealSize, copiedEntry.Length);
            Assert.NotNull(copiedEntry.DataStream);

            Assert.InRange(RealSize, 0, int.MaxValue);
            byte[] content = new byte[(int)RealSize];
            copiedEntry.DataStream.ReadExactly(content);
            VerifyExpandedContent(content, RealSize, segments);

            Assert.Null(reader2.GetNextEntry());
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

        private static (long Offset, long Length)[] PairsToSegments(long[] pairs)
        {
            var segments = new (long Offset, long Length)[pairs.Length / 2];
            for (int i = 0; i < segments.Length; i++)
            {
                segments[i] = (pairs[i * 2], pairs[i * 2 + 1]);
            }

            return segments;
        }

        // Verifies that expanded content has zeros in holes and the correct fill byte
        // (1-based segment index) in data segments, matching BuildSparseArchive's convention.
        private static void VerifyExpandedContent(byte[] buf, long realSize, (long Offset, long Length)[] segments)
        {
            int pos = 0;
            for (int s = 0; s < segments.Length; s++)
            {
                var (offset, length) = segments[s];
                // Hole before this segment
                for (int i = pos; i < (int)offset; i++)
                {
                    Assert.Equal(0, buf[i]);
                }
                // Segment data (BuildSparseArchive fills with 1-based index)
                byte expected = (byte)(s + 1);
                for (int i = (int)offset; i < (int)(offset + length); i++)
                {
                    Assert.Equal(expected, buf[i]);
                }
                pos = (int)(offset + length);
            }
            // Trailing hole
            for (int i = pos; i < (int)realSize; i++)
            {
                Assert.Equal(0, buf[i]);
            }
        }

        private static class ReadOnlyDictionaryAccessors<TKey, TValue> where TKey : notnull
        {
            [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "m_dictionary")]
            public static extern ref IDictionary<TKey, TValue> GetInnerDictionary(ReadOnlyDictionary<TKey, TValue> d);
        }
    }

    // Minimal non-seekable stream wrapper for testing.
    // Unlike WrappedStream, this overrides Read(Span<byte>) to avoid the extra buffer copy
    // in Stream.Read(Span<byte>) that can cause issues with SubReadStream position tracking.
    internal sealed class NonSeekableStream : Stream
    {
        private readonly Stream _inner;

        public NonSeekableStream(Stream inner) => _inner = inner;

        public override bool CanRead => _inner.CanRead;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);
        public override int Read(Span<byte> buffer) => _inner.Read(buffer);
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken ct) => _inner.ReadAsync(buffer, offset, count, ct);
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken ct = default) => _inner.ReadAsync(buffer, ct);

        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
