// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Compression.Tests
{
    public class zip_UpdateTests : ZipFileTestBase
    {
        [Theory]
        [InlineData("normal.zip", "normal")]
        [InlineData("fake64.zip", "small")]
        [InlineData("empty.zip", "empty")]
        [InlineData("appended.zip", "small")]
        [InlineData("prepended.zip", "small")]
        [InlineData("emptydir.zip", "emptydir")]
        [InlineData("small.zip", "small")]
        [InlineData("unicode.zip", "unicode")]
        public static async Task UpdateReadNormal(string zipFile, string zipFolder)
        {
            IsZipSameAsDir(await StreamHelpers.CreateTempCopyStream(zfile(zipFile)), zfolder(zipFolder), ZipArchiveMode.Update, requireExplicit: true, checkTimes: true);
        }

        [Fact]
        public static async Task UpdateReadTwice()
        {
            using (ZipArchive archive = new ZipArchive(await StreamHelpers.CreateTempCopyStream(zfile("small.zip")), ZipArchiveMode.Update))
            {
                ZipArchiveEntry entry = archive.Entries[0];
                string contents1, contents2;
                using (StreamReader s = new StreamReader(entry.Open()))
                {
                    contents1 = s.ReadToEnd();
                }
                using (StreamReader s = new StreamReader(entry.Open()))
                {
                    contents2 = s.ReadToEnd();
                }
                Assert.Equal(contents1, contents2);
            }
        }

        [Theory]
        [InlineData("normal")]
        [InlineData("empty")]
        [InlineData("unicode")]
        public static async Task UpdateCreate(string zipFolder)
        {
            var zs = new LocalMemoryStream();
            await CreateFromDir(zfolder(zipFolder), zs, ZipArchiveMode.Update);
            IsZipSameAsDir(zs.Clone(), zfolder(zipFolder), ZipArchiveMode.Read, requireExplicit: true, checkTimes: true);
        }

        [Theory]
        [InlineData(ZipArchiveMode.Create)]
        [InlineData(ZipArchiveMode.Update)]
        public static void EmptyEntryTest(ZipArchiveMode mode)
        {
            string data1 = "test data written to file.";
            string data2 = "more test data written to file.";
            DateTimeOffset lastWrite = new DateTimeOffset(1992, 4, 5, 12, 00, 30, new TimeSpan(-5, 0, 0));

            var baseline = new LocalMemoryStream();
            using (ZipArchive archive = new ZipArchive(baseline, mode))
            {
                AddEntry(archive, "data1.txt", data1, lastWrite);

                ZipArchiveEntry e = archive.CreateEntry("empty.txt");
                e.LastWriteTime = lastWrite;
                using (Stream s = e.Open()) { }

                AddEntry(archive, "data2.txt", data2, lastWrite);
            }

            var test = new LocalMemoryStream();
            using (ZipArchive archive = new ZipArchive(test, mode))
            {
                AddEntry(archive, "data1.txt", data1, lastWrite);

                ZipArchiveEntry e = archive.CreateEntry("empty.txt");
                e.LastWriteTime = lastWrite;

                AddEntry(archive, "data2.txt", data2, lastWrite);
            }
            //compare
            Assert.True(ArraysEqual(baseline.ToArray(), test.ToArray()), "Arrays didn't match");

            //second test, this time empty file at end
            baseline = baseline.Clone();
            using (ZipArchive archive = new ZipArchive(baseline, mode))
            {
                if (mode == ZipArchiveMode.Create)
                {
                    AddEntry(archive, "data1.txt", data1, lastWrite);

                    ZipArchiveEntry e = archive.CreateEntry("empty.txt");
                    e.LastWriteTime = lastWrite;
                    using (Stream s = e.Open()) { }
                }
                else
                {
                    Assert.Throws<InvalidOperationException>(() => AddEntry(archive, "data1.txt", data1, lastWrite));

                    Assert.Throws<InvalidOperationException>(() => archive.CreateEntry("empty.txt"));
                }
            }

            test = test.Clone();
            using (ZipArchive archive = new ZipArchive(test, mode))
            {
                if (mode == ZipArchiveMode.Create)
                {
                    AddEntry(archive, "data1.txt", data1, lastWrite);

                    ZipArchiveEntry e = archive.CreateEntry("empty.txt");
                    e.LastWriteTime = lastWrite;
                }
                else
                {
                    Assert.Throws<InvalidOperationException>(() => AddEntry(archive, "data1.txt", data1, lastWrite));

                    Assert.Throws<InvalidOperationException>(() => archive.CreateEntry("empty.txt"));
                }
            }
            //compare
            Assert.True(ArraysEqual(baseline.ToArray(), test.ToArray()), "Arrays didn't match after update");
        }

        [Fact]
        public static async Task DeleteAndMoveEntries()
        {
            //delete and move
            var testArchive = await StreamHelpers.CreateTempCopyStream(zfile("normal.zip"));

            using (ZipArchive archive = new ZipArchive(testArchive, ZipArchiveMode.Update, true))
            {
                ZipArchiveEntry toBeDeleted = archive.GetEntry("binary.wmv");
                toBeDeleted.Delete();
                toBeDeleted.Delete(); //delete twice should be okay
                ZipArchiveEntry moved = archive.CreateEntry("notempty/secondnewname.txt");
                ZipArchiveEntry orig = archive.GetEntry("notempty/second.txt");
                using (Stream origMoved = orig.Open(), movedStream = moved.Open())
                {
                    origMoved.CopyTo(movedStream);
                }
                moved.LastWriteTime = orig.LastWriteTime;
                orig.Delete();
            }

            IsZipSameAsDir(testArchive, zmodified("deleteMove"), ZipArchiveMode.Read, requireExplicit: true, checkTimes: true);

        }
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static async Task AppendToEntry(bool writeWithSpans)
        {
            //append
            Stream testArchive = await StreamHelpers.CreateTempCopyStream(zfile("normal.zip"));

            using (ZipArchive archive = new ZipArchive(testArchive, ZipArchiveMode.Update, true))
            {
                ZipArchiveEntry e = archive.GetEntry("first.txt");
                using (Stream s = e.Open())
                {
                    s.Seek(0, SeekOrigin.End);

                    byte[] data = Encoding.ASCII.GetBytes("\r\n\r\nThe answer my friend, is blowin' in the wind.");
                    if (writeWithSpans)
                    {
                        s.Write(data, 0, data.Length);
                    }
                    else
                    {
                        s.Write(new ReadOnlySpan<byte>(data));
                    }
                }

                var file = FileData.GetFile(zmodified(Path.Combine("append", "first.txt")));
                e.LastWriteTime = file.LastModifiedDate;
            }

            IsZipSameAsDir(testArchive, zmodified("append"), ZipArchiveMode.Read, requireExplicit: true, checkTimes: true);

        }
        [Fact]
        public static async Task OverwriteEntry()
        {
            //Overwrite file
            Stream testArchive = await StreamHelpers.CreateTempCopyStream(zfile("normal.zip"));

            using (ZipArchive archive = new ZipArchive(testArchive, ZipArchiveMode.Update, true))
            {
                string fileName = zmodified(Path.Combine("overwrite", "first.txt"));
                ZipArchiveEntry e = archive.GetEntry("first.txt");

                var file = FileData.GetFile(fileName);
                e.LastWriteTime = file.LastModifiedDate;

                using (var stream = await StreamHelpers.CreateTempCopyStream(fileName))
                {
                    using (Stream es = e.Open())
                    {
                        es.SetLength(0);
                        stream.CopyTo(es);
                    }
                }
            }

            IsZipSameAsDir(testArchive, zmodified("overwrite"), ZipArchiveMode.Read, requireExplicit: true, checkTimes: true);
        }

        [Fact]
        public static async Task AddFileToArchive()
        {
            //add file
            var testArchive = await StreamHelpers.CreateTempCopyStream(zfile("normal.zip"));

            using (ZipArchive archive = new ZipArchive(testArchive, ZipArchiveMode.Update, true))
            {
                await updateArchive(archive, zmodified(Path.Combine("addFile", "added.txt")), "added.txt");
            }

            IsZipSameAsDir(testArchive, zmodified ("addFile"), ZipArchiveMode.Read, requireExplicit: true, checkTimes: true);
        }

        [Fact]
        public static async Task AddFileToArchive_AfterReading()
        {
            //add file and read entries before
            Stream testArchive = await StreamHelpers.CreateTempCopyStream(zfile("normal.zip"));

            using (ZipArchive archive = new ZipArchive(testArchive, ZipArchiveMode.Update, true))
            {
                var x = archive.Entries;

                await updateArchive(archive, zmodified(Path.Combine("addFile", "added.txt")), "added.txt");
            }

            IsZipSameAsDir(testArchive, zmodified("addFile"), ZipArchiveMode.Read, requireExplicit: true, checkTimes: true);
        }

        [Fact]
        public static async Task AddFileToArchive_ThenReadEntries()
        {
            //add file and read entries after
            Stream testArchive = await StreamHelpers.CreateTempCopyStream(zfile("normal.zip"));

            using (ZipArchive archive = new ZipArchive(testArchive, ZipArchiveMode.Update, true))
            {
                await updateArchive(archive, zmodified(Path.Combine("addFile", "added.txt")), "added.txt");

                var x = archive.Entries;
            }

            IsZipSameAsDir(testArchive, zmodified("addFile"), ZipArchiveMode.Read, requireExplicit: true, checkTimes: true);
        }

        private static async Task updateArchive(ZipArchive archive, string installFile, string entryName)
        {
            ZipArchiveEntry e = archive.CreateEntry(entryName);

            var file = FileData.GetFile(installFile);
            e.LastWriteTime = file.LastModifiedDate;
            Assert.Equal(e.LastWriteTime, file.LastModifiedDate);

            using (var stream = await StreamHelpers.CreateTempCopyStream(installFile))
            {
                using (Stream es = e.Open())
                {
                    es.SetLength(0);
                    stream.CopyTo(es);
                }
            }
        }

        [Fact]
        public static async Task UpdateModeInvalidOperations()
        {
            using (LocalMemoryStream ms = await LocalMemoryStream.readAppFileAsync(zfile("normal.zip")))
            {
                ZipArchive target = new ZipArchive(ms, ZipArchiveMode.Update, leaveOpen: true);

                ZipArchiveEntry edeleted = target.GetEntry("first.txt");

                Stream s = edeleted.Open();
                //invalid ops while entry open
                Assert.Throws<IOException>(() => edeleted.Open());
                Assert.Throws<InvalidOperationException>(() => { var x = edeleted.Length; });
                Assert.Throws<InvalidOperationException>(() => { var x = edeleted.CompressedLength; });
                Assert.Throws<IOException>(() => edeleted.Delete());
                s.Dispose();

                //invalid ops on stream after entry closed
                Assert.Throws<ObjectDisposedException>(() => s.ReadByte());

                Assert.Throws<InvalidOperationException>(() => { var x = edeleted.Length; });
                Assert.Throws<InvalidOperationException>(() => { var x = edeleted.CompressedLength; });

                edeleted.Delete();
                //invalid ops while entry deleted
                Assert.Throws<InvalidOperationException>(() => edeleted.Open());
                Assert.Throws<InvalidOperationException>(() => { edeleted.LastWriteTime = new DateTimeOffset(); });

                ZipArchiveEntry e = target.GetEntry("notempty/second.txt");

                target.Dispose();

                Assert.Throws<ObjectDisposedException>(() => { var x = target.Entries; });
                Assert.Throws<ObjectDisposedException>(() => target.CreateEntry("dirka"));
                Assert.Throws<ObjectDisposedException>(() => e.Open());
                Assert.Throws<ObjectDisposedException>(() => e.Delete());
                Assert.Throws<ObjectDisposedException>(() => { e.LastWriteTime = new DateTimeOffset(); });
            }
        }

        [Fact]
        public void UpdateUncompressedArchive()
        {
            var utf8WithoutBom = new Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
            byte[] fileContent;
            using (var memStream = new MemoryStream())
            {
                using (var zip = new ZipArchive(memStream, ZipArchiveMode.Create))
                {
                    ZipArchiveEntry entry = zip.CreateEntry("testing", CompressionLevel.NoCompression);
                    using (var writer = new StreamWriter(entry.Open(), utf8WithoutBom))
                    {
                        writer.Write("hello");
                        writer.Flush();
                    }
                }
                fileContent = memStream.ToArray();
            }
            byte compressionMethod = fileContent[8];
            Assert.Equal(0, compressionMethod); // stored => 0, deflate => 8
            using (var memStream = new MemoryStream())
            {
                memStream.Write(fileContent);
                memStream.Position = 0;
                using (var archive = new ZipArchive(memStream, ZipArchiveMode.Update))
                {
                    ZipArchiveEntry entry = archive.GetEntry("testing");
                    using (var writer = new StreamWriter(entry.Open(), utf8WithoutBom))
                    {
                        writer.Write("new");
                        writer.Flush();
                    }
                }
                byte[] modifiedTestContent = memStream.ToArray();
                compressionMethod = modifiedTestContent[8];
                Assert.Equal(0, compressionMethod); // stored => 0, deflate => 8
            }
        }

        [Theory]
        [MemberData(nameof(EncodingSpecificStrings_Data))]
        public static void Update_ZipArchive_Comment(string? comment, Encoding? encoding)
        {
            // The archive comment max size in bytes is determined by the internal
            // ZipEndOfCentralDirectoryBlock.ZipFileCommentMaxLength const field.
            // The current value of that field is set to ushort.MaxValue.

            (string expectedComment, string updatedComment, string expectedUpdatedComment) =
                GetTestStringsForEncoding(comment, ushort.MaxValue, encoding);

            using var stream = new MemoryStream();
            var testStream = new WrappedStream(stream, true, true, true, null);

            // Create
            using (var zip = new ZipArchive(testStream, ZipArchiveMode.Create, leaveOpen: true, encoding))
            {
                zip.Comment = comment;
                Assert.Equal(expectedComment.Length, zip.Comment.Length);
                Assert.Equal(expectedComment, zip.Comment);
            }
            // Read (verify creation)
            using (var zip = new ZipArchive(testStream, ZipArchiveMode.Read, leaveOpen: true, encoding))
            {
                Assert.Equal(expectedComment.Length, zip.Comment.Length);
                Assert.Equal(expectedComment, zip.Comment);
            }
            // Update
            using (var zip = new ZipArchive(testStream, ZipArchiveMode.Update, leaveOpen: true, encoding))
            {
                zip.Comment = updatedComment;
                Assert.Equal(expectedUpdatedComment.Length, zip.Comment.Length);
                Assert.Equal(expectedUpdatedComment, zip.Comment);
            }
            // Read (verify update) and close stream
            using (var zip = new ZipArchive(testStream, ZipArchiveMode.Read, leaveOpen: false, encoding))
            {
                Assert.Equal(expectedUpdatedComment.Length, zip.Comment.Length);
                Assert.Equal(expectedUpdatedComment, zip.Comment);
            }
        }

        [Theory]
        [MemberData(nameof(EncodingSpecificStrings_Data))]
        public static void Update_ZipArchiveEntry_Comment(string? comment, Encoding? encoding)
        {
            // The entry comment max size in bytes is set to ushort.MaxValue internally.

            (string expectedComment, string updatedComment, string expectedUpdatedComment) =
                GetTestStringsForEncoding(comment, ushort.MaxValue, encoding);

            var stream = new MemoryStream();
            var testStream = new WrappedStream(stream, true, true, true, null);

            // Create
            using (var zip = new ZipArchive(testStream, ZipArchiveMode.Create, leaveOpen: true, encoding))
            {
                ZipArchiveEntry entry = zip.CreateEntry("testfile.txt", CompressionLevel.NoCompression);

                entry.Comment = comment;
                Assert.Equal(expectedComment.Length, entry.Comment.Length);
                Assert.Equal(expectedComment, entry.Comment);
            }
            // Read (verify creation)
            using (var zip = new ZipArchive(testStream, ZipArchiveMode.Read, leaveOpen: true, encoding))
            {
                ZipArchiveEntry entry = zip.Entries.FirstOrDefault();

                Assert.Equal(expectedComment.Length, entry.Comment.Length);
                Assert.Equal(expectedComment, entry.Comment);
            }
            // Update
            using (var zip = new ZipArchive(testStream, ZipArchiveMode.Update, leaveOpen: true, encoding))
            {
                ZipArchiveEntry entry = zip.Entries.FirstOrDefault();

                entry.Comment = updatedComment;
                Assert.Equal(expectedUpdatedComment.Length, entry.Comment.Length);
                Assert.Equal(expectedUpdatedComment, entry.Comment);
            }
            // Read (verify update) and close stream
            using (var zip = new ZipArchive(testStream, ZipArchiveMode.Read, leaveOpen: false, encoding))
            {
                ZipArchiveEntry entry = zip.Entries.FirstOrDefault();

                Assert.Equal(expectedUpdatedComment.Length, entry.Comment.Length);
                Assert.Equal(expectedUpdatedComment, entry.Comment);
            }
        }

        [Fact]
        // General purpose bit flag must get the appropriate bit set if a file comment or an entry name is unicode
        public static void Update_Comments_EntryName_DifferentEncodings()
        {
            (string expectedArchiveComment, string updatedArchiveComment, string expectedUpdatedArchiveComment) =
                GetTestStringsForEncoding("utf8 archive comment: ööö", ushort.MaxValue, Encoding.UTF8);

            (string expectedEntryComment, string updatedEntryComment, string expectedUpdatedEntryComment) =
                GetTestStringsForEncoding("latin archive comment: ñññ", ushort.MaxValue, Encoding.Latin1);

            // Only need one because the entry name cannot be updated
            string expectedEntryName = GetEncodedStringOfExpectedByteLength(Encoding.ASCII, ushort.MaxValue, "ascii entry name");

            var stream = new MemoryStream();
            var testStream = new WrappedStream(stream, true, true, true, null);

            // Create with no encoding to autoselect it if one of the two strings is unicode
            using (var zip = new ZipArchive(testStream, ZipArchiveMode.Create, leaveOpen: true))
            {
                zip.Comment = expectedArchiveComment;
                ZipArchiveEntry entry = zip.CreateEntry(expectedEntryName, CompressionLevel.NoCompression);
                entry.Comment = expectedEntryComment;

                Assert.Equal(expectedArchiveComment, zip.Comment);
                Assert.Equal(expectedEntryName, entry.FullName);
                Assert.Equal(expectedEntryComment, entry.Comment);
            }

            // Open with no encoding to verify
            using (var zip = new ZipArchive(testStream, ZipArchiveMode.Read, leaveOpen: true))
            {
                ZipArchiveEntry entry = zip.Entries.FirstOrDefault();

                Assert.Equal(expectedArchiveComment, zip.Comment);
                Assert.Equal(expectedArchiveComment, zip.Comment);
                Assert.Equal(expectedEntryName, entry.FullName);
                Assert.Equal(expectedEntryComment, entry.Comment);
            }

            // Open with no encoding to update
            using (var zip = new ZipArchive(testStream, ZipArchiveMode.Update, leaveOpen: true))
            {
                zip.Comment = updatedArchiveComment;
                ZipArchiveEntry entry = zip.Entries.FirstOrDefault();
                entry.Comment = updatedEntryComment;

                Assert.Equal(updatedArchiveComment, zip.Comment);
                Assert.Equal(updatedEntryComment, entry.Comment);
            }

            // Open with no encoding to verify, close stream
            using (var zip = new ZipArchive(testStream, ZipArchiveMode.Read, leaveOpen: false))
            {
                ZipArchiveEntry entry = zip.Entries.FirstOrDefault();

                Assert.Equal(updatedArchiveComment, zip.Comment);
                Assert.Equal(expectedEntryName, entry.FullName);
                Assert.Equal(updatedEntryComment, entry.Comment);
            }
        }

        // For zip comments, entry comments.
        public static IEnumerable<object[]> EncodingSpecificStrings_Data()
        {
            yield return new object[] { null, null }; // Should get saved as empty string.

            foreach (Encoding encoding in new[] { null, Encoding.ASCII, Encoding.Latin1, Encoding.UTF8 })
            {
                yield return new object[] { string.Empty, encoding };
                yield return new object[] { "1", encoding };
                yield return new object[] { new string('x', ushort.MaxValue), encoding };
                yield return new object[] { new string('y', ushort.MaxValue + 1), encoding };  // Should get truncated
            }

            foreach (Encoding encoding in new[] { Encoding.Latin1,Encoding.UTF8 }) // Exclude ASCII
            {
               yield return new object[] { "ü", encoding };
               yield return new object[] { new string('ñ', ushort.MaxValue), encoding }; // Should get truncated
               yield return new object[] { new string('ö', ushort.MaxValue + 1), encoding }; // Should get truncated
            }
        }

        // For the specified text, returns 3 strings:
        // - The specified text, but encoded in the specified encoding, and truncated to the specified max byte length (adjusted to encoding character size).
        // - The specified text with an extra prefix, no encoding or length modifications.
        // - The specified text with an extra prefix, but encoded and truncated.
        private static (string, string, string) GetTestStringsForEncoding(string text, int maxByteLength, Encoding encoding)
        {
            string expectedText = GetEncodedStringOfExpectedByteLength(encoding, maxByteLength, text);
            string updatedText = "Updated text: " + expectedText;
            string expectedUpdatedText = GetEncodedStringOfExpectedByteLength(encoding, maxByteLength, updatedText);

            return (expectedText, updatedText, expectedUpdatedText);
        }

        // Gets the specified string as a non-null string, in the specified encoding,
        // and no longer than the specified byte length, adjusted for the encoding.
        // If no encoding is specified, UTF8 is the fallback, just like ZipArchive does.
        private static string GetEncodedStringOfExpectedByteLength(Encoding? encoding, int maxBytes, string? comment)
        {
            string nonNullComment = comment ?? string.Empty;

            if (nonNullComment.Length > 0)
            {
                encoding ??= GetEncoding(comment);
                byte[] bytes = encoding.GetBytes(nonNullComment);

                if (bytes.Length > maxBytes)
                {
                    int bytesPerChar = encoding.GetMaxByteCount(1);

                    int encodedCharsThatFit = maxBytes / bytesPerChar;
                    int totalBytesToTruncate = encodedCharsThatFit * bytesPerChar;

                    bytes = bytes[0..totalBytesToTruncate];
                }

                return encoding.GetString(bytes);
            }

            return nonNullComment;
        }

        private static Encoding GetEncoding(string text)
        {
            if (!string.IsNullOrEmpty(text))
            {
                foreach (char c in text)
                {
                    // Same verification as in ZipHelper
                    if (c > 126 || c < 32)
                    {
                        return Encoding.UTF8;
                    }
                }
            }

            return Encoding.ASCII;
        }
    }
}
