// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Compression.Tests
{
    public class ZipFile_Extract : ZipFileTestBase
    {
        public static IEnumerable<object[]> Get_ExtractToDirectoryNormal_Data()
        {
            foreach (bool async in _bools)
            {
                yield return new object[] { "normal.zip", "normal", async };
                yield return new object[] { "empty.zip", "empty", async };
                yield return new object[] { "explicitdir1.zip", "explicitdir", async };
                yield return new object[] { "explicitdir2.zip", "explicitdir", async };
                yield return new object[] { "appended.zip", "small", async };
                yield return new object[] { "prepended.zip", "small", async };
                yield return new object[] { "noexplicitdir.zip", "explicitdir", async };
            }
        }

        [Theory]
        [MemberData(nameof(Get_ExtractToDirectoryNormal_Data))]
        public async Task ExtractToDirectoryNormal(string file, string folder, bool async)
        {
            string zipFileName = zfile(file);
            string folderName = zfolder(folder);
            using TempDirectory tempFolder = new TempDirectory(GetTestFilePath());
            await CallZipFileExtractToDirectory(async, zipFileName, tempFolder.Path);
            await DirsEqual(tempFolder.Path, folderName);
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task ExtractToDirectoryNull(bool async)
        {
            await AssertExtensions.ThrowsAsync<ArgumentNullException>("sourceArchiveFileName", () => CallZipFileExtractToDirectory(async, sourceArchiveFileName: null, GetTestFilePath()));
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/72951", TestPlatforms.iOS | TestPlatforms.tvOS)]
        public async Task ExtractToDirectoryUnicode(bool async)
        {
            string zipFileName = zfile("unicode.zip");
            string folderName = zfolder("unicode");
            using (var tempFolder = new TempDirectory(GetTestFilePath()))
            {
                await CallZipFileExtractToDirectory(async, zipFileName, tempFolder.Path);
                DirFileNamesEqual(tempFolder.Path, folderName);
            }
        }

        [Theory]
        [InlineData("../Foo")]
        [InlineData("../Barbell")]
        public void ExtractOutOfRoot(string entryName)
        {
            string archivePath = GetTestFilePath();
            using (FileStream stream = new FileStream(archivePath, FileMode.Create))
            using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
            {
                ZipArchiveEntry entry = archive.CreateEntry(entryName);
            }

            DirectoryInfo destination = Directory.CreateDirectory(Path.Combine(GetTestFilePath(), "Bar"));
            Assert.Throws<IOException>(() => ZipFile.ExtractToDirectory(archivePath, destination.FullName));
        }

        [Theory]
        [InlineData("../Foo")]
        [InlineData("../Barbell")]
        public async Task ExtractOutOfRoot_Async(string entryName)
        {
            string archivePath = GetTestFilePath();
            using (FileStream stream = new FileStream(archivePath, FileMode.Create))
            await using (ZipArchive archive = await ZipArchive.CreateAsync(stream, ZipArchiveMode.Create, leaveOpen: true, entryNameEncoding: null))
            {
                ZipArchiveEntry entry = archive.CreateEntry(entryName);
            }

            DirectoryInfo destination = Directory.CreateDirectory(Path.Combine(GetTestFilePath(), "Bar"));
            await Assert.ThrowsAsync<IOException>(() => ZipFile.ExtractToDirectoryAsync(archivePath, destination.FullName));
        }

        /// <summary>
        /// This test ensures that a zipfile with path names that are invalid to this OS will throw errors
        /// when an attempt is made to extract them.
        /// </summary>
        [Theory]
        [MemberData(nameof(Get_Unix_ZipWithInvalidFileNames_Data))]
        [PlatformSpecific(TestPlatforms.AnyUnix)]  // Checks Unix-specific invalid file path
        public async Task Unix_ZipWithInvalidFileNames(string zipName, bool async)
        {
            var testDirectory = GetTestFilePath();
            await CallZipFileExtractToDirectory(async, compat(zipName) + ".zip", testDirectory);
            Assert.True(File.Exists(Path.Combine(testDirectory, "a_6b6d")));
        }

        [Theory]
        [MemberData(nameof(Get_Unix_ZipWithOSSpecificFileNames_Data))]
        [PlatformSpecific(TestPlatforms.AnyUnix)]  // Checks Unix-specific invalid file path
        public async Task Unix_ZipWithOSSpecificFileNames(string zipName, string fileName, bool async)
        {
            string tempDir = GetTestFilePath();
            await CallZipFileExtractToDirectory(async, compat(zipName) + ".zip", tempDir);
            string[] results = Directory.GetFiles(tempDir, "*", SearchOption.AllDirectories);
            Assert.Equal(1, results.Length);
            Assert.Equal(fileName, Path.GetFileName(results[0]));
        }

        [Theory]
        [MemberData(nameof(Get_Windows_ZipWithInvalidFileNames_Data))]
        [PlatformSpecific(TestPlatforms.Windows)]
        public async Task Windows_ZipWithInvalidFileNames(string zipFileName, string[] expectedFiles, bool async)
        {
            string testDirectory = GetTestFilePath();

            await CallZipFileExtractToDirectory(async, compat(zipFileName), testDirectory);
            foreach (string expectedFile in expectedFiles)
            {
                string path = Path.Combine(testDirectory, expectedFile);
                Assert.True(File.Exists(path));
                File.Delete(path);
            }
        }

        [Theory]
        [MemberData(nameof(Get_Windows_ZipWithOSSpecificFileNames_Data))]
        [PlatformSpecific(TestPlatforms.Windows)]  // Checks Windows-specific invalid file path
        public async Task Windows_ZipWithOSSpecificFileNames(string zipName, string fileName, bool async)
        {
            string tempDir = GetTestFilePath();
            await CallZipFileExtractToDirectory(async, compat(zipName) + ".zip", tempDir);
            string[] results = Directory.GetFiles(tempDir, "*", SearchOption.AllDirectories);
            Assert.Equal(1, results.Length);
            Assert.Equal(fileName, Path.GetFileName(results[0]));
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task ExtractToDirectoryOverwrite(bool async)
        {
            string zipFileName = zfile("normal.zip");
            string folderName = zfolder("normal");

            using TempDirectory tempFolder = new TempDirectory(GetTestFilePath());

            await CallZipFileExtractToDirectory(async, zipFileName, tempFolder.Path, overwriteFiles: false);
            await Assert.ThrowsAsync<IOException>(() => CallZipFileExtractToDirectory(async, zipFileName, tempFolder.Path /* default false */));
            await Assert.ThrowsAsync<IOException>(() => CallZipFileExtractToDirectory(async, zipFileName, tempFolder.Path, overwriteFiles: false));
            await CallZipFileExtractToDirectory(async, zipFileName, tempFolder.Path, overwriteFiles: true);

            await DirsEqual(tempFolder.Path, folderName);
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task ExtractToDirectoryOverwriteEncoding(bool async)
        {
            string zipFileName = zfile("normal.zip");
            string folderName = zfolder("normal");

            using TempDirectory tempFolder = new(GetTestFilePath());

            await CallZipFileExtractToDirectory(async, zipFileName, tempFolder.Path, Encoding.UTF8, overwriteFiles: false);
            await Assert.ThrowsAsync<IOException>(() => CallZipFileExtractToDirectory(async, zipFileName, tempFolder.Path, Encoding.UTF8 /* default false */));
            await Assert.ThrowsAsync<IOException>(() => CallZipFileExtractToDirectory(async, zipFileName, tempFolder.Path, Encoding.UTF8, overwriteFiles: false));
            await CallZipFileExtractToDirectory(async, zipFileName, tempFolder.Path, Encoding.UTF8, overwriteFiles: true);

            await DirsEqual(tempFolder.Path, folderName);
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task FilesOutsideDirectory(bool async)
        {
            string archivePath = GetTestFilePath();
            ZipArchive archive = await CallZipFileOpen(async, archivePath, ZipArchiveMode.Create);

            ZipArchiveEntry entry = archive.CreateEntry(Path.Combine("..", "entry1"), CompressionLevel.Optimal);
            Stream entryStream = await OpenEntryStream(async, entry);
            using (StreamWriter writer = new StreamWriter(entryStream))
            {
                writer.Write("This is a test.");
            }
            await DisposeStream(async, entryStream);
            await DisposeZipArchive(async, archive);
            await Assert.ThrowsAsync<IOException>(() => CallZipFileExtractToDirectory(async, archivePath, GetTestFilePath()));
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task DirectoryEntryWithData(bool async)
        {
            string archivePath = GetTestFilePath();
            ZipArchive archive = await CallZipFileOpen(async, archivePath, ZipArchiveMode.Create);
            ZipArchiveEntry entry = archive.CreateEntry("testdir" + Path.DirectorySeparatorChar, CompressionLevel.Optimal);
            Stream entryStream = await OpenEntryStream(async, entry);
            using (StreamWriter writer = new StreamWriter(entryStream))
            {
                writer.Write("This is a test.");
            }
            await DisposeStream(async, entryStream);
            await DisposeZipArchive(async, archive);
            await Assert.ThrowsAsync<IOException>(() => CallZipFileExtractToDirectory(async, archivePath, GetTestFilePath()));
        }
    }
}
