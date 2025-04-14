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
    public class zip_InvalidParametersAndStrangeFiles : ZipFileTestBase
    {
        private static readonly int s_bufferSize = 10240;
        private static readonly string s_tamperedFileName = "binary.wmv";
        private static readonly byte[] s_existingSampleData = [0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09];
        private static readonly byte[] s_sampleDataToWrite = [0x00, 0x01, 0x02, 0x03];
        private static void ConstructorThrows<TException>(Func<ZipArchive> constructor, string Message) where TException : Exception
        {
            try
            {
                Assert.Throws<TException>(() =>
                {
                    using (ZipArchive archive = constructor()) { }
                });
            }
            catch (Exception e)
            {
                Console.WriteLine(string.Format("{0}: {1}", Message, e.ToString()));
                throw;
            }
        }

        [Fact]
        public static async Task InvalidInstanceMethods()
        {
            Stream zipFile = await StreamHelpers.CreateTempCopyStream(zfile("normal.zip"));
            using (ZipArchive archive = new ZipArchive(zipFile, ZipArchiveMode.Update))
            {
                //non-existent entry
                Assert.True(null == archive.GetEntry("nonExistentEntry")); //"Should return null on non-existent entry name"
                //null/empty string
                Assert.Throws<ArgumentNullException>(() => archive.GetEntry(null)); //"Should throw on null entry name"

                ZipArchiveEntry entry = archive.GetEntry("first.txt");

                //null/empty string
                AssertExtensions.Throws<ArgumentException>("entryName", () => archive.CreateEntry("")); //"Should throw on empty entry name"
                Assert.Throws<ArgumentNullException>(() => archive.CreateEntry(null)); //"should throw on null entry name"
            }
        }

        [Fact]
        public static void InvalidConstructors()
        {
            //out of range enum values
            ConstructorThrows<ArgumentOutOfRangeException>(() =>
                new ZipArchive(new MemoryStream(), (ZipArchiveMode)(-1)), "Out of range enum");
            ConstructorThrows<ArgumentOutOfRangeException>(() =>
                new ZipArchive(new MemoryStream(), (ZipArchiveMode)(4)), "out of range enum");
            ConstructorThrows<ArgumentOutOfRangeException>(() =>
                new ZipArchive(new MemoryStream(), (ZipArchiveMode)(10)), "Out of range enum");

            //null/closed stream
            ConstructorThrows<ArgumentNullException>(() =>
                new ZipArchive((Stream)null, ZipArchiveMode.Read), "Null/closed stream");
            ConstructorThrows<ArgumentNullException>(() =>
                new ZipArchive((Stream)null, ZipArchiveMode.Create), "Null/closed stream");
            ConstructorThrows<ArgumentNullException>(() =>
                new ZipArchive((Stream)null, ZipArchiveMode.Update), "Null/closed stream");

            MemoryStream ms = new MemoryStream();
            ms.Dispose();

            ConstructorThrows<ArgumentException>(() =>
                new ZipArchive(ms, ZipArchiveMode.Read), "Disposed Base Stream");
            ConstructorThrows<ArgumentException>(() =>
                new ZipArchive(ms, ZipArchiveMode.Create), "Disposed Base Stream");
            ConstructorThrows<ArgumentException>(() =>
                new ZipArchive(ms, ZipArchiveMode.Update), "Disposed Base Stream");

            //non-seekable to update
            using (LocalMemoryStream nonReadable = new LocalMemoryStream(),
                nonWriteable = new LocalMemoryStream(),
                nonSeekable = new LocalMemoryStream())
            {
                nonReadable.SetCanRead(false);
                nonWriteable.SetCanWrite(false);
                nonSeekable.SetCanSeek(false);

                ConstructorThrows<ArgumentException>(() => new ZipArchive(nonReadable, ZipArchiveMode.Read), "Non readable stream");

                ConstructorThrows<ArgumentException>(() => new ZipArchive(nonWriteable, ZipArchiveMode.Create), "Non-writable stream");

                ConstructorThrows<ArgumentException>(() => new ZipArchive(nonReadable, ZipArchiveMode.Update), "Non-readable stream");
                ConstructorThrows<ArgumentException>(() => new ZipArchive(nonWriteable, ZipArchiveMode.Update), "Non-writable stream");
                ConstructorThrows<ArgumentException>(() => new ZipArchive(nonSeekable, ZipArchiveMode.Update), "Non-seekable stream");
            }
        }

        [Theory]
        [InlineData("LZMA.zip")]
        [InlineData("invalidDeflate.zip")]
        public static async Task ZipArchiveEntry_InvalidUpdate(string zipname)
        {
            string filename = bad(zipname);
            Stream updatedCopy = await StreamHelpers.CreateTempCopyStream(filename);
            string name;
            long length, compressedLength;
            DateTimeOffset lastWriteTime;
            using (ZipArchive archive = new ZipArchive(updatedCopy, ZipArchiveMode.Update, true))
            {
                ZipArchiveEntry e = archive.Entries[0];
                name = e.FullName;
                lastWriteTime = e.LastWriteTime;
                length = e.Length;
                compressedLength = e.CompressedLength;
                Assert.Throws<InvalidDataException>(() => e.Open()); //"Should throw on open"
            }

            //make sure that update mode preserves that unreadable file
            using (ZipArchive archive = new ZipArchive(updatedCopy, ZipArchiveMode.Update))
            {
                ZipArchiveEntry e = archive.Entries[0];
                Assert.Equal(name, e.FullName); //"Name isn't the same"
                Assert.Equal(lastWriteTime, e.LastWriteTime); //"LastWriteTime not the same"
                Assert.Equal(length, e.Length); //"Length isn't the same"
                Assert.Equal(compressedLength, e.CompressedLength); //"CompressedLength isn't the same"
                Assert.Throws<InvalidDataException>(() => e.Open()); //"Should throw on open"
            }
        }

        [Fact]
        public static async Task LargeArchive_DataDescriptor_Read_NonZip64_FileLengthGreaterThanIntMax()
        {
            MemoryStream stream = await LocalMemoryStream.readAppFileAsync(strange("fileLengthGreaterIntLessUInt.zip"));

            using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read))
            {
                ZipArchiveEntry e = archive.GetEntry("large.bin");

                Assert.Equal(3_600_000_000, e.Length);
                Assert.Equal(3_499_028, e.CompressedLength);

                using (Stream source = e.Open())
                {
                    byte[] buffer = new byte[s_bufferSize];
                    int read = source.Read(buffer, 0, buffer.Length);   // We don't want to inflate this large archive entirely 
                                                                        // just making sure it read successfully 
                    Assert.Equal(s_bufferSize, read);
                    foreach (byte b in buffer)
                    {
                        if (b != '0')
                        {
                            Assert.Fail($"The file should be all '0's, but found '{(char)b}'");
                        }
                    }
                }
            }
        }

        [Fact]
        public static async Task ZipArchiveEntry_CorruptedStream_ReadMode_CopyTo_UpToUncompressedSize()
        {
            MemoryStream stream = await LocalMemoryStream.readAppFileAsync(zfile("normal.zip"));

            int nameOffset = PatchDataRelativeToFileName(Encoding.ASCII.GetBytes(s_tamperedFileName), stream, 8);  // patch uncompressed size in file header
            PatchDataRelativeToFileName(Encoding.ASCII.GetBytes(s_tamperedFileName), stream, 22, nameOffset + s_tamperedFileName.Length); // patch in central directory too

            using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read))
            {
                ZipArchiveEntry e = archive.GetEntry(s_tamperedFileName);
                using (MemoryStream ms = new MemoryStream())
                using (Stream source = e.Open())
                {
                    source.CopyTo(ms);
                    Assert.Equal(e.Length, ms.Length);     // Only allow to decompress up to uncompressed size
                    byte[] buffer = new byte[s_bufferSize];
                    Assert.Equal(0, source.Read(buffer, 0, buffer.Length)); // shouldn't be able read more
                    ms.Seek(0, SeekOrigin.Begin);
                    int read;
                    while ((read = ms.Read(buffer, 0, buffer.Length)) != 0)
                    { // No need to do anything, just making sure all bytes readable
                    }
                    Assert.Equal(ms.Position, ms.Length); // all bytes must be read
                }
            }
        }

        [Fact]
        public static async Task ZipArchiveEntry_CorruptedStream_ReadMode_Read_UpToUncompressedSize()
        {
            MemoryStream stream = await LocalMemoryStream.readAppFileAsync(zfile("normal.zip"));

            int nameOffset = PatchDataRelativeToFileName(Encoding.ASCII.GetBytes(s_tamperedFileName), stream, 8);  // patch uncompressed size in file header
            PatchDataRelativeToFileName(Encoding.ASCII.GetBytes(s_tamperedFileName), stream, 22, nameOffset + s_tamperedFileName.Length); // patch in central directory too

            using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read))
            {
                ZipArchiveEntry e = archive.GetEntry(s_tamperedFileName);
                using (MemoryStream ms = new MemoryStream())
                using (Stream source = e.Open())
                {
                    byte[] buffer = new byte[s_bufferSize];
                    int read;
                    while ((read = source.Read(buffer, 0, buffer.Length)) != 0)
                    {
                        ms.Write(buffer, 0, read);
                    }
                    Assert.Equal(e.Length, ms.Length);     // Only allow to decompress up to uncompressed size
                    Assert.Equal(0, source.Read(buffer, 0, s_bufferSize)); // shouldn't be able read more
                    ms.Seek(0, SeekOrigin.Begin);
                    while ((read = ms.Read(buffer, 0, buffer.Length)) != 0)
                    { // No need to do anything, just making sure all bytes readable from output stream
                    }
                    Assert.Equal(ms.Position, ms.Length); // all bytes must be read
                }
            }
        }

        [Fact]
        public static void ZipArchiveEntry_CorruptedStream_EnsureNoExtraBytesReadOrOverWritten()
        {
            MemoryStream stream = populateStream().Result;

            int nameOffset = PatchDataRelativeToFileName(Encoding.ASCII.GetBytes(s_tamperedFileName), stream, 8);  // patch uncompressed size in file header
            PatchDataRelativeToFileName(Encoding.ASCII.GetBytes(s_tamperedFileName), stream, 22, nameOffset + s_tamperedFileName.Length); // patch in central directory too

            using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read))
            {
                ZipArchiveEntry e = archive.GetEntry(s_tamperedFileName);
                using (Stream source = e.Open())
                {
                    byte[] buffer = new byte[e.Length + 20];
                    Array.Fill<byte>(buffer, 0xDE);
                    int read;
                    int offset = 0;
                    int length = buffer.Length;

                    while ((read = source.Read(buffer, offset, length)) != 0)
                    {
                        offset += read;
                        length -= read;
                    }
                    for (int i = offset; i < buffer.Length; i++)
                    {
                        Assert.Equal(0xDE, buffer[i]);
                    }
                }
            }
        }

        private static async Task<MemoryStream> populateStream()
        {
            return await LocalMemoryStream.readAppFileAsync(zfile("normal.zip"));
        }

        [Fact]
        public static async Task Zip64ArchiveEntry_CorruptedStream_CopyTo_UpToUncompressedSize()
        {
            MemoryStream stream = await LocalMemoryStream.readAppFileAsync(compat("deflate64.zip"));

            int nameOffset = PatchDataRelativeToFileName(Encoding.ASCII.GetBytes(s_tamperedFileName), stream, 8);  // patch uncompressed size in file header
            PatchDataRelativeToFileName(Encoding.ASCII.GetBytes(s_tamperedFileName), stream, 22, nameOffset + s_tamperedFileName.Length); // patch in central directory too

            using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read))
            {
                ZipArchiveEntry e = archive.GetEntry(s_tamperedFileName);
                using (var ms = new MemoryStream())
                using (Stream source = e.Open())
                {
                    source.CopyTo(ms);
                    Assert.Equal(e.Length, ms.Length);     // Only allow to decompress up to uncompressed size
                    ms.Seek(0, SeekOrigin.Begin);
                    int read;
                    byte[] buffer = new byte[s_bufferSize];
                    while ((read = ms.Read(buffer, 0, buffer.Length)) != 0)
                    { // No need to do anything, just making sure all bytes readable
                    }
                    Assert.Equal(ms.Position, ms.Length); // all bytes must be read
                }
            }
        }

        [Fact]
        public static async Task ZipArchiveEntry_CorruptedStream_UnCompressedSizeBiggerThanExpected_NothingShouldBreak()
        {
            MemoryStream stream = await LocalMemoryStream.readAppFileAsync(zfile("normal.zip"));

            int nameOffset = PatchDataRelativeToFileNameFillBytes(Encoding.ASCII.GetBytes(s_tamperedFileName), stream, 8);  // patch uncompressed size in file header
            PatchDataRelativeToFileNameFillBytes(Encoding.ASCII.GetBytes(s_tamperedFileName), stream, 22, nameOffset + s_tamperedFileName.Length); // patch in central directory too

            using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read))
            {
                ZipArchiveEntry e = archive.GetEntry(s_tamperedFileName);
                using (MemoryStream ms = new MemoryStream())
                using (Stream source = e.Open())
                {
                    source.CopyTo(ms);
                    Assert.True(e.Length > ms.Length);           // Even uncompressed size is bigger than decompressed size there should be no error
                    Assert.True(e.CompressedLength < ms.Length);
                }
            }
        }

        [Fact]
        public static async Task Zip64ArchiveEntry_CorruptedFile_Read_UpToUncompressedSize()
        {
            MemoryStream stream = await LocalMemoryStream.readAppFileAsync(compat("deflate64.zip"));

            int nameOffset = PatchDataRelativeToFileName(Encoding.ASCII.GetBytes(s_tamperedFileName), stream, 8);  // patch uncompressed size in file header
            PatchDataRelativeToFileName(Encoding.ASCII.GetBytes(s_tamperedFileName), stream, 22, nameOffset + s_tamperedFileName.Length); // patch in central directory too

            using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read))
            {
                ZipArchiveEntry e = archive.GetEntry(s_tamperedFileName);
                using (var ms = new MemoryStream())
                using (Stream source = e.Open())
                {
                    byte[] buffer = new byte[s_bufferSize];
                    int read;
                    while ((read = source.Read(buffer, 0, buffer.Length)) != 0)
                    {
                        ms.Write(buffer, 0, read);
                    }
                    Assert.Equal(e.Length, ms.Length);     // Only allow to decompress up to uncompressed size
                    Assert.Equal(0, source.Read(buffer, 0, buffer.Length)); // Shouldn't be readable more
                }
            }
        }


        [Fact]
        public static async Task UnseekableVeryLargeArchive_DataDescriptor_Read_Zip64()
        {
            MemoryStream stream = await LocalMemoryStream.readAppFileAsync(strange("veryLarge.zip"));

            using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read))
            {
                ZipArchiveEntry e = archive.GetEntry("bigFile.bin");

                Assert.Equal(6_442_450_944, e.Length);
                Assert.Equal(6_261_752, e.CompressedLength);

                using (Stream source = e.Open())
                {
                    byte[] buffer = new byte[s_bufferSize];
                    int read = source.Read(buffer, 0, buffer.Length);   // We don't want to inflate this large archive entirely
                                                                        // just making sure it read successfully
                    Assert.Equal(s_bufferSize, read);
                }
            }
        }

        [Fact]
        public static async Task UpdateZipArchive_AppendTo_CorruptedFileEntry()
        {
            MemoryStream stream = await StreamHelpers.CreateTempCopyStream(zfile("normal.zip"));
            int updatedUncompressedLength = 1310976;
            string append = "\r\n\r\nThe answer my friend, is blowin' in the wind.";
            byte[] data = Encoding.ASCII.GetBytes(append);
            long oldCompressedSize = 0;

            int nameOffset = PatchDataRelativeToFileName(Encoding.ASCII.GetBytes(s_tamperedFileName), stream, 8);  // patch uncompressed size in file header
            PatchDataRelativeToFileName(Encoding.ASCII.GetBytes(s_tamperedFileName), stream, 22, nameOffset + s_tamperedFileName.Length); // patch in central directory too

            using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Update, true))
            {
                ZipArchiveEntry e = archive.GetEntry(s_tamperedFileName);
                oldCompressedSize = e.CompressedLength;
                using (Stream s = e.Open())
                {
                    Assert.Equal(updatedUncompressedLength, s.Length);
                    s.Seek(0, SeekOrigin.End);
                    s.Write(data, 0, data.Length);
                    Assert.Equal(updatedUncompressedLength + data.Length, s.Length);
                }
            }

            using (ZipArchive modifiedArchive = new ZipArchive(stream, ZipArchiveMode.Read))
            {
                ZipArchiveEntry e = modifiedArchive.GetEntry(s_tamperedFileName);
                using (Stream s = e.Open())
                using (var ms = new MemoryStream())
                {
                    await s.CopyToAsync(ms, s_bufferSize);
                    Assert.Equal(updatedUncompressedLength + data.Length, ms.Length);
                    ms.Seek(updatedUncompressedLength, SeekOrigin.Begin);
                    byte[] read = new byte[data.Length];
                    ms.Read(read, 0, data.Length);
                    Assert.Equal(append, Encoding.ASCII.GetString(read));
                }
                Assert.True(oldCompressedSize > e.CompressedLength); // old compressed size must be reduced by Uncomressed size limit
            }
        }

        [Fact]
        public static async Task UpdateZipArchive_OverwriteCorruptedEntry()
        {
            MemoryStream stream = await StreamHelpers.CreateTempCopyStream(zfile("normal.zip"));
            int updatedUncompressedLength = 1310976;
            string overwrite = "\r\n\r\nThe answer my friend, is blowin' in the wind.";
            byte[] data = Encoding.ASCII.GetBytes(overwrite);

            int nameOffset = PatchDataRelativeToFileName(Encoding.ASCII.GetBytes(s_tamperedFileName), stream, 8);  // patch uncompressed size in file header
            PatchDataRelativeToFileName(Encoding.ASCII.GetBytes(s_tamperedFileName), stream, 22, nameOffset + s_tamperedFileName.Length); // patch in central directory too

            using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Update, true))
            {
                ZipArchiveEntry e = archive.GetEntry(s_tamperedFileName);
                string fileName = zmodified(Path.Combine("overwrite", "first.txt"));
                var file = FileData.GetFile(fileName);

                using (var s = new MemoryStream(data))
                using (Stream es = e.Open())
                {
                    Assert.Equal(updatedUncompressedLength, es.Length);
                    es.SetLength(0);
                    await s.CopyToAsync(es, s_bufferSize);
                    Assert.Equal(data.Length, es.Length);
                }
            }

            using (ZipArchive modifiedArchive = new ZipArchive(stream, ZipArchiveMode.Read))
            {
                ZipArchiveEntry e = modifiedArchive.GetEntry(s_tamperedFileName);
                using (Stream s = e.Open())
                using (var ms = new MemoryStream())
                {
                    await s.CopyToAsync(ms, s_bufferSize);
                    Assert.Equal(data.Length, ms.Length);
                    Assert.Equal(overwrite, Encoding.ASCII.GetString(ms.GetBuffer(), 0, data.Length));
                }
            }
        }

        [Fact]
        public static async Task UpdateZipArchive_AddFileTo_ZipWithCorruptedFile()
        {
            string addingFile = "added.txt";
            MemoryStream stream = await StreamHelpers.CreateTempCopyStream(zfile("normal.zip"));
            MemoryStream file = await StreamHelpers.CreateTempCopyStream(zmodified(Path.Combine("addFile", addingFile)));

            int nameOffset = PatchDataRelativeToFileName(Encoding.ASCII.GetBytes(s_tamperedFileName), stream, 8);  // patch uncompressed size in file header
            PatchDataRelativeToFileName(Encoding.ASCII.GetBytes(s_tamperedFileName), stream, 22, nameOffset + s_tamperedFileName.Length); // patch in central directory too

            using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Update, true))
            {
                ZipArchiveEntry e = archive.CreateEntry(addingFile);
                using (Stream es = e.Open())
                {
                    file.CopyTo(es);
                }
            }

            using (ZipArchive modifiedArchive = new ZipArchive(stream, ZipArchiveMode.Read))
            {
                ZipArchiveEntry e = modifiedArchive.GetEntry(s_tamperedFileName);
                using (Stream s = e.Open())
                using (var ms = new MemoryStream())
                {
                    await s.CopyToAsync(ms, s_bufferSize);
                    Assert.Equal(e.Length, ms.Length);  // tampered file should read up to uncompressed size
                }

                ZipArchiveEntry addedEntry = modifiedArchive.GetEntry(addingFile);
                Assert.NotNull(addedEntry);
                Assert.Equal(addedEntry.Length, file.Length);

                using (Stream s = addedEntry.Open())
                { // Make sure file content added correctly
                    int read = 0;
                    byte[] buffer1 = new byte[1024];
                    byte[] buffer2 = new byte[1024];
                    file.Seek(0, SeekOrigin.Begin);

                    while ((read = s.Read(buffer1, 0, buffer1.Length)) != 0 )
                    {
                        file.Read(buffer2, 0, buffer2.Length);
                        Assert.Equal(buffer1, buffer2);
                    }
                }
            }
        }

        private static int PatchDataRelativeToFileName(byte[] fileNameInBytes, MemoryStream packageStream, int distance, int start = 0)
        {
            var buffer = packageStream.GetBuffer();
            var startOfName = FindSequenceIndex(fileNameInBytes, buffer, start);
            var startOfUpdatingData = startOfName - distance;

            // updating 4 byte data
            buffer[startOfUpdatingData] = 0;
            buffer[startOfUpdatingData + 1] = 1;
            buffer[startOfUpdatingData + 2] = 20;
            buffer[startOfUpdatingData + 3] = 0;

            return startOfName;
        }

        private static int PatchDataRelativeToFileNameFillBytes(byte[] fileNameInBytes, MemoryStream packageStream, int distance, int start = 0)
        {
            var buffer = packageStream.GetBuffer();
            var startOfName = FindSequenceIndex(fileNameInBytes, buffer, start);
            var startOfUpdatingData = startOfName - distance;

            // updating 4 byte data
            buffer[startOfUpdatingData] = 255;
            buffer[startOfUpdatingData + 1] = 255;
            buffer[startOfUpdatingData + 2] = 255;
            buffer[startOfUpdatingData + 3] = 0;

            return startOfName;
        }

        private static int FindSequenceIndex(byte[] searchItem, byte[] whereToSearch, int startIndex = 0)
        {
            for (int start = startIndex; start < whereToSearch.Length - searchItem.Length; ++start)
            {
                int searchIndex = 0;
                while (searchIndex < searchItem.Length && searchItem[searchIndex] == whereToSearch[start + searchIndex])
                {
                    ++searchIndex;
                }
                if (searchIndex == searchItem.Length)
                {
                    return start;
                }
            }
            return -1;
        }

        [Theory]
        [InlineData("CDoffsetOutOfBounds.zip")]
        [InlineData("EOCDmissing.zip")]
        public static async Task ZipArchive_InvalidStream(string zipname)
        {
            string filename = bad(zipname);
            using (var stream = await StreamHelpers.CreateTempCopyStream(filename))
                Assert.Throws<InvalidDataException>(() => new ZipArchive(stream, ZipArchiveMode.Read));
        }

        [Theory]
        [InlineData("CDoffsetInBoundsWrong.zip")]
        [InlineData("numberOfEntriesDifferent.zip")]
        public static async Task ZipArchive_InvalidEntryTable(string zipname)
        {
            string filename = bad(zipname);
            using (ZipArchive archive = new ZipArchive(await StreamHelpers.CreateTempCopyStream(filename), ZipArchiveMode.Read))
                Assert.Throws<InvalidDataException>(() => archive.Entries[0]);
        }

        [Theory]
        [InlineData("compressedSizeOutOfBounds.zip", true)]
        [InlineData("localFileHeaderSignatureWrong.zip", true)]
        [InlineData("localFileOffsetOutOfBounds.zip", true)]
        [InlineData("LZMA.zip", true)]
        [InlineData("invalidDeflate.zip", false)]
        public static async Task ZipArchive_InvalidEntry(string zipname, bool throwsOnOpen)
        {
            string filename = bad(zipname);
            using (ZipArchive archive = new ZipArchive(await StreamHelpers.CreateTempCopyStream(filename), ZipArchiveMode.Read))
            {
                ZipArchiveEntry e = archive.Entries[0];
                if (throwsOnOpen)
                {
                    Assert.Throws<InvalidDataException>(() => e.Open()); //"should throw on open"
                }
                else
                {
                    using (Stream s = e.Open())
                    {
                        Assert.Throws<InvalidDataException>(() => s.ReadByte()); //"Unreadable stream"
                    }
                }
            }
        }

        [Fact]
        public static async Task ZipArchiveEntry_InvalidLastWriteTime_Read()
        {
            using (ZipArchive archive = new ZipArchive(await StreamHelpers.CreateTempCopyStream(
                 bad("invaliddate.zip")), ZipArchiveMode.Read))
            {
                Assert.Equal(new DateTime(1980, 1, 1, 0, 0, 0), archive.Entries[0].LastWriteTime.DateTime); //"Date isn't correct on invalid date"
            }
        }

        [Fact]
        public static void ZipArchiveEntry_InvalidLastWriteTime_Write()
        {
            using (ZipArchive archive = new ZipArchive(new MemoryStream(), ZipArchiveMode.Create))
            {
                ZipArchiveEntry entry = archive.CreateEntry("test");
                Assert.Throws<ArgumentOutOfRangeException>(() =>
                {
                    //"should throw on bad date"
                    entry.LastWriteTime = new DateTimeOffset(1979, 12, 3, 5, 6, 2, new TimeSpan());
                });
                Assert.Throws<ArgumentOutOfRangeException>(() =>
                {
                    //"Should throw on bad date"
                    entry.LastWriteTime = new DateTimeOffset(2980, 12, 3, 5, 6, 2, new TimeSpan());
                });
            }
        }

        [Theory]
        [InlineData("extradata/extraDataLHandCDentryAndArchiveComments.zip", "verysmall", true)]
        [InlineData("extradata/extraDataThenZip64.zip", "verysmall", true)]
        [InlineData("extradata/zip64ThenExtraData.zip", "verysmall", true)]
        [InlineData("dataDescriptor.zip", "normalWithoutBinary", false)]
        [InlineData("filenameTimeAndSizesDifferentInLH.zip", "verysmall", false)]
        public static async Task StrangeFiles(string zipFile, string zipFolder, bool requireExplicit)
        {
            IsZipSameAsDir(await StreamHelpers.CreateTempCopyStream(strange(zipFile)), zfolder(zipFolder), ZipArchiveMode.Update, requireExplicit, checkTimes: true);
        }

        /// <summary>
        /// This test tiptoes the buffer boundaries to ensure that the size of a read buffer doesn't
        /// cause any bytes to be left in ZLib's buffer.
        /// </summary>
        [Fact]
        public static void ZipWithLargeSparseFile()
        {
            string zipname = strange("largetrailingwhitespacedeflation.zip");
            string entryname = "A/B/C/D";
            using (FileStream stream = File.Open(zipname, FileMode.Open, FileAccess.Read))
            using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read))
            {
                ZipArchiveEntry entry = archive.GetEntry(entryname);
                long size = entry.Length;

                for (int bufferSize = 1; bufferSize <= size; bufferSize++)
                {
                    using (Stream entryStream = entry.Open())
                    {
                        byte[] b = new byte[bufferSize];
                        int read = 0, count = 0;
                        while ((read = entryStream.Read(b, 0, bufferSize)) > 0)
                        {
                            count += read;
                        }
                        Assert.Equal(size, count);
                    }
                }
            }
        }

        private static readonly byte[] s_emptyFileCompressedWithEtx =
        {
            0x50, 0x4B, 0x03, 0x04, 0x14, 0x00, 0x06, 0x00, 0x08, 0x00, 0x00, 0x00, 0x21, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x16, 0x00, 0x00, 0x00, 0x78, 0x6C,
            0x2F, 0x63, 0x75, 0x73, 0x74, 0x6F, 0x6D, 0x50, 0x72, 0x6F, 0x70, 0x65, 0x72, 0x74, 0x79, 0x32,
            0x2E, 0x62, 0x69, 0x6E, 0x03, 0x00, 0x50, 0x4B, 0x01, 0x02, 0x14, 0x00, 0x14, 0x00, 0x06, 0x00,
            0x08, 0x00, 0x00, 0x00, 0x21, 0x00, 0x00, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x16, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x78, 0x6C, 0x2F, 0x63, 0x75, 0x73, 0x74, 0x6F, 0x6D, 0x50, 0x72, 0x6F,
            0x70, 0x65, 0x72, 0x74, 0x79, 0x32, 0x2E, 0x62, 0x69, 0x6E, 0x50, 0x4B, 0x05, 0x06, 0x00, 0x00,
            0x00, 0x00, 0x01, 0x00, 0x01, 0x00, 0x44, 0x00, 0x00, 0x00, 0x36, 0x00, 0x00, 0x00, 0x00, 0x00
        };
        private static readonly byte[] s_emptyFileCompressedWrongSize =
        {
            0x50, 0x4B, 0x03, 0x04, 0x14, 0x00, 0x06, 0x00, 0x08, 0x00, 0x00, 0x00, 0x21, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x16, 0x00, 0x00, 0x00, 0x78, 0x6C,
            0x2F, 0x63, 0x75, 0x73, 0x74, 0x6F, 0x6D, 0x50, 0x72, 0x6F, 0x70, 0x65, 0x72, 0x74, 0x79, 0x32,
            0x2E, 0x62, 0x69, 0x6E, 0xBA, 0xAD, 0x03, 0x00, 0x50, 0x4B, 0x01, 0x02, 0x14, 0x00, 0x14, 0x00,
            0x06, 0x00, 0x08, 0x00, 0x00, 0x00, 0x21, 0x00, 0x00, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x16, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x78, 0x6C, 0x2F, 0x63, 0x75, 0x73, 0x74, 0x6F, 0x6D, 0x50,
            0x72, 0x6F, 0x70, 0x65, 0x72, 0x74, 0x79, 0x32, 0x2E, 0x62, 0x69, 0x6E, 0x50, 0x4B, 0x05, 0x06,
            0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00, 0x44, 0x00, 0x00, 0x00, 0x38, 0x00, 0x00, 0x00,
            0x00, 0x00
        };
        public static IEnumerable<object[]> EmptyFiles = new List<object[]>()
        {
            new object[] { s_emptyFileCompressedWithEtx },
            new object[] { s_emptyFileCompressedWrongSize }
        };

        /// <summary>
        /// This test checks behavior of ZipArchive with unexpected zip files:
        /// 1. EmptyFileCompressedWithEOT has
        /// Deflate 0x08, _uncompressedSize 0, _compressedSize 2, compressed data: 0x0300 (\u0003 ETX)
        /// 2. EmptyFileCompressedWrongSize has
        /// Deflate 0x08, _uncompressedSize 0, _compressedSize 4, compressed data: 0xBAAD0300 (just bad data)
        /// ZipArchive is not expected to make any changes to the compression method of an archive entry unless
        /// it's been changed. If it has been changed, ZipArchive is expected to change compression method to
        /// Stored (0x00) and ignore "bad" compressed size
        /// </summary>
        [Theory]
        [MemberData(nameof(EmptyFiles))]
        public void ReadArchive_WithEmptyDeflatedFile(byte[] fileBytes)
        {
            using (var testStream = new MemoryStream(fileBytes))
            {
                const string ExpectedFileName = "xl/customProperty2.bin";

                byte firstEntryCompressionMethod = fileBytes[8];

                // first attempt: open archive with zero-length file that is compressed (Deflate = 0x8)
                using (var zip = new ZipArchive(testStream, ZipArchiveMode.Update, leaveOpen: true))
                {
                    // dispose without making any changes will make no changes to the input stream
                }

                byte[] fileContent = testStream.ToArray();

                // compression method should not have changed
                Assert.Equal(firstEntryCompressionMethod, fileBytes[8]);

                testStream.Seek(0, SeekOrigin.Begin);
                // second attempt: open archive with zero-length file that is compressed (Deflate = 0x8)
                using (var zip = new ZipArchive(testStream, ZipArchiveMode.Update, leaveOpen: true))
                using (var zipEntryStream = zip.Entries[0].Open())
                {
                    // dispose after opening an entry will rewrite the archive
                }

                fileContent = testStream.ToArray();

                // compression method should change to "uncompressed" (Stored = 0x0)
                Assert.Equal(0, fileContent[8]);

                // extract and check the file. should stay empty.
                using (var zip = new ZipArchive(testStream, ZipArchiveMode.Update))
                {
                    ZipArchiveEntry entry = zip.GetEntry(ExpectedFileName);
                    Assert.Equal(0, entry.Length);
                    Assert.Equal(0, entry.CompressedLength);
                    using (Stream entryStream = entry.Open())
                    {
                        Assert.Equal(0, entryStream.Length);
                    }
                }
            }
        }

        /// <summary>
        /// Opens an empty file that has a 64KB EOCD comment.
        /// Adds two 64KB text entries. Verifies they can be read correctly.
        /// Appends 64KB of garbage at the end of the file. Verifies we throw.
        /// Prepends 64KB of garbage at the beginning of the file. Verifies we throw.
        /// </summary>
        [Fact]
        public static void ReadArchive_WithEOCDComment_TrailingPrecedingGarbage()
        {
            void InsertEntry(ZipArchive archive, string name, string contents)
            {
                ZipArchiveEntry entry = archive.CreateEntry(name);
                using (StreamWriter writer = new StreamWriter(entry.Open()))
                {
                    writer.WriteLine(contents);
                }
            }

            int GetEntryContentsLength(ZipArchiveEntry entry)
            {
                int length = 0;
                using (Stream stream = entry.Open())
                {
                    using (var reader = new StreamReader(stream))
                    {
                        length = reader.ReadToEnd().Length;
                    }
                }
                return length;
            }

            void VerifyValidEntry(ZipArchiveEntry entry, string expectedName, int expectedMinLength)
            {
                Assert.NotNull(entry);
                Assert.Equal(expectedName, entry.Name);
                // The file has a few more bytes, but should be at least as large as its contents
                Assert.True(GetEntryContentsLength(entry) >= expectedMinLength);
            }

            string name0 = "huge0.txt";
            string name1 = "huge1.txt";
            string str64KB = new string('x', ushort.MaxValue);
            byte[] byte64KB = Text.Encoding.ASCII.GetBytes(str64KB);

            // Open empty file with 64KB EOCD comment
            string path = strange("extradata/emptyWith64KBComment.zip");
            using (MemoryStream archiveStream = StreamHelpers.CreateTempCopyStream(path).Result)
            {
                // Insert 2 64KB txt entries
                using (ZipArchive archive = new ZipArchive(archiveStream, ZipArchiveMode.Update, leaveOpen: true))
                {
                    InsertEntry(archive, name0, str64KB);
                    InsertEntry(archive, name1, str64KB);
                }

                // Open and verify items
                archiveStream.Seek(0, SeekOrigin.Begin);
                using (ZipArchive archive = new ZipArchive(archiveStream, ZipArchiveMode.Read, leaveOpen: true))
                {
                    Assert.Equal(2, archive.Entries.Count);
                    VerifyValidEntry(archive.Entries[0], name0, ushort.MaxValue);
                    VerifyValidEntry(archive.Entries[1], name1, ushort.MaxValue);
                }

                // Append 64KB of garbage
                archiveStream.Seek(0, SeekOrigin.End);
                archiveStream.Write(byte64KB, 0, byte64KB.Length);

                // Open should not be possible because we can't find the EOCD in the max search length from the end
                Assert.Throws<InvalidDataException>(() =>
                {
                    ZipArchive archive = new ZipArchive(archiveStream, ZipArchiveMode.Read, leaveOpen: true);
                });

                // Create stream with 64KB of prepended garbage, then the above stream appended
                // Attempting to create a ZipArchive should fail: no EOCD found
                using (MemoryStream prependStream = new MemoryStream())
                {
                    prependStream.Write(byte64KB, 0, byte64KB.Length);
                    archiveStream.WriteTo(prependStream);

                    Assert.Throws<InvalidDataException>(() =>
                    {
                        ZipArchive archive = new ZipArchive(prependStream, ZipArchiveMode.Read);
                    });
                }
            }
        }

        /// <summary>
        /// This test verifies that we can successfully read Zip archives that are "slightly incorrect" in that there is a Zip64 extra field
        /// that contains space for 64-bit values which should only be present if the 32-bit fields read earlier were all 0xFF bytes.
        /// Although this contravenes the Zip spec, such files are created by common tools and are successfully read by Python, Go and Rust, and
        /// 7zip (albeit with a warning)
        /// </summary>
        [Fact]
        public void ReadArchive_WithUnexpectedZip64ExtraFieldSize()
        {
            using ZipArchive archive = new (new MemoryStream(s_slightlyIncorrectZip64));
            ZipArchiveEntry entry = archive.GetEntry("file.txt");
            Assert.Equal(4, entry.Length);
            Assert.Equal(6, entry.CompressedLength);
            using var stream = entry.Open();
            using StreamReader reader = new (stream);
            string text = reader.ReadToEnd();
            Assert.Equal("test", text);
        }

        /// <summary>
        /// As above, but the compressed size in the central directory record is less than 0xFFFFFFFF so the value in that location
        /// should be used instead of in the Zip64 extra field.
        /// </summary>
        [Fact]
        public void ReadArchive_WithUnexpectedZip64ExtraFieldSizeCompressedSizeIn32Bit()
        {
            byte[] input = (byte[])s_slightlyIncorrectZip64.Clone();
            BinaryPrimitives.WriteInt32LittleEndian(input.AsSpan(120), 9); // change 32-bit compressed size from -1

            using var archive = new ZipArchive(new MemoryStream(input));
            ZipArchiveEntry entry = archive.GetEntry("file.txt");
            Assert.Equal(4, entry.Length);
            Assert.Equal(9, entry.CompressedLength); // it should have used 32-bit size
        }

        /// <summary>
        /// As above, but the uncompressed size in the central directory record is less than 0xFFFFFFFF so the value in that location
        /// should be used instead of in the Zip64 extra field.
        /// </summary>
        [Fact]
        public void ReadArchive_WithUnexpectedZip64ExtraFieldSizeUncompressedSizeIn32Bit()
        {
            byte[] input = (byte[])s_slightlyIncorrectZip64.Clone();
            BinaryPrimitives.WriteInt32LittleEndian(input.AsSpan(124), 9); // change 32-bit uncompressed size from -1

            using var archive = new ZipArchive(new MemoryStream(input));
            ZipArchiveEntry entry = archive.GetEntry("file.txt");
            Assert.Equal(9, entry.Length);
            Assert.Equal(6, entry.CompressedLength); // it should have used 32-bit size
        }

        /// <summary>
        /// This test checks behavior of ZipArchive when the startDiskNumber in the extraField is greater than IntMax
        /// </summary>
        [Fact]
        public void ReadArchive_WithDiskStartNumberGreaterThanIntMax()
        {
            byte[] input = (byte[])s_zip64WithBigStartDiskNumber.Clone();
            using var archive = new ZipArchive(new MemoryStream(input));

            var exception = Record.Exception(() => archive.Entries.First());

            Assert.Null(exception);
        }

        /// <summary>
        /// This test checks that an InvalidDataException will be thrown when consuming a zip with bad Huffman data.
        /// </summary>
        [Fact]
        public static async Task ZipArchive_InvalidHuffmanData()
        {
            string filename = bad("HuffmanTreeException.zip");
            using (ZipArchive archive = new ZipArchive(await StreamHelpers.CreateTempCopyStream(filename), ZipArchiveMode.Read))
            {
                ZipArchiveEntry e = archive.Entries[0];
                using (MemoryStream ms = new MemoryStream())
                using (Stream s = e.Open())
                {
                    Assert.Throws<InvalidDataException>(() => s.CopyTo(ms)); //"Should throw on creating Huffman tree"
                }
            }
        }

        [Fact]
        public static void ZipArchive_InvalidVersionToExtract()
        {
            using (MemoryStream updatedStream = new MemoryStream())
            {
                int originalLocalVersionToExtract = s_inconsistentVersionToExtract[4];
                int originalCentralDirectoryVersionToExtract = s_inconsistentVersionToExtract[57];

                // The existing archive will have a "version to extract" of 0.0, but will contain entries
                // with deflate compression (which has a minimum version to extract of 2.0.)
                Assert.Equal(0x00, originalLocalVersionToExtract);
                Assert.Equal(0x00, originalCentralDirectoryVersionToExtract);

                // Write the example data to the stream. We expect to be able to read it (and the entry contents) successfully.
                updatedStream.Write(s_inconsistentVersionToExtract);
                updatedStream.Seek(0, SeekOrigin.Begin);

                using (ZipArchive originalArchive = new ZipArchive(updatedStream, ZipArchiveMode.Read, leaveOpen: true))
                {
                    Assert.Equal(1, originalArchive.Entries.Count);

                    ZipArchiveEntry firstEntry = originalArchive.Entries[0];

                    Assert.Equal("first.bin", firstEntry.Name);
                    Assert.Equal(s_existingSampleData.Length, firstEntry.Length);

                    using (Stream entryStream = firstEntry.Open())
                    {
                        byte[] uncompressedBytes = new byte[firstEntry.Length];
                        int bytesRead = entryStream.Read(uncompressedBytes);

                        Assert.Equal(s_existingSampleData.Length, bytesRead);
                        Assert.Equal(s_existingSampleData, uncompressedBytes);
                    }
                }

                updatedStream.Seek(0, SeekOrigin.Begin);

                // Create a new entry, forcing the central directory headers to be rewritten. The local file header
                // for first.bin would normally be skipped (because it hasn't changed) but it needs to be rewritten
                // because the central directory headers will be rewritten with a valid value and the local file header
                // needs to match.
                using (ZipArchive updatedArchive = new ZipArchive(updatedStream, ZipArchiveMode.Update, leaveOpen: true))
                {
                    ZipArchiveEntry newEntry = updatedArchive.CreateEntry("second.bin", CompressionLevel.NoCompression);

                    // Add data to the new entry
                    using (Stream entryStream = newEntry.Open())
                    {
                        entryStream.Write(s_sampleDataToWrite);
                    }
                }

                byte[] updatedContents = updatedStream.ToArray();
                // Verify that the local file header and the central directory headers have both been rewritten, and both have
                // the correct value.
                int updatedLocalVersionToExtract = updatedContents[4];
                int updatedCentralDirectoryVersionToExtract = updatedContents[101];

                Assert.Equal(20, updatedCentralDirectoryVersionToExtract);
                Assert.Equal(20, updatedLocalVersionToExtract);

                updatedStream.Seek(0, SeekOrigin.Begin);
                // Following an update of the ZipArchive, reopen it in read-only mode. Make sure that both entries are correct.

                using (ZipArchive updatedArchive = new ZipArchive(updatedStream, ZipArchiveMode.Read, true))
                {
                    Assert.Equal(2, updatedArchive.Entries.Count);

                    ZipArchiveEntry firstEntry = updatedArchive.Entries[0];
                    ZipArchiveEntry secondEntry = updatedArchive.Entries[1];

                    Assert.Equal("first.bin", firstEntry.Name);
                    Assert.Equal(s_existingSampleData.Length, firstEntry.Length);

                    Assert.Equal("second.bin", secondEntry.Name);
                    Assert.Equal(s_sampleDataToWrite.Length, secondEntry.Length);

                    using (Stream entryStream = firstEntry.Open())
                    {
                        byte[] uncompressedBytes = new byte[firstEntry.Length];
                        int bytesRead = entryStream.Read(uncompressedBytes);

                        Assert.Equal(s_existingSampleData.Length, bytesRead);
                        Assert.Equal(s_existingSampleData, uncompressedBytes);
                    }

                    using (Stream entryStream = secondEntry.Open())
                    {
                        byte[] uncompressedBytes = new byte[secondEntry.Length];
                        int bytesRead = entryStream.Read(uncompressedBytes);

                        Assert.Equal(s_sampleDataToWrite.Length, bytesRead);
                        Assert.Equal(s_sampleDataToWrite, uncompressedBytes);
                    }
                }
            }
        }

        public static IEnumerable<object[]> ZipArchive_InvalidExtraFieldData_Data()
        {
            // Parameter 1 is the version to extract. Parameter 2 is the total number of "extra data" bytes.

            // "version to extract" is 0 and 20. The valid value (20) proves that updating a ZipArchive with
            // trailing extra field data won't touch existing entries. The invalid value (0) forces ZipArchive
            // to write the corrected header (preserving the trailing data.)
            foreach (byte validVersionToExtract in new byte[] { 0, 20 })
            {
                // "extra field data length" is 3, 4, 7, 8 and 15. This accounts for various interpretations of
                // trailing field data.
                // * 3: zero valid extra fields, trailing data
                // * 4: one valid extra field (type = 0, length = 0) and no trailing data
                // * 7: same as above, with trailing data
                // * 8: multiple valid extra fields, all with type = 0, length = 0. No trailing data
                // * 15: same as above, with trailing data
                foreach (ushort extraFieldDataLength in new ushort[] { 3, 4, 7, 8, 15 })
                {
                    yield return new object[] { validVersionToExtract, extraFieldDataLength };
                }
            }
        }

        [Theory]
        [MemberData(nameof(ZipArchive_InvalidExtraFieldData_Data))]
        public void ZipArchive_InvalidExtraFieldData(byte validVersionToExtract, ushort extraFieldDataLength)
        {
            byte[] invalidExtraFieldData = GenerateInvalidExtraFieldData(validVersionToExtract, extraFieldDataLength,
                out int lhOffset, out int cdOffset);

            using MemoryStream updatedStream = new MemoryStream();

            // Write the example data to the stream. We expect to be able to read it (and the entry contents) successfully.
            updatedStream.Write(invalidExtraFieldData);
            updatedStream.Seek(0, SeekOrigin.Begin);

            using (ZipArchive originalArchive = new ZipArchive(updatedStream, ZipArchiveMode.Read, leaveOpen: true))
            {
                Assert.Equal(1, originalArchive.Entries.Count);

                ZipArchiveEntry firstEntry = originalArchive.Entries[0];

                Assert.Equal("first.bin", firstEntry.Name);
                Assert.Equal(s_existingSampleData.Length, firstEntry.Length);

                using (Stream entryStream = firstEntry.Open())
                {
                    byte[] uncompressedBytes = new byte[firstEntry.Length];
                    int bytesRead = entryStream.Read(uncompressedBytes);

                    Assert.Equal(s_existingSampleData.Length, bytesRead);
                    Assert.Equal(s_existingSampleData, uncompressedBytes);
                }
            }

            updatedStream.Seek(0, SeekOrigin.Begin);

            // Create a new entry, forcing the central directory headers to be rewritten. The local file header
            // for first.bin would normally be skipped (because it hasn't changed) but it needs to be rewritten
            // because the central directory headers will be rewritten with a valid value and the local file header
            // needs to match.
            using (ZipArchive updatedArchive = new ZipArchive(updatedStream, ZipArchiveMode.Update, leaveOpen: true))
            {
                ZipArchiveEntry newEntry = updatedArchive.CreateEntry("second.bin", CompressionLevel.NoCompression);

                // Add data to the new entry
                using (Stream entryStream = newEntry.Open())
                {
                    entryStream.Write(s_sampleDataToWrite);
                }
            }

            byte[] updatedContents = updatedStream.ToArray();
            // Verify that the local file header and the central directory headers have both been rewritten, and both have
            // the correct value. The central directory offset will have moved forwards by 44 bytes - our new entry has been
            // written in front of it.
            int updatedLocalVersionToExtract = updatedContents[lhOffset];
            int updatedCentralDirectoryVersionToExtract = updatedContents[cdOffset + 44];

            Assert.Equal(20, updatedLocalVersionToExtract);
            Assert.Equal(20, updatedCentralDirectoryVersionToExtract);

            updatedStream.Seek(0, SeekOrigin.Begin);
            // Following an update of the ZipArchive, reopen it in read-only mode. Make sure that both entries are correct.

            using (ZipArchive updatedArchive = new ZipArchive(updatedStream, ZipArchiveMode.Read, true))
            {
                Assert.Equal(2, updatedArchive.Entries.Count);

                ZipArchiveEntry firstEntry = updatedArchive.Entries[0];
                ZipArchiveEntry secondEntry = updatedArchive.Entries[1];

                Assert.Equal("first.bin", firstEntry.Name);
                Assert.Equal(s_existingSampleData.Length, firstEntry.Length);

                Assert.Equal("second.bin", secondEntry.Name);
                Assert.Equal(s_sampleDataToWrite.Length, secondEntry.Length);

                using (Stream entryStream = firstEntry.Open())
                {
                    byte[] uncompressedBytes = new byte[firstEntry.Length];
                    int bytesRead = entryStream.Read(uncompressedBytes);

                    Assert.Equal(s_existingSampleData.Length, bytesRead);
                    Assert.Equal(s_existingSampleData, uncompressedBytes);
                }

                using (Stream entryStream = secondEntry.Open())
                {
                    byte[] uncompressedBytes = new byte[secondEntry.Length];
                    int bytesRead = entryStream.Read(uncompressedBytes);

                    Assert.Equal(s_sampleDataToWrite.Length, bytesRead);
                    Assert.Equal(s_sampleDataToWrite, uncompressedBytes);
                }
            }
        }

        // Generates a copy of s_invalidExtraFieldData with a variable number of bytes as extra field data.
        private static byte[] GenerateInvalidExtraFieldData(byte modifiedVersionToExtract, ushort extraFieldDataLength,
            out int lhVersionToExtractOffset,
            out int cdVersionToExtractOffset)
        {
            // s_invalidExtraFieldData contains one byte of extra field data, and extra field data is stored in two places.
            int newZipLength = s_invalidExtraFieldData.Length - 2 + (extraFieldDataLength * 2);
            byte[] extraFieldData = new byte[newZipLength];

            // First 39 bytes are the local file header and filename
            // Byte 4 is the version to extract, bytes 28 and 29 are the extra field data length
            s_invalidExtraFieldData.AsSpan(0, 39).CopyTo(extraFieldData);

            lhVersionToExtractOffset = 4;
            Assert.Equal(0xff, extraFieldData[lhVersionToExtractOffset]);
            Assert.Equal(0x01, extraFieldData[28]);
            extraFieldData[lhVersionToExtractOffset] = modifiedVersionToExtract;

            BinaryPrimitives.WriteUInt16LittleEndian(extraFieldData.AsSpan(28, 2), extraFieldDataLength);

            // Zero out the extra field data
            extraFieldData.AsSpan(39, extraFieldDataLength).Clear();

            // Bytes 40 to 107 are the data and the central directory header. Rewrite the same bytes in this header
            s_invalidExtraFieldData.AsSpan(40, 67).CopyTo(extraFieldData.AsSpan(39 + extraFieldDataLength));

            cdVersionToExtractOffset = 39 + extraFieldDataLength + 18;
            Assert.Equal(0xff, extraFieldData[cdVersionToExtractOffset]);
            Assert.Equal(0x01, extraFieldData[39 + extraFieldDataLength + 42]);
            extraFieldData[cdVersionToExtractOffset] = modifiedVersionToExtract;

            BinaryPrimitives.WriteUInt16LittleEndian(extraFieldData.AsSpan(39 + extraFieldDataLength + 42, 2), extraFieldDataLength);

            // Similarly to the extra field data in the local file header, zero it out in the CD entry
            extraFieldData.AsSpan(39 + extraFieldDataLength + 67).Clear();

            // Copy and modify the EOCD locator header. The offset to the start of the central directory will have changed, so rewrite it
            s_invalidExtraFieldData.AsSpan(108).CopyTo(extraFieldData.AsSpan(39 + extraFieldDataLength + 67 + extraFieldDataLength));

            Assert.Equal((uint)0x34, BinaryPrimitives.ReadUInt32LittleEndian(extraFieldData.AsSpan(39 + extraFieldDataLength + 67 + extraFieldDataLength + 16, 4)));
            BinaryPrimitives.WriteUInt32LittleEndian(
                extraFieldData.AsSpan(39 + extraFieldDataLength + 67 + extraFieldDataLength + 16, 4),
                (uint)39 + extraFieldDataLength + 12);

            return extraFieldData;
        }

        private static readonly byte[] s_inconsistentVersionToExtract =
        {
            // ===== Local file header signature 0x04034b50
            0x50, 0x4b, 0x03, 0x04,
            // version to extract 0.0 (invalid - this should be at least 2.0 to make use of deflate compression)
            0x00, 0x00,
            // general purpose flags
            0x02, 0x00,   // 0000_0002 'for maximum-compression deflating'
            // Deflate
            0x08, 0x00,
            // Last mod file time
            0x3b, 0x33,
            // Last mod date
            0x3f, 0x5a,
            // CRC32
            0x46, 0xd7, 0x6c, 0x45,
            // compressed size
            0x0c, 0x00, 0x00, 0x00,
            // uncompressed size
            0x0a, 0x00, 0x00, 0x00,
            // file name length
            0x09, 0x00,
            // extra field length
            0x00, 0x00,
            // filename
            0x66, 0x69, 0x72, 0x73, 0x74, 0x2e, 0x62, 0x69, 0x6e,
            // -------------
            // Data!
            0x63, 0x60, 0x64, 0x62, 0x66, 0x61, 0x65, 0x63, 0xe7, 0xe0, 0x04, 0x00,
            // -------- Central directory signature 0x02014b50
            0x50, 0x4b, 0x01, 0x02,
            // version made by 2.0
            0x14, 0x00,
            // version to extract 0.0 (invalid - this should be at least 2.0 to make use of deflate compression)
            0x00, 0x00,
            // general purpose flags
            0x02, 0x00,
            // Deflate
            0x08, 0x00,
            // Last mod file time
            0x3b, 0x33,
            // Last mod date
            0x3f, 0x5a,
            // CRC32
            0x46, 0xd7, 0x6c, 0x45,
            // compressed size
            0x0c, 0x00, 0x00, 0x00,
            // uncompressed size
            0x0a, 0x00, 0x00, 0x00,
            // file name length
            0x09, 0x00,
            // extra field length
            0x00, 0x00,
            // file comment length
            0x00, 0x00,
            // disk number start
            0x00, 0x00,
            // internal file attributes
            0x00, 0x00,
            // external file attributes
            0x00, 0x00, 0x00, 0x00,
            // relative offset of local header
            0x00, 0x00, 0x00, 0x00,
            // file name
            0x66, 0x69, 0x72, 0x73, 0x74, 0x2e, 0x62, 0x69, 0x6e,
            // == 'end of CD' signature 0x06054b50
            0x50, 0x4b, 0x05, 0x06,
            // disk number, disk number with CD
            0x00, 0x00,
            0x00, 0x00,
            // total number of entries in CD on this disk, and overall
            0x01, 0x00,
            0x01, 0x00,
            // size of CD
            0x37, 0x00, 0x00, 0x00,
            // offset of start of CD wrt start disk
            0x33, 0x00, 0x00, 0x00,
            // comment length
            0x00, 0x00
        };

        private static readonly byte[] s_invalidExtraFieldData =
        {
            // ===== Local file header signature 0x04034b50
            0x50, 0x4b, 0x03, 0x04,
            // version to extract 0xff.0 - will be substituted for the correct value in the test
            0xFF, 0x00,
            // general purpose flags
            0x02, 0x00,   // 0000_0002 'for maximum-compression deflating'
            // Deflate
            0x08, 0x00,
            // Last mod file time
            0x3b, 0x33,
            // Last mod date
            0x3f, 0x5a,
            // CRC32
            0x46, 0xd7, 0x6c, 0x45,
            // compressed size
            0x0c, 0x00, 0x00, 0x00,
            // uncompressed size
            0x0a, 0x00, 0x00, 0x00,
            // file name length
            0x09, 0x00,
            // extra field length
            0x01, 0x00,
            // filename
            0x66, 0x69, 0x72, 0x73, 0x74, 0x2e, 0x62, 0x69, 0x6e,
            // extra field data
            0x00,
            // -------------
            // Data!
            0x63, 0x60, 0x64, 0x62, 0x66, 0x61, 0x65, 0x63, 0xe7, 0xe0, 0x04, 0x00,
            // -------- Central directory signature 0x02014b50
            0x50, 0x4b, 0x01, 0x02,
            // version made by 2.0
            0x14, 0x00,
            // version to extract 0xff.0 - will be substituted for the correct value in the test
            0xff, 0x00,
            // general purpose flags
            0x02, 0x00,
            // Deflate
            0x08, 0x00,
            // Last mod file time
            0x3b, 0x33,
            // Last mod date
            0x3f, 0x5a,
            // CRC32
            0x46, 0xd7, 0x6c, 0x45,
            // compressed size
            0x0c, 0x00, 0x00, 0x00,
            // uncompressed size
            0x0a, 0x00, 0x00, 0x00,
            // file name length
            0x09, 0x00,
            // extra field length
            0x01, 0x00,
            // file comment length
            0x00, 0x00,
            // disk number start
            0x00, 0x00,
            // internal file attributes
            0x00, 0x00,
            // external file attributes
            0x00, 0x00, 0x00, 0x00,
            // relative offset of local header
            0x00, 0x00, 0x00, 0x00,
            // file name
            0x66, 0x69, 0x72, 0x73, 0x74, 0x2e, 0x62, 0x69, 0x6e,
            // extra field data
            0x00,
            // == 'end of CD' signature 0x06054b50
            0x50, 0x4b, 0x05, 0x06,
            // disk number, disk number with CD
            0x00, 0x00,
            0x00, 0x00,
            // total number of entries in CD on this disk, and overall
            0x01, 0x00,
            0x01, 0x00,
            // size of CD
            0x3a, 0x00, 0x00, 0x00,
            // offset of start of CD wrt start disk
            0x34, 0x00, 0x00, 0x00,
            // comment length
            0x00, 0x00
        };

        private static readonly byte[] s_slightlyIncorrectZip64 =
        {
            // ===== Local file header signature 0x04034b50
            0x50, 0x4b, 0x03, 0x04,
            // version to extract 4.5
            0x2d, 0x00,
            // general purpose flags
            0x00, 0x08,   // 0000_1000 'for enhanced deflating'
            // Deflate
            0x08, 0x00,
            // Last mod file time
            0x17, 0x9b,
            // Last mod date
            0x6d, 0x52,
            // CRC32
            0x0c, 0x7e, 0x7f, 0xd8,
            // compressed size
            0xff, 0xff, 0xff, 0xff,
            // uncompressed size
            0xff, 0xff, 0xff, 0xff,
            // file name length
            0x08, 0x00,
            // extra field length
            0x38, 0x00,
            // filename
            0x66, 0x69, 0x6c, 0x65, 0x2e, 0x74, 0x78, 0x74,
            // -----Zip64 extra field tag
            0x01, 0x00,
            // size of extra field block
            0x10, 0x00,
                    // 8 byte Zip64 uncompressed size, index 42
                    0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                    // 8 byte Zip64 compressed size, index 50
                    0x06, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            // ----- NTFS extra field tag
            0x0a, 0x00,
            // size of extra field block
            0x20, 0x00,
                    // reserved
                    0x00, 0x00, 0x00, 0x00,
                    // tag #1
                    0x01, 0x00,
                    // size of tag #1
                    0x18, 0x00,
                            // Mtime, CTime, Atime
                            0xa8, 0xb1, 0xf6, 0x61, 0x25, 0x18, 0xd7, 0x01,
                            0xa8, 0xb1, 0xf6, 0x61, 0x25, 0x18, 0xd7, 0x01,
                            0xa8, 0xb1, 0xf6, 0x61, 0x25, 0x18, 0xd7, 0x01,
            // -------------
            // Data!
            0x2b, 0x49, 0x2d, 0x2e, 0x01, 0x00,
            // -------- Central directory signature 0x02014b50
            0x50, 0x4b, 0x01, 0x02,
            // version made by 4.5
            0x2d, 0x00,
            // version to extract 4.5
            0x2d, 0x00,
            // general purpose flags
            0x00, 0x08,
            // Deflate
            0x08, 0x00,
            // Last mod file time
            0x17, 0x9b,
            // Last mod date
            0x6d, 0x52,
            // CRC32
            0x0c, 0x7e, 0x7f, 0xd8,
            // 4 byte compressed size, index 120 (-1 indicates refer to Zip64 extra field)
            0xff, 0xff, 0xff, 0xff,
            // 4 byte uncompressed size, index 124 (-1 indicates refer to Zip64 extra field)
            0xff, 0xff, 0xff, 0xff,
            // file name length
            0x08, 0x00,
            // extra field length
            0x44, 0x00,
            // file comment length
            0x00, 0x00,
            // disk number start (-1 indicates refer to Zip64 extra field)
            0x00, 0x00,
            // internal file attributes
            0x00, 0x00,
            // external file attributes
            0x00, 0x00, 0x00, 0x00,
            // relative offset of local header (-1 indicates refer to Zip64 extra field)
            0x00, 0x00, 0x00, 0x00,
            // file name
            0x66, 0x69, 0x6c, 0x65, 0x2e, 0x74, 0x78, 0x74,
            // extra field, content similar to before
            0x01, 0x00,
            0x1c, 0x00,
                    0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                    0x06, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                    0x00, 0x00, 0x00, 0x00,
            0x0a, 0x00,
            0x20, 0x00,
                    0x00, 0x00, 0x00, 0x00,
                    0x01, 0x00, 0x18, 0x00,
                    0xa8, 0xb1, 0xf6, 0x61, 0x25, 0x18, 0xd7, 0x01,
                    0xa8, 0xb1, 0xf6, 0x61, 0x25, 0x18, 0xd7, 0x01,
                    0xa8, 0xb1, 0xf6, 0x61, 0x25, 0x18, 0xd7, 0x01,
            // == 'end of zip64 central directory record' signature 0x06064b50
            0x50, 0x4b, 0x06, 0x06,
            // size
            0x2c, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                    // version made by, version needed
                    0x2d, 0x00, 0x2d, 0x00,
                    // disk number, disk number with CD
                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                    // total number of CD records
                    0x01, 0x00, 0x00, 0x00,
                    // size of CD
                    0x00, 0x00, 0x00, 0x00,
                    // offset of start of CD wrt starting disk
                    0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                    // zip64 extensible data sector
                    0x7a, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x64, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            // == 'zip64 end of CD locator' signature 0x07064b50
            0x50, 0x4b, 0x06, 0x07,
            // number of disk with zip64 CD
            0x00, 0x00, 0x00, 0x00,
            // relative offset of zip64 ECD
            0xde, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            // total number of disks
            0x01, 0x00, 0x00, 0x00,
            // == 'end of CD' signature 0x06054b50
            0x50, 0x4b, 0x05, 0x06,
            // disk number, disk number with CD (-1 indicates refer to Zip64 extra field)
            0x00, 0x00,
            0x00, 0x00,
            // total number of entries in CD on this disk, and overall  (-1 indicates refer to Zip64 extra fields)
            0xff, 0xff,
            0xff, 0xff,
            // size of CD (-1 indicates refer to Zip64 extra field)
            0x7a, 0x00, 0x00, 0x00,
            // offset of start of CD wrt start disk (-1 indicates refer to Zip64 extra field)
            0x64, 0x00, 0x00, 0x00,
            // comment length
            0x00, 0x00
        };

        private static readonly byte[] s_zip64WithBigStartDiskNumber =
        {
            // ===== Local file header signature 0x04034b50
            0x50, 0x4b, 0x03, 0x04,
            // version to extract 4.5
            0x2d, 0x00,
            // general purpose flags
            0x00, 0x08,   // 0000_1000 'for enhanced deflating'
            // Deflate
            0x08, 0x00,
            // Last mod file time
            0x17, 0x9b,
            // Last mod date
            0x6d, 0x52,
            // CRC32
            0x0c, 0x7e, 0x7f, 0xd8,
            // compressed size
            0xff, 0xff, 0xff, 0xff,
            // uncompressed size
            0xff, 0xff, 0xff, 0xff,
            // file name length
            
            0x08, 0x00,
            // extra field length
            0x20, 0x00,
            // filename
            0x66, 0x69, 0x6c, 0x65, 0x2e, 0x74, 0x78, 0x74,
            // -----Zip64 extra field tag
            0x01, 0x00,
            // size of extra field block
            0x20, 0x00,
                    // 8 byte Zip64 uncompressed size, index 42
                    0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                    // 8 byte Zip64 compressed size, index 50
                    0x06, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                    // 8 byte Relative Header Offset 
                    0x0c, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                    // Disk Start Number
                    0xff, 0xff, 0xff, 0xfe, 
            // ----- NTFS extra field tag
            0x0a, 0x00,
            // size of extra field block
            0x20, 0x00,
                    // reserved
                    0x00, 0x00, 0x00, 0x00,
                    // tag #1
                    0x01, 0x00,
                    // size of tag #1
                    0x18, 0x00,
                            // Mtime, CTime, Atime
                            0xa8, 0xb1, 0xf6, 0x61, 0x25, 0x18, 0xd7, 0x01,
                            0xa8, 0xb1, 0xf6, 0x61, 0x25, 0x18, 0xd7, 0x01,
                            0xa8, 0xb1, 0xf6, 0x61, 0x25, 0x18, 0xd7, 0x01,
            // -------------
            // Data!
            0x2b, 0x49, 0x2d, 0x2e, 0x01, 0x00,
            // -------- Central directory signature 0x02014b50
            0x50, 0x4b, 0x01, 0x02,
            // version made by 4.5
            0x2d, 0x00,
            // version to extract 4.5
            0x2d, 0x00,
            // general purpose flags
            0x00, 0x08,
            // Deflate
            0x08, 0x00,
            // Last mod file time
            0x17, 0x9b,
            // Last mod date
            0x6d, 0x52,
            // CRC32
            0x0c, 0x7e, 0x7f, 0xd8,
            // 4 byte compressed size, index 120 (-1 indicates refer to Zip64 extra field)
            0xff, 0xff, 0xff, 0xff,
            // 4 byte uncompressed size, index 124 (-1 indicates refer to Zip64 extra field)
            0xff, 0xff, 0xff, 0xff,
            // file name length
            0x08, 0x00,
            // extra field length
            0x44, 0x00,
            // file comment length
            0x00, 0x00,
            // disk number start (-1 indicates refer to Zip64 extra field)
            0xff, 0xff,
            // internal file attributes
            0x00, 0x00,
            // external file attributes
            0x00, 0x00, 0x00, 0x00,
            // relative offset of local header (-1 indicates refer to Zip64 extra field)
            0x00, 0x00, 0x00, 0x00,
            // file name
            0x66, 0x69, 0x6c, 0x65, 0x2e, 0x74, 0x78, 0x74,
            // extra field, content similar to before
            0x01, 0x00,
            0x1c, 0x00,
                    0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                    0x06, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                    // Disk start number
                    0xff, 0xff, 0xff, 0xfe,
            0x0a, 0x00,
            0x20, 0x00,
                    0x00, 0x00, 0x00, 0x00,
                    0x01, 0x00, 0x18, 0x00,
                    0xa8, 0xb1, 0xf6, 0x61, 0x25, 0x18, 0xd7, 0x01,
                    0xa8, 0xb1, 0xf6, 0x61, 0x25, 0x18, 0xd7, 0x01,
                    0xa8, 0xb1, 0xf6, 0x61, 0x25, 0x18, 0xd7, 0x01,
            // == 'end of zip64 central directory record' signature 0x06064b50
            0x50, 0x4b, 0x06, 0x06,
            // size
            0x2c, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                    // version made by, version needed
                    0x2d, 0x00, 0x2d, 0x00,
                    // disk number, disk number with CD
                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                    // total number of CD records
                    0x01, 0x00, 0x00, 0x00,
                    // size of CD
                    0x00, 0x00, 0x00, 0x00,
                    // offset of start of CD wrt starting disk
                    0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                    // zip64 extensible data sector
                    0x7a, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                    // offset of start cd
                    0x70, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            // == 'zip64 end of CD locator' signature 0x07064b50
            0x50, 0x4b, 0x06, 0x07,
            // number of disk with zip64 CD
            0x00, 0x00, 0x00, 0x00,
            // relative offset of zip64 ECD
            0xea, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            // total number of disks
            0x01, 0x00, 0x00, 0x00,
            // == 'end of CD' signature 0x06054b50
            0x50, 0x4b, 0x05, 0x06,
            // disk number, disk number with CD (-1 indicates refer to Zip64 extra field)
            0x00, 0x00,
            0x00, 0x00,
            // total number of entries in CD on this disk, and overall  (-1 indicates refer to Zip64 extra fields)
            0xff, 0xff,
            0xff, 0xff,
            // size of CD (-1 indicates refer to Zip64 extra field)
            0x7a, 0x00, 0x00, 0x00,
            // offset of start of CD wrt start disk (-1 indicates refer to Zip64 extra field)
            0x70, 0x00, 0x00, 0x00,
            // comment length
            0x00, 0x00
        };
    }
}
