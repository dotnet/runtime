// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Compression.Tests
{
    public class ZipFile_Create : ZipFileTestBase
    {
        [Fact]
        public async Task CreateFromDirectoryNormal()
        {
            string folderName = zfolder("normal");
            string noBaseDir = GetTestFilePath();
            ZipFile.CreateFromDirectory(folderName, noBaseDir);

            await IsZipSameAsDirAsync(noBaseDir, folderName, ZipArchiveMode.Read, requireExplicit: false, checkTimes: false);
        }

        [Fact]
        public void CreateFromDirectory_IncludeBaseDirectory()
        {
            string folderName = zfolder("normal");
            string withBaseDir = GetTestFilePath();
            ZipFile.CreateFromDirectory(folderName, withBaseDir, CompressionLevel.Optimal, true);

            IEnumerable<string> expected = Directory.EnumerateFiles(zfolder("normal"), "*", SearchOption.AllDirectories);
            using (ZipArchive actual_withbasedir = ZipFile.Open(withBaseDir, ZipArchiveMode.Read))
            {
                foreach (ZipArchiveEntry actualEntry in actual_withbasedir.Entries)
                {
                    string expectedFile = expected.Single(i => Path.GetFileName(i).Equals(actualEntry.Name));
                    Assert.StartsWith("normal", actualEntry.FullName);
                    Assert.Equal(new FileInfo(expectedFile).Length, actualEntry.Length);
                    using (Stream expectedStream = File.OpenRead(expectedFile))
                    using (Stream actualStream = actualEntry.Open())
                    {
                        StreamsEqual(expectedStream, actualStream);
                    }
                }
            }
        }

        [Fact]
        public void CreateFromDirectoryUnicode()
        {
            string folderName = zfolder("unicode");
            string noBaseDir = GetTestFilePath();
            ZipFile.CreateFromDirectory(folderName, noBaseDir);

            using (ZipArchive archive = ZipFile.OpenRead(noBaseDir))
            {
                IEnumerable<string> actual = archive.Entries.Select(entry => entry.Name);
                IEnumerable<string> expected = Directory.EnumerateFileSystemEntries(zfolder("unicode"), "*", SearchOption.AllDirectories).ToList();
                Assert.True(Enumerable.SequenceEqual(expected.Select(i => Path.GetFileName(i)), actual.Select(i => i)));
            }
        }

        [Fact]
        public void CreatedEmptyDirectoriesRoundtrip()
        {
            using (var tempFolder = new TempDirectory(GetTestFilePath()))
            {
                DirectoryInfo rootDir = new DirectoryInfo(tempFolder.Path);
                rootDir.CreateSubdirectory("empty1");

                string archivePath = GetTestFilePath();
                ZipFile.CreateFromDirectory(
                    rootDir.FullName, archivePath,
                    CompressionLevel.Optimal, false, Encoding.UTF8);

                using (ZipArchive archive = ZipFile.OpenRead(archivePath))
                {
                    Assert.Equal(1, archive.Entries.Count);
                    Assert.StartsWith("empty1", archive.Entries[0].FullName);
                }
            }
        }

        [Fact]
        public void CreatedEmptyUtf32DirectoriesRoundtrip()
        {
            using (var tempFolder = new TempDirectory(GetTestFilePath()))
            {
                Encoding entryEncoding = Encoding.UTF32;
                DirectoryInfo rootDir = new DirectoryInfo(tempFolder.Path);
                rootDir.CreateSubdirectory("empty1");

                string archivePath = GetTestFilePath();
                ZipFile.CreateFromDirectory(
                    rootDir.FullName, archivePath,
                    CompressionLevel.Optimal, false, entryEncoding);

                using (ZipArchive archive = ZipFile.Open(archivePath, ZipArchiveMode.Read, entryEncoding))
                {
                    Assert.Equal(1, archive.Entries.Count);
                    Assert.StartsWith("empty1", archive.Entries[0].FullName);
                }
            }
        }

        [Fact]
        public void CreatedEmptyRootDirectoryRoundtrips()
        {
            using (var tempFolder = new TempDirectory(GetTestFilePath()))
            {
                DirectoryInfo emptyRoot = new DirectoryInfo(tempFolder.Path);
                string archivePath = GetTestFilePath();
                ZipFile.CreateFromDirectory(
                    emptyRoot.FullName, archivePath,
                    CompressionLevel.Optimal, true);

                using (ZipArchive archive = ZipFile.OpenRead(archivePath))
                {
                    Assert.Equal(1, archive.Entries.Count);
                }
            }
        }

        [Fact]
        public void InvalidInstanceMethods()
        {
            using (TempFile testArchive = CreateTempCopyFile(zfile("normal.zip"), GetTestFilePath()))
            using (ZipArchive archive = ZipFile.Open(testArchive.Path, ZipArchiveMode.Update))
            {
                //non-existent entry
                Assert.True(null == archive.GetEntry("nonExistentEntry"));
                //null/empty string
                Assert.Throws<ArgumentNullException>(() => archive.GetEntry(null));

                ZipArchiveEntry entry = archive.GetEntry("first.txt");

                //null/empty string
                AssertExtensions.Throws<ArgumentException>("entryName", () => archive.CreateEntry(""));
                Assert.Throws<ArgumentNullException>(() => archive.CreateEntry(null));
            }
        }

        [Fact]
        public void InvalidConstructors()
        {
            //out of range enum values
            Assert.Throws<ArgumentOutOfRangeException>(() => ZipFile.Open("bad file", (ZipArchiveMode)(10)));
        }

        [Fact]
        public void InvalidFiles()
        {
            Assert.Throws<InvalidDataException>(() => ZipFile.OpenRead(bad("EOCDmissing.zip")));
            using (TempFile testArchive = CreateTempCopyFile(bad("EOCDmissing.zip"), GetTestFilePath()))
            {
                Assert.Throws<InvalidDataException>(() => ZipFile.Open(testArchive.Path, ZipArchiveMode.Update));
            }

            Assert.Throws<InvalidDataException>(() => ZipFile.OpenRead(bad("CDoffsetOutOfBounds.zip")));
            using (TempFile testArchive = CreateTempCopyFile(bad("CDoffsetOutOfBounds.zip"), GetTestFilePath()))
            {
                Assert.Throws<InvalidDataException>(() => ZipFile.Open(testArchive.Path, ZipArchiveMode.Update));
            }

            using (ZipArchive archive = ZipFile.OpenRead(bad("CDoffsetInBoundsWrong.zip")))
            {
                Assert.Throws<InvalidDataException>(() => { var x = archive.Entries; });
            }

            using (TempFile testArchive = CreateTempCopyFile(bad("CDoffsetInBoundsWrong.zip"), GetTestFilePath()))
            {
                Assert.Throws<InvalidDataException>(() => ZipFile.Open(testArchive.Path, ZipArchiveMode.Update));
            }

            using (ZipArchive archive = ZipFile.OpenRead(bad("numberOfEntriesDifferent.zip")))
            {
                Assert.Throws<InvalidDataException>(() => { var x = archive.Entries; });
            }
            using (TempFile testArchive = CreateTempCopyFile(bad("numberOfEntriesDifferent.zip"), GetTestFilePath()))
            {
                Assert.Throws<InvalidDataException>(() => ZipFile.Open(testArchive.Path, ZipArchiveMode.Update));
            }

            //read mode on empty file
            using (var memoryStream = new MemoryStream())
            {
                Assert.Throws<InvalidDataException>(() => new ZipArchive(memoryStream));
            }

            //offset out of bounds
            using (ZipArchive archive = ZipFile.OpenRead(bad("localFileOffsetOutOfBounds.zip")))
            {
                ZipArchiveEntry e = archive.Entries[0];
                Assert.Throws<InvalidDataException>(() => e.Open());
            }

            using (TempFile testArchive = CreateTempCopyFile(bad("localFileOffsetOutOfBounds.zip"), GetTestFilePath()))
            {
                Assert.Throws<InvalidDataException>(() => ZipFile.Open(testArchive.Path, ZipArchiveMode.Update));
            }

            //compressed data offset + compressed size out of bounds
            using (ZipArchive archive = ZipFile.OpenRead(bad("compressedSizeOutOfBounds.zip")))
            {
                ZipArchiveEntry e = archive.Entries[0];
                Assert.Throws<InvalidDataException>(() => e.Open());
            }

            using (TempFile testArchive = CreateTempCopyFile(bad("compressedSizeOutOfBounds.zip"), GetTestFilePath()))
            {
                Assert.Throws<InvalidDataException>(() => ZipFile.Open(testArchive.Path, ZipArchiveMode.Update));
            }

            //signature wrong
            using (ZipArchive archive = ZipFile.OpenRead(bad("localFileHeaderSignatureWrong.zip")))
            {
                ZipArchiveEntry e = archive.Entries[0];
                Assert.Throws<InvalidDataException>(() => e.Open());
            }

            using (TempFile testArchive = CreateTempCopyFile(bad("localFileHeaderSignatureWrong.zip"), GetTestFilePath()))
            {
                Assert.Throws<InvalidDataException>(() => ZipFile.Open(testArchive.Path, ZipArchiveMode.Update));
            }
        }

        [Theory]
        [InlineData("LZMA.zip", true)]
        [InlineData("invalidDeflate.zip", false)]
        public void UnsupportedCompressionRoutine(string zipName, bool throwsOnOpen)
        {
            string filename = bad(zipName);
            using (ZipArchive archive = ZipFile.OpenRead(filename))
            {
                ZipArchiveEntry e = archive.Entries[0];
                if (throwsOnOpen)
                {
                    Assert.Throws<InvalidDataException>(() => e.Open());
                }
                else
                {
                    using (Stream s = e.Open())
                    {
                        Assert.Throws<InvalidDataException>(() => s.ReadByte());
                    }
                }
            }

            using (TempFile updatedCopy = CreateTempCopyFile(filename, GetTestFilePath()))
            {
                string name;
                long length, compressedLength;
                DateTimeOffset lastWriteTime;
                using (ZipArchive archive = ZipFile.Open(updatedCopy.Path, ZipArchiveMode.Update))
                {
                    ZipArchiveEntry e = archive.Entries[0];
                    name = e.FullName;
                    lastWriteTime = e.LastWriteTime;
                    length = e.Length;
                    compressedLength = e.CompressedLength;
                    Assert.Throws<InvalidDataException>(() => e.Open());
                }

                //make sure that update mode preserves that unreadable file
                using (ZipArchive archive = ZipFile.Open(updatedCopy.Path, ZipArchiveMode.Update))
                {
                    ZipArchiveEntry e = archive.Entries[0];
                    Assert.Equal(name, e.FullName);
                    Assert.Equal(lastWriteTime, e.LastWriteTime);
                    Assert.Equal(length, e.Length);
                    Assert.Equal(compressedLength, e.CompressedLength);
                    Assert.Throws<InvalidDataException>(() => e.Open());
                }
            }
        }

        [Fact]
        public void InvalidDates()
        {
            using (ZipArchive archive = ZipFile.OpenRead(bad("invaliddate.zip")))
            {
                Assert.Equal(new DateTime(1980, 1, 1, 0, 0, 0), archive.Entries[0].LastWriteTime.DateTime);
            }

            // Browser VFS does not support saving file attributes, so skip
            if (!PlatformDetection.IsBrowser)
            {
                FileInfo fileWithBadDate = new FileInfo(GetTestFilePath());
                fileWithBadDate.Create().Dispose();
                fileWithBadDate.LastWriteTimeUtc = new DateTime(1970, 1, 1, 1, 1, 1);

                string archivePath = GetTestFilePath();
                using (FileStream output = File.Open(archivePath, FileMode.Create))
                using (ZipArchive archive = new ZipArchive(output, ZipArchiveMode.Create))
                {
                    archive.CreateEntryFromFile(fileWithBadDate.FullName, "SomeEntryName");
                }
                using (ZipArchive archive = ZipFile.OpenRead(archivePath))
                {
                    Assert.Equal(new DateTime(1980, 1, 1, 0, 0, 0), archive.Entries[0].LastWriteTime.DateTime);
                }
            }
        }

        [Fact]
        public void FilesOutsideDirectory()
        {
            string archivePath = GetTestFilePath();
            using (ZipArchive archive = ZipFile.Open(archivePath, ZipArchiveMode.Create))
            using (StreamWriter writer = new StreamWriter(archive.CreateEntry(Path.Combine("..", "entry1"), CompressionLevel.Optimal).Open()))
            {
                writer.Write("This is a test.");
            }
            Assert.Throws<IOException>(() => ZipFile.ExtractToDirectory(archivePath, GetTestFilePath()));
        }

        [Fact]
        public void DirectoryEntryWithData()
        {
            string archivePath = GetTestFilePath();
            using (ZipArchive archive = ZipFile.Open(archivePath, ZipArchiveMode.Create))
            using (StreamWriter writer = new StreamWriter(archive.CreateEntry("testdir" + Path.DirectorySeparatorChar, CompressionLevel.Optimal).Open()))
            {
                writer.Write("This is a test.");
            }
            Assert.Throws<IOException>(() => ZipFile.ExtractToDirectory(archivePath, GetTestFilePath()));
        }

        [Fact]
        public void ReadStreamOps()
        {
            using (ZipArchive archive = ZipFile.OpenRead(zfile("normal.zip")))
            {
                foreach (ZipArchiveEntry e in archive.Entries)
                {
                    using (Stream s = e.Open())
                    {
                        Assert.True(s.CanRead, "Can read to read archive");
                        Assert.False(s.CanWrite, "Can't write to read archive");
                        Assert.False(s.CanSeek, "Can't seek on archive");
                        Assert.Equal(LengthOfUnseekableStream(s), e.Length);
                    }
                }
            }
        }

        /// <summary>
        /// This test ensures that a zipfile with path names that are invalid to this OS will throw errors
        /// when an attempt is made to extract them.
        /// </summary>
        [Theory]
        [InlineData("NullCharFileName_FromWindows")]
        [InlineData("NullCharFileName_FromUnix")]
        [PlatformSpecific(TestPlatforms.AnyUnix)]  // Checks Unix-specific invalid file path
        public void Unix_ZipWithInvalidFileNames_ThrowsArgumentException(string zipName)
        {
            Assert.Throws<ArgumentException>(() => ZipFile.ExtractToDirectory(compat(zipName) + ".zip", GetTestFilePath()));
        }

        [Fact]
        public void UpdateReadTwice()
        {
            using (TempFile testArchive = CreateTempCopyFile(zfile("small.zip"), GetTestFilePath()))
            using (ZipArchive archive = ZipFile.Open(testArchive.Path, ZipArchiveMode.Update))
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

        [Fact]
        public async Task UpdateAddFile()
        {
            //add file
            using (TempFile testArchive = CreateTempCopyFile(zfile("normal.zip"), GetTestFilePath()))
            {
                using (ZipArchive archive = ZipFile.Open(testArchive.Path, ZipArchiveMode.Update))
                {
                    await UpdateArchive(archive, zmodified(Path.Combine("addFile", "added.txt")), "added.txt");
                }
                await IsZipSameAsDirAsync(testArchive.Path, zmodified("addFile"), ZipArchiveMode.Read);
            }

            //add file and read entries before
            using (TempFile testArchive = CreateTempCopyFile(zfile("normal.zip"), GetTestFilePath()))
            {
                using (ZipArchive archive = ZipFile.Open(testArchive.Path, ZipArchiveMode.Update))
                {
                    var x = archive.Entries;

                    await UpdateArchive(archive, zmodified(Path.Combine("addFile", "added.txt")), "added.txt");
                }
                await IsZipSameAsDirAsync(testArchive.Path, zmodified("addFile"), ZipArchiveMode.Read);
            }

            //add file and read entries after
            using (TempFile testArchive = CreateTempCopyFile(zfile("normal.zip"), GetTestFilePath()))
            {
                using (ZipArchive archive = ZipFile.Open(testArchive.Path, ZipArchiveMode.Update))
                {
                    await UpdateArchive(archive, zmodified(Path.Combine("addFile", "added.txt")), "added.txt");

                    var x = archive.Entries;
                }
                await IsZipSameAsDirAsync(testArchive.Path, zmodified("addFile"), ZipArchiveMode.Read);
            }
        }

        /// <summary>
        /// This test ensures that a zipfile with path names that are invalid to this OS will throw errors
        /// when an attempt is made to extract them.
        /// </summary>
        [Theory]
        [InlineData("WindowsInvalid_FromUnix", null)]
        [InlineData("WindowsInvalid_FromWindows", null)]
        [InlineData("NullCharFileName_FromWindows", "path")]
        [InlineData("NullCharFileName_FromUnix", "path")]
        [PlatformSpecific(TestPlatforms.Windows)]  // Checks Windows-specific invalid file path
        public void Windows_ZipWithInvalidFileNames_ThrowsException(string zipName, string paramName)
        {
            if (paramName == null)
                Assert.Throws<IOException>(() => ZipFile.ExtractToDirectory(compat(zipName) + ".zip", GetTestFilePath()));
            else
                AssertExtensions.Throws<ArgumentException>(paramName, null, () => ZipFile.ExtractToDirectory(compat(zipName) + ".zip", GetTestFilePath()));
        }

        private static async Task UpdateArchive(ZipArchive archive, string installFile, string entryName)
        {
            string fileName = installFile;
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
        public void CreateSetsExternalAttributesCorrectly()
        {
            string folderName = zfolder("normal");
            string filepath = GetTestFilePath();
            ZipFile.CreateFromDirectory(folderName, filepath);

            using (ZipArchive archive = ZipFile.Open(filepath, ZipArchiveMode.Read))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    if (OperatingSystem.IsWindows())
                    {
                        Assert.Equal(0, entry.ExternalAttributes);
                    }
                    else
                    {
                        Assert.NotEqual(0, entry.ExternalAttributes);
                    }
                }
            }
        }
    }
}
