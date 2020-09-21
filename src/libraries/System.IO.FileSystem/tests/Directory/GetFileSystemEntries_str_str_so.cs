// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.IO.Tests
{
    public class Directory_GetFileSystemEntries_str_str_so_alldirs : Directory_GetFileSystemEntries_str_str
    {
        #region Utilities

        public override string[] GetEntries(string dirName)
        {
            return Directory.GetFileSystemEntries(dirName, "*", SearchOption.AllDirectories);
        }

        public override string[] GetEntries(string dirName, string searchPattern)
        {
            return Directory.GetFileSystemEntries(dirName, searchPattern, SearchOption.AllDirectories);
        }

        public virtual string[] GetEntries(string dirName, string searchPattern, SearchOption option)
        {
            return Directory.GetFileSystemEntries(dirName, searchPattern, option);
        }

        #endregion

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]  // ? in search pattern returns results
        public override void WindowsSearchPatternQuestionMarks()
        {
            string testDir1Str = GetTestFileName();
            DirectoryInfo testDir = new DirectoryInfo(TestDirectory);
            testDir.CreateSubdirectory(testDir1Str);

            using (File.Create(Path.Combine(TestDirectory, testDir1Str, GetTestFileName())))
            using (File.Create(Path.Combine(TestDirectory, GetTestFileName())))
            {
                string[] results = GetEntries(TestDirectory, string.Format("{0}.???", new string('?', GetTestFileName().Length)));
                if (TestFiles && TestDirectories)
                    Assert.Equal(3, results.Length);
                else
                    Assert.Equal(1, results.Length);
            }
        }

        [Fact]
        public override void IgnoreSubDirectoryFiles()
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
    }

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

        [Fact]
        public void InvalidSearchOption()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => GetEntries(".", "*", (SearchOption)100));
            Assert.Throws<ArgumentOutOfRangeException>(() => GetEntries(".", "*", (SearchOption)(-1)));
        }
    }
}
