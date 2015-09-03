// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Compression.Tests
{
    public partial class ZipTest
    {
        [Fact]
        public async Task CreateFromDirectoryNormal()
        {
            await TestCreateDirectory(zfolder("normal"), true);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) // [ActiveIssue(846, PlatformID.AnyUnix)]
            {
                await TestCreateDirectory(zfolder("unicode"), true);
            }
        }

        private async Task TestCreateDirectory(string folderName, Boolean testWithBaseDir)
        {
            string noBaseDir = GetTmpFilePath();
            ZipFile.CreateFromDirectory(folderName, noBaseDir);

            await IsZipSameAsDirAsync(noBaseDir, folderName, ZipArchiveMode.Read, true, true);

            if (testWithBaseDir)
            {
                string withBaseDir = GetTmpFilePath();
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
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) // [ActiveIssue(846, PlatformID.AnyUnix)]
            {
                TestExtract(zfile("unicode.zip"), zfolder("unicode"));
            }
            TestExtract(zfile("empty.zip"), zfolder("empty"));
            TestExtract(zfile("explicitdir1.zip"), zfolder("explicitdir"));
            TestExtract(zfile("explicitdir2.zip"), zfolder("explicitdir"));
            TestExtract(zfile("appended.zip"), zfolder("small"));
            TestExtract(zfile("prepended.zip"), zfolder("small"));
            TestExtract(zfile("noexplicitdir.zip"), zfolder("explicitdir"));
        }

        private void TestExtract(string zipFileName, string folderName)
        {
            string tempFolder = GetTmpDirPath(true);
            ZipFile.ExtractToDirectory(zipFileName, tempFolder);
            DirsEqual(tempFolder, folderName);

            Assert.Throws<ArgumentNullException>(() => ZipFile.ExtractToDirectory(null, tempFolder));
        }

        #region "Extension Methods"

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task CreateEntryFromFileTest(bool withCompressionLevel)
        {
            //add file
            string testArchive = CreateTempCopyFile(zfile("normal.zip"));

            using (ZipArchive archive = ZipFile.Open(testArchive, ZipArchiveMode.Update))
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

            await IsZipSameAsDirAsync(testArchive, zmodified("addFile"), ZipArchiveMode.Read, true, true);
        }

        [Fact]
        public void ExtractToFileTest()
        {
            using (ZipArchive archive = ZipFile.Open(zfile("normal.zip"), ZipArchiveMode.Read))
            {
                string file = GetTmpFilePath();
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
                string tempFolder = GetTmpDirPath(false);
                Assert.Throws<ArgumentNullException>(() => ((ZipArchive)null).ExtractToDirectory(tempFolder));
                Assert.Throws<ArgumentNullException>(() => archive.ExtractToDirectory(null));
                archive.ExtractToDirectory(tempFolder);

                DirsEqual(tempFolder, zfolder("normal"));
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) // [ActiveIssue(846, PlatformID.AnyUnix)]
            {
                using (ZipArchive archive = ZipFile.OpenRead(zfile("unicode.zip")))
                {
                    string tempFolder = GetTmpDirPath(false);
                    archive.ExtractToDirectory(tempFolder);

                    DirsEqual(tempFolder, zfolder("unicode"));
                }
            }
        }

        [Fact]
        public void CreatedEmptyDirectoriesRoundtrip()
        {
            DirectoryInfo rootDir = new DirectoryInfo(GetTmpDirPath(create: true));
            rootDir.CreateSubdirectory("empty1");

            string archivePath = GetTmpFilePath();
            ZipFile.CreateFromDirectory(
                rootDir.FullName, archivePath,
                CompressionLevel.Optimal, false, Encoding.UTF8);

            using (ZipArchive archive = ZipFile.OpenRead(archivePath))
            {
                Assert.Equal(1, archive.Entries.Count);
                Assert.True(archive.Entries[0].FullName.StartsWith("empty1"));
            }
        }

        [Fact]
        public void CreatedEmptyRootDirectoryRoundtrips()
        {
            DirectoryInfo emptyRoot = new DirectoryInfo(GetTmpDirPath(create: true));

            string archivePath = GetTmpFilePath();
            ZipFile.CreateFromDirectory(
                emptyRoot.FullName, archivePath,
                CompressionLevel.Optimal, true);

            using (ZipArchive archive = ZipFile.OpenRead(archivePath))
            {
                Assert.Equal(1, archive.Entries.Count);
            }
        }

        #endregion
    }
}
