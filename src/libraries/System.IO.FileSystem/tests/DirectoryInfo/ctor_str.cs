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
        [PlatformSpecific(PlatformID.Windows)] // whitespace-only names are valid on Unix
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
        public void RootOath()
        {
            string dirName = Path.GetPathRoot(Directory.GetCurrentDirectory());
            DirectoryInfo dir = new DirectoryInfo(dirName);
            Assert.Equal(dir.FullName, dirName);
        }

        [Fact]
        [PlatformSpecific(PlatformID.Windows)] // drive labels
        public void CDriveCase()
        {
            DirectoryInfo dir = new DirectoryInfo("c:\\");
            DirectoryInfo dir2 = new DirectoryInfo("C:\\");
            Assert.NotEqual(dir.FullName, dir2.FullName);
        }

        [Fact]
        [PlatformSpecific(PlatformID.Windows)]
        public void DriveLetter_Windows()
        {
            // On Windows, DirectoryInfo will replace "<DriveLetter>:" with "."
            var driveLetter = new DirectoryInfo(Directory.GetCurrentDirectory()[0] + ":");
            var current = new DirectoryInfo(".");
            Assert.Equal(current.Name, driveLetter.Name);
            Assert.Equal(current.FullName, driveLetter.FullName);
        }

        [Fact]
        [PlatformSpecific(PlatformID.AnyUnix)]
        public void DriveLetter_Unix()
        {
            // On Unix, there's no special casing for drive letters, which are valid file names
            var driveLetter = new DirectoryInfo("C:");
            var current = new DirectoryInfo(".");
            Assert.Equal("C:", driveLetter.Name);
            Assert.Equal(Path.Combine(current.FullName, "C:"), driveLetter.FullName);
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
        [PlatformSpecific(PlatformID.Windows)] // UNC shares
        public void NetworkShare()
        {
            string dirName = new string(Path.DirectorySeparatorChar, 2) + Path.Combine("contoso", "amusement", "device");
            Assert.Equal(new DirectoryInfo(dirName).FullName, dirName);
        }

        [Fact]
        public void TrailingSlash()
        {
            DirectoryInfo dir1 = new DirectoryInfo(Directory.GetCurrentDirectory());
            DirectoryInfo dir2 = new DirectoryInfo(Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar);

            Assert.Equal(dir1.Name, dir2.Name);
            Assert.Equal(dir1.Parent.FullName, dir2.Parent.FullName);
            Assert.Equal(dir1.Root.FullName, dir2.Root.FullName);
        }
    }
}
