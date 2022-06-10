// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Xunit;

namespace System.IO.Tests
{
    public abstract class BaseGetSetUnixFileMode : FileSystemTest
    {
        protected abstract UnixFileMode GetMode(string path);
        protected abstract void SetMode(string path, UnixFileMode mode);

        // When true, the APIs follow the link for Get and Set.
        protected virtual bool ApiFollowsLink => false;

        // When false, the Get API returns (UnixFileMode)(-1) when the file doesn't exist.
        protected virtual bool GetThrowsWhenDoesntExist => false;

        // The FileSafeHandle APIs require a readable file to open the handle.
        protected virtual bool GetModeNeedsReadableFile => false;

        // When false, the Get API returns (UnixFileMode)(-1) when the platform is not supported.
        protected virtual bool GetModeThrowsPNSE => true;

        // Determines if the derived Test class is for directories or files.
        protected virtual bool IsDirectory => false;

        // Linux doesn't support setting UnixFileMode on links.
        private bool SetModeSupportsLink => PlatformDetection.IsBsdLike;

        // On OSX, directories created under /tmp have same group as /tmp.
        // Because that group is different from the test user's group, chmod
        // returns EPERM when trying to setgid on directories, and for files
        // chmod filters out the bit.
        // We skip the tests with setgid.
        private bool CanSetGroup => !PlatformDetection.IsBsdLike;

        private string CreateTestItem(string path = null, [CallerMemberName] string memberName = null, [CallerLineNumber] int lineNumber = 0)
        {
            path = path ?? GetTestFilePath(null, memberName, lineNumber);
            if (IsDirectory)
            {
                Directory.CreateDirectory(path);
            }
            else
            {
                File.Create(path).Dispose();
            }
            return path;
        }

        [PlatformSpecific(TestPlatforms.AnyUnix)]
        [Theory]
        [MemberData(nameof(TestUnixFileModes))]
        public void SetThenGet(UnixFileMode mode)
        {
            if (!CanSetGroup && (mode & UnixFileMode.SetGroup) != 0)
            {
                return; // Skip
            }
            if (GetModeNeedsReadableFile)
            {
                // Ensure the file remains readable.
                mode |= UnixFileMode.UserRead;
            }

            string path = CreateTestItem();

            SetMode(path, mode);

            Assert.Equal(mode, GetMode(path));
        }

        [PlatformSpecific(TestPlatforms.AnyUnix)]
        [Theory]
        [MemberData(nameof(TestUnixFileModes))]
        public void SetThenGet_SymbolicLink(UnixFileMode mode)
        {
            if (!CanSetGroup && (mode & UnixFileMode.SetGroup) != 0)
            {
                return; // Skip
            }
            if (GetModeNeedsReadableFile)
            {
                // Ensure the file remains readable.
                mode |= UnixFileMode.UserRead;
            }

            string path = CreateTestItem();

            string linkPath = GetTestFilePath();
            File.CreateSymbolicLink(linkPath, path);

            if (ApiFollowsLink)
            {
                SetMode(linkPath, mode);

                Assert.Equal(mode, GetMode(linkPath));
                Assert.Equal(mode, GetMode(path));
            }
            else
            {
                if (!SetModeSupportsLink)
                {
                    Assert.Throws<IOException>(() => SetMode(linkPath, mode));
                    return;
                }

                UnixFileMode initialMode = GetMode(path);

                SetMode(linkPath, mode);

                Assert.Equal(mode, GetMode(linkPath));
                Assert.Equal(initialMode, GetMode(path));
            }
        }

        [PlatformSpecific(TestPlatforms.AnyUnix)]
        [Fact]
        public void FileDoesntExist()
        {
            string path = GetTestFilePath();

            if (GetThrowsWhenDoesntExist)
            {
                Assert.Throws<FileNotFoundException>(() => GetMode(path));
            }
            else
            {
                Assert.Equal((UnixFileMode)(-1), GetMode(path));
            }
            Assert.Throws<FileNotFoundException>(() => SetMode(path, AllAccess));
        }

        [PlatformSpecific(TestPlatforms.AnyUnix)]
        [Theory]
        [InlineData(UnixFileMode.UserRead)]
        public void FileDoesntExist_SymbolicLink(UnixFileMode mode)
        {
            string path = GetTestFilePath();
            string linkPath = GetTestFilePath();
            File.CreateSymbolicLink(linkPath, path);

            if (ApiFollowsLink)
            {
                Assert.Throws<FileNotFoundException>(() => SetMode(linkPath, AllAccess));

                if (GetThrowsWhenDoesntExist)
                {
                    Assert.Throws<FileNotFoundException>(() => GetMode(path));
                }
                else
                {
                    Assert.Equal((UnixFileMode)(-1), GetMode(path));
                }
            }
            else
            {
                if (!SetModeSupportsLink)
                {
                    Assert.Throws<IOException>(() => SetMode(linkPath, AllAccess));
                    return;
                }

                SetMode(linkPath, mode);

                Assert.Equal(mode, GetMode(linkPath));
            }
        }

        [PlatformSpecific(TestPlatforms.AnyUnix)]
        [Fact]
        public void ParentDirDoesntExist()
        {
            string path = Path.Combine(GetTestFilePath(), "dir", "file");

            if (GetThrowsWhenDoesntExist)
            {
                Assert.Throws<DirectoryNotFoundException>(() => GetMode(path));
            }
            else
            {
                Assert.Equal((UnixFileMode)(-1), GetMode(path));
            }
            Assert.Throws<DirectoryNotFoundException>(() => SetMode(path, AllAccess));
        }

        [PlatformSpecific(TestPlatforms.AnyUnix)]
        [Fact]
        public void NullPath()
        {
            Assert.Throws<ArgumentNullException>(() => GetMode(null));
            Assert.Throws<ArgumentNullException>(() => SetMode(null, AllAccess));
        }

        [PlatformSpecific(TestPlatforms.AnyUnix)]
        [Fact]
        public void InvalidPath()
        {
            Assert.Throws<ArgumentException>(() => GetMode(string.Empty));
            Assert.Throws<ArgumentException>(() => SetMode(string.Empty, AllAccess));
        }

        [PlatformSpecific(TestPlatforms.AnyUnix)]
        [Theory]
        [InlineData((UnixFileMode)(1 << 12))]
        public void InvalidMode(UnixFileMode mode)
        {
            string path = CreateTestItem();

            Assert.Throws<ArgumentException>(() => SetMode(path, mode));
        }

        [PlatformSpecific(TestPlatforms.Windows)]
        [Fact]
        public void Unsupported()
        {
            string path = CreateTestItem();

            Assert.Throws<PlatformNotSupportedException>(() => SetMode(path, AllAccess));

            if (GetModeThrowsPNSE)
            {
                Assert.Throws<PlatformNotSupportedException>(() => GetMode(path));
            }
            else
            {
                Assert.Equal((UnixFileMode)(-1), GetMode(path));
            }
        }
    }
}
