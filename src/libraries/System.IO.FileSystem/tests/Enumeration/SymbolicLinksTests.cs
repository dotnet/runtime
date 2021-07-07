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
        [ConditionalFact(nameof(CanCreateSymbolicLinks))]
        public void EnumerateDirectories_LinksWithCycles_ShouldNotThrow()
        {
            DirectoryInfo testDirectory = CreateDirectoryContainingSelfReferencingSymbolicLink();

            IEnumerable<string> enumerable = new FileSystemEnumerable<string>(
                testDirectory.FullName,
                (ref FileSystemEntry entry) => entry.ToFullPath(),
                // Skipping attributes would force a disk hit which enters the cyclic symlink
                new EnumerationOptions(){ AttributesToSkip = 0 })
            {
                ShouldIncludePredicate = (ref FileSystemEntry entry) => entry.IsDirectory
            };

            // Windows differentiates between dir symlinks and file symlinks
            int expected = OperatingSystem.IsWindows() ? 1 : 0;
            Assert.Equal(expected, enumerable.Count());
        }

        [ConditionalFact(nameof(CanCreateSymbolicLinks))]
        public void EnumerateFiles_LinksWithCycles_ShouldNotThrow()
        {
            DirectoryInfo testDirectory = CreateDirectoryContainingSelfReferencingSymbolicLink();

            IEnumerable<string> enumerable = new FileSystemEnumerable<string>(
                testDirectory.FullName,
                (ref FileSystemEntry entry) => entry.ToFullPath(),
                // Skipping attributes would force a disk hit which enters the cyclic symlink
                new EnumerationOptions(){ AttributesToSkip = 0 })
            {
                ShouldIncludePredicate = (ref FileSystemEntry entry) => !entry.IsDirectory
            };

            // Windows differentiates between dir symlinks and file symlinks
            int expected = OperatingSystem.IsWindows() ? 0 : 1;
            Assert.Equal(expected, enumerable.Count());
        }

        [ConditionalFact(nameof(CanCreateSymbolicLinks))]
        public void EnumerateFileSystemEntries_LinksWithCycles_ShouldNotThrow()
        {
            DirectoryInfo testDirectory = CreateDirectoryContainingSelfReferencingSymbolicLink();

            IEnumerable<string> enumerable = new FileSystemEnumerable<string>(
                testDirectory.FullName,
                (ref FileSystemEntry entry) => entry.ToFullPath(),
                // Skipping attributes would force a disk hit which enters the cyclic symlink
                new EnumerationOptions(){ AttributesToSkip = 0 });

            Assert.Single(enumerable);
        }
    }
}
