// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Xunit;

namespace System.IO.Tests.Enumeration
{
    public abstract class MatchTypesTests : FileSystemTest
    {
        protected abstract string[] GetPaths(string directory, string pattern, EnumerationOptions options);

        [Fact]
        public void QuestionMarkBehavior()
        {
            DirectoryInfo testDirectory = Directory.CreateDirectory(GetTestFilePath());
            FileInfo fileOne = new FileInfo(Path.Combine(testDirectory.FullName, "a.one"));
            FileInfo fileTwo = new FileInfo(Path.Combine(testDirectory.FullName, "ab.two"));
            FileInfo fileThree = new FileInfo(Path.Combine(testDirectory.FullName, "abc.three"));

            fileOne.Create().Dispose();
            fileTwo.Create().Dispose();
            fileThree.Create().Dispose();

            // Question marks collapse to periods in Win32
            string[] paths = GetPaths(testDirectory.FullName, "a??.*", new EnumerationOptions { MatchType = MatchType.Win32 });
            FSAssert.EqualWhenOrdered(new string[] { fileOne.FullName, fileTwo.FullName, fileThree.FullName }, paths);

            paths = GetPaths(testDirectory.FullName, "*.?????", new EnumerationOptions { MatchType = MatchType.Win32 });
            FSAssert.EqualWhenOrdered(new string[] { fileOne.FullName, fileTwo.FullName, fileThree.FullName }, paths);

            // Simple, one question mark is one character
            paths = GetPaths(testDirectory.FullName, "a??.*", new EnumerationOptions { MatchType = MatchType.Simple });
            FSAssert.EqualWhenOrdered(new string[] { fileThree.FullName }, paths);

            paths = GetPaths(testDirectory.FullName, "*.?????", new EnumerationOptions { MatchType = MatchType.Simple });
            FSAssert.EqualWhenOrdered(new string[] { fileThree.FullName }, paths);
        }

