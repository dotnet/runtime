// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.IO.Tests
{
    public class Directory_GetDirectoryRoot : FileSystemTest
    {
        [Fact]
        public void NullPath_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => Directory.GetDirectoryRoot(null));
        }

        [Fact]
        public void GetRootOfRoot()
        {
            string root = Directory.GetDirectoryRoot(Path.GetPathRoot(TestDirectory));
            Assert.Equal(Path.GetPathRoot(TestDirectory), root);
        }

        [Fact]
        public void RelativeDirectory()
        {
            string root = Directory.GetDirectoryRoot(Path.DirectorySeparatorChar + "testDir");
            Assert.Equal(Path.GetPathRoot(Directory.GetCurrentDirectory()), root);
        }

        [Fact]
        public void NestedDirectories()
        {
            string root = Directory.GetDirectoryRoot(Path.Combine("a", "a", "a", "b") + Path.DirectorySeparatorChar);
            Assert.Equal(Path.GetPathRoot(Directory.GetCurrentDirectory()), root);
        }

        [Fact]
        public void DotPaths()
        {
            string root = Directory.GetDirectoryRoot(Path.Combine("Test1", ".", "test2", "..", "test3"));
            Assert.Equal(Path.GetPathRoot(Directory.GetCurrentDirectory()), root);
        }

        [Fact]
        public void WhitespacePaths()
        {
            string root = Directory.GetDirectoryRoot(Path.Combine("T es t1", "te s  t2", "t  est 3"));
            Assert.Equal(Path.GetPathRoot(Directory.GetCurrentDirectory()), root);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]  // UNC shares
        public void UNCShares()
        {
            string root = Directory.GetDirectoryRoot(new string(Path.DirectorySeparatorChar, 2) + Path.Combine("Test1", "test2", "test3"));
            Assert.Equal(new string(Path.DirectorySeparatorChar, 2) + Path.Combine("Test1", "test2"), root);
        }
    }
}
