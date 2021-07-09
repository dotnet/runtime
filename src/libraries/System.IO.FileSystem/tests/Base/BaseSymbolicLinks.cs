// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.IO.Tests
{
    public abstract class BaseSymbolicLinks : FileSystemTest
    {
        protected DirectoryInfo CreateDirectoryContainingSelfReferencingSymbolicLink()
        {
            DirectoryInfo testDirectory = Directory.CreateDirectory(GetTestFilePath());
            string pathToLink = Path.Join(testDirectory.FullName, GetTestFileName());
            Assert.True(MountHelper.CreateSymbolicLink(pathToLink, pathToLink, isDirectory: true)); // Create a symlink cycle
            return testDirectory;
        }
    }
}
