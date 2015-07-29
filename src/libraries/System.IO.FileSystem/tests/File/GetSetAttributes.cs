// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace System.IO.FileSystem.Tests
{
    public class File_GetSetAttributes : FileSystemTest
    {
        #region Utilities

        protected virtual FileAttributes Get(string path)
        {
            return File.GetAttributes(path);
        }

        protected virtual void Set(string path, FileAttributes attributes)
        {
            File.SetAttributes(path, attributes);
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
        }

        [Theory]
        [InlineData(FileAttributes.ReadOnly)]
        [InlineData(FileAttributes.Normal)]
        [PlatformSpecific(PlatformID.AnyUnix)]
        public void UnixAttributeSetting(FileAttributes attr)
        {
            var path = GetTestFilePath();
            File.Create(path).Dispose();
            Set(path, attr);
            Assert.Equal(attr, Get(path));
            Set(path, 0);
        }

        [Theory]
        [InlineData(FileAttributes.ReadOnly)]
        [InlineData(FileAttributes.Hidden)]
        [InlineData(FileAttributes.System)]
        [InlineData(FileAttributes.Archive)]
        [InlineData(FileAttributes.Normal)]
        [InlineData(FileAttributes.Temporary)]
        [InlineData(FileAttributes.ReadOnly | FileAttributes.Hidden)]
        [PlatformSpecific(PlatformID.Windows)]
        public void WindowsAttributeSetting(FileAttributes attr)
        {
            var path = GetTestFilePath();
            File.Create(path).Dispose();
            Set(path, attr);
            Assert.Equal(attr, Get(path));
            Set(path, 0);
        }

        [Theory]
        [InlineData(FileAttributes.SparseFile)]
        [InlineData(FileAttributes.Temporary)]
        [InlineData(FileAttributes.ReparsePoint)]
        [InlineData(FileAttributes.Compressed)]
        [InlineData(FileAttributes.Encrypted)]
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
    }
}