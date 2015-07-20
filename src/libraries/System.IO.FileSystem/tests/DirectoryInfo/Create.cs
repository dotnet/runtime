// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace System.IO.FileSystem.Tests
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

        #endregion

        [Fact]
        [PlatformSpecific(PlatformID.Windows)] // UNC shares for constructor
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
        [PlatformSpecific(PlatformID.Windows)]
        public void WindowsInvalidExtensionsAreRemoved(string extension)
        {
            string testDir = GetTestFilePath();
            DirectoryInfo testInfo = new DirectoryInfo(testDir + extension);
            Assert.Equal(string.Empty, testInfo.Extension);
        }

        [Theory]
        [InlineData(".s", ".")]
        [InlineData(".s", ".s....")]
        [PlatformSpecific(PlatformID.Windows)]
        public void WindowsCurtailTrailingDots(string extension, string trailing)
        {
            string testDir = GetTestFilePath();
            DirectoryInfo testInfo = new DirectoryInfo(testDir + extension + trailing);
            Assert.Equal(extension, testInfo.Extension);
        }


        [Theory]
        [InlineData(".s", ".")]
        [InlineData(".s.s....", ".ls")]
        [PlatformSpecific(PlatformID.AnyUnix)]
        public void UnixLastDotIsExtension(string extension, string trailing)
        {
            string testDir = GetTestFilePath();
            DirectoryInfo testInfo = new DirectoryInfo(testDir + extension + trailing);
            Assert.Equal(trailing, testInfo.Extension);
        }
    }
}
