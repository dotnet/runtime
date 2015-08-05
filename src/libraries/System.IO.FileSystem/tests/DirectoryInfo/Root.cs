// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace System.IO.FileSystem.Tests
{
    public class DirectoryInfo_Root : FileSystemTest
    {
        [Fact]
        public void RootOfRoot()
        {
            string root = Path.GetPathRoot(TestDirectory);
            Assert.Equal(root, new DirectoryInfo(root).Root.FullName);
        }

        [Fact]
        public void TrailingSlashes()
        {
            string root = Path.GetPathRoot(TestDirectory);
            var test = new DirectoryInfo(Path.Combine(TestDirectory, "a") + Path.DirectorySeparatorChar).Root;
            Assert.Equal(root, test.FullName);
        }

        [Fact]
        public void DotsInPathAreValid()
        {
            string root = Path.GetPathRoot(TestDirectory);
            var test = new DirectoryInfo(Path.Combine(TestDirectory, "Test", "..", ".", "Test")).Root;
            Assert.Equal(root, test.FullName);
        }

        [Fact]
        [PlatformSpecific(PlatformID.Windows)]
        public void UNCShares()
        {
            string root = Path.GetPathRoot(Directory.GetCurrentDirectory());
            string path = Path.DirectorySeparatorChar + Path.Combine("Machine", "Test");
            Assert.Equal(root, new DirectoryInfo(path).Root.FullName);

            string root2 = new string(Path.DirectorySeparatorChar, 2) + Path.Combine("Machine", "Test");
            Assert.Equal(root2, new DirectoryInfo(root2).Root.FullName);
        }
    }
}
