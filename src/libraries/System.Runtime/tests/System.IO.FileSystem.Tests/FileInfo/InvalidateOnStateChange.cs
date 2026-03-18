// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.IO.Tests
{
    public class FileInfo_InvalidateOnStateChange : FileSystemTest
    {
        [Fact]
        public void OpenWithFileMode_Create_InvalidatesExistsAndLength()
        {
            // Regression test for dotnet/runtime#117709
            string path = GetTestFilePath();
            FileInfo info = new FileInfo(path);
            Assert.False(info.Exists);

            using (FileStream fs = info.Open(FileMode.Create))
            {
                fs.Write(new byte[] { 1, 2, 3, 4, 5 });
            }

            Assert.True(info.Exists);
        }

        [Fact]
        public void OpenWithFileMode_CreateNew_InvalidatesExists()
        {
            string path = GetTestFilePath();
            FileInfo info = new FileInfo(path);
            Assert.False(info.Exists);

            using (FileStream fs = info.Open(FileMode.CreateNew))
            {
                fs.WriteByte(1);
            }

            Assert.True(info.Exists);
        }

        [Fact]
        public void OpenWithFileStreamOptions_InvalidatesExists()
        {
            string path = GetTestFilePath();
            FileInfo info = new FileInfo(path);
            Assert.False(info.Exists);

            using (FileStream fs = info.Open(new FileStreamOptions { Mode = FileMode.Create, Access = FileAccess.Write }))
            {
                fs.Write(new byte[] { 1, 2, 3 });
            }

            Assert.True(info.Exists);
        }

        [Fact]
        public void OpenWrite_InvalidatesExists()
        {
            string path = GetTestFilePath();
            FileInfo info = new FileInfo(path);
            Assert.False(info.Exists);

            using (FileStream fs = info.OpenWrite())
            {
                fs.Write(new byte[] { 1, 2, 3, 4, 5, 6, 7 });
            }

            Assert.True(info.Exists);
        }

        [Fact]
        public void OpenRead_InvalidatesState()
        {
            string path = GetTestFilePath();
            File.WriteAllBytes(path, new byte[] { 1, 2, 3 });
            FileInfo info = new FileInfo(path);

            // Force caching
            Assert.True(info.Exists);
            Assert.Equal(3, info.Length);

            // Modify file externally
            File.WriteAllBytes(path, new byte[] { 1, 2, 3, 4, 5 });

            using (FileStream fs = info.OpenRead())
            {
                // After OpenRead, state should be invalidated so Length reflects current size
                Assert.Equal(5, info.Length);
            }
        }

        [Fact]
        public void OpenText_InvalidatesState()
        {
            string path = GetTestFilePath();
            File.WriteAllText(path, "abc");
            FileInfo info = new FileInfo(path);

            // Force caching
            Assert.True(info.Exists);
            Assert.Equal(3, info.Length);

            // Modify file externally
            File.WriteAllText(path, "abcde");

            using (StreamReader sr = info.OpenText())
            {
                // After OpenText, state should be invalidated so Length reflects current size
                Assert.Equal(5, info.Length);
            }
        }

        [Fact]
        public void Replace_InvalidatesSourceFileInfo()
        {
            string srcPath = GetTestFilePath();
            string dstPath = GetTestFilePath();

            File.WriteAllBytes(srcPath, new byte[] { 10, 20, 30 });
            File.WriteAllBytes(dstPath, new byte[] { 1 });

            FileInfo srcInfo = new FileInfo(srcPath);

            // Force caching
            Assert.True(srcInfo.Exists);
            Assert.Equal(3, srcInfo.Length);

            srcInfo.Replace(dstPath, null);

            // After Replace, source file no longer exists at original path
            Assert.False(srcInfo.Exists);
        }

        [Fact]
        public void CreateText_InvalidatesExists()
        {
            string path = GetTestFilePath();
            FileInfo info = new FileInfo(path);
            Assert.False(info.Exists);

            using (StreamWriter sw = info.CreateText())
            {
                sw.Write("hello");
            }

            Assert.True(info.Exists);
        }

        [Fact]
        public void AppendText_InvalidatesExists()
        {
            string path = GetTestFilePath();
            FileInfo info = new FileInfo(path);
            Assert.False(info.Exists);

            using (StreamWriter sw = info.AppendText())
            {
                sw.Write("hello");
            }

            Assert.True(info.Exists);
        }

        [ConditionalFact(typeof(MountHelper), nameof(MountHelper.CanCreateHardLinks))]
        public void CreateAsHardLink_InvalidatesExists()
        {
            string targetPath = GetTestFilePath();
            File.WriteAllBytes(targetPath, new byte[] { 1, 2, 3 });

            string linkPath = GetTestFilePath();
            FileInfo linkInfo = new FileInfo(linkPath);
            Assert.False(linkInfo.Exists);

            linkInfo.CreateAsHardLink(targetPath);

            Assert.True(linkInfo.Exists);
        }

        [PlatformSpecific(TestPlatforms.Windows)]
        [Fact]
        public void Encrypt_InvalidatesState()
        {
            string path = GetTestFilePath();
            File.WriteAllText(path, "test content");

            FileInfo info = new FileInfo(path);

            // Force caching
            Assert.True(info.Exists);

            try
            {
                info.Encrypt();
            }
            catch (IOException)
            {
                // Encryption not supported on this Windows edition (e.g. Home)
                return;
            }

            // After Encrypt, attributes should be refreshed on next access
            Assert.True(info.Attributes.HasFlag(FileAttributes.Encrypted));
        }

        [PlatformSpecific(TestPlatforms.Windows)]
        [Fact]
        public void Decrypt_InvalidatesState()
        {
            string path = GetTestFilePath();
            File.WriteAllText(path, "test content");

            FileInfo info = new FileInfo(path);

            try
            {
                info.Encrypt();
            }
            catch (IOException)
            {
                // Encryption not supported on this Windows edition (e.g. Home)
                return;
            }

            // Force caching after encrypt
            Assert.True(info.Attributes.HasFlag(FileAttributes.Encrypted));

            info.Decrypt();
            // After Decrypt, Encrypted flag should be cleared
            Assert.False(info.Attributes.HasFlag(FileAttributes.Encrypted));
        }
    }
}
