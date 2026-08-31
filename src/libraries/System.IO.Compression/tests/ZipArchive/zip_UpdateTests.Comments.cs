// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Compression.Tests
{
    public partial class zip_UpdateTests : ZipFileTestBase
    {
        [Theory]
        [MemberData(nameof(Utf8Comment_Data))]
        public static Task Update_Comment_AsciiEntryName_NullEncoding(string originalComment, string expectedComment, bool async) =>
            Update_Comment_EntryName_Encoding_Internal(AsciiFileName,
                originalComment, expectedComment, null,
                ALettersUShortMaxValueMinusOneAndCopyRightChar, ALettersUShortMaxValueMinusOne, async);

        [Theory]
        [MemberData(nameof(Utf8Comment_Data))]
        public static Task Update_Comment_AsciiEntryName_Utf8Encoding(string originalComment, string expectedComment, bool async) =>
            Update_Comment_EntryName_Encoding_Internal(AsciiFileName,
                originalComment, expectedComment, Encoding.UTF8,
                ALettersUShortMaxValueMinusOneAndCopyRightChar, ALettersUShortMaxValueMinusOne, async);

        [Theory]
        [MemberData(nameof(Latin1Comment_Data))]
        public static Task Update_Comment_AsciiEntryName_Latin1Encoding(string originalComment, string expectedComment, bool async) =>
            Update_Comment_EntryName_Encoding_Internal(AsciiFileName,
                originalComment, expectedComment, Encoding.Latin1,
                ALettersUShortMaxValueMinusOneAndTwoCopyRightChars, ALettersUShortMaxValueMinusOneAndCopyRightChar, async);

        [Theory]
        [MemberData(nameof(Utf8Comment_Data))]
        public static Task Update_Comment_Utf8EntryName_NullEncoding(string originalComment, string expectedComment, bool async) =>
            Update_Comment_EntryName_Encoding_Internal(Utf8FileName,
                originalComment, expectedComment, null,
                ALettersUShortMaxValueMinusOneAndCopyRightChar, ALettersUShortMaxValueMinusOne, async);

        [Theory]
        [MemberData(nameof(Utf8Comment_Data))]
        public static Task Update_Comment_Utf8EntryName_Utf8Encoding(string originalComment, string expectedComment, bool async) =>
            Update_Comment_EntryName_Encoding_Internal(Utf8FileName,
                originalComment, expectedComment, Encoding.UTF8,
                ALettersUShortMaxValueMinusOneAndCopyRightChar, ALettersUShortMaxValueMinusOne, async);

        [Theory]
        [MemberData(nameof(Latin1Comment_Data))]
        public static Task Update_Comment_Utf8EntryName_Latin1Encoding(string originalComment, string expectedComment, bool async) =>
            // Emoji is not supported/detected in latin1
            Update_Comment_EntryName_Encoding_Internal(Utf8AndLatin1FileName,
                originalComment, expectedComment, Encoding.Latin1,
                ALettersUShortMaxValueMinusOneAndTwoCopyRightChars, ALettersUShortMaxValueMinusOneAndCopyRightChar, async);

        private static async Task Update_Comment_EntryName_Encoding_Internal(string entryName,
            string originalCreateComment, string expectedCreateComment, Encoding encoding,
            string originalUpdateComment, string expectedUpdateComment, bool async)
        {
            using var ms = new MemoryStream();

            ZipArchive zip = await CreateZipArchive(async, ms, ZipArchiveMode.Create, leaveOpen: true, encoding);
            ZipArchiveEntry entry1 = zip.CreateEntry(entryName, CompressionLevel.NoCompression);
            entry1.Comment = originalCreateComment;
            Assert.Equal(expectedCreateComment, entry1.Comment);
            await DisposeZipArchive(async, zip);

            zip = await CreateZipArchive(async, ms, ZipArchiveMode.Read, leaveOpen: true, encoding);
            foreach (ZipArchiveEntry entry2 in zip.Entries)
            {
                Assert.Equal(expectedCreateComment, entry2.Comment);
            }
            await DisposeZipArchive(async, zip);

            zip = await CreateZipArchive(async, ms, ZipArchiveMode.Update, leaveOpen: true, encoding);
            foreach (ZipArchiveEntry entry3 in zip.Entries)
            {
                entry3.Comment = originalUpdateComment;
                Assert.Equal(expectedUpdateComment, entry3.Comment);
            }
            await DisposeZipArchive(async, zip);

            zip = await CreateZipArchive(async, ms, ZipArchiveMode.Read, leaveOpen: false, encoding);
            foreach (ZipArchiveEntry entry4 in zip.Entries)
            {
                Assert.Equal(expectedUpdateComment, entry4.Comment);
            }
            await DisposeZipArchive(async, zip);
        }
    }
}
