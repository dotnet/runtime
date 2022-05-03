// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.IO.Tests
{
    public class Directory_Move : FileSystemTest
    {
        #region Utilities

        protected virtual void Move(string sourceDir, string destDir)
        {
            Directory.Move(sourceDir, destDir);
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
        public void NonExistentDirectory()
        {
            DirectoryInfo valid = Directory.CreateDirectory(GetTestFilePath());
            Assert.Throws<DirectoryNotFoundException>(() => Move(GetTestFilePath(), valid.FullName));
            Assert.Throws<DirectoryNotFoundException>(() => Move(valid.FullName, Path.Combine(TestDirectory, GetTestFileName(), GetTestFileName())));
        }

        [Fact]
        public void MoveOntoSameDirectory()
        {
            DirectoryInfo testDir = Directory.CreateDirectory(GetTestFilePath());
            Assert.Throws<IOException>(() => Move(testDir.FullName, testDir.FullName));
        }

        [Fact]
        public void MoveOntoExistingDirectory()
        {
            DirectoryInfo testDir = Directory.CreateDirectory(GetTestFilePath());
            DirectoryInfo secondDir = Directory.CreateDirectory(GetTestFilePath());
            Assert.Throws<IOException>(() => Move(testDir.FullName, secondDir.FullName));
        }

        [Fact]
        public void MoveFile()
        {
            // Regression https://github.com/dotnet/runtime/issues/21673
            string source = GetTestFilePath();
            string destination = GetTestFilePath();
            File.Create(source).Dispose();
            Move(source, destination);
            Assert.True(File.Exists(destination));
            Assert.False(File.Exists(source));
        }

        [Fact]
        public void MoveFile_TrailingDestinationSlash()
        {
            // Regression https://github.com/dotnet/runtime/issues/21673
            string source = GetTestFilePath();
            string destination = GetTestFilePath();
            File.Create(source).Dispose();
            Move(source, destination + Path.DirectorySeparatorChar);
            Assert.True(File.Exists(destination));
            Assert.False(File.Exists(source));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void MoveFile_TrailingDestinationAltSlash_Windows()
        {
            // Regression https://github.com/dotnet/runtime/issues/21673
            string source = GetTestFilePath();
            string destination = GetTestFilePath();
            File.Create(source).Dispose();
            Move(source, destination + Path.AltDirectorySeparatorChar);
            Assert.True(File.Exists(destination));
            Assert.False(File.Exists(source));
        }

        [Fact]
        public void MoveFile_TrailingSourceSlash()
        {
            string source = GetTestFilePath();
            string destination = GetTestFilePath();
            File.Create(source).Dispose();
            Assert.Throws<IOException>(() => Move(source + Path.DirectorySeparatorChar, destination));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void MoveFile_TrailingSourceAltSlash_Windows()
        {
            string source = GetTestFilePath();
            string destination = GetTestFilePath();
            File.Create(source).Dispose();
            Assert.Throws<IOException>(() => Move(source + Path.AltDirectorySeparatorChar, destination));
        }

        [Fact]
        public void MoveOntoFile()
        {
            DirectoryInfo testDir = Directory.CreateDirectory(GetTestFilePath());
            string testFile = GetTestFilePath();
            File.WriteAllText(testFile, "");
            Assert.Throws<IOException>(() => Move(testDir.FullName, testFile));
        }

        [Fact]
        public void MoveIntoCurrentDirectory()
        {
            DirectoryInfo testDir = Directory.CreateDirectory(GetTestFilePath());
            Assert.Throws<IOException>(() => Move(testDir.FullName, Path.Combine(testDir.FullName, ".")));
        }

        [Fact]
        public void MoveOntoParentDirectory()
        {
            DirectoryInfo testDir = Directory.CreateDirectory(GetTestFilePath());
            Assert.Throws<IOException>(() => Move(testDir.FullName, Path.Combine(testDir.FullName, "..")));
        }

        [Fact]
        public void BasicMove()
        {
            string testDirSource = Path.Combine(TestDirectory, GetTestFileName());
            string testDirDest = Path.Combine(TestDirectory, GetTestFileName());

            Directory.CreateDirectory(testDirSource);
            Move(testDirSource, testDirDest);
            Assert.True(Directory.Exists(testDirDest));
        }

        [Fact]
        public void MultipleMoves()
        {
            string testDir = GetTestFilePath();
            string testDirSource = Path.Combine(testDir, GetTestFileName());
            string testDirDest1 = Path.Combine(testDir, GetTestFileName());
            string testDirDest2 = Path.Combine(testDir, GetTestFileName());

            Directory.CreateDirectory(testDirSource);
            Move(testDirSource, testDirDest1);
            Move(testDirDest1, testDirDest2);
            Assert.True(Directory.Exists(testDirDest2));
            Assert.False(Directory.Exists(testDirDest1));
            Assert.False(Directory.Exists(testDirSource));
        }

        [Fact]
        public void DirectoryNameWithSpaces()
        {
            string testDirSource = Path.Combine(TestDirectory, GetTestFileName());
            string testDirDest = Path.Combine(TestDirectory, "    e n   d");

            Directory.CreateDirectory(testDirSource);
            Move(testDirSource, testDirDest);
            Assert.True(Directory.Exists(testDirDest));
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/40536", TestPlatforms.Browser)]
        public void TrailingDirectorySeparators()
        {
            string testDirSource = Path.Combine(TestDirectory, GetTestFileName());
            string testDirDest = Path.Combine(TestDirectory, GetTestFileName());

            Directory.CreateDirectory(testDirSource);
            Move(testDirSource + Path.DirectorySeparatorChar, testDirDest + Path.DirectorySeparatorChar);
            Assert.True(Directory.Exists(testDirDest));
        }

        [Fact]
        public void IncludeSubdirectories()
        {
            string testDirSource = Path.Combine(TestDirectory, GetTestFileName());
            string testDirSubDirectory = GetTestFileName();
            string testDirDest = Path.Combine(TestDirectory, GetTestFileName());

            Directory.CreateDirectory(testDirSource);
            Directory.CreateDirectory(Path.Combine(testDirSource, testDirSubDirectory));
            Move(testDirSource, testDirDest);

            Assert.True(Directory.Exists(testDirDest));
            Assert.False(Directory.Exists(testDirSource));
            Assert.True(Directory.Exists(Path.Combine(testDirDest, testDirSubDirectory)));
        }

        [Fact]
        public void Path_Longer_Than_MaxLongPath_Throws_Exception()
        {
            string testDir = GetTestFilePath();
            Directory.CreateDirectory(testDir);
            Assert.All((IOInputs.GetPathsLongerThanMaxLongPath(GetTestFilePath())), (path) =>
            {
                AssertExtensions.ThrowsAny<PathTooLongException, DirectoryNotFoundException>(() => Move(testDir, path));
                AssertExtensions.ThrowsAny<PathTooLongException, DirectoryNotFoundException>(() => Move(path, testDir));
            });
        }

        [Fact]
        public void ThrowIOExceptionWhenMovingDirectoryToItself()
        {
            Directory.CreateDirectory(Path.Combine(TestDirectory, "foo"));
            Assert.Throws<IOException>(() => Move(Path.Combine(TestDirectory, "foo"), Path.Combine(TestDirectory, "foo")));
        }

        [Fact]
        public void ThrowIOExceptionWhenMovingToExistingDirectoryWithSameCase()
        {
            Directory.CreateDirectory(Path.Combine(TestDirectory, "foo"));
            Directory.CreateDirectory(Path.Combine(TestDirectory, "bar", "foo"));
            Assert.Throws<IOException>(() => Move(Path.Combine(TestDirectory, "foo"), Path.Combine(TestDirectory, "bar", "foo")));
        }

        [Fact]
        public void ToNewDirectoryButWithDifferentCasing()
        {
            Directory.CreateDirectory(Path.Combine(TestDirectory, "foo"));
            var otherDirectory = Path.Combine(TestDirectory, "bar");
            Directory.CreateDirectory(Path.Combine(otherDirectory));
            Move(Path.Combine(TestDirectory, "foo"), Path.Combine(otherDirectory, "FOO"));
            Assert.True(Directory.Exists(Path.Combine(otherDirectory, "FOO")));
            Assert.False(Directory.Exists(Path.Combine(TestDirectory, "foo")));
        }

        [Fact]
        public void SameDirectoryWithDifferentCasing_WithFileContent()
        {
            var fooDirectory = Path.Combine(TestDirectory, "foo");
            var fooDirectoryUppercase = Path.Combine(TestDirectory, "FOO");
            Directory.CreateDirectory(fooDirectory);
            File.WriteAllText(Path.Combine(fooDirectory, "bar.txt"), string.Empty);
            Move(fooDirectory, fooDirectoryUppercase);
            var firstFile = Directory.GetFiles(fooDirectoryUppercase);
            Assert.Equal("bar.txt", Path.GetFileName(firstFile[0]));
        }

        [Fact]
        public void WithDifferentRootCase()
        {
            Directory.CreateDirectory($"{TestDirectory}/bar");
            var root = Path.GetPathRoot(TestDirectory);
            Move($"{TestDirectory}/bar".Replace(root, root.ToLower()), $"{TestDirectory}/foo");
            Assert.True(Directory.Exists($"{TestDirectory}/foo"));
            Assert.False(Directory.Exists($"{TestDirectory}/bar"));
        }

        [Fact]
        public void SameDirectoryWithDifferentCasing_WithDirectoryContent()
        {
            var fooDirectoryPath = Path.Combine(TestDirectory, "foo");
            var fooDirectoryPathUpperCase = Path.Combine(TestDirectory, "FOO");
            Directory.CreateDirectory(fooDirectoryPath);
            Directory.CreateDirectory(Path.Combine(fooDirectoryPath, "bar"));
            Move(fooDirectoryPath, fooDirectoryPathUpperCase);
            var firstFile = Directory.GetDirectories(fooDirectoryPathUpperCase);
            Assert.Equal("bar", Path.GetFileName(firstFile[0]));
        }

        #endregion

        #region PlatformSpecific

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows | TestPlatforms.OSX)]
        public void DirectoryWithDifferentCasingThanFileSystem_ToAnotherDirectory()
        {
            Directory.CreateDirectory(Path.Combine(TestDirectory, "FOO"));
            Directory.CreateDirectory(Path.Combine(TestDirectory, "bar"));
            Move(Path.Combine(TestDirectory, "foo"), Path.Combine(TestDirectory, "bar", "FOO"));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows | TestPlatforms.OSX)]
        public void DirectoryWithDifferentCasingThanFileSystem_ToItself()
        {
            Directory.CreateDirectory(Path.Combine(TestDirectory, "FOO"));
            Move(Path.Combine(TestDirectory, "foo"), Path.Combine(TestDirectory, "FOO"));
            Assert.True(Directory.Exists(Path.Combine(TestDirectory, "FOO")));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Linux)]
        public void DirectoryWithDifferentCasingThanFileSystem_ToAnotherDirectory_CaseSensitiveOS()
        {
            Directory.CreateDirectory(Path.Combine(TestDirectory, "FOO"));
            Directory.CreateDirectory(Path.Combine(TestDirectory, "bar"));
            Assert.Throws<DirectoryNotFoundException>(() => Move(Path.Combine(TestDirectory, "foo"), Path.Combine(TestDirectory, "bar", "FOO")));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Linux)]
        public void DirectoryWithDifferentCasingThanFileSystem_ToItself_CaseSensitiveOS()
        {
            Directory.CreateDirectory(Path.Combine(TestDirectory, "FOO"));
            Assert.Throws<DirectoryNotFoundException>(() => Move(Path.Combine(TestDirectory, "foo"), Path.Combine(TestDirectory, "FOO")));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void CaseVariantDirectoryNameWithCaseVariantPaths_CaseInsensitiveFileSystem()
        {
            var directoryToBeMoved = Path.Combine(TestDirectory, "FOO", "bar");
            var newPath = Path.Combine(TestDirectory, "foo", "bar");
            Directory.CreateDirectory(Path.Combine(TestDirectory, "FOO", "bar"));
            Directory.CreateDirectory(Path.Combine(TestDirectory, "foo"));

            Assert.Throws<IOException>(() => Move(directoryToBeMoved, Path.Combine(newPath, "bar")));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows | TestPlatforms.OSX | TestPlatforms.FreeBSD | TestPlatforms.NetBSD)]
        public void MoveDirectory_FailToMoveDirectoryWithUpperCaseToOtherDirectoryWithLowerCase()
        {
            Directory.CreateDirectory($"{TestDirectory}/FOO");
            Directory.CreateDirectory($"{TestDirectory}/bar/foo");
            Assert.Throws<IOException>(() => Move($"{TestDirectory}/FOO", $"{TestDirectory}/bar/foo"));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void MoveDirectory_FailToMoveLowerCaseDirectoryWhenUpperCaseDirectoryExists()
        {
            Directory.CreateDirectory($"{TestDirectory}/bar/FOO");
            Directory.CreateDirectory($"{TestDirectory}/foo");
            Assert.Throws<IOException>(() => Move($"{TestDirectory}/foo", $"{TestDirectory}/bar/foo"));
        }

        [ConditionalFact(nameof(AreAllLongPathsAvailable))]
        [PlatformSpecific(TestPlatforms.Windows)]  // Long path succeeds
        public void Path_With_Longer_Than_MaxDirectory_Succeeds()
        {
            string testDir = GetTestFilePath();
            Directory.CreateDirectory(testDir);
            Assert.True(Directory.Exists(testDir), "test directory should exist");
            Assert.All((IOInputs.GetPathsLongerThanMaxDirectory(GetTestFilePath())), (path) =>
            {
                string baseDestinationPath = Path.GetDirectoryName(path);
                if (!Directory.Exists(baseDestinationPath))
                {
                    Directory.CreateDirectory(baseDestinationPath);
                }
                Assert.True(Directory.Exists(baseDestinationPath), "base destination path should exist");

                Move(testDir, path);
                Assert.False(Directory.Exists(testDir), "source directory should exist");
                Assert.True(Directory.Exists(path), "destination directory should exist");
                Move(path, testDir);
                Assert.False(Directory.Exists(path), "source directory should exist");
                Assert.True(Directory.Exists(testDir), "destination directory should exist");
            });
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void WindowsWildCharacterPath_Core()
        {
            Assert.ThrowsAny<IOException>(() => Move(Path.Combine(TestDirectory, "*"), GetTestFilePath()));
            Assert.ThrowsAny<IOException>(() => Move(TestDirectory, Path.Combine(TestDirectory, "*")));
            Assert.ThrowsAny<IOException>(() => Move(TestDirectory, Path.Combine(TestDirectory, "Test*t")));
            Assert.ThrowsAny<IOException>(() => Move(TestDirectory, Path.Combine(TestDirectory, "*Test")));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]  // Wild characters in path are allowed
        public void UnixWildCharacterPath()
        {
            // Wildcards are allowed in paths for Unix move commands as literals as well as functional wildcards,
            // but to implement the latter in .NET would be confusing (e.g. having a DirectoryInfo represent multiple directories),
            // so the implementation assumes the former.
            // Thus, any "*" characters will act the same as any other character when used in a file/directory name.
            string testDir = GetTestFilePath();
            string testDirSource = Path.Combine(testDir, "*");
            string testDirShouldntMove = Path.Combine(testDir, "*t");
            string testDirDest = Path.Combine(testDir, "*" + GetTestFileName());

            Directory.CreateDirectory(testDirSource);
            Directory.CreateDirectory(testDirShouldntMove);
            Move(testDirSource, testDirDest);
            Assert.True(Directory.Exists(testDirDest));
            Assert.False(Directory.Exists(testDirSource));
            Assert.True(Directory.Exists(testDirShouldntMove));

            Move(testDirDest, testDirSource);
            Assert.False(Directory.Exists(testDirDest));
            Assert.True(Directory.Exists(testDirSource));
            Assert.True(Directory.Exists(testDirShouldntMove));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void WindowsEmptyPath()
        {
            DirectoryInfo testDir = Directory.CreateDirectory(GetTestFilePath());
            Assert.Throws<ArgumentException>(() => Move(testDir.FullName, "         "));
            Assert.Throws<ArgumentException>(() => Move(testDir.FullName, ""));
            Assert.Throws<ArgumentException>(() => Move(testDir.FullName, "\0"));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]  // Whitespace path allowed
        public void UnixWhitespacePath()
        {
            DirectoryInfo testDir = Directory.CreateDirectory(GetTestFilePath());
            string testDirToMove = Path.Combine(testDir.FullName, GetTestFileName());
            Directory.CreateDirectory(testDirToMove);
            Move(testDirToMove, Path.Combine(testDir.FullName, "         "));
            Move(Path.Combine(testDir.FullName, "         "), Path.Combine(testDir.FullName, "\n"));
            Move(Path.Combine(testDir.FullName, "\n"), Path.Combine(testDir.FullName, "\t"));
            Move(Path.Combine(testDir.FullName, "\t"), Path.Combine(testDir.FullName, ">"));
            Move(Path.Combine(testDir.FullName, ">"), Path.Combine(testDir.FullName, "< "));
            Assert.True(Directory.Exists(Path.Combine(testDir.FullName, "< ")));
        }

        [Fact]
        // Moving to existing directory causes IOException
        public void ExistingDirectory()
        {
            DirectoryInfo testDir = Directory.CreateDirectory(GetTestFilePath());
            string testDirSource = Path.Combine(testDir.FullName, GetTestFileName());
            string testDirDest = Path.Combine(testDir.FullName, GetTestFileName());

            Directory.CreateDirectory(testDirSource);
            Directory.CreateDirectory(testDirDest);
            Assert.Throws<IOException>(() => Move(testDirSource, testDirDest));
            Assert.True(Directory.Exists(testDirDest));
            Assert.True(Directory.Exists(testDirSource));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]  // drive labels
        public void BetweenDriveLabels()
        {
            DirectoryInfo testDir = Directory.CreateDirectory(GetTestFilePath());
            string path = Path.GetFullPath(testDir.FullName);
            if (path.Substring(0, 3) == @"d:\" || path.Substring(0, 3) == @"D:\")
                Assert.Throws<IOException>(() => Move(path, "C:\\DoesntExist"));
            else
                Assert.Throws<IOException>(() => Move(path, "D:\\DoesntExist"));
        }

        #endregion
    }
}
