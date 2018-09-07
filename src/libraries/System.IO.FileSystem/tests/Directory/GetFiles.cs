// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
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
            DirectoryInfo containingFolder = Directory.CreateDirectory(GetTestFilePath());

            // Test a symlink to a file that does and then doesn't exist
            FileInfo targetFile = new FileInfo(GetTestFilePath());
            targetFile.Create().Dispose();

            string linkPath = Path.Combine(containingFolder.FullName, Path.GetRandomFileName());
            Assert.True(MountHelper.CreateSymbolicLink(linkPath, targetFile.FullName, isDirectory: false));

            Assert.True(File.Exists(linkPath));
            Assert.Equal(1, GetEntries(containingFolder.FullName).Count());

            targetFile.Delete();

            // The symlink still exists even though the target file is gone.
            Assert.Equal(1, GetEntries(containingFolder.FullName).Count());

            // The symlink is gone
            File.Delete(linkPath);
            Assert.Equal(0, GetEntries(containingFolder.FullName).Count());
        }

        [ConditionalFact(nameof(AreAllLongPathsAvailable))]
        [ActiveIssue(32167, TargetFrameworkMonikers.NetFramework)]
        public void EnumerateFilesOverLegacyMaxPath()
        {
            // We want to test that directories under the legacy MAX_PATH (260 characters, including the null) can iterate files
            // even if the full path is over 260.

            string directory = IOServices.GetPath(GetTestFilePath(), 250);
            Assert.Equal(250, directory.Length);
            Assert.True(Directory.CreateDirectory(directory).Exists);

            for (int i = 0; i < 6; i++)
            {
                string testFile = Path.Combine(directory, new string((char)('0' + i), i + 7));
                File.Create(testFile).Dispose();
            }

            string[] files = GetEntries(directory);
            Assert.Equal(6, files.Length);
        }

        [ConditionalFact(nameof(AreAllLongPathsAvailable))]
        [ActiveIssue(32167, TargetFrameworkMonikers.NetFramework)]
        public void EnumerateFilesDirectoryOverLegacyMaxPath()
        {
            // Check enumerating when the entire path is over MAX_PATH

            string directory = IOServices.GetPath(GetTestFilePath(), 270);
            Assert.Equal(270, directory.Length);
            Assert.True(Directory.CreateDirectory(directory).Exists);

            for (int i = 0; i < 6; i++)
            {
                string testFile = Path.Combine(directory, new string((char)('0' + i), i + 7));
                File.Create(testFile).Dispose();
            }

            string[] files = GetEntries(directory);
            Assert.Equal(6, files.Length);
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
        public virtual bool IsDirectoryInfo => false;

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

        [Theory, MemberData(nameof(TrailingSeparators))]
        public void DirectoryWithTrailingSeparators(string trailing)
        {
            // When getting strings back we should retain the root path as specified for Directory.
            // DirectoryInfo returns the normalized full path in all cases.

            // Add the trailing separator up front for Directory as we want to validate against
            // the path _with_ the separator on it. Creation doesn't care about trailing, and
            // Path.Combine doesn't change the existing separators, it just adds the canonical
            // separator if needed.
            string root = GetTestFilePath() + (IsDirectoryInfo ? "" : trailing);
            string rootFile = Path.Combine(root, GetTestFileName());
            string subDirectory = Path.Combine(root, GetTestFileName());
            string nestedFile = Path.Combine(subDirectory, GetTestFileName());

            Directory.CreateDirectory(subDirectory);
            File.Create(rootFile).Dispose();
            File.Create(nestedFile).Dispose();

            // Add the trailing separator if we haven't (for DI) so we can validate that we
            // either retain (D) or don't retain (DI) the separators as we specified them.
            // Note that some of the cases actually match canonical (one standard separator)
            // so they never change.
            string[] files = GetEntries(root + (IsDirectoryInfo ? trailing : ""), "*", SearchOption.AllDirectories);
            FSAssert.EqualWhenOrdered(new string[] { rootFile, nestedFile }, files);
        }
    }
}
