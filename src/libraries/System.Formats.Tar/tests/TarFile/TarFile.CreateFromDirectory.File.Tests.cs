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
    public class TarFile_CreateFromDirectory_File_Tests : TarTestsBase
    {
        [Fact]
        public Task CreateFromDirectoryAsync_Cancel()
        {
            CancellationTokenSource cs = new CancellationTokenSource();
            cs.Cancel();
            return Assert.ThrowsAsync<TaskCanceledException>(() => TarFile.CreateFromDirectoryAsync("directory", "file.tar", includeBaseDirectory: false, cs.Token));
        }

        [Theory]
        [MemberData(nameof(Get_Boolean_Data))]
        public async Task InvalidPaths_Throw(bool async)
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => CreateFromDirectory(sourceDirectoryName: null, destinationArchiveFileName: "path", includeBaseDirectory: false, async));
            await Assert.ThrowsAsync<ArgumentException>(() => CreateFromDirectory(sourceDirectoryName: string.Empty, destinationArchiveFileName: "path", includeBaseDirectory: false, async));
            await Assert.ThrowsAsync<ArgumentNullException>(() => CreateFromDirectory(sourceDirectoryName: "path", destinationArchiveFileName: null, includeBaseDirectory: false, async));
            await Assert.ThrowsAsync<ArgumentException>(() => CreateFromDirectory(sourceDirectoryName: "path", destinationArchiveFileName: string.Empty, includeBaseDirectory: false, async));
        }

        [Theory]
        [MemberData(nameof(Get_Boolean_Data))]
        public async Task NonExistentDirectory_Throws(bool async)
        {
            using TempDirectory root = new TempDirectory();
            string filePath = Path.Join(root.Path, "file.tar");

            await Assert.ThrowsAsync<DirectoryNotFoundException>(() => CreateFromDirectory(sourceDirectoryName: "IDontExist", destinationArchiveFileName: filePath, includeBaseDirectory: false, async));
        }

        [Theory]
        [MemberData(nameof(Get_Boolean_Data))]
        public async Task DestinationExists_Throws(bool async)
        {
            using TempDirectory root = new TempDirectory();

            string dirPath = Path.Join(root.Path, "dir");
            Directory.CreateDirectory(dirPath);

            string filePath = Path.Join(root.Path, "file.tar");
            File.Create(filePath).Dispose();

            await Assert.ThrowsAsync<IOException>(() => CreateFromDirectory(dirPath, filePath, includeBaseDirectory: false, async));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task VerifyIncludeBaseDirectory(bool includeBaseDirectory)
        {
            foreach (bool async in Booleans)
            {
                using TempDirectory source = new TempDirectory();
                using TempDirectory destination = new TempDirectory();

                UnixFileMode baseDirectoryMode = TestPermission1;
                SetUnixFileMode(source.Path, baseDirectoryMode);

                string fileName1 = "file1.txt";
                string filePath1 = Path.Join(source.Path, fileName1);
                File.Create(filePath1).Dispose();
                UnixFileMode filename1Mode = TestPermission2;
                SetUnixFileMode(filePath1, filename1Mode);

                string subDirectoryName = "dir/";
                string subDirectoryPath = Path.Join(source.Path, subDirectoryName);
                Directory.CreateDirectory(subDirectoryPath);
                UnixFileMode subDirectoryMode = TestPermission3;
                SetUnixFileMode(subDirectoryPath, subDirectoryMode);

                string fileName2 = "file2.txt";
                string filePath2 = Path.Join(subDirectoryPath, fileName2);
                File.Create(filePath2).Dispose();
                UnixFileMode filename2Mode = TestPermission4;
                SetUnixFileMode(filePath2, filename2Mode);

                string destinationArchiveFileName = Path.Join(destination.Path, "output.tar");
                await CreateFromDirectory(source.Path, destinationArchiveFileName, includeBaseDirectory, async);

                using FileStream fileStream = File.OpenRead(destinationArchiveFileName);
                using TarReader reader = new TarReader(fileStream);

                List<TarEntry> entries = new List<TarEntry>();
                TarEntry entry;
                while ((entry = reader.GetNextEntry()) != null)
                {
                    entries.Add(entry);
                }

                int expectedCount = 3 + (includeBaseDirectory ? 1 : 0);
                Assert.Equal(expectedCount, entries.Count);

                string prefix = includeBaseDirectory ? Path.GetFileName(source.Path) + '/' : string.Empty;

                if (includeBaseDirectory)
                {
                    TarEntry baseEntry = entries.FirstOrDefault(x =>
                        x.EntryType == TarEntryType.Directory &&
                        x.Name == prefix);
                    Assert.NotNull(baseEntry);
                    AssertEntryModeFromFileSystemEquals(baseEntry, baseDirectoryMode);
                }

                TarEntry entry1 = entries.FirstOrDefault(x =>
                    x.EntryType == TarEntryType.RegularFile &&
                    x.Name == prefix + fileName1);
                Assert.NotNull(entry1);
                AssertEntryModeFromFileSystemEquals(entry1, filename1Mode);

                TarEntry directory = entries.FirstOrDefault(x =>
                    x.EntryType == TarEntryType.Directory &&
                    x.Name == prefix + subDirectoryName);
                Assert.NotNull(directory);
                AssertEntryModeFromFileSystemEquals(directory, subDirectoryMode);

                string actualFileName2 = subDirectoryName + fileName2;
                TarEntry entry2 = entries.FirstOrDefault(x =>
                    x.EntryType == TarEntryType.RegularFile &&
                    x.Name == prefix + actualFileName2);
                Assert.NotNull(entry2);
                AssertEntryModeFromFileSystemEquals(entry2, filename2Mode);
            }
        }

        [Theory]
        [MemberData(nameof(Get_Boolean_Data))]
        public async Task IncludeBaseDirectoryIfEmpty(bool async)
        {
            using TempDirectory source = new TempDirectory();
            using TempDirectory destination = new TempDirectory();

            string destinationArchiveFileName = Path.Join(destination.Path, "output.tar");
            await CreateFromDirectory(source.Path, destinationArchiveFileName, includeBaseDirectory: true, async);

            using FileStream fileStream = File.OpenRead(destinationArchiveFileName);
            using TarReader reader = new TarReader(fileStream);

            TarEntry entry = reader.GetNextEntry();
            Assert.NotNull(entry);
            Assert.Equal(TarEntryType.Directory, entry.EntryType);
            Assert.Equal(Path.GetFileName(source.Path) + '/', entry.Name);

            Assert.Null(reader.GetNextEntry());
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task IncludeAllSegmentsOfPath(bool includeBaseDirectory)
        {
            foreach (bool async in Booleans)
            {
                using TempDirectory source = new TempDirectory();
                using TempDirectory destination = new TempDirectory();

                string segment1 = Path.Join(source.Path, "segment1");
                Directory.CreateDirectory(segment1);
                string segment2 = Path.Join(segment1, "segment2");
                Directory.CreateDirectory(segment2);
                string textFile = Path.Join(segment2, "file.txt");
                File.Create(textFile).Dispose();

                string destinationArchiveFileName = Path.Join(destination.Path, "output.tar");
                await CreateFromDirectory(source.Path, destinationArchiveFileName, includeBaseDirectory, async);

                using FileStream fileStream = File.OpenRead(destinationArchiveFileName);
                using TarReader reader = new TarReader(fileStream);

                string prefix = includeBaseDirectory ? Path.GetFileName(source.Path) + '/' : string.Empty;

                TarEntry entry;

                if (includeBaseDirectory)
                {
                    entry = reader.GetNextEntry();
                    Assert.NotNull(entry);
                    Assert.Equal(TarEntryType.Directory, entry.EntryType);
                    Assert.Equal(prefix, entry.Name);
                }

                entry = reader.GetNextEntry();
                Assert.NotNull(entry);
                Assert.Equal(TarEntryType.Directory, entry.EntryType);
                Assert.Equal(prefix + "segment1/", entry.Name);

                entry = reader.GetNextEntry();
                Assert.NotNull(entry);
                Assert.Equal(TarEntryType.Directory, entry.EntryType);
                Assert.Equal(prefix + "segment1/segment2/", entry.Name);

                entry = reader.GetNextEntry();
                Assert.NotNull(entry);
                Assert.Equal(TarEntryType.RegularFile, entry.EntryType);
                Assert.Equal(prefix + "segment1/segment2/file.txt", entry.Name);

                Assert.Null(reader.GetNextEntry());
            }
        }

        [ConditionalTheory(typeof(MountHelper), nameof(MountHelper.CanCreateSymbolicLinks))]
        [MemberData(nameof(Get_Boolean_Data))]
        public async Task SkipRecursionIntoDirectorySymlinks(bool async)
        {
            using TempDirectory root = new TempDirectory();

            string destinationArchive = Path.Join(root.Path, "destination.tar");

            string externalDirectory = Path.Join(root.Path, "externalDirectory");
            Directory.CreateDirectory(externalDirectory);

            File.Create(Path.Join(externalDirectory, "file.txt")).Dispose();

            string sourceDirectoryName = Path.Join(root.Path, "baseDirectory");
            Directory.CreateDirectory(sourceDirectoryName);

            string subDirectory = Path.Join(sourceDirectoryName, "subDirectory");
            Directory.CreateSymbolicLink(subDirectory, externalDirectory);

            await CreateFromDirectory(sourceDirectoryName, destinationArchive, includeBaseDirectory: false, async);

            using FileStream archiveStream = File.OpenRead(destinationArchive);
            using TarReader reader = new TarReader(archiveStream, leaveOpen: false);

            TarEntry entry = reader.GetNextEntry();
            Assert.NotNull(entry);
            Assert.Equal("subDirectory", entry.Name);
            Assert.Equal(TarEntryType.SymbolicLink, entry.EntryType);

            Assert.Null(reader.GetNextEntry());
        }

        [ConditionalTheory(typeof(MountHelper), nameof(MountHelper.CanCreateSymbolicLinks))]
        [MemberData(nameof(Get_Boolean_Data))]
        public async Task SkipRecursionIntoBaseDirectorySymlink(bool async)
        {
            using TempDirectory root = new TempDirectory();

            string destinationArchive = Path.Join(root.Path, "destination.tar");

            string externalDirectory = Path.Join(root.Path, "externalDirectory");
            Directory.CreateDirectory(externalDirectory);

            string subDirectory = Path.Join(externalDirectory, "subDirectory");
            Directory.CreateDirectory(subDirectory);

            string sourceDirectoryName = Path.Join(root.Path, "baseDirectory");
            Directory.CreateSymbolicLink(sourceDirectoryName, externalDirectory);

            await CreateFromDirectory(sourceDirectoryName, destinationArchive, includeBaseDirectory: true, async);

            using FileStream archiveStream = File.OpenRead(destinationArchive);
            using TarReader reader = new TarReader(archiveStream, leaveOpen: false);

            TarEntry entry = reader.GetNextEntry();
            Assert.NotNull(entry);
            Assert.Equal("baseDirectory/", entry.Name);
            Assert.Equal(TarEntryType.SymbolicLink, entry.EntryType);

            Assert.Null(reader.GetNextEntry());
        }

        [Theory]
        [MemberData(nameof(GetTarEntryFormats))]
        public async Task CreateFromDirectory_WithFormat(TarEntryFormat format)
        {
            foreach (bool async in Booleans)
            {
                using TempDirectory source = new TempDirectory();
                using TempDirectory destination = new TempDirectory();

                string fileName = "file.txt";
                File.Create(Path.Join(source.Path, fileName)).Dispose();

                string destinationArchiveFileName = Path.Join(destination.Path, "output.tar");
                await CreateFromDirectory(source.Path, destinationArchiveFileName, includeBaseDirectory: false, format, async);

                using FileStream fileStream = File.OpenRead(destinationArchiveFileName);
                using TarReader reader = new TarReader(fileStream);

                TarEntry entry = reader.GetNextEntry();
                Assert.NotNull(entry);
                Assert.Equal(format, entry.Format);
                Assert.Equal(fileName, entry.Name);

                Assert.Null(reader.GetNextEntry());
            }
        }

        [Theory]
        [MemberData(nameof(GetInvalidTarEntryFormats))]
        public async Task CreateFromDirectory_InvalidFormat_Throws(TarEntryFormat format)
        {
            foreach (bool async in Booleans)
            {
                using TempDirectory source = new TempDirectory();
                using TempDirectory destination = new TempDirectory();
                string destinationArchiveFileName = Path.Join(destination.Path, "output.tar");

                await Assert.ThrowsAsync<ArgumentOutOfRangeException>("format", () =>
                    CreateFromDirectory(source.Path, destinationArchiveFileName, includeBaseDirectory: false, format, async));
            }
        }

        [ConditionalTheory(typeof(MountHelper), nameof(MountHelper.CanCreateHardLinks))]
        [InlineData(true)]
        [InlineData(false)]
        public async Task CreateFromDirectory_UsesWriterOptions(bool toggle)
        {
            foreach (bool async in Booleans)
            {
                bool preserveLinks = toggle;

                using TempDirectory source = CreateSourceDirectoryForCreateFromDirectory_UsesWriterOptions();
                using TempDirectory destination = new TempDirectory();

                TarWriterOptions options = new TarWriterOptions()
                {
                    HardLinkMode = preserveLinks ? TarHardLinkMode.PreserveLink : TarHardLinkMode.CopyContents
                };

                string destinationArchiveFileName = Path.Join(destination.Path, "output.tar");
                await CreateFromDirectory(source.Path, destinationArchiveFileName, includeBaseDirectory: false, options, async);

                using FileStream fileStream = File.OpenRead(destinationArchiveFileName);
                VerifyCreateFromDirectory_UsesWriterOptions(fileStream, preserveLinks);
            }
        }
    }
}
