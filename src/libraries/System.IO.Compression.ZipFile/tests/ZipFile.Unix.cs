// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Xunit;

namespace System.IO.Compression.Tests
{
    public class ZipFile_Unix : ZipFileTestBase
    {
        [Fact]
        public void UnixCreateSetsPermissionsInExternalAttributes()
        {
            // '7600' tests that S_ISUID, S_ISGID, and S_ISVTX bits get preserved in ExternalAttributes
            string[] testPermissions = new[] { "777", "755", "644", "600", "7600" };

            using (var tempFolder = new TempDirectory(Path.Combine(GetTestFilePath(), "testFolder")))
            {
                foreach (string permission in testPermissions)
                {
                    CreateFile(tempFolder.Path, permission);
                }

                string archivePath = GetTestFilePath();
                ZipFile.CreateFromDirectory(tempFolder.Path, archivePath);

                using (ZipArchive archive = ZipFile.OpenRead(archivePath))
                {
                    Assert.Equal(5, archive.Entries.Count);

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

                    foreach (string permission in testPermissions)
                    {
                        string filename = Path.Combine(extractFolder.Path, permission + ".txt");
                        Assert.True(File.Exists(filename));

                        EnsureFilePermissions(filename, permission);
                    }
                }
            }
        }

        [Fact]
        public void UnixExtractSetsFilePermissionsFromExternalAttributes()
        {
            // '7600' tests that S_ISUID, S_ISGID, and S_ISVTX bits don't get extracted to file permissions
            string[] testPermissions = new[] { "777", "755", "644", "754", "7600" };
            byte[] contents = Encoding.UTF8.GetBytes("contents");

            string archivePath = GetTestFilePath();
            using (FileStream fileStream = new FileStream(archivePath, FileMode.CreateNew))
            using (ZipArchive archive = new ZipArchive(fileStream, ZipArchiveMode.Create))
            {
                foreach (string permission in testPermissions)
                {
                    ZipArchiveEntry entry = archive.CreateEntry(permission + ".txt");
                    entry.ExternalAttributes = Convert.ToInt32(permission, 8) << 16;
                    using Stream stream = entry.Open();
                    stream.Write(contents);
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

        private static void CreateFile(string folderPath, string permissions)
        {
            string filename = Path.Combine(folderPath, $"{permissions}.txt");
            File.WriteAllText(filename, "contents");

            Assert.Equal(0, Interop.Sys.ChMod(filename, Convert.ToInt32(permissions, 8)));
        }

        private static void EnsureFilePermissions(string filename, string permissions)
        {
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

        private static string GetExpectedPermissions(string expectedPermissions)
        {
            if (string.IsNullOrEmpty(expectedPermissions))
            {
                // Create a new file, and get its permissions to get the current system default permissions

                using (var tempFolder = new TempDirectory())
                {
                    string filename = Path.Combine(tempFolder.Path, Path.GetRandomFileName());
                    File.WriteAllText(filename, "contents");

                    Interop.Sys.FileStatus status;
                    Assert.Equal(0, Interop.Sys.Stat(filename, out status));

                    expectedPermissions = Convert.ToString(status.Mode & 0xFFF, 8);
                }
            }

            return expectedPermissions;
        }
    }
}
