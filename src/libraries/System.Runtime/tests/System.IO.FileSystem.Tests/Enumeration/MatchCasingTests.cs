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
