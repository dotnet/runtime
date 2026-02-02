// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Linq;
using Xunit;

namespace System.Formats.Tar.Tests
{
    public partial class TarFile_ExtractToDirectory_File_Tests : TarTestsBase
    {
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotPrivilegedProcess))]
        public void Extract_SpecialFiles_Unix_Unelevated_ThrowsUnauthorizedAccess()
        {
            string originalFileName = GetTarFilePath(CompressionMethod.Uncompressed, TestTarFormat.ustar, "specialfiles");
            using TempDirectory root = new TempDirectory();

            string archive = Path.Join(root.Path, "input.tar");
            string destination = Path.Join(root.Path, "dir");

            // Copying the tar to reduce the chance of other tests failing due to being used by another process
            File.Copy(originalFileName, archive);

            Directory.CreateDirectory(destination);

            Assert.Throws<UnauthorizedAccessException>(() => TarFile.ExtractToDirectory(archive, destination, overwriteFiles: false));

            Assert.Equal(0, Directory.GetFileSystemEntries(destination).Count());
        }

        [Theory]
        [InlineData(TarEntryFormat.V7)]
        [InlineData(TarEntryFormat.Ustar)]
        [InlineData(TarEntryFormat.Pax)]
        [InlineData(TarEntryFormat.Gnu)]
        public void HardLinkExtractionRoundtrip(TarEntryFormat format)
        {
            using TempDirectory root = new TempDirectory();

            // Create hardlinked dir1/file.txt and dir2/linked.txt.
            string sourceDir1 = Path.Join(root.Path, "source", "dir1");
            string sourceDir2 = Path.Join(root.Path, "source", "dir2");
            Directory.CreateDirectory(sourceDir1);
            Directory.CreateDirectory(sourceDir2);
            string sourceFile1 = Path.Join(sourceDir1, "file.txt");
            File.WriteAllText(sourceFile1, "test content");
            string sourceFile2 = Path.Join(sourceDir2, "linked.txt");
            File.CreateHardLink(sourceFile2, sourceFile1);

            // Create archive file.
            string archivePath = Path.Join(root.Path, "archive.tar");
            using (FileStream archiveStream = File.Create(archivePath))
            using (TarWriter writer = new TarWriter(archiveStream, format, leaveOpen: false))
            {
                writer.WriteEntry(sourceDir1, "dir1");
                writer.WriteEntry(sourceFile1, "dir1/file.txt");
                writer.WriteEntry(sourceDir2, "dir2");
                writer.WriteEntry(sourceFile2, "dir2/linked.txt");
            }

            // Extract archive using ExtractToDirectory.
            string destination = Path.Join(root.Path, "destination");
            Directory.CreateDirectory(destination);
            TarFile.ExtractToDirectory(archivePath, destination, overwriteFiles: false);

            // Verify extracted files are hard linked
            string targetFile1 = Path.Join(destination, "dir1", "file.txt");
            string targetFile2 = Path.Join(destination, "dir2", "linked.txt");
            Assert.True(File.Exists(targetFile1));
            Assert.True(File.Exists(targetFile2));
            Interop.Sys.LStat(targetFile1, out Interop.Sys.FileStatus status1);
            Interop.Sys.LStat(targetFile2, out Interop.Sys.FileStatus status2);
            Assert.Equal(status1.Ino, status2.Ino);
            Assert.Equal(status1.Dev, status2.Dev);
        }
    }
}
