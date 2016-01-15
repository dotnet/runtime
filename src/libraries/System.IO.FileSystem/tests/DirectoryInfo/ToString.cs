// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace System.IO.Tests
{
    public class DirectoryInfo_ToString : FileSystemTest
    {
        [Fact]
        public void ValidDirectory()
        {
            string path = GetTestFilePath();
            var info = new DirectoryInfo(path);
            Assert.Equal(path, info.ToString());
        }

        [Fact]
        public void RootOfCurrentDrive()
        {
            string path = Path.GetPathRoot(TestDirectory);
            var info = new DirectoryInfo(path);
            Assert.Equal(path, info.ToString());
        }

        [Theory,
            InlineData(@"."),
            InlineData(@".."),
            InlineData(@"foo"),
            InlineData(@"foo/bar"),
            ]
        public void KeepsOriginalPath(string path)
        {
            // ToString should return the passed in path
            var info = new DirectoryInfo(path);
            Assert.Equal(path, info.ToString());
        }

        [Fact]
        [PlatformSpecific(PlatformID.Windows)]
        public void DriveOnlyReturnsPeriod_Windows()
        {
            string path = @"C:";
            var info = new DirectoryInfo(path);
            Assert.Equal(".", info.ToString());
        }
    }
}
