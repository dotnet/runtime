// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO.Compression.Tests.Utilities;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Compression.Tests
{
    // Test utility class that wraps a stream and makes it non-seekable
    internal class NonSeekableStream : Stream
    {
        private readonly Stream _baseStream;

        public NonSeekableStream(Stream baseStream)
        {
            _baseStream = baseStream;
        }

        public override bool CanRead => _baseStream.CanRead;
        public override bool CanSeek => false; // Force non-seekable
        public override bool CanWrite => _baseStream.CanWrite;
        public override long Length => _baseStream.Length;
        public override long Position 
        { 
            get => _baseStream.Position; 
            set => throw new NotSupportedException("Seeking is not supported"); 
        }

        public override void Flush() => _baseStream.Flush();
        public override int Read(byte[] buffer, int offset, int count) => _baseStream.Read(buffer, offset, count);
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException("Seeking is not supported");
        public override void SetLength(long value) => throw new NotSupportedException("SetLength is not supported");
        public override void Write(byte[] buffer, int offset, int count) => _baseStream.Write(buffer, offset, count);

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _baseStream.Dispose();
            base.Dispose(disposing);
        }
    }

    public class zip_ReadTests : ZipFileTestBase
    {
        public static IEnumerable<object[]> Get_ReadNormal_Data()
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
        [MemberData(nameof(Get_ReadNormal_Data))]
        public static Task ReadNormal(string zipFile, string zipFolder, bool async) => IsZipSameAsDir(zfile(zipFile), zfolder(zipFolder), ZipArchiveMode.Read, async);

        [Theory]
        [MemberData(nameof(Get_ReadNormal_Data))]
        public static async Task TestStreamingRead(string zipFile, string zipFolder, bool async)
        {
            using (var stream = await StreamHelpers.CreateTempCopyStream(zfile(zipFile)))
            {
                Stream wrapped = new WrappedStream(stream, true, false, false, null);
                await IsZipSameAsDir(wrapped, zfolder(zipFolder), ZipArchiveMode.Read, requireExplicit: true, checkTimes: true, async);
                Assert.False(wrapped.CanRead, "Wrapped stream should be closed at this point"); //check that it was closed
            }
        }

        public static IEnumerable<object[]> Get_TestPartialReads_Data()
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

        public static IEnumerable<object[]> Get_BooleanCombinations_Data()
        {
            foreach (bool async in _bools)
            {
                foreach (bool useSeekMethod in _bools)
                {
                    yield return new object[] { async, useSeekMethod };
                }
            }
        }

        [Theory]
        [MemberData(nameof(Get_TestPartialReads_Data))]
        public static async Task TestPartialReads(string zipFile, string zipFolder, bool async)
        {
            using (MemoryStream stream = await StreamHelpers.CreateTempCopyStream(zfile(zipFile)))
            {
                Stream clamped = new ClampedReadStream(stream, readSizeLimit: 1);
                await IsZipSameAsDir(clamped, zfolder(zipFolder), ZipArchiveMode.Read, requireExplicit: true, checkTimes: true, async);
            }
        }

        [Fact]
        public static async Task ReadInterleavedAsync()
        {
            ZipArchive archive = await ZipArchive.CreateAsync(await StreamHelpers.CreateTempCopyStream(zfile("normal.zip")), ZipArchiveMode.Read, leaveOpen: false, entryNameEncoding: null);

            ZipArchiveEntry e1 = archive.GetEntry("first.txt");
            ZipArchiveEntry e2 = archive.GetEntry("notempty/second.txt");

            //read all of e1 and e2's contents
            byte[] e1readnormal = new byte[e1.Length];
            byte[] e2readnormal = new byte[e2.Length];
            byte[] e1interleaved = new byte[e1.Length];
            byte[] e2interleaved = new byte[e2.Length];

            await using (Stream e1s = await e1.OpenAsync())
            {
                await ReadBytes(e1s, e1readnormal, e1.Length, async: true);
            }
            await using (Stream e2s = await e2.OpenAsync())
            {
                await ReadBytes(e2s, e2readnormal, e2.Length, async: true);
            }

            //now read interleaved, assume we are working with < 4gb files
            const int bytesAtATime = 15;

            await using (Stream e1s = await e1.OpenAsync(), e2s = await e2.OpenAsync())
            {
                int e1pos = 0;
                int e2pos = 0;

                while (e1pos < e1.Length || e2pos < e2.Length)
                {
                    if (e1pos < e1.Length)
                    {
                        int e1bytesRead = await e1s.ReadAsync(e1interleaved, e1pos,
                            bytesAtATime + e1pos > e1.Length ? (int)e1.Length - e1pos : bytesAtATime);
                        e1pos += e1bytesRead;
                    }

                    if (e2pos < e2.Length)
                    {
                        int e2bytesRead = await e2s.ReadAsync(e2interleaved, e2pos,
                            bytesAtATime + e2pos > e2.Length ? (int)e2.Length - e2pos : bytesAtATime);
                        e2pos += e2bytesRead;
                    }
                }
            }

            //now compare to original read
            ArraysEqual<byte>(e1readnormal, e1interleaved, e1readnormal.Length);
            ArraysEqual<byte>(e2readnormal, e2interleaved, e2readnormal.Length);

            //now read one entry interleaved
            byte[] e1selfInterleaved1 = new byte[e1.Length];
            byte[] e1selfInterleaved2 = new byte[e2.Length];


            await using (Stream s1 = await e1.OpenAsync(), s2 = await e1.OpenAsync())
            {
                int s1pos = 0;
                int s2pos = 0;

                while (s1pos < e1.Length || s2pos < e1.Length)
                {
                    if (s1pos < e1.Length)
                    {
                        int s1bytesRead = s1.Read(e1interleaved, s1pos,
                            bytesAtATime + s1pos > e1.Length ? (int)e1.Length - s1pos : bytesAtATime);
                        s1pos += s1bytesRead;
                    }

                    if (s2pos < e1.Length)
                    {
                        int s2bytesRead = s2.Read(e2interleaved, s2pos,
                            bytesAtATime + s2pos > e1.Length ? (int)e1.Length - s2pos : bytesAtATime);
                        s2pos += s2bytesRead;
                    }
                }
            }

            //now compare to original read
            ArraysEqual<byte>(e1readnormal, e1selfInterleaved1, e1readnormal.Length);
            ArraysEqual<byte>(e1readnormal, e1selfInterleaved2, e1readnormal.Length);

            await archive.DisposeAsync();
        }

        [Fact]
        public static async Task ReadInterleaved()
        {
            using (ZipArchive archive = new ZipArchive(await StreamHelpers.CreateTempCopyStream(zfile("normal.zip"))))
            {
                ZipArchiveEntry e1 = archive.GetEntry("first.txt");
                ZipArchiveEntry e2 = archive.GetEntry("notempty/second.txt");

                //read all of e1 and e2's contents
                byte[] e1readnormal = new byte[e1.Length];
                byte[] e2readnormal = new byte[e2.Length];
                byte[] e1interleaved = new byte[e1.Length];
                byte[] e2interleaved = new byte[e2.Length];

                using (Stream e1s = e1.Open())
                {
                    await ReadBytes(e1s, e1readnormal, e1.Length, async: false);
                }
                using (Stream e2s = e2.Open())
                {
                    await ReadBytes(e2s, e2readnormal, e2.Length, async: false);
                }

                //now read interleaved, assume we are working with < 4gb files
                const int bytesAtATime = 15;

                using (Stream e1s = e1.Open(), e2s = e2.Open())
                {
                    int e1pos = 0;
                    int e2pos = 0;

                    while (e1pos < e1.Length || e2pos < e2.Length)
                    {
                        if (e1pos < e1.Length)
                        {
                            int e1bytesRead = e1s.Read(e1interleaved, e1pos,
                                bytesAtATime + e1pos > e1.Length ? (int)e1.Length - e1pos : bytesAtATime);
                            e1pos += e1bytesRead;
                        }

                        if (e2pos < e2.Length)
                        {
                            int e2bytesRead = e2s.Read(e2interleaved, e2pos,
                                bytesAtATime + e2pos > e2.Length ? (int)e2.Length - e2pos : bytesAtATime);
                            e2pos += e2bytesRead;
                        }
                    }
                }

                //now compare to original read
                ArraysEqual<byte>(e1readnormal, e1interleaved, e1readnormal.Length);
                ArraysEqual<byte>(e2readnormal, e2interleaved, e2readnormal.Length);

                //now read one entry interleaved
                byte[] e1selfInterleaved1 = new byte[e1.Length];
                byte[] e1selfInterleaved2 = new byte[e2.Length];


                using (Stream s1 = e1.Open(), s2 = e1.Open())
                {
                    int s1pos = 0;
                    int s2pos = 0;

                    while (s1pos < e1.Length || s2pos < e1.Length)
                    {
                        if (s1pos < e1.Length)
                        {
                            int s1bytesRead = s1.Read(e1interleaved, s1pos,
                                bytesAtATime + s1pos > e1.Length ? (int)e1.Length - s1pos : bytesAtATime);
                            s1pos += s1bytesRead;
                        }

                        if (s2pos < e1.Length)
                        {
                            int s2bytesRead = s2.Read(e2interleaved, s2pos,
                                bytesAtATime + s2pos > e1.Length ? (int)e1.Length - s2pos : bytesAtATime);
                            s2pos += s2bytesRead;
                        }
                    }
                }

                //now compare to original read
                ArraysEqual<byte>(e1readnormal, e1selfInterleaved1, e1readnormal.Length);
                ArraysEqual<byte>(e1readnormal, e1selfInterleaved2, e1readnormal.Length);
            }
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public static async Task ReadModeInvalidOpsTest(bool async)
        {
            await using MemoryStream ms = await StreamHelpers.CreateTempCopyStream(zfile("normal.zip"));

            ZipArchive archive = await CreateZipArchive(async, ms, ZipArchiveMode.Read);
            ZipArchiveEntry e = archive.GetEntry("first.txt");

            //should also do it on deflated stream

            //on archive
            Assert.Throws<NotSupportedException>(() => archive.CreateEntry("hi there")); //"Should not be able to create entry"

            //on entry
            Assert.Throws<NotSupportedException>(() => e.Delete()); //"Should not be able to delete entry"
            //Throws<NotSupportedException>(() => e.MoveTo("dirka"));
            Assert.Throws<NotSupportedException>(() => e.LastWriteTime = new DateTimeOffset()); //"Should not be able to update time"

            //on stream
            Stream s = await OpenEntryStream(async, e);
            Assert.Throws<NotSupportedException>(() => s.Flush()); //"Should not be able to flush on read stream"
            Assert.Throws<NotSupportedException>(() => s.WriteByte(25)); //"should not be able to write to read stream"
            
            // Seeking behavior depends on whether the entry is compressed and the underlying stream is seekable
            if (!s.CanSeek)
            {
                Assert.Throws<NotSupportedException>(() => s.Position = 4); //"should not be able to seek on non-seekable read stream"
                Assert.Throws<NotSupportedException>(() => s.Seek(0, SeekOrigin.Begin)); //"should not be able to seek on non-seekable read stream"
            }
            
            Assert.Throws<NotSupportedException>(() => s.SetLength(0)); //"should not be able to resize read stream"

            await DisposeZipArchive(async, archive);

            //after disposed
            Assert.Throws<ObjectDisposedException>(() => { var x = archive.Entries; }); //"Should not be able to get entries on disposed archive"
            Assert.Throws<NotSupportedException>(() => archive.CreateEntry("dirka")); //"should not be able to create on disposed archive"

            await Assert.ThrowsAsync<ObjectDisposedException>(() => OpenEntryStream(async, e)); //"should not be able to open on disposed archive"
            Assert.Throws<NotSupportedException>(() => e.Delete()); //"should not be able to delete on disposed archive"
            Assert.Throws<ObjectDisposedException>(() => { e.LastWriteTime = new DateTimeOffset(); }); //"Should not be able to update on disposed archive"

            Assert.Throws<NotSupportedException>(() => s.ReadByte()); //"should not be able to read on disposed archive"

            await DisposeStream(async, s);
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public static async Task TestEmptyLastModifiedEntryValueNotThrowingInternalException(bool async)
        {
            var emptyDateIndicator = new DateTimeOffset(new DateTime(1980, 1, 1, 0, 0, 0));
            var buffer = new byte[100];//empty archive we will make will have exact this size
            using var memoryStream = new MemoryStream(buffer);

            ZipArchive singleEntryArchive = await CreateZipArchive(async, memoryStream, ZipArchiveMode.Create, true);
            singleEntryArchive.CreateEntry("1");
            await DisposeZipArchive(async, singleEntryArchive);

            //set LastWriteTime bits to 0 in this trivial archive
            const int lastWritePosition = 43;
            buffer[lastWritePosition] = 0;
            buffer[lastWritePosition + 1] = 0;
            buffer[lastWritePosition + 2] = 0;
            buffer[lastWritePosition + 3] = 0;
            memoryStream.Seek(0, SeekOrigin.Begin);

            ZipArchive archive = await CreateZipArchive(async, memoryStream, ZipArchiveMode.Read, true);
            Assert.Equal(archive.Entries[0].LastWriteTime, emptyDateIndicator);
            await DisposeZipArchive(async, archive);
        }

        [Theory]
        [InlineData("normal.zip", false)]
        [InlineData("normal.zip", true)]
        [InlineData("small.zip", false)]
        [InlineData("small.zip", true)]
        public static async Task EntriesNotEncryptedByDefault(string zipFile, bool async)
        {
            ZipArchive archive = await CreateZipArchive(async, await StreamHelpers.CreateTempCopyStream(zfile(zipFile)), ZipArchiveMode.Read);
            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                Assert.False(entry.IsEncrypted);
            }
            await DisposeZipArchive(async, archive);
        }

        public static IEnumerable<object[]> Get_IdentifyEncryptedEntries_Data()
        {
            foreach (bool async in _bools)
            {
                yield return new object[] { "encrypted_entries_weak.zip", async };
                yield return new object[] { "encrypted_entries_aes256.zip", async };
                yield return new object[] { "encrypted_entries_mixed.zip", async };
            }
        }

        [Theory]
        [MemberData(nameof(Get_IdentifyEncryptedEntries_Data))]
        public static async Task IdentifyEncryptedEntries(string zipFile, bool async)
        {
            var entriesEncrypted = new Dictionary<string, bool>();

            ZipArchive archive = await CreateZipArchive(async, await StreamHelpers.CreateTempCopyStream(zfile(zipFile)), ZipArchiveMode.Read);
            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                entriesEncrypted.Add(entry.Name, entry.IsEncrypted);
            }
            await DisposeZipArchive(async, archive);

            var expectedEntries = new Dictionary<string, bool>()
            {
                { "file1-encrypted.txt", true },
                { "file2-unencrypted.txt", false },
                { "file3-encrypted.txt", true },
                { "file4-unencrypted.txt", false },
            };

            Assert.Equal(expectedEntries, entriesEncrypted);
        }

        public static IEnumerable<object[]> Get_EnsureDisposeIsCalledAsExpectedOnTheUnderlyingStream_Data()
        {
            foreach (bool async in _bools)
            {
                // leaveOpen, expectedDisposeCalls, async
                yield return new object[] { true, 0, async };
                yield return new object[] { false, 1, async };
            }
        }

        [Theory]
        [MemberData(nameof(Get_EnsureDisposeIsCalledAsExpectedOnTheUnderlyingStream_Data))]
        public static async Task EnsureDisposeIsCalledAsExpectedOnTheUnderlyingStream(bool leaveOpen, int expectedDisposeCalls, bool async)
        {
            var disposeCallCountingStream = new DisposeCallCountingStream();
            using (var tempStream = await StreamHelpers.CreateTempCopyStream(zfile("small.zip")))
            {
                tempStream.CopyTo(disposeCallCountingStream);
            }

            ZipArchive archive = await CreateZipArchive(async, disposeCallCountingStream, ZipArchiveMode.Read, leaveOpen);
            // Iterate through entries to ensure read of zip file
            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                Assert.False(entry.IsEncrypted);
            }
            await DisposeZipArchive(async, archive);

            Assert.Equal(expectedDisposeCalls, disposeCallCountingStream.NumberOfDisposeCalls);
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public static async Task CanReadLargeCentralDirectoryHeader(bool async)
        {
            // A 19-character filename will result in a 65-byte central directory header. 64 of these will make the central directory
            // read process stretch into two 4KB buffers.
            int count = 64;
            string entryNameFormat = "example/file-{0:00}.dat";

            using (MemoryStream archiveStream = new MemoryStream())
            {
                ZipArchive creationArchive = await CreateZipArchive(async, archiveStream, ZipArchiveMode.Create, true);
                for (int i = 0; i < count; i++)
                {
                    creationArchive.CreateEntry(string.Format(entryNameFormat, i));
                }
                await DisposeZipArchive(async, creationArchive);

                archiveStream.Seek(0, SeekOrigin.Begin);

                ZipArchive readArchive = await CreateZipArchive(async, archiveStream, ZipArchiveMode.Read);
                Assert.Equal(count, readArchive.Entries.Count);
                for (int i = 0; i < count; i++)
                {
                    Assert.Equal(string.Format(entryNameFormat, i), readArchive.Entries[i].FullName);
                    Assert.Equal(0, readArchive.Entries[i].CompressedLength);
                    Assert.Equal(0, readArchive.Entries[i].Length);
                }
                await DisposeZipArchive(async, readArchive);
            }
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public static async Task ArchivesInOffsetOrder_UpdateMode(bool async)
        {
            // When the ZipArchive which has been opened in Update mode is disposed of, its entries will be rewritten in order of their offset within the file.
            // This requires the entries to be sorted when the file is opened.
            byte[] sampleEntryContents = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19];
            byte[] sampleZipFile = ReverseCentralDirectoryEntries(await CreateZipFile(50, sampleEntryContents, async));

            using MemoryStream ms = new MemoryStream();

            ms.Write(sampleZipFile);
            ms.Seek(0, SeekOrigin.Begin);

            ZipArchive source = await CreateZipArchive(async, ms, ZipArchiveMode.Update, leaveOpen: true);

            long previousOffset = long.MinValue;
            FieldInfo offsetOfLocalHeader = typeof(ZipArchiveEntry).GetField("_offsetOfLocalHeader", BindingFlags.NonPublic | BindingFlags.Instance);

            for (int i = 0; i < source.Entries.Count; i++)
            {
                ZipArchiveEntry entry = source.Entries[i];
                long offset = (long)offsetOfLocalHeader.GetValue(entry);

                Assert.True(offset > previousOffset);
                previousOffset = offset;
            }

            await DisposeZipArchive(async, source);
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public static async Task ArchivesInCentralDirectoryOrder_ReadMode(bool async)
        {
            // When the ZipArchive is opened in Read mode, no sort is necessary. The entries will be added to the ZipArchive in the order
            // that they appear in the central directory (in this case, sorted by offset descending.)
            byte[] sampleEntryContents = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19];
            byte[] sampleZipFile = ReverseCentralDirectoryEntries(await CreateZipFile(50, sampleEntryContents, async));

            using MemoryStream ms = new MemoryStream();

            ms.Write(sampleZipFile);
            ms.Seek(0, SeekOrigin.Begin);

            ZipArchive source = await CreateZipArchive(async, ms, ZipArchiveMode.Read, true);

            long previousOffset = long.MaxValue;
            FieldInfo offsetOfLocalHeader = typeof(ZipArchiveEntry).GetField("_offsetOfLocalHeader", BindingFlags.NonPublic | BindingFlags.Instance);

            for (int i = 0; i < source.Entries.Count; i++)
            {
                ZipArchiveEntry entry = source.Entries[i];
                long offset = (long)offsetOfLocalHeader.GetValue(entry);

                Assert.True(offset < previousOffset);
                previousOffset = offset;
            }

            await DisposeZipArchive(async, source);
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public static async Task EntriesMalformed_InvalidDataException(bool async)
        {
            string entryName = "entry.txt";

            var stream = new MemoryStream();
            ZipArchive archiveWrite = await CreateZipArchive(async, stream, ZipArchiveMode.Create, true);
            archiveWrite.CreateEntry(entryName);
            await DisposeZipArchive(async, archiveWrite);

            stream.Position = 0;

            // Malform the archive
            ZipArchive archiveRead = await CreateZipArchive(async, stream, ZipArchiveMode.Read, true);

            var unused = archiveRead.Entries;

            // Read the last 22 bytes of stream to get the EOCD.
            byte[] buffer = new byte[22];
            stream.Seek(-22, SeekOrigin.End);
            stream.ReadExactly(buffer);

            var startCentralDir = (long)typeof(ZipArchive).GetField("_centralDirectoryStart", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(archiveRead);
            // Truncate to exactly 46 bytes after start.
            stream.SetLength(startCentralDir + 46);

            // Write the EOCD back.
            stream.Seek(-22, SeekOrigin.End);
            stream.Write(buffer);

            await DisposeZipArchive(async, archiveRead);

            stream.Position = 0;

            ZipArchive archive = new ZipArchive(stream);

            Assert.Throws<InvalidDataException>(() => _ = archive.Entries);
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public static async Task ReadStreamOps(bool async)
        {
            MemoryStream ms = await StreamHelpers.CreateTempCopyStream(zfile("normal.zip"));
            ZipArchive archive = await CreateZipArchive(async, ms, ZipArchiveMode.Read);

            // Helper method to get the compression method from the central directory
            static ushort GetCompressionMethod(ZipArchiveEntry entry)
            {
                // The CompressionMethod property is not public, so we parse the central directory header.
                // The ZipArchiveEntry has a private field _archive and _offsetOfLocalHeader, but we can't access them.
                // Instead, we can open the archive file and parse the central directory for the entry.
                // Since we have the entry's FullName, we can find the entry in the archive's central directory.
                // This helper assumes the archive is backed by a seekable stream.

                ZipArchive archive = entry.Archive;
                Stream baseStream = archive.Mode == ZipArchiveMode.Read
                    ? ((MemoryStream)archive.GetType().GetProperty("Stream", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(archive))
                    : null;
                if (baseStream == null)
                    throw new InvalidOperationException("Cannot access base stream of archive.");

                long originalPosition = baseStream.Position;
                try
                {
                    // Find the central directory by scanning from the end of the stream
                    baseStream.Seek(-22, SeekOrigin.End);
                    byte[] eocd = new byte[22];
                    baseStream.Read(eocd, 0, 22);
                    // Central directory offset is at offset 16 in EOCD
                    int centralDirOffset = BitConverter.ToInt32(eocd, 16);
                    baseStream.Seek(centralDirOffset, SeekOrigin.Begin);

                    // Scan central directory entries
                    while (true)
                    {
                        byte[] header = new byte[46];
                        int read = baseStream.Read(header, 0, 46);
                        if (read < 46)
                            break;
                        // Central directory file header signature
                        if (header[0] != 0x50 || header[1] != 0x4b || header[2] != 0x01 || header[3] != 0x02)
                            break;
                        ushort fileNameLength = BitConverter.ToUInt16(header, 28);
                        ushort extraFieldLength = BitConverter.ToUInt16(header, 30);
                        ushort fileCommentLength = BitConverter.ToUInt16(header, 32);
                        ushort compressionMethod = BitConverter.ToUInt16(header, 10);
                        byte[] fileNameBytes = new byte[fileNameLength];
                        baseStream.Read(fileNameBytes, 0, fileNameLength);
                        string fileName = System.Text.Encoding.UTF8.GetString(fileNameBytes);
                        // Compare with entry name
                        if (fileName == entry.FullName)
                            return compressionMethod;
                        // Skip extra field and file comment
                        baseStream.Seek(extraFieldLength + fileCommentLength, SeekOrigin.Current);
                    }
                    throw new InvalidOperationException("Entry not found in central directory.");
                }
                finally
                {
                    baseStream.Position = originalPosition;
                }
            }
            foreach (ZipArchiveEntry e in archive.Entries)
            {
                Stream s = await OpenEntryStream(async, e);

                Assert.True(s.CanRead, "Can read to read archive");
                Assert.False(s.CanWrite, "Can't write to read archive");
                
                // Check the entry's compression method to determine seekability
                // SubReadStream should be seekable when the underlying stream is seekable and the entry is stored (uncompressed)
                // If the entry is compressed (Deflate, Deflate64, etc.), it will be wrapped in a compression stream which is not seekable
                ushort compressionMethod = (ushort)compressionMethodField.GetValue(e);
                const ushort StoredCompressionMethod = CompressionMethodValues.Stored;
                
                if (compressionMethod == StoredCompressionMethod)
                {
                    // Entry is stored (uncompressed), should be seekable
                    Assert.True(s.CanSeek, $"SubReadStream should be seekable for stored (uncompressed) entry '{e.FullName}' with compression method {compressionMethod} when underlying stream is seekable");
                }
                else
                {
                    // Entry is compressed (Deflate, Deflate64, etc.), wrapped in compression stream, should not be seekable
                    Assert.False(s.CanSeek, $"Entry '{e.FullName}' with compression method {compressionMethod} should not be seekable because compressed entries are wrapped in non-seekable compression streams");
                }
                
                Assert.Equal(await LengthOfUnseekableStream(s), e.Length); //"Length is not correct on stream"

                await DisposeStream(async, s);
            }

            await DisposeZipArchive(async, archive);
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public static async Task ReadStreamSeekOps(bool async)
        {
            // Create a ZIP archive with stored (uncompressed) entries to test SubReadStream seekability
            using (var ms = new MemoryStream())
            {
                // Create a ZIP with stored entries
                using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, true))
                {
                    var entry = archive.CreateEntry("test.txt", CompressionLevel.NoCompression);
                    using (var stream = entry.Open())
                    {
                        var testData = "This is test data for seeking operations."u8.ToArray();
                        stream.Write(testData, 0, testData.Length);
                    }
                }

                ms.Position = 0;
                using (var archive = await CreateZipArchive(async, ms, ZipArchiveMode.Read))
                {
                    foreach (ZipArchiveEntry e in archive.Entries)
                    {
                        if (e.Length == 0) continue; // Skip empty entries for this test

                        Stream s = await OpenEntryStream(async, e);

                        // For stored entries, SubReadStream should be seekable when underlying stream is seekable
                        Assert.True(s.CanSeek, $"SubReadStream should be seekable for stored entry '{e.FullName}' when underlying stream is seekable");

                        // Test seeking to beginning
                        long pos = s.Seek(0, SeekOrigin.Begin);
                        Assert.Equal(0, pos);
                        Assert.Equal(0, s.Position);

                        // Test seeking to end
                        pos = s.Seek(0, SeekOrigin.End);
                        Assert.Equal(e.Length, pos);
                        Assert.Equal(e.Length, s.Position);

                        // Test seeking from current position
                        s.Position = 0;
                        if (e.Length > 1)
                        {
                            pos = s.Seek(1, SeekOrigin.Current);
                            Assert.Equal(1, pos);
                            Assert.Equal(1, s.Position);
                        }

                        // Test setting position directly
                        s.Position = 0;
                        Assert.Equal(0, s.Position);

                        // Test that seeking before beginning throws, but beyond end is allowed
                        Assert.Throws<ArgumentOutOfRangeException>(() => s.Position = -1);
                        Assert.Throws<IOException>(() => s.Seek(-1, SeekOrigin.Begin));
                        
                        // Seeking beyond end should be allowed (no exception)
                        s.Position = e.Length + 1;
                        Assert.Equal(e.Length + 1, s.Position);
                        s.Seek(1, SeekOrigin.End);
                        Assert.Equal(e.Length + 1, s.Position);

                        await DisposeStream(async, s);
                    }
                }
            }
        }

        [Theory]
        [MemberData(nameof(Get_BooleanCombinations_Data))]
        public static async Task ReadEntryContentTwice(bool async, bool useSeekMethod)
        {
            // Create a ZIP archive with stored (uncompressed) entries to test reading content twice
            using (var ms = new MemoryStream())
            {
                var testData = "This is test data for reading content twice with seeking operations."u8.ToArray();
                
                // Create a ZIP with stored entries
                using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, true))
                {
                    var entry = archive.CreateEntry("test.txt", CompressionLevel.NoCompression);
                    using (var stream = entry.Open())
                    {
                        stream.Write(testData, 0, testData.Length);
                    }
                }

                ms.Position = 0;
                using (var archive = await CreateZipArchive(async, ms, ZipArchiveMode.Read))
                {
                    foreach (ZipArchiveEntry e in archive.Entries)
                    {
                        if (e.Length == 0) continue; // Skip empty entries for this test

                        Stream s = await OpenEntryStream(async, e);

                        // For stored entries, SubReadStream should be seekable when underlying stream is seekable
                        Assert.True(s.CanSeek, $"SubReadStream should be seekable for stored entry '{e.FullName}' when underlying stream is seekable");

                        // Read content first time
                        byte[] firstRead = new byte[e.Length];
                        int bytesRead1 = s.Read(firstRead, 0, (int)e.Length);
                        Assert.Equal(e.Length, bytesRead1);

                        // Rewind to beginning using specified method
                        if (useSeekMethod)
                        {
                            long pos = s.Seek(0, SeekOrigin.Begin);
                            Assert.Equal(0, pos);
                        }
                        else
                        {
                            s.Position = 0;
                        }
                        Assert.Equal(0, s.Position);

                        // Read content second time
                        byte[] secondRead = new byte[e.Length];
                        int bytesRead2 = s.Read(secondRead, 0, (int)e.Length);
                        Assert.Equal(e.Length, bytesRead2);

                        // Compare the content - should be identical
                        Assert.Equal(firstRead, secondRead);
                        Assert.Equal(testData, firstRead);
                        Assert.Equal(testData, secondRead);

                        await DisposeStream(async, s);
                    }
                }
            }
        }

        private static byte[] ReverseCentralDirectoryEntries(byte[] zipFile)
        {
            byte[] destinationBuffer = new byte[zipFile.Length];

            // Inspect the "end of central directory" header. This is the final 22 bytes of the file, and it contains the offset and the size
            // of the central directory.
            int eocdHeaderOffset_CentralDirectoryPosition = zipFile.Length - 6;
            int eocdHeaderOffset_CentralDirectoryLength = zipFile.Length - 10;
            int centralDirectoryPosition = BinaryPrimitives.ReadInt32LittleEndian(zipFile.AsSpan(eocdHeaderOffset_CentralDirectoryPosition, sizeof(int)));
            int centralDirectoryLength = BinaryPrimitives.ReadInt32LittleEndian(zipFile.AsSpan(eocdHeaderOffset_CentralDirectoryLength, sizeof(int)));
            List<Range> centralDirectoryRanges = new List<Range>();

            Assert.True(centralDirectoryPosition + centralDirectoryLength < zipFile.Length);

            // With the starting position of the central directory in hand, work through each entry, recording its starting position and its length.
            for (int currPosition = centralDirectoryPosition; currPosition < centralDirectoryPosition + centralDirectoryLength;)
            {
                // The length of a central directory entry is determined by the length of its static components (46 bytes), plus the length of its filename
                // (offset 28), extra fields (offset 30) and file comment (offset 32).
                short filenameLength = BinaryPrimitives.ReadInt16LittleEndian(zipFile.AsSpan(currPosition + 28, sizeof(short)));
                short extraFieldLength = BinaryPrimitives.ReadInt16LittleEndian(zipFile.AsSpan(currPosition + 30, sizeof(short)));
                short fileCommentLength = BinaryPrimitives.ReadInt16LittleEndian(zipFile.AsSpan(currPosition + 32, sizeof(short)));
                int totalHeaderLength = 46 + filenameLength + extraFieldLength + fileCommentLength;

                // The sample data generated by the tests should never have extra fields and comments.
                Assert.True(filenameLength > 0);
                Assert.True(extraFieldLength == 0);
                Assert.True(fileCommentLength == 0);

                centralDirectoryRanges.Add(new Range(currPosition, currPosition + totalHeaderLength));
                currPosition += totalHeaderLength;
            }

            // Begin building the destination archive. The file contents (everything up to the central directory header) can be copied as-is.
            zipFile.AsSpan(0, centralDirectoryPosition).CopyTo(destinationBuffer);

            int cumulativeCentralDirectoryLength = 0;

            // Reverse the order of the central directory entries
            foreach (Range cdHeader in centralDirectoryRanges)
            {
                Span<byte> sourceSpan = zipFile.AsSpan(cdHeader);
                Span<byte> destSpan;

                cumulativeCentralDirectoryLength += sourceSpan.Length;
                Assert.True(cumulativeCentralDirectoryLength <= centralDirectoryLength);

                destSpan = destinationBuffer.AsSpan(centralDirectoryPosition + centralDirectoryLength - cumulativeCentralDirectoryLength, sourceSpan.Length);
                sourceSpan.CopyTo(destSpan);
            }

            Assert.Equal(centralDirectoryLength, cumulativeCentralDirectoryLength);
            Assert.Equal(22, destinationBuffer.Length - centralDirectoryPosition - centralDirectoryLength);

            // Copy the "end of central directory header" entry to the destination buffer.
            zipFile.AsSpan(zipFile.Length - 22).CopyTo(destinationBuffer.AsSpan(destinationBuffer.Length - 22));

            return destinationBuffer;
        }

        private class DisposeCallCountingStream : MemoryStream
        {
            public int NumberOfDisposeCalls { get; private set; }

            protected override void Dispose(bool disposing)
            {
                NumberOfDisposeCalls++;
                base.Dispose(disposing);
            }
        }
    }
}
