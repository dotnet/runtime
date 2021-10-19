// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.InteropServices;
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
        [MemberData(nameof(TestComments))]
        public static void Update_ZipArchive_Comment(string? comment, Encoding? encoding)
        {
            string expectedComment = GetExpectedComment(comment);
            string updatedComment = "Updated comment " + comment;
            string expectedUpdatedComment = GetExpectedComment(updatedComment);

            using var stream = new MemoryStream();
            var testStream = new WrappedStream(stream, true, true, true, null);

            // Create
            using (var zip = new ZipArchive(testStream, ZipArchiveMode.Create, leaveOpen: true, encoding))
            {
                zip.Comment = comment;
                Assert.Equal(expectedComment, zip.Comment);
            }
            // Read (verify creation)
            using (var zip = new ZipArchive(testStream, ZipArchiveMode.Read, leaveOpen: true, encoding))
            {
                Assert.Equal(expectedComment, zip.Comment);
            }
            // Update
            using (var zip = new ZipArchive(testStream, ZipArchiveMode.Update, leaveOpen: true, encoding))
            {
                zip.Comment = updatedComment;
                Assert.Equal(expectedUpdatedComment, zip.Comment);
            }
            // Read (verify update) and autoclose
            using (var zip = new ZipArchive(testStream, ZipArchiveMode.Read, leaveOpen: false, encoding))
            {
                Assert.Equal(expectedUpdatedComment, zip.Comment);
            }
        }

        [Theory]
        [MemberData(nameof(TestComments))]
        public static void Update_ZipArchiveEntry_Comment(string? comment, Encoding? encoding)
        {
            string expectedComment = GetExpectedComment(comment);
            string updatedComment = "Updated comment " + comment;
            string expectedUpdatedComment = GetExpectedComment(updatedComment);

            var stream = new MemoryStream();
            var testStream = new WrappedStream(stream, true, true, true, null);

            // Create
            using (var zip = new ZipArchive(testStream, ZipArchiveMode.Create, leaveOpen: true, encoding))
            {
                ZipArchiveEntry entry = zip.CreateEntry("testfile.txt", CompressionLevel.NoCompression);

                entry.Comment = comment;
                Assert.Equal(expectedComment, entry.Comment);
            }
            // Read (verify creation)
            using (var zip = new ZipArchive(testStream, ZipArchiveMode.Read, leaveOpen: true, encoding))
            {
                foreach (ZipArchiveEntry entry in zip.Entries)
                {
                    Assert.Equal(expectedComment, entry.Comment);
                }
            }
            // Update
            using (var zip = new ZipArchive(testStream, ZipArchiveMode.Update, leaveOpen: true, encoding))
            {
                foreach (ZipArchiveEntry entry in zip.Entries)
                {
                    entry.Comment = updatedComment;
                    Assert.Equal(expectedUpdatedComment, entry.Comment);
                }
            }
            // Read (verify update) and autoclose
            using (var zip = new ZipArchive(testStream, ZipArchiveMode.Read, leaveOpen: false, encoding))
            {
                foreach (ZipArchiveEntry entry in zip.Entries)
                {
                    Assert.Equal(expectedUpdatedComment, entry.Comment);
                }
            }
        }

        [Theory]
        // General purpose bit flag must get the appropriate bit set if a file comment or an entry name is unicode
        [InlineData("ascii", "ascii!!!", "utf-8", "Uƒ‰÷ˆ’ı‹¸")]
        [InlineData("utf-8", "Uƒ‰÷ˆ’ı‹¸", "ascii", "ascii!!!")]
        [InlineData("ascii", "ascii!!!", "latin1", "Lƒ‰÷ˆ’ı‹¸")]
        [InlineData("latin1", "Lƒ‰÷ˆ’ı‹¸", "ascii", "ascii!!!")]
        [InlineData("utf-8", "Uƒ‰÷ˆ’ı‹¸", "latin1", "Lƒ‰÷ˆ’ı‹¸")]
        [InlineData("latin1", "Lƒ‰÷ˆ’ı‹¸", "utf-8", "Uƒ‰÷ˆ’ı‹¸")]
        public static void Update_ZipArchiveEntry_DifferentEncodingsFullNameAndComment(string en1, string s1, string en2, string s2)
        {
            Encoding e1 = Encoding.GetEncoding(en1);
            Encoding e2 = Encoding.GetEncoding(en2);
            string entryName = e1.GetString(e1.GetBytes(s1));
            string comment = e2.GetString(e2.GetBytes(s2));

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
                foreach (var entry in zip.Entries)
                {
                    Assert.Equal(entryName, entry.FullName);
                    Assert.Equal(comment, entry.Comment);
                }
            }

            // Open with no encoding to update
            using (var zip = new ZipArchive(testStream, ZipArchiveMode.Read, leaveOpen: true))
            {
                foreach (var entry in zip.Entries)
                {
                    entry.Comment = entryName; // Change it to the other string
                }
            }

            // Open with no encoding to verify
            using (var zip = new ZipArchive(testStream, ZipArchiveMode.Read, leaveOpen: false))
            {
                foreach (var entry in zip.Entries)
                {
                    Assert.Equal(entryName, entry.FullName);
                    Assert.Equal(comment, entry.Comment);
                }
            }
        }

        private static Encoding[] s_encodings = new[] { Encoding.ASCII, Encoding.Latin1, Encoding.UTF8 };

        // entryName, encoding
        public static IEnumerable<object[]> TestEntryNames()
        {
            var entryNames = new string[]
            {
                "entry.txt",
                "ƒ‰÷ˆ’ı‹¸.txt",
                new string('x', ushort.MaxValue),
            };

            foreach (string entryName in entryNames)
            {
                foreach (Encoding encoding in s_encodings)
                {
                    yield return new object[] { entryName, encoding };
                }
            }
        }

        // comment, encoding
        public static IEnumerable<object[]> TestComments()
        {
            var comments = new string[] {
                "",
                "1",
                new string('x', ushort.MaxValue),
                new string('y', ushort.MaxValue + 1), // Should get truncated
            };

            yield return new object[] { null, null }; // Should get saved as empty string.

            foreach (string comment in comments)
            {
                foreach (Encoding encoding in s_encodings)
                {
                    yield return new object[] { comment, encoding };
                }
            }

            foreach (Encoding encoding in new[] { Encoding.Latin1, Encoding.UTF8 })
            {
                yield return new object[] { "ƒ‰÷ˆ’ı‹¸", encoding };
            }
        }

        // Ensures the specified comment is returned as a non-null string no longer than ushort.MaxValue.
        private static string GetExpectedComment(string? comment)
        {
            string nonNullComment = comment ?? string.Empty;
            int nonNullCommentMaxLength = nonNullComment.Length > ushort.MaxValue ? ushort.MaxValue : nonNullComment.Length;
            return nonNullComment.Substring(0, nonNullCommentMaxLength);
        }

    }
}
