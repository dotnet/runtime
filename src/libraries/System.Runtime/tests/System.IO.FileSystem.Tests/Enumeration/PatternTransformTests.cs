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
