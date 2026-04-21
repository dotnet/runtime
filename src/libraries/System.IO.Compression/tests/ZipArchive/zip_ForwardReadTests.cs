// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text;
using System.Threading;
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

            // Consume first entry fully
            ZipArchiveEntry? first = await GetNextEntry(archive, async);
            Assert.NotNull(first);
            using (Stream ds = first.Open())
            {
                byte[] data = await ReadStreamFully(ds, async);
                Assert.Equal(expected[0], data);
            }

            // Skip second entry (don't open/read)
            ZipArchiveEntry? second = await GetNextEntry(archive, async);
            Assert.NotNull(second);
            Assert.Equal("medium.bin", second.FullName);

            // Consume third entry fully
            ZipArchiveEntry? third = await GetNextEntry(archive, async);
            Assert.NotNull(third);
            using (Stream ds = third.Open())
            {
                byte[] data = await ReadStreamFully(ds, async);
                Assert.Equal(expected[2], data);
            }

            // End of archive
            Assert.Null(await GetNextEntry(archive, async));
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task SeekableStream_StoredEntries_ReadsCorrectly(bool async)
        {
            byte[] zipBytes = CreateZipWithEntries(CompressionLevel.NoCompression, seekable: true);
            byte[][] expected = [s_smallContent, s_mediumContent, s_largeContent];

            using MemoryStream archiveStream = new(zipBytes);
            using ZipArchive archive = new(archiveStream, ZipArchiveMode.ForwardRead);

            for (int i = 0; i < expected.Length; i++)
            {
                ZipArchiveEntry? entry = await GetNextEntry(archive, async);
                Assert.NotNull(entry);
                Assert.Equal(ZipCompressionMethod.Stored, entry.CompressionMethod);

                using Stream ds = entry.Open();
                Assert.Equal(expected[i], await ReadStreamFully(ds, async));
            }
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

            // Only read a few bytes, don't finish
            using (Stream ds = first.Open())
            {
                byte[] partial = new byte[3];
                await ReadStream(ds, partial, async);
            }

            // Next entry should still be readable
            ZipArchiveEntry? second = await GetNextEntry(archive, async);
            Assert.NotNull(second);
            Assert.Equal("medium.bin", second.FullName);

            using Stream ds2 = second.Open();
            Assert.Equal(s_mediumContent, await ReadStreamFully(ds2, async));
        }

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

        // ── Unsupported feature guards ──────────────────────────────────────

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

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task DataDescriptorEntry_SizeAndCrcProperties_AlwaysThrow(bool async)
        {
            byte[] zipBytes = CreateZipWithEntries(CompressionLevel.Optimal, seekable: false);

            using MemoryStream archiveStream = new(zipBytes);
            using ZipArchive archive = new(archiveStream, ZipArchiveMode.ForwardRead);

            ZipArchiveEntry? entry = await GetNextEntry(archive, async);
            Assert.NotNull(entry);

            // Non-size properties work
            Assert.Equal("small.txt", entry.FullName);
            _ = entry.LastWriteTime;
            Assert.Equal(ZipCompressionMethod.Deflate, entry.CompressionMethod);

            // Size/CRC properties throw — permanently, even after reading
            Assert.Throws<InvalidOperationException>(() => entry.Crc32);
            Assert.Throws<InvalidOperationException>(() => entry.CompressedLength);
            Assert.Throws<InvalidOperationException>(() => entry.Length);

            using (Stream ds = entry.Open())
                await ReadStreamFully(ds, async);

            await GetNextEntry(archive, async); // drains data descriptor

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

        // ── Dispose / lifecycle ─────────────────────────────────────────────

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

            // Read last entry, then dispose — Dispose must drain data descriptor
            ZipArchiveEntry? entry;
            do { entry = await GetNextEntry(archive, async); }
            while (entry is not null && entry.FullName != "large.bin");

            Assert.NotNull(entry);
            using (Stream ds = entry.Open())
                await ReadStreamFully(ds, async);

            if (async)
                await archive.DisposeAsync();
            else
                archive.Dispose();
        }

        // ── Helpers ─────────────────────────────────────────────────────────

        private static async ValueTask<ZipArchiveEntry?> GetNextEntry(ZipArchive archive, bool async) =>
            async ? await archive.GetNextEntryAsync() : archive.GetNextEntry();

        private static async ValueTask<int> ReadStream(Stream stream, byte[] buffer, bool async) =>
            async ? await stream.ReadAsync(buffer) : stream.Read(buffer);

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

        private static async Task<byte[]> ReadStreamFully(Stream stream, bool async)
        {
            using MemoryStream result = new();
            if (async)
                await stream.CopyToAsync(result);
            else
                stream.CopyTo(result);
            return result.ToArray();
        }
    }
}
