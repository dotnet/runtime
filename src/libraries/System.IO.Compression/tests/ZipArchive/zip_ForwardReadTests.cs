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

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task Read_DeflateWithKnownSize_ReturnsDecompressedData(bool async)
        {
            byte[] zipBytes = CreateZipWithEntries(CompressionLevel.Optimal, seekable: true);
            byte[][] expectedContents = [s_smallContent, s_mediumContent, s_largeContent];

            using MemoryStream archiveStream = new(zipBytes);
            using ZipArchive archive = new(archiveStream, ZipArchiveMode.ForwardRead);

            for (int i = 0; i < expectedContents.Length; i++)
            {
                ZipArchiveEntry? entry = await GetNextEntry(archive, async);

                Assert.NotNull(entry);
                Assert.Equal(ZipCompressionMethod.Deflate, entry.CompressionMethod);

                using Stream dataStream = entry.Open();
                byte[] decompressed = await ReadStreamFully(dataStream, async);
                Assert.Equal(expectedContents[i], decompressed);
            }

            ZipArchiveEntry? end = await GetNextEntry(archive, async);
            Assert.Null(end);
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task Read_StoredWithKnownSize_ReturnsUncompressedData(bool async)
        {
            byte[] zipBytes = CreateZipWithEntries(CompressionLevel.NoCompression, seekable: true);
            byte[][] expectedContents = [s_smallContent, s_mediumContent, s_largeContent];

            using MemoryStream archiveStream = new(zipBytes);
            using ZipArchive archive = new(archiveStream, ZipArchiveMode.ForwardRead);

            for (int i = 0; i < expectedContents.Length; i++)
            {
                ZipArchiveEntry? entry = await GetNextEntry(archive, async);

                Assert.NotNull(entry);
                Assert.Equal(ZipCompressionMethod.Stored, entry.CompressionMethod);

                using Stream dataStream = entry.Open();
                byte[] decompressed = await ReadStreamFully(dataStream, async);
                Assert.Equal(expectedContents[i], decompressed);
            }
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task Read_DeflateWithDataDescriptor_ReturnsDecompressedData(bool async)
        {
            byte[] zipBytes = CreateZipWithEntries(CompressionLevel.Optimal, seekable: false);
            byte[][] expectedContents = [s_smallContent, s_mediumContent, s_largeContent];

            using MemoryStream archiveStream = new(zipBytes);
            using WrappedStream nonSeekableStream = new(archiveStream, canRead: true, canWrite: false, canSeek: false, null);
            using ZipArchive archive = new(nonSeekableStream, ZipArchiveMode.ForwardRead);

            for (int i = 0; i < expectedContents.Length; i++)
            {
                ZipArchiveEntry? entry = await GetNextEntry(archive, async);

                Assert.NotNull(entry);
                Assert.Equal(ZipCompressionMethod.Deflate, entry.CompressionMethod);

                using Stream dataStream = entry.Open();
                byte[] decompressed = await ReadStreamFully(dataStream, async);
                Assert.Equal(expectedContents[i], decompressed);
            }
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task Read_FromNonSeekableStream(bool async)
        {
            byte[] zipBytes = CreateZipWithEntries(CompressionLevel.Optimal, seekable: true);

            using MemoryStream archiveStream = new(zipBytes);
            using WrappedStream nonSeekableStream = new(archiveStream, canRead: true, canWrite: false, canSeek: false, null);
            using ZipArchive archive = new(nonSeekableStream, ZipArchiveMode.ForwardRead);

            ZipArchiveEntry? entry = await GetNextEntry(archive, async);
            Assert.NotNull(entry);
            Assert.Equal("small.txt", entry.FullName);

            using Stream dataStream = entry.Open();
            byte[] decompressed = await ReadStreamFully(dataStream, async);
            Assert.Equal(s_smallContent, decompressed);
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task EmptyArchive_ReturnsNull(bool async)
        {
            using MemoryStream ms = new();
            using (new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true)) { }

            ms.Position = 0;
            using ZipArchive archive = new(ms, ZipArchiveMode.ForwardRead);

            ZipArchiveEntry? entry = await GetNextEntry(archive, async);
            Assert.Null(entry);
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task PartialRead_ThenGetNextEntry_AdvancesCorrectly(bool async)
        {
            byte[] zipBytes = CreateZipWithEntries(CompressionLevel.Optimal, seekable: true);

            using MemoryStream archiveStream = new(zipBytes);
            using ZipArchive archive = new(archiveStream, ZipArchiveMode.ForwardRead);

            ZipArchiveEntry? first = await GetNextEntry(archive, async);
            Assert.NotNull(first);

            // Read only a few bytes
            using (Stream ds = first.Open())
            {
                byte[] partial = new byte[5];
                await ReadStream(ds, partial, async);
            }

            ZipArchiveEntry? second = await GetNextEntry(archive, async);

            Assert.NotNull(second);
            Assert.Equal("medium.bin", second.FullName);

            using Stream dataStream = second.Open();
            byte[] decompressed = await ReadStreamFully(dataStream, async);
            Assert.Equal(s_mediumContent, decompressed);
        }

        [Fact]
        public void Entries_ThrowsNotSupportedException()
        {
            byte[] zipBytes = CreateZipWithEntries(CompressionLevel.Optimal, seekable: true);

            using MemoryStream archiveStream = new(zipBytes);
            using ZipArchive archive = new(archiveStream, ZipArchiveMode.ForwardRead);

            Assert.Throws<NotSupportedException>(() => archive.Entries);
        }

        [Fact]
        public void GetEntry_ThrowsNotSupportedException()
        {
            byte[] zipBytes = CreateZipWithEntries(CompressionLevel.Optimal, seekable: true);

            using MemoryStream archiveStream = new(zipBytes);
            using ZipArchive archive = new(archiveStream, ZipArchiveMode.ForwardRead);

            Assert.Throws<NotSupportedException>(() => archive.GetEntry("small.txt"));
        }

        [Fact]
        public void CreateEntry_ThrowsNotSupportedException()
        {
            byte[] zipBytes = CreateZipWithEntries(CompressionLevel.Optimal, seekable: true);

            using MemoryStream archiveStream = new(zipBytes);
            using ZipArchive archive = new(archiveStream, ZipArchiveMode.ForwardRead);

            Assert.Throws<NotSupportedException>(() => archive.CreateEntry("new.txt"));
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task GetNextEntry_AfterDispose_ThrowsObjectDisposedException(bool async)
        {
            byte[] zipBytes = CreateZipWithEntries(CompressionLevel.Optimal, seekable: true);

            using MemoryStream archiveStream = new(zipBytes);
            ZipArchive archive = new(archiveStream, ZipArchiveMode.ForwardRead, leaveOpen: true);

            ZipArchiveEntry? entry = await GetNextEntry(archive, async);
            Assert.NotNull(entry);

            archive.Dispose();

            if (async)
            {
                await Assert.ThrowsAsync<ObjectDisposedException>(() => archive.GetNextEntryAsync().AsTask());
            }
            else
            {
                Assert.Throws<ObjectDisposedException>(() => archive.GetNextEntry());
            }
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task AsyncGetNextEntryAsync_Works(bool async)
        {
            byte[] zipBytes = CreateZipWithEntries(CompressionLevel.Optimal, seekable: true);

            using MemoryStream archiveStream = new(zipBytes);
            using ZipArchive archive = new(archiveStream, ZipArchiveMode.ForwardRead);

            ZipArchiveEntry? entry = await GetNextEntry(archive, async);
            Assert.NotNull(entry);
            Assert.Equal("small.txt", entry.FullName);
        }

        [Fact]
        public async Task AsyncCancellation_ThrowsOperationCanceled()
        {
            byte[] zipBytes = CreateZipWithEntries(CompressionLevel.Optimal, seekable: true);

            using MemoryStream archiveStream = new(zipBytes);
            using ZipArchive archive = new(archiveStream, ZipArchiveMode.ForwardRead);

            using CancellationTokenSource cts = new();
            cts.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => archive.GetNextEntryAsync(cancellationToken: cts.Token).AsTask());
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task MultipleEntries_MixedSkipAndRead(bool async)
        {
            byte[] zipBytes = CreateZipWithEntries(CompressionLevel.Optimal, seekable: true);

            using MemoryStream archiveStream = new(zipBytes);
            using ZipArchive archive = new(archiveStream, ZipArchiveMode.ForwardRead);

            // Skip first entry (don't read data)
            ZipArchiveEntry? first = await GetNextEntry(archive, async);
            Assert.NotNull(first);

            // Read second entry fully
            ZipArchiveEntry? second = await GetNextEntry(archive, async);
            Assert.NotNull(second);
            using (Stream ds = second.Open())
            {
                byte[] data = await ReadStreamFully(ds, async);
                Assert.Equal(s_mediumContent, data);
            }

            // Skip third entry
            ZipArchiveEntry? third = await GetNextEntry(archive, async);
            Assert.NotNull(third);

            // Confirm end
            ZipArchiveEntry? end = await GetNextEntry(archive, async);
            Assert.Null(end);
        }

        [Fact]
        public void GetNextEntry_NotInForwardReadMode_ThrowsNotSupportedException()
        {
            byte[] zipBytes = CreateZipWithEntries(CompressionLevel.Optimal, seekable: true);

            using MemoryStream archiveStream = new(zipBytes);
            using ZipArchive archive = new(archiveStream, ZipArchiveMode.Read);

            Assert.Throws<NotSupportedException>(() => archive.GetNextEntry());
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task StoredWithDataDescriptor_ThrowsNotSupported(bool async)
        {
            byte[] zipBytes = CreateZipWithEntries(CompressionLevel.NoCompression, seekable: false);

            using MemoryStream archiveStream = new(zipBytes);
            using WrappedStream nonSeekableStream = new(archiveStream, canRead: true, canWrite: false, canSeek: false, null);
            using ZipArchive archive = new(nonSeekableStream, ZipArchiveMode.ForwardRead);

            if (async)
            {
                await Assert.ThrowsAsync<NotSupportedException>(() => archive.GetNextEntryAsync().AsTask());
            }
            else
            {
                Assert.Throws<NotSupportedException>(() => archive.GetNextEntry());
            }
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task PartialRead_DataDescriptor_ThenGetNextEntry_AdvancesCorrectly(bool async)
        {
            byte[] zipBytes = CreateZipWithEntries(CompressionLevel.Optimal, seekable: false);

            using MemoryStream archiveStream = new(zipBytes);
            using ZipArchive archive = new(archiveStream, ZipArchiveMode.ForwardRead);

            ZipArchiveEntry? first = await GetNextEntry(archive, async);
            Assert.NotNull(first);

            // Read only a few bytes via Open()
            using (Stream ds = first.Open())
            {
                byte[] partial = new byte[3];
                await ReadStream(ds, partial, async);
            }

            ZipArchiveEntry? second = await GetNextEntry(archive, async);

            Assert.NotNull(second);
            Assert.Equal("medium.bin", second.FullName);

            using Stream dataStream2 = second.Open();
            byte[] decompressed = await ReadStreamFully(dataStream2, async);
            Assert.Equal(s_mediumContent, decompressed);
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task ZeroLengthEntry_ReturnsEntryWithEmptyStream(bool async)
        {
            using MemoryStream ms = new();
            using (ZipArchive create = new(ms, ZipArchiveMode.Create, leaveOpen: true))
            {
                create.CreateEntry("empty.txt");
            }

            ms.Position = 0;
            using ZipArchive archive = new(ms, ZipArchiveMode.ForwardRead);

            ZipArchiveEntry? entry = await GetNextEntry(archive, async);

            Assert.NotNull(entry);
            Assert.Equal("empty.txt", entry.FullName);
            Assert.Equal(0, entry.CompressedLength);

            // Confirm end
            ZipArchiveEntry? end = await GetNextEntry(archive, async);
            Assert.Null(end);
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task DirectoryEntry_ReturnsEntryWithNoDataStream(bool async)
        {
            using MemoryStream ms = new();
            using (ZipArchive create = new(ms, ZipArchiveMode.Create, leaveOpen: true))
            {
                create.CreateEntry("mydir/");
            }

            ms.Position = 0;
            using ZipArchive archive = new(ms, ZipArchiveMode.ForwardRead);

            ZipArchiveEntry? entry = await GetNextEntry(archive, async);

            Assert.NotNull(entry);
            Assert.Equal("mydir/", entry.FullName);
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task Dispose_WhileEntryPartiallyRead_DoesNotThrow(bool async)
        {
            byte[] zipBytes = CreateZipWithEntries(CompressionLevel.Optimal, seekable: true);

            using MemoryStream archiveStream = new(zipBytes);
            ZipArchive archive = new(archiveStream, ZipArchiveMode.ForwardRead, leaveOpen: true);

            ZipArchiveEntry? entry = await GetNextEntry(archive, async);
            Assert.NotNull(entry);

            // Partially read via Open()
            Stream ds = entry.Open();
            byte[] partial = new byte[5];
            await ReadStream(ds, partial, async);

            // Dispose should not throw
            archive.Dispose();
        }

        [Fact]
        public void LeaveOpen_DoesNotDisposeStream()
        {
            byte[] zipBytes = CreateZipWithEntries(CompressionLevel.Optimal, seekable: true);

            using MemoryStream archiveStream = new(zipBytes);

            ZipArchive archive = new(archiveStream, ZipArchiveMode.ForwardRead, leaveOpen: true);
            archive.Dispose();

            Assert.True(archiveStream.CanRead);
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task Open_CalledTwice_Throws(bool async)
        {
            byte[] zipBytes = CreateZipWithEntries(CompressionLevel.Optimal, seekable: true);

            using MemoryStream archiveStream = new(zipBytes);
            using ZipArchive archive = new(archiveStream, ZipArchiveMode.ForwardRead);

            ZipArchiveEntry? entry = await GetNextEntry(archive, async);
            Assert.NotNull(entry);

            using Stream first = entry.Open();
            Assert.Throws<IOException>(() => entry.Open());
        }

        // ── Sync/async dispatch helpers ──────────────────────────────────────

        private static async ValueTask<ZipArchiveEntry?> GetNextEntry(
            ZipArchive archive, bool async)
        {
            return async
                ? await archive.GetNextEntryAsync()
                : archive.GetNextEntry();
        }

        private static async ValueTask<int> ReadStream(Stream stream, byte[] buffer, bool async)
        {
            return async
                ? await stream.ReadAsync(buffer)
                : stream.Read(buffer);
        }

        // ── Test data helpers ────────────────────────────────────────────────

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
        }

        private static void AddEntry(ZipArchive archive, string name, byte[] contents, CompressionLevel level)
        {
            ZipArchiveEntry entry = archive.CreateEntry(name, level);
            using Stream stream = entry.Open();
            stream.Write(contents);
        }

        private static async Task<byte[]> ReadStreamFully(Stream stream, bool async)
        {
            using MemoryStream result = new();
            if (async)
            {
                await stream.CopyToAsync(result);
            }
            else
            {
                stream.CopyTo(result);
            }

            return result.ToArray();
        }
    }
}
