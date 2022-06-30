// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Formats.Tar.Tests
{
    public partial class TarFile_ExtractToDirectoryAsync_File_Tests : TarTestsBase
    {
        [Fact]
        public Task ExtractToDirectoryAsync_Cancel()
        {
            CancellationTokenSource cs = new CancellationTokenSource();
            cs.Cancel();
            return Assert.ThrowsAsync<TaskCanceledException>(() => TarFile.ExtractToDirectoryAsync("file.tar", "directory", overwriteFiles: true, cs.Token));
        }

        [Fact]
        public async Task InvalidPaths_Throw()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => TarFile.ExtractToDirectoryAsync(sourceFileName: null, destinationDirectoryName: "path", overwriteFiles: false));
            await Assert.ThrowsAsync<ArgumentException>(() => TarFile.ExtractToDirectoryAsync(sourceFileName: string.Empty, destinationDirectoryName: "path", overwriteFiles: false));
            await Assert.ThrowsAsync<ArgumentNullException>(() => TarFile.ExtractToDirectoryAsync(sourceFileName: "path", destinationDirectoryName: null, overwriteFiles: false));
            await Assert.ThrowsAsync<ArgumentException>(() => TarFile.ExtractToDirectoryAsync(sourceFileName: "path", destinationDirectoryName: string.Empty, overwriteFiles: false));
        }

        [Fact]
        public async Task NonExistentFile_Throws_Async()
        {
            using (TempDirectory root = new TempDirectory())
            {
                string filePath = Path.Join(root.Path, "file.tar");
                string dirPath = Path.Join(root.Path, "dir");

                Directory.CreateDirectory(dirPath);

                await Assert.ThrowsAsync<FileNotFoundException>(() => TarFile.ExtractToDirectoryAsync(sourceFileName: filePath, destinationDirectoryName: dirPath, overwriteFiles: false));
            }
        }

        [Fact]
        public async Task NonExistentDirectory_Throws_Async()
        {
            using (TempDirectory root = new TempDirectory())
            {
                string filePath = Path.Join(root.Path, "file.tar");
                string dirPath = Path.Join(root.Path, "dir");

                File.Create(filePath).Dispose();

                await Assert.ThrowsAsync<DirectoryNotFoundException>(() => TarFile.ExtractToDirectoryAsync(sourceFileName: filePath, destinationDirectoryName: dirPath, overwriteFiles: false));
            }
        }

        [Theory]
        [InlineData(TestTarFormat.v7)]
        [InlineData(TestTarFormat.ustar)]
        [InlineData(TestTarFormat.pax)]
        [InlineData(TestTarFormat.pax_gea)]
        [InlineData(TestTarFormat.gnu)]
        [InlineData(TestTarFormat.oldgnu)]
        public async Task Extract_Archive_File_Async(TestTarFormat testFormat)
        {
            string sourceArchiveFileName = GetTarFilePath(CompressionMethod.Uncompressed, testFormat, "file");

            using (TempDirectory destination = new TempDirectory())
            {
                string filePath = Path.Join(destination.Path, "file.txt");

                await TarFile.ExtractToDirectoryAsync(sourceArchiveFileName, destination.Path, overwriteFiles: false);

                Assert.True(File.Exists(filePath));
            }
        }

        [Fact]
        public async Task Extract_Archive_File_OverwriteTrue_Async()
        {
            string testCaseName = "file";
            string archivePath = GetTarFilePath(CompressionMethod.Uncompressed, TestTarFormat.pax, testCaseName);

            using (TempDirectory destination = new TempDirectory())
            {
                string filePath = Path.Join(destination.Path, "file.txt");
                using (FileStream fileStream = File.Create(filePath))
                {
                    using StreamWriter writer = new StreamWriter(fileStream, leaveOpen: false);
                    writer.WriteLine("Original text");
                }

                await TarFile.ExtractToDirectoryAsync(archivePath, destination.Path, overwriteFiles: true);

                Assert.True(File.Exists(filePath));

                using (FileStream fileStream = File.Open(filePath, FileMode.Open))
                {
                    using StreamReader reader = new StreamReader(fileStream);
                    string actualContents = reader.ReadLine();
                    Assert.Equal($"Hello {testCaseName}", actualContents); // Confirm overwrite
                }
            }
        }

        [Fact]
        public async Task Extract_Archive_File_OverwriteFalse_Async()
        {
            using (TempDirectory destination = new TempDirectory())
            {
                string sourceArchiveFileName = GetTarFilePath(CompressionMethod.Uncompressed, TestTarFormat.pax, "file");

                string filePath = Path.Join(destination.Path, "file.txt");

                File.Create(filePath).Dispose();

                await Assert.ThrowsAsync<IOException>(() => TarFile.ExtractToDirectoryAsync(sourceArchiveFileName, destination.Path, overwriteFiles: false));
            }
        }

        [Fact]
        public async Task Extract_AllSegmentsOfPath_Async()
        {
            using (TempDirectory source = new TempDirectory())
            {
                string archivePath = Path.Join(source.Path, "archive.tar");
                using (TempDirectory destination = new TempDirectory())
                {
                    FileStreamOptions fileOptions = new()
                    {
                        Access = FileAccess.Write,
                        Mode = FileMode.CreateNew,
                        Share = FileShare.None,
                        Options = FileOptions.Asynchronous
                    };

                    await using (FileStream archiveStream = new FileStream(archivePath, fileOptions))
                    {
                        await using (TarWriter writer = new TarWriter(archiveStream))
                        {
                            PaxTarEntry segment1 = new PaxTarEntry(TarEntryType.Directory, "segment1");
                            await writer.WriteEntryAsync(segment1);

                            PaxTarEntry segment2 = new PaxTarEntry(TarEntryType.Directory, "segment1/segment2");
                            await writer.WriteEntryAsync(segment2);

                            PaxTarEntry file = new PaxTarEntry(TarEntryType.RegularFile, "segment1/segment2/file.txt");
                            await writer.WriteEntryAsync(file);
                        }
                    }

                    await TarFile.ExtractToDirectoryAsync(archivePath, destination.Path, overwriteFiles: false);

                    string segment1Path = Path.Join(destination.Path, "segment1");
                    Assert.True(Directory.Exists(segment1Path), $"{segment1Path}' does not exist.");

                    string segment2Path = Path.Join(segment1Path, "segment2");
                    Assert.True(Directory.Exists(segment2Path), $"{segment2Path}' does not exist.");

                    string filePath = Path.Join(segment2Path, "file.txt");
                    Assert.True(File.Exists(filePath), $"{filePath}' does not exist.");
                }
            }
        }

        [Fact]
        public async Task ExtractArchiveWithEntriesThatStartWithSlashDotPrefix_Async()
        {
            using (TempDirectory root = new TempDirectory())
            {
                await using (MemoryStream archiveStream = GetStrangeTarMemoryStream("prefixDotSlashAndCurrentFolderEntry"))
                {
                    await TarFile.ExtractToDirectoryAsync(archiveStream, root.Path, overwriteFiles: true);

                    archiveStream.Position = 0;

                    await using (TarReader reader = new TarReader(archiveStream, leaveOpen: false))
                    {
                        TarEntry entry;
                        while ((entry = await reader.GetNextEntryAsync()) != null)
                        {
                            // Normalize the path (remove redundant segments), remove trailing separators
                            // this is so the first entry can be skipped if it's the same as the root directory
                            string entryPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(Path.Join(root.Path, entry.Name)));
                            Assert.True(Path.Exists(entryPath), $"Entry was not extracted: {entryPath}");
                        }
                    }
                }
            }
        }
    }
}
