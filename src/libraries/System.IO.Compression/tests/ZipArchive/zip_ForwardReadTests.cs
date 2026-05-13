// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Xunit;

namespace System.IO.Compression.Tests
{
    public class zip_ForwardReadTests : ZipFileTestBase
    {
        private static readonly byte[] s_smallContent = "Hello, small world!"u8.ToArray();
        private static readonly byte[] s_mediumContent = new byte[8192];
        private static readonly byte[] s_largeContent = new byte[65536];

        static zip_ForwardReadTests()
        {
            Random rng = new(42);
            rng.NextBytes(s_mediumContent);
            rng.NextBytes(s_largeContent);
        }

        // ── Core reading scenarios ──────────────────────────────────────────

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task NonSeekableStream_ConsumeSkipConsume_ReadsCorrectly(bool async)
        {
            byte[] zipBytes = CreateZipWithEntries(CompressionLevel.Optimal, seekable: false);
            byte[][] expected = [s_smallContent, s_mediumContent, s_largeContent];

            using MemoryStream archiveStream = new(zipBytes);
            using WrappedStream nonSeekable = new(archiveStream, canRead: true, canWrite: false, canSeek: false, null);
            using ZipArchive archive = new(nonSeekable, ZipArchiveMode.ForwardRead);

            ZipArchiveEntry? first = await GetNextEntry(archive, async);
            Assert.NotNull(first);
            using (Stream ds = first.Open())
            {
                Assert.Equal(expected[0], await ReadStreamFully(ds, async));
            }

            // Skip second entry without opening
            ZipArchiveEntry? second = await GetNextEntry(archive, async);
            Assert.NotNull(second);
            Assert.Equal("medium.bin", second.FullName);

            ZipArchiveEntry? third = await GetNextEntry(archive, async);
            Assert.NotNull(third);
            using (Stream ds = third.Open())
            {
                Assert.Equal(expected[2], await ReadStreamFully(ds, async));
            }

            Assert.Null(await GetNextEntry(archive, async));
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public async Task StoredEntries_SeekableAndNonSeekable_ReadCorrectly(bool async, bool readSeekable)
        {
            // Always created on seekable stream → known sizes, no data descriptors
            byte[] zipBytes = CreateZipWithEntries(CompressionLevel.NoCompression, seekable: true);
            byte[][] expected = [s_smallContent, s_mediumContent, s_largeContent];

            using MemoryStream archiveStream = new(zipBytes);
            Stream readStream = readSeekable
                ? archiveStream
                : new WrappedStream(archiveStream, canRead: true, canWrite: false, canSeek: false, null);
            using ZipArchive archive = new(readStream, ZipArchiveMode.ForwardRead);

            for (int i = 0; i < expected.Length; i++)
            {
                ZipArchiveEntry? entry = await GetNextEntry(archive, async);
                Assert.NotNull(entry);
                Assert.Equal(ZipCompressionMethod.Stored, entry.CompressionMethod);

                using Stream ds = entry.Open();
                Assert.Equal(expected[i], await ReadStreamFully(ds, async));
            }

            Assert.Null(await GetNextEntry(archive, async));
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task PartialRead_ThenAdvance_ReadsNextEntryCorrectly(bool async)
        {
            byte[] zipBytes = CreateZipWithEntries(CompressionLevel.Optimal, seekable: false);

            using MemoryStream archiveStream = new(zipBytes);
            using ZipArchive archive = new(archiveStream, ZipArchiveMode.ForwardRead);

            ZipArchiveEntry? first = await GetNextEntry(archive, async);
            Assert.NotNull(first);

            using (Stream ds = first.Open())
            {
                byte[] partial = new byte[3];
                await ReadStream(ds, partial, async);
            }

            ZipArchiveEntry? second = await GetNextEntry(archive, async);
            Assert.NotNull(second);
            Assert.Equal("medium.bin", second.FullName);

            using Stream ds2 = second.Open();
            Assert.Equal(s_mediumContent, await ReadStreamFully(ds2, async));
        }

        // ── Edge cases ──────────────────────────────────────────────────────

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task EmptyAndDirectoryEntries_HandleCorrectly(bool async)
        {
            using MemoryStream ms = new();
            using (ZipArchive create = new(ms, ZipArchiveMode.Create, leaveOpen: true))
            {
                create.CreateEntry("mydir/");
                create.CreateEntry("empty.txt");
            }

            ms.Position = 0;
            using ZipArchive archive = new(ms, ZipArchiveMode.ForwardRead);

            ZipArchiveEntry? dir = await GetNextEntry(archive, async);
            Assert.NotNull(dir);
            Assert.Equal("mydir/", dir.FullName);

            ZipArchiveEntry? empty = await GetNextEntry(archive, async);
            Assert.NotNull(empty);
            Assert.Equal("empty.txt", empty.FullName);
            Assert.Equal(0, empty.CompressedLength);

            Assert.Null(await GetNextEntry(archive, async));
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task EmptyArchive_ReturnsNull(bool async)
        {
            using MemoryStream ms = new();
            using (new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true)) { }

            ms.Position = 0;
            using ZipArchive archive = new(ms, ZipArchiveMode.ForwardRead);

            Assert.Null(await GetNextEntry(archive, async));
        }

        // ── Unsupported operation guards ────────────────────────────────────

        [Fact]
        public void UnsupportedOperations_Throw()
        {
            byte[] zipBytes = CreateZipWithEntries(CompressionLevel.Optimal, seekable: true);

            using MemoryStream archiveStream = new(zipBytes);
            using ZipArchive archive = new(archiveStream, ZipArchiveMode.ForwardRead);

            Assert.Throws<NotSupportedException>(() => archive.Entries);
            Assert.Throws<NotSupportedException>(() => archive.GetEntry("small.txt"));
            Assert.Throws<NotSupportedException>(() => archive.CreateEntry("new.txt"));
        }

        [Fact]
        public void GetNextEntry_NotInForwardReadMode_Throws()
        {
            byte[] zipBytes = CreateZipWithEntries(CompressionLevel.Optimal, seekable: true);

            using MemoryStream archiveStream = new(zipBytes);
            using ZipArchive archive = new(archiveStream, ZipArchiveMode.Read);

            Assert.Throws<NotSupportedException>(() => archive.GetNextEntry());
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task StoredWithDataDescriptor_Throws(bool async)
        {
            byte[] zipBytes = CreateZipWithEntries(CompressionLevel.NoCompression, seekable: false);

            using MemoryStream archiveStream = new(zipBytes);
            using WrappedStream nonSeekable = new(archiveStream, canRead: true, canWrite: false, canSeek: false, null);
            using ZipArchive archive = new(nonSeekable, ZipArchiveMode.ForwardRead);

            if (async)
                await Assert.ThrowsAsync<NotSupportedException>(() => archive.GetNextEntryAsync().AsTask());
            else
                Assert.Throws<NotSupportedException>(() => archive.GetNextEntry());
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task EncryptedEntry_MetadataAccessible_OpenThrows(bool async)
        {
            byte[] zipBytes = CreateZipWithEntries(CompressionLevel.Optimal, seekable: true);

            // Set encryption bit in first entry's local file header (offset 6)
            zipBytes[6] |= 0x01;

            using MemoryStream archiveStream = new(zipBytes);
            using ZipArchive archive = new(archiveStream, ZipArchiveMode.ForwardRead);

            ZipArchiveEntry? entry = await GetNextEntry(archive, async);
            Assert.NotNull(entry);
            Assert.True(entry.IsEncrypted);
            Assert.Equal("small.txt", entry.FullName);

            Assert.Throws<NotSupportedException>(() => entry.Open());
        }

        // ── Metadata / property access ──────────────────────────────────────

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task DataDescriptorEntry_SizeAndCrcProperties_AlwaysThrow(bool async)
        {
            byte[] zipBytes = CreateZipWithEntries(CompressionLevel.Optimal, seekable: false);

            using MemoryStream archiveStream = new(zipBytes);
            using ZipArchive archive = new(archiveStream, ZipArchiveMode.ForwardRead);

            ZipArchiveEntry? entry = await GetNextEntry(archive, async);
            Assert.NotNull(entry);

            Assert.Equal("small.txt", entry.FullName);
            _ = entry.LastWriteTime;
            Assert.Equal(ZipCompressionMethod.Deflate, entry.CompressionMethod);

            Assert.Throws<InvalidOperationException>(() => entry.Crc32);
            Assert.Throws<InvalidOperationException>(() => entry.CompressedLength);
            Assert.Throws<InvalidOperationException>(() => entry.Length);

            using (Stream ds = entry.Open())
            {
                await ReadStreamFully(ds, async);
            }

            await GetNextEntry(archive, async);

            // Still throws after reading and advancing
            Assert.Throws<InvalidOperationException>(() => entry.Crc32);
            Assert.Throws<InvalidOperationException>(() => entry.CompressedLength);
            Assert.Throws<InvalidOperationException>(() => entry.Length);
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task KnownSizeEntry_SizeAndCrcProperties_Accessible(bool async)
        {
            byte[] zipBytes = CreateZipWithEntries(CompressionLevel.Optimal, seekable: true);

            using MemoryStream archiveStream = new(zipBytes);
            using ZipArchive archive = new(archiveStream, ZipArchiveMode.ForwardRead);

            ZipArchiveEntry? entry = await GetNextEntry(archive, async);
            Assert.NotNull(entry);

            _ = entry.Crc32;
            Assert.True(entry.CompressedLength > 0);
            Assert.Equal(s_smallContent.Length, entry.Length);
        }

        // ── Lifecycle / dispose ─────────────────────────────────────────────

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task GetNextEntry_AfterDispose_Throws(bool async)
        {
            byte[] zipBytes = CreateZipWithEntries(CompressionLevel.Optimal, seekable: true);

            using MemoryStream archiveStream = new(zipBytes);
            ZipArchive archive = new(archiveStream, ZipArchiveMode.ForwardRead, leaveOpen: true);
            archive.Dispose();

            if (async)
                await Assert.ThrowsAsync<ObjectDisposedException>(() => archive.GetNextEntryAsync().AsTask());
            else
                Assert.Throws<ObjectDisposedException>(() => archive.GetNextEntry());
        }

        [Fact]
        public void LeaveOpen_DoesNotDisposeStream()
        {
            byte[] zipBytes = CreateZipWithEntries(CompressionLevel.Optimal, seekable: true);

            using MemoryStream archiveStream = new(zipBytes);
            new ZipArchive(archiveStream, ZipArchiveMode.ForwardRead, leaveOpen: true).Dispose();

            Assert.True(archiveStream.CanRead);
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task Dispose_WithPendingDataDescriptor_DoesNotThrow(bool async)
        {
            byte[] zipBytes = CreateZipWithEntries(CompressionLevel.Optimal, seekable: false);

            using MemoryStream archiveStream = new(zipBytes);
            ZipArchive archive = new(archiveStream, ZipArchiveMode.ForwardRead, leaveOpen: true);

            ZipArchiveEntry? entry;
            do { entry = await GetNextEntry(archive, async); }
            while (entry is not null && entry.FullName != "large.bin");

            Assert.NotNull(entry);
            using (Stream ds = entry.Open())
            {
                await ReadStreamFully(ds, async);
            }

            if (async)
                await archive.DisposeAsync();
            else
                archive.Dispose();
        }

        // ── Error handling ──────────────────────────────────────────────────

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task KnownSizeEntry_CrcMismatch_ThrowsOnRead(bool async)
        {
            byte[] zipBytes = CreateZipWithEntries(CompressionLevel.NoCompression, seekable: true);

            // Corrupt the CRC-32 field in the first entry's local file header (offset 14)
            zipBytes[14] ^= 0xFF;

            using MemoryStream archiveStream = new(zipBytes);
            using ZipArchive archive = new(archiveStream, ZipArchiveMode.ForwardRead);

            ZipArchiveEntry? entry = await GetNextEntry(archive, async);
            Assert.NotNull(entry);

            using Stream ds = entry.Open();
            await Assert.ThrowsAsync<InvalidDataException>(async () => await ReadStreamFully(ds, async));
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task TruncatedDeflateEntry_ThrowsOnRead(bool async)
        {
            byte[] zipBytes = CreateZipWithEntries(CompressionLevel.Optimal, seekable: true);

            int filenameLength = zipBytes[26] | (zipBytes[27] << 8);
            int extraFieldLength = zipBytes[28] | (zipBytes[29] << 8);
            int dataStart = 30 + filenameLength + extraFieldLength;
            byte[] truncated = zipBytes[..(dataStart + 2)];

            using MemoryStream archiveStream = new(truncated);
            using ZipArchive archive = new(archiveStream, ZipArchiveMode.ForwardRead);

            ZipArchiveEntry? entry = await GetNextEntry(archive, async);
            Assert.NotNull(entry);

            using Stream ds = entry.Open();
            await Assert.ThrowsAsync<InvalidDataException>(async () => await ReadStreamFully(ds, async));
        }

        // ── Helpers ─────────────────────────────────────────────────────────

        private static async ValueTask<ZipArchiveEntry?> GetNextEntry(ZipArchive archive, bool async) =>
            async ? await archive.GetNextEntryAsync() : archive.GetNextEntry();

        private static async ValueTask<int> ReadStream(Stream stream, byte[] buffer, bool async) =>
            async ? await stream.ReadAsync(buffer) : stream.Read(buffer);

        private static async Task<byte[]> ReadStreamFully(Stream stream, bool async)
        {
            using MemoryStream result = new();
            if (async)
                await stream.CopyToAsync(result);
            else
                stream.CopyTo(result);
            return result.ToArray();
        }

        private static byte[] CreateZipWithEntries(CompressionLevel compressionLevel, bool seekable)
        {
            MemoryStream ms = new();

            Stream writeStream = seekable
                ? ms
                : new WrappedStream(ms, canRead: true, canWrite: true, canSeek: false, null);

            using (ZipArchive archive = new(writeStream, ZipArchiveMode.Create, leaveOpen: true))
            {
                AddEntry(archive, "small.txt", s_smallContent, compressionLevel);
                AddEntry(archive, "medium.bin", s_mediumContent, compressionLevel);
                AddEntry(archive, "large.bin", s_largeContent, compressionLevel);
            }

            return ms.ToArray();

            static void AddEntry(ZipArchive archive, string name, byte[] contents, CompressionLevel level)
            {
                ZipArchiveEntry entry = archive.CreateEntry(name, level);
                using Stream stream = entry.Open();
                stream.Write(contents);
            }
        }
    }
}
