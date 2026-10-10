// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace System.Formats.Tar.Tests
{
    public partial class TarFile_ExtractToDirectory_File_Tests : TarFile_ExtractToDirectory_Tests
    {
        [Theory]
        [MemberData(nameof(GetBooleanData))]
        public async Task Extract_SpecialFiles_Windows_ThrowsInvalidOperation(bool async)
        {
            string originalFileName = GetTarFilePath(CompressionMethod.Uncompressed, TestTarFormat.ustar, "specialfiles");
            using TempDirectory root = new TempDirectory();

            string archive = Path.Join(root.Path, "input.tar");
            string destination = Path.Join(root.Path, "dir");

            // Copying the tar to reduce the chance of other tests failing due to being used by another process
            File.Copy(originalFileName, archive);
            Directory.CreateDirectory(destination);

            await Assert.ThrowsAsync<InvalidOperationException>(() => ExtractToDirectory(archive, destination, overwriteFiles: false, async));

            Assert.Equal(0, Directory.GetFileSystemEntries(destination).Count());
        }

        [ConditionalFact(typeof(MountHelper), nameof(MountHelper.CanCreateSymbolicLinks))]
        public void ExtractToDirectory_RejectsSymlinkWithRootedTargetOutsideDestination()
        {
            using TempDirectory root = new TempDirectory();
            string destDir = Path.Combine(root.Path, "dest");
            Directory.CreateDirectory(destDir);
            // A rooted but ambiguous path.
            string rootedLinkTarget = @"\Temp\temp.ini";
            string tarPath = Path.Combine(root.Path, "windows_symlink.tar");
            using (FileStream stream = new FileStream(tarPath, FileMode.Create, FileAccess.Write))
            using (TarWriter writer = new TarWriter(stream, leaveOpen: false))
            {
                writer.WriteEntry(new PaxTarEntry(TarEntryType.SymbolicLink, "outside.txt") { LinkName = rootedLinkTarget });
            }
            Assert.Throws<IOException>(() => TarFile.ExtractToDirectory(tarPath, destDir, overwriteFiles: true));
            Assert.Empty(Directory.EnumerateFileSystemEntries(destDir));
        }

        [ConditionalTheory(typeof(MountHelper), nameof(MountHelper.CanCreateSymbolicLinks))]
        [MemberData(nameof(GetBooleanData))]
        public async Task ExtractToDirectory_DestinationThroughSymbolicLink(bool async)
        {
            using TempDirectory root = new TempDirectory();
            string physicalDirectory = Path.Join(root.Path, "physical");
            string logicalDirectory = Path.Join(root.Path, "logical");
            string archive = Path.Join(root.Path, "input.tar");
            byte[] expected = [1, 2, 3];
            Directory.CreateDirectory(physicalDirectory);
            Directory.CreateSymbolicLink(logicalDirectory, physicalDirectory);
            using MemoryStream data = new MemoryStream(expected);
            using (FileStream stream = new FileStream(archive, FileMode.CreateNew, FileAccess.Write))
            using (TarWriter writer = new TarWriter(stream))
            {
                writer.WriteEntry(new PaxTarEntry(TarEntryType.RegularFile, "nested/file.txt")
                {
                    DataStream = data,
                    ModificationTime = TestModificationTime
                });
            }

            await ExtractToDirectory(archive, logicalDirectory, overwriteFiles: false, async);

            string destination = Path.Join(physicalDirectory, "nested", "file.txt");
            Assert.Equal(expected, File.ReadAllBytes(destination));
            Assert.Equal(TestModificationTime.UtcDateTime, File.GetLastWriteTimeUtc(destination));
        }

        [ConditionalTheory(typeof(MountHelper), nameof(MountHelper.CanCreateSymbolicLinks))]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public async Task ExtractToDirectory_ReparseRootIsResolvedForEachEntry(bool ancestorLink, bool async)
        {
            using TempDirectory root = new TempDirectory();
            string firstPhysical = Path.Join(root.Path, "first");
            string secondPhysical = Path.Join(root.Path, "second");
            string logical = Path.Join(root.Path, "logical");
            Directory.CreateDirectory(firstPhysical);
            Directory.CreateDirectory(secondPhysical);
            Directory.CreateSymbolicLink(logical, firstPhysical);
            string destination = ancestorLink ? Path.Join(logical, "destination") : logical;
            string firstDestination = ancestorLink ? Path.Join(firstPhysical, "destination") : firstPhysical;
            string secondDestination = ancestorLink ? Path.Join(secondPhysical, "destination") : secondPhysical;
            Directory.CreateDirectory(firstDestination);
            Directory.CreateDirectory(secondDestination);
            byte[] expected = [1, 2, 3];
            long secondHeaderOffset;
            using MemoryStream archive = new MemoryStream();
            using (TarWriter writer = new TarWriter(archive, TarEntryFormat.Ustar, leaveOpen: true))
            {
                using MemoryStream firstData = new MemoryStream(expected);
                writer.WriteEntry(new UstarTarEntry(TarEntryType.RegularFile, "first.txt") { DataStream = firstData });
                secondHeaderOffset = archive.Position;
                using MemoryStream secondData = new MemoryStream(expected);
                writer.WriteEntry(new UstarTarEntry(TarEntryType.RegularFile, "second.txt") { DataStream = secondData });
            }

            bool switched = false;
            using RootChangeStream source = new(archive.ToArray(), secondHeaderOffset, () =>
            {
                Assert.Equal(expected, File.ReadAllBytes(Path.Join(firstDestination, "first.txt")));
                Directory.Delete(logical);
                Directory.CreateSymbolicLink(logical, secondPhysical);
                switched = true;
            });

            await ExtractToDirectory(source, destination, overwriteFiles: false, async);

            Assert.True(switched);
            Assert.Equal(expected, File.ReadAllBytes(Path.Join(secondDestination, "second.txt")));
            Assert.False(File.Exists(Path.Join(firstDestination, "second.txt")));
        }

    }
}
