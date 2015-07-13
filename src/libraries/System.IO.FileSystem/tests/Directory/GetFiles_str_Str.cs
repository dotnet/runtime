// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace System.IO.FileSystem.Tests
{
    public class Directory_GetFiles_str_str : Directory_GetFileSystemEntries_str_str
    {
        #region Utilities

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

        #endregion

        #region UniversalTests

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
        [ActiveIssue(846, PlatformID.AnyUnix)]
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

        #endregion

    }
}