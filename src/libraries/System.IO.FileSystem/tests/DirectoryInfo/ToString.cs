// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotInAppContainer))] // Can't read root in appcontainer
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, ".NET Framework returns '.' when calling DirectoryInfo.ToString() on a drive-only path")]
        [PlatformSpecific(TestPlatforms.Windows)]  // Drive letter only
        public void DriveOnlyReturnsDrive_Windows()
        {
            string path = @"C:";
            var info = new DirectoryInfo(path);
            Assert.Equal(path, info.ToString());
        }

        [Fact]
        public void ParentToString()
        {
            string filePath = GetTestFilePath();

            string dirPath = Path.GetDirectoryName(filePath);
            DirectoryInfo dirInfo = new DirectoryInfo(dirPath);

            string parentDirPath = Path.GetDirectoryName(dirPath);
            DirectoryInfo parentDirInfo = new DirectoryInfo(parentDirPath);

            string dirInfoParentString = PlatformDetection.IsNetCore ? dirInfo.Parent.FullName : dirInfo.Parent.Name;
            string parentDirInfoString = PlatformDetection.IsNetCore ? parentDirInfo.FullName : parentDirInfo.Name;

            Assert.Equal(dirInfo.Parent.ToString(), dirInfoParentString);
            Assert.Equal(dirInfo.Parent.ToString(), parentDirInfoString);
        }
    }
}
