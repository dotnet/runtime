// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Xunit;

namespace System.IO.Tests.Enumeration
{
    public class PatternTransformTests_Directory : FileSystemTest
    {
        protected virtual string[] GetFiles(string directory, string pattern)
        {
            return Directory.GetFiles(directory, pattern);
        }

        protected virtual string[] GetFiles(string directory, string pattern, EnumerationOptions options)
        {
            return Directory.GetFiles(directory, pattern, options);
        }

        [Theory,
            InlineData("."),
            InlineData("*.*")]
        public void GetFiles_WildcardPatternIsTranslated(string pattern)
        {
            DirectoryInfo testDirectory = Directory.CreateDirectory(GetTestFilePath());
            FileInfo fileOne = new FileInfo(Path.Combine(testDirectory.FullName, "File.One"));
            FileInfo fileTwo = new FileInfo(Path.Combine(testDirectory.FullName, "FileTwo"));
            fileOne.Create().Dispose();
            fileTwo.Create().Dispose();
            string[] results = GetFiles(testDirectory.FullName, pattern);
            FSAssert.EqualWhenOrdered(new string[] { fileOne.FullName, fileTwo.FullName }, results);

            results = GetFiles(testDirectory.FullName, pattern, new EnumerationOptions { MatchType = MatchType.Win32 });
            FSAssert.EqualWhenOrdered(new string[] { fileOne.FullName, fileTwo.FullName }, results);
        }

        [Fact]
        public void GetFiles_WildcardPatternIsNotTranslated()
        {
            DirectoryInfo testDirectory = Directory.CreateDirectory(GetTestFilePath());
            FileInfo fileOne = new FileInfo(Path.Combine(testDirectory.FullName, "File.One"));
            FileInfo fileTwo = new FileInfo(Path.Combine(testDirectory.FullName, "FileTwo"));
            fileOne.Create().Dispose();
            fileTwo.Create().Dispose();
            string[] results = GetFiles(testDirectory.FullName, ".", new EnumerationOptions());
            Assert.Empty(results);

            results = GetFiles(testDirectory.FullName, "*.*", new EnumerationOptions());
            Assert.Equal(new string[] { fileOne.FullName }, results);
        }

