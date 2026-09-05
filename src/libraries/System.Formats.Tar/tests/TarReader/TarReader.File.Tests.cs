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
        public static IEnumerable<object[]> GetArchiveFormatsAndBooleanData() => GetDataAndBooleanData(new[]
        {
            new object[] { TarEntryFormat.V7, TestTarFormat.v7 },
            new object[] { TarEntryFormat.Ustar, TestTarFormat.ustar },
            new object[] { TarEntryFormat.Pax, TestTarFormat.pax },
            new object[] { TarEntryFormat.Gnu, TestTarFormat.gnu },
            new object[] { TarEntryFormat.Gnu, TestTarFormat.oldgnu }
        });

        public static IEnumerable<object[]> GetNonV7ArchiveFormatsAndBooleanData() => GetDataAndBooleanData(new[]
        {
            new object[] { TarEntryFormat.Ustar, TestTarFormat.ustar },
            new object[] { TarEntryFormat.Pax, TestTarFormat.pax },
            new object[] { TarEntryFormat.Gnu, TestTarFormat.gnu },
            new object[] { TarEntryFormat.Gnu, TestTarFormat.oldgnu }
        });

        public static IEnumerable<object[]> GetPaxAndGnuArchiveFormatsAndBooleanData() => GetDataAndBooleanData(new[]
        {
            new object[] { TarEntryFormat.Pax, TestTarFormat.pax },
            new object[] { TarEntryFormat.Gnu, TestTarFormat.gnu },
            new object[] { TarEntryFormat.Gnu, TestTarFormat.oldgnu }
        });

        public static IEnumerable<object[]> GetV7TestCaseNamesAndBooleanData() => GetDataAndBooleanData(GetV7TestCaseNames());

        public static IEnumerable<object[]> GetUstarTestCaseNamesAndBooleanData() => GetDataAndBooleanData(GetUstarTestCaseNames());

        public static IEnumerable<object[]> GetPaxAndGnuTestCaseNamesAndBooleanData() => GetDataAndBooleanData(GetPaxAndGnuTestCaseNames());

        public static IEnumerable<object[]> GetGoLangTarTestCaseNamesAndBooleanData() => GetDataAndBooleanData(GetGoLangTarTestCaseNames());

        public static IEnumerable<object[]> GetNodeTarTestCaseNamesAndBooleanData() => GetDataAndBooleanData(GetNodeTarTestCaseNames());

        public static IEnumerable<object[]> GetRsTarTestCaseNamesAndBooleanData() => GetDataAndBooleanData(GetRsTarTestCaseNames());

        public static IEnumerable<object[]> GetAllowSpacesInOctalFieldsAndBooleanData() => GetDataAndBooleanData(new[]
        {
            new object[] { "tar-rs", "spaces" },
            new object[] { "golang_tar", "v7" }
        });

        public static IEnumerable<object[]> GetThrowArchivesWithRandomCharsAndBooleanData() => GetDataAndBooleanData(new[]
        {
            new object[] { "pax-multi-hdrs" }, // Multiple consecutive PAX metadata entries
            new object[] { "gnu-multi-hdrs" }, // Multiple consecutive GNU metadata entries
            new object[] { "neg-size" }, // Garbage chars
            new object[] { "invalid-go17" }, // Many octal fields are all zero chars
            new object[] { "issue11169" }, // Extended header uses spaces instead of newlines to separate records
            new object[] { "pax-bad-hdr-file" }, // Extended header record is not terminated by newline
            new object[] { "issue10968" }
        });

        public static IEnumerable<object[]> GetSparseEntryNotSupportedAndBooleanData() => GetDataAndBooleanData(new[]
        {
            new object[] { "golang_tar", "gnu-nil-sparse-data" },
            new object[] { "golang_tar", "gnu-nil-sparse-hole" },
            new object[] { "golang_tar", "gnu-sparse-big" },
            new object[] { "golang_tar", "sparse-formats" },
            new object[] { "tar-rs", "sparse-1" },
            new object[] { "tar-rs", "sparse" }
        });
        [Theory]
        [MemberData(nameof(GetArchiveFormatsAndBooleanData))]
        public Task Read_Archive_File(TarEntryFormat format, TestTarFormat testFormat, bool async) =>
            Read_Archive_File_Internal(format, testFormat, async);
        [Theory]
        [MemberData(nameof(GetArchiveFormatsAndBooleanData))]
        public Task Read_Archive_File_HardLink(TarEntryFormat format, TestTarFormat testFormat, bool async) =>
            Read_Archive_File_HardLink_Internal(format, testFormat, async);
        [Theory]
        [MemberData(nameof(GetArchiveFormatsAndBooleanData))]
        public Task Read_Archive_File_SymbolicLink(TarEntryFormat format, TestTarFormat testFormat, bool async) =>
            Read_Archive_File_SymbolicLink_Internal(format, testFormat, async);
        [Theory]
        [MemberData(nameof(GetArchiveFormatsAndBooleanData))]
        public Task Read_Archive_Folder_File(TarEntryFormat format, TestTarFormat testFormat, bool async) =>
            Read_Archive_Folder_File_Internal(format, testFormat, async);
        [Theory]
        [MemberData(nameof(GetArchiveFormatsAndBooleanData))]
        public Task Read_Archive_Folder_File_Utf8(TarEntryFormat format, TestTarFormat testFormat, bool async) =>
            Read_Archive_Folder_File_Utf8_Internal(format, testFormat, async);
        [Theory]
        [MemberData(nameof(GetArchiveFormatsAndBooleanData))]
        public Task Read_Archive_Folder_Subfolder_File(TarEntryFormat format, TestTarFormat testFormat, bool async) =>
            Read_Archive_Folder_Subfolder_File_Internal(format, testFormat, async);
        [Theory]
        [MemberData(nameof(GetArchiveFormatsAndBooleanData))]
        public Task Read_Archive_FolderSymbolicLink_Folder_Subfolder_File(TarEntryFormat format, TestTarFormat testFormat, bool async) =>
            Read_Archive_FolderSymbolicLink_Folder_Subfolder_File_Internal(format, testFormat, async);
        [Theory]
        [MemberData(nameof(GetArchiveFormatsAndBooleanData))]
        public Task Read_Archive_Many_Small_Files(TarEntryFormat format, TestTarFormat testFormat, bool async) =>
            Read_Archive_Many_Small_Files_Internal(format, testFormat, async);

        [Theory]
        // V7 does not support longer filenames
        [MemberData(nameof(GetNonV7ArchiveFormatsAndBooleanData))]
        public Task Read_Archive_LongPath_Splitable_Under255(TarEntryFormat format, TestTarFormat testFormat, bool async) =>
            Read_Archive_LongPath_Splitable_Under255_Internal(format, testFormat, async);

        [Theory]
        // V7 does not support block devices, character devices or fifos
        [MemberData(nameof(GetNonV7ArchiveFormatsAndBooleanData))]
        public Task Read_Archive_SpecialFiles(TarEntryFormat format, TestTarFormat testFormat, bool async) =>
            Read_Archive_SpecialFiles_Internal(format, testFormat, async);

        [Theory]
        // Neither V7 not Ustar can handle links with long target filenames
        [MemberData(nameof(GetPaxAndGnuArchiveFormatsAndBooleanData))]
        public Task Read_Archive_File_LongSymbolicLink(TarEntryFormat format, TestTarFormat testFormat, bool async) =>
            Read_Archive_File_LongSymbolicLink_Internal(format, testFormat, async);

        [Theory]
        // Neither V7 not Ustar can handle a path that does not have separators that can be split under 100 bytes
        [MemberData(nameof(GetPaxAndGnuArchiveFormatsAndBooleanData))]
        public Task Read_Archive_LongFileName_Over100_Under255(TarEntryFormat format, TestTarFormat testFormat, bool async) =>
            Read_Archive_LongFileName_Over100_Under255_Internal(format, testFormat, async);

        [Theory]
        // Neither V7 not Ustar can handle path lengths waaaay beyond name+prefix length
        [MemberData(nameof(GetPaxAndGnuArchiveFormatsAndBooleanData))]
        public Task Read_Archive_LongPath_Over255(TarEntryFormat format, TestTarFormat testFormat, bool async) =>
            Read_Archive_LongPath_Over255_Internal(format, testFormat, async);
        [Theory]
        [MemberData(nameof(GetV7TestCaseNamesAndBooleanData))]
        public Task ReadDataStreamOfTarGzV7(string testCaseName, bool async) =>
            VerifyDataStreamOfTarGzInternal(TestTarFormat.v7, testCaseName, copyData: false, async);
        [Theory]
        [MemberData(nameof(GetUstarTestCaseNamesAndBooleanData))]
        public Task ReadDataStreamOfTarGzUstar(string testCaseName, bool async) =>
            VerifyDataStreamOfTarGzInternal(TestTarFormat.ustar, testCaseName, copyData: false, async);
        [Theory]
        [MemberData(nameof(GetPaxAndGnuTestCaseNamesAndBooleanData))]
        public Task ReadDataStreamOfTarGzPax(string testCaseName, bool async) =>
            VerifyDataStreamOfTarGzInternal(TestTarFormat.pax, testCaseName, copyData: false, async);
        [Theory]
        [MemberData(nameof(GetPaxAndGnuTestCaseNamesAndBooleanData))]
        public Task ReadDataStreamOfTarGzPaxGea(string testCaseName, bool async) =>
            VerifyDataStreamOfTarGzInternal(TestTarFormat.pax_gea, testCaseName, copyData: false, async);
        [Theory]
        [MemberData(nameof(GetPaxAndGnuTestCaseNamesAndBooleanData))]
        public Task ReadDataStreamOfTarGzOldGnu(string testCaseName, bool async) =>
            VerifyDataStreamOfTarGzInternal(TestTarFormat.oldgnu, testCaseName, copyData: false, async);
        [Theory]
        [MemberData(nameof(GetPaxAndGnuTestCaseNamesAndBooleanData))]
        public Task ReadDataStreamOfTarGzGnu(string testCaseName, bool async) =>
            VerifyDataStreamOfTarGzInternal(TestTarFormat.gnu, testCaseName, copyData: false, async);
        [Theory]
        [MemberData(nameof(GetV7TestCaseNamesAndBooleanData))]
        public Task ReadCopiedDataStreamOfTarGzV7(string testCaseName, bool async) =>
            VerifyDataStreamOfTarGzInternal(TestTarFormat.v7, testCaseName, copyData: true, async);
        [Theory]
        [MemberData(nameof(GetUstarTestCaseNamesAndBooleanData))]
        public Task ReadCopiedDataStreamOfTarGzUstar(string testCaseName, bool async) =>
            VerifyDataStreamOfTarGzInternal(TestTarFormat.ustar, testCaseName, copyData: true, async);
        [Theory]
        [MemberData(nameof(GetPaxAndGnuTestCaseNamesAndBooleanData))]
        public Task ReadCopiedDataStreamOfTarGzPax(string testCaseName, bool async) =>
            VerifyDataStreamOfTarGzInternal(TestTarFormat.pax, testCaseName, copyData: true, async);
        [Theory]
        [MemberData(nameof(GetPaxAndGnuTestCaseNamesAndBooleanData))]
        public Task ReadCopiedDataStreamOfTarGzPaxGea(string testCaseName, bool async) =>
            VerifyDataStreamOfTarGzInternal(TestTarFormat.pax_gea, testCaseName, copyData: true, async);
        [Theory]
        [MemberData(nameof(GetPaxAndGnuTestCaseNamesAndBooleanData))]
        public Task ReadCopiedDataStreamOfTarGzOldGnu(string testCaseName, bool async) =>
            VerifyDataStreamOfTarGzInternal(TestTarFormat.oldgnu, testCaseName, copyData: true, async);
        [Theory]
        [MemberData(nameof(GetPaxAndGnuTestCaseNamesAndBooleanData))]
        public Task ReadCopiedDataStreamOfTarGzGnu(string testCaseName, bool async) =>
            VerifyDataStreamOfTarGzInternal(TestTarFormat.gnu, testCaseName, copyData: true, async);
        [Theory]
        [MemberData(nameof(GetGoLangTarTestCaseNamesAndBooleanData))]
        public Task ReadDataStreamOfExternalAssetsGoLang(string testCaseName, bool async) =>
            VerifyDataStreamOfTarUncompressedInternal("golang_tar", testCaseName, copyData: false, async);
        [Theory]
        [MemberData(nameof(GetNodeTarTestCaseNamesAndBooleanData))]
        public Task ReadDataStreamOfExternalAssetsNode(string testCaseName, bool async) =>
            VerifyDataStreamOfTarUncompressedInternal("node-tar", testCaseName, copyData: false, async);
        [Theory]
        [MemberData(nameof(GetRsTarTestCaseNamesAndBooleanData))]
        public Task ReadDataStreamOfExternalAssetsRs(string testCaseName, bool async) =>
            VerifyDataStreamOfTarUncompressedInternal("tar-rs", testCaseName, copyData: false, async);
        [Theory]
        [MemberData(nameof(GetGoLangTarTestCaseNamesAndBooleanData))]
        public Task ReadCopiedDataStreamOfExternalAssetsGoLang(string testCaseName, bool async) =>
            VerifyDataStreamOfTarUncompressedInternal("golang_tar", testCaseName, copyData: true, async);
        [Theory]
        [MemberData(nameof(GetNodeTarTestCaseNamesAndBooleanData))]
        public Task ReadCopiedDataStreamOfExternalAssetsNode(string testCaseName, bool async) =>
            VerifyDataStreamOfTarUncompressedInternal("node-tar", testCaseName, copyData: true, async);
        [Theory]
        [MemberData(nameof(GetRsTarTestCaseNamesAndBooleanData))]
        public Task ReadCopiedDataStreamOfExternalAssetsRs(string testCaseName, bool async) =>
            VerifyDataStreamOfTarUncompressedInternal("tar-rs", testCaseName, copyData: true, async);

        [Theory]
        [MemberData(nameof(GetBooleanData))]
        public async Task Throw_FifoContainsNonZeroDataSection(bool async)
        {
            using MemoryStream archiveStream = GetTarMemoryStream(CompressionMethod.Uncompressed, "golang_tar", "hdr-only");
            {
                await using TarReaderHolder readerHolder = CreateTarReader(archiveStream, async, leaveOpen: false);
                TarReader reader = readerHolder;

                Assert.NotNull(await GetNextEntry(reader, async: async)); // Just a regular file
                Assert.NotNull(await GetNextEntry(reader, async: async));
                Assert.NotNull(await GetNextEntry(reader, async: async));
                Assert.NotNull(await GetNextEntry(reader, async: async));
                Assert.NotNull(await GetNextEntry(reader, async: async));
                Assert.NotNull(await GetNextEntry(reader, async: async));
                Assert.NotNull(await GetNextEntry(reader, async: async));
                Assert.NotNull(await GetNextEntry(reader, async: async));

                if (async)
                {
                    await Assert.ThrowsAsync<InvalidDataException>(async () => await GetNextEntry(reader, async: async));
                }
                else
                {
                    Assert.Throws<InvalidDataException>(() => GetNextEntry(reader, async: async).GetAwaiter().GetResult());
                }
                        }
        }

        [Theory]
        [MemberData(nameof(GetBooleanData))]
        public async Task Throw_SingleExtendedAttributesEntryWithNoActualEntry(bool async)
        {
            using MemoryStream archiveStream = GetTarMemoryStream(CompressionMethod.Uncompressed, "golang_tar", "pax-path-hdr");
            {
                await using TarReaderHolder readerHolder = CreateTarReader(archiveStream, async, leaveOpen: false);
                TarReader reader = readerHolder;

                if (async)
                {
                    await Assert.ThrowsAsync<EndOfStreamException>(async () => await GetNextEntry(reader, async: async));
                }
                else
                {
                    Assert.Throws<EndOfStreamException>(() => GetNextEntry(reader, async: async).GetAwaiter().GetResult());
                }
                        }
        }

            // Sparse entries were created for the GNU format, so they are very rare entry types which are excluded from this test method:
        [Fact]
        public async Task ReadDataStreamOfGoLangTarGzGnu()
        {
            using MemoryStream archiveStream = GetTarMemoryStream(CompressionMethod.GZip, "golang_tar", "pax-bad-hdr-large");
            using GZipStream decompressor = new GZipStream(archiveStream, CompressionMode.Decompress);
            await VerifyDataStreamOfTarInternal(decompressor, copyData: false, async: false);
        }

        [Theory]
        [MemberData(nameof(GetAllowSpacesInOctalFieldsAndBooleanData))]
        public async Task AllowSpacesInOctalFields(string folderName, string testCaseName, bool async)
        {
            using MemoryStream archiveStream = GetTarMemoryStream(CompressionMethod.Uncompressed, folderName, testCaseName);
            {
                await using TarReaderHolder readerHolder = CreateTarReader(archiveStream, async, leaveOpen: false);
                TarReader reader = readerHolder;

                TarEntry entry;
                while ((entry = await GetNextEntry(reader, async: async)) != null)
                {
                    AssertExtensions.GreaterThan(entry.Checksum, 0);
                    AssertExtensions.GreaterThan((int)entry.Mode, 0);
        }
            }
        }

        [Theory]
        [MemberData(nameof(GetThrowArchivesWithRandomCharsAndBooleanData))]
        public async Task Throw_ArchivesWithRandomChars(string testCaseName, bool async)
        {
            using MemoryStream archiveStream = GetTarMemoryStream(CompressionMethod.Uncompressed, "golang_tar", testCaseName);
            {
                await using TarReaderHolder readerHolder = CreateTarReader(archiveStream, async, leaveOpen: false);
                TarReader reader = readerHolder;

                if (async)
                {
                    await Assert.ThrowsAsync<InvalidDataException>(async () => await GetNextEntry(reader, async: async));
                }
                else
                {
                    Assert.Throws<InvalidDataException>(() => GetNextEntry(reader, async: async).GetAwaiter().GetResult());
        }
            }
        }

        [Theory]
        [MemberData(nameof(GetBooleanData))]
        public async Task Throw_ArchiveIsShort(bool async)
        {
            // writer-big has a header for a 16G file but not its contents.
            using MemoryStream archiveStream = GetTarMemoryStream(CompressionMethod.Uncompressed, "golang_tar", "writer-big");
            // MemoryStream throws when we try to change its Position past its Length.
            {
                await using TarReaderHolder readerHolder = CreateTarReader(archiveStream, async, leaveOpen: false);
                TarReader reader = readerHolder;

                if (async)
                {
                    await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await GetNextEntry(reader, async: async));
                }
                else
                {
                    Assert.Throws<ArgumentOutOfRangeException>(() => GetNextEntry(reader, async: async).GetAwaiter().GetResult());
                }
                        }
        }

        [Theory]
        [MemberData(nameof(GetBooleanData))]
        public async Task GarbageEntryChecksumZeroReturnNull(bool async)
        {
            using MemoryStream archiveStream = GetTarMemoryStream(CompressionMethod.Uncompressed, "golang_tar", "issue12435");
            {
                await using TarReaderHolder readerHolder = CreateTarReader(archiveStream, async, leaveOpen: false);
                TarReader reader = readerHolder;

                Assert.Null(await GetNextEntry(reader, async: async));
                        }
        }

        [Theory]
        [MemberData(nameof(GetBooleanData))]
        public async Task InvalidChecksum_ThrowsInvalidDataException(bool async)
        {
            using MemoryStream archiveStream = GetTarMemoryStream(CompressionMethod.Uncompressed, "node-tar", "bad-cksum");
            {
                await using TarReaderHolder readerHolder = CreateTarReader(archiveStream, async, leaveOpen: false);
                TarReader reader = readerHolder;

                await GetNextEntry(reader, async: async); // first entry is okay
                if (async)
                {
                    await Assert.ThrowsAsync<InvalidDataException>(async () => await GetNextEntry(reader, async: async));
                }
                else
                {
                    Assert.Throws<InvalidDataException>(() => GetNextEntry(reader, async: async).GetAwaiter().GetResult());
                }
                        }
        }

        [Theory]
        [MemberData(nameof(GetSparseEntryNotSupportedAndBooleanData))]
        public async Task SparseEntryNotSupported(string testFolderName, string testCaseName, bool async)
        {
            // Currently sparse entries are not supported.
            // pax-nil-sparse-data, pax-nil-sparse-hole, pax-sparse-big
            // There are PAX archives archives in the golang folder that have extended attributes for treating a regular file as a sparse file.
            using MemoryStream archiveStream = GetTarMemoryStream(CompressionMethod.Uncompressed, testFolderName, testCaseName);
            {
                await using TarReaderHolder readerHolder = CreateTarReader(archiveStream, async, leaveOpen: false);
                TarReader reader = readerHolder;

                if (async)
                {
                    await Assert.ThrowsAsync<NotSupportedException>(async () => await GetNextEntry(reader, async: async));
                }
                else
                {
                    Assert.Throws<NotSupportedException>(() => GetNextEntry(reader, async: async).GetAwaiter().GetResult());
                }
            }
        }

        [Theory]
        [MemberData(nameof(GetBooleanData))]
        public async Task ReaderIgnoresFieldValueAfterTrailingNull(bool async)
        {
            // Construct an archive that has a filename with some data after the trailing null.
            // Fields in the tar archives are terminated by a trailing null.
            // When reading these fields the reader must ignore all bytes past that null.
            const string FileName = "  filename  ";
            const string FileNameWithDataPastTrailingNull = $"{FileName} nonesense";
            using MemoryStream ms = new();
            using (TarWriter writer = new(ms, leaveOpen: true))
            {
                var entry = new UstarTarEntry(TarEntryType.RegularFile, FileNameWithDataPastTrailingNull);
                writer.WriteEntry(entry);
            }
            ms.Position = 0;
            // Check the writer serialized the complete name passed to the constructor.
            bool archiveIsExpected = ms.ToArray().IndexOf(Encoding.UTF8.GetBytes(FileNameWithDataPastTrailingNull)) != -1;
            Assert.True(archiveIsExpected);
            // Verify the reader doesn't return the data past the trailing null.

            {
                await using TarReaderHolder readerHolder = CreateTarReader(ms, async, leaveOpen: true);
                TarReader reader = readerHolder;

                TarEntry firstEntry = await GetNextEntry(reader, async: async);
                Assert.Equal(FileName, firstEntry.Name);
                        }
        }

        [Theory]
        [MemberData(nameof(GetBooleanData))]
        public async Task DirectoryListRegularFileAndSparse(bool async)
        {
            using MemoryStream archiveStream = GetTarMemoryStream(CompressionMethod.Uncompressed, "golang_tar", "gnu-incremental");
            {
                await using TarReaderHolder readerHolder = CreateTarReader(archiveStream, async, leaveOpen: false);
                TarReader reader = readerHolder;

                TarEntry directoryList = await GetNextEntry(reader, async: async);

                Assert.Equal(TarEntryType.DirectoryList, directoryList.EntryType);
                Assert.NotNull(directoryList.DataStream);
                Assert.Equal(14, directoryList.Length);

                Assert.NotNull(await GetNextEntry(reader, async: async));

                if (async)
                {
                    await Assert.ThrowsAsync<NotSupportedException>(async () => await GetNextEntry(reader, async: async));
                }
                else
                {
                    Assert.Throws<NotSupportedException>(() => GetNextEntry(reader, async: async).GetAwaiter().GetResult());
                }
                        }
        }

        [Theory]
        [MemberData(nameof(GetBooleanData))]
        public async Task PaxSizeLargerThanMaxAllowedByStream(bool async)
        {
            using MemoryStream archiveStream = GetTarMemoryStream(CompressionMethod.Uncompressed, "golang_tar", "writer-big-long");
            // The extended attribute 'size' has the value 17179869184
            // Exception message: Stream length must be non-negative and less than 2^31 - 1 - origin
            {
                await using TarReaderHolder readerHolder = CreateTarReader(archiveStream, async, leaveOpen: false);
                TarReader reader = readerHolder;

                if (async)
                {
                    await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await GetNextEntry(reader, async: async));
                }
                else
                {
                    Assert.Throws<ArgumentOutOfRangeException>(() => GetNextEntry(reader, async: async).GetAwaiter().GetResult());
                }
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
            {
                await using TarReaderHolder readerHolder = CreateTarReader(archiveStream, async, leaveOpen: false);
                TarReader reader = readerHolder;

                TarEntry entry;
                while ((entry = await GetNextEntry(reader, copyData, async: async)) != null)
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
}
