// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
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
        public void Read_Archive_File(TarEntryFormat format, TestTarFormat testFormat) =>
            Read_Archive_File_Internal(format, testFormat);

        [Theory]
        [InlineData(TarEntryFormat.V7, TestTarFormat.v7)]
        [InlineData(TarEntryFormat.Ustar, TestTarFormat.ustar)]
        [InlineData(TarEntryFormat.Pax, TestTarFormat.pax)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.gnu)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.oldgnu)]
        public void Read_Archive_File_HardLink(TarEntryFormat format, TestTarFormat testFormat) =>
            Read_Archive_File_HardLink_Internal(format, testFormat);

        [Theory]
        [InlineData(TarEntryFormat.V7, TestTarFormat.v7)]
        [InlineData(TarEntryFormat.Ustar, TestTarFormat.ustar)]
        [InlineData(TarEntryFormat.Pax, TestTarFormat.pax)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.gnu)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.oldgnu)]
        public void Read_Archive_File_SymbolicLink(TarEntryFormat format, TestTarFormat testFormat) =>
            Read_Archive_File_SymbolicLink_Internal(format, testFormat);

        [Theory]
        [InlineData(TarEntryFormat.V7, TestTarFormat.v7)]
        [InlineData(TarEntryFormat.Ustar, TestTarFormat.ustar)]
        [InlineData(TarEntryFormat.Pax, TestTarFormat.pax)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.gnu)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.oldgnu)]
        public void Read_Archive_Folder_File(TarEntryFormat format, TestTarFormat testFormat) =>
            Read_Archive_Folder_File_Internal(format, testFormat);

        [Theory]
        [InlineData(TarEntryFormat.V7, TestTarFormat.v7)]
        [InlineData(TarEntryFormat.Ustar, TestTarFormat.ustar)]
        [InlineData(TarEntryFormat.Pax, TestTarFormat.pax)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.gnu)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.oldgnu)]
        public void Read_Archive_Folder_File_Utf8(TarEntryFormat format, TestTarFormat testFormat) =>
            Read_Archive_Folder_File_Utf8_Internal(format, testFormat);

        [Theory]
        [InlineData(TarEntryFormat.V7, TestTarFormat.v7)]
        [InlineData(TarEntryFormat.Ustar, TestTarFormat.ustar)]
        [InlineData(TarEntryFormat.Pax, TestTarFormat.pax)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.gnu)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.oldgnu)]
        public void Read_Archive_Folder_Subfolder_File(TarEntryFormat format, TestTarFormat testFormat) =>
            Read_Archive_Folder_Subfolder_File_Internal(format, testFormat);

        [Theory]
        [InlineData(TarEntryFormat.V7, TestTarFormat.v7)]
        [InlineData(TarEntryFormat.Ustar, TestTarFormat.ustar)]
        [InlineData(TarEntryFormat.Pax, TestTarFormat.pax)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.gnu)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.oldgnu)]
        public void Read_Archive_FolderSymbolicLink_Folder_Subfolder_File(TarEntryFormat format, TestTarFormat testFormat) =>
            Read_Archive_FolderSymbolicLink_Folder_Subfolder_File_Internal(format, testFormat);

        [Theory]
        [InlineData(TarEntryFormat.V7, TestTarFormat.v7)]
        [InlineData(TarEntryFormat.Ustar, TestTarFormat.ustar)]
        [InlineData(TarEntryFormat.Pax, TestTarFormat.pax)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.gnu)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.oldgnu)]
        public void Read_Archive_Many_Small_Files(TarEntryFormat format, TestTarFormat testFormat) =>
            Read_Archive_Many_Small_Files_Internal(format, testFormat);

        [Theory]
        // V7 does not support longer filenames
        [InlineData(TarEntryFormat.Ustar, TestTarFormat.ustar)]
        [InlineData(TarEntryFormat.Pax, TestTarFormat.pax)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.gnu)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.oldgnu)]
        public void Read_Archive_LongPath_Splitable_Under255(TarEntryFormat format, TestTarFormat testFormat) =>
            Read_Archive_LongPath_Splitable_Under255_Internal(format, testFormat);

        [Theory]
        // V7 does not support block devices, character devices or fifos
        [InlineData(TarEntryFormat.Ustar, TestTarFormat.ustar)]
        [InlineData(TarEntryFormat.Pax, TestTarFormat.pax)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.gnu)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.oldgnu)]
        public void Read_Archive_SpecialFiles(TarEntryFormat format, TestTarFormat testFormat) =>
            Read_Archive_SpecialFiles_Internal(format, testFormat);

        [Theory]
        // Neither V7 not Ustar can handle links with long target filenames
        [InlineData(TarEntryFormat.Pax, TestTarFormat.pax)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.gnu)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.oldgnu)]
        public void Read_Archive_File_LongSymbolicLink(TarEntryFormat format, TestTarFormat testFormat) =>
            Read_Archive_File_LongSymbolicLink_Internal(format, testFormat);

        [Theory]
        // Neither V7 not Ustar can handle a path that does not have separators that can be split under 100 bytes
        [InlineData(TarEntryFormat.Pax, TestTarFormat.pax)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.gnu)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.oldgnu)]
        public void Read_Archive_LongFileName_Over100_Under255(TarEntryFormat format, TestTarFormat testFormat) =>
            Read_Archive_LongFileName_Over100_Under255_Internal(format, testFormat);

        [Theory]
        // Neither V7 not Ustar can handle path lengths waaaay beyond name+prefix length
        [InlineData(TarEntryFormat.Pax, TestTarFormat.pax)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.gnu)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.oldgnu)]
        public void Read_Archive_LongPath_Over255(TarEntryFormat format, TestTarFormat testFormat) =>
            Read_Archive_LongPath_Over255_Internal(format, testFormat);

        [Theory]
        [MemberData(nameof(GetV7TestCaseNames))]
        public void ReadDataStreamOfTarGzV7(string testCaseName) =>
            VerifyDataStreamOfTarGzInternal(TestTarFormat.v7, testCaseName, copyData: false);

        [Theory]
        [MemberData(nameof(GetUstarTestCaseNames))]
        public void ReadDataStreamOfTarGzUstar(string testCaseName) =>
            VerifyDataStreamOfTarGzInternal(TestTarFormat.ustar, testCaseName, copyData: false);

        [Theory]
        [MemberData(nameof(GetPaxAndGnuTestCaseNames))]
        public void ReadDataStreamOfTarGzPax(string testCaseName) =>
            VerifyDataStreamOfTarGzInternal(TestTarFormat.pax, testCaseName, copyData: false);

        [Theory]
        [MemberData(nameof(GetPaxAndGnuTestCaseNames))]
        public void ReadDataStreamOfTarGzPaxGea(string testCaseName) =>
            VerifyDataStreamOfTarGzInternal(TestTarFormat.pax_gea, testCaseName, copyData: false);

        [Theory]
        [MemberData(nameof(GetPaxAndGnuTestCaseNames))]
        public void ReadDataStreamOfTarGzOldGnu(string testCaseName) =>
            VerifyDataStreamOfTarGzInternal(TestTarFormat.oldgnu, testCaseName, copyData: false);

        [Theory]
        [MemberData(nameof(GetPaxAndGnuTestCaseNames))]
        public void ReadDataStreamOfTarGzGnu(string testCaseName) =>
            VerifyDataStreamOfTarGzInternal(TestTarFormat.gnu, testCaseName, copyData: false);

        [Theory]
        [MemberData(nameof(GetV7TestCaseNames))]
        public void ReadCopiedDataStreamOfTarGzV7(string testCaseName) =>
            VerifyDataStreamOfTarGzInternal(TestTarFormat.v7, testCaseName, copyData: true);

        [Theory]
        [MemberData(nameof(GetUstarTestCaseNames))]
        public void ReadCopiedDataStreamOfTarGzUstar(string testCaseName) =>
            VerifyDataStreamOfTarGzInternal(TestTarFormat.ustar, testCaseName, copyData: true);

        [Theory]
        [MemberData(nameof(GetPaxAndGnuTestCaseNames))]
        public void ReadCopiedDataStreamOfTarGzPax(string testCaseName) =>
            VerifyDataStreamOfTarGzInternal(TestTarFormat.pax, testCaseName, copyData: true);

        [Theory]
        [MemberData(nameof(GetPaxAndGnuTestCaseNames))]
        public void ReadCopiedDataStreamOfTarGzPaxGea(string testCaseName) =>
            VerifyDataStreamOfTarGzInternal(TestTarFormat.pax_gea, testCaseName, copyData: true);

        [Theory]
        [MemberData(nameof(GetPaxAndGnuTestCaseNames))]
        public void ReadCopiedDataStreamOfTarGzOldGnu(string testCaseName) =>
            VerifyDataStreamOfTarGzInternal(TestTarFormat.oldgnu, testCaseName, copyData: true);

        [Theory]
        [MemberData(nameof(GetPaxAndGnuTestCaseNames))]
        public void ReadCopiedDataStreamOfTarGzGnu(string testCaseName) =>
            VerifyDataStreamOfTarGzInternal(TestTarFormat.gnu, testCaseName, copyData: true);

        [Theory]
        [MemberData(nameof(GetGoLangTarTestCaseNames))]
        public void ReadDataStreamOfExternalAssetsGoLang(string testCaseName)
        {
            if (ShouldSkipGoLangAsset(testCaseName))
            {
                return;
            }
            VerifyDataStreamOfTarUncompressedInternal("golang_tar", testCaseName, copyData: false);
        }

        [Theory]
        [MemberData(nameof(GetNodeTarTestCaseNames))]
        public void ReadDataStreamOfExternalAssetsNode(string testCaseName) =>
            VerifyDataStreamOfTarUncompressedInternal("node-tar", testCaseName, copyData: false);

        [Theory]
        [MemberData(nameof(GetRsTarTestCaseNames))]
        public void ReadDataStreamOfExternalAssetsRs(string testCaseName)
        {
            if (testCaseName == "spaces")
            {
                // Tested separately
                return;
            }
            VerifyDataStreamOfTarUncompressedInternal("tar-rs", testCaseName, copyData: false);
        }

        [Theory]
        [MemberData(nameof(GetGoLangTarTestCaseNames))]
        public void ReadCopiedDataStreamOfExternalAssetsGoLang(string testCaseName)
        {
            if (ShouldSkipGoLangAsset(testCaseName))
            {
                return;
            }
            VerifyDataStreamOfTarUncompressedInternal("golang_tar", testCaseName, copyData: true);
        }

        [Theory]
        [MemberData(nameof(GetNodeTarTestCaseNames))]
        public void ReadCopiedDataStreamOfExternalAssetsNode(string testCaseName) =>
            VerifyDataStreamOfTarUncompressedInternal("node-tar", testCaseName, copyData: true);

        [Theory]
        [MemberData(nameof(GetRsTarTestCaseNames))]
        public void ReadCopiedDataStreamOfExternalAssetsRs(string testCaseName)
        {
            if (testCaseName == "spaces")
            {
                // Tested separately
                return;
            }
            VerifyDataStreamOfTarUncompressedInternal("tar-rs", testCaseName, copyData: true);
        }

        private static void VerifyDataStreamOfTarUncompressedInternal(string testFolderName, string testCaseName, bool copyData)
        {
            using MemoryStream archiveStream = GetTarMemoryStream(CompressionMethod.Uncompressed, testFolderName, testCaseName);
            VerifyDataStreamOfTarInternal(archiveStream, copyData);
        }

        private static void VerifyDataStreamOfTarGzInternal(TestTarFormat testTarFormat, string testCaseName, bool copyData)
        {
            using MemoryStream archiveStream = GetTarMemoryStream(CompressionMethod.GZip, testTarFormat, testCaseName);
            using GZipStream decompressor = new GZipStream(archiveStream, CompressionMode.Decompress);
            VerifyDataStreamOfTarInternal(decompressor, copyData);
        }

        private static void VerifyDataStreamOfTarInternal(Stream archiveStream, bool copyData)
        {
            using TarReader reader = new TarReader(archiveStream);

            TarEntry entry;

            while ((entry = reader.GetNextEntry(copyData)) != null)
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
