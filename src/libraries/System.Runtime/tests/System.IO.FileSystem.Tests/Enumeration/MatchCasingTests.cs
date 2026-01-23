// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Xunit;

namespace System.IO.Tests.Enumeration
{
    public abstract class MatchCasingTests : FileSystemTest
    {
        protected abstract string[] GetPaths(string directory, string pattern, EnumerationOptions options);

        [Fact]
        public void EnumerationOptionsNull()
        {
            AssertExtensions.Throws<ArgumentNullException>("enumerationOptions", () => GetPaths(GetTestFilePath(), "file*", null));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // Test assumes Windows' case-insensitive filesystem
        public void MatchCase()
        {
            DirectoryInfo testDirectory = Directory.CreateDirectory(GetTestFilePath());
            DirectoryInfo testSubdirectory = Directory.CreateDirectory(Path.Combine(testDirectory.FullName, "Subdirectory"));
            FileInfo fileOne = new FileInfo(Path.Combine(testDirectory.FullName, "FileOne"));
            FileInfo fileTwo = new FileInfo(Path.Combine(testDirectory.FullName, "FileTwo"));
            FileInfo fileThree = new FileInfo(Path.Combine(testSubdirectory.FullName, "FileThree"));
            FileInfo fileFour = new FileInfo(Path.Combine(testSubdirectory.FullName, "FileFour"));

            fileOne.Create().Dispose();
            fileTwo.Create().Dispose();
            fileThree.Create().Dispose();
            fileFour.Create().Dispose();

            string[] paths = GetPaths(testDirectory.FullName, "file*", new EnumerationOptions { MatchCasing = MatchCasing.CaseSensitive, RecurseSubdirectories = true });

            Assert.Empty(paths);

            paths = GetPaths(testDirectory.FullName, "file*", new EnumerationOptions { MatchCasing = MatchCasing.CaseInsensitive, RecurseSubdirectories = true });
            FSAssert.EqualWhenOrdered(new string[] { fileOne.FullName, fileTwo.FullName, fileThree.FullName, fileFour.FullName }, paths);

            paths = GetPaths(testDirectory.FullName, "FileT*", new EnumerationOptions { MatchCasing = MatchCasing.CaseSensitive, RecurseSubdirectories = true });
            FSAssert.EqualWhenOrdered(new string[] { fileTwo.FullName, fileThree.FullName }, paths);

            paths = GetPaths(testDirectory.FullName, "File???", new EnumerationOptions { MatchCasing = MatchCasing.CaseSensitive, RecurseSubdirectories = true });
            FSAssert.EqualWhenOrdered(new string[] { fileOne.FullName, fileTwo.FullName }, paths);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // Test assumes Windows' case-insensitive filesystem
        public void MatchCasing_CombinedWithMatchType_Win32()
        {
            // Use distinct file names - can't rely on case-sensitive file system
            DirectoryInfo testDirectory = Directory.CreateDirectory(GetTestFilePath());
            FileInfo testFile = new FileInfo(Path.Combine(testDirectory.FullName, "test.txt"));
            FileInfo testNoExt = new FileInfo(Path.Combine(testDirectory.FullName, "test"));
            FileInfo otherFile = new FileInfo(Path.Combine(testDirectory.FullName, "Other.doc"));
            FileInfo abcFile = new FileInfo(Path.Combine(testDirectory.FullName, "ABC.txt"));

            testFile.Create().Dispose();
            testNoExt.Create().Dispose();
            otherFile.Create().Dispose();
            abcFile.Create().Dispose();

            // Win32 + CaseInsensitive: *.* matches everything (Win32 treats *.* as *)
            string[] paths = GetPaths(testDirectory.FullName, "*.*", new EnumerationOptions
            {
                MatchType = MatchType.Win32,
                MatchCasing = MatchCasing.CaseInsensitive
            });
            FSAssert.EqualWhenOrdered(new string[] { abcFile.FullName, otherFile.FullName, testFile.FullName, testNoExt.FullName }, paths);

            // Win32 + CaseSensitive: *.* still matches everything (Win32 treats *.* as *)
            paths = GetPaths(testDirectory.FullName, "*.*", new EnumerationOptions
            {
                MatchType = MatchType.Win32,
                MatchCasing = MatchCasing.CaseSensitive
            });
            FSAssert.EqualWhenOrdered(new string[] { abcFile.FullName, otherFile.FullName, testFile.FullName, testNoExt.FullName }, paths);

            // Win32 + CaseInsensitive: test* matches test files regardless of pattern case
            paths = GetPaths(testDirectory.FullName, "TEST*", new EnumerationOptions
            {
                MatchType = MatchType.Win32,
                MatchCasing = MatchCasing.CaseInsensitive
            });
            FSAssert.EqualWhenOrdered(new string[] { testFile.FullName, testNoExt.FullName }, paths);

            // Win32 + CaseSensitive: TEST* doesn't match lowercase "test" files
            paths = GetPaths(testDirectory.FullName, "TEST*", new EnumerationOptions
            {
                MatchType = MatchType.Win32,
                MatchCasing = MatchCasing.CaseSensitive
            });
            Assert.Empty(paths);

            // Win32 + CaseSensitive: test* matches lowercase "test" files
            paths = GetPaths(testDirectory.FullName, "test*", new EnumerationOptions
            {
                MatchType = MatchType.Win32,
                MatchCasing = MatchCasing.CaseSensitive
            });
            FSAssert.EqualWhenOrdered(new string[] { testFile.FullName, testNoExt.FullName }, paths);

            // Win32 + CaseSensitive: ABC* matches uppercase ABC file
            paths = GetPaths(testDirectory.FullName, "ABC*", new EnumerationOptions
            {
                MatchType = MatchType.Win32,
                MatchCasing = MatchCasing.CaseSensitive
            });
            FSAssert.EqualWhenOrdered(new string[] { abcFile.FullName }, paths);

            // Win32 + CaseSensitive: abc* doesn't match uppercase ABC file
            paths = GetPaths(testDirectory.FullName, "abc*", new EnumerationOptions
            {
                MatchType = MatchType.Win32,
                MatchCasing = MatchCasing.CaseSensitive
            });
            Assert.Empty(paths);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // Test assumes Windows' case-insensitive filesystem
        public void MatchCasing_CombinedWithMatchType_Simple()
        {
            // Use distinct file names - can't rely on case-sensitive file system
            DirectoryInfo testDirectory = Directory.CreateDirectory(GetTestFilePath());
            FileInfo testFile = new FileInfo(Path.Combine(testDirectory.FullName, "test.txt"));
            FileInfo testNoExt = new FileInfo(Path.Combine(testDirectory.FullName, "test"));
            FileInfo otherFile = new FileInfo(Path.Combine(testDirectory.FullName, "Other.doc"));
            FileInfo abcFile = new FileInfo(Path.Combine(testDirectory.FullName, "ABC.txt"));

            testFile.Create().Dispose();
            testNoExt.Create().Dispose();
            otherFile.Create().Dispose();
            abcFile.Create().Dispose();

            // Simple + CaseInsensitive: *.* only matches files with a dot
            string[] paths = GetPaths(testDirectory.FullName, "*.*", new EnumerationOptions
            {
                MatchType = MatchType.Simple,
                MatchCasing = MatchCasing.CaseInsensitive
            });
            FSAssert.EqualWhenOrdered(new string[] { abcFile.FullName, otherFile.FullName, testFile.FullName }, paths);

            // Simple + CaseSensitive: *.* only matches files with a dot
            paths = GetPaths(testDirectory.FullName, "*.*", new EnumerationOptions
            {
                MatchType = MatchType.Simple,
                MatchCasing = MatchCasing.CaseSensitive
            });
            FSAssert.EqualWhenOrdered(new string[] { abcFile.FullName, otherFile.FullName, testFile.FullName }, paths);

            // Simple + CaseInsensitive: test* matches test files regardless of pattern case
            paths = GetPaths(testDirectory.FullName, "TEST*", new EnumerationOptions
            {
                MatchType = MatchType.Simple,
                MatchCasing = MatchCasing.CaseInsensitive
            });
            FSAssert.EqualWhenOrdered(new string[] { testFile.FullName, testNoExt.FullName }, paths);

            // Simple + CaseSensitive: TEST* doesn't match lowercase "test" files
            paths = GetPaths(testDirectory.FullName, "TEST*", new EnumerationOptions
            {
                MatchType = MatchType.Simple,
                MatchCasing = MatchCasing.CaseSensitive
            });
            Assert.Empty(paths);

            // Simple + CaseSensitive: test* matches lowercase "test" files
            paths = GetPaths(testDirectory.FullName, "test*", new EnumerationOptions
            {
                MatchType = MatchType.Simple,
                MatchCasing = MatchCasing.CaseSensitive
            });
            FSAssert.EqualWhenOrdered(new string[] { testFile.FullName, testNoExt.FullName }, paths);

            // Simple + CaseSensitive: ABC* matches uppercase ABC file
            paths = GetPaths(testDirectory.FullName, "ABC*", new EnumerationOptions
            {
                MatchType = MatchType.Simple,
                MatchCasing = MatchCasing.CaseSensitive
            });
            FSAssert.EqualWhenOrdered(new string[] { abcFile.FullName }, paths);

            // Simple + CaseSensitive: abc* doesn't match uppercase ABC file
            paths = GetPaths(testDirectory.FullName, "abc*", new EnumerationOptions
            {
                MatchType = MatchType.Simple,
                MatchCasing = MatchCasing.CaseSensitive
            });
            Assert.Empty(paths);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // Test assumes Windows' case-insensitive filesystem
        public void MatchCasing_QuestionMarkPattern_CombinedWithMatchType()
        {
            // Use distinct file names - can't rely on case-sensitive file system
            DirectoryInfo testDirectory = Directory.CreateDirectory(GetTestFilePath());
            FileInfo aFile = new FileInfo(Path.Combine(testDirectory.FullName, "a.txt"));
            FileInfo bFile = new FileInfo(Path.Combine(testDirectory.FullName, "B.TXT"));
            FileInfo abFile = new FileInfo(Path.Combine(testDirectory.FullName, "ab.txt"));
            FileInfo cdFile = new FileInfo(Path.Combine(testDirectory.FullName, "CD.TXT"));
            FileInfo dotFile = new FileInfo(Path.Combine(testDirectory.FullName, ".txt"));

            aFile.Create().Dispose();
            bFile.Create().Dispose();
            abFile.Create().Dispose();
            cdFile.Create().Dispose();
            dotFile.Create().Dispose();

            // Win32 + CaseInsensitive: ?.txt - DOS_QM can skip to dot, matches single char + .txt and .txt
            string[] paths = GetPaths(testDirectory.FullName, "?.txt", new EnumerationOptions
            {
                MatchType = MatchType.Win32,
                MatchCasing = MatchCasing.CaseInsensitive
            });
            FSAssert.EqualWhenOrdered(new string[] { dotFile.FullName, aFile.FullName, bFile.FullName }, paths);

            // Win32 + CaseSensitive: ?.txt - only matches lowercase .txt extension pattern
            paths = GetPaths(testDirectory.FullName, "?.txt", new EnumerationOptions
            {
                MatchType = MatchType.Win32,
                MatchCasing = MatchCasing.CaseSensitive
            });
            FSAssert.EqualWhenOrdered(new string[] { dotFile.FullName, aFile.FullName }, paths);

            // Win32 + CaseSensitive: ?.TXT - only matches uppercase .TXT extension pattern
            paths = GetPaths(testDirectory.FullName, "?.TXT", new EnumerationOptions
            {
                MatchType = MatchType.Win32,
                MatchCasing = MatchCasing.CaseSensitive
            });
            FSAssert.EqualWhenOrdered(new string[] { bFile.FullName }, paths);

            // Simple + CaseInsensitive: ?.txt - must have exactly one char before .txt
            paths = GetPaths(testDirectory.FullName, "?.txt", new EnumerationOptions
            {
                MatchType = MatchType.Simple,
                MatchCasing = MatchCasing.CaseInsensitive
            });
            FSAssert.EqualWhenOrdered(new string[] { aFile.FullName, bFile.FullName }, paths);

            // Simple + CaseSensitive: ?.txt - must have exactly one char before .txt, case must match
            paths = GetPaths(testDirectory.FullName, "?.txt", new EnumerationOptions
            {
                MatchType = MatchType.Simple,
                MatchCasing = MatchCasing.CaseSensitive
            });
            FSAssert.EqualWhenOrdered(new string[] { aFile.FullName }, paths);

            // Simple + CaseSensitive: ?.TXT - must have exactly one char before .TXT, case must match
            paths = GetPaths(testDirectory.FullName, "?.TXT", new EnumerationOptions
            {
                MatchType = MatchType.Simple,
                MatchCasing = MatchCasing.CaseSensitive
            });
            FSAssert.EqualWhenOrdered(new string[] { bFile.FullName }, paths);
        }
    }

    public class MatchCasingTests_Directory_GetFiles : MatchCasingTests
    {
        protected override string[] GetPaths(string directory, string pattern, EnumerationOptions options)
        {
            return Directory.GetFiles(directory, pattern, options);
        }
    }

    public class MatchCasingTests_DirectoryInfo_GetFiles : MatchCasingTests
    {
        protected override string[] GetPaths(string directory, string pattern, EnumerationOptions options)
        {
            return new DirectoryInfo(directory).GetFiles(pattern, options).Select(i => i.FullName).ToArray();
        }
    }
}
