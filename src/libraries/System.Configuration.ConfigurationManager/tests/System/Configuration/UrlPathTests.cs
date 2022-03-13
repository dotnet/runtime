// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
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
            Assert.Null(test);
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
            string exePathWithoutTrailingSlash = exePath.TrimEnd(Path.DirectorySeparatorChar);
            string pathToNonexistentFile = Path.Combine(exePath, "TestFileForUrlPathTests.txt");

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
            Assert.True(test);
        }

        [Fact]
        public void IsEqualOrSubDirectory_NullSubDir()
        {
            bool test = UrlPath.IsEqualOrSubdirectory("Hello", null);
            Assert.False(test);
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

        public static IEnumerable<object[]> UnixDirectories = new List<object[]>()
        {
            new object[] { "/dir/sub", "/dir", false },     // no slash
            new object[] { "/dir", "/dir/sub", true },      // no slash
            new object[] { "/dir/", "/dir/sub/", true },    // both slash
            new object[] { "/dir/", "/dir/sub", true },     // dir slash
            new object[] { "/dir", "/dir/sub/", true },     // subdir slash
            new object[] { "/dir", "/dir", true },          // no slashes
            new object[] { "/var/", "/var/", true },        // both slashes
            new object[] { "/var/", "/var", true },         // first has slash
            new object[] { "/var", "/var/", true },         // second has slash
        };

        [Theory]
        [MemberData(nameof(UnixDirectories))]
        public void IsEqualOrSubDirectory_UnixPath(string dir, string subdir, bool expected)
        {
            bool actual = UrlPath.IsEqualOrSubdirectory(dir, subdir);
            Assert.Equal(expected, actual);
        }
    }
}
