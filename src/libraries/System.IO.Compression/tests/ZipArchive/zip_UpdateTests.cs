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

                    byte[] data = "\r\n\r\nThe answer my friend, is blowin' in the wind."u8.ToArray();
                    if (writeWithSpans)
                    {
                        s.Write(new ReadOnlySpan<byte>(data));
                    }
                    else
                    {
                        s.Write(data, 0, data.Length);
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

            IsZipSameAsDir(testArchive, zmodified("addFile"), ZipArchiveMode.Read, requireExplicit: true, checkTimes: true);
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

        [Fact]
        public void Update_VerifyDuplicateEntriesAreAllowed()
        {
            using var ms = new MemoryStream();
            using var archive = new ZipArchive(ms, ZipArchiveMode.Update);

            string entryName = "foo";
            AddEntry(archive, entryName, contents: "xxx", DateTimeOffset.Now);
            AddEntry(archive, entryName, contents: "yyy", DateTimeOffset.Now);

            Assert.Equal(2, archive.Entries.Count);
        }

        [Fact]
        public static async Task Update_PerformMinimalWritesWhenNoFilesChanged()
        {
            using (LocalMemoryStream ms = await LocalMemoryStream.readAppFileAsync(zfile("normal.zip")))
            using (CallTrackingStream trackingStream = new CallTrackingStream(ms))
            {
                int writesCalled = trackingStream.TimesCalled(nameof(trackingStream.Write));
                int writeBytesCalled = trackingStream.TimesCalled(nameof(trackingStream.WriteByte));

                ZipArchive target = new ZipArchive(trackingStream, ZipArchiveMode.Update, leaveOpen: true);
                int archiveEntries = target.Entries.Count;

                target.Dispose();
                writesCalled = trackingStream.TimesCalled(nameof(trackingStream.Write)) - writesCalled;
                writeBytesCalled = trackingStream.TimesCalled(nameof(trackingStream.WriteByte)) - writeBytesCalled;

                // No changes to the archive should result in no writes to the file.
                Assert.Equal(0, writesCalled + writeBytesCalled);
            }
        }

        [Theory]
        [InlineData(49, 1, 1)]
        [InlineData(40, 3, 2)]
        [InlineData(30, 5, 3)]
        [InlineData(0, 8, 1)]
        public void Update_PerformMinimalWritesWhenFixedLengthEntryHeaderFieldChanged(int startIndex, int entriesToModify, int step)
        {
            byte[] sampleEntryContents = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19];
            byte[] sampleZipFile = CreateZipFile(50, sampleEntryContents);

            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(sampleZipFile);
                ms.Seek(0, SeekOrigin.Begin);

                using (CallTrackingStream trackingStream = new CallTrackingStream(ms))
                {
                    // Open the first archive in Update mode, then change the value of {entriesToModify} fixed-length entry headers
                    // (LastWriteTime.) Verify the correct number of writes performed as a result, then reopen the same
                    // archive, get the entries and make sure that the fields hold the expected value.
                    int writesCalled = trackingStream.TimesCalled(nameof(trackingStream.Write));
                    int writeBytesCalled = trackingStream.TimesCalled(nameof(trackingStream.WriteByte));
                    ZipArchive target = new ZipArchive(trackingStream, ZipArchiveMode.Update, leaveOpen: true);
                    List<(string EntryName, DateTimeOffset LastWriteTime)> updatedMetadata = new(entriesToModify);

                    for (int i = 0; i < entriesToModify; i++)
                    {
                        int modificationIndex = startIndex + (i * step);
                        ZipArchiveEntry entryToModify = target.Entries[modificationIndex];
                        string entryName = entryToModify.FullName;
                        DateTimeOffset expectedDateTimeOffset = entryToModify.LastWriteTime.AddHours(1.0);

                        entryToModify.LastWriteTime = expectedDateTimeOffset;
                        updatedMetadata.Add((entryName, expectedDateTimeOffset));
                    }

                    target.Dispose();

                    writesCalled = trackingStream.TimesCalled(nameof(trackingStream.Write)) - writesCalled;
                    writeBytesCalled = trackingStream.TimesCalled(nameof(trackingStream.WriteByte)) - writeBytesCalled;
                    // As above, check 1: the number of writes performed should be minimal.
                    // 2 writes per archive entry for the local file header.
                    // 2 writes per archive entry for the central directory header.
                    // 1 write (sometimes 2, if there's a comment) for the end of central directory block.
                    // The EOCD block won't change as a result of our modifications, so is excluded from the counts.
                    Assert.Equal(((2 + 2) * entriesToModify), writesCalled + writeBytesCalled);

                    trackingStream.Seek(0, SeekOrigin.Begin);
                    target = new ZipArchive(trackingStream, ZipArchiveMode.Read);

                    for (int i = 0; i < entriesToModify; i++)
                    {
                        int modificationIndex = startIndex + (i * step);
                        var expectedValues = updatedMetadata[i];
                        ZipArchiveEntry verifiedEntry = target.Entries[modificationIndex];

                        // Check 2: the field holds the expected value (and thus has been written to the file.)
                        Assert.NotNull(verifiedEntry);
                        Assert.Equal(expectedValues.EntryName, verifiedEntry.FullName);
                        Assert.Equal(expectedValues.LastWriteTime, verifiedEntry.LastWriteTime);
                    }

                    // Check 3: no other data has been corrupted as a result
                    for (int i = 0; i < target.Entries.Count; i++)
                    {
                        ZipArchiveEntry entry = target.Entries[i];
                        byte[] expectedBuffer = [.. sampleEntryContents, (byte)(i % byte.MaxValue)];
                        byte[] readBuffer = new byte[expectedBuffer.Length];

                        using (Stream readStream = entry.Open())
                        {
                            readStream.Read(readBuffer.AsSpan());
                        }

                        Assert.Equal(expectedBuffer, readBuffer);
                    }

                    target.Dispose();
                }
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(10)]
        [InlineData(20)]
        [InlineData(30)]
        [InlineData(49)]
        public void Update_PerformMinimalWritesWhenEntryDataChanges(int index)
            => Update_PerformMinimalWritesWithDataAndHeaderChanges(index, -1);

        [Theory]
        [InlineData(0, 0)]
        [InlineData(20, 40)]
        [InlineData(30, 10)]
        public void Update_PerformMinimalWritesWithDataAndHeaderChanges(int dataChangeIndex, int lastWriteTimeChangeIndex)
        {
            byte[] sampleEntryContents = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19];
            byte[] sampleZipFile = CreateZipFile(50, sampleEntryContents);
            byte[] expectedUpdatedEntryContents = [19, 18, 17, 16, 15, 14, 13, 12, 11, 10];

            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(sampleZipFile);
                ms.Seek(0, SeekOrigin.Begin);

                using (CallTrackingStream trackingStream = new CallTrackingStream(ms))
                {
                    // Open the archive in Update mode, then rewrite the data of the {dataChangeIndex}th entry
                    // and set the LastWriteTime of the {lastWriteTimeChangeIndex}th entry.
                    // Verify the correct number of writes performed as a result, then reopen the same
                    // archive, get the entries and make sure that the fields hold the expected value.
                    int writesCalled = trackingStream.TimesCalled(nameof(trackingStream.Write));
                    int writeBytesCalled = trackingStream.TimesCalled(nameof(trackingStream.WriteByte));
                    ZipArchive target = new ZipArchive(trackingStream, ZipArchiveMode.Update, leaveOpen: true);
                    ZipArchiveEntry entryToRewrite = target.Entries[dataChangeIndex];
                    int totalEntries = target.Entries.Count;
                    int expectedEntriesToWrite = target.Entries.Count - dataChangeIndex;
                    DateTimeOffset expectedWriteTime = default;

                    if (lastWriteTimeChangeIndex != -1)
                    {
                        ZipArchiveEntry entryToModify = target.Entries[lastWriteTimeChangeIndex];

                        expectedWriteTime = entryToModify.LastWriteTime.AddHours(1.0);
                        entryToModify.LastWriteTime = expectedWriteTime;
                    }

                    using (var entryStream = entryToRewrite.Open())
                    {
                        entryStream.SetLength(0);
                        entryStream.Write(expectedUpdatedEntryContents);
                    }

                    target.Dispose();

                    writesCalled = trackingStream.TimesCalled(nameof(trackingStream.Write)) - writesCalled;
                    writeBytesCalled = trackingStream.TimesCalled(nameof(trackingStream.WriteByte)) - writeBytesCalled;

                    // If the data changed first, then every entry after it will be written in full. If the fixed-length
                    // metadata changed first, some entries which won't have been fully written - just updated in place.
                    // 2 writes per archive entry for the local file header.
                    // 2 writes per archive entry for the central directory header.
                    // 2 writes for the file data of the updated entry itself
                    // 1 write per archive entry for the file data of other entries after this in the file
                    // 1 write (sometimes 2, if there's a comment) for the end of central directory block.
                    // All of the central directory headers must be rewritten after an entry's data has been modified.
                    if (dataChangeIndex <= lastWriteTimeChangeIndex || lastWriteTimeChangeIndex == -1)
                    {
                        // dataChangeIndex -> totalEntries: rewrite in full
                        // all central directories headers
                        Assert.Equal(1 + 1 + ((2 + 1) * expectedEntriesToWrite) + (2 * totalEntries), writesCalled + writeBytesCalled);
                    }
                    else
                    {
                        // lastWriteTimeChangeIndex: partial rewrite
                        // dataChangeIndex -> totalEntries: rewrite in full
                        // all central directory headers
                        Assert.Equal(1 + 1 + ((2 + 1) * expectedEntriesToWrite) + (2 * totalEntries) + 2, writesCalled + writeBytesCalled);
                    }

                    trackingStream.Seek(0, SeekOrigin.Begin);
                    target = new ZipArchive(trackingStream, ZipArchiveMode.Read);

                    // Check 2: no other data has been corrupted as a result
                    for (int i = 0; i < target.Entries.Count; i++)
                    {
                        ZipArchiveEntry entry = target.Entries[i];
                        byte[] expectedBuffer = i == dataChangeIndex
                            ? expectedUpdatedEntryContents
                            : [.. sampleEntryContents, (byte)(i % byte.MaxValue)];
                        byte[] readBuffer = new byte[expectedBuffer.Length];

                        using (Stream readStream = entry.Open())
                        {
                            readStream.Read(readBuffer.AsSpan());
                        }

                        Assert.Equal(expectedBuffer, readBuffer);

                        if (i == lastWriteTimeChangeIndex)
                        {
                            Assert.Equal(expectedWriteTime, entry.LastWriteTime);
                        }
                    }
                }
            }
        }

        [Fact]
        public async Task Update_PerformMinimalWritesWhenArchiveCommentChanged()
        {
            using (LocalMemoryStream ms = await LocalMemoryStream.readAppFileAsync(zfile("normal.zip")))
            using (CallTrackingStream trackingStream = new CallTrackingStream(ms))
            {
                int writesCalled = trackingStream.TimesCalled(nameof(trackingStream.Write));
                int writeBytesCalled = trackingStream.TimesCalled(nameof(trackingStream.WriteByte));
                string expectedComment = "14 byte comment";

                ZipArchive target = new ZipArchive(trackingStream, ZipArchiveMode.Update, leaveOpen: true);
                target.Comment = expectedComment;

                target.Dispose();
                writesCalled = trackingStream.TimesCalled(nameof(trackingStream.Write)) - writesCalled;
                writeBytesCalled = trackingStream.TimesCalled(nameof(trackingStream.WriteByte)) - writeBytesCalled;

                // We expect 2 writes for the end of central directory block - 1 for the EOCD, 1 for the comment.
                Assert.Equal(2, writesCalled + writeBytesCalled);

                trackingStream.Seek(0, SeekOrigin.Begin);

                target = new ZipArchive(trackingStream, ZipArchiveMode.Read, leaveOpen: true);
                Assert.Equal(expectedComment, target.Comment);
            }
        }

        [Theory]
        [InlineData(-1, 40)]
        [InlineData(-1, 49)]
        [InlineData(-1, 0)]
        [InlineData(42, 40)]
        [InlineData(38, 40)]
        public void Update_PerformMinimalWritesWhenEntriesModifiedAndDeleted(int modifyIndex, int deleteIndex)
        {
            byte[] sampleEntryContents = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19];
            byte[] sampleZipFile = CreateZipFile(50, sampleEntryContents);
            byte[] expectedUpdatedEntryContents = [22, 21, 20, 19, 18, 17, 16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0];

            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(sampleZipFile);
                ms.Seek(0, SeekOrigin.Begin);

                using (CallTrackingStream trackingStream = new CallTrackingStream(ms))
                {
                    // Open the archive in Update mode, then rewrite the data of the {modifyIndex}th entry
                    // and delete the LastWriteTime of the {lastWriteTimeChangeIndex}th entry.
                    // Verify the correct number of writes performed as a result, then reopen the same
                    // archive, get the entries, make sure that the right number of entries have been
                    // found and that the entries have the correct contents.
                    int writesCalled = trackingStream.TimesCalled(nameof(trackingStream.Write));
                    int writeBytesCalled = trackingStream.TimesCalled(nameof(trackingStream.WriteByte));
                    ZipArchive target = new ZipArchive(trackingStream, ZipArchiveMode.Update, leaveOpen: true);
                    int totalEntries = target.Entries.Count;
                    // Everything after the first modification or deletion is to be rewritten.
                    int expectedEntriesToWrite = (totalEntries - 1) - (modifyIndex == -1 ? deleteIndex : Math.Min(modifyIndex, deleteIndex));
                    ZipArchiveEntry entryToDelete = target.Entries[deleteIndex];
                    string deletedPath = entryToDelete.FullName;
                    string modifiedPath = null;

                    if (modifyIndex != -1)
                    {
                        ZipArchiveEntry entryToRewrite = target.Entries[modifyIndex];

                        modifiedPath = entryToRewrite.FullName;
                        using (var entryStream = entryToRewrite.Open())
                        {
                            entryStream.SetLength(0);
                            entryStream.Write(expectedUpdatedEntryContents);
                        }
                    }

                    entryToDelete.Delete();

                    target.Dispose();

                    Assert.True(ms.Length < sampleZipFile.Length);

                    writesCalled = trackingStream.TimesCalled(nameof(trackingStream.Write)) - writesCalled;
                    writeBytesCalled = trackingStream.TimesCalled(nameof(trackingStream.WriteByte)) - writeBytesCalled;

                    // 2 writes per archive entry for the local file header.
                    // 2 writes per archive entry for the central directory header.
                    // 2 writes for the file data of the updated entry itself
                    // 1 write per archive entry for the file data of other entries after this in the file
                    // 1 write (sometimes 2, if there's a comment) for the end of central directory block.
                    // All of the central directory headers must be rewritten after an entry's data has been modified.
                    if (modifyIndex == -1)
                    {
                        Assert.Equal(1 + ((2 + 1) * expectedEntriesToWrite) + (2 * (totalEntries - 1)), writesCalled + writeBytesCalled);
                    }
                    else
                    {
                        Assert.Equal(1 + 1 + ((2 + 1) * expectedEntriesToWrite) + (2 * (totalEntries - 1)), writesCalled + writeBytesCalled);
                    }

                    trackingStream.Seek(0, SeekOrigin.Begin);
                    target = new ZipArchive(trackingStream, ZipArchiveMode.Read);

                    // Check 2: no other data has been corrupted as a result
                    for (int i = 0; i < target.Entries.Count; i++)
                    {
                        ZipArchiveEntry entry = target.Entries[i];
                        // The expected index will be off by one if it's after the deleted index, so compensate
                        int expectedIndex = i < deleteIndex ? i : i + 1;
                        byte[] expectedBuffer = entry.FullName == modifiedPath
                            ? expectedUpdatedEntryContents
                            : [.. sampleEntryContents, (byte)(expectedIndex % byte.MaxValue)];
                        byte[] readBuffer = new byte[expectedBuffer.Length];

                        using (Stream readStream = entry.Open())
                        {
                            readStream.Read(readBuffer.AsSpan());
                        }

                        Assert.Equal(expectedBuffer, readBuffer);

                        Assert.NotEqual(deletedPath, entry.FullName);
                    }
                }
            }
        }

        [Theory]
        [InlineData(1)]
        [InlineData(5)]
        [InlineData(10)]
        [InlineData(12)]
        public void Update_PerformMinimalWritesWhenEntriesModifiedAndAdded(int entriesToCreate)
        {
            byte[] sampleEntryContents = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19];
            byte[] sampleZipFile = CreateZipFile(50, sampleEntryContents);

            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(sampleZipFile);
                ms.Seek(0, SeekOrigin.Begin);

                using (CallTrackingStream trackingStream = new CallTrackingStream(ms))
                {
                    // Open the archive in Update mode. Rewrite the data of the first entry and add five entries
                    // to the end of the archive. Verify the correct number of writes performed as a result, then
                    // reopen the same archive, get the entries, make sure that the right number of entries have
                    // been found and that the entries have the correct contents.
                    int writesCalled = trackingStream.TimesCalled(nameof(trackingStream.Write));
                    int writeBytesCalled = trackingStream.TimesCalled(nameof(trackingStream.WriteByte));
                    ZipArchive target = new ZipArchive(trackingStream, ZipArchiveMode.Update, leaveOpen: true);
                    int totalEntries = target.Entries.Count;
                    ZipArchiveEntry entryToRewrite = target.Entries[^1];
                    string modifiedPath = entryToRewrite.FullName;

                    using (Stream entryStream = entryToRewrite.Open())
                    {
                        entryStream.Seek(0, SeekOrigin.Begin);
                        for (int i = 0; i < 100; i++)
                        {
                            entryStream.Write(sampleEntryContents);
                        }
                    }

                    for (int i = 0; i < entriesToCreate; i++)
                    {
                        ZipArchiveEntry createdEntry = target.CreateEntry($"added/{i}.bin");

                        using (Stream entryWriteStream = createdEntry.Open())
                        {
                            entryWriteStream.Write(sampleEntryContents);
                            entryWriteStream.WriteByte((byte)((i + totalEntries) % byte.MaxValue));
                        }
                    }

                    target.Dispose();

                    writesCalled = trackingStream.TimesCalled(nameof(trackingStream.Write)) - writesCalled;
                    writeBytesCalled = trackingStream.TimesCalled(nameof(trackingStream.WriteByte)) - writeBytesCalled;

                    // 2 writes per archive entry for the local file header.
                    // 2 writes per archive entry for the central directory header.
                    // 2 writes for the file data of the updated entry itself
                    // 1 write (sometimes 2, if there's a comment) for the end of central directory block.
                    // All of the central directory headers must be rewritten after an entry's data has been modified.

                    Assert.Equal(1 + ((2 + 2 + 2) * entriesToCreate) + (2 * (totalEntries - 1) + (2 + 2 + 2)), writesCalled + writeBytesCalled);

                    trackingStream.Seek(0, SeekOrigin.Begin);
                    target = new ZipArchive(trackingStream, ZipArchiveMode.Read);

                    // Check 2: no other data has been corrupted as a result
                    for (int i = 0; i < totalEntries + entriesToCreate; i++)
                    {
                        ZipArchiveEntry entry = target.Entries[i];
                        byte[] expectedBuffer = entry.FullName == modifiedPath
                            ? Enumerable.Repeat(sampleEntryContents, 100).SelectMany(x => x).ToArray()
                            : [.. sampleEntryContents, (byte)(i % byte.MaxValue)];
                        byte[] readBuffer = new byte[expectedBuffer.Length];

                        using (Stream readStream = entry.Open())
                        {
                            readStream.Read(readBuffer.AsSpan());
                        }

                        Assert.Equal(expectedBuffer, readBuffer);
                    }
                }
            }
        }
    }
}
