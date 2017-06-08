// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.IO.Tests
{
    // Tests that are valid for File and FileInfo
    public abstract class FileGetSetAttributes : BaseGetSetAttributes
    {
        [Theory]
        [InlineData(FileAttributes.ReadOnly)]
        [InlineData(FileAttributes.Normal)]
        [PlatformSpecific(TestPlatforms.AnyUnix)]  // Unix valid file attributes
        public void UnixAttributeSetting(FileAttributes attr)
        {
            string path = CreateItem();
            SetAttributes(path, attr);
            Assert.Equal(attr, GetAttributes(path));
            SetAttributes(path, 0);
        }

        [Theory]
        [InlineData(FileAttributes.ReadOnly)]
        [InlineData(FileAttributes.Hidden)]
        [InlineData(FileAttributes.System)]
        [InlineData(FileAttributes.Archive)]
        [InlineData(FileAttributes.Normal)]
        [InlineData(FileAttributes.Temporary)]
        [InlineData(FileAttributes.ReadOnly | FileAttributes.Hidden)]
        [PlatformSpecific(TestPlatforms.Windows)]  // Valid Windows file attribute
        public void WindowsAttributeSetting(FileAttributes attr)
        {
            string path = CreateItem();
            SetAttributes(path, attr);
            Assert.Equal(attr, GetAttributes(path));
            SetAttributes(path, 0);
        }

        [Theory]
        [InlineData(FileAttributes.Temporary)]
        [InlineData(FileAttributes.Encrypted)]
        [InlineData(FileAttributes.SparseFile)]
        [InlineData(FileAttributes.ReparsePoint)]
        [InlineData(FileAttributes.Compressed)]
        [PlatformSpecific(TestPlatforms.AnyUnix)]  // Unix invalid file attributes
        public void UnixInvalidAttributes(FileAttributes attr)
        {
            string path = CreateItem();
            SetAttributes(path, attr);
            Assert.Equal(FileAttributes.Normal, GetAttributes(path));
        }

        [Theory]
        [InlineData(FileAttributes.Normal)]
        [InlineData(FileAttributes.Encrypted)]
        [InlineData(FileAttributes.SparseFile)]
        [InlineData(FileAttributes.ReparsePoint)]
        [InlineData(FileAttributes.Compressed)]
        [PlatformSpecific(TestPlatforms.Windows)]  // Invalid Windows file attributes 
        public void WindowsInvalidAttributes(FileAttributes attr)
        {
            string path = CreateItem();
            SetAttributes(path, attr);
            Assert.Equal(FileAttributes.Normal, GetAttributes(path));
        }
    }
}
