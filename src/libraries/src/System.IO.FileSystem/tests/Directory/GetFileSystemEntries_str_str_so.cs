// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace System.IO.FileSystem.Tests
{
    public class Directory_GetFileSystemEntries_str_str_so : Directory_GetFileSystemEntries_str_str
    {
        #region Utilities

        public override string[] GetEntries(string dirName)
        {
            return Directory.GetFileSystemEntries(dirName, "*", SearchOption.TopDirectoryOnly);
        }

        public override string[] GetEntries(string dirName, string searchPattern)
        {
            return Directory.GetFileSystemEntries(dirName, searchPattern, SearchOption.TopDirectoryOnly);
        }

        public virtual string[] GetEntries(string dirName, string searchPattern, SearchOption option)
        {
            return Directory.GetFileSystemEntries(dirName, searchPattern, option);
        }

        #endregion

        #region UniversalTests

        [Fact]
        public void IncludeSubDirectoryFiles()
        {
            string testDir = GetTestFileName();
            string testFile1 = Path.Combine(TestDirectory, GetTestFileName());
            string testFile2 = Path.Combine(TestDirectory, testDir, GetTestFileName());
            string testSubDir = Path.Combine(TestDirectory, testDir, GetTestFileName());
            Directory.CreateDirectory(Path.Combine(TestDirectory, testDir));
            Directory.CreateDirectory(testSubDir);
            using (File.Create(testFile1))
            using (File.Create(testFile2))
            {
                string[] results = GetEntries(TestDirectory, "*", SearchOption.AllDirectories);
                if (TestFiles)
                {
                    Assert.Contains(testFile1, results);
                    Assert.Contains(testFile2, results);
                }
                if (TestDirectories)
                {
                    Assert.Contains(testSubDir, results);
                    Assert.Contains(Path.Combine(TestDirectory, testDir), results);
                }
            }
        }

        [Fact]
        public void SearchPatternIncludeSubDirectories()
        {
            string testDir = GetTestFileName();
            string testFile1 = Path.Combine(TestDirectory, GetTestFileName());
            string testFile2 = Path.Combine(TestDirectory, testDir, GetTestFileName());
            string testSubDir = Path.Combine(TestDirectory, testDir, GetTestFileName());
            Directory.CreateDirectory(Path.Combine(TestDirectory, testDir));
            Directory.CreateDirectory(testSubDir);
            using (File.Create(testFile1))
            using (File.Create(testFile2))
            {
                string[] results = GetEntries(Directory.GetParent(TestDirectory).FullName, Path.Combine(Path.GetFileName(TestDirectory), "*"), SearchOption.AllDirectories);
                if (TestFiles)
                {
                    Assert.Contains(testFile1, results);
                    Assert.Contains(testFile2, results);
                }
                if (TestDirectories)
                {
                    Assert.Contains(testSubDir, results);
                    Assert.Contains(Path.Combine(TestDirectory, testDir), results);
                }
            }
        }

        [Fact]
        public void InvalidSearchOption()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => GetEntries(".", "*", (SearchOption)100));
            Assert.Throws<ArgumentOutOfRangeException>(() => GetEntries(".", "*", (SearchOption)(-1)));
        }

        #endregion
    }
}
