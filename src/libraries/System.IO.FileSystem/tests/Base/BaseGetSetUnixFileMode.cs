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

        // All Set APIs always affect the target of the link.
        // The Get API returns the link value, except for the FileSafeHandle APIs which return the target value.
        protected virtual bool GetApiTargetsLink => true;

        // When false, the Get API returns (UnixFileMode)(-1) when the file doesn't exist.
        protected virtual bool GetThrowsWhenDoesntExist => false;

        // The FileSafeHandle APIs require the file to be readable to create the handle.
        protected virtual bool GetModeNeedsReadableFile => false;

        // When false, the Get API returns (UnixFileMode)(-1) instead of throwing.
        protected virtual bool GetModeThrowsPNSE => true;

        protected virtual string CreateTestItem(string path = null, [CallerMemberName] string memberName = null, [CallerLineNumber] int lineNumber = 0)
        {
            path = path ?? GetTestFilePath(null, memberName, lineNumber);
            File.Create(path).Dispose();
            return path;
        }

        [PlatformSpecific(TestPlatforms.AnyUnix)]
        [Theory]
        [MemberData(nameof(TestUnixFileModes))]
        public void SetThenGet(UnixFileMode mode)
        {
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
            if (GetModeNeedsReadableFile)
            {
                // Ensure the file remains readable.
                mode |= UnixFileMode.UserRead;
            }

            string path = CreateTestItem();
            UnixFileMode initialMode = GetMode(path);

            string linkPath = GetTestFilePath();
            File.CreateSymbolicLink(linkPath, path);

            // SetMode always changes the target.
            SetMode(linkPath, mode);

            Assert.Equal(mode, GetMode(path));

            if (GetApiTargetsLink)
            {
                Assert.Equal(AllAccess, GetMode(linkPath));
            }
            else
            {
                Assert.Equal(mode, GetMode(linkPath));
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
        [Fact]
        public void FileDoesntExist_SymbolicLink()
        {
            string path = GetTestFilePath();
            string linkPath = GetTestFilePath();
            File.CreateSymbolicLink(linkPath, path);

            if (GetModeNeedsReadableFile && !GetApiTargetsLink)
            {
                Assert.Throws<FileNotFoundException>(() => GetMode(linkPath));
            }
            else
            {
                Assert.Equal(AllAccess, GetMode(linkPath));
            }
            Assert.Throws<FileNotFoundException>(() => SetMode(linkPath, AllAccess));
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
