// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
                AddEntry(archive, "data1.txt", data1, lastWrite);

                ZipArchiveEntry e = archive.CreateEntry("empty.txt");
                e.LastWriteTime = lastWrite;
                using (Stream s = e.Open()) { }
            }

            test = test.Clone();
            using (ZipArchive archive = new ZipArchive(test, mode))
            {
                AddEntry(archive, "data1.txt", data1, lastWrite);

                ZipArchiveEntry e = archive.CreateEntry("empty.txt");
                e.LastWriteTime = lastWrite;
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
    }
}

