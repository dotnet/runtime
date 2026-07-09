// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.IO.Enumeration;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Formats.Tar.Tests
{
    public class TarFile_ExtractToDirectory_Stream_Tests : TarFile_ExtractToDirectory_Tests
    {
        [Fact]
        public async Task ExtractToDirectoryAsync_Cancel()
        {
            CancellationTokenSource cs = new CancellationTokenSource();
            cs.Cancel();
            using MemoryStream archiveStream = new MemoryStream();
            await Assert.ThrowsAsync<TaskCanceledException>(() => TarFile.ExtractToDirectoryAsync(archiveStream, "directory", overwriteFiles: true, cs.Token));
        }

        [Theory]
        [MemberData(nameof(Get_Boolean_Data))]
        public Task NullStream_Throws(bool async) =>
            Assert.ThrowsAsync<ArgumentNullException>(() => ExtractToDirectory(source: null, destinationDirectoryName: "path", overwriteFiles: false, async));

        [Theory]
        [MemberData(nameof(Get_Boolean_Data))]
        public async Task InvalidPath_Throws(bool async)
        {
            using MemoryStream archive = new MemoryStream();
            await Assert.ThrowsAsync<ArgumentNullException>(() => ExtractToDirectory(archive, destinationDirectoryName: null, overwriteFiles: false, async));
            await Assert.ThrowsAsync<ArgumentException>(() => ExtractToDirectory(archive, destinationDirectoryName: string.Empty, overwriteFiles: false, async));
        }

        [Theory]
        [MemberData(nameof(Get_Boolean_Data))]
        public async Task UnreadableStream_Throws(bool async)
        {
            using MemoryStream archive = new MemoryStream();
            using WrappedStream unreadable = new WrappedStream(archive, canRead: false, canWrite: true, canSeek: true);
            await Assert.ThrowsAsync<ArgumentException>(() => ExtractToDirectory(unreadable, destinationDirectoryName: "path", overwriteFiles: false, async));
        }

        [Theory]
        [MemberData(nameof(Get_Boolean_Data))]
        public async Task NonExistentDirectory_Throws(bool async)
        {
            using TempDirectory root = new TempDirectory();
            string dirPath = Path.Join(root.Path, "dir");

            using MemoryStream archive = new MemoryStream();
            await Assert.ThrowsAsync<DirectoryNotFoundException>(() => ExtractToDirectory(archive, destinationDirectoryName: dirPath, overwriteFiles: false, async));
        }

        [Theory]
        [MemberData(nameof(Get_Boolean_Data))]
        public async Task ExtractEntry_ManySubfolderSegments_NoPrecedingDirectoryEntries(bool async)
        {
            using TempDirectory root = new TempDirectory();

            string firstSegment = "a";
            string secondSegment = Path.Join(firstSegment, "b");
            string fileWithTwoSegments = Path.Join(secondSegment, "c.txt");

            using MemoryStream archive = new MemoryStream();
            TarWriter writer = await CreateTarWriter(archive, TarEntryFormat.Ustar, leaveOpen: true, async: async);
            try
            {
                UstarTarEntry entry = new UstarTarEntry(TarEntryType.RegularFile, fileWithTwoSegments)
                {
                    DataStream = new MemoryStream(new byte[] { 0x1 }, writable: false)
                };
                await WriteEntry(writer, entry, async);
            }
            finally
            {
                await DisposeTarWriter(writer, async);
            }

            archive.Seek(0, SeekOrigin.Begin);
            await ExtractToDirectory(archive, root.Path, overwriteFiles: false, async);

            Assert.True(Directory.Exists(Path.Join(root.Path, firstSegment)));
            Assert.True(Directory.Exists(Path.Join(root.Path, secondSegment)));
            Assert.True(File.Exists(Path.Join(root.Path, fileWithTwoSegments)));
        }

        [Fact]
        public async Task ExtractEntry_DockerImageTarWithFileTypeInDirectoriesInMode_SuccessfullyExtracts_Async()
        {
            using TempDirectory root = new TempDirectory();
            using MemoryStream archiveStream = GetTarMemoryStream(CompressionMethod.Uncompressed, "misc", "docker-hello-world");
            await TarFile.ExtractToDirectoryAsync(archiveStream, root.Path, overwriteFiles: true);

            Assert.True(File.Exists(Path.Join(root.Path, "manifest.json")));
            Assert.True(File.Exists(Path.Join(root.Path, "repositories")));
        }

        [ConditionalFact(typeof(MountHelper), nameof(MountHelper.CanCreateSymbolicLinks))]
        public async Task ExtractEntry_PodmanImageTarWithRelativeSymlinksPointingInExtractDirectory_SuccessfullyExtracts_Async()
        {
            using TempDirectory root = new TempDirectory();
            using MemoryStream archiveStream = GetTarMemoryStream(CompressionMethod.Uncompressed, "misc", "podman-hello-world");
            await TarFile.ExtractToDirectoryAsync(archiveStream, root.Path, overwriteFiles: true);

            Assert.True(File.Exists(Path.Join(root.Path, "manifest.json")));
            Assert.True(File.Exists(Path.Join(root.Path, "repositories")));
            Assert.True(File.Exists(Path.Join(root.Path, "efb53921da3394806160641b72a2cbd34ca1a9a8345ac670a85a04ad3d0e3507.tar")));

            string symlinkPath = Path.Join(root.Path, "e7fc2b397c1ab5af9938f18cc9a80d526cccd1910e4678390157d8cc6c94410d/layer.tar");
            Assert.True(File.Exists(symlinkPath));

            FileInfo? fileInfo = new(symlinkPath);
            Assert.Equal("../efb53921da3394806160641b72a2cbd34ca1a9a8345ac670a85a04ad3d0e3507.tar", fileInfo.LinkTarget);

            FileSystemInfo? symlinkTarget = File.ResolveLinkTarget(symlinkPath, returnFinalTarget: true);
            Assert.True(File.Exists(symlinkTarget.FullName));
        }

        [Theory]
        [InlineData(TarEntryType.SymbolicLink)]
        [InlineData(TarEntryType.HardLink)]
        public async Task Extract_LinkEntry_TargetOutsideDirectory(TarEntryType entryType)
        {
            foreach (bool async in Booleans)
            {
                using MemoryStream archive = new MemoryStream();
                using (TarWriter writer = new TarWriter(archive, TarEntryFormat.Ustar, leaveOpen: true))
                {
                    UstarTarEntry entry = new UstarTarEntry(entryType, "link");
                    entry.LinkName = PlatformDetection.IsWindows ? @"C:\Windows\System32\notepad.exe" : "/usr/bin/nano";
                    writer.WriteEntry(entry);
                }

                archive.Seek(0, SeekOrigin.Begin);

                using TempDirectory root = new TempDirectory();

                await Assert.ThrowsAnyAsync<IOException>(() => ExtractToDirectory(archive, root.Path, overwriteFiles: false, async));

                Assert.Equal(0, Directory.GetFileSystemEntries(root.Path).Count());
            }
        }

        [ConditionalTheory(typeof(MountHelper), nameof(MountHelper.CanCreateSymbolicLinks))]
        [InlineData(TarEntryFormat.Pax)]
        [InlineData(TarEntryFormat.Gnu)]
        public async Task Extract_SymbolicLinkEntry_TargetInsideDirectory(TarEntryFormat format)
        {
            foreach (bool async in Booleans)
            {
                await Extract_LinkEntry_TargetInsideDirectory_Internal(async, TarEntryType.SymbolicLink, format, null);
            }
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.SupportsHardLinkCreation))]
        [InlineData(TarEntryFormat.Pax)]
        [InlineData(TarEntryFormat.Gnu)]
        public async Task Extract_HardLinkEntry_TargetInsideDirectory(TarEntryFormat format)
        {
            foreach (bool async in Booleans)
            {
                await Extract_LinkEntry_TargetInsideDirectory_Internal(async, TarEntryType.HardLink, format, null);
            }
        }

        [ConditionalTheory(typeof(MountHelper), nameof(MountHelper.CanCreateSymbolicLinks))]
        [InlineData(TarEntryFormat.Pax)]
        [InlineData(TarEntryFormat.Gnu)]
        public async Task Extract_SymbolicLinkEntry_TargetInsideDirectory_LongBaseDir(TarEntryFormat format)
        {
            foreach (bool async in Booleans)
            {
                await Extract_LinkEntry_TargetInsideDirectory_Internal(async, TarEntryType.SymbolicLink, format, new string('a', 99));
            }
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.SupportsHardLinkCreation))]
        [InlineData(TarEntryFormat.Pax)]
        [InlineData(TarEntryFormat.Gnu)]
        public async Task Extract_HardLinkEntry_TargetInsideDirectory_LongBaseDir(TarEntryFormat format)
        {
            foreach (bool async in Booleans)
            {
                await Extract_LinkEntry_TargetInsideDirectory_Internal(async, TarEntryType.HardLink, format, new string('a', 99));
            }
        }

        private async Task Extract_LinkEntry_TargetInsideDirectory_Internal(bool async, TarEntryType entryType, TarEntryFormat format, string? subfolder)
        {
            using TempDirectory root = new TempDirectory();

            string baseDir = root.Path;
            Directory.CreateDirectory(baseDir);

            string linkName = "link";
            string targetName = "target";
            string targetPath = string.IsNullOrEmpty(subfolder) ? targetName : Path.Join(subfolder, targetName);

            using MemoryStream archive = new MemoryStream();
            using (TarWriter writer = new TarWriter(archive, format, leaveOpen: true))
            {
                TarEntry fileEntry = InvokeTarEntryCreationConstructor(format, TarEntryType.RegularFile, targetPath);
                writer.WriteEntry(fileEntry);

                TarEntry entry = InvokeTarEntryCreationConstructor(format, entryType, linkName);
                entry.LinkName = targetPath;
                writer.WriteEntry(entry);
            }

            archive.Seek(0, SeekOrigin.Begin);

            await ExtractToDirectory(archive, baseDir, overwriteFiles: false, async);

            Assert.Equal(2, Directory.GetFileSystemEntries(baseDir).Count());
        }

        [Theory]
        [InlineData(512)]
        [InlineData(512 + 1)]
        [InlineData(512 + 512 - 1)]
        public async Task Extract_UnseekableStream_BlockAlignmentPadding_DoesNotAffectNextEntries(int contentSize)
        {
            foreach (bool async in Booleans)
            {
                byte[] fileContents = new byte[contentSize];
                Array.Fill<byte>(fileContents, 0x1);

                using MemoryStream archive = new MemoryStream();
                using (GZipStream compressor = new GZipStream(archive, CompressionMode.Compress, leaveOpen: true))
                {
                    TarWriter writer = await CreateTarWriter(compressor, async: async);
                    try
                    {
                        var entry1 = new PaxTarEntry(TarEntryType.RegularFile, "file")
                        {
                            DataStream = new MemoryStream(fileContents)
                        };
                        await WriteEntry(writer, entry1, async);

                        var entry2 = new PaxTarEntry(TarEntryType.RegularFile, "next-file");
                        await WriteEntry(writer, entry2, async);
                    }
                    finally
                    {
                        await DisposeTarWriter(writer, async);
                    }
                }

                archive.Position = 0;
                using GZipStream decompressor = new GZipStream(archive, CompressionMode.Decompress);
                using TempDirectory destination = new TempDirectory();
                await ExtractToDirectory(decompressor, destination.Path, overwriteFiles: true, async);

                Assert.Equal(2, Directory.GetFileSystemEntries(destination.Path, "*", SearchOption.AllDirectories).Count());
            }
        }

        [Theory]
        [MemberData(nameof(Get_Boolean_Data))]
        public async Task PaxNameCollision_DedupInExtendedAttributes(bool async)
        {
            using TempDirectory root = new TempDirectory();

            string sharedRootFolders = Path.Join(root.Path, "folder with spaces", new string('a', 100));
            string path1 = Path.Join(sharedRootFolders, "entry 1 with spaces.txt");
            string path2 = Path.Join(sharedRootFolders, "entry 2 with spaces.txt");

            using MemoryStream stream = new MemoryStream();
            TarWriter writer = await CreateTarWriter(stream, TarEntryFormat.Pax, leaveOpen: true, async: async);
            try
            {
                PaxTarEntry entry1 = new PaxTarEntry(TarEntryType.RegularFile, path1);
                await WriteEntry(writer, entry1, async);
                PaxTarEntry entry2 = new PaxTarEntry(TarEntryType.RegularFile, path2);
                await WriteEntry(writer, entry2, async);
            }
            finally
            {
                await DisposeTarWriter(writer, async);
            }

            stream.Position = 0;
            await ExtractToDirectory(stream, root.Path, overwriteFiles: true, async);

            Assert.True(File.Exists(path1));
            Assert.True(Path.Exists(path2));
        }

        [Theory]
        [MemberData(nameof(GetTestTarFormats))]
        public async Task UnseekableStreams_RoundTrip(TestTarFormat testFormat)
        {
            foreach (bool async in Booleans)
            {
                using TempDirectory root = new TempDirectory();

                using MemoryStream sourceStream = GetTarMemoryStream(CompressionMethod.Uncompressed, testFormat, "many_small_files");
                using WrappedStream sourceUnseekableArchiveStream = new WrappedStream(sourceStream, canRead: true, canWrite: false, canSeek: false);

                await ExtractToDirectory(sourceUnseekableArchiveStream, root.Path, overwriteFiles: false, async);

                using MemoryStream destinationStream = new MemoryStream();
                using WrappedStream destinationUnseekableArchiveStream = new WrappedStream(destinationStream, canRead: true, canWrite: true, canSeek: false);
                await CreateFromDirectory(root.Path, destinationUnseekableArchiveStream, includeBaseDirectory: false, async);

                FileSystemEnumerable<FileSystemInfo> fileSystemEntries = new FileSystemEnumerable<FileSystemInfo>(
                    directory: root.Path,
                    transform: (ref FileSystemEntry entry) => entry.ToFileSystemInfo(),
                    options: new EnumerationOptions() { RecurseSubdirectories = true });

                destinationStream.Position = 0;
                using TarReader reader = new TarReader(destinationStream, leaveOpen: false);

                int bufferLength = 1024;
                byte[] fileContent = new byte[bufferLength];
                byte[] dataStreamContent = new byte[bufferLength];
                TarEntry entry = reader.GetNextEntry();
                do
                {
                    Assert.NotNull(entry);
                    string entryPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(Path.Join(root.Path, entry.Name)));
                    FileSystemInfo fsi = fileSystemEntries.SingleOrDefault(file =>
                        file.FullName == entryPath);
                    Assert.NotNull(fsi);
                    if (entry.EntryType is TarEntryType.RegularFile or TarEntryType.V7RegularFile)
                    {
                        Assert.NotNull(entry.DataStream);

                        using Stream fileData = File.OpenRead(fsi.FullName);

                        AssertExtensions.LessThanOrEqualTo(entry.Length, bufferLength);
                        AssertExtensions.LessThanOrEqualTo(fileData.Length, bufferLength);

                        Assert.Equal(fileData.Length, entry.Length);

                        Array.Clear(fileContent);
                        Array.Clear(dataStreamContent);

                        if (async)
                        {
                            await fileData.ReadExactlyAsync(fileContent.AsMemory(0, (int)entry.Length));
                            await entry.DataStream.ReadExactlyAsync(dataStreamContent.AsMemory(0, (int)entry.Length));
                        }
                        else
                        {
                            fileData.ReadExactly(fileContent, 0, (int)entry.Length);
                            entry.DataStream.ReadExactly(dataStreamContent, 0, (int)entry.Length);
                        }

                        AssertExtensions.SequenceEqual(fileContent, dataStreamContent);
                    }
                }
                while ((entry = reader.GetNextEntry()) != null);
            }
        }

        [Theory]
        [MemberData(nameof(GetExactRootDirMatchCases))]
        [SkipOnPlatform(TestPlatforms.iOS | TestPlatforms.tvOS, "The temporary directory on Apple mobile platforms exceeds the path length limit.")]
        public async Task ExtractToDirectory_ExactRootDirMatch_RegularFile_And_Directory_Throws(TarEntryFormat format, TarEntryType entryType, string fileName)
        {
            foreach (bool async in Booleans)
            {
                await ExtractToDirectory_ExactRootDirMatch_RegularFile_And_Directory_Throws_Internal(async, format, entryType, fileName, inverted: false);
                await ExtractToDirectory_ExactRootDirMatch_RegularFile_And_Directory_Throws_Internal(async, format, entryType, fileName, inverted: true);
            }
        }

        [Theory]
        [MemberData(nameof(Get_Boolean_Data))]
        [SkipOnPlatform(TestPlatforms.iOS | TestPlatforms.tvOS, "The temporary directory on Apple mobile platforms exceeds the path length limit.")]
        public async Task ExtractToDirectory_ExactRootDirMatch_Directory_Relative_Throws(bool async)
        {
            string entryFolderName = "folder";
            string destinationFolderName = "folderSibling";

            using TempDirectory root = new TempDirectory();

            string entryFolderPath = Path.Join(root.Path, entryFolderName);
            string destinationFolderPath = Path.Join(root.Path, destinationFolderName);

            Directory.CreateDirectory(entryFolderPath);
            Directory.CreateDirectory(destinationFolderPath);

            string dirPath1 = Path.Join(entryFolderPath, "..", "folder");
            string dirPath2 = Path.Join(entryFolderPath, "..", "folder" + Path.DirectorySeparatorChar);

            await ExtractRootDirMatch_Verify_Throws(async, TarEntryFormat.Ustar, TarEntryType.Directory, destinationFolderPath, dirPath1, linkTargetPath: null);
            await ExtractRootDirMatch_Verify_Throws(async, TarEntryFormat.Ustar, TarEntryType.Directory, destinationFolderPath, dirPath2, linkTargetPath: null);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.SupportsHardLinkCreation))]
        [InlineData(TarEntryFormat.V7)]
        [InlineData(TarEntryFormat.Ustar)]
        [InlineData(TarEntryFormat.Pax)]
        [InlineData(TarEntryFormat.Gnu)]
        [SkipOnPlatform(TestPlatforms.iOS | TestPlatforms.tvOS, "The temporary directory on Apple mobile platforms exceeds the path length limit.")]
        public async Task ExtractToDirectory_ExactRootDirMatch_HardLinks_Throws(TarEntryFormat format)
        {
            foreach (bool async in Booleans)
            {
                await ExtractToDirectory_ExactRootDirMatch_Links_Throws(async, format, TarEntryType.HardLink, inverted: false);
                await ExtractToDirectory_ExactRootDirMatch_Links_Throws(async, format, TarEntryType.HardLink, inverted: true);
            }
        }

        [ConditionalTheory(typeof(MountHelper), nameof(MountHelper.CanCreateSymbolicLinks))]
        [InlineData(TarEntryFormat.V7)]
        [InlineData(TarEntryFormat.Ustar)]
        [InlineData(TarEntryFormat.Pax)]
        [InlineData(TarEntryFormat.Gnu)]
        public async Task ExtractToDirectory_ExactRootDirMatch_SymLinks_Throws(TarEntryFormat format)
        {
            foreach (bool async in Booleans)
            {
                await ExtractToDirectory_ExactRootDirMatch_Links_Throws(async, format, TarEntryType.SymbolicLink, inverted: false);
                await ExtractToDirectory_ExactRootDirMatch_Links_Throws(async, format, TarEntryType.SymbolicLink, inverted: true);
            }
        }

        [ConditionalTheory(typeof(MountHelper), nameof(MountHelper.CanCreateSymbolicLinks))]
        [MemberData(nameof(Get_Boolean_Data))]
        public async Task ExtractToDirectory_ExactRootDirMatch_SymLinks_TargetOutside_Throws(bool async)
        {
            string entryFolderName = "folder";
            string destinationFolderName = "folderSibling";

            using TempDirectory root = new TempDirectory();

            string entryFolderPath = Path.Join(root.Path, entryFolderName);
            string destinationFolderPath = Path.Join(root.Path, destinationFolderName);

            Directory.CreateDirectory(entryFolderPath);
            Directory.CreateDirectory(destinationFolderPath);

            string linkPath = Path.Join(entryFolderPath, "link");

            string linkTargetPath1 = Path.Join(entryFolderPath, "..", entryFolderName);
            string linkTargetPath2 = Path.Join(entryFolderPath, "..", entryFolderName + Path.DirectorySeparatorChar);

            await ExtractRootDirMatch_Verify_Throws(async, TarEntryFormat.Ustar, TarEntryType.Directory, destinationFolderPath, linkPath, linkTargetPath1);
            await ExtractRootDirMatch_Verify_Throws(async, TarEntryFormat.Ustar, TarEntryType.Directory, destinationFolderPath, linkPath, linkTargetPath2);
        }

        private Task ExtractToDirectory_ExactRootDirMatch_RegularFile_And_Directory_Throws_Internal(bool async, TarEntryFormat format, TarEntryType entryType, string fileName, bool inverted)
        {
            string entryFolderName = inverted ? "folderSibling" : "folder";
            string destinationFolderName = inverted ? "folder" : "folderSibling";

            using TempDirectory root = new TempDirectory();

            string entryFolderPath = Path.Join(root.Path, entryFolderName);
            string destinationFolderPath = Path.Join(root.Path, destinationFolderName);

            Directory.CreateDirectory(entryFolderPath);
            Directory.CreateDirectory(destinationFolderPath);

            string filePath = Path.Join(entryFolderPath, fileName);

            return ExtractRootDirMatch_Verify_Throws(async, format, entryType, destinationFolderPath, filePath, linkTargetPath: null);
        }

        private Task ExtractToDirectory_ExactRootDirMatch_Links_Throws(bool async, TarEntryFormat format, TarEntryType entryType, bool inverted)
        {
            string entryFolderName = inverted ? "folderSibling" : "folder";
            string destinationFolderName = inverted ? "folder" : "folderSibling";

            string linkTargetFileName = "file.txt";
            string linkFileName = "link";

            using TempDirectory root = new TempDirectory();

            string entryFolderPath = Path.Join(root.Path, entryFolderName);
            string destinationFolderPath = Path.Join(root.Path, destinationFolderName);

            string linkPath = Path.Join(entryFolderPath, linkFileName);
            string linkTargetPath = Path.Join(destinationFolderPath, linkTargetFileName);

            Directory.CreateDirectory(entryFolderPath);
            Directory.CreateDirectory(destinationFolderPath);
            File.Create(linkTargetPath).Dispose();

            return ExtractRootDirMatch_Verify_Throws(async, format, entryType, destinationFolderPath, linkPath, linkTargetPath);
        }

        private async Task ExtractRootDirMatch_Verify_Throws(bool async, TarEntryFormat format, TarEntryType entryType, string destinationFolderPath, string entryFilePath, string? linkTargetPath)
        {
            using MemoryStream archive = new MemoryStream();
            TarWriter writer = await CreateTarWriter(archive, format, leaveOpen: true, async: async);
            try
            {
                TarEntry entry = InvokeTarEntryCreationConstructor(format, entryType, entryFilePath);
                MemoryStream? dataStream = null;
                if (entryType is TarEntryType.RegularFile or TarEntryType.V7RegularFile)
                {
                    dataStream = new MemoryStream();
                    dataStream.Write(new byte[] { 0x1 });
                    dataStream.Position = 0;
                    entry.DataStream = dataStream;
                }
                if (entryType is TarEntryType.SymbolicLink or TarEntryType.HardLink)
                {
                    entry.LinkName = linkTargetPath;
                }

                await WriteEntry(writer, entry, async);
                dataStream?.Dispose();
            }
            finally
            {
                await DisposeTarWriter(writer, async);
            }

            archive.Position = 0;

            await Assert.ThrowsAsync<IOException>(() => ExtractToDirectory(archive, destinationFolderPath, overwriteFiles: false, async));
            Assert.False(File.Exists(entryFilePath), $"File should not exist: {entryFilePath}");
        }

        public static IEnumerable<object[]> PaxExtraction_PathOverrideData()
        {
            yield return new object[] { "data/report.txt", "config/settings.txt", "config/settings.txt" };
            yield return new object[] { "../../escape.txt", "safe.txt", "safe.txt" };
        }

        [Theory]
        [MemberData(nameof(PaxExtraction_PathOverrideData))]
        public void PaxExtraction_EntryNameMatchesExtractedPath(string headerName, string eaName, string expectedApiName)
        {
            byte[] content = "test data"u8.ToArray();
            byte[] archive = BuildRawPaxArchiveWithEAPathOverride(headerName, eaName, content);

            using TempDirectory root = new TempDirectory();

            using (var scanStream = new MemoryStream(archive))
            using (var reader = new TarReader(scanStream))
            {
                TarEntry entry = reader.GetNextEntry();
                Assert.NotNull(entry);
                Assert.Equal(expectedApiName, entry.Name);
            }

            using var extractStream = new MemoryStream(archive);
            TarFile.ExtractToDirectory(extractStream, root.Path, overwriteFiles: false);

            string[] files = Directory.GetFiles(root.Path, "*", SearchOption.AllDirectories);
            Assert.Single(files);
            string extractedRelPath = Path.GetRelativePath(root.Path, files[0]).Replace('\\', '/');
            Assert.Equal(expectedApiName, extractedRelPath);
        }

        [Fact]
        public void PaxExtraction_PathTraversalInEA_IsBlocked()
        {
            byte[] content = "test data"u8.ToArray();
            byte[] archive = BuildRawPaxArchiveWithEAPathOverride("safe.txt", "../../escape.txt", content);

            using (var scanStream = new MemoryStream(archive))
            using (var reader = new TarReader(scanStream))
            {
                TarEntry entry = reader.GetNextEntry();
                Assert.NotNull(entry);
                Assert.Contains("..", entry.Name);
            }

            using TempDirectory root = new TempDirectory();
            using var extractStream = new MemoryStream(archive);
            Assert.Throws<IOException>(() =>
                TarFile.ExtractToDirectory(extractStream, root.Path, overwriteFiles: false));
        }

        [Theory]
        [InlineData(10, 50)]
        [InlineData(100, 25)]
        public void PaxExtraction_EntryLengthMatchesExtractedFileSize(int dataSize, long eaSize)
        {
            byte[] actualData = new byte[dataSize];
            Array.Fill<byte>(actualData, (byte)'X');

            byte[] archive = BuildRawPaxArchiveWithSizeOverride("file.bin", "file.bin", actualData, dataSize, eaSize);

            long apiLength;
            using (var scanStream = new MemoryStream(archive))
            using (var reader = new TarReader(scanStream))
            {
                TarEntry entry = reader.GetNextEntry(copyData: true);
                Assert.NotNull(entry);
                apiLength = entry.Length;
            }

            using TempDirectory root = new TempDirectory();
            using var extractStream = new MemoryStream(archive);
            TarFile.ExtractToDirectory(extractStream, root.Path, overwriteFiles: false);

            long extractedSize = new FileInfo(Path.Combine(root.Path, "file.bin")).Length;
            Assert.Equal(apiLength, extractedSize);
        }

        [Fact]
        public void PaxExtraction_SizeAmplification_EntryLengthMatchesExtraction()
        {
            int entryCount = 10;
            byte[] tinyData = "x"u8.ToArray();
            long headerSize = 1;
            long eaSize = 500;

            using var ms = new MemoryStream();
            for (int i = 0; i < entryCount; i++)
            {
                string name = $"file_{i:D3}.bin";
                var extraEAs = new Dictionary<string, string> { ["size"] = eaSize.ToString() };
                byte[] eaData = BuildRawPaxExtendedAttributeData(name, extraEAs);

                WriteRawTarHeader(ms, $"PaxHeaders.0/{name}", 0, 0, 0, eaData.Length, 0, 'x', "");
                ms.Write(eaData);
                PadToTarBlockBoundary(ms);

                WriteRawTarHeader(ms, name, Convert.ToInt32("644", 8), 0, 0, headerSize, 1700000000, '0', "");
                ms.Write(tinyData);
                PadToTarBlockBoundary(ms);
            }
            ms.Write(new byte[1024]);
            byte[] archive = ms.ToArray();

            long apiTotalSize = 0;
            using (var scanStream = new MemoryStream(archive))
            using (var reader = new TarReader(scanStream))
            {
                TarEntry e;
                while ((e = reader.GetNextEntry(copyData: true)) is not null)
                {
                    apiTotalSize += e.Length;
                }
            }

            using TempDirectory root = new TempDirectory();
            using var extractStream = new MemoryStream(archive);
            TarFile.ExtractToDirectory(extractStream, root.Path, overwriteFiles: false);

            long totalExtracted = Directory.GetFiles(root.Path).Sum(f => new FileInfo(f).Length);
            Assert.Equal(apiTotalSize, totalExtracted);
        }
    }
}
