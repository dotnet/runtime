// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Linq;
using Xunit;

namespace System.Formats.Tar.Tests
{
    public partial class TarFile_ExtractToDirectory_File_Tests : TarTestsBase
    {
        [Fact]
        public void InvalidPaths_Throw()
        {
            Assert.Throws<ArgumentNullException>(() => TarFile.ExtractToDirectory(sourceFileName: null, destinationDirectoryName: "path", overwriteFiles: false));
            Assert.Throws<ArgumentException>(() => TarFile.ExtractToDirectory(sourceFileName: string.Empty, destinationDirectoryName: "path", overwriteFiles: false));
            Assert.Throws<ArgumentNullException>(() => TarFile.ExtractToDirectory(sourceFileName: "path", destinationDirectoryName: null, overwriteFiles: false));
            Assert.Throws<ArgumentException>(() => TarFile.ExtractToDirectory(sourceFileName: "path", destinationDirectoryName: string.Empty, overwriteFiles: false));
        }

        [Fact]
        public void NonExistentFile_Throws()
        {
            using TempDirectory root = new TempDirectory();

            string filePath = Path.Join(root.Path, "file.tar");
            string dirPath = Path.Join(root.Path, "dir");

            Directory.CreateDirectory(dirPath);

            Assert.Throws<FileNotFoundException>(() => TarFile.ExtractToDirectory(sourceFileName: filePath, destinationDirectoryName: dirPath, overwriteFiles: false));
        }

        [Fact]
        public void NonExistentDirectory_Throws()
        {
            using TempDirectory root = new TempDirectory();

            string filePath = Path.Join(root.Path, "file.tar");
            string dirPath = Path.Join(root.Path, "dir");

            File.Create(filePath).Dispose();

            Assert.Throws<DirectoryNotFoundException>(() => TarFile.ExtractToDirectory(sourceFileName: filePath, destinationDirectoryName: dirPath, overwriteFiles: false));
        }

        [Fact]
        public void SetsLastModifiedTimeOnExtractedFiles()
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

            TarFile.CreateFromDirectory(sourceDirectoryName: inDir, destinationFileName: tarFile, includeBaseDirectory: false);

            Directory.CreateDirectory(outDir);
            TarFile.ExtractToDirectory(sourceFileName: tarFile, destinationDirectoryName: outDir, overwriteFiles: false);

            Assert.True(File.Exists(outFile));
            Assert.InRange(File.GetLastWriteTime(outFile).Ticks, dt.AddSeconds(-3).Ticks, dt.AddSeconds(3).Ticks); // include some slop for filesystem granularity
        }

        [Fact]
        public void SetsLastModifiedTimeOnExtractedDirectories()
        {
            using TempDirectory root = new TempDirectory();

            DirectoryInfo fromDir = Directory.CreateDirectory(Path.Combine(root.Path, "fromdir"));
            // Create a hierarcy of directories.
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
            TarFile.CreateFromDirectory(sourceDirectoryName: fromDir.FullName, destinationFileName: tarFile, includeBaseDirectory: false);

            string toDir = Path.Join(root.Path, "todir");
            Directory.CreateDirectory(toDir);
            TarFile.ExtractToDirectory(sourceFileName: tarFile, destinationDirectoryName: toDir, overwriteFiles: false);

            string[] extractedDirectories = Directory.GetDirectories(toDir, "*", new EnumerationOptions() { RecurseSubdirectories = true });
            Array.Sort(extractedDirectories);
            Assert.Equal(directories.Length, extractedDirectories.Length);
            for (int i = 0; i < extractedDirectories.Length; i++)
            {
                Assert.Equal(Path.GetFileName(directories[i].FullName), Path.GetFileName(extractedDirectories[i]));
                Assert.InRange(Directory.GetLastWriteTime(extractedDirectories[i]).Ticks, dt[i].AddSeconds(-3).Ticks, dt[i].AddSeconds(3).Ticks); // include some slop for filesystem granularity
            }
        }

