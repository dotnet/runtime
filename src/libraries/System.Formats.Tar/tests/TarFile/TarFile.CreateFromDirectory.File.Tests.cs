// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;

namespace System.Formats.Tar.Tests
{
    public class TarFile_CreateFromDirectory_File_Tests : TarTestsBase
    {
        [Fact]
        public void InvalidPaths_Throw()
        {
            Assert.Throws<ArgumentNullException>(() => TarFile.CreateFromDirectory(sourceDirectoryName: null,destinationFileName: "path", includeBaseDirectory: false));
            Assert.Throws<ArgumentException>(() => TarFile.CreateFromDirectory(sourceDirectoryName: string.Empty,destinationFileName: "path", includeBaseDirectory: false));
            Assert.Throws<ArgumentNullException>(() => TarFile.CreateFromDirectory(sourceDirectoryName: "path",destinationFileName: null, includeBaseDirectory: false));
            Assert.Throws<ArgumentException>(() => TarFile.CreateFromDirectory(sourceDirectoryName: "path",destinationFileName: string.Empty, includeBaseDirectory: false));
        }

        [Fact]
        public void NonExistentDirectory_Throws()
        {
            using TempDirectory root = new TempDirectory();

            string dirPath = Path.Join(root.Path, "dir");
            string filePath = Path.Join(root.Path, "file.tar");

            Assert.Throws<DirectoryNotFoundException>(() => TarFile.CreateFromDirectory(sourceDirectoryName: "IDontExist", destinationFileName: filePath, includeBaseDirectory: false));
        }

        [Fact]
        public void DestinationExists_Throws()
        {
            using TempDirectory root = new TempDirectory();

            string dirPath = Path.Join(root.Path, "dir");
            Directory.CreateDirectory(dirPath);

            string filePath = Path.Join(root.Path, "file.tar");
            File.Create(filePath).Dispose();

            Assert.Throws<IOException>(() => TarFile.CreateFromDirectory(sourceDirectoryName: dirPath, destinationFileName: filePath, includeBaseDirectory: false));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void VerifyIncludeBaseDirectory(bool includeBaseDirectory)
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

            string subDirectoryName = "dir/"; // The trailing separator is preserved in the TarEntry.Name
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
            TarFile.CreateFromDirectory(source.Path, destinationArchiveFileName, includeBaseDirectory);

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

            string actualFileName2 = subDirectoryName + fileName2; // Notice the trailing separator in subDirectoryName
            TarEntry entry2 = entries.FirstOrDefault(x =>
                x.EntryType == TarEntryType.RegularFile &&
                x.Name == prefix + actualFileName2);
            Assert.NotNull(entry2);
            AssertEntryModeFromFileSystemEquals(entry2, filename2Mode);
        }

