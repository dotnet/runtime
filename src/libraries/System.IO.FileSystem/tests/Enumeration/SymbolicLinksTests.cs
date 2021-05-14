// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO.Enumeration;
using System.Linq;
using Xunit;

namespace System.IO.Tests.Enumeration
{
    public class Enumeration_SymbolicLinksTests : BaseSymbolicLinks
    {
        [Fact]
        public void EnumerateDirectories_LinksWithCycles_ShouldNotThrow()
        {
            DirectoryInfo testDirectory = CreateDirectorySymbolicLinkToItself();

            IEnumerable<string> enumerable = new FileSystemEnumerable<string>(
                 testDirectory.FullName,
                 (ref FileSystemEntry entry) => entry.ToFullPath(),
                 // Skipping attributes forces a disk hit which enters the cyclic symlink
                 new EnumerationOptions(){ AttributesToSkip = 0 })
                 {
                     ShouldIncludePredicate = (ref FileSystemEntry entry) => entry.IsDirectory
                 };

            Assert.Equal(0, enumerable.Count());
        }

        [Fact]
        public void EnumerateFiles_LinksWithCycles_ShouldNotThrow()
        {
            DirectoryInfo testDirectory = CreateDirectorySymbolicLinkToItself();

            IEnumerable<string> enumerable = new FileSystemEnumerable<string>(
                 testDirectory.FullName,
                 (ref FileSystemEntry entry) => entry.ToFullPath(),
                 // Skipping attributes forces a disk hit which enters the cyclic symlink
                 new EnumerationOptions(){ AttributesToSkip = 0 })
                 {
                     ShouldIncludePredicate = (ref FileSystemEntry entry) => !entry.IsDirectory
                 };

            Assert.Equal(1, enumerable.Count());
        }

        [Fact]
        public void EnumerateFileSystemEntries_LinksWithCycles_ShouldNotThrow()
        {
            DirectoryInfo testDirectory = CreateDirectorySymbolicLinkToItself();

            IEnumerable<string> enumerable = new FileSystemEnumerable<string>(
                 testDirectory.FullName,
                 (ref FileSystemEntry entry) => entry.ToFullPath(),
                 // Skipping attributes forces a disk hit which enters the cyclic symlink
                 new EnumerationOptions(){ AttributesToSkip = 0 });

            Assert.Equal(1, enumerable.Count());
        }
    }
}