// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Configuration;
using System.IO;
using Xunit;

namespace System.ConfigurationTests
{
    public class UrlPathTests
    {
        [Fact]
        public void GetDirectoryOrRootName_Null()
        {
            string test = UrlPath.GetDirectoryOrRootName(null);
            Assert.Equal(null, null);
        }

        [Fact]
        public void GetDirectoryOrRootName_NotDirectoryOrRoot()
        {
            string test = UrlPath.GetDirectoryOrRootName("Hello");
            Assert.Equal("", test);
        }

        [Fact]
        public void GetDirectoryOrRootName_GettingDirectoryFromAFilePath()
        {
            string exePath = AppDomain.CurrentDomain.BaseDirectory;
            // Remove the trailing slash.  Different OS's use a different slash.
            // This is to make the test pass without worrying about adding a slash 
            // and which kind of slash.
            string exePathWithoutTrailingSlash = exePath.Substring(0, exePath.Length - 1);
            string pathToNonexistentFile = exePath + "TestFileForUrlPathTests.txt";

            string test = UrlPath.GetDirectoryOrRootName(pathToNonexistentFile);
            Assert.Equal(exePathWithoutTrailingSlash, test);
        }

        [Fact]
        public void GetDirectoryOrRootName_OfRoot()
        {
            string exePath = AppDomain.CurrentDomain.BaseDirectory;
            string root = Path.GetPathRoot(exePath);
            string test = UrlPath.GetDirectoryOrRootName(root);

            Assert.Equal(root, test);
        }

        [Fact]
        public void IsEqualOrSubDirectory_NullDir()
        {
            bool test = UrlPath.IsEqualOrSubdirectory(null, "Hello");
            Assert.Equal(true, test);
        }

        [Fact]
        public void IsEqualOrSubDirectory_NullSubDir()
        {
            bool test = UrlPath.IsEqualOrSubdirectory("Hello", null);
            Assert.Equal(false, test);
        }

        [Fact]
        public void IsEqualOrSubDirectory_NullDirAndSubDir()
        {
            bool test = UrlPath.IsEqualOrSubdirectory(null, null);
            Assert.True(test);
        }

        [Fact]
        public void IsEqualOrSubDirectory_EmptyDir()
        {
            bool test = UrlPath.IsEqualOrSubdirectory("", null);
            Assert.True(test);
        }

        [Fact]
        public void IsEqualOrSubDirectory_NotEmptyDir_EmptySubDir()
        {
            bool test = UrlPath.IsEqualOrSubdirectory("Hello", "");
            Assert.False(test);
        }

        [Fact]
        public void IsEqualOrSubDirectory_SubDirAndDirAreReversed_NoTrailingBackslash()
        {
            bool test = UrlPath.IsEqualOrSubdirectory("C:\\Directory\\SubDirectory", "C:\\Directory");
            Assert.False(test);
        }

        [Fact]
        public void IsEqualOrSubDirectory_SubDirIsASubDirOfDir_NoTrailingBackslash()
        {
            bool test = UrlPath.IsEqualOrSubdirectory("C:\\Directory", "C:\\Directory\\SubDirectory");
            Assert.True(test);
        }

        [Fact]
        public void IsEqualOrSubDirectory_True_TrailingBackslashOnBoth()
        {
            bool test = UrlPath.IsEqualOrSubdirectory("C:\\Directory\\", "C:\\Directory\\SubDirectory");
            Assert.True(test);
        }

        [Fact]
        public void IsEqualOrSubDirectory_True_DirBackslash()
        {
            bool test = UrlPath.IsEqualOrSubdirectory("C:\\Directory\\", "C:\\Directory\\SubDirectory");
            Assert.True(test);
        }

        [Fact]
        public void IsEqualOrSubDirectory_True_SubDirBackslash()
        {
            bool test = UrlPath.IsEqualOrSubdirectory("C:\\Directory", "C:\\Directory\\SubDirectory\\");
            Assert.True(test);
        }

        [Fact]
        public void IsEqualOrSubDirectory_Equal_NoBackslashes()
        {
            bool test = UrlPath.IsEqualOrSubdirectory("C:\\Directory", "C:\\Directory");
            Assert.True(test);
        }

        [Fact]
        public void IsEqualOrSubDirectory_Equal_BothBackslashes()
        {
            bool test = UrlPath.IsEqualOrSubdirectory("C:\\Directory\\", "C:\\Directory\\");
            Assert.True(test);
        }

        [Fact]
        public void IsEqualOrSubDirectory_Equal_FirstHasBackslash()
        {
            bool test = UrlPath.IsEqualOrSubdirectory("C:\\Directory\\", "C:\\Directory");
            Assert.True(test);
        }

        [Fact]
        public void IsEqualOrSubDirectory_Equal_SecondHasBackslash()
        {
            bool test = UrlPath.IsEqualOrSubdirectory("C:\\Directory", "C:\\Directory\\");
            Assert.True(test);
        }
    }
}