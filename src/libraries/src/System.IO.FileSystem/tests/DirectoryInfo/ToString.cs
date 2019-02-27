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
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)] // Different behavior on Desktop
        [PlatformSpecific(TestPlatforms.Windows)]  // Drive letter only
        public void DriveOnlyReturnsDrive_Windows()
        {
            string path = @"C:";
            var info = new DirectoryInfo(path);
            Assert.Equal(path, info.ToString());
        }
    }
}
