// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace System.IO.Compression.Tests
{
    public partial class zip_UpdateTests : ZipFileTestBase
    {
        [Theory]
        [MemberData(nameof(Utf8Comment_Data))]
        public static void Update_Comment_AsciiEntryName_NullEncoding(string originalComment, string expectedComment) =>
            Update_Comment_EntryName_Encoding_Internal(AsciiFileName,
                originalComment, expectedComment, null,
                ALettersUShortMaxValueMinusOneAndCopyRightChar, ALettersUShortMaxValueMinusOne);

        [Theory]
        [MemberData(nameof(Utf8Comment_Data))]
        public static void Update_Comment_AsciiEntryName_Utf8Encoding(string originalComment, string expectedComment) =>
            Update_Comment_EntryName_Encoding_Internal(AsciiFileName,
                originalComment, expectedComment, Encoding.UTF8,
                ALettersUShortMaxValueMinusOneAndCopyRightChar, ALettersUShortMaxValueMinusOne);

        [Theory]
        [MemberData(nameof(Latin1Comment_Data))]
        public static void Update_Comment_AsciiEntryName_Latin1Encoding(string originalComment, string expectedComment) =>
            Update_Comment_EntryName_Encoding_Internal(AsciiFileName,
                originalComment, expectedComment, Encoding.Latin1,
                ALettersUShortMaxValueMinusOneAndTwoCopyRightChars, ALettersUShortMaxValueMinusOneAndCopyRightChar);

        [Theory]
        [MemberData(nameof(Utf8Comment_Data))]
        public static void Update_Comment_Utf8EntryName_NullEncoding(string originalComment, string expectedComment) =>
            Update_Comment_EntryName_Encoding_Internal(Utf8FileName,
                originalComment, expectedComment, null,
                ALettersUShortMaxValueMinusOneAndCopyRightChar, ALettersUShortMaxValueMinusOne);

        [Theory]
        [MemberData(nameof(Utf8Comment_Data))]
        public static void Update_Comment_Utf8EntryName_Utf8Encoding(string originalComment, string expectedComment) =>
            Update_Comment_EntryName_Encoding_Internal(Utf8FileName,
                originalComment, expectedComment, Encoding.UTF8,
                ALettersUShortMaxValueMinusOneAndCopyRightChar, ALettersUShortMaxValueMinusOne);

        [Theory]
        [MemberData(nameof(Latin1Comment_Data))]
        public static void Update_Comment_Utf8EntryName_Latin1Encoding(string originalComment, string expectedComment) =>
            // Emoji is not supported/detected in latin1
            Update_Comment_EntryName_Encoding_Internal(Utf8AndLatin1FileName,
                originalComment, expectedComment, Encoding.Latin1,
                ALettersUShortMaxValueMinusOneAndTwoCopyRightChars, ALettersUShortMaxValueMinusOneAndCopyRightChar);

        private static void Update_Comment_EntryName_Encoding_Internal(string entryName,
            string originalCreateComment, string expectedCreateComment, Encoding encoding,
            string originalUpdateComment, string expectedUpdateComment)
        {
            using var ms = new MemoryStream();

            using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true, encoding))
            {
                ZipArchiveEntry entry = zip.CreateEntry(entryName, CompressionLevel.NoCompression);
                entry.Comment = originalCreateComment;
                Assert.Equal(expectedCreateComment, entry.Comment);
            }

            using (var zip = new ZipArchive(ms, ZipArchiveMode.Read, leaveOpen: true, encoding))
            {
                foreach (ZipArchiveEntry entry in zip.Entries)
                {
                    Assert.Equal(expectedCreateComment, entry.Comment);
                }
            }

            using (var zip = new ZipArchive(ms, ZipArchiveMode.Update, leaveOpen: true, encoding))
            {
                foreach (ZipArchiveEntry entry in zip.Entries)
                {
                    entry.Comment = originalUpdateComment;
                    Assert.Equal(expectedUpdateComment, entry.Comment);
                }
            }

            using (var zip = new ZipArchive(ms, ZipArchiveMode.Read, leaveOpen: false, encoding))
            {
                foreach (ZipArchiveEntry entry in zip.Entries)
                {
                    Assert.Equal(expectedUpdateComment, entry.Comment);
                }
            }
        }
    }
}