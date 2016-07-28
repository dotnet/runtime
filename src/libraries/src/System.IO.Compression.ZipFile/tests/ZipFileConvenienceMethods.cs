// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.NetCore.Extensions;

namespace System.IO.Compression.Tests
{
    public class ZipFileTest_ConvenienceMethods : ZipFileTestBase
    {
        [Fact]
        public async Task CreateFromDirectoryNormal()
        {
            await TestCreateDirectory(zfolder("normal"), true);
        }

        [Fact]
        [Trait(XunitConstants.Category, XunitConstants.IgnoreForCI)] // Jenkins fails with unicode characters [JENKINS-12610]
        public async Task CreateFromDirectoryUnicodel()
        {
            await TestCreateDirectory(zfolder("unicode"), true);
        }

        private async Task TestCreateDirectory(string folderName, Boolean testWithBaseDir)
        {
            string noBaseDir = GetTestFilePath();
            ZipFile.CreateFromDirectory(folderName, noBaseDir);

            await IsZipSameAsDirAsync(noBaseDir, folderName, ZipArchiveMode.Read, true, true);

            if (testWithBaseDir)
            {
                string withBaseDir = GetTestFilePath();
                ZipFile.CreateFromDirectory(folderName, withBaseDir, CompressionLevel.Optimal, true);
                SameExceptForBaseDir(noBaseDir, withBaseDir, folderName);
            }
        }

        private static void SameExceptForBaseDir(string zipNoBaseDir, string zipBaseDir, string baseDir)
        {
            //b has the base dir
            using (ZipArchive a = ZipFile.Open(zipNoBaseDir, ZipArchiveMode.Read),
                              b = ZipFile.Open(zipBaseDir, ZipArchiveMode.Read))
            {
                var aCount = a.Entries.Count;
                var bCount = b.Entries.Count;
                Assert.Equal(aCount, bCount);

                int bIdx = 0;
                foreach (ZipArchiveEntry aEntry in a.Entries)
                {
                    ZipArchiveEntry bEntry = b.Entries[bIdx++];

                    Assert.Equal(Path.GetFileName(baseDir) + "/" + aEntry.FullName, bEntry.FullName);
                    Assert.Equal(aEntry.Name, bEntry.Name);
                    Assert.Equal(aEntry.Length, bEntry.Length);
                    Assert.Equal(aEntry.CompressedLength, bEntry.CompressedLength);
                    using (Stream aStream = aEntry.Open(), bStream = bEntry.Open())
                    {
                        StreamsEqual(aStream, bStream);
                    }
                }
            }
        }

        [Fact]
        public void ExtractToDirectoryNormal()
        {
            TestExtract(zfile("normal.zip"), zfolder("normal"));
            TestExtract(zfile("empty.zip"), zfolder("empty"));
            TestExtract(zfile("explicitdir1.zip"), zfolder("explicitdir"));
            TestExtract(zfile("explicitdir2.zip"), zfolder("explicitdir"));
            TestExtract(zfile("appended.zip"), zfolder("small"));
            TestExtract(zfile("prepended.zip"), zfolder("small"));
            TestExtract(zfile("noexplicitdir.zip"), zfolder("explicitdir"));
        }

        [Fact]
        [Trait(XunitConstants.Category, XunitConstants.IgnoreForCI)] // Jenkins fails with unicode characters [JENKINS-12610]
        public void ExtractToDirectoryUnicode()
        {
            TestExtract(zfile("unicode.zip"), zfolder("unicode"));
        }

        private void TestExtract(string zipFileName, string folderName)
        {
            using (var tempFolder = new TempDirectory(GetTestFilePath()))
            {
                ZipFile.ExtractToDirectory(zipFileName, tempFolder.Path);
                DirsEqual(tempFolder.Path, folderName);

                Assert.Throws<ArgumentNullException>(() => ZipFile.ExtractToDirectory(null, tempFolder.Path));
            }
        }

        #region "Extension Methods"

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task CreateEntryFromFileTest(bool withCompressionLevel)
        {
            //add file
            using (TempFile testArchive = CreateTempCopyFile(zfile("normal.zip"), GetTestFilePath()))
            {
                using (ZipArchive archive = ZipFile.Open(testArchive.Path, ZipArchiveMode.Update))
                {
                    string entryName = "added.txt";
                    string sourceFilePath = zmodified(Path.Combine("addFile", entryName));

                    Assert.Throws<ArgumentNullException>(() => ((ZipArchive)null).CreateEntryFromFile(sourceFilePath, entryName));
                    Assert.Throws<ArgumentNullException>(() => archive.CreateEntryFromFile(null, entryName));
                    Assert.Throws<ArgumentNullException>(() => archive.CreateEntryFromFile(sourceFilePath, null));

                    ZipArchiveEntry e = withCompressionLevel ?
                        archive.CreateEntryFromFile(sourceFilePath, entryName) :
                        archive.CreateEntryFromFile(sourceFilePath, entryName, CompressionLevel.Fastest);
                    Assert.NotNull(e);
                }
                await IsZipSameAsDirAsync(testArchive.Path, zmodified("addFile"), ZipArchiveMode.Read, true, true);
            }
        }

        [Fact]
        public void ExtractToFileTest()
        {
            using (ZipArchive archive = ZipFile.Open(zfile("normal.zip"), ZipArchiveMode.Read))
            {
                string file = GetTestFilePath();
                ZipArchiveEntry e = archive.GetEntry("first.txt");

                Assert.Throws<ArgumentNullException>(() => ((ZipArchiveEntry)null).ExtractToFile(file));
                Assert.Throws<ArgumentNullException>(() => e.ExtractToFile(null));

                //extract when there is nothing there
                e.ExtractToFile(file);

                using (Stream fs = File.Open(file, FileMode.Open), es = e.Open())
                {
                    StreamsEqual(fs, es);
                }

                Assert.Throws<IOException>(() => e.ExtractToFile(file, false));

                //truncate file
                using (Stream fs = File.Open(file, FileMode.Truncate)) { }

                //now use overwrite mode
                e.ExtractToFile(file, true);

                using (Stream fs = File.Open(file, FileMode.Open), es = e.Open())
                {
                    StreamsEqual(fs, es);
                }
            }
        }

        [Fact]
        public void ExtractToDirectoryTest()
        {
            using (ZipArchive archive = ZipFile.Open(zfile("normal.zip"), ZipArchiveMode.Read))
            {
                string tempFolder = GetTestFilePath();
                Assert.Throws<ArgumentNullException>(() => ((ZipArchive)null).ExtractToDirectory(tempFolder));
                Assert.Throws<ArgumentNullException>(() => archive.ExtractToDirectory(null));
                archive.ExtractToDirectory(tempFolder);

                DirsEqual(tempFolder, zfolder("normal"));
            }
        }

        [Fact]
        [Trait(XunitConstants.Category, XunitConstants.IgnoreForCI)] // Jenkins fails with unicode characters [JENKINS-12610]
        public void ExtractToDirectoryTest_Unicode()
        {
            using (ZipArchive archive = ZipFile.OpenRead(zfile("unicode.zip")))
            {
                string tempFolder = GetTestFilePath();
                archive.ExtractToDirectory(tempFolder);

                DirsEqual(tempFolder, zfolder("unicode"));
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
                    Assert.True(archive.Entries[0].FullName.StartsWith("empty1"));
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

        #endregion
    }
}
