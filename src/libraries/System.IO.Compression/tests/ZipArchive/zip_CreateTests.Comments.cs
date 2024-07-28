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
        [MemberData(nameof(Utf8Comment_Data))]
        public static void Create_Comment_Utf8EntryName_Utf8Encoding_Default(string originalComment, string expectedComment) =>
            Create_Comment_EntryName_Encoding_Internal(Utf8FileName, originalComment, expectedComment, expectedComment, Encoding.UTF8, null);

        [Theory]
        [MemberData(nameof(Latin1Comment_Data))]
        public static void Create_Comment_Utf8EntryName_Latin1Encoding(string originalComment, string expectedComment) =>
            // Emoji not supported by latin1
            Create_Comment_EntryName_Encoding_Internal(Utf8AndLatin1FileName, originalComment, expectedComment, Encoding.Latin1);

        [Theory]
        [MemberData(nameof(Utf8Comment_Data))]
        public static void Create_Comment_Utf8EntryName_Utf8Encoding_Prioritised(string originalComment, string expectedComment)
            // UTF8 encoding bit is set in the general-purpose bit flags. The verification encoding of Latin1 should be ignored
            => Create_Comment_EntryName_Encoding_Internal(Utf8FileName, originalComment, expectedComment, expectedComment, Encoding.UTF8, Encoding.Latin1);

        [Theory]
        [MemberData(nameof(MismatchingEncodingComment_Data))]
        public static void Create_Comment_AsciiEntryName_Utf8Decoding_Invalid(string originalComment, string expectedPreWriteComment, string expectedPostWriteComment)
            // The UTF8 encoding bit in the general-purpose bit flags should not be set, filenames should be encoded with Latin1, and thus
            // decoding with UTF8 should result in incorrect filenames. This is because the filenames and comments contain code points in the
            // range 0xC0..0xFF (which Latin1 encodes in one byte, and UTF8 encodes in two bytes.)
            => Create_Comment_EntryName_Encoding_Internal(AsciiFileName, originalComment, expectedPreWriteComment, expectedPostWriteComment, Encoding.Latin1, Encoding.UTF8);

        [Theory]
        [MemberData(nameof(MismatchingEncodingComment_Data))]
        public static void Create_Comment_AsciiEntryName_DefaultDecoding_Utf8(string originalComment, string expectedPreWriteComment, string expectedPostWriteComment)
            // Filenames should be encoded with Latin1, resulting in the UTF8 encoding bit in the general-purpose bit flags not being set.
            // However, failing to specify an encoding (or specifying a null encoding) for the read should result in UTF8 being used anyway.
            // This should result in incorrect filenames, since the filenames and comments contain code points in the range 0xC0..0xFF (which
            // Latin1 encodes in one byte, and UTF8 encodes in two bytes.)
            => Create_Comment_EntryName_Encoding_Internal(AsciiFileName, originalComment, expectedPreWriteComment, expectedPostWriteComment, Encoding.Latin1, null);

        private static void Create_Comment_EntryName_Encoding_Internal(string entryName, string originalComment, string expectedComment, Encoding encoding)
            => Create_Comment_EntryName_Encoding_Internal(entryName, originalComment, expectedComment, expectedComment, encoding, encoding);

        private static void Create_Comment_EntryName_Encoding_Internal(string entryName, string originalComment,
            string expectedPreWriteComment, string expectedPostWriteComment,
            Encoding creationEncoding, Encoding verificationEncoding)
        {
            using var ms = new MemoryStream();

            using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true, creationEncoding))
            {
                ZipArchiveEntry entry = zip.CreateEntry(entryName, CompressionLevel.NoCompression);
                entry.Comment = originalComment;
                // The expected pre-write and post-write comment can be different when testing encodings which vary between operations.
                Assert.Equal(expectedPreWriteComment, entry.Comment);
            }

            using (var zip = new ZipArchive(ms, ZipArchiveMode.Read, leaveOpen: false, verificationEncoding))
            {
                foreach (ZipArchiveEntry entry in zip.Entries)
                {
                    Assert.Equal(entryName, entry.Name);
                    Assert.Equal(expectedPostWriteComment, entry.Comment);
                }
            }
        }
    }
}
