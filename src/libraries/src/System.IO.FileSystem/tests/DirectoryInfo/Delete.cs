// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace System.IO.FileSystem.Tests
{
    public partial class DirectoryInfo_Delete : FileSystemTest
    {
        [Fact]
        [ActiveIssue(1223)]
        public void RegularDirectory()
        {
            DirectoryInfo dir = Directory.CreateDirectory(Path.Combine(TestDirectory, Path.GetRandomFileName()));

            Assert.True(dir.Exists);

            dir.Delete();

            Assert.False(dir.Exists);
        }

        [Fact]
        public void NonEmptyDirectory()
        {
            string dirName = "NonEmptyDirectory";
            string subDirName = "ExtraDir";
            Directory.CreateDirectory(Path.Combine(TestDirectory, dirName, subDirName));

            Assert.Throws<IOException>(() => new DirectoryInfo(Path.Combine(TestDirectory, dirName)).Delete());

            new DirectoryInfo(Path.Combine(TestDirectory, dirName, subDirName)).Delete();
            new DirectoryInfo(Path.Combine(TestDirectory, dirName)).Delete();
            Assert.False(new DirectoryInfo(Path.Combine(TestDirectory, dirName)).Exists);
        }

        [Fact]
        public void DeleteRoot()
        {
            Assert.Throws<IOException>(() => new DirectoryInfo("C:\\").Delete());
        }
    }
}
