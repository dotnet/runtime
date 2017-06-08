// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using Xunit;

namespace System.IO.Tests
{
    public class DirectoryInfo_GetSetAttributes : InfoGetSetAttributes<DirectoryInfo>
    {
        protected override FileAttributes GetAttributes(string path) => new DirectoryInfo(path).Attributes;
        protected override void SetAttributes(string path, FileAttributes attributes) => new DirectoryInfo(path).Attributes = attributes;
        protected override string CreateItem(string path = null, [CallerMemberName] string memberName = null, [CallerLineNumber] int lineNumber = 0)
            => Directory.CreateDirectory(path ?? GetTestFilePath()).FullName;
        protected override DirectoryInfo CreateInfo(string path) => new DirectoryInfo(path);
        protected override void DeleteItem(string path) => Directory.Delete(path);
        protected override bool IsDirectory => true;

        [Theory]
        [InlineData(FileAttributes.ReadOnly)]
        [PlatformSpecific(TestPlatforms.AnyUnix)]  // Tests Unix file attributes
        public void UnixAttributeSetting(FileAttributes attr)
        {
            string path = CreateItem();
            SetAttributes(path, attr);
            Assert.Equal(attr | FileAttributes.Directory, GetAttributes(path));
            SetAttributes(path, 0);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public void UnixDirectoryBeginningWithPeriodIsHidden(bool endsWithSlash)
        {
            string testDir = "." + GetTestFileName();
            Directory.CreateDirectory(Path.Combine(TestDirectory, testDir));
            Assert.True(0 != (new DirectoryInfo(Path.Combine(TestDirectory, testDir) + (endsWithSlash ? "/" : "")).Attributes & FileAttributes.Hidden));
        }

        [Theory]
        [InlineData(FileAttributes.ReadOnly)]
        [InlineData(FileAttributes.Hidden)]
        [InlineData(FileAttributes.System)]
        [InlineData(FileAttributes.Archive)]
        [InlineData(FileAttributes.ReadOnly | FileAttributes.Hidden)]
        [PlatformSpecific(TestPlatforms.Windows)]  // Tests Windows file attributes
        public void WindowsAttributeSetting(FileAttributes attr)
        {
            string path = CreateItem();
            SetAttributes(path, attr);
            Assert.Equal(attr | FileAttributes.Directory, GetAttributes(path));
            SetAttributes(path, 0);
        }

        [Theory]
        [InlineData(FileAttributes.Normal)]
        [InlineData(FileAttributes.Temporary)]
        [InlineData(FileAttributes.Encrypted)]
        [InlineData(FileAttributes.SparseFile)]
        [InlineData(FileAttributes.ReparsePoint)]
        [InlineData(FileAttributes.Compressed)]
        [PlatformSpecific(TestPlatforms.AnyUnix)]  // Unix-invalid file attributes that don't get set
        public void UnixInvalidAttributes(FileAttributes attr)
        {
            string path = CreateItem();
            SetAttributes(path, attr);
            Assert.Equal(FileAttributes.Directory, GetAttributes(path));
        }

        [Theory]
        [InlineData(FileAttributes.Normal)]
        [InlineData(FileAttributes.Encrypted)]
        [InlineData(FileAttributes.SparseFile)]
        [InlineData(FileAttributes.ReparsePoint)]
        [InlineData(FileAttributes.Compressed)]
        [PlatformSpecific(TestPlatforms.Windows)]  // Windows-invalid file attributes that don't get set
        public void WindowsInvalidAttributes(FileAttributes attr)
        {
            string path = CreateItem();
            SetAttributes(path, attr);
            Assert.Equal(FileAttributes.Directory, GetAttributes(path));
        }

        [Theory]
        [InlineData(~FileAttributes.ReadOnly)]
        [InlineData(-1)]
        [PlatformSpecific(TestPlatforms.AnyUnix)]  // Unix-invalid file attributes that throw
        public void UnixInvalidAttributes_ThrowArgumentException(FileAttributes attr)
        {
            string path = CreateItem();
            Assert.Throws<ArgumentException>(() => SetAttributes(path, attr));
        }

        [Theory]
        [InlineData(FileAttributes.Temporary)]
        [InlineData(~FileAttributes.ReadOnly)]
        [InlineData(-1)]
        [PlatformSpecific(TestPlatforms.Windows)]  // Windows-invalid file attributes that throw
        public void WindowsInvalidAttributes_ThrowArgumentException(FileAttributes attr)
        {
            string path = CreateItem();
            Assert.Throws<ArgumentException>(() => SetAttributes(path, attr));
        }
    }
}
