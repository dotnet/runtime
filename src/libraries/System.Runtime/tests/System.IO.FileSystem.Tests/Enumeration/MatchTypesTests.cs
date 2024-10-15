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
