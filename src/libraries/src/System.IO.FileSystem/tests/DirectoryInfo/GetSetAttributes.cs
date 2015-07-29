// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace System.IO.FileSystem.Tests
{
    public class DirectoryInfo_GetSetAttributes : FileSystemTest
    {
        #region Utilities

        protected virtual FileAttributes Get(string path)
        {
            var info = new DirectoryInfo(path);
            return info.Attributes;
        }

        protected virtual void Set(string path, FileAttributes attributes)
        {
            var info = new DirectoryInfo(path);
            info.Attributes = attributes;
        }

        #endregion

        [Fact]
        public void NullParameters()
        {
            Assert.Throws<ArgumentNullException>(() => Get(null));
            Assert.Throws<ArgumentNullException>(() => Set(null, FileAttributes.Normal));
        }

        [Fact]
        public void InvalidParameters()
        {
            Assert.Throws<ArgumentException>(() => Get(string.Empty));
            Assert.Throws<ArgumentException>(() => Set(string.Empty, FileAttributes.Normal));
        }

        [Fact]
        public void NonExistentFile()
        {
            Assert.Throws<FileNotFoundException>(() => Set(GetTestFilePath(), FileAttributes.Normal));
            Assert.Throws<FileNotFoundException>(() => Set(IOServices.AddTrailingSlashIfNeeded(GetTestFilePath()), FileAttributes.Normal));
            Assert.Throws<FileNotFoundException>(() => Set(IOServices.RemoveTrailingSlash(GetTestFilePath()), FileAttributes.Normal));
        }

        [Theory]
        [InlineData(FileAttributes.ReadOnly)]
        [PlatformSpecific(PlatformID.AnyUnix)]
        public void UnixAttributeSetting(FileAttributes attr)
        {
            var test = new DirectoryInfo(GetTestFilePath());
            test.Create();
            Set(test.FullName, attr);
            Assert.Equal(attr | FileAttributes.Directory, Get(test.FullName));
            Set(test.FullName, 0);
        }

        [Theory]
        [InlineData(FileAttributes.ReadOnly)]
        [InlineData(FileAttributes.Hidden)]
        [InlineData(FileAttributes.System)]
        [InlineData(FileAttributes.Archive)]
        [InlineData(FileAttributes.ReadOnly | FileAttributes.Hidden)]
        [PlatformSpecific(PlatformID.Windows)]
        public void WindowsAttributeSetting(FileAttributes attr)
        {
            var test = new DirectoryInfo(GetTestFilePath());
            test.Create();
            Set(test.FullName, attr);
            Assert.Equal(attr | FileAttributes.Directory, Get(test.FullName));
            Set(test.FullName, 0);
        }

        [Theory]
        [InlineData(FileAttributes.Normal)]
        [InlineData(FileAttributes.Temporary)]
        [InlineData(FileAttributes.Encrypted)]
        [InlineData(FileAttributes.SparseFile)]
        [InlineData(FileAttributes.ReparsePoint)]
        [InlineData(FileAttributes.Compressed)]
        [PlatformSpecific(PlatformID.AnyUnix)]
        public void UnixInvalidAttributes(FileAttributes attr)
        {
            var path = GetTestFilePath();
            File.Create(path).Dispose();
            Set(path, attr);
            Assert.Equal(FileAttributes.Normal, Get(path));
        }

        [Theory]
        [InlineData(FileAttributes.Normal)]
        [InlineData(FileAttributes.Encrypted)]
        [InlineData(FileAttributes.SparseFile)]
        [InlineData(FileAttributes.ReparsePoint)]
        [InlineData(FileAttributes.Compressed)]
        [PlatformSpecific(PlatformID.Windows)]
        public void WindowsInvalidAttributes(FileAttributes attr)
        {
            var path = GetTestFilePath();
            File.Create(path).Dispose();
            Set(path, attr);
            Assert.Equal(FileAttributes.Normal, Get(path));
        }

        [Theory]
        [InlineData(~FileAttributes.ReadOnly)]
        [InlineData(-1)]
        [PlatformSpecific(PlatformID.AnyUnix)]
        public void UnixInvalidAttributes_ThrowArgumentException(FileAttributes attr)
        {
            var test = new DirectoryInfo(GetTestFilePath());
            test.Create();
            Assert.Throws<ArgumentException>(() => Set(test.FullName, attr));
        }

        [Theory]
        [InlineData(FileAttributes.Temporary)]
        [InlineData(~FileAttributes.ReadOnly)]
        [InlineData(-1)]
        [PlatformSpecific(PlatformID.Windows)]
        public void WindowsInvalidAttributes_ThrowArgumentException(FileAttributes attr)
        {
            var test = new DirectoryInfo(GetTestFilePath());
            test.Create();
            Assert.Throws<ArgumentException>(() => Set(test.FullName, attr));
        }
    }
}