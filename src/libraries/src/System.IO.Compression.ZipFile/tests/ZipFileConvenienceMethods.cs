// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.NetCore.Extensions;

namespace System.IO.Compression.Tests
{
    public partial class ZipFileTest_ConvenienceMethods : ZipFileTestBase
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
                    Assert.True(actualEntry.FullName.StartsWith("normal"));
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

        [Theory]
        [InlineData("normal.zip", "normal")]
        [InlineData("empty.zip", "empty")]
        [InlineData("explicitdir1.zip", "explicitdir")]
        [InlineData("explicitdir2.zip", "explicitdir")]
        [InlineData("appended.zip", "small")]
        [InlineData("prepended.zip", "small")]
        [InlineData("noexplicitdir.zip", "explicitdir")]
        public void ExtractToDirectoryNormal(string file, string folder)
        {
            string zipFileName = zfile(file);
            string folderName = zfolder(folder);
            using (var tempFolder = new TempDirectory(GetTestFilePath()))
            {
                ZipFile.ExtractToDirectory(zipFileName, tempFolder.Path);
                DirsEqual(tempFolder.Path, folderName);
            }
        }

        [Fact]
        public void ExtractToDirectoryNull()
        {
            AssertExtensions.Throws<ArgumentNullException>("sourceArchiveFileName", () => ZipFile.ExtractToDirectory(null, GetTestFilePath()));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotMacOsHighSierraOrHigher))]
        public void ExtractToDirectoryUnicode()
        {
            string zipFileName = zfile("unicode.zip");
            string folderName = zfolder("unicode");
            using (var tempFolder = new TempDirectory(GetTestFilePath()))
            {
                ZipFile.ExtractToDirectory(zipFileName, tempFolder.Path);
                DirFileNamesEqual(tempFolder.Path, folderName);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task CreateEntryFromFileExtension(bool withCompressionLevel)
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
                await IsZipSameAsDirAsync(testArchive.Path, zmodified("addFile"), ZipArchiveMode.Read, requireExplicit: false, checkTimes: false);
            }
        }

        [Fact]
        public void ExtractToFileExtension()
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
        public void ExtractToDirectoryExtension()
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

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotMacOsHighSierraOrHigher))]
        public void ExtractToDirectoryExtension_Unicode()
        {
            using (ZipArchive archive = ZipFile.OpenRead(zfile("unicode.zip")))
            {
                string tempFolder = GetTestFilePath();
                archive.ExtractToDirectory(tempFolder);
                DirFileNamesEqual(tempFolder, zfolder("unicode"));
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
    }
}
