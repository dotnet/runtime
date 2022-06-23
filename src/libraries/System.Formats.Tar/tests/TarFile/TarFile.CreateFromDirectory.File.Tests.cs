// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
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

            using FileStream fileStream = File.OpenRead(destinationArchiveFileName);
            using TarReader reader = new TarReader(fileStream);

            List<TarEntry> entries = new List<TarEntry>();

            TarEntry entry;
            while ((entry = reader.GetNextEntry()) != null)
            {
                entries.Add(entry);
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

            TarEntry entry = reader.GetNextEntry();
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
}
