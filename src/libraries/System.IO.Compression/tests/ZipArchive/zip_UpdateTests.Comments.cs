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
        [MemberData(nameof(Encoding_GetData))]
        public static void Update_ZipArchive_Comment(
            Encoding constructorEncoding,
            string originalComment, string expectedOriginalComment,
            string updatedComment, string expectedUpdatedComment)
        {
            // The archive comment max size in bytes is determined by the internal
            // ZipEndOfCentralDirectoryBlock.ZipFileCommentMaxLength const field.
            // The current value of that field is set to ushort.MaxValue.

            using var stream = new MemoryStream();
            var testStream = new WrappedStream(stream, true, true, true, null);

            // Create
            using (var zip = new ZipArchive(testStream, ZipArchiveMode.Create, leaveOpen: true, constructorEncoding))
            {
                zip.Comment = originalComment;
                Assert.Equal(expectedOriginalComment.Length, zip.Comment.Length);
                Assert.Equal(expectedOriginalComment, zip.Comment);
            }
            // Read (verify creation)
            using (var zip = new ZipArchive(testStream, ZipArchiveMode.Read, leaveOpen: true, constructorEncoding))
            {
                Assert.Equal(expectedOriginalComment.Length, zip.Comment.Length);
                Assert.Equal(expectedOriginalComment, zip.Comment);
            }
            // Update
            using (var zip = new ZipArchive(testStream, ZipArchiveMode.Update, leaveOpen: true, constructorEncoding))
            {
                zip.Comment = updatedComment;
                Assert.Equal(expectedUpdatedComment.Length, zip.Comment.Length);
                Assert.Equal(expectedUpdatedComment, zip.Comment);
            }
            // Read (verify update) and close stream
            using (var zip = new ZipArchive(testStream, ZipArchiveMode.Read, leaveOpen: false, constructorEncoding))
            {
                Assert.Equal(expectedUpdatedComment.Length, zip.Comment.Length);
                Assert.Equal(expectedUpdatedComment, zip.Comment);
            }
        }

        [Theory]
        [MemberData(nameof(Encoding_GetData))]
        public static void Update_ZipArchiveEntry_Comment(
            Encoding constructorEncoding,
            string originalComment, string expectedOriginalComment,
            string updatedComment, string expectedUpdatedComment)
        {
            var stream = new MemoryStream();
            var testStream = new WrappedStream(stream, true, true, true, null);

            // Create
            using (var zip = new ZipArchive(testStream, ZipArchiveMode.Create, leaveOpen: true, constructorEncoding))
            {
                ZipArchiveEntry entry = zip.CreateEntry("testfile.txt", CompressionLevel.NoCompression);

                entry.Comment = originalComment;
                Assert.Equal(expectedOriginalComment.Length, entry.Comment.Length);
                Assert.Equal(expectedOriginalComment, entry.Comment);
            }
            // Read (verify creation)
            using (var zip = new ZipArchive(testStream, ZipArchiveMode.Read, leaveOpen: true, constructorEncoding))
            {
                ZipArchiveEntry entry = zip.Entries.Single();

                Assert.Equal(expectedOriginalComment.Length, entry.Comment.Length);
                Assert.Equal(expectedOriginalComment, entry.Comment);
            }
            // Update
            using (var zip = new ZipArchive(testStream, ZipArchiveMode.Update, leaveOpen: true, constructorEncoding))
            {
                ZipArchiveEntry entry = zip.Entries.Single();

                entry.Comment = updatedComment;
                Assert.Equal(expectedUpdatedComment.Length, entry.Comment.Length);
                Assert.Equal(expectedUpdatedComment, entry.Comment);
            }
            // Read (verify update) and close stream
            using (var zip = new ZipArchive(testStream, ZipArchiveMode.Read, leaveOpen: false, constructorEncoding))
            {
                ZipArchiveEntry entry = zip.Entries.Single();

                Assert.Equal(expectedUpdatedComment.Length, entry.Comment.Length);
                Assert.Equal(expectedUpdatedComment, entry.Comment);
            }
        }

        [Theory]
        [InlineData(20127, 65001)] // ascii, utf8
        [InlineData(65001, 20127)] // utf8, ascii
        // General purpose bit flag must get the appropriate bit set if a file comment or an entry name is unicode
        public static void Update_Comments_EntryName_DifferentEncodings(int entryNameCodePage, int commentCodePage)
        {
            Encoding entryNameEncoding = Encoding.GetEncoding(entryNameCodePage);
            Encoding commentEncoding = Encoding.GetEncoding(commentCodePage);

            string entryName = entryNameEncoding.GetString(Encoding.ASCII.GetBytes("EntryName.txt"));
            string comment = commentEncoding.GetString(Encoding.ASCII.GetBytes("Comment!"));

            var stream = new MemoryStream();
            var testStream = new WrappedStream(stream, true, true, true, null);

            // Create with no encoding to autoselect it if one of the two strings is unicode
            using (var zip = new ZipArchive(testStream, ZipArchiveMode.Create, leaveOpen: true))
            {
                ZipArchiveEntry entry = zip.CreateEntry(entryName, CompressionLevel.NoCompression);
                entry.Comment = comment;

                Assert.Equal(entryName, entry.FullName);
                Assert.Equal(comment, entry.Comment);
            }

            // Open with no encoding to verify
            using (var zip = new ZipArchive(testStream, ZipArchiveMode.Read, leaveOpen: true))
            {
                ZipArchiveEntry entry = zip.Entries.FirstOrDefault();

                Assert.Equal(entryName, entry.FullName);
                Assert.Equal(comment, entry.Comment);
            }
        }

        // constructorEncoding, originalComment, expectedOriginalComment, updatedComment, expectedUpdatedComment
        public static IEnumerable<object[]> Encoding_GetData()
        {
            string largeAscii = new string('a', ushort.MaxValue + 100);
            string truncatedAsciiAsAscii = GetTruncatedString(Encoding.ASCII, largeAscii);
            string truncatedAsciiAsUtf8 = GetTruncatedString(Encoding.UTF8, largeAscii);
            string truncatedAsciiAsLatin1 = GetTruncatedString(Encoding.Latin1, largeAscii);

            char enie = '\u00d1'; // Ñ
            string largeUnicode = new string(enie, ushort.MaxValue + 100);
            string truncatedUnicodeAsUtf8 = GetTruncatedString(Encoding.UTF8, largeUnicode);
            string truncatedUnicodeAsLatin1 = GetTruncatedString(Encoding.Latin1, largeUnicode);

            // Detect ASCII automatically
            yield return new object[] { null, null, string.Empty, largeAscii, truncatedAsciiAsAscii };
            yield return new object[] { null, largeAscii, truncatedAsciiAsAscii, null, string.Empty };

            // Specify encoding in constructor
            // Preserve ASCII
            yield return new object[] { Encoding.ASCII, null, string.Empty, largeAscii, truncatedAsciiAsAscii };
            yield return new object[] { Encoding.ASCII, largeAscii, truncatedAsciiAsAscii, null, string.Empty };

            // Force encoding to UTF8
            yield return new object[] { Encoding.UTF8, null, string.Empty, largeAscii, truncatedAsciiAsUtf8 };
            yield return new object[] { Encoding.UTF8, largeAscii, truncatedAsciiAsUtf8, null, string.Empty };

            // Force encoding to Latin1
            yield return new object[] { Encoding.Latin1, null, string.Empty, largeAscii, truncatedAsciiAsLatin1 };
            yield return new object[] { Encoding.Latin1, largeAscii, truncatedAsciiAsLatin1, null, string.Empty };

            // Detect unicode automatically, choose UTF8
            yield return new object[] { null, null, string.Empty, largeUnicode, truncatedUnicodeAsUtf8 };
            yield return new object[] { null, largeUnicode, truncatedUnicodeAsUtf8, null, string.Empty };

            // Specify encoding in constructor
            // Force encoding to UTF8
            yield return new object[] { Encoding.UTF8, null, string.Empty, largeUnicode, truncatedUnicodeAsUtf8 };
            yield return new object[] { Encoding.UTF8, largeUnicode, truncatedUnicodeAsUtf8, null, string.Empty };

            // Force encoding to Latin1
            yield return new object[] { Encoding.Latin1, null, string.Empty, largeUnicode, truncatedUnicodeAsLatin1 };
            yield return new object[] { Encoding.Latin1, largeUnicode, truncatedUnicodeAsLatin1, null, string.Empty };
        }

        private static string GetTruncatedString(Encoding encoding, string largeString)
        {
            int bytesPerChar = encoding.GetMaxByteCount(1);
            int encodedCharsThatFit = ushort.MaxValue / bytesPerChar;
            int totalBytesToTruncate = encodedCharsThatFit * bytesPerChar;
            byte[] truncatedBytes = encoding.GetBytes(largeString)[0..totalBytesToTruncate];
            return encoding.GetString(truncatedBytes);
        }
    }
}