// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Compression.Tests
{
    public partial class zip_StreamEntryReadTests : ZipFileTestBase
    {
        private static readonly byte[] s_smallContent = "Hello, small world!"u8.ToArray();
        private static readonly byte[] s_mediumContent = new byte[8192];
        private static readonly byte[] s_largeContent = new byte[65536];

        static zip_StreamEntryReadTests()
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
            using ZipStreamReader reader = new(archiveStream);

            for (int i = 0; i < expectedContents.Length; i++)
            {
                ZipForwardReadEntry? entry = await GetNextEntry(reader, async);

                Assert.NotNull(entry);
                Assert.NotNull(entry.DataStream);
                Assert.False(entry.IsDirectory);
                Assert.Equal(ZipCompressionMethod.Deflate, entry.CompressionMethod);

                byte[] decompressed = await ReadStreamFully(entry.DataStream, async);
                Assert.Equal(expectedContents[i], decompressed);
            }

            ZipForwardReadEntry? end = await GetNextEntry(reader, async);
            Assert.Null(end);
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task Read_StoredWithKnownSize_ReturnsUncompressedData(bool async)
        {
            byte[] zipBytes = CreateZipWithEntries(CompressionLevel.NoCompression, seekable: true);
            byte[][] expectedContents = [s_smallContent, s_mediumContent, s_largeContent];

            using MemoryStream archiveStream = new(zipBytes);
            using ZipStreamReader reader = new(archiveStream);

            for (int i = 0; i < expectedContents.Length; i++)
            {
                ZipForwardReadEntry? entry = await GetNextEntry(reader, async);

                Assert.NotNull(entry);
                Assert.NotNull(entry.DataStream);
                Assert.Equal(ZipCompressionMethod.Stored, entry.CompressionMethod);

                byte[] decompressed = await ReadStreamFully(entry.DataStream, async);
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
            using WrappedStream nonSeekableStream = new(archiveStream, canRead: true, canWrite: false, canSeek: false);
            using ZipStreamReader reader = new(nonSeekableStream);

            for (int i = 0; i < expectedContents.Length; i++)
            {
                ZipForwardReadEntry? entry = await GetNextEntry(reader, async);

                Assert.NotNull(entry);
                Assert.NotNull(entry.DataStream);
                Assert.Equal(ZipCompressionMethod.Deflate, entry.CompressionMethod);

                byte[] decompressed = await ReadStreamFully(entry.DataStream, async);
                Assert.Equal(expectedContents[i], decompressed);
            }
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task Read_StoredWithDataDescriptor_ThrowsNotSupported(bool async)
        {
            byte[] zipBytes = CreateZipWithEntries(CompressionLevel.NoCompression, seekable: false);

            using MemoryStream archiveStream = new(zipBytes);
            using WrappedStream nonSeekableStream = new(archiveStream, canRead: true, canWrite: false, canSeek: false);
            using ZipStreamReader reader = new(nonSeekableStream);

            if (async)
            {
                await Assert.ThrowsAsync<NotSupportedException>(() => reader.GetNextEntryAsync().AsTask());
            }
            else
            {
                Assert.Throws<NotSupportedException>(() => reader.GetNextEntry());
            }
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task CopyData_PreservesEntryAfterAdvancing(bool async)
        {
            byte[] zipBytes = CreateZipWithEntries(CompressionLevel.Optimal, seekable: true);

            using MemoryStream archiveStream = new(zipBytes);
            using WrappedStream nonSeekableStream = new(archiveStream, canRead: true, canWrite: false, canSeek: false);
            using ZipStreamReader reader = new(nonSeekableStream);  

            ZipForwardReadEntry? entry = await GetNextEntry(reader, async, copyData: true);

            Assert.NotNull(entry);
            Assert.NotNull(entry.DataStream);

            ZipForwardReadEntry? next = await GetNextEntry(reader, async);
            Assert.NotNull(next);

            entry.DataStream.Position = 0;
            byte[] decompressed = await ReadStreamFully(entry.DataStream, async);
            Assert.Equal(s_smallContent, decompressed);
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task PartialRead_ThenGetNextEntry_AdvancesCorrectly(bool async)
        {
            byte[] zipBytes = CreateZipWithEntries(CompressionLevel.Optimal, seekable: true);

            using MemoryStream archiveStream = new(zipBytes);
            using ZipStreamReader reader = new(archiveStream);

            ZipForwardReadEntry? first = await GetNextEntry(reader, async);
            Assert.NotNull(first);

            byte[] partial = new byte[5];
            await ReadStream(first.DataStream!, partial, async);

            ZipForwardReadEntry? second = await GetNextEntry(reader, async);

            Assert.NotNull(second);
            Assert.Equal("medium.bin", second.FullName);

            byte[] decompressed = await ReadStreamFully(second.DataStream!, async);
            Assert.Equal(s_mediumContent, decompressed);
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task PartialRead_DataDescriptor_ThenGetNextEntry_AdvancesCorrectly(bool async)
        {
            byte[] zipBytes = CreateZipWithEntries(CompressionLevel.Optimal, seekable: false);

            using MemoryStream archiveStream = new(zipBytes);
            using ZipStreamReader reader = new(archiveStream);

            ZipForwardReadEntry? first = await GetNextEntry(reader, async);
            Assert.NotNull(first);

            byte[] partial = new byte[3];
            await ReadStream(first.DataStream!, partial, async);

            ZipForwardReadEntry? second = await GetNextEntry(reader, async);

            Assert.NotNull(second);
            Assert.Equal("medium.bin", second.FullName);

            byte[] decompressed = await ReadStreamFully(second.DataStream!, async);
            Assert.Equal(s_mediumContent, decompressed);
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task Deflate64Entry_ReturnsDecompressedData(bool async)
        {
            MemoryStream ms = await StreamHelpers.CreateTempCopyStream(compat("deflate64.zip"));

            using ZipStreamReader reader = new(ms);

            ZipForwardReadEntry? entry = await GetNextEntry(reader, async);

            Assert.NotNull(entry);
            Assert.Equal(ZipCompressionMethod.Deflate64, entry.CompressionMethod);
            Assert.NotNull(entry.DataStream);

            byte[] data = await ReadStreamFully(entry.DataStream, async);
            Assert.True(data.Length > 0);
        }

        [Theory]
        [InlineData("empty.txt", false, true)]
        [InlineData("empty.txt", false, false)]
        [InlineData("mydir/", true, true)]
        [InlineData("mydir/", true, false)]
        public async Task ZeroLengthEntry_HasNullDataStream(string entryName, bool expectedIsDirectory, bool async)
        {
            using MemoryStream ms = new();
            using (ZipArchive archive = new(ms, ZipArchiveMode.Create, leaveOpen: true))
            {
                archive.CreateEntry(entryName);
            }

            ms.Position = 0;
            using ZipStreamReader reader = new(ms);

            ZipForwardReadEntry? entry = await GetNextEntry(reader, async);

            Assert.NotNull(entry);
            Assert.Equal(entryName, entry.FullName);
            Assert.Equal(expectedIsDirectory, entry.IsDirectory);
            Assert.Null(entry.DataStream);
            Assert.Equal(0, entry.CompressedLength);
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task EncryptedEntry_ReportsIsEncrypted(bool async)
        {
            MemoryStream ms = await StreamHelpers.CreateTempCopyStream(zfile("encrypted_entries_weak.zip"));

            using ZipStreamReader reader = new(ms);

            bool foundEncrypted = false;
            bool foundUnencrypted = false;

            ZipForwardReadEntry? entry;
            while ((entry = await GetNextEntry(reader, async)) is not null)
            {
                if (entry.IsEncrypted)
                    foundEncrypted = true;
                else
                    foundUnencrypted = true;
            }

            Assert.True(foundEncrypted);
            Assert.True(foundUnencrypted);
        }

        [Fact]
        public async Task AsyncCancellation_ThrowsOperationCanceled()
        {
            byte[] zipBytes = CreateZipWithEntries(CompressionLevel.Optimal, seekable: true);

            using MemoryStream archiveStream = new(zipBytes);
            using ZipStreamReader reader = new(archiveStream);

            using CancellationTokenSource cts = new();
            cts.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => reader.GetNextEntryAsync(cancellationToken: cts.Token).AsTask());
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task Dispose_WhileEntryPartiallyRead_DoesNotThrow(bool async)
        {
            byte[] zipBytes = CreateZipWithEntries(CompressionLevel.Optimal, seekable: true);

            using MemoryStream archiveStream = new(zipBytes);
            ZipStreamReader reader = new(archiveStream);

            ZipForwardReadEntry? entry = await GetNextEntry(reader, async);

            Assert.NotNull(entry);

            byte[] partial = new byte[5];
            await ReadStream(entry.DataStream!, partial, async);

            await DisposeReader(reader, async);
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task EmptyArchive_ReturnsNull(bool async)
        {
            using MemoryStream ms = new();
            using (new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true)) { }

            ms.Position = 0;
            using ZipStreamReader reader = new(ms);

            ZipForwardReadEntry? entry = await GetNextEntry(reader, async);

            Assert.Null(entry);
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task LeaveOpen_DoesNotDisposeStream(bool async)
        {
            byte[] zipBytes = CreateZipWithEntries(CompressionLevel.Optimal, seekable: true);

            using MemoryStream archiveStream = new(zipBytes);

            ZipStreamReader reader = new(archiveStream, leaveOpen: true);
            await DisposeReader(reader, async);

            Assert.True(archiveStream.CanRead);
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task Constructor_WithEncoding_ReadsEntryNames(bool async)
        {
            using MemoryStream ms = new();
            using (ZipArchive archive = new(ms, ZipArchiveMode.Create, leaveOpen: true))
            {
                AddEntry(archive, "hello.txt", s_smallContent, CompressionLevel.Optimal);
            }

            ms.Position = 0;
            using ZipStreamReader reader = new(ms, entryNameEncoding: Encoding.UTF8, leaveOpen: true);

            ZipForwardReadEntry? entry = await GetNextEntry(reader, async);

            Assert.NotNull(entry);
            Assert.Equal("hello.txt", entry.FullName);
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task MultipleEntries_MixedSkipAndRead(bool async)
        {
            byte[] zipBytes = CreateZipWithEntries(CompressionLevel.Optimal, seekable: true);

            using MemoryStream archiveStream = new(zipBytes);
            using ZipStreamReader reader = new(archiveStream);

            // Skip first entry
            ZipForwardReadEntry? first = await GetNextEntry(reader, async);
            Assert.NotNull(first);

            // Read second entry fully
            ZipForwardReadEntry? second = await GetNextEntry(reader, async);
            Assert.NotNull(second);
            byte[] data = await ReadStreamFully(second.DataStream!, async);
            Assert.Equal(s_mediumContent, data);

            // Skip third entry
            ZipForwardReadEntry? third = await GetNextEntry(reader, async);
            Assert.NotNull(third);

            // Confirm end
            ZipForwardReadEntry? end = await GetNextEntry(reader, async);
            Assert.Null(end);
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task GetNextEntry_AfterDispose_ThrowsObjectDisposedException(bool async)
        {
            byte[] zipBytes = CreateZipWithEntries(CompressionLevel.Optimal, seekable: true);

            using MemoryStream archiveStream = new(zipBytes);
            ZipStreamReader reader = new(archiveStream);

            // Read one entry to ensure the reader was functional.
            ZipForwardReadEntry? entry = await GetNextEntry(reader, async);
            Assert.NotNull(entry);

            await DisposeReader(reader, async);

            if (async)
            {
                await Assert.ThrowsAsync<ObjectDisposedException>(() => reader.GetNextEntryAsync().AsTask());
            }
            else
            {
                Assert.Throws<ObjectDisposedException>(() => reader.GetNextEntry());
            }
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task CopyData_WithDataDescriptor_PreservesEntryAfterAdvancing(bool async)
        {
            // seekable: false triggers data descriptors for Deflate entries.
            byte[] zipBytes = CreateZipWithEntries(CompressionLevel.Optimal, seekable: false);

            using MemoryStream archiveStream = new(zipBytes);
            using ZipStreamReader reader = new(archiveStream);

            // Read first entry with copyData: true — exercises the path that
            // eagerly decompresses, copies into a MemoryStream, then reads the
            // data descriptor to validate CRC.
            ZipForwardReadEntry? first = await GetNextEntry(reader, async, copyData: true);

            Assert.NotNull(first);
            Assert.NotNull(first.DataStream);

            // Advance to the next entry to confirm the stream position is correct
            // after consuming the data descriptor.
            ZipForwardReadEntry? second = await GetNextEntry(reader, async);
            Assert.NotNull(second);
            Assert.Equal("medium.bin", second.FullName);

            // The copied first entry's data should still be fully readable.
            first.DataStream.Position = 0;
            byte[] decompressed = await ReadStreamFully(first.DataStream, async);
            Assert.Equal(s_smallContent, decompressed);

            // Also verify the second entry's data is correct.
            byte[] secondData = await ReadStreamFully(second.DataStream!, async);
            Assert.Equal(s_mediumContent, secondData);
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task ExtractToFile_CreatesFileWithExpectedContent(bool async)
        {
            byte[] zipBytes = CreateZipWithEntries(CompressionLevel.Optimal, seekable: true);

            using MemoryStream archiveStream = new(zipBytes);
            using ZipStreamReader reader = new(archiveStream);

            ZipForwardReadEntry? entry = await GetNextEntry(reader, async);

            Assert.NotNull(entry);
            Assert.Equal("small.txt", entry.FullName);

            string tempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            try
            {
                await ExtractEntryToFile(entry, tempPath, overwrite: true, async);

                byte[] written = File.ReadAllBytes(tempPath);
                Assert.Equal(s_smallContent, written);
            }
            finally
            {
                File.Delete(tempPath);
            }
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task ExtractToFile_OverwriteTrue_ReplacesExistingFile(bool async)
        {
            byte[] zipBytes = CreateZipWithEntries(CompressionLevel.Optimal, seekable: true);

            using MemoryStream archiveStream = new(zipBytes);
            using ZipStreamReader reader = new(archiveStream);

            ZipForwardReadEntry? entry = await GetNextEntry(reader, async);

            Assert.NotNull(entry);

            string tempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            try
            {
                // Create a pre-existing file with different content.
                File.WriteAllText(tempPath, "old content");

                await ExtractEntryToFile(entry, tempPath, overwrite: true, async);

                byte[] written = File.ReadAllBytes(tempPath);
                Assert.Equal(s_smallContent, written);
            }
            finally
            {
                File.Delete(tempPath);
            }
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task ExtractToFile_OverwriteFalse_ThrowsWhenFileExists(bool async)
        {
            byte[] zipBytes = CreateZipWithEntries(CompressionLevel.Optimal, seekable: true);

            using MemoryStream archiveStream = new(zipBytes);
            using ZipStreamReader reader = new(archiveStream);

            ZipForwardReadEntry? entry = await GetNextEntry(reader, async);

            Assert.NotNull(entry);

            string tempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            try
            {
                File.WriteAllText(tempPath, "existing");

                if (async)
                    await Assert.ThrowsAsync<IOException>(() => entry.ExtractToFileAsync(tempPath, overwrite: false));
                else
                    Assert.Throws<IOException>(() => entry.ExtractToFile(tempPath, overwrite: false));
            }
            finally
            {
                File.Delete(tempPath);
            }
        }

        [Fact]
        public void Constructor_NullStream_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("stream", () => new ZipStreamReader(null!));
        }

        [Fact]
        public void Constructor_UnreadableStream_ThrowsArgumentException()
        {
            using MemoryStream ms = new();
            using WrappedStream unreadable = new(ms, canRead: false, canWrite: true, canSeek: true);

            Assert.Throws<ArgumentException>("stream", () => new ZipStreamReader(unreadable));
        }

        // ── Sync/async dispatch helpers ──────────────────────────────────────

        private static async ValueTask<ZipForwardReadEntry?> GetNextEntry(
            ZipStreamReader reader, bool async, bool copyData = false)
        {
            return async
                ? await reader.GetNextEntryAsync(copyData: copyData)
                : reader.GetNextEntry(copyData: copyData);
        }

        private static async Task ExtractEntryToFile(
            ZipForwardReadEntry entry, string destinationFileName, bool overwrite, bool async)
        {
            if (async)
                await entry.ExtractToFileAsync(destinationFileName, overwrite);
            else
                entry.ExtractToFile(destinationFileName, overwrite);
        }

        private static async Task DisposeReader(ZipStreamReader reader, bool async)
        {
            if (async)
                await reader.DisposeAsync();
            else
                reader.Dispose();
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
                : new WrappedStream(ms, canRead: true, canWrite: true, canSeek: false);

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
            byte[] buffer = new byte[4096];

            int bytesRead;
            if (async)
            {
                while ((bytesRead = await stream.ReadAsync(buffer)) > 0)
                {
                    result.Write(buffer, 0, bytesRead);
                }
            }
            else
            {
                while ((bytesRead = stream.Read(buffer)) > 0)
                {
                    result.Write(buffer, 0, bytesRead);
                }
            }

            return result.ToArray();
        }
    }
}