        [Fact]
        public void IncludeBaseDirectoryIfEmpty()
        {
            using TempDirectory source = new TempDirectory();
            using TempDirectory destination = new TempDirectory();

            string destinationArchiveFileName = Path.Join(destination.Path, "output.tar");
            TarFile.CreateFromDirectory(source.Path, destinationArchiveFileName, includeBaseDirectory: true);

            using FileStream fileStream = File.OpenRead(destinationArchiveFileName);
            using (TarReader reader = new TarReader(fileStream))
            {
                TarEntry entry = reader.GetNextEntry();
                Assert.NotNull(entry);
                Assert.Equal(TarEntryType.Directory, entry.EntryType);
                Assert.Equal(Path.GetFileName(source.Path) + '/', entry.Name);

                Assert.Null(reader.GetNextEntry());
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void IncludeAllSegmentsOfPath(bool includeBaseDirectory)
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

            TarFile.CreateFromDirectory(source.Path, destinationArchiveFileName, includeBaseDirectory);

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

        [ConditionalFact(typeof(MountHelper), nameof(MountHelper.CanCreateSymbolicLinks))]
        public void SkipRecursionIntoDirectorySymlinks()
        {
            using TempDirectory root = new TempDirectory();

            string destinationArchive = Path.Join(root.Path, "destination.tar");

            string externalDirectory = Path.Join(root.Path, "externalDirectory");
            Directory.CreateDirectory(externalDirectory);

            File.Create(Path.Join(externalDirectory, "file.txt")).Dispose();

            string sourceDirectoryName = Path.Join(root.Path, "baseDirectory");
            Directory.CreateDirectory(sourceDirectoryName);

            string subDirectory = Path.Join(sourceDirectoryName, "subDirectory");
            Directory.CreateSymbolicLink(subDirectory, externalDirectory); // Should not recurse here

            TarFile.CreateFromDirectory(sourceDirectoryName, destinationArchive, includeBaseDirectory: false);

            using FileStream archiveStream = File.OpenRead(destinationArchive);
            using TarReader reader = new(archiveStream, leaveOpen: false);

            TarEntry entry = reader.GetNextEntry();
            Assert.NotNull(entry);
            Assert.Equal("subDirectory/", entry.Name);
            Assert.Equal(TarEntryType.SymbolicLink, entry.EntryType);

            Assert.Null(reader.GetNextEntry()); // file.txt should not be found
        }

        [ConditionalFact(typeof(MountHelper), nameof(MountHelper.CanCreateSymbolicLinks))]
        public void SkipRecursionIntoBaseDirectorySymlink()
        {
            using TempDirectory root = new TempDirectory();

            string destinationArchive = Path.Join(root.Path, "destination.tar");

            string externalDirectory = Path.Join(root.Path, "externalDirectory");
            Directory.CreateDirectory(externalDirectory);

            string subDirectory = Path.Join(externalDirectory, "subDirectory");
            Directory.CreateDirectory(subDirectory);

            string sourceDirectoryName = Path.Join(root.Path, "baseDirectory");
            Directory.CreateSymbolicLink(sourceDirectoryName, externalDirectory);

            TarFile.CreateFromDirectory(sourceDirectoryName, destinationArchive, includeBaseDirectory: true); // Base directory is a symlink, do not recurse

            using FileStream archiveStream = File.OpenRead(destinationArchive);
            using TarReader reader = new(archiveStream, leaveOpen: false);

            TarEntry entry = reader.GetNextEntry();
            Assert.NotNull(entry);
            Assert.Equal("baseDirectory/", entry.Name);
            Assert.Equal(TarEntryType.SymbolicLink, entry.EntryType);

            Assert.Null(reader.GetNextEntry());
        }

        [ConditionalTheory(nameof(CanExtractWithTarTool))]
        [MemberData(nameof(GetPaxAndGnuTestCaseNames))]
        public void ResultingArchive_CanBeExtractedByOtherTools(string testCaseName)
        {
            if (testCaseName == "specialfiles" && !PlatformDetection.IsSuperUser)
            {
                throw new SkipTestException("specialfiles is only supported on Unix and it needs sudo permissions for extraction.");
            }

            string[] symlinkTestCases = { "file_symlink", "foldersymlink_folder_subfolder_file", "file_longsymlink" };
            if (symlinkTestCases.Contains(testCaseName) && !MountHelper.CanCreateSymbolicLinks)
            {
                throw new SkipTestException("Symlinks are not supported on this platform.");
            }

            using TempDirectory root = new TempDirectory();

            string archivePath = GetTarFilePath(CompressionMethod.Uncompressed, TestTarFormat.pax.ToString(), testCaseName);
            string tarExtractedPath = Path.Join(root.Path, "tarExtracted");
            Directory.CreateDirectory(tarExtractedPath);

            // extract with /usr/bin/tar
            ExtractWithTarTool(archivePath, tarExtractedPath);

            // create archive with TarFile
            string dotnetTarArchive = Path.Join(root.Path, "dotnetTarArchive.tar");
            TarFile.CreateFromDirectory(tarExtractedPath, dotnetTarArchive, includeBaseDirectory: false);

            // extract TarFile's archive with /usr/bin/tar
            string dotnetTarExtractedPath = Path.Join(root.Path, "dotnetTarExtracted");
            Directory.CreateDirectory(dotnetTarExtractedPath);
            ExtractWithTarTool(dotnetTarArchive, dotnetTarExtractedPath);

            // compare results
            Dictionary<string, FileSystemInfo> expectedEntries = GetFileSystemInfosRecursive(tarExtractedPath).ToDictionary(fsi => fsi.FullName.Substring(tarExtractedPath.Length));

            foreach (FileSystemInfo actualEntry in GetFileSystemInfosRecursive(dotnetTarExtractedPath))
            {
                string keyPath = actualEntry.FullName.Substring(dotnetTarExtractedPath.Length);
                Assert.True(expectedEntries.TryGetValue(keyPath, out FileSystemInfo expectedEntry), $"Entry '{keyPath}' not expected.");
                Assert.Equal(expectedEntry.Attributes, actualEntry.Attributes);
                Assert.Equal(expectedEntry.LinkTarget, actualEntry.LinkTarget);

                expectedEntries.Remove(keyPath);
            }

            // All expected entries should've been found and removed.
            Assert.Equal(0, expectedEntries.Count);
        }
    }
}
