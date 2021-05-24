// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.IO.Tests
{
    public class DirectoryInfo_SymbolicLinks : BaseSymbolicLinks
    {
        [ConditionalTheory(nameof(CanCreateSymbolicLinks))]
        [InlineData(false)]
        [InlineData(true)]
        public void EnumerateDirectories_LinksWithCycles_ThrowsTooManyLevelsOfSymbolicLinks(bool recurse)
        {
            var options  = new EnumerationOptions() { RecurseSubdirectories = recurse };
            DirectoryInfo testDirectory = CreateDirectoryContainingSelfReferencingSymbolicLink();

            // Windows avoids accessing the cyclic symlink if we do not recurse
            if (OperatingSystem.IsWindows() && !recurse)
            {
                testDirectory.EnumerateDirectories("*", options).Count();
                testDirectory.GetDirectories("*", options).Count();
            }
            else
            {
                // Internally transforms the FileSystemEntry to a DirectoryInfo, which performs a disk hit on the cyclic symlink
                Assert.Throws<IOException>(() => testDirectory.EnumerateDirectories("*", options).Count());
                Assert.Throws<IOException>(() => testDirectory.GetDirectories("*", options).Count());
            }
        }

        [ConditionalTheory(nameof(CanCreateSymbolicLinks))]
        [InlineData(false)]
        [InlineData(true)]
        public void EnumerateFiles_LinksWithCycles_ThrowsTooManyLevelsOfSymbolicLinks(bool recurse)
        {
            var options  = new EnumerationOptions() { RecurseSubdirectories = recurse };
            DirectoryInfo testDirectory = CreateDirectoryContainingSelfReferencingSymbolicLink();

            // Windows avoids accessing the cyclic symlink if we do not recurse
            if (OperatingSystem.IsWindows() && !recurse)
            {
                testDirectory.EnumerateFiles("*", options).Count();
                testDirectory.GetFiles("*", options).Count();
            }
            else
            {
                // Internally transforms the FileSystemEntry to a FileInfo, which performs a disk hit on the cyclic symlink
                Assert.Throws<IOException>(() => testDirectory.EnumerateFiles("*", options).Count());
                Assert.Throws<IOException>(() => testDirectory.GetFiles("*", options).Count());
            }
        }

        [ConditionalTheory(nameof(CanCreateSymbolicLinks))]
        [InlineData(false)]
        [InlineData(true)]
        public void EnumerateFileSystemInfos_LinksWithCycles_ThrowsTooManyLevelsOfSymbolicLinks(bool recurse)
        {
            var options  = new EnumerationOptions() { RecurseSubdirectories = recurse };
            DirectoryInfo testDirectory = CreateDirectoryContainingSelfReferencingSymbolicLink();

            // Windows avoids accessing the cyclic symlink if we do not recurse
            if (OperatingSystem.IsWindows() && !recurse)
            {
                testDirectory.EnumerateFileSystemInfos("*", options).Count();
                testDirectory.GetFileSystemInfos("*", options).Count();
            }
            else
            {
                // Internally transforms the FileSystemEntry to a FileSystemInfo, which performs a disk hit on the cyclic symlink
                Assert.Throws<IOException>(() => testDirectory.EnumerateFileSystemInfos("*", options).Count());
                Assert.Throws<IOException>(() => testDirectory.GetFileSystemInfos("*", options).Count());
            }
        }
    }
}
