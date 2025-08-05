// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using System.Text;
using System.Threading.Tasks;

namespace System.IO.Compression.Tests
{
    public partial class zip_CreateTests : ZipFileTestBase
    {
        [Theory]
        [MemberData(nameof(Utf8Comment_Data))]
        public static Task Create_Comment_AsciiEntryName_NullEncoding(string originalComment, string expectedComment, bool async) =>
            Create_Comment_EntryName_Encoding_Internal(AsciiFileName, originalComment, expectedComment, null, async);

        [Theory]
        [MemberData(nameof(Utf8Comment_Data))]
        public static Task Create_Comment_AsciiEntryName_Utf8Encoding(string originalComment, string expectedComment, bool async) =>
            Create_Comment_EntryName_Encoding_Internal(AsciiFileName, originalComment, expectedComment, Encoding.UTF8, async);

        [Theory]
        [MemberData(nameof(Latin1Comment_Data))]
        public static Task Create_Comment_AsciiEntryName_Latin1Encoding(string originalComment, string expectedComment, bool async) =>
            Create_Comment_EntryName_Encoding_Internal(AsciiFileName, originalComment, expectedComment, Encoding.Latin1, async);

        [Theory]
        [MemberData(nameof(Utf8Comment_Data))]
        public static Task Create_Comment_Utf8EntryName_NullEncoding(string originalComment, string expectedComment, bool async) =>
            Create_Comment_EntryName_Encoding_Internal(Utf8FileName, originalComment, expectedComment, null, async);

        [Theory]
        [MemberData(nameof(Utf8Comment_Data))]
        public static Task Create_Comment_Utf8EntryName_Utf8Encoding(string originalComment, string expectedComment, bool async) =>
            Create_Comment_EntryName_Encoding_Internal(Utf8FileName, originalComment, expectedComment, Encoding.UTF8, async);

        [Theory]
        [MemberData(nameof(Utf8Comment_Data))]
        public static Task Create_Comment_Utf8EntryName_Utf8Encoding_Default(string originalComment, string expectedComment, bool async) =>
            Create_Comment_EntryName_Encoding_Internal(Utf8FileName, originalComment, expectedComment, expectedComment, Encoding.UTF8, null, async);

        [Theory]
        [MemberData(nameof(Latin1Comment_Data))]
        public static Task Create_Comment_Utf8EntryName_Latin1Encoding(string originalComment, string expectedComment, bool async) =>
            // Emoji not supported by latin1
            Create_Comment_EntryName_Encoding_Internal(Utf8AndLatin1FileName, originalComment, expectedComment, Encoding.Latin1, async);

        [Theory]
        [MemberData(nameof(Utf8Comment_Data))]
        public static Task Create_Comment_Utf8EntryName_Utf8Encoding_Prioritised(string originalComment, string expectedComment, bool async)
            // UTF8 encoding bit is set in the general-purpose bit flags. The verification encoding of Latin1 should be ignored
            => Create_Comment_EntryName_Encoding_Internal(Utf8FileName, originalComment, expectedComment, expectedComment, Encoding.UTF8, Encoding.Latin1, async);

        [Theory]
        [MemberData(nameof(MismatchingEncodingComment_Data))]
        public static Task Create_Comment_AsciiEntryName_Utf8Decoding_Invalid(string originalComment, string expectedPreWriteComment, string expectedPostWriteComment, bool async)
            // The UTF8 encoding bit in the general-purpose bit flags should not be set, filenames should be encoded with Latin1, and thus
            // decoding with UTF8 should result in incorrect filenames. This is because the filenames and comments contain code points in the
            // range 0xC0..0xFF (which Latin1 encodes in one byte, and UTF8 encodes in two bytes.)
            => Create_Comment_EntryName_Encoding_Internal(AsciiFileName, originalComment, expectedPreWriteComment, expectedPostWriteComment, Encoding.Latin1, Encoding.UTF8, async);

        [Theory]
        [MemberData(nameof(MismatchingEncodingComment_Data))]
        public static Task Create_Comment_AsciiEntryName_DefaultDecoding_Utf8(string originalComment, string expectedPreWriteComment, string expectedPostWriteComment, bool async)
            // Filenames should be encoded with Latin1, resulting in the UTF8 encoding bit in the general-purpose bit flags not being set.
            // However, failing to specify an encoding (or specifying a null encoding) for the read should result in UTF8 being used anyway.
            // This should result in incorrect filenames, since the filenames and comments contain code points in the range 0xC0..0xFF (which
            // Latin1 encodes in one byte, and UTF8 encodes in two bytes.)
            => Create_Comment_EntryName_Encoding_Internal(AsciiFileName, originalComment, expectedPreWriteComment, expectedPostWriteComment, Encoding.Latin1, null, async);

        private static Task Create_Comment_EntryName_Encoding_Internal(string entryName, string originalComment, string expectedComment, Encoding encoding, bool async)
            => Create_Comment_EntryName_Encoding_Internal(entryName, originalComment, expectedComment, expectedComment, encoding, encoding, async);

        private static async Task Create_Comment_EntryName_Encoding_Internal(string entryName, string originalComment,
            string expectedPreWriteComment, string expectedPostWriteComment,
            Encoding creationEncoding, Encoding verificationEncoding, bool async)
        {
            using var ms = new MemoryStream();

            var zip = await CreateZipArchive(async, ms, ZipArchiveMode.Create, leaveOpen: true, creationEncoding);
            ZipArchiveEntry entry1 = zip.CreateEntry(entryName, CompressionLevel.NoCompression);
            entry1.Comment = originalComment;
            // The expected pre-write and post-write comment can be different when testing encodings which vary between operations.
            Assert.Equal(expectedPreWriteComment, entry1.Comment);
            await DisposeZipArchive(async, zip);

            zip = await CreateZipArchive(async, ms, ZipArchiveMode.Read, leaveOpen: false, verificationEncoding);
            foreach (ZipArchiveEntry entry2 in zip.Entries)
            {
                Assert.Equal(entryName, entry2.Name);
                Assert.Equal(expectedPostWriteComment, entry2.Comment);
            }
            await DisposeZipArchive(async, zip);
        }
    }
}