        [Fact]
        public void GetFiles_EmptyPattern()
        {
            DirectoryInfo testDirectory = Directory.CreateDirectory(GetTestFilePath());
            FileInfo fileOne = new FileInfo(Path.Combine(testDirectory.FullName, "File.One"));
            FileInfo fileTwo = new FileInfo(Path.Combine(testDirectory.FullName, "FileTwo"));
            fileOne.Create().Dispose();
            fileTwo.Create().Dispose();

            // We allow for expression to be "foo\" which would translate to "foo\*".
            string[] results = GetFiles(testDirectory.Parent.FullName, testDirectory.Name + Path.DirectorySeparatorChar);
            FSAssert.EqualWhenOrdered(new string[] { fileOne.FullName, fileTwo.FullName }, results);

            results = GetFiles(testDirectory.Parent.FullName, testDirectory.Name + Path.AltDirectorySeparatorChar);
            FSAssert.EqualWhenOrdered(new string[] { fileOne.FullName, fileTwo.FullName }, results);

            results = GetFiles(testDirectory.FullName, string.Empty);
            FSAssert.EqualWhenOrdered(new string[] { fileOne.FullName, fileTwo.FullName }, results);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public void GetFiles_EmptyPattern_Unix()
        {
            DirectoryInfo testDirectory = Directory.CreateDirectory(GetTestFilePath());
            FileInfo fileOne = new FileInfo(Path.Combine(testDirectory.FullName, "File\\One"));
            FileInfo fileTwo = new FileInfo(Path.Combine(testDirectory.FullName, "FileTwo"));
            fileOne.Create().Dispose();
            fileTwo.Create().Dispose();

            // We allow for expression to be "foo\" which would translate to "foo\*". On Unix we should not be
            // considering the backslash as a directory separator.
            string[] results = GetFiles(testDirectory.FullName, "File\\One");
            Assert.Equal(new string[] { fileOne.FullName }, results);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public void GetFiles_ExtendedDosWildcards_Unix()
        {
            // The extended wildcards ('"', '<', and '>') should not be considered on Unix, even when doing DOS style matching.
            // Getting these behaviors requires using the FileSystemEnumerable/Enumerator directly.
            DirectoryInfo testDirectory = Directory.CreateDirectory(GetTestFilePath());
            FileInfo fileOne = new FileInfo(Path.Combine(testDirectory.FullName, "File\"One"));
            FileInfo fileTwo = new FileInfo(Path.Combine(testDirectory.FullName, "File<Two"));
            FileInfo fileThree = new FileInfo(Path.Combine(testDirectory.FullName, "File>Three"));
            fileOne.Create().Dispose();
            fileTwo.Create().Dispose();
            fileThree.Create().Dispose();

            string[] results = GetFiles(testDirectory.FullName, "*\"*");
            Assert.Equal(new string[] { fileOne.FullName }, results);
            results = GetFiles(testDirectory.FullName, "*<*");
            Assert.Equal(new string[] { fileTwo.FullName }, results);
            results = GetFiles(testDirectory.FullName, "*>*");
            Assert.Equal(new string[] { fileThree.FullName }, results);
        }

        [Fact]
        public void GetFiles_LiteralPattern()
        {
            DirectoryInfo testDirectory = Directory.CreateDirectory(GetTestFilePath());
            FileInfo exactMatch = new FileInfo(Path.Combine(testDirectory.FullName, "log.txt"));
            FileInfo noMatch1 = new FileInfo(Path.Combine(testDirectory.FullName, "log.txt.bak"));
            FileInfo noMatch2 = new FileInfo(Path.Combine(testDirectory.FullName, "mylog.txt"));
            FileInfo noMatch3 = new FileInfo(Path.Combine(testDirectory.FullName, "other.txt"));
            exactMatch.Create().Dispose();
            noMatch1.Create().Dispose();
            noMatch2.Create().Dispose();
            noMatch3.Create().Dispose();

            string[] results = GetFiles(testDirectory.FullName, "log.txt");
            Assert.Equal(new string[] { exactMatch.FullName }, results);

            results = GetFiles(testDirectory.FullName, "log.txt", new EnumerationOptions { MatchType = MatchType.Simple });
            Assert.Equal(new string[] { exactMatch.FullName }, results);
        }

        [Fact]
        public void GetFiles_StartsWithPattern()
        {
            DirectoryInfo testDirectory = Directory.CreateDirectory(GetTestFilePath());
            FileInfo prefixOne = new FileInfo(Path.Combine(testDirectory.FullName, "prefixOne.txt"));
            FileInfo prefixTwo = new FileInfo(Path.Combine(testDirectory.FullName, "prefixTwo.txt"));
            FileInfo other = new FileInfo(Path.Combine(testDirectory.FullName, "other.txt"));
            prefixOne.Create().Dispose();
            prefixTwo.Create().Dispose();
            other.Create().Dispose();

            string[] results = GetFiles(testDirectory.FullName, "prefix*");
            FSAssert.EqualWhenOrdered(new string[] { prefixOne.FullName, prefixTwo.FullName }, results);

            results = GetFiles(testDirectory.FullName, "prefix*", new EnumerationOptions { MatchType = MatchType.Simple });
            FSAssert.EqualWhenOrdered(new string[] { prefixOne.FullName, prefixTwo.FullName }, results);
        }

        [Fact]
        public void GetFiles_EndsWithPattern()
        {
            DirectoryInfo testDirectory = Directory.CreateDirectory(GetTestFilePath());
            FileInfo txtOne = new FileInfo(Path.Combine(testDirectory.FullName, "one.txt"));
            FileInfo txtTwo = new FileInfo(Path.Combine(testDirectory.FullName, "two.txt"));
            FileInfo logFile = new FileInfo(Path.Combine(testDirectory.FullName, "app.log"));
            txtOne.Create().Dispose();
            txtTwo.Create().Dispose();
            logFile.Create().Dispose();

            string[] results = GetFiles(testDirectory.FullName, "*.txt");
            FSAssert.EqualWhenOrdered(new string[] { txtOne.FullName, txtTwo.FullName }, results);

            results = GetFiles(testDirectory.FullName, "*.txt", new EnumerationOptions { MatchType = MatchType.Simple });
            FSAssert.EqualWhenOrdered(new string[] { txtOne.FullName, txtTwo.FullName }, results);
        }

        [Fact]
        public void GetFiles_ContainsPattern()
        {
            DirectoryInfo testDirectory = Directory.CreateDirectory(GetTestFilePath());
            FileInfo middleOne = new FileInfo(Path.Combine(testDirectory.FullName, "amiddleb.txt"));
            FileInfo middleTwo = new FileInfo(Path.Combine(testDirectory.FullName, "xmiddley.log"));
            FileInfo noMatch = new FileInfo(Path.Combine(testDirectory.FullName, "other.txt"));
            middleOne.Create().Dispose();
            middleTwo.Create().Dispose();
            noMatch.Create().Dispose();

            string[] results = GetFiles(testDirectory.FullName, "*middle*");
            FSAssert.EqualWhenOrdered(new string[] { middleOne.FullName, middleTwo.FullName }, results);

            results = GetFiles(testDirectory.FullName, "*middle*", new EnumerationOptions { MatchType = MatchType.Simple });
            FSAssert.EqualWhenOrdered(new string[] { middleOne.FullName, middleTwo.FullName }, results);
        }

        [Fact]
        public void GetFiles_PrefixStarSuffixPattern()
        {
            DirectoryInfo testDirectory = Directory.CreateDirectory(GetTestFilePath());
            FileInfo match1 = new FileInfo(Path.Combine(testDirectory.FullName, "file.txt"));
            FileInfo match2 = new FileInfo(Path.Combine(testDirectory.FullName, "file123.txt"));
            FileInfo match3 = new FileInfo(Path.Combine(testDirectory.FullName, "file_extra.txt"));
            FileInfo noMatch1 = new FileInfo(Path.Combine(testDirectory.FullName, "file.log"));
            FileInfo noMatch2 = new FileInfo(Path.Combine(testDirectory.FullName, "other.txt"));
            match1.Create().Dispose();
            match2.Create().Dispose();
            match3.Create().Dispose();
            noMatch1.Create().Dispose();
            noMatch2.Create().Dispose();

            string[] results = GetFiles(testDirectory.FullName, "file*.txt");
            FSAssert.EqualWhenOrdered(new string[] { match1.FullName, match2.FullName, match3.FullName }, results);

            results = GetFiles(testDirectory.FullName, "file*.txt", new EnumerationOptions { MatchType = MatchType.Simple });
            FSAssert.EqualWhenOrdered(new string[] { match1.FullName, match2.FullName, match3.FullName }, results);
        }

        /// <summary>
        /// Tests pattern matching end-to-end through actual file enumeration using the same theory data
        /// as the FileSystemName tests. Creates a file with the given name and verifies the pattern
        /// matches or doesn't match through actual directory enumeration.
        /// </summary>
        [Theory]
        [MemberData(nameof(FileSystemNameTests.SimpleMatchData), MemberType = typeof(FileSystemNameTests))]
        public void EnumerateFiles_SimpleMatch_EndToEnd(string expression, string name, bool ignoreCase, bool expected)
        {
            // Skip null expression and empty name cases as they can't be used for file creation
            if (expression == null || string.IsNullOrEmpty(name))
                return;

            // Skip patterns with characters that are invalid for file names
            if (name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                return;

            // Skip hidden file names (starting with .) on Unix as they have special enumeration behavior
            if (name.StartsWith('.'))
                return;

            // Skip patterns like "*.*" that have special translation rules that differ from direct name matching
            if (expression == "*.*" || expression == ".")
                return;

            DirectoryInfo testDirectory = Directory.CreateDirectory(GetTestFilePath());
            FileInfo testFile = new FileInfo(Path.Combine(testDirectory.FullName, name));
            testFile.Create().Dispose();

            EnumerationOptions options = new EnumerationOptions
            {
                MatchType = MatchType.Simple,
                MatchCasing = ignoreCase ? MatchCasing.CaseInsensitive : MatchCasing.CaseSensitive
            };

            string[] results = GetFiles(testDirectory.FullName, expression, options);
            if (expected)
            {
                Assert.Single(results);
                Assert.Equal(testFile.FullName, results[0]);
            }
            else
            {
                Assert.Empty(results);
            }
        }

        /// <summary>
        /// Tests Win32 pattern matching end-to-end through actual file enumeration using the same theory data
        /// as the FileSystemName tests.
        /// </summary>
        [Theory]
        [MemberData(nameof(FileSystemNameTests.SimpleMatchData), MemberType = typeof(FileSystemNameTests))]
        [MemberData(nameof(FileSystemNameTests.Win32MatchData), MemberType = typeof(FileSystemNameTests))]
        public void EnumerateFiles_Win32Match_EndToEnd(string expression, string name, bool ignoreCase, bool expected)
        {
            // Skip null expression and empty name cases as they can't be used for file creation
            if (expression == null || string.IsNullOrEmpty(name))
                return;

            // Skip patterns with characters that are invalid for file names
            if (name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                return;

            // Skip Win32-specific patterns that use internal DOS characters (", <, >) as they won't work through the public API
            if (expression.IndexOfAny(new[] { '"', '<', '>' }) >= 0)
                return;

            // Skip hidden file names (starting with .) on Unix as they have special enumeration behavior
            if (name.StartsWith('.'))
                return;

            // Skip patterns like "*.*" that have special translation rules in Win32 mode
            // In Win32 mode, "*.*" and "." are translated to "*" which matches everything
            if (expression == "*.*" || expression == ".")
                return;

            DirectoryInfo testDirectory = Directory.CreateDirectory(GetTestFilePath());
            FileInfo testFile = new FileInfo(Path.Combine(testDirectory.FullName, name));
            testFile.Create().Dispose();

            EnumerationOptions options = new EnumerationOptions
            {
                MatchType = MatchType.Win32,
                MatchCasing = ignoreCase ? MatchCasing.CaseInsensitive : MatchCasing.CaseSensitive
            };

            string[] results = GetFiles(testDirectory.FullName, expression, options);
            if (expected)
            {
                Assert.Single(results);
                Assert.Equal(testFile.FullName, results[0]);
            }
            else
            {
                Assert.Empty(results);
            }
        }
    }

    public class PatternTransformTests_DirectoryInfo : PatternTransformTests_Directory
    {

        protected override string[] GetFiles(string directory, string pattern)
        {
            return new DirectoryInfo(directory).GetFiles(pattern).Select(i => i.FullName).ToArray();
        }

        protected override string[] GetFiles(string directory, string pattern, EnumerationOptions options)
        {
            return new DirectoryInfo(directory).GetFiles(pattern, options).Select(i => i.FullName).ToArray();
        }
    }
}
