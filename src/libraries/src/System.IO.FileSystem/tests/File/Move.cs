// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.IO.Tests
{
    public class File_Move : FileSystemTest
    {
        #region Utilities

        public virtual void Move(string sourceFile, string destFile)
        {
            File.Move(sourceFile, destFile);
        }

        #endregion

        #region UniversalTests

        [Fact]
        public void NullPath()
        {
            Assert.Throws<ArgumentNullException>(() => Move(null, "."));
            Assert.Throws<ArgumentNullException>(() => Move(".", null));
        }

        [Fact]
        public void EmptyPath()
        {
            Assert.Throws<ArgumentException>(() => Move(string.Empty, "."));
            Assert.Throws<ArgumentException>(() => Move(".", string.Empty));
        }

        [Fact]
        public virtual void NonExistentPath()
        {
            FileInfo testFile = new FileInfo(GetTestFilePath());
            testFile.Create().Dispose();
            Assert.Throws<FileNotFoundException>(() => Move(GetTestFilePath(), testFile.FullName));
            Assert.Throws<DirectoryNotFoundException>(() => Move(testFile.FullName, Path.Combine(TestDirectory, GetTestFileName(), GetTestFileName())));
            Assert.Throws<FileNotFoundException>(() => Move(Path.Combine(TestDirectory, GetTestFileName(), GetTestFileName()), testFile.FullName));
        }

        [Theory, MemberData(nameof(PathsWithInvalidCharacters))]
        public void PathWithIllegalCharacters(string invalidPath)
        {
            FileInfo testFile = new FileInfo(GetTestFilePath());
            testFile.Create().Dispose();

            // Under legacy normalization we kick \\?\ paths back as invalid with ArgumentException
            // New style we don't prevalidate \\?\ at all
            if (invalidPath.Contains(@"\\?\") && !PathFeatures.IsUsingLegacyPathNormalization())
                Assert.Throws<IOException>(() => Move(testFile.FullName, invalidPath));
            else
                Assert.Throws<ArgumentException>(() => Move(testFile.FullName, invalidPath));
        }

        [Fact]
        public void BasicMove()
        {
            FileInfo testFileSource = new FileInfo(GetTestFilePath());
            testFileSource.Create().Dispose();
            string testFileDest = GetTestFilePath();

            Move(testFileSource.FullName, testFileDest);
            Assert.True(File.Exists(testFileDest));
            Assert.False(File.Exists(testFileSource.FullName));
        }

        [Fact]
        public void MoveNonEmptyFile()
        {
            FileInfo testFileSource = new FileInfo(GetTestFilePath());
            using (var stream = testFileSource.Create())
            {
                var writer = new StreamWriter(stream);
                writer.Write("testing\nwrite\n");
                writer.Flush();
            }
            string testFileDest = GetTestFilePath();

            Move(testFileSource.FullName, testFileDest);
            Assert.True(File.Exists(testFileDest));
            Assert.False(File.Exists(testFileSource.FullName));
            Assert.Equal("testing\nwrite\n", File.ReadAllText(testFileDest));
        }

        [Fact]
        public void MoveOntoDirectory()
        {
            FileInfo testFile = new FileInfo(GetTestFilePath());
            testFile.Create().Dispose();
            Assert.Throws<IOException>(() => Move(testFile.FullName, TestDirectory));
        }

        [Fact]
        public void MoveOntoExistingFile()
        {
            FileInfo testFileSource = new FileInfo(GetTestFilePath());
            testFileSource.Create().Dispose();
            FileInfo testFileDest = new FileInfo(GetTestFilePath());
            testFileDest.Create().Dispose();
            Assert.Throws<IOException>(() => Move(testFileSource.FullName, testFileDest.FullName));
            Assert.True(File.Exists(testFileSource.FullName));
            Assert.True(File.Exists(testFileDest.FullName));
        }

        [Fact]
        public void MoveIntoParentDirectory()
        {
            string testDir = GetTestFilePath();
            Directory.CreateDirectory(testDir);
            FileInfo testFileSource = new FileInfo(Path.Combine(testDir, GetTestFileName()));
            testFileSource.Create().Dispose();
            FileInfo testFileDest = new FileInfo(Path.Combine(testDir, "..", GetTestFileName()));

            Move(testFileSource.FullName, testFileDest.FullName);
            Assert.True(File.Exists(testFileDest.FullName));
        }

        [Fact]
        public void MoveToSameName()
        {
            string testDir = GetTestFilePath();
            Directory.CreateDirectory(testDir);

            FileInfo testFileSource = new FileInfo(Path.Combine(testDir, GetTestFileName()));
            testFileSource.Create().Dispose();

            Move(testFileSource.FullName, testFileSource.FullName);
            Assert.True(File.Exists(testFileSource.FullName));
        }

        [Fact]
        public void MoveToSameNameDifferentCasing()
        {
            string testDir = GetTestFilePath();
            Directory.CreateDirectory(testDir);

            FileInfo testFileSource = new FileInfo(Path.Combine(testDir, Path.GetRandomFileName().ToLowerInvariant()));
            testFileSource.Create().Dispose();

            FileInfo testFileDest = new FileInfo(Path.Combine(testFileSource.DirectoryName, testFileSource.Name.ToUpperInvariant()));

            Move(testFileSource.FullName, testFileDest.FullName);
            Assert.True(File.Exists(testFileDest.FullName));
        }

        [Fact]
        public void MultipleMoves()
        {
            FileInfo testFileSource = new FileInfo(GetTestFilePath());
            testFileSource.Create().Dispose();
            string testFileDest1 = GetTestFilePath();
            string testFileDest2 = GetTestFilePath();

            Move(testFileSource.FullName, testFileDest1);
            Move(testFileDest1, testFileDest2);
            Assert.True(File.Exists(testFileDest2));
            Assert.False(File.Exists(testFileDest1));
            Assert.False(File.Exists(testFileSource.FullName));
        }

        [Fact]
        public void FileNameWithSignificantWhitespace()
        {
            string testFileSource = Path.Combine(TestDirectory, GetTestFileName());
            string testFileDest = Path.Combine(TestDirectory, "    e n   d");

            File.Create(testFileSource).Dispose();
            Move(testFileSource, testFileDest);
            Assert.True(File.Exists(testFileDest));
            Assert.False(File.Exists(testFileSource));
        }

        [ConditionalFact(nameof(AreAllLongPathsAvailable))]
        [PlatformSpecific(TestPlatforms.Windows)]  // Path longer than max path limit
        public void OverMaxPathWorks_Windows()
        {
            // Create a destination path longer than the traditional Windows limit of 256 characters,
            // but under the long path limitation (32K).

            string testFileSource = Path.Combine(TestDirectory, GetTestFileName());
            File.Create(testFileSource).Dispose();
            Assert.True(File.Exists(testFileSource), "test file should exist");

            Assert.All(IOInputs.GetPathsLongerThanMaxPath(GetTestFilePath()), (path) =>
            {
                string baseDestinationPath = Path.GetDirectoryName(path);
                if (!Directory.Exists(baseDestinationPath))
                {
                    Directory.CreateDirectory(baseDestinationPath);
                }
                Assert.True(Directory.Exists(baseDestinationPath), "base destination path should exist");

                Move(testFileSource, path);
                Assert.True(File.Exists(path), "moved test file should exist");
                File.Delete(testFileSource);
                Assert.False(File.Exists(testFileSource), "source test file should not exist");
                Move(path, testFileSource);
                Assert.True(File.Exists(testFileSource), "restored test file should exist");
            });
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public void LongPath()
        {
            string testFileSource = Path.Combine(TestDirectory, GetTestFileName());
            File.Create(testFileSource).Dispose();

            Assert.All(IOInputs.GetPathsLongerThanMaxLongPath(GetTestFilePath()), (path) =>
            {
                AssertExtensions.ThrowsAny<PathTooLongException, FileNotFoundException, DirectoryNotFoundException>(() => Move(testFileSource, path));
                File.Delete(testFileSource);
                AssertExtensions.ThrowsAny<PathTooLongException, FileNotFoundException, DirectoryNotFoundException>(() => Move(path, testFileSource));
            });
        }

        #endregion

        #region PlatformSpecific

        [Theory, MemberData(nameof(PathsWithInvalidColons))]
        [PlatformSpecific(TestPlatforms.Windows)]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Versions of netfx older than 4.6.2 throw an ArgumentException instead of NotSupportedException. Until all of our machines run netfx against the actual latest version, these will fail.")]
        public void WindowsPathWithIllegalColons(string invalidPath)
        {
            FileInfo testFile = new FileInfo(GetTestFilePath());
            testFile.Create().Dispose();
            Assert.Throws<NotSupportedException>(() => Move(testFile.FullName, invalidPath));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]  // Wild characters in path throw ArgumentException
        public void WindowsWildCharacterPath()
        {
            Assert.Throws<ArgumentException>(() => Move("*", GetTestFilePath()));
            Assert.Throws<ArgumentException>(() => Move(GetTestFilePath(), "*"));
            Assert.Throws<ArgumentException>(() => Move(GetTestFilePath(), "Test*t"));
            Assert.Throws<ArgumentException>(() => Move(GetTestFilePath(), "*Test"));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]  // Wild characters in path are allowed
        public void UnixWildCharacterPath()
        {
            string testDir = GetTestFilePath();
            string testFileSource = Path.Combine(testDir, "*");
            string testFileShouldntMove = Path.Combine(testDir, "*t");
            string testFileDest = Path.Combine(testDir, "*" + GetTestFileName());

            Directory.CreateDirectory(testDir);
            File.Create(testFileSource).Dispose();
            File.Create(testFileShouldntMove).Dispose();

            Move(testFileSource, testFileDest);
            Assert.True(File.Exists(testFileDest));
            Assert.False(File.Exists(testFileSource));
            Assert.True(File.Exists(testFileShouldntMove));

            Move(testFileDest, testFileSource);
            Assert.False(File.Exists(testFileDest));
            Assert.True(File.Exists(testFileSource));
            Assert.True(File.Exists(testFileShouldntMove));
        }

        [Theory,
            MemberData(nameof(WhiteSpace))]
        [PlatformSpecific(TestPlatforms.Windows)]  // Whitespace in path throws ArgumentException
        public void WindowsWhitespacePath(string whitespace)
        {
            FileInfo testFile = new FileInfo(GetTestFilePath());
            Assert.Throws<ArgumentException>(() => Move(testFile.FullName, whitespace));
        }

        [Theory,
            MemberData(nameof(WhiteSpace))]
        [PlatformSpecific(TestPlatforms.AnyUnix)]  // Whitespace in path allowed
        public void UnixWhitespacePath(string whitespace)
        {
            FileInfo testFileSource = new FileInfo(GetTestFilePath());
            testFileSource.Create().Dispose();

            Move(testFileSource.FullName, Path.Combine(TestDirectory, whitespace));
            Move(Path.Combine(TestDirectory, whitespace), testFileSource.FullName);

        }

        #endregion
    }
}
