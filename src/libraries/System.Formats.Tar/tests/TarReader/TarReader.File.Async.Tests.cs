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
        public Task ReadDataStreamOfExternalAssetsGoLangAsync(string testCaseName)
        {
            if (ShouldSkipGoLangAsset(testCaseName))
            {
                return Task.CompletedTask;
            }
            return VerifyDataStreamOfTarUncompressedInternalAsync("golang_tar", testCaseName, copyData: false);
        }

        [Theory]
        [MemberData(nameof(GetNodeTarTestCaseNames))]
        public Task ReadDataStreamOfExternalAssetsNodeAsync(string testCaseName) =>
            VerifyDataStreamOfTarUncompressedInternalAsync("node-tar", testCaseName, copyData: false);

        [Theory]
        [MemberData(nameof(GetRsTarTestCaseNames))]
        public Task ReadDataStreamOfExternalAssetsRsAsync(string testCaseName)
        {
            if (testCaseName == "spaces")
            {
                // Tested separately
                return Task.CompletedTask;
            }
            return VerifyDataStreamOfTarUncompressedInternalAsync("tar-rs", testCaseName, copyData: false);
        }

        [Theory]
        [MemberData(nameof(GetGoLangTarTestCaseNames))]
        public Task ReadCopiedDataStreamOfExternalAssetsGoLangAsync(string testCaseName)
        {
            if (ShouldSkipGoLangAsset(testCaseName))
            {
                return Task.CompletedTask;
            }
            return VerifyDataStreamOfTarUncompressedInternalAsync("golang_tar", testCaseName, copyData: true);
        }

        [Theory]
        [MemberData(nameof(GetNodeTarTestCaseNames))]
        public Task ReadCopiedDataStreamOfExternalAssetsNodeAsync(string testCaseName) =>
            VerifyDataStreamOfTarUncompressedInternalAsync("node-tar", testCaseName, copyData: true);

        [Theory]
        [MemberData(nameof(GetRsTarTestCaseNames))]
        public Task ReadCopiedDataStreamOfExternalAssetsRsAsync(string testCaseName)
        {
            if (testCaseName == "spaces")
            {
                // Tested separately
                return Task.CompletedTask;
            }
            return VerifyDataStreamOfTarUncompressedInternalAsync("tar-rs", testCaseName, copyData: true);
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
