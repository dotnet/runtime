// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Runtime.InteropServices;
using Xunit;

namespace System.IO.Tests
{
    public class Directory_GetFiles_str : Directory_GetFileSystemEntries_str
    {
        protected override bool TestFiles { get { return true; } }
        protected override bool TestDirectories { get { return false; } }

        public override string[] GetEntries(string path)
        {
            return Directory.GetFiles(path);
        }

        [ConditionalFact(nameof(CanCreateSymbolicLinks))]
        public void EnumerateWithSymLinkToFile()
        {
            using (var containingFolder = new TemporaryDirectory())
            {
                string linkPath;

                // Test a symlink to a file that does and then doesn't exist
                using (var targetFile = new TemporaryFile())
                {
                    linkPath = Path.Combine(containingFolder.Path, Path.GetRandomFileName());
                    Assert.True(MountHelper.CreateSymbolicLink(linkPath, targetFile.Path, isDirectory: false));

                    Assert.True(File.Exists(linkPath));
                    Assert.Equal(1, GetEntries(containingFolder.Path).Count());
                }

                // The symlink still exists even though the target file is gone.
                Assert.Equal(1, GetEntries(containingFolder.Path).Count());

                // The symlink is gone
                File.Delete(linkPath);
                Assert.Equal(0, GetEntries(containingFolder.Path).Count());
            }
        }
    }

    public class Directory_GetFiles_str_str : Directory_GetFileSystemEntries_str_str
    {
        protected override bool TestFiles { get { return true; } }
        protected override bool TestDirectories { get { return false; } }

        public override string[] GetEntries(string path)
        {
            return Directory.GetFiles(path, "*");
        }

        public override string[] GetEntries(string path, string searchPattern)
        {
            return Directory.GetFiles(path, searchPattern);
        }

        [Fact]
        public void SearchPatternSubset()
        {
            string testDir1Str = GetTestFileName();
            string testDir11Str = GetTestFileName();
            DirectoryInfo testDir = new DirectoryInfo(TestDirectory);
            DirectoryInfo testDir1 = testDir.CreateSubdirectory(testDir1Str);
            testDir1.CreateSubdirectory(testDir11Str);

            string testFile1 = GetTestFileName();
            string testFile2 = "ca1" + testFile1;
            testFile1 = "ca2" + testFile1;
            string testFile3 = GetTestFileName();
            string searchPattern = string.Format("???{0}", testFile1.Substring(3));

            using (File.Create(Path.Combine(TestDirectory, testDir1Str, testDir11Str, GetTestFileName())))
            using (File.Create(Path.Combine(TestDirectory, testDir1Str, testFile1)))
            using (File.Create(Path.Combine(TestDirectory, testDir1Str, testFile2)))
            using (File.Create(Path.Combine(TestDirectory, testDir1Str, testFile3)))
            {
                string[] results = GetEntries(Path.Combine(TestDirectory, testDir1Str), searchPattern);
                Assert.Equal(2, results.Length);
                Assert.Contains(Path.Combine(TestDirectory, testDir1Str, testFile1), results);
                Assert.Contains(Path.Combine(TestDirectory, testDir1Str, testFile2), results);

                //suffix only should return the empty array
                Assert.Empty(GetEntries(Path.Combine(TestDirectory, testDir1Str), testFile1.Substring(3)));
            }
        }

        [Fact]
        public void CharacterTests()
        {
            //bug #417100 - not sure if this hard coded approach is safe in all 9x platforms!!!

            int[] validGreaterThan128ButLessThans160 = { 129, 133, 141, 143, 144, 157 };
            for (int i = 0; i < validGreaterThan128ButLessThans160.Length; i++)
            {
                GetEntries(".", ((Char)validGreaterThan128ButLessThans160[i]).ToString());
            }

            for (int i = 160; i < 256; i++)
            {
                GetEntries(".", ((Char)i).ToString());
            }
        }
    }

    public class Directory_GetFiles_str_str_so : Directory_GetFileSystemEntries_str_str_so
    {
        protected override bool TestFiles { get { return true; } }
        protected override bool TestDirectories { get { return false; } }

        public override string[] GetEntries(string path)
        {
            return Directory.GetFiles(path, "*", SearchOption.TopDirectoryOnly);
        }

        public override string[] GetEntries(string path, string searchPattern)
        {
            return Directory.GetFiles(path, searchPattern, SearchOption.TopDirectoryOnly);
        }

        public override string[] GetEntries(string path, string searchPattern, SearchOption option)
        {
            return Directory.GetFiles(path, searchPattern, option);
        }
    }
}
