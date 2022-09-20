// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace System.Formats.Tar.Tests
{
    public partial class TarWriter_WriteEntryAsync_File_Tests : TarWriter_File_Base
    {
        [Fact]
        public async Task ThrowIf_AddFile_AfterDispose_Async()
        {
            using MemoryStream archiveStream = new MemoryStream();
            TarWriter writer = new TarWriter(archiveStream);
            await writer.DisposeAsync();

            await Assert.ThrowsAsync<ObjectDisposedException>(() => writer.WriteEntryAsync("fileName", "entryName"));
        }

        [Fact]
        public async Task FileName_NullOrEmpty_Async()
        {
            using MemoryStream archiveStream = new MemoryStream();
            await using (TarWriter writer = new TarWriter(archiveStream))
            {
                await Assert.ThrowsAsync<ArgumentNullException>(() => writer.WriteEntryAsync(null, "entryName"));
                await Assert.ThrowsAsync<ArgumentException>(() => writer.WriteEntryAsync(string.Empty, "entryName"));
            }
        }

        [Fact]
        public async Task EntryName_NullOrEmpty_Async()
        {
            using (TempDirectory root = new TempDirectory())
            {
                string file1Name = "file1.txt";
                string file2Name = "file2.txt";

                string file1Path = Path.Join(root.Path, file1Name);
                string file2Path = Path.Join(root.Path, file2Name);

                File.Create(file1Path).Dispose();
                File.Create(file2Path).Dispose();

                await using (MemoryStream archiveStream = new MemoryStream())
                {
                    await using (TarWriter writer = new TarWriter(archiveStream, TarEntryFormat.Pax, leaveOpen: true))
                    {
                        await writer.WriteEntryAsync(file1Path, null);
                        await writer.WriteEntryAsync(file2Path, string.Empty);
                    }

                    archiveStream.Seek(0, SeekOrigin.Begin);
                    await using (TarReader reader = new TarReader(archiveStream))
                    {
                        TarEntry first = await reader.GetNextEntryAsync();
                        Assert.NotNull(first);
                        Assert.Equal(file1Name, first.Name);

                        TarEntry second = await reader.GetNextEntryAsync();
                        Assert.NotNull(second);
                        Assert.Equal(file2Name, second.Name);

                        Assert.Null(await reader.GetNextEntryAsync());
                    }
                }
            }
        }

        [Theory]
        [InlineData(TarEntryFormat.V7)]
        [InlineData(TarEntryFormat.Ustar)]
        [InlineData(TarEntryFormat.Pax)]
        [InlineData(TarEntryFormat.Gnu)]
        public async Task Add_File_Async(TarEntryFormat format)
        {
            using (TempDirectory root = new TempDirectory())
            {
                string fileName = "file.txt";
                string filePath = Path.Join(root.Path, fileName);
                string fileContents = "Hello world";

                using (StreamWriter streamWriter = File.CreateText(filePath))
                {
                    streamWriter.Write(fileContents);
                }

                await using (MemoryStream archive = new MemoryStream())
                {
                    await using (TarWriter writer = new TarWriter(archive, format, leaveOpen: true))
                    {
                        await writer.WriteEntryAsync(fileName: filePath, entryName: fileName);
                    }

                    archive.Seek(0, SeekOrigin.Begin);
                    await using (TarReader reader = new TarReader(archive))
                    {
                        TarEntry entry = await reader.GetNextEntryAsync();
                        Assert.NotNull(entry);
                        Assert.Equal(format, entry.Format);
                        Assert.Equal(fileName, entry.Name);
                        TarEntryType expectedEntryType = format is TarEntryFormat.V7 ? TarEntryType.V7RegularFile : TarEntryType.RegularFile;
                        Assert.Equal(expectedEntryType, entry.EntryType);
                        Assert.True(entry.Length > 0);
                        Assert.NotNull(entry.DataStream);

                        entry.DataStream.Seek(0, SeekOrigin.Begin);
                        using StreamReader dataReader = new StreamReader(entry.DataStream);
                        string dataContents = dataReader.ReadLine();

                        Assert.Equal(fileContents, dataContents);

                        VerifyPlatformSpecificMetadata(filePath, entry);

                        Assert.Null(await reader.GetNextEntryAsync());
                    }
                }
            }
        }

        [Theory]
        [InlineData(TarEntryFormat.V7, false)]
        [InlineData(TarEntryFormat.V7, true)]
        [InlineData(TarEntryFormat.Ustar, false)]
        [InlineData(TarEntryFormat.Ustar, true)]
        [InlineData(TarEntryFormat.Pax, false)]
        [InlineData(TarEntryFormat.Pax, true)]
        [InlineData(TarEntryFormat.Gnu, false)]
        [InlineData(TarEntryFormat.Gnu, true)]
        public async Task Add_Directory_Async(TarEntryFormat format, bool withContents)
        {
            using (TempDirectory root = new TempDirectory())
            {
                string dirName = "dir";
                string dirPath = Path.Join(root.Path, dirName);
                Directory.CreateDirectory(dirPath);

                if (withContents)
                {
                    // Add a file inside the directory, we need to ensure the contents
                    // of the directory are ignored when using AddFile
                    File.Create(Path.Join(dirPath, "file.txt")).Dispose();
                }

                await using (MemoryStream archive = new MemoryStream())
                {
                    await using (TarWriter writer = new TarWriter(archive, format, leaveOpen: true))
                    {
                        await writer.WriteEntryAsync(fileName: dirPath, entryName: dirName);
                    }

                    archive.Seek(0, SeekOrigin.Begin);
                    await using (TarReader reader = new TarReader(archive))
                    {
                        TarEntry entry = await reader.GetNextEntryAsync();
                        Assert.Equal(format, entry.Format);

                        Assert.NotNull(entry);
                        Assert.Equal(dirName, entry.Name);
                        Assert.Equal(TarEntryType.Directory, entry.EntryType);
                        Assert.Null(entry.DataStream);

                        VerifyPlatformSpecificMetadata(dirPath, entry);

                        Assert.Null(await reader.GetNextEntryAsync()); // If the dir had contents, they should've been excluded
                    }
                }
            }
        }

        [ConditionalTheory(typeof(MountHelper), nameof(MountHelper.CanCreateSymbolicLinks))]
        [InlineData(TarEntryFormat.V7, false)]
        [InlineData(TarEntryFormat.V7, true)]
        [InlineData(TarEntryFormat.Ustar, false)]
        [InlineData(TarEntryFormat.Ustar, true)]
        [InlineData(TarEntryFormat.Pax, false)]
        [InlineData(TarEntryFormat.Pax, true)]
        [InlineData(TarEntryFormat.Gnu, false)]
        [InlineData(TarEntryFormat.Gnu, true)]
        public async Task Add_SymbolicLink_Async(TarEntryFormat format, bool createTarget)
        {
            using (TempDirectory root = new TempDirectory())
            {
                string targetName = "file.txt";
                string linkName = "link.txt";
                string targetPath = Path.Join(root.Path, targetName);
                string linkPath = Path.Join(root.Path, linkName);

                if (createTarget)
                {
                    File.Create(targetPath).Dispose();
                }

                FileInfo linkInfo = new FileInfo(linkPath);
                linkInfo.CreateAsSymbolicLink(targetName);

                await using (MemoryStream archive = new MemoryStream())
                {
                    await using (TarWriter writer = new TarWriter(archive, format, leaveOpen: true))
                    {
                        await writer.WriteEntryAsync(fileName: linkPath, entryName: linkName);
                    }

                    archive.Seek(0, SeekOrigin.Begin);
                    await using (TarReader reader = new TarReader(archive))
                    {
                        TarEntry entry = await reader.GetNextEntryAsync();
                        Assert.Equal(format, entry.Format);

                        Assert.NotNull(entry);
                        Assert.Equal(linkName, entry.Name);
                        Assert.Equal(targetName, entry.LinkName);
                        Assert.Equal(TarEntryType.SymbolicLink, entry.EntryType);
                        Assert.Null(entry.DataStream);

                        VerifyPlatformSpecificMetadata(linkPath, entry);

                        Assert.Null(await reader.GetNextEntryAsync());
                    }
                }
            }
        }

        [Theory]
        [MemberData(nameof(WriteEntry_CopyArchive_Data))]
        public async Task WriteEntry_CopyArchiveAsync(TestTarFormat testFormat, string testCaseName, CompressionMethod compressionMethod, bool copyData)
        {
            TarEntryFormat entryFormat = GetEntryFormatForTestTarFormat(testFormat);
            string pathWithExpectedFiles = GetTestCaseUnarchivedFolderPath(testCaseName);
            pathWithExpectedFiles = PathInternal.EnsureTrailingSeparator(pathWithExpectedFiles);

            await using Stream fileMemoryStream = GetTarMemoryStream(compressionMethod, testFormat, testCaseName);
            await using Stream originArchive = compressionMethod == CompressionMethod.GZip ? new GZipStream(fileMemoryStream, CompressionMode.Decompress) : fileMemoryStream;

            await VerifyCopyArchiveAsync(pathWithExpectedFiles, originArchive, entryFormat, copyData);
        }

        protected async Task VerifyCopyArchiveAsync(string pathWithExpectedFiles, Stream originArchive, TarEntryFormat entryFormat, bool copyData)
        {
            TarEntry entry;

            await using MemoryStream destinationArchive = new MemoryStream();
            await using (TarReader reader = new TarReader(originArchive, leaveOpen: false))
            {
                await using (TarWriter writer = new TarWriter(destinationArchive, entryFormat, leaveOpen: true))
                {
                    while ((entry = await reader.GetNextEntryAsync(copyData)) != null)
                    {
                        if (entry is PaxTarEntry paxEntry)
                        {
                            paxEntry.GroupName = TestLongGName;
                            paxEntry.UserName = TestLongUName;
                        }
                        await writer.WriteEntryAsync(entry);
                    }
                }
            }

            destinationArchive.Position = 0;

            Dictionary<string, FileSystemInfo> expectedEntries = GetFileSystemInfosRecursive(pathWithExpectedFiles).ToDictionary(fsi =>
                fsi.FullName.Substring(pathWithExpectedFiles.Length).Replace(Path.DirectorySeparatorChar, '/'));

            int count = 0;
            await using (TarReader destinationReader = new TarReader(destinationArchive, leaveOpen: false))
            {
                while ((entry = await destinationReader.GetNextEntryAsync(copyData)) != null)
                {
                    if (entry is PaxGlobalExtendedAttributesTarEntry) // Metadata entry
                    {
                        continue;
                    }
                    string keyPath = Path.TrimEndingDirectorySeparator(entry.Name);
                    Assert.True(expectedEntries.TryGetValue(keyPath, out FileSystemInfo expectedFSI), $"Entry '{keyPath}' not found in FileSystemInfos dictionary.");

                    // Cannot compare entry.LinkName with expectedFSI.LinkTarget because links in runtime-assets are not stored by nuget as such, but as regular files
                    if (entry.EntryType is TarEntryType.HardLink or TarEntryType.SymbolicLink)
                    {
                        AssertExtensions.GreaterThan(entry.LinkName.Length, 0, $"Entry LinkName length is 0.");
                        string expectedLinkTargetPath = Path.Join(pathWithExpectedFiles, entry.LinkName);
                        Assert.True(Path.Exists(expectedLinkTargetPath), $"Link target does not exist: {expectedLinkTargetPath}");
                    }
                    else
                    {
                        Assert.Equal(string.Empty, entry.LinkName);
                    }

                    if (entry is PaxTarEntry paxEntry)
                    {
                        Assert.Equal(TestLongGName, paxEntry.GroupName);
                        Assert.Equal(TestLongUName, paxEntry.UserName);
                    }

                    count++;
                }
            }
            Assert.Equal(expectedEntries.Count, count);
        }
    }
}
