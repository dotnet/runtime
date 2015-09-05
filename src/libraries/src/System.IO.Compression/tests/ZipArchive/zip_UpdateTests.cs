// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Compression.Tests
{
    public class zip_UpdateTests
    {
        [Fact]
        public static async Task UpdateReadNormal()
        {
            ZipTest.IsZipSameAsDir(
                await StreamHelpers.CreateTempCopyStream(ZipTest.zfile("normal.zip")), ZipTest.zfolder("normal"), ZipArchiveMode.Update, false, false);
            ZipTest.IsZipSameAsDir(
                await StreamHelpers.CreateTempCopyStream(ZipTest.zfile("fake64.zip")), ZipTest.zfolder("small"), ZipArchiveMode.Update, false, false);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                ZipTest.IsZipSameAsDir(
                    await StreamHelpers.CreateTempCopyStream(ZipTest.zfile("unicode.zip")), ZipTest.zfolder("unicode"), ZipArchiveMode.Update, false, false);
            }
            ZipTest.IsZipSameAsDir(
                await StreamHelpers.CreateTempCopyStream(ZipTest.zfile("empty.zip")), ZipTest.zfolder("empty"), ZipArchiveMode.Update, false, false);
            ZipTest.IsZipSameAsDir(
                await StreamHelpers.CreateTempCopyStream(ZipTest.zfile("appended.zip")), ZipTest.zfolder("small"), ZipArchiveMode.Update, false, false);
            ZipTest.IsZipSameAsDir(
                await StreamHelpers.CreateTempCopyStream(ZipTest.zfile("prepended.zip")), ZipTest.zfolder("small"), ZipArchiveMode.Update, false, false);
        }

        [Fact]
        public static async Task UpdateReadTwice()
        {
            using (ZipArchive archive = new ZipArchive(await StreamHelpers.CreateTempCopyStream(ZipTest.zfile("small.zip")), ZipArchiveMode.Update))
            {
                ZipArchiveEntry entry = archive.Entries[0];
                String contents1, contents2;
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

        [Fact]
        public static async Task UpdateCreate()
        {
            await testFolder("normal");
            await testFolder("empty");
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                await testFolder("unicode");
            }
        }

        public static async Task testFolder(String s)
        {
            var zs = new LocalMemoryStream();
            await ZipTest.CreateFromDir(ZipTest.zfolder(s), zs, ZipArchiveMode.Update);
            ZipTest.IsZipSameAsDir(zs.Clone(), ZipTest.zfolder(s), ZipArchiveMode.Read, false, false);
        }


        [Fact]
        public static void UpdateEmptyEntry()
        {
            ZipTest.EmptyEntryTest(ZipArchiveMode.Update);
        }

        [Fact]
        public static async Task UpdateModifications()
        {
            //delete and move
            var testArchive = await StreamHelpers.CreateTempCopyStream(ZipTest.zfile("normal.zip"));

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

            ZipTest.IsZipSameAsDir(testArchive, ZipTest.zmodified("deleteMove"), ZipArchiveMode.Read, false, false);
            
            //append
            testArchive = await StreamHelpers.CreateTempCopyStream(ZipTest.zfile("normal.zip"));

            using (ZipArchive archive = new ZipArchive(testArchive, ZipArchiveMode.Update, true))
            {
                ZipArchiveEntry e = archive.GetEntry("first.txt");

                using (StreamWriter s = new StreamWriter(e.Open()))
                {
                    s.BaseStream.Seek(0, SeekOrigin.End);

                    s.Write("\r\n\r\nThe answer my friend, is blowin' in the wind.");
                }

                e.LastWriteTime = new DateTimeOffset(2010, 7, 7, 11, 57, 18, new TimeSpan(-7, 0, 0));
            }

            ZipTest.IsZipSameAsDir(testArchive, ZipTest.zmodified("append"), ZipArchiveMode.Read, false, false);

            //Overwrite file
            testArchive = await StreamHelpers.CreateTempCopyStream(ZipTest.zfile("normal.zip"));

            using (ZipArchive archive = new ZipArchive(testArchive, ZipArchiveMode.Update, true))
            {
                String fileName = ZipTest.zmodified(Path.Combine("overwrite", "first.txt"));
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

            ZipTest.IsZipSameAsDir(testArchive, ZipTest.zmodified("overwrite"), ZipArchiveMode.Read, false, false);
        }

        [Fact]
        public static async Task UpdateAddFile()
        {
            //add file
            var testArchive = await StreamHelpers.CreateTempCopyStream(ZipTest.zfile("normal.zip"));

            using (ZipArchive archive = new ZipArchive(testArchive, ZipArchiveMode.Update, true))
            {
                await updateArchive(archive, ZipTest.zmodified(Path.Combine("addFile", "added.txt")), "added.txt");
            }

            ZipTest.IsZipSameAsDir(testArchive, ZipTest.zmodified ("addFile"), ZipArchiveMode.Read, false, false);

            //add file and read entries before
            testArchive = await StreamHelpers.CreateTempCopyStream(ZipTest.zfile("normal.zip"));

            using (ZipArchive archive = new ZipArchive(testArchive, ZipArchiveMode.Update, true))
            {
                var x = archive.Entries;

                await updateArchive(archive, ZipTest.zmodified(Path.Combine("addFile", "added.txt")), "added.txt");
            }

            ZipTest.IsZipSameAsDir(testArchive, ZipTest.zmodified("addFile"), ZipArchiveMode.Read, false, false);


            //add file and read entries after
            testArchive = await StreamHelpers.CreateTempCopyStream(ZipTest.zfile("normal.zip"));

            using (ZipArchive archive = new ZipArchive(testArchive, ZipArchiveMode.Update, true))
            {
                await updateArchive(archive, ZipTest.zmodified(Path.Combine("addFile", "added.txt")), "added.txt");
                
                var x = archive.Entries;
            }

            ZipTest.IsZipSameAsDir(testArchive, ZipTest.zmodified("addFile"), ZipArchiveMode.Read, false, false);
        }

        private static async Task updateArchive(ZipArchive archive, String installFile, String entryName)
        {
            String fileName = installFile;
            ZipArchiveEntry e = archive.CreateEntry(entryName);

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

        [Fact]
        public static async Task UpdateModeInvalidOperations()
        {
            using (LocalMemoryStream ms = await LocalMemoryStream.readAppFileAsync(ZipTest.zfile("normal.zip")))
            {
                ZipArchive target = new ZipArchive(ms, ZipArchiveMode.Update, true);

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
    }
}

