// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace System.Formats.Tar.Tests
{
    public class TarReader_File_Async_Tests : TarReader_File_Tests_Async_Base
    {
        [Theory]
        [InlineData(TarEntryFormat.V7, TestTarFormat.v7)]
        [InlineData(TarEntryFormat.Ustar, TestTarFormat.ustar)]
        [InlineData(TarEntryFormat.Pax, TestTarFormat.pax)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.gnu)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.oldgnu)]
        public Task Read_Archive_File_Async(TarEntryFormat format, TestTarFormat testFormat) =>
            Read_Archive_File_Async_Internal(format, testFormat);

        [Theory]
        [InlineData(TarEntryFormat.V7, TestTarFormat.v7)]
        [InlineData(TarEntryFormat.Ustar, TestTarFormat.ustar)]
        [InlineData(TarEntryFormat.Pax, TestTarFormat.pax)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.gnu)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.oldgnu)]
        public Task Read_Archive_File_HardLink_Async(TarEntryFormat format, TestTarFormat testFormat) =>
            Read_Archive_File_HardLink_Async_Internal(format, testFormat);

        [Theory]
        [InlineData(TarEntryFormat.V7, TestTarFormat.v7)]
        [InlineData(TarEntryFormat.Ustar, TestTarFormat.ustar)]
        [InlineData(TarEntryFormat.Pax, TestTarFormat.pax)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.gnu)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.oldgnu)]
        public Task Read_Archive_File_SymbolicLink_Async(TarEntryFormat format, TestTarFormat testFormat) =>
            Read_Archive_File_SymbolicLink_Async_Internal(format, testFormat);

        [Theory]
        [InlineData(TarEntryFormat.V7, TestTarFormat.v7)]
        [InlineData(TarEntryFormat.Ustar, TestTarFormat.ustar)]
        [InlineData(TarEntryFormat.Pax, TestTarFormat.pax)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.gnu)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.oldgnu)]
        public Task Read_Archive_Folder_File_Async(TarEntryFormat format, TestTarFormat testFormat) =>
            Read_Archive_Folder_File_Async_Internal(format, testFormat);

        [Theory]
        [InlineData(TarEntryFormat.V7, TestTarFormat.v7)]
        [InlineData(TarEntryFormat.Ustar, TestTarFormat.ustar)]
        [InlineData(TarEntryFormat.Pax, TestTarFormat.pax)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.gnu)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.oldgnu)]
        public Task Read_Archive_Folder_File_Utf8_Async(TarEntryFormat format, TestTarFormat testFormat) =>
            Read_Archive_Folder_File_Utf8_Async_Internal(format, testFormat);

        [Theory]
        [InlineData(TarEntryFormat.V7, TestTarFormat.v7)]
        [InlineData(TarEntryFormat.Ustar, TestTarFormat.ustar)]
        [InlineData(TarEntryFormat.Pax, TestTarFormat.pax)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.gnu)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.oldgnu)]
        public Task Read_Archive_Folder_Subfolder_File_Async(TarEntryFormat format, TestTarFormat testFormat) =>
            Read_Archive_Folder_Subfolder_File_Async_Internal(format, testFormat);

        [Theory]
        [InlineData(TarEntryFormat.V7, TestTarFormat.v7)]
        [InlineData(TarEntryFormat.Ustar, TestTarFormat.ustar)]
        [InlineData(TarEntryFormat.Pax, TestTarFormat.pax)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.gnu)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.oldgnu)]
        public Task Read_Archive_FolderSymbolicLink_Folder_Subfolder_File_Async(TarEntryFormat format, TestTarFormat testFormat) =>
            Read_Archive_FolderSymbolicLink_Folder_Subfolder_File_Async_Internal(format, testFormat);

        [Theory]
        [InlineData(TarEntryFormat.V7, TestTarFormat.v7)]
        [InlineData(TarEntryFormat.Ustar, TestTarFormat.ustar)]
        [InlineData(TarEntryFormat.Pax, TestTarFormat.pax)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.gnu)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.oldgnu)]
        public Task Read_Archive_Many_Small_Files_Async(TarEntryFormat format, TestTarFormat testFormat) =>
            Read_Archive_Many_Small_Files_Async_Internal(format, testFormat);

        [Theory]
        // V7 does not support longer filenames
        [InlineData(TarEntryFormat.Ustar, TestTarFormat.ustar)]
        [InlineData(TarEntryFormat.Pax, TestTarFormat.pax)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.gnu)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.oldgnu)]
        public Task Read_Archive_LongPath_Splitable_Under255_Async(TarEntryFormat format, TestTarFormat testFormat) =>
            Read_Archive_LongPath_Splitable_Under255_Async_Internal(format, testFormat);

        [Theory]
        // V7 does not support block devices, character devices or fifos
        [InlineData(TarEntryFormat.Ustar, TestTarFormat.ustar)]
        [InlineData(TarEntryFormat.Pax, TestTarFormat.pax)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.gnu)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.oldgnu)]
        public Task Read_Archive_SpecialFiles_Async(TarEntryFormat format, TestTarFormat testFormat) =>
            Read_Archive_SpecialFiles_Async_Internal(format, testFormat);

        [Theory]
        // Neither V7 not Ustar can handle links with long target filenames
        [InlineData(TarEntryFormat.Pax, TestTarFormat.pax)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.gnu)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.oldgnu)]
        public Task Read_Archive_File_LongSymbolicLink_Async(TarEntryFormat format, TestTarFormat testFormat) =>
            Read_Archive_File_LongSymbolicLink_Async_Internal(format, testFormat);

        [Theory]
        // Neither V7 not Ustar can handle a path that does not have separators that can be split under 100 bytes
        [InlineData(TarEntryFormat.Pax, TestTarFormat.pax)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.gnu)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.oldgnu)]
        public Task Read_Archive_LongFileName_Over100_Under255_Async(TarEntryFormat format, TestTarFormat testFormat) =>
            Read_Archive_LongFileName_Over100_Under255_Async_Internal(format, testFormat);

        [Theory]
        // Neither V7 not Ustar can handle path lengths waaaay beyond name+prefix length
        [InlineData(TarEntryFormat.Pax, TestTarFormat.pax)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.gnu)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.oldgnu)]
        public Task Read_Archive_LongPath_Over255_Async(TarEntryFormat format, TestTarFormat testFormat) =>
            Read_Archive_LongPath_Over255_Async_Internal(format, testFormat);

        [Theory]
        [MemberData(nameof(GetV7TestCaseNames))]
        public Task ReadDataStreamOfTarGzV7Async(string testCaseName) =>
            VerifyDataStreamOfTarGzInternalAsync(TestTarFormat.v7, testCaseName, copyData: false);

        [Theory]
        [MemberData(nameof(GetUstarTestCaseNames))]
        public Task ReadDataStreamOfTarGzUstarAsync(string testCaseName) =>
            VerifyDataStreamOfTarGzInternalAsync(TestTarFormat.ustar, testCaseName, copyData: false);

        [Theory]
        [MemberData(nameof(GetPaxAndGnuTestCaseNames))]
        public Task ReadDataStreamOfTarGzPaxAsync(string testCaseName) =>
            VerifyDataStreamOfTarGzInternalAsync(TestTarFormat.pax, testCaseName, copyData: false);

        [Theory]
        [MemberData(nameof(GetPaxAndGnuTestCaseNames))]
        public Task ReadDataStreamOfTarGzPaxGeaAsync(string testCaseName) =>
            VerifyDataStreamOfTarGzInternalAsync(TestTarFormat.pax_gea, testCaseName, copyData: false);

        [Theory]
        [MemberData(nameof(GetPaxAndGnuTestCaseNames))]
        public Task ReadDataStreamOfTarGzOldGnuAsync(string testCaseName) =>
            VerifyDataStreamOfTarGzInternalAsync(TestTarFormat.oldgnu, testCaseName, copyData: false);

        [Theory]
        [MemberData(nameof(GetPaxAndGnuTestCaseNames))]
        public Task ReadDataStreamOfTarGzGnuAsync(string testCaseName) =>
            VerifyDataStreamOfTarGzInternalAsync(TestTarFormat.gnu, testCaseName, copyData: false);

        [Theory]
        [MemberData(nameof(GetV7TestCaseNames))]
        public Task ReadCopiedDataStreamOfTarGzV7Async(string testCaseName) =>
            VerifyDataStreamOfTarGzInternalAsync(TestTarFormat.v7, testCaseName, copyData: true);

        [Theory]
        [MemberData(nameof(GetUstarTestCaseNames))]
        public Task ReadCopiedDataStreamOfTarGzUstarAsync(string testCaseName) =>
            VerifyDataStreamOfTarGzInternalAsync(TestTarFormat.ustar, testCaseName, copyData: true);

        [Theory]
        [MemberData(nameof(GetPaxAndGnuTestCaseNames))]
        public Task ReadCopiedDataStreamOfTarGzPaxAsync(string testCaseName) =>
            VerifyDataStreamOfTarGzInternalAsync(TestTarFormat.pax, testCaseName, copyData: true);

        [Theory]
        [MemberData(nameof(GetPaxAndGnuTestCaseNames))]
        public Task ReadCopiedDataStreamOfTarGzPaxGeaAsync(string testCaseName) =>
            VerifyDataStreamOfTarGzInternalAsync(TestTarFormat.pax_gea, testCaseName, copyData: true);

        [Theory]
        [MemberData(nameof(GetPaxAndGnuTestCaseNames))]
        public Task ReadCopiedDataStreamOfTarGzOldGnuAsync(string testCaseName) =>
            VerifyDataStreamOfTarGzInternalAsync(TestTarFormat.oldgnu, testCaseName, copyData: true);

        [Theory]
        [MemberData(nameof(GetPaxAndGnuTestCaseNames))]
        public Task ReadCopiedDataStreamOfTarGzGnuAsync(string testCaseName) =>
            VerifyDataStreamOfTarGzInternalAsync(TestTarFormat.gnu, testCaseName, copyData: true);

        [Theory]
        [MemberData(nameof(GetGoLangTarTestCaseNames))]
        public Task ReadDataStreamOfExternalAssetsGoLangAsync(string testCaseName) =>
            VerifyDataStreamOfTarUncompressedInternalAsync("golang_tar", testCaseName, copyData: false);

        [Theory]
        [MemberData(nameof(GetNodeTarTestCaseNames))]
        public Task ReadDataStreamOfExternalAssetsNodeAsync(string testCaseName) =>
            VerifyDataStreamOfTarUncompressedInternalAsync("node-tar", testCaseName, copyData: false);

        [Theory]
        [MemberData(nameof(GetRsTarTestCaseNames))]
        public Task ReadDataStreamOfExternalAssetsRsAsync(string testCaseName) =>
            VerifyDataStreamOfTarUncompressedInternalAsync("tar-rs", testCaseName, copyData: false);

        [Theory]
        [MemberData(nameof(GetGoLangTarTestCaseNames))]
        public Task ReadCopiedDataStreamOfExternalAssetsGoLangAsync(string testCaseName) =>
            VerifyDataStreamOfTarUncompressedInternalAsync("golang_tar", testCaseName, copyData: true);

        [Theory]
        [MemberData(nameof(GetNodeTarTestCaseNames))]
        public Task ReadCopiedDataStreamOfExternalAssetsNodeAsync(string testCaseName) =>
            VerifyDataStreamOfTarUncompressedInternalAsync("node-tar", testCaseName, copyData: true);

        [Theory]
        [MemberData(nameof(GetRsTarTestCaseNames))]
        public Task ReadCopiedDataStreamOfExternalAssetsRsAsync(string testCaseName) =>
            VerifyDataStreamOfTarUncompressedInternalAsync("tar-rs", testCaseName, copyData: true);

        [Fact]
        public async Task Throw_FifoContainsNonZeroDataSectionAsync()
        {
            await using MemoryStream archiveStream = GetTarMemoryStream(CompressionMethod.Uncompressed, "golang_tar", "hdr-only");
            await using TarReader reader = new TarReader(archiveStream);
            Assert.NotNull(await reader.GetNextEntryAsync());
            Assert.NotNull(await reader.GetNextEntryAsync());
            Assert.NotNull(await reader.GetNextEntryAsync());
            Assert.NotNull(await reader.GetNextEntryAsync());
            Assert.NotNull(await reader.GetNextEntryAsync());
            Assert.NotNull(await reader.GetNextEntryAsync());
            Assert.NotNull(await reader.GetNextEntryAsync());
            Assert.NotNull(await reader.GetNextEntryAsync());
            await Assert.ThrowsAsync<InvalidDataException>(async () => await reader.GetNextEntryAsync());
        }

        [Fact]
        public async Task Throw_SingleExtendedAttributesEntryWithNoActualEntryAsync()
        {
            await using MemoryStream archiveStream = GetTarMemoryStream(CompressionMethod.Uncompressed, "golang_tar", "pax-path-hdr");
            await using TarReader reader = new TarReader(archiveStream);
            await Assert.ThrowsAsync<EndOfStreamException>(async () => await reader.GetNextEntryAsync());
        }

        [Theory]
        [InlineData("tar-rs", "spaces")]
        [InlineData("golang_tar", "v7")]
        public async Task AllowSpacesInOctalFieldsAsync(string folderName, string testCaseName)
        {
            await using MemoryStream archiveStream = GetTarMemoryStream(CompressionMethod.Uncompressed, folderName, testCaseName);
            await using TarReader reader = new TarReader(archiveStream);
            TarEntry entry;
            while ((entry = await reader.GetNextEntryAsync()) != null)
            {
                AssertExtensions.GreaterThan(entry.Checksum, 0);
                AssertExtensions.GreaterThan((int)entry.Mode, 0);
            }
        }

        [Theory]
        [InlineData("pax-multi-hdrs")] // Multiple consecutive PAX metadata entries
        [InlineData("gnu-multi-hdrs")] // Multiple consecutive GNU metadata entries
        [InlineData("neg-size")] // Garbage chars
        [InlineData("invalid-go17")] // Many octal fields are all zero chars
        [InlineData("issue11169")] // Checksum with null in the middle
        [InlineData("issue10968")] // Garbage chars
        public async Task Throw_ArchivesWithRandomCharsAsync(string testCaseName)
        {
            await using MemoryStream archiveStream = GetTarMemoryStream(CompressionMethod.Uncompressed, "golang_tar", testCaseName);
            await using TarReader reader = new TarReader(archiveStream);
            await Assert.ThrowsAsync<InvalidDataException>(async () => await reader.GetNextEntryAsync());
        }

        [Fact]
        public async Task Throw_ArchiveIsShortAsync()
        {
            // writer-big has a header for a 16G file but not its contents.
            using MemoryStream archiveStream = GetTarMemoryStream(CompressionMethod.Uncompressed, "golang_tar", "writer-big");
            using TarReader reader = new TarReader(archiveStream);
            // MemoryStream throws when we try to change its Position past its Length.
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await reader.GetNextEntryAsync());
        }

        [Fact]
        public async Task GarbageEntryChecksumZeroReturnNullAsync()
        {
            await using MemoryStream archiveStream = GetTarMemoryStream(CompressionMethod.Uncompressed, "golang_tar", "issue12435");
            await using TarReader reader = new TarReader(archiveStream);
            Assert.Null(await reader.GetNextEntryAsync());
        }

        [Theory]
        [InlineData("golang_tar", "gnu-nil-sparse-data")]
        [InlineData("golang_tar", "gnu-nil-sparse-hole")]
        [InlineData("golang_tar", "gnu-sparse-big")]
        [InlineData("golang_tar", "sparse-formats")]
        [InlineData("tar-rs", "sparse-1")]
        [InlineData("tar-rs", "sparse")]
        public async Task SparseEntryNotSupportedAsync(string testFolderName, string testCaseName)
        {
            // Currently sparse entries are not supported.

            // There are PAX archives archives in the golang folder that have extended attributes for treating a regular file as a sparse file.
            // Sparse entries were created for the GNU format, so they are very rare entry types which are excluded from this test method:
            // pax-nil-sparse-data, pax-nil-sparse-hole, pax-sparse-big

            await using MemoryStream archiveStream = GetTarMemoryStream(CompressionMethod.Uncompressed, testFolderName, testCaseName);
            await using TarReader reader = new TarReader(archiveStream);
            await Assert.ThrowsAsync<NotSupportedException>(async () => await reader.GetNextEntryAsync());
        }

        [Fact]
        public async Task DirectoryListRegularFileAndSparseAsync()
        {
            await using MemoryStream archiveStream = GetTarMemoryStream(CompressionMethod.Uncompressed, "golang_tar", "gnu-incremental");
            await using TarReader reader = new TarReader(archiveStream);
            TarEntry directoryList = await reader.GetNextEntryAsync();

            Assert.Equal(TarEntryType.DirectoryList, directoryList.EntryType);
            Assert.NotNull(directoryList.DataStream);
            Assert.Equal(14, directoryList.Length);

            Assert.NotNull(await reader.GetNextEntryAsync()); // Just a regular file

            await Assert.ThrowsAsync<NotSupportedException>(async () => await reader.GetNextEntryAsync()); // Sparse
        }

        [Fact]
        public async Task PaxSizeLargerThanMaxAllowedByStreamAsync()
        {
            await using MemoryStream archiveStream = GetTarMemoryStream(CompressionMethod.Uncompressed, "golang_tar", "writer-big-long");
            await using TarReader reader = new TarReader(archiveStream);
            // The extended attribute 'size' has the value 17179869184
            // Exception message: Stream length must be non-negative and less than 2^31 - 1 - origin
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await reader.GetNextEntryAsync());
        }

        private static async Task VerifyDataStreamOfTarUncompressedInternalAsync(string testFolderName, string testCaseName, bool copyData)
        {
            await using MemoryStream archiveStream = GetTarMemoryStream(CompressionMethod.Uncompressed, testFolderName, testCaseName);
            await VerifyDataStreamOfTarInternalAsync(archiveStream, copyData);
        }

        private static async Task VerifyDataStreamOfTarGzInternalAsync(TestTarFormat testTarFormat, string testCaseName, bool copyData)
        {
            await using MemoryStream archiveStream = GetTarMemoryStream(CompressionMethod.GZip, testTarFormat, testCaseName);
            await using GZipStream decompressor = new GZipStream(archiveStream, CompressionMode.Decompress);
            await VerifyDataStreamOfTarInternalAsync(decompressor, copyData);
        }

        private static async Task VerifyDataStreamOfTarInternalAsync(Stream archiveStream, bool copyData)
        {
            await using TarReader reader = new TarReader(archiveStream);

            TarEntry entry;

            await using MemoryStream ms = new MemoryStream();
            while ((entry = await reader.GetNextEntryAsync(copyData)) != null)
            {
                if (entry.EntryType is TarEntryType.V7RegularFile or TarEntryType.RegularFile)
                {
                    if (entry.Length == 0)
                    {
                        Assert.Null(entry.DataStream);
                    }
                    else
                    {
                        Assert.NotNull(entry.DataStream);
                        Assert.Equal(entry.DataStream.Length, entry.Length);
                        if (copyData)
                        {
                            Assert.True(entry.DataStream.CanSeek);
                            Assert.Equal(0, entry.DataStream.Position);
                        }
                    }
                }
            }
        }
    }
}
