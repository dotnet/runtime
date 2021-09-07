// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.IO.Tests
{
    [ConditionalClass(typeof(BaseSymbolicLinks), nameof(CanCreateSymbolicLinks))]
    // Contains helper methods that are shared by all symbolic link test classes.
    public abstract partial class BaseSymbolicLinks : FileSystemTest
    {
        protected DirectoryInfo CreateDirectoryContainingSelfReferencingSymbolicLink()
        {
            DirectoryInfo testDirectory = Directory.CreateDirectory(GetRandomDirPath());
            string pathToLink = Path.Join(testDirectory.FullName, GetRandomDirName());
            Assert.True(MountHelper.CreateSymbolicLink(pathToLink, pathToLink, isDirectory: true)); // Create a symlink cycle
            return testDirectory;
        }

        protected DirectoryInfo CreateSelfReferencingSymbolicLink()
        {
            string path = GetRandomDirPath();
            return (DirectoryInfo)Directory.CreateSymbolicLink(path, path);
        }

        protected string GetRandomFileName() => GetTestFileName() + ".txt";
        protected string GetRandomLinkName() => GetTestFileName() + ".link";
        protected string GetRandomDirName()  => GetTestFileName() + "_dir";

        protected string GetRandomFilePath() => Path.Join(ActualTestDirectory.Value, GetRandomFileName());
        protected string GetRandomLinkPath() => Path.Join(ActualTestDirectory.Value, GetRandomLinkName());
        protected string GetRandomDirPath()  => Path.Join(ActualTestDirectory.Value, GetRandomDirName());

        private Lazy<string> ActualTestDirectory => new Lazy<string>(() => GetTestDirectoryActualCasing());

        /// <summary>
        /// Changes the current working directory path to a new temporary directory.
        /// Important: Make sure to call this inside a remote executor to avoid changing the cwd for all tests in same process.
        /// </summary>
        /// <returns>The path of the new cwd.</returns>
        protected string ChangeCurrentDirectory()
        {
            string tempCwd = GetRandomDirPath();
            Directory.CreateDirectory(tempCwd);
            Directory.SetCurrentDirectory(tempCwd);
            return tempCwd;
        }
    }
}
