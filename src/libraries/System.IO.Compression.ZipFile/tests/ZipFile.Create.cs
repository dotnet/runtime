// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Compression.Tests
{
    public class ZipFile_Create : ZipFileTestBase
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
                    Assert.StartsWith("normal", actualEntry.FullName);
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
        public async Task CreateFromDirectory_IncludeBaseDirectoryAsync()
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
                    Assert.StartsWith("normal", actualEntry.FullName);
                    Assert.Equal(new FileInfo(expectedFile).Length, actualEntry.Length);
                    using (Stream expectedStream = File.OpenRead(expectedFile))
                    using (Stream actualStream = actualEntry.Open())
                    {
                        await StreamsEqualAsync(expectedStream, actualStream);
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
                    Assert.StartsWith("empty1", archive.Entries[0].FullName);
                }
            }
        }

        [Fact]
        public void CreatedEmptyUtf32DirectoriesRoundtrip()
        {
            using (var tempFolder = new TempDirectory(GetTestFilePath()))
            {
                Encoding entryEncoding = Encoding.UTF32;
                DirectoryInfo rootDir = new DirectoryInfo(tempFolder.Path);
                rootDir.CreateSubdirectory("empty1");

                string archivePath = GetTestFilePath();
                ZipFile.CreateFromDirectory(
                    rootDir.FullName, archivePath,
                    CompressionLevel.Optimal, false, entryEncoding);

                using (ZipArchive archive = ZipFile.Open(archivePath, ZipArchiveMode.Read, entryEncoding))
                {
                    Assert.Equal(1, archive.Entries.Count);
                    Assert.StartsWith("empty1", archive.Entries[0].FullName);
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

        [Fact]
        public void CreateSetsExternalAttributesCorrectly()
        {
            string folderName = zfolder("normal");
            string filepath = GetTestFilePath();
            ZipFile.CreateFromDirectory(folderName, filepath);

            using (ZipArchive archive = ZipFile.Open(filepath, ZipArchiveMode.Read))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    if (OperatingSystem.IsWindows())
                    {
                        Assert.Equal(0, entry.ExternalAttributes);
                    }
                    else
                    {
                        Assert.NotEqual(0, entry.ExternalAttributes);
                    }
                }
            }
        }
    }
}
