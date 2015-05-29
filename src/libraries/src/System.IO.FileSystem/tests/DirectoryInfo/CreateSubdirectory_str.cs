// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace System.IO.FileSystem.Tests
{
    public partial class DirectoryInfo_CreateSubdirectory_str : FileSystemTest
    {
        [Fact]
        public void NullArgument()
        {
            Assert.Throws<ArgumentNullException>(() => new DirectoryInfo(".").CreateSubdirectory(null));
        }

        [Fact]
        public void EmptyStringArgument()
        {
            Assert.Throws<ArgumentException>(() => new DirectoryInfo(".").CreateSubdirectory(String.Empty));
        }

        [Fact]
        [ActiveIssue(1222)]
        public void Spaces()
        {
            string subDir = "         ";
            DirectoryInfo dir = new DirectoryInfo(TestDirectory).CreateSubdirectory(subDir);
            Assert.Equal(dir.FullName, Path.Combine(TestDirectory, subDir));
        }

        [Fact]
        [PlatformSpecific(PlatformID.Windows)] // tab valid filename char on Unix
        public void Tab()
        {
            Assert.Throws<ArgumentException>(() => new DirectoryInfo(TestDirectory).CreateSubdirectory("\t"));
        }

        [Fact]
        public void CurrentDirectory()
        {
            Assert.Equal(
                new DirectoryInfo(TestDirectory).CreateSubdirectory(".").FullName,
                TestDirectory);
        }

        [Fact]
        public void ParentDirectory()
        {
            Assert.Throws<ArgumentException>(() => new DirectoryInfo(TestDirectory).CreateSubdirectory(".."));
        }

        [Fact]
        public void PathTooLong()
        {
            StringBuilder sb = new StringBuilder();
            while (sb.Length + TestDirectory.Count() < IOInputs.MaxPath + 10)
                sb.Append("a");

            Assert.Throws<PathTooLongException>(() => new DirectoryInfo(TestDirectory).CreateSubdirectory(sb.ToString()));
        }

        [Fact]
        public void PathJustTooLong()
        {
            StringBuilder sb = new StringBuilder();
            while (sb.Length + TestDirectory.Count() < IOInputs.MaxDirectory)
                sb.Append("a");

            Assert.Throws<PathTooLongException>(() => new DirectoryInfo(TestDirectory).CreateSubdirectory(sb.ToString()));
        }

        [Fact]
        public void PathJustShortEnough()
        {
            StringBuilder sb = new StringBuilder();
            while (sb.Length + TestDirectory.Count() + 1 < 247)
                sb.Append("a");

            DirectoryInfo dir = new DirectoryInfo(TestDirectory).CreateSubdirectory(sb.ToString());

            Assert.Equal(dir.FullName, Path.Combine(TestDirectory, sb.ToString()));
        }

        [Fact]
        public void Symbols()
        {
            string subdirName = "!@#$%^&" + Path.GetRandomFileName();

            DirectoryInfo dir = new DirectoryInfo(TestDirectory).CreateSubdirectory(subdirName);

            Assert.Equal(dir.FullName, Path.Combine(TestDirectory, subdirName));
        }

        [Fact]
        public void RegularDirectory()
        {
            string subdirName = "TestDirectory";

            DirectoryInfo dir = new DirectoryInfo(TestDirectory).CreateSubdirectory(subdirName);

            Assert.Equal(dir.FullName, Path.Combine(TestDirectory, subdirName));
        }

        [Fact]
        public void MultipleNewDirectories()
        {
            string subdirName = Path.Combine("Test", "Test", "Test");

            DirectoryInfo dir = new DirectoryInfo(TestDirectory).CreateSubdirectory(subdirName);

            Assert.Equal(dir.FullName, Path.Combine(TestDirectory, subdirName));
        }

        [Fact]
        [PlatformSpecific(PlatformID.Windows)] // colon is a valid filename char on Unix
        public void Colon()
        {
            string[] subdirNames = { ":", ":t", ":test", "te:", "test:", "te:st" };

            foreach (string subdirName in subdirNames)
            {
                Assert.Throws<NotSupportedException>(() => new DirectoryInfo(TestDirectory).CreateSubdirectory(subdirName));
            }
        }

        [Fact]
        public void NetworkName()
        {
            Assert.Throws<ArgumentException>(() => new DirectoryInfo(TestDirectory).CreateSubdirectory(new string(Path.DirectorySeparatorChar, 2) + Path.Combine("contoso", "amusement", "device")));
        }

        [Fact]
        [ActiveIssue(1222)]
        public void DotDot()
        {
            Assert.Throws<ArgumentException>(() => new DirectoryInfo(Path.Combine(TestDirectory, "scratch")).CreateSubdirectory(Path.Combine("..", "scratch2")));
        }

        [Fact]
        public void LotsOfForwardSlashes()
        {
            Assert.Throws<ArgumentException>(() => new DirectoryInfo(".").CreateSubdirectory("////////"));
        }

    }
}
