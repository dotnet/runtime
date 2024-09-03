// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        [PlatformSpecific(TestPlatforms.Windows)]  // Drive letter only
        public void DriveOnlyReturnsDrive_Windows()
        {
            string path = @"C:";
            var info = new DirectoryInfo(path);
            Assert.Equal(path, info.ToString());
        }

        [Fact]
        public void ParentToString_Core()
        {
            ParentToString(true);
        }

        private void ParentToString(bool compareFullName)
        {
            string filePath = GetTestFilePath();

            string dirPath = Path.GetDirectoryName(filePath);
            DirectoryInfo dirInfo = new DirectoryInfo(dirPath);

            string parentDirPath = Path.GetDirectoryName(dirPath);
            DirectoryInfo parentDirInfo = new DirectoryInfo(parentDirPath);

            string dirInfoParentString = compareFullName ? dirInfo.Parent.FullName : dirInfo.Parent.Name;
            string parentDirInfoString = compareFullName ? parentDirInfo.FullName : parentDirInfo.Name;

            Assert.Equal(dirInfo.Parent.ToString(), dirInfoParentString);
            Assert.Equal(dirInfo.Parent.ToString(), parentDirInfoString);
        }
    }
}
