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
        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task CreateFromDirectoryNormal(bool async)
        {
            string folderName = zfolder("normal");
            string noBaseDir = GetTestFilePath();
            await CallZipFileCreateFromDirectory(async, folderName, noBaseDir);

            await IsZipSameAsDir(noBaseDir, folderName, ZipArchiveMode.Read, requireExplicit: false, checkTimes: false, async);
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task CreateFromDirectory_IncludeBaseDirectory(bool async)
        {
            string folderName = zfolder("normal");
            string withBaseDir = GetTestFilePath();
            await CallZipFileCreateFromDirectory(async, folderName, withBaseDir, CompressionLevel.Optimal, true);

            IEnumerable<string> expected = Directory.EnumerateFiles(zfolder("normal"), "*", SearchOption.AllDirectories);

            ZipArchive actual_withbasedir = await CallZipFileOpen(async, withBaseDir, ZipArchiveMode.Read);

            foreach (ZipArchiveEntry actualEntry in actual_withbasedir.Entries)
            {
                string expectedFile = expected.Single(i => Path.GetFileName(i).Equals(actualEntry.Name));
                Assert.StartsWith("normal", actualEntry.FullName);
                Assert.Equal(new FileInfo(expectedFile).Length, actualEntry.Length);
                using Stream expectedStream = File.OpenRead(expectedFile);
                Stream actualStream = await OpenEntryStream(async, actualEntry);
                StreamsEqual(expectedStream, actualStream);
                await DisposeStream(async, actualStream);
            }

            await DisposeZipArchive(async, actual_withbasedir);
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task CreateFromDirectoryUnicode(bool async)
        {
            string folderName = zfolder("unicode");
            string noBaseDir = GetTestFilePath();
            await CallZipFileCreateFromDirectory(async, folderName, noBaseDir);

            ZipArchive archive = await CallZipFileOpenRead(async, noBaseDir);

            IEnumerable<string> actual = archive.Entries.Select(entry => entry.Name);
            IEnumerable<string> expected = Directory.EnumerateFileSystemEntries(zfolder("unicode"), "*", SearchOption.AllDirectories).ToList();
            Assert.True(Enumerable.SequenceEqual(expected.Select(i => Path.GetFileName(i)), actual.Select(i => i)));

            await DisposeZipArchive(async, archive);
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task CreatedEmptyDirectoriesRoundtrip(bool async)
        {
            string folderName = "empty1";
            using (var tempFolder = new TempDirectory(GetTestFilePath()))
            {
                DirectoryInfo rootDir = new DirectoryInfo(tempFolder.Path);
                rootDir.CreateSubdirectory(folderName);

                string archivePath = GetTestFilePath();
                await CallZipFileCreateFromDirectory(async, rootDir.FullName, archivePath,
                    CompressionLevel.Optimal, includeBaseDirectory: false, Encoding.UTF8);

                ZipArchive archive = await CallZipFileOpenRead(async, archivePath);

                Assert.Equal(1, archive.Entries.Count);
                Assert.StartsWith(folderName, archive.Entries[0].FullName);

                await DisposeZipArchive(async, archive);
            }
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task CreatedEmptyUtf32DirectoriesRoundtrip(bool async)
        {
            using (var tempFolder = new TempDirectory(GetTestFilePath()))
            {
                Encoding entryEncoding = Encoding.UTF32;
                DirectoryInfo rootDir = new DirectoryInfo(tempFolder.Path);
                string folderName = "empty1";
                rootDir.CreateSubdirectory(folderName);

                string archivePath = GetTestFilePath();
                await CallZipFileCreateFromDirectory(async, rootDir.FullName, archivePath,
                    CompressionLevel.Optimal, false, entryEncoding);

                ZipArchive archive = await CallZipFileOpen(async, archivePath, ZipArchiveMode.Read, entryEncoding);

                Assert.Equal(1, archive.Entries.Count);
                Assert.StartsWith(folderName, archive.Entries[0].FullName);

                await DisposeZipArchive(async, archive);
            }
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task CreatedEmptyRootDirectoryRoundtrips(bool async)
        {
            using (var tempFolder = new TempDirectory(GetTestFilePath()))
            {
                DirectoryInfo emptyRoot = new DirectoryInfo(tempFolder.Path);
                string archivePath = GetTestFilePath();
                await CallZipFileCreateFromDirectory(async, emptyRoot.FullName,
                    archivePath, CompressionLevel.Optimal, includeBaseDirectory: true);

                ZipArchive archive = await CallZipFileOpenRead(async, archivePath);
                Assert.Equal(1, archive.Entries.Count);
                await DisposeZipArchive(async, archive);
            }
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task CreateSetsExternalAttributesCorrectly(bool async)
        {
            string folderName = zfolder("normal");
            string filepath = GetTestFilePath();
            await CallZipFileCreateFromDirectory(async, folderName, filepath);

            ZipArchive archive = await CallZipFileOpen(async, filepath, ZipArchiveMode.Read);

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

            await DisposeZipArchive(async, archive);
        }
    }
}
