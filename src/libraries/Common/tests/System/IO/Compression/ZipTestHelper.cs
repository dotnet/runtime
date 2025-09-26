// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Compression.Tests
{
    public partial class ZipFileTestBase : FileCleanupTestBase
    {
        public static string bad(string filename) => Path.Combine("ZipTestData", "badzipfiles", filename);
        public static string compat(string filename) => Path.Combine("ZipTestData", "compat", filename);
        public static string strange(string filename) => Path.Combine("ZipTestData", "StrangeZipFiles", filename);
        public static string zfile(string filename) => Path.Combine("ZipTestData", "refzipfiles", filename);
        public static string zfolder(string filename) => Path.Combine("ZipTestData", "refzipfolders", filename);
        public static string zmodified(string filename) => Path.Combine("ZipTestData", "modified", filename);

        protected TempFile CreateTempCopyFile(string path, string newPath)
        {
            TempFile newfile = new TempFile(newPath);
            File.Copy(path, newPath, overwrite: true);
            return newfile;
        }

        public static async Task<long> LengthOfUnseekableStream(Stream s)
        {
            long totalBytes = 0;
            const int bufSize = 4096;
            byte[] buf = new byte[bufSize];
            long bytesRead = 0;

            do
            {
                bytesRead = await s.ReadAsync(buf, 0, bufSize);
                totalBytes += bytesRead;
            } while (bytesRead > 0);

            return totalBytes;
        }

        // reads exactly bytesToRead out of stream, unless it is out of bytes
        public static async Task ReadBytes(Stream stream, byte[] buffer, long bytesToRead, bool async)
        {
            int bytesLeftToRead;
            if (bytesToRead > int.MaxValue)
            {
                throw new NotImplementedException("64 bit addresses");
            }
            else
            {
                bytesLeftToRead = (int)bytesToRead;
            }
            int totalBytesRead = 0;

            while (bytesLeftToRead > 0)
            {
                int bytesRead = async ?
                    await stream.ReadAsync(buffer, totalBytesRead, bytesLeftToRead) :
                   stream.Read(buffer, totalBytesRead, bytesLeftToRead);

                if (bytesRead == 0) throw new IOException("Unexpected end of stream");

                totalBytesRead += bytesRead;
                bytesLeftToRead -= bytesRead;
            }
        }

        public static async Task<int> ReadAllBytesAsync(Stream stream, byte[] buffer, int offset, int count)
        {
            int bytesRead;
            int totalRead = 0;
            while ((bytesRead = await stream.ReadAsync(buffer, offset + totalRead, count - totalRead)) != 0)
            {
                totalRead += bytesRead;
            }
            return totalRead;
        }

        public static int ReadAllBytes(Stream stream, byte[] buffer, int offset, int count)
        {
            int bytesRead;
            int totalRead = 0;
            while ((bytesRead = stream.Read(buffer, offset + totalRead, count - totalRead)) != 0)
            {
                totalRead += bytesRead;
            }
            return totalRead;
        }

        public static bool ArraysEqual<T>(T[] a, T[] b) where T : IComparable<T>
        {
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i].CompareTo(b[i]) != 0) return false;
            }
            return true;
        }

        public static bool ArraysEqual<T>(T[] a, T[] b, int length) where T : IComparable<T>
        {
            for (int i = 0; i < length; i++)
            {
                if (a[i].CompareTo(b[i]) != 0) return false;
            }
            return true;
        }

        public static void StreamsEqual(Stream ast, Stream bst)
        {
            StreamsEqual(ast, bst, -1);
        }

        public static void StreamsEqual(Stream ast, Stream bst, int blocksToRead)
        {
            if (ast.CanSeek)
                ast.Seek(0, SeekOrigin.Begin);
            if (bst.CanSeek)
                bst.Seek(0, SeekOrigin.Begin);

            const int bufSize = 4096;
            byte[] ad = new byte[bufSize];
            byte[] bd = new byte[bufSize];

            int ac = 0;
            int bc = 0;

            int blocksRead = 0;

            //assume read doesn't do weird things
            do
            {
                if (blocksToRead != -1 && blocksRead >= blocksToRead)
                    break;

                ac = ReadAllBytes(ast, ad, 0, 4096);
                bc = ReadAllBytes(bst, bd, 0, 4096);

                if (ac != bc)
                {
                    bd = NormalizeLineEndings(bd);
                }

                Assert.True(ArraysEqual<byte>(ad, bd, ac), "Stream contents not equal: " + ast.ToString() + ", " + bst.ToString());

                blocksRead++;
            } while (ac == bufSize);
        }

        public static async Task StreamsEqualAsync(Stream ast, Stream bst, int blocksToRead)
        {
            if (ast.CanSeek)
                ast.Seek(0, SeekOrigin.Begin);
            if (bst.CanSeek)
                bst.Seek(0, SeekOrigin.Begin);

            const int bufSize = 4096;
            byte[] ad = new byte[bufSize];
            byte[] bd = new byte[bufSize];

            int ac = 0;
            int bc = 0;

            int blocksRead = 0;

            //assume read doesn't do weird things
            do
            {
                if (blocksToRead != -1 && blocksRead >= blocksToRead)
                    break;

                ac = await ast.ReadAtLeastAsync(ad, 4096, throwOnEndOfStream: false);
                bc = await bst.ReadAtLeastAsync(bd, 4096, throwOnEndOfStream: false);

                if (ac != bc)
                {
                    bd = NormalizeLineEndings(bd);
                }

                AssertExtensions.SequenceEqual(ad.AsSpan(0, ac), bd.AsSpan(0, bc));

                blocksRead++;
            } while (ac == bufSize);
        }

        public static async Task IsZipSameAsDir(string archiveFile, string directory, ZipArchiveMode mode, bool async) =>
            await IsZipSameAsDir(archiveFile, directory, mode, requireExplicit: false, checkTimes: false, async);

        public static async Task IsZipSameAsDir(string archiveFile, string directory, ZipArchiveMode mode, bool requireExplicit, bool checkTimes, bool async)
        {
            var s = await StreamHelpers.CreateTempCopyStream(archiveFile);
            await IsZipSameAsDir(s, directory, mode, requireExplicit, checkTimes, async);
        }

        public static byte[] NormalizeLineEndings(byte[] str)
        {
            string rep = Text.Encoding.Default.GetString(str);
            rep = rep.Replace("\r\n", "\n");
            rep = rep.Replace("\n", "\r\n");
            return Text.Encoding.Default.GetBytes(rep);
        }

        public static async Task IsZipSameAsDir(Stream archiveFile, string directory, ZipArchiveMode mode, bool requireExplicit, bool checkTimes, bool async)
        {
            ZipArchive archive = await CreateZipArchive(async, archiveFile, mode);

            List<FileData> files = FileData.InPath(directory);

            int count = 0;
            foreach (FileData file in files)
            {
                count++;
                string entryName = file.FullName;
                if (file.IsFolder)
                    entryName += Path.DirectorySeparatorChar;
                ZipArchiveEntry entry = archive.GetEntry(entryName);
                if (entry == null)
                {
                    entryName = FlipSlashes(entryName);
                    entry = archive.GetEntry(entryName);
                }
                if (file.IsFile)
                {
                    Assert.NotNull(entry);

                    // IMPORTANT: Call Length before opening the entry in Update mode
                    long givenLength = entry.Length;
                    var buffer = new byte[entry.Length];

                    Stream entryStream = await OpenEntryStream(async, entry);

                    ReadAllBytes(entryStream, buffer, 0, buffer.Length);
#if NET
                    uint zipcrc = entry.Crc32;
                    Assert.Equal(CRC.CalculateCRC(buffer), zipcrc);
#endif

                    if (file.Length != givenLength)
                    {
                        buffer = NormalizeLineEndings(buffer);
                    }

                    Assert.Equal(file.Length, buffer.Length);
                    ulong crc = CRC.CalculateCRC(buffer);
                    Assert.Equal(file.CRC, crc.ToString());

                    await DisposeStream(async, entryStream);

                    if (checkTimes)
                    {
                        const int zipTimestampResolution = 2; // Zip follows the FAT timestamp resolution of two seconds for file records
                        DateTime lower = file.LastModifiedDate.AddSeconds(-zipTimestampResolution);
                        DateTime upper = file.LastModifiedDate.AddSeconds(zipTimestampResolution);
                        Assert.InRange(entry.LastWriteTime.Ticks, lower.Ticks, upper.Ticks);
                    }

                    Assert.Equal(file.Name, entry.Name);
                    Assert.Equal(entryName, entry.FullName);
                    Assert.Equal(entryName, entry.ToString());
                    Assert.Equal(archive, entry.Archive);
                }
                else if (file.IsFolder)
                {
                    if (entry == null) //entry not found
                    {
                        string entryNameOtherSlash = FlipSlashes(entryName);
                        bool isEmpty = !files.Any(
                            f => f.IsFile &&
                                    (f.FullName.StartsWith(entryName, StringComparison.OrdinalIgnoreCase) ||
                                    f.FullName.StartsWith(entryNameOtherSlash, StringComparison.OrdinalIgnoreCase)));
                        if (requireExplicit || isEmpty)
                        {
                            Assert.Contains("emptydir", entryName);
                        }

                        if ((!requireExplicit && !isEmpty) || entryName.Contains("emptydir"))
                            count--; //discount this entry
                    }
                    else
                    {
                        Stream es = await OpenEntryStream(async, entry);

                        try
                        {
                            Assert.Equal(0, es.Length);
                        }
                        catch (NotSupportedException)
                        {
                            try
                            {
                                Assert.Equal(-1, es.ReadByte());
                            }
                            catch (Exception)
                            {
                                Console.WriteLine("Didn't return EOF");
                                throw;
                            }
                        }

                        await DisposeStream(async, es);
                    }
                }
            }
            Assert.Equal(count, archive.Entries.Count);

            await DisposeZipArchive(async, archive);
        }

        private static string FlipSlashes(string name)
        {
            Debug.Assert(!(name.Contains("\\") && name.Contains("/")));
            return
                name.Contains("\\") ? name.Replace("\\", "/") :
                name.Contains("/") ? name.Replace("/", "\\") :
                name;
        }

        public static FileStream CreateFileStreamRead(bool async, string fileName) =>
            new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: async);

        public static async Task DirsEqual(string actual, string expected)
        {
            var expectedList = FileData.InPath(expected);
            var actualList = Directory.GetFiles(actual, "*.*", SearchOption.AllDirectories);
            var actualFolders = Directory.GetDirectories(actual, "*.*", SearchOption.AllDirectories);
            var actualCount = actualList.Length + actualFolders.Length;
            Assert.Equal(expectedList.Count, actualCount);

            await ItemEqual(actualList, expectedList, isFile: true);
            await ItemEqual(actualFolders, expectedList, isFile: false);
        }

        public static void DirFileNamesEqual(string actual, string expected)
        {
            IEnumerable<string> actualEntries = Directory.EnumerateFileSystemEntries(actual, "*", SearchOption.AllDirectories).Select(Path.GetFileName);
            IEnumerable<string> expectedEntries = Directory.EnumerateFileSystemEntries(expected, "*", SearchOption.AllDirectories).Select(Path.GetFileName);

            // iOS/tvOS filesystems use NFD by default for non-ASCII characters, so we normalize to NFC for cross-platform comparison
            actualEntries = actualEntries.Select(name => name.Normalize(NormalizationForm.FormC)).Order();
            expectedEntries = expectedEntries.Select(name => name.Normalize(NormalizationForm.FormC)).Order();

            AssertExtensions.SequenceEqual(actualEntries.ToArray(), expectedEntries.ToArray());
        }

        private static async Task ItemEqual(string[] actualList, List<FileData> expectedList, bool isFile)
        {
            for (int i = 0; i < actualList.Length; i++)
            {
                var actualFile = actualList[i];
                string aEntry = Path.GetFullPath(actualFile);
                string aName = Path.GetFileName(aEntry);

                var bData = expectedList.Where(f => string.Equals(f.Name, aName, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                string bEntry = Path.GetFullPath(Path.Combine(bData.OrigFolder, bData.FullName));
                string bName = Path.GetFileName(bEntry);
                // expected 'emptydir' folder doesn't exist because MSBuild doesn't copy empty dir
                if (!isFile && aName.Contains("emptydir") && bName.Contains("emptydir"))
                    continue;

                //we want it to be false that one of them is a directory and the other isn't
                Assert.False(Directory.Exists(aEntry) ^ Directory.Exists(bEntry), "Directory in one is file in other");

                //contents same
                if (isFile)
                {
                    Stream sa = await StreamHelpers.CreateTempCopyStream(aEntry);
                    Stream sb = await StreamHelpers.CreateTempCopyStream(bEntry);
                    StreamsEqual(sa, sb); // Not testing zip features, can always be async
                }
            }
        }

        /// <param name="useSpansForWriting">Tests the Span and Memory overloads of Write and WriteAsync.</param>
        /// <param name="writeInChunks">Writes in chunks of 5 to test Write with a nonzero offset</param>
        public static async Task CreateFromDir(string directory, Stream archiveStream, bool async, ZipArchiveMode mode, bool useSpansForWriting = false, bool writeInChunks = false)
        {
            var files = FileData.InPath(directory);

            ZipArchive archive = await CreateZipArchive(async, archiveStream, mode, leaveOpen: true);

            foreach (var i in files)
            {
                if (i.IsFolder)
                {
                    string entryName = i.FullName;

                    ZipArchiveEntry e = archive.CreateEntry(entryName.Replace('\\', '/') + "/");
                    e.LastWriteTime = i.LastModifiedDate;
                }
            }

            foreach (var i in files)
            {
                if (i.IsFile)
                {
                    string entryName = i.FullName;

                    MemoryStream installStream = await StreamHelpers.CreateTempCopyStream(Path.Combine(i.OrigFolder, i.FullName));

                    if (installStream != null)
                    {
                        ZipArchiveEntry e = archive.CreateEntry(entryName.Replace('\\', '/'));
                        e.LastWriteTime = i.LastModifiedDate;

                        Stream entryStream = await OpenEntryStream(async, e);

                        int bytesRead;
                        var buffer = new byte[1024];
                        if (useSpansForWriting)
                        {
                            while ((bytesRead = await installStream.ReadAsync(buffer)) != 0)
                            {
                                if (async)
                                {
                                    await entryStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                                }
                                else
                                {
                                    entryStream.Write(buffer.AsSpan(0, bytesRead));
                                }
                            }
                        }
                        else if (writeInChunks)
                        {
                            while ((bytesRead = await installStream.ReadAsync(buffer, 0, buffer.Length)) != 0)
                            {
                                for (int k = 0; k < bytesRead; k += 5)
                                {
                                    int count = Math.Min(5, bytesRead - k);
                                    if (async)
                                    {
                                        await entryStream.WriteAsync(buffer, k, count);
                                    }
                                    else
                                    {
                                        entryStream.Write(buffer, k, count);
                                    }
                                }
                            }
                        }
                        else
                        {
                            while ((bytesRead = await installStream.ReadAsync(buffer, 0, buffer.Length)) != 0)
                            {
                                if (async)
                                {
                                    await entryStream.WriteAsync(buffer, 0, bytesRead);
                                }
                                else
                                {
                                    entryStream.Write(buffer, 0, bytesRead);
                                }
                            }
                        }

                        await DisposeStream(async, entryStream);
                    }
                }
            }

            await DisposeZipArchive(async, archive);
        }

        internal static async Task AddEntry(ZipArchive archive, string name, string contents, DateTimeOffset lastWrite, bool async)
        {
            ZipArchiveEntry e = archive.CreateEntry(name);
            e.LastWriteTime = lastWrite;

            Stream entryStream = await OpenEntryStream(async, e);

            // Leave the streamopen so we can test the entry stream's disposing
            StreamWriter w = new StreamWriter(entryStream, encoding: null, bufferSize: 2048, leaveOpen: true);
            await w.WriteLineAsync(contents);

            await DisposeStream(async, entryStream);
        }

        public static async Task<byte[]> CreateZipFile(int entryCount, byte[] entryContents, bool async)
        {
            MemoryStream ms = new();

            ZipArchive createdArchive = await CreateZipArchive(async, ms, ZipArchiveMode.Create, leaveOpen: true);

            for (int i = 0; i < entryCount; i++)
            {
                string fileName = $"dummydata/{i}.bin";
                ZipArchiveEntry newEntry = createdArchive.CreateEntry(fileName);

                newEntry.LastWriteTime = DateTimeOffset.Now.AddHours(-1.0);

                Stream entryWriteStream = await OpenEntryStream(async, newEntry);
                if (async)
                {
                    await entryWriteStream.WriteAsync(entryContents);
                }
                else
                {
                    entryWriteStream.Write(entryContents);
                }
                entryWriteStream.WriteByte((byte)(i % byte.MaxValue));

                await DisposeStream(async, entryWriteStream);
            }

            await DisposeZipArchive(async, createdArchive);

            await ms.FlushAsync();
            await ms.DisposeAsync();

            return ms.ToArray();
        }

        protected const string Utf8SmileyEmoji = "\ud83d\ude04";
        protected const string Utf8LowerCaseOUmlautChar = "\u00F6";
        protected const string Utf8CopyrightChar = "\u00A9";
        protected const string AsciiFileName = "file.txt";
        protected const string UnicodeFileName = "\u4f60\u597D.txt";
        // The o with umlaut is a character that exists in both latin1 and utf8
        protected const string Utf8AndLatin1FileName = $"{Utf8LowerCaseOUmlautChar}.txt";
        // emojis only make sense in utf8
        protected const string Utf8FileName = $"{Utf8SmileyEmoji}.txt";
        protected static readonly string ALettersUShortMaxValueMinusOne = new string('a', ushort.MaxValue - 1);
        protected static readonly string ALettersUShortMaxValue = ALettersUShortMaxValueMinusOne + 'a';
        protected static readonly string ALettersUShortMaxValueMinusOneAndCopyRightChar = ALettersUShortMaxValueMinusOne + Utf8CopyrightChar;
        protected static readonly string ALettersUShortMaxValueMinusOneAndTwoCopyRightChars = ALettersUShortMaxValueMinusOneAndCopyRightChar + Utf8CopyrightChar;

        protected static readonly bool[] _bools = [false, true];

        public static async Task<ZipArchive> CreateZipArchive(bool async, Stream stream, ZipArchiveMode mode, bool leaveOpen = false, Encoding entryNameEncoding = null)
        {
            return async ?
                await ZipArchive.CreateAsync(stream, mode, leaveOpen, entryNameEncoding) :
                new ZipArchive(stream, mode, leaveOpen, entryNameEncoding);
        }

        public static async Task DisposeZipArchive(bool async, ZipArchive archive)
        {
            if (async)
            {
                await archive.DisposeAsync();
            }
            else
            {
                archive.Dispose();
            }
        }

        public static async Task<Stream> OpenEntryStream(bool async, ZipArchiveEntry entry)
        {
            return async ? await entry.OpenAsync() : entry.Open();
        }

        public static async Task DisposeStream(bool async, Stream stream)
        {
            if (async)
            {
                await stream.DisposeAsync();
            }
            else
            {
                stream.Dispose();
            }
        }
        public static IEnumerable<object[]> Get_Booleans_Data() => _bools.Select(b => new object[] { b });

        // Returns pairs that are returned the same way by Utf8 and Latin1
        // Returns: originalComment, expectedComment
        private static IEnumerable<object[]> SharedComment_Data()
        {
            foreach (bool async in _bools)
            {
                yield return new object[] { null, string.Empty, async };
                yield return new object[] { string.Empty, string.Empty, async };
                yield return new object[] { "a", "a", async };
                yield return new object[] { Utf8LowerCaseOUmlautChar, Utf8LowerCaseOUmlautChar, async };
            }
        }

        // Returns pairs as expected by Utf8
        // Returns: originalComment, expectedComment
        public static IEnumerable<object[]> Utf8Comment_Data()
        {
            string asciiOriginalOverMaxLength = ALettersUShortMaxValue + "aaa";

            // A smiley emoji code point consists of two characters,
            // meaning the whole emoji should be fully truncated
            string utf8OriginalALettersAndOneEmojiDoesNotFit = ALettersUShortMaxValueMinusOne + Utf8SmileyEmoji;

            // A smiley emoji code point consists of two characters,
            // so it should not be truncated if it's the last character and the total length is not over the limit.
            string utf8OriginalALettersAndOneEmojiFits = "aaaaa" + Utf8SmileyEmoji;

            foreach (bool async in _bools)
            {
                yield return new object[] { asciiOriginalOverMaxLength, ALettersUShortMaxValue, async };
                yield return new object[] { utf8OriginalALettersAndOneEmojiDoesNotFit, ALettersUShortMaxValueMinusOne, async };
                yield return new object[] { utf8OriginalALettersAndOneEmojiFits, utf8OriginalALettersAndOneEmojiFits, async };
            }

            foreach (object[] e in SharedComment_Data())
            {
                yield return e;
            }
        }

        // Returns pairs as expected by Latin1
        // Returns: originalComment, expectedComment
        public static IEnumerable<object[]> Latin1Comment_Data()
        {
            // In Latin1, all characters are exactly 1 byte

            string latin1ExpectedALettersAndOneOUmlaut = ALettersUShortMaxValueMinusOne + Utf8LowerCaseOUmlautChar;
            string latin1OriginalALettersAndTwoOUmlauts = latin1ExpectedALettersAndOneOUmlaut + Utf8LowerCaseOUmlautChar;

            foreach (bool async in _bools)
            {
                yield return new object[] { latin1OriginalALettersAndTwoOUmlauts, latin1ExpectedALettersAndOneOUmlaut, async };
            }

            foreach (object[] e in SharedComment_Data())
            {
                yield return e;
            }
        }

        // Returns pairs encoded with Latin1, but decoded with UTF8.
        // Returns: originalComment, expectedComment, transcoded expectedComment
        public static IEnumerable<object[]> MismatchingEncodingComment_Data()
        {
            foreach (bool async in _bools)
            {
                foreach (object[] e in Latin1Comment_Data())
                {
                    byte[] expectedBytes = Encoding.Latin1.GetBytes(e[1] as string);

                    yield return new object[] { e[0], e[1], Encoding.UTF8.GetString(expectedBytes), async };
                }
            }
        }
    }
}
