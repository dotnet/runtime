// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Linq;
using Xunit;

namespace System.Formats.Tar.Tests
{
    public partial class TarFile_ExtractToDirectory_File_Tests : TarTestsBase
    {
        [Fact]
        public void InvalidPaths_Throw()
        {
            Assert.Throws<ArgumentNullException>(() => TarFile.ExtractToDirectory(sourceFileName: null, destinationDirectoryName: "path", overwriteFiles: false));
            Assert.Throws<ArgumentException>(() => TarFile.ExtractToDirectory(sourceFileName: string.Empty, destinationDirectoryName: "path", overwriteFiles: false));
            Assert.Throws<ArgumentNullException>(() => TarFile.ExtractToDirectory(sourceFileName: "path", destinationDirectoryName: null, overwriteFiles: false));
            Assert.Throws<ArgumentException>(() => TarFile.ExtractToDirectory(sourceFileName: "path", destinationDirectoryName: string.Empty, overwriteFiles: false));
        }

        [Fact]
        public void NonExistentFile_Throws()
        {
            using TempDirectory root = new TempDirectory();

            string filePath = Path.Join(root.Path, "file.tar");
            string dirPath = Path.Join(root.Path, "dir");

            Directory.CreateDirectory(dirPath);

            Assert.Throws<FileNotFoundException>(() => TarFile.ExtractToDirectory(sourceFileName: filePath, destinationDirectoryName: dirPath, overwriteFiles: false));
        }

        [Fact]
        public void NonExistentDirectory_Throws()
        {
            using TempDirectory root = new TempDirectory();

            string filePath = Path.Join(root.Path, "file.tar");
            string dirPath = Path.Join(root.Path, "dir");

            File.Create(filePath).Dispose();

            Assert.Throws<DirectoryNotFoundException>(() => TarFile.ExtractToDirectory(sourceFileName: filePath, destinationDirectoryName: dirPath, overwriteFiles: false));
        }

        [Theory]
        [InlineData(TestTarFormat.v7)]
        [InlineData(TestTarFormat.ustar)]
        [InlineData(TestTarFormat.pax)]
        [InlineData(TestTarFormat.pax_gea)]
        [InlineData(TestTarFormat.gnu)]
        [InlineData(TestTarFormat.oldgnu)]
        public void Extract_Archive_File(TestTarFormat testFormat)
        {
            string sourceArchiveFileName = GetTarFilePath(CompressionMethod.Uncompressed, testFormat, "file");

            using TempDirectory destination = new TempDirectory();

            string filePath = Path.Join(destination.Path, "file.txt");

            TarFile.ExtractToDirectory(sourceArchiveFileName, destination.Path, overwriteFiles: false);

            Assert.True(File.Exists(filePath));
        }

        [Fact]
        public void Extract_Archive_File_OverwriteTrue()
        {
            string testCaseName = "file";
            string archivePath = GetTarFilePath(CompressionMethod.Uncompressed, TestTarFormat.pax, testCaseName);

            using TempDirectory destination = new TempDirectory();

            string filePath = Path.Join(destination.Path, "file.txt");
            using (FileStream fileStream = File.Create(filePath))
            {
                using StreamWriter writer = new StreamWriter(fileStream, leaveOpen: false);
                writer.WriteLine("Original text");
            }

            TarFile.ExtractToDirectory(archivePath, destination.Path, overwriteFiles: true);

            Assert.True(File.Exists(filePath));

            using (FileStream fileStream = File.Open(filePath, FileMode.Open))
            {
                using StreamReader reader = new StreamReader(fileStream);
                string actualContents = reader.ReadLine();
                Assert.Equal($"Hello {testCaseName}", actualContents); // Confirm overwrite
            }
        }

        [Fact]
        public void Extract_Archive_File_OverwriteFalse()
        {
            string sourceArchiveFileName = GetTarFilePath(CompressionMethod.Uncompressed, TestTarFormat.pax, "file");

            using TempDirectory destination = new TempDirectory();

            string filePath = Path.Join(destination.Path, "file.txt");

            File.Create(filePath).Dispose();

            Assert.Throws<IOException>(() => TarFile.ExtractToDirectory(sourceArchiveFileName, destination.Path, overwriteFiles: false));
        }

        [Fact]
        public void Extract_AllSegmentsOfPath()
        {
            using TempDirectory source = new TempDirectory();
            using TempDirectory destination = new TempDirectory();

            string archivePath = Path.Join(source.Path, "archive.tar");
            using FileStream archiveStream = File.Create(archivePath);
            using (TarWriter writer = new TarWriter(archiveStream))
            {
                PaxTarEntry segment1 = new PaxTarEntry(TarEntryType.Directory, "segment1");
                writer.WriteEntry(segment1);

                PaxTarEntry segment2 = new PaxTarEntry(TarEntryType.Directory, "segment1/segment2");
                writer.WriteEntry(segment2);

                PaxTarEntry file = new PaxTarEntry(TarEntryType.RegularFile, "segment1/segment2/file.txt");
                writer.WriteEntry(file);
            }

            TarFile.ExtractToDirectory(archivePath, destination.Path, overwriteFiles: false);

            string segment1Path = Path.Join(destination.Path, "segment1");
            Assert.True(Directory.Exists(segment1Path), $"{segment1Path}' does not exist.");

            string segment2Path = Path.Join(segment1Path, "segment2");
            Assert.True(Directory.Exists(segment2Path), $"{segment2Path}' does not exist.");

            string filePath = Path.Join(segment2Path, "file.txt");
            Assert.True(File.Exists(filePath), $"{filePath}' does not exist.");
        }
    }
}
