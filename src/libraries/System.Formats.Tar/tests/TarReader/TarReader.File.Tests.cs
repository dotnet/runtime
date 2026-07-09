// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using static System.Formats.Tar.Tests.TarTestsBase;

namespace System.Formats.Tar.Tests
{
    public class TarReader_File_Tests : TarReader_File_Tests_Base
    {
        [Theory]
        [InlineData(TarEntryFormat.V7, TestTarFormat.v7)]
        [InlineData(TarEntryFormat.Ustar, TestTarFormat.ustar)]
        [InlineData(TarEntryFormat.Pax, TestTarFormat.pax)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.gnu)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.oldgnu)]
        public async Task Read_Archive_File(TarEntryFormat format, TestTarFormat testFormat)
        {
            foreach (bool async in Booleans)
            {
                await Read_Archive_File_Internal(format, testFormat, async);
            }
        }

        [Theory]
        [InlineData(TarEntryFormat.V7, TestTarFormat.v7)]
        [InlineData(TarEntryFormat.Ustar, TestTarFormat.ustar)]
        [InlineData(TarEntryFormat.Pax, TestTarFormat.pax)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.gnu)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.oldgnu)]
        public async Task Read_Archive_File_HardLink(TarEntryFormat format, TestTarFormat testFormat)
        {
            foreach (bool async in Booleans)
            {
                await Read_Archive_File_HardLink_Internal(format, testFormat, async);
            }
        }

        [Theory]
        [InlineData(TarEntryFormat.V7, TestTarFormat.v7)]
        [InlineData(TarEntryFormat.Ustar, TestTarFormat.ustar)]
        [InlineData(TarEntryFormat.Pax, TestTarFormat.pax)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.gnu)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.oldgnu)]
        public async Task Read_Archive_File_SymbolicLink(TarEntryFormat format, TestTarFormat testFormat)
        {
            foreach (bool async in Booleans)
            {
                await Read_Archive_File_SymbolicLink_Internal(format, testFormat, async);
            }
        }

        [Theory]
        [InlineData(TarEntryFormat.V7, TestTarFormat.v7)]
        [InlineData(TarEntryFormat.Ustar, TestTarFormat.ustar)]
        [InlineData(TarEntryFormat.Pax, TestTarFormat.pax)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.gnu)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.oldgnu)]
        public async Task Read_Archive_Folder_File(TarEntryFormat format, TestTarFormat testFormat)
        {
            foreach (bool async in Booleans)
            {
                await Read_Archive_Folder_File_Internal(format, testFormat, async);
            }
        }

        [Theory]
        [InlineData(TarEntryFormat.V7, TestTarFormat.v7)]
        [InlineData(TarEntryFormat.Ustar, TestTarFormat.ustar)]
        [InlineData(TarEntryFormat.Pax, TestTarFormat.pax)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.gnu)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.oldgnu)]
        public async Task Read_Archive_Folder_File_Utf8(TarEntryFormat format, TestTarFormat testFormat)
        {
            foreach (bool async in Booleans)
            {
                await Read_Archive_Folder_File_Utf8_Internal(format, testFormat, async);
            }
        }

        [Theory]
        [InlineData(TarEntryFormat.V7, TestTarFormat.v7)]
        [InlineData(TarEntryFormat.Ustar, TestTarFormat.ustar)]
        [InlineData(TarEntryFormat.Pax, TestTarFormat.pax)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.gnu)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.oldgnu)]
        public async Task Read_Archive_Folder_Subfolder_File(TarEntryFormat format, TestTarFormat testFormat)
        {
            foreach (bool async in Booleans)
            {
                await Read_Archive_Folder_Subfolder_File_Internal(format, testFormat, async);
            }
        }

        [Theory]
        [InlineData(TarEntryFormat.V7, TestTarFormat.v7)]
        [InlineData(TarEntryFormat.Ustar, TestTarFormat.ustar)]
        [InlineData(TarEntryFormat.Pax, TestTarFormat.pax)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.gnu)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.oldgnu)]
        public async Task Read_Archive_FolderSymbolicLink_Folder_Subfolder_File(TarEntryFormat format, TestTarFormat testFormat)
        {
            foreach (bool async in Booleans)
            {
                await Read_Archive_FolderSymbolicLink_Folder_Subfolder_File_Internal(format, testFormat, async);
            }
        }

        [Theory]
        [InlineData(TarEntryFormat.V7, TestTarFormat.v7)]
        [InlineData(TarEntryFormat.Ustar, TestTarFormat.ustar)]
        [InlineData(TarEntryFormat.Pax, TestTarFormat.pax)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.gnu)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.oldgnu)]
        public async Task Read_Archive_Many_Small_Files(TarEntryFormat format, TestTarFormat testFormat)
        {
            foreach (bool async in Booleans)
            {
                await Read_Archive_Many_Small_Files_Internal(format, testFormat, async);
            }
        }

        [Theory]
        // V7 does not support longer filenames
        [InlineData(TarEntryFormat.Ustar, TestTarFormat.ustar)]
        [InlineData(TarEntryFormat.Pax, TestTarFormat.pax)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.gnu)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.oldgnu)]
        public async Task Read_Archive_LongPath_Splitable_Under255(TarEntryFormat format, TestTarFormat testFormat)
        {
            foreach (bool async in Booleans)
            {
                await Read_Archive_LongPath_Splitable_Under255_Internal(format, testFormat, async);
            }
        }

        [Theory]
        // V7 does not support block devices, character devices or fifos
        [InlineData(TarEntryFormat.Ustar, TestTarFormat.ustar)]
        [InlineData(TarEntryFormat.Pax, TestTarFormat.pax)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.gnu)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.oldgnu)]
        public async Task Read_Archive_SpecialFiles(TarEntryFormat format, TestTarFormat testFormat)
        {
            foreach (bool async in Booleans)
            {
                await Read_Archive_SpecialFiles_Internal(format, testFormat, async);
            }
        }

        [Theory]
        // Neither V7 not Ustar can handle links with long target filenames
        [InlineData(TarEntryFormat.Pax, TestTarFormat.pax)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.gnu)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.oldgnu)]
        public async Task Read_Archive_File_LongSymbolicLink(TarEntryFormat format, TestTarFormat testFormat)
        {
            foreach (bool async in Booleans)
            {
                await Read_Archive_File_LongSymbolicLink_Internal(format, testFormat, async);
            }
        }

        [Theory]
        // Neither V7 not Ustar can handle a path that does not have separators that can be split under 100 bytes
        [InlineData(TarEntryFormat.Pax, TestTarFormat.pax)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.gnu)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.oldgnu)]
        public async Task Read_Archive_LongFileName_Over100_Under255(TarEntryFormat format, TestTarFormat testFormat)
        {
            foreach (bool async in Booleans)
            {
                await Read_Archive_LongFileName_Over100_Under255_Internal(format, testFormat, async);
            }
        }

        [Theory]
        // Neither V7 not Ustar can handle path lengths waaaay beyond name+prefix length
        [InlineData(TarEntryFormat.Pax, TestTarFormat.pax)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.gnu)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.oldgnu)]
        public async Task Read_Archive_LongPath_Over255(TarEntryFormat format, TestTarFormat testFormat)
        {
            foreach (bool async in Booleans)
            {
                await Read_Archive_LongPath_Over255_Internal(format, testFormat, async);
            }
        }

        [Theory]
        [MemberData(nameof(GetV7TestCaseNames))]
        public async Task ReadDataStreamOfTarGzV7(string testCaseName)
        {
            foreach (bool async in Booleans)
            {
                await VerifyDataStreamOfTarGzInternal(TestTarFormat.v7, testCaseName, copyData: false, async);
            }
        }

        [Theory]
        [MemberData(nameof(GetUstarTestCaseNames))]
        public async Task ReadDataStreamOfTarGzUstar(string testCaseName)
        {
            foreach (bool async in Booleans)
            {
                await VerifyDataStreamOfTarGzInternal(TestTarFormat.ustar, testCaseName, copyData: false, async);
            }
        }

        [Theory]
        [MemberData(nameof(GetPaxAndGnuTestCaseNames))]
        public async Task ReadDataStreamOfTarGzPax(string testCaseName)
        {
            foreach (bool async in Booleans)
            {
                await VerifyDataStreamOfTarGzInternal(TestTarFormat.pax, testCaseName, copyData: false, async);
            }
        }

        [Theory]
        [MemberData(nameof(GetPaxAndGnuTestCaseNames))]
        public async Task ReadDataStreamOfTarGzPaxGea(string testCaseName)
        {
            foreach (bool async in Booleans)
            {
                await VerifyDataStreamOfTarGzInternal(TestTarFormat.pax_gea, testCaseName, copyData: false, async);
            }
        }

        [Theory]
        [MemberData(nameof(GetPaxAndGnuTestCaseNames))]
        public async Task ReadDataStreamOfTarGzOldGnu(string testCaseName)
        {
            foreach (bool async in Booleans)
            {
                await VerifyDataStreamOfTarGzInternal(TestTarFormat.oldgnu, testCaseName, copyData: false, async);
            }
        }

        [Theory]
        [MemberData(nameof(GetPaxAndGnuTestCaseNames))]
        public async Task ReadDataStreamOfTarGzGnu(string testCaseName)
        {
            foreach (bool async in Booleans)
            {
                await VerifyDataStreamOfTarGzInternal(TestTarFormat.gnu, testCaseName, copyData: false, async);
            }
        }

        [Theory]
        [MemberData(nameof(GetV7TestCaseNames))]
        public async Task ReadCopiedDataStreamOfTarGzV7(string testCaseName)
        {
            foreach (bool async in Booleans)
            {
                await VerifyDataStreamOfTarGzInternal(TestTarFormat.v7, testCaseName, copyData: true, async);
            }
        }

        [Theory]
        [MemberData(nameof(GetUstarTestCaseNames))]
        public async Task ReadCopiedDataStreamOfTarGzUstar(string testCaseName)
        {
            foreach (bool async in Booleans)
            {
                await VerifyDataStreamOfTarGzInternal(TestTarFormat.ustar, testCaseName, copyData: true, async);
            }
        }

        [Theory]
        [MemberData(nameof(GetPaxAndGnuTestCaseNames))]
        public async Task ReadCopiedDataStreamOfTarGzPax(string testCaseName)
        {
            foreach (bool async in Booleans)
            {
                await VerifyDataStreamOfTarGzInternal(TestTarFormat.pax, testCaseName, copyData: true, async);
            }
        }

        [Theory]
        [MemberData(nameof(GetPaxAndGnuTestCaseNames))]
        public async Task ReadCopiedDataStreamOfTarGzPaxGea(string testCaseName)
        {
            foreach (bool async in Booleans)
            {
                await VerifyDataStreamOfTarGzInternal(TestTarFormat.pax_gea, testCaseName, copyData: true, async);
            }
        }

        [Theory]
        [MemberData(nameof(GetPaxAndGnuTestCaseNames))]
        public async Task ReadCopiedDataStreamOfTarGzOldGnu(string testCaseName)
        {
            foreach (bool async in Booleans)
            {
                await VerifyDataStreamOfTarGzInternal(TestTarFormat.oldgnu, testCaseName, copyData: true, async);
            }
        }

        [Theory]
        [MemberData(nameof(GetPaxAndGnuTestCaseNames))]
        public async Task ReadCopiedDataStreamOfTarGzGnu(string testCaseName)
        {
            foreach (bool async in Booleans)
            {
                await VerifyDataStreamOfTarGzInternal(TestTarFormat.gnu, testCaseName, copyData: true, async);
            }
        }

        [Theory]
        [MemberData(nameof(GetGoLangTarTestCaseNames))]
        public async Task ReadDataStreamOfExternalAssetsGoLang(string testCaseName)
        {
            foreach (bool async in Booleans)
            {
                await VerifyDataStreamOfTarUncompressedInternal("golang_tar", testCaseName, copyData: false, async);
            }
        }

        [Theory]
        [MemberData(nameof(GetNodeTarTestCaseNames))]
        public async Task ReadDataStreamOfExternalAssetsNode(string testCaseName)
        {
            foreach (bool async in Booleans)
            {
                await VerifyDataStreamOfTarUncompressedInternal("node-tar", testCaseName, copyData: false, async);
            }
        }

        [Theory]
        [MemberData(nameof(GetRsTarTestCaseNames))]
        public async Task ReadDataStreamOfExternalAssetsRs(string testCaseName)
        {
            foreach (bool async in Booleans)
            {
                await VerifyDataStreamOfTarUncompressedInternal("tar-rs", testCaseName, copyData: false, async);
            }
        }

        [Theory]
        [MemberData(nameof(GetGoLangTarTestCaseNames))]
        public async Task ReadCopiedDataStreamOfExternalAssetsGoLang(string testCaseName)
        {
            foreach (bool async in Booleans)
            {
                await VerifyDataStreamOfTarUncompressedInternal("golang_tar", testCaseName, copyData: true, async);
            }
        }

        [Theory]
        [MemberData(nameof(GetNodeTarTestCaseNames))]
        public async Task ReadCopiedDataStreamOfExternalAssetsNode(string testCaseName)
        {
            foreach (bool async in Booleans)
            {
                await VerifyDataStreamOfTarUncompressedInternal("node-tar", testCaseName, copyData: true, async);
            }
        }

        [Theory]
        [MemberData(nameof(GetRsTarTestCaseNames))]
        public async Task ReadCopiedDataStreamOfExternalAssetsRs(string testCaseName)
        {
            foreach (bool async in Booleans)
            {
                await VerifyDataStreamOfTarUncompressedInternal("tar-rs", testCaseName, copyData: true, async);
            }
        }

        [Theory]
        [MemberData(nameof(Get_Boolean_Data))]
        public async Task Throw_FifoContainsNonZeroDataSection(bool async)
        {
            using MemoryStream archiveStream = GetTarMemoryStream(CompressionMethod.Uncompressed, "golang_tar", "hdr-only");
            TarReader reader = await CreateTarReader(archiveStream, async);
            try
            {
                Assert.NotNull(await GetNextEntry(reader, async));
                Assert.NotNull(await GetNextEntry(reader, async));
                Assert.NotNull(await GetNextEntry(reader, async));
                Assert.NotNull(await GetNextEntry(reader, async));
                Assert.NotNull(await GetNextEntry(reader, async));
                Assert.NotNull(await GetNextEntry(reader, async));
                Assert.NotNull(await GetNextEntry(reader, async));
                Assert.NotNull(await GetNextEntry(reader, async));

                if (async)
                {
                    await Assert.ThrowsAsync<InvalidDataException>(async () => await GetNextEntry(reader, async));
                }
                else
                {
                    Assert.Throws<InvalidDataException>(() => GetNextEntry(reader, async).GetAwaiter().GetResult());
                }
            }
            finally
            {
                await DisposeTarReader(reader, async);
            }
        }

        [Theory]
        [MemberData(nameof(Get_Boolean_Data))]
        public async Task Throw_SingleExtendedAttributesEntryWithNoActualEntry(bool async)
        {
            using MemoryStream archiveStream = GetTarMemoryStream(CompressionMethod.Uncompressed, "golang_tar", "pax-path-hdr");
            TarReader reader = await CreateTarReader(archiveStream, async);
            try
            {
                if (async)
                {
                    await Assert.ThrowsAsync<EndOfStreamException>(async () => await GetNextEntry(reader, async));
                }
                else
                {
                    Assert.Throws<EndOfStreamException>(() => GetNextEntry(reader, async).GetAwaiter().GetResult());
                }
            }
            finally
            {
                await DisposeTarReader(reader, async);
            }
        }

        [Fact]
        public async Task ReadDataStreamOfGoLangTarGzGnu()
        {
            using MemoryStream archiveStream = GetTarMemoryStream(CompressionMethod.GZip, "golang_tar", "pax-bad-hdr-large");
            using GZipStream decompressor = new GZipStream(archiveStream, CompressionMode.Decompress);
            await VerifyDataStreamOfTarInternal(decompressor, copyData: false, async: false);
        }

        [Theory]
        [InlineData("tar-rs", "spaces")]
        [InlineData("golang_tar", "v7")]
        public async Task AllowSpacesInOctalFields(string folderName, string testCaseName)
        {
            foreach (bool async in Booleans)
            {
                using MemoryStream archiveStream = GetTarMemoryStream(CompressionMethod.Uncompressed, folderName, testCaseName);
                TarReader reader = await CreateTarReader(archiveStream, async);
                try
                {
                    TarEntry entry;
                    while ((entry = await GetNextEntry(reader, async)) != null)
                    {
                        AssertExtensions.GreaterThan(entry.Checksum, 0);
                        AssertExtensions.GreaterThan((int)entry.Mode, 0);
                    }
                }
                finally
                {
                    await DisposeTarReader(reader, async);
                }
            }
        }

        [Theory]
        [InlineData("pax-multi-hdrs")]
        [InlineData("gnu-multi-hdrs")]
        [InlineData("neg-size")]
        [InlineData("invalid-go17")]
        [InlineData("issue11169")]
        [InlineData("pax-bad-hdr-file")]
        [InlineData("issue10968")]
        public async Task Throw_ArchivesWithRandomChars(string testCaseName)
        {
            foreach (bool async in Booleans)
            {
                using MemoryStream archiveStream = GetTarMemoryStream(CompressionMethod.Uncompressed, "golang_tar", testCaseName);
                TarReader reader = await CreateTarReader(archiveStream, async);
                try
                {
                    if (async)
                    {
                        await Assert.ThrowsAsync<InvalidDataException>(async () => await GetNextEntry(reader, async));
                    }
                    else
                    {
                        Assert.Throws<InvalidDataException>(() => GetNextEntry(reader, async).GetAwaiter().GetResult());
                    }
                }
                finally
                {
                    await DisposeTarReader(reader, async);
                }
            }
        }

        [Theory]
        [MemberData(nameof(Get_Boolean_Data))]
        public async Task Throw_ArchiveIsShort(bool async)
        {
            using MemoryStream archiveStream = GetTarMemoryStream(CompressionMethod.Uncompressed, "golang_tar", "writer-big");
            TarReader reader = await CreateTarReader(archiveStream, async);
            try
            {
                if (async)
                {
                    await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await GetNextEntry(reader, async));
                }
                else
                {
                    Assert.Throws<ArgumentOutOfRangeException>(() => GetNextEntry(reader, async).GetAwaiter().GetResult());
                }
            }
            finally
            {
                await DisposeTarReader(reader, async);
            }
        }

        [Theory]
        [MemberData(nameof(Get_Boolean_Data))]
        public async Task GarbageEntryChecksumZeroReturnNull(bool async)
        {
            using MemoryStream archiveStream = GetTarMemoryStream(CompressionMethod.Uncompressed, "golang_tar", "issue12435");
            TarReader reader = await CreateTarReader(archiveStream, async);
            try
            {
                Assert.Null(await GetNextEntry(reader, async));
            }
            finally
            {
                await DisposeTarReader(reader, async);
            }
        }

        [Theory]
        [MemberData(nameof(Get_Boolean_Data))]
        public async Task InvalidChecksum_ThrowsInvalidDataException(bool async)
        {
            using MemoryStream archiveStream = GetTarMemoryStream(CompressionMethod.Uncompressed, "node-tar", "bad-cksum");
            TarReader reader = await CreateTarReader(archiveStream, async);
            try
            {
                await GetNextEntry(reader, async); // first entry is okay
                if (async)
                {
                    await Assert.ThrowsAsync<InvalidDataException>(async () => await GetNextEntry(reader, async));
                }
                else
                {
                    Assert.Throws<InvalidDataException>(() => GetNextEntry(reader, async).GetAwaiter().GetResult());
                }
            }
            finally
            {
                await DisposeTarReader(reader, async);
            }
        }

        [Theory]
        [InlineData("golang_tar", "gnu-nil-sparse-data")]
        [InlineData("golang_tar", "gnu-nil-sparse-hole")]
        [InlineData("golang_tar", "gnu-sparse-big")]
        [InlineData("golang_tar", "sparse-formats")]
        [InlineData("tar-rs", "sparse-1")]
        [InlineData("tar-rs", "sparse")]
        public async Task SparseEntryNotSupported(string testFolderName, string testCaseName)
        {
            foreach (bool async in Booleans)
            {
                using MemoryStream archiveStream = GetTarMemoryStream(CompressionMethod.Uncompressed, testFolderName, testCaseName);
                TarReader reader = await CreateTarReader(archiveStream, async);
                try
                {
                    if (async)
                    {
                        await Assert.ThrowsAsync<NotSupportedException>(async () => await GetNextEntry(reader, async));
                    }
                    else
                    {
                        Assert.Throws<NotSupportedException>(() => GetNextEntry(reader, async).GetAwaiter().GetResult());
                    }
                }
                finally
                {
                    await DisposeTarReader(reader, async);
                }
            }
        }

        [Theory]
        [MemberData(nameof(Get_Boolean_Data))]
        public async Task ReaderIgnoresFieldValueAfterTrailingNull(bool async)
        {
            const string FileName = "  filename  ";
            const string FileNameWithDataPastTrailingNull = $"{FileName} nonesense";
            using MemoryStream ms = new();
            using (TarWriter writer = new(ms, leaveOpen: true))
            {
                var entry = new UstarTarEntry(TarEntryType.RegularFile, FileNameWithDataPastTrailingNull);
                writer.WriteEntry(entry);
            }
            ms.Position = 0;
            bool archiveIsExpected = ms.ToArray().IndexOf(Encoding.UTF8.GetBytes(FileNameWithDataPastTrailingNull)) != -1;
            Assert.True(archiveIsExpected);

            TarReader reader = await CreateTarReader(ms, async, leaveOpen: true);
            try
            {
                TarEntry firstEntry = await GetNextEntry(reader, async);
                Assert.Equal(FileName, firstEntry.Name);
            }
            finally
            {
                await DisposeTarReader(reader, async);
            }
        }

        [Theory]
        [MemberData(nameof(Get_Boolean_Data))]
        public async Task DirectoryListRegularFileAndSparse(bool async)
        {
            using MemoryStream archiveStream = GetTarMemoryStream(CompressionMethod.Uncompressed, "golang_tar", "gnu-incremental");
            TarReader reader = await CreateTarReader(archiveStream, async);
            try
            {
                TarEntry directoryList = await GetNextEntry(reader, async);

                Assert.Equal(TarEntryType.DirectoryList, directoryList.EntryType);
                Assert.NotNull(directoryList.DataStream);
                Assert.Equal(14, directoryList.Length);

                Assert.NotNull(await GetNextEntry(reader, async));

                if (async)
                {
                    await Assert.ThrowsAsync<NotSupportedException>(async () => await GetNextEntry(reader, async));
                }
                else
                {
                    Assert.Throws<NotSupportedException>(() => GetNextEntry(reader, async).GetAwaiter().GetResult());
                }
            }
            finally
            {
                await DisposeTarReader(reader, async);
            }
        }

        [Theory]
        [MemberData(nameof(Get_Boolean_Data))]
        public async Task PaxSizeLargerThanMaxAllowedByStream(bool async)
        {
            using MemoryStream archiveStream = GetTarMemoryStream(CompressionMethod.Uncompressed, "golang_tar", "writer-big-long");
            TarReader reader = await CreateTarReader(archiveStream, async);
            try
            {
                if (async)
                {
                    await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await GetNextEntry(reader, async));
                }
                else
                {
                    Assert.Throws<ArgumentOutOfRangeException>(() => GetNextEntry(reader, async).GetAwaiter().GetResult());
                }
            }
            finally
            {
                await DisposeTarReader(reader, async);
            }
        }

        private static async Task VerifyDataStreamOfTarUncompressedInternal(string testFolderName, string testCaseName, bool copyData, bool async)
        {
            using MemoryStream archiveStream = GetTarMemoryStream(CompressionMethod.Uncompressed, testFolderName, testCaseName);
            await VerifyDataStreamOfTarInternal(archiveStream, copyData, async);
        }

        private static async Task VerifyDataStreamOfTarGzInternal(TestTarFormat testTarFormat, string testCaseName, bool copyData, bool async)
        {
            using MemoryStream archiveStream = GetTarMemoryStream(CompressionMethod.GZip, testTarFormat, testCaseName);
            using GZipStream decompressor = new GZipStream(archiveStream, CompressionMode.Decompress);
            await VerifyDataStreamOfTarInternal(decompressor, copyData, async);
        }

        private static async Task VerifyDataStreamOfTarInternal(Stream archiveStream, bool copyData, bool async)
        {
            TarReader reader = await CreateTarReader(archiveStream, async);
            try
            {
                TarEntry entry;
                while ((entry = await GetNextEntry(reader, async, copyData)) != null)
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
            finally
            {
                await DisposeTarReader(reader, async);
            }
        }
    }
}
