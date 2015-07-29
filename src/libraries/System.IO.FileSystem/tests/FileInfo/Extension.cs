// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace System.IO.FileSystem.Tests
{
    public class FileInfo_Extension : FileSystemTest
    {
        [Theory]
        [InlineData("filename", ".end")]
        [InlineData("filename", "")]
        [InlineData("foo.bar.fkl;fkds92-509450-4359.$#%()#%().%#(%)_#(%_)", ".cool")]
        [InlineData("filename", ".$#@$_)+_)!@@!!@##&_$)#_")]
        [InlineData("filename", ".$#@$_)+_)!@@!!@##&_$)#_")]
        public void ValidExtensions(string fileName, string extension)
        {
            string path = Path.Combine(TestDirectory, fileName + extension);
            FileInfo file = new FileInfo(path);
            Assert.Equal(extension, file.Extension);
        }

        [Theory]
        [InlineData(".")]
        [InlineData("............")]
        [PlatformSpecific(PlatformID.Windows)]
        public void WindowsInvalidExtensionsAreRemoved(string extension)
        {
            string testFile = GetTestFilePath();
            FileInfo testInfo = new FileInfo(testFile + extension);
            Assert.Equal(string.Empty, testInfo.Extension);
        }

        [Theory]
        [InlineData(".s", ".")]
        [InlineData(".s", ".s....")]
        [PlatformSpecific(PlatformID.Windows)]
        public void WindowsCurtailTrailingDots(string extension, string trailing)
        {
            string testFile = GetTestFilePath();
            FileInfo testInfo = new FileInfo(testFile + extension + trailing);
            Assert.Equal(extension, testInfo.Extension);
        }


        [Theory]
        [InlineData(".s", ".")]
        [InlineData(".s.s....", ".ls")]
        [PlatformSpecific(PlatformID.AnyUnix)]
        public void UnixLastDotIsExtension(string extension, string trailing)
        {
            string testFile = GetTestFilePath();
            FileInfo testInfo = new FileInfo(testFile + extension + trailing);
            Assert.Equal(trailing, testInfo.Extension);
        }
    }
}
