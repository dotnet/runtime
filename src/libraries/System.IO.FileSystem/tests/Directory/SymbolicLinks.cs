// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.IO.Tests
{
    public class Directory_SymbolicLinks : BaseSymbolicLinks
    {
        [Fact]
        public void EnumerateDirectories_LinksWithCycles_ShouldNotThrow()
        {
            DirectoryInfo testDirectory = CreateDirectorySymbolicLinkToItself();
            Assert.Equal(0, Directory.EnumerateDirectories(testDirectory.FullName).Count());
        }

        [Fact]
        public void EnumerateFiles_LinksWithCycles_ShouldNotThrow()
        {
            DirectoryInfo testDirectory = CreateDirectorySymbolicLinkToItself();
            Assert.Equal(1, Directory.EnumerateFiles(testDirectory.FullName).Count());
        }

        [Fact]
        public void EnumerateFileSystemEntries_LinksWithCycles_ShouldNotThrow()
        {
            DirectoryInfo testDirectory = CreateDirectorySymbolicLinkToItself();
            Assert.Equal(1, Directory.EnumerateFileSystemEntries(testDirectory.FullName).Count());
        }
    }
}