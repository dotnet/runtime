// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using System.Text;

namespace System.IO.Compression.Tests
{
    public partial class zip_CreateTests : ZipFileTestBase
    {
        [Theory]
        [MemberData(nameof(Utf8Comment_Data))]
        public static void Create_Comment_AsciiEntryName_NullEncoding(string originalComment, string expectedComment) =>
            Create_Comment_EntryName_Encoding_Internal(AsciiFileName, originalComment, expectedComment, null);

        [Theory]
        [MemberData(nameof(Utf8Comment_Data))]
        public static void Create_Comment_AsciiEntryName_Utf8Encoding(string originalComment, string expectedComment) =>
            Create_Comment_EntryName_Encoding_Internal(AsciiFileName, originalComment, expectedComment, Encoding.UTF8);

        [Theory]
        [MemberData(nameof(Latin1Comment_Data))]
        public static void Create_Comment_AsciiEntryName_Latin1Encoding(string originalComment, string expectedComment) =>
            Create_Comment_EntryName_Encoding_Internal(AsciiFileName, originalComment, expectedComment, Encoding.Latin1);

        [Theory]
        [MemberData(nameof(Utf8Comment_Data))]
        public static void Create_Comment_Utf8EntryName_NullEncoding(string originalComment, string expectedComment) =>
            Create_Comment_EntryName_Encoding_Internal(Utf8FileName, originalComment, expectedComment, null);

        [Theory]
        [MemberData(nameof(Utf8Comment_Data))]
        public static void Create_Comment_Utf8EntryName_Utf8Encoding(string originalComment, string expectedComment) =>
            Create_Comment_EntryName_Encoding_Internal(Utf8FileName, originalComment, expectedComment, Encoding.UTF8);

        [Theory]
        [MemberData(nameof(Latin1Comment_Data))]
        public static void Create_Comment_Utf8EntryName_Latin1Encoding(string originalComment, string expectedComment) =>
            // Emoji not supported by latin1
            Create_Comment_EntryName_Encoding_Internal(Utf8AndLatin1FileName, originalComment, expectedComment, Encoding.Latin1);

        private static void Create_Comment_EntryName_Encoding_Internal(string entryName, string originalComment, string expectedComment, Encoding encoding)
        {
            using var ms = new MemoryStream();

            using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true, encoding))
            {
                ZipArchiveEntry entry = zip.CreateEntry(entryName, CompressionLevel.NoCompression);
                entry.Comment = originalComment;
                Assert.Equal(expectedComment, entry.Comment);
            }

            using (var zip = new ZipArchive(ms, ZipArchiveMode.Read, leaveOpen: false, encoding))
            {
                foreach (ZipArchiveEntry entry in zip.Entries)
                {
                    Assert.Equal(entryName, entry.Name);
                    Assert.Equal(expectedComment, entry.Comment);
                }
            }
        }
    }
}