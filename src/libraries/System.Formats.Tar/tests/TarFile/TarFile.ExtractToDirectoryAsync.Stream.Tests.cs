// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.IO.Compression;
using System.IO.Enumeration;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Formats.Tar.Tests
{
    public class TarFile_ExtractToDirectoryAsync_Stream_Tests : TarFile_ExtractToDirectory_Tests
    {
        [Fact]
        public async Task ExtractToDirectoryAsync_Cancel()
        {
            CancellationTokenSource cs = new CancellationTokenSource();
            cs.Cancel();
            using (MemoryStream archiveStream = new MemoryStream())
            {
                await Assert.ThrowsAsync<TaskCanceledException>(() => TarFile.ExtractToDirectoryAsync(archiveStream, "directory", overwriteFiles: true, cs.Token));
            }
        }

        [Fact]
        public Task NullStream_Throws_Async() =>
            Assert.ThrowsAsync<ArgumentNullException>(() => TarFile.ExtractToDirectoryAsync(source: null, destinationDirectoryName: "path", overwriteFiles: false));

        [Fact]
        public async Task InvalidPath_Throws_Async()
        {
            using (MemoryStream archive = new MemoryStream())
            {
                await Assert.ThrowsAsync<ArgumentNullException>(() => TarFile.ExtractToDirectoryAsync(archive, destinationDirectoryName: null, overwriteFiles: false));
                await Assert.ThrowsAsync<ArgumentException>(() => TarFile.ExtractToDirectoryAsync(archive, destinationDirectoryName: string.Empty, overwriteFiles: false));
            }
        }

        [Fact]
        public async Task UnreadableStream_Throws_Async()
        {
            using (MemoryStream archive = new MemoryStream())
            {
                using (WrappedStream unreadable = new WrappedStream(archive, canRead: false, canWrite: true, canSeek: true))
                {
                    await Assert.ThrowsAsync<ArgumentException>(() => TarFile.ExtractToDirectoryAsync(unreadable, destinationDirectoryName: "path", overwriteFiles: false));
                }
            }
        }

        [Fact]
        public async Task NonExistentDirectory_Throws_Async()
        {
            using (TempDirectory root = new TempDirectory())
            {
                string dirPath = Path.Join(root.Path, "dir");

                using (MemoryStream archive = new MemoryStream())
                {
                    await Assert.ThrowsAsync<DirectoryNotFoundException>(() => TarFile.ExtractToDirectoryAsync(archive, destinationDirectoryName: dirPath, overwriteFiles: false));
                }
            }
        }

        [Fact]
        public async Task ExtractEntry_ManySubfolderSegments_NoPrecedingDirectoryEntries_Async()
        {
            using (TempDirectory root = new TempDirectory())
            {
                string firstSegment = "a";
                string secondSegment = Path.Join(firstSegment, "b");
                string fileWithTwoSegments = Path.Join(secondSegment, "c.txt");

                await using (MemoryStream archive = new MemoryStream())
                {
                    await using (TarWriter writer = new TarWriter(archive, TarEntryFormat.Ustar, leaveOpen: true))
                    {
                        // No preceding directory entries for the segments
                        UstarTarEntry entry = new UstarTarEntry(TarEntryType.RegularFile, fileWithTwoSegments);

                        entry.DataStream = new MemoryStream();
                        entry.DataStream.Write(new byte[] { 0x1 });
                        entry.DataStream.Seek(0, SeekOrigin.Begin);

                        await writer.WriteEntryAsync(entry);
                    }

                    archive.Seek(0, SeekOrigin.Begin);
                    await TarFile.ExtractToDirectoryAsync(archive, root.Path, overwriteFiles: false);

                    Assert.True(Directory.Exists(Path.Join(root.Path, firstSegment)));
                    Assert.True(Directory.Exists(Path.Join(root.Path, secondSegment)));
                    Assert.True(File.Exists(Path.Join(root.Path, fileWithTwoSegments)));
                }
            }
        }

        [Fact]
        public async Task ExtractEntry_DockerImageTarWithFileTypeInDirectoriesInMode_SuccessfullyExtracts_Async()
        {
            using (TempDirectory root = new TempDirectory())
            {
                await using MemoryStream archiveStream = GetTarMemoryStream(CompressionMethod.Uncompressed, "misc", "docker-hello-world");
                await TarFile.ExtractToDirectoryAsync(archiveStream, root.Path, overwriteFiles: true);

                Assert.True(File.Exists(Path.Join(root.Path, "manifest.json")));
                Assert.True(File.Exists(Path.Join(root.Path, "repositories")));
            }
        }

        [ConditionalFact(typeof(MountHelper), nameof(MountHelper.CanCreateSymbolicLinks))]
        public async Task ExtractEntry_PodmanImageTarWithRelativeSymlinksPointingInExtractDirectory_SuccessfullyExtracts_Async()
        {
            using (TempDirectory root = new TempDirectory())
            {
                await using MemoryStream archiveStream = GetTarMemoryStream(CompressionMethod.Uncompressed, "misc", "podman-hello-world");
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
        }

        [Theory]
        [InlineData(TarEntryType.SymbolicLink)]
        [InlineData(TarEntryType.HardLink)]
        public async Task Extract_LinkEntry_TargetOutsideDirectory_Async(TarEntryType entryType)
        {
            await using (MemoryStream archive = new MemoryStream())
            {
                await using (TarWriter writer = new TarWriter(archive, TarEntryFormat.Ustar, leaveOpen: true))
                {
                    UstarTarEntry entry = new UstarTarEntry(entryType, "link");
                    entry.LinkName = PlatformDetection.IsWindows ? @"C:\Windows\System32\notepad.exe" : "/usr/bin/nano";
                    await writer.WriteEntryAsync(entry);
                }

                archive.Seek(0, SeekOrigin.Begin);

                using (TempDirectory root = new TempDirectory())
                {
                    await Assert.ThrowsAnyAsync<IOException>(() => TarFile.ExtractToDirectoryAsync(archive, root.Path, overwriteFiles: false));
                    Assert.Equal(0, Directory.GetFileSystemEntries(root.Path).Count());
                }
            }
        }

        [ConditionalTheory(typeof(MountHelper), nameof(MountHelper.CanCreateSymbolicLinks))]
        [InlineData(TarEntryFormat.Pax)]
        [InlineData(TarEntryFormat.Gnu)]
        public Task Extract_SymbolicLinkEntry_TargetInsideDirectory_Async(TarEntryFormat format) => Extract_LinkEntry_TargetInsideDirectory_Internal_Async(TarEntryType.SymbolicLink, format, null);

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.SupportsHardLinkCreation))]
        [InlineData(TarEntryFormat.Pax)]
        [InlineData(TarEntryFormat.Gnu)]
        public Task Extract_HardLinkEntry_TargetInsideDirectory_Async(TarEntryFormat format) => Extract_LinkEntry_TargetInsideDirectory_Internal_Async(TarEntryType.HardLink, format, null);

        [ConditionalTheory(typeof(MountHelper), nameof(MountHelper.CanCreateSymbolicLinks))]
        [InlineData(TarEntryFormat.Pax)]
        [InlineData(TarEntryFormat.Gnu)]
        public Task Extract_SymbolicLinkEntry_TargetInsideDirectory_LongBaseDir_Async(TarEntryFormat format) => Extract_LinkEntry_TargetInsideDirectory_Internal_Async(TarEntryType.SymbolicLink, format, new string('a', 99));

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.SupportsHardLinkCreation))]
        [InlineData(TarEntryFormat.Pax)]
        [InlineData(TarEntryFormat.Gnu)]
        public Task Extract_HardLinkEntry_TargetInsideDirectory_LongBaseDir_Async(TarEntryFormat format) => Extract_LinkEntry_TargetInsideDirectory_Internal_Async(TarEntryType.HardLink, format, new string('a', 99));

        // This test would not pass for the V7 and Ustar formats in some OSs like MacCatalyst, tvOSSimulator and OSX, because the TempDirectory gets created in
        // a folder with a path longer than 100 bytes, and those tar formats have no way of handling pathnames and linknames longer than that length.
        // The rest of the OSs create the TempDirectory in a path that does not surpass the 100 bytes, so the 'subfolder' parameter gives a chance to extend
        // the base directory past that length, to ensure this scenario is tested everywhere.
        private async Task Extract_LinkEntry_TargetInsideDirectory_Internal_Async(TarEntryType entryType, TarEntryFormat format, string subfolder)
        {
            using (TempDirectory root = new TempDirectory())
            {
                string baseDir = root.Path;
                Directory.CreateDirectory(baseDir);

                string linkName = "link";
                string targetName = "target";
                string targetPath = string.IsNullOrEmpty(subfolder) ? targetName : Path.Join(subfolder, targetName);

                await using (MemoryStream archive = new MemoryStream())
                {
                    await using (TarWriter writer = new TarWriter(archive, format, leaveOpen: true))
                    {
                        TarEntry fileEntry = InvokeTarEntryCreationConstructor(format, TarEntryType.RegularFile, targetPath);
                        await writer.WriteEntryAsync(fileEntry);

                        TarEntry entry = InvokeTarEntryCreationConstructor(format, entryType, linkName);
                        entry.LinkName = targetPath;
                        await writer.WriteEntryAsync(entry);
                    }

                    archive.Seek(0, SeekOrigin.Begin);

                    await TarFile.ExtractToDirectoryAsync(archive, baseDir, overwriteFiles: false);

                    Assert.Equal(2, Directory.GetFileSystemEntries(baseDir).Count());
                }
            }
        }

        [Theory]
        [InlineData(512)]
        [InlineData(512 + 1)]
        [InlineData(512 + 512 - 1)]
        public async Task Extract_UnseekableStream_BlockAlignmentPadding_DoesNotAffectNextEntries_Async(int contentSize)
        {
            byte[] fileContents = new byte[contentSize];
            Array.Fill<byte>(fileContents, 0x1);

            using var archive = new MemoryStream();
            using (var compressor = new GZipStream(archive, CompressionMode.Compress, leaveOpen: true))
            {
                using var writer = new TarWriter(compressor);
                var entry1 = new PaxTarEntry(TarEntryType.RegularFile, "file");
                entry1.DataStream = new MemoryStream(fileContents);
                await writer.WriteEntryAsync(entry1);

                var entry2 = new PaxTarEntry(TarEntryType.RegularFile, "next-file");
                await writer.WriteEntryAsync(entry2);
            }

            archive.Position = 0;
            using var decompressor = new GZipStream(archive, CompressionMode.Decompress);
            using var reader = new TarReader(decompressor);

            using TempDirectory destination = new TempDirectory();
            await TarFile.ExtractToDirectoryAsync(decompressor, destination.Path, overwriteFiles: true);

            Assert.Equal(2, Directory.GetFileSystemEntries(destination.Path, "*", SearchOption.AllDirectories).Count());
        }

        [Fact]
        public async Task PaxNameCollision_DedupInExtendedAttributesAsync()
        {
            using TempDirectory root = new();

            string sharedRootFolders = Path.Join(root.Path, "folder with spaces", new string('a', 100));
            string path1 = Path.Join(sharedRootFolders, "entry 1 with spaces.txt");
            string path2 = Path.Join(sharedRootFolders, "entry 2 with spaces.txt");

            await using MemoryStream stream = new();
            await using (TarWriter writer = new(stream, TarEntryFormat.Pax, leaveOpen: true))
            {
                // Paths don't fit in the standard 'name' field, but they differ in the filename,
                // which is fully stored as an extended attribute
                PaxTarEntry entry1 = new(TarEntryType.RegularFile, path1);
                await writer.WriteEntryAsync(entry1);
                PaxTarEntry entry2 = new(TarEntryType.RegularFile, path2);
                await writer.WriteEntryAsync(entry2);
            }
            stream.Position = 0;

            await TarFile.ExtractToDirectoryAsync(stream, root.Path, overwriteFiles: true);

            Assert.True(File.Exists(path1));
            Assert.True(Path.Exists(path2));
        }

        [Theory]
        [MemberData(nameof(GetTestTarFormats))]
        public async Task UnseekableStreams_RoundTrip_Async(TestTarFormat testFormat)
        {
            using TempDirectory root = new();

            await using MemoryStream sourceStream = GetTarMemoryStream(CompressionMethod.Uncompressed, testFormat, "many_small_files");
            await using WrappedStream sourceUnseekableArchiveStream = new(sourceStream, canRead: true, canWrite: false, canSeek: false);

            await TarFile.ExtractToDirectoryAsync(sourceUnseekableArchiveStream, root.Path, overwriteFiles: false);

            await using MemoryStream destinationStream = new();
            await using WrappedStream destinationUnseekableArchiveStream = new(destinationStream, canRead: true, canWrite: true, canSeek: false);
            await TarFile.CreateFromDirectoryAsync(root.Path, destinationUnseekableArchiveStream, includeBaseDirectory: false);

            FileSystemEnumerable<FileSystemInfo> fileSystemEntries = new FileSystemEnumerable<FileSystemInfo>(
                directory: root.Path,
                transform: (ref FileSystemEntry entry) => entry.ToFileSystemInfo(),
                options: new EnumerationOptions() { RecurseSubdirectories = true });

            destinationStream.Position = 0;
            await using TarReader reader = new TarReader(destinationStream, leaveOpen: false);

            // Size of files in many_small_files.tar are expected to be tiny and all equal
            int bufferLength = 1024;
            byte[] fileContent = new byte[bufferLength];
            byte[] dataStreamContent = new byte[bufferLength];
            TarEntry entry = await reader.GetNextEntryAsync();
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

                    await using Stream fileData = File.OpenRead(fsi.FullName);

                    // If the size of the files in manu_small_files.tar ever gets larger than bufferLength,
                    // these asserts should fail and the test will need to be updated
                    AssertExtensions.LessThanOrEqualTo(entry.Length, bufferLength);
                    AssertExtensions.LessThanOrEqualTo(fileData.Length, bufferLength);

                    Assert.Equal(fileData.Length, entry.Length);

                    Array.Clear(fileContent);
                    Array.Clear(dataStreamContent);

                    await fileData.ReadExactlyAsync(fileContent, 0, (int)entry.Length);
                    await entry.DataStream.ReadExactlyAsync(dataStreamContent, 0, (int)entry.Length);

                    AssertExtensions.SequenceEqual(fileContent, dataStreamContent);
                }
            }
            while ((entry = await reader.GetNextEntryAsync()) != null);
        }

        [Theory]
        [MemberData(nameof(GetExactRootDirMatchCases))]
        [SkipOnPlatform(TestPlatforms.iOS | TestPlatforms.tvOS, "The temporary directory on Apple mobile platforms exceeds the path length limit.")]
        public async Task ExtractToDirectory_ExactRootDirMatch_RegularFile_And_Directory_Throws_Async(TarEntryFormat format, TarEntryType entryType, string fileName)
        {
            await ExtractToDirectory_ExactRootDirMatch_RegularFile_And_Directory_Throws_Internal_Async(format, entryType, fileName, inverted: false);
            await ExtractToDirectory_ExactRootDirMatch_RegularFile_And_Directory_Throws_Internal_Async(format, entryType, fileName, inverted: true);
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.iOS | TestPlatforms.tvOS, "The temporary directory on Apple mobile platforms exceeds the path length limit.")]
        public async Task ExtractToDirectory_ExactRootDirMatch_Directory_Relative_Throws_Async()
        {
            string entryFolderName = "folder";
            string destinationFolderName = "folderSibling";

            using TempDirectory root = new TempDirectory();

            string entryFolderPath = Path.Join(root.Path, entryFolderName);
            string destinationFolderPath = Path.Join(root.Path, destinationFolderName);

            Directory.CreateDirectory(entryFolderPath);
            Directory.CreateDirectory(destinationFolderPath);

            // Relative segments should not change the final destination folder
            string dirPath1 = Path.Join(entryFolderPath, "..", "folder");
            string dirPath2 = Path.Join(entryFolderPath, "..", "folder" + Path.DirectorySeparatorChar);

            await ExtractRootDirMatch_Verify_Throws_Async(TarEntryFormat.Ustar, TarEntryType.Directory, destinationFolderPath, dirPath1, linkTargetPath: null);
            await ExtractRootDirMatch_Verify_Throws_Async(TarEntryFormat.Ustar, TarEntryType.Directory, destinationFolderPath, dirPath2, linkTargetPath: null);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.SupportsHardLinkCreation))]
        [InlineData(TarEntryFormat.V7)]
        [InlineData(TarEntryFormat.Ustar)]
        [InlineData(TarEntryFormat.Pax)]
        [InlineData(TarEntryFormat.Gnu)]
        [SkipOnPlatform(TestPlatforms.iOS | TestPlatforms.tvOS, "The temporary directory on Apple mobile platforms exceeds the path length limit.")]
        public async Task ExtractToDirectory_ExactRootDirMatch_HardLinks_Throws_Async(TarEntryFormat format)
        {
            await ExtractToDirectory_ExactRootDirMatch_Links_Throws_Async(format, TarEntryType.HardLink, inverted: false);
            await ExtractToDirectory_ExactRootDirMatch_Links_Throws_Async(format, TarEntryType.HardLink, inverted: true);
        }

        [ConditionalTheory(typeof(MountHelper), nameof(MountHelper.CanCreateSymbolicLinks))]
        [InlineData(TarEntryFormat.V7)]
        [InlineData(TarEntryFormat.Ustar)]
        [InlineData(TarEntryFormat.Pax)]
        [InlineData(TarEntryFormat.Gnu)]
        public async Task ExtractToDirectory_ExactRootDirMatch_SymLinks_Throws_Async(TarEntryFormat format)
        {
            await ExtractToDirectory_ExactRootDirMatch_Links_Throws_Async(format, TarEntryType.SymbolicLink, inverted: false);
            await ExtractToDirectory_ExactRootDirMatch_Links_Throws_Async(format, TarEntryType.SymbolicLink, inverted: true);
        }

        [ConditionalFact(typeof(MountHelper), nameof(MountHelper.CanCreateSymbolicLinks))]
        public async Task ExtractToDirectory_ExactRootDirMatch_SymLinks_TargetOutside_Throws_Async()
        {
            string entryFolderName = "folder";
            string destinationFolderName = "folderSibling";

            using TempDirectory root = new TempDirectory();

            string entryFolderPath = Path.Join(root.Path, entryFolderName);
            string destinationFolderPath = Path.Join(root.Path, destinationFolderName);

            Directory.CreateDirectory(entryFolderPath);
            Directory.CreateDirectory(destinationFolderPath);

            string linkPath = Path.Join(entryFolderPath, "link");

            // Links target outside the destination path should not be allowed
            // Ensure relative segments do not go around this restriction
            string linkTargetPath1 = Path.Join(entryFolderPath, "..", entryFolderName);
            string linkTargetPath2 = Path.Join(entryFolderPath, "..", entryFolderName + Path.DirectorySeparatorChar);

            await ExtractRootDirMatch_Verify_Throws_Async(TarEntryFormat.Ustar, TarEntryType.Directory, destinationFolderPath, linkPath, linkTargetPath1);
            await ExtractRootDirMatch_Verify_Throws_Async(TarEntryFormat.Ustar, TarEntryType.Directory, destinationFolderPath, linkPath, linkTargetPath2);
        }

        private async Task ExtractToDirectory_ExactRootDirMatch_RegularFile_And_Directory_Throws_Internal_Async(TarEntryFormat format, TarEntryType entryType, string fileName, bool inverted)
        {
            // inverted == false:
            //   destination: folderSibling/
            //   entry folder: folder/ (does not match destination)

            // inverted == true:
            //   destination: folder/
            //   entry folder: folderSibling/ (does not match destination)

            string entryFolderName = inverted ? "folderSibling" : "folder";
            string destinationFolderName = inverted ? "folder" : "folderSibling";

            using TempDirectory root = new TempDirectory();

            string entryFolderPath = Path.Join(root.Path, entryFolderName);
            string destinationFolderPath = Path.Join(root.Path, destinationFolderName);

            Directory.CreateDirectory(entryFolderPath);
            Directory.CreateDirectory(destinationFolderPath);

            string filePath = Path.Join(entryFolderPath, fileName);

            await ExtractRootDirMatch_Verify_Throws_Async(format, entryType, destinationFolderPath, filePath, linkTargetPath: null);
        }

        private Task ExtractToDirectory_ExactRootDirMatch_Links_Throws_Async(TarEntryFormat format, TarEntryType entryType, bool inverted)
        {
            // inverted == false:
            //   destination: folderSibling/
            //   entry folder: folder/ (does not match destination)
            //   link entry file path: folder/link (does not match destination, should not be extracted)

            // inverted == true:
            //   destination: folder/
            //   entry folder: folderSibling/ (does not match destination)
            //   link entry file path: folderSibling/link (does not match destination, should not be extracted)

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

            return ExtractRootDirMatch_Verify_Throws_Async(format, entryType, destinationFolderPath, linkPath, linkTargetPath);
        }

        private async Task ExtractRootDirMatch_Verify_Throws_Async(TarEntryFormat format, TarEntryType entryType, string destinationFolderPath, string entryFilePath, string linkTargetPath)
        {
            await using MemoryStream archive = new();
            await using (TarWriter writer = new TarWriter(archive, format, leaveOpen: true))
            {
                TarEntry entry = InvokeTarEntryCreationConstructor(format, entryType, entryFilePath);
                MemoryStream dataStream = null;
                if (entryType is TarEntryType.RegularFile or TarEntryType.V7RegularFile)
                {
                    dataStream = new MemoryStream();
                    await dataStream.WriteAsync(new byte[] { 0x1 });
                    entry.DataStream = dataStream;
                }
                if (entryType is TarEntryType.SymbolicLink or TarEntryType.HardLink)
                {
                    entry.LinkName = linkTargetPath;
                }
                await writer.WriteEntryAsync(entry);
                if (dataStream != null)
                {
                    await dataStream.DisposeAsync();
                }
            }
            archive.Position = 0;

            await Assert.ThrowsAsync<IOException>(() => TarFile.ExtractToDirectoryAsync(archive, destinationFolderPath, overwriteFiles: false));
            Assert.False(File.Exists(entryFilePath), $"File should not exist: {entryFilePath}");
        }
    }
}
