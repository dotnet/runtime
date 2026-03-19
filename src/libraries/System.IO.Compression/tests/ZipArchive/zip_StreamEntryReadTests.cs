// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
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

        public static IEnumerable<object[]> DeflateWithKnownSize_Data()
        {
            foreach (bool async in _bools)
            {
                yield return new object[] { async };
            }
        }

        public static IEnumerable<object[]> StoredWithKnownSize_Data()
        {
            foreach (bool async in _bools)
            {
                yield return new object[] { async };
            }
        }

        public static IEnumerable<object[]> DeflateWithDataDescriptor_Data()
        {
            foreach (bool async in _bools)
            {
                yield return new object[] { async };
            }
        }

        public static IEnumerable<object[]> StoredWithDataDescriptor_Data()
        {
            foreach (bool async in _bools)
            {
                yield return new object[] { async };
            }
        }

        [Theory]
        [MemberData(nameof(DeflateWithKnownSize_Data))]
        public async Task Read_DeflateWithKnownSize_ReturnsDecompressedData(bool async)
        {
            byte[] zipBytes = CreateZipWithEntries(CompressionLevel.Optimal, seekable: true);
            byte[][] expectedContents = [s_smallContent, s_mediumContent, s_largeContent];

            using MemoryStream archiveStream = new(zipBytes);
            using ZipStreamReader reader = new(archiveStream);

            for (int i = 0; i < expectedContents.Length; i++)
            {
                ZipStreamEntry entry = async
                    ? await reader.GetNextEntryAsync()
                    : reader.GetNextEntry();

                Assert.NotNull(entry);
                Assert.False(entry.IsDirectory);
                Assert.Equal(ZipCompressionMethod.Deflate, entry.CompressionMethod);

                byte[] decompressed = await ReadEntryFully(entry, async);
                Assert.Equal(expectedContents[i], decompressed);
            }

            ZipStreamEntry end = async
                ? await reader.GetNextEntryAsync()
                : reader.GetNextEntry();
            Assert.Null(end);
        }

        [Theory]
        [MemberData(nameof(StoredWithKnownSize_Data))]
        public async Task Read_StoredWithKnownSize_ReturnsUncompressedData(bool async)
        {
            byte[] zipBytes = CreateZipWithEntries(CompressionLevel.NoCompression, seekable: true);
            byte[][] expectedContents = [s_smallContent, s_mediumContent, s_largeContent];

            using MemoryStream archiveStream = new(zipBytes);
            using ZipStreamReader reader = new(archiveStream);

            for (int i = 0; i < expectedContents.Length; i++)
            {
                ZipStreamEntry entry = async
                    ? await reader.GetNextEntryAsync()
                    : reader.GetNextEntry();

                Assert.NotNull(entry);
                Assert.Equal(ZipCompressionMethod.Stored, entry.CompressionMethod);

                byte[] decompressed = await ReadEntryFully(entry, async);
                Assert.Equal(expectedContents[i], decompressed);
            }
        }

        [Theory]
        [MemberData(nameof(DeflateWithDataDescriptor_Data))]
        public async Task Read_DeflateWithDataDescriptor_ReturnsDecompressedData(bool async)
        {
            byte[] zipBytes = CreateZipWithEntries(CompressionLevel.Optimal, seekable: false);
            byte[][] expectedContents = [s_smallContent, s_mediumContent, s_largeContent];

            using MemoryStream archiveStream = new(zipBytes);
            using ZipStreamReader reader = new(archiveStream);

            for (int i = 0; i < expectedContents.Length; i++)
            {
                ZipStreamEntry entry = async
                    ? await reader.GetNextEntryAsync()
                    : reader.GetNextEntry();

                Assert.NotNull(entry);
                Assert.Equal(ZipCompressionMethod.Deflate, entry.CompressionMethod);

                byte[] decompressed = await ReadEntryFully(entry, async);
                Assert.Equal(expectedContents[i], decompressed);
            }
        }

        [Theory]
        [MemberData(nameof(StoredWithDataDescriptor_Data))]
        public async Task Read_StoredWithDataDescriptor_ThrowsNotSupported(bool async)
        {
            byte[] zipBytes = CreateZipWithEntries(CompressionLevel.NoCompression, seekable: false);

            using MemoryStream archiveStream = new(zipBytes);
            using ZipStreamReader reader = new(archiveStream);

            ZipStreamEntry entry = async
                ? await reader.GetNextEntryAsync()
                : reader.GetNextEntry();

            Assert.NotNull(entry);
            Assert.Equal(ZipCompressionMethod.Stored, entry.CompressionMethod);

            byte[] buffer = new byte[256];

            if (async)
            {
                await Assert.ThrowsAsync<NotSupportedException>(() => entry.ReadAsync(buffer).AsTask());
            }
            else
            {
                Assert.Throws<NotSupportedException>(() => entry.Read(buffer));
            }
        }

        /// <summary>
        /// Creates a ZIP archive with three entries of different sizes (small, medium, large).
        /// When <paramref name="seekable"/> is true, the archive is written to a seekable stream
        /// so entries have known sizes. When false, a non-seekable wrapper is used so the
        /// archive writer sets the data descriptor bit.
        /// </summary>
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

        private static async Task<byte[]> ReadEntryFully(ZipStreamEntry entry, bool async)
        {
            using MemoryStream result = new();
            byte[] buffer = new byte[4096];

            int bytesRead;
            if (async)
            {
                while ((bytesRead = await entry.ReadAsync(buffer)) > 0)
                {
                    result.Write(buffer, 0, bytesRead);
                }
            }
            else
            {
                while ((bytesRead = entry.Read(buffer)) > 0)
                {
                    result.Write(buffer, 0, bytesRead);
                }
            }

            return result.ToArray();
        }
    }
}
