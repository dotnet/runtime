// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace System.IO.FileSystem.Tests
{
    public partial class DirectoryInfo_Create : FileSystemTest
    {
        [Fact]
        public void CreateCurrentDirectory()
        {
            DirectoryInfo dir = new DirectoryInfo(Path.Combine(TestDirectory, "."));
            dir.Create();
            Assert.Equal(dir.FullName, TestDirectory);
        }

        [Fact]
        public void CreateCurrentDirectoryWithRelativeTraversal()
        {
            DirectoryInfo dir = new DirectoryInfo(Path.Combine(TestDirectory, "abc", "xyz", "..", ".."));
            dir.Create();
            Assert.Equal(dir.FullName, TestDirectory);
        }

        [Fact]
        [PlatformSpecific(PlatformID.Windows)] // tabs are valid in file names on Unix
        public void MultipleTabCharacters()
        {
            Assert.Throws<ArgumentException>(() => new DirectoryInfo("\t\t\t\t"));
        }

        [Fact]
        public void ValidDirectoryName()
        {
            string dirName = Path.Combine(TestDirectory, Path.GetRandomFileName());
            DirectoryInfo dir = new DirectoryInfo(dirName);
            dir.Create();
            Assert.Equal(dir.Name, Path.GetFileName(dirName));
        }

        [Fact]
        public void DirectoryAlreadyExists()
        {
            DirectoryInfo dir = new DirectoryInfo(TestDirectory);
            dir.Create();
            Assert.Equal(dir.FullName, TestDirectory);
        }

        [Fact]
        public void PathTooLong()
        {
            StringBuilder sb = new StringBuilder(TestDirectory);
            while (sb.Length < IOInputs.MaxPath)
            {
                sb.Append("a");
            }

            Assert.Throws<PathTooLongException>(() => new DirectoryInfo(sb.ToString()).Create());
        }

        [Fact]
        public void PathJustTooLong()
        {
            StringBuilder sb = new StringBuilder(TestDirectory + Path.DirectorySeparatorChar);
            while (sb.Length < IOInputs.MaxDirectory + 1)
            {
                sb.Append("a");
            }

            Assert.Throws<PathTooLongException>(() => new DirectoryInfo(sb.ToString()).Create());
        }

        [Fact]
        public void PathJustShortEnough()
        {
            StringBuilder sb = new StringBuilder(TestDirectory + Path.DirectorySeparatorChar);
            while (sb.Length < 247)
            {
                sb.Append("a");
            }

            DirectoryInfo dir = new DirectoryInfo(sb.ToString());
            dir.Create();

            Assert.Equal(dir.FullName, sb.ToString());
        }

        [Fact]
        public void AllowedSymbols()
        {
            string dirName = Path.Combine(TestDirectory, Path.GetRandomFileName() + "!@#$%^&");
            DirectoryInfo dir = new DirectoryInfo(dirName);
            dir.Create();

            Assert.Equal(dir.FullName, dirName);
        }

        [Fact]
        public void CreateMultipleSubdirectories()
        {
            string dirName = Path.Combine(TestDirectory, "Test", "Test", "Test");
            DirectoryInfo dir = new DirectoryInfo(dirName);
            dir.Create();

            Assert.Equal(dir.FullName, dirName);
        }

        [Fact]
        [PlatformSpecific(PlatformID.Windows)] // colon valid filename char on Unix
        public void CreateColon()
        {
            Assert.Throws<ArgumentException>(() => new DirectoryInfo(":"));
        }

        [Fact]
        public void WithRelativeDirectoryInMiddle()
        {
            string dirName = Path.Combine(TestDirectory, Path.GetRandomFileName(), "..", "TestDir");
            DirectoryInfo dir = new DirectoryInfo(dirName);
            dir.Create();
            dir.Delete(true);
        }


    }
}
