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
        public void Extract_SpecialFiles_Windows_ThrowsInvalidOperation()
        {
            string originalFileName = GetTarFilePath(CompressionMethod.Uncompressed, TestTarFormat.ustar, "specialfiles");
            using TempDirectory root = new TempDirectory();

            string archive = Path.Join(root.Path, "input.tar");
            string destination = Path.Join(root.Path, "dir");

            // Copying the tar to reduce the chance of other tests failing due to being used by another process
            File.Copy(originalFileName, archive);

            Directory.CreateDirectory(destination);

            Assert.Throws<InvalidOperationException>(() => TarFile.ExtractToDirectory(archive, destination, overwriteFiles: false));

            Assert.Equal(0, Directory.GetFileSystemEntries(destination).Count());
        }

        [ConditionalFact(typeof(MountHelper), nameof(MountHelper.CanCreateSymbolicLinks))]
        public void ExtractToDirectory_RejectsSymlinkWithRootedTargetOutsideDestination()
        {
            using TempDirectory root = new TempDirectory();
            string destDir = Path.Combine(root.Path, "dest");
            Directory.CreateDirectory(destDir);

            // A rooted path that points outside destDir (the target doesn't need to exist).
            string rootedLinkTarget = @"\Temp\temp.ini";

            string tarPath = Path.Combine(root.Path, "windows_symlink.tar");
            using (FileStream stream = new FileStream(tarPath, FileMode.Create, FileAccess.Write))
            using (TarWriter writer = new TarWriter(stream, leaveOpen: false))
            {
                writer.WriteEntry(new PaxTarEntry(TarEntryType.SymbolicLink, "outside.txt") { LinkName = rootedLinkTarget });
            }

            Assert.Throws<IOException>(() => TarFile.ExtractToDirectory(tarPath, destDir, overwriteFiles: true));

            string symlinkPath = Path.Combine(destDir, "outside.txt");
            Assert.False(File.Exists(symlinkPath) || Directory.Exists(symlinkPath), "outside.txt should not have been created.");
        }
    }
}
