// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.IO.Tests
{
    public class DirectoryInfo_Create : Directory_CreateDirectory
    {
        #region Utilities

        public override DirectoryInfo Create(string path)
        {
            DirectoryInfo result = new DirectoryInfo(path);
            result.Create();
            return result;
        }

        public override bool IsDirectoryCreate => false;

        #endregion

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // UNC shares for constructor
        public void NetworkShare()
        {
            string dirName = new string(Path.DirectorySeparatorChar, 2) + Path.Combine("contoso", "amusement", "device");
            Assert.Equal(new DirectoryInfo(dirName).FullName, dirName);
        }

        [Theory]
        [InlineData("")]
        [InlineData(".ext")]
        [InlineData(".longlonglonglonglonglonglonglonglonglonglonglonglong")]
        [InlineData(".$#@$_)+_)!@@!!@##&_$)#_")]
        public void ValidExtensionsArePreserved(string extension)
        {
            string testDir = GetTestFilePath();
            DirectoryInfo testInfo = new DirectoryInfo(testDir + extension);
            Assert.Equal(extension, testInfo.Extension);
        }

        [Theory]
        [InlineData(".")]
        [InlineData("............")]
        [PlatformSpecific(TestPlatforms.Windows)]  // Windows-invalid extensions are removed
        public void WindowsInvalidExtensionsAreRemoved(string extension)
        {
            string testDir = GetTestFilePath();
            DirectoryInfo testInfo = new DirectoryInfo(testDir + extension);
            Assert.Equal(string.Empty, testInfo.Extension);
        }

        [Theory]
        [InlineData(".s", ".")]
        [InlineData(".s", ".s....")]
        [PlatformSpecific(TestPlatforms.Windows)]  // Trailing dots in extension are removed
        public void WindowsCurtailTrailingDots(string extension, string trailing)
        {
            string testDir = GetTestFilePath();
            DirectoryInfo testInfo = new DirectoryInfo(testDir + extension + trailing);
            Assert.Equal(extension, testInfo.Extension);
        }


        [Theory]
        [InlineData(".s", ".")]
        [InlineData(".s.s....", ".ls")]
        [PlatformSpecific(TestPlatforms.AnyUnix)]  // Last dot is extension
        public void UnixLastDotIsExtension(string extension, string trailing)
        {
            string testDir = GetTestFilePath();
            DirectoryInfo testInfo = new DirectoryInfo(testDir + extension + trailing);
            Assert.Equal(trailing, testInfo.Extension);
        }
    }
}
