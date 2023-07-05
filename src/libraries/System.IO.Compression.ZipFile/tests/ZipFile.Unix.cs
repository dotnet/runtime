// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.IO.Compression.Tests
{
    public partial class ZipFile_Unix : ZipFileTestBase
    {
        [Fact]
        public void UnixCreateSetsPermissionsInExternalAttributes()
        {
            // '7600' tests that S_ISUID, S_ISGID, and S_ISVTX bits get preserved in ExternalAttributes
            string[] testPermissions = new[] { "777", "755", "644", "600", "7600" };

            using (var tempFolder = new TempDirectory(Path.Combine(GetTestFilePath(), "testFolder")))
            {
                string[] expectedPermissions = CreateFiles(tempFolder.Path, testPermissions);

                string archivePath = GetTestFilePath();
                ZipFile.CreateFromDirectory(tempFolder.Path, archivePath);

                using (ZipArchive archive = ZipFile.OpenRead(archivePath))
                {
                    Assert.Equal(expectedPermissions.Length, archive.Entries.Count);

                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        Assert.EndsWith(".txt", entry.Name, StringComparison.Ordinal);
                        EnsureExternalAttributes(entry.Name.Substring(0, entry.Name.Length - 4), entry);
                    }

                    void EnsureExternalAttributes(string permissions, ZipArchiveEntry entry)
                    {
                        Assert.Equal(Convert.ToInt32(permissions, 8), (entry.ExternalAttributes >> 16) & 0xFFF);
                    }
                }

                // test that round tripping the archive has the same file permissions
                using (var extractFolder = new TempDirectory(Path.Combine(GetTestFilePath(), "extract")))
                {
                    ZipFile.ExtractToDirectory(archivePath, extractFolder.Path);

                    foreach (string permission in expectedPermissions)
                    {
                        string filename = Path.Combine(extractFolder.Path, permission + ".txt");
                        Assert.True(File.Exists(filename));

                        EnsureFilePermissions(filename, permission);
                    }
                }
            }
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void UnixCreateSetsPermissionsInExternalAttributesUMaskZero()
        {
            RemoteExecutor.Invoke(() =>
            {
                umask(0);
                new ZipFile_Unix().UnixCreateSetsPermissionsInExternalAttributes();
            }).Dispose();
        }

        [Fact]
        public void UnixExtractSetsFilePermissionsFromExternalAttributes()
        {
            // '7600' tests that S_ISUID, S_ISGID, and S_ISVTX bits don't get extracted to file permissions
            string[] testPermissions = new[] { "777", "755", "644", "754", "7600" };

            string archivePath = GetTestFilePath();
            using (FileStream fileStream = new FileStream(archivePath, FileMode.CreateNew))
            using (ZipArchive archive = new ZipArchive(fileStream, ZipArchiveMode.Create))
            {
                foreach (string permission in testPermissions)
                {
                    ZipArchiveEntry entry = archive.CreateEntry(permission + ".txt");
                    entry.ExternalAttributes = Convert.ToInt32(permission, 8) << 16;
                    using Stream stream = entry.Open();
                    stream.Write("contents"u8);
                    stream.Flush();
                }
            }

            using (var tempFolder = new TempDirectory(GetTestFilePath()))
            {
                ZipFile.ExtractToDirectory(archivePath, tempFolder.Path);

                foreach (string permission in testPermissions)
                {
                    string filename = Path.Combine(tempFolder.Path, permission + ".txt");
                    Assert.True(File.Exists(filename));

                    EnsureFilePermissions(filename, permission);
                }
            }
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void UnixExtractSetsFilePermissionsFromExternalAttributesUMaskZero()
        {
            RemoteExecutor.Invoke(() =>
            {
                umask(0);
                new ZipFile_Unix().UnixExtractSetsFilePermissionsFromExternalAttributes();
            }).Dispose();
        }

        private static string[] CreateFiles(string folderPath, string[] testPermissions)
        {
            string[] expectedPermissions = new string[testPermissions.Length];

            for (int i = 0; i < testPermissions.Length; i++)
            {
                string permissions =  testPermissions[i];
                string filename = Path.Combine(folderPath, $"{permissions}.txt");
                File.WriteAllText(filename, "contents");

                File.SetUnixFileMode(filename, (UnixFileMode)Convert.ToInt32(permissions, 8));

                // In some environments, the file mode may be modified by the OS.
                // See the Rationale section of https://linux.die.net/man/3/chmod.

                // To workaround this, read the file mode back, and if it has changed, update the file name
                // since the name is used to compare the file mode.
                Interop.Sys.FileStatus status;
                Assert.Equal(0, Interop.Sys.Stat(filename, out status));
                string updatedPermissions = Convert.ToString(status.Mode & 0xFFF, 8);
                if (updatedPermissions != permissions)
                {
                    string newFileName = Path.Combine(folderPath, $"{updatedPermissions}.txt");
                    File.Move(filename, newFileName);

                    permissions = updatedPermissions;
                }

                expectedPermissions[i] = permissions;
            }

            return expectedPermissions;
        }

        private static void EnsureFilePermissions(string filename, string permissions)
        {
            permissions = GetExpectedPermissions(permissions);

            Interop.Sys.FileStatus status;
            Assert.Equal(0, Interop.Sys.Stat(filename, out status));

            // note that we don't extract S_ISUID, S_ISGID, and S_ISVTX bits,
            // so only use the last 3 numbers of permissions to verify the file permissions
            permissions = permissions.Length > 3 ? permissions.Substring(permissions.Length - 3) : permissions;
            Assert.Equal(Convert.ToInt32(permissions, 8), status.Mode & 0xFFF);
        }

        [Theory]
        [InlineData("sharpziplib.zip", null)] // ExternalAttributes are not set in this .zip, use the system default
        [InlineData("Linux_RW_RW_R__.zip", "664")]
        [InlineData("Linux_RWXRW_R__.zip", "764")]
        [InlineData("OSX_RWXRW_R__.zip", "764")]
        public void UnixExtractFilePermissionsCompat(string zipName, string expectedPermissions)
        {
            expectedPermissions = GetExpectedPermissions(expectedPermissions);

            string zipFileName = compat(zipName);
            using (var tempFolder = new TempDirectory(GetTestFilePath()))
            {
                ZipFile.ExtractToDirectory(zipFileName, tempFolder.Path);

                using ZipArchive archive = ZipFile.Open(zipFileName, ZipArchiveMode.Read);
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    string filename = Path.Combine(tempFolder.Path, entry.FullName);
                    Assert.True(File.Exists(filename), $"File '{filename}' should exist");

                    EnsureFilePermissions(filename, expectedPermissions);
                }
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix & ~TestPlatforms.Browser & ~TestPlatforms.tvOS & ~TestPlatforms.iOS)] // browser doesn't have libc mkfifo. tvOS/iOS return an error for mkfifo.
        [SkipOnPlatform(TestPlatforms.LinuxBionic, "Bionic is not normal Linux, has no normal file permissions")]
        public void ZipNamedPipeIsNotSupported()
        {
            string destPath = Path.Combine(TestDirectory, "dest.zip");

            string subFolderPath = Path.Combine(TestDirectory, "subfolder");
            string fifoPath = Path.Combine(subFolderPath, "namedPipe");
            Directory.CreateDirectory(subFolderPath); // mandatory before calling mkfifo
            Assert.Equal(0, mkfifo(fifoPath, 438 /* 666 in octal */));

            Assert.Throws<IOException>(() => ZipFile.CreateFromDirectory(subFolderPath, destPath));
        }

        private static string GetExpectedPermissions(string expectedPermissions)
        {
            using (var tempFolder = new TempDirectory())
            {
                string filename = Path.Combine(tempFolder.Path, Path.GetRandomFileName());
                FileStreamOptions fileStreamOptions = new()
                {
                    Access = FileAccess.Write,
                    Mode = FileMode.CreateNew
                };
                if (expectedPermissions != null)
                {
                    fileStreamOptions.UnixCreateMode = (UnixFileMode)Convert.ToInt32(expectedPermissions, 8);
                }
                new FileStream(filename, fileStreamOptions).Dispose();

                return Convert.ToString((int)File.GetUnixFileMode(filename), 8);
            }
        }

        [LibraryImport("libc", StringMarshalling = StringMarshalling.Utf8, SetLastError = true)]
        private static partial int mkfifo(string path, int mode);

        [LibraryImport("libc", StringMarshalling = StringMarshalling.Utf8)]
        private static partial int umask(int umask);
    }
}
