// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Compression.Test
{
    public partial class ZipTest
    {
        [Fact]
        public static async Task CreateFromDirectoryNormal()
        {
            await TestCreateDirectory(zfolder("normal"), true);
            if (Interop.IsWindows) // [ActiveIssue(846, PlatformID.Linux | PlatformID.OSX)]
            {
                await TestCreateDirectory(zfolder("unicode"), true);
            }
        }

        private static async Task TestCreateDirectory(String folderName, Boolean testWithBaseDir)
        {
            String noBaseDir = StreamHelpers.GetTmpFileName();
            ZipFile.CreateFromDirectory(folderName, noBaseDir);

            await IsZipSameAsDirAsync(noBaseDir, folderName, ZipArchiveMode.Read, true, true);

            if (testWithBaseDir)
            {
                String withBaseDir = StreamHelpers.GetTmpFileName();
                ZipFile.CreateFromDirectory(folderName, withBaseDir, CompressionLevel.Optimal, true);
                SameExceptForBaseDir(noBaseDir, withBaseDir, folderName);
            }
        }

        private static void SameExceptForBaseDir(String zipNoBaseDir, String zipBaseDir, String baseDir)
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

                    Assert.Equal(Path.Combine(Path.GetFileName(baseDir), aEntry.FullName), bEntry.FullName);
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
        public static void ExtractToDirectoryNormal()
        {
            TestExtract(zfile("normal.zip"), zfolder("normal"));
            if (Interop.IsWindows) // [ActiveIssue(846, PlatformID.Linux | PlatformID.OSX)]
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

        private static void TestExtract(String zipFileName, String folderName)
        {
            String tempFolder = StreamHelpers.GetTmpPath(true);
            ZipFile.ExtractToDirectory(zipFileName, tempFolder);
            DirsEqual(tempFolder, folderName);
        }

        #region "Extension Methods"

        [Fact]
        public static async Task CreateEntryFromFileTest()
        {
            //add file
            String testArchive = StreamHelpers.CreateTempCopyFile(zfile("normal.zip"));

            using (ZipArchive archive = ZipFile.Open(testArchive, ZipArchiveMode.Update))
            {
                ZipArchiveEntry e = archive.CreateEntryFromFile(zmodified(Path.Combine("addFile", "added.txt")), "added.txt");
                Assert.NotNull(e);
            }

            await IsZipSameAsDirAsync(testArchive, zmodified("addFile"), ZipArchiveMode.Read, true, true);
        }

        [Fact]
        public static void ExtractToFileTest()
        {
            using (ZipArchive archive = ZipFile.Open(zfile("normal.zip"), ZipArchiveMode.Read))
            {
                String file = StreamHelpers.GetTmpFileName();
                ZipArchiveEntry e = archive.GetEntry("first.txt");

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
        public static void ExtractToDirectoryTest()
        {
            using (ZipArchive archive = ZipFile.Open(zfile("normal.zip"), ZipArchiveMode.Read))
            {
                String tempFolder = StreamHelpers.GetTmpPath(false);
                archive.ExtractToDirectory(tempFolder);

                DirsEqual(tempFolder, zfolder("normal"));
            }

            if (Interop.IsWindows) // [ActiveIssue(846, PlatformID.Linux | PlatformID.OSX)]
            {
                using (ZipArchive archive = ZipFile.OpenRead(zfile("unicode.zip")))
                {
                    String tempFolder = StreamHelpers.GetTmpPath(false);
                    archive.ExtractToDirectory(tempFolder);

                    DirsEqual(tempFolder, zfolder("unicode"));
                }
            }
        }

        #endregion
    }
}
