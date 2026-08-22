// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
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
                ZipArchiveEntry? entry = await GetNextEntry(reader, async);

                Assert.NotNull(entry);
                Assert.False(IsDirectory(entry));
                Assert.Equal(ZipCompressionMethod.Deflate, entry.CompressionMethod);

                Stream dataStream = entry.Open();
                byte[] decompressed = await ReadStreamFully(dataStream, async);
                Assert.Equal(expectedContents[i], decompressed);
            }

            ZipArchiveEntry? end = await GetNextEntry(reader, async);
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
                ZipArchiveEntry? entry = await GetNextEntry(reader, async);

                Assert.NotNull(entry);
                Assert.Equal(ZipCompressionMethod.Stored, entry.CompressionMethod);

                Stream dataStream = entry.Open();
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
            using WrappedStream nonSeekableStream = new(archiveStream, canRead: true, canWrite: false, canSeek: false);
            using ZipStreamReader reader = new(nonSeekableStream);

            for (int i = 0; i < expectedContents.Length; i++)
            {
                ZipArchiveEntry? entry = await GetNextEntry(reader, async);

                Assert.NotNull(entry);
                Assert.Equal(ZipCompressionMethod.Deflate, entry.CompressionMethod);

                Stream dataStream = entry.Open();
                byte[] decompressed = await ReadStreamFully(dataStream, async);
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
        public async Task Read_TruncatedLocalFileHeader_ThrowsInvalidData(bool async)
        {
            byte[] zipBytes = CreateZipWithEntries(CompressionLevel.Optimal, seekable: true);

            // Cut the stream in the middle of the first local file header. The reader sees a
            // non-empty partial read that is not an end-of-entries record, so it must treat the
            // archive as truncated rather than silently reporting end-of-archive.
            byte[] truncated = zipBytes[..15];

            using MemoryStream archiveStream = new(truncated);
            using ZipStreamReader reader = new(archiveStream);

            if (async)
            {
                await Assert.ThrowsAsync<InvalidDataException>(() => reader.GetNextEntryAsync().AsTask());
            }
            else
            {
                Assert.Throws<InvalidDataException>(() => reader.GetNextEntry());
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

            ZipArchiveEntry? entry = await GetNextEntry(reader, async, copyData: true);

            Assert.NotNull(entry);
            Stream dataStream = entry.Open();

            ZipArchiveEntry? next = await GetNextEntry(reader, async);
            Assert.NotNull(next);

            dataStream.Position = 0;
            byte[] decompressed = await ReadStreamFully(dataStream, async);
            Assert.Equal(s_smallContent, decompressed);
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task PartialRead_ThenGetNextEntry_AdvancesCorrectly(bool async)
        {
            byte[] zipBytes = CreateZipWithEntries(CompressionLevel.Optimal, seekable: true);

            using MemoryStream archiveStream = new(zipBytes);
            using ZipStreamReader reader = new(archiveStream);

            ZipArchiveEntry? first = await GetNextEntry(reader, async);
            Assert.NotNull(first);

            byte[] partial = new byte[5];
            await ReadStream(first.Open(), partial, async);

            ZipArchiveEntry? second = await GetNextEntry(reader, async);

            Assert.NotNull(second);
            Assert.Equal("medium.bin", second.FullName);

            byte[] decompressed = await ReadStreamFully(second.Open(), async);
            Assert.Equal(s_mediumContent, decompressed);
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task PartialRead_DataDescriptor_ThenGetNextEntry_AdvancesCorrectly(bool async)
        {
            byte[] zipBytes = CreateZipWithEntries(CompressionLevel.Optimal, seekable: false);

            using MemoryStream archiveStream = new(zipBytes);
            using ZipStreamReader reader = new(archiveStream);

            ZipArchiveEntry? first = await GetNextEntry(reader, async);
            Assert.NotNull(first);

            byte[] partial = new byte[3];
            await ReadStream(first.Open(), partial, async);

            ZipArchiveEntry? second = await GetNextEntry(reader, async);

            Assert.NotNull(second);
            Assert.Equal("medium.bin", second.FullName);

            byte[] decompressed = await ReadStreamFully(second.Open(), async);
            Assert.Equal(s_mediumContent, decompressed);
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task Deflate64Entry_ReturnsDecompressedData(bool async)
        {
            MemoryStream ms = await StreamHelpers.CreateTempCopyStream(compat("deflate64.zip"));

            using ZipStreamReader reader = new(ms);

            ZipArchiveEntry? entry = await GetNextEntry(reader, async);

            Assert.NotNull(entry);
            Assert.Equal(ZipCompressionMethod.Deflate64, entry.CompressionMethod);

            byte[] data = await ReadStreamFully(entry.Open(), async);
            Assert.True(data.Length > 0);
        }

        [Theory]
        [InlineData("empty.txt", false, true)]
        [InlineData("empty.txt", false, false)]
        [InlineData("mydir/", true, true)]
        [InlineData("mydir/", true, false)]
        public async Task ZeroLengthEntry_OpenReturnsEmptyStream(string entryName, bool expectedIsDirectory, bool async)
        {
            using MemoryStream ms = new();
            using (ZipArchive archive = new(ms, ZipArchiveMode.Create, leaveOpen: true))
            {
                archive.CreateEntry(entryName);
            }

            ms.Position = 0;
            using ZipStreamReader reader = new(ms);

            ZipArchiveEntry? entry = await GetNextEntry(reader, async);

            Assert.NotNull(entry);
            Assert.Equal(entryName, entry.FullName);
            Assert.Equal(expectedIsDirectory, IsDirectory(entry));
            Assert.Equal(0, entry.CompressedLength);

            byte[] data = await ReadStreamFully(entry.Open(), async);
            Assert.Empty(data);
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task EncryptedEntry_ReportsIsEncrypted(bool async)
        {
            MemoryStream ms = await StreamHelpers.CreateTempCopyStream(zfile("encrypted_entries_weak.zip"));

            using ZipStreamReader reader = new(ms);

            bool foundEncrypted = false;
            bool foundUnencrypted = false;

            ZipArchiveEntry? entry;
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

        [Theory]
        [SkipOnPlatform(TestPlatforms.Browser, "ZIP encryption is not supported on browser.")]
        [InlineData(ZipEncryptionMethod.ZipCrypto, true)]
        [InlineData(ZipEncryptionMethod.ZipCrypto, false)]
        [InlineData(ZipEncryptionMethod.Aes128, true)]
        [InlineData(ZipEncryptionMethod.Aes128, false)]
        [InlineData(ZipEncryptionMethod.Aes192, true)]
        [InlineData(ZipEncryptionMethod.Aes192, false)]
        [InlineData(ZipEncryptionMethod.Aes256, true)]
        [InlineData(ZipEncryptionMethod.Aes256, false)]
        public async Task EncryptedEntry_ReportsMetadata(ZipEncryptionMethod method, bool async)
        {
            // A seekable write stores the real compressed size in the local header for every encryption
            // method (ZipCrypto keeps a trailing data descriptor, AES does not), so the entry can be read
            // forward-only either way.
            byte[] zipBytes = CreateZipWithEncryptedEntry(method, seekable: true, s_smallContent);

            using MemoryStream archiveStream = new(zipBytes);
            using ZipStreamReader reader = new(archiveStream);

            ZipArchiveEntry? entry = await GetNextEntry(reader, async);

            Assert.NotNull(entry);
            Assert.Equal("secret.bin", entry.FullName);
            Assert.True(entry.IsEncrypted);
            Assert.Equal(method, entry.EncryptionMethod);
            Assert.False(IsDirectory(entry));
            Assert.True(entry.CompressedLength > 0);

            ZipArchiveEntry? end = await GetNextEntry(reader, async);
            Assert.Null(end);
        }

        [Theory]
        [SkipOnPlatform(TestPlatforms.Browser, "ZIP encryption is not supported on browser.")]
        [InlineData(ZipEncryptionMethod.ZipCrypto, true)]
        [InlineData(ZipEncryptionMethod.ZipCrypto, false)]
        [InlineData(ZipEncryptionMethod.Aes256, true)]
        [InlineData(ZipEncryptionMethod.Aes256, false)]
        public async Task EncryptedEntry_CanAdvancePastToNextEntry(ZipEncryptionMethod method, bool async)
        {
            // Encryption written to a seekable stream carries the compressed size in the local header, so
            // the reader can drain past the encrypted entry (and skip a ZipCrypto data descriptor) without
            // a password to reach the following plain entry.
            byte[] zipBytes = CreateZipWithEncryptedThenPlainEntry(method);

            using MemoryStream archiveStream = new(zipBytes);
            using ZipStreamReader reader = new(archiveStream);

            ZipArchiveEntry? encrypted = await GetNextEntry(reader, async);
            Assert.NotNull(encrypted);
            Assert.True(encrypted.IsEncrypted);

            ZipArchiveEntry? plain = await GetNextEntry(reader, async);
            Assert.NotNull(plain);
            Assert.False(plain.IsEncrypted);
            Assert.Equal("plain.bin", plain.FullName);

            byte[] data = await ReadStreamFully(plain.Open(), async);
            Assert.Equal(s_mediumContent, data);
        }

        [Theory]
        [SkipOnPlatform(TestPlatforms.Browser, "ZIP encryption is not supported on browser.")]
        [InlineData(ZipEncryptionMethod.ZipCrypto, true)]
        [InlineData(ZipEncryptionMethod.ZipCrypto, false)]
        [InlineData(ZipEncryptionMethod.Aes128, true)]
        [InlineData(ZipEncryptionMethod.Aes128, false)]
        [InlineData(ZipEncryptionMethod.Aes192, true)]
        [InlineData(ZipEncryptionMethod.Aes192, false)]
        [InlineData(ZipEncryptionMethod.Aes256, true)]
        [InlineData(ZipEncryptionMethod.Aes256, false)]
        public async Task EncryptedEntry_OpenWithPassword_ReturnsDecryptedData(ZipEncryptionMethod method, bool async)
        {
            byte[] zipBytes = CreateZipWithEncryptedEntry(method, seekable: true, s_mediumContent);

            using MemoryStream archiveStream = new(zipBytes);
            using ZipStreamReader reader = new(archiveStream);

            ZipArchiveEntry? entry = await GetNextEntry(reader, async);
            Assert.NotNull(entry);
            Assert.True(entry.IsEncrypted);

            using Stream dataStream = await OpenEntry(entry, EncryptionPassword, async);
            byte[] decrypted = await ReadStreamFully(dataStream, async);
            Assert.Equal(s_mediumContent, decrypted);
        }

        [Theory]
        [SkipOnPlatform(TestPlatforms.Browser, "ZIP encryption is not supported on browser.")]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task EncryptedEntry_OpenWithoutPassword_Throws(bool async)
        {
            byte[] zipBytes = CreateZipWithEncryptedEntry(ZipEncryptionMethod.Aes256, seekable: true, s_smallContent);

            using MemoryStream archiveStream = new(zipBytes);
            using ZipStreamReader reader = new(archiveStream);

            ZipArchiveEntry? entry = await GetNextEntry(reader, async);
            Assert.NotNull(entry);
            Assert.True(entry.IsEncrypted);

            if (async)
            {
                await Assert.ThrowsAsync<InvalidDataException>(() => entry.OpenAsync());
            }
            else
            {
                Assert.Throws<InvalidDataException>(() => entry.Open());
            }
        }

        [Theory]
        [SkipOnPlatform(TestPlatforms.Browser, "ZIP encryption is not supported on browser.")]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task EncryptedEntry_OpenWithWrongPassword_Throws(bool async)
        {
            byte[] zipBytes = CreateZipWithEncryptedEntry(ZipEncryptionMethod.Aes256, seekable: true, s_smallContent);

            using MemoryStream archiveStream = new(zipBytes);
            using ZipStreamReader reader = new(archiveStream);

            ZipArchiveEntry? entry = await GetNextEntry(reader, async);
            Assert.NotNull(entry);
            Assert.True(entry.IsEncrypted);

            // A wrong password is rejected by the AES password verifier when the stream is opened.
            await Assert.ThrowsAsync<InvalidDataException>(async () =>
            {
                using Stream dataStream = await OpenEntry(entry, "wrong-password", async);
                await ReadStreamFully(dataStream, async);
            });
        }

        [Theory]
        [SkipOnPlatform(TestPlatforms.Browser, "ZIP encryption is not supported on browser.")]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task EncryptedEntry_StreamedUnknownSize_ThrowsNotSupported(bool async)
        {
            // A ZipCrypto entry written to a non-seekable stream stores a zero compressed size in its
            // local header (the real size lives in the trailing data descriptor), so the forward reader
            // cannot determine the entry boundary and must throw rather than read past the entry.
            byte[] zipBytes = CreateZipWithEncryptedEntry(ZipEncryptionMethod.ZipCrypto, seekable: false, s_smallContent);

            using MemoryStream archiveStream = new(zipBytes);
            using ZipStreamReader reader = new(archiveStream);

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
        [SkipOnPlatform(TestPlatforms.Browser, "ZIP encryption is not supported on browser.")]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task EncryptedEntry_ExternalArchive_DecryptsForwardOnly(bool async)
        {
            // A real archive (produced by another tool) storing both a ZipCrypto entry (hello.txt) and an
            // AES-256 entry (goodbye.txt) with the compressed size in each local header (no data descriptor),
            // so both can be decrypted while reading forward-only.
            const string password = "S3cur3P@ssw0rd";
            using Stream fileStream = await StreamHelpers.CreateTempCopyStream(passwordProtected("PasswordProtected_MixedEncryptions.zip"));
            byte[] archiveBytes = ((MemoryStream)fileStream).ToArray();

            using MemoryStream archiveStream = new(archiveBytes);
            using ZipStreamReader reader = new(archiveStream);

            var decryptedContents = new Dictionary<string, string>();
            var methods = new Dictionary<string, ZipEncryptionMethod>();

            ZipArchiveEntry? entry;
            while ((entry = await GetNextEntry(reader, async)) is not null)
            {
                Assert.True(entry.IsEncrypted);

                methods[entry.FullName] = entry.EncryptionMethod;

                using Stream dataStream = await OpenEntry(entry, password, async);
                byte[] data = await ReadStreamFully(dataStream, async);
                decryptedContents[entry.FullName] = Encoding.UTF8.GetString(data).TrimEnd();
            }

            Assert.Equal(ZipEncryptionMethod.ZipCrypto, methods["hello.txt"]);
            Assert.Equal(ZipEncryptionMethod.Aes256, methods["goodbye.txt"]);
            Assert.Equal("Hello", decryptedContents["hello.txt"]);
            Assert.Equal("Goodbye", decryptedContents["goodbye.txt"]);
        }

        [Theory]
        [SkipOnPlatform(TestPlatforms.Browser, "ZIP encryption is not supported on browser.")]
        [InlineData(ZipEncryptionMethod.ZipCrypto, true)]
        [InlineData(ZipEncryptionMethod.ZipCrypto, false)]
        [InlineData(ZipEncryptionMethod.Aes256, true)]
        [InlineData(ZipEncryptionMethod.Aes256, false)]
        public async Task EncryptedEntry_CopyData_SkipsDataDescriptor_AndAdvances(ZipEncryptionMethod method, bool async)
        {
            // With copyData, the reader buffers the (still-encrypted) bytes and must consume any trailing
            // data descriptor (ZipCrypto always has one) so the following entry is found. The buffered copy
            // remains decryptable after the reader advances.
            byte[] zipBytes = CreateZipWithEncryptedThenPlainEntry(method);

            using MemoryStream archiveStream = new(zipBytes);
            using ZipStreamReader reader = new(archiveStream);

            ZipArchiveEntry? encrypted = await GetNextEntry(reader, async, copyData: true);
            Assert.NotNull(encrypted);
            Assert.True(encrypted.IsEncrypted);

            // If the trailing data descriptor were not skipped, the reader would be misaligned here.
            ZipArchiveEntry? plain = await GetNextEntry(reader, async);
            Assert.NotNull(plain);
            Assert.Equal("plain.bin", plain.FullName);
            Assert.Equal(s_mediumContent, await ReadStreamFully(plain.Open(), async));

            // The buffered encrypted entry is still decryptable after advancing.
            using Stream decrypted = await OpenEntry(encrypted, EncryptionPassword, async);
            Assert.Equal(s_smallContent, await ReadStreamFully(decrypted, async));
        }

        [Theory]
        [SkipOnPlatform(TestPlatforms.Browser, "ZIP encryption is not supported on browser.")]
        [InlineData(ZipEncryptionMethod.ZipCrypto, true)]
        [InlineData(ZipEncryptionMethod.ZipCrypto, false)]
        [InlineData(ZipEncryptionMethod.Aes256, true)]
        [InlineData(ZipEncryptionMethod.Aes256, false)]
        public async Task EncryptedEntry_PartialRead_ThenGetNextEntry_AdvancesCorrectly(ZipEncryptionMethod method, bool async)
        {
            byte[] zipBytes = CreateZipWithEncryptedThenPlainEntry(method);

            using MemoryStream archiveStream = new(zipBytes);
            using ZipStreamReader reader = new(archiveStream);

            ZipArchiveEntry? encrypted = await GetNextEntry(reader, async);
            Assert.NotNull(encrypted);
            Assert.True(encrypted.IsEncrypted);

            // Decrypt and read only a few bytes, then advance: the reader must drain the rest of the
            // partially-consumed encrypted stream and skip any trailing descriptor.
            using (Stream dataStream = await OpenEntry(encrypted, EncryptionPassword, async))
            {
                byte[] partial = new byte[4];
                await ReadStream(dataStream, partial, async);
            }

            ZipArchiveEntry? plain = await GetNextEntry(reader, async);
            Assert.NotNull(plain);
            Assert.Equal("plain.bin", plain.FullName);
            Assert.Equal(s_mediumContent, await ReadStreamFully(plain.Open(), async));
        }

        [Theory]
        [SkipOnPlatform(TestPlatforms.Browser, "ZIP encryption is not supported on browser.")]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task EncryptedEntry_OpenWithoutPassword_ThenRetryWithPassword_Succeeds(bool async)
        {
            // A failed open that reads no bytes (missing password) must not consume the single-use stream,
            // so a subsequent open with the correct password still succeeds.
            byte[] zipBytes = CreateZipWithEncryptedEntry(ZipEncryptionMethod.Aes256, seekable: true, s_mediumContent);

            using MemoryStream archiveStream = new(zipBytes);
            using ZipStreamReader reader = new(archiveStream);

            ZipArchiveEntry? entry = await GetNextEntry(reader, async);
            Assert.NotNull(entry);
            Assert.True(entry.IsEncrypted);

            if (async)
            {
                await Assert.ThrowsAsync<InvalidDataException>(() => entry.OpenAsync());
            }
            else
            {
                Assert.Throws<InvalidDataException>(() => entry.Open());
            }

            using Stream dataStream = await OpenEntry(entry, EncryptionPassword, async);
            byte[] decrypted = await ReadStreamFully(dataStream, async);
            Assert.Equal(s_mediumContent, decrypted);
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

            ZipArchiveEntry? entry = await GetNextEntry(reader, async);

            Assert.NotNull(entry);

            byte[] partial = new byte[5];
            await ReadStream(entry.Open(), partial, async);

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

            ZipArchiveEntry? entry = await GetNextEntry(reader, async);

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

            ZipArchiveEntry? entry = await GetNextEntry(reader, async);

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
            ZipArchiveEntry? first = await GetNextEntry(reader, async);
            Assert.NotNull(first);

            // Read second entry fully
            ZipArchiveEntry? second = await GetNextEntry(reader, async);
            Assert.NotNull(second);
            byte[] data = await ReadStreamFully(second.Open(), async);
            Assert.Equal(s_mediumContent, data);

            // Skip third entry
            ZipArchiveEntry? third = await GetNextEntry(reader, async);
            Assert.NotNull(third);

            // Confirm end
            ZipArchiveEntry? end = await GetNextEntry(reader, async);
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
            ZipArchiveEntry? entry = await GetNextEntry(reader, async);
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
            ZipArchiveEntry? first = await GetNextEntry(reader, async, copyData: true);

            Assert.NotNull(first);
            Stream firstData = first.Open();

            // Advance to the next entry to confirm the stream position is correct
            // after consuming the data descriptor.
            ZipArchiveEntry? second = await GetNextEntry(reader, async);
            Assert.NotNull(second);
            Assert.Equal("medium.bin", second.FullName);

            // The copied first entry's data should still be fully readable.
            firstData.Position = 0;
            byte[] decompressed = await ReadStreamFully(firstData, async);
            Assert.Equal(s_smallContent, decompressed);

            // Also verify the second entry's data is correct.
            byte[] secondData = await ReadStreamFully(second.Open(), async);
            Assert.Equal(s_mediumContent, secondData);
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task Open_CalledTwice_ThrowsInvalidOperation(bool async)
        {
            byte[] zipBytes = CreateZipWithEntries(CompressionLevel.Optimal, seekable: true);

            using MemoryStream archiveStream = new(zipBytes);
            using ZipStreamReader reader = new(archiveStream);

            ZipArchiveEntry? entry = await GetNextEntry(reader, async);

            Assert.NotNull(entry);
            entry.Open();

            Assert.Throws<InvalidOperationException>(() => entry.Open());
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

        private static async ValueTask<ZipArchiveEntry?> GetNextEntry(
            ZipStreamReader reader, bool async, bool copyData = false)
        {
            return async
                ? await reader.GetNextEntryAsync(copyData: copyData)
                : reader.GetNextEntry(copyData: copyData);
        }

        private static async ValueTask<Stream> OpenEntry(ZipArchiveEntry entry, bool async) =>
            async ? await entry.OpenAsync() : entry.Open();

        private static async ValueTask<Stream> OpenEntry(ZipArchiveEntry entry, FileAccess access, bool async) =>
            async ? await entry.OpenAsync(access) : entry.Open(access);

        private static async ValueTask<Stream> OpenEntry(ZipArchiveEntry entry, string password, bool async) =>
            async ? await entry.OpenAsync(password) : entry.Open(password);

        private static bool IsDirectory(ZipArchiveEntry entry) =>
            entry.FullName.Length > 0 && (entry.FullName[^1] is '/' or '\\');

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

        private const string EncryptionPassword = "forward-read-secret";

        private static byte[] CreateZipWithEncryptedEntry(ZipEncryptionMethod method, bool seekable, byte[] contents)
        {
            MemoryStream ms = new();

            Stream writeStream = seekable
                ? ms
                : new WrappedStream(ms, canRead: true, canWrite: true, canSeek: false);

            using (ZipArchive archive = new(writeStream, ZipArchiveMode.Create, leaveOpen: true))
            {
                ZipArchiveEntry entry = archive.CreateEntry("secret.bin", CompressionLevel.Optimal, EncryptionPassword.AsSpan(), method);
                using Stream stream = entry.Open();
                stream.Write(contents);
            }

            return ms.ToArray();
        }

        private static byte[] CreateZipWithEncryptedThenPlainEntry(ZipEncryptionMethod method)
        {
            MemoryStream ms = new();

            using (ZipArchive archive = new(ms, ZipArchiveMode.Create, leaveOpen: true))
            {
                ZipArchiveEntry encrypted = archive.CreateEntry("encrypted.bin", CompressionLevel.Optimal, EncryptionPassword.AsSpan(), method);
                using (Stream stream = encrypted.Open())
                {
                    stream.Write(s_smallContent);
                }

                AddEntry(archive, "plain.bin", s_mediumContent, CompressionLevel.Optimal);
            }

            return ms.ToArray();
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
