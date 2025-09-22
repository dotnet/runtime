// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public static async Task InvalidInstanceMethods(bool async)
        {
            Stream zipFile = await StreamHelpers.CreateTempCopyStream(zfile("normal.zip"));
            ZipArchive archive = await CreateZipArchive(async, zipFile, ZipArchiveMode.Update);

            //non-existent entry
            Assert.True(null == archive.GetEntry("nonExistentEntry")); //"Should return null on non-existent entry name"
                                                                       //null/empty string
            Assert.Throws<ArgumentNullException>(() => archive.GetEntry(null)); //"Should throw on null entry name"

            ZipArchiveEntry entry = archive.GetEntry("first.txt");

            //null/empty string
            AssertExtensions.Throws<ArgumentException>("entryName", () => archive.CreateEntry("")); //"Should throw on empty entry name"
            Assert.Throws<ArgumentNullException>(() => archive.CreateEntry(null)); //"should throw on null entry name"

            await DisposeZipArchive(async, archive);
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

        [Fact]
        public static async Task InvalidConstructorsAsync()
        {
            //out of range enum values
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
                ZipArchive.CreateAsync(new MemoryStream(), (ZipArchiveMode)(-1), leaveOpen: false, entryNameEncoding: null));
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
                ZipArchive.CreateAsync(new MemoryStream(), (ZipArchiveMode)(4), leaveOpen: false, entryNameEncoding: null));
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
                ZipArchive.CreateAsync(new MemoryStream(), (ZipArchiveMode)(10), leaveOpen: false, entryNameEncoding: null));

            //null/closed stream
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                ZipArchive.CreateAsync((Stream)null, ZipArchiveMode.Read, leaveOpen: false, entryNameEncoding: null));
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                ZipArchive.CreateAsync((Stream)null, ZipArchiveMode.Create, leaveOpen: false, entryNameEncoding: null));
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                ZipArchive.CreateAsync((Stream)null, ZipArchiveMode.Update, leaveOpen: false, entryNameEncoding: null));

            MemoryStream ms = new MemoryStream();
            ms.Dispose();

            await Assert.ThrowsAsync<ArgumentException>(() =>
                ZipArchive.CreateAsync(ms, ZipArchiveMode.Read, leaveOpen: false, entryNameEncoding: null));
            await Assert.ThrowsAsync<ArgumentException>(() =>
                ZipArchive.CreateAsync(ms, ZipArchiveMode.Create, leaveOpen: false, entryNameEncoding: null));
            await Assert.ThrowsAsync<ArgumentException>(() =>
                ZipArchive.CreateAsync(ms, ZipArchiveMode.Update, leaveOpen: false, entryNameEncoding: null));

            //non-seekable to update
            using (LocalMemoryStream nonReadable = new LocalMemoryStream(),
                nonWriteable = new LocalMemoryStream(),
                nonSeekable = new LocalMemoryStream())
            {
                nonReadable.SetCanRead(false);
                nonWriteable.SetCanWrite(false);
                nonSeekable.SetCanSeek(false);

                await Assert.ThrowsAsync<ArgumentException>(() => ZipArchive.CreateAsync(nonReadable, ZipArchiveMode.Read, leaveOpen: false, entryNameEncoding: null));
                await Assert.ThrowsAsync<ArgumentException>(() => ZipArchive.CreateAsync(nonWriteable, ZipArchiveMode.Create, leaveOpen: false, entryNameEncoding: null));
                await Assert.ThrowsAsync<ArgumentException>(() => ZipArchive.CreateAsync(nonReadable, ZipArchiveMode.Update, leaveOpen: false, entryNameEncoding: null));
                await Assert.ThrowsAsync<ArgumentException>(() => ZipArchive.CreateAsync(nonWriteable, ZipArchiveMode.Update, leaveOpen: false, entryNameEncoding: null));
                await Assert.ThrowsAsync<ArgumentException>(() => ZipArchive.CreateAsync(nonSeekable, ZipArchiveMode.Update, leaveOpen: false, entryNameEncoding: null));
            }
        }

        [Theory]
        [InlineData("LZMA.zip", false)]
        [InlineData("LZMA.zip", true)]
        [InlineData("invalidDeflate.zip", false)]
        [InlineData("invalidDeflate.zip", true)]
        public static async Task ZipArchiveEntry_InvalidUpdate(string zipname, bool async)
        {
            string filename = bad(zipname);
            Stream updatedCopy = await StreamHelpers.CreateTempCopyStream(filename);
            string name;
            long length, compressedLength;
            DateTimeOffset lastWriteTime;
            ZipArchive archive = await CreateZipArchive(async, updatedCopy, ZipArchiveMode.Update, true);
            ZipArchiveEntry e = archive.Entries[0];
            name = e.FullName;
            lastWriteTime = e.LastWriteTime;
            length = e.Length;
            compressedLength = e.CompressedLength;
            await Assert.ThrowsAsync<InvalidDataException>(() => OpenEntryStream(async, e)); //"Should throw on open"
            await DisposeZipArchive(async, archive);

            //make sure that update mode preserves that unreadable file
            archive = await CreateZipArchive(async, updatedCopy, ZipArchiveMode.Update);
            e = archive.Entries[0];
            Assert.Equal(name, e.FullName); //"Name isn't the same"
            Assert.Equal(lastWriteTime, e.LastWriteTime); //"LastWriteTime not the same"
            Assert.Equal(length, e.Length); //"Length isn't the same"
            Assert.Equal(compressedLength, e.CompressedLength); //"CompressedLength isn't the same"
            await Assert.ThrowsAsync<InvalidDataException>(() => OpenEntryStream(async, e)); //"Should throw on open"
            await DisposeZipArchive(async, archive);
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public static async Task LargeArchive_DataDescriptor_Read_NonZip64_FileLengthGreaterThanIntMax(bool async)
        {
            MemoryStream stream = await LocalMemoryStream.ReadAppFileAsync(strange("fileLengthGreaterIntLessUInt.zip"));

            ZipArchive archive = await CreateZipArchive(async, stream, ZipArchiveMode.Read);
            ZipArchiveEntry e = archive.GetEntry("large.bin");

            Assert.Equal(3_600_000_000, e.Length);
            Assert.Equal(3_499_028, e.CompressedLength);

            Stream source = await OpenEntryStream(async, e);
            byte[] buffer = new byte[s_bufferSize];
            int read = await source.ReadAsync(buffer, 0, buffer.Length);   // We don't want to inflate this large archive entirely
                                                                           // just making sure it read successfully
            Assert.Equal(s_bufferSize, read);
            foreach (byte b in buffer)
            {
                if (b != '0')
                {
                    Assert.Fail($"The file should be all '0's, but found '{(char)b}'");
                }
            }
            await DisposeStream(async, source);

            await DisposeZipArchive(async, archive);
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public static async Task ZipArchiveEntry_CorruptedStream_ReadMode_CopyTo_UpToUncompressedSize(bool async)
        {
            MemoryStream stream = await LocalMemoryStream.ReadAppFileAsync(zfile("normal.zip"));

            int nameOffset = PatchDataRelativeToFileName(Encoding.ASCII.GetBytes(s_tamperedFileName), stream, 8);  // patch uncompressed size in file header
            PatchDataRelativeToFileName(Encoding.ASCII.GetBytes(s_tamperedFileName), stream, 22, nameOffset + s_tamperedFileName.Length); // patch in central directory too

            ZipArchive archive = await CreateZipArchive(async, stream, ZipArchiveMode.Read);

            ZipArchiveEntry e = archive.GetEntry(s_tamperedFileName);

            using (MemoryStream ms = new MemoryStream())
            {
                Stream source = await OpenEntryStream(async, e);

                await source.CopyToAsync(ms);
                Assert.Equal(e.Length, ms.Length);     // Only allow to decompress up to uncompressed size
                byte[] buffer = new byte[s_bufferSize];
                Assert.Equal(0, await source.ReadAsync(buffer, 0, buffer.Length)); // shouldn't be able read more
                ms.Seek(0, SeekOrigin.Begin);
                int read;
                while ((read = await ms.ReadAsync(buffer, 0, buffer.Length)) != 0)
                { // No need to do anything, just making sure all bytes readable
                }
                Assert.Equal(ms.Position, ms.Length); // all bytes must be read

                await DisposeStream(async, source);
            }

            await DisposeZipArchive(async, archive);
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public static async Task ZipArchiveEntry_CorruptedStream_ReadMode_Read_UpToUncompressedSize(bool async)
        {
            MemoryStream stream = await LocalMemoryStream.ReadAppFileAsync(zfile("normal.zip"));

            int nameOffset = PatchDataRelativeToFileName(Encoding.ASCII.GetBytes(s_tamperedFileName), stream, 8);  // patch uncompressed size in file header
            PatchDataRelativeToFileName(Encoding.ASCII.GetBytes(s_tamperedFileName), stream, 22, nameOffset + s_tamperedFileName.Length); // patch in central directory too

            ZipArchive archive = await CreateZipArchive(async, stream, ZipArchiveMode.Read);

            ZipArchiveEntry e = archive.GetEntry(s_tamperedFileName);
            using (MemoryStream ms = new MemoryStream())
            {
                Stream source = await OpenEntryStream(async, e);

                byte[] buffer = new byte[s_bufferSize];
                int read;
                while ((read = await source.ReadAsync(buffer, 0, buffer.Length)) != 0)
                {
                    await ms.WriteAsync(buffer, 0, read);
                }
                Assert.Equal(e.Length, ms.Length);     // Only allow to decompress up to uncompressed size
                Assert.Equal(0, await source.ReadAsync(buffer, 0, s_bufferSize)); // shouldn't be able read more
                ms.Seek(0, SeekOrigin.Begin);
                while ((read = await ms.ReadAsync(buffer, 0, buffer.Length)) != 0)
                { // No need to do anything, just making sure all bytes readable from output stream
                }
                Assert.Equal(ms.Position, ms.Length); // all bytes must be read

                await DisposeStream(async, source);
            }

            await DisposeZipArchive(async, archive);
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public static async Task ZipArchiveEntry_CorruptedStream_EnsureNoExtraBytesReadOrOverWritten(bool async)
        {
            MemoryStream stream = await LocalMemoryStream.ReadAppFileAsync(zfile("normal.zip"));

            int nameOffset = PatchDataRelativeToFileName(Encoding.ASCII.GetBytes(s_tamperedFileName), stream, 8);  // patch uncompressed size in file header
            PatchDataRelativeToFileName(Encoding.ASCII.GetBytes(s_tamperedFileName), stream, 22, nameOffset + s_tamperedFileName.Length); // patch in central directory too

            ZipArchive archive = await CreateZipArchive(async, stream, ZipArchiveMode.Read);

            ZipArchiveEntry e = archive.GetEntry(s_tamperedFileName);
            Stream source = await OpenEntryStream(async, e);

            byte[] buffer = new byte[e.Length + 20];
            Array.Fill<byte>(buffer, 0xDE);
            int read;
            int offset = 0;
            int length = buffer.Length;

            while ((read = await source.ReadAsync(buffer, offset, length)) != 0)
            {
                offset += read;
                length -= read;
            }
            for (int i = offset; i < buffer.Length; i++)
            {
                Assert.Equal(0xDE, buffer[i]);
            }

            await DisposeStream(async, source);

            await DisposeZipArchive(async, archive);
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public static async Task Zip64ArchiveEntry_CorruptedStream_CopyTo_UpToUncompressedSize(bool async)
        {
            MemoryStream stream = await LocalMemoryStream.ReadAppFileAsync(compat("deflate64.zip"));

            int nameOffset = PatchDataRelativeToFileName(Encoding.ASCII.GetBytes(s_tamperedFileName), stream, 8);  // patch uncompressed size in file header
            PatchDataRelativeToFileName(Encoding.ASCII.GetBytes(s_tamperedFileName), stream, 22, nameOffset + s_tamperedFileName.Length); // patch in central directory too

            ZipArchive archive = await CreateZipArchive(async, stream, ZipArchiveMode.Read);

            ZipArchiveEntry e = archive.GetEntry(s_tamperedFileName);
            using (var ms = new MemoryStream())
            {
                Stream source = await OpenEntryStream(async, e);

                await source.CopyToAsync(ms);
                Assert.Equal(e.Length, ms.Length);     // Only allow to decompress up to uncompressed size
                ms.Seek(0, SeekOrigin.Begin);
                int read;
                byte[] buffer = new byte[s_bufferSize];
                while ((read = await ms.ReadAsync(buffer, 0, buffer.Length)) != 0)
                { // No need to do anything, just making sure all bytes readable
                }
                Assert.Equal(ms.Position, ms.Length); // all bytes must be read

                await DisposeStream(async, source);
            }

            await DisposeZipArchive(async, archive);
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public static async Task ZipArchiveEntry_CorruptedStream_UnCompressedSizeBiggerThanExpected_NothingShouldBreak(bool async)
        {
            MemoryStream stream = await LocalMemoryStream.ReadAppFileAsync(zfile("normal.zip"));

            int nameOffset = PatchDataRelativeToFileNameFillBytes(Encoding.ASCII.GetBytes(s_tamperedFileName), stream, 8);  // patch uncompressed size in file header
            PatchDataRelativeToFileNameFillBytes(Encoding.ASCII.GetBytes(s_tamperedFileName), stream, 22, nameOffset + s_tamperedFileName.Length); // patch in central directory too

            ZipArchive archive = await CreateZipArchive(async, stream, ZipArchiveMode.Read);

            ZipArchiveEntry e = archive.GetEntry(s_tamperedFileName);
            using (MemoryStream ms = new MemoryStream())
            {
                Stream source = await OpenEntryStream(async, e);

                await source.CopyToAsync(ms);
                Assert.True(e.Length > ms.Length);           // Even uncompressed size is bigger than decompressed size there should be no error
                Assert.True(e.CompressedLength < ms.Length);

                await DisposeStream(async, source);
            }

            await DisposeZipArchive(async, archive);
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public static async Task Zip64ArchiveEntry_CorruptedFile_Read_UpToUncompressedSize(bool async)
        {
            MemoryStream stream = await LocalMemoryStream.ReadAppFileAsync(compat("deflate64.zip"));

            int nameOffset = PatchDataRelativeToFileName(Encoding.ASCII.GetBytes(s_tamperedFileName), stream, 8);  // patch uncompressed size in file header
            PatchDataRelativeToFileName(Encoding.ASCII.GetBytes(s_tamperedFileName), stream, 22, nameOffset + s_tamperedFileName.Length); // patch in central directory too

            ZipArchive archive = await CreateZipArchive(async, stream, ZipArchiveMode.Read);

            ZipArchiveEntry e = archive.GetEntry(s_tamperedFileName);
            using (var ms = new MemoryStream())
            {
                Stream source = await OpenEntryStream(async, e);

                byte[] buffer = new byte[s_bufferSize];
                int read;
                while ((read = await source.ReadAsync(buffer, 0, buffer.Length)) != 0)
                {
                    await ms.WriteAsync(buffer, 0, read);
                }
                Assert.Equal(e.Length, ms.Length);     // Only allow to decompress up to uncompressed size
                Assert.Equal(0, await source.ReadAsync(buffer, 0, buffer.Length)); // Shouldn't be readable more

                await DisposeStream(async, source);
            }

            await DisposeZipArchive(async, archive);
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public static async Task UnseekableVeryLargeArchive_DataDescriptor_Read_Zip64(bool async)
        {
            MemoryStream stream = await LocalMemoryStream.ReadAppFileAsync(strange("veryLarge.zip"));

            ZipArchive archive = await CreateZipArchive(async, stream, ZipArchiveMode.Read);

            ZipArchiveEntry e = archive.GetEntry("bigFile.bin");

            Assert.Equal(6_442_450_944, e.Length);
            Assert.Equal(6_261_752, e.CompressedLength);

            Stream source = await OpenEntryStream(async, e);

            byte[] buffer = new byte[s_bufferSize];
            int read = source.Read(buffer, 0, buffer.Length);   // We don't want to inflate this large archive entirely
                                                                // just making sure it read successfully
            Assert.Equal(s_bufferSize, read);

            await DisposeStream(async, source);

            await DisposeZipArchive(async, archive);
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public static async Task UpdateZipArchive_AppendTo_CorruptedFileEntry(bool async)
        {
            MemoryStream stream = await StreamHelpers.CreateTempCopyStream(zfile("normal.zip"));
            int updatedUncompressedLength = 1310976;
            string append = "\r\n\r\nThe answer my friend, is blowin' in the wind.";
            byte[] data = Encoding.ASCII.GetBytes(append);

            int nameOffset = PatchDataRelativeToFileName(Encoding.ASCII.GetBytes(s_tamperedFileName), stream, 8);  // patch uncompressed size in file header
            PatchDataRelativeToFileName(Encoding.ASCII.GetBytes(s_tamperedFileName), stream, 22, nameOffset + s_tamperedFileName.Length); // patch in central directory too

            ZipArchive archive = await CreateZipArchive(async, stream, ZipArchiveMode.Update, true);

            ZipArchiveEntry e = archive.GetEntry(s_tamperedFileName);
            long oldCompressedSize = e.CompressedLength;
            Stream source = await OpenEntryStream(async, e);

            Assert.Equal(updatedUncompressedLength, source.Length);
            source.Seek(0, SeekOrigin.End);
            source.Write(data, 0, data.Length);
            Assert.Equal(updatedUncompressedLength + data.Length, source.Length);

            await DisposeStream(async, source);

            await DisposeZipArchive(async, archive);

            ZipArchive modifiedArchive = await CreateZipArchive(async, stream, ZipArchiveMode.Read);

            e = modifiedArchive.GetEntry(s_tamperedFileName);

            source = await OpenEntryStream(async, e);
            using (var ms = new MemoryStream())
            {
                await source.CopyToAsync(ms, s_bufferSize);
                Assert.Equal(updatedUncompressedLength + data.Length, ms.Length);
                ms.Seek(updatedUncompressedLength, SeekOrigin.Begin);
                byte[] read = new byte[data.Length];
                await ms.ReadAsync(read, 0, data.Length);
                Assert.Equal(append, Encoding.ASCII.GetString(read));
            }
            await DisposeStream(async, source);
            Assert.True(oldCompressedSize > e.CompressedLength); // old compressed size must be reduced by Uncomressed size limit

            await DisposeZipArchive(async, archive);
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public static async Task UpdateZipArchive_OverwriteCorruptedEntry(bool async)
        {
            MemoryStream stream = await StreamHelpers.CreateTempCopyStream(zfile("normal.zip"));
            int updatedUncompressedLength = 1310976;
            string overwrite = "\r\n\r\nThe answer my friend, is blowin' in the wind.";
            byte[] data = Encoding.ASCII.GetBytes(overwrite);

            int nameOffset = PatchDataRelativeToFileName(Encoding.ASCII.GetBytes(s_tamperedFileName), stream, 8);  // patch uncompressed size in file header
            PatchDataRelativeToFileName(Encoding.ASCII.GetBytes(s_tamperedFileName), stream, 22, nameOffset + s_tamperedFileName.Length); // patch in central directory too

            ZipArchive archive = await CreateZipArchive(async, stream, ZipArchiveMode.Update, true);

            ZipArchiveEntry e = archive.GetEntry(s_tamperedFileName);
            string fileName = zmodified(Path.Combine("overwrite", "first.txt"));
            var file = FileData.GetFile(fileName);

            using (var ms = new MemoryStream(data))
            {
                Stream es = await OpenEntryStream(async, e);

                Assert.Equal(updatedUncompressedLength, es.Length);
                es.SetLength(0);
                await ms.CopyToAsync(es, s_bufferSize);
                Assert.Equal(data.Length, es.Length);

                await DisposeStream(async, es);
            }

            await DisposeZipArchive(async, archive);

            ZipArchive modifiedArchive = await CreateZipArchive(async, stream, ZipArchiveMode.Read);

            e = modifiedArchive.GetEntry(s_tamperedFileName);
            Stream s = await OpenEntryStream(async, e);
            using (var ms = new MemoryStream())
            {
                await s.CopyToAsync(ms, s_bufferSize);
                Assert.Equal(data.Length, ms.Length);
                Assert.Equal(overwrite, Encoding.ASCII.GetString(ms.GetBuffer(), 0, data.Length));
            }
            await DisposeStream(async, s);

            await DisposeZipArchive(async, archive);
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public static async Task UpdateZipArchive_AddFileTo_ZipWithCorruptedFile(bool async)
        {
            string addingFile = "added.txt";
            MemoryStream stream = await StreamHelpers.CreateTempCopyStream(zfile("normal.zip"));
            MemoryStream file = await StreamHelpers.CreateTempCopyStream(zmodified(Path.Combine("addFile", addingFile)));

            int nameOffset = PatchDataRelativeToFileName(Encoding.ASCII.GetBytes(s_tamperedFileName), stream, 8);  // patch uncompressed size in file header
            PatchDataRelativeToFileName(Encoding.ASCII.GetBytes(s_tamperedFileName), stream, 22, nameOffset + s_tamperedFileName.Length); // patch in central directory too

            ZipArchive archive = await CreateZipArchive(async, stream, ZipArchiveMode.Update, true);

            ZipArchiveEntry e = archive.CreateEntry(addingFile);

            Stream es = await OpenEntryStream(async, e);
            await file.CopyToAsync(es);
            await DisposeStream(async, es);

            await DisposeZipArchive(async, archive);

            ZipArchive modifiedArchive = await CreateZipArchive(async, stream, ZipArchiveMode.Read);

            e = modifiedArchive.GetEntry(s_tamperedFileName);
            Stream s = await OpenEntryStream(async, e);
            using (var ms = new MemoryStream())
            {
                await s.CopyToAsync(ms, s_bufferSize);
                Assert.Equal(e.Length, ms.Length);  // tampered file should read up to uncompressed size
            }
            await DisposeStream(async, s);

            ZipArchiveEntry addedEntry = modifiedArchive.GetEntry(addingFile);
            Assert.NotNull(addedEntry);
            Assert.Equal(addedEntry.Length, file.Length);

            s = await OpenEntryStream(async, addedEntry);
            // Make sure file content added correctly
            byte[] buffer1 = new byte[1024];
            byte[] buffer2 = new byte[1024];
            file.Seek(0, SeekOrigin.Begin);

            while (await s.ReadAsync(buffer1, 0, buffer1.Length) != 0)
            {
                await file.ReadAsync(buffer2, 0, buffer2.Length);
                Assert.Equal(buffer1, buffer2);
            }

            await DisposeStream(async, s);

            await DisposeZipArchive(async, archive);
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
        [InlineData("CDoffsetOutOfBounds.zip", false)]
        [InlineData("CDoffsetOutOfBounds.zip", true)]
        [InlineData("EOCDmissing.zip", false)]
        [InlineData("EOCDmissing.zip", true)]
        public static async Task ZipArchive_InvalidStream(string zipname, bool async)
        {
            string filename = bad(zipname);
            using (var stream = await StreamHelpers.CreateTempCopyStream(filename))
            {
                await Assert.ThrowsAsync<InvalidDataException>(() => CreateZipArchive(async, stream, ZipArchiveMode.Read));
            }
        }

        [Theory]
        [InlineData("CDoffsetInBoundsWrong.zip", false)]
        [InlineData("CDoffsetInBoundsWrong.zip", true)]
        [InlineData("numberOfEntriesDifferent.zip", false)]
        [InlineData("numberOfEntriesDifferent.zip", true)]
        public static async Task ZipArchive_InvalidEntryTable(string zipname, bool async)
        {
            string filename = bad(zipname);
            await using (ZipArchive archive = await CreateZipArchive(async, await StreamHelpers.CreateTempCopyStream(filename), ZipArchiveMode.Read))
            {
                Assert.Throws<InvalidDataException>(() => archive.Entries[0]);
            }
        }

        public static IEnumerable<object[]> Get_ZipArchive_InvalidEntry_Data()
        {
            foreach (bool async in _bools)
            {
                yield return new object[] { "compressedSizeOutOfBounds.zip", true, async };
                yield return new object[] { "localFileHeaderSignatureWrong.zip", true, async };
                yield return new object[] { "localFileOffsetOutOfBounds.zip", true, async };
                yield return new object[] { "LZMA.zip", true, async };
                yield return new object[] { "invalidDeflate.zip", false, async };
            }
        }

        [Theory]
        [MemberData(nameof(Get_ZipArchive_InvalidEntry_Data))]
        public static async Task ZipArchive_InvalidEntry(string zipname, bool throwsOnOpen, bool async)
        {
            string filename = bad(zipname);
            ZipArchive archive = await CreateZipArchive(async, await StreamHelpers.CreateTempCopyStream(filename), ZipArchiveMode.Read);

            ZipArchiveEntry e = archive.Entries[0];
            if (throwsOnOpen)
            {
                await Assert.ThrowsAsync<InvalidDataException>(() => OpenEntryStream(async, e)); //"should throw on open"
            }
            else
            {
                Stream s = await OpenEntryStream(async, e);
                Assert.Throws<InvalidDataException>(() => s.ReadByte()); //"Unreadable stream"
                await DisposeStream(async, s);
            }

            await DisposeZipArchive(async, archive);
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public static async Task ZipArchiveEntry_InvalidLastWriteTime_Read(bool async)
        {
            ZipArchive archive = await CreateZipArchive(async, await StreamHelpers.CreateTempCopyStream(
                 bad("invaliddate.zip")), ZipArchiveMode.Read);
            Assert.Equal(new DateTime(1980, 1, 1, 0, 0, 0), archive.Entries[0].LastWriteTime.DateTime); //"Date isn't correct on invalid date"
            await DisposeZipArchive(async, archive);
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public static async Task ZipArchiveEntry_InvalidLastWriteTime_Write(bool async)
        {
            ZipArchive archive = await CreateZipArchive(async, new MemoryStream(), ZipArchiveMode.Create);

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

            await DisposeZipArchive(async, archive);
        }

        public static IEnumerable<object[]> Get_StrangeFiles_Data()
        {
            foreach (bool async in _bools)
            {
                yield return new object[] { "extradata/extraDataLHandCDentryAndArchiveComments.zip", "verysmall", true, async };
                yield return new object[] { "extradata/extraDataThenZip64.zip", "verysmall", true, async };
                yield return new object[] { "extradata/zip64ThenExtraData.zip", "verysmall", true, async };
                yield return new object[] { "dataDescriptor.zip", "normalWithoutBinary", false, async };
                yield return new object[] { "filenameTimeAndSizesDifferentInLH.zip", "verysmall", false, async };
            }
        }

        [Theory]
        [MemberData(nameof(Get_StrangeFiles_Data))]
        public static async Task StrangeFiles(string zipFile, string zipFolder, bool requireExplicit, bool async)
        {
            MemoryStream stream = await StreamHelpers.CreateTempCopyStream(strange(zipFile));
            await IsZipSameAsDir(stream, zfolder(zipFolder), ZipArchiveMode.Update, requireExplicit, checkTimes: true, async);
        }

        /// <summary>
        /// This test tiptoes the buffer boundaries to ensure that the size of a read buffer doesn't
        /// cause any bytes to be left in ZLib's buffer.
        /// </summary>
        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public static async Task ZipWithLargeSparseFile(bool async)
        {
            string zipname = strange("largetrailingwhitespacedeflation.zip");
            string entryname = "A/B/C/D";
            using (FileStream stream = File.Open(zipname, FileMode.Open, FileAccess.Read))
            {
                ZipArchive archive = await CreateZipArchive(async, stream, ZipArchiveMode.Read);

                ZipArchiveEntry entry = archive.GetEntry(entryname);
                long size = entry.Length;

                for (int bufferSize = 1; bufferSize <= size; bufferSize++)
                {
                    Stream entryStream = await OpenEntryStream(async, entry);

                    byte[] b = new byte[bufferSize];
                    int read = 0, count = 0;
                    while ((read = await entryStream.ReadAsync(b, 0, bufferSize)) > 0)
                    {
                        count += read;
                    }
                    Assert.Equal(size, count);

                    await DisposeStream(async, entryStream);
                }

                await DisposeZipArchive(async, archive);
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

        public static IEnumerable<object[]> EmptyFiles()
        {
            foreach (bool async in _bools)
            {
                yield return new object[] { s_emptyFileCompressedWithEtx, async };
                yield return new object[] { s_emptyFileCompressedWrongSize, async };
            }
        }

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
        public async Task ReadArchive_WithEmptyDeflatedFile(byte[] fileBytes, bool async)
        {
            using (var testStream = new MemoryStream(fileBytes))
            {
                const string ExpectedFileName = "xl/customProperty2.bin";

                byte firstEntryCompressionMethod = fileBytes[8];

                // first attempt: open archive with zero-length file that is compressed (Deflate = 0x8)
                ZipArchive zip = await CreateZipArchive(async, testStream, ZipArchiveMode.Update, leaveOpen: true);
                // dispose without making any changes will make no changes to the input stream
                await DisposeZipArchive(async, zip);

                byte[] fileContent = testStream.ToArray();

                // compression method should not have changed
                Assert.Equal(firstEntryCompressionMethod, fileBytes[8]);

                testStream.Seek(0, SeekOrigin.Begin);
                // second attempt: open archive with zero-length file that is compressed (Deflate = 0x8)
                zip = await CreateZipArchive(async, testStream, ZipArchiveMode.Update, leaveOpen: true);

                var zipEntryStream = await OpenEntryStream(async, zip.Entries[0]);
                // dispose after opening an entry will rewrite the archive
                await DisposeStream(async, zipEntryStream);

                await DisposeZipArchive(async, zip);

                fileContent = testStream.ToArray();

                // compression method should change to "uncompressed" (Stored = 0x0)
                Assert.Equal(0, fileContent[8]);

                // extract and check the file. should stay empty.
                zip = await CreateZipArchive(async, testStream, ZipArchiveMode.Update);

                ZipArchiveEntry entry = zip.GetEntry(ExpectedFileName);
                Assert.Equal(0, entry.Length);
                Assert.Equal(0, entry.CompressedLength);
                Stream entryStream = await OpenEntryStream(async, entry);
                Assert.Equal(0, entryStream.Length);
                await DisposeStream(async, entryStream);

                await DisposeZipArchive(async, zip);
            }
        }

        /// <summary>
        /// Opens an empty file that has a 64KB EOCD comment.
        /// Adds two 64KB text entries. Verifies they can be read correctly.
        /// Appends 64KB of garbage at the end of the file. Verifies we throw.
        /// Prepends 64KB of garbage at the beginning of the file. Verifies we throw.
        /// </summary>
        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public static async Task ReadArchive_WithEOCDComment_TrailingPrecedingGarbage(bool async)
        {
            async Task InsertEntry(ZipArchive archive, string name, string contents, bool async)
            {
                ZipArchiveEntry entry = archive.CreateEntry(name);
                Stream s = await OpenEntryStream(async, entry);
                using (StreamWriter writer = new StreamWriter(s))
                {
                    writer.WriteLine(contents);
                }
                await DisposeStream(async, s);
            }

            async Task<int> GetEntryContentsLength(ZipArchiveEntry entry, bool async)
            {
                int length = 0;
                Stream stream = await OpenEntryStream(async, entry);
                using (var reader = new StreamReader(stream))
                {
                    length = reader.ReadToEnd().Length;
                }
                await DisposeStream(async, stream);
                return length;
            }

            async Task VerifyValidEntry(ZipArchiveEntry entry, string expectedName, int expectedMinLength, bool async)
            {
                Assert.NotNull(entry);
                Assert.Equal(expectedName, entry.Name);
                // The file has a few more bytes, but should be at least as large as its contents
                Assert.True(await GetEntryContentsLength(entry, async) >= expectedMinLength);
            }

            string name0 = "huge0.txt";
            string name1 = "huge1.txt";
            string str64KB = new string('x', ushort.MaxValue);
            byte[] byte64KB = Encoding.ASCII.GetBytes(str64KB);

            // Open empty file with 64KB EOCD comment
            string path = strange("extradata/emptyWith64KBComment.zip");
            using (MemoryStream archiveStream = await StreamHelpers.CreateTempCopyStream(path))
            {
                // Insert 2 64KB txt entries
                ZipArchive archive = await CreateZipArchive(async, archiveStream, ZipArchiveMode.Update, leaveOpen: true);

                await InsertEntry(archive, name0, str64KB, async);
                await InsertEntry(archive, name1, str64KB, async);

                await DisposeZipArchive(async, archive);

                // Open and verify items
                archiveStream.Seek(0, SeekOrigin.Begin);
                archive = await CreateZipArchive(async, archiveStream, ZipArchiveMode.Read, leaveOpen: true);

                Assert.Equal(2, archive.Entries.Count);
                await VerifyValidEntry(archive.Entries[0], name0, ushort.MaxValue, async);
                await VerifyValidEntry(archive.Entries[1], name1, ushort.MaxValue, async);

                await DisposeZipArchive(async, archive);

                // Append 64KB of garbage
                archiveStream.Seek(0, SeekOrigin.End);
                await archiveStream.WriteAsync(byte64KB, 0, byte64KB.Length);

                // Open should not be possible because we can't find the EOCD in the max search length from the end
                await Assert.ThrowsAsync<InvalidDataException>(async () =>
                {
                    ZipArchive archive = await CreateZipArchive(async, archiveStream, ZipArchiveMode.Read, leaveOpen: true);
                });

                // Create stream with 64KB of prepended garbage, then the above stream appended
                // Attempting to create a ZipArchive should fail: no EOCD found
                using (MemoryStream prependStream = new MemoryStream())
                {
                    await prependStream.WriteAsync(byte64KB, 0, byte64KB.Length);
                    archiveStream.WriteTo(prependStream);

                    await Assert.ThrowsAsync<InvalidDataException>(async () =>
                    {
                        ZipArchive archive = await CreateZipArchive(async, prependStream, ZipArchiveMode.Read);
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
        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task ReadArchive_WithUnexpectedZip64ExtraFieldSize(bool async)
        {
            ZipArchive archive = await CreateZipArchive(async, new MemoryStream(s_slightlyIncorrectZip64), ZipArchiveMode.Read);
            ZipArchiveEntry entry = archive.GetEntry("file.txt");
            Assert.Equal(4, entry.Length);
            Assert.Equal(6, entry.CompressedLength);

            Stream stream = await OpenEntryStream(async, entry);
            string text;
            using (StreamReader reader = new(stream))
            {
                text = await reader.ReadToEndAsync();
            }
            await DisposeStream(async, stream);

            Assert.Equal("test", text);
            await DisposeZipArchive(async, archive);
        }

        /// <summary>
        /// As above, but the compressed size in the central directory record is less than 0xFFFFFFFF so the value in that location
        /// should be used instead of in the Zip64 extra field.
        /// </summary>
        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task ReadArchive_WithUnexpectedZip64ExtraFieldSizeCompressedSizeIn32Bit(bool async)
        {
            byte[] input = (byte[])s_slightlyIncorrectZip64.Clone();
            BinaryPrimitives.WriteInt32LittleEndian(input.AsSpan(120), 9); // change 32-bit compressed size from -1

            ZipArchive archive = await CreateZipArchive(async, new MemoryStream(input), ZipArchiveMode.Read);
            ZipArchiveEntry entry = archive.GetEntry("file.txt");
            Assert.Equal(4, entry.Length);
            Assert.Equal(9, entry.CompressedLength); // it should have used 32-bit size
            await DisposeZipArchive(async, archive);
        }

        /// <summary>
        /// As above, but the uncompressed size in the central directory record is less than 0xFFFFFFFF so the value in that location
        /// should be used instead of in the Zip64 extra field.
        /// </summary>
        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task ReadArchive_WithUnexpectedZip64ExtraFieldSizeUncompressedSizeIn32Bit(bool async)
        {
            byte[] input = (byte[])s_slightlyIncorrectZip64.Clone();
            BinaryPrimitives.WriteInt32LittleEndian(input.AsSpan(124), 9); // change 32-bit uncompressed size from -1

            ZipArchive archive = await CreateZipArchive(async, new MemoryStream(input), ZipArchiveMode.Read);
            ZipArchiveEntry entry = archive.GetEntry("file.txt");
            Assert.Equal(9, entry.Length);
            Assert.Equal(6, entry.CompressedLength); // it should have used 32-bit size
            await DisposeZipArchive(async, archive);
        }

        /// <summary>
        /// This test checks behavior of ZipArchive when the startDiskNumber in the extraField is greater than IntMax
        /// </summary>
        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task ReadArchive_WithDiskStartNumberGreaterThanIntMax(bool async)
        {
            byte[] input = (byte[])s_zip64WithBigStartDiskNumber.Clone();
            ZipArchive archive = await CreateZipArchive(async, new MemoryStream(input), ZipArchiveMode.Read);
            var exception = Record.Exception(() => archive.Entries.First());
            Assert.Null(exception);
            await DisposeZipArchive(async, archive);
        }

        /// <summary>
        /// This test checks that an InvalidDataException will be thrown when consuming a zip with bad Huffman data.
        /// </summary>
        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public static async Task ZipArchive_InvalidHuffmanData(bool async)
        {
            string filename = bad("HuffmanTreeException.zip");
            ZipArchive archive = await CreateZipArchive(async, await StreamHelpers.CreateTempCopyStream(filename), ZipArchiveMode.Read);

            ZipArchiveEntry e = archive.Entries[0];
            using (MemoryStream ms = new MemoryStream())
            using (Stream s = await OpenEntryStream(async, e))
            {
                //"Should throw on creating Huffman tree"
                if (async)
                {
                    await Assert.ThrowsAsync<InvalidDataException>(() => s.CopyToAsync(ms));
                }
                else
                {
                    Assert.Throws<InvalidDataException>(() => s.CopyTo(ms));
                }
            }

            await DisposeZipArchive(async, archive);
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

        [Fact]
        public static async Task ZipArchive_InvalidVersionToExtract_Async()
        {
            await using (MemoryStream updatedStream = new MemoryStream())
            {
                int originalLocalVersionToExtract = s_inconsistentVersionToExtract[4];
                int originalCentralDirectoryVersionToExtract = s_inconsistentVersionToExtract[57];

                // The existing archive will have a "version to extract" of 0.0, but will contain entries
                // with deflate compression (which has a minimum version to extract of 2.0.)
                Assert.Equal(0x00, originalLocalVersionToExtract);
                Assert.Equal(0x00, originalCentralDirectoryVersionToExtract);

                // Write the example data to the stream. We expect to be able to read it (and the entry contents) successfully.
                await updatedStream.WriteAsync(s_inconsistentVersionToExtract);
                updatedStream.Seek(0, SeekOrigin.Begin);

                await using (ZipArchive originalArchive = await ZipArchive.CreateAsync(updatedStream, ZipArchiveMode.Read, leaveOpen: true, entryNameEncoding: null))
                {
                    Assert.Equal(1, originalArchive.Entries.Count);

                    ZipArchiveEntry firstEntry = originalArchive.Entries[0];

                    Assert.Equal("first.bin", firstEntry.Name);
                    Assert.Equal(s_existingSampleData.Length, firstEntry.Length);

                    await using (Stream entryStream = await firstEntry.OpenAsync())
                    {
                        byte[] uncompressedBytes = new byte[firstEntry.Length];
                        int bytesRead = await entryStream.ReadAsync(uncompressedBytes);

                        Assert.Equal(s_existingSampleData.Length, bytesRead);
                        Assert.Equal(s_existingSampleData, uncompressedBytes);
                    }
                }

                updatedStream.Seek(0, SeekOrigin.Begin);

                // Create a new entry, forcing the central directory headers to be rewritten. The local file header
                // for first.bin would normally be skipped (because it hasn't changed) but it needs to be rewritten
                // because the central directory headers will be rewritten with a valid value and the local file header
                // needs to match.
                await using (ZipArchive updatedArchive = await ZipArchive.CreateAsync(updatedStream, ZipArchiveMode.Update, leaveOpen: true, entryNameEncoding: null))
                {
                    ZipArchiveEntry newEntry = updatedArchive.CreateEntry("second.bin", CompressionLevel.NoCompression);

                    // Add data to the new entry
                    await using (Stream entryStream = await newEntry.OpenAsync())
                    {
                        await entryStream.WriteAsync(s_sampleDataToWrite);
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

                await using (ZipArchive updatedArchive = await ZipArchive.CreateAsync(updatedStream, ZipArchiveMode.Read, true, entryNameEncoding: null))
                {
                    Assert.Equal(2, updatedArchive.Entries.Count);

                    ZipArchiveEntry firstEntry = updatedArchive.Entries[0];
                    ZipArchiveEntry secondEntry = updatedArchive.Entries[1];

                    Assert.Equal("first.bin", firstEntry.Name);
                    Assert.Equal(s_existingSampleData.Length, firstEntry.Length);

                    Assert.Equal("second.bin", secondEntry.Name);
                    Assert.Equal(s_sampleDataToWrite.Length, secondEntry.Length);

                    await using (Stream entryStream = await firstEntry.OpenAsync())
                    {
                        byte[] uncompressedBytes = new byte[firstEntry.Length];
                        int bytesRead = await entryStream.ReadAsync(uncompressedBytes);

                        Assert.Equal(s_existingSampleData.Length, bytesRead);
                        Assert.Equal(s_existingSampleData, uncompressedBytes);
                    }

                    await using (Stream entryStream = await secondEntry.OpenAsync())
                    {
                        byte[] uncompressedBytes = new byte[secondEntry.Length];
                        int bytesRead = await entryStream.ReadAsync(uncompressedBytes);

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

        [Theory]
        [MemberData(nameof(ZipArchive_InvalidExtraFieldData_Data))]
        public async Task ZipArchive_InvalidExtraFieldData_Async(byte validVersionToExtract, ushort extraFieldDataLength)
        {
            byte[] invalidExtraFieldData = GenerateInvalidExtraFieldData(validVersionToExtract, extraFieldDataLength,
                out int lhOffset, out int cdOffset);

            await using MemoryStream updatedStream = new MemoryStream();

            // Write the example data to the stream. We expect to be able to read it (and the entry contents) successfully.
            await updatedStream.WriteAsync(invalidExtraFieldData);
            updatedStream.Seek(0, SeekOrigin.Begin);

            await using (ZipArchive originalArchive = await ZipArchive.CreateAsync(updatedStream, ZipArchiveMode.Read, leaveOpen: true, entryNameEncoding: null))
            {
                Assert.Equal(1, originalArchive.Entries.Count);

                ZipArchiveEntry firstEntry = originalArchive.Entries[0];

                Assert.Equal("first.bin", firstEntry.Name);
                Assert.Equal(s_existingSampleData.Length, firstEntry.Length);

                await using (Stream entryStream = await firstEntry.OpenAsync())
                {
                    byte[] uncompressedBytes = new byte[firstEntry.Length];
                    int bytesRead = await entryStream.ReadAsync(uncompressedBytes);

                    Assert.Equal(s_existingSampleData.Length, bytesRead);
                    Assert.Equal(s_existingSampleData, uncompressedBytes);
                }
            }

            updatedStream.Seek(0, SeekOrigin.Begin);

            // Create a new entry, forcing the central directory headers to be rewritten. The local file header
            // for first.bin would normally be skipped (because it hasn't changed) but it needs to be rewritten
            // because the central directory headers will be rewritten with a valid value and the local file header
            // needs to match.
            await using (ZipArchive updatedArchive = await ZipArchive.CreateAsync(updatedStream, ZipArchiveMode.Update, leaveOpen: true, entryNameEncoding: null))
            {
                ZipArchiveEntry newEntry = updatedArchive.CreateEntry("second.bin", CompressionLevel.NoCompression);

                // Add data to the new entry
                await using (Stream entryStream = await newEntry.OpenAsync())
                {
                    await entryStream.WriteAsync(s_sampleDataToWrite);
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

            await using (ZipArchive updatedArchive = await ZipArchive.CreateAsync(updatedStream, ZipArchiveMode.Read, true, entryNameEncoding: null))
            {
                Assert.Equal(2, updatedArchive.Entries.Count);

                ZipArchiveEntry firstEntry = updatedArchive.Entries[0];
                ZipArchiveEntry secondEntry = updatedArchive.Entries[1];

                Assert.Equal("first.bin", firstEntry.Name);
                Assert.Equal(s_existingSampleData.Length, firstEntry.Length);

                Assert.Equal("second.bin", secondEntry.Name);
                Assert.Equal(s_sampleDataToWrite.Length, secondEntry.Length);

                await using (Stream entryStream = await firstEntry.OpenAsync())
                {
                    byte[] uncompressedBytes = new byte[firstEntry.Length];
                    int bytesRead = await entryStream.ReadAsync(uncompressedBytes);

                    Assert.Equal(s_existingSampleData.Length, bytesRead);
                    Assert.Equal(s_existingSampleData, uncompressedBytes);
                }

                await using (Stream entryStream = await secondEntry.OpenAsync())
                {
                    byte[] uncompressedBytes = new byte[secondEntry.Length];
                    int bytesRead = await entryStream.ReadAsync(uncompressedBytes);

                    Assert.Equal(s_sampleDataToWrite.Length, bytesRead);
                    Assert.Equal(s_sampleDataToWrite, uncompressedBytes);
                }
            }
        }

        [Fact]
        public static async Task NoAsyncCallsWhenUsingSync()
        {
            using MemoryStream ms = new();
            using NoAsyncCallsStream s = new(ms); // Only allows sync calls

            // Create mode
            using (ZipArchive archive = new ZipArchive(s, ZipArchiveMode.Create, leaveOpen: true, entryNameEncoding: Encoding.UTF8))
            {
                using MemoryStream normalZipStream = await StreamHelpers.CreateTempCopyStream(zfile("normal.zip"));
                normalZipStream.Position = 0;

                // Note this is not using NoAsyncCallsStream, so it can be opened in async mode
                await using (ZipArchive normalZipArchive = await ZipArchive.CreateAsync(normalZipStream, ZipArchiveMode.Read, leaveOpen: false, entryNameEncoding: null))
                {
                    var normalZipEntries = normalZipArchive.Entries;

                    foreach (ZipArchiveEntry normalEntry in normalZipEntries)
                    {
                        ZipArchiveEntry newEntry = archive.CreateEntry(normalEntry.FullName);
                        using (Stream newEntryStream = newEntry.Open())
                        {
                            // Note the parent archive is not using NoAsyncCallsStream, so it can be opened in async mode
                            await using (Stream normalEntryStream = await normalEntry.OpenAsync())
                            {
                                // Note the parent archive is not using NoAsyncCallsStream, so it can be copied in async mode
                                await normalEntryStream.CopyToAsync(newEntryStream);
                            }
                        }
                    }
                }
            }

            ms.Position = 0;

            // Read mode
            using (ZipArchive archive = new ZipArchive(s, ZipArchiveMode.Read, leaveOpen: true, entryNameEncoding: Encoding.UTF8))
            {
                _ = archive.Comment;

                // Entries is sync only
                s.IsRestrictionEnabled = false;
                var entries = archive.Entries;
                s.IsRestrictionEnabled = true;

                foreach (var entry in entries)
                {
                    _ = archive.GetEntry(entry.Name);
                    _ = entry.Archive;
                    _ = entry.Comment;
                    _ = entry.CompressedLength;
                    _ = entry.Crc32;
                    _ = entry.ExternalAttributes;
                    _ = entry.FullName;
                    _ = entry.IsEncrypted;
                    _ = entry.LastWriteTime;
                    _ = entry.Length;
                    _ = entry.Name;
                    using (var es = entry.Open())
                    {
                        byte[] buffer = [0x0];

                        _ = es.Read(buffer, 0, buffer.Length);
                        _ = es.Read(buffer.AsSpan());
                        _ = es.ReadByte();
                    }
                }
                _ = archive.Mode;
            }

            ms.Position = 0;

            // Update mode
            using (ZipArchive archive = new ZipArchive(s, ZipArchiveMode.Update, leaveOpen: false, entryNameEncoding: Encoding.UTF8))
            {
                // Entries is sync only
                s.IsRestrictionEnabled = false;
                ZipArchiveEntry entryToDelete = archive.Entries[0];
                s.IsRestrictionEnabled = true;

                entryToDelete.Delete();

                ZipArchiveEntry entry = archive.CreateEntry("mynewentry.txt");
                using (var es = entry.Open())
                {
                    byte[] buffer = [0x0];
                    es.Write(buffer, 0, buffer.Length);
                    es.Write(buffer.AsSpan());
                    es.WriteByte(buffer[0]);
                }
            }
        }

        [Fact]
        public static async Task NoSyncCallsWhenUsingAsync()
        {
            using MemoryStream ms = new();
            using NoSyncCallsStream s = new(ms); // Only allows async calls

            // Create mode
            await using (ZipArchive archive = await ZipArchive.CreateAsync(s, ZipArchiveMode.Create, leaveOpen: true, entryNameEncoding: Encoding.UTF8))
            {
                await using MemoryStream normalZipStream = await StreamHelpers.CreateTempCopyStream(zfile("normal.zip"));
                normalZipStream.Position = 0;

                // Note this is not using NoSyncCallsStream, so it can be opened in sync mode
                using (ZipArchive normalZipArchive = new ZipArchive(normalZipStream, ZipArchiveMode.Read, leaveOpen: false, entryNameEncoding: null))
                {
                    var normalZipEntries = normalZipArchive.Entries;

                    foreach (ZipArchiveEntry normalEntry in normalZipEntries)
                    {
                        ZipArchiveEntry newEntry = archive.CreateEntry(normalEntry.FullName);
                        await using (Stream newEntryStream = await newEntry.OpenAsync())
                        {
                            // Note the parent archive is not using NoSyncCallsStream, so it can be opened in sync mode
                            using (Stream normalEntryStream = normalEntry.Open())
                            {
                                // Note the parent archive is not using NoSyncCallsStream, so it can be copied in sync mode
                                normalEntryStream.CopyTo(newEntryStream);
                            }
                        }
                    }
                }
            }

            ms.Position = 0;

            // Read mode
            await using (ZipArchive archive = await ZipArchive.CreateAsync(s, ZipArchiveMode.Read, leaveOpen: true, entryNameEncoding: Encoding.UTF8))
            {
                _ = archive.Comment;

                // Entries is sync only
                s.IsRestrictionEnabled = false;
                var entries = archive.Entries;
                s.IsRestrictionEnabled = true;

                foreach (var entry in entries)
                {
                    _ = archive.GetEntry(entry.Name);
                    _ = entry.Archive;
                    _ = entry.Comment;
                    _ = entry.CompressedLength;
                    _ = entry.Crc32;
                    _ = entry.ExternalAttributes;
                    _ = entry.FullName;
                    _ = entry.IsEncrypted;
                    _ = entry.LastWriteTime;
                    _ = entry.Length;
                    _ = entry.Name;
                    await using (var es = await entry.OpenAsync())
                    {
                        byte[] buffer = [0x0];

                        _ = await es.ReadAsync(buffer);
                        _ = await es.ReadAsync(buffer.AsMemory());
                        _ = await es.ReadByteAsync();
                    }
                }
                _ = archive.Mode;
            }

            ms.Position = 0;

            await using (ZipArchive archive = await ZipArchive.CreateAsync(s, ZipArchiveMode.Update, leaveOpen: false, entryNameEncoding: Encoding.UTF8))
            {
                // Entries is sync only
                s.IsRestrictionEnabled = false;
                ZipArchiveEntry entryToDelete = archive.Entries[0];
                s.IsRestrictionEnabled = true;

                entryToDelete.Delete(); // Delete is async only

                ZipArchiveEntry entry = archive.CreateEntry("mynewentry.txt");
                await using (var es = await entry.OpenAsync())
                {
                    byte[] buffer = [0x0];
                    await es.WriteAsync(buffer, 0, buffer.Length);
                    await es.WriteAsync(buffer.AsMemory());
                }
            }
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task ReadArchive_FrontTruncatedFile_Throws(bool async)
        {
            for (int i = 1; i < s_slightlyIncorrectZip64.Length - 1; i++)
            {
                await Assert.ThrowsAsync<InvalidDataException>(
                    // The archive is truncated, so it should throw an exception
                    () => CreateZipArchive(async, new MemoryStream(s_slightlyIncorrectZip64, i, s_slightlyIncorrectZip64.Length - i), ZipArchiveMode.Read));
            }
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task ReadArchive_Zip64EocdLocatorInvalidOffset_Throws(bool async)
        {
            byte[] data = s_slightlyIncorrectZip64.ToArray();

            foreach (long offset in new[] { -1, int.MaxValue - 1 })
            {
                // Find Zip64 EOCD locator record
                int eocdlOffset = data.AsSpan().LastIndexOf(new byte[] { 0x50, 0x4b, 0x06, 0x07 });
                Assert.True(eocdlOffset >= 0, "Zip64 EOCD locator not found in test data");

                // skip 4B signature and 4B index of disk, then overwrite the 8B offset to start of central directory
                BinaryPrimitives.WriteInt64LittleEndian(data.AsSpan(eocdlOffset + 8, 8), offset);

                await Assert.ThrowsAsync<InvalidDataException>(
                    // The archive is truncated, so it should throw an exception
                    () => CreateZipArchive(async, new MemoryStream(data), ZipArchiveMode.Read));
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
