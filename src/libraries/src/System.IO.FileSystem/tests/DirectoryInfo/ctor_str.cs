// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace System.IO.FileSystem.Tests
{
    public partial class DirectoryInfo_ctor_str : FileSystemTest
    {
        [Fact]
        public void NullParameter()
        {
            Assert.Throws<ArgumentNullException>(() => new DirectoryInfo(null));
        }

        [Fact]
        public void EmptyParameter()
        {
            Assert.Throws<ArgumentException>(() => new DirectoryInfo(""));
        }

        [Fact]
        public void Spaces()
        {
            Assert.Throws<ArgumentException>(() => new DirectoryInfo("      "));
        }

        [Fact]
        public void CurrentDirectory()
        {
            Assert.Equal(new DirectoryInfo(Directory.GetCurrentDirectory()).FullName, Directory.GetCurrentDirectory());
        }

        [Fact]
        public void CDrive()
        {
            string dirName = "c:\\";
            DirectoryInfo dir = new DirectoryInfo(dirName);
            Assert.Equal(dir.FullName, dirName);
        }

        [Fact]
        public void CDriveCase()
        {
            DirectoryInfo dir = new DirectoryInfo("c:\\");
            DirectoryInfo dir2 = new DirectoryInfo("C:\\");
            Assert.NotEqual(dir.FullName, dir2.FullName);
        }

        [Fact]
        [ActiveIssue(1222)]
        public void TrailingWhitespace()
        {
            new DirectoryInfo(Path.Combine(TestDirectory, "Testing\t\t\t\n")).Create();
        }

        [Fact]
        public void CurrentDirectoryWithDot()
        {
            Assert.Equal(new DirectoryInfo(".").FullName, Directory.GetCurrentDirectory());
        }

        [Fact]
        public void ParentDirectoryWithDotDot()
        {
            Assert.Equal(new DirectoryInfo("..").FullName, Directory.GetParent(Directory.GetCurrentDirectory()).FullName);
        }

        [Fact]
        public void NetworkShare()
        {
            string dirName = "\\\\contoso\\amusement\\device";
            Assert.Equal(new DirectoryInfo(dirName).FullName, dirName);
        }

        [Fact]
        public void TrailingSlash()
        {
            DirectoryInfo dir1 = new DirectoryInfo("C:\\hello");
            DirectoryInfo dir2 = new DirectoryInfo("C:\\hello\\");

            Assert.Equal(dir1.Name, dir2.Name);
            Assert.Equal(dir1.Parent.FullName, dir2.Parent.FullName);
            Assert.Equal(dir1.Root.FullName, dir2.Root.FullName);
        }
    }
}
