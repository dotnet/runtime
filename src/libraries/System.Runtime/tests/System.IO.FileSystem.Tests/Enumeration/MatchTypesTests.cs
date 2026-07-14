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
            string[] paths = GetPaths(testDirectory.FullName, ".*", new EnumerationOptions { MatchType = MatchType.Win32, AttributesToSkip = 0 });
            FSAssert.EqualWhenOrdered(new string[] { dotFile.FullName, doubleDotFile.FullName }, paths);

            paths = GetPaths(testDirectory.FullName, ".*", new EnumerationOptions { MatchType = MatchType.Simple, AttributesToSkip = 0 });
            FSAssert.EqualWhenOrdered(new string[] { dotFile.FullName, doubleDotFile.FullName }, paths);

            // Pattern "..*" should match files starting with two dots
            paths = GetPaths(testDirectory.FullName, "..*", new EnumerationOptions { MatchType = MatchType.Win32, AttributesToSkip = 0 });
            FSAssert.EqualWhenOrdered(new string[] { doubleDotFile.FullName }, paths);

            paths = GetPaths(testDirectory.FullName, "..*", new EnumerationOptions { MatchType = MatchType.Simple, AttributesToSkip = 0 });
            FSAssert.EqualWhenOrdered(new string[] { doubleDotFile.FullName }, paths);

            // Exact match for .gitignore
            paths = GetPaths(testDirectory.FullName, ".gitignore", new EnumerationOptions { MatchType = MatchType.Win32, AttributesToSkip = 0 });
            FSAssert.EqualWhenOrdered(new string[] { dotFile.FullName }, paths);

            paths = GetPaths(testDirectory.FullName, ".gitignore", new EnumerationOptions { MatchType = MatchType.Simple, AttributesToSkip = 0 });
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
            string[] paths = GetPaths(testDirectory.FullName, "?.txt", new EnumerationOptions { MatchType = MatchType.Win32, AttributesToSkip = 0 });
            FSAssert.EqualWhenOrdered(new string[] { file3.FullName, file1.FullName }, paths);

            // Simple: "?.txt" must have exactly one character before .txt
            paths = GetPaths(testDirectory.FullName, "?.txt", new EnumerationOptions { MatchType = MatchType.Simple, AttributesToSkip = 0 });
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

        [Fact]
        public void OverlappingExtensions_Win32QuestionMarkPattern()
        {
            // Tests the specific scenario of files with overlapping extensions:
            // foo, foo.b, foo.ba, foo.bar against foo.??? pattern
            // Win32 DOS_QM semantics: ? can match 0-1 chars at end and collapses to periods

            DirectoryInfo testDirectory = Directory.CreateDirectory(GetTestFilePath());
            FileInfo fooNoExt = new FileInfo(Path.Combine(testDirectory.FullName, "foo"));
            FileInfo fooB = new FileInfo(Path.Combine(testDirectory.FullName, "foo.b"));
            FileInfo fooBa = new FileInfo(Path.Combine(testDirectory.FullName, "foo.ba"));
            FileInfo fooBar = new FileInfo(Path.Combine(testDirectory.FullName, "foo.bar"));
            FileInfo fooBarr = new FileInfo(Path.Combine(testDirectory.FullName, "foo.barr"));
            FileInfo fooX = new FileInfo(Path.Combine(testDirectory.FullName, "foo.x"));
            FileInfo fooXy = new FileInfo(Path.Combine(testDirectory.FullName, "foo.xy"));
            FileInfo fooXyz = new FileInfo(Path.Combine(testDirectory.FullName, "foo.xyz"));
            FileInfo barBaz = new FileInfo(Path.Combine(testDirectory.FullName, "bar.baz"));

            fooNoExt.Create().Dispose();
            fooB.Create().Dispose();
            fooBa.Create().Dispose();
            fooBar.Create().Dispose();
            fooBarr.Create().Dispose();
            fooX.Create().Dispose();
            fooXy.Create().Dispose();
            fooXyz.Create().Dispose();
            barBaz.Create().Dispose();

            // Win32: foo.??? matches foo (no ext), foo.b, foo.ba, foo.bar, foo.x, foo.xy, foo.xyz
            // because DOS_QM (?) can match 0-1 chars at end of name/extension
            string[] paths = GetPaths(testDirectory.FullName, "foo.???", new EnumerationOptions { MatchType = MatchType.Win32 });
            FSAssert.EqualWhenOrdered(new string[] {
                fooNoExt.FullName, fooB.FullName, fooBa.FullName, fooBar.FullName,
                fooX.FullName, fooXy.FullName, fooXyz.FullName
            }, paths);

            // Simple: foo.??? requires exactly 3 characters after the dot
            paths = GetPaths(testDirectory.FullName, "foo.???", new EnumerationOptions { MatchType = MatchType.Simple });
            FSAssert.EqualWhenOrdered(new string[] { fooBar.FullName, fooXyz.FullName }, paths);
        }

        [Fact]
        public void OverlappingExtensions_Win32TwoQuestionMarks()
        {
            // Tests foo.?? pattern with overlapping extensions

            DirectoryInfo testDirectory = Directory.CreateDirectory(GetTestFilePath());
            FileInfo fooNoExt = new FileInfo(Path.Combine(testDirectory.FullName, "foo"));
            FileInfo fooB = new FileInfo(Path.Combine(testDirectory.FullName, "foo.b"));
            FileInfo fooBa = new FileInfo(Path.Combine(testDirectory.FullName, "foo.ba"));
            FileInfo fooBar = new FileInfo(Path.Combine(testDirectory.FullName, "foo.bar"));
            FileInfo barBa = new FileInfo(Path.Combine(testDirectory.FullName, "bar.ba"));

            fooNoExt.Create().Dispose();
            fooB.Create().Dispose();
            fooBa.Create().Dispose();
            fooBar.Create().Dispose();
            barBa.Create().Dispose();

            // Win32: foo.?? matches foo (no ext), foo.b, foo.ba due to DOS_QM semantics
            string[] paths = GetPaths(testDirectory.FullName, "foo.??", new EnumerationOptions { MatchType = MatchType.Win32 });
            FSAssert.EqualWhenOrdered(new string[] { fooNoExt.FullName, fooB.FullName, fooBa.FullName }, paths);

            // Simple: foo.?? requires exactly 2 characters after the dot
            paths = GetPaths(testDirectory.FullName, "foo.??", new EnumerationOptions { MatchType = MatchType.Simple });
            FSAssert.EqualWhenOrdered(new string[] { fooBa.FullName }, paths);
        }

        [Fact]
        public void OverlappingExtensions_AllMatchTypes_Comprehensive()
        {
            // Comprehensive test ensuring no false positives or false negatives
            // Tests multiple patterns against the same file set

            DirectoryInfo testDirectory = Directory.CreateDirectory(GetTestFilePath());

            // Create a comprehensive set of files with overlapping patterns
            string[] allFiles = { "foo", "foo.b", "foo.ba", "foo.bar", "foo.bars", "foo.x", "foo.xy", "foo.xyz",
                                  "bar", "bar.b", "bar.ba", "bar.baz", "baz.txt", "qux" };
            foreach (string fileName in allFiles)
            {
                File.Create(Path.Combine(testDirectory.FullName, fileName)).Dispose();
            }

            // Test pattern: *.bar (exact extension match - should be same for both modes)
            string[] win32Paths = GetPaths(testDirectory.FullName, "*.bar", new EnumerationOptions { MatchType = MatchType.Win32 });
            string[] simplePaths = GetPaths(testDirectory.FullName, "*.bar", new EnumerationOptions { MatchType = MatchType.Simple });
            FSAssert.EqualWhenOrdered(win32Paths, simplePaths);
            FSAssert.EqualWhenOrdered(new string[] { Path.Combine(testDirectory.FullName, "foo.bar") }, win32Paths);

            // Test pattern: *.b (exact single char extension - should be same for both)
            win32Paths = GetPaths(testDirectory.FullName, "*.b", new EnumerationOptions { MatchType = MatchType.Win32 });
            simplePaths = GetPaths(testDirectory.FullName, "*.b", new EnumerationOptions { MatchType = MatchType.Simple });
            FSAssert.EqualWhenOrdered(win32Paths, simplePaths);
            FSAssert.EqualWhenOrdered(new string[] {
                Path.Combine(testDirectory.FullName, "bar.b"),
                Path.Combine(testDirectory.FullName, "foo.b")
            }, win32Paths);

            // Test pattern: foo (exact match - no wildcards)
            win32Paths = GetPaths(testDirectory.FullName, "foo", new EnumerationOptions { MatchType = MatchType.Win32 });
            simplePaths = GetPaths(testDirectory.FullName, "foo", new EnumerationOptions { MatchType = MatchType.Simple });
            FSAssert.EqualWhenOrdered(win32Paths, simplePaths);
            FSAssert.EqualWhenOrdered(new string[] { Path.Combine(testDirectory.FullName, "foo") }, win32Paths);

            // Test pattern: foo* (prefix match - should be same for both)
            win32Paths = GetPaths(testDirectory.FullName, "foo*", new EnumerationOptions { MatchType = MatchType.Win32 });
            simplePaths = GetPaths(testDirectory.FullName, "foo*", new EnumerationOptions { MatchType = MatchType.Simple });
            FSAssert.EqualWhenOrdered(win32Paths, simplePaths);
            // Should match foo, foo.b, foo.ba, foo.bar, foo.bars, foo.x, foo.xy, foo.xyz
            Assert.Equal(8, win32Paths.Length);
            Assert.Contains(Path.Combine(testDirectory.FullName, "foo"), win32Paths);
            Assert.Contains(Path.Combine(testDirectory.FullName, "foo.bar"), win32Paths);

            // Test that bar* does NOT match foo files (no false positives)
            win32Paths = GetPaths(testDirectory.FullName, "bar*", new EnumerationOptions { MatchType = MatchType.Win32 });
            Assert.Equal(4, win32Paths.Length); // bar, bar.b, bar.ba, bar.baz
            Assert.DoesNotContain(win32Paths, p => p.Contains("foo"));
        }

        [Fact]
        public void Win32_StarDot_MatchesNoExtension()
        {
            // *. pattern in Win32 mode matches files without extensions

            DirectoryInfo testDirectory = Directory.CreateDirectory(GetTestFilePath());
            FileInfo fooNoExt = new FileInfo(Path.Combine(testDirectory.FullName, "foo"));
            FileInfo fooB = new FileInfo(Path.Combine(testDirectory.FullName, "foo.b"));
            FileInfo barNoExt = new FileInfo(Path.Combine(testDirectory.FullName, "bar"));
            FileInfo bazTxt = new FileInfo(Path.Combine(testDirectory.FullName, "baz.txt"));

            fooNoExt.Create().Dispose();
            fooB.Create().Dispose();
            barNoExt.Create().Dispose();
            bazTxt.Create().Dispose();

            // Win32: *. matches files without extension
            string[] paths = GetPaths(testDirectory.FullName, "*.", new EnumerationOptions { MatchType = MatchType.Win32 });
            FSAssert.EqualWhenOrdered(new string[] { barNoExt.FullName, fooNoExt.FullName }, paths);

            // Simple: *. matches files ending with a period (none in this case without extended paths)
            paths = GetPaths(testDirectory.FullName, "*.", new EnumerationOptions { MatchType = MatchType.Simple });
            Assert.Empty(paths);
        }

        [Fact]
        public void Win32_QuestionMarkCollapsesToPeriod_InMiddleOfExtension()
        {
            // Tests that ? collapses properly in complex patterns

            DirectoryInfo testDirectory = Directory.CreateDirectory(GetTestFilePath());
            FileInfo file1 = new FileInfo(Path.Combine(testDirectory.FullName, "test.a1"));
            FileInfo file2 = new FileInfo(Path.Combine(testDirectory.FullName, "test.ab"));
            FileInfo file3 = new FileInfo(Path.Combine(testDirectory.FullName, "test.a"));
            FileInfo file4 = new FileInfo(Path.Combine(testDirectory.FullName, "test.abc"));

            file1.Create().Dispose();
            file2.Create().Dispose();
            file3.Create().Dispose();
            file4.Create().Dispose();

            // Win32: test.a? - matches test.a (? matches nothing at end), test.a1, test.ab
            string[] paths = GetPaths(testDirectory.FullName, "test.a?", new EnumerationOptions { MatchType = MatchType.Win32 });
            FSAssert.EqualWhenOrdered(new string[] { file3.FullName, file1.FullName, file2.FullName }, paths);

            // Simple: test.a? - must have exactly one char after 'a'
            paths = GetPaths(testDirectory.FullName, "test.a?", new EnumerationOptions { MatchType = MatchType.Simple });
            FSAssert.EqualWhenOrdered(new string[] { file1.FullName, file2.FullName }, paths);
        }

        [Fact]
        public void StarQuestionMarkCombinations()
        {
            // Tests *? and ?* pattern combinations

            DirectoryInfo testDirectory = Directory.CreateDirectory(GetTestFilePath());
            FileInfo a = new FileInfo(Path.Combine(testDirectory.FullName, "a.txt"));
            FileInfo ab = new FileInfo(Path.Combine(testDirectory.FullName, "ab.txt"));
            FileInfo abc = new FileInfo(Path.Combine(testDirectory.FullName, "abc.txt"));
            FileInfo abcd = new FileInfo(Path.Combine(testDirectory.FullName, "abcd.txt"));
            FileInfo x = new FileInfo(Path.Combine(testDirectory.FullName, "x.log"));  // Decoy - different extension
            FileInfo abcLog = new FileInfo(Path.Combine(testDirectory.FullName, "abc.log"));  // Decoy - different extension

            a.Create().Dispose();
            ab.Create().Dispose();
            abc.Create().Dispose();
            abcd.Create().Dispose();
            x.Create().Dispose();
            abcLog.Create().Dispose();

            // ?*.txt - at least one character then anything
            // Win32: matches a.txt, ab.txt, abc.txt, abcd.txt
            string[] paths = GetPaths(testDirectory.FullName, "?*.txt", new EnumerationOptions { MatchType = MatchType.Win32 });
            FSAssert.EqualWhenOrdered(new string[] { a.FullName, ab.FullName, abc.FullName, abcd.FullName }, paths);

            // Simple: same behavior
            paths = GetPaths(testDirectory.FullName, "?*.txt", new EnumerationOptions { MatchType = MatchType.Simple });
            FSAssert.EqualWhenOrdered(new string[] { a.FullName, ab.FullName, abc.FullName, abcd.FullName }, paths);

            // *?.txt - anything then at least one character
            // Win32: matches a.txt, ab.txt, abc.txt, abcd.txt
            paths = GetPaths(testDirectory.FullName, "*?.txt", new EnumerationOptions { MatchType = MatchType.Win32 });
            FSAssert.EqualWhenOrdered(new string[] { a.FullName, ab.FullName, abc.FullName, abcd.FullName }, paths);

            // Simple: same behavior
            paths = GetPaths(testDirectory.FullName, "*?.txt", new EnumerationOptions { MatchType = MatchType.Simple });
            FSAssert.EqualWhenOrdered(new string[] { a.FullName, ab.FullName, abc.FullName, abcd.FullName }, paths);

            // ??*.txt - at least two characters then anything in Simple; in Win32, ? can collapse to period
            // Win32: a.txt matches because first ? matches 'a', second ? collapses to the period
            paths = GetPaths(testDirectory.FullName, "??*.txt", new EnumerationOptions { MatchType = MatchType.Win32 });
            FSAssert.EqualWhenOrdered(new string[] { a.FullName, ab.FullName, abc.FullName, abcd.FullName }, paths);

            // Simple: requires exactly 2+ chars before the star
            paths = GetPaths(testDirectory.FullName, "??*.txt", new EnumerationOptions { MatchType = MatchType.Simple });
            FSAssert.EqualWhenOrdered(new string[] { ab.FullName, abc.FullName, abcd.FullName }, paths);
        }

        [Fact]
        public void QuestionMarkInMiddleOfName()
        {
            // Tests ? in the middle of filename (not just at start/end or in extension)

            DirectoryInfo testDirectory = Directory.CreateDirectory(GetTestFilePath());
            FileInfo fooFile = new FileInfo(Path.Combine(testDirectory.FullName, "foo.txt"));
            FileInfo fXoFile = new FileInfo(Path.Combine(testDirectory.FullName, "fXo.txt"));
            FileInfo f1oFile = new FileInfo(Path.Combine(testDirectory.FullName, "f1o.txt"));
            FileInfo fABoFile = new FileInfo(Path.Combine(testDirectory.FullName, "fABo.txt")); // Decoy - two chars in middle
            FileInfo fooo = new FileInfo(Path.Combine(testDirectory.FullName, "fooo.txt")); // Decoy - extra o
            FileInfo fo = new FileInfo(Path.Combine(testDirectory.FullName, "fo.txt")); // Decoy - missing middle char
            FileInfo bar = new FileInfo(Path.Combine(testDirectory.FullName, "bar.txt")); // Decoy - different name

            fooFile.Create().Dispose();
            fXoFile.Create().Dispose();
            f1oFile.Create().Dispose();
            fABoFile.Create().Dispose();
            fooo.Create().Dispose();
            fo.Create().Dispose();
            bar.Create().Dispose();

            // f?o.txt - exactly one char between f and o
            string[] paths = GetPaths(testDirectory.FullName, "f?o.txt", new EnumerationOptions { MatchType = MatchType.Win32 });
            FSAssert.EqualWhenOrdered(new string[] { f1oFile.FullName, fXoFile.FullName, fooFile.FullName }, paths);

            paths = GetPaths(testDirectory.FullName, "f?o.txt", new EnumerationOptions { MatchType = MatchType.Simple });
            FSAssert.EqualWhenOrdered(new string[] { f1oFile.FullName, fXoFile.FullName, fooFile.FullName }, paths);

            // f??o.txt - exactly two chars between f and o
            paths = GetPaths(testDirectory.FullName, "f??o.txt", new EnumerationOptions { MatchType = MatchType.Win32 });
            FSAssert.EqualWhenOrdered(new string[] { fABoFile.FullName, fooo.FullName }, paths);

            paths = GetPaths(testDirectory.FullName, "f??o.txt", new EnumerationOptions { MatchType = MatchType.Simple });
            FSAssert.EqualWhenOrdered(new string[] { fABoFile.FullName, fooo.FullName }, paths);
        }

        [Fact]
        public void MultipleExtensions()
        {
            // Tests patterns with files that have multiple dots (e.g., file.tar.gz)

            DirectoryInfo testDirectory = Directory.CreateDirectory(GetTestFilePath());
            FileInfo tarGz = new FileInfo(Path.Combine(testDirectory.FullName, "archive.tar.gz"));
            FileInfo tarBz2 = new FileInfo(Path.Combine(testDirectory.FullName, "archive.tar.bz2"));
            FileInfo justTar = new FileInfo(Path.Combine(testDirectory.FullName, "archive.tar"));
            FileInfo justGz = new FileInfo(Path.Combine(testDirectory.FullName, "file.gz"));
            FileInfo configBackup = new FileInfo(Path.Combine(testDirectory.FullName, "config.json.backup"));
            FileInfo noDots = new FileInfo(Path.Combine(testDirectory.FullName, "nodots")); // Decoy
            FileInfo singleDot = new FileInfo(Path.Combine(testDirectory.FullName, "single.ext")); // Decoy - only one dot

            tarGz.Create().Dispose();
            tarBz2.Create().Dispose();
            justTar.Create().Dispose();
            justGz.Create().Dispose();
            configBackup.Create().Dispose();
            noDots.Create().Dispose();
            singleDot.Create().Dispose();

            // *.tar.gz - exact double extension
            string[] paths = GetPaths(testDirectory.FullName, "*.tar.gz", new EnumerationOptions { MatchType = MatchType.Win32 });
            FSAssert.EqualWhenOrdered(new string[] { tarGz.FullName }, paths);

            paths = GetPaths(testDirectory.FullName, "*.tar.gz", new EnumerationOptions { MatchType = MatchType.Simple });
            FSAssert.EqualWhenOrdered(new string[] { tarGz.FullName }, paths);

            // *.tar.* - any file with .tar. in the middle (note: Win32 .* can match zero chars or missing extension)
            paths = GetPaths(testDirectory.FullName, "*.tar.*", new EnumerationOptions { MatchType = MatchType.Win32 });
            FSAssert.EqualWhenOrdered(new string[] { justTar.FullName, tarBz2.FullName, tarGz.FullName }, paths);

            paths = GetPaths(testDirectory.FullName, "*.tar.*", new EnumerationOptions { MatchType = MatchType.Simple });
            FSAssert.EqualWhenOrdered(new string[] { tarBz2.FullName, tarGz.FullName }, paths);

            // *.gz - matches both .gz and .tar.gz
            paths = GetPaths(testDirectory.FullName, "*.gz", new EnumerationOptions { MatchType = MatchType.Win32 });
            FSAssert.EqualWhenOrdered(new string[] { tarGz.FullName, justGz.FullName }, paths);

            paths = GetPaths(testDirectory.FullName, "*.gz", new EnumerationOptions { MatchType = MatchType.Simple });
            FSAssert.EqualWhenOrdered(new string[] { tarGz.FullName, justGz.FullName }, paths);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void ShortNameMatching()
        {
            // Tests that Win32 matching can match against 8.3 short names on Windows
            // This is important because Windows can generate short names for files with long names

            DirectoryInfo testDirectory = Directory.CreateDirectory(GetTestFilePath());

            // Create files with names that will generate 8.3 short names
            // Names > 8 chars or with special chars get short names like LONGFI~1.TXT
            FileInfo longName1 = new FileInfo(Path.Combine(testDirectory.FullName, "LongFileName1.txt"));
            FileInfo longName2 = new FileInfo(Path.Combine(testDirectory.FullName, "LongFileName2.txt"));
            FileInfo shortName = new FileInfo(Path.Combine(testDirectory.FullName, "short.txt"));
            FileInfo exactEight = new FileInfo(Path.Combine(testDirectory.FullName, "eightchr.txt")); // Exactly 8 chars - no short name needed

            longName1.Create().Dispose();
            longName2.Create().Dispose();
            shortName.Create().Dispose();
            exactEight.Create().Dispose();

            // LONGFI~*.* - should match files whose short name starts with LONGFI~
            // This tests that enumeration checks both long and short names
            string[] paths = GetPaths(testDirectory.FullName, "LONGFI~*.*", new EnumerationOptions { MatchType = MatchType.Win32 });

            // The exact results depend on whether 8.3 name generation is enabled on the volume
            // If enabled, should match longName1 and longName2; if disabled, might match nothing
            // We verify that we don't get false positives
            Assert.DoesNotContain(shortName.FullName, paths);
            Assert.DoesNotContain(exactEight.FullName, paths);

            // If short names are enabled, we should get matches
            if (paths.Length > 0)
            {
                Assert.True(paths.All(p => p.Contains("LongFileName")));
            }

            // Long* should match long file names by their actual name
            paths = GetPaths(testDirectory.FullName, "Long*", new EnumerationOptions { MatchType = MatchType.Win32 });
            FSAssert.EqualWhenOrdered(new string[] { longName1.FullName, longName2.FullName }, paths);
        }

        [Fact]
        public void LeadingDotPatterns()
        {
            // Tests patterns matching files that start with dots

            DirectoryInfo testDirectory = Directory.CreateDirectory(GetTestFilePath());
            FileInfo gitignore = new FileInfo(Path.Combine(testDirectory.FullName, ".gitignore"));
            FileInfo gitconfig = new FileInfo(Path.Combine(testDirectory.FullName, ".gitconfig"));
            FileInfo env = new FileInfo(Path.Combine(testDirectory.FullName, ".env"));
            FileInfo envLocal = new FileInfo(Path.Combine(testDirectory.FullName, ".env.local"));
            FileInfo doubleDot = new FileInfo(Path.Combine(testDirectory.FullName, "..hidden"));
            FileInfo dotA = new FileInfo(Path.Combine(testDirectory.FullName, ".a"));
            FileInfo dotAB = new FileInfo(Path.Combine(testDirectory.FullName, ".ab"));
            FileInfo normalFile = new FileInfo(Path.Combine(testDirectory.FullName, "normal.txt")); // Decoy
            FileInfo gitDir = new FileInfo(Path.Combine(testDirectory.FullName, "git.txt")); // Decoy - starts with "git" not ".git"

            gitignore.Create().Dispose();
            gitconfig.Create().Dispose();
            env.Create().Dispose();
            envLocal.Create().Dispose();
            doubleDot.Create().Dispose();
            dotA.Create().Dispose();
            dotAB.Create().Dispose();
            normalFile.Create().Dispose();
            gitDir.Create().Dispose();

            // .git* - files starting with .git
            string[] paths = GetPaths(testDirectory.FullName, ".git*", new EnumerationOptions { MatchType = MatchType.Win32, AttributesToSkip = 0 });
            FSAssert.EqualWhenOrdered(new string[] { gitconfig.FullName, gitignore.FullName }, paths);

            paths = GetPaths(testDirectory.FullName, ".git*", new EnumerationOptions { MatchType = MatchType.Simple, AttributesToSkip = 0 });
            FSAssert.EqualWhenOrdered(new string[] { gitconfig.FullName, gitignore.FullName }, paths);

            // .??? - files with dot followed by exactly 3 chars
            // Win32: .??? with DOS_QM collapses to dot, so .a, .ab, .env all match
            paths = GetPaths(testDirectory.FullName, ".???", new EnumerationOptions { MatchType = MatchType.Win32, AttributesToSkip = 0 });
            FSAssert.EqualWhenOrdered(new string[] { dotA.FullName, dotAB.FullName, env.FullName }, paths);

            // Simple: .??? must be exactly 3 chars after dot
            paths = GetPaths(testDirectory.FullName, ".???", new EnumerationOptions { MatchType = MatchType.Simple, AttributesToSkip = 0 });
            FSAssert.EqualWhenOrdered(new string[] { env.FullName }, paths);

            // .env* - matches .env and .env.local
            paths = GetPaths(testDirectory.FullName, ".env*", new EnumerationOptions { MatchType = MatchType.Win32, AttributesToSkip = 0 });
            FSAssert.EqualWhenOrdered(new string[] { env.FullName, envLocal.FullName }, paths);

            // ..* - files starting with two dots
            paths = GetPaths(testDirectory.FullName, "..*", new EnumerationOptions { MatchType = MatchType.Win32, AttributesToSkip = 0 });
            FSAssert.EqualWhenOrdered(new string[] { doubleDot.FullName }, paths);

            paths = GetPaths(testDirectory.FullName, "..*", new EnumerationOptions { MatchType = MatchType.Simple, AttributesToSkip = 0 });
            FSAssert.EqualWhenOrdered(new string[] { doubleDot.FullName }, paths);
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
