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
    public partial class zip_CreateTests : ZipFileTestBase
    {
        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public static async Task CreateModeInvalidOperations(bool async)
        {
            MemoryStream ms = new MemoryStream();

            ZipArchive z = await CreateZipArchive(async, ms, ZipArchiveMode.Create);

            Assert.Throws<NotSupportedException>(() => { var x = z.Entries; }); //"Entries not applicable on Create"
            Assert.Throws<NotSupportedException>(() => z.GetEntry("dirka")); //"GetEntry not applicable on Create"

            ZipArchiveEntry e = z.CreateEntry("hey");
            Assert.Throws<NotSupportedException>(() => e.Delete()); //"Can't delete new entry"

            Stream s = await OpenEntryStream(async, e);
            Assert.Throws<NotSupportedException>(() => s.ReadByte()); //"Can't read on new entry"
            Assert.Throws<NotSupportedException>(() => s.Seek(0, SeekOrigin.Begin)); //"Can't seek on new entry"
            Assert.Throws<NotSupportedException>(() => s.Position = 0); //"Can't set position on new entry"
            Assert.Throws<NotSupportedException>(() => { var x = s.Length; }); //"Can't get length on new entry"

            Assert.Throws<IOException>(() => e.LastWriteTime = new DateTimeOffset()); //"Can't get LastWriteTime on new entry"
            Assert.Throws<InvalidOperationException>(() => { var x = e.Length; }); //"Can't get length on new entry"
            Assert.Throws<InvalidOperationException>(() => { var x = e.CompressedLength; }); //"can't get CompressedLength on new entry"

            Assert.Throws<IOException>(() => z.CreateEntry("bad"));

            await DisposeStream(async, s);

            Assert.Throws<ObjectDisposedException>(() => s.WriteByte(25)); //"Can't write to disposed entry"

            await Assert.ThrowsAsync<IOException>(() => OpenEntryStream(async, e));

            Assert.Throws<IOException>(() => e.LastWriteTime = new DateTimeOffset());
            Assert.Throws<InvalidOperationException>(() => { var x = e.Length; });
            Assert.Throws<InvalidOperationException>(() => { var x = e.CompressedLength; });

            ZipArchiveEntry e1 = z.CreateEntry("e1");
            ZipArchiveEntry e2 = z.CreateEntry("e2");

            // Can't open previous entry after new entry created
            await Assert.ThrowsAsync<IOException>(() => OpenEntryStream(async, e1));

            await DisposeZipArchive(async, z);

            Assert.Throws<ObjectDisposedException>(() => z.CreateEntry("dirka")); //"Can't create after dispose"
        }

        private static readonly string[] _folderNames = ["small", "normal", "empty", "emptydir"];

        public static IEnumerable<object[]> GetCreateNormal_Seekable_Data()
        {
            foreach (string folder in _folderNames)
            {
                yield return new object[] { folder, false, false, };
            }

            yield return new object[] { "small", false, true };
            yield return new object[] { "small", true, false };
            yield return new object[] { "normal", false, true };
            yield return new object[] { "normal", true, false };
        }

        public static IEnumerable<object[]> GetCreateNormal_Seekable_Async_Data()
        {
            foreach (bool async in _bools)
            {
                foreach (object[] data in GetCreateNormal_Seekable_Data())
                {
                    string folder = (string)data[0];
                    bool useSpansForWriting = (bool)data[1];
                    bool writeInChunks = (bool)data[2];
                    yield return new object[] { folder, useSpansForWriting, writeInChunks, async };
                }
            }
        }

        [Theory]
        [MemberData(nameof(GetCreateNormal_Seekable_Async_Data))]
        public static async Task CreateNormal_Seekable(string folder, bool useSpansForWriting, bool writeInChunks, bool async)
        {
            using (var s = new MemoryStream())
            {
                var testStream = new WrappedStream(s, false, true, true, null);
                await CreateFromDir(zfolder(folder), testStream, async, ZipArchiveMode.Create, useSpansForWriting: useSpansForWriting, writeInChunks: writeInChunks);
                await IsZipSameAsDir(s, zfolder(folder), ZipArchiveMode.Read, requireExplicit: true, checkTimes: true, async: async);
            }
        }

        [Theory]
        [MemberData(nameof(GetCreateNormal_Seekable_Data))]
        public static async Task CreateNormal_Seekable_CompareSyncAndAsync(string folder, bool useSpansForWriting, bool writeInChunks)
        {
            using var s_sync = new MemoryStream();
            using var s_async = new MemoryStream();

            var testStream_sync = new WrappedStream(s_sync, false, true, true, null);
            await CreateFromDir(zfolder(folder), testStream_sync, async: false, ZipArchiveMode.Create, useSpansForWriting: useSpansForWriting, writeInChunks: writeInChunks);

            var testStream_async = new WrappedStream(s_async, false, true, true, null);
            await CreateFromDir(zfolder(folder), testStream_async, async: true, ZipArchiveMode.Create, useSpansForWriting: useSpansForWriting, writeInChunks: writeInChunks);

            s_sync.Position = 0;
            s_async.Position = 0;

            Assert.Equal(s_sync.ToArray(), s_async.ToArray());
        }

        public static IEnumerable<object[]> Get_FolderNames_Data()
        {
            foreach (string folder in _folderNames)
            {
                yield return new object[] { folder };
            }
        }

        public static IEnumerable<object[]> Get_CreateNormal_Unseekable_Data()
        {
            foreach (string folder in _folderNames)
            {
                yield return new object[] { folder, false };
                yield return new object[] { folder, true };
            }
        }

        [Theory]
        [MemberData(nameof(Get_CreateNormal_Unseekable_Data))]
        public static async Task CreateNormal_Unseekable(string folder, bool async)
        {
            using (var s = new MemoryStream())
            {
                var testStream = new WrappedStream(s, false, true, false, null);
                await CreateFromDir(zfolder(folder), testStream, async, ZipArchiveMode.Create);
                await IsZipSameAsDir(s, zfolder(folder), ZipArchiveMode.Read, requireExplicit: true, checkTimes: true, async);
            }
        }

        [Theory]
        [MemberData(nameof(Get_FolderNames_Data))]
        public static async Task CreateNormal_Unseekable_CompareSyncAndAsync(string folder)
        {
            using var s_sync = new MemoryStream();
            using var s_async = new MemoryStream();

            var testStream_sync = new WrappedStream(s_sync, false, true, canSeek: false, null);
            await CreateFromDir(zfolder(folder), testStream_sync, async: false, ZipArchiveMode.Create);

            var testStream_async = new WrappedStream(s_async, false, true, canSeek: false, null);
            await CreateFromDir(zfolder(folder), testStream_async, async: true, ZipArchiveMode.Create);

            s_sync.Position = 0;
            s_async.Position = 0;

            Assert.Equal(s_sync.ToArray(), s_async.ToArray());
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public static async Task CreateNormal_Unicode_Seekable(bool async)
        {
            using (var s = new MemoryStream())
            {
                var testStream = new WrappedStream(s, false, true, true, null);
                await CreateFromDir(zfolder("unicode"), testStream, async, ZipArchiveMode.Create);
                await IsZipSameAsDir(s, zfolder("unicode"), ZipArchiveMode.Read, requireExplicit: true, checkTimes: true, async);
            }
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public static async Task CreateNormal_Unicode_Unseekable(bool async)
        {
            using (var s = new MemoryStream())
            {
                var testStream = new WrappedStream(s, false, true, false, null);
                await CreateFromDir(zfolder("unicode"), testStream, async, ZipArchiveMode.Create);
                await IsZipSameAsDir(s, zfolder("unicode"), ZipArchiveMode.Read, requireExplicit: true, checkTimes: true, async);
            }
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public static async Task CreateUncompressedArchive(bool async)
        {
            using (var testStream = new MemoryStream())
            {
                var testfilename = "testfile";
                var testFileContent = "Lorem ipsum dolor sit amet, consectetur adipiscing elit.";

                ZipArchive zip = await CreateZipArchive(async, testStream, ZipArchiveMode.Create);

                var utf8WithoutBom = new Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
                ZipArchiveEntry newEntry = zip.CreateEntry(testfilename, CompressionLevel.NoCompression);

                Stream entryStream = await OpenEntryStream(async, newEntry);
                using (var writer = new StreamWriter(entryStream, utf8WithoutBom))
                {
                    writer.Write(testFileContent);
                    writer.Flush();
                }

                byte[] fileContent = testStream.ToArray();
                // zip file header stores values as little-endian
                byte compressionMethod = fileContent[8];
                Assert.Equal(0, compressionMethod); // stored => 0, deflate => 8
                uint compressedSize = BitConverter.ToUInt32(fileContent, 18);
                uint uncompressedSize = BitConverter.ToUInt32(fileContent, 22);
                Assert.Equal(uncompressedSize, compressedSize);
                byte filenamelength = fileContent[26];
                Assert.Equal(testfilename.Length, filenamelength);
                string readFileName = ReadStringFromSpan(fileContent.AsSpan(30, filenamelength));
                Assert.Equal(testfilename, readFileName);
                string readFileContent = ReadStringFromSpan(fileContent.AsSpan(30 + filenamelength, testFileContent.Length));
                Assert.Equal(testFileContent, readFileContent);

                await DisposeZipArchive(async, zip);
            }
        }

        public static IEnumerable<object[]> Get_CreateArchiveEntriesWithBitFlags_Data()
        {
            foreach (bool async in _bools)
            {
                yield return new object[] { CompressionLevel.NoCompression, 0, async };
                yield return new object[] { CompressionLevel.Optimal, 0, async };
                yield return new object[] { CompressionLevel.SmallestSize, 2, async };
                yield return new object[] { CompressionLevel.Fastest, 6, async };
            }
        }

        // This test checks to ensure that setting the compression level of an archive entry sets the general-purpose
        // bit flags correctly. It verifies that these have been set by reading from the MemoryStream manually, and by
        // reopening the generated file to confirm that the compression levels match.
        [Theory]
        // Special-case NoCompression: in this case, the CompressionMethod becomes Stored and the bits are unset.
        [MemberData(nameof(Get_CreateArchiveEntriesWithBitFlags_Data))]
        public static async Task CreateArchiveEntriesWithBitFlags(CompressionLevel compressionLevel, ushort expectedGeneralBitFlags, bool async)
        {
            var testfilename = "testfile";
            var testFileContent = "Lorem ipsum dolor sit amet, consectetur adipiscing elit.";
            var utf8WithoutBom = new Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

            byte[] zipFileContent;

            using (var testStream = new MemoryStream())
            {
                ZipArchive zip = await CreateZipArchive(async, testStream, ZipArchiveMode.Create);

                ZipArchiveEntry newEntry = zip.CreateEntry(testfilename, compressionLevel);

                Stream entryStream = await OpenEntryStream(async, newEntry);
                using (var writer = new StreamWriter(entryStream, utf8WithoutBom))
                {
                    writer.Write(testFileContent);
                    writer.Flush();
                }

                ZipArchiveEntry secondNewEntry = zip.CreateEntry(testFileContent + "_post", CompressionLevel.NoCompression);

                await DisposeZipArchive(async, zip);

                zipFileContent = testStream.ToArray();
            }

            // expected bit flags are at position 6 in the file header
            var generalBitFlags = BinaryPrimitives.ReadUInt16LittleEndian(zipFileContent.AsSpan(6));

            Assert.Equal(expectedGeneralBitFlags, generalBitFlags);

            using (var reReadStream = new MemoryStream(zipFileContent))
            {
                ZipArchive reReadZip = await CreateZipArchive(async, reReadStream, ZipArchiveMode.Read);

                var firstArchive = reReadZip.Entries[0];
                var secondArchive = reReadZip.Entries[1];
                var compressionLevelFieldInfo = typeof(ZipArchiveEntry).GetField("_compressionLevel", Reflection.BindingFlags.NonPublic | Reflection.BindingFlags.Instance);
                var generalBitFlagsFieldInfo = typeof(ZipArchiveEntry).GetField("_generalPurposeBitFlag", Reflection.BindingFlags.NonPublic | Reflection.BindingFlags.Instance);

                var reReadCompressionLevel = (CompressionLevel)compressionLevelFieldInfo.GetValue(firstArchive);
                var reReadGeneralBitFlags = (ushort)generalBitFlagsFieldInfo.GetValue(firstArchive);

                Assert.Equal(compressionLevel, reReadCompressionLevel);
                Assert.Equal(expectedGeneralBitFlags, reReadGeneralBitFlags);

                reReadCompressionLevel = (CompressionLevel)compressionLevelFieldInfo.GetValue(secondArchive);
                Assert.Equal(CompressionLevel.NoCompression, reReadCompressionLevel);

                Stream entryStream = await OpenEntryStream(async, firstArchive);
                var readBuffer = new byte[firstArchive.Length];
                entryStream.Read(readBuffer);
                var readText = Text.Encoding.UTF8.GetString(readBuffer);
                Assert.Equal(readText, testFileContent);
                await DisposeStream(async, entryStream);

                await DisposeZipArchive(async, reReadZip);
            }
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public static async Task CreateNormal_VerifyDataDescriptor(bool async)
        {
            using var memoryStream = new MemoryStream();
            // We need an non-seekable stream so the data descriptor bit is turned on when saving
            var wrappedStream = new WrappedStream(memoryStream, true, true, false, null);

            // Creation will go through the path that sets the data descriptor bit when the stream is unseekable
            ZipArchive archive = await CreateZipArchive(async, wrappedStream, ZipArchiveMode.Create);

            CreateEntry(archive, "A", "xxx");
            CreateEntry(archive, "B", "yyy");

            await DisposeZipArchive(async, archive);

            AssertDataDescriptor(memoryStream, true);

            // Update should flip the data descriptor bit to zero on save
            archive = await CreateZipArchive(async, memoryStream, ZipArchiveMode.Update);

            ZipArchiveEntry entry = archive.Entries[0];
            Stream entryStream = await OpenEntryStream(async, entry);
            StreamReader reader = new StreamReader(entryStream);
            string content = reader.ReadToEnd();

            // Append a string to this entry
            entryStream.Seek(0, SeekOrigin.End);
            StreamWriter writer = new StreamWriter(entryStream);
            writer.Write("zzz");
            writer.Flush();

            await DisposeStream(async, entryStream);

            await DisposeZipArchive(async, archive);

            AssertDataDescriptor(memoryStream, false);
        }

        public static IEnumerable<object[]> Get_CreateNormal_VerifyUnicodeFileNameAndComment_Data()
        {
            foreach (bool async in _bools)
            {
                yield return new object[] { UnicodeFileName, UnicodeFileName, true, async };
                yield return new object[] { UnicodeFileName, AsciiFileName, true, async };
                yield return new object[] { AsciiFileName, UnicodeFileName, true, async };
                yield return new object[] { AsciiFileName, AsciiFileName, false, async };
            }
        }

        [Theory]
        [MemberData(nameof(Get_CreateNormal_VerifyUnicodeFileNameAndComment_Data))]
        public static async Task CreateNormal_VerifyUnicodeFileNameAndComment(string fileName, string entryComment, bool isUnicodeFlagExpected, bool async)
        {
            using var ms = new MemoryStream();
            ZipArchive archive = await CreateZipArchive(async, ms, ZipArchiveMode.Create);
            CreateEntry(archive, fileName, fileContents: "xxx", entryComment);
            AssertUnicodeFileNameAndComment(ms, isUnicodeFlagExpected);
            await DisposeZipArchive(async, archive);
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task Create_VerifyDuplicateEntriesAreAllowed(bool async)
        {
            using var ms = new MemoryStream();
            ZipArchive archive = await CreateZipArchive(async, ms, ZipArchiveMode.Create, leaveOpen: true);

            string entryName = "foo";
            await AddEntry(archive, entryName, contents: "xxx", DateTimeOffset.Now, async);
            await AddEntry(archive, entryName, contents: "yyy", DateTimeOffset.Now, async);

            await DisposeZipArchive(async, archive);

            archive = await CreateZipArchive(async, ms, ZipArchiveMode.Update);

            Assert.Equal(2, archive.Entries.Count);

            await DisposeZipArchive(async, archive);

        }

        private static string ReadStringFromSpan(Span<byte> input)
        {
            return Text.Encoding.UTF8.GetString(input.ToArray());
        }

        private static void CreateEntry(ZipArchive archive, string fileName, string fileContents, string entryComment = null)
        {
            ZipArchiveEntry entry = archive.CreateEntry(fileName);
            using StreamWriter writer = new StreamWriter(entry.Open());
            writer.Write(fileContents);
            entry.Comment = entryComment;
        }

        private static void AssertDataDescriptor(MemoryStream memoryStream, bool hasDataDescriptor)
        {
            byte[] fileBytes = memoryStream.ToArray();
            Assert.Equal(hasDataDescriptor ? 8 : 0, fileBytes[6]);
            Assert.Equal(0, fileBytes[7]);
        }

        private static void AssertUnicodeFileNameAndComment(MemoryStream memoryStream, bool isUnicodeFlagExpected)
        {
            byte[] fileBytes = memoryStream.ToArray();
            Assert.Equal(0, fileBytes[6]);
            Assert.Equal(isUnicodeFlagExpected ? 8 : 0, fileBytes[7]);
        }

        [Fact]
        public void CreateEntry_NormalizesPaths()
        {
            var sourceContent = "file content";
            var sourceBytes = Encoding.UTF8.GetBytes(sourceContent);

            using (var zipStream = new MemoryStream())
            {
                using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: true))
                {
                    var entry1 = archive.CreateEntry(@"dir1\dir2\file.txt");
                    using (var entryStream = entry1.Open())
                    using (var sourceStream = new MemoryStream(sourceBytes))
                    {
                        sourceStream.CopyTo(entryStream);
                    }

                    var entry2 = archive.CreateEntry("dirA/dirB/file.txt");
                    using (var entryStream = entry2.Open())
                    using (var sourceStream = new MemoryStream(sourceBytes))
                    {
                        sourceStream.CopyTo(entryStream);
                    }

                    var entry3 = archive.CreateEntry(@"dirX/dirY\file.txt");
                    using (var entryStream = entry3.Open())
                    using (var sourceStream = new MemoryStream(sourceBytes))
                    {
                        sourceStream.CopyTo(entryStream);
                    }
                }

                zipStream.Seek(0, SeekOrigin.Begin);
                using var archiveRead = new ZipArchive(zipStream, ZipArchiveMode.Read);
                var entries = archiveRead.Entries.Select(e => e.FullName).ToList();

                Assert.Contains("dir1/dir2/file.txt", entries);
                Assert.Contains("dirA/dirB/file.txt", entries);
                Assert.Contains("dirX/dirY/file.txt", entries);

                Assert.DoesNotContain(@"dir1\dir2\file.txt", entries);
                Assert.DoesNotContain(@"dirX/dirY\file.txt", entries);
            }
        }

        [Fact]
        public void CreateEntryFromFile_NormalizesPaths()
        {
            var sourceFile = Path.GetTempFileName();
            try
            {
                File.WriteAllText(sourceFile, "This is a test file content.");

                using var zipStream = new MemoryStream();

                using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: true))
                {
                    archive.CreateEntryFromFile(sourceFile, @"dir1\dir2\file.txt");

                    archive.CreateEntryFromFile(sourceFile, "dirA/dirB/file.txt");

                    archive.CreateEntryFromFile(sourceFile, @"dirX/dirY\file.txt");
                }

                zipStream.Seek(0, SeekOrigin.Begin);
                using var archiveRead = new ZipArchive(zipStream, ZipArchiveMode.Read);
                var entries = archiveRead.Entries.Select(e => e.FullName).ToList();

                Assert.Contains("dir1/dir2/file.txt", entries);
                Assert.Contains("dirA/dirB/file.txt", entries);
                Assert.Contains("dirX/dirY/file.txt", entries);

                Assert.DoesNotContain(@"dir1\dir2\file.txt", entries);
                Assert.DoesNotContain(@"dirX/dirY\file.txt", entries);
            }
            finally
            {
                if (File.Exists(sourceFile))
                {
                    File.Delete(sourceFile);
                }
            }
        }
    }
}
