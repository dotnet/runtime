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
        [ConditionalTheory(typeof(MountHelper), nameof(MountHelper.CanCreateSymbolicLinks))]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public async Task ExtractToDirectory_SymbolicLinkRootIsResolvedForEachEntry(bool ancestorLink, bool async)
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
                writer.WriteEntry(new UstarTarEntry(TarEntryType.RegularFile, "nested/first.txt") { DataStream = firstData });
                secondHeaderOffset = archive.Position;
                using MemoryStream secondData = new MemoryStream(expected);
                writer.WriteEntry(new UstarTarEntry(TarEntryType.RegularFile, "nested/second.txt") { DataStream = secondData });
            }

            bool switched = false;
            using RootChangeStream source = new(archive.ToArray(), secondHeaderOffset, () =>
            {
                Assert.Equal(expected, File.ReadAllBytes(Path.Join(firstDestination, "nested", "first.txt")));
                Directory.Delete(logical);
                Directory.CreateSymbolicLink(logical, secondPhysical);
                switched = true;
            });

            await ExtractToDirectory(source, destination, overwriteFiles: false, async);

            Assert.True(switched);
            Assert.Equal(expected, File.ReadAllBytes(Path.Join(secondDestination, "nested", "second.txt")));
            Assert.False(File.Exists(Path.Join(firstDestination, "nested", "second.txt")));
        }

        [ConditionalTheory(typeof(MountHelper), nameof(MountHelper.CanCreateSymbolicLinks))]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public async Task ExtractToDirectory_LateDirectorySymbolicLink_ExtractsInsideTarget(bool missingDestination, bool async)
        {
            using TempDirectory root = new TempDirectory();
            string destination = Path.Join(root.Path, "destination");
            if (!missingDestination)
            {
                Directory.CreateDirectory(destination);
            }
            byte[] expected = [1, 2, 3];
            using MemoryStream archive = new MemoryStream();
            using (TarWriter writer = new TarWriter(archive, leaveOpen: true))
            {
                foreach (string directory in new[] { "first", "second" })
                {
                    using MemoryStream data = new MemoryStream(expected);
                    writer.WriteEntry(new PaxTarEntry(TarEntryType.RegularFile, $"{directory}/file.txt") { DataStream = data });
                }
                writer.WriteEntry(new PaxTarEntry(TarEntryType.SymbolicLink, "alias") { LinkName = "second" });
                using MemoryStream finalData = new MemoryStream(expected);
                writer.WriteEntry(new PaxTarEntry(TarEntryType.RegularFile, "alias/nested/final.txt") { DataStream = finalData });
            }
            archive.Position = 0;

            await ExtractToDirectory(archive, destination, overwriteFiles: true, async);

            Assert.Equal("second", new DirectoryInfo(Path.Join(destination, "alias")).LinkTarget);
            Assert.Equal(expected, File.ReadAllBytes(Path.Join(destination, "second", "nested", "final.txt")));
            Assert.False(File.Exists(Path.Join(destination, "first", "nested", "final.txt")));
        }

        [ConditionalTheory(typeof(MountHelper), nameof(MountHelper.CanCreateSymbolicLinks))]
        [MemberData(nameof(GetBooleanData))]
        public async Task ExtractToDirectory_ExistingDirectorySymbolicLink_CannotBeOverwritten(bool async)
        {
            using TempDirectory root = new TempDirectory();
            string destination = Path.Join(root.Path, "destination");
            Directory.CreateDirectory(destination);
            byte[] expected = [1, 2, 3];
            using MemoryStream archive = new MemoryStream();
            using (TarWriter writer = new TarWriter(archive, leaveOpen: true))
            {
                foreach (string directory in new[] { "first", "second" })
                {
                    using MemoryStream data = new MemoryStream(expected);
                    writer.WriteEntry(new PaxTarEntry(TarEntryType.RegularFile, $"{directory}/file.txt") { DataStream = data });
                }
                writer.WriteEntry(new PaxTarEntry(TarEntryType.SymbolicLink, "alias") { LinkName = "first" });
                writer.WriteEntry(new PaxTarEntry(TarEntryType.SymbolicLink, "alias") { LinkName = "second" });
            }
            archive.Position = 0;

            await Assert.ThrowsAsync<IOException>(() => ExtractToDirectory(archive, destination, overwriteFiles: true, async));

            Assert.Equal("first", new DirectoryInfo(Path.Join(destination, "alias")).LinkTarget);
            Assert.Equal(expected, File.ReadAllBytes(Path.Join(destination, "first", "file.txt")));
            Assert.Equal(expected, File.ReadAllBytes(Path.Join(destination, "second", "file.txt")));
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotPrivilegedProcess))]
        [MemberData(nameof(GetBooleanData))]
        public async Task ExtractToDirectory_DestinationWithoutEnumerationPermission(bool async)
        {
            using TempDirectory root = new TempDirectory();
            string destination = Path.Join(root.Path, "destination");
            Directory.CreateDirectory(destination);
            byte[] expected = [1, 2, 3];
            using MemoryStream archive = new MemoryStream();
            using (TarWriter writer = new TarWriter(archive, leaveOpen: true))
            {
                foreach (string name in new[] { "first.txt", "second.txt" })
                {
                    using MemoryStream data = new MemoryStream(expected);
                    writer.WriteEntry(new PaxTarEntry(TarEntryType.RegularFile, name) { DataStream = data });
                }
            }
            archive.Position = 0;
            UnixFileMode originalMode = File.GetUnixFileMode(destination);
            File.SetUnixFileMode(destination, UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            try
            {
                await ExtractToDirectory(archive, destination, overwriteFiles: false, async);

                Assert.Equal(expected, File.ReadAllBytes(Path.Join(destination, "first.txt")));
                Assert.Equal(expected, File.ReadAllBytes(Path.Join(destination, "second.txt")));
            }
            finally
            {
                File.SetUnixFileMode(destination, originalMode);
            }
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotPrivilegedProcess))]
        [MemberData(nameof(GetBooleanData))]
        public async Task Extract_SpecialFiles_Unix_Unelevated_ThrowsUnauthorizedAccess(bool async)
        {
            string originalFileName = GetTarFilePath(CompressionMethod.Uncompressed, TestTarFormat.ustar, "specialfiles");
            using TempDirectory root = new TempDirectory();

            string archive = Path.Join(root.Path, "input.tar");
            string destination = Path.Join(root.Path, "dir");

            // Copying the tar to reduce the chance of other tests failing due to being used by another process
            File.Copy(originalFileName, archive);
            Directory.CreateDirectory(destination);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => ExtractToDirectory(archive, destination, overwriteFiles: false, async));

            Assert.Equal(0, Directory.GetFileSystemEntries(destination).Count());
        }

    }
}
