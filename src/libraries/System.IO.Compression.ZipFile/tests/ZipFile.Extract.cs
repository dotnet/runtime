// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Xunit;

namespace System.IO.Compression.Tests
{
    public class ZipFile_Extract : ZipFileTestBase
    {
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

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/52616", TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst)]
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
        [ActiveIssue("https://github.com/dotnet/runtime/issues/25099")]
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

        [Fact]
        public void ExtractToDirectoryOverwrite()
        {
            string zipFileName = zfile("normal.zip");
            string folderName = zfolder("normal");

            using (var tempFolder = new TempDirectory(GetTestFilePath()))
            {
                ZipFile.ExtractToDirectory(zipFileName, tempFolder.Path, overwriteFiles: false);
                Assert.Throws<IOException>(() => ZipFile.ExtractToDirectory(zipFileName, tempFolder.Path /* default false */));
                Assert.Throws<IOException>(() => ZipFile.ExtractToDirectory(zipFileName, tempFolder.Path, overwriteFiles: false));
                ZipFile.ExtractToDirectory(zipFileName, tempFolder.Path, overwriteFiles: true);

                DirsEqual(tempFolder.Path, folderName);
            }
        }

        [Fact]
        public void ExtractToDirectoryOverwriteEncoding()
        {
            string zipFileName = zfile("normal.zip");
            string folderName = zfolder("normal");

            using (var tempFolder = new TempDirectory(GetTestFilePath()))
            {
                ZipFile.ExtractToDirectory(zipFileName, tempFolder.Path, Encoding.UTF8, overwriteFiles: false);
                Assert.Throws<IOException>(() => ZipFile.ExtractToDirectory(zipFileName, tempFolder.Path, Encoding.UTF8 /* default false */));
                Assert.Throws<IOException>(() => ZipFile.ExtractToDirectory(zipFileName, tempFolder.Path, Encoding.UTF8, overwriteFiles: false));
                ZipFile.ExtractToDirectory(zipFileName, tempFolder.Path, Encoding.UTF8, overwriteFiles: true);

                DirsEqual(tempFolder.Path, folderName);
            }
        }

        [Fact]
        public void ExtractToDirectoryZipArchiveOverwrite()
        {
            string zipFileName = zfile("normal.zip");
            string folderName = zfolder("normal");

            using (var tempFolder = new TempDirectory(GetTestFilePath()))
            {
                using (ZipArchive archive = ZipFile.Open(zipFileName, ZipArchiveMode.Read))
                {
                    archive.ExtractToDirectory(tempFolder.Path);
                    Assert.Throws<IOException>(() => archive.ExtractToDirectory(tempFolder.Path /* default false */));
                    Assert.Throws<IOException>(() => archive.ExtractToDirectory(tempFolder.Path, overwriteFiles: false));
                    archive.ExtractToDirectory(tempFolder.Path, overwriteFiles: true);

                    DirsEqual(tempFolder.Path, folderName);
                }
            }
        }
    }
}
