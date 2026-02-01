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
        public static IEnumerable<object[]> Get_UpdateReadNormal_Data()
        {
            foreach (bool async in _bools)
            {
                yield return new object[] { "normal.zip", "normal", async };
                yield return new object[] { "fake64.zip", "small", async };
                yield return new object[] { "empty.zip", "empty", async };
                yield return new object[] { "appended.zip", "small", async };
                yield return new object[] { "prepended.zip", "small", async };
                yield return new object[] { "emptydir.zip", "emptydir", async };
                yield return new object[] { "small.zip", "small", async };
                yield return new object[] { "unicode.zip", "unicode", async };
            }
        }

        [Theory]
        [MemberData(nameof(Get_UpdateReadNormal_Data))]
        public static async Task UpdateReadNormal(string zipFile, string zipFolder, bool async)
        {
            MemoryStream ms = await StreamHelpers.CreateTempCopyStream(zfile(zipFile));
            await IsZipSameAsDir(ms, zfolder(zipFolder), ZipArchiveMode.Update, requireExplicit: true, checkTimes: true, async);
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task UpdateReadTwice(bool async)
        {
            MemoryStream ms = await StreamHelpers.CreateTempCopyStream(zfile("small.zip"));

            ZipArchive archive = await CreateZipArchive(async, ms, ZipArchiveMode.Update);

            ZipArchiveEntry entry = archive.Entries[0];
            string contents1, contents2;

            Stream es = await OpenEntryStream(async, entry);
            using (StreamReader s = new StreamReader(es))
            {
                contents1 = s.ReadToEnd();
            }

            es = await OpenEntryStream(async, entry);
            using (StreamReader s = new StreamReader(es))
            {
                contents2 = s.ReadToEnd();
            }

            Assert.Equal(contents1, contents2);

            await DisposeZipArchive(async, archive);
        }

        public static IEnumerable<object[]> Get_UpdateCreate_Data()
        {
            foreach (bool async in _bools)
            {
                yield return new object[] { "normal", async };
                yield return new object[] { "empty", async };
                yield return new object[] { "unicode", async };
            }
        }

        [Theory]
        [MemberData(nameof(Get_UpdateCreate_Data))]
        public static async Task UpdateCreate(string zipFolder, bool async)
        {
            var zs = new LocalMemoryStream();
            await CreateFromDir(zfolder(zipFolder), zs, async, ZipArchiveMode.Update);
            await IsZipSameAsDir(zs.Clone(), zfolder(zipFolder), ZipArchiveMode.Read, requireExplicit: true, checkTimes: true, async);
        }

        [Theory]
        [InlineData(ZipArchiveMode.Create, false)]
        [InlineData(ZipArchiveMode.Update, false)]
        [InlineData(ZipArchiveMode.Create, true)]
        [InlineData(ZipArchiveMode.Update, true)]
        public static async Task EmptyEntryTest(ZipArchiveMode mode, bool async)
        {
            string data1 = "test data written to file.";
            string data2 = "more test data written to file.";
            DateTimeOffset lastWrite = new DateTimeOffset(1992, 4, 5, 12, 00, 30, new TimeSpan(-5, 0, 0));

            async Task<byte[]> WriteTestArchive(bool openEntryStream, bool emptyEntryAtTheEnd)
            {
                var archiveStream = new LocalMemoryStream();
                ZipArchive archive = await CreateZipArchive(async, archiveStream, mode);

                await AddEntry(archive, "data1.txt", data1, lastWrite, async);

                ZipArchiveEntry e = archive.CreateEntry("empty.txt");
                e.LastWriteTime = lastWrite;

                if (openEntryStream)
                {
                    Stream s = await OpenEntryStream(async, e);
                    await DisposeStream(async, s);
                }

                if (!emptyEntryAtTheEnd)
                {
                    await AddEntry(archive, "data2.txt", data2, lastWrite, async);
                }

                await DisposeZipArchive(async, archive);

                return archiveStream.ToArray();
            }

            var baseline = await WriteTestArchive(openEntryStream: false, emptyEntryAtTheEnd: false);
            var test = await WriteTestArchive(openEntryStream: true, emptyEntryAtTheEnd: false);
            Assert.Equal(baseline, test);

            baseline = await WriteTestArchive(openEntryStream: false, emptyEntryAtTheEnd: true);
            test = await WriteTestArchive(openEntryStream: true, emptyEntryAtTheEnd: true);
            Assert.Equal(baseline, test);
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public static async Task DeleteAndMoveEntries(bool async)
        {
            //delete and move
            MemoryStream testArchive = await StreamHelpers.CreateTempCopyStream(zfile("normal.zip"));

            ZipArchive archive = await CreateZipArchive(async, testArchive, ZipArchiveMode.Update, leaveOpen: true);

            ZipArchiveEntry toBeDeleted = archive.GetEntry("binary.wmv");
            toBeDeleted.Delete();
            toBeDeleted.Delete(); //delete twice should be okay
            ZipArchiveEntry moved = archive.CreateEntry("notempty/secondnewname.txt");
            ZipArchiveEntry orig = archive.GetEntry("notempty/second.txt");

            if (async)
            {
                await using (Stream origMoved = await orig.OpenAsync(), movedStream = await moved.OpenAsync())
                {
                    await origMoved.CopyToAsync(movedStream);
                }
            }
            else
            {
                using (Stream origMoved = orig.Open(), movedStream = moved.Open())
                {
                    origMoved.CopyTo(movedStream);
                }
            }

            moved.LastWriteTime = orig.LastWriteTime;
            orig.Delete();

            await DisposeZipArchive(async, archive);

            await IsZipSameAsDir(testArchive, zmodified("deleteMove"), ZipArchiveMode.Read, requireExplicit: true, checkTimes: true, async);

        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public static async Task AppendToEntry(bool writeWithSpans, bool async)
        {
            //append
            Stream testArchive = await StreamHelpers.CreateTempCopyStream(zfile("normal.zip"));

            ZipArchive archive = await CreateZipArchive(async, testArchive, ZipArchiveMode.Update, true);

            ZipArchiveEntry e = archive.GetEntry("first.txt");

            Stream s = await OpenEntryStream(async, e);

            s.Seek(0, SeekOrigin.End);

            byte[] data = "\r\n\r\nThe answer my friend, is blowin' in the wind."u8.ToArray();
            if (writeWithSpans)
            {
                if (async)
                {
                    await s.WriteAsync(data);
                }
                else
                {
                    s.Write(new ReadOnlySpan<byte>(data));
                }
            }
            else
            {
                if (async)
                {
                    await s.WriteAsync(data);
                }
                else
                {
                    s.Write(data, 0, data.Length);
                }
            }

            await DisposeStream(async, s);

            var file = FileData.GetFile(zmodified(Path.Combine("append", "first.txt")));
            e.LastWriteTime = file.LastModifiedDate;

            await DisposeZipArchive(async, archive);

            await IsZipSameAsDir(testArchive, zmodified("append"), ZipArchiveMode.Read, requireExplicit: true, checkTimes: true, async);

        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public static async Task OverwriteEntry(bool async)
        {
            //Overwrite file
            Stream testArchive = await StreamHelpers.CreateTempCopyStream(zfile("normal.zip"));

            ZipArchive archive = await CreateZipArchive(async, testArchive, ZipArchiveMode.Update, true);

            string fileName = zmodified(Path.Combine("overwrite", "first.txt"));

            string entryName = "first.txt";
            ZipArchiveEntry e = archive.GetEntry(entryName);
            await UpdateEntry(e, fileName, entryName, async);

            await DisposeZipArchive(async, archive);

            await IsZipSameAsDir(testArchive, zmodified("overwrite"), ZipArchiveMode.Read, requireExplicit: true, checkTimes: true, async);
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public static async Task AddFileToArchive(bool async)
        {
            //add file
            var testArchive = await StreamHelpers.CreateTempCopyStream(zfile("normal.zip"));

            ZipArchive archive = await CreateZipArchive(async, testArchive, ZipArchiveMode.Update, true);

            await CreateAndUpdateEntry(archive, zmodified(Path.Combine("addFile", "added.txt")), "added.txt", async);

            await DisposeZipArchive(async, archive);

            await IsZipSameAsDir(testArchive, zmodified("addFile"), ZipArchiveMode.Read, requireExplicit: true, checkTimes: true, async);
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public static async Task AddFileToArchive_AfterReading(bool async)
        {
            //add file and read entries before
            Stream testArchive = await StreamHelpers.CreateTempCopyStream(zfile("normal.zip"));

            ZipArchive archive = await CreateZipArchive(async, testArchive, ZipArchiveMode.Update, true);

            var x = archive.Entries;

            await CreateAndUpdateEntry(archive, zmodified(Path.Combine("addFile", "added.txt")), "added.txt", async);

            await DisposeZipArchive(async, archive);

            await IsZipSameAsDir(testArchive, zmodified("addFile"), ZipArchiveMode.Read, requireExplicit: true, checkTimes: true, async);
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public static async Task AddFileToArchive_ThenReadEntries(bool async)
        {
            //add file and read entries after
            Stream testArchive = await StreamHelpers.CreateTempCopyStream(zfile("normal.zip"));

            ZipArchive archive = await CreateZipArchive(async, testArchive, ZipArchiveMode.Update, true);

            await CreateAndUpdateEntry(archive, zmodified(Path.Combine("addFile", "added.txt")), "added.txt", async);

            var x = archive.Entries;

            await DisposeZipArchive(async, archive);

            await IsZipSameAsDir(testArchive, zmodified("addFile"), ZipArchiveMode.Read, requireExplicit: true, checkTimes: true, async);
        }

        private static Task CreateAndUpdateEntry(ZipArchive archive, string installFile, string entryName, bool async)
        {
            ZipArchiveEntry e = archive.CreateEntry(entryName);
            return UpdateEntry(e, installFile, entryName, async);
        }

        private static async Task UpdateEntry(ZipArchiveEntry e, string installFile, string entryName, bool async)
        {
            FileData file = FileData.GetFile(installFile);
            e.LastWriteTime = file.LastModifiedDate;
            Assert.Equal(e.LastWriteTime, file.LastModifiedDate);

            using (var stream = await StreamHelpers.CreateTempCopyStream(installFile))
            {
                if (async)
                {
                    await using Stream es = await e.OpenAsync();
                    es.SetLength(0);
                    await stream.CopyToAsync(es);
                }
                else
                {
                    using Stream es = e.Open();
                    es.SetLength(0);
                    stream.CopyTo(es);
                }
            }
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public static async Task UpdateModeInvalidOperations(bool async)
        {
            using LocalMemoryStream ms = await LocalMemoryStream.ReadAppFileAsync(zfile("normal.zip"));

            ZipArchive target = await CreateZipArchive(async, ms, ZipArchiveMode.Update, true);

            ZipArchiveEntry edeleted = target.GetEntry("first.txt");

            // Record original values before opening
            long originalLength = edeleted.Length;
            long originalCompressedLength = edeleted.CompressedLength;

            Stream s = await OpenEntryStream(async, edeleted);

            //invalid ops while entry open
            await Assert.ThrowsAsync<IOException>(() => OpenEntryStream(async, edeleted));

            // Length and CompressedLength should still be accessible while stream is open but no writes occurred
            Assert.Equal(originalLength, edeleted.Length);
            Assert.Equal(originalCompressedLength, edeleted.CompressedLength);

            Assert.Throws<IOException>(() => edeleted.Delete());

            await DisposeStream(async, s);

            //invalid ops on stream after entry closed
            Assert.Throws<ObjectDisposedException>(() => s.ReadByte());

            // Length and CompressedLength should still be accessible after stream closed without writes
            Assert.Equal(originalLength, edeleted.Length);
            Assert.Equal(originalCompressedLength, edeleted.CompressedLength);

            edeleted.Delete();

            //invalid ops while entry deleted
            await Assert.ThrowsAsync<InvalidOperationException>(() => OpenEntryStream(async, edeleted));

            Assert.Throws<InvalidOperationException>(() => { edeleted.LastWriteTime = new DateTimeOffset(); });

            ZipArchiveEntry e = target.GetEntry("notempty/second.txt");

            await DisposeZipArchive(async, target);

            Assert.Throws<ObjectDisposedException>(() => { var x = target.Entries; });
            Assert.Throws<ObjectDisposedException>(() => target.CreateEntry("dirka"));

            await Assert.ThrowsAsync<ObjectDisposedException>(() => OpenEntryStream(async, e));

            Assert.Throws<ObjectDisposedException>(() => e.Delete());
            Assert.Throws<ObjectDisposedException>(() => { e.LastWriteTime = new DateTimeOffset(); });
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public static async Task UpdateModeInvalidOperations_AfterWrite(bool async)
        {
            using LocalMemoryStream ms = await LocalMemoryStream.ReadAppFileAsync(zfile("normal.zip"));

            ZipArchive target = await CreateZipArchive(async, ms, ZipArchiveMode.Update, true);

            ZipArchiveEntry entry = target.GetEntry("first.txt");

            Stream s = await OpenEntryStream(async, entry);

            // Write to the stream - this should mark the entry as modified
            s.WriteByte(42);

            // After writing, Length and CompressedLength should throw
            Assert.Throws<InvalidOperationException>(() => { var x = entry.Length; });
            Assert.Throws<InvalidOperationException>(() => { var x = entry.CompressedLength; });

            await DisposeStream(async, s);

            // After stream is closed with writes, Length and CompressedLength should still throw
            Assert.Throws<InvalidOperationException>(() => { var x = entry.Length; });
            Assert.Throws<InvalidOperationException>(() => { var x = entry.CompressedLength; });

            await DisposeZipArchive(async, target);
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task UpdateUncompressedArchive(bool async)
        {
            var utf8WithoutBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
            byte[] fileContent;
            using (var memStream = new MemoryStream())
            {
                ZipArchive zip = await CreateZipArchive(async, memStream, ZipArchiveMode.Create);

                ZipArchiveEntry entry = zip.CreateEntry("testing", CompressionLevel.NoCompression);
                using (var writer = new StreamWriter(entry.Open(), utf8WithoutBom))
                {
                    writer.Write("hello");
                    writer.Flush();
                }

                await DisposeZipArchive(async, zip);

                fileContent = memStream.ToArray();
            }
            byte compressionMethod = fileContent[8];
            Assert.Equal(0, compressionMethod); // stored => 0, deflate => 8
            using (var memStream = new MemoryStream())
            {
                memStream.Write(fileContent);
                memStream.Position = 0;
                ZipArchive archive = await CreateZipArchive(async, memStream, ZipArchiveMode.Update);

                ZipArchiveEntry entry = archive.GetEntry("testing");
                using (var writer = new StreamWriter(entry.Open(), utf8WithoutBom))
                {
                    writer.Write("new");
                    writer.Flush();
                }

                await DisposeZipArchive(async, archive);

                byte[] modifiedTestContent = memStream.ToArray();
                compressionMethod = modifiedTestContent[8];
                Assert.Equal(0, compressionMethod); // stored => 0, deflate => 8
            }
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task Update_VerifyDuplicateEntriesAreAllowed(bool async)
        {
            using var ms = new MemoryStream();
            ZipArchive archive = await CreateZipArchive(async, ms, ZipArchiveMode.Update);

            string entryName = "foo";
            await AddEntry(archive, entryName, contents: "xxx", DateTimeOffset.Now, async);
            await AddEntry(archive, entryName, contents: "yyy", DateTimeOffset.Now, async);

            Assert.Equal(2, archive.Entries.Count);
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task Update_PerformMinimalWritesWhenNoFilesChanged(bool async)
        {
            using (LocalMemoryStream ms = await LocalMemoryStream.ReadAppFileAsync(zfile("normal.zip")))
            using (CallTrackingStream trackingStream = new CallTrackingStream(ms))
            {
                int writesCalled = trackingStream.TimesCalled(nameof(trackingStream.Write));
                int writeBytesCalled = trackingStream.TimesCalled(nameof(trackingStream.WriteByte));

                ZipArchive target = await CreateZipArchive(async, trackingStream, ZipArchiveMode.Update, leaveOpen: true);
                int archiveEntries = target.Entries.Count;

                target.Dispose();
                writesCalled = trackingStream.TimesCalled(nameof(trackingStream.Write)) - writesCalled;
                writeBytesCalled = trackingStream.TimesCalled(nameof(trackingStream.WriteByte)) - writeBytesCalled;

                // No changes to the archive should result in no writes to the file.
                Assert.Equal(0, writesCalled + writeBytesCalled);

                await DisposeZipArchive(async, target);
            }
        }

        public static IEnumerable<object[]> Get_Update_PerformMinimalWritesWhenFixedLengthEntryHeaderFieldChanged_Data()
        {
            yield return [49, 1, 1,];
            yield return [40, 3, 2,];
            yield return [30, 5, 3,];
            yield return [0, 8, 1,];
        }

        [Theory]
        [MemberData(nameof(Get_Update_PerformMinimalWritesWhenFixedLengthEntryHeaderFieldChanged_Data))]
        public async Task Update_PerformMinimalWritesWhenFixedLengthEntryHeaderFieldChanged(int startIndex, int entriesToModify, int step)
        {
            byte[] sampleEntryContents = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19];
            byte[] sampleZipFile = await CreateZipFile(50, sampleEntryContents, async: false);

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
        [MemberData(nameof(Get_Update_PerformMinimalWritesWhenFixedLengthEntryHeaderFieldChanged_Data))]
        public async Task Update_PerformMinimalWritesWhenFixedLengthEntryHeaderFieldChanged_Async(int startIndex, int entriesToModify, int step)
        {
            byte[] sampleEntryContents = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19];
            byte[] sampleZipFile = await CreateZipFile(50, sampleEntryContents, async: false);

            await using (MemoryStream ms = new MemoryStream())
            {
                await ms.WriteAsync(sampleZipFile);
                ms.Seek(0, SeekOrigin.Begin);

                await using (CallTrackingStream trackingStream = new CallTrackingStream(ms))
                {
                    // Open the first archive in Update mode, then change the value of {entriesToModify} fixed-length entry headers
                    // (LastWriteTime.) Verify the correct number of writes performed as a result, then reopen the same
                    // archive, get the entries and make sure that the fields hold the expected value.
                    int writesCalled = trackingStream.TimesCalled(nameof(trackingStream.WriteAsync));
                    int writeBytesCalled = trackingStream.TimesCalled(nameof(trackingStream.WriteByte));
                    ZipArchive target = await ZipArchive.CreateAsync(trackingStream, ZipArchiveMode.Update, leaveOpen: true, entryNameEncoding: null);
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

                    await target.DisposeAsync();

                    writesCalled = trackingStream.TimesCalled(nameof(trackingStream.WriteAsync)) - writesCalled;
                    writeBytesCalled = trackingStream.TimesCalled(nameof(trackingStream.WriteByte)) - writeBytesCalled;
                    // As above, check 1: the number of writes performed should be minimal.
                    // 2 writes per archive entry for the local file header.
                    // 2 writes per archive entry for the central directory header.
                    // 1 write (sometimes 2, if there's a comment) for the end of central directory block.
                    // The EOCD block won't change as a result of our modifications, so is excluded from the counts.
                    Assert.Equal(((2 + 2) * entriesToModify), writesCalled + writeBytesCalled);

                    trackingStream.Seek(0, SeekOrigin.Begin);
                    target = await ZipArchive.CreateAsync(trackingStream, ZipArchiveMode.Read, leaveOpen: false, entryNameEncoding: null);

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

                        await using (Stream readStream = await entry.OpenAsync())
                        {
                            await readStream.ReadAsync(readBuffer.AsMemory());
                        }

                        Assert.Equal(expectedBuffer, readBuffer);
                    }

                    await target.DisposeAsync();
                }
            }
        }

        public static IEnumerable<object[]> Get_Update_PerformMinimalWritesWhenEntryDataChanges_Data()
        {
            yield return new object[] { 0, };
            yield return new object[] { 10, };
            yield return new object[] { 20, };
            yield return new object[] { 30, };
            yield return new object[] { 49, };
        }

        [Theory]
        [MemberData(nameof(Get_Update_PerformMinimalWritesWhenEntryDataChanges_Data))]
        public Task Update_PerformMinimalWritesWhenEntryDataChanges(int index) => Update_PerformMinimalWritesWithDataAndHeaderChanges(index, -1);

        [Theory]
        [MemberData(nameof(Get_Update_PerformMinimalWritesWhenEntryDataChanges_Data))]
        public Task Update_PerformMinimalWritesWhenEntryDataChanges_Async(int index) => Update_PerformMinimalWritesWithDataAndHeaderChanges_Async(index, -1);

        public static IEnumerable<object[]> Get_PerformMinimalWritesWithDataAndHeaderChanges_Data()
        {
            yield return [0, 0];
            yield return [20, 40];
            yield return [30, 10];
        }

        [Theory]
        [MemberData(nameof(Get_PerformMinimalWritesWithDataAndHeaderChanges_Data))]
        public async Task Update_PerformMinimalWritesWithDataAndHeaderChanges(int dataChangeIndex, int lastWriteTimeChangeIndex)
        {
            byte[] sampleEntryContents = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19];
            byte[] sampleZipFile = await CreateZipFile(50, sampleEntryContents, async: false);
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

        [Theory]
        [MemberData(nameof(Get_PerformMinimalWritesWithDataAndHeaderChanges_Data))]
        public async Task Update_PerformMinimalWritesWithDataAndHeaderChanges_Async(int dataChangeIndex, int lastWriteTimeChangeIndex)
        {
            byte[] sampleEntryContents = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19];
            byte[] sampleZipFile = await CreateZipFile(50, sampleEntryContents, async: true);
            byte[] expectedUpdatedEntryContents = [19, 18, 17, 16, 15, 14, 13, 12, 11, 10];

            await using (MemoryStream ms = new MemoryStream())
            {
                await ms.WriteAsync(sampleZipFile);
                ms.Seek(0, SeekOrigin.Begin);

                await using (CallTrackingStream trackingStream = new CallTrackingStream(ms))
                {
                    // Open the archive in Update mode, then rewrite the data of the {dataChangeIndex}th entry
                    // and set the LastWriteTime of the {lastWriteTimeChangeIndex}th entry.
                    // Verify the correct number of writes performed as a result, then reopen the same
                    // archive, get the entries and make sure that the fields hold the expected value.
                    int writesCalled = trackingStream.TimesCalled(nameof(trackingStream.WriteAsync));
                    int writeBytesCalled = trackingStream.TimesCalled(nameof(trackingStream.WriteByte));
                    ZipArchive target = await ZipArchive.CreateAsync(trackingStream, ZipArchiveMode.Update, leaveOpen: true, entryNameEncoding: null);
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

                    await using (var entryStream = await entryToRewrite.OpenAsync())
                    {
                        entryStream.SetLength(0);
                        await entryStream.WriteAsync(expectedUpdatedEntryContents);
                    }

                    await target.DisposeAsync();

                    writesCalled = trackingStream.TimesCalled(nameof(trackingStream.WriteAsync)) - writesCalled;
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
                    target = await ZipArchive.CreateAsync(trackingStream, ZipArchiveMode.Read, leaveOpen: false, entryNameEncoding: null);

                    // Check 2: no other data has been corrupted as a result
                    for (int i = 0; i < target.Entries.Count; i++)
                    {
                        ZipArchiveEntry entry = target.Entries[i];
                        byte[] expectedBuffer = i == dataChangeIndex
                            ? expectedUpdatedEntryContents
                            : [.. sampleEntryContents, (byte)(i % byte.MaxValue)];
                        byte[] readBuffer = new byte[expectedBuffer.Length];

                        await using (Stream readStream = await entry.OpenAsync())
                        {
                            await readStream.ReadAsync(readBuffer);
                        }

                        Assert.Equal(expectedBuffer, readBuffer);

                        if (i == lastWriteTimeChangeIndex)
                        {
                            Assert.Equal(expectedWriteTime, entry.LastWriteTime);
                        }
                    }

                    await target.DisposeAsync();
                }
            }
        }

        [Fact]
        public async Task Update_PerformMinimalWritesWhenArchiveCommentChanged()
        {
            using (LocalMemoryStream ms = await LocalMemoryStream.ReadAppFileAsync(zfile("normal.zip")))
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
                target.Dispose();
            }
        }

        [Fact]
        public async Task Update_PerformMinimalWritesWhenArchiveCommentChanged_Async()
        {
            await using (LocalMemoryStream ms = await LocalMemoryStream.ReadAppFileAsync(zfile("normal.zip")))
            await using (CallTrackingStream trackingStream = new CallTrackingStream(ms))
            {
                int writesCalled = trackingStream.TimesCalled(nameof(trackingStream.WriteAsync));
                int writeBytesCalled = trackingStream.TimesCalled(nameof(trackingStream.WriteByte));
                string expectedComment = "14 byte comment";

                ZipArchive target = await ZipArchive.CreateAsync(trackingStream, ZipArchiveMode.Update, leaveOpen: true, entryNameEncoding: null);
                target.Comment = expectedComment;
                await target.DisposeAsync();

                writesCalled = trackingStream.TimesCalled(nameof(trackingStream.WriteAsync)) - writesCalled;
                writeBytesCalled = trackingStream.TimesCalled(nameof(trackingStream.WriteByte)) - writeBytesCalled;

                // We expect 2 writes for the end of central directory block - 1 for the EOCD, 1 for the comment.
                Assert.Equal(2, writesCalled + writeBytesCalled);

                trackingStream.Seek(0, SeekOrigin.Begin);

                target = await ZipArchive.CreateAsync(trackingStream, ZipArchiveMode.Read, leaveOpen: true, entryNameEncoding: null);
                Assert.Equal(expectedComment, target.Comment);
                await target.DisposeAsync();
            }
        }

        public static IEnumerable<object[]> Get_Update_PerformMinimalWritesWhenEntriesModifiedAndDeleted_Data()
        {
            yield return [-1, 40];
            yield return [-1, 49];
            yield return [-1, 0];
            yield return [42, 40];
            yield return [38, 40];
        }

        [Theory]
        [MemberData(nameof(Get_Update_PerformMinimalWritesWhenEntriesModifiedAndDeleted_Data))]
        public async Task Update_PerformMinimalWritesWhenEntriesModifiedAndDeleted(int modifyIndex, int deleteIndex)
        {
            byte[] sampleEntryContents = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19];
            byte[] sampleZipFile = await CreateZipFile(50, sampleEntryContents, async: false);
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
        [MemberData(nameof(Get_Update_PerformMinimalWritesWhenEntriesModifiedAndDeleted_Data))]
        public async Task Update_PerformMinimalWritesWhenEntriesModifiedAndDeleted_Async(int modifyIndex, int deleteIndex)
        {
            byte[] sampleEntryContents = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19];
            byte[] sampleZipFile = await CreateZipFile(50, sampleEntryContents, async: false);
            byte[] expectedUpdatedEntryContents = [22, 21, 20, 19, 18, 17, 16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0];

            await using (MemoryStream ms = new MemoryStream())
            {
                await ms.WriteAsync(sampleZipFile);
                ms.Seek(0, SeekOrigin.Begin);

                await using (CallTrackingStream trackingStream = new CallTrackingStream(ms))
                {
                    // Open the archive in Update mode, then rewrite the data of the {modifyIndex}th entry
                    // and delete the LastWriteTime of the {lastWriteTimeChangeIndex}th entry.
                    // Verify the correct number of writes performed as a result, then reopen the same
                    // archive, get the entries, make sure that the right number of entries have been
                    // found and that the entries have the correct contents.
                    int writesCalled = trackingStream.TimesCalled(nameof(trackingStream.WriteAsync));
                    int writeBytesCalled = trackingStream.TimesCalled(nameof(trackingStream.WriteByte));
                    ZipArchive target = await ZipArchive.CreateAsync(trackingStream, ZipArchiveMode.Update, leaveOpen: true, entryNameEncoding: null);
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
                        await using (var entryStream = await entryToRewrite.OpenAsync())
                        {
                            entryStream.SetLength(0);
                            await entryStream.WriteAsync(expectedUpdatedEntryContents);
                        }
                    }

                    entryToDelete.Delete();

                    await target.DisposeAsync();

                    Assert.True(ms.Length < sampleZipFile.Length);

                    writesCalled = trackingStream.TimesCalled(nameof(trackingStream.WriteAsync)) - writesCalled;
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
                    target = await ZipArchive.CreateAsync(trackingStream, ZipArchiveMode.Read, leaveOpen: false, entryNameEncoding: null);

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

                        await using (Stream readStream = await entry.OpenAsync())
                        {
                            await readStream.ReadAsync(readBuffer.AsMemory());
                        }

                        Assert.Equal(expectedBuffer, readBuffer);

                        Assert.NotEqual(deletedPath, entry.FullName);
                    }
                }
            }
        }

        public static IEnumerable<object[]> Get_Update_PerformMinimalWritesWhenEntriesModifiedAndAdded_Data()
        {
            yield return [1];
            yield return [5];
            yield return [10];
            yield return [12];
        }

        [Theory]
        [MemberData(nameof(Get_Update_PerformMinimalWritesWhenEntriesModifiedAndAdded_Data))]
        public async Task Update_PerformMinimalWritesWhenEntriesModifiedAndAdded(int entriesToCreate)
        {
            byte[] sampleEntryContents = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19];
            byte[] sampleZipFile = await CreateZipFile(50, sampleEntryContents, async: false);

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


        [Theory]
        [MemberData(nameof(Get_Update_PerformMinimalWritesWhenEntriesModifiedAndAdded_Data))]
        public async Task Update_PerformMinimalWritesWhenEntriesModifiedAndAdded_Async(int entriesToCreate)
        {
            byte[] sampleEntryContents = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19];
            byte[] sampleZipFile = await CreateZipFile(50, sampleEntryContents, async: false);

            await using (MemoryStream ms = new MemoryStream())
            {
                await ms.WriteAsync(sampleZipFile);
                ms.Seek(0, SeekOrigin.Begin);

                await using (CallTrackingStream trackingStream = new CallTrackingStream(ms))
                {
                    // Open the archive in Update mode. Rewrite the data of the first entry and add five entries
                    // to the end of the archive. Verify the correct number of writes performed as a result, then
                    // reopen the same archive, get the entries, make sure that the right number of entries have
                    // been found and that the entries have the correct contents.
                    int writesCalled = trackingStream.TimesCalled(nameof(trackingStream.WriteAsync));
                    int writeBytesCalled = trackingStream.TimesCalled(nameof(trackingStream.WriteByte));
                    ZipArchive target = await ZipArchive.CreateAsync(trackingStream, ZipArchiveMode.Update, leaveOpen: true, entryNameEncoding: null);
                    int totalEntries = target.Entries.Count;
                    ZipArchiveEntry entryToRewrite = target.Entries[^1];
                    string modifiedPath = entryToRewrite.FullName;

                    await using (Stream entryStream = await entryToRewrite.OpenAsync())
                    {
                        entryStream.Seek(0, SeekOrigin.Begin);
                        for (int i = 0; i < 100; i++)
                        {
                            await entryStream.WriteAsync(sampleEntryContents);
                        }
                    }

                    for (int i = 0; i < entriesToCreate; i++)
                    {
                        ZipArchiveEntry createdEntry = target.CreateEntry($"added/{i}.bin");

                        using (Stream entryWriteStream = createdEntry.Open())
                        {
                            await entryWriteStream.WriteAsync(sampleEntryContents);
                            entryWriteStream.WriteByte((byte)((i + totalEntries) % byte.MaxValue));
                        }
                    }

                    await target.DisposeAsync();

                    writesCalled = trackingStream.TimesCalled(nameof(trackingStream.WriteAsync)) - writesCalled;
                    writeBytesCalled = trackingStream.TimesCalled(nameof(trackingStream.WriteByte)) - writeBytesCalled;

                    // 2 writes per archive entry for the local file header.
                    // 2 writes per archive entry for the central directory header.
                    // 2 writes for the file data of the updated entry itself
                    // 1 write (sometimes 2, if there's a comment) for the end of central directory block.
                    // All of the central directory headers must be rewritten after an entry's data has been modified.

                    Assert.Equal(1 + ((2 + 2 + 2) * entriesToCreate) + (2 * (totalEntries - 1) + (2 + 2 + 2)), writesCalled + writeBytesCalled);

                    trackingStream.Seek(0, SeekOrigin.Begin);
                    target = await ZipArchive.CreateAsync(trackingStream, ZipArchiveMode.Read, leaveOpen: false, entryNameEncoding: null);

                    // Check 2: no other data has been corrupted as a result
                    for (int i = 0; i < totalEntries + entriesToCreate; i++)
                    {
                        ZipArchiveEntry entry = target.Entries[i];
                        byte[] expectedBuffer = entry.FullName == modifiedPath
                            ? Enumerable.Repeat(sampleEntryContents, 100).SelectMany(x => x).ToArray()
                            : [.. sampleEntryContents, (byte)(i % byte.MaxValue)];
                        byte[] readBuffer = new byte[expectedBuffer.Length];

                        await using (Stream readStream = await entry.OpenAsync())
                        {
                            await readStream.ReadAsync(readBuffer.AsMemory());
                        }

                        Assert.Equal(expectedBuffer, readBuffer);
                    }
                }
            }
        }

        /// <summary>
        /// Tests that opening an entry stream and disposing it without writing does not mark the archive as modified,
        /// thus not triggering a rewrite on Dispose. This is the scenario from the bug where using a non-expandable
        /// MemoryStream with ZipArchiveMode.Update would throw NotSupportedException on Dispose if any entry was opened.
        /// </summary>
        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task Update_OpenEntryWithoutWriting_DoesNotTriggerRewrite(bool async)
        {
            // Create a valid zip file
            byte[] sampleEntryContents = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9];
            byte[] sampleZipFile = await CreateZipFile(3, sampleEntryContents, async);
            long originalLength = sampleZipFile.Length;

            // Use a non-expandable MemoryStream (fixed buffer)
            // This would throw NotSupportedException if Dispose tries to write/grow the stream
            using (MemoryStream ms = new MemoryStream(sampleZipFile, writable: true))
            {
                ZipArchive archive = async
                    ? await ZipArchive.CreateAsync(ms, ZipArchiveMode.Update, leaveOpen: true, entryNameEncoding: null)
                    : new ZipArchive(ms, ZipArchiveMode.Update, leaveOpen: true);

                // Open an entry and read it without writing
                ZipArchiveEntry entry = archive.Entries[0];
                Stream entryStream = async ? await entry.OpenAsync() : entry.Open();
                byte[] buffer = new byte[sampleEntryContents.Length + 1]; // +1 for the index byte added by CreateZipFile
                int bytesRead = async
                    ? await entryStream.ReadAsync(buffer)
                    : entryStream.Read(buffer, 0, buffer.Length);

                Assert.InRange(bytesRead, 1, buffer.Length);

                // Close the entry stream without writing anything
                if (async)
                    await entryStream.DisposeAsync();
                else
                    entryStream.Dispose();

                // Dispose should not throw NotSupportedException because no writes occurred
                // and the archive should not try to grow the stream
                if (async)
                    await archive.DisposeAsync();
                else
                    archive.Dispose();

                // Verify the stream was not modified
                Assert.Equal(originalLength, ms.Length);
            }
        }

        /// <summary>
        /// Regression test for the exact scenario from the bug report https://github.com/dotnet/runtime/issues/123419
        /// Opening a Package-like zip archive (with [Content_Types].xml) on a non-expandable MemoryStream, disposing
        /// without modifications, should not throw NotSupportedException. This mimics Package.Open behavior where simply opening
        /// and closing a package without changes should not attempt to rewrite the archive.
        /// </summary>
        [Fact]
        public void Update_PackageLikeArchive_DisposeWithoutChanges_DoesNotThrow()
        {
            // This is the exact byte array from the bug report
            byte[] compressed =
            [
                80,75,3,4,20,0,6,0,8,0,0,0,33,0,42,221,170,64,210,0,0,0,55,1,0,0,19,0,8,2,91,67,111,110,116,101,110,116,95,84,121,112,101,115,93,46,120,109,108,32,162,4,2,40,160,0,2,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,108,143,189,78,196,48,16,132,123,36,222,193,218,254,178,129,2,33,148,228,10,126,74,184,226,120,128,149,179,201,89,216,107,203,94,80,238,237,113,46,84,64,185,63,51,223,76,183,95,130,55,159,156,139,139,210,195,77,211,130,97,177,113,116,50,247,240,126,124,217,221,131,41,74,50,146,143,194,61,156,185,192,126,184,190,234,142,231,196,197,84,181,148,30,78,170,233,1,177,216,19,7,42,77,76,44,245,50,197,28,72,235,152,103,76,100,63,104,102,188,109,219,59,180,81,148,69,119,186,122,192,208,61,241,68,159,94,205,243,82,215,91,146,0,230,113,251,90,65,61,40,47,138,201,147,19,192,127,5,149,247,75,66,41,121,103,73,107,51,92,175,85,247,86,155,102,55,178,57,80,214,87,10,213,24,43,102,114,51,30,182,128,205,95,159,11,250,199,0,47,181,135,111,0,0,0,255,255,3,0,80,75,3,4,20,0,2,0,8,0,0,0,33,0,10,177,174,11,172,0,0,0,247,0,0,0,18,0,0,0,67,111,110,102,105,103,47,80,97,99,107,97,103,101,46,120,109,108,132,143,189,14,130,48,28,196,119,19,223,129,116,167,31,160,11,41,101,112,149,196,132,104,92,27,104,164,17,254,53,180,88,222,205,193,71,242,21,132,40,234,230,120,119,191,228,238,30,183,59,207,134,182,9,174,170,179,218,64,138,24,166,40,176,78,66,37,27,3,42,69,96,80,38,150,11,190,147,229,89,158,84,48,210,96,147,193,86,41,170,157,187,36,132,120,239,177,143,177,233,78,36,162,148,145,99,190,45,202,90,181,18,125,96,253,31,14,53,76,181,165,66,130,31,94,107,68,132,25,91,227,104,21,99,202,201,108,242,92,195,23,136,198,193,83,250,99,242,77,223,184,190,83,66,65,184,47,56,153,37,39,239,15,226,9,0,0,255,255,3,0,80,75,3,4,20,0,2,0,8,0,0,0,33,0,98,124,210,234,150,0,0,0,177,2,0,0,19,0,0,0,70,111,114,109,117,108,97,115,47,83,101,99,116,105,111,110,49,46,109,42,78,77,46,201,204,207,83,8,134,208,134,214,92,92,197,25,137,69,169,41,10,129,165,169,69,149,62,249,137,41,169,41,33,249,33,137,73,57,169,10,182,10,57,169,37,188,92,10,64,16,156,95,90,148,12,18,81,82,226,229,202,204,67,22,196,103,132,75,98,73,162,111,126,74,106,14,101,102,5,100,150,229,151,80,193,77,8,115,208,28,134,110,30,208,56,194,166,57,231,231,229,65,130,209,63,47,167,146,50,151,161,154,69,13,215,129,253,234,12,148,40,161,66,152,129,205,161,196,85,225,153,37,25,249,165,37,8,111,18,235,40,0,0,0,0,255,255,3,0,80,75,1,2,45,0,20,0,6,0,8,0,0,0,33,0,42,221,170,64,210,0,0,0,55,1,0,0,19,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,91,67,111,110,116,101,110,116,95,84,121,112,101,115,93,46,120,109,108,80,75,1,2,45,0,20,0,2,0,8,0,0,0,33,0,10,177,174,11,172,0,0,0,247,0,0,0,18,0,0,0,0,0,0,0,0,0,0,0,0,0,11,3,0,0,67,111,110,102,105,103,47,80,97,99,107,97,103,101,46,120,109,108,80,75,1,2,45,0,20,0,2,0,8,0,0,0,33,0,98,124,210,234,150,0,0,0,177,2,0,0,19,0,0,0,0,0,0,0,0,0,0,0,0,0,231,3,0,0,70,111,114,109,117,108,97,115,47,83,101,99,116,105,111,110,49,46,109,80,75,5,6,0,0,0,0,3,0,3,0,194,0,0,0,174,4,0,0,0,0
            ];

            // Use a non-expandable MemoryStream (fixed buffer) - this is what Package.Open does internally
            // This would throw NotSupportedException if Dispose tries to write/grow the stream
            using MemoryStream stream = new MemoryStream(compressed);

            // Open in Update mode (like Package.Open with FileAccess.ReadWrite)
            using ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Update, leaveOpen: true);

            // Access entries (simulating what Package does when reading [Content_Types].xml)
            Assert.Equal(3, archive.Entries.Count);
            ZipArchiveEntry contentTypesEntry = archive.GetEntry("[Content_Types].xml");
            Assert.NotNull(contentTypesEntry);

            // Open and read the entry without writing (like Package reading [Content_Types].xml)
            using (Stream entryStream = contentTypesEntry.Open())
            {
                using StreamReader reader = new StreamReader(entryStream);
                string content = reader.ReadToEnd();
                Assert.Contains("ContentType", content);
            }

            // Archive will be disposed here - should NOT throw NotSupportedException
            // because no actual modifications were made

        }
    }
}
