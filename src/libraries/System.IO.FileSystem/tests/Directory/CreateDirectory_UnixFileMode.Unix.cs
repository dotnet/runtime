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

        [Theory]
        [MemberData(nameof(TestUnixFileModes))]
        public void CreateWithUnixFileMode(UnixFileMode mode)
        {
            string path = GetRandomDirPath();
            DirectoryInfo di = Directory.CreateDirectory(path, mode);

            // under Linux the created directory gets mode (mode & ~umask & 01777)
            // under OSX, it seems to be (mode & ~umask & 01777).
            UnixFileMode expectedMode = mode & ~GetUmask() &
                                        (UnixFileMode)(PlatformDetection.IsBsdLike ? 0b111_111_111 : 0b1_111_111_111);
            Assert.Equal(expectedMode, di.UnixFileMode);
        }

        [Fact]
        public void CreateDoesntChangeExistingMode()
        {
            string path = GetRandomDirPath();
            DirectoryInfo di = Directory.CreateDirectory(path, AllAccess);
            UnixFileMode initialMode = di.UnixFileMode;

            DirectoryInfo di2 = Directory.CreateDirectory(path, UnixFileMode.UserRead);
            Assert.Equal(initialMode, di2.UnixFileMode);
        }
    }
}
