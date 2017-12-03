// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.IO.Compression.Tests
{
    public partial class ZipFileTest_Invalid : ZipFileTestBase
    {
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

        /// <summary>
        /// This test ensures that a zipfile with path names that are invalid to this OS will throw errors
        /// when an attempt is made to extract them.
        /// </summary>
        [ActiveIssue(25665)]
        [Theory]
        [InlineData("NullCharFileName_FromWindows")]
        [InlineData("NullCharFileName_FromUnix")]
        [PlatformSpecific(TestPlatforms.AnyUnix)]  // Checks Unix-specific invalid file path
        public void Unix_ZipWithInvalidFileNames_ThrowsArgumentException(string zipName)
        {
            AssertExtensions.Throws<ArgumentException>("path", () => ZipFile.ExtractToDirectory(compat(zipName) + ".zip", GetTestFilePath()));
        }

        [Theory]
        [InlineData("backslashes_FromUnix", "aa\\bb\\cc\\dd")]
        [InlineData("backslashes_FromWindows", "aa\\bb\\cc\\dd")]
        [InlineData("WindowsInvalid_FromUnix", "aa<b>d")]
        [InlineData("WindowsInvalid_FromWindows", "aa<b>d")]
        [PlatformSpecific(TestPlatforms.AnyUnix)]  // Checks Unix-specific invalid file path
        public void Unix_ZipWithOSSpecificFileNames(string zipName, string fileName)
        {
            string tempDir = GetTestFilePath();
            ZipFile.ExtractToDirectory(compat(zipName) + ".zip", tempDir);
            string[] results = Directory.GetFiles(tempDir, "*", SearchOption.AllDirectories);
            Assert.Equal(1, results.Length);
            Assert.Equal(fileName, Path.GetFileName(results[0]));
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
        public void Windows_ZipWithInvalidFileNames_ThrowsArgumentException(string zipName, string paramName)
        {
            AssertExtensions.Throws<ArgumentException>(paramName, null, () => ZipFile.ExtractToDirectory(compat(zipName) + ".zip", GetTestFilePath()));
        }

        [Theory]
        [InlineData("backslashes_FromUnix", "dd")]
        [InlineData("backslashes_FromWindows", "dd")]
        [PlatformSpecific(TestPlatforms.Windows)]  // Checks Windows-specific invalid file path
        public void Windows_ZipWithOSSpecificFileNames(string zipName, string fileName)
        {
            string tempDir = GetTestFilePath();
            ZipFile.ExtractToDirectory(compat(zipName) + ".zip", tempDir);
            string[] results = Directory.GetFiles(tempDir, "*", SearchOption.AllDirectories);
            Assert.Equal(1, results.Length);
            Assert.Equal(fileName, Path.GetFileName(results[0]));
        }
    }
}
