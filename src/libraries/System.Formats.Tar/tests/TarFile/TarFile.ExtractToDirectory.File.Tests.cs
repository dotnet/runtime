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
    public partial class TarFile_ExtractToDirectory_File_Tests : TarTestsBase
    {
        [Fact]
        public Task ExtractToDirectoryAsync_Cancel()
        {
            CancellationTokenSource cs = new CancellationTokenSource();
            cs.Cancel();
            return Assert.ThrowsAsync<TaskCanceledException>(() => TarFile.ExtractToDirectoryAsync("file.tar", "directory", overwriteFiles: true, cs.Token));
        }

        [Theory]
        [MemberData(nameof(Get_Boolean_Data))]
        public async Task InvalidPaths_Throw(bool async)
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => ExtractToDirectory(sourceArchiveFileName: null, destinationDirectoryName: "path", overwriteFiles: false, async));
            await Assert.ThrowsAsync<ArgumentException>(() => ExtractToDirectory(sourceArchiveFileName: string.Empty, destinationDirectoryName: "path", overwriteFiles: false, async));
            await Assert.ThrowsAsync<ArgumentNullException>(() => ExtractToDirectory(sourceArchiveFileName: "path", destinationDirectoryName: null, overwriteFiles: false, async));
            await Assert.ThrowsAsync<ArgumentException>(() => ExtractToDirectory(sourceArchiveFileName: "path", destinationDirectoryName: string.Empty, overwriteFiles: false, async));
        }

        [Theory]
        [MemberData(nameof(Get_Boolean_Data))]
        public async Task NonExistentFile_Throws(bool async)
        {
            using TempDirectory root = new TempDirectory();

            string filePath = Path.Join(root.Path, "file.tar");
            string dirPath = Path.Join(root.Path, "dir");

            Directory.CreateDirectory(dirPath);

            await Assert.ThrowsAsync<FileNotFoundException>(() => ExtractToDirectory(filePath, dirPath, overwriteFiles: false, async));
        }

        [Theory]
        [MemberData(nameof(Get_Boolean_Data))]
        public async Task NonExistentDirectory_Throws(bool async)
        {
            using TempDirectory root = new TempDirectory();

            string filePath = Path.Join(root.Path, "file.tar");
            string dirPath = Path.Join(root.Path, "dir");

            File.Create(filePath).Dispose();

            await Assert.ThrowsAsync<DirectoryNotFoundException>(() => ExtractToDirectory(filePath, dirPath, overwriteFiles: false, async));
        }

        [Theory]
        [MemberData(nameof(Get_Boolean_Data))]
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
            Assert.InRange(File.GetLastWriteTime(outFile).Ticks, dt.AddSeconds(-3).Ticks, dt.AddSeconds(3).Ticks);
        }

        [Theory]
        [MemberData(nameof(Get_Boolean_Data))]
        public async Task SetsLastModifiedTimeOnExtractedDirectories(bool async)
        {
            using TempDirectory root = new TempDirectory();

            DirectoryInfo fromDir = Directory.CreateDirectory(Path.Combine(root.Path, "fromdir"));
            var directories = new DirectoryInfo[]
            {
                Directory.CreateDirectory(Path.Combine(fromDir.FullName, "dir")),
                Directory.CreateDirectory(Path.Combine(fromDir.FullName, "dir", "child")),
                Directory.CreateDirectory(Path.Combine(fromDir.FullName, "dir", "child", "subchild")),
                Directory.CreateDirectory(Path.Combine(fromDir.FullName, "dir2")),
                Directory.CreateDirectory(Path.Combine(fromDir.FullName, "dir2", "child2")),
            };
            var dt = new DateTime[directories.Length];
            for (int i = directories.Length - 1; i >= 0; i--)
            {
                File.Create(Path.Combine(directories[i].FullName, "file")).Dispose();
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
        [InlineData(TestTarFormat.v7)]
        [InlineData(TestTarFormat.ustar)]
        [InlineData(TestTarFormat.pax)]
        [InlineData(TestTarFormat.pax_gea)]
        [InlineData(TestTarFormat.gnu)]
        [InlineData(TestTarFormat.oldgnu)]
        public async Task Extract_Archive_File(TestTarFormat testFormat)
        {
            string sourceArchiveFileName = GetTarFilePath(CompressionMethod.Uncompressed, testFormat, "file");

            foreach (bool async in Booleans)
            {
                using TempDirectory destination = new TempDirectory();

                string filePath = Path.Join(destination.Path, "file.txt");

                await ExtractToDirectory(sourceArchiveFileName, destination.Path, overwriteFiles: false, async);

                Assert.True(File.Exists(filePath));
            }
        }

        [Theory]
        [MemberData(nameof(Get_Boolean_Data))]
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
                Assert.Equal($"Hello {testCaseName}", actualContents);
            }
        }

        [Theory]
        [MemberData(nameof(Get_Boolean_Data))]
        public async Task Extract_Archive_File_OverwriteFalse(bool async)
        {
            string sourceArchiveFileName = GetTarFilePath(CompressionMethod.Uncompressed, TestTarFormat.pax, "file");

            using TempDirectory destination = new TempDirectory();

            string filePath = Path.Join(destination.Path, "file.txt");
            File.Create(filePath).Dispose();

            await Assert.ThrowsAsync<IOException>(() => ExtractToDirectory(sourceArchiveFileName, destination.Path, overwriteFiles: false, async));
        }

        [Theory]
        [MemberData(nameof(Get_Boolean_Data))]
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
        [MemberData(nameof(Get_Boolean_Data))]
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
                string entryPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(Path.Join(root.Path, entry.Name)));
                Assert.True(Path.Exists(entryPath), $"Entry was not extracted: {entryPath}");
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task UnixFileModes(bool overwrite)
        {
            foreach (bool async in Booleans)
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

                    PaxTarEntry missingParentDir = new PaxTarEntry(TarEntryType.Directory, "missing_parent/dir");
                    missingParentDir.Mode = TestPermission3;
                    writer.WriteEntry(missingParentDir);

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

                Assert.True(Directory.Exists(missingParentPath), $"{missingParentPath}' does not exist.");
                AssertFileModeEquals(missingParentPath, CreateDirectoryDefaultMode);

                Assert.True(Directory.Exists(missingParentDirPath), $"{missingParentDirPath}' does not exist.");
                AssertFileModeEquals(missingParentDirPath, TestPermission3);

                Assert.True(Directory.Exists(outOfOrderDirPath), $"{outOfOrderDirPath}' does not exist.");
                AssertFileModeEquals(outOfOrderDirPath, TestPermission4);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task UnixFileModes_RestrictiveParentDir(bool overwrite)
        {
            foreach (bool async in Booleans)
            {
                using TempDirectory source = new TempDirectory();
                using TempDirectory destination = new TempDirectory();

                string archivePath = Path.Join(source.Path, "archive.tar");
                using FileStream archiveStream = File.Create(archivePath);
                using (TarWriter writer = new TarWriter(archiveStream))
                {
                    PaxTarEntry dir = new PaxTarEntry(TarEntryType.Directory, "dir");
                    dir.Mode = UnixFileMode.None;
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

                SetUnixFileMode(dirPath, UserAll);

                Assert.True(File.Exists(filePath), $"{filePath}' does not exist.");
                AssertFileModeEquals(filePath, TestPermission1);
            }
        }

        [ConditionalTheory(typeof(MountHelper), nameof(MountHelper.CanCreateSymbolicLinks))]
        [MemberData(nameof(Get_Boolean_Data))]
        public async Task LinkBeforeTarget(bool async)
        {
            using TempDirectory source = new TempDirectory();
            using TempDirectory destination = new TempDirectory();

            string archivePath = Path.Join(source.Path, "archive.tar");
            using FileStream archiveStream = File.Create(archivePath);
            using (TarWriter writer = new TarWriter(archiveStream))
            {
                PaxTarEntry link = new PaxTarEntry(TarEntryType.SymbolicLink, "link");
                link.LinkName = "dir/file";
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
            TarFile.ExtractToDirectory(archivePath, destination, overwriteFiles: false);

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
        [MemberData(nameof(Get_Boolean_Data))]
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
            TarExtractOptions extractOptions = new TarExtractOptions() { HardLinkMode = TarHardLinkMode.CopyContents };
            await ExtractToDirectory(archivePath, destination, extractOptions, async);

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

            string linkTarget = "/tmp/outside";

            string tarPath = Path.Combine(root.Path, "symlink_dir_traversal.tar");
            using (FileStream stream = new FileStream(tarPath, FileMode.Create, FileAccess.Write))
            using (TarWriter writer = new TarWriter(stream, leaveOpen: false))
            {
                writer.WriteEntry(new PaxTarEntry(TarEntryType.SymbolicLink, "link")
                {
                    LinkName = linkTarget
                });

                byte[] content = Encoding.UTF8.GetBytes("hello");
                var fileEntry = new PaxTarEntry(TarEntryType.RegularFile, "link/test.txt")
                {
                    DataStream = new MemoryStream(content, writable: false)
                };

                fileEntry.DataStream.Position = 0;
                writer.WriteEntry(fileEntry);
            }

            Assert.Throws<IOException>(() => TarFile.ExtractToDirectory(tarPath, destDir, overwriteFiles: true));

            string linkPath = Path.Combine(destDir, "link");
            string outsideFilePath = Path.Combine(destDir, "link", "test.txt");
            Assert.False(File.Exists(linkPath) || Directory.Exists(linkPath), "link should not have been created.");
            Assert.False(File.Exists(outsideFilePath) || Directory.Exists(outsideFilePath), "traversal link should not have been created.");
        }

        [ConditionalFact(typeof(MountHelper), nameof(MountHelper.CanCreateSymbolicLinks))]
        public void ExtractToDirectory_RejectsChainedSymlinkDirectoryTraversal_WithNestedFile()
        {
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

                writer.WriteEntry(new PaxTarEntry(TarEntryType.SymbolicLink, "a/b/c/d") { LinkName = "../../outside" });

                var pwned = new PaxTarEntry(TarEntryType.RegularFile, "a/d/pwned.txt")
                {
                    DataStream = new MemoryStream(Encoding.UTF8.GetBytes("pwned"))
                };
                writer.WriteEntry(pwned);
            }

            if (OperatingSystem.IsWindows())
            {
                Assert.Throws<UnauthorizedAccessException>(() => TarFile.ExtractToDirectory(tarPath, destDir, overwriteFiles: true));
            }
            else
            {
                Assert.Throws<IOException>(() => TarFile.ExtractToDirectory(tarPath, destDir, overwriteFiles: true));
            }

            string outsideDir = Path.Combine(root.Path, "outside");
            Assert.False(Directory.Exists(outsideDir), "outside/directory should not have been created.");
            Assert.False(File.Exists(Path.Combine(outsideDir, "pwned.txt")), "pwned.txt should not have been written outside destination.");
        }
    }
}
