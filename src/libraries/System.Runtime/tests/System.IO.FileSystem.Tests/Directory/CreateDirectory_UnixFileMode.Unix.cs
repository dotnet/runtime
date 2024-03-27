// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Xunit;

namespace System.IO.Tests
{
    public class CreateDirectoryWithUnixFileMode : Directory_CreateDirectory
    {
        // Runs base class tests using CreateDirectory method that takes a UnixFileMode.
        public override DirectoryInfo Create(string path)
        {
            return Directory.CreateDirectory(path, AllAccess);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsStartingProcessesSupported))]
        [MemberData(nameof(TestUnixFileModes))]
        public void CreateWithUnixFileMode(UnixFileMode mode)
        {
            string path = GetRandomDirPath();
            DirectoryInfo dir = Directory.CreateDirectory(path, mode);

            // under Linux the created directory gets mode (mode & ~umask & 01777).
            // under OSX, it gets (mode & ~umask & 0777).
            UnixFileMode platformFilter = UnixFileMode.SetGroup | UnixFileMode.SetUser | (PlatformDetection.IsBsdLike ? UnixFileMode.StickyBit : UnixFileMode.None);
            UnixFileMode expectedMode = mode & ~GetUmask() & ~platformFilter;
            Assert.Equal(expectedMode, dir.UnixFileMode);
        }

        [Fact]
        public void CreateDoesntChangeExistingMode()
        {
            string path = GetRandomDirPath();
            DirectoryInfo dir = Directory.CreateDirectory(path, AllAccess);
            UnixFileMode initialMode = dir.UnixFileMode;

            DirectoryInfo sameDir = Directory.CreateDirectory(path, UnixFileMode.UserRead);
            Assert.Equal(initialMode, sameDir.UnixFileMode);
        }

        [Fact]
        public void MissingParentsHaveDefaultPermissions()
        {
            string parent = GetRandomDirPath();
            string child = Path.Combine(parent, "child");

            const UnixFileMode childMode = UnixFileMode.UserRead | UnixFileMode.UserExecute;
            DirectoryInfo childDir = Directory.CreateDirectory(child, childMode);

            Assert.Equal(childMode, childDir.UnixFileMode);

            UnixFileMode defaultPermissions = Directory.CreateDirectory(GetRandomDirPath()).UnixFileMode;
            Assert.Equal(defaultPermissions, File.GetUnixFileMode(parent));
        }

        [Theory]
        [InlineData((UnixFileMode)(1 << 12), false)]
        [InlineData((UnixFileMode)(1 << 12), true)]
        public void InvalidModeThrows(UnixFileMode mode, bool alreadyExists)
        {
            string path = GetRandomDirPath();

            if (alreadyExists)
            {
                Directory.CreateDirectory(path);
            }

            Assert.Throws<ArgumentException>(() => Directory.CreateDirectory(path, mode));
        }
    }
}
