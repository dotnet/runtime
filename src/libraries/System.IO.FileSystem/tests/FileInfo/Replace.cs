// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.IO.Tests
{
    public class FileInfo_Replace : FileSystemTest
    {
        [Fact]
        public void Null_FileName()
        {
            string testFile = GetTestFilePath();
            AssertExtensions.Throws<ArgumentNullException>("destinationFileName", () => Replace(testFile, null, ""));
        }

        [Fact]
        public void FileInfo_IsSame_NoBackup()
        {
            string srcFile = GetTestFilePath();
            string destFile = GetTestFilePath();
            File.Create(destFile).Dispose();
            FileInfo destInfo = new FileInfo(destFile);

            var srcContents = new byte[] { 1, 3, 4, 8 };
            File.WriteAllBytes(srcFile, srcContents);

            FileInfo newInfo = Replace(srcFile, destFile, null);

            Assert.False(File.Exists(srcFile));
            Assert.NotNull(newInfo);
            Assert.Equal(destInfo.FullName, newInfo.FullName);
            Assert.Equal(srcContents, File.ReadAllBytes(newInfo.FullName));
        }

        [Fact]
        public void FileInfo_IsSame_Backup()
        {
            string srcFile = GetTestFilePath();
            string destFile = GetTestFilePath();
            string destBackupPath = GetTestFilePath();
            File.Create(srcFile).Dispose();

            var destContents = new byte[] { 1, 3, 4, 8 };
            File.WriteAllBytes(destFile, destContents);

            FileInfo destInfo = new FileInfo(destFile);
            FileInfo newInfo = Replace(srcFile, destFile, destBackupPath);

            Assert.False(File.Exists(srcFile));
            Assert.NotNull(newInfo);
            Assert.Equal(destInfo.FullName, newInfo.FullName);
            Assert.Equal(destContents, File.ReadAllBytes(destBackupPath));
        }

        private FileInfo Replace(string source, string dest, string destBackup)
        {
            return new FileInfo(source).Replace(dest, destBackup);
        }
    }
}
