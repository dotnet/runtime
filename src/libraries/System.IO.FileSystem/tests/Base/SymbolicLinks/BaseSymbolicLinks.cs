// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Xunit;

namespace System.IO.Tests
{
    [ConditionalClass(typeof(BaseSymbolicLinks), nameof(CanCreateSymbolicLinks))]
    // Contains helper methods that are shared by all symbolic link test classes.
    public abstract class BaseSymbolicLinks : FileSystemTest
    {
        protected DirectoryInfo CreateDirectoryContainingSelfReferencingSymbolicLink()
        {
            DirectoryInfo testDirectory = Directory.CreateDirectory(GetTestFilePath());
            string pathToLink = Path.Join(testDirectory.FullName, GetTestFileName());
            Assert.True(MountHelper.CreateSymbolicLink(pathToLink, pathToLink, isDirectory: true)); // Create a symlink cycle
            return testDirectory;
        }

        protected string GetRandomFileName() => GetTestFileName() + ".txt";
        protected string GetRandomLinkName() => GetTestFileName() + ".link";
        protected string GetRandomDirName()  => GetTestFileName() + "_dir";

        protected string GetRandomFilePath() => Path.Join(TestDirectory, GetRandomFileName());
        protected string GetRandomLinkPath() => Path.Join(TestDirectory, GetRandomLinkName());
        protected string GetRandomDirPath()  => Path.Join(TestDirectory, GetRandomDirName());

    }
}