        [Fact]
        public void StarDotBehavior()
        {
            DirectoryInfo testDirectory = Directory.CreateDirectory(GetTestFilePath());
            FileInfo fileOne = new FileInfo(Path.Combine(testDirectory.FullName, "one"));
            FileInfo fileTwo = new FileInfo(Path.Combine(testDirectory.FullName, "one.two"));
            string fileThree = Path.Combine(testDirectory.FullName, "three.");

            fileOne.Create().Dispose();
            fileTwo.Create().Dispose();

            // Need extended device syntax to create a name with a trailing dot.
            File.Create(PlatformDetection.IsWindows ? @"\\?\" + fileThree : fileThree).Dispose();

            // *. means any file without an extension
            string[] paths = GetPaths(testDirectory.FullName, "*.", new EnumerationOptions { MatchType = MatchType.Win32 });
            FSAssert.EqualWhenOrdered(new string[] { fileOne.FullName, fileThree }, paths);

            // Simple, anything with a trailing period
            paths = GetPaths(testDirectory.FullName, "*.", new EnumerationOptions { MatchType = MatchType.Simple });
            FSAssert.EqualWhenOrdered(new string[] { fileThree }, paths);
        }

        [Fact]
        public void SimpleMatchType_QuestionMarkIsExactlyOneCharacter()
        {
            // This test validates that MatchType.Simple with '?' correctly matches exactly one character.
            // If the OS-level filter incorrectly received an untransformed '?' pattern, Windows would
            // interpret it as DOS_QM which has different semantics (collapses to periods, can match
            // zero characters at end of name), resulting in fewer matches than expected.

            DirectoryInfo testDirectory = Directory.CreateDirectory(GetTestFilePath());
            FileInfo file1 = new FileInfo(Path.Combine(testDirectory.FullName, "test1.txt"));
            FileInfo file2 = new FileInfo(Path.Combine(testDirectory.FullName, "test12.txt"));
            FileInfo file3 = new FileInfo(Path.Combine(testDirectory.FullName, "test123.txt"));
            FileInfo file4 = new FileInfo(Path.Combine(testDirectory.FullName, "test.txt"));

            file1.Create().Dispose();
            file2.Create().Dispose();
            file3.Create().Dispose();
            file4.Create().Dispose();

            // With Simple matching, "test?.txt" should match exactly files with one character after "test"
            string[] paths = GetPaths(testDirectory.FullName, "test?.txt", new EnumerationOptions { MatchType = MatchType.Simple });
            FSAssert.EqualWhenOrdered(new string[] { file1.FullName }, paths);

            // With Simple matching, "test??.txt" should match exactly files with two characters after "test"
            paths = GetPaths(testDirectory.FullName, "test??.txt", new EnumerationOptions { MatchType = MatchType.Simple });
            FSAssert.EqualWhenOrdered(new string[] { file2.FullName }, paths);

            // With Simple matching, "test???.txt" should match exactly files with three characters after "test"
            paths = GetPaths(testDirectory.FullName, "test???.txt", new EnumerationOptions { MatchType = MatchType.Simple });
            FSAssert.EqualWhenOrdered(new string[] { file3.FullName }, paths);
        }

        [Fact]
        public void ConsecutiveDotsInPattern()
        {
            // This test validates that patterns with consecutive dots work correctly.
            // Files with consecutive dots in their names (e.g., "file..txt") are valid and
            // should be found when searching with a matching pattern.

            DirectoryInfo testDirectory = Directory.CreateDirectory(GetTestFilePath());
            FileInfo fileWithDoubleDot = new FileInfo(Path.Combine(testDirectory.FullName, "file..txt"));
            FileInfo normalFile = new FileInfo(Path.Combine(testDirectory.FullName, "file.txt"));
            FileInfo anotherDoubleDot = new FileInfo(Path.Combine(testDirectory.FullName, "test..log"));

            fileWithDoubleDot.Create().Dispose();
            normalFile.Create().Dispose();
            anotherDoubleDot.Create().Dispose();

            // Search for the exact pattern with consecutive dots
            string[] paths = GetPaths(testDirectory.FullName, "file..txt", new EnumerationOptions { MatchType = MatchType.Win32 });
            FSAssert.EqualWhenOrdered(new string[] { fileWithDoubleDot.FullName }, paths);

            paths = GetPaths(testDirectory.FullName, "file..txt", new EnumerationOptions { MatchType = MatchType.Simple });
            FSAssert.EqualWhenOrdered(new string[] { fileWithDoubleDot.FullName }, paths);

            // Search with wildcard that should match files with consecutive dots
            paths = GetPaths(testDirectory.FullName, "*..txt", new EnumerationOptions { MatchType = MatchType.Win32 });
            FSAssert.EqualWhenOrdered(new string[] { fileWithDoubleDot.FullName }, paths);

            paths = GetPaths(testDirectory.FullName, "*..txt", new EnumerationOptions { MatchType = MatchType.Simple });
            FSAssert.EqualWhenOrdered(new string[] { fileWithDoubleDot.FullName }, paths);

            // Search with pattern that matches all files with consecutive dots
            paths = GetPaths(testDirectory.FullName, "*..*", new EnumerationOptions { MatchType = MatchType.Win32 });
            FSAssert.EqualWhenOrdered(new string[] { fileWithDoubleDot.FullName, anotherDoubleDot.FullName }, paths);

            paths = GetPaths(testDirectory.FullName, "*..*", new EnumerationOptions { MatchType = MatchType.Simple });
            FSAssert.EqualWhenOrdered(new string[] { fileWithDoubleDot.FullName, anotherDoubleDot.FullName }, paths);
        }

        [Fact]
        public void PatternWithDirectoryPath()
        {
            // This test validates that patterns containing directory paths work correctly.
            // The pattern is split into directory and filename components during normalization.

            DirectoryInfo testDirectory = Directory.CreateDirectory(GetTestFilePath());
            DirectoryInfo subDir = testDirectory.CreateSubdirectory("subdir");
            FileInfo fileInRoot = new FileInfo(Path.Combine(testDirectory.FullName, "root.txt"));
            FileInfo fileInSub = new FileInfo(Path.Combine(subDir.FullName, "sub.txt"));

            fileInRoot.Create().Dispose();
            fileInSub.Create().Dispose();

            // Pattern with subdirectory path should find files in that subdirectory
            string[] paths = GetPaths(testDirectory.FullName, Path.Combine("subdir", "*.txt"), new EnumerationOptions { MatchType = MatchType.Win32 });
            Assert.Single(paths);
            Assert.EndsWith("sub.txt", paths[0]);

            paths = GetPaths(testDirectory.FullName, Path.Combine("subdir", "*.txt"), new EnumerationOptions { MatchType = MatchType.Simple });
            Assert.Single(paths);
            Assert.EndsWith("sub.txt", paths[0]);
        }

        [Fact]
        public void PatternWithParentDirectoryReference()
        {
            // This test validates that patterns with ".." parent directory references work correctly.
            // When the pattern contains "..", it navigates up to the parent directory.
            // Note: The returned path may contain ".." - it's not normalized.

            DirectoryInfo testDirectory = Directory.CreateDirectory(GetTestFilePath());
            DirectoryInfo subDir = testDirectory.CreateSubdirectory("subdir");
            FileInfo fileInRoot = new FileInfo(Path.Combine(testDirectory.FullName, "root.txt"));
            FileInfo fileInSub = new FileInfo(Path.Combine(subDir.FullName, "sub.txt"));

            fileInRoot.Create().Dispose();
            fileInSub.Create().Dispose();

            // From subdir, pattern with ".." should find files in parent (testDirectory)
            string[] paths = GetPaths(subDir.FullName, Path.Combine("..", "*.txt"), new EnumerationOptions { MatchType = MatchType.Win32 });
            Assert.Single(paths);
            Assert.EndsWith("root.txt", paths[0]);

            paths = GetPaths(subDir.FullName, Path.Combine("..", "*.txt"), new EnumerationOptions { MatchType = MatchType.Simple });
            Assert.Single(paths);
            Assert.EndsWith("root.txt", paths[0]);

            // Pattern "subdir/../*.txt" from testDirectory should find files in testDirectory
            paths = GetPaths(testDirectory.FullName, Path.Combine("subdir", "..", "*.txt"), new EnumerationOptions { MatchType = MatchType.Win32 });
            Assert.Single(paths);
            Assert.EndsWith("root.txt", paths[0]);

            paths = GetPaths(testDirectory.FullName, Path.Combine("subdir", "..", "*.txt"), new EnumerationOptions { MatchType = MatchType.Simple });
            Assert.Single(paths);
            Assert.EndsWith("root.txt", paths[0]);
        }

        [Fact]
        public void PatternWithCurrentDirectoryReference()
        {
            // This test validates that patterns with "." current directory references work correctly.

            DirectoryInfo testDirectory = Directory.CreateDirectory(GetTestFilePath());
            FileInfo file = new FileInfo(Path.Combine(testDirectory.FullName, "test.txt"));
            file.Create().Dispose();

            // Pattern with "./" should work the same as without it
            string[] paths = GetPaths(testDirectory.FullName, Path.Combine(".", "*.txt"), new EnumerationOptions { MatchType = MatchType.Win32 });
            Assert.Single(paths);
            Assert.EndsWith("test.txt", paths[0]);

            paths = GetPaths(testDirectory.FullName, Path.Combine(".", "*.txt"), new EnumerationOptions { MatchType = MatchType.Simple });
            Assert.Single(paths);
            Assert.EndsWith("test.txt", paths[0]);
        }

        [Fact]
        public void FilenamesStartingWithDots()
        {
            // Filenames starting with dots are valid (e.g., .gitignore, ..foo)

            DirectoryInfo testDirectory = Directory.CreateDirectory(GetTestFilePath());
            FileInfo dotFile = new FileInfo(Path.Combine(testDirectory.FullName, ".gitignore"));
            FileInfo doubleDotFile = new FileInfo(Path.Combine(testDirectory.FullName, "..foo"));
            FileInfo normalFile = new FileInfo(Path.Combine(testDirectory.FullName, "normal.txt"));

            dotFile.Create().Dispose();
            doubleDotFile.Create().Dispose();
            normalFile.Create().Dispose();

            // Pattern ".*" matches files starting with dot (includes both .gitignore and ..foo)
            string[] paths = GetPaths(testDirectory.FullName, ".*", new EnumerationOptions { MatchType = MatchType.Win32 });
            FSAssert.EqualWhenOrdered(new string[] { dotFile.FullName, doubleDotFile.FullName }, paths);

            paths = GetPaths(testDirectory.FullName, ".*", new EnumerationOptions { MatchType = MatchType.Simple });
            FSAssert.EqualWhenOrdered(new string[] { dotFile.FullName, doubleDotFile.FullName }, paths);

            // Pattern "..*" should match files starting with two dots
            paths = GetPaths(testDirectory.FullName, "..*", new EnumerationOptions { MatchType = MatchType.Win32 });
            FSAssert.EqualWhenOrdered(new string[] { doubleDotFile.FullName }, paths);

            paths = GetPaths(testDirectory.FullName, "..*", new EnumerationOptions { MatchType = MatchType.Simple });
            FSAssert.EqualWhenOrdered(new string[] { doubleDotFile.FullName }, paths);

            // Exact match for .gitignore
            paths = GetPaths(testDirectory.FullName, ".gitignore", new EnumerationOptions { MatchType = MatchType.Win32 });
            FSAssert.EqualWhenOrdered(new string[] { dotFile.FullName }, paths);

            paths = GetPaths(testDirectory.FullName, ".gitignore", new EnumerationOptions { MatchType = MatchType.Simple });
            FSAssert.EqualWhenOrdered(new string[] { dotFile.FullName }, paths);
        }

        [Fact]
        public void MultipleConsecutiveDots()
        {
            // Test filenames with multiple consecutive dots

            DirectoryInfo testDirectory = Directory.CreateDirectory(GetTestFilePath());
            FileInfo twoDots = new FileInfo(Path.Combine(testDirectory.FullName, "a..b"));
            FileInfo threeDots = new FileInfo(Path.Combine(testDirectory.FullName, "a...b"));
            FileInfo manyDots = new FileInfo(Path.Combine(testDirectory.FullName, "a....b"));
            FileInfo normal = new FileInfo(Path.Combine(testDirectory.FullName, "a.b"));

            twoDots.Create().Dispose();
            threeDots.Create().Dispose();
            manyDots.Create().Dispose();
            normal.Create().Dispose();

            // Exact matches
            string[] paths = GetPaths(testDirectory.FullName, "a..b", new EnumerationOptions { MatchType = MatchType.Win32 });
            FSAssert.EqualWhenOrdered(new string[] { twoDots.FullName }, paths);

            paths = GetPaths(testDirectory.FullName, "a...b", new EnumerationOptions { MatchType = MatchType.Simple });
            FSAssert.EqualWhenOrdered(new string[] { threeDots.FullName }, paths);

            // Wildcard matching multiple dots
            paths = GetPaths(testDirectory.FullName, "a..*", new EnumerationOptions { MatchType = MatchType.Win32 });
            FSAssert.EqualWhenOrdered(new string[] { twoDots.FullName, threeDots.FullName, manyDots.FullName }, paths);

            paths = GetPaths(testDirectory.FullName, "a..*", new EnumerationOptions { MatchType = MatchType.Simple });
            FSAssert.EqualWhenOrdered(new string[] { twoDots.FullName, threeDots.FullName, manyDots.FullName }, paths);
        }

        [Fact]
        public void NoMatchReturnsEmpty()
        {
            // Patterns that don't match any files should return empty

            DirectoryInfo testDirectory = Directory.CreateDirectory(GetTestFilePath());
            FileInfo file = new FileInfo(Path.Combine(testDirectory.FullName, "exists.txt"));
            file.Create().Dispose();

            string[] paths = GetPaths(testDirectory.FullName, "doesnotexist.txt", new EnumerationOptions { MatchType = MatchType.Win32 });
            Assert.Empty(paths);

            paths = GetPaths(testDirectory.FullName, "doesnotexist.txt", new EnumerationOptions { MatchType = MatchType.Simple });
            Assert.Empty(paths);

            paths = GetPaths(testDirectory.FullName, "*.xyz", new EnumerationOptions { MatchType = MatchType.Win32 });
            Assert.Empty(paths);

            paths = GetPaths(testDirectory.FullName, "*.xyz", new EnumerationOptions { MatchType = MatchType.Simple });
            Assert.Empty(paths);
        }

        [Fact]
        public void StarDotStarBehavior()
        {
            // *.* has different behavior between Win32 and Simple:
            // - Win32: *.* is treated as * (matches everything)
            // - Simple: *.* requires a dot in the filename

            DirectoryInfo testDirectory = Directory.CreateDirectory(GetTestFilePath());
            FileInfo withDot = new FileInfo(Path.Combine(testDirectory.FullName, "file.txt"));
            FileInfo withoutDot = new FileInfo(Path.Combine(testDirectory.FullName, "nodot"));
            FileInfo multipleDots = new FileInfo(Path.Combine(testDirectory.FullName, "a.b.c"));

            withDot.Create().Dispose();
            withoutDot.Create().Dispose();
            multipleDots.Create().Dispose();

            // Win32: *.* matches everything (treated as *)
            string[] paths = GetPaths(testDirectory.FullName, "*.*", new EnumerationOptions { MatchType = MatchType.Win32 });
            FSAssert.EqualWhenOrdered(new string[] { multipleDots.FullName, withDot.FullName, withoutDot.FullName }, paths);

            // Simple: *.* requires at least one dot
            paths = GetPaths(testDirectory.FullName, "*.*", new EnumerationOptions { MatchType = MatchType.Simple });
            FSAssert.EqualWhenOrdered(new string[] { multipleDots.FullName, withDot.FullName }, paths);
        }

        [Fact]
        public void QuestionMarkAtEndOfName()
        {
            // ? at end of name has different behavior:
            // - Win32 (DOS_QM): can match zero characters at end of name
            // - Simple: must match exactly one character

            DirectoryInfo testDirectory = Directory.CreateDirectory(GetTestFilePath());
            FileInfo file1 = new FileInfo(Path.Combine(testDirectory.FullName, "test"));
            FileInfo file2 = new FileInfo(Path.Combine(testDirectory.FullName, "test1"));
            FileInfo file3 = new FileInfo(Path.Combine(testDirectory.FullName, "test12"));

            file1.Create().Dispose();
            file2.Create().Dispose();
            file3.Create().Dispose();

            // Win32: "test?" can match "test" (zero chars) and "test1" (one char) due to DOS_QM
            string[] paths = GetPaths(testDirectory.FullName, "test?", new EnumerationOptions { MatchType = MatchType.Win32 });
            FSAssert.EqualWhenOrdered(new string[] { file1.FullName, file2.FullName }, paths);

            // Simple: "test?" must match exactly one character after "test"
            paths = GetPaths(testDirectory.FullName, "test?", new EnumerationOptions { MatchType = MatchType.Simple });
            FSAssert.EqualWhenOrdered(new string[] { file2.FullName }, paths);
        }

        [Fact]
        public void QuestionMarkBeforeDot()
        {
            // ? before a dot has different behavior:
            // - Win32 (DOS_QM): skips over the dot
            // - Simple: must match exactly one character (not a dot)

            DirectoryInfo testDirectory = Directory.CreateDirectory(GetTestFilePath());
            FileInfo file1 = new FileInfo(Path.Combine(testDirectory.FullName, "a.txt"));
            FileInfo file2 = new FileInfo(Path.Combine(testDirectory.FullName, "ab.txt"));
            FileInfo file3 = new FileInfo(Path.Combine(testDirectory.FullName, ".txt"));

            file1.Create().Dispose();
            file2.Create().Dispose();
            file3.Create().Dispose();

            // Win32: "?.txt" - DOS_QM can skip to the dot, matches "a.txt" and ".txt"
            string[] paths = GetPaths(testDirectory.FullName, "?.txt", new EnumerationOptions { MatchType = MatchType.Win32 });
            FSAssert.EqualWhenOrdered(new string[] { file3.FullName, file1.FullName }, paths);

            // Simple: "?.txt" must have exactly one character before .txt
            paths = GetPaths(testDirectory.FullName, "?.txt", new EnumerationOptions { MatchType = MatchType.Simple });
            FSAssert.EqualWhenOrdered(new string[] { file1.FullName }, paths);
        }

        [Fact]
        public void DotQuestionMarkBehavior()
        {
            // .? pattern behavior differs between Win32 and Simple

            DirectoryInfo testDirectory = Directory.CreateDirectory(GetTestFilePath());
            FileInfo file1 = new FileInfo(Path.Combine(testDirectory.FullName, "test.a"));
            FileInfo file2 = new FileInfo(Path.Combine(testDirectory.FullName, "test.ab"));
            FileInfo file3 = new FileInfo(Path.Combine(testDirectory.FullName, "test."));
            FileInfo file4 = new FileInfo(Path.Combine(testDirectory.FullName, "test"));

            file1.Create().Dispose();
            file2.Create().Dispose();
            // Need extended syntax for trailing dot on Windows
            File.Create(PlatformDetection.IsWindows ? @"\\?\" + file3.FullName : file3.FullName).Dispose();
            file4.Create().Dispose();

            // Win32: "test.?" - DOS_DOT + DOS_QM has special behavior
            string[] paths = GetPaths(testDirectory.FullName, "test.?", new EnumerationOptions { MatchType = MatchType.Win32 });
            // Win32 matches files with 0-1 char extension
            Assert.Contains(paths, p => p.EndsWith("test.a"));

            // Simple: "test.?" must have exactly one character after the dot
            paths = GetPaths(testDirectory.FullName, "test.?", new EnumerationOptions { MatchType = MatchType.Simple });
            FSAssert.EqualWhenOrdered(new string[] { file1.FullName }, paths);
        }

        [Fact]
        public void Win32AndSimpleIdenticalForLiteralPatterns()
        {
            // For patterns without wildcards, Win32 and Simple should behave identically

            DirectoryInfo testDirectory = Directory.CreateDirectory(GetTestFilePath());
            FileInfo file1 = new FileInfo(Path.Combine(testDirectory.FullName, "readme.txt"));
            FileInfo file2 = new FileInfo(Path.Combine(testDirectory.FullName, "readme.md"));
            FileInfo file3 = new FileInfo(Path.Combine(testDirectory.FullName, "file..name"));

            file1.Create().Dispose();
            file2.Create().Dispose();
            file3.Create().Dispose();

            // Literal pattern - both should match exactly
            string[] win32Paths = GetPaths(testDirectory.FullName, "readme.txt", new EnumerationOptions { MatchType = MatchType.Win32 });
            string[] simplePaths = GetPaths(testDirectory.FullName, "readme.txt", new EnumerationOptions { MatchType = MatchType.Simple });
            FSAssert.EqualWhenOrdered(win32Paths, simplePaths);
            Assert.Single(win32Paths);

            // Literal pattern with consecutive dots - both should match exactly
            win32Paths = GetPaths(testDirectory.FullName, "file..name", new EnumerationOptions { MatchType = MatchType.Win32 });
            simplePaths = GetPaths(testDirectory.FullName, "file..name", new EnumerationOptions { MatchType = MatchType.Simple });
            FSAssert.EqualWhenOrdered(win32Paths, simplePaths);
            Assert.Single(win32Paths);
        }

        [Fact]
        public void Win32AndSimpleIdenticalForSimpleStarPatterns()
        {
            // For patterns with only * (no ?), Win32 and Simple should behave identically
            // (except for *.* which is tested separately)

            DirectoryInfo testDirectory = Directory.CreateDirectory(GetTestFilePath());
            FileInfo file1 = new FileInfo(Path.Combine(testDirectory.FullName, "test.txt"));
            FileInfo file2 = new FileInfo(Path.Combine(testDirectory.FullName, "test.log"));
            FileInfo file3 = new FileInfo(Path.Combine(testDirectory.FullName, "other.txt"));
            FileInfo file4 = new FileInfo(Path.Combine(testDirectory.FullName, "test..double"));

            file1.Create().Dispose();
            file2.Create().Dispose();
            file3.Create().Dispose();
            file4.Create().Dispose();

            // *.txt - should be identical
            string[] win32Paths = GetPaths(testDirectory.FullName, "*.txt", new EnumerationOptions { MatchType = MatchType.Win32 });
            string[] simplePaths = GetPaths(testDirectory.FullName, "*.txt", new EnumerationOptions { MatchType = MatchType.Simple });
            FSAssert.EqualWhenOrdered(win32Paths, simplePaths);

            // test* - should be identical
            win32Paths = GetPaths(testDirectory.FullName, "test*", new EnumerationOptions { MatchType = MatchType.Win32 });
            simplePaths = GetPaths(testDirectory.FullName, "test*", new EnumerationOptions { MatchType = MatchType.Simple });
            FSAssert.EqualWhenOrdered(win32Paths, simplePaths);

            // *test* - should be identical
            win32Paths = GetPaths(testDirectory.FullName, "*test*", new EnumerationOptions { MatchType = MatchType.Win32 });
            simplePaths = GetPaths(testDirectory.FullName, "*test*", new EnumerationOptions { MatchType = MatchType.Simple });
            FSAssert.EqualWhenOrdered(win32Paths, simplePaths);

            // *..*  - should be identical (consecutive dots)
            win32Paths = GetPaths(testDirectory.FullName, "*..*", new EnumerationOptions { MatchType = MatchType.Win32 });
            simplePaths = GetPaths(testDirectory.FullName, "*..*", new EnumerationOptions { MatchType = MatchType.Simple });
            FSAssert.EqualWhenOrdered(win32Paths, simplePaths);
            Assert.Single(win32Paths); // Only test..double matches
        }
    }

    public class MatchTypesTests_Directory_GetFiles : MatchTypesTests
    {
        protected override string[] GetPaths(string directory, string pattern, EnumerationOptions options)
        {
            return Directory.GetFiles(directory, pattern, options);
        }
    }

    public class MatchTypesTests_DirectoryInfo_GetFiles : MatchTypesTests
    {
        protected override string[] GetPaths(string directory, string pattern, EnumerationOptions options)
        {
            return new DirectoryInfo(directory).GetFiles(pattern, options).Select(i => i.FullName).ToArray();
        }
    }
}