        [Theory]
        [InlineData(TestTarFormat.v7)]
        [InlineData(TestTarFormat.ustar)]
        [InlineData(TestTarFormat.pax)]
        [InlineData(TestTarFormat.pax_gea)]
        [InlineData(TestTarFormat.gnu)]
        [InlineData(TestTarFormat.oldgnu)]
        public void Extract_Archive_File(TestTarFormat testFormat)
        {
            string sourceArchiveFileName = GetTarFilePath(CompressionMethod.Uncompressed, testFormat, "file");

            using TempDirectory destination = new TempDirectory();

            string filePath = Path.Join(destination.Path, "file.txt");

            TarFile.ExtractToDirectory(sourceArchiveFileName, destination.Path, overwriteFiles: false);

            Assert.True(File.Exists(filePath));
        }

        [Fact]
        public void Extract_Archive_File_OverwriteTrue()
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

            TarFile.ExtractToDirectory(archivePath, destination.Path, overwriteFiles: true);

            Assert.True(File.Exists(filePath));

            using (FileStream fileStream = File.Open(filePath, FileMode.Open))
            {
                using StreamReader reader = new StreamReader(fileStream);
                string actualContents = reader.ReadLine();
                Assert.Equal($"Hello {testCaseName}", actualContents); // Confirm overwrite
            }
        }

        [Fact]
        public void Extract_Archive_File_OverwriteFalse()
        {
            string sourceArchiveFileName = GetTarFilePath(CompressionMethod.Uncompressed, TestTarFormat.pax, "file");

            using TempDirectory destination = new TempDirectory();

            string filePath = Path.Join(destination.Path, "file.txt");

            File.Create(filePath).Dispose();

            Assert.Throws<IOException>(() => TarFile.ExtractToDirectory(sourceArchiveFileName, destination.Path, overwriteFiles: false));
        }

        [Fact]
        public void Extract_AllSegmentsOfPath()
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

            TarFile.ExtractToDirectory(archivePath, destination.Path, overwriteFiles: false);

            string segment1Path = Path.Join(destination.Path, "segment1");
            Assert.True(Directory.Exists(segment1Path), $"{segment1Path}' does not exist.");

            string segment2Path = Path.Join(segment1Path, "segment2");
            Assert.True(Directory.Exists(segment2Path), $"{segment2Path}' does not exist.");

            string filePath = Path.Join(segment2Path, "file.txt");
            Assert.True(File.Exists(filePath), $"{filePath}' does not exist.");
        }

        [Fact]
        public void ExtractArchiveWithEntriesThatStartWithSlashDotPrefix()
        {
            using TempDirectory root = new TempDirectory();

            using MemoryStream archiveStream = GetStrangeTarMemoryStream("prefixDotSlashAndCurrentFolderEntry");

            TarFile.ExtractToDirectory(archiveStream, root.Path, overwriteFiles: true);

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
        [InlineData(true)]
        [InlineData(false)]
        public void UnixFileModes(bool overwrite)
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

            TarFile.ExtractToDirectory(archivePath, destination.Path, overwriteFiles: overwrite);

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
        [InlineData(true)]
        [InlineData(false)]
        public void UnixFileModes_RestrictiveParentDir(bool overwrite)
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

            TarFile.ExtractToDirectory(archivePath, destination.Path, overwriteFiles: overwrite);

            Assert.True(Directory.Exists(dirPath), $"{dirPath}' does not exist.");
            AssertFileModeEquals(dirPath, UnixFileMode.None);

            // Set dir permissions so we can access file.
            SetUnixFileMode(dirPath, UserAll);

            Assert.True(File.Exists(filePath), $"{filePath}' does not exist.");
            AssertFileModeEquals(filePath, TestPermission1);
        }

        [ConditionalFact(typeof(MountHelper), nameof(MountHelper.CanCreateSymbolicLinks))]
        public void LinkBeforeTarget()
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

            TarFile.ExtractToDirectory(archivePath, destination.Path, overwriteFiles: true);

            Assert.True(File.Exists(filePath), $"{filePath}' does not exist.");
            Assert.True(File.Exists(linkPath), $"{linkPath}' does not exist.");
        }
    }
}
