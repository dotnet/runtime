// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Formats.Tar.Tests
{
    public class TarFile_CreateFromDirectoryAsync_File_Tests : TarTestsBase
    {
        [Fact]
        public Task CreateFromDirectoryAsync_Cancel()
        {
            CancellationTokenSource cs = new CancellationTokenSource();
            cs.Cancel();
            return Assert.ThrowsAsync<TaskCanceledException>(() => TarFile.CreateFromDirectoryAsync("directory", "file.tar", includeBaseDirectory: false, cs.Token));
        }

        [Fact]
        public async Task InvalidPaths_Throw_Async()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => TarFile.CreateFromDirectoryAsync(sourceDirectoryName: null,destinationFileName: "path", includeBaseDirectory: false));
            await Assert.ThrowsAsync<ArgumentException>(() => TarFile.CreateFromDirectoryAsync(sourceDirectoryName: string.Empty,destinationFileName: "path", includeBaseDirectory: false));
            await Assert.ThrowsAsync<ArgumentNullException>(() => TarFile.CreateFromDirectoryAsync(sourceDirectoryName: "path",destinationFileName: null, includeBaseDirectory: false));
            await Assert.ThrowsAsync<ArgumentException>(() => TarFile.CreateFromDirectoryAsync(sourceDirectoryName: "path",destinationFileName: string.Empty, includeBaseDirectory: false));
        }

        [Fact]
        public async Task NonExistentDirectory_Throws_Async()
        {
            using (TempDirectory root = new TempDirectory())
            {
                string dirPath = Path.Join(root.Path, "dir");
                string filePath = Path.Join(root.Path, "file.tar");

                await Assert.ThrowsAsync<DirectoryNotFoundException>(() => TarFile.CreateFromDirectoryAsync(sourceDirectoryName: "IDontExist", destinationFileName: filePath, includeBaseDirectory: false));
            }
        }

        [Fact]
        public async Task DestinationExists_Throws_Async()
        {
            using (TempDirectory root = new TempDirectory())
            {
                string dirPath = Path.Join(root.Path, "dir");
                Directory.CreateDirectory(dirPath);

                string filePath = Path.Join(root.Path, "file.tar");
                File.Create(filePath).Dispose();

                await Assert.ThrowsAsync<IOException>(() => TarFile.CreateFromDirectoryAsync(sourceDirectoryName: dirPath, destinationFileName: filePath, includeBaseDirectory: false));
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task VerifyIncludeBaseDirectory_Async(bool includeBaseDirectory)
        {
            using (TempDirectory source = new TempDirectory())
            using (TempDirectory destination = new TempDirectory())
            {
                string fileName1 = "file1.txt";
                string filePath1 = Path.Join(source.Path, fileName1);
                File.Create(filePath1).Dispose();

                string subDirectoryName = "dir/"; // The trailing separator is preserved in the TarEntry.Name
                string subDirectoryPath = Path.Join(source.Path, subDirectoryName);
                Directory.CreateDirectory(subDirectoryPath);

                string fileName2 = "file2.txt";
                string filePath2 = Path.Join(subDirectoryPath, fileName2);
                File.Create(filePath2).Dispose();

                string destinationArchiveFileName = Path.Join(destination.Path, "output.tar");
                TarFile.CreateFromDirectory(source.Path, destinationArchiveFileName, includeBaseDirectory);

                List<TarEntry> entries = new List<TarEntry>();

                FileStreamOptions readOptions = new()
                {
                    Access = FileAccess.Read,
                    Mode = FileMode.Open,
                    Options = FileOptions.Asynchronous,
                };

                await using (FileStream fileStream = File.Open(destinationArchiveFileName, readOptions))
                {
                    await using (TarReader reader = new TarReader(fileStream))
                    {
                        TarEntry entry;
                        while ((entry = await reader.GetNextEntryAsync()) != null)
                        {
                            entries.Add(entry);
                        }
                    }
                }

                Assert.Equal(3, entries.Count);

                string prefix = includeBaseDirectory ? Path.GetFileName(source.Path) + '/' : string.Empty;

                TarEntry entry1 = entries.FirstOrDefault(x =>
                    x.EntryType == TarEntryType.RegularFile &&
                    x.Name == prefix + fileName1);
                Assert.NotNull(entry1);

                TarEntry directory = entries.FirstOrDefault(x =>
                    x.EntryType == TarEntryType.Directory &&
                    x.Name == prefix + subDirectoryName);
                Assert.NotNull(directory);

                string actualFileName2 = subDirectoryName + fileName2; // Notice the trailing separator in subDirectoryName
                TarEntry entry2 = entries.FirstOrDefault(x =>
                    x.EntryType == TarEntryType.RegularFile &&
                    x.Name == prefix + actualFileName2);
                Assert.NotNull(entry2);
            }
        }

        [Fact]
        public async Task IncludeBaseDirectoryIfEmpty_Async()
        {
            using (TempDirectory source = new TempDirectory())
            using (TempDirectory destination = new TempDirectory())
            {
                string destinationArchiveFileName = Path.Join(destination.Path, "output.tar");

                await TarFile.CreateFromDirectoryAsync(source.Path, destinationArchiveFileName, includeBaseDirectory: true);

                FileStreamOptions readOptions = new()
                {
                    Access = FileAccess.Read,
                    Mode = FileMode.Open,
                    Options = FileOptions.Asynchronous,
                };

                await using (FileStream fileStream = File.Open(destinationArchiveFileName, readOptions))
                {
                    await using (TarReader reader = new TarReader(fileStream))
                    {
                        TarEntry entry = await reader.GetNextEntryAsync();
                        Assert.NotNull(entry);
                        Assert.Equal(TarEntryType.Directory, entry.EntryType);
                        Assert.Equal(Path.GetFileName(source.Path) + '/', entry.Name);

                        Assert.Null(await reader.GetNextEntryAsync());
                    }
                }
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task IncludeAllSegmentsOfPath_Async(bool includeBaseDirectory)
        {
            using (TempDirectory source = new TempDirectory())
            using (TempDirectory destination = new TempDirectory())
            {
                string segment1 = Path.Join(source.Path, "segment1");
                Directory.CreateDirectory(segment1);
                string segment2 = Path.Join(segment1, "segment2");
                Directory.CreateDirectory(segment2);
                string textFile = Path.Join(segment2, "file.txt");
                File.Create(textFile).Dispose();

                string destinationArchiveFileName = Path.Join(destination.Path, "output.tar");

                await TarFile.CreateFromDirectoryAsync(source.Path, destinationArchiveFileName, includeBaseDirectory);

                FileStreamOptions readOptions = new()
                {
                    Access = FileAccess.Read,
                    Mode = FileMode.Open,
                    Options = FileOptions.Asynchronous,
                };

                await using (FileStream fileStream = File.Open(destinationArchiveFileName, readOptions))
                {
                    await using (TarReader reader = new TarReader(fileStream))
                    {
                        string prefix = includeBaseDirectory ? Path.GetFileName(source.Path) + '/' : string.Empty;

                        TarEntry entry = await reader.GetNextEntryAsync();
                        Assert.NotNull(entry);
                        Assert.Equal(TarEntryType.Directory, entry.EntryType);
                        Assert.Equal(prefix + "segment1/", entry.Name);

                        entry = await reader.GetNextEntryAsync();
                        Assert.NotNull(entry);
                        Assert.Equal(TarEntryType.Directory, entry.EntryType);
                        Assert.Equal(prefix + "segment1/segment2/", entry.Name);

                        entry = await reader.GetNextEntryAsync();
                        Assert.NotNull(entry);
                        Assert.Equal(TarEntryType.RegularFile, entry.EntryType);
                        Assert.Equal(prefix + "segment1/segment2/file.txt", entry.Name);

                        Assert.Null(await reader.GetNextEntryAsync());
                    }
                }
            }
        }
    }
}
