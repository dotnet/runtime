// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
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
        /// thus not triggering a rewrite on Dispose.
        /// </summary>
        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task Update_OpenEntryWithoutWriting_DoesNotTriggerRewrite(bool async)
        {
            // Create a valid zip file
            byte[] sampleEntryContents = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9];
            byte[] sampleZipFile = await CreateZipFile(3, sampleEntryContents, async);
            long originalLength = sampleZipFile.Length;

            // Keep a copy of the original contents to verify no in-place rewrite occurred
            byte[] originalContents = new byte[sampleZipFile.Length];
            Array.Copy(sampleZipFile, originalContents, sampleZipFile.Length);

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
                // and the archive should not try to rewrite the stream
                if (async)
                    await archive.DisposeAsync();
                else
                    archive.Dispose();

                // Verify the stream was not modified - neither length nor contents
                Assert.Equal(originalLength, ms.Length);
                Assert.Equal(originalContents, sampleZipFile);
            }
        }

        private static async Task<byte[]> CreateNonSeekableZip(bool async, byte[] entryData, int entryCount)
        {
            using MemoryStream backing = new MemoryStream();
            using (var nonSeekable = new WrappedStream(backing, canRead: false, canWrite: true, canSeek: false))
            {
                ZipArchive createArchive = await CreateZipArchive(async, nonSeekable, ZipArchiveMode.Create);

                for (int i = 0; i < entryCount; i++)
                {
                    ZipArchiveEntry entry = createArchive.CreateEntry($"entry{i}.bin", CompressionLevel.NoCompression);
                    Stream s = await OpenEntryStream(async, entry);
                    if (async)
                        await s.WriteAsync(entryData);
                    else
                        s.Write(entryData);
                    s.WriteByte((byte)i);
                    await DisposeStream(async, s);
                }

                await DisposeZipArchive(async, createArchive);
            }

            return backing.ToArray();
        }

        private static List<(int LocalHeaderOffset, uint CompressedSize)> ParseZipEntryHeaders(byte[] zipBytes, out int totalEntries)
        {
            const uint EndOfCentralDirectorySignature = 0x06054B50;
            const uint CentralDirectoryFileHeaderSignature = 0x02014B50;

            ReadOnlySpan<byte> span = zipBytes.AsSpan();

            int eocdOffset = -1;
            for (int i = zipBytes.Length - 22; i >= 0; i--)
            {
                if (BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(i)) == EndOfCentralDirectorySignature)
                {
                    eocdOffset = i;
                    break;
                }
            }
            Assert.True(eocdOffset >= 0, "EOCD not found.");

            totalEntries = BinaryPrimitives.ReadUInt16LittleEndian(span.Slice(eocdOffset + 10));
            int centralDirOffset = checked((int)BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(eocdOffset + 16)));

            List<(int LocalHeaderOffset, uint CompressedSize)> entries = new();
            int cdOffset = centralDirOffset;
            for (int i = 0; i < totalEntries; i++)
            {
                Assert.Equal(CentralDirectoryFileHeaderSignature, BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(cdOffset)));
                uint compressedSize = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(cdOffset + 20));
                ushort fileNameLength = BinaryPrimitives.ReadUInt16LittleEndian(span.Slice(cdOffset + 28));
                ushort extraFieldLength = BinaryPrimitives.ReadUInt16LittleEndian(span.Slice(cdOffset + 30));
                ushort fileCommentLength = BinaryPrimitives.ReadUInt16LittleEndian(span.Slice(cdOffset + 32));
                uint localHeaderOffset = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(cdOffset + 42));
                entries.Add((checked((int)localHeaderOffset), compressedSize));
                cdOffset += 46 + fileNameLength + extraFieldLength + fileCommentLength;
            }

            return entries;
        }

        /// <summary>
        /// Creates a zip archive via a non-seekable stream (which forces data descriptor / bit 3),
        /// then reopens in Update mode, adds a new entry, and verifies the archive structure remains valid.
        /// </summary>
        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task Update_DataDescriptorSignature_IsCorrectlyWrittenAndPreserved(bool async)
        {
            const uint LocalFileHeaderSignature = 0x04034b50;
            const uint DataDescriptorSignature = 0x08074b50;
            const ushort DataDescriptorBitFlag = 0x0008;

            byte[] entryData = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15];
            int originalEntryCount = 3;

            byte[] zipBytes = await CreateNonSeekableZip(async, entryData, originalEntryCount);

            // Reopen in Update mode and add a new entry
            byte[] updatedZipBytes;
            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(zipBytes);
                ms.Position = 0;

                ZipArchive updateArchive = await CreateZipArchive(async, ms, ZipArchiveMode.Update, leaveOpen: true);
                Assert.Equal(originalEntryCount, updateArchive.Entries.Count);

                ZipArchiveEntry added = updateArchive.CreateEntry("added.bin");
                Stream addedStream = await OpenEntryStream(async, added);
                if (async)
                    await addedStream.WriteAsync(entryData);
                else
                    addedStream.Write(entryData);
                addedStream.WriteByte(0xFF);
                await DisposeStream(async, addedStream);

                await DisposeZipArchive(async, updateArchive);

                updatedZipBytes = ms.ToArray();
            }

            // Validate the updated archive structurally
            List<(int LocalHeaderOffset, uint CompressedSize)> updatedEntries = ParseZipEntryHeaders(updatedZipBytes, out int updatedTotalEntries);
            ReadOnlySpan<byte> updatedSpan = updatedZipBytes.AsSpan();

            int dataDescriptorCount = 0;
            int entriesWithDataDescriptorBit = 0;
            foreach ((int localHeaderOffset, uint compressedSize) in updatedEntries)
            {
                Assert.Equal(LocalFileHeaderSignature, BinaryPrimitives.ReadUInt32LittleEndian(updatedSpan.Slice(localHeaderOffset)));
                ushort flags = BinaryPrimitives.ReadUInt16LittleEndian(updatedSpan.Slice(localHeaderOffset + 6));

                if ((flags & DataDescriptorBitFlag) != 0)
                {
                    entriesWithDataDescriptorBit++;

                    ushort localFileNameLength = BinaryPrimitives.ReadUInt16LittleEndian(updatedSpan.Slice(localHeaderOffset + 26));
                    ushort localExtraFieldLength = BinaryPrimitives.ReadUInt16LittleEndian(updatedSpan.Slice(localHeaderOffset + 28));
                    int descriptorOffset = localHeaderOffset + 30 + localFileNameLength + localExtraFieldLength + (int)compressedSize;
                    Assert.Equal(DataDescriptorSignature, BinaryPrimitives.ReadUInt32LittleEndian(updatedSpan.Slice(descriptorOffset)));

                    dataDescriptorCount++;
                }
            }

            Assert.Equal(originalEntryCount + 1, updatedTotalEntries);
            Assert.Equal(originalEntryCount, entriesWithDataDescriptorBit);
            Assert.Equal(originalEntryCount, dataDescriptorCount);
        }

        /// <summary>
        /// Creates a zip archive via a non-seekable stream (which forces data descriptor / bit 3),
        /// reopens in Update mode, deletes an entry from the middle, and verifies the remaining
        /// entries are intact. This exercises the EndOfLocalEntryData values computed while reading
        /// the central directory, which are then used to correctly shift subsequent entries after
        /// an entry with a data descriptor is removed.
        /// </summary>
        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task Update_DataDescriptorWithDeletedEntry_PreservesArchive(bool async)
        {
            byte[] entryData = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15];
            int originalEntryCount = 5;

            byte[] zipBytes = await CreateNonSeekableZip(async, entryData, originalEntryCount);

            byte[] updatedZipBytes;
            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(zipBytes);
                ms.Position = 0;

                ZipArchive updateArchive = await CreateZipArchive(async, ms, ZipArchiveMode.Update, leaveOpen: true);
                Assert.Equal(originalEntryCount, updateArchive.Entries.Count);

                ZipArchiveEntry toDelete = updateArchive.GetEntry("entry1.bin");
                Assert.NotNull(toDelete);
                toDelete.Delete();

                await DisposeZipArchive(async, updateArchive);

                updatedZipBytes = ms.ToArray();
            }

            using (MemoryStream ms = new MemoryStream(updatedZipBytes))
            {
                ZipArchive readArchive = await CreateZipArchive(async, ms, ZipArchiveMode.Read, leaveOpen: true);
                Assert.Equal(originalEntryCount - 1, readArchive.Entries.Count);
                Assert.Null(readArchive.GetEntry("entry1.bin"));

                for (int i = 0; i < originalEntryCount; i++)
                {
                    if (i == 1)
                        continue;

                    ZipArchiveEntry entry = readArchive.GetEntry($"entry{i}.bin");
                    Assert.NotNull(entry);
                    byte[] expected = [.. entryData, (byte)i];
                    byte[] actual = new byte[expected.Length];
                    Stream rs = await OpenEntryStream(async, entry);
                    if (async)
                        await rs.ReadExactlyAsync(actual);
                    else
                        rs.ReadExactly(actual);
                    Assert.Equal(expected, actual);
                    await DisposeStream(async, rs);
                }

                await DisposeZipArchive(async, readArchive);
            }
        }

        /// <summary>
        /// Creates a zip archive via a non-seekable stream (which forces data descriptor / bit 3),
        /// reopens in Update mode, modifies only metadata (LastWriteTime) on a middle entry without
        /// reading its data, adds a new entry, and verifies all entries are intact.
        /// This exercises the metadata-only rewrite path which must seek past data descriptors.
        /// </summary>
        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task Update_DataDescriptorWithMetadataOnlyChange_PreservesArchive(bool async)
        {
            byte[] entryData = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15];
            int originalEntryCount = 3;

            byte[] zipBytes = await CreateNonSeekableZip(async, entryData, originalEntryCount);

            // Reopen in Update mode, change metadata on the middle entry, and add a new entry
            byte[] updatedZipBytes;
            DateTimeOffset newTimestamp = new DateTimeOffset(2020, 6, 15, 12, 0, 0, TimeSpan.Zero);
            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(zipBytes);
                ms.Position = 0;

                ZipArchive updateArchive = await CreateZipArchive(async, ms, ZipArchiveMode.Update, leaveOpen: true);
                Assert.Equal(originalEntryCount, updateArchive.Entries.Count);

                // Only change metadata — do NOT open the entry stream (exercises the metadata-only rewrite path)
                ZipArchiveEntry middleEntry = updateArchive.GetEntry("entry1.bin");
                Assert.NotNull(middleEntry);
                middleEntry.LastWriteTime = newTimestamp;

                ZipArchiveEntry added = updateArchive.CreateEntry("added.bin");
                Stream addedStream = await OpenEntryStream(async, added);
                if (async)
                    await addedStream.WriteAsync(entryData);
                else
                    addedStream.Write(entryData);
                addedStream.WriteByte(0xFF);
                await DisposeStream(async, addedStream);

                await DisposeZipArchive(async, updateArchive);

                updatedZipBytes = ms.ToArray();
            }

            // Verify the metadata change was preserved (compare DateTime only — DOS time format
            // does not preserve timezone offset)
            using (MemoryStream ms = new MemoryStream(updatedZipBytes))
            {
                ZipArchive readArchive = await CreateZipArchive(async, ms, ZipArchiveMode.Read, leaveOpen: true);
                ZipArchiveEntry verifyMiddle = readArchive.GetEntry("entry1.bin");
                Assert.NotNull(verifyMiddle);
                Assert.Equal(newTimestamp.DateTime, verifyMiddle.LastWriteTime.DateTime);
                await DisposeZipArchive(async, readArchive);
            }

            // Validate structural integrity — original entries must retain bit 3 and
            // have CRC/sizes zeroed in the local header (values live in the data descriptor).
            const uint LocalFileHeaderSignature = 0x04034b50;
            const uint DataDescriptorSignature = 0x08074b50;
            const ushort DataDescriptorBitFlag = 0x0008;

            List<(int LocalHeaderOffset, uint CompressedSize)> entries = ParseZipEntryHeaders(updatedZipBytes, out int totalEntries);
            Assert.Equal(originalEntryCount + 1, totalEntries);

            ReadOnlySpan<byte> span = updatedZipBytes.AsSpan();
            for (int i = 0; i < entries.Count; i++)
            {
                (int localHeaderOffset, uint compressedSize) = entries[i];
                Assert.Equal(LocalFileHeaderSignature, BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(localHeaderOffset)));
                ushort flags = BinaryPrimitives.ReadUInt16LittleEndian(span.Slice(localHeaderOffset + 6));
                uint localCrc = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(localHeaderOffset + 14));
                uint localCompSize = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(localHeaderOffset + 18));
                uint localUncompSize = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(localHeaderOffset + 22));

                if (i < originalEntryCount)
                {
                    // Original entries must preserve bit 3 and have zeroed CRC/sizes in the local header.
                    Assert.True((flags & DataDescriptorBitFlag) != 0, $"Entry {i}: bit 3 should be preserved.");
                    Assert.True(localCrc == 0, $"Entry {i}: local header CRC should be 0 when bit 3 is set.");
                    Assert.True(localCompSize == 0, $"Entry {i}: local header compressed size should be 0 when bit 3 is set.");
                    Assert.True(localUncompSize == 0, $"Entry {i}: local header uncompressed size should be 0 when bit 3 is set.");

                    // Verify the data descriptor is present with the correct signature.
                    ushort fileNameLength = BinaryPrimitives.ReadUInt16LittleEndian(span.Slice(localHeaderOffset + 26));
                    ushort extraFieldLength = BinaryPrimitives.ReadUInt16LittleEndian(span.Slice(localHeaderOffset + 28));
                    int descriptorOffset = localHeaderOffset + 30 + fileNameLength + extraFieldLength + (int)compressedSize;
                    Assert.Equal(DataDescriptorSignature, BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(descriptorOffset)));
                }
                else
                {
                    // Newly added entry should NOT have bit 3 (written on seekable stream).
                    Assert.True((flags & DataDescriptorBitFlag) == 0, $"Entry {i}: added entry should not have bit 3.");
                }
            }
        }
    }
}
