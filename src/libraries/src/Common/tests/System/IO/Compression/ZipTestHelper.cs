// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Compression.Tests
{
    public partial class ZipFileTestBase : FileCleanupTestBase
    {
        #region filename helpers

        public static String bad(String filename) { return Path.Combine("ZipTestData", "badzipfiles", filename); }
        public static String compat(String filename) { return Path.Combine("ZipTestData", "compat", filename); }
        public static String strange(String filename) { return Path.Combine("ZipTestData", "StrangeZipFiles", filename); }
        public static String zfile(String filename) { return Path.Combine("ZipTestData", "refzipfiles", filename); }
        public static String zfolder(String filename) { return Path.Combine("ZipTestData", "refzipfolders", filename); }
        public static String zmodified(String filename) { return Path.Combine("ZipTestData", "modified", filename); }

        #endregion

        #region helpers

        protected TempFile CreateTempCopyFile(string path, string newPath)
        {
            TempFile newfile = new TempFile(newPath);
            File.Copy(path, newPath, overwrite: true);
            return newfile;
        }

        public static Int64 LengthOfUnseekableStream(Stream s)
        {
            Int64 totalBytes = 0;
            const Int32 bufSize = 4096;
            Byte[] buf = new Byte[bufSize];
            Int64 bytesRead = 0;

            do
            {
                bytesRead = s.Read(buf, 0, bufSize);
                totalBytes += bytesRead;
            } while (bytesRead > 0);

            return totalBytes;
        }

        //reads exactly bytesToRead out of stream, unless it is out of bytes
        public static void ReadBytes(Stream stream, Byte[] buffer, Int64 bytesToRead)
        {
            Int32 bytesLeftToRead;
            if (bytesToRead > Int32.MaxValue)
            {
                throw new NotImplementedException("64 bit addresses");
            }
            else
            {
                bytesLeftToRead = (Int32)bytesToRead;
            }
            Int32 totalBytesRead = 0;

            while (bytesLeftToRead > 0)
            {
                Int32 bytesRead = stream.Read(buffer, totalBytesRead, bytesLeftToRead);
                if (bytesRead == 0) throw new IOException("Unexpected end of stream");

                totalBytesRead += bytesRead;
                bytesLeftToRead -= bytesRead;
            }
        }

        public static bool ArraysEqual<T>(T[] a, T[] b) where T : IComparable<T>
        {
            if (a.Length != b.Length) return false;
            for (Int32 i = 0; i < a.Length; i++)
            {
                if (a[i].CompareTo(b[i]) != 0) return false;
            }
            return true;
        }

        public static bool ArraysEqual<T>(T[] a, T[] b, Int32 length) where T : IComparable<T>
        {
            for (Int32 i = 0; i < length; i++)
            {
                if (a[i].CompareTo(b[i]) != 0) return false;
            }
            return true;
        }

        public static void StreamsEqual(Stream ast, Stream bst)
        {
            StreamsEqual(ast, bst, -1);
        }

        public static void StreamsEqual(Stream ast, Stream bst, Int32 blocksToRead)
        {
            if (ast.CanSeek)
                ast.Seek(0, SeekOrigin.Begin);
            if (bst.CanSeek)
                bst.Seek(0, SeekOrigin.Begin);

            const Int32 bufSize = 4096;
            Byte[] ad = new Byte[bufSize];
            Byte[] bd = new Byte[bufSize];

            Int32 ac = 0;
            Int32 bc = 0;

            Int32 blocksRead = 0;

            //assume read doesn't do weird things
            do
            {
                if (blocksToRead != -1 && blocksRead >= blocksToRead)
                    break;

                ac = ast.Read(ad, 0, 4096);
                bc = bst.Read(bd, 0, 4096);

                Assert.Equal(ac, bc);
                Assert.True(ArraysEqual<Byte>(ad, bd, ac), "Stream contents not equal: " + ast.ToString() + ", " + bst.ToString());

                blocksRead++;
            } while (ac == 4096);
        }

        #endregion

        #region "Validation"

        public static async Task IsZipSameAsDirAsync(String archiveFile, String directory, ZipArchiveMode mode)
        {
            await IsZipSameAsDirAsync(archiveFile, directory, mode, false, false);
        }

        public static async Task IsZipSameAsDirAsync(String archiveFile, String directory, ZipArchiveMode mode, bool requireExplicit, bool checkTimes)
        {
            var s = await StreamHelpers.CreateTempCopyStream(archiveFile);
            IsZipSameAsDir(s, directory, mode, requireExplicit, checkTimes);
        }

        public static void IsZipSameAsDir(Stream archiveFile, String directory, ZipArchiveMode mode, bool requireExplicit, bool checkTimes)
        {
            int count = 0;

            using (ZipArchive archive = new ZipArchive(archiveFile, mode))
            {
                List<FileData> files = FileData.InPath(directory);
                Assert.All<FileData>(files, (file) => {
                    count++;
                    String entryName = file.FullName;
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
                        Int64 givenLength = entry.Length;

                        var buffer = new byte[entry.Length];
                        using (Stream entrystream = entry.Open())
                        {
                            entrystream.Read(buffer, 0, buffer.Length);
                            String crc = CRC.CalculateCRC(buffer);
                            Assert.Equal(file.Length, givenLength);
                            Assert.Equal(file.CRC, crc);
                        }

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
                            bool isEmtpy = !files.Any(
                                f => f.IsFile &&
                                     (f.FullName.StartsWith(entryName, StringComparison.OrdinalIgnoreCase) ||
                                      f.FullName.StartsWith(entryNameOtherSlash, StringComparison.OrdinalIgnoreCase)));
                            if (requireExplicit || isEmtpy)
                            {
                                Assert.Contains("emptydir", entryName);
                            }

                            if ((!requireExplicit && !isEmtpy) || entryName.Contains("emptydir"))
                                count--; //discount this entry
                        }
                        else
                        {
                            using (Stream es = entry.Open())
                            {
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
                            }
                        }
                    }
                });
                Assert.Equal(count, archive.Entries.Count);
            }
        }

        private static string FlipSlashes(string name)
        {
            Debug.Assert(!(name.Contains("\\") && name.Contains("/")));
            return
                name.Contains("\\") ? name.Replace("\\", "/") :
                name.Contains("/") ? name.Replace("/", "\\") :
                name;
        }

        public static void DirsEqual(String actual, String expected)
        {
            var expectedList = FileData.InPath(expected);
            var actualList = Directory.GetFiles(actual, "*.*", SearchOption.AllDirectories);
            var actualFolders = Directory.GetDirectories(actual, "*.*", SearchOption.AllDirectories);
            var actualCount = actualList.Length + actualFolders.Length;
            Assert.Equal(expectedList.Count, actualCount);

            ItemEqual(actualList, expectedList, true);
            ItemEqual(actualFolders, expectedList, false);
        }

        public static void DirFileNamesEqual(string actual, string expected)
        {
            IEnumerable<string> actualEntries = Directory.EnumerateFileSystemEntries(actual, "*", SearchOption.AllDirectories);
            IEnumerable<string> expectedEntries = Directory.EnumerateFileSystemEntries(expected, "*", SearchOption.AllDirectories);
            Assert.True(Enumerable.SequenceEqual(expectedEntries.Select(i => Path.GetFileName(i)), actualEntries.Select(i => Path.GetFileName(i))));
        }

        private static void ItemEqual(String[] actualList, List<FileData> expectedList, Boolean isFile)
        {
            for (int i = 0; i < actualList.Length; i++)
            {
                var actualFile = actualList[i];
                String aEntry = Path.GetFullPath(actualFile);
                String aName = Path.GetFileName(aEntry);

                var bData = expectedList.Where(f => String.Equals(f.Name, aName, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                String bEntry = Path.GetFullPath(Path.Combine(bData.OrigFolder, bData.FullName));
                String bName = Path.GetFileName(bEntry);
                // expected 'emptydir' folder doesn't exist because MSBuild doesn't copy empty dir
                if (!isFile && aName.Contains("emptydir") && bName.Contains("emptydir"))
                    continue;

                //we want it to be false that one of them is a directory and the other isn't
                Assert.False(Directory.Exists(aEntry) ^ Directory.Exists(bEntry), "Directory in one is file in other");

                //contents same
                if (isFile)
                {
                    Stream sa = StreamHelpers.CreateTempCopyStream(aEntry).Result;
                    Stream sb = StreamHelpers.CreateTempCopyStream(bEntry).Result;
                    StreamsEqual(sa, sb);
                }
            }
        }

        public static async Task CreateFromDir(String directory, Stream archiveStream, ZipArchiveMode mode)
        {
            var files = FileData.InPath(directory);
            using (ZipArchive archive = new ZipArchive(archiveStream, mode, true))
            {
                foreach (var i in files)
                {
                    if (i.IsFolder)
                    {
                        String entryName = i.FullName;

                        ZipArchiveEntry e = archive.CreateEntry(entryName.Replace('\\', '/') + "/");
                        e.LastWriteTime = i.LastModifiedDate;
                    }
                }

                foreach (var i in files)
                {
                    if (i.IsFile)
                    {
                        String entryName = i.FullName;

                        var installStream = await StreamHelpers.CreateTempCopyStream(Path.Combine(i.OrigFolder, i.FullName));

                        if (installStream != null)
                        {
                            ZipArchiveEntry e = archive.CreateEntry(entryName.Replace('\\', '/'));
                            e.LastWriteTime = i.LastModifiedDate;
                            using (Stream entryStream = e.Open())
                            {
                                installStream.CopyTo(entryStream);
                            }
                        }
                    }
                }
            }
        }

        internal static void AddEntry(ZipArchive archive, String name, String contents, DateTimeOffset lastWrite)
        {
            ZipArchiveEntry e = archive.CreateEntry(name);
            e.LastWriteTime = lastWrite;
            using (StreamWriter w = new StreamWriter(e.Open()))
            {
                w.WriteLine(contents);
            }
        }

        #endregion
    }
}
