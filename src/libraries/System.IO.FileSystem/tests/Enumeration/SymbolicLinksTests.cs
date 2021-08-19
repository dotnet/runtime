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

        [ConditionalTheory(nameof(CanCreateSymbolicLinks))]
        [InlineData(false, false)] // OK
        [InlineData(false, true)] // throw
        [InlineData(true, false)] // throw, OK on Unix
        [InlineData(true, true)] // throw
        public void EnumerateGet_SelfReferencingLink_Instance(bool recurse, bool linkAsRoot)
        {
            var options = new EnumerationOptions() { RecurseSubdirectories = recurse };

            DirectoryInfo testDirectory = linkAsRoot ?
                CreateSelfReferencingSymbolicLink() :
                CreateDirectoryContainingSelfReferencingSymbolicLink();

            // Unix doesn't have a problem when it steps in a self-referencing link through the directory recursion.
            if ((!recurse || !OperatingSystem.IsWindows()) && !linkAsRoot)
            {
                testDirectory.EnumerateFileSystemInfos("*", options).Count();
                testDirectory.GetFileSystemInfos("*", options).Count();

                testDirectory.EnumerateDirectories("*", options).Count();
                testDirectory.GetDirectories("*", options).Count();

                testDirectory.EnumerateFiles("*", options).Count();
                testDirectory.GetFiles("*", options).Count();
            }
            else
            {
                Assert.Throws<IOException>(() => testDirectory.EnumerateFileSystemInfos("*", options).Count());
                Assert.Throws<IOException>(() => testDirectory.GetFileSystemInfos("*", options).Count());

                Assert.Throws<IOException>(() => testDirectory.EnumerateDirectories("*", options).Count());
                Assert.Throws<IOException>(() => testDirectory.GetDirectories("*", options).Count());

                Assert.Throws<IOException>(() => testDirectory.EnumerateFiles("*", options).Count());
                Assert.Throws<IOException>(() => testDirectory.GetFiles("*", options).Count());
            }
        }

        [ConditionalTheory(nameof(CanCreateSymbolicLinks))]
        [InlineData(false, false)] // OK
        [InlineData(false, true)] // throw
        [InlineData(true, false)] // throw, OK on Unix
        [InlineData(true, true)] // throw
        public void EnumerateGet_SelfReferencingLink_Static(bool recurse, bool linkAsRoot)
        {
            var options = new EnumerationOptions() { RecurseSubdirectories = recurse };

            DirectoryInfo testDirectory = linkAsRoot ?
                CreateSelfReferencingSymbolicLink() :
                CreateDirectoryContainingSelfReferencingSymbolicLink();

            // Unix doesn't have a problem when it steps in a self-referencing link through the directory recursion.
            if ((!recurse || !OperatingSystem.IsWindows()) && !linkAsRoot)
            {
                Directory.EnumerateFileSystemEntries(testDirectory.FullName, "*", options).Count();
                Directory.GetFileSystemEntries(testDirectory.FullName, "*", options).Count();

                Directory.EnumerateDirectories(testDirectory.FullName, "*", options).Count();
                Directory.GetDirectories(testDirectory.FullName, "*", options).Count();

                Directory.EnumerateFiles(testDirectory.FullName, "*", options).Count();
                Directory.GetFiles(testDirectory.FullName, "*", options).Count();
            }
            else
            {
                Assert.Throws<IOException>(() => Directory.EnumerateFileSystemEntries(testDirectory.FullName, "*", options).Count());
                Assert.Throws<IOException>(() => Directory.GetFileSystemEntries(testDirectory.FullName, "*", options).Count());

                Assert.Throws<IOException>(() => Directory.EnumerateDirectories(testDirectory.FullName, "*", options).Count());
                Assert.Throws<IOException>(() => Directory.GetDirectories(testDirectory.FullName, "*", options).Count());

                Assert.Throws<IOException>(() => Directory.EnumerateFiles(testDirectory.FullName, "*", options).Count());
                Assert.Throws<IOException>(() => Directory.GetFiles(testDirectory.FullName, "*", options).Count());
            }
        }
    }
}
