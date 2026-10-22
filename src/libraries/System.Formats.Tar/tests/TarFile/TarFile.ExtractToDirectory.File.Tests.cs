// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Formats.Tar.Tests
{
    public partial class TarFile_ExtractToDirectory_File_Tests : TarFile_ExtractToDirectory_Tests
    {
        // ensures that the link name is in the correct format for the current OS.
        private static string NormalizePathSeparators(string path)
        {
            if (OperatingSystem.IsWindows())
            {
                return path.Replace('/', '\\');
            }
            else
            {
                return path;
            }
        }

        protected override Task ExtractArchive(MemoryStream archive, string destinationDirectoryName, bool overwriteFiles, bool useOptions, bool async, CancellationToken cancellationToken = default)
        {
            string archivePath = GetTestFilePath();
            File.WriteAllBytes(archivePath, archive.ToArray());
            if (async)
            {
                return useOptions
                    ? TarFile.ExtractToDirectoryAsync(archivePath, destinationDirectoryName, new TarExtractOptions { OverwriteFiles = overwriteFiles }, cancellationToken)
                    : TarFile.ExtractToDirectoryAsync(archivePath, destinationDirectoryName, overwriteFiles, cancellationToken);
            }

            if (useOptions)
            {
                TarFile.ExtractToDirectory(archivePath, destinationDirectoryName, new TarExtractOptions { OverwriteFiles = overwriteFiles });
            }
            else
            {
                TarFile.ExtractToDirectory(archivePath, destinationDirectoryName, overwriteFiles);
            }
            return Task.CompletedTask;
        }

        [Theory]
        [MemberData(nameof(GetBooleanData))]
        public async Task InvalidPaths_Throw(bool async)
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => ExtractToDirectory(sourceArchiveFileName: null, destinationDirectoryName: "path", overwriteFiles: false, async));
            await Assert.ThrowsAsync<ArgumentException>(() => ExtractToDirectory(sourceArchiveFileName: string.Empty, destinationDirectoryName: "path", overwriteFiles: false, async));
            await Assert.ThrowsAsync<ArgumentNullException>(() => ExtractToDirectory(sourceArchiveFileName: "path", destinationDirectoryName: null, overwriteFiles: false, async));
            await Assert.ThrowsAsync<ArgumentException>(() => ExtractToDirectory(sourceArchiveFileName: "path", destinationDirectoryName: string.Empty, overwriteFiles: false, async));
        }

        [Theory]
        [MemberData(nameof(GetBooleanData))]
        public async Task NonExistentFile_Throws(bool async)
        {
            using TempDirectory root = new TempDirectory();

            string filePath = Path.Join(root.Path, "file.tar");
            string dirPath = Path.Join(root.Path, "dir");

            Directory.CreateDirectory(dirPath);

            await Assert.ThrowsAsync<FileNotFoundException>(() => ExtractToDirectory(filePath, dirPath, overwriteFiles: false, async));
        }

        [Theory]
        [MemberData(nameof(GetBooleanData))]
        public async Task SetsLastModifiedTimeOnExtractedFiles(bool async)
        {
            using TempDirectory root = new TempDirectory();

            string inDir = Path.Join(root.Path, "indir");
            string inFile = Path.Join(inDir, "file");

            string tarFile = Path.Join(root.Path, "file.tar");

            string outDir = Path.Join(root.Path, "outdir");
            string outFile = Path.Join(outDir, "file");

            Directory.CreateDirectory(inDir);
            File.Create(inFile).Dispose();
            var dt = new DateTime(2001, 1, 2, 3, 4, 5, DateTimeKind.Local);
            File.SetLastWriteTime(inFile, dt);

            await CreateFromDirectory(inDir, tarFile, includeBaseDirectory: false, async);

            Directory.CreateDirectory(outDir);
            await ExtractToDirectory(tarFile, outDir, overwriteFiles: false, async);

            Assert.True(File.Exists(outFile));
            Assert.InRange(File.GetLastWriteTime(outFile).Ticks, dt.AddSeconds(-3).Ticks, dt.AddSeconds(3).Ticks); // include some slop for filesystem granularity
        }

        [Theory]
        [MemberData(nameof(GetBooleanData))]
        public async Task SetsLastModifiedTimeOnExtractedDirectories(bool async)
        {
            using TempDirectory root = new TempDirectory();

            DirectoryInfo fromDir = Directory.CreateDirectory(Path.Combine(root.Path, "fromdir"));
            // Create a hierarchy of directories.
            var directories = new DirectoryInfo[]
            {
                Directory.CreateDirectory(Path.Combine(fromDir.FullName, "dir")),                      // 'fromdir/dir'
                Directory.CreateDirectory(Path.Combine(fromDir.FullName, "dir", "child")),             // 'fromdir/dir/child'
                Directory.CreateDirectory(Path.Combine(fromDir.FullName, "dir", "child", "subchild")), // 'fromdir/dir/child/subchild'
                Directory.CreateDirectory(Path.Combine(fromDir.FullName, "dir2")),                     // 'fromdir/dir2'
                Directory.CreateDirectory(Path.Combine(fromDir.FullName, "dir2", "child2")),           // 'fromdir/dir2/child2'
            };
            var dt = new DateTime[directories.Length];
            for (int i = directories.Length - 1; i >= 0; i--) // Reverse order to preserve parent timestamps.
            {
                // Add a file.
                File.Create(Path.Combine(directories[i].FullName, "file")).Dispose();
                // Set the directory timestamp.
                dt[i] = new DateTime(2000 + i, 1 + i, 2 + i, 3 + i, 4 + i, 5 + i, DateTimeKind.Local);
                directories[i].LastWriteTime = dt[i];
            }

            string tarFile = Path.Join(root.Path, "file.tar");
            await CreateFromDirectory(fromDir.FullName, tarFile, includeBaseDirectory: false, async);

            string toDir = Path.Join(root.Path, "todir");
            Directory.CreateDirectory(toDir);
            await ExtractToDirectory(tarFile, toDir, overwriteFiles: false, async);

            string[] extractedDirectories = Directory.GetDirectories(toDir, "*", new EnumerationOptions() { RecurseSubdirectories = true });
            Array.Sort(extractedDirectories);
            Assert.Equal(directories.Length, extractedDirectories.Length);
            for (int i = 0; i < extractedDirectories.Length; i++)
            {
                Assert.Equal(Path.GetFileName(directories[i].FullName), Path.GetFileName(extractedDirectories[i]));
                Assert.InRange(Directory.GetLastWriteTime(extractedDirectories[i]).Ticks, dt[i].AddSeconds(-3).Ticks, dt[i].AddSeconds(3).Ticks);
            }
        }

        [Theory]
        [MemberData(nameof(GetTestTarFormatsAndBooleanData))]
        public async Task Extract_Archive_File(TestTarFormat testFormat, bool async)
        {
            string sourceArchiveFileName = GetTarFilePath(CompressionMethod.Uncompressed, testFormat, "file");

            using TempDirectory destination = new TempDirectory();

            string filePath = Path.Join(destination.Path, "file.txt");

            await ExtractToDirectory(sourceArchiveFileName, destination.Path, overwriteFiles: false, async);

            Assert.True(File.Exists(filePath));
        }

        [Theory]
        [MemberData(nameof(GetBooleanData))]
        public async Task Extract_Archive_File_OverwriteTrue(bool async)
        {
            string testCaseName = "file";
            string archivePath = GetTarFilePath(CompressionMethod.Uncompressed, TestTarFormat.pax, testCaseName);

            using TempDirectory destination = new TempDirectory();

            string filePath = Path.Join(destination.Path, "file.txt");
            using (FileStream fileStream = File.Create(filePath))
            {
                using StreamWriter writer = new StreamWriter(fileStream, leaveOpen: false);
                writer.WriteLine("Original text");
            }

            await ExtractToDirectory(archivePath, destination.Path, overwriteFiles: true, async);

            Assert.True(File.Exists(filePath));

            using (FileStream fileStream = File.Open(filePath, FileMode.Open))
            {
                using StreamReader reader = new StreamReader(fileStream);
                string actualContents = reader.ReadLine();
                Assert.Equal($"Hello {testCaseName}", actualContents); // Confirm overwrite
            }
        }

        [Theory]
        [MemberData(nameof(GetBooleanData))]
        public async Task Extract_Archive_File_OverwriteFalse(bool async)
        {
            string sourceArchiveFileName = GetTarFilePath(CompressionMethod.Uncompressed, TestTarFormat.pax, "file");

            using TempDirectory destination = new TempDirectory();

            string filePath = Path.Join(destination.Path, "file.txt");
            File.Create(filePath).Dispose();

            await Assert.ThrowsAsync<IOException>(() => ExtractToDirectory(sourceArchiveFileName, destination.Path, overwriteFiles: false, async));
        }

        [Theory]
        [MemberData(nameof(GetBooleanData))]
        public async Task Extract_AllSegmentsOfPath(bool async)
        {
            using TempDirectory source = new TempDirectory();
            using TempDirectory destination = new TempDirectory();

            string archivePath = Path.Join(source.Path, "archive.tar");
            using FileStream archiveStream = File.Create(archivePath);
            using (TarWriter writer = new TarWriter(archiveStream))
            {
                PaxTarEntry segment1 = new PaxTarEntry(TarEntryType.Directory, "segment1");
                writer.WriteEntry(segment1);

                PaxTarEntry segment2 = new PaxTarEntry(TarEntryType.Directory, "segment1/segment2");
                writer.WriteEntry(segment2);

                PaxTarEntry file = new PaxTarEntry(TarEntryType.RegularFile, "segment1/segment2/file.txt");
                writer.WriteEntry(file);
            }

            await ExtractToDirectory(archivePath, destination.Path, overwriteFiles: false, async);

            string segment1Path = Path.Join(destination.Path, "segment1");
            Assert.True(Directory.Exists(segment1Path), $"{segment1Path}' does not exist.");

            string segment2Path = Path.Join(segment1Path, "segment2");
            Assert.True(Directory.Exists(segment2Path), $"{segment2Path}' does not exist.");

            string filePath = Path.Join(segment2Path, "file.txt");
            Assert.True(File.Exists(filePath), $"{filePath}' does not exist.");
        }

        [Theory]
        [MemberData(nameof(GetBooleanData))]
        public async Task ExtractArchiveWithEntriesThatStartWithSlashDotPrefix(bool async)
        {
            using TempDirectory root = new TempDirectory();
            using MemoryStream archiveStream = GetStrangeTarMemoryStream("prefixDotSlashAndCurrentFolderEntry");

            await ExtractToDirectory(archiveStream, root.Path, overwriteFiles: true, async);

            archiveStream.Position = 0;

            using TarReader reader = new TarReader(archiveStream, leaveOpen: false);
            TarEntry entry;
            while ((entry = reader.GetNextEntry()) != null)
            {
                // Normalize the path (remove redundant segments), remove trailing separators
                // this is so the first entry can be skipped if it's the same as the root directory
                string entryPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(Path.Join(root.Path, entry.Name)));
                Assert.True(Path.Exists(entryPath), $"Entry was not extracted: {entryPath}");
            }
        }

        [Theory]
        [MemberData(nameof(GetTwoBooleansData))]
        public async Task UnixFileModes(bool overwrite, bool async)
        {
            using TempDirectory source = new TempDirectory();
            using TempDirectory destination = new TempDirectory();

            string archivePath = Path.Join(source.Path, "archive.tar");
            using FileStream archiveStream = File.Create(archivePath);
            using (TarWriter writer = new TarWriter(archiveStream))
            {
                PaxTarEntry dir = new PaxTarEntry(TarEntryType.Directory, "dir");
                dir.Mode = TestPermission1;
                writer.WriteEntry(dir);

                PaxTarEntry file = new PaxTarEntry(TarEntryType.RegularFile, "file");
                file.Mode = TestPermission2;
                writer.WriteEntry(file);

                // Archive has no entry for missing_parent.
                PaxTarEntry missingParentDir = new PaxTarEntry(TarEntryType.Directory, "missing_parent/dir");
                missingParentDir.Mode = TestPermission3;
                writer.WriteEntry(missingParentDir);

                // out_of_order_parent/file entry comes before out_of_order_parent entry.
                PaxTarEntry outOfOrderFile = new PaxTarEntry(TarEntryType.RegularFile, "out_of_order_parent/file");
                writer.WriteEntry(outOfOrderFile);

                PaxTarEntry outOfOrderDir = new PaxTarEntry(TarEntryType.Directory, "out_of_order_parent");
                outOfOrderDir.Mode = TestPermission4;
                writer.WriteEntry(outOfOrderDir);
            }

            string dirPath = Path.Join(destination.Path, "dir");
            string filePath = Path.Join(destination.Path, "file");
            string missingParentPath = Path.Join(destination.Path, "missing_parent");
            string missingParentDirPath = Path.Join(missingParentPath, "dir");
            string outOfOrderDirPath = Path.Join(destination.Path, "out_of_order_parent");

            if (overwrite)
            {
                File.OpenWrite(filePath).Dispose();
                Directory.CreateDirectory(dirPath);
                Directory.CreateDirectory(missingParentDirPath);
                Directory.CreateDirectory(outOfOrderDirPath);
            }

            await ExtractToDirectory(archivePath, destination.Path, overwriteFiles: overwrite, async);

            Assert.True(Directory.Exists(dirPath), $"{dirPath}' does not exist.");
            AssertFileModeEquals(dirPath, TestPermission1);

            Assert.True(File.Exists(filePath), $"{filePath}' does not exist.");
            AssertFileModeEquals(filePath, TestPermission2);

            // Missing parents are created with CreateDirectoryDefaultMode.
            Assert.True(Directory.Exists(missingParentPath), $"{missingParentPath}' does not exist.");
            AssertFileModeEquals(missingParentPath, CreateDirectoryDefaultMode);

            Assert.True(Directory.Exists(missingParentDirPath), $"{missingParentDirPath}' does not exist.");
            AssertFileModeEquals(missingParentDirPath, TestPermission3);

            // Directory modes that are out-of-order are still applied.
            Assert.True(Directory.Exists(outOfOrderDirPath), $"{outOfOrderDirPath}' does not exist.");
            AssertFileModeEquals(outOfOrderDirPath, TestPermission4);
        }

        [Theory]
        [MemberData(nameof(GetTwoBooleansData))]
        public async Task UnixFileModes_RestrictiveParentDir(bool overwrite, bool async)
        {
            using TempDirectory source = new TempDirectory();
            using TempDirectory destination = new TempDirectory();

            string archivePath = Path.Join(source.Path, "archive.tar");
            using FileStream archiveStream = File.Create(archivePath);
            using (TarWriter writer = new TarWriter(archiveStream))
            {
                PaxTarEntry dir = new PaxTarEntry(TarEntryType.Directory, "dir");
                dir.Mode = UnixFileMode.None; // Restrict permissions.
                writer.WriteEntry(dir);

                PaxTarEntry file = new PaxTarEntry(TarEntryType.RegularFile, "dir/file");
                file.Mode = TestPermission1;
                writer.WriteEntry(file);
            }

            string dirPath = Path.Join(destination.Path, "dir");
            string filePath = Path.Join(dirPath, "file");

            if (overwrite)
            {
                Directory.CreateDirectory(dirPath);
                File.OpenWrite(filePath).Dispose();
            }

            await ExtractToDirectory(archivePath, destination.Path, overwriteFiles: overwrite, async);

            Assert.True(Directory.Exists(dirPath), $"{dirPath}' does not exist.");
            AssertFileModeEquals(dirPath, UnixFileMode.None);

            // Set dir permissions so we can access file.
            SetUnixFileMode(dirPath, UserAll);

            Assert.True(File.Exists(filePath), $"{filePath}' does not exist.");
            AssertFileModeEquals(filePath, TestPermission1);
        }

        [ConditionalTheory(typeof(MountHelper), nameof(MountHelper.CanCreateSymbolicLinks))]
        [MemberData(nameof(GetBooleanData))]
        public async Task LinkBeforeTarget(bool async)
        {
            using TempDirectory source = new TempDirectory();
            using TempDirectory destination = new TempDirectory();

            string archivePath = Path.Join(source.Path, "archive.tar");
            using FileStream archiveStream = File.Create(archivePath);
            using (TarWriter writer = new TarWriter(archiveStream))
            {
                PaxTarEntry link = new PaxTarEntry(TarEntryType.SymbolicLink, "link");
                link.LinkName = NormalizePathSeparators("dir/file");
                writer.WriteEntry(link);

                PaxTarEntry file = new PaxTarEntry(TarEntryType.RegularFile, "dir/file");
                writer.WriteEntry(file);
            }

            string filePath = Path.Join(destination.Path, "dir", "file");
            string linkPath = Path.Join(destination.Path, "link");

            File.WriteAllText(linkPath, "");

            await ExtractToDirectory(archivePath, destination.Path, overwriteFiles: true, async);

            Assert.True(File.Exists(filePath), $"{filePath}' does not exist.");
            Assert.True(File.Exists(linkPath), $"{linkPath}' does not exist.");
        }

        [ConditionalTheory(typeof(MountHelper), nameof(MountHelper.CanCreateHardLinks))]
        [InlineData(TarEntryFormat.V7, TarHardLinkMode.PreserveLink)]
        [InlineData(TarEntryFormat.Ustar, TarHardLinkMode.PreserveLink)]
        [InlineData(TarEntryFormat.Pax, TarHardLinkMode.PreserveLink)]
        [InlineData(TarEntryFormat.Gnu, TarHardLinkMode.PreserveLink)]
        [InlineData(TarEntryFormat.V7, TarHardLinkMode.CopyContents)]
        [InlineData(TarEntryFormat.Ustar, TarHardLinkMode.CopyContents)]
        [InlineData(TarEntryFormat.Pax, TarHardLinkMode.CopyContents)]
        [InlineData(TarEntryFormat.Gnu, TarHardLinkMode.CopyContents)]
        public void HardLinkExtractionRoundtrip(TarEntryFormat format, TarHardLinkMode linkMode)
        {
            // Create hardlinked dir1/file.txt and dir2/linked.txt.
            using TempDirectory root = new TempDirectory();

            string sourceDir1 = Path.Join(root.Path, "source", "dir1");
            string sourceDir2 = Path.Join(root.Path, "source", "dir2");
            Directory.CreateDirectory(sourceDir1);
            Directory.CreateDirectory(sourceDir2);
            string sourceFile1 = Path.Join(sourceDir1, "file.txt");
            File.WriteAllText(sourceFile1, "test content");
            string sourceFile2 = Path.Join(sourceDir2, "linked.txt");
            File.CreateHardLink(sourceFile2, sourceFile1);

            string archivePath = Path.Join(root.Path, "archive.tar");
            // Create archive file.
            TarWriterOptions options = new TarWriterOptions() { Format = format, HardLinkMode = linkMode };
            using (FileStream archiveStream = File.Create(archivePath))
            using (TarWriter writer = new TarWriter(archiveStream, options, leaveOpen: false))
            {
                writer.WriteEntry(sourceDir1, "dir1");
                writer.WriteEntry(sourceFile1, "dir1/file.txt");
                writer.WriteEntry(sourceDir2, "dir2");
                writer.WriteEntry(sourceFile2, "dir2/linked.txt");
            }

            string destination = Path.Join(root.Path, "destination");
            Directory.CreateDirectory(destination);
            // Extract archive using ExtractToDirectory.
            TarFile.ExtractToDirectory(archivePath, destination, overwriteFiles: false);
            // Verify extracted files

            string targetFile1 = Path.Join(destination, "dir1", "file.txt");
            string targetFile2 = Path.Join(destination, "dir2", "linked.txt");
            if (linkMode == TarHardLinkMode.PreserveLink)
            {
                AssertPathsAreHardLinked(targetFile1, targetFile2);
            }
            else
            {
                Assert.True(File.Exists(targetFile1));
                Assert.True(File.Exists(targetFile2));
                Assert.Equal("test content", File.ReadAllText(targetFile1));
                Assert.Equal("test content", File.ReadAllText(targetFile2));
            }
        }

        [ConditionalTheory(typeof(MountHelper), nameof(MountHelper.CanCreateHardLinks))]
        [MemberData(nameof(GetBooleanData))]
        public async Task HardLinkExtraction_CopyContents(bool async)
        {
            using TempDirectory root = new TempDirectory();

            string sourceDir1 = Path.Join(root.Path, "source", "dir1");
            string sourceDir2 = Path.Join(root.Path, "source", "dir2");
            Directory.CreateDirectory(sourceDir1);
            Directory.CreateDirectory(sourceDir2);
            string sourceFile1 = Path.Join(sourceDir1, "file.txt");
            File.WriteAllText(sourceFile1, "test content");
            string sourceFile2 = Path.Join(sourceDir2, "linked.txt");
            File.CreateHardLink(sourceFile2, sourceFile1);

            string archivePath = Path.Join(root.Path, "archive.tar");
            // Create archive with hard link preservation.
            TarWriterOptions writerOptions = new TarWriterOptions() { Format = TarEntryFormat.Pax, HardLinkMode = TarHardLinkMode.PreserveLink };
            using (FileStream archiveStream = File.Create(archivePath))
            using (TarWriter writer = new TarWriter(archiveStream, writerOptions, leaveOpen: false))
            {
                writer.WriteEntry(sourceDir1, "dir1");
                writer.WriteEntry(sourceFile1, "dir1/file.txt");
                writer.WriteEntry(sourceDir2, "dir2");
                writer.WriteEntry(sourceFile2, "dir2/linked.txt");
            }

            string destination = Path.Join(root.Path, "destination");
            Directory.CreateDirectory(destination);
            // Extract archive with CopyContents mode.
            TarExtractOptions extractOptions = new TarExtractOptions() { HardLinkMode = TarHardLinkMode.CopyContents };
            await ExtractToDirectory(archivePath, destination, extractOptions, async);

            // Verify extracted files are independent copies.
            string targetFile1 = Path.Join(destination, "dir1", "file.txt");
            string targetFile2 = Path.Join(destination, "dir2", "linked.txt");
            Assert.True(File.Exists(targetFile1));
            Assert.True(File.Exists(targetFile2));
            Assert.Equal("test content", File.ReadAllText(targetFile1));
            Assert.Equal("test content", File.ReadAllText(targetFile2));
            AssertPathsAreNotHardLinked(targetFile1, targetFile2);
        }

        [ConditionalFact(typeof(MountHelper), nameof(MountHelper.CanCreateSymbolicLinks))]
        public void ExtractToDirectory_RejectsSymlinkDirectoryTraversal_WithNestedFile()
        {
            using TempDirectory root = new TempDirectory();
            string destDir = Path.Combine(root.Path, "dest");
            Directory.CreateDirectory(destDir);

            // Absolute path outside destDir
            string linkTarget = "/tmp/outside";

            string tarPath = Path.Combine(root.Path, "symlink_dir_traversal.tar");
            using (FileStream stream = new FileStream(tarPath, FileMode.Create, FileAccess.Write))
            using (TarWriter writer = new TarWriter(stream, leaveOpen: false))
            {
                // symlink: "link" -> "/tmp/outside"
                writer.WriteEntry(new PaxTarEntry(TarEntryType.SymbolicLink, "link")
                {
                    LinkName = NormalizePathSeparators(linkTarget)
                });

                // file: "link/test.txt" with "hello"
                byte[] content = Encoding.UTF8.GetBytes("hello");
                var fileEntry = new PaxTarEntry(TarEntryType.RegularFile, "link/test.txt")
                {
                    DataStream = new MemoryStream(content, writable: false)
                };

                fileEntry.DataStream.Position = 0;
                writer.WriteEntry(fileEntry);
            }

            Assert.Throws<IOException>(() => TarFile.ExtractToDirectory(tarPath, destDir, overwriteFiles: true));

            // Nothing should be created in dest
            string linkPath = Path.Combine(destDir, "link");
            string outsideFilePath = Path.Combine(destDir, "link", "test.txt");
            Assert.False(File.Exists(linkPath) || Directory.Exists(linkPath), "link should not have been created.");
            Assert.False(File.Exists(outsideFilePath) || Directory.Exists(outsideFilePath), "traversal link should not have been created.");
        }

        [ConditionalFact(typeof(MountHelper), nameof(MountHelper.CanCreateSymbolicLinks))]
        public void ExtractToDirectory_RejectsChainedSymlinkDirectoryTraversal_WithNestedFile()
        {
            // dir a/
            // symlink a/b → .
            // symlink a/b/c → .
            // symlink a/b/c/d → ../../outside
            // file a/d/ pwned.txt escapes

            using TempDirectory root = new TempDirectory();
            string destDir = Path.Combine(root.Path, "dest");
            Directory.CreateDirectory(destDir);

            string tarPath = Path.Combine(root.Path, "chained_symlink_traversal.tar");
            using (FileStream stream = new FileStream(tarPath, FileMode.Create, FileAccess.Write))
            using (TarWriter writer = new TarWriter(stream, leaveOpen: false))
            {
                writer.WriteEntry(new PaxTarEntry(TarEntryType.Directory, "a/"));

                writer.WriteEntry(new PaxTarEntry(TarEntryType.SymbolicLink, "a/b") { LinkName = "." });

                writer.WriteEntry(new PaxTarEntry(TarEntryType.SymbolicLink, "a/b/c") { LinkName = "." });

                writer.WriteEntry(new PaxTarEntry(TarEntryType.SymbolicLink, "a/b/c/d") { LinkName = NormalizePathSeparators("../../outside") });

                var pwned = new PaxTarEntry(TarEntryType.RegularFile, "a/d/pwned.txt")
                {
                    DataStream = new MemoryStream(Encoding.UTF8.GetBytes("pwned"))
                };
                writer.WriteEntry(pwned);
            }

            if (OperatingSystem.IsWindows())
            {
                // Windows only creates file symlinks and trying to process a directory symlink will throw
                // UnauthorizedAccessException instead of IOException.
                // Windows always creates file symlinks (FileInfo.CreateAsSymbolicLink), so entry "a/b" becomes a
                // *file* symlink whose target (".") is a *directory*. Processing the nested entries forces the
                // extractor to resolve/descend through that type-mismatched reparse point, which Windows rejects,
                // but the surfaced exception depends on the OS build:
                //   - Some builds throw UnauthorizedAccessException while resolving the link (FileInfo.ResolveLinkTarget
                //     during the traversal-safety walk in FilePathEscapesDirectory).
                //   - Others resolve the link successfully, then fail creating a directory where the file symlink
                //     already exists, throwing IOException ("a file or directory with the same name already exists").
                // Either way the archive is rejected and nothing escapes (verified below), so accept both.
                Exception ex = Assert.ThrowsAny<Exception>(() => TarFile.ExtractToDirectory(tarPath, destDir, overwriteFiles: true));
                Assert.True(ex is IOException or UnauthorizedAccessException, $"Unexpected exception type: {ex}");
            }
            else
            {
                Assert.Throws<IOException>(() => TarFile.ExtractToDirectory(tarPath, destDir, overwriteFiles: true));
            }

            string outsideDir = Path.Combine(root.Path, "outside");
            Assert.False(Directory.Exists(outsideDir), "outside/directory should not have been created.");
            Assert.False(File.Exists(Path.Combine(outsideDir, "pwned.txt")), "pwned.txt should not have been written outside destination.");
        }

        [ConditionalFact(typeof(MountHelper), nameof(MountHelper.CanCreateSymbolicLinks))]
        public void ExtractToDirectory_RejectsSymlinkDirectoryTraversal()
        {
            using TempDirectory root = new TempDirectory();
            string destDir = Path.Combine(root.Path, "dest");
            Directory.CreateDirectory(destDir);

            string tarPath = Path.Combine(root.Path, "symlink_dir_traversal.tar");
            using (FileStream stream = new FileStream(tarPath, FileMode.Create, FileAccess.Write))
            using (TarWriter writer = new TarWriter(stream, leaveOpen: false))
            {
                // symlink: "link" -> "/tmp/outside" (absolute path outside destDir)
                writer.WriteEntry(new PaxTarEntry(TarEntryType.SymbolicLink, "link")
                {
                    LinkName = NormalizePathSeparators("/tmp/outside")
                });
            }

            Assert.Throws<IOException>(() => TarFile.ExtractToDirectory(tarPath, destDir, overwriteFiles: true));
        }

        [ConditionalFact(typeof(MountHelper), nameof(MountHelper.CanCreateSymbolicLinks))]
        public void ExtractToDirectory_DefersSymlinkWithDotDot()
        {
            // Symlinks whose target contains '..' are deferred: a placeholder file is created
            // during extraction, and the real symlink replaces it after all entries are processed.
            // This prevents subsequent entries from following the symlink to escape the destination.

            using TempDirectory root = new TempDirectory();
            string destDir = Path.Combine(root.Path, "dest");
            Directory.CreateDirectory(destDir);

            string tarPath = Path.Combine(root.Path, "deferred_symlink.tar");
            using (FileStream stream = new FileStream(tarPath, FileMode.Create, FileAccess.Write))
            using (TarWriter writer = new TarWriter(stream, leaveOpen: false))
            {
                writer.WriteEntry(new PaxTarEntry(TarEntryType.Directory, "a/"));
                writer.WriteEntry(new PaxTarEntry(TarEntryType.SymbolicLink, "a/link") { LinkName = NormalizePathSeparators("../b") });
                writer.WriteEntry(new PaxTarEntry(TarEntryType.Directory, "b/"));
            }

            TarFile.ExtractToDirectory(tarPath, destDir, overwriteFiles: true);

            string linkPath = Path.Combine(destDir, "a", "link");
            FileSystemInfo linkInfo = new FileInfo(linkPath);
            Assert.Equal(NormalizePathSeparators("../b"), linkInfo.LinkTarget);
        }

        [ConditionalFact(typeof(MountHelper), nameof(MountHelper.CanCreateSymbolicLinks))]
        public void ExtractToDirectory_DefersSymlinkWithDotDot_UnderSymlinkedParent()
        {
            using TempDirectory root = new TempDirectory();
            string realRoot = Path.Combine(root.Path, "real");
            Directory.CreateDirectory(realRoot);

            string linkedRoot = Path.Combine(root.Path, "link");
            Directory.CreateSymbolicLink(linkedRoot, realRoot);

            string destDir = Path.Combine(linkedRoot, "dest");
            Directory.CreateDirectory(destDir);

            string tarPath = Path.Combine(root.Path, "deferred_symlink.tar");
            using (FileStream stream = new FileStream(tarPath, FileMode.Create, FileAccess.Write))
            using (TarWriter writer = new TarWriter(stream, leaveOpen: false))
            {
                writer.WriteEntry(new PaxTarEntry(TarEntryType.Directory, "a/"));
                writer.WriteEntry(new PaxTarEntry(TarEntryType.SymbolicLink, "a/link") { LinkName = NormalizePathSeparators("../b") });
                writer.WriteEntry(new PaxTarEntry(TarEntryType.Directory, "b/"));
            }

            TarFile.ExtractToDirectory(tarPath, destDir, overwriteFiles: true);

            string linkPath = Path.Combine(destDir, "a", "link");
            Assert.Equal(NormalizePathSeparators("../b"), new FileInfo(linkPath).LinkTarget);
        }

        [ConditionalFact(typeof(MountHelper), nameof(MountHelper.CanCreateSymbolicLinks))]
        public void ExtractToDirectory_DeferredSymlinkBlocksTraversal()
        {
            // A file entry after a deferred symlink cannot follow the symlink to write
            // outside the destination directory, because the symlink is a placeholder file
            // during extraction.
            //
            // dir a/
            // symlink a/b -> .
            // symlink a/b/c -> .
            // symlink a/b/c/escape -> ../../..   (deferred: contains '..')
            // file a/b/c/escape/pwned            (would escape via chained symlinks if not deferred)

            using TempDirectory root = new TempDirectory();
            string destDir = Path.Combine(root.Path, "dest");
            Directory.CreateDirectory(destDir);

            string tarPath = Path.Combine(root.Path, "chained_symlink_traversal.tar");
            using (FileStream stream = new FileStream(tarPath, FileMode.Create, FileAccess.Write))
            using (TarWriter writer = new TarWriter(stream, leaveOpen: false))
            {
                writer.WriteEntry(new PaxTarEntry(TarEntryType.Directory, "a/"));
                writer.WriteEntry(new PaxTarEntry(TarEntryType.SymbolicLink, "a/b") { LinkName = "." });
                writer.WriteEntry(new PaxTarEntry(TarEntryType.SymbolicLink, "a/b/c") { LinkName = "." });
                writer.WriteEntry(new PaxTarEntry(TarEntryType.SymbolicLink, "a/b/c/escape") { LinkName = NormalizePathSeparators("../../..") });
                writer.WriteEntry(new PaxTarEntry(TarEntryType.RegularFile, "a/b/c/escape/pwned"));
            }

            // The file entry "a/b/c/escape/pwned" cannot follow the 'escape' symlink because
            // it is a placeholder file during extraction. Creating the file's parent directory
            // will fail because 'escape' is a regular file, not a directory.
            Assert.ThrowsAny<IOException>(() => TarFile.ExtractToDirectory(tarPath, destDir, overwriteFiles: true));

            // The file must not exist at the root (where the symlink chain would have led).
            Assert.False(File.Exists(Path.Combine(root.Path, "pwned")));
        }

        [ConditionalFact(typeof(MountHelper), nameof(MountHelper.CanCreateSymbolicLinks))]
        public void ExtractToDirectory_AllowsSymlinkToDestinationDirectory()
        {
            using TempDirectory root = new TempDirectory();
            string destDir = Path.Combine(root.Path, "dest");
            Directory.CreateDirectory(destDir);

            string tarPath = Path.Combine(root.Path, "symlink_to_dest.tar");
            using (FileStream stream = new FileStream(tarPath, FileMode.Create, FileAccess.Write))
            using (TarWriter writer = new TarWriter(stream, leaveOpen: false))
            {
                // symlink: "link" -> "." (points to the destination directory itself)
                writer.WriteEntry(new PaxTarEntry(TarEntryType.SymbolicLink, "link") { LinkName = "." });
            }

            TarFile.ExtractToDirectory(tarPath, destDir, overwriteFiles: true);

            string linkPath = Path.Combine(destDir, "link");
            FileSystemInfo linkInfo = new FileInfo(linkPath);
            Assert.Equal(".", linkInfo.LinkTarget);
        }
        [ConditionalFact(typeof(MountHelper), nameof(MountHelper.CanCreateSymbolicLinks))]
        public void ExtractToDirectory_DeferredSymlinkAllowedUnderExistingExternalSymlink()
        {
            // When the destination contains an existing symlink that points outside,
            // a deferred symlink (with '..') under that external location is allowed
            // as long as it stays within the directory the existing symlink resolved to.

            using TempDirectory root = new TempDirectory();
            string destDir = Path.Combine(root.Path, "dest");
            Directory.CreateDirectory(destDir);

            // Create an external directory and an existing symlink in dest pointing to it.
            string externalDir = Path.Combine(root.Path, "external");
            Directory.CreateDirectory(Path.Combine(externalDir, "sub"));
            Directory.CreateSymbolicLink(Path.Combine(destDir, "ext"), externalDir);

            string tarPath = Path.Combine(root.Path, "deferred_under_external.tar");
            using (FileStream stream = new FileStream(tarPath, FileMode.Create, FileAccess.Write))
            using (TarWriter writer = new TarWriter(stream, leaveOpen: false))
            {
                // ext/sub/link -> .. (resolves to externalDir, stays within it)
                writer.WriteEntry(new PaxTarEntry(TarEntryType.SymbolicLink, "ext/sub/link") { LinkName = ".." });
            }

            TarFile.ExtractToDirectory(tarPath, destDir, overwriteFiles: true);

            string linkPath = Path.Combine(destDir, "ext", "sub", "link");
            Assert.Equal("..", new FileInfo(linkPath).LinkTarget);
        }

        [ConditionalFact(typeof(MountHelper), nameof(MountHelper.CanCreateSymbolicLinks))]
        public void ExtractToDirectory_DeferredSymlinkRejectedWhenEscapingExternalSymlink()
        {
            // When the destination contains an existing symlink that points outside,
            // a deferred symlink that escapes beyond that external directory is rejected.

            using TempDirectory root = new TempDirectory();
            string destDir = Path.Combine(root.Path, "dest");
            Directory.CreateDirectory(destDir);

            string externalDir = Path.Combine(root.Path, "external");
            Directory.CreateDirectory(externalDir);
            Directory.CreateSymbolicLink(Path.Combine(destDir, "ext"), externalDir);

            string tarPath = Path.Combine(root.Path, "escape_external.tar");
            using (FileStream stream = new FileStream(tarPath, FileMode.Create, FileAccess.Write))
            using (TarWriter writer = new TarWriter(stream, leaveOpen: false))
            {
                // ext/link -> ../.. (escapes beyond externalDir)
                writer.WriteEntry(new PaxTarEntry(TarEntryType.SymbolicLink, "ext/link") { LinkName = NormalizePathSeparators("../..") });
            }

            Assert.Throws<IOException>(() => TarFile.ExtractToDirectory(tarPath, destDir, overwriteFiles: true));
        }

        [ConditionalFact(typeof(MountHelper), nameof(MountHelper.CanCreateSymbolicLinks))]
        public void ExtractToDirectory_CircularDeferredSymlinksAllowed()
        {
            // Circular symlinks that contain '..' but stay within the destination are allowed.
            // The cycle detection (visitedLinks) prevents infinite recursion during resolution.
            //
            // a/link1 -> ../b
            // b/link2 -> ../a

            using TempDirectory root = new TempDirectory();
            string destDir = Path.Combine(root.Path, "dest");
            Directory.CreateDirectory(destDir);

            string tarPath = Path.Combine(root.Path, "circular_deferred.tar");
            using (FileStream stream = new FileStream(tarPath, FileMode.Create, FileAccess.Write))
            using (TarWriter writer = new TarWriter(stream, leaveOpen: false))
            {
                writer.WriteEntry(new PaxTarEntry(TarEntryType.Directory, "a/"));
                writer.WriteEntry(new PaxTarEntry(TarEntryType.Directory, "b/"));
                writer.WriteEntry(new PaxTarEntry(TarEntryType.SymbolicLink, "a/link1") { LinkName = NormalizePathSeparators("../b") });
                writer.WriteEntry(new PaxTarEntry(TarEntryType.SymbolicLink, "b/link2") { LinkName = NormalizePathSeparators("../a") });
            }

            TarFile.ExtractToDirectory(tarPath, destDir, overwriteFiles: true);

            string link1Path = Path.Combine(destDir, "a", "link1");
            string link2Path = Path.Combine(destDir, "b", "link2");
            Assert.Equal(NormalizePathSeparators("../b"), new FileInfo(link1Path).LinkTarget);
            Assert.Equal(NormalizePathSeparators("../a"), new FileInfo(link2Path).LinkTarget);
        }
        [ConditionalFact(typeof(MountHelper), nameof(MountHelper.CanCreateSymbolicLinks))]
        public void ExtractToDirectory_OverwriteDeferredSymlink()
        {
            // When a second symlink entry overwrites a previously deferred symlink,
            // the placeholder is replaced and the final symlink uses the second target.

            using TempDirectory root = new TempDirectory();
            string destDir = Path.Combine(root.Path, "dest");
            Directory.CreateDirectory(destDir);

            string tarPath = Path.Combine(root.Path, "overwrite_deferred.tar");
            using (FileStream stream = new FileStream(tarPath, FileMode.Create, FileAccess.Write))
            using (TarWriter writer = new TarWriter(stream, leaveOpen: false))
            {
                writer.WriteEntry(new PaxTarEntry(TarEntryType.Directory, "a/"));
                writer.WriteEntry(new PaxTarEntry(TarEntryType.Directory, "b/"));
                writer.WriteEntry(new PaxTarEntry(TarEntryType.Directory, "c/"));
                writer.WriteEntry(new PaxTarEntry(TarEntryType.SymbolicLink, "a/link") { LinkName = NormalizePathSeparators("../b") });
                writer.WriteEntry(new PaxTarEntry(TarEntryType.SymbolicLink, "a/link") { LinkName = NormalizePathSeparators("../c") });
            }

            TarFile.ExtractToDirectory(tarPath, destDir, overwriteFiles: true);

            string linkPath = Path.Combine(destDir, "a", "link");
            Assert.Equal(NormalizePathSeparators("../c"), new FileInfo(linkPath).LinkTarget);
        }

        [ConditionalFact(typeof(MountHelper), nameof(MountHelper.CanCreateSymbolicLinks))]
        public void ExtractToDirectory_RegularFileOverwritesDeferredSymlink()
        {
            // When a regular file entry overwrites a previously deferred symlink,
            // the placeholder is replaced by the file and its pending link entry is
            // dropped, so the final entry is the file (not the symlink).

            using TempDirectory root = new TempDirectory();
            string destDir = Path.Combine(root.Path, "dest");
            Directory.CreateDirectory(destDir);

            string tarPath = Path.Combine(root.Path, "file_overwrites_deferred.tar");
            using (FileStream stream = new FileStream(tarPath, FileMode.Create, FileAccess.Write))
            using (TarWriter writer = new TarWriter(stream, leaveOpen: false))
            {
                writer.WriteEntry(new PaxTarEntry(TarEntryType.Directory, "a/"));
                writer.WriteEntry(new PaxTarEntry(TarEntryType.Directory, "b/"));
                writer.WriteEntry(new PaxTarEntry(TarEntryType.SymbolicLink, "a/link") { LinkName = NormalizePathSeparators("../b") });
                writer.WriteEntry(new PaxTarEntry(TarEntryType.RegularFile, "a/link")
                {
                    DataStream = new MemoryStream(Encoding.UTF8.GetBytes("content"))
                });
            }

            TarFile.ExtractToDirectory(tarPath, destDir, overwriteFiles: true);

            string linkPath = Path.Combine(destDir, "a", "link");
            Assert.Null(new FileInfo(linkPath).LinkTarget);
            Assert.Equal("content", File.ReadAllText(linkPath));
        }

        [ConditionalFact(typeof(MountHelper), nameof(MountHelper.CanCreateSymbolicLinks))]
        public void ExtractToDirectory_AbsoluteSymlinkWithinDestination()
        {
            // A symlink with a fully qualified target that points within the destination
            // is allowed and is not deferred (no '..').

            using TempDirectory root = new TempDirectory();
            string destDir = Path.Combine(root.Path, "dest");
            Directory.CreateDirectory(destDir);

            string absoluteTarget = Path.Combine(destDir, "target");

            string tarPath = Path.Combine(root.Path, "absolute_symlink.tar");
            using (FileStream stream = new FileStream(tarPath, FileMode.Create, FileAccess.Write))
            using (TarWriter writer = new TarWriter(stream, leaveOpen: false))
            {
                writer.WriteEntry(new PaxTarEntry(TarEntryType.Directory, "target/"));
                writer.WriteEntry(new PaxTarEntry(TarEntryType.SymbolicLink, "link") { LinkName = NormalizePathSeparators(absoluteTarget) });
            }

            TarFile.ExtractToDirectory(tarPath, destDir, overwriteFiles: true);

            string linkPath = Path.Combine(destDir, "link");
            Assert.Equal(absoluteTarget, new FileInfo(linkPath).LinkTarget);
        }

        [ConditionalFact(typeof(MountHelper), nameof(MountHelper.CanCreateSymbolicLinks))]
        public void ExtractToDirectory_DeferredSymlinkWithAbsoluteTarget()
        {
            // A deferred symlink (contains '..') with a fully qualified target that
            // resolves within the destination is allowed. Exercises the
            // rootPath = resolvedDestinationPath path in ResolvePathInRootOrDestination.

            using TempDirectory root = new TempDirectory();
            string destDir = Path.Combine(root.Path, "dest");
            Directory.CreateDirectory(destDir);

            // Absolute target with '..' that resolves back into dest.
            string absoluteTarget = NormalizePathSeparators(Path.Combine(destDir, "a", "..", "b"));

            string tarPath = Path.Combine(root.Path, "deferred_absolute.tar");
            using (FileStream stream = new FileStream(tarPath, FileMode.Create, FileAccess.Write))
            using (TarWriter writer = new TarWriter(stream, leaveOpen: false))
            {
                writer.WriteEntry(new PaxTarEntry(TarEntryType.Directory, "b/"));
                writer.WriteEntry(new PaxTarEntry(TarEntryType.SymbolicLink, "link") { LinkName = absoluteTarget });
            }

            TarFile.ExtractToDirectory(tarPath, destDir, overwriteFiles: true);

            string linkPath = Path.Combine(destDir, "link");
            // Windows APIs collapse the intermediate ".." in the link target, so the expected target is destDir/b. On Unix, the link target is preserved as-is.
            string expectedTarget = OperatingSystem.IsWindows() ? Path.Combine(destDir, "b") : absoluteTarget;
            Assert.Equal(expectedTarget, new FileInfo(linkPath).LinkTarget);
        }

        [ConditionalFact(typeof(MountHelper), nameof(MountHelper.CanCreateSymbolicLinks))]
        public void ExtractToDirectory_ChainedDeferredSymlinks()
        {
            // Two deferred symlinks where resolving the second one's target
            // passes through the first one, forcing recursive resolution
            // during CreatePendingLinks.
            //
            // a/link1 -> ../b             (deferred: contains '..')
            // a/link2 -> link1/../b/sub   (deferred: contains '..',
            //                              resolving encounters link1 as pending link,
            //                              triggering recursive creation of link1 first)

            using TempDirectory root = new TempDirectory();
            string destDir = Path.Combine(root.Path, "dest");
            Directory.CreateDirectory(destDir);

            string tarPath = Path.Combine(root.Path, "chained_deferred.tar");
            using (FileStream stream = new FileStream(tarPath, FileMode.Create, FileAccess.Write))
            using (TarWriter writer = new TarWriter(stream, leaveOpen: false))
            {
                writer.WriteEntry(new PaxTarEntry(TarEntryType.Directory, "a/"));
                writer.WriteEntry(new PaxTarEntry(TarEntryType.Directory, "b/"));
                writer.WriteEntry(new PaxTarEntry(TarEntryType.Directory, "b/sub/"));
                writer.WriteEntry(new PaxTarEntry(TarEntryType.SymbolicLink, "a/link1") { LinkName = NormalizePathSeparators("../b") });
                writer.WriteEntry(new PaxTarEntry(TarEntryType.SymbolicLink, "a/link2") { LinkName = NormalizePathSeparators("link1/../b/sub") });
            }

            TarFile.ExtractToDirectory(tarPath, destDir, overwriteFiles: true);

            string link1Path = Path.Combine(destDir, "a", "link1");
            string link2Path = Path.Combine(destDir, "a", "link2");
            Assert.Equal(NormalizePathSeparators("../b"), new FileInfo(link1Path).LinkTarget);
            Assert.Equal(NormalizePathSeparators("link1/../b/sub"), new FileInfo(link2Path).LinkTarget);
        }

        [ConditionalFact(typeof(MountHelper), nameof(MountHelper.CanCreateSymbolicLinks))]
        public void ExtractToDirectory_EscapeViaSymlinkToCurrentDirectory_Throws()
        {
            using TempDirectory root = new TempDirectory();
            string destDir = Path.Combine(root.Path, "dest");
            Directory.CreateDirectory(destDir);

            string tarPath = Path.Combine(root.Path, "escape.tar");
            using (FileStream stream = new FileStream(tarPath, FileMode.Create, FileAccess.Write))
            using (TarWriter writer = new TarWriter(stream, leaveOpen: false))
            {
                writer.WriteEntry(new PaxTarEntry(TarEntryType.SymbolicLink, "a") { LinkName = "." });
                writer.WriteEntry(new PaxTarEntry(TarEntryType.SymbolicLink, "link") { LinkName = NormalizePathSeparators("a/../outside") });
            }

            Assert.Throws<IOException>(() => TarFile.ExtractToDirectory(tarPath, destDir, overwriteFiles: true));
        }

        [ConditionalFact(typeof(MountHelper), nameof(MountHelper.CanCreateSymbolicLinks))]
        public void ExtractToDirectory_ExceedsMaxSymlinkDepth()
        {
            // A chain of deferred symlinks that exceeds MaxSymlinkDepth (40)
            // should throw during CreatePendingLinks.
            //
            // Each link_N -> ../link_{N-1} (deferred because of '..').
            // Resolving link_41's target chains through all 41 preceding
            // symlinks, exceeding the depth limit.

            using TempDirectory root = new TempDirectory();
            string destDir = Path.Combine(root.Path, "dest");
            Directory.CreateDirectory(destDir);

            string tarPath = Path.Combine(root.Path, "deep_symlinks.tar");
            using (FileStream stream = new FileStream(tarPath, FileMode.Create, FileAccess.Write))
            using (TarWriter writer = new TarWriter(stream, leaveOpen: false))
            {
                const int count = 42;
                for (int i = 0; i < count; i++)
                {
                    writer.WriteEntry(new PaxTarEntry(TarEntryType.Directory, $"d{i}/"));
                }
                // link_0 -> ../d0 (base case)
                writer.WriteEntry(new PaxTarEntry(TarEntryType.SymbolicLink, "d0/link_0") { LinkName = NormalizePathSeparators("../d0") });
                // link_N -> ../d{N-1}/link_{N-1} — each follows the previous, all deferred
                for (int i = 1; i < count; i++)
                {
                    writer.WriteEntry(new PaxTarEntry(TarEntryType.SymbolicLink, $"d{i}/link_{i}") { LinkName = NormalizePathSeparators($"../d{i - 1}/link_{i - 1}") });
                }
            }

            Assert.Throws<IOException>(() => TarFile.ExtractToDirectory(tarPath, destDir, overwriteFiles: true));
        }

        [ConditionalTheory(typeof(MountHelper), nameof(MountHelper.CanCreateSymbolicLinks))]
        [InlineData("../b")] // deferred: contains '..'
        [InlineData("b")]    // not deferred
        public void ExtractToDirectory_OverwritesExistingDanglingSymlink(string linkName)
        {
            using TempDirectory root = new TempDirectory();
            string destDir = Path.Combine(root.Path, "dest");
            Directory.CreateDirectory(Path.Combine(destDir, "a"));

            // A pre-existing symlink whose target does not exist.
            File.CreateSymbolicLink(Path.Combine(destDir, "a", "link"), "does_not_exist");

            string tarPath = Path.Combine(root.Path, "overwrite_dangling.tar");
            using (FileStream stream = new FileStream(tarPath, FileMode.Create, FileAccess.Write))
            using (TarWriter writer = new TarWriter(stream, leaveOpen: false))
            {
                writer.WriteEntry(new PaxTarEntry(TarEntryType.Directory, "a/"));
                writer.WriteEntry(new PaxTarEntry(TarEntryType.SymbolicLink, "a/link") { LinkName = NormalizePathSeparators(linkName) });
                writer.WriteEntry(new PaxTarEntry(TarEntryType.Directory, "b/"));
            }

            TarFile.ExtractToDirectory(tarPath, destDir, overwriteFiles: true);

            Assert.Equal(NormalizePathSeparators(linkName), new FileInfo(Path.Combine(destDir, "a", "link")).LinkTarget);
        }

        [ConditionalFact(typeof(MountHelper), nameof(MountHelper.CanCreateSymbolicLinks))]
        public void ExtractToDirectory_HardlinkToSymlink_DoesNotEscape()
        {
            // entries:
            // a/ (dir)
            // a/b -> ../ (symlink)
            // c -> a/b (hardlink to symlink)
            // c/d (file) — would escape to dest/../d if symlink is followed

            using TempDirectory root = new TempDirectory();
            string destDir = Path.Combine(root.Path, "dest");
            Directory.CreateDirectory(destDir);

            using MemoryStream stream = new MemoryStream();
            using (TarWriter writer = new TarWriter(stream, leaveOpen: true))
            {
                writer.WriteEntry(new PaxTarEntry(TarEntryType.Directory, "a/"));
                writer.WriteEntry(new PaxTarEntry(TarEntryType.SymbolicLink, "a/b") { LinkName = NormalizePathSeparators("../") });
                writer.WriteEntry(new PaxTarEntry(TarEntryType.HardLink, "c") { LinkName = "a/b" });
                writer.WriteEntry(new PaxTarEntry(TarEntryType.RegularFile, "c/d")
                {
                    DataStream = new MemoryStream(Encoding.UTF8.GetBytes("hello"))
                });
            }

            stream.Seek(0, SeekOrigin.Begin);

            Assert.Throws<IOException>(() => TarFile.ExtractToDirectory(stream, destDir, overwriteFiles: true));
            Assert.False(File.Exists(Path.Combine(destDir, "d")), "File should not have been created outside destination.");
        }
    }
}
